using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using NtoLib.Recipes.MbeTable.ModuleApplication;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Facade;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ModulePresentation.State;

using Tests.MbeTable.Application.Clipboard.Helpers;

using Xunit;

namespace Tests.MbeTable.Presentation;

[Trait("Category", "Integration")]
[Trait("Component", "Presentation")]
[Trait("Area", "DefaultedCellTracker")]
public sealed class DefaultedCellTrackerTests
{
	private static DefaultedCellTracker Build(
		out RecipeOperationService app,
		out IReadOnlyList<ColumnDefinition> columns,
		out PropertyStateProvider stateProvider,
		out RecipeFacade facade,
		out IServiceProvider services)
	{
		var dir = ClipboardYamlHelper.PrepareYamlConfigDirectory();
		var provider = new ClipboardTestConfigProvider(dir);
		services = ApplicationTestServiceProviderFactory.Create(
			provider.AppConfiguration,
			provider.CompiledFormulas);

		app = services.GetRequiredService<RecipeOperationService>();
		columns = services.GetRequiredService<IReadOnlyList<ColumnDefinition>>();
		stateProvider = services.GetRequiredService<PropertyStateProvider>();
		facade = services.GetRequiredService<RecipeFacade>();

		return new DefaultedCellTracker(app, facade, stateProvider, columns);
	}

	private static int IndexOf(IReadOnlyList<ColumnDefinition> columns, ColumnIdentifier key)
	{
		for (var i = 0; i < columns.Count; i++)
		{
			if (columns[i].Key == key)
			{
				return i;
			}
		}

		return -1;
	}

	[Fact]
	public async Task MarkRow_SeedsExactlyEnabledNonActionColumns()
	{
		using var tracker = Build(out var app, out var columns, out var stateProvider, out var facade, out var services);
		using var servicesScope = services as IDisposable;
		app.AddStep(0);

		await app.SetCellValueAsync(0, MandatoryColumns.Action, (short)ServiceActions.Wait);

		var step = facade.CurrentSnapshot.Recipe.Steps[0];
		for (var col = 0; col < columns.Count; col++)
		{
			var key = columns[col].Key;
			var expected = key != MandatoryColumns.Action
							&& stateProvider.GetPropertyState(step, key) == PropertyState.Enabled;
			tracker.IsMarked(0, col).Should().Be(expected, $"column {key.Value}");
		}
	}

	[Fact]
	public async Task MarkRow_NeverMarksActionOrReadonlyStartTime()
	{
		using var tracker = Build(out var app, out var columns, out _, out _, out var services);
		using var servicesScope = services as IDisposable;
		app.AddStep(0);

		await app.SetCellValueAsync(0, MandatoryColumns.Action, (short)ServiceActions.Wait);

		tracker.IsMarked(0, IndexOf(columns, MandatoryColumns.Action)).Should().BeFalse();
		tracker.IsMarked(0, IndexOf(columns, MandatoryColumns.StepStartTime)).Should().BeFalse();
	}

	[Fact]
	public async Task MarkRow_RaisesMarksChangedForThatRow()
	{
		using var tracker = Build(out var app, out _, out _, out _, out var services);
		using var servicesScope = services as IDisposable;
		app.AddStep(0);

		var changes = new List<MarksChange>();
		tracker.MarksChanged += c => changes.Add(c);

		await app.SetCellValueAsync(0, MandatoryColumns.Action, (short)ServiceActions.Wait);

		changes.Should().ContainSingle();
		changes[0].Row.Should().Be(0);
	}

	[Fact]
	public async Task MarkRow_Repeated_ReplacesSet()
	{
		using var tracker = Build(out var app, out var columns, out var stateProvider, out var facade, out var services);
		using var servicesScope = services as IDisposable;
		app.AddStep(0);

		await app.SetCellValueAsync(0, MandatoryColumns.Action, (short)ServiceActions.Wait);
		await app.SetCellValueAsync(0, MandatoryColumns.Action, (short)ServiceActions.Wait);

		var step = facade.CurrentSnapshot.Recipe.Steps[0];
		var enabledColumns = Enumerable.Range(0, columns.Count)
			.Where(c => columns[c].Key != MandatoryColumns.Action
						&& stateProvider.GetPropertyState(step, columns[c].Key) == PropertyState.Enabled)
			.ToList();

		enabledColumns.Should().NotBeEmpty();
		enabledColumns.Should().OnlyContain(c => tracker.IsMarked(0, c));
	}

