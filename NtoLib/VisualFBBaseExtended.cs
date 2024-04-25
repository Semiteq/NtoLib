using FB.VisualFB;
using InSAT.OPC;
using System;
using System.Runtime.InteropServices;
using System.Timers;

namespace NtoLib
{
    [Serializable]
    [ComVisible(true)]
    public abstract class VisualFBBaseExtended : VisualFBBase
    {
        [NonSerialized]
        private Timer _connectionCheckTimer;
        [NonSerialized]
        private OpcQuality _previousOpcQuality;



        protected override void ToRuntime()
        {
            _connectionCheckTimer = new Timer();
            _connectionCheckTimer.Interval = 100;
            _connectionCheckTimer.AutoReset = true;
            _connectionCheckTimer.Elapsed += UpdateIfQualityChanged;
            _connectionCheckTimer.Start();

            _previousOpcQuality = GetConnectionQuality();
        }

        protected override void ToDesign()
        {
            _connectionCheckTimer.Dispose();
        }

        protected abstract OpcQuality GetConnectionQuality();



        private void UpdateIfQualityChanged(object sender, EventArgs e)
        {
            OpcQuality quality = GetConnectionQuality();
            if(quality != _previousOpcQuality)
                UpdateData();

            _previousOpcQuality = quality;
        }
    }
}
