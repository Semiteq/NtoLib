namespace NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

public sealed record ErrorDefinition(
    Codes Code,
    string Message,
    ErrorSeverity Severity,
    BlockingScope BlockingScope
);