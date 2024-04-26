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
    public class PumpFB : VisualFBBaseExtended
    {
        public const int StatusWordId = 1;
        public const int CommandWordId = 5;


        public const int ConnectionOkId = 0;
        public const int MainErrorId = 1;
        public const int UsedByAutoModeId = 2;
        public const int WorkOnNominalSpeedId = 3;
        public const int StoppedId = 4;
        public const int AcceleratingId = 5;
        public const int DeceseratingId = 6;
        public const int WarningId = 7;
        public const int Error1Id = 8;
        public const int Error2Id = 9;
        public const int Error3Id = 10;
        public const int Error4Id = 11;
        public const int ForceStopId = 12;
        public const int BlockStartId = 13;
        public const int BlockStopId = 14;
        public const int UseId = 15;


        public const int StartCmdId = 100;
        public const int StopCmdId = 101;


        public const int ConnectionDisabledEventId = 5000;
        private EventTrigger _connectionDisabledEvent;


        public const int AccelerationStartEventId = 5010;
        private EventTrigger _accelerationEvent;

        public const int DecelerationStartEventId = 5011;
        private EventTrigger _decelerationEvent;

        public const int ReachNominalSpeedEventId = 5012;
        private EventTrigger _nominalSpeedEvent;

        public const int StopEventId = 5013;
        private EventTrigger _stopEvent;



        protected override void ToRuntime()
        {
            base.ToRuntime();

            string[] splittedString = FullName.Split('.');
            string name = splittedString[splittedString.Length - 1];

            TimeSpan initialInactivity = TimeSpan.FromSeconds(10);

            string message = $"Нет соединения с {name}";
            _connectionDisabledEvent = new EventTrigger(this, ConnectionDisabledEventId, message, initialInactivity);

            message = $"Начало ускорения {name}";
            _accelerationEvent = new EventTrigger(this, AccelerationStartEventId, message, initialInactivity);

            message = $"Начало торможения {name}";
            _decelerationEvent = new EventTrigger(this, DecelerationStartEventId, message, initialInactivity);

            message = $"{name} вышел на номинальную скорость";
            _nominalSpeedEvent = new EventTrigger(this, ReachNominalSpeedEventId, message, initialInactivity);

            message = $"{name} остановился";
            _stopEvent = new EventTrigger(this, StopEventId, message, initialInactivity, true);
        }

        protected override void UpdateData()
        {
            base.UpdateData();

            int statusWord = 0;
            if(GetPinQuality(StatusWordId) == OpcQuality.Ok)
                statusWord = GetPinValue<int>(StatusWordId);

            bool connectionOk = statusWord.GetBit(ConnectionOkId);
            SetVisualAndUiPin(ConnectionOkId, connectionOk);
            SetVisualAndUiPin(MainErrorId, statusWord.GetBit(MainErrorId));
            SetVisualAndUiPin(UsedByAutoModeId, statusWord.GetBit(UsedByAutoModeId));
            bool workOnNominalSpeed = statusWord.GetBit(ConnectionOkId);
            SetVisualAndUiPin(WorkOnNominalSpeedId, workOnNominalSpeed);
            bool stopped = statusWord.GetBit(StoppedId);
            SetVisualAndUiPin(StoppedId, stopped);
            bool accelerating = statusWord.GetBit(AcceleratingId);
            SetVisualAndUiPin(AcceleratingId, accelerating);
            bool decelerating = statusWord.GetBit(DeceseratingId);
            SetVisualAndUiPin(DeceseratingId, decelerating);
            SetVisualAndUiPin(WarningId, statusWord.GetBit(WarningId));
            SetVisualAndUiPin(Error1Id, statusWord.GetBit(Error1Id));
            SetVisualAndUiPin(Error2Id, statusWord.GetBit(Error2Id));
            SetVisualAndUiPin(Error3Id, statusWord.GetBit(Error3Id));
            SetVisualAndUiPin(Error4Id, statusWord.GetBit(Error4Id));
            SetVisualAndUiPin(ForceStopId, statusWord.GetBit(ForceStopId));
            SetVisualAndUiPin(BlockStartId, statusWord.GetBit(BlockStartId));
            SetVisualAndUiPin(BlockStopId, statusWord.GetBit(BlockStopId));
            SetVisualAndUiPin(UseId, statusWord.GetBit(UseId));


            int commandWord = 0;
            commandWord.SetBit(0, GetVisualPin<bool>(StartCmdId));
            commandWord.SetBit(1, GetVisualPin<bool>(StopCmdId));
            SetPinValue(CommandWordId, commandWord);



            _connectionDisabledEvent.Update(!connectionOk);

            _nominalSpeedEvent.Update(workOnNominalSpeed);
            _stopEvent.Update(stopped);
            _accelerationEvent.Update(accelerating);
            _decelerationEvent.Update(decelerating);
        }



        protected override OpcQuality GetConnectionQuality()
        {
            return GetPinQuality(StatusWordId);
        }
    }
}
