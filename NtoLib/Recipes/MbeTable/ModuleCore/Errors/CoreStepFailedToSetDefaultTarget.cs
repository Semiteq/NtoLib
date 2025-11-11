using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Errors;

public sealed class CoreStepFailedToSetDefaultTarget  : BilingualError
{
    public CoreStepFailedToSetDefaultTarget(string columnKey)
        : base($"Failed to set default target for column {columnKey}",
            $"Не удалось установить значение цели по умолчанию для столбца {columnKey}"
            )
    {
    }
}