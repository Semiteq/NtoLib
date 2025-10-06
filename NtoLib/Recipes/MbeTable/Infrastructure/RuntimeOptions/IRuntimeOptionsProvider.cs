namespace NtoLib.Recipes.MbeTable.Infrastructure.RuntimeOptions;

public interface IRuntimeOptionsProvider
{
    RuntimeOptions GetCurrent();
}