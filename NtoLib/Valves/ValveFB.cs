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
        public const int StatusWordId = 1;
        public const int CommandWordId = 5;


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
            _connectionCheckTimer.Elapsed += CheckConnection;
            _connectionCheckTimer.Start();

            _previousOpcQuality = GetPinQuality(StatusWordId);


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
            int statusWord = 0;
            if(GetPinQuality(StatusWordId) == OpcQuality.Ok)
                statusWord = GetPinValue<int>(StatusWordId);

            bool connectionOk = statusWord.GetBit(0);
            SetVisualAndUiPin(ConnectionOkId, connectionOk);
            bool notOpened = statusWord.GetBit(1);
            SetVisualAndUiPin(NotOpenedId, notOpened);
            bool notClosed = statusWord.GetBit(2);
            SetVisualAndUiPin(NotClosedId, notClosed);
            bool collision = statusWord.GetBit(3);
            SetVisualAndUiPin(CollisionId, collision);
            SetVisualAndUiPin(UsedByAutoModeId, statusWord.GetBit(4));
            bool opened = statusWord.GetBit(5);
            SetVisualAndUiPin(OpenedId, opened);
            bool openedSmoothly = statusWord.GetBit(6);
            SetVisualAndUiPin(SmoothlyOpenedId, openedSmoothly);
            bool closed = statusWord.GetBit(7);
            SetVisualAndUiPin(ClosedId, closed);
            SetVisualAndUiPin(OpeningClosingId, statusWord.GetBit(8));

            SetVisualAndUiPin(BlockClosingId, statusWord.GetBit(13));
            SetVisualAndUiPin(BlockOpeningId, statusWord.GetBit(14));
            SetVisualAndUiPin(IsSmoothValveId, statusWord.GetBit(15));


            int commandWord = 0;
            commandWord.SetBit(0, GetVisualPin<bool>(OpenCmdId));
            commandWord.SetBit(1, GetVisualPin<bool>(OpenSmoothlyCmdId));
            commandWord.SetBit(2, GetVisualPin<bool>(CloseCmdId));
            SetPinValue(CommandWordId, commandWord);



            _collisionEvent.Update(collision);
            _notOpenedEvent.Update(notOpened);
            _notClosedEvent.Update(notClosed);
            _connectionDisabledEvent.Update(!connectionOk);

            _openedEvent.Update(opened);
            _openedSmoothlyEvent.Update(openedSmoothly);
            _closedEvent.Update(closed);
        }



        private void CheckConnection(object sender, EventArgs e)
        {
            OpcQuality quality = GetPinQuality(StatusWordId);
            if(quality != _previousOpcQuality)
                UpdateData();

            _previousOpcQuality = quality;
        }

        /// <summary>
        /// Пересылает информацию с внешнего входа на соответствующий выход для UI
        /// и VisualPin
        /// </summary>
        /// <param name="id"></param>
        private void RetransmitPin(int id)
        {
            object value = GetPinValue(id);

            SetPinValue(id + 200, value);
            VisualPins.SetPinValue(id + 1000, value);
        }

        /// <summary>
        /// Пересылает информацию с входа с контрола на внешние выходы
        /// </summary>
        /// <param name="id"></param>
        private void RetransmitVisualPin(int id)
        {
            object value = VisualPins.GetPinValue(id + 1000);
            SetPinValue(id, value);
        }



        private void SetVisualAndUiPin(int id, object value)
        {
            SetPinValue(id + 100, value);
            VisualPins.SetPinValue(id + 1000, value);
        }

        private T GetVisualPin<T>(int id)
        {
            return (T)VisualPins.GetPinValue(id + 1000);
        }

        private int SetBit(int word, int index, bool value)
        {
            int valueInt = value ? 1 : 0;
            return word | (valueInt << index);
        }

        private bool GetBit(int word, int index)
        {
            return (word & (1 << index)) != 0;
        }
    }
}
