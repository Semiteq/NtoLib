using FluentAssertions;

using NtoLib.Test.MbeTable.Application.Clipboard.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Application.Clipboard.Integration;

[Trait("Category", "Integration")]
[Trait("Component", "Application")]
[Trait("Area", "ClipboardDelete")]
public sealed class ClipboardDeleteTests
{
    [Fact]
    public void DeleteSingleRow_RemovesStep()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1).AddWait(2);

        var beforeCount = app.GetRowCount();
        var result = app.DeleteRowsAsync(new List<int> { 1 }).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
        app.GetRowCount().Should().Be(beforeCount - 1);
    }

    [Fact]
    public void DeleteMultipleNonContiguous_RemovesAll()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1).AddWait(2).AddWait(3).AddWait(4);

        var result = app.DeleteRowsAsync(new List<int> { 1, 3 }).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
        app.GetRowCount().Should().Be(3);
    }

    [Fact]
    public void DeleteWithOutOfRangeIndices_IgnoresInvalid()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1).AddWait(2);

        var result = app.DeleteRowsAsync(new List<int> { 1, 10 }).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
        app.GetRowCount().Should().Be(2);
    }

    [Fact]
    public void DeleteEmptySelection_NoChange()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1);

        var beforeCount = app.GetRowCount();
        var result = app.DeleteRowsAsync(new List<int>()).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
        app.GetRowCount().Should().Be(beforeCount);
    }
}