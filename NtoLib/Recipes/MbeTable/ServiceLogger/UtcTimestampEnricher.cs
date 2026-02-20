using System;

using Serilog.Core;
using Serilog.Events;

namespace NtoLib.Recipes.MbeTable.ServiceLogger;

internal sealed class UtcTimestampEnricher : ILogEventEnricher
{
	public const string PropertyName = "UtcTimestamp";

	public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
	{
		if (logEvent == null)
		{
			return;
		}
		var prop = propertyFactory.CreateProperty(PropertyName, DateTimeOffset.UtcNow);
		logEvent.AddOrUpdateProperty(prop);
	}
}
