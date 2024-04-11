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
        public const int SmoothlyOpened = 3;
        public const int Closed = 4;
        public const int Error = 5;
        public const int BlockOpening = 6;
        public const int BlockClosing = 7;
        public const int AutoMode = 8;
        public const int Collision = 9;
        public const int Opening = 10;
        public const int Closing = 11;

        public const int IsSmoothValve = 25;

        public const int OpenCMD = 101;
        public const int CloseCMD = 102;
        public const int OpenSmoothlyCMD = 103;



        protected override void UpdateData()
        {
            RetransmitPin(ConnectionOk);
            RetransmitPin(Opened);
            RetransmitPin(SmoothlyOpened);
            RetransmitPin(Closed);
            RetransmitPin(Error);
            RetransmitPin(BlockOpening);
            RetransmitPin(BlockClosing);
            RetransmitPin(AutoMode);
            RetransmitPin(Collision);
            RetransmitPin(Opening);
            RetransmitPin(Closing);
            RetransmitPin(IsSmoothValve);

            RetransmitVisualPin(OpenCMD);
            RetransmitVisualPin(CloseCMD);
            RetransmitVisualPin(OpenSmoothlyCMD);
        }



        /// <summary>
        /// Пересылает информацию с внешнего входа на соответствующий выход для UI
        /// и VisualPin
        /// </summary>
        /// <param name="id"></param>
        private void RetransmitPin(int id)
        {
            object value = GetPinValue(id);

            SetPinValue(id + 200, value);
            VisualPins.SetPinValue(id + 1000, value);
        }

        /// <summary>
        /// Пересылает информацию с входа с контрола на внешние выходы
        /// </summary>
        /// <param name="id"></param>
        private void RetransmitVisualPin(int id)
        {
            object value = VisualPins.GetPinValue(id + 1000);
            SetPinValue(id, value);
        }
    }
}
