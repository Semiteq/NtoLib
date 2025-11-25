using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModulePresentation.DataAccess;
using NtoLib.Recipes.MbeTable.ModulePresentation.Models;
using NtoLib.Recipes.MbeTable.ModulePresentation.Rendering;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Cells;

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
			var currentValue = Value;

			comboBox.SelectedIndexChanged -= OnComboBoxSelectedIndexChanged;

			try
			{
				comboBox.DataSource = null;
				comboBox.Items.Clear();

				comboBox.DataSource = items;
				comboBox.DisplayMember = "Value";
				comboBox.ValueMember = "Key";

				if (currentValue != null)
				{
					if (short.TryParse(currentValue.ToString(), out var sv) && items.Any(kv => kv.Key == sv))
						comboBox.SelectedValue = sv;
					else
						comboBox.SelectedIndex = 0;
				}
				else
				{
					comboBox.SelectedIndex = 0;
				}
			}
			finally
			{
				comboBox.SelectedIndexChanged += OnComboBoxSelectedIndexChanged;
			}

			comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
		}
	}

	private void OnComboBoxSelectedIndexChanged(object? sender, EventArgs e)
	{
		if (DataGridView != null && !DataGridView.IsDisposed)
			DataGridView.NotifyCurrentCellDirty(true);
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
		// Always render via custom renderer to avoid default white painting of ComboBox cells.
		if (_renderer != null)
		{
			// Prefer CellVisualState from Tag (set by TableRenderCoordinator).
			// Fallback to cellStyle values if Tag is not yet populated.
			Font font;
			Color fore;
			Color back;

			if (Tag is CellVisualState visual)
			{
				font = visual.Font;
				fore = visual.ForeColor;
				back = visual.BackColor;

				// Keep DisplayStyle in sync in case renderer relies on it visually.
				if (DisplayStyle != visual.ComboDisplayStyle)
					DisplayStyle = visual.ComboDisplayStyle;
			}
			else
			{
				font = cellStyle?.Font ?? DataGridView?.Font ?? Control.DefaultFont;
				fore = cellStyle?.ForeColor.IsEmpty == false ? cellStyle.ForeColor : SystemColors.ControlText;
				back = cellStyle?.BackColor.IsEmpty == false ? cellStyle.BackColor : SystemColors.Window;
			}

			var text = GetDisplayText(value, rowIndex);

			var ctx = new CellRenderContext(
				Graphics: graphics,
				Bounds: cellBounds,
				IsCurrent: ReferenceEquals(DataGridView?.CurrentCell, this),
				Font: font,
				ForeColor: fore,
				BackColor: back,
				FormattedValue: text);

			_renderer.Render(ctx);
			return; // do not call base.Paint to prevent white background
		}

		// Fallback if renderer not injected.
		base.Paint(graphics, clipBounds, cellBounds, rowIndex, elementState,
			value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);
	}

	private string GetDisplayText(object? value, int rowIndex)
	{
		if (value == null)
			return string.Empty;

		if (!short.TryParse(value.ToString(), out var sv))
			return value.ToString() ?? string.Empty;

		if (_itemsProvider != null && OwningColumn != null && rowIndex >= 0)
		{
			var key = new ColumnIdentifier(OwningColumn.Name);
			var items = _itemsProvider.GetItems(rowIndex, key);
			var match = items.FirstOrDefault(kv => kv.Key == sv);
			if (!match.Equals(default(KeyValuePair<short, string>)))
				return match.Value;
		}

		return value.ToString() ?? string.Empty;
	}

	protected override bool SetValue(int rowIndex, object? value)
	{
		if (value == null)
			return base.SetValue(rowIndex, null);

		if (!short.TryParse(value.ToString(), out var sv))
			return base.SetValue(rowIndex, null);

		if (_itemsProvider != null && OwningColumn != null)
		{
			var key = new ColumnIdentifier(OwningColumn.Name);
			var items = _itemsProvider.GetItems(rowIndex, key);
			if (items.Count > 0 && !items.Any(kv => kv.Key == sv))
				return base.SetValue(rowIndex, items[0].Key);
		}

		return base.SetValue(rowIndex, sv);
	}

	public override object? ParseFormattedValue(
		object? formattedValue,
		DataGridViewCellStyle? cellStyle,
		TypeConverter? formattedValueTypeConverter,
		TypeConverter? valueTypeConverter)
	{
		if (formattedValue == null)
			return null;

		if (formattedValue is KeyValuePair<short, string> kvp)
			return kvp.Key;

		var s = formattedValue.ToString();
		if (string.IsNullOrEmpty(s))
			return null;

		if (_itemsProvider != null && OwningColumn != null && RowIndex >= 0)
		{
			var key = new ColumnIdentifier(OwningColumn.Name);
			var items = _itemsProvider.GetItems(RowIndex, key);

			var byText = items.FirstOrDefault(kv => kv.Value == s);
			if (!byText.Equals(default(KeyValuePair<short, string>)))
				return byText.Key;

			if (short.TryParse(s, out var parsed) && items.Any(kv => kv.Key == parsed))
				return parsed;

			return items.Count > 0 ? (short?)items[0].Key : null;
		}

		return short.TryParse(s, out var val) ? val : null;
	}

	protected override object? GetFormattedValue(
		object? value,
		int rowIndex,
		ref DataGridViewCellStyle cellStyle,
		TypeConverter? valueTypeConverter,
		TypeConverter? formattedValueTypeConverter,
		DataGridViewDataErrorContexts context)
	{
		return GetDisplayText(value, rowIndex);
	}
}
