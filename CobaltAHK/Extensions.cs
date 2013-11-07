using System.Collections.Generic;

namespace CobaltAHK
{
	internal static class Extensions
	{
#if DEBUG
		private static readonly System.Collections.Generic.IDictionary<string, string> map = new System.Collections.Generic.Dictionary<string, string>() {
			{ "\n", "\\n" },
			{ "\r", "\\r" },
			{ "\t", "\\t" }
		};

		public static string Escape(this string str)
		{
			foreach (var escape in map) {
				str = str.Replace(escape.Key, escape.Value);
			}
			return str;
		}

		public static string TypeName(this object o)
		{
			return o.GetType().Name;
		}
#endif
		public static void Remove<T>(this IList<T> e, IEnumerable<T> list)
		{
			foreach (var item in list) {
				e.Remove(item);
			}
		}
	}
}