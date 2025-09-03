#nullable enable

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns;

/// <summary>
/// Manages the creation and configuration of DataGridView columns based on a table schema.
/// It acts as an orchestrator, selecting the appropriate column factory based on the
/// 'ColumnType' specified in the schema definition.
/// </summary>
public class TableColumnManager
{
    private readonly DataGridView _table;
    private readonly TableSchema _tableSchema;
    private readonly ColorScheme _colorScheme;
    private readonly IReadOnlyDictionary<string, IColumnFactory> _factories;

    public TableColumnManager(DataGridView table,
        TableSchema tableSchema,
        ColorScheme colorScheme,
        IComboboxDataProvider dataProvider)
    {
        _table = table ?? throw new ArgumentNullException(nameof(table));
        _tableSchema = tableSchema ?? throw new ArgumentNullException(nameof(tableSchema));
        _colorScheme = colorScheme ?? throw new ArgumentNullException(nameof(colorScheme));


        _factories = new Dictionary<string, IColumnFactory>(StringComparer.OrdinalIgnoreCase)
        {
            { "ActionComboBox",         new ActionComboBoxColumnFactory(dataProvider) },
            { "ActionTargetComboBox",   new ActionTargetComboBoxColumnFactory() },
            { "PropertyField",          new PropertyColumnFactory() },
            { "StepStartTimeField",     new StepStartTimeColumnFactory() },
            { "TextField",              new TextBoxColumnFactory() }
        };
    }

    /// <summary>
    /// Initializes the visual style of the table headers.
    /// </summary>
    public void InitializeHeaders()
    {
        _table.ColumnHeadersDefaultCellStyle.Font = _colorScheme.HeaderFont;
        _table.ColumnHeadersDefaultCellStyle.BackColor = _colorScheme.HeaderBgColor;
        _table.ColumnHeadersDefaultCellStyle.ForeColor = _colorScheme.HeaderTextColor;
        _table.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

        _table.RowHeadersVisible = true;
        _table.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _table.RowHeadersDefaultCellStyle.Font = _colorScheme.HeaderFont;
        _table.RowHeadersDefaultCellStyle.BackColor = _colorScheme.HeaderBgColor;
        _table.RowHeadersDefaultCellStyle.ForeColor = _colorScheme.HeaderTextColor;

        _table.EnableHeadersVisualStyles = false;
    }

    /// <summary>
    /// Clears existing columns and generates new ones based on the table schema.
    /// </summary>
    public void InitializeTableColumns()
    {
        _table.AutoGenerateColumns = false;
        _table.Columns.Clear();
        var defaultFactory = _factories["PropertyField"];
        foreach (var colDef in _tableSchema.GetColumns())
        {
            _factories.TryGetValue(colDef.ColumnType, out var factory);
            factory ??= defaultFactory;
            var column = factory.CreateColumn(colDef, _colorScheme);
            column.DataPropertyName = colDef.Key.Value;
            _table.Columns.Add(column);
        }

        _table.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        _table.AllowUserToAddRows = false;
        _table.AllowUserToDeleteRows = false;
        _table.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _table.MultiSelect = false;
    }

    /// <summary>
    /// Clears all rows and sets the template height.
    /// </summary>
    public void InitializeTableRows()
    {
        _table.Rows.Clear();
        _table.RowTemplate.Height = _colorScheme.LineFont.Height + 8;
    }
}