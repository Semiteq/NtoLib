namespace NtoLib.Recipes.MbeTable
{
    internal class CommunicationSettings
    {
        public MbeTableFB.ControllerProtocol _protocol;
        public MbeTableFB.SLMP_area _SLMP_Area;
        public ushort _modbus_transactionID;

        public uint _FloatBaseAddr;
        public uint _IntBaseAddr;
        public uint _BoolBaseAddr;

        public uint _ControlBaseAddr;

        public int _float_colum_num;
        public int _int_colum_num;
        public int _bool_colum_num;

        public uint _FloatAreaSize;
        public uint _IntAreaSize;
        public uint _BoolAreaSize;

        public uint _IP1;
        public uint _IP2;
        public uint _IP3;
        public uint _IP4;
        public uint _Port;
        public uint _Timeout;


        
    }
}
