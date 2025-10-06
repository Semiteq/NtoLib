using System.Drawing;
using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.Config.Domain.Columns;
using NtoLib.Recipes.MbeTable.Presentation.DataAccess;
using NtoLib.Recipes.MbeTable.Presentation.Models;
using NtoLib.Recipes.MbeTable.Presentation.Rendering;

namespace NtoLib.Recipes.MbeTable.Presentation.Cells;

public sealed class RecipeComboBoxCell : DataGridViewComboBoxCell
{
    private IComboBoxItemsProvider? _itemsProvider;
    private ICellRenderer? _renderer;

    public RecipeComboBoxCell()
    {
        FlatStyle = FlatStyle.Flat;
        ValueType = typeof(short?);
        DisplayMember = "Value";
        ValueMember = "Key";
        DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
        DisplayStyleForCurrentCellOnly = true;
    }

    public void SetItemsProvider(IComboBoxItemsProvider itemsProvider) => _itemsProvider = itemsProvider;
    public void SetRenderer(ICellRenderer renderer) => _renderer = renderer;

    public override object Clone()
    {
        var clone = (RecipeComboBoxCell)base.Clone();
        clone._itemsProvider = _itemsProvider;
        clone._renderer = _renderer;
        return clone;
    }

    public override void InitializeEditingControl(
        int rowIndex,
        object? formattedValue,
        DataGridViewCellStyle cellStyle)
    {
        base.InitializeEditingControl(rowIndex, formattedValue, cellStyle);

        if (_itemsProvider == null || OwningColumn == null || DataGridView?.EditingControl is not ComboBox comboBox)
            return;

        var key = new ColumnIdentifier(OwningColumn.Name);
        var items = _itemsProvider.GetItems(rowIndex, key);

        if (items.Count > 0)
        {
            comboBox.DataSource = items;
            comboBox.DisplayMember = "Value";
            comboBox.ValueMember = "Key";
        }
    }

    protected override void Paint(
        Graphics graphics,
        Rectangle clipBounds,
        Rectangle cellBounds,
        int rowIndex,
        DataGridViewElementStates elementState,
        object? value,
        object? formattedValue,
        string? errorText,
        DataGridViewCellStyle cellStyle,
        DataGridViewAdvancedBorderStyle advancedBorderStyle,
        DataGridViewPaintParts paintParts)
    {
        // Use renderer only when visual is provided by coordinator.
        if (_renderer is null || Tag is not CellVisualState visual)
        {
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, elementState,
                value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);
            return;
        }

        var ctx = new CellRenderContext(
            Graphics: graphics,
            Bounds: cellBounds,
            IsCurrent: ReferenceEquals(DataGridView?.CurrentCell, this),
            Font: visual.Font,
            ForeColor: visual.ForeColor,
            BackColor: visual.BackColor,
            FormattedValue: formattedValue);

        _renderer.Render(ctx);
    }
}