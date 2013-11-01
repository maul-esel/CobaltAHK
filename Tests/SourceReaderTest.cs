using System.IO;
using System.Linq;
using NUnit.Framework;

namespace CobaltAHK.Tests
{
	[TestFixture]
	public class SourceReaderTest
	{
		private static readonly string code = "any text here,\t doesn't matter.\n Maybe put in 'some \n newlines\n #and stuff\r...";

		[SetUp]
		public void Init()
		{
			reader = new SourceReader(new StringReader(code));
		}

		private SourceReader reader;

		[Test]
		public void TestRead()
		{
			for (int i = 0; i < code.Length; i++) {
				Assert.AreEqual(reader.Read(), code[i], "Unexpected char at position " + i);
			}
			Assert.AreEqual(reader.Read(), Lexer.charEOF, "SourceReader has not terminated");
		}

		[Test]
		public void TestPeekRead()
		{
			for (int i = 0; i < code.Length; i++) {
				Assert.AreEqual(reader.Peek(), code[i], "Peek() returned unexpected char at position " + i);
				Assert.AreEqual(reader.Peek(), code[i], "Peek() affected output");
				Assert.AreEqual(reader.Peek(), reader.Read(), "Read() differed from Peek() output");
			}
		}

		[Test]
		public void TestPosition()
		{
			for (int i = 0; i < code.Length; i++) {
				string substr = code.Substring(0, i);
				int currentLine = substr.Count(c => c == '\n') + 1;
				int currentCol = substr.Contains('\n') ? (i - substr.LastIndexOf('\n') - 1) : i;

				AssertPosition(i, "before", i, currentLine, currentCol);

				bool newline = reader.Read() == '\n';

				AssertPosition(i, "after", i + 1, currentLine + (newline ? 1 : 0), newline ? 0 : currentCol + 1);
			}
		}

		private void AssertPosition(int i, string hint, int index, int line, int col)
		{
			Assert.AreEqual(reader.Position.Index,  index, "(" + hint + ") Position.Index was incorrect at i=" + i);
			Assert.AreEqual(reader.Position.Line,   line,  "(" + hint + ") Position.Line was incorrect at i=" + i);
			Assert.AreEqual(reader.Position.Column, col,   "(" + hint + ") Position.Column was incorrect at i=" + i);
		}
	}
}

