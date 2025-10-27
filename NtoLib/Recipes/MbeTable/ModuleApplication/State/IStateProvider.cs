using System;

using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.State;

public interface IStateProvider
{
    event Action<UiPermissions>? PermissionsChanged;

    UiPermissions GetUiPermissions();

    UiStateSnapshot GetSnapshot();

    OperationDecision Evaluate(OperationId operation);

    Result<IDisposable> BeginOperation(OperationKind kind, OperationId operation);

    void EndOperation();

    void SetValidation(bool isValid);

    void SetStepCount(int stepCount);

    void SetPlcFlags(bool enaSendOk, bool recipeActive);
}