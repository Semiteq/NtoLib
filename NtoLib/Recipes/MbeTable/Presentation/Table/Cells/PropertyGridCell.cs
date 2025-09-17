#nullable enable

using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Cells;

/// <summary>
/// A custom DataGridView cell that interacts directly with a StepProperty
/// to provide advanced parsing and formatting of cell values.
/// </summary>
public class PropertyGridCell : DataGridViewTextBoxCell
{
    /// <summary>
    /// Overrides the painting logic to ensure consistent background color,
    /// especially when the cell is selected.
    /// </summary>
    protected override void Paint(
        Graphics graphics,
        Rectangle clipBounds,
        Rectangle cellBounds,
        int rowIndex,
        DataGridViewElementStates elementState,
        object value,
        object formattedValue,
        string errorText,
        DataGridViewCellStyle cellStyle,
        DataGridViewAdvancedBorderStyle advancedBorderStyle,
        DataGridViewPaintParts paintParts)
    {
        // Force non-selection background to avoid the default blue highlight.
        paintParts &= ~DataGridViewPaintParts.SelectionBackground;
        base.Paint(graphics, clipBounds, cellBounds, rowIndex, elementState, value, formattedValue, errorText,
            cellStyle, advancedBorderStyle, paintParts);
    }

    /// <summary>
    /// Converts a cell's display value into the actual underlying value for the data source.
    /// This is where user input is parsed using the domain logic.
    /// </summary>
    public override object? ParseFormattedValue(
        object? formattedValue,
        DataGridViewCellStyle cellStyle,
        TypeConverter? formattedValueTypeConverter,
        TypeConverter? valueTypeConverter)
    {
        if (formattedValue == null || DataGridView == null)
        {
            return base.ParseFormattedValue(formattedValue, cellStyle, formattedValueTypeConverter, valueTypeConverter);
        }

        var property = GetStepProperty();
        if (property == null)
        {
            // If there's no property (e.g., cell is disabled), do not attempt to parse.
            return base.ParseFormattedValue(formattedValue, cellStyle, formattedValueTypeConverter, valueTypeConverter);
        }

        // Use the domain model's "smart" parser.
        var result = property.WithValue(formattedValue);

        if (result.IsSuccess)
        {
            // If parsing is successful, return the raw, typed value (e.g., a float)
            // that the data source expects.
            return result.Value.GetValueAsObject();
        }

        // If parsing fails, throw a FormatException.
        // The DataGridView will catch this and raise the DataError event,
        // which we will handle centrally in TableBehaviorManager.
        throw new FormatException(result.Errors.First().Message);
    }

    /// <summary>
    /// Converts the underlying data source value into a display-friendly format.
    /// This is where the formatted string (e.g., with units) is generated.
    /// </summary>
    protected override object? GetFormattedValue(
        object? value,
        int rowIndex,
        ref DataGridViewCellStyle cellStyle,
        TypeConverter? valueTypeConverter,
        TypeConverter? formattedValueTypeConverter,
        DataGridViewDataErrorContexts context)
    {
        var property = GetStepProperty();
        if (property != null)
        {
            // Use the domain model's "smart" formatter.
            return property.GetDisplayValue();
        }

        return base.GetFormattedValue(value, rowIndex, ref cellStyle, valueTypeConverter, formattedValueTypeConverter,
            context);
    }

    /// <summary>
    /// A helper method to retrieve the StepProperty associated with this cell.
    /// </summary>
    private StepProperty? GetStepProperty()
    {
        if (DataGridView == null || RowIndex < 0 || ColumnIndex < 0)
        {
            return null;
        }

        if (DataGridView.Rows[RowIndex].DataBoundItem is not StepViewModel viewModel)
        {
            return null;
        }

        // Find the column key based on the cell's column index.
        var columnKey = new ColumnIdentifier(DataGridView.Columns[ColumnIndex].Name);
        return viewModel.GetProperty(columnKey);
    }
}