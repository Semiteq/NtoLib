namespace NtoLib.MbeTable.ServiceModbusTCP;

public sealed class AlwaysDisconnectStrategy : IDisconnectStrategy
{
	public bool ShouldDisconnect(string operationType) => true;
}
