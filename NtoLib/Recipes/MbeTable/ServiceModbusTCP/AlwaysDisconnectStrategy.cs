namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP;

public sealed class AlwaysDisconnectStrategy : IDisconnectStrategy
{
    public bool ShouldDisconnect(string operationType) => true;
}