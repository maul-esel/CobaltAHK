using System;
using System.Collections.Generic;
using System.Linq;

namespace CobaltAHK
{
	public abstract class Operator
	{
		protected Operator(string op, uint prec)
		{
			code = op;
			precedence = prec;
			set.Add(this);
		}

		public virtual bool Matches(string op)
		{
			return Code == op.ToUpper();
		}

		#region fields

		protected readonly string code;

		private readonly uint precedence;

		#endregion
		#region properties

		public string Code { get { return code; } }

		public uint Precedence { get { return precedence; } }

		#endregion

		protected static ISet<Operator> set = new HashSet<Operator>();

		internal static IEnumerable<Operator> Operators { get { return set; } }

		public static Operator GetOperator(string code)
		{
			return set.First(op => op.Matches(code));
		}

		public static bool IsOperator(string code)
		{
			return set.Where(op => op.Matches(code)).Count() > 0;
		}

		#region instances
		// todo: New
		public static readonly Operator Deref                   = new   UnaryOperator("%",  13); // todo
		public static readonly Operator ObjectAccess            = new  BinaryOperator(".", 13,
		                                                                              BinaryOperationType.Other,
		                                                                              Whitespace.neither);
		public static readonly Operator Increment               = new   UnaryOperator("++", 12/*,
		                                                                              Whitespace.before_xor_after*/);
		public static readonly Operator Decrement               = new   UnaryOperator("--", 12/*,
		                                                                              Whitespace.before_xor_after*/);
		public static readonly Operator Power                   = new  BinaryOperator("**", 11, BinaryOperationType.Arithmetic);
		public static readonly Operator UnaryMinus              = new   UnaryOperator("-",  10);
		public static readonly Operator LogicalNot              = new   UnaryOperator("!",  10);
		public static readonly Operator BitwiseNot              = new   UnaryOperator("~",  10);
		public static readonly Operator Address                 = new   UnaryOperator("&",  10);
		public static readonly Operator Dereference             = new   UnaryOperator("*",  10);
		public static readonly Operator Multiply                = new  BinaryOperator("*",  10,
		                                                                              BinaryOperationType.Arithmetic,
		                                                                              Whitespace.both_or_neither);
		public static readonly Operator TrueDivide              = new  BinaryOperator("/",  10, BinaryOperationType.Arithmetic);
		public static readonly Operator FloorDivide             = new  BinaryOperator("//", 10, BinaryOperationType.Arithmetic);
		public static readonly Operator Add                     = new  BinaryOperator("+",   9,
		                                                                              BinaryOperationType.Arithmetic,
		                                                                              Whitespace.both_or_neither);
		public static readonly Operator Subtract                = new  BinaryOperator("-",   9,
		                                                                              BinaryOperationType.Arithmetic,
		                                                                              Whitespace.both_or_neither);
		public static readonly Operator BitShiftLeft            = new  BinaryOperator("<<",  9, BinaryOperationType.BitShift);
		public static readonly Operator BitShiftRight           = new  BinaryOperator(">>",  9, BinaryOperationType.BitShift);
		public static readonly Operator BitwiseAnd              = new  BinaryOperator("&",   8,
		                                                                              BinaryOperationType.Bitwise,
		                                                                              Whitespace.both_or_neither);
		public static readonly Operator BitwiseXor              = new  BinaryOperator("^",   8, BinaryOperationType.Bitwise);
		public static readonly Operator BitwiseOr               = new  BinaryOperator("|",   8, BinaryOperationType.Bitwise);
		public static readonly Operator Concatenate             = new  BinaryOperator(".",   7,
		                                                                              BinaryOperationType.Other,
		                                                                              Whitespace.both);
		public static readonly Operator RegexMatch              = new  BinaryOperator("~=",  7, BinaryOperationType.Comparison);
		public static readonly Operator Greater                 = new  BinaryOperator(">",   6, BinaryOperationType.Comparison);
		public static readonly Operator Less                    = new  BinaryOperator("<",   6, BinaryOperationType.Comparison);
		public static readonly Operator GreaterOrEqual          = new  BinaryOperator(">=",  6, BinaryOperationType.Comparison);
		public static readonly Operator LessOrEqual             = new  BinaryOperator("<=",  6, BinaryOperationType.Comparison);
		public static readonly Operator Equal                   = new  BinaryOperator("=",   5, BinaryOperationType.Comparison);
		public static readonly Operator CaseEqual               = new  BinaryOperator("==",  5, BinaryOperationType.Comparison);
		public static readonly Operator NotEqual                = new  BinaryOperator("!=",  5, BinaryOperationType.Comparison);
		public static readonly Operator NotEqualAlt             = new  BinaryOperator("<>",  5, BinaryOperationType.Comparison);
		public static readonly Operator WordLogicalNot          = new   UnaryOperator("NOT", 4/*, Whitespace.both*/);
		public static readonly Operator LogicalAnd              = new  BinaryOperator("&&",  3, BinaryOperationType.Logical);
		public static readonly Operator WordLogicalAnd          = new  BinaryOperator("AND", 3, BinaryOperationType.Logical);
		public static readonly Operator LogicalOr               = new  BinaryOperator("||",  3, BinaryOperationType.Logical);
		public static readonly Operator WordLogicalOr           = new  BinaryOperator("OR",  3, BinaryOperationType.Logical);
		public static readonly Operator Ternary                 = new TernaryOperator("?", ":",  2); // todo
		public static readonly Operator Assign                  = new  BinaryOperator(":=",  1, BinaryOperationType.Assign);
		public static readonly Operator AddAssign               = new  BinaryOperator("+=",  1, BinaryOperationType.Assign|BinaryOperationType.Arithmetic);
		public static readonly Operator SubtractAssign          = new  BinaryOperator("-=",  1, BinaryOperationType.Assign|BinaryOperationType.Arithmetic);
		public static readonly Operator MultiplyAssign          = new  BinaryOperator("*=",  1, BinaryOperationType.Assign|BinaryOperationType.Arithmetic);
		public static readonly Operator TrueDivideAssign        = new  BinaryOperator("/=",  1, BinaryOperationType.Assign|BinaryOperationType.Arithmetic);
		public static readonly Operator FloorDivideAssign       = new  BinaryOperator("//=", 1, BinaryOperationType.Assign|BinaryOperationType.Arithmetic);
		public static readonly Operator ConcatenateAssign       = new  BinaryOperator(".=",  1, BinaryOperationType.Assign|BinaryOperationType.Other);
		public static readonly Operator BitwiseOrAssign         = new  BinaryOperator("|=",  1, BinaryOperationType.Assign|BinaryOperationType.Bitwise);
		public static readonly Operator BitwiseAndAssign        = new  BinaryOperator("&=",  1, BinaryOperationType.Assign|BinaryOperationType.Bitwise);
		public static readonly Operator BitwiseXorAssign        = new  BinaryOperator("^=",  1, BinaryOperationType.Assign|BinaryOperationType.Bitwise);
		public static readonly Operator BitShiftLeftAssign      = new  BinaryOperator("<<=", 1, BinaryOperationType.Assign|BinaryOperationType.BitShift);
		public static readonly Operator BitShiftRightAssign     = new  BinaryOperator(">>=", 1, BinaryOperationType.Assign|BinaryOperationType.BitShift);
		public static readonly Operator AltObjAccess            = new  BinaryOperator("[",   0,
		                                                                              BinaryOperationType.Other,
		                                                                              Whitespace.not_before); // todo: is precedence correct? e.g. `f . a[b]` => `(f . a)[b]` ?
		#endregion

