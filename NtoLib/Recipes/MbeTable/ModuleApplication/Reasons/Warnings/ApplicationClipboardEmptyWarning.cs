using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Warnings;

public sealed class ApplicationClipboardEmptyWarning : BilingualWarning
{
    public ApplicationClipboardEmptyWarning()
        : base(
            "Clipboard is empty",
            "Буфер обмена пуст")
    {
    }
}