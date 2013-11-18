using System.Collections.Generic;

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

	public class DirectiveExpression : Expression
	{
		public DirectiveExpression(SourcePosition pos, Syntax.Directive dir, ValueExpression[] prms)
		: base(pos)
		{
			directive = dir;
			parameters = prms;
		}

		private readonly Syntax.Directive directive;

		public Syntax.Directive Directive { get { return directive; } }

		private readonly ValueExpression[] parameters;

#if DEBUG
		public override string ToString()
		{
			string str = "";
			foreach (var p in parameters) {
				str += (p == null ? "<null>" : p.TypeName()) + " ";
			}
			return string.Format("[DirectiveExpression Directive='{0}', {1} parameters: {2}]", directive, parameters.Length, str);
		}
#endif
	}

	public class IfDirectiveExpression : Expression
	{
		public IfDirectiveExpression(SourcePosition pos, ValueExpression cond) : base(pos) { }
	}

	#region value expressions

	public abstract class ValueExpression : Expression
	{
		protected ValueExpression(SourcePosition pos) : base(pos) { }
	}

	public class FunctionCallExpression : ValueExpression
	{
		public FunctionCallExpression(SourcePosition pos, string funcName, ValueExpression[] prms)
		: base(pos)
		{
			name = funcName;
			parameters = prms;
		}

		private readonly string name;

		public string Name { get { return name; } }

		private readonly ValueExpression[] parameters;

		public ValueExpression[] Parameters { get { return parameters; } }
	}

	public class ValueKeywordExpression : ValueExpression
	{
		public ValueKeywordExpression(SourcePosition pos, Syntax.ValueKeyword kw)
		: base(pos)
		{
			keyword = kw;
		}

		private readonly Syntax.ValueKeyword keyword;

		public Syntax.ValueKeyword Keyword { get { return keyword; } }
	}

	public class CLRNameExpression : ValueExpression
	{
		public CLRNameExpression(SourcePosition pos, string n)
		: base(pos)
		{
			name = n;
		}

		private readonly string name;

		public string Name { get { return name; } }

	}

	#region members

	public abstract class MemberExpression : ValueExpression
	{
		public MemberExpression(SourcePosition pos, ValueExpression o, string m)
		: base(pos)
		{
			obj = o;
			member = m;
		}

		private readonly ValueExpression obj;

		public ValueExpression Object { get { return obj; } }

		private readonly string member;

		public string Member { get { return member; } }
	}

	public class MemberAccessExpression : MemberExpression
	{
		public MemberAccessExpression(SourcePosition pos, ValueExpression o, string member) : base(pos, o, member) { }
	}

	public class MemberInvokeExpression : MemberExpression // todo: arguments
	{
		public MemberInvokeExpression(SourcePosition pos, ValueExpression o, string member) : base(pos, o, member) { }
	}

	#endregion

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
		protected OperatorExpression(SourcePosition pos, Operator op, params ValueExpression[] exprs)
		: base(pos)
		{
			this.op = op;
			expressions = exprs;
		}

		private readonly Operator op;

		public Operator Operator { get { return op; } }

		private readonly ValueExpression[] expressions;

		public ValueExpression[] Expressions { get { return expressions; } }
	}

	// todo: differentiate between post/pre-increment and -decrement
	public class UnaryExpression : OperatorExpression
	{
		public UnaryExpression(SourcePosition pos, Operator op, ValueExpression expr) : base(pos, op, expr) { }
	}

	public class BinaryExpression : OperatorExpression
	{
		public BinaryExpression(SourcePosition pos, Operator op, ValueExpression expr1, ValueExpression expr2) : base(pos, op, expr1, expr2) { }
	}

	public class TernaryExpression : OperatorExpression
	{
		public TernaryExpression(SourcePosition pos, ValueExpression cond, ValueExpression ifTrue, ValueExpression ifFalse)
		: this(pos, Operator.Ternary, cond, ifTrue, ifFalse) { }

		public TernaryExpression(SourcePosition pos, Operator op, ValueExpression expr1, ValueExpression expr2, ValueExpression expr3) : base(pos, op, expr1, expr2, expr3) { }
	}

	#endregion

	#endregion
}