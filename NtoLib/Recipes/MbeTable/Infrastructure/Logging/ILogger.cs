#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Logging;

public interface ILogger
{
    void Log(string message, [CallerMemberName] string caller = "");
    void LogException(Exception ex, object? contextData = null, [CallerMemberName] string caller = "");
}