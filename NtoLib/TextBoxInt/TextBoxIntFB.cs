using FB;
using FB.VisualFB;
using InSAT.Library.Interop;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NtoLib.TextBoxInt
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

        public const int OutputToControlId = 110;
        public const int InputFromControlId = 150;



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
        }
    }
}
