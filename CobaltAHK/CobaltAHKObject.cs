using System;
using System.Dynamic;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace CobaltAHK
{
	public class CobaltAHKObject : DynamicObject, IDictionary<object, object>
	{
		internal CobaltAHKObject(IEnumerable<object> keys, IEnumerable<object> values)
		{
			if (keys.Count() != values.Count()) {
				throw new Exception(); // todo
			}
			for (var i = 0; i < keys.Count(); i++) {
				(this as IDictionary<object, object>).Add(keys.ElementAt(i), values.ElementAt(i));
			}
		}

		private readonly IDictionary<object, object> dict = new Dictionary<object, object>();

		public override bool TryGetIndex(GetIndexBinder binder, object[] args, out object result)
		{
			if (args.Length != 1) {
				throw new InvalidOperationException();
			}
			return dict.TryGetValue(args[0], out result);
		}

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			return dict.TryGetValue(binder.Name, out result);
		}

		public override bool TrySetIndex(SetIndexBinder binder, object[] args, object value)
		{
			if (args.Length != 1) {
				throw new InvalidOperationException();
			}
			dict[args[0]] = value;
			return true;
		}

		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			dict[binder.Name] = value;
			return true;
		}

		#region IDictionary<object, object>

		object IDictionary<object, object>.this[object key] {
			get {
				return dict[key];
			}
			set {
				dict[key] = value;
			}
		}

		ICollection<object> IDictionary<object, object>.Keys {
			get {
				return dict.Keys;
			}
		}

		ICollection<object> IDictionary<object, object>.Values {
			get {
				return dict.Values;
			}
		}

		void IDictionary<object, object>.Add(object key, object value)
		{
			dict.Add(key, value);
		}

		bool IDictionary<object, object>.Remove(object key)
		{
			return dict.Remove(key);
		}

		bool IDictionary<object, object>.ContainsKey(object key)
		{
			return dict.ContainsKey(key);
		}

		bool IDictionary<object, object>.TryGetValue(object key, out object value)
		{
			return dict.TryGetValue(key, out value);
		}

		#region ICollection<KeyValuePair<object, object>>

		int ICollection<KeyValuePair<object, object>>.Count {
			get {
				return dict.Count;
			}
		}

		bool ICollection<KeyValuePair<object, object>>.IsReadOnly {
			get {
				return false;
			}
		}

		void ICollection<KeyValuePair<object, object>>.Add(KeyValuePair<object, object> kv)
		{
			dict.Add(kv);
		}

		void ICollection<KeyValuePair<object, object>>.Clear()
		{
			dict.Clear();
		}

		bool ICollection<KeyValuePair<object, object>>.Contains(KeyValuePair<object, object> kv)
		{
			return dict.Contains(kv);
		}

		void ICollection<KeyValuePair<object, object>>.CopyTo(KeyValuePair<object, object>[] arr, int arrIndex)
		{
			dict.CopyTo(arr, arrIndex);
		}

		bool ICollection<KeyValuePair<object, object>>.Remove(KeyValuePair<object, object> kv)
		{
			return dict.Remove(kv);
		}

		IEnumerator<KeyValuePair<object, object>> IEnumerable<KeyValuePair<object, object>>.GetEnumerator()
		{
			return dict.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return dict.GetEnumerator();
		}

		#endregion

		#endregion
	}
}