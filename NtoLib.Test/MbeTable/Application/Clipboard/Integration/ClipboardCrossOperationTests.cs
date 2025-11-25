using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FluentAssertions;

using NtoLib.Test.MbeTable.Application.Clipboard.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Application.Clipboard.Integration;

[Trait("Category", "Integration")]
[Trait("Component", "Application")]
[Trait("Area", "ClipboardCrossOperation")]
public sealed class ClipboardCrossOperationTests
{
	private const int ExpectedWaitActionId = 10;

	[Fact]
	public async Task CopyThenPaste_DuplicatesRows()
	{
		var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
		using var _ = services as IDisposable;

		var d = new ClipboardTestDriver(app);
		d.AddWait(0).AddWait(1).AddWait(2);

		await app.CopyRowsAsync(new List<int> { 0, 1 });
		var result = await app.PasteRowsAsync(2);

		result.IsSuccess.Should().BeTrue();
		app.GetRowCount().Should().Be(5);
	}

	[Fact]
	public async Task CutThenPaste_MovesRows()
	{
		var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
		using var _ = services as IDisposable;

		var d = new ClipboardTestDriver(app);
		d.AddWait(0).AddWait(1).AddWait(2).AddWait(3);

		await app.CutRowsAsync(new List<int> { 1, 2 });
		var result = await app.PasteRowsAsync(0);

		result.IsSuccess.Should().BeTrue();
		app.GetRowCount().Should().Be(4);
	}

	[Fact]
	public async Task DeleteThenCopyThenPaste_SequenceWorks()
	{
		var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
		using var _ = services as IDisposable;

		var d = new ClipboardTestDriver(app);
		d.AddWait(0).AddWait(1).AddWait(2).AddWait(3).AddWait(4);

		await app.DeleteRowsAsync(new List<int> { 0, 4 });
		await app.CopyRowsAsync(new List<int> { 1 });
		var result = await app.PasteRowsAsync(2);

		result.IsSuccess.Should().BeTrue();
		app.GetRowCount().Should().Be(4);
	}
}
