using FB;
using FB.VisualFB;
using InSAT.Library.Interop;
using NtoLib.Utils;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NtoLib.Valves
{
    [Serializable]
    [ComVisible(true)]
    [Guid("89DB76A4-6551-40D7-BBD0-64734F158B5B")]
    [CatID(CatIDs.CATID_OTHER)]
    [DisplayName("Шибер или клапан")]
    [VisualControls(typeof(ValveControl))]
    public class ValveFB : VisualFBBase
    {
        public const int IsSmoothValveId = 1;
        public const int ConnectionOkId = 2;
        public const int AutoModeId = 3;
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



        protected override void ToRuntime()
        {
            string[] splittedString = FullName.Split('.');
            string name = splittedString[splittedString.Length - 1];


            string message = $"Коллизия концевиков у {name}!";
            _collisionEvent = new EventTrigger(this, CollistionEventId, message);

            message = $"{name} не открылся!";
            _notOpenedEvent = new EventTrigger(this, NotOpenedEventId, message);

            message = $"{name} не закрылся!";
            _notClosedEvent = new EventTrigger(this, NotClosedEventId, message);

            message = $"Соединение с {name} оборвано!";
            _connectionDisabledEvent = new EventTrigger(this, ConnectionDisabledEventId, message);

            message = $"{name} открылся";
            _openedEvent = new EventTrigger(this, OpenedEventId, message, true);

            message = $"{name} плавно открылся";
            _openedSmoothlyEvent = new EventTrigger(this, OpenedSmoothlyEventId, message, true);

            message = $"{name} закрылся";
            _closedEvent = new EventTrigger(this, ClosedEventId, message, true);
        }

        protected override void UpdateData()
        {
            RetransmitPin(IsSmoothValveId);
            RetransmitPin(ConnectionOkId);
            RetransmitPin(AutoModeId);
            RetransmitPin(OpenedId);
            RetransmitPin(SmoothlyOpenedId);
            RetransmitPin(ClosedId);
            RetransmitPin(OpeningClosingId);
            RetransmitPin(BlockOpeningId);
            RetransmitPin(BlockClosingId);
            RetransmitPin(CollisionId);
            RetransmitPin(NotOpenedId);
            RetransmitPin(NotClosedId);

            RetransmitVisualPin(OpenCmdId);
            RetransmitVisualPin(CloseCmdId);
            RetransmitVisualPin(OpenSmoothlyCmdId);


            _collisionEvent.Update(GetPinValue<bool>(CollisionId));
            _notOpenedEvent.Update(GetPinValue<bool>(NotOpenedId));
            _notClosedEvent.Update(GetPinValue<bool>(NotClosedId));
            _connectionDisabledEvent.Update(!GetPinValue<bool>(ConnectionOkId));

            _openedEvent.Update(GetPinValue<bool>(OpenedId));
            _openedSmoothlyEvent.Update(GetPinValue<bool>(SmoothlyOpenedId));
            _closedEvent.Update(GetPinValue<bool>(ClosedId));
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
    }
}
