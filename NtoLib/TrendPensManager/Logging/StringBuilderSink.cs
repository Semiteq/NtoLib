using System;
using System.Text;

using Serilog.Core;
using Serilog.Events;

namespace NtoLib.TrendPensManager.Logging;

public class StringBuilderSink : ILogEventSink
{
	private readonly StringBuilder _buffer;
	private readonly IFormatProvider? _formatProvider;
	private readonly object _lock = new();

	public StringBuilderSink(StringBuilder buffer, IFormatProvider? formatProvider = null)
	{
		_buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
		_formatProvider = formatProvider;
	}

	public void Emit(LogEvent logEvent)
	{
		var message = logEvent.RenderMessage(_formatProvider);
		var level = logEvent.Level.ToString().Substring(0, 3).ToUpper();
		var timestamp = logEvent.Timestamp.ToString("HH:mm:ss.fff");

		// Ensure proper encoding for Cyrillic
		var formattedLine = $"[{timestamp}] [{level}] {message}";

		lock (_lock)
		{
			_buffer.Append(formattedLine);
			_buffer.Append('\n');

			if (logEvent.Exception != null)
			{
				_buffer.Append($"Exception: {logEvent.Exception.Message}");
				_buffer.Append('\n');
			}
		}
	}

	public void Clear()
	{
		lock (_lock)
		{
			_buffer.Clear();
		}
	}

	public string GetLogs()
	{
		lock (_lock)
		{
			return _buffer.ToString();
		}
	}
}
