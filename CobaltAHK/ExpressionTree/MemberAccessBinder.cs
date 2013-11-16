using System;
using System.Dynamic;
using System.Linq;
#if CustomDLR
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif

namespace CobaltAHK.ExpressionTree
{
	public class MemberAccessBinder : GetMemberBinder
	{
		public MemberAccessBinder(string member) : base(member, true) { }

		public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
		{
			if (!target.HasValue) {
				return Defer(target);
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