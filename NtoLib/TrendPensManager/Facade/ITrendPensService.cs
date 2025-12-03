using FluentResults;

namespace NtoLib.TrendPensManager.Facade;

public interface ITrendPensService
{
	Result Refresh(string trendPath, string pinPath);
}
