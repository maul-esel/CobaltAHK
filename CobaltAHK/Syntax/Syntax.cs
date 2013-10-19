using System;
using System.Linq;

namespace CobaltAHK
{
	public static partial class Syntax
	{
		public static bool IsDirective(string name)
		{
			Directive dir;
			return Directive.TryParse(name, true, out dir);
		}

		public static Directive GetDirective(string name)
		{
			Directive dir;
			if (Directive.TryParse(name, true, out dir)) {
				return dir;
			}
			throw new ArgumentException();
		}

		public static bool IsBuiltinVariable(string name)
		{
			BuiltinVariable v;
			return BuiltinVariable.TryParse(name, true, out v);
		}

		public static BuiltinVariable GetBuiltinVariable(string name)
		{
			BuiltinVariable v;
			if (BuiltinVariable.TryParse(name, true, out v)) {
				return v;
			}
			throw new ArgumentException();
		}

		public static bool IsParameterModifier(string name)
		{
			ParameterModifier m;
			return ParameterModifier.TryParse(name, true, out m);
		}

		public static ParameterModifier GetParameterModifier(string name)
		{
			ParameterModifier m;
			if (ParameterModifier.TryParse(name, true, out m)) {
				return m;
			}
			throw new ArgumentException();
		}

		public static bool IsKeyword(string name)
		{
			Keyword k;
			return Keyword.TryParse(name, true, out k);
		}

		public static Keyword GetKeyword(string name)
		{
			Keyword k;
			if (Keyword.TryParse(name, true, out k)) {
				return k;
			}
			throw new ArgumentException();
		}

		private static readonly BuiltinVariable[] variablesInInclude = new[] {
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

