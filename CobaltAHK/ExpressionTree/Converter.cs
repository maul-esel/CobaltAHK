using System;
using System.Globalization;
using System.Linq;
#if CustomDLR
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using System.Reflection;

namespace CobaltAHK.ExpressionTree
{
	internal static class Converter
	{
		private static readonly Type[] NumberTypes = { typeof(double), typeof(int), typeof(uint) };

		private static readonly Expression NumberFormat = Expression.Constant(CultureInfo.InvariantCulture.NumberFormat);

		private static readonly MethodInfo ObjectToString = typeof(object).GetMethod("ToString", Type.EmptyTypes);

		private static readonly MethodInfo NumberToString = typeof(IConvertible).GetMethod("ToString", new[] { typeof(IFormatProvider) });

		private static readonly MethodInfo ConvertibleToType = typeof(IConvertible).GetMethod("ToType", new[] { typeof(Type), typeof(IFormatProvider) });

		internal static Expression ConvertTo(Expression value, Type type)
		{
			if (CanCast(value.Type, type)) {
				return Expression.Convert(value, type);
			} else if (type == typeof(string)) {
				return ConvertToString(value);
			}

			return Expression.Condition(
				Is<IConvertible>(value),
				ConvertConvertible(value, type),
				Expression.Default(type) // todo: throw exception here?
			);
		}

		internal static Expression ConvertToObject(Expression value)
		{
			return Cast<object>(value);
		}

		#region to boolean

		internal static Expression ConvertToBoolean(Expression value)
		{
			var tmp = Expression.Parameter(typeof(object));
			return Expression.Block(new[] { tmp },
				Expression.Assign(tmp, Cast<object>(value)),
				Expression.Condition(
					Is<bool>(tmp),
					Cast<bool>(tmp),
					AndAlso(
						IsNotNull(tmp),
						IsNotEmptyString(tmp),
						IsNotZero(tmp)
					)
				)
			);
		}

		private static Expression IsNotNull(Expression expr)
		{
			return Expression.NotEqual(expr, Generator.NULL);
		}

		private static Expression IsNotEmptyString(Expression expr)
		{
			return Expression.NotEqual(expr, Generator.EMPTY_STRING);
		}

		private static Expression IsNotZero(Expression expr)
		{
			return Expression.Not(
				AndAlso(
					IsNumber(expr),
					Expression.Equal(
						ConvertConvertible(expr, typeof(double)),
						Expression.Constant(0.0)
					)
				)
			);
		}

		#endregion

		#region to string

		internal static Expression ConvertToString(Expression value)
		{
			return Expression.Condition(
				Expression.Equal(Cast<object>(value), Generator.NULL),
				Generator.EMPTY_STRING,
				Expression.Condition(IsNumber(value),
					ConvertNumberToString(value),
					Expression.Condition(Is<bool>(value),
						ConvertNumberToString(ConvertConvertible(value, typeof(int))),
						CreateToString(value)
				        )
			        )
			);
		}

		private static Expression CreateToString(Expression value)
		{
			return Expression.Call(value, ObjectToString);
		}

		internal static Expression ConvertNumberToString(Expression value)
		{
			return Expression.Call(Cast<IConvertible>(value), NumberToString, NumberFormat);
		}

		#endregion

		private static Expression ConvertConvertible(Expression value, Type type)
		{
			return Expression.Convert(
				Expression.Block(
					Expression.IfThen(
						Expression.Not(Is<IConvertible>(value)),
						Expression.Throw(Expression.Constant(new InvalidCastException()))
					),
					Expression.Call(
						Cast<IConvertible>(value),
						ConvertibleToType,
						Expression.Constant(type),
						Expression.Constant(null, typeof(IFormatProvider))
					)
				),
				type
			);
		}

		private static Expression IsNumber(Expression value)
		{
			return OrElse(NumberTypes.Select(type => Expression.TypeEqual(value, type)).ToArray());
		}

		private static Expression Cast<T>(Expression expr)
		{
			return Expression.Convert(expr, typeof(T));
		}

		private static Expression Is<T>(Expression expr)
		{
			return Expression.TypeIs(expr, typeof(T));
		}

		private static Expression AndAlso(params Expression[] exprs)
		{
			return exprs.Aggregate(Generator.TRUE, (condition, expr) => Expression.AndAlso(condition, expr));
		}

		private static Expression OrElse(params Expression[] exprs)
		{
			return exprs.Aggregate(Generator.FALSE, (condition, expr) => Expression.OrElse(condition, expr));
		}

		private static bool CanCast(Type from, Type to)
		{
			// original code taken from http://stackoverflow.com/questions/292437/#answer-4640305
			var input = Expression.Parameter(from);
			try {
				// If this succeeds then we can cast 'from' type to 'to' type using implicit coercion
				Expression.Lambda(Expression.Convert(input, to), input).Compile();
			} catch (InvalidOperationException) {
				return false;
			}
			return true;
		}
	}
}