using System;

using InSAT.OPC;

namespace NtoLib.Recipes.MbeTable;

public partial class MbeTableFB
{
    internal const int IdRecipeActive = 1;
    internal const int IdCurrentLine = 3;
    internal const int IdStepCurrentTime = 4;
    internal const int IdForLoopCount1 = 5;
    internal const int IdForLoopCount2 = 6;
    internal const int IdForLoopCount3 = 7;
    internal const int IdEnaSend = 8;
    internal const int IdTotalTimeLeft = 101;
    internal const int IdLineTimeLeft = 102;
    
    /// <summary>
    /// Handles timer updates and synchronizes time values with pins.
    /// </summary>
    /// <param name="stepTimeLeft">Time remaining for current step.</param>
    /// <param name="totalTimeLeft">Total time remaining for recipe.</param>
    private void OnTimesUpdated(TimeSpan stepTimeLeft, TimeSpan totalTimeLeft)
    {
        if (GetPinQuality(IdLineTimeLeft) != OpcQuality.Good
            || !AreFloatsEqual(GetPinValue<float>(IdLineTimeLeft), (float)stepTimeLeft.TotalSeconds))
        {
            SetPinValue(IdLineTimeLeft, (float)stepTimeLeft.TotalSeconds);
        }

        if (GetPinQuality(IdTotalTimeLeft) != OpcQuality.Good
            || !AreFloatsEqual(GetPinValue<float>(IdTotalTimeLeft), (float)totalTimeLeft.TotalSeconds))
        {
            SetPinValue(IdTotalTimeLeft, (float)totalTimeLeft.TotalSeconds);
        }
    }
    
    private bool AreFloatsEqual(float a, float b) => Math.Abs(a - b) <= _epsilon;
}

