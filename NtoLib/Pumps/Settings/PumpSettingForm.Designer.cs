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
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.stateLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.forceStopLamp = new NtoLib.Utils.Lamp();
            this.blockStartLamp = new NtoLib.Utils.Lamp();
            this.lampAnyError = new NtoLib.Utils.Lamp();
            this.lampManual = new NtoLib.Utils.Lamp();
            this.lampAuto = new NtoLib.Utils.Lamp();
            this.lampConnectionNotOk = new NtoLib.Utils.Lamp();
            this.lampBlockStop = new NtoLib.Utils.Lamp();
            this.SuspendLayout();
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(40, 168);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(150, 20);
            this.label5.TabIndex = 36;
            this.label5.Text = "Нет связи";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(40, 108);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(150, 20);
            this.label3.TabIndex = 30;
            this.label3.Text = "Блокировка остановки";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(40, 198);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(150, 20);
            this.label2.TabIndex = 43;
            this.label2.Text = "Ошибка";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(40, 78);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(150, 20);
            this.label4.TabIndex = 45;
            this.label4.Text = "Блокировка запуска";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // stateLabel
            // 
            this.stateLabel.Location = new System.Drawing.Point(15, 45);
            this.stateLabel.Name = "stateLabel";
            this.stateLabel.Size = new System.Drawing.Size(175, 20);
            this.stateLabel.TabIndex = 25;
            this.stateLabel.Text = "Состояние: ";
            this.stateLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(40, 138);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(150, 20);
            this.label1.TabIndex = 47;
            this.label1.Text = "Принудительная остановка";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // forceStopLamp
            // 
            this.forceStopLamp.Active = false;
            this.forceStopLamp.ActiveColor = System.Drawing.Color.Yellow;
            this.forceStopLamp.BackColor = System.Drawing.Color.Transparent;
            this.forceStopLamp.Cursor = System.Windows.Forms.Cursors.Default;
            this.forceStopLamp.Location = new System.Drawing.Point(12, 138);
            this.forceStopLamp.Margin = new System.Windows.Forms.Padding(5);
            this.forceStopLamp.Name = "forceStopLamp";
            this.forceStopLamp.Shape = NtoLib.Utils.Shape.Circle;
            this.forceStopLamp.Size = new System.Drawing.Size(20, 20);
            this.forceStopLamp.TabIndex = 46;
            this.forceStopLamp.TextOnLamp = null;
            // 
            // blockStartLamp
            // 
            this.blockStartLamp.Active = false;
            this.blockStartLamp.ActiveColor = System.Drawing.Color.Yellow;
            this.blockStartLamp.BackColor = System.Drawing.Color.Transparent;
            this.blockStartLamp.Cursor = System.Windows.Forms.Cursors.Default;
            this.blockStartLamp.Location = new System.Drawing.Point(12, 78);
            this.blockStartLamp.Margin = new System.Windows.Forms.Padding(5);
            this.blockStartLamp.Name = "blockStartLamp";
            this.blockStartLamp.Shape = NtoLib.Utils.Shape.Circle;
            this.blockStartLamp.Size = new System.Drawing.Size(20, 20);
            this.blockStartLamp.TabIndex = 44;
            this.blockStartLamp.TextOnLamp = "";
            // 
            // lampAnyError
            // 
            this.lampAnyError.Active = false;
            this.lampAnyError.ActiveColor = System.Drawing.Color.OrangeRed;
            this.lampAnyError.BackColor = System.Drawing.Color.Transparent;
            this.lampAnyError.Cursor = System.Windows.Forms.Cursors.Default;
            this.lampAnyError.Location = new System.Drawing.Point(12, 198);
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
            this.lampManual.AutoSize = true;
            this.lampManual.BackColor = System.Drawing.Color.Transparent;
            this.lampManual.Cursor = System.Windows.Forms.Cursors.Default;
            this.lampManual.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lampManual.Location = new System.Drawing.Point(110, 15);
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
            this.lampAuto.Location = new System.Drawing.Point(15, 15);
            this.lampAuto.Margin = new System.Windows.Forms.Padding(10);
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
            this.lampConnectionNotOk.Location = new System.Drawing.Point(12, 168);
            this.lampConnectionNotOk.Margin = new System.Windows.Forms.Padding(5);
            this.lampConnectionNotOk.Name = "lampConnectionNotOk";
            this.lampConnectionNotOk.Shape = NtoLib.Utils.Shape.Circle;
            this.lampConnectionNotOk.Size = new System.Drawing.Size(20, 20);
            this.lampConnectionNotOk.TabIndex = 32;
            this.lampConnectionNotOk.TextOnLamp = null;
            // 
            // lampBlockStop
            // 
            this.lampBlockStop.Active = false;
            this.lampBlockStop.ActiveColor = System.Drawing.Color.Yellow;
            this.lampBlockStop.BackColor = System.Drawing.Color.Transparent;
            this.lampBlockStop.Cursor = System.Windows.Forms.Cursors.Default;
            this.lampBlockStop.Location = new System.Drawing.Point(12, 108);
            this.lampBlockStop.Margin = new System.Windows.Forms.Padding(5);
            this.lampBlockStop.Name = "lampBlockStop";
            this.lampBlockStop.Shape = NtoLib.Utils.Shape.Circle;
            this.lampBlockStop.Size = new System.Drawing.Size(20, 20);
            this.lampBlockStop.TabIndex = 28;
            this.lampBlockStop.TextOnLamp = null;
            // 
            // PumpSettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(205, 231);
            this.ControlBox = false;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.forceStopLamp);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.blockStartLamp);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lampAnyError);
            this.Controls.Add(this.lampManual);
            this.Controls.Add(this.lampAuto);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.lampConnectionNotOk);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.stateLabel);
            this.Controls.Add(this.lampBlockStop);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "PumpSettingForm";
            this.Padding = new System.Windows.Forms.Padding(5);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Utils.Lamp lampManual;
        private Utils.Lamp lampAuto;
        private System.Windows.Forms.Label label5;
        private Utils.Lamp lampConnectionNotOk;
        private System.Windows.Forms.Label label3;
        private Utils.Lamp lampBlockStop;
        private System.Windows.Forms.Label label2;
        private Utils.Lamp lampAnyError;
        private System.Windows.Forms.Label label4;
        private Utils.Lamp blockStartLamp;
        private System.Windows.Forms.Label stateLabel;
        private System.Windows.Forms.Label label1;
        private Utils.Lamp forceStopLamp;
    }
}