		#region compound assignments

		private static readonly IDictionary<Operator, Operator> compoundAssigns = new Dictionary<Operator, Operator>() {
			{ Operator.ConcatenateAssign,   Operator.Concatenate   },
			{ Operator.AddAssign,           Operator.Add           },
			{ Operator.SubtractAssign,      Operator.Subtract      },
			{ Operator.MultiplyAssign,      Operator.Multiply      },
			{ Operator.TrueDivideAssign,    Operator.TrueDivide    },
			{ Operator.FloorDivideAssign,   Operator.FloorDivide   },
			{ Operator.BitwiseOrAssign,     Operator.BitwiseOr     },
			{ Operator.BitwiseAndAssign,    Operator.BitwiseAnd    },
			{ Operator.BitwiseXorAssign,    Operator.BitwiseXor    },
			{ Operator.BitShiftLeftAssign,  Operator.BitShiftLeft  },
			{ Operator.BitShiftRightAssign, Operator.BitShiftRight }
		};

		internal static bool IsCompoundAssignment(BinaryOperator op)
		{
			return op.Type.HasFlag(BinaryOperationType.Assign) && op.Type != BinaryOperationType.Assign;
		}

		internal static BinaryOperator CompoundGetUnderlyingOperator(BinaryOperator op)
		{
			return (BinaryOperator)compoundAssigns[op];
		}

		#endregion
	}

	[Flags]
	public enum Whitespace
	{
		none = 0,
		before = 1,
		not_before = 2,
		after = 4,
		not_after = 8,
		both = before|after,
		neither = not_before|not_after,
		both_or_neither = 16
	}

	internal class UnaryOperator : Operator
	{
		internal UnaryOperator(string op, uint prec)

		: base(op, prec)
	}

	internal class BinaryOperator : Operator
	{
		internal BinaryOperator(string op, uint prec, BinaryOperationType tp, Whitespace white)
		: base(op, prec)
		{
			type = tp;
			whitespace = white;
		}

		internal BinaryOperator(string op, uint prec, BinaryOperationType tp)
		: this(op, prec, tp, Whitespace.none)
		{
		}

		private readonly Whitespace whitespace = Whitespace.none;

		public Whitespace Whitespace { get { return whitespace; } }

		public bool Is(BinaryOperationType type)
		{
			return Type.HasFlag(type);
		}

		private readonly BinaryOperationType type;

		public BinaryOperationType Type { get { return type; } }
	}

	[Flags]
	public enum BinaryOperationType
	{
		None        =   0,
		Numeric     =   1,
		Comparison  =   4,
		Bitwise     =   8,
		BitShift    =  16,
		Logical     =  32,
		Other       =  64,
		Assign      = 128,
		Arithmetic  = Numeric|2
	}

	internal class TernaryOperator : Operator
	{
		internal TernaryOperator(string first, string second, uint prec)
		: base(first + second, prec)
		{
			this.first = first;
			this.second = second;
		}

		private readonly string first;
		private readonly string second;

		public override bool Matches(string op)
		{
			return op.ToUpper() == first || op.ToUpper() == second;
		}
	}
}