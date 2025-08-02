using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.PinDataManager;

public class ActionTargetProvider : IActionTargetProvider
{
    private IReadOnlyDictionary<int, string> _shutterNames = new Dictionary<int, string>();
    private IReadOnlyDictionary<int, string> _heaterNames = new Dictionary<int, string>();
    private IReadOnlyDictionary<int, string> _nitrogenSourceNames = new Dictionary<int, string>();

    public void RefreshTargets(MbeTableFB fb)
    {
        _shutterNames = fb.GetShutterNames();
        _heaterNames = fb.GetHeaterNames();
        _nitrogenSourceNames = fb.GetNitrogenSourceNames();
    }
    
    public IReadOnlyDictionary<int, string> GetShutterNames() => _shutterNames;
    public IReadOnlyDictionary<int, string> GetHeaterNames() => _heaterNames;
    public IReadOnlyDictionary<int, string> GetNitrogenSourceNames() => _nitrogenSourceNames;
    
}