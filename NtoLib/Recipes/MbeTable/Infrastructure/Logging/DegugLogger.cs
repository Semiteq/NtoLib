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

public class DebugLogger : ILogger
{
    private static readonly object _fileLock = new();
    private static readonly string _logFilePath = Path.Combine(AppContext.BaseDirectory, "debug_log.txt");

    public void Log(string message, [CallerMemberName] string caller = "")
    {
#if DEBUG
        var output = $"{DateTime.Now:HH:mm:ss.fff} [{caller}] {message}";
        Debug.WriteLine(output);
        Console.WriteLine(output);
        WriteToFile(output);
#endif
    }
    
    public void LogException(Exception ex, object? contextData = null, [CallerMemberName] string caller = "")
    {
#if DEBUG
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var sb = new StringBuilder();

        var exceptionInfo = $"{timestamp} [ERROR] [{caller}] {ex.GetType().Name}: {ex.Message}";
        sb.AppendLine(exceptionInfo);
        
        Debug.WriteLine(exceptionInfo);
        Console.WriteLine(exceptionInfo);
        
        if (contextData != null)
        {
            var contextInfo = $"{timestamp} [CONTEXT] [{caller}] {SerializeContext(contextData)}";
            sb.AppendLine(contextInfo);
            Debug.WriteLine(contextInfo);
            Console.WriteLine(contextInfo);
        }
        
        if (!string.IsNullOrEmpty(ex.StackTrace))
        {
            var stackTraceInfo = $"{timestamp} [STACK] [{caller}] {ex.StackTrace}";
            sb.AppendLine(stackTraceInfo);
            Debug.WriteLine(stackTraceInfo);
        }
        
        WriteToFile(sb.ToString());
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
            // Fallback to debug output if file logging fails
            Debug.WriteLine($"Failed to write to log file: {fileEx.Message}");
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
        if (depth > 3) return "..."; // Prevent infinite recursion
        if (obj == null) return "null";

        var type = obj.GetType();
    
        // Handle primitives and strings
        if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime))
            return obj.ToString() ?? "null";

        // Handle collections
        if (obj is IEnumerable enumerable && type != typeof(string))
        {
            var items = new List<string>();
            foreach (var item in enumerable)
            {
                items.Add(SerializeObject(item, depth + 1));
                if (items.Count > 10) { items.Add("..."); break; } // Limit array size
            }
            return $"[{string.Join(",", items)}]";
        }

        // Handle objects
        var sb = new StringBuilder("{");
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .Take(5); // Limit properties count

        var first = true;
        foreach (var prop in properties)
        {
            if (!first) sb.Append(",");
            var value = prop.GetValue(obj);
            sb.Append($"\"{prop.Name}\":{SerializeObject(value, depth + 1)}");
            first = false;
        }
        sb.Append("}");
        return sb.ToString();
    }
}