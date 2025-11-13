namespace NtoLib.Recipes.MbeTable.ServiceStatus;

/// <summary>
/// UI-specific sink that actually renders status messages.
/// </summary>
public interface IStatusSink
{
    void Write(string message, StatusKind kind);
    void Clear();
}