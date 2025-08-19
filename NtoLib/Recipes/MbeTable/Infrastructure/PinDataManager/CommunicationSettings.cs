namespace NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager
{
    public record CommunicationSettings
    {
        public bool IsRecipeActive;
        public int LineNumber;

        public int FloatBaseAddr;
        public int IntBaseAddr;
        public int BoolBaseAddr;

        public int ControlBaseAddr;

        public int IntColumNum = 2;
        public int FloatColumNum = 4;
        public int BoolColumNum = 0;

        public int FloatAreaSize;
        public int IntAreaSize;
        public int BoolAreaSize;

        public int Ip1;
        public int Ip2;
        public int Ip3;
        public int Ip4;

        public int Port;

        public WordOrder WordOrder;

        public int VerifyDelayMs = 200;

        public float Epsilon = 1e-2f;
    }
}
