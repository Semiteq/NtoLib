using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using FB.VisualFB;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModulePresentation;
using NtoLib.Recipes.MbeTable.ModulePresentation.Adapters;
using NtoLib.Recipes.MbeTable.ModulePresentation.Behavior;
using NtoLib.Recipes.MbeTable.ModulePresentation.Rendering;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable;

/// <summary>
/// Main UI control for MbeTable recipe management.
/// Integrates with MasterSCADA 3.12 environment.
/// </summary>
[ComVisible(true)]
[DisplayName("Таблица рецептов МБЕ")]
[Guid("8161DF32-8D80-4B81-AF52-3021AE0AD293")]
public partial class TableControl : VisualControlBase
{
	[NonSerialized] private IServiceProvider? _serviceProvider;
	[NonSerialized] private TablePresenter? _presenter;
	[NonSerialized] private ITableView? _tableView;
	[NonSerialized] private DesignTimeColorSchemeProvider? _colorSchemeProvider;

	[NonSerialized] private bool _runtimeInitialized;
	[NonSerialized] private TableBehaviorManager? _behaviorManager;
	[NonSerialized] private TableRenderCoordinator? _renderCoordinator;
	[NonSerialized] private ILogger? _logger;

	public TableControl() : base(true)
	{
		InitializeComponent();
	}
}
