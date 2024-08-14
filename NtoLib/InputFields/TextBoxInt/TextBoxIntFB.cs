using FB;
using FB.VisualFB;
using InSAT.Library.Interop;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NtoLib.InputFields.TextBoxInt
{
    [Serializable]
    [ComVisible(true)]
    [Guid("FF21CFEA-B957-43E8-B035-09BAE4CB6DB1")]
    [CatID(CatIDs.CATID_OTHER)]
    [DisplayName("Целочисленное поле")]
    [VisualControls(typeof(TextBoxIntControl))]
    public class TextBoxIntFB : VisualFBBase
    {
        private const int InputFromScadaId = 10;
        private const int OutputToScadaId = 50;

        private const int LockFromScadaId = 15;

        private const int MaxValueId = 20;
        private const int MinValueId = 25;

        public const int OutputToControlId = 110;
        public const int InputFromControlId = 150;

        public const int LockToControl = 115;

        public const int MaxValueToControlId = 120;
        public const int MinValueToControlId = 125;

        private int _lastInput;
        private int _lastOutput;



        protected override void ToRuntime()
        {
            base.ToRuntime();

            int input = GetPinValue<int>(InputFromScadaId); 
            VisualPins.SetPinValue(OutputToControlId, input);

            SetPinValue(OutputToScadaId, input);
        }

        protected override void UpdateData()
        {
            base.UpdateData();

            int input = GetPinValue<int>(InputFromScadaId);
            bool inputChanged = false;
            if(input != _lastInput)
            {
                _lastInput = input;
                inputChanged = true;

                VisualPins.SetPinValue(OutputToControlId, input);
            }

            int output = VisualPins.GetPinValue<int>(InputFromControlId);
            bool outputChanged = false;
            if(output != _lastOutput)
            {
                _lastOutput = output;
                outputChanged = true;
            }

            if(inputChanged)
                SetPinValue(OutputToScadaId, input);
            else if(outputChanged)
                SetPinValue(OutputToScadaId, output);

            bool locked = GetPinValue<bool>(LockFromScadaId);
            VisualPins.SetPinValue(LockToControl, locked);

            int max = GetPinValue<int>(MaxValueId);
            VisualPins.SetPinValue(MaxValueToControlId, max);

            int min = GetPinValue<int>(MinValueId);
            VisualPins.SetPinValue(MinValueToControlId, min);
        }
    }
}
