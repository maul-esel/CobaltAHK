namespace CobaltAHK
{
	public interface ITokenStream
	{
		Token PeekToken();

		Token GetToken();

		SourcePosition Position { get; }
	}
}