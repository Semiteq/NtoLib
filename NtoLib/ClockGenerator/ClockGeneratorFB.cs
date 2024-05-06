using FB;
using InSAT.Library.Attributes;
using InSAT.Library.Interop;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Timers;

namespace NtoLib.ClockGenerator
{
    [Serializable]
    [ComVisible(true)]
    [Guid("6C051A25-C703-4ADD-9022-5D306B0C236A")]
    [CatID(CatIDs.CATID_OTHER)]
    [DisplayName("Инкремент по времени")]
    public class ClockGeneratorFB : StaticFBBase
    {
        private int _delay;
        [Order(2)]
        [DisplayName("Задержка, мс")]
        public int Delay
        {
            get
            {
                return _delay;
            }
            set
            {
                _delay = value;

                if(_delay <= 1)
                    _delay = 1;
            }
        }

        [Order(0)]
        [DisplayName("Максимум")]
        public int Max { get; set; }

        [Order(1)]
        [DisplayName("Минимум")]
        public int Min { get; set; }

        [NonSerialized]
        private Timer _clockTimer;

        private int _counter;



        protected override void ToRuntime()
        {
            base.ToRuntime();

            _counter = Min;

            _clockTimer = new Timer();
            _clockTimer.AutoReset = true;

            _clockTimer.Interval = Delay;
            _clockTimer.Elapsed += IncClock;
            _clockTimer.Start();
        }

        protected override void ToDesign()
        {
            base.ToDesign();

            _clockTimer?.Dispose();
        }

        protected override void UpdateData()
        {
            if(_clockTimer?.Interval != Delay)
                _clockTimer.Interval = Delay;
        }



        private void IncClock(object sender, EventArgs e)
        {
            _counter++;

            if(_counter > Max)
                _counter = Min;

            SetPinValue(100, _counter);
        }
    }
}
