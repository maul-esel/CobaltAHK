using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using CobaltAHK.Expressions;
using Traditional = CobaltAHK.Expressions.Traditional;

namespace CobaltAHK
{
	public class Parser
	{
		public Expression[] Parse(TextReader code)
		{
			var lexer = new Lexer(code);
			var expressions = new List<Expression>();

			var token = lexer.PeekToken();
			while (token != Token.EOF) {
				expressions.Add(ParseExpression(lexer));
				token = lexer.PeekToken();
			}

			return expressions.ToArray();
		}

		private Expression ParseExpression(Lexer lexer)
		{
			var token = lexer.PeekToken();

			// skip empty lines
			while (token == Token.Newline) {
				lexer.GetToken();
				token = lexer.PeekToken();
			}

			if (token is CommentToken) {
				lexer.GetToken();
				return new CommentExpression(lexer.Position, ((CommentToken)token).Text, token is MultiLineCommentToken);

			} else if (token is DirectiveToken) {
				var directive = (DirectiveToken)token;
				if (directive.Directive == Syntax.Directive.If) {
					return new IfDirectiveExpression(lexer.Position, ParseExpressionChain(lexer).ToExpression());
				}
				return ParseDirective(lexer);

			} else if (token is IdToken) {
				var id = (IdToken)lexer.GetToken();

				Expression expr;
				if (TryParseIdExpression(lexer, id, out expr) || TryParseIdTraditional(lexer, id, out expr)) {
					return expr;
				}

				throw new Exception(); // todo

			} else if (token is FunctionToken) {
				return ParseFunctionCallOrDefinition(lexer);

			} else if (token == Token.ClassDefinition) {
				return ParseClassDefinition(lexer);

			} else if (token is HotkeyToken) {
				throw new NotImplementedException("hotkey");

			} else if (token is HotstringToken) {
				throw new NotImplementedException("hotstring");
			}
			throw new NotImplementedException(token.ToString());
		}

		#region try modes

		private bool TryParseIdExpression(Lexer lexer, IdToken id, out Expression expr)
		{
			return ParseWithState(lexer, Lexer.State.Expression, out expr, token => {
				if (token is OperatorToken) {
					var chain = new ExpressionChain();
					chain.Append(GetVariable(id.Text, lexer.Position));

					ParseExpressionChain(lexer, chain);
					return chain.ToExpression();
				}
				return null;
			});
		}

		private bool TryParseIdTraditional(Lexer lexer, IdToken id, out Expression expr)
		{
			return ParseWithState(lexer, Lexer.State.Traditional, out expr, token => {
				if (token == Token.Comma || token == Token.Newline || token == Token.EOF || token == Token.ForceExpression || token is TraditionalStringToken || token is VariableToken) {
					return ParseCommand(lexer, id);
				}
				return null;
			});
		}

		#endregion

		private CommandCallExpression ParseCommand(Lexer lexer, IdToken command)
		{
			lexer.PushState(Lexer.State.Traditional);
			if (lexer.PeekToken() == Token.Comma) {
				lexer.GetToken();// consume it so it isn't mistaked for an empty parameter
			}

			var parameters = ParseParameters(lexer);
			lexer.PopState();
			return new CommandCallExpression(lexer.Position, command.Text, parameters);
		}

		private DirectiveExpression ParseDirective(Lexer lexer)
		{
			AssertToken(lexer.PeekToken(), typeof(DirectiveToken));
			var directive = (DirectiveToken)lexer.GetToken();

			lexer.PushState(Lexer.State.Traditional);
			if (lexer.PeekToken() == Token.Comma) {
				lexer.GetToken(); // consume so it's not considered an empty parameter
			}

			var parameters = ValidateDirectiveParams(ParseParameters(lexer), directive.Directive);
			lexer.PopState();
			return new DirectiveExpression(lexer.Position, directive.Directive, parameters.ToArray());
		}

