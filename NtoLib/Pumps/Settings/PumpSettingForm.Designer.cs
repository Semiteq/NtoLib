namespace NtoLib.Pumps.Settings
{
    partial class PumpSettingForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if(disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label5 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lampAnyError = new NtoLib.Utils.Lamp();
            this.lampManual = new NtoLib.Utils.Lamp();
            this.lampAuto = new NtoLib.Utils.Lamp();
            this.lampConnectionNotOk = new NtoLib.Utils.Lamp();
            this.useLamp = new NtoLib.Utils.Lamp();
            this.lampBlockStop = new NtoLib.Utils.Lamp();
            this.label4 = new System.Windows.Forms.Label();
            this.blockStartLamp = new NtoLib.Utils.Lamp();
            this.SuspendLayout();
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(37, 129);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(150, 20);
            this.label5.TabIndex = 36;
            this.label5.Text = "Нет ответа";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(37, 99);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(150, 20);
            this.label3.TabIndex = 30;
            this.label3.Text = "Блокировка остановки";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(37, 39);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(150, 20);
            this.label1.TabIndex = 25;
            this.label1.Text = "Используется";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(37, 159);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(150, 20);
            this.label2.TabIndex = 43;
            this.label2.Text = "Ошибка";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lampAnyError
            // 
            this.lampAnyError.Active = false;
            this.lampAnyError.ActiveColor = System.Drawing.Color.OrangeRed;
            this.lampAnyError.BackColor = System.Drawing.Color.Transparent;
            this.lampAnyError.Cursor = System.Windows.Forms.Cursors.Default;
            this.lampAnyError.Location = new System.Drawing.Point(9, 159);
            this.lampAnyError.Margin = new System.Windows.Forms.Padding(5);
            this.lampAnyError.Name = "lampAnyError";
            this.lampAnyError.Shape = NtoLib.Utils.Shape.Circle;
            this.lampAnyError.Size = new System.Drawing.Size(20, 20);
            this.lampAnyError.TabIndex = 42;
            this.lampAnyError.TextOnLamp = null;
            // 
            // lampManual
            // 
            this.lampManual.Active = false;
            this.lampManual.ActiveColor = System.Drawing.Color.LimeGreen;
            this.lampManual.BackColor = System.Drawing.Color.Transparent;
            this.lampManual.Cursor = System.Windows.Forms.Cursors.Default;
            this.lampManual.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lampManual.Location = new System.Drawing.Point(99, 9);
            this.lampManual.Margin = new System.Windows.Forms.Padding(5);
            this.lampManual.Name = "lampManual";
            this.lampManual.Shape = NtoLib.Utils.Shape.Square;
            this.lampManual.Size = new System.Drawing.Size(80, 20);
            this.lampManual.TabIndex = 41;
            this.lampManual.TextOnLamp = "Ручной";
            // 
            // lampAuto
            // 
            this.lampAuto.Active = false;
            this.lampAuto.ActiveColor = System.Drawing.Color.LimeGreen;
            this.lampAuto.BackColor = System.Drawing.Color.Transparent;
            this.lampAuto.Cursor = System.Windows.Forms.Cursors.Default;
            this.lampAuto.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lampAuto.Location = new System.Drawing.Point(9, 9);
            this.lampAuto.Margin = new System.Windows.Forms.Padding(0);
            this.lampAuto.Name = "lampAuto";
            this.lampAuto.Shape = NtoLib.Utils.Shape.Square;
            this.lampAuto.Size = new System.Drawing.Size(80, 20);
            this.lampAuto.TabIndex = 40;
            this.lampAuto.TextOnLamp = "Авто";
            // 
            // lampConnectionNotOk
            // 
            this.lampConnectionNotOk.Active = false;
            this.lampConnectionNotOk.ActiveColor = System.Drawing.Color.OrangeRed;
            this.lampConnectionNotOk.BackColor = System.Drawing.Color.Transparent;
            this.lampConnectionNotOk.Cursor = System.Windows.Forms.Cursors.Default;
            this.lampConnectionNotOk.Location = new System.Drawing.Point(9, 129);
            this.lampConnectionNotOk.Margin = new System.Windows.Forms.Padding(5);
            this.lampConnectionNotOk.Name = "lampConnectionNotOk";
            this.lampConnectionNotOk.Shape = NtoLib.Utils.Shape.Circle;
            this.lampConnectionNotOk.Size = new System.Drawing.Size(20, 20);
            this.lampConnectionNotOk.TabIndex = 32;
            this.lampConnectionNotOk.TextOnLamp = null;
            // 
            // useLamp
            // 
            this.useLamp.Active = false;
            this.useLamp.ActiveColor = System.Drawing.Color.LimeGreen;
            this.useLamp.BackColor = System.Drawing.Color.Transparent;
            this.useLamp.Cursor = System.Windows.Forms.Cursors.Default;
            this.useLamp.Location = new System.Drawing.Point(9, 39);
            this.useLamp.Margin = new System.Windows.Forms.Padding(5);
            this.useLamp.Name = "useLamp";
            this.useLamp.Shape = NtoLib.Utils.Shape.Circle;
            this.useLamp.Size = new System.Drawing.Size(20, 20);
            this.useLamp.TabIndex = 24;
            this.useLamp.TextOnLamp = "";
            // 
            // lampBlockStop
            // 
            this.lampBlockStop.Active = false;
            this.lampBlockStop.ActiveColor = System.Drawing.Color.Yellow;
            this.lampBlockStop.BackColor = System.Drawing.Color.Transparent;
            this.lampBlockStop.Cursor = System.Windows.Forms.Cursors.Default;
            this.lampBlockStop.Location = new System.Drawing.Point(9, 99);
            this.lampBlockStop.Margin = new System.Windows.Forms.Padding(5);
            this.lampBlockStop.Name = "lampBlockStop";
            this.lampBlockStop.Shape = NtoLib.Utils.Shape.Circle;
            this.lampBlockStop.Size = new System.Drawing.Size(20, 20);
            this.lampBlockStop.TabIndex = 28;
            this.lampBlockStop.TextOnLamp = null;
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(37, 69);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(150, 20);
            this.label4.TabIndex = 45;
            this.label4.Text = "Блокировка запуска";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // blockStartLamp
            // 
            this.blockStartLamp.Active = false;
            this.blockStartLamp.ActiveColor = System.Drawing.Color.Yellow;
            this.blockStartLamp.BackColor = System.Drawing.Color.Transparent;
            this.blockStartLamp.Cursor = System.Windows.Forms.Cursors.Default;
            this.blockStartLamp.Location = new System.Drawing.Point(9, 69);
            this.blockStartLamp.Margin = new System.Windows.Forms.Padding(5);
            this.blockStartLamp.Name = "blockStartLamp";
            this.blockStartLamp.Shape = NtoLib.Utils.Shape.Circle;
            this.blockStartLamp.Size = new System.Drawing.Size(20, 20);
            this.blockStartLamp.TabIndex = 44;
            this.blockStartLamp.TextOnLamp = "";
            // 
            // PumpSettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(189, 189);
            this.ControlBox = false;
            this.Controls.Add(this.label4);
            this.Controls.Add(this.blockStartLamp);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lampAnyError);
            this.Controls.Add(this.lampManual);
            this.Controls.Add(this.lampAuto);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.lampConnectionNotOk);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.useLamp);
            this.Controls.Add(this.lampBlockStop);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "PumpSettingForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.ResumeLayout(false);

        }

        #endregion

        private Utils.Lamp lampManual;
        private Utils.Lamp lampAuto;
        private System.Windows.Forms.Label label5;
        private Utils.Lamp lampConnectionNotOk;
        private System.Windows.Forms.Label label3;
        private Utils.Lamp lampBlockStop;
        private System.Windows.Forms.Label label1;
        private Utils.Lamp useLamp;
        private System.Windows.Forms.Label label2;
        private Utils.Lamp lampAnyError;
        private System.Windows.Forms.Label label4;
        private Utils.Lamp blockStartLamp;
    }
}