namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP;

public sealed class KeepAliveStrategy : IDisconnectStrategy
{
	public bool ShouldDisconnect(string operationType)
	{
		return false;
	}
}
