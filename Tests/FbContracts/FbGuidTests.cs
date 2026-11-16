using System.Reflection;
using System.Runtime.InteropServices;

using FluentAssertions;

using Xunit;

namespace Tests.FbContracts;

[Trait("Category", "Unit")]
[Trait("Component", "FbContracts")]
[Trait("Area", "Guid")]
public sealed class FbGuidTests
{
	// Saved MasterSCADA projects find each placed block and control by this GUID and type name.
	// Changing or removing a line breaks every project that uses the block; a new block adds one.
	private static readonly Dictionary<Guid, string> _releasedComIdentities = new()
	{
		[new Guid("6C051A25-C703-4ADD-9022-5D306B0C236A")] = "NtoLib.ClockGenerator.ClockGeneratorFB",
		[new Guid("01187EA6-1B07-431B-9F4F-B7205C8246C9")] = "NtoLib.ConfigLoader.ConfigLoaderFB",
		[new Guid("0B9D612A-E69E-4817-AA0A-878186938C3F")] = "NtoLib.Devices.Pumps.PumpFB",
		[new Guid("664141D1-44B3-43D7-9897-3A10C936315A")] = "NtoLib.Devices.Pumps.PumpControl",
		[new Guid("0B747EAD-4E9B-47CE-99AA-12BF8F5192A4")] = "NtoLib.Devices.Valves.ValveFB",
		[new Guid("4641CB00-693D-46F2-85EC-5142B0C0EDA4")] = "NtoLib.Devices.Valves.ValveControl",
		[new Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890")] = "NtoLib.LinkSwitcher.LinkSwitcherFB",
		[new Guid("2E7D90D7-D24A-4A23-AD06-D797F1721E79")] = "NtoLib.NumericBox.NumericBoxFB",
		[new Guid("E44A8182-8463-4D07-977D-FE7C993F76CD")] = "NtoLib.NumericBox.NumericBoxControl",
		[new Guid("3F7A9C2E-B841-4D56-A0F3-8C1E2D5B7094")] = "NtoLib.OpcTreeManager.OpcTreeManagerFB",
		[new Guid("DFB05172-07CD-492C-925E-A091B197D8A8")] = "NtoLib.Recipes.MbeTable.MbeTableFB",
		[new Guid("8161DF32-8D80-4B81-AF52-3021AE0AD293")] = "NtoLib.Recipes.MbeTable.TableControl",
		[new Guid("B9FC0D44-3827-4D86-9CD8-0F0965DCED47")] = "NtoLib.Recipes.MbeTableEditor.MbeTableEditorFB",
		[new Guid("55E77764-CDD5-44DB-B914-80A44776C165")] = "NtoLib.Recipes.MbeTableEditor.MbeTableEditorControl",
		[new Guid("F191FFCB-A4F9-4A50-9922-9E77EA7C4C46")] = "NtoLib.Spectrometer.SpectrometerFB",
		[new Guid("C92B2C17-3FCD-4D1A-950D-AC25DE65D8D4")] = "NtoLib.TrendPensManager.TrendPensManagerFB",
	};

	private static readonly string[] _vendorAssemblyNames =
	[
		"FB", "InSAT.Library", "MasterSCADA.Common", "MasterSCADA.Trend", "MasterSCADALib", "OpcUaClient",
		"Opc.Ua.Core",
	];

	[Fact]
	public void ComIdentities_MatchReleasedList()
	{
		var actual = TypesWithGuid(FbTypeCatalog.Library)
			.ToDictionary(t => GuidOf(t), t => t.FullName!);

		actual.Should().Equal(_releasedComIdentities);
	}

	// ObjectFactoryService keys every scanned type by [Guid]; a duplicate silently replaces the
	// earlier type, and that block disappears from the palette.
	[Fact]
	public void LibraryGuids_AreUniqueWithinLibrary()
	{
		TypesWithGuid(FbTypeCatalog.Library).Select(GuidOf).Should().OnlyHaveUniqueItems();
	}

	[Fact]
	public void LibraryGuids_DoNotCollideWithVendorAssemblies()
	{
		var vendorTypesByGuid = VendorAssemblies()
			.SelectMany(TypesWithGuid)
			.ToLookup(GuidOf);

		var collisions = TypesWithGuid(FbTypeCatalog.Library)
			.Where(t => vendorTypesByGuid.Contains(GuidOf(t)))
			.Select(t => $"{t.FullName} = {vendorTypesByGuid[GuidOf(t)].First().AssemblyQualifiedName}");

		collisions.Should().BeEmpty();
	}

	private static IEnumerable<Assembly> VendorAssemblies()
	{
		return _vendorAssemblyNames.Select(Assembly.Load);
	}

	private static IEnumerable<Type> TypesWithGuid(Assembly assembly)
	{
		return LoadableTypes(assembly).Where(t => t.IsDefined(typeof(GuidAttribute), false));
	}

	private static IEnumerable<Type> LoadableTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			return ex.Types.Where(t => t is not null)!;
		}
	}

	private static Guid GuidOf(Type type)
	{
		var attribute = (GuidAttribute)Attribute.GetCustomAttribute(type, typeof(GuidAttribute), false)!;
		return new Guid(attribute.Value);
	}
}
