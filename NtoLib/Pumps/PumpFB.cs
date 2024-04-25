using FB;
using FB.VisualFB;
using InSAT.Library.Interop;
using InSAT.OPC;
using NtoLib.Utils;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NtoLib.Pumps
{
    [Serializable]
    [ComVisible(true)]
    [Guid("0B9D612A-E69E-4817-AA0A-878186938C3F")]
    [CatID(CatIDs.CATID_OTHER)]
    [DisplayName("Насос")]
    [VisualControls(typeof(PumpControl))]
    public class PumpFB : VisualFBBase
    {
        public const int StatusWordId = 1;
        public const int CommandWordId = 100;

        public const int ConnectionOkId = 1;
        public const int MainErrorId = 2;
        public const int UsedByAutoModeId = 3;
        public const int WorkOnNominalSpeed = 4;
        public const int Stopped = 5;
        public const int Accelerating = 6;
        public const int Deceserating = 7;
        public const int Warning = 8;
        public const int Error1 = 9;
        public const int Error2 = 10;
        public const int Error3 = 11;
        public const int Error4 = 12;
        public const int ForceStop = 13;
        public const int BlockStart = 14;
        public const int BlockStop = 15;
        public const int Use = 16;


        public const int StartCmdId = 101;
        public const int StopCmdId = 102;


        public const int ConnectionDisabledEventId = 5001;
        private EventTrigger _connectionDisabledEvent;


        public const int AccelerationStartEventId = 5010;
        private EventTrigger _accelerationEvent;

        public const int DecelerationStartEventId = 5011;
        private EventTrigger _decelerationEvent;

        public const int ReachNominalSpeedEventId = 5012;
        private EventTrigger _nominalSpeedEvent;

        public const int StopEventId = 5013;
        private EventTrigger _stopEvent;
    }
}