		private ClassDefinitionExpression ParseClassDefinition(Lexer lexer)
		{
			AssertToken(lexer.GetToken(), Token.ClassDefinition);
			lexer.PushState(Lexer.State.Expression);

			AssertToken(lexer.PeekToken(), typeof(IdToken));
			var name = (IdToken)lexer.GetToken();

			SkipNewline(lexer);
			var token = lexer.PeekToken();

			AssertToken(token, Token.OpenBrace);
			var expressions = ParseDefinitionBody(lexer);

			IEnumerable<FunctionDefinitionExpression> methods;
			FilterClassBodyExpressions(expressions, out methods);

			lexer.PopState();
			return new ClassDefinitionExpression(lexer.Position, name.Text, methods);
		}

		/// <summary>
		/// 	Parses a function call.
		/// </summary>
		/// <returns>
		/// 	The FunctionCallExpression representing the function call.
		/// </returns>
		/// <param name='lexer'>
		/// 	The current Lexer instance;
		/// </param>
		/// <remarks>
		/// 	Expects a <see cref="FunctionToken"/> and <see cref="Token.OpenParenthesis"/> from the Lexer.
		/// </remarks>
		private FunctionCallExpression ParseFunctionCall(Lexer lexer)
		{
			AssertToken(lexer.PeekToken(), typeof(FunctionToken));
			var func = (FunctionToken)lexer.GetToken();

			lexer.PushState(Lexer.State.Expression);

			AssertToken(lexer.PeekToken(), Token.OpenParenthesis);
			var parameters = ParseExpressionList(lexer);

			lexer.PopState();
			return new FunctionCallExpression(lexer.Position, func.Text, parameters);
		}

		private Expression ParseFunctionCallOrDefinition(Lexer lexer)
		{
			AssertToken(lexer.PeekToken(), typeof(FunctionToken));
			var func = (FunctionToken)lexer.GetToken();

			lexer.PushState(Lexer.State.Expression);

			AssertToken(lexer.PeekToken(), Token.OpenParenthesis);
			var parameters = ParseExpressionList(lexer);

			bool newline = SkipNewline(lexer);
			var token = lexer.PeekToken();

			Expression result;
			if (token == Token.OpenBrace) { // function definition
				var body = ParseDefinitionBody(lexer);
				var prms = ValidateFunctionDefParams(parameters);
				result = new FunctionDefinitionExpression(lexer.Position, func.Text, prms, body);

			} else { // function call
				var funcExpr = new FunctionCallExpression(lexer.Position, func.Text, parameters);
				bool concat = newline && token is OperatorToken; // todo
				if (!newline || concat) {
					var chain = new ExpressionChain();
					chain.Append(funcExpr);
					ParseExpressionChain(lexer, chain); // parse further expressions here, like `myfunc("myParam").Add(5)`
					result = chain.ToExpression();

				} else {
					result = funcExpr;
				}
			}

			lexer.PopState();
			return result;
		}

		#region helpers

		#region traditional

		private IEnumerable<Traditional.SimpleExpression> ValidateDirectiveParams(IEnumerable<Traditional.Expression> parameters, Syntax.Directive directive)
		{
			var list = new List<Traditional.SimpleExpression>();

			foreach (var param in parameters) {
				if (param is Traditional.ForceExpressionExpression) {
					throw new Exception(); // todo

				} else if (param is Traditional.CustomVariableExpression) {
					throw new Exception(); // todo

				} else if (param is Traditional.BuiltinVariableExpression) {
					if (directive != Syntax.Directive.Include) {
						throw new Exception(); // todo
					}

					var variable = (Traditional.BuiltinVariableExpression)param;
					if (!variable.Variable.IsAllowedInInclude()) {
						throw new Exception(); // todo
					}

				} else if (param is Traditional.CombinedStringExpression) {
					ValidateDirectiveParams(((Traditional.CombinedStringExpression)param).Expressions, directive);
				}

				list.Add((Traditional.SimpleExpression)param);
			}

			return list.ToArray();
		}

