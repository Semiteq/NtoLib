using System;
using System.Reflection;
using FB.VisualFB;
using InSAT.OPC;

namespace NtoLib.Recipes.MbeTable.PLC
{
    internal class SettingsReader
    {
        private VisualFBConnector _fbConnector;

        public SettingsReader(VisualFBConnector visualFbConnector)
        {
            _fbConnector = visualFbConnector;
        }

        public void Initialize(VisualFBConnector visualFBConnector)
        {
            _fbConnector = visualFBConnector;
        }

        public bool CheckQuality()
        {
            for (int id = Params.IdHmiCommProtocol; id < Params.IdHmiActualLine; id++)
                if (GetPinQuality(id) != OpcQuality.Good)
                    return false;
            return true;
        }

        public CommunicationSettings ReadTableSettings()
        {
            var settings = new CommunicationSettings();

            if (!IsQualityGood(Params.IdHmiCommProtocol))
                return settings;

            var protocol = GetPinValue<uint>(Params.IdHmiCommProtocol);
            if (protocol != 1)
            {
                settings.Protocol = protocol == 2
                    ? MbeTableFB.ControllerProtocol.SlmpNotImplimated
                    : settings.Protocol;
                return settings;
            }

            settings.Protocol = MbeTableFB.ControllerProtocol.Modbus;
            if (!TrySetSlmpArea(settings))
                return settings;

            var settingsMap = new[]
            {
                (Params.IdHmiFloatBaseAddr,      (Action<uint>)(v => settings.FloatBaseAddr = v)),
                (Params.IdHmiFloatAreaSize,      (Action<uint>)(v => settings.FloatAreaSize = v)),
                (Params.IdHmiIntBaseAddr,        (Action<uint>)(v => settings.IntBaseAddr = v)),
                (Params.IdHmiIntAreaSize,        (Action<uint>)(v => settings.IntAreaSize = v)),
                (Params.IdHmiBoolBaseAddr,       (Action<uint>)(v => settings.BoolBaseAddr = v)),
                (Params.IdHmiBoolAreaSize,       (Action<uint>)(v => settings.BoolAreaSize = v)),
                (Params.IdHmiControlBaseAddr,    (Action<uint>)(v => settings.ControlBaseAddr = v)),
                (Params.IdHmiIp1,                (Action<uint>)(v => settings.Ip1 = v)),
                (Params.IdHmiIp2,                (Action<uint>)(v => settings.Ip2 = v)),
                (Params.IdHmiIp3,                (Action<uint>)(v => settings.Ip3 = v)),
                (Params.IdHmiIp4,                (Action<uint>)(v => settings.Ip4 = v)),
                (Params.IdHmiPort,               (Action<uint>)(v => settings.Port = v)),
                (Params.IdHmiTimeout,            (Action<uint>)(v => settings.Timeout = v))
            };

            foreach (var (id, setter) in settingsMap)
            {
                if (!IsQualityGood(id))
                    return settings;
                setter(GetPinValue<uint>(id));
            }

            return settings;
        }

        private bool TrySetSlmpArea(CommunicationSettings settings)
        {
            if (!IsQualityGood(Params.IdHmiAddrArea))
                return false;

            var areaValue = GetPinValue<uint>(Params.IdHmiAddrArea);
            settings.SlmpArea = areaValue switch
            {
                1 => MbeTableFB.SlmpArea.D,
                2 => MbeTableFB.SlmpArea.R,
                _ => settings.SlmpArea
            };
            return true;
        }

        private bool IsQualityGood(int id) => GetPinQuality(id) == OpcQuality.Good;

        private T GetPinValue<T>(int id) => _fbConnector.GetPinValue<T>(id);

        private OpcQuality GetPinQuality(int id) => _fbConnector.GetPinQuality(id);
    }
}