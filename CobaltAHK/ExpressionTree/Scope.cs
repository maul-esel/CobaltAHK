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
		}

		public Scope(Scope parentScope)
		{
			parent = parentScope;
		}

		private void LoadBuiltinFunctions()
		{
			var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly;
			var methods = typeof(IronAHK.Rusty.Core).GetMethods(flags).Where(m => !m.IsSpecialName);

			foreach (var method in methods) {
				// todo: filter by attribute?
				var paramList = method.GetParameters();
				if (HasFunction(method.Name) || paramList.Any(p => p.ParameterType.IsByRef)) {
					continue; // skips byRef and overloads // todo: support both!
				}

				var prms  = paramList.Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
				var types = paramList.Select(p => p.ParameterType).ToList();
				types.Add(method.ReturnType);

				var lambda = Expression.Lambda(Expression.GetFuncType(types.ToArray()),
				                               Expression.Call(method, prms),
				                               prms);
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

		protected readonly IDictionary<string, ParameterExpression> variables = new Dictionary<string, ParameterExpression>();

		public virtual void AddVariable(string name, ParameterExpression variable)
		{
			// todo: add VariableScope param; depending on it add it on parent scope or here (and override)
			variables[name.ToLower()] = variable;
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

		public virtual Expression ResolveBuiltinVariable(Syntax.BuiltinVariable variable)
		{
			// todo: Expression.Dynamic
			throw new NotImplementedException();
		}

		public virtual void OverrideBuiltinVariable(Syntax.BuiltinVariable variable, ParameterExpression value) // used for A_Index etc.
		{
			// todo
		}

		#endregion
	}

	public class FunctionScope : Scope
	{
		public FunctionScope(Scope parent) : base(parent) { }

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
		public LoopScope(Scope parent) : base(parent) { }

		public override Expression ResolveBuiltinVariable(Syntax.BuiltinVariable variable)
		{
			if (variable == Syntax.BuiltinVariable.A_Index) {
				// todo
			}
			return base.ResolveBuiltinVariable(variable);
		}
	}

	public class LoopFilesScope : LoopScope
	{
		public LoopFilesScope(Scope parent) : base(parent) { }

		public override Expression ResolveBuiltinVariable(Syntax.BuiltinVariable variable)
		{
			if (variable == Syntax.BuiltinVariable.A_LoopFileName) { // todo: etc.
				// todo
			}
			return base.ResolveBuiltinVariable(variable);
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
