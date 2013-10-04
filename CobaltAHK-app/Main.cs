using System;
using System.IO;

namespace CobaltAHK
{
	class Program
	{
		public static void Main(string[] args)
		{
			CobaltAHK engine = new CobaltAHK();
			engine.Execute(File.ReadAllText(args[0])); // todo
		}
	}
}
