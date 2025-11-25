using System.Threading.Tasks;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Modbus;

/// <summary>
/// Provides async file and PLC operations for recipe management.
/// Returns FluentResults for top-level error handling.
/// </summary>
public interface IModbusTcpService
{
	/// <summary>
	/// Sends the specified recipe to PLC asynchronously.
	/// </summary>
	/// <param name="recipe">Recipe to send.</param>
	/// <returns>Result indicating success or errors.</returns>
	Task<Result> SendRecipeAsync(Recipe recipe);

	/// <summary>
	/// Receives a recipe from PLC asynchronously.
	/// </summary>
	/// <returns>Result containing the received recipe or errors.</returns>
	Task<Result<Recipe>> ReceiveRecipeAsync();
}
