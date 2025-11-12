using System.Collections.Generic;
using FluentResults;
using NtoLib.Recipes.MbeTable.ModuleApplication.State;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Policies;

public interface IPolicyEngine
{
    OperationDecision Decide(OperationId operation, IEnumerable<IReason> reasons);
    bool IsBlocking(OperationId operation, IReason reason);
}