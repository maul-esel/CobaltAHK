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

		[System.Diagnostics.Conditional("DEBUG")]
		internal void Debug(string str, params object[] placeholders)
		{
			Console.WriteLine(str, placeholders);
		}

		public void Execute(TextReader code)
		{
			var expressions = parser.Parse(code);
			Debug("Parsed {0} expressions.", expressions.Length);

			var scope = new ExpressionTree.Scope();
			var settings = new ScriptSettings();

			ExpressionTree.Preprocessor.Process(expressions, scope, settings);
			var generator = new ExpressionTree.Generator(settings);

			var et = new List<Expression>();
			foreach (var e in expressions) {
				et.Add(generator.Generate(e, scope));
			}
			Debug("Generated {0} expressions.", et.Count);

			var lambda = Expression.Lambda<Action>(Expression.Block(scope.GetVariables(), et));
			Debug("Generated lambda wrapper.");
			var exec = lambda.Compile();
			Debug("Compiled lambda wrapper. Begin execution...");
			exec();
		}

		private Parser parser = new Parser();
	}
}

