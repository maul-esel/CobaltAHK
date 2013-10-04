using System;
using System.Linq;

namespace CobaltAHK
{
	public class TraditionalLexer : LexerBase
	{
		public TraditionalLexer(SourceReader source)
		: base(source) { }

		public override Token GetToken()
		{
			char ch = reader.Peek();
			if (ch == '%') {
				reader.Read();
				if (IsWhitespace(reader.Peek())) {
					return Token.ForceExpression;
				} else {
					return ReadVariable();
				}
			}
			return ReadString();
		}

		private static readonly char[] variableTerminators = new[] { '%' };

		private VariableToken ReadVariable()
		{
			// ExpectString("%"); // already consumed by GetToken()
			var variable = ReadUntilTerminators(variableTerminators, true, ch => {
				if (!IsIdChar(ch)) {
					throw new Exception(); // todo
				}
			});
			return new VariableToken(variable);
		}
		
		private static readonly char[] definiteStringTerminators = new[] { '\n', Lexer.charEOF };
		private static readonly char[] escapableStringTerminators = new[] { '%', ',', ';' };

		private TraditionalStringToken ReadString()
		{
			bool escape = false;
			var str = ReadUntilTerminators(definiteStringTerminators, false, ch => {
				if (escapableStringTerminators.Contains(ch) && !escape) {
					throw new TerminateReadingException();
				}
				escape = ch == '`';
			});
			return new TraditionalStringToken(str);
		}
	}
}

