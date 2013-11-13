using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace CobaltAHK
{
	public class ExpressionLexer : LexerBase
	{
		public ExpressionLexer(SourceReader source)
		: base(source) { }

		private static readonly IDictionary<char, Token> punctuation = new Dictionary<char, Token>() {
			{ '(', Token.OpenParenthesis },
			{ ')', Token.CloseParenthesis },
			{ '{', Token.OpenBrace },
			{ '}', Token.CloseBrace },
			{ ']', Token.CloseBracket },
			{ ',', Token.Comma } // needed here in case of whitespace before comma, because then Lexer doesn't recognize it.
		};

		protected override IDictionary<char, Token> punctuationTokens {
			get {
				return punctuation;
			}
		}

		public override Token GetToken()
		{
			bool whitespace = IsWhitespace(reader.Peek());
			SkipWhitespace();

			var token = ReadPunctuationToken();
			if (token != null) {
				return token;
			}

			char ch = reader.Peek();

			switch (ch) {
				case '"':
					return ReadQuotedString();

				case '[':
					if (whitespace) { // todo: or comma, or operator, or CloseBracket, or OpenParenthesis (NOT closeParens)
						reader.Read();
						return Token.OpenBracket;
					}
					break;
				case ':':
					reader.Read();
					if (reader.Peek() == '=') {
						reader.Read();
						return OperatorToken.GetToken(Operator.Assign); // special handling because colon is not an operator
					}
					return Token.Colon;
				case '?':
					reader.Read();
					if (whitespace && IsWhitespace(reader.Peek())) {
						return OperatorToken.GetToken(Operator.Ternary);
					}
					throw new Exception(); // todo
			}

			if (IsDigit(ch)) {
				return ReadNumber();

			} else if (IsIdChar(ch)) {
				return ReadIdOrOperator();
			}

			return ReadOperator(whitespace);
		}

		#region operator parsing

		private Token ReadOperator(bool wsBefore)
		{
			string str = "";
			char ch = reader.Peek();
			var ops = Operator.Operators;

			while (!IsWhitespace(ch) && ops.Count() > 0) {
				ops = Operator.Operators.Where(op => op.Code.StartsWith(str + ch)
					&& MatchesWhitespace(op, wsBefore, null)
				);

				str += reader.Read();
				ch = reader.Peek();
			}

			bool wsAfter = IsWhitespace(reader.Peek());

			ops = Operator.Operators.Where(op => op.Code == str && MatchesWhitespace(op, wsBefore, wsAfter));
			if (ops.Count() != 1) {
				throw new Exception(); // todo
			}
			return OperatorToken.GetToken(ops.ElementAt(0));
		}

		private bool Implies(Whitespace combi, Whitespace flag, bool conclusion)
		{
			return Implies(combi.HasFlag(flag), conclusion);
		}

		private bool Implies(bool a, bool b)
		{
			return !a || b;
		}

		private bool MatchesWhitespace(Operator op, bool before, bool? after)
		{
			if (op is UnaryOperator) {
				return MatchesWhitespace((UnaryOperator)op, before, after);
			} else if (op is BinaryOperator) {
				return MatchesWhitespace((BinaryOperator)op, before, after);
			}
			throw new Exception(); // todo
		}

		private bool MatchesWhitespace(UnaryOperator op, bool before, bool? after)
		{
			bool result = Implies(op.Position == Position.postfix, !before);
			// note: prefix doesn't imply before, e.g. `(!a)`

			if (after != null) {
				result = result && Implies(op.Position == Position.prefix, !after.Value);
				// note: as above, postfix doesn't imply after, e.g. `(a++)`
			}
			return result;
		}

		private bool MatchesWhitespace(BinaryOperator op, bool before, bool? after)
		{
			var white = op.Whitespace;

			bool result = Implies(white, Whitespace.before,  before)
				&& Implies(white, Whitespace.not_before, !before);

			if (after != null) {
				result = result && Implies(white, Whitespace.after,    after.Value)
					&& Implies(white, Whitespace.not_after,        !after.Value)
					&& Implies(white, Whitespace.both_or_neither,  before == after.Value);
			}

			return result;
		}

		#endregion

		private Token ReadIdOrOperator()
		{
			char ch = reader.Peek();
			if (IsDigit(ch)) {
				throw new LexerException(reader.Position); // todo: type
			}

			string name = "";
			while (IsIdChar(ch)) {
				name += reader.Read();
				ch = reader.Peek();
			}

			if (reader.Peek() == '(') {
				return new FunctionToken(name);
			} else if (Syntax.IsValueKeyword(name)) {
				return ValueKeywordToken.GetToken(Syntax.GetValueKeyword(name));
			} else if (Operator.IsOperator(name)) {
				return OperatorToken.GetToken(Operator.GetOperator(name));
			} else { // variables
				return new IdToken(name);
			}
		}

		#region literals

		private static readonly char[] definiteStringTerminators =  { };
		private static readonly char[] escapableStringTerminators = { '"' };

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
			return new QuotedStringToken(Unescape(str));
		}

		private static readonly char[] numberTerminators = { ' ', '\t', '\n', ',', ')', ']', Lexer.charEOF };

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
			// todo: support scientific without decimal, as #AHKv2 does

			char first = reader.Peek();
			if (!IsDigit(first)) {
				throw new InvalidNumberException(reader.Position);
			}

			int i = 1, dec = -1, exp = -1, minus = -1;
			bool hex = false;
			var number = ReadUntilTerminators(numberTerminators, false, ch => {
				if (!IsDigit(ch)) {
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
				i++;
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