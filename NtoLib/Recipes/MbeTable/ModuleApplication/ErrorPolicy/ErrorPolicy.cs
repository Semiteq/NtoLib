namespace NtoLib.Recipes.MbeTable.ModuleApplication.ErrorPolicy;

public sealed record ErrorPolicy(
    ErrorSeverity Severity,
    BlockingScope BlockingScope
);