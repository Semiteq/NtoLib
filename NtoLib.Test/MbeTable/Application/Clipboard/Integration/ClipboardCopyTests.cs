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
    public async Task CopySingleRow_ValidIndex_WritesToClipboard()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1).AddWait(2);

        var result = await app.CopyRowsAsync(new List<int> { 1 });

        result.IsSuccess.Should().BeTrue();
        clipboard.WrittenText.Should().NotBeNullOrWhiteSpace();
        clipboard.WrittenText.Split('\n').Should().HaveCount(1);
    }

    [Fact]
    public async Task CopyMultipleRows_ValidIndices_WritesAllRows()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1).AddWait(2).AddWait(3);

        var result = await app.CopyRowsAsync(new List<int> { 1, 2, 3 });

        result.IsSuccess.Should().BeTrue();
        clipboard.WrittenText.Split('\n').Should().HaveCount(3);
    }

    [Fact]
    public async Task CopyWithOutOfRangeIndices_IgnoresInvalid()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1).AddWait(2);

        var result = await app.CopyRowsAsync(new List<int> { 1, 5, -1 });

        result.IsSuccess.Should().BeTrue();
        clipboard.WrittenText.Split('\n').Should().HaveCount(1);
    }

    [Fact]
    public async Task CopyEmptySelection_Succeeds()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1);

        var result = await app.CopyRowsAsync(new List<int>());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CopyWithDuplicateIndices_WritesUniqueRows()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1).AddWait(2);

        var result = await app.CopyRowsAsync(new List<int> { 1, 1, 2 });

        result.IsSuccess.Should().BeTrue();
        clipboard.WrittenText.Split('\n').Should().HaveCount(2);
    }
}