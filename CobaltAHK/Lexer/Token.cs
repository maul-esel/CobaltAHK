using System.Collections.Generic;

namespace CobaltAHK
{
	public abstract class Token
	{
		#region any state
		public static readonly Token EOF              = new CharacterToken(unchecked ((char)-1));
		public static readonly Token Newline          = new CharacterToken('\n');
		public static readonly Token Comma            = new CharacterToken(',');
		#endregion

		public static readonly Token ClassDefinition  = new TextToken("class");

		// State.Traditional
		public static readonly Token ForceExpression  = new CharacterToken('%');

		#region State.Expression (and others)
		[System.Obsolete]
		public static readonly Token Colon            = new CharacterToken(':');

		public static readonly Token OpenParenthesis  = new CharacterToken('(');
		public static readonly Token CloseParenthesis = new CharacterToken(')');
		public static readonly Token OpenBrace        = new CharacterToken('{'); // blocks or object literals
		public static readonly Token CloseBrace       = new CharacterToken('}');
		public static readonly Token OpenBracket      = new CharacterToken('['); // object access or array literals
		public static readonly Token CloseBracket     = new CharacterToken(']');
		#endregion
	}

	#region TextToken

	public /*abstract*/ class TextToken : Token
	{
		public TextToken(string str)
		{
			text = str;
		}

		private string text;

		public string Text { get { return text; } }

#if DEBUG
		public override string ToString()
		{
			return string.Format("[" + GetType() + ": Text='{0}']", Text.Escape());
		}
#endif
	}

	public class CharacterToken : TextToken
	{
		internal CharacterToken(char ch) : base(ch.ToString()) { }
	}

	#region comments

	public abstract class CommentToken : TextToken
	{
		public CommentToken(string comment) : base(comment) { }
	}

	public class SingleCommentToken : CommentToken
	{
		public SingleCommentToken(string comment) : base(comment) { }
	}

	public class MultiLineCommentToken : CommentToken
	{
		public MultiLineCommentToken (string comment) : base(comment) { }
	}

	#endregion

	public class QuotedStringToken : TextToken
	{
		public QuotedStringToken(string str) : base(str) { }
	}

	public class TraditionalStringToken : TextToken
	{
		public TraditionalStringToken(string str) : base(str) { }
	}

	public class NumberToken : TextToken
	{
		public NumberToken(string str, Syntax.NumberType t)
		: base(str)
		{
			type = t;
		}

		private Syntax.NumberType type;

		public Syntax.NumberType Type { get { return type; } }
	}

	public class VariableToken : TextToken // for variables in traditional mode
	{
		public VariableToken(string var) : base(var) { }
	}

	public class IdToken : TextToken // for variables, command names, ...
	{
		public IdToken(string id) : base(id) { }
	}

	public class FunctionToken : TextToken // for function calls or definitions (id followed by opening parenthese)
	{
		public FunctionToken(string name) : base(name) { }
	}

	#endregion


	public class DirectiveToken : Token
	{
		public DirectiveToken(Syntax.Directive dir)
		{
			directive = dir;
		}

		private readonly Syntax.Directive directive;

		public Syntax.Directive Directive {
			get { return directive; }
		}
	}

	public class HotkeyToken : Token { }

	public class HotstringToken : Token { }

	public class OperatorToken : Token
	{
		protected OperatorToken(Operator _op)
		{
			op = _op;
		}

		private Operator op;

		public Operator Operator { get { return op; } }

#if DEBUG
		public override string ToString()
		{
			return string.Format("[OperatorToken: Operator='{0}' Type={1}]", Operator.Code, Operator.GetType());
		}
#endif

		private static IDictionary<Operator, OperatorToken> map = new Dictionary<Operator, OperatorToken>();

		public static OperatorToken GetToken(Operator op)
		{
			if (!map.ContainsKey(op)) {
				map[op] = new OperatorToken(op);
			}
			return map[op];
		}
	}
}

