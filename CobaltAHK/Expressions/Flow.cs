using System.Collections.Generic;

namespace CobaltAHK.Expressions
{
	public class BlockExpression : Expression
	{
		public BlockExpression(SourcePosition pos, IEnumerable<ControlFlowExpression> exprs)
		: base(pos)
		{
			branches = exprs;
		}

		private readonly IEnumerable<ControlFlowExpression> branches;

		public IEnumerable<ControlFlowExpression> Branches { get { return branches; } }
	}

	public abstract class ControlFlowExpression : Expression
	{
		public ControlFlowExpression(SourcePosition pos, IEnumerable<Expression> exprs)
		: base(pos)
		{
			body = exprs;
		}

		private readonly IEnumerable<Expression> body;

		public IEnumerable<Expression> Body { get { return body; } }
	}

	public class IfExpression : ControlFlowExpression
	{
		public IfExpression(SourcePosition pos, ValueExpression cond, IEnumerable<Expression> body)
		: base(pos, body)
		{
			condition = cond;
		}

		private readonly ValueExpression condition;

		public ValueExpression Condition { get { return condition; } }
	}

	public class ElseExpression : ControlFlowExpression
	{
		public ElseExpression(SourcePosition pos, IEnumerable<Expression> body) : base(pos, body) { }
	}
}