using System.Drawing;

using FluentAssertions;

using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

using Xunit;

namespace Tests.MbeTable.Presentation;

public sealed class ColorStyleHelpersDefaultedTintTests
{
	private static readonly ColorScheme _scheme = ColorScheme.Default;

	[Fact]
	public void ApplyDefaultedTint_WhenNotMarked_ReturnsBaseColorUnchanged()
	{
		var baseColor = Color.FromArgb(0x20, 0x40, 0x60);

		var result = ColorStyleHelpers.ApplyDefaultedTint(baseColor, isMarked: false, isRestricted: false, _scheme);

		result.Should().Be(baseColor);
	}

	[Fact]
	public void ApplyDefaultedTint_WhenMarked_BlendsTowardOrangeAtConfiguredWeight()
	{
		var baseColor = Color.FromArgb(0x00, 0x00, 0x00);
		var expected = ColorStyleHelpers.Blend(baseColor, _scheme.DefaultedCellBgColor, _scheme.DefaultedCellTintWeight);

		var result = ColorStyleHelpers.ApplyDefaultedTint(baseColor, isMarked: true, isRestricted: false, _scheme);

		result.Should().Be(expected);
	}

	[Fact]
	public void ApplyDefaultedTint_WhenMarked_MovesColorTowardOrange()
	{
		var baseColor = Color.FromArgb(0x00, 0x00, 0x00);

		var result = ColorStyleHelpers.ApplyDefaultedTint(baseColor, isMarked: true, isRestricted: false, _scheme);

		result.R.Should().BeGreaterThan(baseColor.R);
		result.G.Should().BeGreaterThan(baseColor.G);
		result.Should().NotBe(baseColor);
	}

	[Fact]
	public void ApplyDefaultedTint_WhenRestricted_AttenuatesWeightLikeExecutionTint()
	{
		var baseColor = Color.FromArgb(0x00, 0x00, 0x00);
		var expectedAttenuated =
			ColorStyleHelpers.Blend(baseColor, _scheme.DefaultedCellBgColor, _scheme.DefaultedCellTintWeight * 0.6f);

		var restricted = ColorStyleHelpers.ApplyDefaultedTint(baseColor, isMarked: true, isRestricted: true, _scheme);
		var full = ColorStyleHelpers.ApplyDefaultedTint(baseColor, isMarked: true, isRestricted: false, _scheme);

		restricted.Should().Be(expectedAttenuated);
		restricted.R.Should().BeLessThan(full.R);
	}

	[Fact]
	public void ApplyDefaultedTint_WhenRestrictedButNotMarked_StillReturnsBaseColor()
	{
		var baseColor = Color.FromArgb(0x10, 0x20, 0x30);

		var result = ColorStyleHelpers.ApplyDefaultedTint(baseColor, isMarked: false, isRestricted: true, _scheme);

		result.Should().Be(baseColor);
	}
}
