using Serilog.Core;
using Serilog.Events;

namespace Tests.OpcTreeManager;

internal sealed class CapturingSink : ILogEventSink
{
	public List<LogEvent> Events { get; } = new();

	public IEnumerable<string> Warnings => MessagesAt(LogEventLevel.Warning);

	public IEnumerable<string> Debugs => MessagesAt(LogEventLevel.Debug);

	public void Emit(LogEvent logEvent)
	{
		Events.Add(logEvent);
	}

	private IEnumerable<string> MessagesAt(LogEventLevel level)
	{
		return Events.Where(e => e.Level == level).Select(e => e.RenderMessage());
	}
}