		private Traditional.VariableExpression GetVariableTraditional(VariableToken token, SourcePosition pos)
		{
			if (Syntax.IsBuiltinVariable(token.Text)) {
				return new Traditional.BuiltinVariableExpression(pos, Syntax.GetBuiltinVariable(token.Text));
			} else {
				return new Traditional.CustomVariableExpression(pos, token.Text);
			}
		}

		private static bool IsEmptyString(Traditional.SimpleExpression expr)
		{
			return expr is Traditional.StringExpression && ((Traditional.StringExpression)expr).String.Trim() == String.Empty;
		}

		private Traditional.SimpleExpression CombineTraditionalExpressions(Traditional.SimpleExpression currentExpr, Traditional.SimpleExpression newExpr)
		{
			if (currentExpr == null) {
				return IsEmptyString(newExpr) ? null : newExpr;

			} else if (currentExpr is Traditional.CombinedStringExpression) {
				((Traditional.CombinedStringExpression)currentExpr).Append(newExpr);
				return currentExpr;

			} else {
				return new Traditional.CombinedStringExpression(currentExpr.Position, new[] { currentExpr, newExpr });
			}
		}

		private Traditional.Expression[] ParseParameters(Lexer lexer)
		{
			lexer.PushState(Lexer.State.Traditional);
			var list = new List<Traditional.Expression>();

			var token = lexer.PeekToken();
			Traditional.Expression currentParam = null;
			while (true) {
				bool consumed = false;
				if (token == Token.EOF || token is SingleCommentToken) {
					if (currentParam == null && list.Count > 0) { // throw, but not if there are no params at all
						throw new Exception(); // todo

					} else if (currentParam != null) {
						list.Add(currentParam);
					}

					currentParam = null;
					break;

				} else if (token == Token.Newline) {
					Console.WriteLine(lexer.GetToken());
					consumed = true;

					var pos = lexer.Position;
					bool concat = lexer.PeekToken() == Token.Comma; // todo: concat with a comment in between // possibly: expression queue

					if (currentParam == null && !concat && list.Count > 0) { // throw, but not if there are no params at all
						throw new Exception(); // todo
					}

					if (!concat) {
						if (currentParam != null) {
							list.Add(currentParam);
							currentParam = null;
						}

						lexer.ResetToken();
						lexer.Rewind(pos);

						break;
					}

				} else if (token == Token.Comma) {
					list.Add(currentParam); // appends NULL for empty parameters
					currentParam = null;

				} else if (token == Token.ForceExpression) {
					if (currentParam != null) {
						throw new Exception("ForceExpression must be first"); // todo
					}

					lexer.GetToken();
					consumed = true;

					var expr = ParseExpressionChain(lexer, new[] { Token.Comma }).ToExpression();
					currentParam = new Traditional.ForceExpressionExpression(lexer.Position, expr);

				} else if (token is TraditionalStringToken) {
					// todo: ensure currentParam is not ForceExpressionExpression
					var str = (TraditionalStringToken)token;
					var expr = new Traditional.StringExpression(lexer.Position, str.Text);
					currentParam = CombineTraditionalExpressions((Traditional.SimpleExpression)currentParam, expr);

				} else if (token is VariableToken) {
					// todo: ensure currentParam is not ForceExpressionExpression
					var expr = GetVariableTraditional((VariableToken)token, lexer.Position);
					currentParam = CombineTraditionalExpressions((Traditional.SimpleExpression)currentParam, expr);

				} else {
					throw new Exception("unsupported token: " + token); // todo
				}

				if (!consumed) {
					lexer.GetToken();
				}
				token = lexer.PeekToken();
			}

			lexer.PopState();
			return list.ToArray();
		}

		#endregion

		#region expression mode

		private ExpressionChain ParseExpressionChain(Lexer lexer, IEnumerable<Token> terminators = null)
		{
			var chain = new ExpressionChain();
			ParseExpressionChain(lexer, chain, terminators);
			return chain;
		}

