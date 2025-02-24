﻿using FB;
using FB.VisualFB;
using InSAT.Library.Interop;
using InSAT.OPC;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
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

        private CountdownTimer _countdownTimer;
        
        private TimeSpan _lastRecipeTimeLeft;
        
        private int _previousLineNumber = -1;
        
        private bool _isRecipeRunning;
        private bool _isTimerPaused;

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
            // Update communication protocol and address area values
            VisualPins.SetPinValue(Params.ID_HMI_CommProtocol, GetProtocolValue());
            VisualPins.SetPinValue(Params.ID_HMI_AddrArea, GetAddressAreaValue());

            // Update HMI values
            VisualPins.SetPinValue(Params.ID_HMI_FloatBaseAddr, Params.UFloatBaseAddr);
            VisualPins.SetPinValue(Params.ID_HMI_FloatAreaSize, Params.UFloatAreaSize);
            VisualPins.SetPinValue(Params.ID_HMI_IntBaseAddr, Params.UIntBaseAddr);
            VisualPins.SetPinValue(Params.ID_HMI_IntAreaSize, Params.UIntAreaSize);
            VisualPins.SetPinValue(Params.ID_HMI_BoolBaseAddr, Params.UBoolBaseAddr);
            VisualPins.SetPinValue(Params.ID_HMI_BoolAreaSize, Params.UBoolAreaSize);
            VisualPins.SetPinValue(Params.ID_HMI_ControlBaseAddr, Params.UControlBaseAddr);

            // Update controller IP and port values
            VisualPins.SetPinValue(Params.ID_HMI_IP1, Params.ConntrollerIP1);
            VisualPins.SetPinValue(Params.ID_HMI_IP2, Params.ConntrollerIP2);
            VisualPins.SetPinValue(Params.ID_HMI_IP3, Params.ConntrollerIP3);
            VisualPins.SetPinValue(Params.ID_HMI_IP4, Params.ConntrollerIP4);
            VisualPins.SetPinValue(Params.ID_HMI_Port, Params.ConntrollerTCPPort);

            // Update timeout value
            VisualPins.SetPinValue(Params.ID_HMI_Timeout, Params.Timeout);

            // Process actual line and enable load status
            var actualLine = GetActualLineValue();
            var statusFlags = CalculateStatusFlags(actualLine);

            // Update status values
            VisualPins.SetPinValue(Params.ID_HMI_ActualLine, actualLine);
            VisualPins.SetPinValue(Params.ID_HMI_Status, statusFlags);
            
            // Current step inside FOR loop of first nesting level
            var forLoopCount1 = GetPinValue<int>(ID_ForLoopCount1);
            var forLoopCount2 = GetPinValue<int>(ID_ForLoopCount2);
            var forLoopCount3 = GetPinValue<int>(ID_ForLoopCount3);

            var currentLine = GetPinValue<int>(ID_ActualLineNumber);
            var plcLineTime = GetPinValue<double>(ID_StepCurrentTime);
            
            var isRecipeActive = GetPinValue<bool>(ID_RecipeActive);
            var isRecipePaused = GetPinValue<bool>(ID_RecipePaused);
            
            // Get expected time for current line from flattened recipe data
            var currentLineTime = RecipeTimeManager.GetRowTime(currentLine, forLoopCount1, forLoopCount2, forLoopCount3);

            // Update timer info
            RecipeRunControl(isRecipeActive, isRecipePaused, plcLineTime, currentLineTime);
            
            // Update recipe time
            UpdateRecipeTime(currentLine, plcLineTime, currentLineTime);
        }

        private void UpdateRecipeTime(int currentLine, double plcLineTime, TimeSpan currentLineTime)
        {
            var recipeTimeLeft = _countdownTimer?.GetRemainingTime() ?? TimeSpan.Zero;
            var lineTimeLeft = currentLineTime - TimeSpan.FromSeconds(plcLineTime);
            
            // Update recipe time left if line number has changed
            if (currentLine != _previousLineNumber)
            {
                _lastRecipeTimeLeft = recipeTimeLeft;
                _previousLineNumber = currentLine;
            }
            
            SetPinValue(ID_TotalTimeLeft, recipeTimeLeft.TotalSeconds);
            SetPinValue(ID_LineTimeLeft, lineTimeLeft.TotalSeconds);
        }
        
        private void RecipeRunControl(bool isRecipeActive, bool isRecipePaused, double plcLineTime, TimeSpan expectedTimePassed)
        {
            if (RecipeTimeManager.TotalTime == TimeSpan.Zero) return;

            if (isRecipeActive && !_isRecipeRunning)
            {
                _countdownTimer ??= new CountdownTimer(RecipeTimeManager.TotalTime);
                _isRecipeRunning = true;
                _countdownTimer.Start();
            }

            if (isRecipeActive)
            {
                if (isRecipePaused && !_isTimerPaused)
                {
                    _isTimerPaused = true;
                    _countdownTimer.Pause();
                }
                else if (!isRecipePaused && _isTimerPaused)
                {
                    _isTimerPaused = false;
                    _countdownTimer.Resume();
                }

                // Time correction between PLC and HMI
                // Delta time in seconds to consider when correction is applied
                var delta = 0.5f;
                if (plcLineTime < delta && _lastRecipeTimeLeft < TimeSpan.FromSeconds(delta))
                {
                    var actualTimePassed = _lastRecipeTimeLeft - _countdownTimer.GetRemainingTime();
                    var timeError = expectedTimePassed - actualTimePassed;

                    if (Math.Abs(timeError.TotalSeconds) > 1)
                    {
                        _countdownTimer.SetRemainingTime(_countdownTimer.GetRemainingTime() + timeError);
                    }

                    _lastRecipeTimeLeft = TimeSpan.Zero;
                }

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
