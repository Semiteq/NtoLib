using System.Windows.Forms;

namespace NtoLib.Utils
{
	public partial class LabeledValue : UserControl
	{
		public string LabelText
		{
			get => label.Text;
			set => label.Text = value;
		}

		public string ValueText
		{
			get => valueLabel.Text;
			set => valueLabel.Text = value;
		}



		public LabeledValue()
		{
			InitializeComponent();
		}
	}
}
