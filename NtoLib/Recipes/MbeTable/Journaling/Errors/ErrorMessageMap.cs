namespace NtoLib.Recipes.MbeTable.Journaling.Errors;

public class ErrorMessageMap
{
    private record ErrorMessageBox(ErrorCode Code, string UiMessage, string SystemMessage);
    
    
}