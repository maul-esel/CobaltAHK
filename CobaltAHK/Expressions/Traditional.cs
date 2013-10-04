using System.Collections.Generic;

namespace CobaltAHK.Expressions.Traditional
{
	public abstract class Expression : Expressions.Expression
	{
		protected Expression(SourcePosition pos) : base(pos) { }
	}

	public abstract class SimpleExpression : Expression
	{
		protected SimpleExpression(SourcePosition pos) : base(pos) { }
	}

	public class StringExpression : SimpleExpression // the default (strings)
	{
		public StringExpression(SourcePosition pos, string val)
		: base(pos)
		{
			str = val;
		}

		private readonly string str;

		public string String { get { return str; } }
	}

	public abstract class VariableExpression : SimpleExpression // for %variables%
	{
		protected VariableExpression(SourcePosition pos) : base(pos) { }
	}

	public class CustomVariableExpression : VariableExpression
	{
		public CustomVariableExpression(SourcePosition pos, string name) : base(pos) { }
	}

	public class BuiltinVariableExpression : VariableExpression
	{
		public BuiltinVariableExpression(SourcePosition pos, Syntax.BuiltinVariable builtin)
		: base(pos)
		{
			variable = builtin;
		}

		private readonly Syntax.BuiltinVariable variable;

		public Syntax.BuiltinVariable Variable { get { return variable; } }
	}

	public class CombinedStringExpression : SimpleExpression // combines strings and variables
	{
		public CombinedStringExpression(SourcePosition pos) : base(pos) { }

		public CombinedStringExpression(SourcePosition pos, IEnumerable<SimpleExpression> initial)
		: this(pos)
		{
			foreach (var expr in initial) {
				Append(expr);
			}
		}

		public void Append(SimpleExpression e)
		{
			if (e is CombinedStringExpression) {
				foreach (var expr in ((CombinedStringExpression)e).Expressions) {
					Append(e);
				}
			} else {
				expressions.Add(e);
			}
		}

		private readonly IList<SimpleExpression> expressions = new List<SimpleExpression>();

		public IEnumerable<SimpleExpression> Expressions { get { return expressions; } }
	}

	public class ForceExpressionExpression : Expression // for commands with params like `% "my str" . var.func()`
	{
		public ForceExpressionExpression(SourcePosition pos, ValueExpression expr) : base(pos) { }
	}
}