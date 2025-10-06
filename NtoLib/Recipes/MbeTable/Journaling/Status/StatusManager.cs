using System;

namespace NtoLib.Recipes.MbeTable.Journaling.Status
{
    public class StatusManager : IStatusManager
    {
        public event Action<string, StatusMessage> StatusUpdated;
        public event Action StatusCleared; 

        public StatusManager() { }

        public void WriteStatusMessage(string message, StatusMessage statusMessage)
            => StatusUpdated?.Invoke(message, statusMessage);
        
        public void ClearStatusMessage() 
            => StatusCleared?.Invoke();
    }
}