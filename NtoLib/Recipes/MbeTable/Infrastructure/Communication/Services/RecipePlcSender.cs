#nullable enable
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Contracts;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication.Utils;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Communication.Services;

public sealed class RecipePlcSender : IRecipePlcSender
{
    private readonly IModbusTransport _modbusTransport;
    private readonly IPlcProtocol _plcProtocol;
    private readonly IPlcRecipeSerializer _plcRecipeSerializer;
    private readonly IRecipeComparator _recipeComparator;
    private readonly PlcCapacityCalculator _plcCapacityCalculator;
    private readonly ICommunicationSettingsProvider _communicationSettingsProvider;
    private readonly ILogger _debugLogger;

    public RecipePlcSender(
        IModbusTransport modbusTransport,
        IPlcProtocol plcProtocol,
        IPlcRecipeSerializer plcRecipeSerializer,
        IRecipeComparator recipeComparator,
        PlcCapacityCalculator plcCapacityCalculator,
        ICommunicationSettingsProvider communicationSettingsProvider,
        ILogger debugLogger)
    {
        _modbusTransport = modbusTransport ?? throw new ArgumentNullException(nameof(modbusTransport)); // НОВОЕ
        _plcProtocol = plcProtocol ?? throw new ArgumentNullException(nameof(plcProtocol));
        _plcRecipeSerializer = plcRecipeSerializer ?? throw new ArgumentNullException(nameof(plcRecipeSerializer));
        _recipeComparator = recipeComparator ?? throw new ArgumentNullException(nameof(recipeComparator));
        _plcCapacityCalculator = plcCapacityCalculator ?? throw new ArgumentNullException(nameof(plcCapacityCalculator));
        _communicationSettingsProvider = communicationSettingsProvider ?? throw new ArgumentNullException(nameof(communicationSettingsProvider));
        _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));
    }

    private CommunicationSettings Settings => _communicationSettingsProvider.GetSettings();
    
    public async Task<Result> UploadAndVerifyAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        try
        {
            var capacityResult = _plcCapacityCalculator.TryCheckCapacity(recipe, Settings);
            if (capacityResult.IsFailed) return capacityResult;

            var connectResult = _modbusTransport.Connect();
            if (connectResult.IsFailed)
                return connectResult;

            var (ints, floats, bools) = _plcRecipeSerializer.ToRegisters(recipe.Steps);

            var writeRes = _plcProtocol.WriteAllAreas(ints, floats, bools, recipe.Steps.Count);
            if (writeRes.IsFailed)
            {
                return writeRes;
            }

            await Task.Delay(Math.Max(0, Settings.VerifyDelayMs), cancellationToken);

            var readRes = _plcProtocol.ReadAllAreas();
            if (readRes.IsFailed)
            {
                return readRes.ToResult();
            }

            var steps = _plcRecipeSerializer.FromRegisters(readRes.Value.IntData, readRes.Value.FloatData, readRes.Value.RowCount);
            var back = new Recipe(steps.ToImmutableList());

            var compareResult = _recipeComparator.Compare(recipe, back);
            if (compareResult.IsFailed)
            {
                var errors = string.Join(", ", compareResult.Errors.Select(e => e.Message));
                _debugLogger.Log($"Verification failed: PLC data does not match uploaded recipe.");
                return Result.Fail($"Проверка не пройдена: данные в ПЛК отличаются от загруженных. Ошибка: {errors}");
            }

            return Result.Ok().WithSuccess("Рецепт успешно записан и проверен.");
        }
        catch (OperationCanceledException)
        {
            return Result.Fail("Операция отменена.");
        }
        catch (Exception ex)
        {
            _debugLogger.Log($"Unexpected error: {ex}");
            return Result.Fail("Произошла непредвиденная ошибка при записи рецепта.");
        }
        finally
        {
            _modbusTransport.TryDisconnect();
        }
    }

    public Task<Result<Recipe>> DownloadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var connectResult = _modbusTransport.Connect();
            if (connectResult.IsFailed)
                return Task.FromResult(Result.Fail<Recipe>(connectResult.Errors));

            var readRes = _plcProtocol.ReadAllAreas();
            if (readRes.IsFailed)
                return Task.FromResult(readRes.ToResult<Recipe>());

            var steps = _plcRecipeSerializer.FromRegisters(readRes.Value.IntData, readRes.Value.FloatData, readRes.Value.RowCount);
            var recipe = new Recipe(steps.ToImmutableList());
            return Task.FromResult(Result.Ok(recipe));
        }
        catch (Exception ex)
        {
            _debugLogger.Log($"Unexpected error on download: {ex}");
            return Task.FromResult(Result.Fail<Recipe>("Произошла непредвиденная ошибка при чтении рецепта."));
        }
        finally
        {
            _modbusTransport.TryDisconnect();
        }
    }
}