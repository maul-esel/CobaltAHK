using System.Collections.Generic;
using System.Reflection;

namespace CobaltAHK
{
	public abstract class Token
	{
		#region any state
		public static readonly Token EOF              = new CharacterToken(unchecked ((char)-1));
		public static readonly Token Newline          = new CharacterToken('\n');
		public static readonly Token Comma            = new CharacterToken(',');
		#endregion

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

	public class HotkeyToken : Token { }

	public class HotstringToken : Token { }

	public abstract class EnumToken<T, TToken> : Token where TToken : EnumToken<T, TToken>
	{
		protected EnumToken(T val)
		{
			value = val;
		}

		protected T value;

		private static IDictionary<T, TToken> map = new Dictionary<T, TToken>();

		public static TToken GetToken(T key)
		{
			if (!map.ContainsKey(key)) {
				map[key] = CreateInstance(key);
			}
			return map[key];
		}

		private static TToken CreateInstance(T val)
		{
			var c = typeof(TToken).GetConstructor(
				BindingFlags.NonPublic|BindingFlags.Instance,
				null,
				new[] { typeof(T) },
				default(ParameterModifier[])
			);
			return (TToken)c.Invoke(new object[] { val });
		}
	}

	public class DirectiveToken : EnumToken<Syntax.Directive, DirectiveToken>
	{
		protected DirectiveToken(Syntax.Directive dir) : base(dir) { }

		public Syntax.Directive Directive { get { return value; } }
	}

	public class OperatorToken : EnumToken<Operator, OperatorToken>
	{
		protected OperatorToken(Operator op) : base(op) { }

		public Operator Operator { get { return value; } }

#if DEBUG
		public override string ToString()
		{
			return string.Format("[OperatorToken: Operator='{0}' Type={1}]", Operator.Code, Operator.GetType());
		}
#endif
	}

	public class KeywordToken : EnumToken<Syntax.Keyword, KeywordToken>
	{
		protected KeywordToken(Syntax.Keyword kw) : base(kw) { }

		public Syntax.Keyword Keyword { get { return value; } }
	}
}

