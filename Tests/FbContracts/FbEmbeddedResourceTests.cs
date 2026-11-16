using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Xml.Serialization;

using FB;
using FB.Persist;
using FB.VisualFB;

using FluentAssertions;

using NtoLib.ConfigLoader;

using Xunit;

namespace Tests.FbContracts;

[Trait("Category", "Unit")]
[Trait("Component", "FbContracts")]
[Trait("Area", "EmbeddedResources")]
public sealed class FbEmbeddedResourceTests
{
	private const int PaletteIconSize = 16;

	private static readonly Assembly _library = typeof(ConfigLoaderFB).Assembly;

	public static TheoryData<string> FbTypeNames()
	{
		var data = new TheoryData<string>();
		foreach (var type in FindFbTypes())
		{
			data.Add(type.FullName!);
		}

		return data;
	}

	[Fact]
	public void Library_ContainsFbTypes()
	{
		FindFbTypes().Should().Contain(typeof(ConfigLoaderFB));
	}

	// FB.Services.FbInfo.FbInfoService.GetResourceImage throws when the icon is missing,
	// and MasterSCADA then drops the block from the palette without an error dialog.
	[Theory]
	[MemberData(nameof(FbTypeNames))]
	public void Fb_HasEmbeddedPaletteIcon(string fbTypeName)
	{
		_library.GetManifestResourceNames().Should().Contain($"{fbTypeName}.bmp");
	}

	[Theory]
	[MemberData(nameof(FbTypeNames))]
	public void Fb_PaletteIconIsSixteenColorSixteenPixelBitmap(string fbTypeName)
	{
		using var stream = _library.GetManifestResourceStream($"{fbTypeName}.bmp");
		stream.Should().NotBeNull();

		using var icon = Image.FromStream(stream!);

		icon.Width.Should().Be(PaletteIconSize);
		icon.Height.Should().Be(PaletteIconSize);
		icon.PixelFormat.Should().Be(PixelFormat.Format4bppIndexed);
	}

	[Theory]
	[MemberData(nameof(FbTypeNames))]
	public void Fb_HasEmbeddedPinMap(string fbTypeName)
	{
		_library.GetManifestResourceNames().Should().Contain($"{fbTypeName}.xml");
	}

	// FB.Persist.FBsConfigCache reads the pin map with the same serializer when the block is added.
	[Theory]
	[MemberData(nameof(FbTypeNames))]
	public void Fb_PinMapDeserializesWithUniquePinIds(string fbTypeName)
	{
		using var stream = _library.GetManifestResourceStream($"{fbTypeName}.xml");
		stream.Should().NotBeNull();

		var config = (FBConfig)new XmlSerializer(typeof(FBConfig)).Deserialize(stream!);

		var pinIds = PinsOf(config.Map).Concat(PinsOf(config.VisualMap)).Select(pin => pin.ID).ToList();
		pinIds.Should().OnlyHaveUniqueItems();
	}

	private static IEnumerable<Type> FindFbTypes()
	{
		return _library.GetExportedTypes()
			.Where(t => !t.IsAbstract)
			.Where(t => typeof(StaticFBBase).IsAssignableFrom(t) || typeof(VisualFBBase).IsAssignableFrom(t));
	}

	private static IEnumerable<PersistItemBase> PinsOf(PersistGroup? group)
	{
		if (group?.PersistItems is null)
		{
			yield break;
		}

		foreach (var item in group.PersistItems)
		{
			if (item is PersistGroup nested)
			{
				foreach (var pin in PinsOf(nested))
				{
					yield return pin;
				}
			}
			else
			{
				yield return item;
			}
		}
	}
}
