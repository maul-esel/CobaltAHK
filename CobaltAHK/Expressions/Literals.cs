using System.Collections.Generic;
using System.Globalization;

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
		public NumberLiteralExpression(SourcePosition pos, string val, Syntax.NumberType type)
		: base(pos)
		{
			strVal = val;
			numType = type;
		}

		private readonly string strVal;

		private readonly Syntax.NumberType numType;

		public object GetValue()
		{
			switch (numType) {
				case Syntax.NumberType.Integer:
					return uint.Parse(strVal);
				case Syntax.NumberType.Hexadecimal:
					return uint.Parse(strVal.Substring(2), NumberStyles.AllowHexSpecifier);
				case Syntax.NumberType.Decimal:
					return double.Parse(strVal, NumberStyles.AllowDecimalPoint);
				case Syntax.NumberType.Scientific:
					return double.Parse(strVal, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent);
			}
			throw new System.Exception(); // todo
		}
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
