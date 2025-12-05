namespace NtoLib.MbeTable.ModuleApplication.Policy.Registry;

public sealed record ErrorPolicy(
	ErrorSeverity Severity,
	BlockingScope BlockingScope
);
