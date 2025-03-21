#nullable enable
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.RecipeLines.RecipeTime
{
    // Interface for recipe time manager to enable unit testing and decoupling.
    public interface IRecipeTimeManager
    {
        TimeSpan TotalTime { get; }
        void SetData(List<RecipeLine>? tableData, DataGridView? dataGridView);
        void Recalculate();
        TimeSpan GetRowTime(int originalIndex, int depth1, int depth2, int depth3);
        ICountTimer? ManageRecipeTimer(bool isRecipeActive, ICountTimer? currentTimer, TimeSpan totalRecipeTime);
        void UpdateRecipeTimeDisplay(float plcLineTime, ICountTimer? countdownTimer, Action<double> setTotalTime, Action<double> setLineTime);
    }

}