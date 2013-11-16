using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace CobaltAHK.CLRInterop
{
	internal abstract class CLRWrapper : DynamicObject
	{
		protected CLRWrapper(string n)
		{
			name = n;
		}

		protected readonly string name;

		protected string FullName(string nested)
		{
			return name + "." + nested;
		}

		internal static bool TryFindType(string name, out Type type)
		{
			type = assemblies.Select(
				a => a.GetExportedTypes()
			).Aggregate(
				(x, y) => x.Concat(y).ToArray()
			).FirstOrDefault(t => t.FullName == name);

			return type != null;
		}

		internal static bool NamespaceExists(string name)
		{
			return assemblies.Any(
				a => a.GetTypes().Any(
					t => t.Namespace == name
				)
			);
		}

		internal static void AddAssembly(Assembly assembly)
		{
			assemblies.Add(assembly);
		}

		private static readonly HashSet<Assembly> assemblies = new HashSet<Assembly>();
	}
}