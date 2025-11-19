using FluentAssertions;

using NtoLib.Test.MbeTable.Application.Clipboard.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Application.Clipboard.Integration;

[Trait("Category", "Integration")]
[Trait("Component", "Application")]
[Trait("Area", "ClipboardCopy")]
public sealed class ClipboardCopyTests
{
    [Fact]
    public void CopySingleRow_ValidIndex_WritesToClipboard()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1).AddWait(2);

        var result = app.CopyRowsAsync(new List<int> { 1 }).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
        clipboard.WrittenText.Should().NotBeNullOrWhiteSpace();
        clipboard.WrittenText.Split('\n').Should().HaveCount(1);
    }

    [Fact]
    public void CopyMultipleRows_ValidIndices_WritesAllRows()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1).AddWait(2).AddWait(3);

        var result = app.CopyRowsAsync(new List<int> { 1, 2, 3 }).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
        clipboard.WrittenText.Split('\n').Should().HaveCount(3);
    }

    [Fact]
    public void CopyWithOutOfRangeIndices_IgnoresInvalid()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1).AddWait(2);

        var result = app.CopyRowsAsync(new List<int> { 1, 5, -1 }).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
        clipboard.WrittenText.Split('\n').Should().HaveCount(1);
    }

    [Fact]
    public void CopyEmptySelection_Succeeds()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1);

        var result = app.CopyRowsAsync(new List<int>()).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CopyWithDuplicateIndices_WritesUniqueRows()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1).AddWait(2);

        var result = app.CopyRowsAsync(new List<int> { 1, 1, 2 }).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
        clipboard.WrittenText.Split('\n').Should().HaveCount(2);
    }
}