using System.Collections.Generic;

namespace CobaltAHK.Expressions
{
	public class ReturnExpression : Expression
	{
		public ReturnExpression(SourcePosition pos, ValueExpression val, IEnumerable<ValueExpression> exprs)
		: base(pos)
		{
			value = val;
			other = exprs;
		}

		private readonly ValueExpression value;

		public ValueExpression Value { get { return value; } }

		private readonly IEnumerable<ValueExpression> other;

		public IEnumerable<ValueExpression> OtherExpressions { get { return other; } }
	}

	public class ThrowExpression : Expression
	{
		public ThrowExpression(SourcePosition pos, ValueExpression val)
		: base(pos)
		{
			value = val;
		}

		private readonly ValueExpression value;

		public ValueExpression Value { get { return value; } }
	}
}