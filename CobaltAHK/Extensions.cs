namespace CobaltAHK
{
	internal static class Extensions
	{
		public static bool IsEOF(this char ch)
		{
			return ch == Lexer.charEOF;
		}

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

		public static string Times(this int i, string str)
		{
			string result = "";
			for (var index = 0; index < i; index++)
			{
				result += str;
			}
			return result;
		}

		public static string TypeName(this object o)
		{
			return o.GetType().Name;
		}
#endif
	}
}

