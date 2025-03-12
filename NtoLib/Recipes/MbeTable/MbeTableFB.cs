using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FB;
using FB.VisualFB;
using InSAT.Library.Interop;
using InSAT.OPC;
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
        private ControllerProtocol _enumProtocol;
        private SLMP_area _enumSLMP_area = SLMP_area.R;

        private const int ID_RecipeActive = 1;
        private const int ID_RecipePaused = 2;
        private const int ID_ActualLineNumber = 3;
        private const int ID_StepCurrentTime = 4;
        private const int ID_ForLoopCount1 = 5;
        private const int ID_ForLoopCount2 = 6;
        private const int ID_ForLoopCount3 = 7;

        private const int ID_EnaLoad = 8;

        private const int ID_TotalTimeLeft = 101;
        private const int ID_LineTimeLeft = 102;

        [NonSerialized]
        private CountTimer _countdownTimer;

        [NonSerialized]
        private CountTimer _lineTimer;

        private int _previousLineNumber = -1;
        private double _previousPlcLineTime;

        private bool _isRecipeRunning;

        #region VisualProperties

        private uint _uFloatBaseAddr = Params.UFloatBaseAddr;
        private uint _uFloatAreaSize = Params.UFloatAreaSize;
        private uint _uIntBaseAddr = Params.UIntBaseAddr;
        private uint _uIntAreaSize = Params.UIntAreaSize;
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
            get => this._controllerIp1;
            set => this._controllerIp1 = value;
        }

        [DisplayName("11.  IP адрес контроллера байт 2")]
        [Description("IP адрес контроллера байт 2")]
        public uint conntrollerIP2
        {
            get => this._controllerIp2;
            set => this._controllerIp2 = value;
        }

        [Description("IP адрес контроллера байт 3")]
        [DisplayName("12.  IP адрес контроллера байт 3")]
        public uint conntrollerIP3
        {
            get => this._controllerIp3;
            set => this._controllerIp3 = value;
        }

        [Description("IP адрес контроллера байт 4")]
        [DisplayName("13.  IP адрес контроллера байт 4")]
        public uint conntrollerIP4
        {
            get => this._controllerIp4;
            set => this._controllerIp4 = value;
        }

        [DisplayName("14.  TCP порт")]
        [Description("TCP порт")]
        public uint conntrollerTCPPort
        {
            get => this._controllerTcpPort;
            set => this._controllerTcpPort = value;
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
            // Update communication protocol and address area values
            VisualPins.SetPinValue(Params.IdHmiCommProtocol, GetProtocolValue());
            VisualPins.SetPinValue(Params.IdHmiAddrArea, GetAddressAreaValue());

            // Update HMI values
            VisualPins.SetPinValue(Params.IdHmiFloatBaseAddr, _uFloatBaseAddr);
            VisualPins.SetPinValue(Params.IdHmiFloatAreaSize, _uFloatAreaSize);
            VisualPins.SetPinValue(Params.IdHmiIntBaseAddr, _uIntBaseAddr);
            VisualPins.SetPinValue(Params.IdHmiIntAreaSize, _uIntAreaSize);
            VisualPins.SetPinValue(Params.IdHmiBoolBaseAddr, _uBoolBaseAddr);
            VisualPins.SetPinValue(Params.IdHmiBoolAreaSize, _uBoolAreaSize);
            VisualPins.SetPinValue(Params.IdHmiControlBaseAddr, _uControlBaseAddr);

            // Update controller IP and port values
            VisualPins.SetPinValue(Params.IdHmiIp1, _controllerIp1);
            VisualPins.SetPinValue(Params.IdHmiIp2, _controllerIp2);
            VisualPins.SetPinValue(Params.IdHmiIp3, _controllerIp3);
            VisualPins.SetPinValue(Params.IdHmiIp4, _controllerIp4);
            VisualPins.SetPinValue(Params.IdHmiPort, _controllerTcpPort);

            // Update timeout value
            VisualPins.SetPinValue(Params.IdHmiTimeout, Params.Timeout);

            // Process actual line and enable load status
            var actualLine = GetActualLineValue();
            var statusFlags = CalculateStatusFlags(actualLine);

            // Update status values
            VisualPins.SetPinValue(Params.IdHmiActualLine, actualLine);
            VisualPins.SetPinValue(Params.IdHmiStatus, statusFlags);

            // Current step inside FOR loop of first nesting level
            var forLoopCount1 = GetPinValue<int>(ID_ForLoopCount1);
            var forLoopCount2 = GetPinValue<int>(ID_ForLoopCount2);
            var forLoopCount3 = GetPinValue<int>(ID_ForLoopCount3);

            var currentLine = GetPinValue<int>(ID_ActualLineNumber);
            var plcLineTime = GetPinValue<float>(ID_StepCurrentTime);

            var isRecipeActive = GetPinValue<bool>(ID_RecipeActive);

            // Get expected time for current line from flattened recipe data
            var currentLineTime = RecipeTimeManager.GetRowTime(currentLine, forLoopCount1, forLoopCount2, forLoopCount3);

            // Update timer info
            RecipeRunControl(isRecipeActive, plcLineTime);

            // Update recipe time
            UpdateRecipeTime(currentLine, plcLineTime, currentLineTime);

            // Trigger LineChanged event if the line has changed
            if (currentLine != _previousLineNumber)
            {
                OnLineChanged(isRecipeActive, currentLine, plcLineTime);
                _previousLineNumber = currentLine;
            }
        }

        private void UpdateRecipeTime(int currentLine, float plcLineTime, TimeSpan currentLineTime)
        {
            var recipeTimeLeft = _countdownTimer?.GetRemainingTime() ?? TimeSpan.Zero;
            var lineTimeLeft = TimeSpan.FromSeconds(plcLineTime);

            SetPinValue(ID_TotalTimeLeft, recipeTimeLeft.TotalSeconds);
            SetPinValue(ID_LineTimeLeft, lineTimeLeft.TotalSeconds);
        }

        private void OnLineChanged(bool isRecipeActive, int currentLine, float plcLineTime)
        {
            var delta = 0.5f;
            Debug.WriteLine("------------------------------------------------");
            Debug.WriteLine($"[LineChanged] Начало обработки перехода строки. isRecipeActive: {isRecipeActive}, текущая строка: {currentLine}, plcLineTime для новой строки: {plcLineTime}");

            // If there is a timer for the previous line, calculate the difference between internal and external elapsed time
            if (_lineTimer is not null)
            {
                // Getting internal and extenal elapsed time for the previous line
                TimeSpan internalRemaining = _lineTimer.GetRemainingTime();
                double internalElapsedSeconds = _previousPlcLineTime - internalRemaining.TotalSeconds;

                // Using Plc time as the reference value
                double externalElapsedSeconds = _previousPlcLineTime;
                double diffSeconds = externalElapsedSeconds - internalElapsedSeconds;

                Debug.WriteLine($"[LineChanged] Коррекция для предыдущей строки: предыдущий plcLineTime: {_previousPlcLineTime} с, " +
                                $"внутреннее прошедшее время: {internalElapsedSeconds:F2} с, разница: {diffSeconds:F2} с");

                if (Math.Abs(diffSeconds) > delta)
                {
                    if (_countdownTimer is not null)
                    {
                        TimeSpan currentOverallRemaining = _countdownTimer.GetRemainingTime();
                        TimeSpan newOverallRemaining = currentOverallRemaining + TimeSpan.FromSeconds(diffSeconds);
                        _countdownTimer.SetRemainingTime(newOverallRemaining);
                        Debug.WriteLine($"[LineChanged] Применена коррекция. Было: {currentOverallRemaining.TotalSeconds:F2} с, стало: {newOverallRemaining.TotalSeconds:F2} с");
                    }
                    else
                    {
                        Debug.WriteLine("[LineChanged] _countdownTimer равен null, коррекция не выполнена.");
                    }
                }
                else
                {
                    Debug.WriteLine($"[LineChanged] Разница менее или равна {delta} с – коррекция не требуется.");
                }

                // Stop and dispose the timer for the previous line
                _lineTimer.Stop();
                _lineTimer = null;
            }
            else
            {
                Debug.WriteLine("[LineChanged] Нет активного таймера строки – коррекция не выполняется.");
            }

            // Set new timer for the current line
            _previousPlcLineTime = plcLineTime;
            _lineTimer = new CountTimer(TimeSpan.FromSeconds(plcLineTime));
            _lineTimer.Start();
            Debug.WriteLine($"[LineChanged] Запущен новый таймер строки с длительностью: {plcLineTime} с для строки: {currentLine}");

            _previousLineNumber = currentLine;
        }



        private void RecipeRunControl(bool isRecipeActive, double plcLineTime)
        {
            if (RecipeTimeManager.TotalTime == TimeSpan.Zero) return;

            if (isRecipeActive && !_isRecipeRunning)
            {
                if (_countdownTimer == null)
                    _countdownTimer = new CountTimer(RecipeTimeManager.TotalTime);
                else
                    _countdownTimer.SetRemainingTime(RecipeTimeManager.TotalTime);
                _isRecipeRunning = true;
                _countdownTimer.Start();

            }

            if (isRecipeActive)
            {
                if (_countdownTimer.GetRemainingTime() == TimeSpan.Zero)
                {
                    _countdownTimer.Stop();

                    _isRecipeRunning = false;
                }
            }
            else
            {
                _countdownTimer?.Stop();

                _isRecipeRunning = false;
            }
        }

        private int GetProtocolValue()
        {
            return _enumProtocol switch
            {
                ControllerProtocol.Modbus => 1,
                ControllerProtocol.SLMP_not_implimated => 2,
                _ => 0
            };
        }

        private int GetAddressAreaValue()
        {
            return _enumSLMP_area switch
            {
                SLMP_area.D => 1,
                SLMP_area.R => 2,
                _ => 0
            };
        }

        /// <summary>
        /// Check quality and return line number.
        /// </summary>
        /// <returns>Line number if pin quality good, otherwise -1.</returns>
        private int GetActualLineValue()
        {
            int actualLine = GetPinInt(ID_ActualLineNumber);
            return GetPinQuality(ID_ActualLineNumber) == OpcQuality.Good ? actualLine : -1;
        }

        /// <summary>
        /// Calculates bit mask status.
        /// </summary>
        /// <returns>Bit mask depending on status of ID_EnaLoad, 
        /// ID_EnaLoad pin quality and actualLine number.</returns>
        private uint CalculateStatusFlags(int actualLine)
        {
            return (GetPinBool(ID_EnaLoad) ? 1u : 0) |
                   (GetPinQuality(ID_EnaLoad) == OpcQuality.Good ? 2u : 0) |
                   (actualLine != -1 ? 4u : 0);
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
