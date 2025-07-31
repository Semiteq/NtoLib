namespace NtoLib.Recipes.MbeTable.Logger;

public interface ILogger
{
    void Log(string message, string caller = "");
}