using System;

namespace CobaltAHK
{
	public class LexerException : Exception
	{
		public LexerException(SourcePosition pos, string msg = null)
		{
			position = pos;
			message = msg;
		}

		private string message;

		private SourcePosition position;

		public override string Message {
			get {
				return string.Format("Error: {0} at position {1} (line {2}, column {3}) - message: {4}", GetType(), position.Index, position.Line, position.Column, message);
			}
		}
	}

	public class UnexpectedEOFException : LexerException
	{
		public UnexpectedEOFException(SourcePosition pos, string msg = null)
		: base(pos, msg) { }
	}

	public class UnterminatedStringException : LexerException
	{
		public UnterminatedStringException(SourcePosition pos, string msg = null)
		: base(pos, msg) { }
	}

	public class UnterminatedObjectException : LexerException
	{
		public UnterminatedObjectException(SourcePosition pos, string msg = null)
		: base(pos, msg) { }
	}

	public class UnterminatedArrayException : LexerException
	{
		public UnterminatedArrayException(SourcePosition pos, string msg = null)
		: base(pos, msg) { }
	}

	public class InvalidNumberException : LexerException
	{
		public InvalidNumberException(SourcePosition pos, string msg = null)
		: base(pos, msg) { }
	}

	public class UnexpectedCodeException : LexerException
	{
		public UnexpectedCodeException(SourcePosition pos, string expected, string actual, string msg = null) : base(pos, msg) { }
	}
}

