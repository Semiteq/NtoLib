#nullable enable

using System.Threading;
using System.Threading.Tasks;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Infrastructure.PlcCommunication;

/// <summary>
/// High-level PLC recipe service: uploads a recipe, verifies by reading it back, or downloads it.
/// </summary>
public interface IRecipePlcService
{
    /// <summary>
    /// Asynchronously uploads a recipe to a PLC and verifies the upload by reading the recipe back.
    /// </summary>
    /// <param name="recipe">The recipe to be uploaded, represented as an immutable snapshot.</param>
    /// <param name="settings">The communication settings for interacting with the PLC.</param>
    /// <param name="cancellationToken">Token to monitor for request cancellation.</param>
    /// <returns>A tuple containing a boolean indicating success or failure, and an error message if the operation fails.</returns>
    Task<(bool Ok, string? Error)> UploadAndVerifyAsync(
        Recipe recipe,
        CommunicationSettings settings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously downloads a recipe from a PLC.
    /// </summary>
    /// <param name="settings">The communication settings required to connect to the PLC.</param>
    /// <param name="cancellationToken">Token to monitor for request cancellation.</param>
    /// <returns>A tuple containing the downloaded recipe if successful, and an error message if the operation fails.</returns>
    Task<(Recipe? Recipe, string? Error)> DownloadAsync(
        CommunicationSettings settings,
        CancellationToken cancellationToken = default);
}