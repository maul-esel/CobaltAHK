using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using DLR = System.Linq.Expressions;
using CobaltAHK.Expressions;

namespace CobaltAHK.ExpressionTree
{
	public class Generator
	{
		internal static readonly DLR.Expression NULL = DLR.Expression.Constant(null);
		internal static readonly DLR.Expression TRUE = DLR.Expression.Constant(true);
		internal static readonly DLR.Expression FALSE = DLR.Expression.Constant(false);

		public Generator(ScriptSettings config)
		{
			settings = config;
		}

		private readonly ScriptSettings settings;

		public DLR.Expression Generate(Expression expr, Scope scope)
		{
			if (expr is FunctionCallExpression) {
				return GenerateFunctionCall((FunctionCallExpression)expr, scope);
			} else if (expr is FunctionDefinitionExpression) {
				return GenerateFunctionDefinition((FunctionDefinitionExpression)expr, scope);
			} else if (expr is CustomVariableExpression) {
				return scope.ResolveVariable(((CustomVariableExpression)expr).Name);
			} else if (expr is ValueKeywordExpression) {
				switch (((ValueKeywordExpression)expr).Keyword) {
					case Syntax.ValueKeyword.False: return FALSE;
					case Syntax.ValueKeyword.True:  return TRUE;
					case Syntax.ValueKeyword.Null:  return NULL;
				}
			} else if (expr is BinaryExpression) {
				return GenerateBinaryExpression((BinaryExpression)expr, scope);
			} else if (expr is TernaryExpression) {
				return GenerateTernaryExpression((TernaryExpression)expr, scope);
			} else if (expr is StringLiteralExpression) {
				return DLR.Expression.Constant(((StringLiteralExpression)expr).String);
			} else if (expr is NumberLiteralExpression) {
				return DLR.Expression.Constant(((NumberLiteralExpression)expr).GetValue());
			} else if (expr is ObjectLiteralExpression) {
				return GenerateObjectLiteral((ObjectLiteralExpression)expr, scope);
			} else if (expr is ArrayLiteralExpression) {
				return GenerateArrayLiteral((ArrayLiteralExpression)expr, scope);
			} else if (expr is BlockExpression) {
				return GenerateIfElse((BlockExpression)expr, scope);
			}
			throw new NotImplementedException();
		}

		private DLR.Expression GenerateObjectLiteral(ObjectLiteralExpression obj, Scope scope)
		{
			var t = typeof(IEnumerable<object>);
			var constructor = typeof(CobaltAHKObject).GetConstructor(new[] { t, t });

			var keys = ExpressionArray(obj.Dictionary.Keys, scope);
			var values = ExpressionArray(obj.Dictionary.Values, scope);
			return DLR.Expression.New(constructor, keys, values);
		}

		private DLR.Expression GenerateArrayLiteral(ArrayLiteralExpression arr, Scope scope)
		{
			var create = DLR.Expression.New(typeof(List<object>));
			if (arr.List.Count() == 0) {
				return create;
			}

			return DLR.Expression.ListInit(
				create,
				arr.List.Select(e => DLR.Expression.Convert(Generate(e, scope), typeof(object)))
			);
		}

		[Obsolete]
		private DLR.Expression ExpressionArray(IEnumerable<Expression> exprs, Scope scope)
		{
			return DLR.Expression.NewArrayInit(typeof(object),
			                                   exprs.Select(e => DLR.Expression.Convert(Generate(e, scope), typeof(object))));
		}

		private DLR.Expression GenerateIfElse(BlockExpression block, Scope scope)
		{
			return GenerateIfElse(block.Branches, scope);
		}

