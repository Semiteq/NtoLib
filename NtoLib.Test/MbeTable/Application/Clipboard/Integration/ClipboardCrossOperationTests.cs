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
    public void CopyThenPaste_DuplicatesRows()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1).AddWait(2);

        app.CopyRowsAsync(new List<int> { 0, 1 }).GetAwaiter().GetResult();
        var result = app.PasteRowsAsync(2).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
        app.GetRowCount().Should().Be(5);
    }

    [Fact]
    public void CutThenPaste_MovesRows()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1).AddWait(2).AddWait(3);

        app.CutRowsAsync(new List<int> { 1, 2 }).GetAwaiter().GetResult();
        var result = app.PasteRowsAsync(0).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
        app.GetRowCount().Should().Be(4);
    }

    [Fact]
    public void DeleteThenCopyThenPaste_SequenceWorks()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1).AddWait(2).AddWait(3).AddWait(4);

        app.DeleteRowsAsync(new List<int> { 0, 4 }).GetAwaiter().GetResult();
        app.CopyRowsAsync(new List<int> { 1 }).GetAwaiter().GetResult();
        var result = app.PasteRowsAsync(2).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
        app.GetRowCount().Should().Be(4);
    }
}