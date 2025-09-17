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
            FloatBaseAddr = (int)_fb.UFloatBaseAddr,
            IntBaseAddr = (int)_fb.UIntBaseAddr,
            
            ControlBaseAddr = (int)_fb.UControlBaseAddr,
            
            FloatAreaSize = (int)_fb.UFloatAreaSize,
            IntAreaSize = (int)_fb.UIntAreaSize,
            
            Ip1 = (int)_fb.UControllerIp1,
            Ip2 = (int)_fb.UControllerIp2,
            Ip3 = (int)_fb.UControllerIp3,
            Ip4 = (int)_fb.UControllerIp4,
            
            Port = (int)_fb.ControllerTcpPort
        };
        
    }
}