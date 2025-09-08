using System;

namespace NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

public class PlcRecipeStatusProvider : IPlcRecipeStatusProvider
{
    public event Action<PlcRecipeAvailable> AvailabilityChanged;
    public event Action<PlcRecipeStatus> StatusChanged;

    private bool _lastIsRecipeActive = true;
    private bool _lastIsEnaSend = false;
    private int _lastCurrentLine = -1;

    public void UpdateStatus(bool isRecipeActive, bool isEnaSend, int curentLine)
    {
        if (isRecipeActive != _lastIsRecipeActive
            || isEnaSend != _lastIsEnaSend)
        {
            AvailabilityChanged?.Invoke(new PlcRecipeAvailable(isRecipeActive, isEnaSend));
        }

        if (isRecipeActive != _lastIsRecipeActive
            || curentLine != _lastCurrentLine)
        {
            StatusChanged?.Invoke(new PlcRecipeStatus(isRecipeActive, curentLine));
        }

        _lastIsRecipeActive = isRecipeActive;
        _lastIsEnaSend = isEnaSend;
        _lastCurrentLine = curentLine;
    }

    public PlcRecipeStatus GetStatus() => new PlcRecipeStatus(_lastIsRecipeActive, _lastCurrentLine);
    public PlcRecipeAvailable GetAvailability() => new PlcRecipeAvailable(_lastIsRecipeActive, _lastIsEnaSend);
}