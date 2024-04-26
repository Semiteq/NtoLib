using FB.VisualFB;
using InSAT.Library.Gui;
using InSAT.Library.Interop.Win32;
using NtoLib.Render.Valves;
using NtoLib.Valves.Settings;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NtoLib.Valves
{
    [ComVisible(true)]
    [Guid("4641CB00-693D-46F2-85EC-5142B0C0EDA4")]
    [DisplayName("Клапан")]
    public partial class ValveControl : VisualControlBase
    {
        private Render.Orientation _orientation;
        [DisplayName("Ориентация")]
        public Render.Orientation Orientation
        {
            get
            {
                return _orientation;
            }
            set
            {
                if(Math.Abs((int)_orientation - (int)value) % 180 == 90)
                    (Width, Height) = (Height, Width);

                _orientation = value;
            }
        }

        private bool _isSlideGate;
        [DisplayName("Шибер")]
        public bool IsSlideGate
        {
            get
            {
                return _isSlideGate;
            }
            set
            {
                _isSlideGate = value;

                if(_isSlideGate)
                {
                    if(_renderer.GetType() != typeof(SlideGateRenderer))
                        _renderer = new SlideGateRenderer(this);
                }
                else
                {
                    if(_isSmoothValve)
                    {
                        if(_renderer.GetType() != typeof(SmoothValveRenderer))
                            _renderer = new SmoothValveRenderer(this);
                    }
                    else
                    {
                        if(_renderer.GetType() != typeof(CommonValveRenderer))
                            _renderer = new CommonValveRenderer(this);
                    }
                }
            }
        }

        internal Status Status;

        private ValveBaseRenderer _renderer;

        private SettingsForm _settingsForm;

        private bool _isSmoothValve = false;

        private Timer _impulseTimer;
        private int _currentCommand;

        private Timer _mouseHoldTimer;

        private Timer _animationTimer;
        private bool _animationClocker;



        public ValveControl()
        {
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();

            InitializeComponent();

            _renderer = new CommonValveRenderer(this);

            _impulseTimer = new Timer();
            _impulseTimer.Interval = 500;
            _impulseTimer.Tick += DisableCommandImpulse;

            _animationTimer = new Timer();
            _animationTimer.Interval = 500;
            _animationTimer.Tick += UpdateAnimation;

            _mouseHoldTimer = new Timer();
            _mouseHoldTimer.Interval = 200;
            _mouseHoldTimer.Tick += HandleMouseHoldDown;
        }



        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);

            if(!FBConnector.DesignMode)
                UpdateStatus();

            _renderer.Draw(e.Graphics, Bounds, Orientation, _animationClocker);
        }

        protected override void ToDesign()
        {
            _settingsForm?.Close();
            base.ToDesign();
        }

        private void UpdateAnimation(object sender, EventArgs e)
        {
            _animationClocker = !_animationClocker;
            this.Invalidate();
        }



        private void HandleMouseDown(object sender, MouseEventArgs e)
        {
            if(e.Button != MouseButtons.Right)
            {
                _mouseHoldTimer.Stop();
                _settingsForm?.Close();
                return;
            }

            _mouseHoldTimer.Start();
        }

        private void HandleMouseHoldDown(object sender, EventArgs e)
        {
            OpenSettingsForm();
            _mouseHoldTimer.Stop();
        }

        private void HandleMouseUp(object sender, MouseEventArgs e)
        {
            if(e.Button != MouseButtons.Right)
                return;

            _mouseHoldTimer.Stop();
            _settingsForm?.Close();
        }

        private void StopHoldTimer(object sender, EventArgs e)
        {
            _mouseHoldTimer?.Stop();
        }



        private void HandleDoubleClick(object sender, EventArgs e)
        {
            MouseEventArgs me = (MouseEventArgs)e;
            if(Status.UsedByAutoMode)
                return;


            int commandId = -1;

            if(me.Button == MouseButtons.Left)
            {
                if(Status.BlockOpening)
                    return;

                if(_isSmoothValve)
                {
                    if(Status.State == State.Closed)
                        commandId = ValveFB.OpenSmoothlyCmdId;
                    else if(Status.State == State.SmothlyOpened)
                        commandId = ValveFB.OpenCmdId;
                    else
                        commandId = ValveFB.CloseCmdId;
                }
                else
                {
                    if(Status.State == State.Closed)
                        commandId = ValveFB.OpenCmdId;
                    else
                        commandId = ValveFB.CloseCmdId;
                }
            }
            else if(me.Button == MouseButtons.Right)
            {
                if(Status.BlockClosing)
                    return;

                commandId = ValveFB.CloseCmdId;
            }

            if(commandId < 0)
                return;

            SendCommand(commandId);
        }

        private void HandleVisibleChanged(object sender, EventArgs e)
        {
            if(!Visible)
                _settingsForm?.Close();
        }

        private void DisableCommandImpulse(object sender, EventArgs e)
        {
            _impulseTimer.Stop();
            SetPinValue(_currentCommand, false);
        }



        private void SendCommand(int commandId)
        {
            if(_impulseTimer.Enabled)
            {
                DisableCommandImpulse(this, null);
                _impulseTimer.Stop();
            }

            _currentCommand = commandId;
            SetPinValue(_currentCommand, true);
            _impulseTimer.Start();
        }

        private void OpenSettingsForm()
        {
            _settingsForm = new SettingsForm(this);
            Point formLocation = MousePosition;
            Rectangle area = Screen.GetWorkingArea(formLocation);
            if(formLocation.X + _settingsForm.Width > area.Right)
                formLocation.X -= _settingsForm.Width;
            if(formLocation.Y + _settingsForm.Height > area.Bottom)
                formLocation.Y -= _settingsForm.Height;

            _settingsForm.Location = formLocation;
            _settingsForm.FormClosed += RemoveSettingsFormReference;
            _settingsForm.Show(Win32Window.FromInt32(User32.GetParent(Handle)));
        }

        private void RemoveSettingsFormReference(object sender, FormClosedEventArgs e)
        {
            SettingsForm form = (SettingsForm)sender;
            form.FormClosed -= RemoveSettingsFormReference;
            _settingsForm = null;
        }


        private void UpdateStatus()
        {
            Status.NoConnection = !GetPinValue<bool>(ValveFB.ConnectionOkId);
            Status.UsedByAutoMode = GetPinValue<bool>(ValveFB.UsedByAutoModeId);

            bool open = GetPinValue<bool>(ValveFB.OpenedId);
            bool closed = GetPinValue<bool>(ValveFB.ClosedId);
            bool openingClosing = GetPinValue<bool>(ValveFB.OpeningClosingId);
            bool smoothlyOpened = GetPinValue<bool>(ValveFB.SmoothlyOpenedId);
            Status.Collision = GetPinValue<bool>(ValveFB.CollisionId);

            if(Status.NoConnection)
                Status.State = State.NoData;
            else if(Status.Collision)
                Status.State = State.Collision;
            else if(openingClosing)
                Status.State = State.OpeningClosing;
            else if(smoothlyOpened)
                Status.State = State.SmothlyOpened;
            else if(open)
                Status.State = State.Opened;
            else if(closed)
                Status.State = State.Closed;

            Status.NotOpened = GetPinValue<bool>(ValveFB.NotOpenedId);
            Status.NotClosed = GetPinValue<bool>(ValveFB.NotClosedId);

            Status.BlockOpening = GetPinValue<bool>(ValveFB.BlockOpeningId);
            Status.BlockClosing = GetPinValue<bool>(ValveFB.BlockClosingId);

            _isSmoothValve = GetPinValue<bool>(ValveFB.IsSmoothValveId);

            if(!IsSlideGate)
            {
                if(_isSmoothValve && _renderer.GetType() != typeof(SmoothValveRenderer))
                    _renderer = new SmoothValveRenderer(this);
                else if(!_isSmoothValve && _renderer.GetType() != typeof(CommonValveRenderer))
                    _renderer = new CommonValveRenderer(this);
            }

            bool animationNeeded = openingClosing || Status.Collision;
            if(!_animationTimer.Enabled && animationNeeded)
                _animationTimer.Start();
            if(_animationTimer.Enabled && !animationNeeded)
                _animationTimer.Stop();

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
}
