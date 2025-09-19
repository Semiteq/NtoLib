#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Logging;

/// <summary>
/// Provides logging services to a debug file and the system's Event Viewer.
/// </summary>
public class DebugLogger : ILogger
{
    private static readonly object _fileLock = new();
    private static readonly string _logFilePath;

    /// <summary>
    /// Initializes static members of the <see cref="DebugLogger"/> class.
    /// Determines the correct log file path in a user's application data folder.
    /// </summary>
    static DebugLogger()
    {
        try
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string logDirectory = Path.Combine(appDataPath, "NtoLibLogs");
            
            Directory.CreateDirectory(logDirectory);

            _logFilePath = Path.Combine(logDirectory, "debug_log.txt");
        }
        catch (Exception ex)
        {
            _logFilePath = Path.Combine(Path.GetTempPath(), "ntolib_debug_log.txt");
            LogToEventViewer("Failed to create log directory in AppData. Falling back to Temp folder.", ex);
        }
    }

    /// <inheritdoc />
    public void Log(string message, [CallerMemberName] string caller = "")
    {
        var output = $"{DateTime.Now:HH:mm:ss.fff} [{caller}] {message}";
        WriteToFile(output);
        
#if DEBUG
        Debug.WriteLine(output);
        Console.WriteLine(output);
#endif
    }
    
    /// <inheritdoc />
    public void LogException(Exception ex, object? contextData = null, [CallerMemberName] string caller = "")
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var messageBuilder = new StringBuilder();

        var exceptionInfo = $"{timestamp} [ERROR] [{caller}] {ex.GetType().Name}: {ex.Message}";
        messageBuilder.AppendLine(exceptionInfo);
        
        if (contextData != null)
        {
            var contextInfo = $"{timestamp} [CONTEXT] [{caller}] {SerializeContext(contextData)}";
            messageBuilder.AppendLine(contextInfo);
        }
        
        if (!string.IsNullOrEmpty(ex.StackTrace))
        {
            var stackTraceInfo = $"{timestamp} [STACK] [{caller}] {ex.StackTrace}";
            messageBuilder.AppendLine(stackTraceInfo);
        }
        
        var fullMessage = messageBuilder.ToString();
        WriteToFile(fullMessage);

#if DEBUG
        Debug.Write(fullMessage);
        Console.Write(fullMessage);
#endif
    }

    private void WriteToFile(string message)
    {
        try
        {
            lock (_fileLock)
            {
                File.AppendAllText(_logFilePath, message + Environment.NewLine);
            }
        }
        catch (Exception fileEx)
        {
            LogToEventViewer($"Failed to write to log file '{_logFilePath}'. Error: {fileEx.Message}", fileEx);
        }
    }

    private static void LogToEventViewer(string message, Exception? ex = null)
    {
        try
        {
            const string source = "NtoLibApplication";
            if (!EventLog.SourceExists(source))
            {
                EventLog.CreateEventSource(source, "Application");
            }

            var eventMessage = new StringBuilder(message);
            if (ex != null)
            {
                eventMessage.AppendLine().Append(ex.ToString());
            }

            EventLog.WriteEntry(source, eventMessage.ToString(), EventLogEntryType.Warning);
        }
        catch
        {
            // Fallback: Ignore exceptions from EventLog to prevent an infinite error loop.
        }
    }

    private string SerializeContext(object contextData)
    {
        try
        {
            return SerializeObject(contextData, 0);
        }
        catch
        {
            return contextData.ToString() ?? "null";
        }
    }

    private string SerializeObject(object? obj, int depth)
    {
        if (depth > 3) return "...";
        if (obj == null) return "null";

        var type = obj.GetType();
    
        if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime))
            return obj.ToString() ?? "null";

        if (obj is IEnumerable enumerable && type != typeof(string))
        {
            var items = new List<string>();
            foreach (var item in enumerable)
            {
                items.Add(SerializeObject(item, depth + 1));
                if (items.Count > 10) 
                { 
                    items.Add("..."); 
                    break; 
                }
            }
            return $"[{string.Join(",", items)}]";
        }

        var sb = new StringBuilder("{");
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .Take(5);

        var first = true;
        foreach (var prop in properties)
        {
            if (!first)
            {
                sb.Append(",");
            }
            
            var value = prop.GetValue(obj);
            sb.Append($"\"{prop.Name}\":{SerializeObject(value, depth + 1)}");
            first = false;
        }
        sb.Append("}");
        return sb.ToString();
    }
}