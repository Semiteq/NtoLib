using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using FB.VisualFB;

using InSAT.Library.Gui;
using InSAT.Library.Interop.Win32;

using NtoLib.Devices.Pumps.Settings;
using NtoLib.Devices.Render.Common;
using NtoLib.Devices.Render.Pumps;

using Orientation = NtoLib.Devices.Render.Common.Orientation;

namespace NtoLib.Devices.Pumps;

[ComVisible(true)]
[Guid("664141D1-44B3-43D7-9897-3A10C936315A")]
[DisplayName("Насос")]
public partial class PumpControl : VisualControlBase
{
	private bool _noButtons;

	// See the https://github.com/Semiteq/NtoLib/issues/80
	public override Color BackColor
	{
		get => base.BackColor;
		set
		{
			if (value == Color.Transparent)
				return;
			base.BackColor = value;
		}
	}

	[DisplayName("Скрыть кнопки")]
	public bool NoButtons
	{
		get => _noButtons;
		set
		{
			var updateRequired = _noButtons != value;
			_noButtons = value;
			if (updateRequired)
			{
				UpdateLayout();
			}
		}
	}

	private Orientation _orientation;

	[DisplayName("Ориентация")]
	public Orientation Orientation
	{
		get => _orientation;
		set
		{
			var updateRequired = _orientation != value;
			_orientation = value;
			if (updateRequired)
			{
				UpdateLayout();
			}
		}
	}

	private ButtonOrientation _buttonOrientation;

	[DisplayName("Ориентация кнопок")]
	public ButtonOrientation ButtonOrientation
	{
		get => _buttonOrientation;
		set
		{
			var updateRequired = _buttonOrientation != value;
			_buttonOrientation = value;
			if (updateRequired)
			{
				UpdateLayout();
			}
		}
	}

	internal Status Status;

	private PumpRenderer? _renderer;

	private PumpSettingForm? _settingsForm;

	private Timer? _impulseTimer;
	private int _currentCommand;

	private Timer? _mouseHoldTimer;

	private Timer? _animationTimer;
	private bool _animationClocker;

	private Timer? _redrawTimer;


	public PumpControl()
	{
		SetStyle(ControlStyles.UserPaint, true);
		SetStyle(ControlStyles.AllPaintingInWmPaint, true);
		SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
		UpdateStyles();

		InitializeComponent();

		_renderer = new PumpRenderer(this);
	}


	protected override void ToRuntime()
	{
		base.ToRuntime();
		UpdateLayout();

		_impulseTimer = new Timer();
		_impulseTimer.Interval = 500;
		_impulseTimer.Tick += DisableCommandImpulse;

		_animationTimer = new Timer();
		_animationTimer.Interval = 500;
		_animationTimer.Tick += UpdateAnimation;

		_mouseHoldTimer = new Timer();
		_mouseHoldTimer.Interval = 200;
		_mouseHoldTimer.Tick += HandleMouseHoldDown;

		// Windows may suppress OnPaint event if the control is not visible
		// or not in focus. This timer will force the control to redraw.
		_redrawTimer = new Timer();
		_redrawTimer.Interval = 50;
		_redrawTimer.Tick += (s, e) => Invalidate();
		_redrawTimer.Start();
	}

	protected override void ToDesign()
	{
		base.ToDesign();
		UpdateLayout();

		_settingsForm?.Close();

		_impulseTimer?.Dispose();
		_animationTimer?.Dispose();
		_mouseHoldTimer?.Dispose();
		_redrawTimer?.Dispose();
	}

	private void HandleResize(object sender, EventArgs e)
	{
		UpdateLayout();
	}


	private void HandleStartClick(object sender, EventArgs e)
	{
		if (!Status.UsedByAutoMode && !Status.BlockStart)
		{
			SendCommand(PumpFB.StartCmdId);
		}
	}

