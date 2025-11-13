using System.Collections.Generic;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Pipeline;

internal sealed class ReasonMerger
{
    public static IEnumerable<IReason> From(Result result)
    {
        return result.Reasons ?? Enumerable.Empty<IReason>();
    }

    public static IEnumerable<IReason> From<T>(Result<T> result)
    {
        var own = result.Reasons ?? Enumerable.Empty<IReason>();

        if (result.IsSuccess && result.Value is ValidationSnapshot snapshot)
        {
            var snap = snapshot.Reasons ?? Enumerable.Empty<IReason>();
            return own.Concat(snap);
        }

        return own;
    }
}