#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Communication;

public interface IRecipePlcSender
{
    Task<Result> SendAndVerifyRecipeAsync(Recipe recipe, CancellationToken cancellationToken = default);
    Task<Result<Recipe>> ReciveRecipeAsync(CancellationToken cancellationToken = default);
}