namespace NtoLib.MbeTable.ServiceModbusTCP;

public sealed class KeepAliveStrategy : IDisconnectStrategy
{
	public bool ShouldDisconnect(string operationType) => false;
}
