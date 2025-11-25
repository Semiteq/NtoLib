namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP;

public interface IDisconnectStrategy
{
	bool ShouldDisconnect(string operationType);
}
