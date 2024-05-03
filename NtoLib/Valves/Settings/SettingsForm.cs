﻿using System.Windows.Forms;

namespace NtoLib.Valves.Settings
{
    public partial class SettingsForm : Form
    {
        private ValveControl _valveControl;



        public SettingsForm(ValveControl valveCotrol)
        {
            _valveControl = valveCotrol;

            InitializeComponent();
        }



        protected override void OnPaint(PaintEventArgs e)
        {
            Status status = _valveControl.Status;

            openedLamp.Active = status.State == State.Opened;
            closedLamp.Active = status.State == State.Closed;

            blockOpeningLamp.Active = status.BlockOpening;
            blockClosingLamp.Active = status.BlockClosing;


            //lampAuto.Active = status.UsedByAutoMode;
            //lampManual.Active = !status.UsedByAutoMode && !status.NoConnection;
            openedLamp.Active = status.NotOpened;
            closedLamp.Active = status.NotClosed;
            collisionLamp.Active = status.Collision;

            base.OnPaint(e);
        }
    }
}
