using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Errors;

public sealed class AssemblyUnknownPlcAreaError : BilingualError
{
    public string Area { get; }

    public AssemblyUnknownPlcAreaError(string area)
        : base(
            $"Unknown PLC area: {area}",
            $"Неизвестная область PLC: {area}")
    {
        Area = area;
        Metadata["area"] = area;
    }
}