using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleApplication;
using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

using Tests.MbeTable.Application.Clipboard.Helpers;

using Xunit;

namespace Tests.MbeTable.Application;

[Trait("Category", "Integration")]
[Trait("Component", "Application")]
[Trait("Area", "RecipeOperationServiceEventContract")]
public sealed class RecipeOperationServiceEventContractTests
{
	private static RecipeOperationService Build(out IServiceProvider services, bool registerModbus = true)
	{
		var dir = ClipboardYamlHelper.PrepareYamlConfigDirectory();
		var provider = new ClipboardTestConfigProvider(dir);
		services = ApplicationTestServiceProviderFactory.Create(
			provider.AppConfiguration,
			provider.CompiledFormulas,
			registerModbus);

		return services.GetRequiredService<RecipeOperationService>();
	}

	[Fact]
	public async Task SetCellValueAsync_OnActionColumn_RaisesActionReplacedOnly()
	{
		var app = Build(out var services);
		using var _ = services as IDisposable;
		app.AddStep(0);

		var actionReplacedRows = new List<int>();
		var commits = new List<(int Row, ColumnIdentifier Column)>();
		app.ActionReplaced += r => actionReplacedRows.Add(r);
		app.CellValueCommitted += c => commits.Add(c);

		var result = await app.SetCellValueAsync(0, MandatoryColumns.Action, (short)ServiceActions.Wait);

		result.IsSuccess.Should().BeTrue();
		actionReplacedRows.Should().ContainSingle().Which.Should().Be(0);
		commits.Should().BeEmpty();
	}

	[Fact]
	public async Task SetCellValueAsync_OnNonActionColumn_RaisesCellValueCommittedOnly()
	{
		var app = Build(out var services);
		using var _ = services as IDisposable;
		app.AddStep(0);
		await app.SetCellValueAsync(0, MandatoryColumns.Action, (short)ServiceActions.Wait);

		var actionReplacedRows = new List<int>();
		var commits = new List<(int Row, ColumnIdentifier Column)>();
		app.ActionReplaced += r => actionReplacedRows.Add(r);
		app.CellValueCommitted += c => commits.Add(c);

		var result = await app.SetCellValueAsync(0, MandatoryColumns.StepDuration, 5f);

		result.IsSuccess.Should().BeTrue();
		actionReplacedRows.Should().BeEmpty();
		commits.Should().ContainSingle();
		commits[0].Row.Should().Be(0);
		commits[0].Column.Should().Be(MandatoryColumns.StepDuration);
	}

	[Fact]
	public void AddStep_RaisesInsertStructureChange()
	{
		var app = Build(out var services);
		using var _ = services as IDisposable;

		var changes = new List<StructureChange>();
		app.RecipeStructureChanged += c => changes.Add(c);

		app.AddStep(0);

		changes.Should().ContainSingle();
		changes[0].Kind.Should().Be(StructureChangeKind.Insert);
		changes[0].Index.Should().Be(0);
		changes[0].Count.Should().Be(1);
	}

	[Fact]
	public void RemoveStep_RaisesRemoveStructureChange()
	{
		var app = Build(out var services);
		using var _ = services as IDisposable;
		app.AddStep(0);
		app.AddStep(1);

		var changes = new List<StructureChange>();
		app.RecipeStructureChanged += c => changes.Add(c);

		app.RemoveStep(1);

		changes.Should().ContainSingle();
		changes[0].Kind.Should().Be(StructureChangeKind.Remove);
		changes[0].RemovedIndices.Should().Equal(1);
	}

	[Fact]
	public async Task PasteRows_RaisesInsertWithStepCount()
	{
		var app = Build(out var services);
		using var _ = services as IDisposable;
		app.AddStep(0);
		app.AddStep(1);
		await app.CopyRowsAsync(new[] { 0, 1 });

		var changes = new List<StructureChange>();
		app.RecipeStructureChanged += c => changes.Add(c);

		var result = await app.PasteRowsAsync(2);

		result.IsSuccess.Should().BeTrue();
		changes.Should().ContainSingle();
		changes[0].Kind.Should().Be(StructureChangeKind.Insert);
		changes[0].Index.Should().Be(2);
		changes[0].Count.Should().Be(2);
	}

	[Fact]
	public async Task DeleteRows_RaisesRemoveWithDeduplicatedIndices()
	{
		var app = Build(out var services);
		using var _ = services as IDisposable;
		app.AddStep(0);
		app.AddStep(1);
		app.AddStep(2);

		var changes = new List<StructureChange>();
		app.RecipeStructureChanged += c => changes.Add(c);

		var result = await app.DeleteRowsAsync(new[] { 2, 0, 2 });

		result.IsSuccess.Should().BeTrue();
		changes.Should().ContainSingle();
		changes[0].Kind.Should().Be(StructureChangeKind.Remove);
		changes[0].RemovedIndices.Should().BeEquivalentTo(new[] { 2, 0 });
	}