		private void ParseExpressionChain(Lexer lexer, ExpressionChain chain, IEnumerable<Token> terminators = null)
		{
			lexer.PushState(Lexer.State.Expression);
			var token = lexer.PeekToken();

			while (token != Token.EOF) {
				if (token == Token.Newline) {
					lexer.GetToken();
					token = lexer.PeekToken();
					if (!(token is OperatorToken)) { // don't concat
						break;
					}
				}

				if (terminators != null && terminators.Contains(token)) {
					break;
				}

				if (token == Token.OpenParenthesis) {
					lexer.GetToken(); // consume parenthesis
					chain.Append(ParseExpressionChain(lexer, new[] { Token.CloseParenthesis }).ToExpression());
				
				} else if (token is OperatorToken) {
					if (((OperatorToken)token).Operator == Operator.AltObjAccess) { // special handling for f[A, B]
						ParseAltObjAccess(lexer, chain);

					} else if (((OperatorToken)token).Operator is UnaryOperator) {
						throw new NotImplementedException();

					} else {
						chain.Append(((OperatorToken)token).Operator);
					}

				} else {
					chain.Append(TokenToValueExpression(lexer));
					token = lexer.PeekToken();
					continue;
				}

				lexer.GetToken();
				token = lexer.PeekToken();
			}

			lexer.PopState();
		}

		private ValueExpression[] ParseExpressionList(Lexer lexer)
		{
			return ParseExpressionList(lexer, Token.OpenParenthesis, Token.CloseParenthesis);
		}

		private ValueExpression[] ParseExpressionList(Lexer lexer, Token open, Token close)
		{
			lexer.PushState(Lexer.State.Expression);
			AssertToken(lexer.GetToken(), open);

			var list = new List<ValueExpression>();
			ExpressionChain currentExpr = null;

			var token = lexer.PeekToken();
			while (true) {
				if (token == Token.EOF) {
					throw new UnexpectedEOFException(lexer.Position);
				}

				if (token == Token.Comma) {
					// todo: if currentExpr == null -> allow empty? or fail? (param to define?) (to allow empty, must add Expression.Empty parameter)
					list.Add(currentExpr.ToExpression());
					currentExpr = null;

				} else if (token == close) {
					// todo: if currentExpr == null -> allow empty? or fail? (to allow empty, just ignore) (if list.Count == 0 -> don't fail)
					if (currentExpr != null) {
						list.Add(currentExpr.ToExpression());
					}
					break;

				} else {
					currentExpr = ParseExpressionChain(lexer, new[] { Token.Comma, close });
					token = lexer.PeekToken();
					continue;
				}

				lexer.GetToken();
				token = lexer.PeekToken();
			}

			AssertToken(lexer.GetToken(), close);
			lexer.PopState();
			return list.ToArray();
		}

		/// <summary>
		/// 	Converts a Token to a ValueExpression.
		/// </summary>
		/// <returns>
		/// 	The ValueExpression.
		/// </returns>
		/// <param name='lexer'>
		/// 	The current Lexer instance.
		/// </param>
		/// <exception cref="Exception">
		/// 	Thrown if the token cannot be converted.
		/// </exception>
		/// <remarks>
		/// 	This function consumes the converted token(s).
		/// </remarks>
		private ValueExpression TokenToValueExpression(Lexer lexer)
		{
			ValueExpression expr;

			var token = lexer.PeekToken();
			if (token is FunctionToken) {
				expr = ParseFunctionCall(lexer);

			} else if (token is IdToken) {
				var id = (IdToken)lexer.GetToken();
				expr = GetVariable(id.Text, lexer.Position);
			
			} else if (token is QuotedStringToken) {
				var str = (QuotedStringToken)lexer.GetToken();
				expr = new StringLiteralExpression(lexer.Position, str.Text);

			} else if (token is NumberToken) {
				var number = (NumberToken)lexer.GetToken();
				expr = new NumberLiteralExpression(lexer.Position, number.Text, number.Type);
			
			} else if (token == Token.OpenBracket) {
				var arr = ParseExpressionList(lexer, Token.OpenBracket, Token.CloseBracket);
				expr = new ArrayLiteralExpression(lexer.Position, arr);
			
			} else if (token == Token.OpenBrace) {
				var obj = ParseObjectLiteral(lexer);
				expr = new ObjectLiteralExpression(lexer.Position, obj);

			} else {
				throw new Exception(token.ToString()); // todo
			}
	
			return expr;
		}