		private DLR.Expression GenerateIfElse(IEnumerable<ControlFlowExpression> branches, Scope scope)
		{
			if (!(branches.ElementAt(0) is IfExpression)) {
				throw new InvalidOperationException();
			}
			var ifExpr = (IfExpression)branches.ElementAt(0);
			var ifCond = MakeBoolean(Generate(ifExpr.Condition, scope));
			var ifBlock = DLR.Expression.Block(scope.GetVariables(), ifExpr.Body.Select(e => Generate(e, scope)).Concat(new[] { DLR.Expression.Empty() }));

			if (branches.Count() == 1) {
				return DLR.Expression.IfThen(ifCond, ifBlock);

			} else if (branches.ElementAt(1) is ElseExpression) {
				var elseExpr = (ElseExpression)branches.ElementAt(1);
				return DLR.Expression.IfThenElse(ifCond, ifBlock, DLR.Expression.Block(scope.GetVariables(),
				                                                                       elseExpr.Body.Select(e => Generate(e, scope))));
			} else {
				return DLR.Expression.IfThenElse(ifCond, ifBlock, GenerateIfElse(branches.Except(new[] { ifExpr }), scope));
			}
		}

		private DLR.Expression MakeBoolean(DLR.Expression expr) // todo
		{
			var tmp = DLR.Expression.Parameter(typeof(object));
			return DLR.Expression.Block(new[] { tmp },
				DLR.Expression.Assign(tmp, DLR.Expression.Convert(expr, typeof(object))),
				DLR.Expression.Condition(
					DLR.Expression.TypeIs(tmp, typeof(bool)),
					DLR.Expression.Convert(tmp, typeof(bool)),
					DLR.Expression.AndAlso(
						DLR.Expression.NotEqual(tmp, DLR.Expression.Constant(null, typeof(object))),
						DLR.Expression.AndAlso(
							DLR.Expression.NotEqual(tmp, DLR.Expression.Constant("", typeof(object))),
					                DLR.Expression.AndAlso(
								DLR.Expression.NotEqual(tmp, DLR.Expression.Constant(false, typeof(object))),
								DLR.Expression.NotEqual(tmp, DLR.Expression.Constant(0, typeof(object)))
							)
						)
					)
				)
			);
		}

		private DLR.Expression GenerateFunctionCall(FunctionCallExpression func, Scope scope)
		{
			if (!scope.FunctionExists(func.Name)) {
				throw new Exception("Unknown function: " + func.Name); // todo

			} else if (!scope.IsFunctionDefined(func.Name)) {
				return GenerateDynamicFunctionCall(func, scope);
			}

			var lambda = scope.ResolveFunction(func.Name);

			var prms = GenerateParams(func, scope);
			if (lambda.Parameters.Count != prms.Count()) {
				throw new Exception(); // todo
			}
			return DLR.Expression.Invoke(lambda, prms);
		}

		private DLR.Expression GenerateDynamicFunctionCall(FunctionCallExpression func, Scope scope)
		{
			var args = new List<DLR.Expression>(func.Parameters.Count() + 1);
			args.Add(DLR.Expression.Constant(func.Name));
			args.AddRange(GenerateParams(func, scope));
			// todo: store param count in scope and validate?

			var binder = new FunctionCallBinder(new CallInfo(func.Parameters.Count()), scope); // todo: cache instances?

			return DLR.Expression.Dynamic(binder, typeof(object), args);
		}

		private IEnumerable<DLR.Expression> GenerateParams(FunctionCallExpression func, Scope scope)
		{
			return func.Parameters.Select(p => Generate(p, scope)); // todo: convert param types
		}

