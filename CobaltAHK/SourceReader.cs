using System;
using System.IO;

namespace CobaltAHK
{
	public class SourceReader
	{
		public SourceReader(TextReader reader)
		{
			buffer = reader.ReadToEnd().ToCharArray();
		}

		public char Peek()
		{
			return index >= buffer.Length ? Lexer.charEOF : buffer[index];
		}

		public char Read()
		{
			char ch = Peek();

			// keep the current position
			if (ch != Lexer.charEOF) {
				index++;
				col++;
			}
			if (ch == '\n') {
				line++;
				col = 0;
			}

			return ch;
		}

		public void Rewind(SourcePosition pos)
		{
			if (pos.Index > buffer.Length) { // can't go there!
				throw new Exception(); // todo
			}
			index = pos.Index;
			line = pos.Line;
			col = pos.Column;
		}

		private char[] buffer;

		#region position
		private uint line = 1;
		private uint col = 0;
		private uint index = 0;

		public SourcePosition Position { get { return new SourcePosition(line, col, index); } }
		#endregion
	}
}