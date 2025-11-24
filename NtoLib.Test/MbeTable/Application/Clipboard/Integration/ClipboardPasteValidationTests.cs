using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Test.MbeTable.Application.Clipboard.Helpers;

using Xunit;

namespace NtoLib.Test.MbeTable.Application.Clipboard.Integration;

[Trait("Category", "Integration")]
[Trait("Component", "Application")]
[Trait("Area", "ClipboardPasteValidation")]
public sealed class ClipboardPasteValidationTests
{
    private const short ActionIdWithTask = 90;

    [Fact]
    public async Task PasteWithNegativeDuration_Fails()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0);

        await app.CopyRowsAsync(new List<int> { 0 });
        var tsv = clipboard.WrittenText;
        tsv = tsv.Replace("10\t", "-5\t");
        clipboard.SetText(tsv);

        var beforeCount = app.GetRowCount();
        var result = await app.PasteRowsAsync(0);

        result.IsFailed.Should().BeTrue();
        app.GetRowCount().Should().Be(beforeCount);
    }

    [Fact]
    public async Task PasteWithExcessiveDuration_Fails()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0);

        await app.CopyRowsAsync(new List<int> { 0 });
        var tsv = clipboard.WrittenText;
        tsv = tsv.Replace("10\t", "9999999\t");
        clipboard.SetText(tsv);

        var beforeCount = app.GetRowCount();
        var result = await app.PasteRowsAsync(0);

        result.IsFailed.Should().BeTrue();
        app.GetRowCount().Should().Be(beforeCount);
    }

    [Fact]
    public async Task PasteWithInvalidFloatFormat_Fails()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0);

        await app.CopyRowsAsync(new List<int> { 0 });
        var tsv = clipboard.WrittenText;
        tsv = tsv.Replace("10\t", "abc\t");
        clipboard.SetText(tsv);

        var beforeCount = app.GetRowCount();
        var result = await app.PasteRowsAsync(0);

        result.IsFailed.Should().BeTrue();
        app.GetRowCount().Should().Be(beforeCount);
    }

    [Fact]
    public async Task PasteWithNegativeTask_Fails()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var repo = services.GetRequiredService<IActionRepository>();
        var actionResult = repo.GetActionDefinitionById(ActionIdWithTask);
        if (actionResult.IsFailed)
        {
            return;
        }

        var d = new ClipboardTestDriver(app);
        d.AddActionStep(0, ActionIdWithTask);

        await app.CopyRowsAsync(new List<int> { 0 });
        var tsv = clipboard.WrittenText;
        var parts = tsv.Split('\t');
        if (parts.Length > 1)
        {
            parts[1] = "-10";
            tsv = string.Join("\t", parts);
            clipboard.SetText(tsv);

            var beforeCount = app.GetRowCount();
            var result = await app.PasteRowsAsync(0);

            result.IsFailed.Should().BeTrue();
            app.GetRowCount().Should().Be(beforeCount);
        }
    }

    [Fact]
    public async Task PasteWithExcessiveTask_Fails()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var repo = services.GetRequiredService<IActionRepository>();
        var actionResult = repo.GetActionDefinitionById(ActionIdWithTask);
        if (actionResult.IsFailed)
        {
            return;
        }

        var d = new ClipboardTestDriver(app);
        d.AddActionStep(0, ActionIdWithTask);

        await app.CopyRowsAsync(new List<int> { 0 });
        var tsv = clipboard.WrittenText;
        var parts = tsv.Split('\t');
        if (parts.Length > 1)
        {
            parts[1] = "99999";
            tsv = string.Join("\t", parts);
            clipboard.SetText(tsv);

            var beforeCount = app.GetRowCount();
            var result = await app.PasteRowsAsync(0);

            result.IsFailed.Should().BeTrue();
            app.GetRowCount().Should().Be(beforeCount);
        }
    }

    [Fact]
    public async Task PasteMultipleRows_OneInvalid_FailsAll()
    {
        var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
        using var _ = services as IDisposable;

        var d = new ClipboardTestDriver(app);
        d.AddWait(0).AddWait(1).AddWait(2);

        await app.CopyRowsAsync(new List<int> { 0, 1, 2 });
        var tsv = clipboard.WrittenText;
        var lines = tsv.Split('\n');
        if (lines.Length > 1)
        {
            lines[1] = lines[1].Replace("10\t", "-999\t");
            tsv = string.Join("\n", lines);
            clipboard.SetText(tsv);

            var beforeCount = app.GetRowCount();
            var result = await app.PasteRowsAsync(0);

            result.IsFailed.Should().BeTrue();
            app.GetRowCount().Should().Be(beforeCount);
        }
    }
}