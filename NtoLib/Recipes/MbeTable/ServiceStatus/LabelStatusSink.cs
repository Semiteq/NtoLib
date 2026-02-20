using System;
using System.Drawing;
using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable.ServiceStatus;

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

	public void Dispose()
	{
		_disposed = true;
	}

	public void Write(string message, StatusKind kind)
	{
		if (_disposed)
		{
			return;
		}

		if (!EnsureHandle())
		{
			return;
		}

		void Apply()
		{
			if (_disposed)
			{
				return;
			}
			_label.Text = message ?? string.Empty;
			_label.BackColor = ResolveBackColor(kind);
		}

		TryInvoke(Apply);
	}

	public void Clear()
	{
		if (_disposed)
		{
			return;
		}

		if (!EnsureHandle())
		{
			return;
		}

		void Apply()
		{
			if (_disposed)
			{
				return;
			}
			_label.Text = string.Empty;
			_label.BackColor = _scheme.StatusBgColor;
		}

		TryInvoke(Apply);
	}

	private Color ResolveBackColor(StatusKind kind)
	{
		switch (kind)
		{
			case StatusKind.Info:
				return _scheme.StatusInfoColor;
			case StatusKind.Success:
				return _scheme.StatusSuccessColor;
			case StatusKind.Warning:
				return _scheme.StatusWarningColor;
			case StatusKind.Error:
				return _scheme.StatusErrorColor;
			case StatusKind.None:
			default:
				return _scheme.StatusBgColor;
		}
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
			{
				_label.BeginInvoke(action);
			}
			else
			{
				action();
			}
		}
		catch
		{
		}
	}
}
