using FB;
using FB.VisualFB;
using InSAT.Library.Interop;
using InSAT.OPC;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
        private ControllerProtocol _enumProtocol;
        private SLMP_area _enumSLMP_area = SLMP_area.R;

        #region VisualProperties

        private uint _uFloatBaseAddr        = Params.UFloatBaseAddr;
        private uint _uFloatAreaSize        = Params.UFloatAreaSize;
        private uint _uIntBaseAddr          = Params.UIntBaseAddr;
        private uint _uIntAreaSize          = Params.UIntAreaSize;
        private uint _uBoolBaseAddr         = Params.UBoolBaseAddr;
        private uint _uBoolAreaSize         = Params.UBoolAreaSize;
        private uint _uControlBaseAddr      = Params.UControlBaseAddr;
        private uint _conntrollerIP1        = Params.ConntrollerIP1;
        private uint _conntrollerIP2        = Params.ConntrollerIP2;
        private uint _conntrollerIP3        = Params.ConntrollerIP3;
        private uint _conntrollerIP4        = Params.ConntrollerIP4;
        private uint _conntrollerTCPPort    = Params.ConntrollerTCPPort;
        private uint _timeout               = Params.Timeout;

        [DisplayName(" 1. Протокол обмена передачи данных в контроллер")]
        [Description("Определяет по какому протоколу передаются данные в контроллер")]
        public MbeTableFB.ControllerProtocol enumProtocol
        {
            get => _enumProtocol;
            set => _enumProtocol = value;
        }

        [DisplayName(" 2. Пространство хранения данных при использовании SLMP")]
        [Description("Определяет в какой области (D или R) помещаются данные таблицы")]
        public MbeTableFB.SLMP_area enumSLMP_area
        {
            get => _enumSLMP_area;
            set => _enumSLMP_area = value;
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
            VisualPins.SetPinValue(Params.ID_HMI_CommProtocol, _enumProtocol == ControllerProtocol.Modbus ? 1 : (_enumProtocol == ControllerProtocol.SLMP_not_implimated ? 2 : 0));
            VisualPins.SetPinValue(Params.ID_HMI_AddrArea, _enumSLMP_area == SLMP_area.D ? 1 : (_enumSLMP_area == SLMP_area.R ? 2 : 0));
            VisualPins.SetPinValue(Params.ID_HMI_FloatBaseAddr, Params.UFloatBaseAddr);
            VisualPins.SetPinValue(Params.ID_HMI_FloatAreaSize, Params.UFloatAreaSize);
            VisualPins.SetPinValue(Params.ID_HMI_IntBaseAddr, Params.UIntBaseAddr);
            VisualPins.SetPinValue(Params.ID_HMI_IntAreaSize, Params.UIntAreaSize);
            VisualPins.SetPinValue(Params.ID_HMI_BoolBaseAddr, Params.UBoolBaseAddr);
            VisualPins.SetPinValue(Params.ID_HMI_BoolAreaSize, Params.UBoolAreaSize);
            VisualPins.SetPinValue(Params.ID_HMI_ControlBaseAddr, Params.UControlBaseAddr);
            VisualPins.SetPinValue(Params.ID_HMI_IP1, Params.ConntrollerIP1);
            VisualPins.SetPinValue(Params.ID_HMI_IP2, Params.ConntrollerIP2);
            VisualPins.SetPinValue(Params.ID_HMI_IP3, Params.ConntrollerIP3);
            VisualPins.SetPinValue(Params.ID_HMI_IP4, Params.ConntrollerIP4);
            VisualPins.SetPinValue(Params.ID_HMI_Port, Params.ConntrollerTCPPort);
            VisualPins.SetPinValue(Params.ID_HMI_Timeout, Params.Timeout);
            
            //todo: naming
            int num1 = GetPinInt(Params.ID_ActualLine);
            
            bool flag1 = GetPinQuality(Params.ID_ActualLine) == OpcQuality.Good;
            bool pinBool = GetPinBool(Params.ID_EnaLoad);
            bool flag2 = GetPinQuality(Params.ID_EnaLoad) == OpcQuality.Good;
            
            if (!flag1)
                num1 = -1;
            uint num2 = (uint)(0 + (pinBool ? 1 : 0) + (flag2 ? 2 : 0) + (flag1 ? 4 : 0));

            VisualPins.SetPinValue(Params.ID_HMI_ActualLine, Params.UBoolAreaSize);
            VisualPins.SetPinValue(Params.ID_HMI_Status, Params.UBoolAreaSize);

            SendPinsToVFB();
        }

        private void SendPinsToVFB()
        {
            UpdatePins(Params.ShutterNameQuantity, Params.FirstPinShutterName, Params.GroupID_ShutterNames);
            UpdatePins(Params.HeaterNameQuantity, Params.FirstPinHeaterName, Params.GroupID_HeaterNames);
        }

        private void UpdatePins(int quantity, int firstBit, int groupID)
        {
            for (int i = 0; i < quantity; i++)
            {
                var value = GetPinValue(PinDef.CreateID(i, groupID));
                VisualPins.SetPinValue(firstBit + i, value);
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
