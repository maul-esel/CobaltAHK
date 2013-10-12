using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;

namespace CobaltAHK
{
	public class CobaltAHK
	{
		public void Execute(string code)
		{
			Execute(new StringReader(code));
		}

		public void Execute(TextReader code)
		{
			var expressions = parser.Parse(code);
#if DEBUG
			Console.WriteLine(String.Format("{0} expressions", expressions.Length));
			foreach (var e in expressions) {
				Console.WriteLine("\t" + e.ToString());
			}
#endif
			var scope = new ExpressionTree.Scope();
			var settings = new ScriptSettings();

			ExpressionTree.Preprocessor.Process(expressions, scope, settings);
			var generator = new ExpressionTree.Generator(settings);

			var et = new List<Expression>();
			foreach (var e in expressions) {
				et.Add(generator.Generate(e, scope));
			}

			var lambda = Expression.Lambda<Action>(Expression.Block(scope.GetVariables(), et));
			var exec = lambda.Compile();
			exec();
		}

		private Parser parser = new Parser();
	}
}

