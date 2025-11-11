using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Errors;

public sealed class AssemblyUnsupportedTypeError : BilingualError
{
    public string TypeName { get; }

    public AssemblyUnsupportedTypeError(string typeName)
        : base(
            $"Unsupported type: {typeName}",
            $"Неподдерживаемый тип: {typeName}")
    {
        TypeName = typeName;
        Metadata["typeName"] = typeName;
    }
}