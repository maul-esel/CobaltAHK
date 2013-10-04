using System.Collections.Generic;

namespace CobaltAHK.Expressions
{
	public class BlockExpression : Expression
	{
		public BlockExpression(SourcePosition pos, IEnumerable<ControlFlowExpression> branches) : base(pos) { }
	}

	public abstract class ControlFlowExpression : Expression
	{
		public ControlFlowExpression(SourcePosition pos, IEnumerable<Expression> body) : base(pos) { }
	}

	public class IfExpression : ControlFlowExpression
	{
		public IfExpression(SourcePosition pos, ValueExpression cond, IEnumerable<Expression> body) : base(pos, body) { }
	}

	public class ElseIfExpression : ControlFlowExpression
	{
		public ElseIfExpression(SourcePosition pos, ValueExpression cond, IEnumerable<Expression> body) : base(pos, body) { }
	}

	public class ElseExpression : ControlFlowExpression
	{
		public ElseExpression(SourcePosition pos, IEnumerable<Expression> body) : base(pos, body) { }
	}
}