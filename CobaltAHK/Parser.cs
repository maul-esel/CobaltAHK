using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using CobaltAHK.Expressions;

namespace CobaltAHK
{
	public class Parser
	{
		public Expression[] Parse(TextReader code)
		{
			var lexer = new Lexer(code);
			var expressions = new List<Expression>();

			SkipNewlinesAndComments(lexer);
			var token = lexer.PeekToken();

			while (token != Token.EOF) {
				expressions.Add(ParseExpression(lexer));

				SkipNewlinesAndComments(lexer);
				token = lexer.PeekToken();
			}

			return expressions.ToArray();
		}

		private Expression ParseExpression(Lexer lexer)
		{
			var token = lexer.PeekToken();

			if (token is DirectiveToken) {
				var directive = (DirectiveToken)token;
				if (directive.Directive == Syntax.Directive.If) {
					return ParseWithState(lexer, Lexer.State.Expression,
					                 () => new IfDirectiveExpression(lexer.Position, ParseExpressionChain(lexer).ToExpression())
					);
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

			} else if (token is KeywordToken) {
				var kw = ((KeywordToken)token).Keyword;
				switch (kw) {
					case Syntax.Keyword.Class:
						return ParseClassDefinition(lexer);
					case Syntax.Keyword.Return:
						return ParseReturn(lexer);
					case Syntax.Keyword.Throw:
						return ParseThrow(lexer);
					case Syntax.Keyword.If:
						return ParseIf(lexer);
					case Syntax.Keyword.Else:
						throw new InvalidOperationException(); // todo
				}

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

					if (token == OperatorToken.GetToken(Operator.ObjectAccess)) {
						var acc = ParseObjectAccess(lexer, GetVariable(id.Text, id.Position));
						chain.Append(acc);
					} else {
						chain.Append(GetVariable(id.Text, id.Position));
					}

					ParseExpressionChain(lexer, chain);
					return chain.ToExpression();
				}
				// todo: ParseExpressionSequence() parses a list of unrelated, comma-separated expressions
				// todo: put them in the expression queue
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

		private FunctionCallExpression ParseCommand(Lexer lexer, IdToken command)
		{
			lexer.PushState(Lexer.State.Traditional);
			if (lexer.PeekToken() == Token.Comma) {
				lexer.GetToken();// consume it so it isn't mistakened for an empty parameter
			}

			var parameters = ParseParameters(lexer);
			lexer.PopState();
			return new FunctionCallExpression(command.Position, command.Text, parameters);
		}

		private BlockExpression ParseIf(Lexer lexer)
		{
			AssertToken(lexer.PeekToken(), KeywordToken.GetToken(Syntax.Keyword.If));
			var before = lexer.Position;
			var token = lexer.PeekToken();

			var branches = new List<ControlFlowExpression>();
			while (token is KeywordToken
			       && (((KeywordToken)token).Keyword == Syntax.Keyword.If || ((KeywordToken)token).Keyword == Syntax.Keyword.Else)) {
				branches.Add(ParseControlFlowBranch(lexer));

				before = lexer.Position;
				SkipNewline(lexer, UInt32.MaxValue);
				token = lexer.PeekToken();
			}
			lexer.Rewind(before);
			lexer.ResetToken();

			return new BlockExpression(lexer.Position, branches.ToArray());
		}

		private ControlFlowExpression ParseControlFlowBranch(Lexer lexer)
		{
			AssertToken(lexer.PeekToken(), typeof(KeywordToken));
			var token = (KeywordToken)lexer.GetToken();
			ValueExpression cond = null;

			bool isElse = false;
			if (token.Keyword == Syntax.Keyword.Else) {
				var before = lexer.Position;
				isElse = lexer.PeekToken() != KeywordToken.GetToken(Syntax.Keyword.If);

				if (isElse) {
					lexer.Rewind(before);
					lexer.ResetToken();
				} else {
					lexer.GetToken();
				}

			} else if (token.Keyword != Syntax.Keyword.If) {
				throw new Exception(); // todo
			}
			if (!isElse) {
				cond = ParseIfCondition(lexer);
			}

			var body = ParseBlock(lexer, e => ValidateExpressionInIfElse(e));

			if (isElse) {
				return new ElseExpression(lexer.Position, body);
			} else {
				return new IfExpression(lexer.Position, cond, body);
			}
		}

		private void ValidateExpressionInIfElse(Expression expr)
		{
			if (expr is DirectiveExpression || expr is FunctionDefinitionExpression || expr is ClassDefinitionExpression) {
				throw new Exception(); // todo
			}
		}

		private ValueExpression ParseIfCondition(Lexer lexer)
		{
			lexer.PushState(Lexer.State.Expression);
			var endToken = Token.OpenBrace; // todo: what about object literals?

			bool inParentheses = lexer.PeekToken() == Token.OpenParenthesis;
			if (inParentheses) {
				lexer.GetToken();
				endToken = Token.CloseParenthesis;
			}

			var cond = ParseExpressionChain(lexer, endToken).ToExpression();

			if (inParentheses) {
				AssertToken(lexer.GetToken(), Token.CloseParenthesis);
			}

			lexer.PopState();
			return cond;
		}

		private ReturnExpression ParseReturn(Lexer lexer)
		{
			AssertToken(lexer.GetToken(), KeywordToken.GetToken(Syntax.Keyword.Return));
			lexer.PushState(Lexer.State.Expression);

			Token endToken = null;
			if (lexer.PeekToken() == Token.OpenParenthesis) {
				lexer.GetToken();
				endToken = Token.CloseParenthesis;
			}

			var exprs = ParseExpressionSequence(lexer, endToken);

			if (endToken != null) {
				AssertToken(lexer.GetToken(), endToken);
			}

			var value = exprs.Length > 0 ? exprs.Last() : null;
			var others = exprs.Except(new[] { value });

			lexer.PopState();
			return new ReturnExpression(lexer.Position, value, others);
		}

		private ThrowExpression ParseThrow(Lexer lexer)
		{
			AssertToken(lexer.GetToken(), KeywordToken.GetToken(Syntax.Keyword.Throw));
			lexer.PushState(Lexer.State.Expression);

			Token[] endToken = {};
			if (lexer.PeekToken() == Token.OpenParenthesis) {
				lexer.GetToken();
				endToken = new[] { Token.CloseParenthesis };
			}

			var val = ParseExpressionChain(lexer, endToken);

			if (endToken.Length > 0) {
				AssertToken(lexer.GetToken(), endToken[0]);
			}

			lexer.PopState();
			return new ThrowExpression(lexer.Position, val.ToExpression());
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
			AssertToken(lexer.GetToken(), KeywordToken.GetToken(Syntax.Keyword.Class));
			lexer.PushState(Lexer.State.Expression);

			AssertToken(lexer.PeekToken(), typeof(IdToken));
			var name = (IdToken)lexer.GetToken();
			ValueExpression baseObj = null;

			var token = lexer.PeekToken();

			if (token is IdToken && ((IdToken)token).Text.ToLower() == "extends") {
				lexer.GetToken();
				AssertToken(lexer.PeekToken(), typeof(IdToken));

				var baseId = (IdToken)lexer.GetToken();
				baseObj = GetVariable(baseId.Text, baseId.Position);
			}

			SkipNewline(lexer);

			AssertToken(lexer.PeekToken(), Token.OpenBrace);
			var expressions = ParseBlock(lexer, e => ValidateExpressionInDefinition(e));
			AssertToken(lexer.GetToken(), Token.Newline, Token.EOF);

			IEnumerable<FunctionDefinitionExpression> methods;
			FilterClassBodyExpressions(expressions, out methods);

			lexer.PopState();
			return new ClassDefinitionExpression(lexer.Position, baseObj, name.Text, methods);
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
		private FunctionCallExpression ParseFunctionCall(ITokenStream stream)
		{
			AssertToken(stream.PeekToken(), typeof(FunctionToken));
			var func = (FunctionToken)stream.GetToken();

			AssertToken(stream.PeekToken(), Token.OpenParenthesis);
			var parameters = ParseExpressionList(stream);

			return new FunctionCallExpression(func.Position, func.Text, parameters);
		}

		private Expression ParseFunctionCallOrDefinition(Lexer lexer)
		{
			AssertToken(lexer.PeekToken(), typeof(FunctionToken));
			var func = (FunctionToken)lexer.GetToken();

			lexer.PushState(Lexer.State.Expression);
			var parameters = ReadFunctionParamTokens(lexer);

			var beforeToken = lexer.Position;
			bool newline = SkipNewline(lexer);
			var token = lexer.PeekToken();

			Expression result;
			if (token == Token.OpenBrace) { // function definition
				var body = ParseBlock(lexer, e => ValidateExpressionInDefinition(e));
				AssertToken(lexer.GetToken(), Token.Newline, Token.EOF);

				var prms = ParseParamDefinitions(parameters);
				result = new FunctionDefinitionExpression(func.Position, func.Text, prms, body);

			} else { // function call
				var funcExpr = new FunctionCallExpression(func.Position, func.Text, ParseExpressionList(parameters));
				bool concat = newline && token is OperatorToken; // todo
				if (!newline || concat) {
					var chain = new ExpressionChain();

					if (token == OperatorToken.GetToken(Operator.ObjectAccess)) {
						chain.Append(ParseObjectAccess(lexer, funcExpr));
					} else {
						chain.Append(funcExpr);
					}

					ParseExpressionChain(lexer, chain); // parse further expressions here, like `myfunc("myParam").Add(5)`
					result = chain.ToExpression();

				} else {
					result = funcExpr;
					lexer.Rewind(beforeToken);
					lexer.ResetToken();
				}
			}

			lexer.PopState();
			return result;
		}

		private ITokenStream ReadFunctionParamTokens(Lexer lexer)
		{
			var list = new List<Token>();
			var pos = lexer.Position;
			int level = 0;

			lexer.PushState(Lexer.State.Expression);
			AssertToken(lexer.PeekToken(), Token.OpenParenthesis);
			list.Add(lexer.GetToken());

			var token = lexer.PeekToken();
			while (token != Token.CloseParenthesis || level > 0) {
				if (token == Token.OpenParenthesis) {
					++level;
				} else if (token == Token.CloseParenthesis) {
					--level;
				} else if (token == Token.EOF) {
					throw new Exception(); // todo
				}

				list.Add(lexer.GetToken());
				token = lexer.PeekToken();
			}

			AssertToken(lexer.PeekToken(), Token.CloseParenthesis);
			list.Add(lexer.GetToken());

			lexer.PopState();
			return new ArrayTokenStream(pos, list.ToArray());
		}

		#region helpers

		#region traditional

		private IEnumerable<ValueExpression> ValidateDirectiveParams(IEnumerable<ValueExpression> parameters, Syntax.Directive directive)
		{
			var list = new List<ValueExpression>();

			foreach (var param in parameters) {
				if (param is BuiltinVariableExpression) {
					if (directive != Syntax.Directive.Include) {
						throw new Exception(); // todo
					}

					var variable = (BuiltinVariableExpression)param;
					if (!variable.Variable.IsAllowedInInclude()) {
						throw new Exception(); // todo
					}

				} else if (param is BinaryExpression && ((BinaryExpression)param).Operator == Operator.Concatenate) { // (implicit, traditional) concat
					ValidateDirectiveParams(((BinaryExpression)param).Expressions, directive);

				} else {
					throw new Exception(); // todo
				}

				list.Add(param);
			}

			return list.ToArray();
		}

		private ValueExpression[] ParseParameters(Lexer lexer)
		{
			lexer.PushState(Lexer.State.Traditional);
			var list = new List<ValueExpression>();

			var token = lexer.PeekToken();
			ExpressionChain currentParam = new ExpressionChain();
			while (true) {
				bool consumed = false;
				if (token == Token.EOF || token is SingleCommentToken) {
					if (currentParam.Length == 0 && list.Count > 0) { // throw, but not if there are no params at all
						throw new Exception(); // todo

					} else if (currentParam.Length > 0) {
						list.Add(currentParam.ToExpression());
					}

					currentParam = null;
					break;

				} else if (token == Token.Newline) {
					consumed = true;

					var pos = lexer.Position;
					bool concat = lexer.PeekToken() == Token.Comma; // todo: concat with a comment in between // possibly: expression queue

					if (currentParam.Length == 0 && !concat && list.Count > 0) { // throw, but not if there are no params at all
						throw new Exception(); // todo
					}

					if (!concat) {
						if (currentParam.Length > 0) {
							list.Add(currentParam.ToExpression());
							currentParam = null;
						}

						lexer.ResetToken();
						lexer.Rewind(pos);

						break;
					}

				} else if (token == Token.Comma) {
					if (currentParam.Length == 0) {
						list.Add(null); // append NULL for empty parameters
					} else {
						list.Add(currentParam.ToExpression());
					}
					currentParam = new ExpressionChain();

				} else if (token == Token.ForceExpression) {
					if (currentParam.Length > 0) {
						throw new Exception("ForceExpression must be first"); // todo
					}

					lexer.GetToken();
					consumed = true;

					currentParam.Append(
						ParseWithState(lexer, Lexer.State.Expression,
					               () => ParseExpressionChain(lexer, Token.Comma).ToExpression()
					        )
					);

				} else if (token is TraditionalStringToken) {
					// todo: ensure currentParam is not forced expression
					var str = (TraditionalStringToken)token;
					if (str.Text.Trim() == String.Empty && currentParam.Length == 0) {
						continue; // ignore leading whitespace
					}
					var expr = new StringLiteralExpression(str.Position, str.Text);
					currentParam.Append(expr);

				} else if (token is VariableToken) {
					// todo: ensure currentParam is not forced expression
					var variable = (VariableToken)token;
					var expr = GetVariable(variable.Text, variable.Position);
					currentParam.Append(expr);

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

		private ExpressionChain ParseExpressionChain(ITokenStream stream, params Token[] terminators)
		{
			var chain = new ExpressionChain();
			ParseExpressionChain(stream, chain, terminators);
			return chain;
		}

		private void ParseExpressionChain(ITokenStream stream, ExpressionChain chain, IEnumerable<Token> terminators = null)
		{
			var token = stream.PeekToken();
			UnaryOperator prefixOp = null;

			while (token != Token.EOF) {
				if (token == Token.Newline) {
					if (prefixOp != null) {
						throw new Exception(); // todo
					}

					stream.GetToken();
					token = stream.PeekToken();
					if (!(token is OperatorToken)) { // don't concat
						break;
					}
				}

				if (terminators != null && terminators.Contains(token)) {
					if (prefixOp != null) {
						throw new Exception(); // todo
					}
					break;
				}

				if (token is OperatorToken) {
					if (prefixOp != null) {
						throw new Exception(); // todo
					}

					var op = ((OperatorToken)token).Operator;
					if (op == Operator.AltObjAccess) { // special handling for f[A, B]
						ParseAltObjAccess(stream, chain);

					} else if (op is UnaryOperator) {
						prefixOp = (UnaryOperator)op;
						if (prefixOp.Position != Position.prefix) {
							throw new Exception(); // todo
						}

					} else if (op is BinaryOperator) {
						chain.Append((BinaryOperator)op);

					} else {
						throw new Exception(); // todo
					}
				} else {
					ValueExpression expr;
					if (token == Token.OpenParenthesis) {
						stream.GetToken(); // consume parenthesis
						expr = ParseExpressionChain(stream, Token.CloseParenthesis).ToExpression();
						stream.GetToken(); // consume closing parenthesis

					} else {
						expr = TokenToValueExpression(stream);
					}

					if (prefixOp != null) {
						expr = new UnaryExpression(stream.Position, prefixOp, expr);
							prefixOp = null;
					}

					token = stream.PeekToken();

					var objAcc = OperatorToken.GetToken(Operator.ObjectAccess);
					var ternary = OperatorToken.GetToken(Operator.Ternary);
					while (token == objAcc || token == ternary || IsUnaryPostfixOperator(token)) {
						if (token == objAcc) {
							expr = ParseObjectAccess(stream, expr);
						} else if (token == ternary) {
							expr = ParseTernary(stream, expr, terminators);
						} else if (IsUnaryPostfixOperator(token)) {
							stream.GetToken();
							expr = new UnaryExpression(expr.Position, ((OperatorToken)token).Operator, expr);
						}
						token = stream.PeekToken();
					}

					chain.Append(expr);
					continue;
				}

				stream.GetToken();
				token = stream.PeekToken();
			}
		}

		private static bool IsUnaryPostfixOperator(Token token)
		{
			return token is OperatorToken
				&& ((OperatorToken)token).Operator is UnaryOperator
				&& ((UnaryOperator)((OperatorToken)token).Operator).Position == Position.postfix;
		}

		private ValueExpression[] ParseExpressionList(ITokenStream stream)
		{
			return ParseExpressionList(stream, Token.OpenParenthesis, Token.CloseParenthesis);
		}

		private ValueExpression[] ParseExpressionList(ITokenStream stream, Token open, Token close)
		{
			AssertToken(stream.GetToken(), open);
			var list = ParseExpressionSequence(stream, close);

			AssertToken(stream.GetToken(), close);
			return list;
		}

		private ValueExpression[] ParseExpressionSequence(ITokenStream stream, Token abort = null)
		{
			var list = new List<ValueExpression>();
			ExpressionChain currentExpr = null;

			var token = stream.PeekToken();
			while (true) {
				if (token == Token.EOF) {
					// todo: if currentExpr == null -> allow empty? or fail? (param to define?) (to allow empty, must add Expression.Empty parameter)
					list.Add(currentExpr.ToExpression());
					break;

				} else if (token == Token.Comma) {
					// todo: if currentExpr == null -> allow empty? or fail? (param to define?) (to allow empty, must add Expression.Empty parameter)
					list.Add(currentExpr.ToExpression());
					currentExpr = null;

				} else if (abort != null && token == abort) {
					// todo: if currentExpr == null -> allow empty? or fail? (to allow empty, just ignore) (if list.Count == 0 -> don't fail)
					if (currentExpr != null) {
						list.Add(currentExpr.ToExpression());
					}
					break;

				} else {
					if (currentExpr != null) {
						if (token == Token.Newline) {
							break;
						}
						throw new Exception(token.ToString());
					}
					currentExpr = ParseExpressionChain(stream, Token.Comma, abort);
					token = stream.PeekToken();
					continue;
				}

				stream.GetToken();
				token = stream.PeekToken();
			}

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
		private ValueExpression TokenToValueExpression(ITokenStream stream)
		{
			var token = stream.PeekToken();
			if (token is FunctionToken) {
				return ParseFunctionCall(stream);

			} else if (token is IdToken) {
				var id = (IdToken)stream.GetToken();
				return GetVariable(id.Text, id.Position);
			
			} else if (token is ValueKeywordToken) {
				var value = (ValueKeywordToken)stream.GetToken();
				return new ValueKeywordExpression(stream.Position, value.Keyword);

			} else if (token is QuotedStringToken) {
				var str = (QuotedStringToken)stream.GetToken();
				return new StringLiteralExpression(str.Position, str.Text);

			} else if (token is NumberToken) {
				var number = (NumberToken)stream.GetToken();
				return new NumberLiteralExpression(number.Position, number.Text, number.Type);
			
			} else if (token == Token.OpenBracket) {
				var arr = ParseExpressionList(stream, Token.OpenBracket, Token.CloseBracket);
				return new ArrayLiteralExpression(stream.Position, arr);
			
			} else if (token == Token.OpenBrace) {
				var obj = ParseObjectLiteral(stream);
				return new ObjectLiteralExpression(stream.Position, obj);
			}

			throw new Exception(token.ToString()); // todo
		}

		private TernaryExpression ParseTernary(ITokenStream stream, ValueExpression cond, IEnumerable<Token> terminators)
		{
			AssertToken(stream.GetToken(), OperatorToken.GetToken(Operator.Ternary));
			var ifTrue  = ParseExpressionChain(stream, Token.Colon);

			AssertToken(stream.GetToken(), Token.Colon);
			var ifFalse = ParseExpressionChain(stream, terminators != null ? terminators.ToArray() : null);

			return new TernaryExpression(cond.Position,
			                             cond,
			                             ifTrue.ToExpression(),
			                             ifFalse.ToExpression());
		}

		private MemberExpression ParseObjectAccess(ITokenStream stream, ValueExpression obj)
		{
			AssertToken(stream.GetToken(), OperatorToken.GetToken(Operator.ObjectAccess));
			AssertToken(stream.PeekToken(), typeof(TextToken));

			AssertToken(stream.PeekToken(), typeof(TextToken));
			var token = (TextToken)stream.GetToken();

			if (token is IdToken) {
				return new MemberAccessExpression(obj.Position, obj, token.Text);
			} else if (token is FunctionToken) {
				return new MemberInvokeExpression(obj.Position, obj, token.Text);
			}

			throw new Exception(); // todo
		}

		private void ParseAltObjAccess(ITokenStream stream, ExpressionChain chain)
		{
			AssertToken(stream.GetToken(), OperatorToken.GetToken(Operator.AltObjAccess));
			chain.Append((BinaryOperator)Operator.AltObjAccess);

			var token = stream.PeekToken();
			ValueExpression currentParam = null;
			while (token != Token.CloseBracket) {
				// todo
				if (token == Token.Comma) {
					stream.GetToken();
					if (currentParam == null) {
						// todo: empty expression? fail?
					}
					chain.Append(currentParam);
					chain.Append((BinaryOperator)Operator.AltObjAccess);
					currentParam = null;

				} else {
					if (currentParam != null) {
						throw new Exception(); // todo
					}
					currentParam = ParseExpressionChain(stream, Token.Comma, Token.CloseBracket).ToExpression();
				}
				token = stream.PeekToken();
			}
			if (currentParam == null) {
				// todo (see above for comma)
			}
			chain.Append(currentParam);
		}

		private IDictionary<ValueExpression, ValueExpression> ParseObjectLiteral(ITokenStream stream)
		{
			AssertToken(stream.GetToken(), Token.OpenBrace);

			var dict = new Dictionary<ValueExpression, ValueExpression>();

			var token = stream.PeekToken();
			while (token != Token.CloseBrace) {
				var key = ParseExpressionChain(stream, Token.Colon).ToExpression();
				AssertToken(stream.GetToken(), Token.Colon);
				var value = ParseExpressionChain(stream, Token.Comma, Token.CloseBrace).ToExpression();

				dict[key] = value;
				if (stream.PeekToken() == Token.Comma) {
					stream.GetToken();
				}
				token = stream.PeekToken();
			}
			AssertToken(stream.GetToken(), Token.CloseBrace);

			return dict;
		}

		#endregion

		#region definitions

		#region function definitions

		private ParameterDefinitionExpression[] ParseParamDefinitions(ITokenStream stream)
		{
			var list = new List<ParameterDefinitionExpression>();
			AssertToken(stream.GetToken(), Token.OpenParenthesis);

			while (stream.PeekToken() != Token.CloseParenthesis) {
				IdToken nameToken = null, modifierToken = null;
				ValueExpression value = null;

				AssertToken(stream.PeekToken(), typeof(IdToken));
				nameToken = (IdToken)stream.GetToken();

				if (stream.PeekToken() is IdToken) { // first was actually a modifier
					if (!IsValidParamModifier(nameToken)) {
						throw new Exception(); // todo
					}
					modifierToken = nameToken;
					nameToken = (IdToken)stream.GetToken();
				}

				if (stream.PeekToken() is OperatorToken) { // default value specified
					stream.GetToken();
					value = ParseExpressionChain(stream, Token.Comma, Token.CloseParenthesis).ToExpression(); // todo: what's actually allowed as default value?
				}

				AssertToken(stream.PeekToken(), Token.Comma, Token.CloseParenthesis);
				if (stream.PeekToken() == Token.Comma) {
					stream.GetToken();
				}

				Syntax.ParameterModifier modifier = modifierToken != null ? Syntax.GetParameterModifier(modifierToken.Text) : Syntax.ParameterModifier.None;
				list.Add(
					new ParameterDefinitionExpression((modifierToken ?? nameToken).Position,
				                                  nameToken.Text,
				                                  modifier,
				                                  value
				        )
				);

			}

			AssertToken(stream.GetToken(), Token.CloseParenthesis);
			return list.ToArray();
		}

		private bool IsValidParamModifier(IdToken token)
		{
			return Syntax.IsParameterModifier(token.Text);
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

		private Expression[] ParseBlock(Lexer lexer, Action<Expression> validate)
		{
			AssertToken(lexer.GetToken(), Token.OpenBrace);
			AssertToken(lexer.GetToken(), Token.Newline); // todo: is newline enforced?

			var body = new List<Expression>();
			lexer.PushState(Lexer.State.Root);

			var token = lexer.PeekToken();
			while (token != Token.CloseBrace) {
				if (token == Token.EOF) {
					throw new UnexpectedEOFException(lexer.Position);

				} else if (token == Token.Newline) {
					lexer.GetToken(); // consume newline
					token = lexer.PeekToken();
					continue;
				}

				if (token is CommentToken) {
					lexer.GetToken(); // consume token
				} else {
					var expr = ParseExpression(lexer); // consumes tokens
					validate(expr);
					body.Add(expr);
				}

				token = lexer.PeekToken();
			}
			lexer.GetToken(); // swallow the closing brace

			lexer.PopState();
			return body.ToArray();
		}

		private void ValidateExpressionInDefinition(Expression expr)
		{
			if (expr is DirectiveExpression) {
				throw new Exception(); // todo
			}
			// this currently allows nested functions and classes - must be properly handled or disallowed
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

		private TResult ParseWithState<TResult>(Lexer lexer, Lexer.State state, Func<TResult> fn)
		{
			lexer.PushState(state);
			var expr = fn();
			lexer.PopState();
			return expr;
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
			if (!actual.GetType().TypeIs(expected)) {
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

		private void SkipNewlinesAndComments(Lexer lexer)
		{
			var token = lexer.PeekToken();
			while (token == Token.Newline || token is CommentToken) {
				lexer.GetToken();
				token = lexer.PeekToken();
			}
		}

		#endregion
	}

	public class UnexpectedTokenException : Exception
	{
		public UnexpectedTokenException(string msg) : base(msg) { }
	}
}