#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Communication;

public interface IRecipePlcSender
{
    Task<Result> UploadAndVerifyAsync(Recipe recipe, CancellationToken cancellationToken = default);
    Task<Result<Recipe>> DownloadAsync(CancellationToken cancellationToken = default);
}