	private void HandleStopClick(object sender, EventArgs e)
	{
		if (!Status.UsedByAutoMode && !Status.BlockStop)
		{
			SendCommand(PumpFB.StopCmdId);
		}
	}


	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.Clear(BackColor);

		if (!FBConnector.DesignMode)
			UpdateStatus();

		UpdateSprite();
	}

	protected override void OnPinReceive(int pinID, bool valueChanged)
	{
		base.OnPinReceive(pinID, valueChanged);

		if (pinID != PumpFB.UsedId + 1000)
			return;

		void Apply()
		{
			var used = FBConnector.GetPinValue<bool>(pinID);
			if (Visible != used)
				Visible = used;
			if (used)
				Invalidate();
		}

		if (InvokeRequired)
			BeginInvoke((Action)Apply);
		else
			Apply();
	}

	private void UpdateAnimation(object sender, EventArgs e)
	{
		_animationClocker = !_animationClocker;
		UpdateSprite();
	}

	private void UpdateLayout()
	{
		var layout = LayoutBuilder.BuildLayout(this, NoButtons);
		spriteBox.Bounds = layout.DeviceRectangle;
		buttonTable.Bounds = layout.ButtonTableRectangle;

		UpdateButtonTable();
		UpdateSprite();
	}

	private void UpdateButtonTable()
	{
		Orientation buttonsOrientation;
		if (!IsHorizontal())
		{
			buttonsOrientation = ButtonOrientation == ButtonOrientation.LeftTop
				? Orientation.Top
				: Orientation.Bottom;
		}
		else
		{
			buttonsOrientation = ButtonOrientation == ButtonOrientation.LeftTop
				? Orientation.Left
				: Orientation.Right;
		}

		var buttons = new Button[] { buttonStart, buttonStop };

		LayoutBuilder.RebuildTable(buttonTable, buttonsOrientation, buttons);
	}

	private void UpdateSprite()
	{
		_renderer ??= new PumpRenderer(this);

		spriteBox.Image = new Bitmap(Math.Max(1, spriteBox.Width), Math.Max(1, spriteBox.Height));

		using var g = Graphics.FromImage(spriteBox.Image);
		g.Clear(BackColor);

		var unit = GraphicsUnit.Point;
		_renderer.Draw(g, spriteBox.Image.GetBounds(ref unit), Orientation, _animationClocker);
	}

	public bool IsHorizontal()
	{
		return Bounds.Width >= Bounds.Height;
	}


	private void HandleMouseDown(object sender, MouseEventArgs e)
	{
		if (e.Button != MouseButtons.Right)
		{
			_mouseHoldTimer?.Stop();
			_settingsForm?.Close();
			return;
		}

		_mouseHoldTimer?.Start();
	}

	private void HandleMouseHoldDown(object sender, EventArgs e)
	{
		OpenSettingsForm();
		_mouseHoldTimer?.Stop();
	}

	private void HandleMouseUp(object sender, MouseEventArgs e)
	{
		if (e.Button != MouseButtons.Right)
			return;

		_mouseHoldTimer?.Stop();
		_settingsForm?.Close();
	}

	private void StopHoldTimer(object sender, EventArgs e)
	{
		_mouseHoldTimer?.Stop();
	}


	private void HandleVisibleChanged(object sender, EventArgs e)
	{
		if (!Visible)
			_settingsForm?.Close();
	}

	private void DisableCommandImpulse(object sender, EventArgs? e)
	{
		_impulseTimer?.Stop();
		SetPinValue(_currentCommand, false);
	}


	private void SendCommand(int commandId)
	{
		if (_impulseTimer != null && _impulseTimer.Enabled)
		{
			DisableCommandImpulse(this, null);
			_impulseTimer.Stop();
		}

		_currentCommand = commandId;
		SetPinValue(_currentCommand, true);
		_impulseTimer?.Start();
	}

	private void OpenSettingsForm()
	{
		_settingsForm = new PumpSettingForm(this);
		var formLocation = MousePosition;
		var area = Screen.GetWorkingArea(formLocation);
		if (formLocation.X + _settingsForm.Width > area.Right)
			formLocation.X -= _settingsForm.Width;
		if (formLocation.Y + _settingsForm.Height > area.Bottom)
			formLocation.Y -= _settingsForm.Height;

		_settingsForm.Location = formLocation;
		_settingsForm.FormClosed += RemoveSettingsFormReference;
		_settingsForm.Show(Win32Window.FromInt32(User32.GetParent(Handle)));
	}

	private void RemoveSettingsFormReference(object sender, FormClosedEventArgs e)
	{
		var form = (PumpSettingForm)sender;
		form.FormClosed -= RemoveSettingsFormReference;
		_settingsForm = null;
	}


	private void UpdateStatus()
	{
		Status.ConnectionOk = GetPinValue<bool>(PumpFB.ConnectionOkId);
		Status.MainError = GetPinValue<bool>(PumpFB.MainErrorId);
		Status.UsedByAutoMode = GetPinValue<bool>(PumpFB.UsedByAutoModeId);
		Status.WorkOnNominalSpeed = GetPinValue<bool>(PumpFB.WorkOnNominalSpeedId);
		Status.Stopped = GetPinValue<bool>(PumpFB.StoppedId);
		Status.Accelerating = GetPinValue<bool>(PumpFB.AcceleratingId);
		Status.Decelerating = GetPinValue<bool>(PumpFB.DeceseratingId);
		Status.Warning = GetPinValue<bool>(PumpFB.WarningId);
		Status.Message1 = GetPinValue<bool>(PumpFB.Message1Id);
		Status.Message2 = GetPinValue<bool>(PumpFB.Message2Id);
		Status.Message3 = GetPinValue<bool>(PumpFB.Message3Id);
		Status.ForceStop = GetPinValue<bool>(PumpFB.ForceStopId);
		Status.BlockStart = GetPinValue<bool>(PumpFB.BlockStartId);
		Status.BlockStop = GetPinValue<bool>(PumpFB.BlockStopId);
		Status.Use = GetPinValue<bool>(PumpFB.UsedId);

		buttonStart.Enabled = !Status.UsedByAutoMode && !Status.BlockStart && !Status.ForceStop;
		buttonStop.Enabled = !Status.UsedByAutoMode && !Status.BlockStop;

		var fb = FBConnector.Fb as PumpFB;
		Status.Temperature = GetPinValue<float>(PumpFB.TemperatureId);

		if (fb == null)
			return;

		switch (fb.PumpType)
		{
			case PumpType.Forvacuum:
			{
				break;
			}
			case PumpType.Turbine:
			{
				Status.Speed = GetPinValue<float>(PumpFB.TurbineSpeedId);

				Status.Temperature = GetPinValue<float>(PumpFB.TemperatureId);
				break;
			}
			case PumpType.Ion:
			{
				Status.SafeMode = GetPinValue<bool>(PumpFB.Message1Id);

				Status.Voltage = GetPinValue<float>(PumpFB.IonPumpVoltage);
				Status.Current = GetPinValue<float>(PumpFB.IonPumpCurrent);
				break;
			}
			case PumpType.Cryogen:
			{
				Status.TemperatureIn = GetPinValue<float>(PumpFB.CryoInTemperature);
				Status.TemperatureOut = GetPinValue<float>(PumpFB.CryoOutTemperature);
				break;
			}
		}

		var animationNeeded = Status.AnimationNeeded;

		if (_animationTimer != null)
		{
			if (!_animationTimer.Enabled && animationNeeded)
				_animationTimer.Start();
			if (_animationTimer.Enabled && !animationNeeded)
				_animationTimer.Stop();
		}

		_settingsForm?.Invalidate();
	}

	private T GetPinValue<T>(int id)
	{
		return FBConnector.GetPinValue<T>(id + 1000);
	}

	private void SetPinValue<T>(int id, T value)
	{
		FBConnector.SetPinValue(id + 1000, value);
	}
}
