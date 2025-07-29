namespace NtoLib.Recipes.MbeTable.Recipe.StatusManager
{
    public interface IStatusManager
    {
        void WriteStatusMessage(string message, StatusMessage isError);
    }
}
