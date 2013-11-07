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
				Append(Operator.Concatenate);
			}
			expressions.Add(expr);
		}

		private IList<ValueExpression> expressions = new List<ValueExpression>();

		public void Append(Operator op)
		{
			if (!(op is BinaryOperator) && !(op is TernaryOperator)) {
				throw new InvalidOperationException("type");
			}
			if (operators.Count >= expressions.Count) {
				throw new InvalidOperationException("count: " + operators.Count + " >= " + expressions.Count);
			}
			operators.Add(op);

			if (op is TernaryOperator) {
				operators.Add(Operator.Dummy); // add a dummy to keep the count statisfied
			}
		}

		private IList<Operator> operators = new List<Operator>();

		public int Length { get { return expressions.Count; } }

		public bool IsValid ()
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

			var ops = new List<Operator>(operators);
			var exps = new List<ValueExpression>(expressions);

			var precedences = new List<uint>(ops.Select(op => op.Precedence).Distinct());
			var match = new Func<Operator, bool>(op => op.Precedence == precedences.Max());

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

				OperatorExpression expr;

				if (currentOp is BinaryOperator) {
					expr = new BinaryExpression(exps[index].Position, currentOp, exps[index], exps[index + 1]);
				} else if (currentOp is TernaryOperator) {
					expr = new TernaryExpression(exps[index].Position, currentOp, exps[index], exps[index + 1], exps[index + 2]);

					ops.RemoveAt(index + 1); // remove dummy operator
				} else {
					throw new Exception(); // todo
				}

				ops.RemoveAt(index);
				exps.Remove(expr.Expressions);

				exps.Insert(index, expr);
			}

			return exps[0];
		}

		private bool IsRightToLeft(Operator op)
		{
			return (op is BinaryOperator && ((BinaryOperator)op).Is(BinaryOperationType.Assign)) || op == Operator.Ternary;
		}
	}
}