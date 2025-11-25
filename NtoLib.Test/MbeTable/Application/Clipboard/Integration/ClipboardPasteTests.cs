using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FluentAssertions;

using NtoLib.Test.MbeTable.Application.Clipboard.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Application.Clipboard.Integration;

[Trait("Category", "Integration")]
[Trait("Component", "Application")]
[Trait("Area", "ClipboardPaste")]
public sealed class ClipboardPasteTests
{
	[Fact]
	public async Task PasteSingleRow_ValidClipboard_InsertsStep()
	{
		var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
		using var _ = services as IDisposable;

		var d = new ClipboardTestDriver(app);
		d.AddWait(0).AddWait(1);

		await app.CopyRowsAsync(new List<int> { 0 });
		var tsv = clipboard.WrittenText;
		clipboard.SetText(tsv);

		var beforeCount = app.GetRowCount();
		var result = await app.PasteRowsAsync(1);

		result.IsSuccess.Should().BeTrue();
		app.GetRowCount().Should().Be(beforeCount + 1);
	}

	[Fact]
	public async Task PasteMultipleRows_ValidClipboard_InsertsAll()
	{
		var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
		using var _ = services as IDisposable;

		var d = new ClipboardTestDriver(app);
		d.AddWait(0).AddWait(1).AddWait(2).AddWait(3);

		await app.CopyRowsAsync(new List<int> { 0, 1, 2 });
		var tsv = clipboard.WrittenText;
		clipboard.SetText(tsv);

		var beforeCount = app.GetRowCount();
		var result = await app.PasteRowsAsync(0);

		result.IsSuccess.Should().BeTrue();
		app.GetRowCount().Should().Be(beforeCount + 3);
	}

	[Fact]
	public async Task PasteIntoEmptyRecipe_InsertsRows()
	{
		var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
		using var _ = services as IDisposable;

		var d = new ClipboardTestDriver(app);
		d.AddWait(0).AddWait(1);

		await app.CopyRowsAsync(new List<int> { 0, 1 });
		await app.DeleteRowsAsync(new List<int> { 0, 1 });
		var tsv = clipboard.WrittenText;
		clipboard.Clear();

		clipboard.SetText(tsv);

		var result = await app.PasteRowsAsync(0);

		result.IsSuccess.Should().BeTrue();
		app.GetRowCount().Should().Be(2);
	}

	[Fact]
	public async Task PasteWithInvalidActionId_Fails()
	{
		var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
		using var _ = services as IDisposable;

		var d = new ClipboardTestDriver(app);
		d.AddWait(0);

		await app.CopyRowsAsync(new List<int> { 0 });
		var tsv = clipboard.WrittenText;
		tsv = tsv.Replace("10\t", "999\t");
		clipboard.SetText(tsv);

		var beforeCount = app.GetRowCount();
		var result = await app.PasteRowsAsync(0);

		result.IsFailed.Should().BeTrue();
		app.GetRowCount().Should().Be(beforeCount);
	}

	[Fact]
	public async Task PasteWithColumnCountMismatch_Fails()
	{
		var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
		using var _ = services as IDisposable;

		var d = new ClipboardTestDriver(app);
		d.AddWait(0);

		clipboard.SetText("10\t\t10\t\t");

		var beforeCount = app.GetRowCount();
		var result = await app.PasteRowsAsync(0);

		result.IsFailed.Should().BeTrue();
		app.GetRowCount().Should().Be(beforeCount);
	}

	[Fact]
	public async Task PasteEmptyClipboard_SucceedsWithWarning()
	{
		var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
		using var _ = services as IDisposable;

		var d = new ClipboardTestDriver(app);
		d.AddWait(0);

		clipboard.Clear();

		var beforeCount = app.GetRowCount();
		var result = await app.PasteRowsAsync(0);

		result.IsSuccess.Should().BeTrue();
		result.Reasons.Should().NotBeEmpty();
		app.GetRowCount().Should().Be(beforeCount);
	}

	[Fact]
	public async Task PasteOutOfRangeTargetIndex_ClampsToEnd()
	{
		var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
		using var _ = services as IDisposable;

		var d = new ClipboardTestDriver(app);
		d.AddWait(0).AddWait(1);

		await app.CopyRowsAsync(new List<int> { 0 });
		var tsv = clipboard.WrittenText;
		clipboard.SetText(tsv);

		var result = await app.PasteRowsAsync(10);

		result.IsSuccess.Should().BeTrue();
		app.GetRowCount().Should().Be(3);
	}

	[Fact]
	public async Task PasteNegativeTargetIndex_ClampsToZero()
	{
		var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
		using var _ = services as IDisposable;

		var d = new ClipboardTestDriver(app);
		d.AddWait(0).AddWait(1);

		await app.CopyRowsAsync(new List<int> { 0 });
		var tsv = clipboard.WrittenText;
		clipboard.SetText(tsv);

		var result = await app.PasteRowsAsync(-5);

		result.IsSuccess.Should().BeTrue();
		app.GetRowCount().Should().Be(3);
	}
}
