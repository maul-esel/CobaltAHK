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
			} else if (expr is BinaryExpression) {
				return GenerateBinaryExpression((BinaryExpression)expr, scope);
			} else if (expr is StringLiteralExpression) {
				return DLR.Expression.Constant(((StringLiteralExpression)expr).String);
			} else if (expr is NumberLiteralExpression) {
				return DLR.Expression.Constant(((NumberLiteralExpression)expr).GetValue());
			} else if (expr is ObjectLiteralExpression) {
				return GenerateObjectLiteral((ObjectLiteralExpression)expr, scope);
			} else if (expr is ArrayLiteralExpression) {
				return GenerateArrayLiteral((ArrayLiteralExpression)expr, scope);
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
			return DLR.Expression.ListInit(
				DLR.Expression.New(typeof(List<object>)),
				arr.List.Select(e => DLR.Expression.Convert(Generate(e, scope), typeof(object)))
			);
		}

		[Obsolete]
		private DLR.Expression ExpressionArray(IEnumerable<Expression> exprs, Scope scope)
		{
			return DLR.Expression.NewArrayInit(typeof(object),
			                                   exprs.Select(e => DLR.Expression.Convert(Generate(e, scope), typeof(object))));
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
				// todo: default values
				var param = DLR.Expression.Parameter(typeof(object), p.Name);
				prms.Add(param);
				funcScope.AddVariable(p.Name, param);

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
				if (IsReturn(e)) {
					expr = MakeReturn((FunctionCallExpression)e, scope, funcBody, endOfFunc);
				} else {
					expr = Generate(e, funcScope);
				}
				funcBody.Add(expr);
			}
			funcBody.Add(DLR.Expression.Label(endOfFunc, DLR.Expression.Constant(null))); // default return value is null

			var function = DLR.Expression.Lambda(
				DLR.Expression.GetFuncType(types.ToArray()),
				DLR.Expression.Block(prms, funcBody),
				func.Name,
				prms
			);

			scope.AddFunction(func.Name, function);

			return function;
		}

		private bool IsReturn(Expression expr)
		{
			return expr is FunctionCallExpression && ((FunctionCallExpression)expr).Name.ToLower() == "return";
		}

		private DLR.Expression MakeReturn(FunctionCallExpression expr, Scope scope, IList<DLR.Expression> body, DLR.LabelTarget target)
		{
			var prms = expr.Parameters.ToArray();
			if (prms.Length == 0) {
				return DLR.Expression.Return(target);
			}
			for (var i = 0; i < prms.Length - 1; i++) {
				body.Add(Generate(prms[i], scope));
			}
			var val = Generate(prms[prms.Length - 1], scope);
			return DLR.Expression.Return(target, DLR.Expression.Convert(val, typeof(object)));
		}

		private DLR.Expression GenerateBinaryExpression(BinaryExpression expr, Scope scope)
		{
			var left  = Generate(expr.Expressions.ElementAt(0), scope);
			var right = Generate(expr.Expressions.ElementAt(1), scope);

			return GenerateBinaryExpression(left, expr.Operator, right, scope);
		}

		private DLR.Expression GenerateBinaryExpression(DLR.Expression left, Operator op, DLR.Expression right, Scope scope)
		{
			// todo: correct conversion workaround for math operations

			if (op == Operator.Concatenate) {
				var concat = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });
				return DLR.Expression.Call(concat, MakeString(left), MakeString(right));

			} else if (op == Operator.ConcatenateAssign) {
				return GenerateBinaryExpression(left, Operator.Assign, GenerateBinaryExpression(left, Operator.Concatenate, right)); // `a .= b` <=> `a := a . b`

			} else if (op == Operator.Add) {
				return DLR.Expression.Add(DLR.Expression.Convert(left, right.Type), right);

			} else if (op == Operator.AddAssign) {
				return DLR.Expression.AddAssign(DLR.Expression.Convert(left, right.Type), right);

			} else if (op == Operator.Subtract) {
				return DLR.Expression.Subtract(DLR.Expression.Convert(left, right.Type), right);

			} else if (op == Operator.SubtractAssign) {
				return DLR.Expression.SubtractAssign(DLR.Expression.Convert(left, right.Type), right);

			} else if (op == Operator.Multiply) {
				return DLR.Expression.Multiply(DLR.Expression.Convert(left, right.Type), right);

			} else if (op == Operator.MultiplyAssign) {
				return DLR.Expression.MultiplyAssign(DLR.Expression.Convert(left, right.Type), right);

			} else if (op == Operator.TrueDivide) {
				return DLR.Expression.Divide(DLR.Expression.Convert(left, right.Type), right);

			} else if (op == Operator.TrueDivideAssign) {
				return DLR.Expression.DivideAssign(DLR.Expression.Convert(left, right.Type), right);
			} else if (op == Operator.Assign) {
				return DLR.Expression.Assign(left, DLR.Expression.Convert(right, left.Type));
			}

			throw new NotImplementedException();
		}

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

			return DLR.Expression.Condition(DLR.Expression.Equal(DLR.Expression.Convert(expr, typeof(object)), DLR.Expression.Constant(null)), // handle null values
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