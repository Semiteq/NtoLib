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

namespace NtoLib.Recipes.MbeTableEditor;

[ComVisible(true)]
[DisplayName("Редактор рецептов MBE")]
[Guid("55E77764-CDD5-44DB-B914-80A44776C165")]
public partial class MbeTableEditorControl : VisualControlBase
{
	[NonSerialized] private TableBehaviorManager? _behaviorManager;
	[NonSerialized] private DesignTimeColorSchemeProvider? _colorSchemeProvider;
	[NonSerialized] private ILogger? _logger;
	[NonSerialized] private TablePresenter? _presenter;
	[NonSerialized] private TableRenderCoordinator? _renderCoordinator;

	[NonSerialized] private bool _runtimeInitialized;
	[NonSerialized] private IServiceProvider? _serviceProvider;
	[NonSerialized] private ITableView? _tableView;

	public MbeTableEditorControl() : base(true)
	{
		InitializeComponent();
	}
}
