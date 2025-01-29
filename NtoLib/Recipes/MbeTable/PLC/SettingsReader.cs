using FB;
using FB.VisualFB;
using InSAT.OPC;
using MasterSCADALib;
using System;

namespace NtoLib.Recipes.MbeTable
{
    internal class SettingsReader
    {
        private VisualFBConnector FBConnector;

        public SettingsReader(VisualFBConnector visualFBConnector)
        {
            FBConnector = visualFBConnector;
        }



        public bool CheckQuality()
        {
            for (int id = Params.ID_HMI_CommProtocol; id < Params.ID_HMI_ActualLine; id++)
                if (GetPinQuality(Params.ID_HMI_CommProtocol) != OpcQuality.Good)
                    return false;

            return true;
        }

        public CommunicationSettings ReadTableSettings()
        {
            CommunicationSettings settings = new CommunicationSettings();

            bool settingOk = false;
            uint pinValue1 = GetPinValue<uint>(Params.ID_HMI_CommProtocol);
            if (GetPinQuality(1001) != OpcQuality.Good)
            {
                settingOk = false;
            }
            else
            {
                switch (pinValue1)
                {
                    case 1:
                        settings._protocol = MbeTableFB.ControllerProtocol.Modbus;
                        uint pinValue2 = GetPinValue<uint>(Params.ID_HMI_AddrArea);
                        if (GetPinQuality(Params.ID_HMI_AddrArea) != OpcQuality.Good)
                            break;
                        switch (pinValue2)
                        {
                            case 1:
                                settings._SLMP_Area = MbeTableFB.SLMP_area.D;
                                break;
                            case 2:
                                settings._SLMP_Area = MbeTableFB.SLMP_area.R;
                                break;
                        }

                        if (GetPinQuality(Params.ID_HMI_FloatBaseAddr) != OpcQuality.Good) break;
                        settings._FloatBaseAddr = GetPinValue<uint>(Params.ID_HMI_FloatBaseAddr);

                        if (GetPinQuality(Params.ID_HMI_FloatAreaSize) != OpcQuality.Good) break;
                        settings._FloatAreaSize = GetPinValue<uint>(Params.ID_HMI_FloatAreaSize);

                        if (GetPinQuality(Params.ID_HMI_IntBaseAddr) != OpcQuality.Good) break;
                        settings._IntBaseAddr = GetPinValue<uint>(Params.ID_HMI_IntBaseAddr);

                        if (GetPinQuality(Params.ID_HMI_IntAreaSize) != OpcQuality.Good) break;
                        settings._IntAreaSize = GetPinValue<uint>(Params.ID_HMI_IntAreaSize);

                        if (GetPinQuality(Params.ID_HMI_BoolBaseAddr) != OpcQuality.Good) break;
                        settings._BoolBaseAddr = GetPinValue<uint>(Params.ID_HMI_BoolBaseAddr);

                        if (GetPinQuality(Params.ID_HMI_BoolAreaSize) != OpcQuality.Good) break;
                        settings._BoolAreaSize = GetPinValue<uint>(Params.ID_HMI_BoolAreaSize);

                        if (GetPinQuality(Params.ID_HMI_ControlBaseAddr) != OpcQuality.Good) break;
                        settings._ControlBaseAddr = GetPinValue<uint>(Params.ID_HMI_ControlBaseAddr);

                        if (GetPinQuality(Params.ID_HMI_IP1) != OpcQuality.Good) break;
                        settings._IP1 = GetPinValue<uint>(Params.ID_HMI_IP1);

                        if (GetPinQuality(Params.ID_HMI_IP2) != OpcQuality.Good) break;
                        settings._IP2 = GetPinValue<uint>(Params.ID_HMI_IP2);

                        if (GetPinQuality(Params.ID_HMI_IP3) != OpcQuality.Good) break;
                        settings._IP3 = GetPinValue<uint>(Params.ID_HMI_IP3); ;

                        if (GetPinQuality(Params.ID_HMI_IP4) != OpcQuality.Good) break;
                        settings._IP4 = GetPinValue<uint>(Params.ID_HMI_IP4);

                        if (GetPinQuality(Params.ID_HMI_Port) != OpcQuality.Good) break;
                        settings._Port = GetPinValue<uint>(Params.ID_HMI_Port);

                        if (GetPinQuality(Params.ID_HMI_Timeout) != OpcQuality.Good) break;
                        settings._Timeout = GetPinValue<uint>(Params.ID_HMI_Timeout);

                        settingOk = true;
                        break;
                    case 2:
                        settings._protocol = MbeTableFB.ControllerProtocol.SLMP_not_implimated;
                        break;
                    default:
                        break;

                }

                settings._int_colum_num = 2;
                settings._float_colum_num = 2;
            }
            return settings;
        }

        private T GetPinValue<T>(int id)
        {
            return FBConnector.GetPinValue<T>(id);
        }

        private OpcQuality GetPinQuality(int id)
        {
            return FBConnector.GetPinQuality(id);
        }
    }
}