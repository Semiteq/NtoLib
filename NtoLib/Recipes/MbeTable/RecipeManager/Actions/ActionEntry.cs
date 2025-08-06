using NtoLib.Recipes.MbeTable.RecipeManager.StepManager;

namespace NtoLib.Recipes.MbeTable.RecipeManager.Actions;

public record ActionEntry(int Id, string Name, ActionType ActionType, DeployDuration DeployDuration)
{
    public int Id { get; } = Id;
    public string Name { get; } = Name;
    public ActionType ActionType { get; } = ActionType;
}