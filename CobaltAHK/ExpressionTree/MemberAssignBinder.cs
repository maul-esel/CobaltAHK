using System;
using System.Dynamic;
using System.Linq;
#if CustomDLR
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using System.Reflection;

namespace CobaltAHK.ExpressionTree
{
	public class MemberAssignBinder : SetMemberBinder
	{
		public MemberAssignBinder(string member) : base(member, true) { }

		public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
		{
			if (!target.HasValue || !value.HasValue) {
				return Defer(target, value);
			}

			// todo: special properties like base, builtin obj functions etc.
			// todo: .NET types

			return errorSuggestion ?? new DynamicMetaObject(
				ThrowOnFailure(target.Expression, value.Expression),
				BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType));
		}

		private Expression ThrowOnFailure(Expression target, Expression value)
		{
			return Expression.Throw(Expression.New(typeof(InvalidOperationException))); // todo: supply message
		}
	}
}