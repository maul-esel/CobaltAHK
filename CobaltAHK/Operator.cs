using System;
using System.Collections.Generic;
using System.Linq;

namespace CobaltAHK
{
	public abstract class Operator
	{
		private Operator() { }
		private class DummyOperator : Operator { } // todo: see how ExpressionChain behaves with no precedence value
		public static readonly Operator Dummy = new DummyOperator();

		protected Operator(string code, uint prec)
		{
			op = code;
			precedence = prec;
			set.Add(this);
		}

		public virtual bool Matches(string op)
		{
			return this.op == op.ToUpper();
		}

		protected readonly string op;
		public string Code { get { return op; } }

		private readonly uint precedence;
		public uint Precedence { get { return precedence; } }

		protected static SortedSet<Operator> set = new SortedSet<Operator>(new OperatorComparer());

		public static Operator GetOperator(string code)
		{
			return set.First(op => op.Matches(code));
		}

		public static bool IsOperator(string code)
		{
			return set.Where(op => op.Matches(code)).Count() > 0;
		}
		
		// todo: New
		public static readonly Operator Deref                   = new   UnaryOperator("%",  13); // todo
		public static readonly Operator ObjectAccess            = new  BinaryOperator(".",  13);
		public static readonly Operator Increment               = new   UnaryOperator("++", 12);
		public static readonly Operator Decrement               = new   UnaryOperator("--", 12);
		public static readonly Operator Power                   = new  BinaryOperator("**", 11);
		public static readonly Operator UnaryMinus              = new   UnaryOperator("-",  10);
		public static readonly Operator LogicalNot              = new   UnaryOperator("!",  10);
		public static readonly Operator BitwiseNot              = new   UnaryOperator("~",  10);
		public static readonly Operator Address                 = new   UnaryOperator("&",  10);
		public static readonly Operator Dereference             = new   UnaryOperator("*",  10);
		public static readonly Operator Multiply                = new  BinaryOperator("*",  10);
		public static readonly Operator TrueDivide              = new  BinaryOperator("/",  10);
		public static readonly Operator FloorDivide             = new  BinaryOperator("//", 10);
		public static readonly Operator Add                     = new  BinaryOperator("+",   9);
		public static readonly Operator Subtract                = new  BinaryOperator("-",   9);
		public static readonly Operator BitShiftLeft            = new  BinaryOperator("<<",  9);
		public static readonly Operator BitShiftRight           = new  BinaryOperator(">>",  9);
		public static readonly Operator BitwiseAnd              = new  BinaryOperator("&",   8);
		public static readonly Operator BitwiseXor              = new  BinaryOperator("^",   8);
		public static readonly Operator BitwiseOr               = new  BinaryOperator("|",   8);
		public static readonly Operator Concatenate             = new  BinaryOperator(".",   7);
		public static readonly Operator RegexMatch              = new  BinaryOperator("~=",  7);
		public static readonly Operator Greater                 = new  BinaryOperator(">",   6);
		public static readonly Operator Less                    = new  BinaryOperator("<",   6);
		public static readonly Operator GreaterOrEqual          = new  BinaryOperator(">=",  6);
		public static readonly Operator LessOrEqual             = new  BinaryOperator("<=",  6);
		public static readonly Operator Equal                   = new  BinaryOperator("=",   5);
		public static readonly Operator CaseEqual               = new  BinaryOperator("==",  5);
		public static readonly Operator NotEqual                = new  BinaryOperator("!=",  5);
		public static readonly Operator NotEqualAlt             = new  BinaryOperator("<>",  5);
		public static readonly Operator WordLogicalNot          = new   UnaryOperator("NOT", 4);
		public static readonly Operator LogicalAnd              = new  BinaryOperator("&&",  3);
		public static readonly Operator WordLogicalAnd          = new  BinaryOperator("AND", 3);
		public static readonly Operator LogicalOr               = new  BinaryOperator("||",  3);
		public static readonly Operator WordLogicalOr           = new  BinaryOperator("OR",  3);
		public static readonly Operator Ternary                 = new TernaryOperator("?", ":",  2); // todo
		public static readonly Operator Assign                  = new  BinaryOperator(":=",  1);
		public static readonly Operator AddAssign               = new  BinaryOperator("+=",  1);
		public static readonly Operator SubtractAssign          = new  BinaryOperator("-=",  1);
		public static readonly Operator MultiplyAssign          = new  BinaryOperator("*=",  1);
		public static readonly Operator TrueDivideAssign        = new  BinaryOperator("/=",  1);
		public static readonly Operator FloorDivideAssign       = new  BinaryOperator("//=", 1);
		public static readonly Operator ConcatenateAssign       = new  BinaryOperator(".=",  1);
		public static readonly Operator BitwiseOrAssign         = new  BinaryOperator("|=",  1);
		public static readonly Operator BitwiseAndAssign        = new  BinaryOperator("&=",  1);
		public static readonly Operator BitwiseXorAssign        = new  BinaryOperator("^=",  1);
		public static readonly Operator BitShiftLeftAssign      = new  BinaryOperator("<<=", 1);
		public static readonly Operator BitShiftRightAssign     = new  BinaryOperator(">>=", 1);
		public static readonly Operator AltObjAccess            = new  BinaryOperator("[",   0); // todo: is precedence correct? e.g. `f . a[b]` => `(f . a)[b]` ?

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

		public static bool IsCompoundAssignment(Operator op)
		{
			return compoundAssigns.ContainsKey(op);
		}

		public static Operator CompoundGetUnderlyingOperator(Operator op)
		{
			return compoundAssigns[op];
		}

		public static bool IsArithmetic(Operator op)
		{
			return op == Operator.Add        || op == Operator.FloorDivide
			    || op == Operator.Multiply   || op == Operator.Subtract
			    || op == Operator.TrueDivide || op == Operator.Power
			    || (IsCompoundAssignment(op) && IsArithmetic(CompoundGetUnderlyingOperator(op)));
		}

		private class OperatorComparer : IComparer<Operator>
		{
			public int Compare(Operator first, Operator second)
			{
				return (int)(first.Precedence - second.Precedence);
			}
		}
	}

	internal class UnaryOperator : Operator
	{
		internal UnaryOperator(string op, uint prec)
		: base(op, prec) { }
	}

	internal class BinaryOperator : Operator
	{
		internal BinaryOperator(string op, uint prec)
		: base(op, prec) { }
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

