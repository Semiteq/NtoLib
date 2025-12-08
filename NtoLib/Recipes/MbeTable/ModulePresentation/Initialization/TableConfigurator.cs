using System;
using System.Collections.Generic;
using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModulePresentation.Adapters;
using NtoLib.Recipes.MbeTable.ModulePresentation.Columns;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Initialization;

/// <summary>
/// Creates and styles DataGridView columns according to YAML configuration
/// and applies common grid options (VirtualMode, selection, double-buffer etc.).
/// </summary>
public sealed class TableConfigurator
{
	private readonly DataGridView _grid;
	private readonly IColorSchemeProvider _schemeProvider;
	private readonly IReadOnlyList<ColumnDefinition> _columnDefinitions;
	private readonly FactoryColumnRegistry _registry;

	public TableConfigurator(
		DataGridView grid,
		IColorSchemeProvider schemeProvider,
		IReadOnlyList<ColumnDefinition> columnDefinitions,
		FactoryColumnRegistry registry)
	{
		_grid = grid ?? throw new ArgumentNullException(nameof(grid));
		_schemeProvider = schemeProvider;
		_columnDefinitions = columnDefinitions;
		_registry = registry;
	}

	/// <summary>
	/// Applies every visual/config option and builds columns.
	/// Must be called once during table initialization (Presenter.Initialize()).
	/// </summary>
	public void Configure()
	{
		GridOptionsApplier.Apply(_grid);
		ApplyScheme(_schemeProvider.Current);
		BuildColumns();
	}

	private void BuildColumns()
	{
		_grid.Columns.Clear();

		foreach (var def in _columnDefinitions)
		{
			var column = _registry.CreateColumn(def);
			_grid.Columns.Add(column);
		}
	}

	private void ApplyScheme(ColorScheme scheme)
	{
		_grid.ColumnHeadersDefaultCellStyle.BackColor = scheme.HeaderBgColor;
		_grid.ColumnHeadersDefaultCellStyle.ForeColor = scheme.HeaderTextColor;
		_grid.ColumnHeadersDefaultCellStyle.Font = scheme.HeaderFont;

		_grid.DefaultCellStyle.BackColor = scheme.LineBgColor;
		_grid.DefaultCellStyle.ForeColor = scheme.LineTextColor;
		_grid.DefaultCellStyle.Font = scheme.LineFont;
		_grid.RowTemplate.Height = scheme.LineHeight;

		_grid.BackgroundColor = scheme.TableBackgroundColor;
	}
}
