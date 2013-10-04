using System;

namespace CobaltAHK
{
	public struct SourcePosition
	{
		public SourcePosition(uint line, uint col, uint index)
		{
			this.line = line;
			this.col = col;
			this.index = index;
		}

		private uint line, col, index;

		public uint Line { get { return line; } }
		public uint Column { get { return col; } }
		public uint Index { get { return index; } }
	}
}

