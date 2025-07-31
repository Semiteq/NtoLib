using System;

namespace NtoLib.Recipes.MbeTable.Status
{
    public interface IStatusManager
    {
        event Action<string, StatusMessage> StatusUpdated;
        event Action StatusCleared;
        void WriteStatusMessage(string message, StatusMessage statusMessage);
    }
}
