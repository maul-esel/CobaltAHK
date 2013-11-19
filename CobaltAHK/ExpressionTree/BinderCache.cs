using System.Collections.Generic;
using System.Dynamic;

namespace CobaltAHK.ExpressionTree
{
	internal static class BinderCache
	{
		private static IDictionary<string, GetMemberBinder> getMemberBinders = new Dictionary<string, GetMemberBinder>();

		internal static GetMemberBinder GetGetMemberBinder(string name)
		{
			name = name.ToLower();
			if (!getMemberBinders.ContainsKey(name)) {
				getMemberBinders[name] = new MemberAccessBinder(name);
			}
			return getMemberBinders[name];
		}

		private static IDictionary<string, SetMemberBinder> setMemberBinders = new Dictionary<string, SetMemberBinder>();

		internal static SetMemberBinder GetSetMemberBinder(string name)
		{
			name = name.ToLower();
			if (!setMemberBinders.ContainsKey(name)) {
				setMemberBinders[name] = new MemberAssignBinder(name);
			}
			return setMemberBinders[name];
		}
	}
}

