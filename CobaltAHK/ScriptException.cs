using System;

namespace CobaltAHK
{
	public class ScriptException : Exception
	{
		public ScriptException(object val)
		: base(val != null ? val.ToString() : "")
		{
			value = val;
		}

		private readonly object value;

		public object Value { get { return value; } }
	}
}