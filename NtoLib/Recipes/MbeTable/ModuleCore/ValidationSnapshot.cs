using System.Collections.Generic;

using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleCore;

public sealed record ValidationSnapshot(
    bool IsValid,
    IReadOnlyList<IReason>? Reasons = null
);