	[Fact]
	public async Task ClearCell_ClearsOneCell_SiblingsSurvive()
	{
		using var tracker = Build(out var app, out var columns, out var stateProvider, out var facade, out var services);
		using var servicesScope = services as IDisposable;
		app.AddStep(0);
		await app.SetCellValueAsync(0, MandatoryColumns.Action, (short)ServiceActions.Wait);

		var step = facade.CurrentSnapshot.Recipe.Steps[0];
		var enabledColumns = Enumerable.Range(0, columns.Count)
			.Where(c => columns[c].Key != MandatoryColumns.Action
						&& stateProvider.GetPropertyState(step, columns[c].Key) == PropertyState.Enabled)
			.ToList();
		enabledColumns.Count.Should().BeGreaterThan(1);

		var clearedKey = columns[enabledColumns[0]].Key;
		await app.SetCellValueAsync(0, clearedKey, 1f);

		tracker.IsMarked(0, enabledColumns[0]).Should().BeFalse();
		foreach (var survivor in enabledColumns.Skip(1))
		{
			tracker.IsMarked(0, survivor).Should().BeTrue($"column index {survivor} must survive");
		}
	}

	[Fact]
	public async Task ClearCell_KeyToIndexMapping_MatchesColumnPosition()
	{
		using var tracker = Build(out var app, out var columns, out _, out _, out var services);
		using var servicesScope = services as IDisposable;
		app.AddStep(0);
		await app.SetCellValueAsync(0, MandatoryColumns.Action, (short)ServiceActions.Wait);

		var stepDurationIndex = IndexOf(columns, MandatoryColumns.StepDuration);
		tracker.IsMarked(0, stepDurationIndex).Should().BeTrue();

		await app.SetCellValueAsync(0, MandatoryColumns.StepDuration, 5f);

		tracker.IsMarked(0, stepDurationIndex).Should().BeFalse();
	}

	[Fact]
	public async Task Insert_ShiftsMarkedRowsAtOrAfterIndex()
	{
		using var tracker = Build(out var app, out var columns, out _, out _, out var services);
		using var servicesScope = services as IDisposable;
		app.AddStep(0);
		app.AddStep(1);
		await app.SetCellValueAsync(1, MandatoryColumns.Action, (short)ServiceActions.Wait);

		var durationIndex = IndexOf(columns, MandatoryColumns.StepDuration);
		tracker.IsMarked(1, durationIndex).Should().BeTrue();

		app.AddStep(0);

		tracker.IsMarked(1, durationIndex).Should().BeFalse();
		tracker.IsMarked(2, durationIndex).Should().BeTrue();
	}

	[Fact]
	public async Task Insert_LeavesRowsBeforeIndexUnchanged()
	{
		using var tracker = Build(out var app, out var columns, out _, out _, out var services);
		using var servicesScope = services as IDisposable;
		app.AddStep(0);
		app.AddStep(1);
		await app.SetCellValueAsync(0, MandatoryColumns.Action, (short)ServiceActions.Wait);

		var durationIndex = IndexOf(columns, MandatoryColumns.StepDuration);
		tracker.IsMarked(0, durationIndex).Should().BeTrue();

		app.AddStep(2).IsSuccess.Should().BeTrue();

		tracker.IsMarked(0, durationIndex).Should().BeTrue();
	}

	[Fact]
	public async Task Remove_DropsRemovedRow_DecrementsSurvivors()
	{
		using var tracker = Build(out var app, out var columns, out _, out _, out var services);
		using var servicesScope = services as IDisposable;
		app.AddStep(0);
		app.AddStep(1);
		app.AddStep(2);
		await app.SetCellValueAsync(0, MandatoryColumns.Action, (short)ServiceActions.Wait);
		await app.SetCellValueAsync(2, MandatoryColumns.Action, (short)ServiceActions.Wait);

		var durationIndex = IndexOf(columns, MandatoryColumns.StepDuration);
		tracker.IsMarked(0, durationIndex).Should().BeTrue();
		tracker.IsMarked(2, durationIndex).Should().BeTrue();

		app.RemoveStep(1);

		tracker.IsMarked(0, durationIndex).Should().BeTrue();
		tracker.IsMarked(1, durationIndex).Should().BeTrue();
		tracker.IsMarked(2, durationIndex).Should().BeFalse();
	}