		private void ParseAltObjAccess(Lexer lexer, ExpressionChain chain)
		{
			AssertToken(lexer.GetToken(), OperatorToken.GetToken(Operator.AltObjAccess));
			chain.Append(Operator.AltObjAccess);

			var token = lexer.PeekToken();
			ValueExpression currentParam = null;
			while (token != Token.CloseBracket) {
				// todo
				if (token == Token.Comma) {
					lexer.GetToken();
					if (currentParam == null) {
						// todo: empty expression? fail?
					}
					chain.Append(currentParam);
					chain.Append(Operator.AltObjAccess);
					currentParam = null;

				} else {
					if (currentParam != null) {
						throw new Exception(); // todo
					}
					currentParam = ParseExpressionChain(lexer, new[] { Token.Comma, Token.CloseBracket }).ToExpression();
				}
				token = lexer.PeekToken();
			}
			if (currentParam == null) {
				// todo (see above for comma)
			}
			chain.Append(currentParam);
		}

		private IDictionary<ValueExpression, ValueExpression> ParseObjectLiteral(Lexer lexer)
		{
			lexer.PushState(Lexer.State.Expression);
			AssertToken(lexer.GetToken(), Token.OpenBrace);

			var dict = new Dictionary<ValueExpression, ValueExpression>();

			var token = lexer.PeekToken();
			while (token != Token.CloseBrace) {
				var key = ParseExpressionChain(lexer, new[] { Token.Colon }).ToExpression();
				AssertToken(lexer.GetToken(), Token.Colon);
				var value = ParseExpressionChain(lexer, new[] { Token.Comma, Token.CloseBrace }).ToExpression();

				dict[key] = value;
				token = lexer.PeekToken();
			}
			AssertToken(lexer.GetToken(), Token.CloseBrace);

			lexer.PopState();
			return dict;
		}

		#endregion

		#region definitions

		#region function definitions

		private ParameterDefinitionExpression[] ValidateFunctionDefParams(ValueExpression[] exprs)
		{
			var list = new List<ParameterDefinitionExpression>();

			foreach (var expr in exprs) {
				string name;
				Syntax.ParameterModifier modifier = Syntax.ParameterModifier.None;
				ValueExpression defaultValue = null;

				ExtractParamDef(expr, out name, ref modifier, ref defaultValue);
				var param = new ParameterDefinitionExpression(expr.Position, name, modifier, defaultValue);

				list.Add(param);
			}

			return list.ToArray();
		}

		private void ExtractParamDef(ValueExpression expr, out string name, ref Syntax.ParameterModifier modifier, ref ValueExpression defaultValue)
		{
			if (expr is CustomVariableExpression) {
				name = ExtractParamDefName(expr);

			} else if (expr is BinaryExpression) {
				var bin = (BinaryExpression)expr;

				if (bin.Operator == Operator.Concatenate) { // todo: allow implicit only
					ExtractParamDefModifier(bin, out name, out modifier);

				} else if (bin.Operator == Operator.Assign) {
					var first = bin.Expressions.ElementAt(0);
					if (first is CustomVariableExpression) {
						name = ExtractParamDefName(first);
					} else {
						ExtractParamDefModifier(bin.Expressions.ElementAt(0), out name, out modifier);
					}

					defaultValue = bin.Expressions.ElementAt(1); // todo: what's allowed as default? other vars? or only Literals?

				} else {
					throw new Exception("invalid operation"); // todo
				}

			} else {
				throw new Exception("invalid expression"); // todo
			}
		}

		private string ExtractParamDefName(ValueExpression expr)
		{
			var v = expr as CustomVariableExpression;

			if (v == null) {
				throw new Exception(); // todo
			} else if (Syntax.IsParameterModifier(v.Name)) {
				throw new Exception(); // todo
			}

			return v.Name;
		}

