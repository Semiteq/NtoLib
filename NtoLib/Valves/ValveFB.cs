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
        public const int CommandWordId = 100;

        public const int IsSmoothValveId = 1;
        public const int ConnectionOkId = 2;
        public const int UsedByAutoModeId = 3;
        public const int OpenedId = 4;
        public const int SmoothlyOpenedId = 5;
        public const int ClosedId = 6;
        public const int OpeningClosingId = 7;
        public const int BlockOpeningId = 8;
        public const int BlockClosingId = 9;
        public const int CollisionId = 10;
        public const int NotOpenedId = 11;
        public const int NotClosedId = 12;


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

            bool ConnectionOk = GetBit(statusWord, 0);
            SetVisualAndUiPin(ConnectionOkId, ConnectionOk);
            bool NotOpened = GetBit(statusWord, 1);
            SetVisualAndUiPin(NotOpenedId, NotOpened);
            bool NotClosed = GetBit(statusWord, 2);
            SetVisualAndUiPin(NotClosedId, NotClosed);
            bool Collision = GetBit(statusWord, 3);
            SetVisualAndUiPin(CollisionId, Collision);
            bool UsedByAutoMode = GetBit(statusWord, 4);
            SetVisualAndUiPin(UsedByAutoModeId, UsedByAutoMode);
            bool Opened = GetBit(statusWord, 5);
            SetVisualAndUiPin(OpenedId, Opened);
            bool SmoothlyOpened = GetBit(statusWord, 6);
            SetVisualAndUiPin(SmoothlyOpenedId, SmoothlyOpened);
            bool Closed = GetBit(statusWord, 7);
            SetVisualAndUiPin(ClosedId, Closed);
            bool OpeningClosing = GetBit(statusWord, 8);
            SetVisualAndUiPin(OpeningClosingId, OpeningClosing);

            bool blockClosing = GetBit(statusWord, 13);
            SetVisualAndUiPin(BlockClosingId, blockClosing);
            bool blockOpening = GetBit(statusWord, 14);
            SetVisualAndUiPin(BlockOpeningId, blockOpening);
            bool isSmoothValve = GetBit(statusWord, 15);
            SetVisualAndUiPin(IsSmoothValveId, isSmoothValve);



            bool openCommand = GetVisualPin<bool>(OpenCmdId);
            bool openSmoothlyCommand = GetVisualPin<bool>(OpenSmoothlyCmdId);
            bool closeCommand = GetVisualPin<bool>(CloseCmdId);

            int commandWord = 0;
            commandWord = SetBit(commandWord, 0, openCommand);
            commandWord = SetBit(commandWord, 1, openSmoothlyCommand);
            commandWord = SetBit(commandWord, 2, closeCommand);
            SetPinValue(CommandWordId, commandWord);



            _collisionEvent.Update(GetPinValue<bool>(CollisionId));
            _notOpenedEvent.Update(GetPinValue<bool>(NotOpenedId));
            _notClosedEvent.Update(GetPinValue<bool>(NotClosedId));
            _connectionDisabledEvent.Update(!GetPinValue<bool>(ConnectionOkId));

            _openedEvent.Update(GetPinValue<bool>(OpenedId));
            _openedSmoothlyEvent.Update(GetPinValue<bool>(SmoothlyOpenedId));
            _closedEvent.Update(GetPinValue<bool>(ClosedId));
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
            SetPinValue(id + 200, value);
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
