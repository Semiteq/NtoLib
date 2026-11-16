using System.Drawing;
using System.Drawing.Imaging;
using System.Xml.Serialization;

using FB.Persist;

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

	public static TheoryData<string> FbTypeNames()
	{
		return FbTypeCatalog.ConcreteFbTypeNames();
	}

	[Fact]
	public void Library_ContainsFbTypes()
	{
		FbTypeCatalog.ConcreteFbTypes().Should().Contain(typeof(ConfigLoaderFB));
	}

	// FB.Services.FbInfo.FbInfoService.GetResourceImage throws when the icon is missing,
	// and MasterSCADA then drops the block from the palette without an error dialog.
	[Theory]
	[MemberData(nameof(FbTypeNames))]
	public void Fb_HasEmbeddedPaletteIcon(string fbTypeName)
	{
		FbTypeCatalog.Library.GetManifestResourceNames().Should().Contain($"{fbTypeName}.bmp");
	}

	[Theory]
	[MemberData(nameof(FbTypeNames))]
	public void Fb_PaletteIconIsSixteenColorSixteenPixelBitmap(string fbTypeName)
	{
		using var stream = FbTypeCatalog.Library.GetManifestResourceStream($"{fbTypeName}.bmp");
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
		FbTypeCatalog.Library.GetManifestResourceNames().Should().Contain($"{fbTypeName}.xml");
	}

	// FB.Persist.FBsConfigCache reads the pin map with the same serializer when the block is added.
	[Theory]
	[MemberData(nameof(FbTypeNames))]
	public void Fb_PinMapDeserializesWithUniquePinIds(string fbTypeName)
	{
		using var stream = FbTypeCatalog.Library.GetManifestResourceStream($"{fbTypeName}.xml");
		stream.Should().NotBeNull();

		var config = (FBConfig)new XmlSerializer(typeof(FBConfig)).Deserialize(stream!);

		var pinIds = PinsOf(config.Map).Concat(PinsOf(config.VisualMap)).Select(pin => pin.ID).ToList();
		pinIds.Should().OnlyHaveUniqueItems();
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
