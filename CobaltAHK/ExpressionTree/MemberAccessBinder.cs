using System;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;

namespace CobaltAHK.ExpressionTree
{
	public class MemberAccessBinder : GetIndexBinder
	{
		private static readonly CallInfo MemberCallInfo = new CallInfo(1);

		public MemberAccessBinder() : base(MemberCallInfo) { }

		public override DynamicMetaObject FallbackGetIndex(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
		{
			if (args.Length > 1) {
				throw new InvalidOperationException();
			}

			if (!target.HasValue || args.Any(a => !a.HasValue)) {
				return Defer(target, args);
			}

			if (target.LimitType.Is<DynamicObject>()) { // todo: IDynamicMetaObjProv. in general
				var dyn = target.Value as DynamicObject;
				object result;
				if (dyn.TryGetIndex(this, args.Select(a => a.Value).ToArray(), out result)) {
					return new DynamicMetaObject(Expression.Constant(result, typeof(object)),
								     BindingRestrictions.GetInstanceRestriction(target.Expression, dyn));
				}
			}
			// todo: other types, like static .NET
			return errorSuggestion;
		}
	}
}

