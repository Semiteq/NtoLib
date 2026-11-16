using System.Reflection;

using FB;
using FB.VisualFB;

using NtoLib.ConfigLoader;

using Xunit;

namespace Tests.FbContracts;

internal static class FbTypeCatalog
{
	public static Assembly Library { get; } = typeof(ConfigLoaderFB).Assembly;

	public static IEnumerable<Type> ConcreteFbTypes()
	{
		return Library.GetExportedTypes()
			.Where(t => !t.IsAbstract)
			.Where(t => typeof(StaticFBBase).IsAssignableFrom(t) || typeof(VisualFBBase).IsAssignableFrom(t));
	}

	public static TheoryData<string> ConcreteFbTypeNames()
	{
		var data = new TheoryData<string>();
		foreach (var type in ConcreteFbTypes())
		{
			data.Add(type.FullName!);
		}

		return data;
	}

	public static Type Resolve(string fullName)
	{
		return Library.GetType(fullName, throwOnError: true)!;
	}
}
