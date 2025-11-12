using System.Collections.Generic;
using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.State;

public interface IPolicyReasonsSink
{
    void SetPolicyReasons(IEnumerable<IReason> reasons);
}