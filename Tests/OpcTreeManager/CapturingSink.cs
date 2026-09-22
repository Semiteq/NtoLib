using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Tests.OpcTreeManager;

internal sealed class CapturingSink : ILogEventSink
{
	public List<LogEvent> Events { get; } = new();

	public void Emit(LogEvent logEvent)
	{
		Events.Add(logEvent);
	}

	/// <summary>A logger that captures every level into this sink.</summary>
	public Logger ToLogger()
	{
		return new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.WriteTo.Sink(this)
			.CreateLogger();
	}

	/// <summary>Renders one structured property, the assertion target that survives template edits.</summary>
	public static string Property(LogEvent logEvent, string name)
	{
		return ((ScalarValue)logEvent.Properties[name]).Value?.ToString() ?? string.Empty;
	}
}
