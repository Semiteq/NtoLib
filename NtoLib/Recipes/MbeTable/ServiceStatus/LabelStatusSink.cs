﻿using System;
using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable.ServiceStatus;

/// <summary>
/// WinForms sink that writes messages into a Label using ColorScheme and UI-thread marshaling.
/// </summary>
public sealed class LabelStatusSink : IStatusSink, IDisposable
{
    private readonly Label _label;
    private readonly ColorScheme _scheme;
    private bool _disposed;

    public LabelStatusSink(Label label, ColorScheme scheme)
    {
        _label = label ?? throw new ArgumentNullException(nameof(label));
        _scheme = scheme ?? throw new ArgumentNullException(nameof(scheme));
    }

    public void Write(string message, StatusKind kind)
    {
        if (_disposed) return;
        if (!EnsureHandle()) return;

        void Apply()
        {
            if (_disposed) return;
            _label.Text = message ?? string.Empty;
            _label.BackColor = kind switch
            {
                StatusKind.Info => _scheme.StatusInfoColor,
                StatusKind.Warning => _scheme.StatusWarningColor,
                StatusKind.Error => _scheme.StatusErrorColor,
                _ => _scheme.StatusBgColor
            };
        }

        TryInvoke(Apply);
    }

    public void Clear()
    {
        if (_disposed) return;
        if (!EnsureHandle()) return;

        void Apply()
        {
            if (_disposed) return;
            _label.Text = string.Empty;
            _label.BackColor = _scheme.StatusBgColor;
        }

        TryInvoke(Apply);
    }

    public void Dispose()
    {
        _disposed = true;
    }

    private bool EnsureHandle()
    {
        try
        {
            return _label.IsHandleCreated && !_label.IsDisposed;
        }
        catch
        {
            return false;
        }
    }

    private void TryInvoke(Action action)
    {
        try
        {
            if (_label.InvokeRequired)
                _label.BeginInvoke(action);
            else
                action();
        }
        catch
        {
        }
    }
}