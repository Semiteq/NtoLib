using System;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Transport;

internal sealed class ConnectionContext
{
	public Guid ConnectionId { get; }
	public DateTime ConnectedAtUtc { get; }
	public string Reason { get; }
	public string ConnectionString { get; }

	public ConnectionContext(string connectionString, string reason)
	{
		ConnectionId = Guid.NewGuid();
		ConnectedAtUtc = DateTime.UtcNow;
		ConnectionString = connectionString;
		Reason = reason;
	}
}
