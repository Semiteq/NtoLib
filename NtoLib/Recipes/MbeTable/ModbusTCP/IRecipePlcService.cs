

using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using NtoLib.Recipes.MbeTable.Core.Entities;

namespace NtoLib.Recipes.MbeTable.ModbusTCP;

/// <summary>
/// Public facade for recipe upload/download operations with PLC.
/// Now returns raw data instead of assembled Recipe.
/// </summary>
public interface IRecipePlcService
{
    /// <summary>
    /// Sends recipe to PLC.
    /// </summary>
    Task<Result> SendAsync(Recipe recipe, CancellationToken ct = default);

    /// <summary>
    /// Receives raw recipe data from PLC.
    /// </summary>
    Task<Result<(int[] IntData, int[] FloatData, int RowCount)>> ReceiveAsync(CancellationToken ct = default);
}