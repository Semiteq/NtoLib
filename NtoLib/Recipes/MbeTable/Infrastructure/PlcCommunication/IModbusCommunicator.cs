using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;

namespace NtoLib.Recipes.MbeTable.Infrastructure.PlcCommunication;

public interface IModbusCommunicator
{
    // Returns true if connection check succeeded, otherwise throws an exception
    bool CheckConnection(PinDataManager.CommunicationSettings settings);
    // Returns true if recipe successfully written, otherwise throws an exception
    bool WriteRecipeToPlc(List<Step> recipe, PinDataManager.CommunicationSettings settings);
    // Returns recipe lines if successful, otherwise throws an exception
    List<Step> LoadRecipeFromPlc(PinDataManager.CommunicationSettings settings);
}