using System.Collections.Generic;
#if DEBUG
using System.Linq;
#endif

namespace CobaltAHK.Expressions
{
	public abstract class Expression
	{
		protected Expression(SourcePosition pos)
		{
			position = pos;
		}

		private readonly SourcePosition position;

		public SourcePosition Position { get { return position; } }
	}

	public class CommentExpression : Expression
	{
		public CommentExpression(SourcePosition pos, string comment, bool multiline) : base(pos) { }
	}

	public class DirectiveExpression : Expression
	{
		public DirectiveExpression(SourcePosition pos, Syntax.Directive dir, IEnumerable<ValueExpression> prms)
		: base(pos)
		{
			directive = dir;
			parameters = prms;
		}

		private readonly Syntax.Directive directive;

		public Syntax.Directive Directive { get { return directive; } }

		private readonly IEnumerable<ValueExpression> parameters;

#if DEBUG
		public override string ToString()
		{
			string str = "";
			foreach (var p in parameters) {
				str += (p == null ? "<null>" : p.TypeName()) + " ";
			}
			return string.Format("[DirectiveExpression Directive='{0}', {1} parameters: {2}]", directive, parameters.Count(), str);
		}
#endif
	}

	public class IfDirectiveExpression : Expression
	{
		public IfDirectiveExpression(SourcePosition pos, ValueExpression cond) : base(pos) { }
	}

	public class ReturnExpression : Expression
	{
		public ReturnExpression(SourcePosition pos, ValueExpression val, IEnumerable<ValueExpression> exprs)
		: base(pos)
		{
			value = val;
			other = exprs;
		}

		private ValueExpression value;

		public ValueExpression Value { get { return value; } }

		private IEnumerable<ValueExpression> other;

		public IEnumerable<ValueExpression> OtherExpressions { get { return other; } }
	}

	#region value expressions

	public abstract class ValueExpression : Expression
	{
		protected ValueExpression(SourcePosition pos) : base(pos) { }
	}

	public class FunctionCallExpression : ValueExpression
	{
		public FunctionCallExpression(SourcePosition pos, string funcName, IEnumerable<ValueExpression> prms)
		: base(pos)
		{
			name = funcName;
			parameters = prms;
		}

		private readonly string name;

		public string Name { get { return name; } }

		private readonly IEnumerable<ValueExpression> parameters;

		public IEnumerable<ValueExpression> Parameters { get { return parameters; } }
	}

	#region variables

	public abstract class VariableExpression : ValueExpression
	{
		protected VariableExpression(SourcePosition pos) : base(pos) { }
	}

	public class CustomVariableExpression : VariableExpression
	{
		public CustomVariableExpression(SourcePosition pos, string v)
		: base(pos)
		{
			name = v;
		}

		private readonly string name;

		public string Name { get { return name; } }
	}

	public class BuiltinVariableExpression : VariableExpression
	{
		public BuiltinVariableExpression(SourcePosition pos, Syntax.BuiltinVariable var)
		: base(pos)
		{
			variable = var;
		}

		private readonly Syntax.BuiltinVariable variable;

		public Syntax.BuiltinVariable Variable { get { return variable; } }
	}

	#endregion

	#region operator expressions

	public abstract class OperatorExpression : ValueExpression
	{
		protected OperatorExpression(SourcePosition pos, Operator op, IEnumerable<ValueExpression> exprs)
		: base(pos)
		{
			this.op = op;
			expressions = exprs;
		}

		private readonly Operator op;

		public Operator Operator { get { return op; } }

		private readonly IEnumerable<ValueExpression> expressions;

		public IEnumerable<ValueExpression> Expressions { get { return expressions; } }
	}

	// todo: differentiate between post/pre-increment and -decrement
	public class UnaryExpression : OperatorExpression
	{
		public UnaryExpression(SourcePosition pos, Operator op, ValueExpression expr) : base(pos, op, new[] { expr }) { }
	}

	public class BinaryExpression : OperatorExpression
	{
		public BinaryExpression(SourcePosition pos, Operator op, ValueExpression expr1, ValueExpression expr2) : base(pos, op, new[] { expr1, expr2 }) { }
	}

	public class TernaryExpression : OperatorExpression
	{
		public TernaryExpression(SourcePosition pos, Operator op, ValueExpression expr1, ValueExpression expr2, ValueExpression expr3) : base(pos, op, new[] { expr1, expr2, expr3 }) { }
	}

	#endregion

	#endregion
}

