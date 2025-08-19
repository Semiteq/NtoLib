using NtoLib.Recipes.MbeTable.Core.Domain.Actions;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Contracts;

public interface IActionsFingerprintUtil
{
    string Compute(ActionManager actionManager);
}