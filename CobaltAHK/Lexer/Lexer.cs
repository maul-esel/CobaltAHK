using System;
using System.Collections.Generic;
using System.Linq;

namespace CobaltAHK
{
	public partial class Lexer : LexerBase
	{
		public Lexer(System.IO.TextReader code)
		: base(new SourceReader(code))
		{
			rootLexer = new RootLexer(reader);
			exprLexer = new ExpressionLexer(reader);
			tradLexer = new TraditionalLexer(reader);
		}

		public SourcePosition Position { get { return reader.Position; } }

		public void Rewind(SourcePosition pos)
		{
			reader.Rewind(pos);
		}

		public const char charEOF = ((char)(unchecked((char)(-1))));

		#region public token interface

		private Token next = null;

		public Token PeekToken()
		{
			return next ?? (next = FindNextToken());
		}

		public void ResetToken()
		{
			next = null;
		}

		public override Token GetToken()
		{
			var tmp = PeekToken();
			ResetToken();
			return tmp;
		}

		#endregion

		#region state stack handling

		private Stack<State> stack = new Stack<State>(new[] { State.Root });

		public void PushState(State state)
		{
			stack.Push(state);
		}

		public State PopState()
		{
			return stack.Pop();
		}

		private State PeekState()
		{
			return stack.Peek();
		}

		#endregion

		#region sublexers

		private readonly LexerBase exprLexer;

		private readonly LexerBase tradLexer;

		private readonly LexerBase rootLexer;

		#endregion

		private Token FindNextToken()
		{
			State state = PeekState();
			char ch = reader.Peek();
			if (state == State.Root) {
				SkipWhitespace();
			}

			// state-independent tokens
			switch(ch) {
				case charEOF:
					reader.Read();
					return Token.EOF;
				case '\n':
					reader.Read();
					return Token.Newline;
				case ',':
					reader.Read();
					return Token.Comma;
				case ';':
					reader.Read();
					return new SingleCommentToken(ReadUntilTerminators(singleCommentTerminators));
			}

			// state-dependent tokens
			switch(state) {
				case State.Root:        return rootLexer.GetToken();
				case State.Traditional: return tradLexer.GetToken();
				case State.Expression:  return exprLexer.GetToken();
			}
			throw new NotImplementedException();
		}

		private static readonly char[] singleCommentTerminators = new[] { '\n', charEOF };
	}
}

