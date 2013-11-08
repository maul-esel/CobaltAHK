using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using CobaltAHK.ExpressionTree;

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
			var exec = Compile(code);
			Debug("Compiled lambda wrapper. Begin execution...");

			exec();
		}

		public Action Compile(TextReader code)
		{
			var scope = new ExpressionTree.Scope();
			var settings = new ScriptSettings();

			var expressions = parser.Parse(code);
			Debug("Parsed {0} expressions.", expressions.Length);

			Preprocess(expressions, scope, settings);
			Debug("Preprocessing returned {0} expressions.", expressions.Length);

			var et = Generate(expressions, scope, settings);
			Debug("Generated {0} expressions.", et.Length);

			var lambda = Expression.Lambda<Action>(Expression.Block(scope.GetVariables(), et));
			Debug("Generated lambda wrapper.");

			return lambda.Compile();
		}

		private Parser parser = new Parser();

		private void Preprocess(IList<Expressions.Expression> exprs, Scope scope, ScriptSettings settings)
		{
			Preprocessor.Process(exprs, scope, settings);
		}

		private Expression[] Generate(IEnumerable<Expressions.Expression> exprs, Scope scope, ScriptSettings settings)
		{
			var generator = new Generator(settings);
			return exprs.Select(e => generator.Generate(e, scope)).ToArray();
		}

		[System.Diagnostics.Conditional("DEBUG")]
		internal void Debug(string str, params object[] placeholders)
		{
			Console.WriteLine(str, placeholders);
		}
	}
}

