using System;
using System.Dynamic;

namespace CobaltAHK.CLRInterop
{
	internal class NamespaceWrapper : CLRWrapper
	{
		internal NamespaceWrapper(string name)
		: base(name) { }

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			string name = FullName(binder.Name);

			Type type;
			TryFindType(name, out type);
			string ns = NamespaceExists(name) ? name : null;

			if ((ns == null) == (type == null)) {
				result = null;
				return false;

			} else if (ns != null) {
				result = new NamespaceWrapper(ns);

			} else { // (type != null)
				result = new TypeWrapper(type);
			}
			return true;
		}
	}
}

