using FluentResults;

using NtoLib.TrendPensManager.Entities;

namespace NtoLib.TrendPensManager.Facade;

public interface ITrendPensService
{
	Result<AutoConfigResult> AutoConfigurePens(
		string trendRootPath,
		string configLoaderRootPath,
		string dataRootPath,
		ServiceSelectionOptions serviceSelectionOptions);
}
