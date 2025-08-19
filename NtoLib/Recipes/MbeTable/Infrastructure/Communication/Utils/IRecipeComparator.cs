using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Communication.Utils;

public interface IRecipeComparator
{
    Result Compare(Recipe recipe1, Recipe recipe2);
    Result CompareSteps(Step a, Step b);
    string Format(object v);
    bool ValueEquals(object a, object b);
    bool TryToDouble(object v, out double d);
}