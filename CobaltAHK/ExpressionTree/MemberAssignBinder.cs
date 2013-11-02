using System;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CobaltAHK.ExpressionTree
{
	public class MemberAssignBinder : SetIndexBinder
	{
		private static readonly CallInfo MemberCallInfo = new CallInfo(2);

		public MemberAssignBinder() : base(MemberCallInfo) { }

		public override DynamicMetaObject FallbackSetIndex(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
		{
			if (args.Length != 1) {
				throw new InvalidOperationException();
			}

			if (!target.HasValue || args.Any(a => !a.HasValue) || !value.HasValue) {
				return Defer(target, args.Concat(new[] { value }).ToArray());
			}

			// todo: special properties like base, builtin obj functions etc.
			// todo: .NET types

			return errorSuggestion ?? new DynamicMetaObject(
				ThrowOnFailure(target.Expression, args[0].Expression, value.Expression),
				BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType));
		}

		private static Expression ThrowOnFailure(Expression target, Expression key, Expression value)
		{
			return Expression.Throw(Expression.Constant(""), typeof(InvalidOperationException)); // todo: supply message
		}
	}
}

