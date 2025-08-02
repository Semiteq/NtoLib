using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.PinDataManager;

public interface IActionTargetProvider
{
    IReadOnlyDictionary<int, string> GetShutterNames();
    IReadOnlyDictionary<int, string> GetHeaterNames();
    IReadOnlyDictionary<int, string> GetNitrogenSourceNames();

    void RefreshTargets(MbeTableFB fb);
}