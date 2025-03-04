namespace NtoLib.Recipes.MbeTable.PLC
{
    internal class CommunicationSettings
    {
        public MbeTableFB.ControllerProtocol Protocol;
        public MbeTableFB.SLMP_area SlmpArea;
        public ushort ModbusTransactionId = 0;

        public uint FloatBaseAddr;
        public uint IntBaseAddr;
        public uint BoolBaseAddr;

        public uint ControlBaseAddr;

        public int FloatColumNum = Params.FloatColumNum;
        public readonly int IntColumNum = Params.IntColumNum;
        public readonly int BoolColumNum = 0;

        public uint FloatAreaSize;
        public uint IntAreaSize;
        public uint BoolAreaSize;

        public uint Ip1;
        public uint Ip2;
        public uint Ip3;
        public uint Ip4;
        public uint Port;
        public uint Timeout;
    }
}
