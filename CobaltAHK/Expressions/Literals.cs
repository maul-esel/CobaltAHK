using System.Collections.Generic;

namespace CobaltAHK.Expressions
{
	public abstract class ValueLiteralExpression : ValueExpression
	{
		protected ValueLiteralExpression(SourcePosition pos) : base(pos) { }
	}

	public class StringLiteralExpression : ValueLiteralExpression
	{
		public StringLiteralExpression(SourcePosition pos, string val)
		: base(pos)
		{
			str = val;
		}

		private readonly string str;

		public string String { get { return str; } }
	}

	public class NumberLiteralExpression : ValueLiteralExpression
	{
		public NumberLiteralExpression(SourcePosition pos, string val, Syntax.NumberType type) : base(pos) { }
	}

	public class ObjectLiteralExpression : ValueLiteralExpression
	{
		public ObjectLiteralExpression(SourcePosition pos, IDictionary<ValueExpression, ValueExpression> obj) : base(pos) { }
	}

	public class ArrayLiteralExpression : ValueLiteralExpression
	{
		public ArrayLiteralExpression(SourcePosition pos, IEnumerable<ValueExpression> arr) : base(pos) { }
	}
}
