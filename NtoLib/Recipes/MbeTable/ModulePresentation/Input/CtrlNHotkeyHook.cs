using System;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Input;

internal sealed class CtrlNHotkeyHook : IMessageFilter, IDisposable
{
	private const int WmKeyDown = 0x0100;
	private const int WmSysKeyDown = 0x0104;

	private readonly WeakReference<Control> _focusRoot;
	private readonly Action _onCtrlN;
	private bool _isDisposed;

	public CtrlNHotkeyHook(Control focusRoot, Action onCtrlN)
	{
		if (focusRoot == null)
		{
			throw new ArgumentNullException(nameof(focusRoot));
		}

		_onCtrlN = onCtrlN ?? throw new ArgumentNullException(nameof(onCtrlN));

		_focusRoot = new WeakReference<Control>(focusRoot);

		try
		{
			Application.AddMessageFilter(this);
		}
		catch
		{
			// ignored (some hosts may not support global filters)
		}
	}

	public void Dispose()
	{
		if (_isDisposed)
		{
			return;
		}

		_isDisposed = true;

		try
		{
			Application.RemoveMessageFilter(this);
		}
		catch
		{
			// ignored
		}
	}

	public bool PreFilterMessage(ref Message m)
	{
		if (_isDisposed)
		{
			return false;
		}

		if (m.Msg != WmKeyDown && m.Msg != WmSysKeyDown)
		{
			return false;
		}

		if ((Control.ModifierKeys & Keys.Control) != Keys.Control)
		{
			return false;
		}

		var key = (Keys)(int)m.WParam;
		if (key != Keys.N)
		{
			return false;
		}

		if (!_focusRoot.TryGetTarget(out var focusRoot))
		{
			TrySelfRemove();

			return false;
		}

		try
		{
			if (focusRoot.IsDisposed || !focusRoot.ContainsFocus)
			{
				return false;
			}
		}
		catch
		{
			return false;
		}

		try
		{
			_onCtrlN();
		}
		catch
		{
			// ignored
		}

		return true;
	}

	private void TrySelfRemove()
	{
		try
		{
			Application.RemoveMessageFilter(this);
		}
		catch
		{
			// ignored
		}
	}
}
