#nullable enable
using System;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Editing;

internal sealed class BaseRecipeComboBoxEditingControl : DataGridViewComboBoxEditingControl
{
    private bool _styled;

    protected override void OnCreateControl()
    {
        base.OnCreateControl();
        Normalize();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        Normalize();
    }

    private void Normalize()
    {
        if (_styled) return;
        FlatStyle = FlatStyle.Flat;
        DropDownStyle = ComboBoxStyle.DropDownList;
        IntegralHeight = false;
        DrawMode = DrawMode.Normal;
        _styled = true;
    }

    public void ApplyStyleFromCurrentCell()
    {
        var dgv = EditingControlDataGridView;
        if (dgv == null) return;

        var style = dgv.CurrentCell?.InheritedStyle ?? dgv.DefaultCellStyle;
        try
        {
            BackColor = style.BackColor;
            ForeColor = style.ForeColor;
            Font = style.Font;
        }
        catch { /* ignore */ }
    }

    protected override void OnSelectionChangeCommitted(EventArgs e)
    {
        base.OnSelectionChangeCommitted(e);
        if (EditingControlDataGridView?.IsCurrentCellInEditMode ?? false)
            EditingControlDataGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }
}