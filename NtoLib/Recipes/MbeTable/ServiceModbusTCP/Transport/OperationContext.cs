using System;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Transport;

internal sealed class OperationContext
{
    public Guid OperationId { get; }
    public string Type { get; }
    public int Address { get; }
    public int Size { get; }
    public Guid? ConnectionId { get; }

    public OperationContext(string type, int address, int size, Guid? connectionId = null)
    {
        OperationId = Guid.NewGuid();
        Type = type;
        Address = address;
        Size = size;
        ConnectionId = connectionId;
    }
}