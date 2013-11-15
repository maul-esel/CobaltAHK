namespace CobaltAHK
{
	public class ArrayTokenStream : ITokenStream
	{
		public ArrayTokenStream(SourcePosition pos, Token[] arr)
		{
			tokens = arr;
			position = pos;
		}

		private int index;

		private readonly Token[] tokens;

		private readonly SourcePosition position;

		public Token PeekToken()
		{
			return index < tokens.Length ? tokens[index] : Token.EOF;
		}

		public Token GetToken()
		{
			var token = PeekToken();
			if (token != Token.EOF) {
				++index;
			}
			return token;
		}

		public SourcePosition Position {
			get {
				for (int i = index; i >= 0; --i) {
					if (tokens[index] is PositionedToken) {
						return ((PositionedToken)tokens[index]).Position;
					}
				}
				return position;
			}
		}
	}
}

