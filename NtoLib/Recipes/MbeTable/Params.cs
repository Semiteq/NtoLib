namespace NtoLib.Recipes.MbeTable
{
    internal static class Params
    {
        public const uint UFloatBaseAddr = 0;
        public const uint UFloatAreaSize = 100;
        public const uint UIntBaseAddr = 100;
        public const uint UIntAreaSize = 100;
        public const uint UBoolBaseAddr = 200;
        public const uint UBoolAreaSize = 100;
        public const uint UControlBaseAddr = 200;
        
        // todo: дублируют CommunicationSettings.cs
        public const uint ConntrollerIP1 = 192;
        public const uint ConntrollerIP2 = 168;
        public const uint ConntrollerIP3 = 0;
        public const uint ConntrollerIP4 = 1;

        public const int TotalTimeLeft = 101;
        public const int LineTimeLeft = 102;

        public const uint ConntrollerTCPPort = 502;
        public const uint Timeout = 1000;

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

        //Номер пина, с которого начинается нумерация пинов с информацией о названиях заслонок в MasterSCADA
        public const int FirstPinShutterName = 201;

        //Номер пина, с которого начинается нумерация пинов с информацией о названиях нагревателей в MasterSCADA
        public const int FirstPinHeaterName = 301;
        
        //Номер пина, с которого начинается нумерация пинов с информацией о названиях линий NH3 в MasterSCADA
        public const int FirstPinNitrogenSourceName = 401;

        public const int ShutterNameQuantity = 32;
        public const int HeaterNameQuantity = 32;
        public const int NitrogenSourceNameQuantity = 3;
        public const int MaxLoopCount = 3;

        //Номера столбцов соответствующих параметров в таблице
        public const int CommandIndex = 0;
        public const int NumberIndex = 1;
        public const int SetpointIndex = 2;
        public const int TimeSetpointIndex = 3;
        public const int RecipeTimeIndex = 4;
        public const int CommentIndex = 5;

        //Общее число столбцов в таблице
        public const int ColumnCount = 6;
    }
}
