using NtoLib.Recipes.MbeTable.Core.Domain.Services;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Actions;

public record ActionEntry(int Id, string Name, ActionType ActionType, DeployDuration DeployDuration)
{
    public int Id { get; } = Id;
    public string Name { get; } = Name;
    public ActionType ActionType { get; } = ActionType;
}