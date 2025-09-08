using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Style;

// Manages per-button enabled/disabled visual styling.
public sealed class ButtonStateStyler : IDisposable
{
    private readonly struct Entry
    {
        public readonly Button Button;
        public readonly Color NormalBack;
        public readonly Color NormalFore;
        public readonly EventHandler Handler;
        public Entry(Button b, Color nb, Color nf, EventHandler h)
        {
            Button = b; NormalBack = nb; NormalFore = nf; Handler = h;
        }
    }

    private readonly List<Entry> _entries = new();
    private bool _disposed;

    public Color DisabledBackColor { get; set; } = Color.DimGray;
    public Color DisabledForeColor { get; set; } = Color.White;

    public void Register(Button button)
    {
        if (button == null) return;
        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;

        // Capture initial colors as "normal"
        var normalBack = button.BackColor;
        var normalFore = button.ForeColor;

        void Apply()
        {
            if (button.IsDisposed) return;
            if (button.Enabled)
            {
                button.BackColor = normalBack;
                button.ForeColor = normalFore;
            }
            else
            {
                button.BackColor = DisabledBackColor;
                button.ForeColor = DisabledForeColor;
            }
        }

        var handler = new EventHandler((_, _) => Apply());
        button.EnabledChanged += handler;
        Apply(); // initial apply

        _entries.Add(new Entry(button, normalBack, normalFore, handler));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        for (int i = 0; i < _entries.Count; i++)
        {
            var e = _entries[i];
            try { e.Button.EnabledChanged -= e.Handler; } catch { /* ignore */ }
        }
        _entries.Clear();
    }
}