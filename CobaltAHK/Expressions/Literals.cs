using System.Collections.Generic;
using System.Globalization;

namespace CobaltAHK.Expressions
{
	public abstract class ValueLiteralExpression<T> : ValueExpression
	{
		protected ValueLiteralExpression(SourcePosition pos, T val)
		: base(pos)
		{
			value = val;
		}

		private readonly T value;

		public T Value { get { return value; } }
	}

	public class StringLiteralExpression : ValueLiteralExpression<string>
	{
		public StringLiteralExpression(SourcePosition pos, string val)
		: base(pos, val) { }
	}

	public class NumberLiteralExpression : ValueLiteralExpression<string>
	{
		public NumberLiteralExpression(SourcePosition pos, string val, Syntax.NumberType type)
		: base(pos, val)
		{
			numType = type;
		}

		private readonly Syntax.NumberType numType;

		public object GetValue()
		{
			switch (numType) {
				case Syntax.NumberType.Integer:
					return uint.Parse(Value);
				case Syntax.NumberType.Hexadecimal:
					return uint.Parse(Value.Substring(2), NumberStyles.AllowHexSpecifier);
				case Syntax.NumberType.Decimal:
					return double.Parse(Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture.NumberFormat);
				case Syntax.NumberType.Scientific:
					return double.Parse(Value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture.NumberFormat);
			}
			throw new System.Exception(); // todo
		}
	}

	public class ObjectLiteralExpression : ValueLiteralExpression<IDictionary<ValueExpression, ValueExpression>>
	{
		public ObjectLiteralExpression(SourcePosition pos, IDictionary<ValueExpression, ValueExpression> obj)
		: base(pos, obj) { }
	}

	public class ArrayLiteralExpression : ValueLiteralExpression<ValueExpression[]>
	{
		public ArrayLiteralExpression(SourcePosition pos, ValueExpression[] arr)
		: base(pos, arr) { }
	}
}