namespace NtoLib.Recipes.MbeTable.Core.Domain.Actions;

/// <summary>
/// The service actions have locked ID's and not present in config yaml
/// </summary>
public enum ServiceActions
{
    Wait = 10,
    ForLoop = 20,
    EndForLoop = 30,
    Pause = 40
}