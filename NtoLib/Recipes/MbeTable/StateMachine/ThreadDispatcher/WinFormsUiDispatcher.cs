#nullable enable

using System;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.StateMachine.Contracts;

namespace NtoLib.Recipes.MbeTable.StateMachine.ThreadDispatcher;

/// <summary>
/// WinForms implementation that posts actions via Control.BeginInvoke.
/// </summary>
public sealed class WinFormsUiDispatcher : IUiDispatcher
{
    private readonly Control _control;

    public WinFormsUiDispatcher(Control control)
    {
        _control = control ?? throw new ArgumentNullException(nameof(control));
    }

    /// <inheritdoc />   
    public void Post(Action action)
    {
        if (action == null) return;

        try
        {
            if (_control.IsHandleCreated && !_control.IsDisposed)
            {
                _control.BeginInvoke(action);
            }
            else
            {
                // If control is not ready, fallback to inline to avoid losing the action.
                action();
            }
        }
        catch
        {
            // As a last resort, execute inline.
            try { action(); } catch { /* ignore */ }
        }
    }
}