		private void ExtractParamDefModifier(ValueExpression expr, out string name, out Syntax.ParameterModifier modifier)
		{
			var bin = expr as BinaryExpression;
			if (bin == null || bin.Operator != Operator.Concatenate) { // todo: allow implicit concat only
				throw new Exception("not concat"); // todo
			}

			var first = bin.Expressions.ElementAt(0) as CustomVariableExpression;
			if (first == null || !Syntax.IsParameterModifier(first.Name)) {
				throw new Exception("invalid modifier");
			}

			modifier = Syntax.GetParameterModifier(first.Name);
			name = ExtractParamDefName(bin.Expressions.ElementAt(1));
		}

		#endregion

		#region class definitions

		private void FilterClassBodyExpressions(IEnumerable<Expression> expressions, out IEnumerable<FunctionDefinitionExpression> methods)
		{
			var methodList = new List<FunctionDefinitionExpression> ();

			foreach (var expr in expressions) {
				if (expr is FunctionDefinitionExpression) {
					methodList.Add((FunctionDefinitionExpression)expr);
				} else { // todo: fields, comments and other allowed expressions
					throw new Exception(); // todo
				}
			}

			methods = methodList.ToArray();
		}

		#endregion

		private Expression[] ParseDefinitionBody(Lexer lexer)
		{
			AssertToken(lexer.GetToken(), Token.OpenBrace);
			AssertToken(lexer.GetToken(), Token.Newline); // todo: is newline enforced?

			var body = new List<Expression>();
			lexer.PushState(Lexer.State.Root); // this should allow nested functions - must be properly handled or disallowed

			var token = lexer.PeekToken();
			while (token != Token.CloseBrace) {
				if (token == Token.EOF) {
					throw new UnexpectedEOFException(lexer.Position);

				} else if (token == Token.Newline) {
					lexer.GetToken(); // consume newline
					token = lexer.PeekToken();
					continue;
				}

				body.Add(ParseExpression(lexer)); // consumes tokens
				token = lexer.PeekToken();
			}
			lexer.GetToken(); // swallow the closing brace
			AssertToken(lexer.GetToken(), Token.Newline, Token.EOF);

			lexer.PopState();
			return body.ToArray();
		}

		#endregion

		private bool ParseWithState(Lexer lexer, Lexer.State state, out Expression result, Func<Token, Expression> fn)
		{
			var before = lexer.Position;
			lexer.PushState(state);

			var token = lexer.PeekToken();
			result = fn(token);
			var success = result != null;

			if (!success) {
				lexer.ResetToken();
				lexer.Rewind(before);
			}
			lexer.PopState();

			return success;
		}

		private VariableExpression GetVariable(string name, SourcePosition pos)
		{
			if (Syntax.IsBuiltinVariable(name)) {
				return new BuiltinVariableExpression(pos, Syntax.GetBuiltinVariable(name));
			} else {
				return new CustomVariableExpression(pos, name);
			}
		}

		private void AssertToken(Token actual, params Token[] expected)
		{
			if (!expected.Contains(actual)) {
				throw new UnexpectedTokenException("Expected token '" + expected.ToString() + "' but was '" + actual.ToString() + "'");
			}
		}

		private void AssertToken(Token actual, Type expected)
		{
			if (actual.GetType() != expected && !actual.GetType().IsSubclassOf(expected)) {
				throw new UnexpectedTokenException("Expected token of type '" + expected.ToString() + "' but was '" + actual.ToString() + "'");
			}
		}

		private bool SkipNewline(Lexer lexer, uint max = 1)
		{
			var token = lexer.PeekToken();
			uint index = 0;
			bool skipped = false;
			while (token == Token.Newline && index < max) {
				skipped = true;
				lexer.GetToken();
				token = lexer.PeekToken();
			}
			return skipped;
		}

		#endregion
	}

	public class UnexpectedTokenException : Exception
	{
		public UnexpectedTokenException(string msg) : base(msg) { }
	}
}