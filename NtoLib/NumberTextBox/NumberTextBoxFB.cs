using FB;
using FB.VisualFB;
using InSAT.Library.Interop;
using InSAT.OPC;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NtoLib.NumberTextBox
{
    [Serializable]
    [ComVisible(true)]  
    [Guid("563453A9-A4E3-4EA1-B5CF-FF2DFACCC889")]
    [CatID(CatIDs.CATID_OTHER)]
    [DisplayName("Числовое поле")]
    [VisualControls(typeof(NumberTextBoxControl))]
    public class NumberTextBoxFB : VisualFBBaseExtended
    {
        public const int InputToControl = 1005;
        public const int OutputFromControl = 1010;

        private const int InputId = 5;
        private const int OutputId = 10;

        private int _lastInput;
        private int _lastOutput;



        protected override void UpdateData()
        {
            base.UpdateData();

            int input = GetPinValue<int>(InputId);
            if(input != _lastInput)
            {
                _lastInput = input;
                VisualPins.SetPinValue(InputToControl, input);
            }

            int output = VisualPins.GetPinValue<int>(OutputFromControl);
            if(output != _lastOutput)
            {
                _lastOutput = output;
                SetPinValue(OutputId, output);
            }
        }



        protected override OpcQuality GetConnectionQuality()
        {
            return GetPinQuality(InputId);
        }
    }
}
