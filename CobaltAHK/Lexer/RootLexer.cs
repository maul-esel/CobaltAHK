using System;
using System.Collections.Generic;

namespace CobaltAHK
{
	public class RootLexer : LexerBase
	{
		public RootLexer(SourceReader source)
		: base(source) { }

		private static readonly IDictionary<char, Token> punctuation = new Dictionary<char, Token>() {
			{ '{', Token.OpenBrace },
			{ '}', Token.CloseBrace }
		};

		public override Token GetToken()
		{
			SkipWhitespaceAndNewlines();

			var token = ReadPunctuationToken();
			if (token != null) {
				return token;
			}

			char ch = reader.Peek();

			// Possible syntax:
			// 1. a directive
			//   => DirectiveToken
			// 2. a command
			// 3. a variable (assignment, member-access, ...)
			//   => IdToken
			// 4. a function call
			// 5. a function definition
			//   => FunctionToken
			// 6. a hotkey
			//   => HotkeyToken
			// 7. a hotstring
			//   => HotStringToken
			// 8. an operator, doing concat to the previous line
			//   => throw exception!
			// 9. double-deref??

			switch (ch) {
				// comments are handled by the Lexer class
				// todo: handle non-word operators (comma handled by Lexer)
				case ':':
					reader.Read();
					switch (reader.Peek()) {
						case ':': return ReadHotstringDefinition();
						case '=':
							reader.Read();
							return OperatorToken.GetToken(Operator.Assign); // #OperatorConcat
						default:
							throw new UnexpectedCodeException(reader.Position, ": or =", reader.Peek().ToString());
					}
				case '#': return ReadDirectiveOrHotkey();
				// todo: hotkey chars, such as ^!...
				default: return ReadTextToken();
			}
		}

		private Token ReadTextToken()
		{
			char ch = reader.Peek();
			if (!IsIdChar(ch) || IsDigit(ch)) {
				throw new LexerException(reader.Position, ch.ToString()); // todo: type
			}

			string name = "";
			while (IsIdChar(ch)) {
				name += reader.Read();
				ch = reader.Peek();
			}

			if (Syntax.IsKeyword(name)) {
				return KeywordToken.GetToken(Syntax.GetKeyword(name));

			} else if (reader.Peek() == '(') { // only function calls or definitions are followed by opening parentheses
				return new FunctionToken(name);

			} else if (Operator.IsOperator(name)) {
				return OperatorToken.GetToken(Operator.GetOperator(name));
			}

			// todo: handle hotkeys here as well!
			return new IdToken(name);
		}

		private Token ReadHotstringDefinition()
		{
			ExpectString("::");
			var def = ReadUntilString("::");
			def = def.Remove(def.Length - 2); // remove trailing double-colon syntax
			// todo: validate def, e.g. make sure no newlines
			throw new NotImplementedException(); // todo
		}

		private static readonly char[] directiveTerminators = new[] { ' ', '\t', '\r', '\n', ',', Lexer.charEOF };

		private Token ReadDirectiveOrHotkey()
		{
			ExpectString("#");
			var code = ReadUntilTerminators(directiveTerminators);
			if (Syntax.IsDirective(code)) {
				return DirectiveToken.GetToken(Syntax.GetDirective(code));
			}
			return ReadHotkey('#' + code);
		}

		private HotkeyToken ReadHotkey()
		{
			// todo: read definition and pass it to overload
			throw new NotImplementedException();
		}

		private HotkeyToken ReadHotkey(string definition)
		{
			throw new NotImplementedException();
		}

		private CommentToken ReadMultiComment()
		{
			ExpectString("/*");
			string comment = ReadUntilString("\n*/");

			// after the comment is closed, only whitespace may come
			SkipWhitespace();
			ExpectString("\n");

			return new MultiLineCommentToken(comment);
		}
	}
}

