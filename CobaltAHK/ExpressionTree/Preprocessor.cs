using System;
using System.Collections.Generic;
using CobaltAHK.Expressions;

namespace CobaltAHK.ExpressionTree
{
	public static class Preprocessor
	{
		public static void Process(IEnumerable<Expression> exprs, Scope scope, ScriptSettings settings)
		{
			foreach (var expr in exprs) {
				Process(expr, scope, settings);
			}
		}

		public static void Process(Expression expr, Scope scope, ScriptSettings settings)
		{
			if (expr is DirectiveExpression) {
				// todo

			} else if (expr is FunctionDefinitionExpression) {
				var func = (FunctionDefinitionExpression)expr;
				scope.AddFunctionName(func.Name);

				var funcScope = new FunctionScope(scope);
				scope.AddScope(expr, funcScope);
				Process(func.Body, funcScope, settings);
			}
		}
	}
}

