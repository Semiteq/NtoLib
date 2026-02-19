using System.Collections.Generic;
using System.Windows.Forms;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleApplication;
using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModulePresentation.Columns;
using NtoLib.Recipes.MbeTable.ModulePresentation.State;
using NtoLib.Recipes.MbeTable.ModulePresentation.StateProviders;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;
using NtoLib.Recipes.MbeTable.ServiceStatus;

namespace NtoLib.Recipes.MbeTable.ModulePresentation;

/// <summary>
/// Bundles all DI-resolved services that <see cref="TableControl"/> needs
/// during its runtime lifecycle. Resolved once from the container instead
/// of 12+ individual <c>GetRequiredService</c> calls.
/// </summary>
internal sealed class TableControlServices
{
	public ILogger<TableControl> Logger { get; }
	public DesignTimeColorSchemeProvider ColorSchemeProvider { get; }
	public StateProvider StateProvider { get; }
	public StatusService StatusService { get; }
	public ColorScheme ColorScheme { get; }
	public IReadOnlyList<ColumnDefinition> ColumnDefinitions { get; }
	public FactoryColumnRegistry ColumnRegistry { get; }
	public RecipeOperationService RecipeOperationService { get; }
	public ThreadSafeRowExecutionStateProvider RowStateProvider { get; }
	public BusyStateManager BusyStateManager { get; }
	public OpenFileDialog OpenFileDialog { get; }
	public SaveFileDialog SaveFileDialog { get; }

	public TableControlServices(
		ILogger<TableControl> logger,
		DesignTimeColorSchemeProvider colorSchemeProvider,
		StateProvider stateProvider,
		StatusService statusService,
		ColorScheme colorScheme,
		IReadOnlyList<ColumnDefinition> columnDefinitions,
		FactoryColumnRegistry columnRegistry,
		RecipeOperationService recipeOperationService,
		ThreadSafeRowExecutionStateProvider rowStateProvider,
		BusyStateManager busyStateManager,
		OpenFileDialog openFileDialog,
		SaveFileDialog saveFileDialog)
	{
		Logger = logger;
		ColorSchemeProvider = colorSchemeProvider;
		StateProvider = stateProvider;
		StatusService = statusService;
		ColorScheme = colorScheme;
		ColumnDefinitions = columnDefinitions;
		ColumnRegistry = columnRegistry;
		RecipeOperationService = recipeOperationService;
		RowStateProvider = rowStateProvider;
		BusyStateManager = busyStateManager;
		OpenFileDialog = openFileDialog;
		SaveFileDialog = saveFileDialog;
	}
}
