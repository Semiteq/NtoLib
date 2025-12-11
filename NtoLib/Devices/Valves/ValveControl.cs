using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using FB.VisualFB;

using InSAT.Library.Gui;
using InSAT.Library.Interop.Win32;

using NtoLib.Devices.Render.Common;
using NtoLib.Devices.Render.Valves;
using NtoLib.Devices.Valves.Settings;

using Orientation = NtoLib.Devices.Render.Common.Orientation;

namespace NtoLib.Devices.Valves;

[ComVisible(true)]
[Guid("4641CB00-693D-46F2-85EC-5142B0C0EDA4")]
[DisplayName("Клапан")]
public partial class ValveControl : VisualControlBase
{
	private bool _noButtons;

	[Browsable(false)]
	[DisplayName("Скрыть кнопки")]
	public bool NoButtons
	{
		get => _noButtons;
		set
		{
			var updateRequired = _noButtons != value;
			_noButtons = value;
			if (updateRequired)
				UpdateLayout();
		}
	}

	private Orientation _orientation;

	[DisplayName("Ориентация клапана")]
	public Orientation Orientation
	{
		get => _orientation;
		set
		{
			if (Math.Abs((int)_orientation - (int)value) % 180 == 90)
				(Width, Height) = (Height, Width);

			var updateRequired = _orientation != value;
			_orientation = value;
			if (updateRequired)
				UpdateLayout();
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
				UpdateLayout();
		}
	}

	private bool _isSlideGate;

	[DisplayName("Шибер")]
	[Description("Изменяет отображение на шибер. Имеет приоретет над плавным клапаном.")]
	public bool IsSlideGate
	{
		get => _isSlideGate;
		set
		{
			_isSlideGate = value;
			UpdateRenderer();
			UpdateLayout();
		}
	}

	private bool _smoothValvePreview;

	[DisplayName("Предпросмотр плавного клапана")]
	[Description("Изменяет отображение на плавный клапан в DesignMode. В Runtime тип будет определятся из StatusWord.")]
	public bool SmoothValvePreview
	{
		get => _smoothValvePreview;
		set
		{
			_smoothValvePreview = value;
			UpdateRenderer();
			UpdateLayout();
		}
	}

	internal Status Status;
	private bool _previousSmoothValve;

	private ValveBaseRenderer? _renderer;

	private SettingsForm? _settingsForm;

	private bool _isSmoothValve = false;

	private Timer? _impulseTimer;
	private int _currentCommand;

	private Timer? _mouseHoldTimer;

	private Timer? _animationTimer;
	private bool _animationClocker;

	private Timer? _redrawTimer;

