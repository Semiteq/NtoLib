#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Errors;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Presentation.Context;
using NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Initialization;

/// <summary>
/// Consolidates all DataGridView table initialization logic into a single responsible class.
/// Configures headers, columns, rows, styles, and behavior settings.
/// </summary>
public sealed class TableInitializer
{
    private readonly DataGridView _table;
    private readonly IColorSchemeProvider _colorSchemeProvider;
    private readonly TableColumns _tableColumns;
    private readonly PropertyDefinitionRegistry _propertyDefinitionRegistry;
    private readonly IComboboxDataProvider _comboboxDataProvider;
    private readonly IComboBoxContext _comboBoxContext;
    private readonly ILogger _logger;

    private readonly Dictionary<string, Func<BaseColumnFactory>> _factoryCreators;

    public TableInitializer(
        DataGridView table,
        IColorSchemeProvider colorSchemeProvider,
        TableColumns tableColumns,
        PropertyDefinitionRegistry propertyDefinitionRegistry,
        IComboboxDataProvider comboboxDataProvider,
        IComboBoxContext comboBoxContext,
        ILogger logger)
    {
        _table = table ?? throw new ArgumentNullException(nameof(table));
        _colorSchemeProvider = colorSchemeProvider ?? throw new ArgumentNullException(nameof(colorSchemeProvider));
        _tableColumns = tableColumns ?? throw new ArgumentNullException(nameof(tableColumns));
        _propertyDefinitionRegistry = propertyDefinitionRegistry ?? throw new ArgumentNullException(nameof(propertyDefinitionRegistry));
        _comboboxDataProvider = comboboxDataProvider ?? throw new ArgumentNullException(nameof(comboboxDataProvider));
        _comboBoxContext = comboBoxContext ?? throw new ArgumentNullException(nameof(comboBoxContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _factoryCreators = new Dictionary<string, Func<BaseColumnFactory>>(StringComparer.OrdinalIgnoreCase)
        {
            { "action_combo_box", () => new ActionComboBoxColumnFactory(_comboBoxContext, _comboboxDataProvider) },
            { "action_target_combo_box", () => new ActionTargetComboBoxColumnFactory(_comboBoxContext) },
            { "property_field", () => new PropertyColumnFactory(_comboBoxContext, _propertyDefinitionRegistry) },
            { "step_start_time_field", () => new StepStartTimeColumnFactory(_comboBoxContext) },
            { "text_field", () => new TextBoxColumnFactory(_comboBoxContext) }
        };
    }

    /// <summary>
    /// Initializes the entire DataGridView table with all required settings.
    /// Call this once after table control is created.
    /// </summary>
    public void InitializeTable()
    {
        _logger.Log("Starting table initialization");

        try
        {
            ConfigureBasicBehavior();
            ConfigureHeaders();
            ConfigureColumns();
            ConfigureRows();
            ConfigureStyles();

            _logger.Log("Table initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, new { errorCode = RecipeErrorCodes.TableInitializationFailed });
            throw;
        }
    }

    private void ConfigureBasicBehavior()
    {
        _table.VirtualMode = true;
        _table.AutoGenerateColumns = false;
        _table.AllowUserToAddRows = false;
        _table.AllowUserToDeleteRows = false;
        _table.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _table.MultiSelect = false;
        _table.EditMode = DataGridViewEditMode.EditOnEnter;
        _table.EnableHeadersVisualStyles = false;

        EnableDoubleBuffering();
    }

    private void ConfigureHeaders()
    {
        var colorScheme = _colorSchemeProvider.Current;

        _table.ColumnHeadersDefaultCellStyle.Font = colorScheme.HeaderFont;
        _table.ColumnHeadersDefaultCellStyle.BackColor = colorScheme.HeaderBgColor;
        _table.ColumnHeadersDefaultCellStyle.ForeColor = colorScheme.HeaderTextColor;
        _table.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

        _table.RowHeadersVisible = true;
        _table.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _table.RowHeadersDefaultCellStyle.Font = colorScheme.HeaderFont;
        _table.RowHeadersDefaultCellStyle.BackColor = colorScheme.HeaderBgColor;
        _table.RowHeadersDefaultCellStyle.ForeColor = colorScheme.HeaderTextColor;
        _table.RowHeadersWidth = 50;
        _table.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
    }

    private void ConfigureColumns()
    {
        _table.Columns.Clear();

        var colorScheme = _colorSchemeProvider.Current;

        foreach (var columnDefinition in _tableColumns.GetColumns())
        {
            if (!_factoryCreators.TryGetValue(columnDefinition.ColumnType, out var factoryCreator))
            {
                var error = new RecipeError(
                    $"Unknown column type '{columnDefinition.ColumnType}' for column '{columnDefinition.Key.Value}'",
                    RecipeErrorCodes.ConfigInvalidSchema);
                _logger.LogError(error);
                throw new InvalidOperationException(error.Message);
            }

            var factory = factoryCreator();
            var column = factory.CreateColumn(columnDefinition, colorScheme);
            column.DataPropertyName = columnDefinition.Key.Value;

            _table.Columns.Add(column);
        }

        _table.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
    }

    private void ConfigureRows()
    {
        var colorScheme = _colorSchemeProvider.Current;

        _table.Rows.Clear();
        _table.RowTemplate.Height = colorScheme.LineHeight;
    }

    private void ConfigureStyles()
    {
        var colorScheme = _colorSchemeProvider.Current;

        _table.BackgroundColor = colorScheme.TableBackgroundColor;

        EqualizeSelectionColors(_table.DefaultCellStyle);
        EqualizeSelectionColors(_table.RowsDefaultCellStyle);
        EqualizeSelectionColors(_table.ColumnHeadersDefaultCellStyle);
        EqualizeSelectionColors(_table.RowHeadersDefaultCellStyle);
    }

    private static void EqualizeSelectionColors(DataGridViewCellStyle style)
    {
        style.SelectionBackColor = style.BackColor;
        style.SelectionForeColor = style.ForeColor;
    }

    private void EnableDoubleBuffering()
    {
        typeof(DataGridView).InvokeMember(
            "DoubleBuffered",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
            null,
            _table,
            new object[] { true });
    }
}