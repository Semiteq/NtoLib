namespace NtoLib.Recipes.MbeTable.Core.Domain.Steps;

public interface IStepFactory
{
    StepBuilder ForAction(int actionId);
}