using System.ComponentModel;
using System.Runtime.InteropServices;

using FB;

using FluentAssertions;

using InSAT.Library.Interop;

using Xunit;

namespace Tests.FbContracts;

// MasterSCADA.Services.ObjectFactory.ObjectFactoryService lists a type as an FB when it derives
// from FBDesignBase and carries [Guid]; FbInfoService then filters the palette by [CatID] and
// takes the shown name from [DisplayName].
[Trait("Category", "Unit")]
[Trait("Component", "FbContracts")]
[Trait("Area", "ClassContract")]
public sealed class FbClassContractTests
{
	public static TheoryData<string> FbTypeNames()
	{
		return FbTypeCatalog.ConcreteFbTypeNames();
	}

	[Theory]
	[MemberData(nameof(FbTypeNames))]
	public void Fb_DerivesFromFbDesignBase(string fbTypeName)
	{
		FbTypeCatalog.Resolve(fbTypeName).IsSubclassOf(typeof(FBDesignBase)).Should().BeTrue();
	}

	[Theory]
	[MemberData(nameof(FbTypeNames))]
	public void Fb_HasGuid(string fbTypeName)
	{
		FbTypeCatalog.Resolve(fbTypeName).GetCustomAttributes(typeof(GuidAttribute), false).Should().ContainSingle();
	}

	[Theory]
	[MemberData(nameof(FbTypeNames))]
	public void Fb_HasPaletteCategory(string fbTypeName)
	{
		FbTypeCatalog.Resolve(fbTypeName).GetCustomAttributes(typeof(CatIDAttribute), false).Should().NotBeEmpty();
	}

	[Theory]
	[MemberData(nameof(FbTypeNames))]
	public void Fb_HasDisplayName(string fbTypeName)
	{
		var displayName = (DisplayNameAttribute?)Attribute.GetCustomAttribute(
			FbTypeCatalog.Resolve(fbTypeName),
			typeof(DisplayNameAttribute),
			false);

		displayName.Should().NotBeNull();
		displayName!.DisplayName.Should().NotBeNullOrWhiteSpace();
	}

	[Fact]
	public void AbstractFbBase_HasNoGuid()
	{
		var abstractFbTypesWithGuid = FbTypeCatalog.Library.GetTypes()
			.Where(t => t.IsAbstract && t.IsSubclassOf(typeof(FBDesignBase)))
			.Where(t => t.IsDefined(typeof(GuidAttribute), false))
			.Select(t => t.FullName);

		abstractFbTypesWithGuid.Should().BeEmpty();
	}
}
