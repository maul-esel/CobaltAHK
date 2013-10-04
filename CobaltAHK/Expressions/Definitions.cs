using System.Collections.Generic;

namespace CobaltAHK.Expressions
{
	public class FunctionDefinitionExpression : Expression
	{
		public FunctionDefinitionExpression(SourcePosition pos, string funcName, IEnumerable<ParameterDefinitionExpression> prms, IEnumerable<Expression> funcBody)
		: base(pos)
		{
			name = funcName;
			parameters = prms;
			body = funcBody;
		}

		private readonly string name;

		public string Name { get { return name; } }

		private readonly IEnumerable<ParameterDefinitionExpression> parameters;

		public IEnumerable<ParameterDefinitionExpression> Parameters { get { return parameters; } }

		private readonly IEnumerable<Expression> body;

		public IEnumerable<Expression> Body { get { return body; } }
	}

	public class ClassDefinitionExpression : Expression
	{
		public ClassDefinitionExpression(SourcePosition pos, string name, IEnumerable<FunctionDefinitionExpression> methods) : base(pos) { } // todo: how to set fields? or a ruby-like approach (any expression?)
	}

	public class ParameterDefinitionExpression : Expression
	{
		public ParameterDefinitionExpression(SourcePosition pos, string paramName, Syntax.ParameterModifier mod, ValueExpression defValue)
		: base(pos)
		{
			name = paramName;
			modifier = mod;
			defaultValue = defValue;
		}

		private readonly string name;

		public string Name { get { return name; } }

		private readonly Syntax.ParameterModifier modifier;

		public Syntax.ParameterModifier Modifier { get { return modifier; } }

		private readonly ValueExpression defaultValue;

		public ValueExpression DefaultValue { get { return defaultValue; } }
	}
}
