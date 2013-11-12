using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CobaltAHK.ExpressionTree
{
	public class Scope
	{
		public Scope() : this(null)
		{
			LoadBuiltinFunctions();
			LoadBuiltinVariables();
		}

		public Scope(Scope parentScope)
		{
			parent = parentScope;
		}

		protected Scope(Scope parentScope, LabelTarget ret, LabelTarget con, LabelTarget brk)
		: this(parentScope)
		{
			returnTarget = ret;
			continueTarget = con;
			breakTarget = brk;
		}

		private void LoadBuiltinFunctions()
		{
			var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly;
			var methods = typeof(IronAHK.Rusty.Core).GetMethods(flags).Where(m => !m.IsSpecialName);

			foreach (var method in methods) {
				// todo: filter by attribute?
				var paramList = method.GetParameters();
				if (HasFunction(method.Name)) {
					continue; // skips overloads // todo: support!
				}

				var prms  = paramList.Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
				var types = paramList.Select(p => p.ParameterType).ToList();
				types.Add(method.ReturnType);

				var lambda = Expression.Lambda(Expression.Call(method, prms), prms);
				AddFunctionName(method.Name);
				AddFunction(method.Name, lambda);
			}
		}

		protected readonly Scope parent;

		public bool IsRoot { get { return parent == null; } }

		#region scopes

		protected readonly IDictionary<Expressions.Expression, Scope> scopes = new Dictionary<Expressions.Expression, Scope>();

		public void AddScope(Expressions.Expression source, Scope scope)
		{
			scopes[source] = scope;
		}

		public Scope GetScope(Expressions.Expression source)
		{
			return scopes[source];
		}

		#endregion

		#region functions

		protected readonly IDictionary<string, LambdaExpression> functions = new Dictionary<string, LambdaExpression>();

		public virtual void AddFunctionName(string name)
		{
			functions.Add(name.ToLower(), null);
		}

		public virtual void AddFunction(string name, LambdaExpression func) {
			if (!functions.ContainsKey(name.ToLower())) {
				throw new Exception(); // todo
			}
			functions[name.ToLower()] = func;
		}

		public virtual bool HasFunction(string name)
		{
			return functions.ContainsKey(name.ToLower());
		}

		public virtual bool FunctionExists(string name)
		{
			return HasFunction(name) || (!IsRoot && parent.FunctionExists(name));
		}

		public virtual bool IsFunctionDefined(string name)
		{
			if (IsRoot && !HasFunction(name)) {
				throw new Exception(); // todo
			}
			return HasFunction(name) ? functions[name.ToLower()] != null : parent.IsFunctionDefined(name);
		}

		public virtual LambdaExpression ResolveFunction(string name)
		{
			if (HasFunction(name)) {
				if (functions[name.ToLower()] != null) {
					return functions[name.ToLower()];

				} else { // name is stored, but func not yet generated
					throw new UndefinedFunctionException(name); // todo
				}

			} else if (!IsRoot) {
				return parent.ResolveFunction(name);
			}
			throw new FunctionNotFoundException(name);
		}

		#endregion

		#region variables

		protected readonly ISet<ParameterExpression> allVariables = new HashSet<ParameterExpression>();

		protected readonly IDictionary<string, ParameterExpression> variables = new Dictionary<string, ParameterExpression>();

		public virtual IEnumerable<ParameterExpression> GetVariables()
		{
			return allVariables;
		}

		public virtual void AddVariable(string name, ParameterExpression variable)
		{
			// todo: add VariableScope param; depending on it add it on parent scope or here (and override)
			variables[name.ToLower()] = variable;
			allVariables.Add(variable);
		}

		protected virtual bool HasVariable(string name)
		{
			return variables.ContainsKey(name.ToLower());
		}

		public virtual ParameterExpression ResolveVariable(string name)
		{
			if (!HasVariable(name)) {
				if (!IsRoot) {
					return parent.ResolveVariable(name);
				}
				AddVariable(name, Expression.Parameter(typeof(object), name));
			}
			return variables[name.ToLower()];
		}

		#region builtin

		private readonly IDictionary<Syntax.BuiltinVariable, PropertyInfo> builtinVars = new Dictionary<Syntax.BuiltinVariable, PropertyInfo>();

		private readonly IDictionary<Syntax.BuiltinVariable, ParameterExpression> overridenBuiltinVars = new Dictionary<Syntax.BuiltinVariable, ParameterExpression>();

		private void LoadBuiltinVariables()
		{
			var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly;
			var properties = typeof(IronAHK.Rusty.Core).GetProperties(flags);

			foreach (var prop in properties) {
				if (Syntax.IsBuiltinVariable(prop.Name)) {
					builtinVars[Syntax.GetBuiltinVariable(prop.Name)] = prop;
				}
			}
		}

		public virtual Expression ResolveBuiltinVariable(Syntax.BuiltinVariable variable)
		{
			if (overridenBuiltinVars.ContainsKey(variable)) {
				return overridenBuiltinVars[variable];

			} else if (!builtinVars.ContainsKey(variable)) {
				if (IsRoot) {
					throw new InvalidOperationException();
				}
				return parent.ResolveBuiltinVariable(variable);
			}
			return Expression.Property(null, builtinVars[variable]);
		}

		public virtual void OverrideBuiltinVariable(Syntax.BuiltinVariable variable, ParameterExpression value)
		{
			overridenBuiltinVars[variable] = value;
		}

		#endregion

		#endregion

		#region targets

		private readonly LabelTarget returnTarget = null;

		private readonly LabelTarget continueTarget = null;

		private readonly LabelTarget breakTarget = null;

		public LabelTarget Return { get { return returnTarget; } }

		public LabelTarget Continue { get { return continueTarget; } }

		public LabelTarget Break { get { return breakTarget; } }

		#endregion
	}

	public class FunctionScope : Scope
	{
		public FunctionScope(Scope parent)
		: base(parent, CreateReturnLabel(), null, null)
		{
		}

		private static LabelTarget CreateReturnLabel()
		{
			return Expression.Label(typeof(object));
		}

		public override ParameterExpression ResolveVariable(string name) // override, because we can't just use parent scope vars
		{
			if (!HasVariable(name)) {
				// todo: except if declared as global / caller, or super-global in ancestor scope
				AddVariable(name, Expression.Parameter(typeof(object), name));
			}
			return variables[name.ToLower()];
		}
	}

	public class LoopScope : Scope
	{
		public LoopScope(Scope parent)
		: base(parent, null, CreateJumpLabel(), CreateJumpLabel())
		{
			OverrideBuiltinVariable(Syntax.BuiltinVariable.A_Index,
			                        Expression.Parameter(typeof(int), "A_Index"));
		}

		private static LabelTarget CreateJumpLabel()
		{
			return Expression.Label(); // void
		}
	}

	public class LoopFilesScope : LoopScope
	{
		public LoopFilesScope(Scope parent)
		: base(parent)
		{
			OverrideBuiltinVariable(Syntax.BuiltinVariable.A_LoopFileName,
			                        Expression.Parameter(typeof(string), "A_LoopFileName"));
			// todo: etc.
		}
	}

	public class FunctionNotFoundException : System.Exception
	{
		public FunctionNotFoundException(string func) : base("Function '" + func + "' was not found!") { }
	}

	public class UndefinedFunctionException : System.Exception
	{
		public UndefinedFunctionException(string func) : base("Function '" + func + "' is registered, but not yet defined!") { }
	}
}
