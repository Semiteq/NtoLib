using System;

namespace NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

public interface IPlcRecipeStatusProvider
{
    event Action<PlcRecipeAvailable> AvailabilityChanged;
    void UpdateStatus(bool isRecipeActive, bool isEnaSend, int curentLine);
    public PlcRecipeStatus GetStatus();
    public PlcRecipeAvailable GetAvailability();
}