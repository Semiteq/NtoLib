#nullable enable
namespace NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

using System.Collections.Generic;

using NtoLib.Recipes.MbeTable;

/// <summary>
/// Initializes pin groups and pins in the FB from a configuration file,
/// and returns a snapshot of configured groups for later runtime access.
/// </summary>
public interface IPinMapInitializer
{
    /// <summary>
    /// Reads PinGroupDefs.yaml, validates it, creates groups and pins in the provided FB,
    /// and returns a snapshot: GroupName -> (FirstPinId, PinQuantity).
    /// </summary>
    /// <param name="fb">Function block where pins must be created.</param>
    /// <param name="baseDirectory">Optional base directory. If null, AppDomain.CurrentDomain.BaseDirectory is used.</param>
    /// <param name="fileName">Configuration file name. Defaults to "PinGroupDefs.yaml".</param>
    /// <returns>Snapshot of configured groups: GroupName -> (FirstPinId, PinQuantity).</returns>
    Dictionary<string, (int FirstPinId, int PinQuantity)> InitializePinsFromConfig(
        MbeTableFB fb,
        string? baseDirectory = null,
        string fileName = "PinGroupDefs.yaml");
}