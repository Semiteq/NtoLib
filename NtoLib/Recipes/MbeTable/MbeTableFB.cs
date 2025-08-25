using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using FB;
using FB.VisualFB;
using InSAT.Library.Interop;
using InSAT.OPC;
using MasterSCADA.Hlp;
using NtoLib.Recipes.MbeTable.Composition;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

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
        public ServiceProvider ServiceProvider => _serviceProvider ??= new ServiceProvider();

        [NonSerialized] private ServiceProvider _serviceProvider;
        [NonSerialized] private TimerService _timerService;
        [NonSerialized] private IPlcStateMonitor _plcStateMonitor;
        [NonSerialized] private IActionTargetProvider _actionTargetProvider;
        [NonSerialized] private ICommunicationSettingsProvider _commSettingsProvider;
        [NonSerialized] private IPlcRecipeStatusProvider _plcRecipeStatusProvider;

        #region Numerical Properties
        // Pin ID's
        private const int IdRecipeActive = 1;
        private const int IdRecipePaused = 2;
        private const int IdCurrentLine = 3;
        private const int IdStepCurrentTime = 4;
        private const int IdForLoopCount1 = 5;
        private const int IdForLoopCount2 = 6;
        private const int IdForLoopCount3 = 7;
        private const int IdEnaSend = 8;
        private const int IdTotalTimeLeft = 101;
        private const int IdLineTimeLeft = 102;

        // ActionProperty pin ID's
        private const int ShutterNamesGroupId = 200;
        private const int HeaterNamesGroupId = 300;
        private const int NitrogenSourcesGroupId = 400;

        private const int IdFirstShutterName = 201;
        private const int IdFirstHeaterName = 301;
        private const int IdFirstNitrogenSourceName = 401;

        private const int ShutterNameQuantity = 32;
        private const int HeaterNameQuantity = 32;
        private const int NitrogenSourceNameQuantity = 3;

        // Communication pin ID's
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

        // Default values
        private int _floatBaseAddr = 8100;
        private int _floatAreaSize = 19600;

        private int _intBaseAddr = 27700;
        private int _intAreaSize = 1400;

        private int _boolBaseAddr = 29100;
        private int _boolAreaSize = 50;

        private int _controlBaseAddr = 8000;

        private int _controllerIp1 = 192;
        private int _controllerIp2 = 168;
        private int _controllerIp3 = 0;
        private int _controllerIp4 = 141;

        private int _controllerTcpPort = 502;
        #endregion

        #region VisualProperties

        [Description("Определяет начальный адрес, куда помещаются данные типа 'вещественный'")]
        [DisplayName(" 1.  Базовый адрес хранения данных типа Real (Float)")]
        public int FloatBaseAddr
        {
            get => _floatBaseAddr;
            set => _floatBaseAddr = value;
        }

        [Description("Определяет размер области для данных типа 'вещественный'.")]
        [DisplayName(" 2.  Размер области хранения данных типа Real (Float)")]
        public int FloatAreaSize
        {
            get => _floatAreaSize;
            set => _floatAreaSize = value;
        }

        [Description("Определяет начальный адрес, куда помещаются данные типа 'целый 16 бит'")]
        [DisplayName(" 3.  Базовый адрес хранения данных типа Int")]
        public int IntBaseAddr
        {
            get => _intBaseAddr;
            set => _intBaseAddr = value;
        }

        [DisplayName(" 4.  Размер области хранения данных типа Int")]
        [Description("Определяет размер области для данных типа 'целый 16 бит'")]
        public int IntAreaSize
        {
            get => _intAreaSize;
            set => _intAreaSize = value;
        }

        [DisplayName(" 5.  Базовый адрес хранения данных типа Boolean")]
        [Description("Определяет начальный адрес, куда помещаются данные типа 'логический'.")]
        public int BoolBaseAddr
        {
            get => _boolBaseAddr;
            set => _boolBaseAddr = value;
        }

        [Description("Определяет размер области для данных типа 'логический'.")]
        [DisplayName(" 6.  Размер области хранения данных типа Boolean")]
        public int BoolAreaSize
        {
            get => _boolAreaSize;
            set => _boolAreaSize = value;
        }

        [DisplayName(" 7.  Базовый адрес контрольной области")]
        [Description("Определяет начальный адрес, где располагается зона контрольных данных (3 слова)")]
        public int ControlBaseAddr
        {
            get => _controlBaseAddr;
            set => _controlBaseAddr = value;
        }

        [Description("IP адрес контроллера байт 1")]
        [DisplayName("8.  IP адрес контроллера байт 1")]
        public int ControllerIp1
        {
            get => _controllerIp1;
            set => _controllerIp1 = value;
        }

        [DisplayName("9.  IP адрес контроллера байт 2")]
        [Description("IP адрес контроллера байт 2")]
        public int ControllerIp2
        {
            get => _controllerIp2;
            set => _controllerIp2 = value;
        }

        [Description("IP адрес контроллера байт 3")]
        [DisplayName("10.  IP адрес контроллера байт 3")]
        public int ControllerIp3
        {
            get => _controllerIp3;
            set => _controllerIp3 = value;
        }

        [Description("IP адрес контроллера байт 4")]
        [DisplayName("11.  IP адрес контроллера байт 4")]
        public int ControllerIp4
        {
            get => _controllerIp4;
            set => _controllerIp4 = value;
        }

        [DisplayName("12.  TCP порт")]
        [Description("TCP порт")]
        public int ControllerTcpPort
        {
            get => _controllerTcpPort;
            set => _controllerTcpPort = value;
        }

        #endregion

        public Dictionary<int, string> GetShutterNames() => ReadPinGroup(IdFirstShutterName, ShutterNameQuantity);
        public Dictionary<int, string> GetHeaterNames() => ReadPinGroup(IdFirstHeaterName, HeaterNameQuantity);
        public Dictionary<int, string> GetNitrogenSourceNames() => ReadPinGroup(IdFirstNitrogenSourceName, NitrogenSourceNameQuantity);

        protected override void UpdateData()
        {
            base.UpdateData();

            var stepCurrentTime = GetPinQuality(IdCurrentLine) is OpcQuality.Good ? GetPinValue<float>(IdStepCurrentTime) : -1f;
            var lineNumber = GetPinQuality(IdCurrentLine) is OpcQuality.Good ? GetPinValue<int>(IdCurrentLine) : -1;
            var forLoopCount1 = GetPinQuality(IdCurrentLine) is OpcQuality.Good ? GetPinValue<int>(IdForLoopCount1) : -1;
            var forLoopCount2 = GetPinQuality(IdCurrentLine) is OpcQuality.Good ? GetPinValue<int>(IdForLoopCount2) : -1;
            var forLoopCount3 = GetPinQuality(IdCurrentLine) is OpcQuality.Good ? GetPinValue<int>(IdForLoopCount3) : -1;

            _plcStateMonitor.UpdateState(lineNumber, forLoopCount1, forLoopCount2, forLoopCount3, stepCurrentTime);

            // For safety reason if failed to read, then consider the recipe is running
            var isRecipeActive = GetPinQuality(IdRecipeActive) is OpcQuality.Good && GetPinValue<bool>(IdRecipeActive);
            var curentLine = GetPinQuality(IdCurrentLine) is OpcQuality.Good ? GetPinValue<int>(IdCurrentLine) : -1;
            var isEnaSend = GetPinQuality(IdEnaSend) is OpcQuality.Good && GetPinValue<bool>(IdEnaSend);

            _plcRecipeStatusProvider.UpdateStatus(isRecipeActive, isEnaSend, curentLine);

            _timerService?.Update();
            
            UpdateUiConnectionPins();
        }

        protected override void ToDesign()
        {
            base.ToDesign();
            InitializeServices();
            CleanupServices();
        }

        protected override void ToRuntime()
        {
            base.ToRuntime();

            // May occur in case ToDesign() was called before ToRuntime()
            if (_serviceProvider is null || !_serviceProvider.IsInitialized)
            {
                _serviceProvider = new ServiceProvider();
                _serviceProvider.InitializeServices(this);
            }

            _timerService = _serviceProvider.TimerService;
            _plcStateMonitor = _serviceProvider.PlcStateMonitor;
            _plcRecipeStatusProvider = _serviceProvider.PlcRecipeStatusProvider;
            _actionTargetProvider = _serviceProvider.ActionTargetProvider;

            _actionTargetProvider.RefreshTargets();
            
            _timerService.TimesUpdated += OnTimesUpdated;
        }

        public override void Dispose()
        {
            if (_timerService != null)
            {
                _timerService.TimesUpdated -= OnTimesUpdated; // <-- Отписаться
            }
            CleanupServices();
            base.Dispose();
        }

        private void OnTimesUpdated(TimeSpan stepTimeLeft, TimeSpan totalTimeLeft)
        {
            if (GetPinQuality(IdLineTimeLeft) != OpcQuality.Good 
                || !AreFloatsEqual(GetPinValue<float>(IdLineTimeLeft), (float)stepTimeLeft.TotalSeconds))
            {
                SetPinValue(IdLineTimeLeft, (float)stepTimeLeft.TotalSeconds);
            }

            if (GetPinQuality(IdTotalTimeLeft) != OpcQuality.Good 
                || !AreFloatsEqual(GetPinValue<float>(IdTotalTimeLeft), (float)totalTimeLeft.TotalSeconds))
            {
                SetPinValue(IdTotalTimeLeft, (float)totalTimeLeft.TotalSeconds);
            }
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

        private void InitializeServices()
        {
            _serviceProvider.InitializeServices(this);

            _plcStateMonitor = _serviceProvider.PlcStateMonitor;
            _actionTargetProvider = _serviceProvider.ActionTargetProvider;

            _actionTargetProvider.RefreshTargets();
        }

        private void UpdateUiConnectionPins()
        {
            VisualPins.SetValue<int>(IdHmiFloatBaseAddr, FloatBaseAddr);
            VisualPins.SetValue<int>(IdHmiFloatAreaSize, FloatAreaSize);
            VisualPins.SetValue<int>(IdHmiIntBaseAddr, IntBaseAddr);
            VisualPins.SetValue<int>(IdHmiIntAreaSize, IntAreaSize);
            VisualPins.SetValue<int>(IdHmiBoolBaseAddr, BoolBaseAddr);
            VisualPins.SetValue<int>(IdHmiBoolAreaSize, BoolAreaSize);
            VisualPins.SetValue<int>(IdHmiControlBaseAddr, ControlBaseAddr);
            VisualPins.SetValue<int>(IdHmiIp1, ControllerIp1);
            VisualPins.SetValue<int>(IdHmiIp2, ControllerIp2);
            VisualPins.SetValue<int>(IdHmiIp3, ControllerIp3);
            VisualPins.SetValue<int>(IdHmiIp4, ControllerIp4);
            VisualPins.SetValue<int>(IdHmiPort, ControllerTcpPort);
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

        private void CleanupServices()
        {
            _plcStateMonitor = null;
            _actionTargetProvider = null;
            _serviceProvider = null;
        }
        
        private bool AreFloatsEqual(float a, float b)
        {
            return Math.Abs(a - b) < 0.01;
        }
    }
}