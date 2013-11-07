using System;
using System.Collections.Generic;
using System.Linq;

namespace CobaltAHK.Expressions
{
	public class ExpressionChain
	{
		public void Append(ValueExpression expr)
		{
			if (operators.Count < expressions.Count) {
				Append((BinaryOperator)Operator.Concatenate);
			}
			expressions.Add(expr);
		}

		private IList<ValueExpression> expressions = new List<ValueExpression>();

		internal void Append(BinaryOperator op)
		{
			if (operators.Count >= expressions.Count) {
				throw new InvalidOperationException("count: " + operators.Count + " >= " + expressions.Count);
			}
			operators.Add(op);
		}

		private IList<BinaryOperator> operators = new List<BinaryOperator>();

		public int Length { get { return expressions.Count; } }

		public bool IsValid()
		{
			return operators.Count == expressions.Count - 1;
		}

		public ValueExpression ToExpression()
		{
			if (!IsValid()) {
				throw new InvalidOperationException(
#if DEBUG
					string.Format("The chain is not valid. There are {0} expressions and {1} operators.", expressions.Count, operators.Count)
#endif
					);
			}

			var ops = new List<BinaryOperator>(operators);
			var exps = new List<ValueExpression>(expressions);

			var precedences = new List<uint>(ops.Select(op => op.Precedence).Distinct());
			var match = new Func<BinaryOperator, bool>(op => op.Precedence == precedences.Max());

			while (exps.Count > 1 && ops.Count > 0) {
				while (ops.Where(match).Count() == 0) {
					precedences.Remove(precedences.Max());
				}

				var currentOp = ops.First(match);
				var index = ops.IndexOf(currentOp);

				if (IsRightToLeft(currentOp)) {
					currentOp = ops.Last(match);
					index = ops.LastIndexOf(currentOp);
				}

				var expr = new BinaryExpression(exps[index].Position, currentOp, exps[index], exps[index + 1]);

				ops.RemoveAt(index);
				exps.Remove(expr.Expressions);

				exps.Insert(index, expr);
			}

			return exps[0];
		}

		private bool IsRightToLeft(BinaryOperator op)
		{
			return op.Is(BinaryOperationType.Assign);
		}
	}
}