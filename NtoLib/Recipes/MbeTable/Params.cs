namespace NtoLib.Recipes.MbeTable
{
    internal readonly ref struct Params
    {
        public const uint UFloatBaseAddr = 0;
        public const uint UFloatAreaSize = 100;
        public const uint UIntBaseAddr = 100;
        public const uint UIntAreaSize = 100;
        public const uint UBoolBaseAddr = 200;
        public const uint UBoolAreaSize = 100;
        public const uint UControlBaseAddr = 200;

        public const uint ControllerIp1 = 192;
        public const uint ControllerIp2 = 168;
        public const uint ControllerIp3 = 0;
        public const uint ControllerIp4 = 1;

        public const int TotalTimeLeft = 101;
        public const int LineTimeLeft = 102;

        public const uint ControllerTcpPort = 502;
        public const uint Timeout = 1000;

        public const int IdHmiCommProtocol = 1001;
        public const int IdHmiAddrArea = 1002;
        public const int IdHmiFloatBaseAddr = 1003;
        public const int IdHmiFloatAreaSize = 1004;
        public const int IdHmiIntBaseAddr = 1005;
        public const int IdHmiIntAreaSize = 1006;
        public const int IdHmiBoolBaseAddr = 1007;
        public const int IdHmiBoolAreaSize = 1008;
        public const int IdHmiControlBaseAddr = 1009;
        public const int IdHmiIp1 = 1010;
        public const int IdHmiIp2 = 1011;
        public const int IdHmiIp3 = 1012;
        public const int IdHmiIp4 = 1013;
        public const int IdHmiPort = 1014;
        public const int IdHmiTimeout = 1015;

        public const int IdHmiActualLine = 1016;
        public const int IdHmiStatus = 1017;

        // Номер пина, с которого начинается нумерация пинов с информацией о названиях заслонок в MasterSCADA
        public const int IdFirstShutterName = 201;

        // Номер пина, с которого начинается нумерация пинов с информацией о названиях нагревателей в MasterSCADA
        public const int IdFirstHeaterName = 301;

        // Номер пина, с которого начинается нумерация пинов с информацией о названиях линий NH3 в MasterSCADA
        public const int IdFirstNitrogenSourceName = 401;

        public const int ShutterNameQuantity = 32;
        public const int HeaterNameQuantity = 32;
        public const int NitrogenSourceNameQuantity = 3;
        public const int MaxLoopCount = 3;

        // Номера столбцов соответствующих параметров в таблице
        public const int ActionIndex = 0;
        public const int ActionTargetIndex = 1;
        public const int InitialValueIndex = 2;
        public const int SetpointIndex = 3;
        public const int SpeedIndex = 4;
        public const int TimeSetpointIndex = 5;
        public const int RecipeTimeIndex = 6;
        public const int CommentIndex = 7;

        // Названия столбцов в таблице
        public static readonly string[] ColumnNames = { "Действие",
                                                        "Объект",
                                                        "Нач.значение",
                                                        "Задание",
                                                        "Скорость",
                                                        "Длительность",
                                                        "Время",
                                                        "Комментарий" };

        // Количество столбцов в таблице соответствующих типов для передачи в PLC
        public const int IntColumNum = 2;
        public const int FloatColumNum = 4;

        // Общее число столбцов в UI таблице
        public const int ColumnCount = 8;
    }
}
