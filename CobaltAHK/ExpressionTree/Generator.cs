using System;
using System.Collections.Generic;
using System.Linq;
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
			} else if (expr is CustomVariableExpression) {
				return scope.ResolveVariable(((CustomVariableExpression)expr).Name);
			} else if (expr is StringLiteralExpression) {
				return DLR.Expression.Constant(((StringLiteralExpression)expr).String);
			} else if (expr is NumberLiteralExpression) {
				return DLR.Expression.Constant(((NumberLiteralExpression)expr).GetValue());
			} else if (expr is ObjectLiteralExpression) {
				return GenerateObjectLiteral((ObjectLiteralExpression)expr, scope, settings);
			} else if (expr is ArrayLiteralExpression) {
				return GenerateArrayLiteral((ArrayLiteralExpression)expr, scope, settings);
			}
			throw new NotImplementedException();
		}

		private static DLR.Expression GenerateObjectLiteral(ObjectLiteralExpression obj, Scope scope, ScriptSettings settings)
		{
			var t = typeof(IEnumerable<object>);
			var constructor = typeof(CobaltAHKObject).GetConstructor(new[] { t, t });

			var keys = DLR.Expression.NewArrayInit(typeof(object), obj.Dictionary.Keys.Select(e => Generate(e, scope, settings)));
			var values = DLR.Expression.NewArrayInit(typeof(object), obj.Dictionary.Values.Select(e => Generate(e, scope, settings)));
			return DLR.Expression.New(constructor, keys, values);
		}

		private static DLR.Expression GenerateArrayLiteral(ArrayLiteralExpression arr, Scope scope, ScriptSettings settings)
		{
			var t = typeof(IEnumerable<object>);
			var constructor = typeof(List<object>).GetConstructor(new[] { t });

			var values = DLR.Expression.NewArrayInit(typeof(object), arr.List.Select(e => DLR.Expression.Convert(Generate(e, scope, settings), typeof(object))));
			return DLR.Expression.New(constructor, values);
		}

		private static DLR.Expression GenerateFunctionCall(FunctionCallExpression func, Scope scope, ScriptSettings settings)
		{
			var lambda = scope.ResolveFunction(func.Name);

			var prms = func.Parameters.Select(p => Generate(p, scope, settings)).ToList();
			if (lambda.Parameters.Count != prms.Count()) {
				throw new Exception(); // todo
			}
			return DLR.Expression.Invoke(lambda, prms);
		}

		private static DLR.Expression GenerateFunctionDefinition(FunctionDefinitionExpression func, Scope scope, ScriptSettings settings)
		{
			var funcScope = new Scope();

			var prms = new List<DLR.ParameterExpression>();
			var types = new List<Type>(prms.Count + 1);
			var funcBody = new List<DLR.Expression>();

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

			var endOfFunc = DLR.Expression.Label(typeof(object));
			foreach (var e in func.Body) {
				DLR.Expression expr;
				if (IsReturn(e)) {
					expr = MakeReturn((FunctionCallExpression)e, scope, settings, funcBody, endOfFunc);
				} else {
					expr = Generate(e, funcScope, settings);
				}
				funcBody.Add(expr);
			}
			funcBody.Add(DLR.Expression.Label(endOfFunc, DLR.Expression.Constant(null))); // default return value is null

			var function = DLR.Expression.Lambda(
				DLR.Expression.GetFuncType(types.ToArray()),
				DLR.Expression.Block(prms, funcBody),
				func.Name,
				prms
			);

			scope.AddFunction(func.Name, function);

			return function;
		}

		private static bool IsReturn(Expression expr)
		{
			return expr is FunctionCallExpression && ((FunctionCallExpression)expr).Name.ToLower() == "return";
		}

		private static DLR.Expression MakeReturn(FunctionCallExpression expr, Scope scope, ScriptSettings settings, IList<DLR.Expression> body, DLR.LabelTarget target)
		{
			var prms = expr.Parameters.ToArray();
			if (prms.Length == 0) {
				return DLR.Expression.Return(target);
			}
			for (var i = 0; i < prms.Length - 1; i++) {
				body.Add(Generate(prms[i], scope, settings));
			}
			var val = Generate(prms[prms.Length - 1], scope, settings);
			return DLR.Expression.Return(target, DLR.Expression.Convert(val, typeof(object)));
		}
	}
}