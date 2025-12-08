using System;
using System.Diagnostics;

using Microsoft.Extensions.Logging;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Transport;

/// <summary>
/// Simple disposable helper that writes elapsed time to the provided logger.
/// </summary>
internal sealed class MetricsStopwatch : IDisposable
{
	private readonly ILogger _logger;
	private readonly string _operation;
	private readonly Stopwatch _sw;

	private MetricsStopwatch(string operation, ILogger logger)
	{
		_operation = operation;
		_logger = logger;
		_sw = Stopwatch.StartNew();
	}

	public static MetricsStopwatch Start(string operation, ILogger logger) =>
		new(operation, logger);

	public void Dispose()
	{
		_sw.Stop();
		_logger.LogDebug("{Operation} completed in {ElapsedMilliseconds} ms", _operation, _sw.ElapsedMilliseconds);
	}
}
