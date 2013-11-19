using System;
using System.Dynamic;
#if CustomDLR
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif

namespace CobaltAHK.ExpressionTree
{
	public class MemberAssignBinder : SetMemberBinder
	{
		public MemberAssignBinder(string member) : base(member, true) { }

		private static readonly System.Reflection.MethodInfo baseProperty
			= typeof(CobaltAHKObject).GetProperty("Base", typeof(IDynamicMetaObjectProvider)).GetSetMethod();

		public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
		{
			if (!target.HasValue || !value.HasValue) {
				return Defer(target, value);
			}

			// special object property: base
			if (target.LimitType.TypeIs<CobaltAHKObject>() && Name.ToLower() == CobaltAHKObject.BasePropertyName) {
				var restrictions = BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType)
					.Merge(BindingRestrictions.GetTypeRestriction(value.Expression, value.LimitType));

				if (value.LimitType.TypeIs<IDynamicMetaObjectProvider>()) {
					return new DynamicMetaObject(
						Expression.Assign(
							Expression.Property(target.Expression, baseProperty),
							value.Expression
						),
						restrictions
					);
				} else {
					return new DynamicMetaObject(
						Expression.Block(
							Expression.Throw(Expression.Constant(new InvalidOperationException())),
							Expression.Default(typeof(object))
						),
						restrictions
					);
				}
			}

			// todo: builtin object functions
			// todo: .NET types

			return errorSuggestion ?? new DynamicMetaObject(
				ThrowOnFailure(target.Expression, value.Expression),
				BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType));
		}

		private Expression ThrowOnFailure(Expression target, Expression value)
		{
			return Expression.Block(
				Expression.Throw(Expression.New(typeof(InvalidOperationException))), // todo: supply message
			        Generator.NULL
			);
		}
	}
}