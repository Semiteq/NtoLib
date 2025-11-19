using System;

using FluentResults;
using Microsoft.Extensions.Logging;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Common;

internal static class AssemblyPipeline
{
    public static Result<Recipe> Assemble(
        string sourceTag,
        ILogger logger,
        AssemblyValidator validator,
        Func<Result<Recipe>> assembleOperation)
    {
        logger.LogDebug("Starting {SourceTag} assembly", sourceTag);

        var assemblyResult = assembleOperation();
        if (assemblyResult.IsFailed)
        {
            logger.LogError("{SourceTag} assembly failed", sourceTag);
            return assemblyResult;
        }

        var recipe = assemblyResult.Value;
        logger.LogDebug("Assembled {StepsCount} steps from {SourceTag}", recipe.Steps.Count, sourceTag);

        var validationResult = validator.ValidateRecipe(recipe);
        if (validationResult.IsFailed)
        {
            logger.LogError("Recipe validation failed after {SourceTag} assembly", sourceTag);
            return validationResult.ToResult<Recipe>();
        }

        var result = Result.Ok(recipe);
        if (validationResult.Reasons.Count > 0)
            result = result.WithReasons(validationResult.Reasons);

        logger.LogDebug("{SourceTag} assembly completed successfully", sourceTag);
        return result;
    }
}