	[Fact]
	public async Task CutRows_RaisesRemoveStructureChange()
	{
		var app = Build(out var services);
		using var _ = services as IDisposable;
		app.AddStep(0);
		app.AddStep(1);

		var changes = new List<StructureChange>();
		app.RecipeStructureChanged += c => changes.Add(c);

		var result = await app.CutRowsAsync(new[] { 0 });

		result.IsSuccess.Should().BeTrue();
		changes.Should().ContainSingle();
		changes[0].Kind.Should().Be(StructureChangeKind.Remove);
		changes[0].RemovedIndices.Should().Equal(0);
	}

	[Fact]
	public async Task LoadRecipeAsync_RaisesResetStructureChange()
	{
		var app = Build(out var services);
		using var _ = services as IDisposable;

		var changes = new List<StructureChange>();
		app.RecipeStructureChanged += c => changes.Add(c);

		var result = await app.LoadRecipeAsync("any.csv");

		result.IsSuccess.Should().BeTrue();
		changes.Should().ContainSingle();
		changes[0].Kind.Should().Be(StructureChangeKind.Reset);
	}

	[Fact]
	public async Task ReceiveRecipeAsync_RaisesResetStructureChange()
	{
		var app = Build(out var services);
		using var _ = services as IDisposable;

		var changes = new List<StructureChange>();
		app.RecipeStructureChanged += c => changes.Add(c);

		var result = await app.ReceiveRecipeAsync();

		result.IsSuccess.Should().BeTrue();
		changes.Should().ContainSingle();
		changes[0].Kind.Should().Be(StructureChangeKind.Reset);
	}

	[Fact]
	public async Task SaveRecipeAsync_OnSuccess_RaisesRecipeSaved()
	{
		var app = Build(out var services);
		using var _ = services as IDisposable;
		app.AddStep(0);

		var saved = 0;
		app.RecipeSaved += () => saved++;

		var result = await app.SaveRecipeAsync("out.csv");

		result.IsSuccess.Should().BeTrue();
		saved.Should().Be(1);
	}

	[Fact]
	public async Task SendRecipeAsync_OnSuccess_RaisesRecipeSent()
	{
		var app = Build(out var services);
		using var _ = services as IDisposable;
		services.GetRequiredService<StateProvider>().SetPlcFlags(enaSendOk: true, recipeActive: false);
		app.AddStep(0);
		await app.SetCellValueAsync(0, MandatoryColumns.Action, (short)ServiceActions.Wait);
		await app.SetCellValueAsync(0, MandatoryColumns.StepDuration, 5f);

		var sent = 0;
		app.RecipeSent += () => sent++;

		var result = await app.SendRecipeAsync();

		result.IsSuccess.Should().BeTrue();
		sent.Should().Be(1);
	}

	[Fact]
	public async Task SendRecipeAsync_OnFailure_DoesNotRaiseRecipeSent()
	{
		var app = Build(out var services);
		using var _ = services as IDisposable;
		// PLC send-enable flag left false -> the pipeline blocks Send before the modbus call.
		app.AddStep(0);
		await app.SetCellValueAsync(0, MandatoryColumns.Action, (short)ServiceActions.Wait);

		var sent = 0;
		app.RecipeSent += () => sent++;

		var result = await app.SendRecipeAsync();

		result.IsFailed.Should().BeTrue();
		sent.Should().Be(0);
	}

	[Fact]
	public async Task SendRecipeAsync_WhenModbusMissing_RaisesNeitherSentNorErrors()
	{
		var app = Build(out var services, registerModbus: false);
		using var _ = services as IDisposable;

		var sent = 0;
		app.RecipeSent += () => sent++;

		var result = await app.SendRecipeAsync();

		result.IsFailed.Should().BeTrue();
		sent.Should().Be(0);
	}

	[Fact]
	public void RecipeStructureChanged_ThrowingSubscriber_DoesNotPreventLaterSubscribers()
	{
		var app = Build(out var services);
		using var _ = services as IDisposable;

		var probeChanges = new List<StructureChange>();
		app.RecipeStructureChanged += _ => throw new InvalidOperationException("subscriber failure");
		app.RecipeStructureChanged += c => probeChanges.Add(c);

		var result = app.AddStep(0);

		result.IsSuccess.Should().BeTrue();
		probeChanges.Should().ContainSingle().Which.Kind.Should().Be(StructureChangeKind.Insert);
	}
}
