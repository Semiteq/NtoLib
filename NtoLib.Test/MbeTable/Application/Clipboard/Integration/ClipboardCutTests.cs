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
    public void CutSingleRow_WritesAndDeletes()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1).AddWait(2);

        var beforeCount = app.GetRowCount();
        var result = app.CutRowsAsync(new List<int> { 1 }).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
        clipboard.WrittenText.Should().NotBeNullOrWhiteSpace();
        app.GetRowCount().Should().Be(beforeCount - 1);
    }

    [Fact]
    public void CutMultipleRows_DeletesAll()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1).AddWait(2).AddWait(3).AddWait(4);

        var result = app.CutRowsAsync(new List<int> { 0, 2, 4 }).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
        clipboard.WrittenText.Split('\n').Should().HaveCount(3);
        app.GetRowCount().Should().Be(2);
    }

    [Fact]
    public void CutAllRows_EmptiesRecipe()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1).AddWait(2);

        var result = app.CutRowsAsync(new List<int> { 0, 1, 2 }).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
        clipboard.WrittenText.Split('\n').Should().HaveCount(3);
        app.GetRowCount().Should().Be(0);
    }

    [Fact]
    public void CutEmptySelection_NoChange()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1);

        var beforeCount = app.GetRowCount();
        var result = app.CutRowsAsync(new List<int>()).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
        app.GetRowCount().Should().Be(beforeCount);
    }
}