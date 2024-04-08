using FB;
using FB.VisualFB;
using InSAT.Library.Interop;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NtoLib.Valves
{
    [Serializable]
    [ComVisible(true)]
    [Guid("89DB76A4-6551-40D7-BBD0-64734F158B5B")]
    [CatID(CatIDs.CATID_OTHER)]
    [DisplayName("Шибер или клапан")]
    [VisualControls(typeof(ValveControl))]
    public class ValveFB : VisualFBBase
    {
        public const int ConnectionOk = 1;
        public const int Opened = 2;
        public const int Closed = 3;
        public const int Error = 4;
        public const int BlockOpening = 5;
        public const int BlockClosing = 6;
        public const int AutoMode = 7;
        public const int Collision = 8;
        public const int Opening = 9;
        public const int Closing = 10;

        public const int OpenCMD = 101;
        public const int CloseCMD = 102;
        public const int OpenSlowlyCMD = 103;



        protected override void UpdateData()
        {
            UpdateVisualPout(ConnectionOk);
            UpdateVisualPout(Opened);
            UpdateVisualPout(Closed);
            UpdateVisualPout(Error);
            UpdateVisualPout(BlockOpening);
            UpdateVisualPout(BlockClosing);
            UpdateVisualPout(AutoMode);
            UpdateVisualPout(Collision);
            UpdateVisualPout(Opening);
            UpdateVisualPout(Closing);

            UpdatePout(OpenCMD);
            UpdatePout(CloseCMD);
            UpdatePout(OpenSlowlyCMD);
        }



        private void UpdateVisualPout(int id)
        {
            object value = GetPinValue(id);
            VisualPins.SetPinValue(id + 1000, value);
        }

        private void UpdatePout(int id)
        {
            object value = VisualPins.GetPinValue(id + 1000);
            SetPinValue(id, value);
        }
    }
}
