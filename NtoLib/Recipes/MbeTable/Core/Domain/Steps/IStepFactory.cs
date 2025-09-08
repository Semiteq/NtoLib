namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps;

public interface IStepFactory
{
    IStepBuilder ForAction(int actionId);
}