using System;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Behavior;

internal sealed class TableEditingControlBehavior : ITableGridBehavior
{
	private readonly DataGridView _table;
	private bool _attached;

	public TableEditingControlBehavior(DataGridView table)
	{
		_table = table ?? throw new ArgumentNullException(nameof(table));
	}

	public void Attach()
	{
		if (_attached)
		{
			return;
		}

		_table.EditingControlShowing += OnEditingControlShowing;
		_attached = true;
	}

	public void Detach()
	{
		if (!_attached)
		{
			return;
		}

		try
		{
			_table.EditingControlShowing -= OnEditingControlShowing;
		}
		catch
		{
			// ignored
		}

		_attached = false;
	}

	public void Dispose()
	{
		Detach();
	}

	private void OnEditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
	{
		if (_table.CurrentCell is null)
		{
			return;
		}

		switch (e.Control)
		{
			case DataGridViewTextBoxEditingControl textBox:
			{
				var style = _table.CurrentCell.InheritedStyle;
				try
				{
					textBox.BackColor = style.BackColor;
					textBox.ForeColor = style.ForeColor;
					textBox.Font = style.Font;
				}
				catch
				{
					// ignored
				}

				break;
			}
			case ComboBox comboBox:
				comboBox.DropDownStyle = ComboBoxStyle.DropDownList;

				comboBox.DropDown -= OnComboBoxDropDownAdjustSize;
				comboBox.DropDown += OnComboBoxDropDownAdjustSize;

				break;
		}
	}

	private void OnComboBoxDropDownAdjustSize(object? sender, EventArgs e)
	{
		if (sender is not ComboBox comboBox)
		{
			return;
		}

		var desired = comboBox.MaxDropDownItems;
		try
		{
			if (_table.CurrentCell is not null &&
				_table.Columns[_table.CurrentCell.ColumnIndex] is DataGridViewComboBoxColumn col &&
				col.MaxDropDownItems > 0)
			{
				desired = col.MaxDropDownItems;
			}
		}
		catch
		{
			// ignored
		}

		var visible = Math.Max(1, Math.Min(desired, comboBox.Items.Count));
		comboBox.IntegralHeight = true;
		comboBox.MaxDropDownItems = visible;
	}
}
