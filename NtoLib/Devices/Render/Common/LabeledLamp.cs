using System.Drawing;
using System.Windows.Forms;

namespace NtoLib.Devices.Render.Common;

public partial class LabeledLamp : UserControl
{
	public LabeledLamp()
	{
		InitializeComponent();
	}

	public Color ActiveColor
	{
		get => lamp1.ActiveColor;
		set => lamp1.ActiveColor = value;
	}

	public string LabelText
	{
		get => label1.Text;
		set => label1.Text = value;
	}

	public bool Active
	{
		get => lamp1.Active;
		set => lamp1.Active = value;
	}
}
