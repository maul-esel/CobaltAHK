using System.Collections.Generic;
using System.Reflection;

namespace CobaltAHK
{
	public abstract class Token
	{
		#region any state
		public static readonly Token EOF              = new PunctuationToken(unchecked ((char)-1));
		public static readonly Token Newline          = new PunctuationToken('\n');
		public static readonly Token Comma            = new PunctuationToken(',');
		#endregion

		// State.Traditional
		public static readonly Token ForceExpression  = new PunctuationToken('%');

		#region State.Expression (and others)
		public static readonly Token Colon            = new PunctuationToken(':');
		public static readonly Token OpenParenthesis  = new PunctuationToken('(');
		public static readonly Token CloseParenthesis = new PunctuationToken(')');
		public static readonly Token OpenBrace        = new PunctuationToken('{'); // blocks or object literals
		public static readonly Token CloseBrace       = new PunctuationToken('}');
		public static readonly Token OpenBracket      = new PunctuationToken('['); // object access or array literals
		public static readonly Token CloseBracket     = new PunctuationToken(']');
		#endregion
	}

	public abstract class PositionedToken : Token
	{
		protected PositionedToken(SourcePosition pos)
		{
			position = pos;
		}

		private readonly SourcePosition position;

		public SourcePosition Position { get { return position; } }
	}

	public class PunctuationToken : Token
	{
		public PunctuationToken(char ch)
		{
			character = ch;
		}

		private readonly char character;

		public char Character { get { return character; } }
	}

	#region TextToken

	public abstract class TextToken : PositionedToken
	{
		protected TextToken(SourcePosition pos, string str)
		: base(pos)
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

	#region comments

	public abstract class CommentToken : TextToken
	{
		public CommentToken(SourcePosition pos, string comment) : base(pos, comment) { }
	}

	public class SingleCommentToken : CommentToken
	{
		public SingleCommentToken(SourcePosition pos, string comment) : base(pos, comment) { }
	}

	public class MultiLineCommentToken : CommentToken
	{
		public MultiLineCommentToken(SourcePosition pos, string comment) : base(pos, comment) { }
	}

	#endregion

	public class QuotedStringToken : TextToken
	{
		public QuotedStringToken(SourcePosition pos, string str) : base(pos, str) { }
	}

	public class TraditionalStringToken : TextToken
	{
		public TraditionalStringToken(SourcePosition pos, string str) : base(pos, str) { }
	}

	public class NumberToken : TextToken
	{
		public NumberToken(SourcePosition pos, string str, Syntax.NumberType t)
		: base(pos, str)
		{
			type = t;
		}

		private Syntax.NumberType type;

		public Syntax.NumberType Type { get { return type; } }
	}

	public class VariableToken : TextToken // for variables in traditional mode
	{
		public VariableToken(SourcePosition pos, string var) : base(pos, var) { }
	}

	public class IdToken : TextToken // for variables, command names, ...
	{
		public IdToken(SourcePosition pos, string id) : base(pos, id) { }
	}

	public class FunctionToken : TextToken // for function calls or definitions (id followed by opening parenthese)
	{
		public FunctionToken(SourcePosition pos, string name) : base(pos, name) { }
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

	public class ValueKeywordToken : EnumToken<Syntax.ValueKeyword, ValueKeywordToken>
	{
		protected ValueKeywordToken(Syntax.ValueKeyword kw) : base(kw) { }

		public Syntax.ValueKeyword Keyword { get { return value; } }
	}
}