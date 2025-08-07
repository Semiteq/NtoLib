using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

public interface IActionTargetProvider
{
    Dictionary<int, string> GetShutterNames();
    Dictionary<int, string> GetHeaterNames();
    Dictionary<int, string> GetNitrogenSourceNames();

    void RefreshTargets(MbeTableFB fb);

    public int GetMinimalShutterId();
    public int GetMinimalHeaterId();
    public int GetMinimalNitrogenSourceId();
}