	public ValveControl()
	{
		SetStyle(ControlStyles.UserPaint, true);
		SetStyle(ControlStyles.AllPaintingInWmPaint, true);
		SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
		UpdateStyles();

		InitializeComponent();

		_renderer = new CommonValveRenderer(this);
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

	private void HandleOpenClick(object sender, EventArgs e)
	{
		if (!Status.UsedByAutoMode && !Status.BlockOpening)
		{
			SendCommand(ValveFB.OpenCmdId);
		}
	}

	private void HandleOpenSmoothlyClick(object sender, EventArgs e)
	{
		if (!Status.UsedByAutoMode && !Status.BlockOpening)
		{
			SendCommand(ValveFB.OpenSmoothlyCmdId);
		}
	}

	private void HandleCloseClick(object sender, EventArgs e)
	{
		if (!Status.UsedByAutoMode && !Status.BlockClosing)
		{
			SendCommand(ValveFB.CloseCmdId);
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		if (!FBConnector.DesignMode)
			UpdateStatus();
		UpdateSprite();
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
		if (IsHorizontal())
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

		Button[] buttons;
		if (NoButtons)
		{
			buttonOpen.Visible = false;
			buttonOpenSmoothly.Visible = false;
			buttonClose.Visible = false;
			buttons = new Button[] { };
		}
		else
		{
			buttonOpen.Visible = true;
			buttonClose.Visible = true;

			if (_isSlideGate || !(_isSmoothValve || (DesignMode && SmoothValvePreview)))
			{
				buttonOpenSmoothly.Visible = false;
				buttons = new Button[] { buttonOpen, buttonClose };
			}
			else
			{
				buttonOpenSmoothly.Visible = true;
				buttons = new Button[] { buttonOpen, buttonOpenSmoothly, buttonClose };
			}
		}

		LayoutBuilder.RebuildTable(buttonTable, buttonsOrientation, buttons);
	}

	private void UpdateSprite()
	{
		_renderer ??= new CommonValveRenderer(this);

		spriteBox.Image = new Bitmap(Math.Max(1, spriteBox.Width), Math.Max(1, spriteBox.Height));

		var unit = GraphicsUnit.Point;
		using (var g = Graphics.FromImage(spriteBox.Image))
		{
			g.Clear(BackColor);
			_renderer.Draw(g, spriteBox.Image.GetBounds(ref unit), Orientation, _animationClocker);
		}

		spriteBox.Refresh();
	}

	private bool IsHorizontal()
	{
		return Orientation == Orientation.Right || Orientation == Orientation.Left;
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
		_settingsForm = new SettingsForm(this);
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
		var form = (SettingsForm)sender;
		form.FormClosed -= RemoveSettingsFormReference;
		_settingsForm = null;
	}

	private void UpdateStatus()
	{
		Status.ConnectionOk = GetPinValue<bool>(ValveFB.ConnectionOkId);
		Status.NotOpened = GetPinValue<bool>(ValveFB.NotOpenedId);
		Status.NotClosed = GetPinValue<bool>(ValveFB.NotClosedId);
		if (Status.NotOpened && Status.NotClosed)
		{
			Status.NotOpened = false;
			Status.NotClosed = false;
			Status.UnknownState = true;
		}
		else
		{
			Status.UnknownState = false;
		}

		Status.Collision = GetPinValue<bool>(ValveFB.CollisionId);
		Status.UsedByAutoMode = GetPinValue<bool>(ValveFB.UsedByAutoModeId);
		Status.Used = GetPinValue<bool>(ValveFB.UsedId);
		Status.Manual = GetPinValue<bool>(ValveFB.ManualId);
		Status.Opened = GetPinValue<bool>(ValveFB.OpenedId);
		Status.OpenedSmoothly = GetPinValue<bool>(ValveFB.SmoothlyOpenedId);
		Status.Closed = GetPinValue<bool>(ValveFB.ClosedId);
		Status.OpeningClosing = GetPinValue<bool>(ValveFB.OpeningClosingId);

		Status.ForceClose = GetPinValue<bool>(ValveFB.ForceCloseId);
		Status.BlockClosing = GetPinValue<bool>(ValveFB.BlockClosingId);
		Status.BlockOpening = GetPinValue<bool>(ValveFB.BlockOpeningId);
		buttonOpen.Enabled = !Status.UsedByAutoMode && !Status.BlockOpening && !Status.ForceClose;
		buttonOpenSmoothly.Enabled = !Status.UsedByAutoMode && !Status.BlockOpening && !Status.ForceClose;
		buttonClose.Enabled = !Status.UsedByAutoMode && !Status.BlockClosing;

		spriteBox.Visible = Status.Used;
		buttonTable.Visible = Status.Used && !Status.Manual && !NoButtons;

		_isSmoothValve = GetPinValue<bool>(ValveFB.IsSmoothValveId);
		UpdateRenderer();

		if (_isSmoothValve != _previousSmoothValve)
			UpdateLayout();
		_previousSmoothValve = _isSmoothValve;


		if (_animationTimer != null)
		{
			if (!_animationTimer.Enabled && Status.AnimationNeeded)
				_animationTimer.Start();
			if (_animationTimer.Enabled && !Status.AnimationNeeded)
				_animationTimer.Stop();
		}

		_settingsForm?.Invalidate();
	}

	private void UpdateRenderer()
	{
		if (IsSlideGate)
		{
			if (_renderer?.GetType() != typeof(SlideGateRenderer))
				_renderer = new SlideGateRenderer(this);
		}
		else if (_isSmoothValve || (DesignMode && SmoothValvePreview))
		{
			if (_renderer?.GetType() != typeof(SmoothValveRenderer))
				_renderer = new SmoothValveRenderer(this);
		}
		else
		{
			if (_renderer?.GetType() != typeof(CommonValveRenderer))
				_renderer = new CommonValveRenderer(this);
		}
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
