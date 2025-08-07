using System;
using System.Collections.Generic;
using System.Linq;

namespace NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

public class ActionTargetProvider : IActionTargetProvider
{
    private Dictionary<int, string> _shutterNames = new();
    private Dictionary<int, string> _heaterNames = new();
    private Dictionary<int, string> _nitrogenSourceNames = new();

    public void RefreshTargets(MbeTableFB fb)
    {
        _shutterNames = fb.GetShutterNames();
        _heaterNames = fb.GetHeaterNames();
        _nitrogenSourceNames = fb.GetNitrogenSourceNames();
    }
    
    public Dictionary<int, string> GetShutterNames() => _shutterNames;
    public Dictionary<int, string> GetHeaterNames() => _heaterNames;
    public Dictionary<int, string> GetNitrogenSourceNames() => _nitrogenSourceNames;
    
    public int GetMinimalShutterId()
    {
        if (_shutterNames.Count == 0)
            throw new InvalidOperationException("Shutter names dictionary is empty.");
        
        return _shutterNames.Keys.Min();
    }
    
    public int GetMinimalHeaterId()
    {
        if (_heaterNames.Count == 0)
            throw new InvalidOperationException("Heater names dictionary is empty.");
        
        return _heaterNames.Keys.Min();
    }
    
    public int GetMinimalNitrogenSourceId()
    {
        if (_nitrogenSourceNames.Count == 0)
            throw new InvalidOperationException("Nitrogen source names dictionary is empty.");
        
        return _nitrogenSourceNames.Keys.Min();
    }
}