	[Fact]
	public async Task Remove_DropsMarkedRowItself()
	{
		using var tracker = Build(out var app, out var columns, out _, out _, out var services);
		using var servicesScope = services as IDisposable;
		app.AddStep(0);
		app.AddStep(1);
		await app.SetCellValueAsync(1, MandatoryColumns.Action, (short)ServiceActions.Wait);

		var durationIndex = IndexOf(columns, MandatoryColumns.StepDuration);
		tracker.IsMarked(1, durationIndex).Should().BeTrue();

		app.RemoveStep(1);

		tracker.IsMarked(1, durationIndex).Should().BeFalse();
		tracker.IsMarked(0, durationIndex).Should().BeFalse();
	}

	[Fact]
	public async Task Remove_UnsortedDeduplicatedIndices_ShiftsSurvivorsCorrectly()
	{
		using var tracker = Build(out var app, out var columns, out _, out _, out var services);
		using var servicesScope = services as IDisposable;
		for (var i = 0; i < 5; i++)
		{
			app.AddStep(i);
		}

		await app.SetCellValueAsync(4, MandatoryColumns.Action, (short)ServiceActions.Wait);

		var durationIndex = IndexOf(columns, MandatoryColumns.StepDuration);
		tracker.IsMarked(4, durationIndex).Should().BeTrue();

		// PerformDelete shape: Distinct but no OrderBy; remove rows 0, 2, 0 -> {0, 2}
		await app.DeleteRowsAsync(new[] { 2, 0, 0 });

		// Row 4 had two removed indices below it -> decremented to 2.
		tracker.IsMarked(2, durationIndex).Should().BeTrue();
		tracker.IsMarked(4, durationIndex).Should().BeFalse();
	}

	[Fact]
	public async Task Reset_ClearsAll_RaisesBulkMarksChanged()
	{
		using var tracker = Build(out var app, out var columns, out _, out _, out var services);
		using var servicesScope = services as IDisposable;
		app.AddStep(0);
		await app.SetCellValueAsync(0, MandatoryColumns.Action, (short)ServiceActions.Wait);

		var durationIndex = IndexOf(columns, MandatoryColumns.StepDuration);
		tracker.IsMarked(0, durationIndex).Should().BeTrue();

		var changes = new List<MarksChange>();
		tracker.MarksChanged += c => changes.Add(c);

		await app.LoadRecipeAsync("any.csv");

		tracker.IsMarked(0, durationIndex).Should().BeFalse();
		changes.Should().Contain(c => c.Row == null);
	}

	[Fact]
	public async Task RecipeSaved_ClearsAll()
	{
		using var tracker = Build(out var app, out var columns, out _, out _, out var services);
		using var servicesScope = services as IDisposable;
		app.AddStep(0);
		await app.SetCellValueAsync(0, MandatoryColumns.Action, (short)ServiceActions.Wait);

		var durationIndex = IndexOf(columns, MandatoryColumns.StepDuration);
		tracker.IsMarked(0, durationIndex).Should().BeTrue();

		await app.SaveRecipeAsync("out.csv");

		tracker.IsMarked(0, durationIndex).Should().BeFalse();
	}

	[Fact]
	public async Task ClearCell_OnUnmarkedCell_RaisesNoEvent()
	{
		using var tracker = Build(out var app, out _, out _, out _, out var services);
		using var servicesScope = services as IDisposable;
		app.AddStep(0);
		// Establish an action first so committing a value targets a non-action edit.
		await app.SetCellValueAsync(0, MandatoryColumns.Action, (short)ServiceActions.Wait);
		// Commit once to clear the cell's mark.
		await app.SetCellValueAsync(0, MandatoryColumns.StepDuration, 5f);

		var changes = new List<MarksChange>();
		tracker.MarksChanged += c => changes.Add(c);

		// Committing the same already-cleared cell again must not raise.
		await app.SetCellValueAsync(0, MandatoryColumns.StepDuration, 7f);

		changes.Should().BeEmpty();
	}

