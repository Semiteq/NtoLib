namespace NtoLib.MbeTable.ServiceModbusTCP;

public interface IDisconnectStrategy
{
	bool ShouldDisconnect(string operationType);
}
