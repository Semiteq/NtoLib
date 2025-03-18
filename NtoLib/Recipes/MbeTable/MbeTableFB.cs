using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using FB;
using FB.VisualFB;
using InSAT.Library.Interop;
using InSAT.OPC;
using Microsoft.Extensions.Logging;
using NtoLib.Recipes.MbeTable.Logging;
using NtoLib.Recipes.MbeTable.RecipeLines.RecipeTime;

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
        private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.AddConsole(options =>
            {
                options.FormatterName = "compact";
            });
            builder.AddConsoleFormatter<CompactConsoleFormatter, CompactConsoleFormatterOptions>(options =>
            {
                options.TimestampFormat = "HH:mm:ss.fff";
            });
        });
        private readonly ILogger<MbeTableFB> _logger = LoggerFactory.CreateLogger<MbeTableFB>();

        private ControllerProtocol _enumProtocol;
        private SlmpArea _enumSlmpArea = SlmpArea.R;

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

        [NonSerialized]
        private CountTimer _recipeTimer;

        [NonSerialized]
        private CountTimer _lineTimer;

        private int _previousLineNumber = -1;
        private double _previousPlcLineTime;
        private int _previousForLoopCount1;
        private int _previousForLoopCount2;
        private int _previousForLoopCount3;
        private LineChangeProcessor _lineChangeProcessor;

        private bool _isRecipeRunning;

        #region VisualProperties

        private uint _uFloatBaseAddr = Params.UFloatBaseAddr;
        private uint _uFloatAreaSize = Params.UFloatAreaSize;
        private uint _uIntBaseAddr = Params.UIntBaseAddr;
        private uint _uIntAreaSize = Params.UIntBaseAddr;
        private uint _uBoolBaseAddr = Params.UBoolBaseAddr;
        private uint _uBoolAreaSize = Params.UBoolAreaSize;
        private uint _uControlBaseAddr = Params.UControlBaseAddr;
        private uint _controllerIp1 = Params.ControllerIp1;
        private uint _controllerIp2 = Params.ControllerIp2;
        private uint _controllerIp3 = Params.ControllerIp3;
        private uint _controllerIp4 = Params.ControllerIp4;
        private uint _controllerTcpPort = Params.ControllerTcpPort;
        private uint _timeout = Params.Timeout;

        [DisplayName(" 1. Протокол обмена передачи данных в контроллер")]
        [Description("Определяет по какому протоколу передаются данные в контроллер")]
        public ControllerProtocol EnumProtocol
        {
            get => _enumProtocol;
            set => _enumProtocol = value;
        }

        [DisplayName(" 2. Пространство хранения данных при использовании SLMP")]
        [Description("Определяет в какой области (D или R) помещаются данные таблицы")]
        public SlmpArea EnumSlmpArea
        {
            get => _enumSlmpArea;
            set => _enumSlmpArea = value;
        }

        [Description("Определяет начальный адрес, куда помещаются данные типа 'вещественный'")]
        [DisplayName(" 3.  Базовый адрес хранения данных типа Real (Float)")]
        public uint UFloatBaseAddr
        {
            get => _uFloatBaseAddr;
            set => _uFloatBaseAddr = value;
        }

        [Description("Определяет размер области для данных типа 'вещественный'. в 16-тибитных словах (2 слова на переменную). Если используется, например, область с адресами 100..199, то это 100 слов или 50 переменных типа float. Укажите в этом параметре 100.")]
        [DisplayName(" 4.  Размер области хранения данных типа Real (Float)")]
        public uint UFloatAreaSize
        {
            get => _uFloatAreaSize;
            set => _uFloatAreaSize = value;
        }

        [Description("Определяет начальный адрес, куда помещаются данные типа 'целый 16 бит'")]
        [DisplayName(" 5.  Базовый адрес хранения данных типа Int")]
        public uint UIntBaseAddr
        {
            get => _uIntBaseAddr;
            set => _uIntBaseAddr = value;
        }

        [DisplayName(" 6.  Размер области хранения данных типа Int")]
        [Description("Определяет размер области для данных типа 'целый 16 бит'")]
        public uint UIntAreaSize
        {
            get => _uIntAreaSize;
            set => _uIntAreaSize = value;
        }

        [DisplayName(" 7.  Базовый адрес хранения данных типа Boolean")]
        [Description("Определяет начальный адрес, куда помещаются данные типа 'логический'. Упаковываются в 16-ти битные слова.")]
        public uint UBoolBaseAddr
        {
            get => _uBoolBaseAddr;
            set => _uBoolBaseAddr = value;
        }

        [Description("Определяет размер области для данных типа 'логический'. Определяется в 16-ти битных словах")]
        [DisplayName(" 8.  Размер области хранения данных типа Boolean")]
        public uint UBoolAreaSize
        {
            get => _uBoolAreaSize;
            set => _uBoolAreaSize = value;
        }

        [DisplayName(" 9.  Базовый адрес контрольной области")]
        [Description("Определяет начальный адрес, где располагается зона контрольных данных (3 слова)")]
        public uint UControlBaseAddr
        {
            get => _uControlBaseAddr;
            set => _uControlBaseAddr = value;
        }

        [Description("IP адрес контроллера байт 1")]
        [DisplayName("10.  IP адрес контроллера байт 1")]
        public uint ControllerIp1
        {
            get => _controllerIp1;
            set => _controllerIp1 = value;
        }

        [DisplayName("11.  IP адрес контроллера байт 2")]
        [Description("IP адрес контроллера байт 2")]
        public uint ControllerIp2
        {
            get => _controllerIp2;
            set => _controllerIp2 = value;
        }

        [Description("IP адрес контроллера байт 3")]
        [DisplayName("12.  IP адрес контроллера байт 3")]
        public uint ControllerIp3
        {
            get => _controllerIp3;
            set => _controllerIp3 = value;
        }

        [Description("IP адрес контроллера байт 4")]
        [DisplayName("13.  IP адрес контроллера байт 4")]
        public uint ControllerIp4
        {
            get => _controllerIp4;
            set => _controllerIp4 = value;
        }

        [DisplayName("14.  TCP порт")]
        [Description("TCP порт")]
        public uint ControllerTcpPort
        {
            get => _controllerTcpPort;
            set => _controllerTcpPort = value;
        }

        [DisplayName("15.  Timeout")]
        [Description("Timeout")]
        public uint Timeout
        {
            get => _timeout;
            set => _timeout = value;
        }
        #endregion

        protected override void ToRuntime() { }
        protected override void ToDesign() { }

        protected override void UpdateData()
        {
            // Update communication protocol and address area values.
            VisualPins.SetPinValue(Params.IdHmiCommProtocol, GetProtocolValue());
            VisualPins.SetPinValue(Params.IdHmiAddrArea, GetAddressAreaValue());

            // Update HMI values.
            VisualPins.SetPinValue(Params.IdHmiFloatBaseAddr, _uFloatBaseAddr);
            VisualPins.SetPinValue(Params.IdHmiFloatAreaSize, _uFloatAreaSize);
            VisualPins.SetPinValue(Params.IdHmiIntBaseAddr, _uIntBaseAddr);
            VisualPins.SetPinValue(Params.IdHmiIntAreaSize, _uIntAreaSize);
            VisualPins.SetPinValue(Params.IdHmiBoolBaseAddr, _uBoolBaseAddr);
            VisualPins.SetPinValue(Params.IdHmiBoolAreaSize, _uBoolAreaSize);
            VisualPins.SetPinValue(Params.IdHmiControlBaseAddr, _uControlBaseAddr);

            // Update controller IP and port values.
            VisualPins.SetPinValue(Params.IdHmiIp1, _controllerIp1);
            VisualPins.SetPinValue(Params.IdHmiIp2, _controllerIp2);
            VisualPins.SetPinValue(Params.IdHmiIp3, _controllerIp3);
            VisualPins.SetPinValue(Params.IdHmiIp4, _controllerIp4);
            VisualPins.SetPinValue(Params.IdHmiPort, _controllerTcpPort);

            // Update timeout value.
            VisualPins.SetPinValue(Params.IdHmiTimeout, Params.Timeout);

            // Process actual line and enable load status.
            var actualLine = GetActualLineValue();
            var statusFlags = CalculateStatusFlags(actualLine);
            VisualPins.SetPinValue(Params.IdHmiActualLine, actualLine);
            VisualPins.SetPinValue(Params.IdHmiStatus, statusFlags);

            // Retrieve FOR loop counters and recipe line data.
            var forLoopCount1 = GetPinValue<int>(IdForLoopCount1);
            var forLoopCount2 = GetPinValue<int>(IdForLoopCount2);
            var forLoopCount3 = GetPinValue<int>(IdForLoopCount3);
            var currentLine = GetPinValue<int>(IdActualLineNumber);
            var plcLineTime = GetPinValue<float>(IdStepCurrentTime);
            var isRecipeActive = GetPinValue<bool>(IdRecipeActive);

            // Update HMI displays for remaining overall and line times.
            _recipeTimer = RecipeTimeManager.ManageRecipeTimer(isRecipeActive, _recipeTimer, RecipeTimeManager.TotalTime, LoggerFactory);

            // Trigger line change event if any relevant parameter has changed.
            if (currentLine != _previousLineNumber ||
                _previousForLoopCount1 != forLoopCount1 ||
                _previousForLoopCount2 != forLoopCount2 ||
                _previousForLoopCount3 != forLoopCount3)
            {
                // Get expected time for the current line from recipe data.
                var currentLineTime = RecipeTimeManager.GetRowTime(currentLine, forLoopCount1, forLoopCount2, forLoopCount3);
                
                _lineChangeProcessor ??= new LineChangeProcessor(
                    LoggerFactory.CreateLogger<LineChangeProcessor>(),
                    LoggerFactory);
                _lineChangeProcessor.Process(isRecipeActive, currentLine, (float)currentLineTime.TotalSeconds, _recipeTimer);
            }

            _previousLineNumber = currentLine;
            _previousForLoopCount1 = forLoopCount1;
            _previousForLoopCount2 = forLoopCount2;
            _previousForLoopCount3 = forLoopCount3;
            
            RecipeTimeManager.UpdateRecipeTimeDisplay(
                plcLineTime,
                _recipeTimer,
                total => SetPinValue(IdTotalTimeLeft, total),
                line => SetPinValue(IdLineTimeLeft, line),
                _logger);
        }

        private int GetProtocolValue() =>
            _enumProtocol switch
            {
                ControllerProtocol.Modbus => 1,
                ControllerProtocol.SlmpNotImplimated => 2,
                _ => 0
            };

        private int GetAddressAreaValue() =>
            _enumSlmpArea switch
            {
                SlmpArea.D => 1,
                SlmpArea.R => 2,
                _ => 0
            };

        /// <summary>
        /// Returns the actual line number if the pin quality is good; otherwise returns -1.
        /// </summary>
        private int GetActualLineValue()
        {
            int actualLine = GetPinInt(IdActualLineNumber);
            return GetPinQuality(IdActualLineNumber) == OpcQuality.Good ? actualLine : -1;
        }

        /// <summary>
        /// Calculates status flags based on the enabled load flag and pin quality.
        /// </summary>
        private uint CalculateStatusFlags(int actualLine) =>
            (GetPinBool(IdEnaLoad) ? 1u : 0) |
            (GetPinQuality(IdEnaLoad) == OpcQuality.Good ? 2u : 0) |
            (actualLine != -1 ? 4u : 0);

        public enum ControllerProtocol
        {
            Modbus,
            SlmpNotImplimated,
        }

        public enum SlmpArea
        {
            D,
            R,
        }
    }
}