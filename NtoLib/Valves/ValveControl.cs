using FB.VisualFB;
using InSAT.Library.Gui;
using InSAT.Library.Interop.Win32;
using NtoLib.Utils;
using NtoLib.Valves.Render;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NtoLib.Valves
{
    [ComVisible(true)]
    [Guid("09DF2B43-AE07-4A93-9670-FE1ED79E0751")]
    [DisplayName("Имя какое-то")]
    public partial class ValveControl : VisualControlBase
    {
        private float _penWidth = 2f;
        [DisplayName("Толщина линии")]
        public float PenWidth
        {
            get
            {
                return _penWidth;
            }
            set
            {
                if(value < 1)
                    _penWidth = 1;
                else
                    _penWidth = value;
            }
        }

        private float _errorPenWidth = 2f;
        [DisplayName("Толщина линии ошибки")]
        public float ErrorPenWidth
        {
            get
            {
                return _errorPenWidth;
            }
            set
            {
                if(value < 1)
                    _errorPenWidth = 1;
                else
                    _errorPenWidth = value;
            }
        }

        private float _errorOffset = 5f;
        [DisplayName("Отступ от ошибки")]
        public float ErrorOffset
        {
            get
            {
                return _errorOffset;
            }
            set
            {
                if(value < 0)
                    _errorOffset = 0;
                else
                    _errorOffset = value;
            }
        }

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

        private BaseRenderer _renderer;

        private SettingsForm _settingsForm;
        private Blinker _blinker;

        private bool _isSmoothValve = false;



        public ValveControl()
        {
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

            PaintData paintData = new PaintData {
                Bounds = Bounds,
                LineWidth = PenWidth,
                ErrorLineWidth = ErrorPenWidth,
                ErrorOffset = ErrorOffset,
                Orientation = Orientation,
                IsLight = _blinker.IsLight
            };

            _renderer.Draw(e.Graphics, paintData);
        }



        private void HandleSingleClick(object sender, EventArgs e)
        {
            MouseEventArgs me = (MouseEventArgs)e;
            if(me.Button != MouseButtons.Right)
                return;

            if(_settingsForm == null)
                OpenSettingsForm();
        }

        private void HandleDoubleClick(object sender, EventArgs e)
        {
            MouseEventArgs me = (MouseEventArgs)e;
            if(me.Button != MouseButtons.Left || _commandImpulseInProgress)
                return;

            int commandId;
            if(Status.State == State.Closed)
                commandId = ValveFB.OpenCMD;
            else if(Status.State == State.Opened)
                commandId = ValveFB.CloseCMD;
            else
                return;

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
            if(Status.State == State.Opening || Status.State == State.Closing || Status.State == State.Collision)
                Invalidate();
        }


        private void OpenSettingsForm()
        {
            _settingsForm = new SettingsForm(this);
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
            bool connectionOk = GetPinValue<bool>(ValveFB.ConnectionOk);
            bool open = GetPinValue<bool>(ValveFB.Opened);
            bool closed = GetPinValue<bool>(ValveFB.Closed);
            bool opening = GetPinValue<bool>(ValveFB.Opening);
            bool smoothlyOpened = GetPinValue<bool>(ValveFB.SmoothlyOpened);
            bool closing = GetPinValue<bool>(ValveFB.Closing);
            bool collision = GetPinValue<bool>(ValveFB.Collision);

            if(!connectionOk)
                Status.State = State.NoData;
            else if(collision)
                Status.State = State.Collision;
            else if(opening)
                Status.State = State.Opening;
            else if(smoothlyOpened)
                Status.State = State.SmothlyOpened;
            else if(closing)
                Status.State = State.Closing;
            else if(open)
                Status.State = State.Opened;
            else if(closed)
                Status.State = State.Closed;
            //else
            //    throw new Exception("Undefined state");


            Status.Error = GetPinValue<bool>(ValveFB.Error);
            Status.BlockOpening = GetPinValue<bool>(ValveFB.BlockOpening);
            Status.BlockClosing = GetPinValue<bool>(ValveFB.BlockClosing);

            _isSmoothValve = GetPinValue<bool>(ValveFB.IsSmoothValve);

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
