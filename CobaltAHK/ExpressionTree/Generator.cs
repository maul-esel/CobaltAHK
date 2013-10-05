using System;
using System.Collections.Generic;
using DLR = System.Linq.Expressions;
using CobaltAHK.Expressions;

namespace CobaltAHK.ExpressionTree
{
	public static class Generator
	{
		public static DLR.Expression Generate(Expression expr, Scope scope, ScriptSettings settings)
		{
			if (expr is DirectiveExpression) {
				// todo
			} else if (expr is CommandCallExpression) {
				// todo
			} else if (expr is FunctionDefinitionExpression) {
				return GenerateFunctionDefinition((FunctionDefinitionExpression)expr, scope, settings);
			} else if (expr is ClassDefinitionExpression) {
				// todo
			}
			throw new NotImplementedException();
		}

		private static DLR.Expression GenerateFunctionDefinition(FunctionDefinitionExpression func, Scope scope, ScriptSettings settings)
		{
			var funcScope = new Scope();

			var prms = new List<DLR.ParameterExpression>();
			var types = new List<Type>(prms.Count + 1);

			foreach (var p in func.Parameters) {
				// todo: default values
				var param = DLR.Expression.Parameter(typeof(object), p.Name);
				prms.Add(param);
				funcScope.AddVariable(p.Name, param);

				var type = typeof(object);
				if (p.Modifier.HasFlag(Syntax.ParameterModifier.ByRef)) {
					type = type.MakeByRefType();
				}
				types.Add(type);
			}
			types.Add(typeof(object)); // return value

			var funcBody = new List<DLR.Expression>();
			foreach (var e in func.Body) {
				funcBody.Add(Generate(e, funcScope, settings));
			}

			var funcType = DLR.Expression.GetFuncType(types.ToArray());
			var function = DLR.Expression.Lambda(funcType, DLR.Expression.Block(funcBody), func.Name, prms);
			scope.AddFunction(func.Name, function);

			return function;
		}
	}
}