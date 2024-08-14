﻿using System;
using System.Windows.Forms;

namespace NtoLib.Utils
{
    public partial class TextBoxInt : TextBox
    {
        public event Action ValidatingValue;



        public TextBoxInt()
        {
            InitializeComponent();
        }



        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            if(!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '-')
                e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if(e.KeyCode == Keys.Enter)
            {
                ValidatingValue?.Invoke();

                e.SuppressKeyPress = true;
                e.Handled = true;
            }
        }
    }
}
