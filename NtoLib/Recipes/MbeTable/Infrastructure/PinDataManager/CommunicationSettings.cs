namespace NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager
{
    public class CommunicationSettings
    {
        public uint FloatBaseAddr;
        public uint IntBaseAddr;
        public uint BoolBaseAddr;

        public uint ControlBaseAddr;
        
        public const int FloatColumNum = 2;
        public const int IntColumNum = 4;
        public const int BoolColumNum = 0;

        public uint FloatAreaSize;
        public uint IntAreaSize;
        public uint BoolAreaSize;

        public uint Ip1;
        public uint Ip2;
        public uint Ip3;
        public uint Ip4;
        
        public uint Port;
    }
}
