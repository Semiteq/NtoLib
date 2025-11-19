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
    public void PasteSingleRow_ValidClipboard_InsertsStep()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1);

        app.CopyRowsAsync(new List<int> { 0 }).GetAwaiter().GetResult();
        var tsv = clipboard.WrittenText;
        clipboard.SetText(tsv);

        var beforeCount = app.GetRowCount();
        var result = app.PasteRowsAsync(1).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
        app.GetRowCount().Should().Be(beforeCount + 1);
    }

    [Fact]
    public void PasteMultipleRows_ValidClipboard_InsertsAll()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1).AddWait(2).AddWait(3);

        app.CopyRowsAsync(new List<int> { 0, 1, 2 }).GetAwaiter().GetResult();
        var tsv = clipboard.WrittenText;
        clipboard.SetText(tsv);

        var beforeCount = app.GetRowCount();
        var result = app.PasteRowsAsync(0).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
        app.GetRowCount().Should().Be(beforeCount + 3);
    }

    [Fact]
    public void PasteIntoEmptyRecipe_InsertsRows()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1);

        app.CopyRowsAsync(new List<int> { 0, 1 }).GetAwaiter().GetResult();
        app.DeleteRowsAsync(new List<int> { 0, 1 }).GetAwaiter().GetResult();
        var tsv = clipboard.WrittenText;
        clipboard.Clear();

        d = new ClipboardTestDriver(app);
        clipboard.SetText(tsv);

        var result = app.PasteRowsAsync(0).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
        app.GetRowCount().Should().Be(2);
    }

    [Fact]
    public void PasteWithInvalidActionId_Fails()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0);

        app.CopyRowsAsync(new List<int> { 0 }).GetAwaiter().GetResult();
        var tsv = clipboard.WrittenText;
        tsv = tsv.Replace("10\t", "999\t");
        clipboard.SetText(tsv);

        var beforeCount = app.GetRowCount();
        var result = app.PasteRowsAsync(0).GetAwaiter().GetResult();

        result.IsFailed.Should().BeTrue();
        app.GetRowCount().Should().Be(beforeCount);
    }

    [Fact]
    public void PasteWithColumnCountMismatch_Fails()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0);

        clipboard.SetText("10\t\t10\t\t");

        var beforeCount = app.GetRowCount();
        var result = app.PasteRowsAsync(0).GetAwaiter().GetResult();

        result.IsFailed.Should().BeTrue();
        app.GetRowCount().Should().Be(beforeCount);
    }

    [Fact]
    public void PasteEmptyClipboard_SucceedsWithWarning()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0);

        clipboard.Clear();

        var beforeCount = app.GetRowCount();
        var result = app.PasteRowsAsync(0).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
        result.Reasons.Should().NotBeEmpty();
        app.GetRowCount().Should().Be(beforeCount);
    }

    [Fact]
    public void PasteOutOfRangeTargetIndex_ClampsToEnd()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1);

        app.CopyRowsAsync(new List<int> { 0 }).GetAwaiter().GetResult();
        var tsv = clipboard.WrittenText;
        clipboard.SetText(tsv);

        var result = app.PasteRowsAsync(10).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
        app.GetRowCount().Should().Be(3);
    }

    [Fact]
    public void PasteNegativeTargetIndex_ClampsToZero()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1);

        app.CopyRowsAsync(new List<int> { 0 }).GetAwaiter().GetResult();
        var tsv = clipboard.WrittenText;
        clipboard.SetText(tsv);

        var result = app.PasteRowsAsync(-5).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
        app.GetRowCount().Should().Be(3);
    }
}