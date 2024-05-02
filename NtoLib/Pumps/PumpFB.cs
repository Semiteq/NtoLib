using FB;
using FB.VisualFB;
using InSAT.Library.Interop;
using InSAT.OPC;
using MasterSCADA.Hlp;
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
        private PumpType _pumpType;
        [DisplayName("Тип насоса")]
        public PumpType PumpType
        {
            get
            {
                return _pumpType;
            }
            set
            {
                _pumpType = value;
                RecreatePinMap();
            }
        }

        [DisplayName("Показывать лампочку \"Нет соединения\"")]
        [Description("Переключает отображение лампочки \"Нет соединения\" в окне параметров насоса")]
        public bool UseNoConnectionLamp { get; set; }

        [DisplayName("Отображать температуру")]
        [Description("Переключает отображение температуры в окне параметров насоса")]
        public bool UseTemperatureLabel { get; set; }

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
        public const int Message1Id = 8;
        public const int Message2Id = 9;
        public const int Message3Id = 10;
        public const int CustomId = 11;
        public const int ForceStopId = 12;
        public const int BlockStartId = 13;
        public const int BlockStopId = 14;
        public const int UseId = 15;

        public const int TemperatureId = 20;


        public const int DynamicGroupId = 50;

        public const int TurbineSpeedId = 60;

        public const int IonPumpVoltage = 70;
        public const int IonPumpCurrent = 75;
        public const int IonPumpPower = 80;

        public const int CryoInTemperature = 90;
        public const int CryoOutTemperature = 95;


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

            message = $"Начало разгона {name}";
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
            bool workOnNominalSpeed = statusWord.GetBit(WorkOnNominalSpeedId);
            SetVisualAndUiPin(WorkOnNominalSpeedId, workOnNominalSpeed);
            bool stopped = statusWord.GetBit(StoppedId);
            SetVisualAndUiPin(StoppedId, stopped);
            bool accelerating = statusWord.GetBit(AcceleratingId);
            SetVisualAndUiPin(AcceleratingId, accelerating);
            bool decelerating = statusWord.GetBit(DeceseratingId);
            SetVisualAndUiPin(DeceseratingId, decelerating);
            SetVisualAndUiPin(WarningId, statusWord.GetBit(WarningId));
            SetVisualAndUiPin(Message1Id, statusWord.GetBit(Message1Id));
            SetVisualAndUiPin(Message2Id, statusWord.GetBit(Message2Id));
            SetVisualAndUiPin(Message3Id, statusWord.GetBit(Message3Id));
            SetVisualAndUiPin(CustomId, statusWord.GetBit(CustomId));
            SetVisualAndUiPin(ForceStopId, statusWord.GetBit(ForceStopId));
            SetVisualAndUiPin(BlockStartId, statusWord.GetBit(BlockStartId));
            SetVisualAndUiPin(BlockStopId, statusWord.GetBit(BlockStopId));
            SetVisualAndUiPin(UseId, statusWord.GetBit(UseId));


            switch(PumpType)
            {
                case PumpType.Forvacuum:
                {

                    break;
                }
                case PumpType.Turbine:
                {
                    SetVisualPin(TurbineSpeedId, GetPinValue<float>(TurbineSpeedId));
                    break;
                }
                case PumpType.Ion:
                {
                    SetVisualPin(IonPumpVoltage, GetPinValue<float>(IonPumpVoltage));
                    SetVisualPin(IonPumpCurrent, GetPinValue<float>(IonPumpCurrent));
                    SetVisualPin(IonPumpPower, GetPinValue<float>(IonPumpPower));
                    break;
                }
                case PumpType.Cryogen:
                {
                    SetVisualPin(CryoInTemperature, GetPinValue<float>(CryoInTemperature));
                    SetVisualPin(CryoOutTemperature, GetPinValue<float>(CryoOutTemperature));
                    break;
                }
            }


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



        protected override void CreatePinMap(bool newObject)
        {
            base.CreatePinMap(newObject);

            FBGroup group = (FBGroup)_mapIDtoItem[DynamicGroupId];
            switch(PumpType)
            {
                case PumpType.Forvacuum:
                {

                    break;
                }
                case PumpType.Turbine:
                {
                    group.AddPinWithID(TurbineSpeedId, "Speed", PinType.Pin, typeof(float), 0d);
                    break;
                }
                case PumpType.Ion:
                {
                    group.AddPinWithID(IonPumpVoltage, "Voltage", PinType.Pin, typeof(float), 0d);
                    group.AddPinWithID(IonPumpCurrent, "Current", PinType.Pin, typeof(float), 0d);
                    group.AddPinWithID(IonPumpPower, "Power", PinType.Pin, typeof(float), 0d);
                    break;
                }
                case PumpType.Cryogen:
                {
                    group.AddPinWithID(CryoInTemperature, "TemperatureIn", PinType.Pin, typeof(float), 0d);
                    group.AddPinWithID(CryoOutTemperature, "TemperatureOut", PinType.Pin, typeof(float), 0d);
                    break;
                }
            }

            FirePinSpaceChanged();
        }



        protected override OpcQuality GetConnectionQuality()
        {
            return GetPinQuality(StatusWordId);
        }
    }
}
