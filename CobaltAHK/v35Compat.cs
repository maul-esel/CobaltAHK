#if CLR_35
namespace CobaltAHK.v35Compat
{
	using System;

	internal class EnumHelper
	{
		public static bool HasFlag(this Enum combination, Enum flag)
		{
			var flagValue = (flag as IConvertible).ToInt32(null);
			var combiValue = (combination as IConvertible).ToInt32(null);
			return (combiValue & flagValue) == flagValue;
		}

		public static bool TryParse<TEnum>(string value, bool ignoreCase, out TEnum result)
		{
			try {
				result = (TEnum)Enum.Parse(typeof(TEnum), value, ignoreCase);
			} catch (Exception e) {
				result = default(TEnum);
				return false;
			}
			return true;
		}
	}
}
#endif