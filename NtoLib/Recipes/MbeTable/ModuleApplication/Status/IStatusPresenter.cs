namespace NtoLib.Recipes.MbeTable.ModuleApplication.Status;

public interface IStatusPresenter
{
    void Clear();
    void ShowSuccess(string message);
    void ShowWarning(string message);
    void ShowError(string message);
    void ShowInfo(string message);
}