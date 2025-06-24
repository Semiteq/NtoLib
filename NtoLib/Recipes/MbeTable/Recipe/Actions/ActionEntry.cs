namespace NtoLib.Recipes.MbeTable.Recipe.Actions;

public record ActionEntry(int Id, string Name, ActionType ActionType)
{
    public int Id { get; } = Id;
    public string Name { get; } = Name;
    public ActionType ActionType { get; } = ActionType;
}