		private DLR.Expression GenerateFunctionDefinition(FunctionDefinitionExpression func, Scope scope)
		{
			var funcScope = scope.GetScope(func); // get the scope created by the preprocessor

			var prms = new List<DLR.ParameterExpression>();
			var types = new List<Type>(prms.Count + 1);
			var funcBody = new List<DLR.Expression>();

			foreach (var p in func.Parameters) {
				var param = DLR.Expression.Parameter(typeof(object), p.Name);
				prms.Add(param);
				funcScope.AddVariable(p.Name, param);

				/*if (p.DefaultValue != null) {
					// todo: init: `if (!param) { param := default }`
					// todo: this conflicts with intentionally-passed NULL
					// todo: instead, add overloads
					// todo: however, especially with named parameter support, there's endless possibilities and thus endless overloads
					// todo: instead, store default values and detect them on function call
				}*/

				var type = typeof(object);
				if (p.Modifier.HasFlag(Syntax.ParameterModifier.ByRef)) {
					type = type.MakeByRefType();
				}
				types.Add(type);
			}
			types.Add(typeof(object)); // return value

			var endOfFunc = DLR.Expression.Label(typeof(object));
			foreach (var e in func.Body) {
				DLR.Expression expr;
				if (e is ReturnExpression) {
					expr = MakeReturn((ReturnExpression)e, scope, funcBody, endOfFunc);
				} else {
					expr = Generate(e, funcScope);
				}
				funcBody.Add(expr);
			}
			funcBody.Add(DLR.Expression.Label(endOfFunc, NULL)); // default return value is null

			var function = DLR.Expression.Lambda(
				DLR.Expression.GetFuncType(types.ToArray()),
				DLR.Expression.Block(prms, funcBody),
				func.Name,
				prms
			);

			scope.AddFunction(func.Name, function);

			return function;
		}

		private DLR.Expression MakeReturn(ReturnExpression expr, Scope scope, IList<DLR.Expression> body, DLR.LabelTarget target)
		{
			foreach (var e in expr.OtherExpressions) {
				body.Add(Generate(e, scope));
			}
			if (expr.Value != null) {
				var val = Generate(expr.Value, scope);
				return DLR.Expression.Return(target, DLR.Expression.Convert(val, typeof(object)));
			}
			return DLR.Expression.Return(target, NULL);
		}

		private DLR.Expression GenerateTernaryExpression(TernaryExpression expr, Scope scope)
		{
			var cond = Generate(expr.Expressions.ElementAt(0), scope);
			var ifTrue = Generate(expr.Expressions.ElementAt(1), scope);
			var ifFalse = Generate(expr.Expressions.ElementAt(2), scope);

			return DLR.Expression.Condition(MakeBoolean(cond), ifTrue, ifFalse);
		}

		#region binary operations

		private DLR.Expression GenerateBinaryExpression(BinaryExpression expr, Scope scope)
		{
			var left  = Generate(expr.Expressions.ElementAt(0), scope);
			var right = Generate(expr.Expressions.ElementAt(1), scope);

			return GenerateBinaryExpression(left, expr.Operator, right, scope);
		}

		private DLR.Expression GenerateBinaryExpression(DLR.Expression left, Operator op, DLR.Expression right, Scope scope)
		{
			if (Operator.IsArithmetic(op)) {
				return GenerateArithmeticExpression(left, op, right, scope);
			}

			if (op == Operator.Concatenate) {
				var concat = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });
				return DLR.Expression.Call(concat, MakeString(left), MakeString(right));

			} else if (op == Operator.ConcatenateAssign) { // todo: retype to string
				return CompoundAssigment((DLR.ParameterExpression)left, left, op, right, scope); // `a .= b` <=> `a := a . b`

			} else if (op == Operator.Assign) {
				if (left is DLR.ParameterExpression) {
					left = RetypeVariable((DLR.ParameterExpression)left, right.Type, scope);
				}
				return DLR.Expression.Assign(left, right);
			}

