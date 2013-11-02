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
			if (args.Length != 1) {
				throw new InvalidOperationException();
			}

			if (!target.HasValue || args.Any(a => !a.HasValue)) {
				return Defer(target, args);
			}

			// todo: special properties like base
			// todo: .NET types

			return errorSuggestion ?? new DynamicMetaObject(
				Expression.Block(Expression.Throw(Expression.Constant("")), Generator.NULL),
			        BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType)
			);
		}
	}
}

