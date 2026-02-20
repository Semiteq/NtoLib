using System.Windows.Forms;

namespace NtoLib.Devices.Valves.Settings;

public partial class SettingsForm : Form
{
	private readonly ValveControl _valveControl;

	public SettingsForm(ValveControl valveControl)
	{
		_valveControl = valveControl;

		InitializeComponent();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		var status = _valveControl.Status;

		openedLamp.Active = status.Opened;
		closedLamp.Active = status.Closed;

		blockOpeningLamp.Active = status.BlockOpening;
		blockClosingLamp.Active = status.BlockClosing;
		forceCloseLamp.Active = status.ForceClose;

		noConnectionLamp.Visible = !status.ConnectionOk;
		notOpenedLamp.Visible = status.NotOpened;
		notClosedLamp.Visible = status.NotClosed;
		unknownStateLamp.Visible = status.UnknownState;
		collisionLamp.Visible = status.Collision;

		base.OnPaint(e);
	}
}
