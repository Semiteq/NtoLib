#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Infrastructure.PlcCommunication;

/// <summary>
/// PLC recipe orchestrator: connection check, capacity check, write, read-back, compare.
/// </summary>
public sealed class RecipePlcServiceV1 : IRecipePlcService
{
    private readonly IModbusCommunicator _modbus;
    private readonly IRecipeComparator _comparator;
    private readonly ILogger _debugLogger;
    private readonly PlcCapacityCalculator _plcCapacityCalculator;

    public RecipePlcServiceV1(IModbusCommunicator modbus, IRecipeComparator comparator, PlcCapacityCalculator plcCapacityCalculator, DebugLogger debugLogger)
    {
        _modbus = modbus ?? throw new ArgumentNullException(nameof(modbus));
        _comparator = comparator ?? throw new ArgumentNullException(nameof(comparator));
        _plcCapacityCalculator = plcCapacityCalculator ?? throw new ArgumentNullException(nameof(plcCapacityCalculator));
        _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));
    }

    /// <summary>
    /// Uploads a recipe to the PLC, verifies its correctness by reading it back,
    /// and compares it with the original recipe to ensure accuracy.
    /// </summary>
    /// <param name="recipe">The recipe to be uploaded to the PLC.</param>
    /// <param name="settings">The communication settings used to connect to the PLC.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests during the operation.</param>
    /// <returns>A tuple containing a boolean indicating success, and an error message if the upload or verification fails.</returns>
    public async Task<(bool Ok, string? Error)> UploadAndVerifyAsync(
        Recipe recipe,
        CommunicationSettings settings,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            _debugLogger.Log("Checking connection...");
            var connected = await Task.Run(() =>
            {
                try { return _modbus.CheckConnection(settings); }
                catch { return false; }
            }, cancellationToken);

            if (!connected)
                return (false, "PLC connection failed");

            var (okCapacity, capacityError) = _plcCapacityCalculator.TryCheckCapacity(recipe, settings);
            if (!okCapacity)
            {
                _debugLogger.Log($"Capacity error: {capacityError}");
                return (false, capacityError ?? "Insufficient PLC memory");
            }

            cancellationToken.ThrowIfCancellationRequested();

            _debugLogger.Log("Writing recipe to PLC...");
            await Task.Run(() => _modbus.WriteRecipeToPlc(recipe.Steps.ToList(), settings), cancellationToken);

            await Task.Delay(200, cancellationToken);

            _debugLogger.Log("Reading recipe back from PLC...");
            var readBack = await Task.Run(() => _modbus.LoadRecipeFromPlc(settings), cancellationToken);

            _debugLogger.Log("Comparing recipes...");
            var equal = _comparator.Compare(recipe.Steps.ToList(), readBack);

            if (!equal)
            {
                _debugLogger.Log("Uploaded recipe differs from PLC content");
                return (false, "Recipe differs after upload");
            }

            _debugLogger.Log("Upload and verification succeeded");
            return (true, null);
        }
        catch (OperationCanceledException)
        {
            _debugLogger.Log("Canceled");
            return (false, "Operation canceled");
        }
        catch (Exception ex)
        {
            _debugLogger.LogException(ex);
            _debugLogger.Log($"Error: {ex}");
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Downloads a recipe from the PLC and returns it with an error message if applicable.
    /// </summary>
    /// <param name="settings">The communication settings used to connect to the PLC.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A tuple containing the downloaded Recipe if successful, and an error message if an exception occurs or the operation fails.</returns>
    public async Task<(Recipe? Recipe, string? Error)> DownloadAsync(
        CommunicationSettings settings,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            _debugLogger.Log("Checking connection...");
            var connected = await Task.Run(() =>
            {
                try { return _modbus.CheckConnection(settings); }
                catch { return false; }
            }, cancellationToken);

            if (!connected)
                return (null, "PLC connection failed");

            _debugLogger.Log("Loading recipe from PLC...");
            var steps = await Task.Run(() => _modbus.LoadRecipeFromPlc(settings), cancellationToken);

            var recipe = new Recipe(steps.ToImmutableList());
            return (recipe, null);
        }
        catch (OperationCanceledException)
        {
            _debugLogger.Log("Canceled");
            return (null, "Operation canceled");
        }
        catch (Exception ex)
        {
            _debugLogger.LogException(ex);
            _debugLogger.Log($"Error: {ex}");
            return (null, ex.Message);
        }
    }
}