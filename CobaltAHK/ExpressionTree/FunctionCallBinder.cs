using System;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;

namespace CobaltAHK.ExpressionTree
{
	public class FunctionCallBinder : InvokeBinder
	{
		public FunctionCallBinder(CallInfo info, Scope scp)
		: base(info)
		{
			scope = scp;
		}

		private readonly Scope scope;

		public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
		{
			if (!target.HasValue || args.Any(a => !a.HasValue)) {
				return Defer(target, args);
			}

			var func = (string)target.Value;
			if (!scope.IsFunctionDefined(func)) { // todo: defer when !HasValue ?
				return Defer(target, args);
			}

			var lambda = scope.ResolveFunction(func);
			var prms = args.Select(arg => Converter.ConvertToObject(arg.Expression));

			return new DynamicMetaObject(
				Expression.Invoke(lambda, prms),
			        BindingRestrictions.GetInstanceRestriction(target.Expression, func) // todo
			);
		}
	}
}