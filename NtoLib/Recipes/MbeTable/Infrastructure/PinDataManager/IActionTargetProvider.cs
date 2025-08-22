using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

public interface IActionTargetProvider
{
    public Dictionary<int, string> GetShutterNames();
    public Dictionary<int, string> GetHeaterNames();
    public Dictionary<int, string> GetNitrogenSourceNames();

    public void RefreshTargets();

    public int GetMinimalShutterId();
    public int GetMinimalHeaterId();
    public int GetMinimalNitrogenSourceId();
}