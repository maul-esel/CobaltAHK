using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CobaltAHK
{
	public abstract class LexerBase
	{
		public LexerBase(SourceReader source)
		{
			reader = source;
		}

		protected SourceReader reader;

		public abstract Token GetToken();

		#region type checks

		protected bool IsDigit(char ch)
		{
			return ch >= '0' && ch <= '9'; // cannot use char.IsDigit() here!
		}

		protected bool IsAlpha(char ch)
		{
			return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z'); // todo: ä ö ü etc...
		}

		protected bool IsAlphaNumeric(char ch)
		{
			return IsAlpha(ch) || IsDigit(ch);
		}

		private char[] whitespace = new[] { ' ', '\t' };

		protected bool IsWhitespace(char ch)
		{
			return whitespace.Contains(ch);
		}

		protected bool IsIdChar(char ch)
		{
			return IsAlphaNumeric(ch) || ch == '_';

		}

		#endregion

		#region helper methods

		protected virtual IDictionary<char, Token> punctuationTokens {
			get { return new Dictionary<char, Token>(0); }
		}

		protected Token ReadPunctuationToken()
		{
			char ch = reader.Peek();
			if (punctuationTokens.ContainsKey(ch)) {
				reader.Read();
				return punctuationTokens[ch];
			}
			return null;
		}

		protected void ExpectString(string expected)
		{
			foreach (char ch in expected) {
				char actual = reader.Peek();
				if (actual != ch) {
					throw new UnexpectedCodeException(reader.Position, expected, actual.ToString()); // todo: actual
				}
				reader.Read();
			}
		}

		[Obsolete]
		protected string ReadUntilString(string end)
		{
			return ReadUntilStrings(new[] { end });
		}

		[Obsolete]
		protected string ReadUntilStrings(string[] ends)
		{
			string text = "";
			char ch = reader.Peek();
			while (!ch.IsEOF() || ends.Any(e => e[0].IsEOF())) { // todo: nicer eof handling
				text += ch;

				var valid_ends = ends.Where(str => text.Substring(text.Length - str.Length) == str);
				if (valid_ends.Count() > 0) {
					reader.Read(); // read the last char, which is already part of `text`
					return text.Substring(0, text.Length - valid_ends.ElementAt(0).Length); // return what was read, excluding the end string
				}

				reader.Read();
				ch = reader.Peek();
			}
			throw new UnexpectedEOFException(reader.Position);
		}

		// todo: support escapable terminators?
		protected string ReadUntilTerminators(char[] terminators, bool consumeTerminator = false, Action<char> body = null)
		{
			var builder = new System.Text.StringBuilder();
			bool earlyTerminate = false;
			char ch = reader.Peek();
			while (!terminators.Contains(ch)) {
				if (body != null) {
					try {
						body(ch);
					} catch (TerminateReadingException) {
						earlyTerminate = true;
						break;
					}
				}
				builder.Append(reader.Read());
				ch = reader.Peek();
			}
			if (consumeTerminator && !earlyTerminate) {
				reader.Read();
			}
			return builder.ToString();
		}

		protected class TerminateReadingException : Exception { } // todo: singleton?

		protected void SkipWhitespace()
		{
			char ch = reader.Peek();
			while (!ch.IsEOF() && IsWhitespace(ch)) {
				reader.Read();
				ch = reader.Peek();
			}
		}

		protected void SkipWhitespaceAndNewlines()
		{
			char ch = reader.Peek();
			while (!ch.IsEOF() && (IsWhitespace(ch) || ch == '\n' || ch == '\r')) {
				reader.Read();
				ch = reader.Peek();
			}
		}

		#endregion
	}
}

