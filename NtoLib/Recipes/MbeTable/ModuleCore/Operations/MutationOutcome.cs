using System.Collections.Generic;
using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Operations;

// Meta outcome of a domain mutation operation (core decides to apply or reject).
public sealed record MutationOutcome(
    bool Applied,
    bool StructureChanged,
    bool DataChanged,
    int? AffectedRowIndex,
    IReadOnlyList<IReason> Reasons
);