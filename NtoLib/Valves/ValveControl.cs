using FB.VisualFB;
using InSAT.Library.Gui;
using InSAT.Library.Interop.Win32;
using NtoLib.Render.Valves;
using NtoLib.Utils;
using NtoLib.Valves.Settings;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NtoLib.Valves
{
    [ComVisible(true)]
    [Guid("4641CB00-693D-46F2-85EC-5142B0C0EDA4")]
    [DisplayName("Имя какое-то")]
    public partial class ValveControl : VisualControlBase
    {
        [DisplayName("Ориентация")]
        public Orientation Orientation { get; set; }

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
        private bool _commandImpulseInProgress;

        private ValveBaseRenderer _renderer;

        private SettingsForm _settingsForm;
        private Blinker _blinker;

        private bool _isSmoothValve = false;



        public ValveControl()
        {
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();

            InitializeComponent();

            _blinker = new Blinker(500);
            _blinker.OnLightChanged += InvalidateIfNeeded;

            _renderer = new CommonValveRenderer(this);
        }



        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);

            if(!FBConnector.DesignMode)
                UpdateStatus();

            _renderer.Draw(e.Graphics, Bounds, Orientation, _blinker.IsLight);
        }

        protected override void ToDesign()
        {
            _settingsForm?.Close();
            base.ToDesign();
        }



        private void HandleSingleClick(object sender, EventArgs e)
        {
            MouseEventArgs me = (MouseEventArgs)e;
            if(me.Button != MouseButtons.Right)
                return;

            if(_settingsForm == null)
                OpenSettingsForm();
        }

        private void HandleMouseDown(object sender, MouseEventArgs e)
        {
            if(e.Button != MouseButtons.Right)
                return;

            OpenSettingsForm();
        }

        private void HandleMouseUp(object sender, MouseEventArgs e)
        {
            if(e.Button != MouseButtons.Right)
                return;

            _settingsForm?.Close();
        }

        private void HandleDoubleClick(object sender, EventArgs e)
        {
            MouseEventArgs me = (MouseEventArgs)e;
            if(me.Button != MouseButtons.Left || _commandImpulseInProgress)
                return;

            if(Status.AutoMode)
                return;

            int commandId;
            if(_isSmoothValve)
            {
                if(Status.State == State.Closed && !Status.BlockOpening)
                    commandId = ValveFB.OpenSmoothlyCmdId;
                else if(Status.State == State.SmothlyOpened && !Status.BlockOpening)
                    commandId = ValveFB.OpenCmdId;
                else if(Status.State == State.Opened && !Status.BlockClosing)
                    commandId = ValveFB.CloseCmdId;
                else
                    return;
            }
            else
            {
                if(Status.State == State.Closed && !Status.BlockOpening)
                    commandId = ValveFB.OpenCmdId;
                else if(Status.State == State.Opened && !Status.BlockClosing)
                    commandId = ValveFB.CloseCmdId;
                else
                    return;
            }

            Task.Run(() => SendCommandImpulseAsync(commandId, 500));
        }



        private void HandleVisibleChanged(object sender, EventArgs e)
        {
            if(!Visible)
                _settingsForm?.Close();
        }



        private async Task SendCommandImpulseAsync(int outputId, int msDuration)
        {
            SetPinValue(outputId, true);
            _commandImpulseInProgress = true;

            await Task.Delay(msDuration);

            SetPinValue(outputId, false);
            _commandImpulseInProgress = false;
        }

        private void InvalidateIfNeeded()
        {
            if(Status.State == State.OpeningClosing || Status.State == State.Collision)
                Invalidate();
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
            Status.AutoMode = GetPinValue<bool>(ValveFB.AutoModeId);

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
