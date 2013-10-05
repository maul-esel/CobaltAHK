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
			if (expr is FunctionCallExpression) {
				return GenerateFunctionCall((FunctionCallExpression)expr, scope, settings);
			} else if (expr is FunctionDefinitionExpression) {
				return GenerateFunctionDefinition((FunctionDefinitionExpression)expr, scope, settings);
			} else if (expr is StringLiteralExpression) {
				return DLR.Expression.Constant(((StringLiteralExpression)expr).String);
			}
			throw new NotImplementedException();
		}

		private static DLR.Expression GenerateFunctionCall(FunctionCallExpression func, Scope scope, ScriptSettings settings)
		{
			var lambda = scope.ResolveFunction(func.Name);

			var prms = new List<DLR.Expression>();
			foreach (var p in func.Parameters) {
				prms.Add(Generate(p, scope, settings));
			}

			return DLR.Expression.Invoke(lambda, prms);
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
			types.Add(typeof(void)); // return value

			var funcBody = new List<DLR.Expression>();
			foreach (var e in func.Body) {
				funcBody.Add(Generate(e, funcScope, settings));
			}

			var funcType = DLR.Expression.GetFuncType(types.ToArray());
			var function = DLR.Expression.Lambda(funcType, DLR.Expression.Block(funcBody), func.Name, prms); // todo: use Label instead of Block? (see dlr-overview p. 35)
			scope.AddFunction(func.Name, function); // todo: can't call itself, because body is generated before function is complete

			return function;
		}
	}
}