namespace NtoLib.Recipes.MbeTable.StatusManager
{
    public interface IStatusManager
    {
        void WriteStatusMessage(string message, StatusMessage isError);
    }
}
