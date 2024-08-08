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

        private const int MaxValueId = 20;
        private const int MinValueId = 25;

        public const int OutputToControlId = 110;
        public const int InputFromControlId = 150;

        public const int MaxValueToControlId = 120;
        public const int MinValueToControlId = 125;



        protected override void ToRuntime()
        {
            base.ToRuntime();
        }

        protected override void UpdateData()
        {
            base.UpdateData();

            int input = GetPinInt(InputFromScadaId);
            VisualPins.SetPinValue(OutputToControlId, input);

            int output = VisualPins.GetPinInt(InputFromControlId);
            SetPinValue(OutputToScadaId, output);

            int max = GetPinInt(MaxValueId);
            VisualPins.SetPinValue(MaxValueToControlId, max);

            int min = GetPinInt(MinValueId);
            VisualPins.SetPinValue(MinValueToControlId, min);
        }
    }
}
