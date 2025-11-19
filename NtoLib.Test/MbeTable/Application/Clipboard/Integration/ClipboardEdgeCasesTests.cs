using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Schema;
using NtoLib.Test.MbeTable.Application.Clipboard.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Application.Clipboard.Integration;

[Trait("Category", "Integration")]
[Trait("Component", "Application")]
[Trait("Area", "ClipboardEdgeCases")]
public sealed class ClipboardEdgeCasesTests
{
    private const int ExpectedWaitActionId = 10;

    [Fact]
    public void Schema_HasExpectedColumnCount()
    {
        var (services, _, _) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var schema = services.GetRequiredService<IClipboardSchemaDescriptor>();
        schema.TransferColumns.Count.Should().Be(7);
    }

    [Fact]
    public void PasteWithUnsafeCharacters_Sanitizes()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1);

        app.CopyRowsAsync(new List<int> { 0 }).GetAwaiter().GetResult();
        var tsv = clipboard.WrittenText;
        tsv = tsv.Replace("\n", "\tComment\twith\ttabs\n");
        clipboard.SetText(tsv);

        var result = app.PasteRowsAsync(0).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void PasteWithExcessiveLength_Truncates()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0);

        app.CopyRowsAsync(new List<int> { 0 }).GetAwaiter().GetResult();
        var longString = new string('a', 3000);

        var tsv = clipboard.WrittenText;
        tsv = tsv.Replace("\n", $"\t{longString}\n");

        clipboard.SetText(tsv);

        var result = app.PasteRowsAsync(0).GetAwaiter().GetResult();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void PasteMultipleOperationsInSequence_MaintainsConsistency()
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