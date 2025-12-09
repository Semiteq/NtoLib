using System;
using System.Windows.Forms;

namespace NtoLib.InputFields.Helpers;

public partial class TextBoxFloat : TextBox
{
	public event Action ValidatingValue;

	public TextBoxFloat()
	{
		InitializeComponent();
	}

	protected override void OnKeyPress(KeyPressEventArgs e)
	{
		base.OnKeyPress(e);

		if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '+' && e.KeyChar != '-' &&
			e.KeyChar != ',' && e.KeyChar != '.' && e.KeyChar != 'E' && e.KeyChar != 'e')
			e.Handled = true;
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);

		if (e.KeyCode != Keys.Enter)
		{
			return;
		}

		ValidatingValue.Invoke();

		e.SuppressKeyPress = true;
		e.Handled = true;
	}
}
