using System;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable;

public partial class TableControl
{
    private async void ClickButton_AddLineAfter(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode || _presenter == null) return;
        await _presenter.AddStepAfterCurrent().ConfigureAwait(true);
    }

    private async void ClickButton_AddLineBefore(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode || _presenter == null) return;
        await _presenter.AddStepBeforeCurrent().ConfigureAwait(true);
    }

    private async void ClickButton_Delete(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode || _presenter == null) return;
        await _presenter.RemoveCurrentStep().ConfigureAwait(true);
    }

    private async void ClickButton_Open(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode || _presenter == null) return;
        await _presenter.LoadRecipeAsync().ConfigureAwait(true);
    }

    private async void ClickButton_Save(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode || _presenter == null) return;
        await _presenter.SaveRecipeAsync().ConfigureAwait(true);
    }

    private async void ClickButton_Send(object sender, EventArgs e)
    {
        if (FBConnector.DesignMode || _presenter == null) return;
        await _presenter.SendRecipeAsync().ConfigureAwait(true);

    }
}