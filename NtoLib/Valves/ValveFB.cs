using FB;
using FB.VisualFB;
using InSAT.Library.Interop;
using InSAT.OPC;
using NtoLib.Utils;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Timers;

namespace NtoLib.Valves
{
    [Serializable]
    [ComVisible(true)]
    [Guid("0B747EAD-4E9B-47CE-99AA-12BF8F5192A4")]
    [CatID(CatIDs.CATID_OTHER)]
    [DisplayName("Клапан")]
    [VisualControls(typeof(ValveControl))]
    public class ValveFB : VisualFBBase
    {
        public const int StatusWordId = 0;
        public const int CommandWordId = 1;


        public const int ConnectionOkId = 0;
        public const int NotOpenedId = 1;
        public const int NotClosedId = 2;
        public const int CollisionId = 3;
        public const int UsedByAutoModeId = 4;
        public const int OpenedId = 5;
        public const int SmoothlyOpenedId = 6;
        public const int ClosedId = 7;
        public const int OpeningClosingId = 8;

        public const int BlockClosingId = 13;
        public const int BlockOpeningId = 14;
        public const int IsSmoothValveId = 15;

        public const int OpenCmdId = 101;
        public const int CloseCmdId = 102;
        public const int OpenSmoothlyCmdId = 103;


        public const int CollistionEventId = 5000;
        private EventTrigger _collisionEvent;

        public const int NotOpenedEventId = 5001;
        private EventTrigger _notOpenedEvent;

        public const int NotClosedEventId = 5002;
        private EventTrigger _notClosedEvent;

        public const int ConnectionDisabledEventId = 5003;
        private EventTrigger _connectionDisabledEvent;


        public const int OpenedEventId = 5010;
        private EventTrigger _openedEvent;

        public const int OpenedSmoothlyEventId = 5011;
        private EventTrigger _openedSmoothlyEvent;

        public const int ClosedEventId = 5012;
        private EventTrigger _closedEvent;


        [NonSerialized]
        private Timer _connectionCheckTimer;
        [NonSerialized]
        private OpcQuality _previousOpcQuality;



        protected override void ToRuntime()
        {
            _connectionCheckTimer = new Timer();
            _connectionCheckTimer.Interval = 100;
            _connectionCheckTimer.AutoReset = true;
            _connectionCheckTimer.Elapsed += UpdateIfConnectionChanged;
            _connectionCheckTimer.Start();

            _previousOpcQuality = OpcQuality.Bad;


            string[] splittedString = FullName.Split('.');
            string name = splittedString[splittedString.Length - 1];

            TimeSpan initialInactivity = TimeSpan.FromSeconds(10);

            string message = $"Коллизия концевиков у {name}";
            _collisionEvent = new EventTrigger(this, CollistionEventId, message, initialInactivity);

            message = $"{name} не открылся";
            _notOpenedEvent = new EventTrigger(this, NotOpenedEventId, message, initialInactivity);

            message = $"{name} не закрылся";
            _notClosedEvent = new EventTrigger(this, NotClosedEventId, message, initialInactivity);

            message = $"Соединение с {name} оборвано";
            _connectionDisabledEvent = new EventTrigger(this, ConnectionDisabledEventId, message, initialInactivity);

            message = $"{name} открылся";
            _openedEvent = new EventTrigger(this, OpenedEventId, message, initialInactivity, true);

            message = $"{name} плавно открылся";
            _openedSmoothlyEvent = new EventTrigger(this, OpenedSmoothlyEventId, message, initialInactivity, true);

            message = $"{name} закрылся";
            _closedEvent = new EventTrigger(this, ClosedEventId, message, initialInactivity, true);
        }

        protected override void ToDesign()
        {
            _connectionCheckTimer?.Dispose();
        }

        protected override void UpdateData()
        {
            int statusWord = GetPinValue<int>(StatusWordId);
            if(GetPinQuality(StatusWordId) != OpcQuality.Ok)
                statusWord = 0;

            SetVisualAndUiPin(ConnectionOkId, statusWord.GetBit(ConnectionOkId));
            SetVisualAndUiPin(NotOpenedId, statusWord.GetBit(NotOpenedId));
            SetVisualAndUiPin(NotClosedId, statusWord.GetBit(NotClosedId));
            SetVisualAndUiPin(CollisionId, statusWord.GetBit(CollisionId));
            SetVisualAndUiPin(UsedByAutoModeId, statusWord.GetBit(UsedByAutoModeId));
            SetVisualAndUiPin(OpenedId, statusWord.GetBit(OpenedId));
            SetVisualAndUiPin(SmoothlyOpenedId, statusWord.GetBit(SmoothlyOpenedId));
            SetVisualAndUiPin(ClosedId, statusWord.GetBit(ClosedId));
            SetVisualAndUiPin(OpeningClosingId, statusWord.GetBit(OpeningClosingId));

            SetVisualAndUiPin(BlockClosingId, statusWord.GetBit(BlockClosingId));
            SetVisualAndUiPin(BlockOpeningId, statusWord.GetBit(BlockOpeningId));
            SetVisualAndUiPin(IsSmoothValveId, statusWord.GetBit(IsSmoothValveId));


            int commandWord = 0;
            commandWord.SetBit(0, GetVisualPin<bool>(OpenCmdId));
            commandWord.SetBit(1, GetVisualPin<bool>(OpenSmoothlyCmdId));
            commandWord.SetBit(2, GetVisualPin<bool>(CloseCmdId));
            SetPinValue(CommandWordId, commandWord);


            _collisionEvent.Update(GetPinValue<bool>(CollisionId));
            _notOpenedEvent.Update(GetPinValue<bool>(NotOpenedId));
            _notClosedEvent.Update(GetPinValue<bool>(NotClosedId));
            bool noConnection = !GetPinValue<bool>(ConnectionOkId);
            _connectionDisabledEvent.Update(noConnection);

            _openedEvent.Update(GetPinValue<bool>(OpenedId));
            _openedSmoothlyEvent.Update(GetPinValue<bool>(SmoothlyOpenedId));
            _closedEvent.Update(GetPinValue<bool>(ClosedId));
        }



        private void UpdateIfConnectionChanged(object sender, EventArgs e)
        {
            OpcQuality quality = GetPinQuality(StatusWordId);
            if(quality != _previousOpcQuality)
                UpdateData();

            _previousOpcQuality = quality;
        }

        private void SetVisualAndUiPin(int id, object value)
        {
            SetPinValue(id + 200, value);
            VisualPins.SetPinValue(id + 1000, value);
        }

        private T GetVisualPin<T>(int id)
        {
            return (T)VisualPins.GetPinValue(id + 1000);
        }
    }
}
