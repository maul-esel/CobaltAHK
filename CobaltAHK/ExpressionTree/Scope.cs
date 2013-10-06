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
			var methods = typeof(IronAHK.Rusty.Core).GetMethods(flags);

			foreach (var method in methods) {
				var paramList = method.GetParameters();
				if (HasFunction(method.Name) || paramList.Any(p => p.ParameterType.IsByRef)) {
					continue; // skips byRef and overloads // todo: support overloads!
				}

				var prms  = paramList.Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
				var types = paramList.Select(p => p.ParameterType).ToList();
				types.Add(method.ReturnType);

				var lambda = Expression.Lambda(Expression.GetFuncType(types.ToArray()),
				                               Expression.Call(method, prms),
				                               prms);
				AddFunction(method.Name, lambda);
			}
		}

		protected readonly Scope parent;

		public bool IsRoot { get { return parent == null; } }

		protected readonly IDictionary<string, LambdaExpression> functions = new Dictionary<string, LambdaExpression>();

		public virtual void AddFunction(string name, LambdaExpression func) {
			functions[name.ToLower()] = func;
		}

		protected virtual bool HasFunction(string name)
		{
			return functions.ContainsKey(name.ToLower());
		}

		public virtual LambdaExpression ResolveFunction(string name)
		{
			if (HasFunction(name)) {
				return functions[name.ToLower()];

			} else if (parent != null) {
				return parent.ResolveFunction(name);
			}
			throw new FunctionNotFoundException(name);
		}

		protected readonly IDictionary<string, ParameterExpression> variables = new Dictionary<string, ParameterExpression>();

		public virtual void AddVariable(string name, ParameterExpression variable)
		{
			variables[name.ToLower()] = variable;
		}

		public virtual ParameterExpression ResolveVariable(string name)
		{
			if (!variables.ContainsKey(name.ToLower())) {
				AddVariable(name, Expression.Parameter(typeof(object), name));
			}
			return variables[name.ToLower()];
		}
	}

	public class FunctionNotFoundException : System.Exception
	{
		public FunctionNotFoundException(string func) : base("Function '" + func + "' was not found!") { }
	}
}
