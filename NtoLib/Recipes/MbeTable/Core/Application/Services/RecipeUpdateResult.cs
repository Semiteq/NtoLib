using NtoLib.Recipes.MbeTable.Core.Domain.Analysis;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;

namespace NtoLib.Recipes.MbeTable.Core.Application.Services;

public record RecipeUpdateResult(Recipe Recipe, LoopValidationResult LoopResult, RecipeTimeAnalysis TimeResult);