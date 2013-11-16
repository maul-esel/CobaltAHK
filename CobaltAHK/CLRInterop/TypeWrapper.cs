using System;
using System.Linq;
using System.Dynamic;
using System.Reflection;

namespace CobaltAHK.CLRInterop
{
	internal class TypeWrapper : CLRWrapper
	{
		internal TypeWrapper(Type tp)
		: base(tp.FullName)
		{
			type = tp;
		}

		private readonly Type type;

		private static BindingFlags TypeFlags = BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy;

		#region DynamicObject overrides

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			return TryAccessMember(binder.Name, out result);
		}

		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			return TryAssignMember(binder.Name, value);
		}

		public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
		{
			var method = type.GetMethod(binder.Name, TypeFlags);
			if (method != null) {
				throw new NotImplementedException("parameter type matching");
			}

			// todo: real extension methods (and AHK-faked ones?)

			result = null;
			return false;
		}

		public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
		{
			if (indexes.Length == 1 && indexes[0] is String) {
				if (TryAccessMember((string)indexes[0], out result)) {
					return true;
				}
			}

			var index = type.GetProperties(TypeFlags).FirstOrDefault(prop => prop.GetIndexParameters().Length > 0); // todo: try all, not just first
			if (index != null && index.CanRead) {
				throw new NotImplementedException("parameter type matching");
			}

			result = null;
			return false;
		}

		public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
		{
			if (indexes.Length == 1 && indexes[0] is String) {
				if (TryAssignMember((string)indexes[0], value)) {
					return true;
				}
			}

			var index = type.GetProperties(TypeFlags).FirstOrDefault(prop => prop.GetIndexParameters().Length > 0);
			if (index != null && index.CanWrite) {
				throw new NotImplementedException("parameter type matching");
			}

			return false;
		}

		#endregion

		private bool TryAccessMember(string name, out object result)
		{
			var nested = type.GetNestedType(name);
			if (nested != null) {
				result = new TypeWrapper(nested);
				return true;
			}

			var prop = type.GetProperty(name, TypeFlags);
			if (prop != null && prop.CanRead) {
				result = type.InvokeMember(prop.Name, TypeFlags|BindingFlags.GetProperty, null, null, null);
				return true;
			}

			var field = type.GetField(name, TypeFlags);
			if (field != null) {
				result = type.InvokeMember(field.Name, TypeFlags|BindingFlags.GetField, null, null, null);
				return true;
			}

			// reflection-objects for methods??

			// todo: try index property

			result = null;
			return false;

		}

		private bool TryAssignMember(string name, object value)
		{
			var prop = type.GetProperty(name, TypeFlags);
			if (prop != null && prop.CanWrite) {
				type.InvokeMember(prop.Name, TypeFlags|BindingFlags.SetProperty, null, null, new[] { value });
				return true;
			}

			var field = type.GetField(name, TypeFlags);
			if (field != null && !field.IsInitOnly) {
				type.InvokeMember(field.Name, TypeFlags|BindingFlags.SetField, null, null, new[] { value });
				return true;
			}

			// todo: try index property

			return false;
		}
	}
}