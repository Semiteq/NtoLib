namespace NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

public class CommunicationSettingsProvider : ICommunicationSettingsProvider
{
    private readonly MbeTableFB _fb;

    public CommunicationSettingsProvider(MbeTableFB fb)
    {
        _fb = fb;
    }

    public CommunicationSettings GetSettings()
    {
        return new CommunicationSettings
        {
            FloatBaseAddr = _fb.FloatBaseAddr,
            IntBaseAddr = _fb.IntBaseAddr,
            BoolBaseAddr = _fb.BoolBaseAddr,
            
            ControlBaseAddr = _fb.ControlBaseAddr,
            
            FloatAreaSize = _fb.FloatAreaSize,
            IntAreaSize = _fb.IntAreaSize,
            BoolAreaSize = _fb.BoolAreaSize,
            
            Ip1 = _fb.ControllerIp1,
            Ip2 = _fb.ControllerIp2,
            Ip3 = _fb.ControllerIp3,
            Ip4 = _fb.ControllerIp4,
            
            Port = _fb.ControllerTcpPort
        };
        
    }
}