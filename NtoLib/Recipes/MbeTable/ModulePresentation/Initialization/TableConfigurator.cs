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
    private readonly DataGridView           _grid;
    private readonly IColorSchemeProvider   _schemeProvider;
    private readonly IReadOnlyList<ColumnDefinition> _columnDefinitions;
    private readonly IColumnFactoryRegistry _factoryRegistry;

    public TableConfigurator(
        DataGridView           grid,
        IColorSchemeProvider   schemeProvider,
        IReadOnlyList<ColumnDefinition> columnDefinitions,
        IColumnFactoryRegistry factoryRegistry)
    {
        _grid             = grid ?? throw new ArgumentNullException(nameof(grid));
        _schemeProvider   = schemeProvider;
        _columnDefinitions= columnDefinitions;
        _factoryRegistry  = factoryRegistry;
    }

    /// <summary>
    /// Applies every visual/config option and builds columns.
    /// Must be called once during table initialization (Presenter.Initialize()).
    /// </summary>
    public void Configure()
    {
        GridOptionsApplier.Apply(_grid);               // базовые флаги
        ApplyScheme(_schemeProvider.Current);          // цвета/шрифты
        BuildColumns();                                // фабрики
    }

    // ---------------- Internal helpers ----------------

    private void BuildColumns()
    {
        _grid.Columns.Clear();

        foreach (var def in _columnDefinitions)
        {
            var column = _factoryRegistry.CreateColumn(def);
            _grid.Columns.Add(column);
        }
    }

    private void ApplyScheme(ColorScheme scheme)
    {
        // Header
        _grid.ColumnHeadersDefaultCellStyle.BackColor = scheme.HeaderBgColor;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = scheme.HeaderTextColor;
        _grid.ColumnHeadersDefaultCellStyle.Font      = scheme.HeaderFont;

        // Lines
        _grid.DefaultCellStyle.BackColor = scheme.LineBgColor;
        _grid.DefaultCellStyle.ForeColor = scheme.LineTextColor;
        _grid.DefaultCellStyle.Font      = scheme.LineFont;
        _grid.RowTemplate.Height         = scheme.LineHeight;

        // Control-level
        _grid.BackgroundColor = scheme.TableBackgroundColor;
    }
}