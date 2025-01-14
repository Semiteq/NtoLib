using System.Collections.Generic;
using System.ComponentModel;

namespace NtoLib.Recipes.MbeTable
{
    internal static class Params
    {
        public const int ID_ActualLine = 1;
        public const int ID_EnaLoad = 2;

        public const uint UFloatBaseAddr = 0;
        public const uint UFloatAreaSize = 100;
        public const uint UIntBaseAddr = 100;
        public const uint UIntAreaSize = 100;
        public const uint UBoolBaseAddr = 200;
        public const uint UBoolAreaSize = 100;
        public const uint UControlBaseAddr = 200;
        public const uint ConntrollerIP1 = 192;
        public const uint ConntrollerIP2 = 168;
        public const uint ConntrollerIP3 = 0;
        public const uint ConntrollerIP4 = 1;
        public const uint ConntrollerTCPPort = 502;
        public const uint Timeout = 1000;

        public const int GroupID_ActTemperaure = 100;
        public const int GroupID_ActPower = 200;
        public const int GroupID_ActLoopCount = 300;
        public const int GroupID_ShutterNames = 400;
        public const int GroupID_HeaterNames = 500;

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

        //Номер пина, с которого начинается нумерация пинов с информацией о количестве циклов включения в MasterSCADA
        public const int FirstPinActLoopAcount = 1301;

        //Номер пина, с которого начинается нумерация пинов с информацией о названиях заслонок в MasterSCADA
        public const int FirstPinShutterName = 1401;

        //Номер пина, с которого начинается нумерация пинов с информацией о названиях нагревателей в MasterSCADA
        public const int FirstPinHeaterName = 1501;

        public const int ActLoopAcountQuantity = 5;
        public const int ShutterNameQuantity = 32;
        public const int HeaterNameQuantity = 32;

        //Номера столбцов соответствующих параметров в таблице
        public const int CommandIndex = 0; //todo: унифицировать action и command
        public const int NumberIndex = 1;
        public const int SetpointIndex = 2;
        public const int TimeSetpointIndex = 3;
        public const int RecipeTimeIndex = 4;
        public const int CommentIndex = 5;

        //Общее число столбцов в таблице
        public const int ColumnCount = 6;

        public static readonly Dictionary<int, string> ActionTypes = new()
        {
            //Типы Actions с привязкой действия к заслонкам или к нагревателям
            //todo: привязать к RecipeLine.Actions()
            { 10, "shutter" },
            { 20, "shutter" },
            { 30, "shutter" },
            { 40, "shutter" },

            { 50, "heater" },
            { 60, "heater" },
            { 70, "heater" },
            { 80, "heater" },
            { 90, "heater" },
            { 100, "heater" },
            { 110, "heater" },
            { 120, "heater" },

            { 130, "" },
            { 140, "" },
            { 150, "" },
            { 160, "" },
            { 170, "" },
            { 180, "" },
            { 190, "" }
        };
    }
}
