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
            FloatBaseAddr = _fb.UFloatBaseAddr,
            IntBaseAddr = _fb.UIntBaseAddr,
            BoolBaseAddr = _fb.UBoolBaseAddr,
            
            ControlBaseAddr = _fb.UControlBaseAddr,
            
            FloatAreaSize = _fb.UFloatAreaSize,
            IntAreaSize = _fb.UIntAreaSize,
            BoolAreaSize = _fb.UBoolAreaSize,
            
            Ip1 = _fb.ControllerIp1,
            Ip2 = _fb.ControllerIp2,
            Ip3 = _fb.ControllerIp3,
            Ip4 = _fb.ControllerIp4,
            
            Port = _fb.ControllerTcpPort,
        };
        
    }
}