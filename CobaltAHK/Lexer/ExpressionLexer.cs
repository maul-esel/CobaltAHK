using System;
using System.Text;
using System.Linq;

namespace CobaltAHK
{
	public class ExpressionLexer : LexerBase
	{
		public ExpressionLexer(SourceReader source)
		: base(source) { }

		public override Token GetToken()
		{
			bool whitespace = IsWhitespace(reader.Peek());
			SkipWhitespace();

			char ch = reader.Peek();
			switch (ch) {
				/*case ',':
					reader.Read();
					return Token.Comma;*/
				case '"':
					return ReadQuotedString();

				case '(':
					reader.Read();
					return Token.OpenParenthesis;
				case ')':
					reader.Read();
					return Token.CloseParenthesis;
				case '{':
					reader.Read();
					return Token.OpenBrace;
				case '}':
					reader.Read();
					return Token.CloseBrace;
				case '[':
					reader.Read();
					if (whitespace) { // todo: or comma, or operator, or OpenParenthesis (NOT closeParens)
						return Token.OpenBracket;
					} else {
						return OperatorToken.GetToken(Operator.AltObjAccess);
					}
				case ']':
					reader.Read();
					return Token.CloseBracket;
				
				case '.':
					reader.Read();
					if (whitespace && IsWhitespace(reader.Peek())) {
						return OperatorToken.GetToken(Operator.Concatenate);
					} else if (!whitespace && !IsWhitespace(reader.Peek())) {
						return OperatorToken.GetToken(Operator.ObjectAccess);
					} else {
						throw new Exception(); // todo
					}
				case '+':
				case '-':
					char first = reader.Read(), next = reader.Peek();
					if (next == '=') {
						reader.Read();
						return OperatorToken.GetToken(first == '+' ? Operator.AddAssign : Operator.SubtractAssign);
					} else if ((whitespace && IsWhitespace(next)) || (!whitespace && !IsWhitespace(next))) {
						return OperatorToken.GetToken(first == '+' ? Operator.Add : Operator.Subtract);
					} else if (next == first) {
						reader.Read();
						return OperatorToken.GetToken(first == '+' ? Operator.Increment : Operator.Decrement);
					} else if (first == '-') {
						return OperatorToken.GetToken(Operator.UnaryMinus);
					} else {
						throw new Exception(); // todo
					}
				case '*':
				case '/':
					reader.Read();
					if (reader.Peek() == '=') {
						reader.Read();
						return OperatorToken.GetToken(ch == '*' ? Operator.MultiplyAssign : Operator.TrueDivideAssign);
					} else {
						return OperatorToken.GetToken(ch == '*' ? Operator.Multiply : Operator.TrueDivide);
					}
					// todo: FloorDivide && FloorDivideAssign
				case ':':
					reader.Read();
					if (reader.Peek() == '=') {
						reader.Read();
						return OperatorToken.GetToken(Operator.Assign);
					} else {
						return Token.Colon;
					}
				case '=':
					reader.Read();
					if (reader.Peek() == '=') {
						reader.Read();
						return OperatorToken.GetToken(Operator.CaseEqual);
					} else {
						return OperatorToken.GetToken(Operator.Equal);
					}
					// todo: operators & | ^ && || ...
				default:
					if (IsNumeric(ch)) {
						return ReadNumber();
					}
					return ReadIdOrOperator();
			}
		}

		private Token ReadIdOrOperator()
		{
			char ch = reader.Peek();
			if (IsNumeric(ch)) {
				throw new LexerException(reader.Position); // todo: type
			}

			string name = "";
			while (IsIdChar(ch)) {
				name += reader.Read();
				ch = reader.Peek();
			}

			if (reader.Peek() == '(') {
				return new FunctionToken(name);
			}

			if (Operator.IsOperator(name)) {
				return OperatorToken.GetToken(Operator.GetOperator(name));
			} else { // variables
				return new IdToken(name);
			}
		}

		#region literals

		private static readonly char[] definiteStringTerminators = new char[0];
		private static readonly char[] escapableStringTerminators = new[] { '"' };

		private QuotedStringToken ReadQuotedString()
		{
			ExpectString("\"");

			bool escape = false;
			var str = ReadUntilTerminators(definiteStringTerminators, true, ch => {
				if (escapableStringTerminators.Contains(ch) && !escape) {
					reader.Read(); // consume escaped terminator
					throw new LexerBase.TerminateReadingException();
				}
				escape = ch == '`';
			});
			return new QuotedStringToken(str);
		}

		private static readonly char[] numberTerminators = new[] { ' ', '\t', '\n', ',', ')', ']', Lexer.charEOF };

		private NumberToken ReadNumber()
		{
			/*
			 * The following number notations are supported:
			 * ints: 130
			 * floats: 13.2888
			 * hex ints: 0x456
			 * scientific: 1.0e4 or -2.1E-4
			 */
			// todo: support binary and octal, as #AHKv2 does

			char first = reader.Peek();
			if (!IsNumeric(first)) {
				throw new InvalidNumberException(reader.Position);
			}

			int i = 1, dec = -1, exp = -1, minus = -1;
			bool hex = false;
			var number = ReadUntilTerminators(numberTerminators, false, ch => {
				if (!IsNumeric(ch)) {
					if (ch == 'x' && i == 2 && first == '0') { // a hexadecimal integer (i == 2 implies !dec)
						hex = true;
					} else if (ch == '.' && i > 1 && !hex) { // a decimal (or scientific)
						dec = i;
					} else if (ch == 'e' && dec > -1 && dec < (i - 1)) { // a scientific number
						exp = i;
					} else if (ch == '-' && exp == (i - 1)) { // a negative exponent in a scientific number
						minus = i;
					} else {
						throw new InvalidNumberException(reader.Position, ch.ToString());
					}
				}
			});

			// catch unterminated hex ("0x") or dec (e.g. "12.") or scientific (e.g. "12.3e" or "12.3e-")
			if ((hex && number.Length < 3) || (number.Length == dec) || (number.Length == exp) || (number.Length == minus)) {
				throw new InvalidNumberException(reader.Position);
			}

			return new NumberToken(number,
			                       hex ? Syntax.NumberType.Hexadecimal
			                       : exp > -1 ? Syntax.NumberType.Scientific
			                       : dec > -1 ? Syntax.NumberType.Decimal
			                       : Syntax.NumberType.Integer);
		}

		#endregion
	}
}

