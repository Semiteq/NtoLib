using System;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable;

public partial class TableControl
{
    private async void ClickButton_AddLineAfter(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode || _presenter == null) return;

        try
        {
            await _presenter.AddStepAfterCurrent().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Add step failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void ClickButton_AddLineBefore(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode || _presenter == null) return;

        try
        {
            await _presenter.AddStepBeforeCurrent().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Add step failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void ClickButton_Delete(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode || _presenter == null) return;

        try
        {
            await _presenter.RemoveCurrentStep().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Remove step failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void ClickButton_Open(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode || _presenter == null) return;

        try
        {
            await _presenter.LoadRecipeAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Load recipe failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void ClickButton_Save(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode || _presenter == null) return;

        try
        {
            await _presenter.SaveRecipeAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Save recipe failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void ClickButton_Send(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode || _presenter == null) return;

        try
        {
            await _presenter.SendRecipeAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Send recipe failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}