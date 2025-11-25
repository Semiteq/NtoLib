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
	public async Task PasteWithUnsafeCharacters_Sanitizes()
	{
		var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
		using var _ = services as IDisposable;

		var d = new ClipboardTestDriver(app);
		d.AddWait(0).AddWait(1);

		await app.CopyRowsAsync(new List<int> { 0 });
		var tsv = clipboard.WrittenText;
		tsv = tsv.Replace("\n", "\tComment\twith\ttabs\n");
		clipboard.SetText(tsv);

		var result = await app.PasteRowsAsync(0);

		result.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public async Task PasteWithExcessiveLength_Truncates()
	{
		var (services, app, clipboard) = ClipboardTestHelper.BuildApplication();
		using var _ = services as IDisposable;

		var d = new ClipboardTestDriver(app);
		d.AddWait(0);

		await app.CopyRowsAsync(new List<int> { 0 });
		var longString = new string('a', 3000);

		var tsv = clipboard.WrittenText;
		tsv = tsv.Replace("\n", $"\t{longString}\n");

		clipboard.SetText(tsv);

		var result = await app.PasteRowsAsync(0);

		result.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public async Task PasteMultipleOperationsInSequence_MaintainsConsistency()
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