	[Fact]
	public async Task ClearCellByIndex_ClearsTargetedCell_SiblingsSurvive()
	{
		using var tracker = Build(out var app, out var columns, out var stateProvider, out var facade, out var services);
		using var servicesScope = services as IDisposable;
		app.AddStep(0);
		await app.SetCellValueAsync(0, MandatoryColumns.Action, (short)ServiceActions.Wait);

		var step = facade.CurrentSnapshot.Recipe.Steps[0];
		var enabledColumns = Enumerable.Range(0, columns.Count)
			.Where(c => columns[c].Key != MandatoryColumns.Action
						&& stateProvider.GetPropertyState(step, columns[c].Key) == PropertyState.Enabled)
			.ToList();
		enabledColumns.Count.Should().BeGreaterThan(1);

		var changes = new List<MarksChange>();
		tracker.MarksChanged += c => changes.Add(c);

		tracker.ClearCell(0, enabledColumns[0]);

		tracker.IsMarked(0, enabledColumns[0]).Should().BeFalse();
		foreach (var survivor in enabledColumns.Skip(1))
		{
			tracker.IsMarked(0, survivor).Should().BeTrue($"column index {survivor} must survive");
		}

		changes.Should().ContainSingle();
		changes[0].Row.Should().Be(0);
	}

	[Fact]
	public async Task ClearCellByIndex_OnUnmarkedCell_RaisesNoEvent()
	{
		using var tracker = Build(out var app, out var columns, out _, out _, out var services);
		using var servicesScope = services as IDisposable;
		app.AddStep(0);
		await app.SetCellValueAsync(0, MandatoryColumns.Action, (short)ServiceActions.Wait);

		var actionIndex = IndexOf(columns, MandatoryColumns.Action);

		var changes = new List<MarksChange>();
		tracker.MarksChanged += c => changes.Add(c);

		// Action column is never marked, so clearing it by index is a no-op.
		tracker.ClearCell(0, actionIndex);

		changes.Should().BeEmpty();
	}

	[Fact]
	public async Task Dispose_UnsubscribesFromServiceEvents()
	{
		var tracker = Build(out var app, out var columns, out _, out _, out var services);
		using var servicesScope = services as IDisposable;
		app.AddStep(0);

		tracker.Dispose();
		await app.SetCellValueAsync(0, MandatoryColumns.Action, (short)ServiceActions.Wait);

		var durationIndex = IndexOf(columns, MandatoryColumns.StepDuration);
		tracker.IsMarked(0, durationIndex).Should().BeFalse();
	}

	[Fact]
	public async Task ConcurrentReadsDuringBulkClear_DoNotCorruptMarkStore()
	{
		using var tracker = Build(out var app, out var columns, out _, out _, out var services);
		using var servicesScope = services as IDisposable;
		for (var i = 0; i < 20; i++)
		{
			app.AddStep(i);
			await app.SetCellValueAsync(i, MandatoryColumns.Action, (short)ServiceActions.Wait);
		}

		var durationIndex = IndexOf(columns, MandatoryColumns.StepDuration);
		using var cancellation = new CancellationTokenSource();

		var reader = Task.Run(() =>
		{
			while (!cancellation.IsCancellationRequested)
			{
				for (var row = 0; row < 20; row++)
				{
					tracker.IsMarked(row, durationIndex);
				}
			}
		});

		var writer = Task.Run(() =>
		{
			for (var iteration = 0; iteration < 200; iteration++)
			{
				tracker.ClearCell(iteration % 20, durationIndex);
			}

			cancellation.Cancel();
		});

		// Without serialized access to the mark store the reader would throw
		// InvalidOperationException; the lock must let both tasks complete cleanly.
		Func<Task> act = async () => await Task.WhenAll(reader, writer);
		await act.Should().NotThrowAsync();
	}
}
