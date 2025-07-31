using System;
using System.Diagnostics;

namespace NtoLib.Recipes.MbeTable.Logger;

public class DebugLogger : ILogger
{
    public void Log(string message, string caller = "")
    {
        Debug.WriteLine($"{DateTime.Now:HH:mm:ss} [{caller}] {message}");
    }
}