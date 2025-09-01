#nullable enable

using System;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Editing;

/// <summary>
/// A specialized control that extends <see cref="DataGridViewComboBoxEditingControl"/>
/// for use within a <see cref="DataGridView"/>. This control is tailored to handle
/// the editing of action target values in cells of type <c>ActionTargetComboBoxCell</c>.
/// </summary>
/// <remarks>
/// This control provides custom behavior for editing, including appropriate style
/// application and selection management.
/// </remarks>
internal sealed class ActionTargetEditingControl : DataGridViewComboBoxEditingControl
{
    protected override void OnCreateControl()
    {
        base.OnCreateControl();
        ForceNormalDrawMode();
        ApplyStyleFromCurrentCell();
    }

    public override void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle)
    {
        base.ApplyCellStyleToEditingControl(dataGridViewCellStyle);
        try
        {
            BackColor = dataGridViewCellStyle.BackColor;
            ForeColor = dataGridViewCellStyle.ForeColor;
            Font = dataGridViewCellStyle.Font;
        }
        catch { /* ignore styling errors */ }
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        ForceNormalDrawMode();
        ApplyStyleFromCurrentCell();
    }

    private void ForceNormalDrawMode()
    {
        DrawMode = DrawMode.Normal;
        FlatStyle = FlatStyle.Standard;
        DropDownStyle = ComboBoxStyle.DropDownList;
        IntegralHeight = false;
    }

    // Compatibility helper for callers that explicitly apply style from the current cell.
    public void ApplyStyleFromCurrentCell()
    {
        var dgv = EditingControlDataGridView;
        if (dgv == null) return;

        var style = dgv.CurrentCell?.InheritedStyle ?? dgv.DefaultCellStyle;
        ApplyCellStyleToEditingControl(style);
    }

    protected override void OnSelectionChangeCommitted(EventArgs e)
    {
        base.OnSelectionChangeCommitted(e);
        if (EditingControlDataGridView?.IsCurrentCellInEditMode ?? false)
        {
            EditingControlDataGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }
}