using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Warnings;

public sealed class CoreForLoopMissingIterationCountWarning : BilingualWarning
{
    public int StepIndex { get; }

    public CoreForLoopMissingIterationCountWarning(int stepIndex)
        : base(
            $"Missing iteration count property at step {stepIndex}",
            $"Отсутствует свойство количества итераций на шаге {stepIndex + 1}")
    {
        StepIndex = stepIndex;
        Metadata["stepIndex"] = stepIndex;
    }
}