using System;
using System.Threading;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable.ServiceStatus;

public sealed class StatusService : IStatusService
{
    private readonly StatusFormatter _formatter;
    private IStatusSink? _sink;

    public StatusService(StatusFormatter formatter)
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    public void AttachLabel(Label label, ColorScheme colorScheme)
    {
        var sink = new LabelStatusSink(label, colorScheme);
        SetSink(sink);
    }

    public void Detach()
    {
        var old = Interlocked.Exchange(ref _sink, null);
        (old as IDisposable)?.Dispose();
    }

    public void SetSink(IStatusSink? sink)
    {
        var old = Interlocked.Exchange(ref _sink, sink);
        (old as IDisposable)?.Dispose();
    }

    public void ShowInfo(string message) => Write(message, StatusKind.Info);
    public void ShowSuccess(string message) => Write(message, StatusKind.Success);
    public void ShowWarning(string message) => Write(message, StatusKind.Warning);
    public void ShowError(string message) => Write(message, StatusKind.Error);

    public void Clear()
    {
        var sink = Volatile.Read(ref _sink);
        sink?.Clear();
    }

    private void Write(string message, StatusKind kind)
    {
        var sink = Volatile.Read(ref _sink);
        if (sink == null) return;

        var formatted = _formatter.Format(message);
        sink.Write(formatted, kind);
    }
}