using System;
using System.Windows.Forms;

namespace NtoLib.NumericBox.Helpers;

public partial class NumericBox : TextBox
{
	public NumericBox()
	{
		InitializeComponent();
	}

	public event Action? ValidatingValue;

	protected override void OnKeyPress(KeyPressEventArgs e)
	{
		base.OnKeyPress(e);

		if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '+' && e.KeyChar != '-' &&
			e.KeyChar != ',' && e.KeyChar != '.' && e.KeyChar != 'E' && e.KeyChar != 'e')
		{
			e.Handled = true;
		}
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);

		if (e.KeyCode != Keys.Enter)
		{
			return;
		}

		ValidatingValue?.Invoke();

		e.SuppressKeyPress = true;
		e.Handled = true;
	}
}
