using FluentAssertions;

using NtoLib.ConfigLoader.Entities;
using NtoLib.Test.ConfigLoader.Helpers;

using Xunit;

namespace NtoLib.Test.ConfigLoader.Integration;

[Trait("Category", "Integration")]
[Trait("Component", "ConfigLoader")]
[Trait("Area", "Validation")]
public sealed class ValidationTests
{
	[Fact]
	public void Save_NameExceedsMaxLength_ReturnsError()
	{
		using var tempDir = ConfigLoaderTestHelper.CreateEmptyTempDirectory();
		var service = ConfigLoaderTestHelper.CreateService();
		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		var shutters = CreateEmptyArray(16);
		shutters[0] = new string('A', 256);

		var dto = new LoaderDto(
			shutters,
			CreateEmptyArray(32),
			CreateEmptyArray(16),
			CreateEmptyArray(16));

		var result = service.Save(filePath, dto);

		result.IsFailed.Should().BeTrue();
		service.LastError.Should().Contain("maximum length");
	}

	[Fact]
	public void Save_NameWithSpecialSymbols_ReturnsError()
	{
		using var tempDir = ConfigLoaderTestHelper.CreateEmptyTempDirectory();
		var service = ConfigLoaderTestHelper.CreateService();
		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		var shutters = CreateEmptyArray(16);
		shutters[0] = "Name@With#Symbols";

		var dto = new LoaderDto(
			shutters,
			CreateEmptyArray(32),
			CreateEmptyArray(16),
			CreateEmptyArray(16));

		var result = service.Save(filePath, dto);

		result.IsFailed.Should().BeTrue();
		service.LastError.Should().Contain("invalid characters");
	}

	[Theory]
	[InlineData("ValidName")]
	[InlineData("Valid Name")]
	[InlineData("Valid. Name")]
	[InlineData("Valid-Name")]
	[InlineData("Valid_Name")]
	[InlineData("Valid123")]
	[InlineData("")]
	public void Save_ValidNameFormats_Succeeds(string name)
	{
		using var tempDir = ConfigLoaderTestHelper.CreateEmptyTempDirectory();
		var service = ConfigLoaderTestHelper.CreateService();
		var filePath = ConfigLoaderTestHelper.GetConfigFilePath(tempDir);

		var shutters = CreateEmptyArray(16);
		shutters[0] = name;

		var dto = new LoaderDto(
			shutters,
			CreateEmptyArray(32),
			CreateEmptyArray(16),
			CreateEmptyArray(16));

		var result = service.Save(filePath, dto);

		result.IsSuccess.Should().BeTrue();
	}

	private static string[] CreateEmptyArray(int size)
	{
		var array = new string[size];
		for (var i = 0; i < size; i++)
		{
			array[i] = string.Empty;
		}

		return array;
	}
}
