using FB;
using FB.VisualFB;
using InSAT.Library.Interop;
using InSAT.OPC;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NtoLib.Recipes.MbeTable
{
    [CatID(CatIDs.CATID_OTHER)]
    [Guid("DFB05172-07CD-492C-925E-A091B197D8A8")]
    [FBOptions(FBOptions.EnableChangeConfigInRT)]
    [VisualControls(typeof(TableControl))]
    [DisplayName("Таблица рецептов MBE")]
    [ComVisible(true)]
    [Serializable]
    public class MbeTableFB : VisualFBBase
    {
        private const int ID_ActualLine = 1;
        private const int ID_EnaLoad = 2;

        private const int GroupID_ActTemperaure = 100;
        private const int GroupID_ActPower = 200;
        private const int GroupID_ActLoopCount = 300;

        public const int ID_HMI_CommProtocol = 1001;
        public const int ID_HMI_AddrArea = 1002;
        public const int ID_HMI_FloatBaseAddr = 1003;
        public const int ID_HMI_FloatAreaSize = 1004;
        public const int ID_HMI_IntBaseAddr = 1005;
        public const int ID_HMI_IntAreaSize = 1006;
        public const int ID_HMI_BoolBaseAddr = 1007;
        public const int ID_HMI_BoolAreaSize = 1008;
        public const int ID_HMI_ControlBaseAddr = 1009;
        public const int ID_HMI_IP1 = 1010;
        public const int ID_HMI_IP2 = 1011;
        public const int ID_HMI_IP3 = 1012;
        public const int ID_HMI_IP4 = 1013;
        public const int ID_HMI_Port = 1014;
        public const int ID_HMI_Timeout = 1015;

        public const int ID_HMI_ActualLine = 1016;
        public const int ID_HMI_Status = 1017;

        private MbeTableFB.ControllerProtocol _enumProtocol;
        private MbeTableFB.SLMP_area _enumSLMP_area = MbeTableFB.SLMP_area.R;
        private uint _uFloatBaseAddr;
        private uint _uFloatAreaSize = 100;
        private uint _uIntBaseAddr = 100;
        private uint _uIntAreaSize = 100;
        private uint _uBoolBaseAddr = 200;
        private uint _uBoolAreaSize = 100;
        private uint _uControlBaseAddr = 200;
        private uint _conntrollerIP1 = 192;
        private uint _conntrollerIP2 = 168;
        private uint _conntrollerIP3;
        private uint _conntrollerIP4 = 1;
        private uint _conntrollerTCPPort = 502;
        private uint _timeout = 1000;

        #region Properties

        [DisplayName(" 1. Протокол обмена передачи данных в контроллер")]
        [Description("Определяет по какому протоколу передаются данные в контроллер")]
        public MbeTableFB.ControllerProtocol enumProtocol
        {
            get => this._enumProtocol;
            set => this._enumProtocol = value;
        }

        [DisplayName(" 2. Пространство хранения данных при использовании SLMP")]
        [Description("Определяет в какой области (D или R) помещаются данные таблицы")]
        public MbeTableFB.SLMP_area enumSLMP_area
        {
            get => this._enumSLMP_area;
            set => this._enumSLMP_area = value;
        }

        [Description("Определяет начальный адрес, куда помещаются данные типа 'вещественный'")]
        [DisplayName(" 3.  Базовый адрес хранения данных типа Real (Float)")]
        public uint uFloatBaseAddr
        {
            get => this._uFloatBaseAddr;
            set => this._uFloatBaseAddr = value;
        }

        [Description("Определяет размер области для данных типа 'вещественный'. в 16-тибитных словах (2 слова на переменную). Если Используется например область с адресами 100..199, то это 100 слов или 50 переменных типа float. Укажите в это параметре 100.")]
        [DisplayName(" 4.  Размер области хранения данных типа Real (Float)")]
        public uint uFloatAreaSize
        {
            get => this._uFloatAreaSize;
            set => this._uFloatAreaSize = value;
        }

        [Description("Определяет начальный адрес, куда помещаются данные типа 'целый 16 бит'")]
        [DisplayName(" 5.  Базовый адрес хранения данных типа Int")]
        public uint uIntBaseAddr
        {
            get => this._uIntBaseAddr;
            set => this._uIntBaseAddr = value;
        }

        [DisplayName(" 6.  Размер области хранения данных типа Int")]
        [Description("Определяет размер области для данных типа 'целый 16 бит'")]
        public uint uIntAreaSize
        {
            get => this._uIntAreaSize;
            set => this._uIntAreaSize = value;
        }

        [DisplayName(" 7.  Базовый адрес хранения данных типа Boolean")]
        [Description("Определяет начальный адрес, куда помещаются данные типа 'логический'. Упаковываются в 16ти битные слова.")]
        public uint uBoolBaseAddr
        {
            get => this._uBoolBaseAddr;
            set => this._uBoolBaseAddr = value;
        }

        [Description("Определяет размер области для данных типа 'логический'. Определяется в 16-ти битных словах")]
        [DisplayName(" 8.  Размер области хранения данных типа Boolean")]
        public uint uBoolAreaSize
        {
            get => this._uBoolAreaSize;
            set => this._uBoolAreaSize = value;
        }

        [DisplayName(" 9.  Базовый адрес контрольной области")]
        [Description("Определяет начальный адрес, где располагается зона контрольных данных (3 слова)")]
        public uint uControlBaseAddr
        {
            get => this._uControlBaseAddr;
            set => this._uControlBaseAddr = value;
        }

        [Description("IP адрес контроллера байт 1")]
        [DisplayName("10.  IP адрес контроллера байт 1")]
        public uint conntrollerIP1
        {
            get => this._conntrollerIP1;
            set => this._conntrollerIP1 = value;
        }

        [DisplayName("11.  IP адрес контроллера байт 2")]
        [Description("IP адрес контроллера байт 2")]
        public uint conntrollerIP2
        {
            get => this._conntrollerIP2;
            set => this._conntrollerIP2 = value;
        }

        [Description("IP адрес контроллера байт 3")]
        [DisplayName("12.  IP адрес контроллера байт 3")]
        public uint conntrollerIP3
        {
            get => this._conntrollerIP3;
            set => this._conntrollerIP3 = value;
        }

        [Description("IP адрес контроллера байт 4")]
        [DisplayName("13.  IP адрес контроллера байт 4")]
        public uint conntrollerIP4
        {
            get => this._conntrollerIP4;
            set => this._conntrollerIP4 = value;
        }

        [DisplayName("14.  TCP порт")]
        [Description("TCP порт")]
        public uint conntrollerTCPPort
        {
            get => this._conntrollerTCPPort;
            set => this._conntrollerTCPPort = value;
        }

        [DisplayName("15.  Timeout")]
        [Description("Timeout")]
        public uint timeout
        {
            get => this._timeout;
            set => this._timeout = value;
        }
        #endregion


        protected override void ToRuntime() { }

        protected override void ToDesign() { }

        protected override void UpdateData()
        {
            ((FBBase)this.VisualPins).SetPinValue(1001, (object)(this._enumProtocol == MbeTableFB.ControllerProtocol.Modbus ? 1 : (this._enumProtocol == MbeTableFB.ControllerProtocol.SLMP_not_implimated ? 2 : 0)));
            ((FBBase)this.VisualPins).SetPinValue(1002, (object)(this._enumSLMP_area == MbeTableFB.SLMP_area.D ? 1 : (this._enumSLMP_area == MbeTableFB.SLMP_area.R ? 2 : 0)));
            ((FBBase)this.VisualPins).SetPinValue(1003, (object)this._uFloatBaseAddr);
            ((FBBase)this.VisualPins).SetPinValue(1004, (object)this._uFloatAreaSize);
            ((FBBase)this.VisualPins).SetPinValue(1005, (object)this._uIntBaseAddr);
            ((FBBase)this.VisualPins).SetPinValue(1006, (object)this._uIntAreaSize);
            ((FBBase)this.VisualPins).SetPinValue(1007, (object)this._uBoolBaseAddr);
            ((FBBase)this.VisualPins).SetPinValue(1008, (object)this._uBoolAreaSize);
            ((FBBase)this.VisualPins).SetPinValue(1009, (object)this._uControlBaseAddr);
            ((FBBase)this.VisualPins).SetPinValue(1010, (object)this._conntrollerIP1);
            ((FBBase)this.VisualPins).SetPinValue(1011, (object)this._conntrollerIP2);
            ((FBBase)this.VisualPins).SetPinValue(1012, (object)this._conntrollerIP3);
            ((FBBase)this.VisualPins).SetPinValue(1013, (object)this._conntrollerIP4);
            ((FBBase)this.VisualPins).SetPinValue(1014, (object)this._conntrollerTCPPort);
            ((FBBase)this.VisualPins).SetPinValue(1015, (object)this._timeout);
            int num1 = ((FBBase)this).GetPinInt(1);
            bool flag1 = ((FBBase)this).GetPinQuality(1) == OpcQuality.Good;
            bool pinBool = ((FBBase)this).GetPinBool(2);
            bool flag2 = ((FBBase)this).GetPinQuality(2) == OpcQuality.Good;
            if (!flag1)
                num1 = -1;
            uint num2 = (uint)(0 + (pinBool ? 1 : 0) + (flag2 ? 2 : 0) + (flag1 ? 4 : 0));
            ((FBBase)this.VisualPins).SetPinValue(1016, (object)num1);
            ((FBBase)this.VisualPins).SetPinValue(1017, (object)num2);

            SendPinsToVFB();
        }

        private void SendPinsToVFB()
        {
            object[] actTemperature = new object[16];
            object[] actPower = new object[16];
            object[] actLoopAcount = new object[5];

            for (int i = 0; i < 16; i++)
            {
                actTemperature[i] = GetPinValue(PinDef.CreateID(i, GroupID_ActTemperaure));
                VisualPins.SetPinValue(1101 + i, actTemperature[i]);

                actPower[i] = GetPinValue(PinDef.CreateID(i, GroupID_ActPower));
                VisualPins.SetPinValue(1201 + i, actPower[i]);
            }

            for (int i = 0; i < 5; i++)
            {
                actLoopAcount[i] = GetPinValue(PinDef.CreateID(i, GroupID_ActLoopCount));
                VisualPins.SetPinValue(1301 + i, actLoopAcount[i]);
            }
        }

        public enum ControllerProtocol
        {
            Modbus,
            SLMP_not_implimated,
        }

        public enum SLMP_area
        {
            D,
            R,
        }
    }
}
