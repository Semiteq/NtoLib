using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FluentAssertions;

using NtoLib.Test.MbeTable.Application.Clipboard.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Application.Clipboard.Integration;

[Trait("Category", "Integration")]
[Trait("Component", "Application")]
[Trait("Area", "ClipboardCut")]
public sealed class ClipboardCutTests
{
	[Fact]
	public async Task CutSingleRow_WritesAndDeletes()
	{
		var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
		using var _ = services as IDisposable;

		var d = new ClipboardTestDriver(app);
		d.AddWait(0).AddWait(1).AddWait(2);

		var beforeCount = app.GetRowCount();
		var result = await app.CutRowsAsync(new List<int> { 1 });

		result.IsSuccess.Should().BeTrue();
		clipboard.WrittenText.Should().NotBeNullOrWhiteSpace();
		app.GetRowCount().Should().Be(beforeCount - 1);
	}

	[Fact]
	public async Task CutMultipleRows_DeletesAll()
	{
		var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
		using var _ = services as IDisposable;

		var d = new ClipboardTestDriver(app);
		d.AddWait(0).AddWait(1).AddWait(2).AddWait(3).AddWait(4);

		var result = await app.CutRowsAsync(new List<int> { 0, 2, 4 });

		result.IsSuccess.Should().BeTrue();
		clipboard.WrittenText.Split('\n').Should().HaveCount(3);
		app.GetRowCount().Should().Be(2);
	}

	[Fact]
	public async Task CutAllRows_EmptiesRecipe()
	{
		var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
		using var _ = services as IDisposable;

		var d = new ClipboardTestDriver(app);
		d.AddWait(0).AddWait(1).AddWait(2);

		var result = await app.CutRowsAsync(new List<int> { 0, 1, 2 });

		result.IsSuccess.Should().BeTrue();
		clipboard.WrittenText.Split('\n').Should().HaveCount(3);
		app.GetRowCount().Should().Be(0);
	}

	[Fact]
	public async Task CutEmptySelection_NoChange()
	{
		var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
		using var _ = services as IDisposable;

		var d = new ClipboardTestDriver(app);
		d.AddWait(0).AddWait(1);

		var beforeCount = app.GetRowCount();
		var result = await app.CutRowsAsync(new List<int>());

		result.IsSuccess.Should().BeTrue();
		app.GetRowCount().Should().Be(beforeCount);
	}
}
