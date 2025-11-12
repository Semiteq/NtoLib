using System;
using System.Collections.Generic;
using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.State;

public interface IStateProvider
{
    event Action<UiPermissions>? PermissionsChanged;
    event Action<bool>? RecipeConsistencyChanged;

    UiPermissions GetUiPermissions();
    UiStateSnapshot GetSnapshot();

    OperationDecision Evaluate(OperationId operation);

    Result<IDisposable> BeginOperation(OperationKind kind, OperationId operation);
    void EndOperation();

    void SetStepCount(int stepCount);
    void SetPlcFlags(bool enaSendOk, bool recipeActive);

    void SetPolicyReasons(IEnumerable<IReason> reasons);

    void SetRecipeConsistent(bool isConsistent);
}