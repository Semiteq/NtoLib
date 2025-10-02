#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Presentation.Context;
using NtoLib.Recipes.MbeTable.Presentation.DataSource;
using NtoLib.Recipes.MbeTable.Presentation.Table.Editing;
using NtoLib.Recipes.MbeTable.Presentation.Table.State;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Cells;

/// <summary>
/// ComboBox cell supporting dynamic or static data strategies in VirtualMode.
/// Paint is state-aware: Execution state (Current / Passed) overrides Disabled.
/// Disabled (no property / dynamic null) renders without editor visuals.
/// </summary>
public sealed class RecipeComboBoxCell : DataGridViewComboBoxCell
{
    private IComboBoxContext? _context;
    private IComboBoxDataSourceStrategy? _strategy;
    private ColorScheme? _colorScheme;
    private IRowExecutionStateProvider? _rowStateProvider;

    public RecipeComboBoxCell()
    {
        FlatStyle = FlatStyle.Flat;
        ValueType = typeof(int?);
        DisplayMember = "Value";
        ValueMember = "Key";
        DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
        DisplayStyleForCurrentCellOnly = true;
    }

    public bool IsInitialized =>
        _context != null &&
        _strategy != null &&
        _colorScheme != null &&
        _rowStateProvider != null;

    public void Initialize(
        IComboBoxContext context,
        IComboBoxDataSourceStrategy strategy,
        ColorScheme colorScheme,
        IRowExecutionStateProvider rowStateProvider)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        _colorScheme = colorScheme ?? throw new ArgumentNullException(nameof(colorScheme));
        _rowStateProvider = rowStateProvider ?? throw new ArgumentNullException(nameof(rowStateProvider));
    }

    public override Type EditType => typeof(BaseRecipeComboBoxEditingControl);

    public override object Clone()
    {
        var copy = (RecipeComboBoxCell)base.Clone();
        copy._context = _context;
        copy._strategy = _strategy;
        copy._colorScheme = _colorScheme;
        copy._rowStateProvider = _rowStateProvider;
        return copy;
    }

    public override void InitializeEditingControl(
        int rowIndex,
        object? formattedValue,
        DataGridViewCellStyle cellStyle)
    {
        if (!IsInitialized)
            return;

        // Do not enter edit mode for disabled cells (property not applicable)
        if (IsDisabledForRow(rowIndex))
            return;

        var viewModel = GetRowViewModel(rowIndex);
        if (viewModel == null)
            return;

        var columnKey = GetCurrentColumnKey();
        List<KeyValuePair<int, string>>? dynamicItems;
        try
        {
            dynamicItems = _strategy!.GetItems(viewModel, columnKey);
        }
        catch
        {
            return;
        }

        if (dynamicItems == null)
            return;

        base.InitializeEditingControl(rowIndex, formattedValue, cellStyle);

        if (DataGridView?.EditingControl is not BaseRecipeComboBoxEditingControl editingControl)
            return;

        if (dynamicItems.Count > 0)
        {
            DataSource = dynamicItems;
            editingControl.DataSource = dynamicItems;
        }
        else
        {
            var columnDataSource = ((DataGridViewComboBoxColumn)OwningColumn).DataSource;
            DataSource = columnDataSource;
            editingControl.DataSource = columnDataSource;
        }

        editingControl.DisplayMember = DisplayMember;
        editingControl.ValueMember = ValueMember;
        editingControl.DropDownStyle = ComboBoxStyle.DropDownList;
        editingControl.FlatStyle = FlatStyle.Flat;

        var key = CoerceToNullableInt(Value);
        if (key.HasValue)
            editingControl.SelectedValue = key.Value;
        else
            editingControl.SelectedIndex = -1;

        editingControl.ApplyStyleFromCurrentCell();
    }

    protected override object? GetFormattedValue(
        object? value,
        int rowIndex,
        ref DataGridViewCellStyle cellStyle,
        TypeConverter? valueTypeConverter,
        TypeConverter? formattedValueTypeConverter,
        DataGridViewDataErrorContexts context)
    {
        if (!IsInitialized)
            return string.Empty;

        var vm = GetRowViewModel(rowIndex);
        if (vm == null)
            return string.Empty;

        var columnKey = GetCurrentColumnKey();
        List<KeyValuePair<int, string>>? dynamicItems;
        try
        {
            dynamicItems = _strategy!.GetItems(vm, columnKey);
        }
        catch
        {
            return string.Empty;
        }

        if (dynamicItems == null)
            return string.Empty;

        var key = CoerceToNullableInt(value);
        if (!key.HasValue)
            return string.Empty;

        if (dynamicItems.Count > 0)
        {
            var display = dynamicItems.FirstOrDefault(p => p.Key == key.Value).Value;
            if (!string.IsNullOrEmpty(display))
                return display;
        }

        if (OwningColumn is DataGridViewComboBoxColumn col &&
            col.DataSource is IEnumerable<KeyValuePair<int, string>> columnList)
        {
            var display = columnList.FirstOrDefault(p => p.Key == key.Value).Value;
            return display ?? string.Empty;
        }

        return base.GetFormattedValue(
            value,
            rowIndex,
            ref cellStyle,
            valueTypeConverter,
            formattedValueTypeConverter,
            context) ?? string.Empty;
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
        if (!IsInitialized)
        {
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, elementState,
                value, formattedValue, errorText ?? string.Empty,
                cellStyle, advancedBorderStyle, paintParts);
            return;
        }

        var rowState = _rowStateProvider!.GetState(rowIndex);
        var isExecuting = rowState == RowExecutionState.Current || rowState == RowExecutionState.Passed;
        var isDisabled = IsDisabledForRow(rowIndex);

        if (isExecuting)
        {
            PaintExecutionBg(graphics, cellBounds, rowState);

            if (!isDisabled)
            {
                DrawCellText(graphics, cellBounds, formattedValue as string,
                    GetExecutionTextColor(rowState, cellStyle), cellStyle);
            }

            DrawGridLines(graphics, cellBounds);
            DrawFocusBox(graphics, cellBounds);
            return;
        }

        if (isDisabled)
        {
            PaintDisabledBg(graphics, cellBounds);
            DrawGridLines(graphics, cellBounds);
            DrawFocusBox(graphics, cellBounds);
            return;
        }

        base.Paint(
            graphics,
            clipBounds,
            cellBounds,
            rowIndex,
            elementState,
            value,
            formattedValue,
            errorText ?? string.Empty,
            cellStyle,
            advancedBorderStyle,
            paintParts);

        DrawFocusBox(graphics, cellBounds);
    }

    private void PaintDisabledBg(Graphics g, Rectangle bounds)
    {
        var scheme = _colorScheme!;
        using var back = new SolidBrush(scheme.BlockedBgColor);
        g.FillRectangle(back, bounds);
    }

    private void PaintExecutionBg(Graphics g, Rectangle bounds, RowExecutionState state)
    {
        var scheme = _colorScheme!;
        Color fill = state switch
        {
            RowExecutionState.Current => scheme.SelectedLineBgColor,
            RowExecutionState.Passed  => scheme.PassedLineBgColor,
            _                         => scheme.LineBgColor
        };
        using var back = new SolidBrush(fill);
        g.FillRectangle(back, bounds);
    }
    
    private Color GetExecutionTextColor(RowExecutionState state, DataGridViewCellStyle style)
    {
        var scheme = _colorScheme!;
        return state switch
        {
            RowExecutionState.Current => scheme.SelectedLineTextColor,
            RowExecutionState.Passed  => scheme.PassedLineTextColor,
            _                         => style.ForeColor
        };
    }

    private void DrawCellText(Graphics g, Rectangle bounds, string? text, Color foreColor, DataGridViewCellStyle style)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var rect = new Rectangle(bounds.X + 3, bounds.Y + 2, bounds.Width - 6, bounds.Height - 4);

        var flags = TextFormatFlags.EndEllipsis | TextFormatFlags.PreserveGraphicsClipping | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter;

        switch (style.Alignment)
        {
            case DataGridViewContentAlignment.MiddleCenter:
            case DataGridViewContentAlignment.TopCenter:
            case DataGridViewContentAlignment.BottomCenter:
                flags |= TextFormatFlags.HorizontalCenter;
                break;
            case DataGridViewContentAlignment.MiddleRight:
            case DataGridViewContentAlignment.TopRight:
            case DataGridViewContentAlignment.BottomRight:
                flags |= TextFormatFlags.Right;
                break;
        }

        TextRenderer.DrawText(
            g,
            text,
            style.Font ?? DataGridView?.Font ?? SystemFonts.DefaultFont,
            rect,
            foreColor,
            flags);
    }
    
    private void DrawGridLines(Graphics g, Rectangle bounds)
    {
        if (DataGridView == null) return;
        using var pen = new Pen(DataGridView.GridColor, 1);
        g.DrawLine(pen, bounds.Right - 1, bounds.Top, bounds.Right - 1, bounds.Bottom - 1);
        g.DrawLine(pen, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);
    }

    private void DrawFocusBox(Graphics g, Rectangle bounds)
    {
        if (DataGridView?.CurrentCell != this || _colorScheme == null)
            return;

        using var pen = new Pen(_colorScheme.SelectedOutlineColor,
            Math.Max(1, _colorScheme.SelectedOutlineThickness));
        var rect = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
        g.DrawRectangle(pen, rect);
    }

    private bool IsDisabledForRow(int rowIndex)
    {
        var vm = GetRowViewModel(rowIndex);
        if (vm == null)
            return true;

        var key = GetCurrentColumnKey();
        var prop = vm.GetProperty(key);
        if (prop == null)
            return true;

        if (_strategy is RowDynamicDataSource)
        {
            try
            {
                var items = _strategy.GetItems(vm, key);
                if (items == null)
                    return true;
            }
            catch
            {
                return true;
            }
        }

        return false;
    }

    private int? CoerceToNullableInt(object? raw)
    {
        switch (raw)
        {
            case int i:
                return i;
            case IConvertible convertible:
                try { return convertible.ToInt32(CultureInfo.InvariantCulture); }
                catch { return null; }
            default:
                return null;
        }
    }

    private StepViewModel? GetRowViewModel(int rowIndex)
    {
        if (rowIndex < 0 || DataGridView == null)
            return null;

        var count = _context?.RecipeViewModel.ViewModels.Count ?? 0;
        if (rowIndex >= count)
            return null;

        return _context!.RecipeViewModel.ViewModels[rowIndex];
    }

    private ColumnIdentifier GetCurrentColumnKey()
    {
        if (DataGridView == null || ColumnIndex < 0)
            return new ColumnIdentifier(string.Empty);

        return new ColumnIdentifier(DataGridView.Columns[ColumnIndex].Name);
    }
}