using FB.VisualFB;
using InSAT.Library.Gui;
using InSAT.Library.Interop.Win32;
using NtoLib.Pumps.Settings;
using NtoLib.Render.Pumps;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NtoLib.Pumps
{
    [ComVisible(true)]
    [Guid("664141D1-44B3-43D7-9897-3A10C936315A")]
    [DisplayName("Насос")]
    public partial class PumpControl : VisualControlBase
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
                if(_orientation != value)
                    (Width, Height) = (Height, Width);

                _orientation = value;
            }
        }

        internal Status Status;

        private PumpRenderer _renderer;

        private PumpSettingForm _settingsForm;

        private Timer _impulseTimer;
        private int _currentCommand;

        private Timer _mouseHoldTimer;

        private Timer _animationTimer;
        private bool _animationClocker;



        public PumpControl()
        {
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();

            InitializeComponent();

            _renderer = new PumpRenderer(this);

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
                if(Status.BlockStart)
                    return;

                if(Status.Stopped || Status.Decelerating)
                    commandId = PumpFB.StartCmdId;
                else if(Status.WorkOnNominalSpeed || Status.Accelerating)
                    commandId = PumpFB.StopCmdId;
            }
            else if(me.Button == MouseButtons.Right)
            {
                if(Status.BlockStop)
                    return;

                commandId = PumpFB.StopCmdId;
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
            _settingsForm = new PumpSettingForm(this);
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
            PumpSettingForm form = (PumpSettingForm)sender;
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
            Status.Use = GetPinValue<bool>(PumpFB.UseId);

            var fb = FBConnector.Fb as PumpFB;
            Status.Temperature = GetPinValue<float>(PumpFB.TemperatureId);

            switch(fb.PumpType)
            {
                case PumpType.Forvacuum:
                {

                    break;
                }
                case PumpType.Turbine:
                {
                    Status.Units = GetPinValue<bool>(PumpFB.CustomId);

                    Status.Temperature = GetPinValue<float>(PumpFB.TemperatureId);
                    break;
                }
                case PumpType.Ion:
                {
                    Status.SafeMode = GetPinValue<bool>(PumpFB.CustomId);

                    Status.Voltage = GetPinValue<float>(PumpFB.IonPumpVoltage);
                    Status.Current = GetPinValue<float>(PumpFB.IonPumpCurrent);
                    Status.Power = GetPinValue<float>(PumpFB.IonPumpPower);
                    break;
                }
                case PumpType.Cryogen:
                {
                    Status.TemperatureIn = GetPinValue<float>(PumpFB.CryoInTemperature);
                    Status.TemperatureOut = GetPinValue<float>(PumpFB.CryoOutTemperature);
                    break;
                }
            }

            bool animationNeeded = Status.AnimationNeeded;
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
