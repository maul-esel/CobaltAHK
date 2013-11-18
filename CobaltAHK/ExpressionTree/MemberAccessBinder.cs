using System.Dynamic;
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

		private static readonly System.Reflection.MethodInfo baseProperty
			= typeof(CobaltAHKObject).GetProperty("Base", typeof(IDynamicMetaObjectProvider)).GetGetMethod();

		public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
		{
			if (!target.HasValue) {
				return Defer(target);
			}

			// special object property: base
			if (target.LimitType.TypeIs<CobaltAHKObject>() && Name.ToLower() == CobaltAHKObject.BasePropertyName) {
				return new DynamicMetaObject(
					Expression.Property(target.Expression, baseProperty),
					BindingRestrictions.GetTypeRestriction(target.Expression, typeof(CobaltAHKObject))
				);
			}

			// todo: .NET types

			return errorSuggestion ?? new DynamicMetaObject(
				Expression.Block(Expression.Throw(Expression.Constant("")), Generator.NULL),
			        BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType)
			);
		}
	}
}