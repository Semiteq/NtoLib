using FluentResults;

using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

namespace NtoLib.Recipes.MbeTable.ResultsExtension;

public sealed class ValidationIssue : Success
{
    public Codes Code { get; }
    
    public ValidationIssue(Codes code) : base(string.Empty)
    {
        Code = code;
        WithMetadata(nameof(Codes), code);
    }
}