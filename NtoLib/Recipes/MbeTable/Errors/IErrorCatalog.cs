namespace NtoLib.Recipes.MbeTable.Errors;

public interface IErrorCatalog
{
    bool TryGetMessage(Codes code, out string message);
    string GetMessageOrDefault(Codes code);
}