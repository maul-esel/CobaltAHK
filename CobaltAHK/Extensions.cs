using System.Collections.Generic;
using System.Linq;

namespace CobaltAHK
{
	internal static class Extensions
	{
#if DEBUG
		private static readonly IDictionary<string, string> map = new Dictionary<string, string>() {
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

		public static IEnumerable<T> Append<T>(this IEnumerable<T> list, params T[] elements)
		{
			return list.Concat(elements);
		}
	}
}