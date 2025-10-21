using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable.ServiceStatus;

/// <summary>
/// Facade for posting status messages. Thread-safe.
/// UI sink can be attached later via AttachLabel or SetSink.
/// </summary>
public interface IStatusService
{
    /// <summary>
    /// Attaches WinForms Label as a rendering sink using provided ColorScheme.
    /// </summary>
    void AttachLabel(Label label, ColorScheme colorScheme);

    /// <summary>
    /// Detaches current sink if any.
    /// </summary>
    void Detach();

    /// <summary>
    /// Attaches or replaces the current sink. Pass null to detach.
    /// </summary>
    void SetSink(IStatusSink? sink);

    void ShowInfo(string message);
    void ShowWarning(string message);
    void ShowError(string message);
    void Clear();
}