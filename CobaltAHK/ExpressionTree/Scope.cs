using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CobaltAHK.ExpressionTree
{
	public class Scope
	{
		public Scope() : this(null) { }

		public Scope(Scope parentScope) {
			parent = parentScope;
#if DEBUG
			var param = Expression.Parameter(typeof(string), "str");
			var msgbox = Expression.Lambda<Action<string>>(
				Expression.Call(typeof(IronAHK.Rusty.Core).GetMethod("MsgBox", new[] { typeof(string) }), param),
				param
			);
			AddFunction("MsgBox", msgbox);
#endif
		}

		private readonly Scope parent;

		public bool IsRoot { get { return parent == null; } }

		private readonly IDictionary<string, LambdaExpression> functions = new Dictionary<string, LambdaExpression>();

		public void AddFunction(string name, LambdaExpression func) {
			functions[name.ToLower()] = func;
		}

		public LambdaExpression ResolveFunction(string name)
		{
			if (functions.ContainsKey(name.ToLower())) {
				return functions[name.ToLower()];

			} else if (parent != null) {
				return parent.ResolveFunction(name);
			}
			throw new FunctionNotFoundException(name);
		}

		private readonly IDictionary<string, ParameterExpression> variables = new Dictionary<string, ParameterExpression>();

		public void AddVariable(string name, ParameterExpression variable)
		{
			variables.Add(name.ToLower(), variable);
		}

		public ParameterExpression ResolveVariable(string name)
		{
			if (!variables.ContainsKey(name.ToLower())) {
				AddVariable(name, Expression.Parameter(typeof(object), name));
			}
			return variables[name.ToLower()];
		}
		// todo: RootScope() initialized with builtin functions and commands
	}

	public class FunctionNotFoundException : System.Exception
	{
		public FunctionNotFoundException(string func) : base("Function '" + func + "' was not found!") { }
	}
}
