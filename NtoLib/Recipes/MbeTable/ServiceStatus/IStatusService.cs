using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable.ServiceStatus;

public interface IStatusService
{
    void AttachLabel(Label label, ColorScheme colorScheme);
    void Detach();
    void SetSink(IStatusSink? sink);

    void ShowInfo(string message);
    void ShowSuccess(string message);
    void ShowWarning(string message);
    void ShowError(string message);
    void Clear();
}