using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using FB;
using FB.VisualFB;
using InSAT.Library.Interop;
using InSAT.OPC;
using MasterSCADA.Hlp;
using NtoLib.Recipes.MbeTable.IO;
using NtoLib.Recipes.MbeTable.PinDataManager;
using CommunicationSettings = NtoLib.Recipes.MbeTable.IO.CommunicationSettings;

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
        [NonSerialized]
        private ServiceProvider _serviceProvider; 
        public ServiceProvider ServiceProvider => _serviceProvider ??= new ServiceProvider();
        
        [NonSerialized] private IPlcStateMonitor _plcStateMonitor;
        [NonSerialized] private IActionTargetProvider _actionTargetProvider;
        [NonSerialized] private ICommunicationSettingsProvider _commSettingsProvider;
        
        // Pin IDs
        private const int IdRecipeActive = 1;
        private const int IdRecipePaused = 2;
        private const int IdActualLineNumber = 3;
        private const int IdStepCurrentTime = 4;
        private const int IdForLoopCount1 = 5;
        private const int IdForLoopCount2 = 6;
        private const int IdForLoopCount3 = 7;
        private const int IdEnaLoad = 8;
        private const int IdTotalTimeLeft = 101;
        private const int IdLineTimeLeft = 102;

        // ActionProperty pins
        private const int ShutterNamesGroupId = 200;
        private const int HeaterNamesGroupId = 300;
        private const int NitrogenSourcesGroupId = 400;
        
        private const int IdFirstShutterName = 201;
        private const int IdFirstHeaterName = 301;
        private const int IdFirstNitrogenSourceName = 401;

        private const int ShutterNameQuantity = 32;
        private const int HeaterNameQuantity = 32;
        private const int NitrogenSourceNameQuantity = 3;

        // Communication pins
        private const int IdHmiFloatBaseAddr = 1003;
        private const int IdHmiFloatAreaSize = 1004;
        private const int IdHmiIntBaseAddr = 1005;
        private const int IdHmiIntAreaSize = 1006;
        private const int IdHmiBoolBaseAddr = 1007;
        private const int IdHmiBoolAreaSize = 1008;
        private const int IdHmiControlBaseAddr = 1009;
        private const int IdHmiIp1 = 1010;
        private const int IdHmiIp2 = 1011;
        private const int IdHmiIp3 = 1012;
        private const int IdHmiIp4 = 1013;
        private const int IdHmiPort = 1014;

        private uint _uFloatBaseAddr = 8100;
        private uint _uFloatAreaSize = 19600;

        private uint _uIntBaseAddr = 27700;
        private uint _uIntAreaSize = 1400;

        private uint _uBoolBaseAddr = 29100;
        private uint _uBoolAreaSize = 50;

        private uint _uControlBaseAddr = 8000;

        private uint _controllerIp1 = 192;
        private uint _controllerIp2 = 168;
        private uint _controllerIp3 = 0;
        private uint _controllerIp4 = 141;

        private uint _controllerTcpPort = 502;

        #region VisualProperties
        
        [Description("Определяет начальный адрес, куда помещаются данные типа 'вещественный'")]
        [DisplayName(" 1.  Базовый адрес хранения данных типа Real (Float)")]
        public uint UFloatBaseAddr
        {
            get => _uFloatBaseAddr;
            set => _uFloatBaseAddr = value;
        }

        [Description("Определяет размер области для данных типа 'вещественный'.")]
        [DisplayName(" 2.  Размер области хранения данных типа Real (Float)")]
        public uint UFloatAreaSize
        {
            get => _uFloatAreaSize;
            set => _uFloatAreaSize = value;
        }

        [Description("Определяет начальный адрес, куда помещаются данные типа 'целый 16 бит'")]
        [DisplayName(" 3.  Базовый адрес хранения данных типа Int")]
        public uint UIntBaseAddr
        {
            get => _uIntBaseAddr;
            set => _uIntBaseAddr = value;
        }

        [DisplayName(" 4.  Размер области хранения данных типа Int")]
        [Description("Определяет размер области для данных типа 'целый 16 бит'")]
        public uint UIntAreaSize
        {
            get => _uIntAreaSize;
            set => _uIntAreaSize = value;
        }

        [DisplayName(" 5.  Базовый адрес хранения данных типа Boolean")]
        [Description("Определяет начальный адрес, куда помещаются данные типа 'логический'.")]
        public uint UBoolBaseAddr
        {
            get => _uBoolBaseAddr;
            set => _uBoolBaseAddr = value;
        }

        [Description("Определяет размер области для данных типа 'логический'.")]
        [DisplayName(" 6.  Размер области хранения данных типа Boolean")]
        public uint UBoolAreaSize
        {
            get => _uBoolAreaSize;
            set => _uBoolAreaSize = value;
        }

        [DisplayName(" 7.  Базовый адрес контрольной области")]
        [Description("Определяет начальный адрес, где располагается зона контрольных данных (3 слова)")]
        public uint UControlBaseAddr
        {
            get => _uControlBaseAddr;
            set => _uControlBaseAddr = value;
        }

        [Description("IP адрес контроллера байт 1")]
        [DisplayName("8.  IP адрес контроллера байт 1")]
        public uint ControllerIp1
        {
            get => _controllerIp1;
            set => _controllerIp1 = value;
        }

        [DisplayName("9.  IP адрес контроллера байт 2")]
        [Description("IP адрес контроллера байт 2")]
        public uint ControllerIp2
        {
            get => _controllerIp2;
            set => _controllerIp2 = value;
        }

        [Description("IP адрес контроллера байт 3")]
        [DisplayName("10.  IP адрес контроллера байт 3")]
        public uint ControllerIp3
        {
            get => _controllerIp3;
            set => _controllerIp3 = value;
        }

        [Description("IP адрес контроллера байт 4")]
        [DisplayName("11.  IP адрес контроллера байт 4")]
        public uint ControllerIp4
        {
            get => _controllerIp4;
            set => _controllerIp4 = value;
        }

        [DisplayName("12.  TCP порт")]
        [Description("TCP порт")]
        public uint ControllerTcpPort
        {
            get => _controllerTcpPort;
            set => _controllerTcpPort = value;
        }

        #endregion
        
        protected override void UpdateData()
        {
            base.UpdateData();

            var stepCurrentTime = GetPinValue<float>(IdStepCurrentTime);
            var lineNumber = GetPinValue<int>(IdActualLineNumber);
            var forLoopCount1 = GetPinValue<int>(IdForLoopCount1);
            var forLoopCount2 = GetPinValue<int>(IdForLoopCount2);
            var forLoopCount3 = GetPinValue<int>(IdForLoopCount3);
            
            _plcStateMonitor.UpdateState(lineNumber, forLoopCount1, forLoopCount2, forLoopCount3, stepCurrentTime);
            
            UpdateUiConnectionPins();
        }
        
        private void InitializeServices()
        {
            _serviceProvider = ServiceProvider;
            _serviceProvider.InitializeServices(this); 

            _plcStateMonitor = _serviceProvider.PlcStateMonitor;
            _actionTargetProvider = _serviceProvider.ActionTargetProvider;
            
            _actionTargetProvider.RefreshTargets(this);
        }
        
        protected override void ToDesign()
        {
            base.ToDesign();
            InitializeServices();
            CleanupServices();
        }
        
        private void UpdateUiConnectionPins()
        {
            VisualPins.SetValue<uint>(IdHmiFloatBaseAddr, UFloatBaseAddr);
            VisualPins.SetValue<uint>(IdHmiFloatAreaSize, UFloatAreaSize);
            VisualPins.SetValue<uint>(IdHmiIntBaseAddr, UIntBaseAddr);
            VisualPins.SetValue<uint>(IdHmiIntAreaSize, UIntAreaSize);
            VisualPins.SetValue<uint>(IdHmiBoolBaseAddr, UBoolBaseAddr);
            VisualPins.SetValue<uint>(IdHmiBoolAreaSize, UBoolAreaSize);
            VisualPins.SetValue<uint>(IdHmiControlBaseAddr, UControlBaseAddr);
            VisualPins.SetValue<uint>(IdHmiIp1, ControllerIp1);
            VisualPins.SetValue<uint>(IdHmiIp2, ControllerIp2);
            VisualPins.SetValue<uint>(IdHmiIp3, ControllerIp3);
            VisualPins.SetValue<uint>(IdHmiIp4, ControllerIp4);
            VisualPins.SetValue<uint>(IdHmiPort, ControllerTcpPort);
        }
        
        protected override void ToRuntime()
        {
            base.ToRuntime();
            
            if (_serviceProvider == null)
            {
                _serviceProvider = new ServiceProvider();
                _serviceProvider.InitializeServices(this);
            }
            
            _plcStateMonitor = _serviceProvider.PlcStateMonitor;
            _actionTargetProvider = _serviceProvider.ActionTargetProvider;
            
            _actionTargetProvider.RefreshTargets(this);
        }
        
        public override void Dispose()
        {
            CleanupServices();
            base.Dispose();
        }
        
        protected override void CreatePinMap(bool newObject)
        {
            base.CreatePinMap(newObject);

            var shutterGroup = Root.AddGroup(ShutterNamesGroupId, "ShutterNames");
            var heaterGroup = Root.AddGroup(HeaterNamesGroupId, "HeaterNames");
            var nitrogenGroup = Root.AddGroup(NitrogenSourcesGroupId, "NitrogenSourcesNames");

            for (var i = 0; i < ShutterNameQuantity; i++)
            {
                var pinId = IdFirstShutterName + i; 
                var pinName = $"Shutter{i + 1}";
                shutterGroup.AddPinWithID(pinId, pinName, PinType.Pin, typeof(string), 0d);
            }

            for (var i = 0; i < HeaterNameQuantity; i++)
            {
                var pinId = IdFirstHeaterName + i;
                var pinName = $"Heater{i + 1}";
                heaterGroup.AddPinWithID(pinId, pinName, PinType.Pin, typeof(string), 0d);
            }

            for (var i = 0; i < NitrogenSourceNameQuantity; i++)
            {
                var pinId = IdFirstNitrogenSourceName + i;
                var pinName = $"NitrogenSource{i + 1}";
                nitrogenGroup.AddPinWithID(pinId, pinName, PinType.Pin, typeof(string), 0d);
            }

            FirePinSpaceChanged();
        }
        
        private void CleanupServices()
        {
            _plcStateMonitor = null;
            _actionTargetProvider = null;
            _serviceProvider = null; 
        }

        public Dictionary<int, string> GetShutterNames()
        {
            return ReadPinGroup(IdFirstShutterName, ShutterNameQuantity);
        }
        
        public Dictionary<int, string> GetHeaterNames()
        {
            return ReadPinGroup(IdFirstHeaterName, HeaterNameQuantity);
        }
        
        public Dictionary<int, string> GetNitrogenSourceNames()
        {
            return ReadPinGroup(IdFirstNitrogenSourceName, NitrogenSourceNameQuantity);
        }

        private Dictionary<int, string> ReadPinGroup(int firstId, int quantity)
        {
            if (quantity <= 0)
                return new Dictionary<int, string>();

            var pinGroup = new Dictionary<int, string>(quantity);

            for (var pinId = firstId; pinId < firstId + quantity; pinId++)
            {
                if (GetPinQuality(pinId) == OpcQuality.Good)
                {
                    var pinValue = GetPinValue<string>(pinId);
                    if (!string.IsNullOrWhiteSpace(pinValue))
                    {
                        pinGroup[pinId - firstId] = pinValue;
                    }
                }
            }

            return pinGroup;
        }
    }
}