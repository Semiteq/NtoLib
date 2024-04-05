using FB.VisualFB;
using NtoLib.Valves.Render;
using System;
using System.ComponentModel;
using System.Drawing;
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

        [DisplayName("Ориентация")]
        public Orientation Orientation { get; set; }

        [DisplayName("Шибер")]
        public bool IsSlideGate { get; set; }

        [DisplayName("Форма")]
        public Shape Shape { get; set; }


        private State _state;
        private bool _commandImpulseInProgress;



        public ValveControl()
        {
            InitializeComponent();
            BackColor = Color.Transparent;
        }



        protected override void OnPaint(PaintEventArgs e)
        {
            if(!FBConnector.DesignMode)
                UpdateState();

            PaintData paintData = new PaintData {
                Bounds = Bounds,
                LineWidth = PenWidth,
                ErrorLineWidth = ErrorPenWidth,
                ErrorOffset = 10f,
                Orientation = Orientation,
                Shape = Shape
            };


            BaseRenderer renderer = new CommonValveRenderer();
            renderer.Paint(e.Graphics, paintData, _state);
        }

        private void OnClick(object sender, EventArgs e)
        {
            MouseEventArgs me = (MouseEventArgs)e;
            if(me.Button != MouseButtons.Right)
                return;

            //Открытиие формы с настройками
        }

        private void OnDoubleClick(object sender, EventArgs e)
        {
            MouseEventArgs me = (MouseEventArgs)e;
            if(me.Button != MouseButtons.Left || _commandImpulseInProgress)
                return;


            int commandId;
            if(_state.Closed)
                commandId = ValveFB.OpenCMD;
            else if(_state.Opened)
                commandId = ValveFB.CloseCMD;
            else
                return;

            SetPinValue(commandId, true);

            Task.Run(() => SendCommandImpulse(commandId, 500));
        }



        private async Task SendCommandImpulse(int outputId, int msDuration)
        {
            SetPinValue(outputId, true);
            _commandImpulseInProgress = true;

            await Task.Delay(msDuration);

            SetPinValue(outputId, false);
            _commandImpulseInProgress = false;
        }

        private void UpdateState()
        {
            _state.ConnectionOk = GetPinValue<bool>(ValveFB.ConnectionOk);
            _state.Opened = GetPinValue<bool>(ValveFB.Opened);
            _state.Closed = GetPinValue<bool>(ValveFB.Closed);
            _state.Error = GetPinValue<bool>(ValveFB.Error);
            _state.OldError = GetPinValue<bool>(ValveFB.OldError);
            _state.BlockOpening = GetPinValue<bool>(ValveFB.BlockOpening);
            _state.BlockClosing = GetPinValue<bool>(ValveFB.BlockClosing);
            _state.AutoMode = GetPinValue<bool>(ValveFB.AutoMode);
            _state.Collision = GetPinValue<bool>(ValveFB.Collision);
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
