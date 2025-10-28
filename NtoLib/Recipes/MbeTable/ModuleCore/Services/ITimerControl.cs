namespace NtoLib.Recipes.MbeTable.ModuleCore.Services;

public interface ITimerControl
{
    /// <summary>
    /// Resets internal timer state so that until the next active run the timer displays static totals
    /// and ignores PLC loop counters and elapsed seconds.
    /// </summary>
    void ResetForNewRecipe();
}