			throw new NotImplementedException();
		}

		private DLR.Expression CompoundAssigment(DLR.ParameterExpression variable, DLR.Expression left, Operator op, DLR.Expression right, Scope scope)
		{
			return GenerateBinaryExpression(variable, Operator.Assign, GenerateBinaryExpression(left, Operator.CompoundGetUnderlyingOperator(op), right, scope), scope);
		}

		#region arithmetic

		private DLR.Expression GenerateArithmeticExpression(DLR.Expression left, Operator op, DLR.Expression right, Scope scope)
		{
			Type leftType = left.Type, rightType = right.Type;
			DLR.ParameterExpression variable = null;
			if (NegotiateArithmeticTypes(ref leftType, ref rightType)) {
				if (leftType != left.Type) {
					if (left is DLR.ParameterExpression && Operator.IsCompoundAssignment(op)) {
						variable = RetypeVariable((DLR.ParameterExpression)left, leftType, scope);
					}
					left = DLR.Expression.Convert(left, leftType); // todo: see below
				}
				if (rightType != right.Type) {
					right = DLR.Expression.Convert(right, rightType); // todo: conversion from non-arithmetic types like string, object, ...
				}
			}

			if (Operator.IsCompoundAssignment(op)) {
				return CompoundAssigment(variable, left, op, right, scope);

			} else if (op == Operator.Add) {
				return DLR.Expression.Add(left, right);

			} else if (op == Operator.Subtract) {
				return DLR.Expression.Subtract(left, right);

			} else if (op == Operator.Multiply) {
				return DLR.Expression.Multiply(left, right);

			} else if (op == Operator.TrueDivide) {
				return DLR.Expression.Divide(left, right);

			} else if (op == Operator.FloorDivide) {
				var floor = typeof(Math).GetMethod("Floor", new[] { typeof(double) });
				return DLR.Expression.Call(floor, GenerateArithmeticExpression(left, Operator.TrueDivide, right, scope));

			} else if (op == Operator.Power) {
				return DLR.Expression.Power(DLR.Expression.Convert(left, typeof(double)), DLR.Expression.Convert(right, typeof(double)));
			}

			throw new InvalidOperationException(); // todo
		}

		private bool NegotiateArithmeticTypes(ref Type left, ref Type right)
		{
			Type _left = left, _right = right;
			if (!IsArithmeticType(left) && !IsArithmeticType(right)) {
				left = right = arithmeticTypes.First();
			} else {
				left = right = HigherArithmeticType(left, right);
			}
			return _left != left || _right != right;
		}

		private static readonly Type[] arithmeticTypes = new[] { typeof(double), typeof(int), typeof(uint) };

		private Type HigherArithmeticType(Type one, Type two)
		{
			return arithmeticTypes.First(t => t == one || t == two);
		}

		private bool IsArithmeticType(Type type)
		{
			return arithmeticTypes.Contains(type);
		}

		#endregion

		#endregion

		private DLR.ParameterExpression RetypeVariable(DLR.ParameterExpression variable, Type type, Scope scope)
		{
			if (variable.Type != type) {
				Func<DLR.ParameterExpression, bool> match = v => v.Name == variable.Name && v.Type == type;
				if (scope.GetVariables().Any(match)) {
					variable = scope.GetVariables().First(match);
				} else {
					variable = DLR.Expression.Parameter(type, variable.Name);
				}
				scope.AddVariable(variable.Name, variable);
			}
			return variable;
		}

		private DLR.Expression MakeString(DLR.Expression expr)
		{
			DLR.Expression convert;

			var cultureToString = expr.Type.GetMethod("ToString", new[] { typeof(IFormatProvider) });
			if (CanConvert(expr.Type, typeof(string))) {
				convert = DLR.Expression.Convert(expr, typeof(string));

			} else if (cultureToString != null) {
				convert = DLR.Expression.Call(expr, cultureToString, DLR.Expression.Constant(System.Globalization.CultureInfo.InvariantCulture.NumberFormat));

			} else {
				convert = DLR.Expression.Call(expr, expr.Type.GetMethod("ToString", Type.EmptyTypes));
			}

			return DLR.Expression.Condition(DLR.Expression.Equal(DLR.Expression.Convert(expr, typeof(object)), NULL), // handle null values
			                                DLR.Expression.Constant(""),
			                                convert);
		}

		private bool CanConvert(Type from, Type to)
		{
			// original code taken from http://stackoverflow.com/questions/292437/#answer-4640305
			var input = DLR.Expression.Parameter(from);
			try {
				// If this succeeds then we can cast 'from' type to 'to' type using implicit coercion
				DLR.Expression.Lambda(DLR.Expression.Convert(input, to), input).Compile();
			} catch (InvalidOperationException) {
				return false;
			}
			return true;
		}
	}
}