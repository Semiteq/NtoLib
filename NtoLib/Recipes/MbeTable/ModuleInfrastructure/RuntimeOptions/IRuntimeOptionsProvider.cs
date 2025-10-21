namespace NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;

public interface IRuntimeOptionsProvider
{
    RuntimeOptions GetCurrent();
}