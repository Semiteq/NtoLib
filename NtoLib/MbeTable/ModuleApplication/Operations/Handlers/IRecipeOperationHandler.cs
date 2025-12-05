using System.Threading.Tasks;

using FluentResults;

namespace NtoLib.MbeTable.ModuleApplication.Operations.Handlers;

public interface IRecipeOperationHandler<in TArgs>
{
	Task<Result> ExecuteAsync(TArgs args);
}
