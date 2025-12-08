using FluentAssertions;

using Tests.MbeTable.Application.Clipboard.Helpers;

using Xunit;

namespace Tests.MbeTable.Application.Clipboard.Integration;

[Trait("Category", "Integration")]
[Trait("Component", "Application")]
[Trait("Area", "ClipboardDelete")]
public sealed class ClipboardDeleteTests
{
	[Fact]
	public async Task DeleteSingleRow_RemovesStep()
	{
		var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
		using var _ = services as IDisposable;

		var d = new ClipboardTestDriver(app);
		d.AddWait(0).AddWait(1).AddWait(2);

		var beforeCount = app.GetRowCount();
		var result = await app.DeleteRowsAsync(new List<int> { 1 });

		result.IsSuccess.Should().BeTrue();
		app.GetRowCount().Should().Be(beforeCount - 1);
	}

	[Fact]
	public async Task DeleteMultipleNonContiguous_RemovesAll()
	{
		var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
		using var _ = services as IDisposable;

		var d = new ClipboardTestDriver(app);
		d.AddWait(0).AddWait(1).AddWait(2).AddWait(3).AddWait(4);

		var result = await app.DeleteRowsAsync(new List<int> { 1, 3 });

		result.IsSuccess.Should().BeTrue();
		app.GetRowCount().Should().Be(3);
	}

	[Fact]
	public async Task DeleteWithOutOfRangeIndices_IgnoresInvalid()
	{
		var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
		using var _ = services as IDisposable;

		var d = new ClipboardTestDriver(app);
		d.AddWait(0).AddWait(1).AddWait(2);

		var result = await app.DeleteRowsAsync(new List<int> { 1, 10 });

		result.IsSuccess.Should().BeTrue();
		app.GetRowCount().Should().Be(2);
	}

	[Fact]
	public async Task DeleteEmptySelection_NoChange()
	{
		var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
		using var _ = services as IDisposable;

		var d = new ClipboardTestDriver(app);
		d.AddWait(0).AddWait(1);

		var beforeCount = app.GetRowCount();
		var result = await app.DeleteRowsAsync(new List<int>());

		result.IsSuccess.Should().BeTrue();
		app.GetRowCount().Should().Be(beforeCount);
	}
}
