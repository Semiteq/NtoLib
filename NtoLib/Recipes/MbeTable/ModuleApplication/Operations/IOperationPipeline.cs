using System;
using System.Threading.Tasks;
using FluentResults;
using NtoLib.Recipes.MbeTable.ModuleApplication.State;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations;

public interface IOperationPipeline
{
    Task<Result> RunAsync(
        OperationId operationId,
        OperationKind operationKind,
        Func<Task<Result>> execute,
        string? successMessage = null,
        bool affectsRecipe = false);

    Result Run(
        OperationId operationId,
        OperationKind operationKind,
        Func<Result> execute,
        string? successMessage = null,
        bool affectsRecipe = false);
}