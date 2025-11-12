using System.Collections.Generic;
using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Attributes;

public sealed record ValidationSnapshot(
    bool IsValid,
    IReadOnlyList<IReason> Reasons
);