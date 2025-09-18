namespace NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

public record CommunicationSettings
{
    public int ControlBaseAddr;

    public int IntBaseAddr;
    public int IntAreaSize;

    public int FloatBaseAddr;
    public int FloatAreaSize;

    public int Ip1;
    public int Ip2;
    public int Ip3;
    public int Ip4;

    public int Port;

    public WordOrder WordOrder = WordOrder.HighLow;

    public int VerifyDelayMs = 200;

    public float Epsilon = 1e-4f;
}

