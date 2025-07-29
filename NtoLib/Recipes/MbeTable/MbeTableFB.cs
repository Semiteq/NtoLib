using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using FB;
using FB.VisualFB;
using InSAT.Library.Interop;
using InSAT.OPC;
using NtoLib.Recipes.MbeTable.PLC;
using NtoLib.Recipes.MbeTable.Recipe.Actions;
using NtoLib.Recipes.MbeTable.Recipe.StepManager;

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

        // Private fields
        [NonSerialized] private List<Step> _tableData;
        // [NonSerialized] private TableTimeManager _tableTimeManager;
        // [NonSerialized] private RecipeTimerManager _recipeTimerManager;
        [NonSerialized] private CommunicationSettings _communicationSettings;
        [NonSerialized] private ActionTarget _actionTarget;
        // [NonSerialized] private Shutters _shutters;
        // [NonSerialized] private Heaters _heaters;
        // [NonSerialized] private NitrogenSources _nitrogenSources;

        // Default values
        private int _previousLineNumber = -1;
        private int _previousForLoopCount1 = -1;
        private int _previousForLoopCount2 = -1;
        private int _previousForLoopCount3 = -1;

        #region VisualProperties

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

        #region Registration Methods
        // public void RegisterTableData(List<Step> tableData)
        // {
        //     _tableData = tableData ?? throw new ArgumentNullException(nameof(tableData));
        // }
        //
        // public void RegisterTableTimeManager(TableTimeManager tableTimeManager)
        // {
        //     _tableTimeManager = tableTimeManager ?? throw new ArgumentNullException(nameof(tableTimeManager));
        // }
        //
        // public void RegisterDataGridViewUpdater(UpdateBatcher updateBatcher)
        // {
        //     _updateBatcher = updateBatcher ?? throw new ArgumentNullException(nameof(updateBatcher));
        // }
        //
        // public void RegisterCommunicationSettings(CommunicationSettings communicationSettings)
        // {
        //     _communicationSettings = communicationSettings ?? throw new ArgumentNullException(nameof(communicationSettings));
        //     UpdateCommunicationSettings(_communicationSettings);
        // }
        //
        // public void RegisterShutters(Shutters shutters)
        // {
        //     _shutters = shutters ?? throw new ArgumentNullException(nameof(shutters));
        // }
        //
        // public void RegisterHeaters(Heaters heaters)
        // {
        //     _heaters = heaters ?? throw new ArgumentNullException(nameof(heaters));
        // }
        //
        // public void RegisterNitrogenSources(NitrogenSources nitrogenSources)
        // {
        //     _nitrogenSources = nitrogenSources ?? throw new ArgumentNullException(nameof(nitrogenSources));
        // }

        #endregion

        protected override void ToRuntime()
        {
            base.ToRuntime();
        }

        protected override void ToDesign()
        {
            base.ToDesign();
        }

        protected override void UpdateData()
        {
            base.UpdateData();

            UpdateHmiPins();
            if (_communicationSettings != null) UpdateCommunicationSettings(_communicationSettings);

            // UpdateShutters();
            // UpdateHeaters();
            // UpdateNitrogenSources();

            var actualLine = GetActualLineValue();
            var isRecipeActive = GetPinValue<bool>(IdRecipeActive);
            var actualLineNumber = GetPinValue<int>(IdActualLineNumber);
            var stepCurrentTime = GetPinValue<float>(IdStepCurrentTime);
            var forLoopCount1 = GetPinValue<int>(IdForLoopCount1);
            var forLoopCount2 = GetPinValue<int>(IdForLoopCount2);
            var forLoopCount3 = GetPinValue<int>(IdForLoopCount3);

            var isLineChanged = IsLineChanged(actualLineNumber, forLoopCount1, forLoopCount2, forLoopCount3);

            // if (_recipeTimerManager is null && _tableTimeManager is not null)
            //     _recipeTimerManager = new RecipeTimerManager(_tableTimeManager);
            //
            // if (_recipeTimerManager is null) return;

            // if (isRecipeActive)
            // {
            //     var(leftStepTime, leftTotalTime) = _recipeTimerManager.GetLeftTimes(actualLineNumber, stepCurrentTime);
            //     SetPinValue(IdActualLineNumber, leftStepTime);
            //     SetPinValue(IdTotalTimeLeft, leftTotalTime);
            // }
            //
            // if (isRecipeActive && isLineChanged)
            // { 
            //     _recipeTimerManager.HandleLineChange(actualLineNumber);
            //     _updateBatcher.HandleLineChange(actualLineNumber);
            // }
        }

        private void UpdateHmiPins()
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

        private void UpdateCommunicationSettings(CommunicationSettings settings)
        {
            settings.FloatBaseAddr = UFloatBaseAddr;
            settings.FloatAreaSize = UFloatAreaSize;

            settings.IntBaseAddr = UIntBaseAddr;
            settings.IntAreaSize = UIntAreaSize;

            settings.BoolBaseAddr = UBoolBaseAddr;
            settings.BoolAreaSize = UBoolAreaSize;

            settings.ControlBaseAddr = UControlBaseAddr;

            settings.Ip1 = ControllerIp1;
            settings.Ip2 = ControllerIp2;
            settings.Ip3 = ControllerIp3;
            settings.Ip4 = ControllerIp4;

            settings.Port = ControllerTcpPort;
        }

        public int GetActualLineValue() => GetPinQuality(IdActualLineNumber) == OpcQuality.Good ? GetPinInt(IdActualLineNumber) : -1;

        private bool IsLineChanged(int currentLine, int forLoopCount1, int forLoopCount2, int forLoopCount3)
        {
            if (currentLine != _previousLineNumber
                || _previousForLoopCount1 != forLoopCount1
                || _previousForLoopCount2 != forLoopCount2
                || _previousForLoopCount3 != forLoopCount3)
            {
                _previousLineNumber = currentLine;
                _previousForLoopCount1 = forLoopCount1;
                _previousForLoopCount2 = forLoopCount2;
                _previousForLoopCount3 = forLoopCount3;

                return true;
            }
            return false;
        }

        // private void UpdateShutters()
        // {
        //     _shutters.SetNames(ReadPinGroup(IdFirstShutterName, ShutterNameQuantity));
        // }
        //
        // private void UpdateHeaters()
        // {
        //     _heaters.SetNames(ReadPinGroup(IdFirstHeaterName, HeaterNameQuantity));
        // }
        //
        // private void UpdateNitrogenSources()
        // {
        //     _nitrogenSources.SetNames(ReadPinGroup(IdFirstNitrogenSourceName, NitrogenSourceNameQuantity));
        // }

        private Dictionary<int, string> ReadPinGroup(int firstId, int quantity)
        {
            if (quantity <= 0)
                return new Dictionary<int, string>();

            var pinGroup = new Dictionary<int, string>();

            for (var pinId = firstId; pinId < firstId + quantity; pinId++)
            {
                if (GetPinQuality(pinId) != OpcQuality.Good)
                    continue;

                var pinValue = GetPinValue<string>(pinId);
                if (!string.IsNullOrEmpty(pinValue))
                {
                    pinGroup.Add(pinId - firstId, pinValue);
                }
            }

            return pinGroup;
        }
    }
}