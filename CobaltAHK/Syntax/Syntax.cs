using System;
using System.Linq;

namespace CobaltAHK
{
	public static partial class Syntax
	{
		private static bool IsEnumName<TEnum>(string name) where TEnum : struct
		{
			TEnum dummy;
			return Enum.TryParse<TEnum>(name, true, out dummy);
		}

		private static TEnum GetEnumFromName<TEnum>(string name) where TEnum : struct
		{
			TEnum val;
			if (Enum.TryParse<TEnum>(name, true, out val)) {
				return val;
			}
			throw new ArgumentException();
		}

		public static bool IsDirective(string name)
		{
			return IsEnumName<Directive>(name);
		}

		public static Directive GetDirective(string name)
		{
			return GetEnumFromName<Directive>(name);
		}

		public static bool IsBuiltinVariable(string name)
		{
			return IsEnumName<BuiltinVariable>(name);
		}

		public static BuiltinVariable GetBuiltinVariable(string name)
		{
			return GetEnumFromName<BuiltinVariable>(name);
		}

		public static bool IsParameterModifier(string name)
		{
			return IsEnumName<ParameterModifier>(name);
		}

		public static ParameterModifier GetParameterModifier(string name)
		{
			return GetEnumFromName<ParameterModifier>(name);
		}

		public static bool IsKeyword(string name)
		{
			return IsEnumName<Keyword>(name);
		}

		public static Keyword GetKeyword(string name)
		{
			return GetEnumFromName<Keyword>(name);
		}

		public static bool IsValueKeyword(string name)
		{
			return IsEnumName<ValueKeyword>(name);
		}

		public static ValueKeyword GetValueKeyword(string name)
		{
			return GetEnumFromName<ValueKeyword>(name);
		}

		private static readonly BuiltinVariable[] variablesInInclude = {
			BuiltinVariable.A_ScriptDir,
			BuiltinVariable.A_AppData,
			BuiltinVariable.A_AppDataCommon,
			BuiltinVariable.A_LineFile
		};

		public static bool IsAllowedInInclude(this BuiltinVariable variable)
		{
			return variablesInInclude.Contains(variable);
		}
	}
}

