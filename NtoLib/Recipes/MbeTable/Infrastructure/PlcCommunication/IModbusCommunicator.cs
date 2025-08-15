using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;

namespace NtoLib.Recipes.MbeTable.Infrastructure.PlcCommunication;

/// <summary>
/// Provides functionality for Modbus communication with a PLC.
/// </summary>
public interface IModbusCommunicator
{
    // Returns true if the connection check succeeded, otherwise throws an exception
    bool CheckConnection(PinDataManager.CommunicationSettings settings);
    
    // Returns true if a recipe successfully written, otherwise throws an exception
    bool WriteRecipeToPlc(List<Step> recipe, PinDataManager.CommunicationSettings settings);
    
    // Returns recipe lines if successful, otherwise throws an exception
    List<Step> LoadRecipeFromPlc(PinDataManager.CommunicationSettings settings);
}