using FB;
using FB.VisualFB;
using InSAT.OPC;

namespace NtoLib.Recipes.MbeTable
{
    internal class SettingsReader
    {
        private VisualFBConnector connector;

        public SettingsReader(VisualFBConnector visualFBConnector)
        {
            connector = visualFBConnector;
        }



        public bool CheckQuality()
        {
            for (int id = 1001; id < 1016; id++)
                if (((FBBase)connector).GetPinQuality(1001) != OpcQuality.Good)
                    return false;

            return true;
        }

        public CommunicationSettings ReadTableSettings()
        {
            CommunicationSettings settings = new CommunicationSettings();

            bool settingOk = false;
            uint pinValue1 = ((FBBase)connector).GetPinValue<uint>(1001);
            if (((FBBase)connector).GetPinQuality(1001) != OpcQuality.Good)
            {
                settingOk = false;
            }
            else
            {
                switch (pinValue1)
                {
                    case 1:
                        settings._protocol = MbeTableFB.ControllerProtocol.Modbus;
                        uint pinValue2 = ((FBBase)connector).GetPinValue<uint>(1002);
                        if (((FBBase)connector).GetPinQuality(1002) != OpcQuality.Good)
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
                        uint pinValue3 = ((FBBase)connector).GetPinValue<uint>(1003);
                        if (((FBBase)connector).GetPinQuality(1003) != OpcQuality.Good)
                            break;
                        settings._FloatBaseAddr = pinValue3;

                        uint pinValue4 = ((FBBase)connector).GetPinValue<uint>(1004);
                        if (((FBBase)connector).GetPinQuality(1004) != OpcQuality.Good)
                            break;
                        settings._FloatAreaSize = pinValue4;

                        uint pinValue5 = ((FBBase)connector).GetPinValue<uint>(1005);
                        if (((FBBase)connector).GetPinQuality(1005) != OpcQuality.Good)
                            break;
                        settings._IntBaseAddr = pinValue5;

                        uint pinValue6 = ((FBBase)connector).GetPinValue<uint>(1006);
                        if (((FBBase)connector).GetPinQuality(1006) != OpcQuality.Good)
                            break;
                        settings._IntAreaSize = pinValue6;

                        uint pinValue7 = ((FBBase)connector).GetPinValue<uint>(1007);
                        if (((FBBase)connector).GetPinQuality(1007) != OpcQuality.Good)
                            break;
                        settings._BoolBaseAddr = pinValue7;

                        uint pinValue8 = ((FBBase)connector).GetPinValue<uint>(1008);
                        if (((FBBase)connector).GetPinQuality(1008) != OpcQuality.Good)
                            break;
                        settings._BoolAreaSize = pinValue8;

                        uint pinValue9 = ((FBBase)connector).GetPinValue<uint>(1009);
                        if (((FBBase)connector).GetPinQuality(1009) != OpcQuality.Good)
                            break;
                        settings._ControlBaseAddr = pinValue9;

                        uint pinValue10 = ((FBBase)connector).GetPinValue<uint>(1010);
                        if (((FBBase)connector).GetPinQuality(1010) != OpcQuality.Good)
                            break;
                        settings._IP1 = pinValue10;

                        uint pinValue11 = ((FBBase)connector).GetPinValue<uint>(1011);
                        if (((FBBase)connector).GetPinQuality(1011) != OpcQuality.Good)
                            break;
                        settings._IP2 = pinValue11;

                        uint pinValue12 = ((FBBase)connector).GetPinValue<uint>(1012);
                        if (((FBBase)connector).GetPinQuality(1012) != OpcQuality.Good)
                            break;
                        settings._IP3 = pinValue12;

                        uint pinValue13 = ((FBBase)connector).GetPinValue<uint>(1013);
                        if (((FBBase)connector).GetPinQuality(1013) != OpcQuality.Good)
                            break;
                        settings._IP4 = pinValue13;

                        uint pinValue14 = ((FBBase)connector).GetPinValue<uint>(1014);
                        if (((FBBase)connector).GetPinQuality(1014) != OpcQuality.Good)
                            break;
                        settings._Port = pinValue14;

                        uint pinValue15 = ((FBBase)connector).GetPinValue<uint>(1015);
                        if (((FBBase)connector).GetPinQuality(1015) != OpcQuality.Good)
                            break;
                        settings._Timeout = pinValue15;

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
    }
}
