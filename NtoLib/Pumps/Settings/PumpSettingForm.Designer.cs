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
            this.stateLabel = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.speedLabel = new System.Windows.Forms.Label();
            this.temperatureLabel = new System.Windows.Forms.Label();
            this.voltageLabel = new System.Windows.Forms.Label();
            this.currentLabel = new System.Windows.Forms.Label();
            this.powerLabel = new System.Windows.Forms.Label();
            this.temperatureInLabel = new System.Windows.Forms.Label();
            this.temperatureOutLabel = new System.Windows.Forms.Label();
            this.lampAuto = new NtoLib.Utils.Lamp();
            this.lampManual = new NtoLib.Utils.Lamp();
            this.blockStartLamp = new NtoLib.Utils.LabledLamp();
            this.blockStopLamp = new NtoLib.Utils.LabledLamp();
            this.forceStopLamp = new NtoLib.Utils.LabledLamp();
            this.noConnectionLamp = new NtoLib.Utils.LabledLamp();
            this.errorLamp = new NtoLib.Utils.LabledLamp();
            this.safeModeLamp = new NtoLib.Utils.LabledLamp();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // stateLabel
            // 
            this.stateLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.stateLabel.Location = new System.Drawing.Point(8, 10);
            this.stateLabel.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.stateLabel.Name = "stateLabel";
            this.stateLabel.Size = new System.Drawing.Size(175, 20);
            this.stateLabel.TabIndex = 25;
            this.stateLabel.Text = "Состояние: ";
            this.stateLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel1.Controls.Add(this.stateLabel);
            this.flowLayoutPanel1.Controls.Add(this.blockStartLamp);
            this.flowLayoutPanel1.Controls.Add(this.blockStopLamp);
            this.flowLayoutPanel1.Controls.Add(this.forceStopLamp);
            this.flowLayoutPanel1.Controls.Add(this.safeModeLamp);
            this.flowLayoutPanel1.Controls.Add(this.noConnectionLamp);
            this.flowLayoutPanel1.Controls.Add(this.errorLamp);
            this.flowLayoutPanel1.Controls.Add(this.temperatureLabel);
            this.flowLayoutPanel1.Controls.Add(this.speedLabel);
            this.flowLayoutPanel1.Controls.Add(this.voltageLabel);
            this.flowLayoutPanel1.Controls.Add(this.currentLabel);
            this.flowLayoutPanel1.Controls.Add(this.powerLabel);
            this.flowLayoutPanel1.Controls.Add(this.temperatureInLabel);
            this.flowLayoutPanel1.Controls.Add(this.temperatureOutLabel);
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(10, 29);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(5);
            this.flowLayoutPanel1.Size = new System.Drawing.Size(192, 383);
            this.flowLayoutPanel1.TabIndex = 49;
            // 
            // speedLabel
            // 
            this.speedLabel.AutoSize = true;
            this.speedLabel.Location = new System.Drawing.Point(8, 225);
            this.speedLabel.Margin = new System.Windows.Forms.Padding(3);
            this.speedLabel.MaximumSize = new System.Drawing.Size(0, 20);
            this.speedLabel.MinimumSize = new System.Drawing.Size(0, 20);
            this.speedLabel.Name = "speedLabel";
            this.speedLabel.Size = new System.Drawing.Size(58, 20);
            this.speedLabel.TabIndex = 50;
            this.speedLabel.Text = "Скорость:";
            this.speedLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // temperatureLabel
            // 
            this.temperatureLabel.AutoSize = true;
            this.temperatureLabel.Location = new System.Drawing.Point(8, 199);
            this.temperatureLabel.Margin = new System.Windows.Forms.Padding(3);
            this.temperatureLabel.MaximumSize = new System.Drawing.Size(0, 20);
            this.temperatureLabel.MinimumSize = new System.Drawing.Size(0, 20);
            this.temperatureLabel.Name = "temperatureLabel";
            this.temperatureLabel.Size = new System.Drawing.Size(77, 20);
            this.temperatureLabel.TabIndex = 51;
            this.temperatureLabel.Text = "Температура:";
            this.temperatureLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // voltageLabel
            // 
            this.voltageLabel.AutoSize = true;
            this.voltageLabel.Location = new System.Drawing.Point(8, 251);
            this.voltageLabel.Margin = new System.Windows.Forms.Padding(3);
            this.voltageLabel.MaximumSize = new System.Drawing.Size(0, 20);
            this.voltageLabel.MinimumSize = new System.Drawing.Size(0, 20);
            this.voltageLabel.Name = "voltageLabel";
            this.voltageLabel.Size = new System.Drawing.Size(74, 20);
            this.voltageLabel.TabIndex = 52;
            this.voltageLabel.Text = "Напряжение:";
            this.voltageLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // currentLabel
            // 
            this.currentLabel.AutoSize = true;
            this.currentLabel.Location = new System.Drawing.Point(8, 277);
            this.currentLabel.Margin = new System.Windows.Forms.Padding(3);
            this.currentLabel.MaximumSize = new System.Drawing.Size(0, 20);
            this.currentLabel.MinimumSize = new System.Drawing.Size(0, 20);
            this.currentLabel.Name = "currentLabel";
            this.currentLabel.Size = new System.Drawing.Size(29, 20);
            this.currentLabel.TabIndex = 53;
            this.currentLabel.Text = "Ток:";
            this.currentLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // powerLabel
            // 
            this.powerLabel.AutoSize = true;
            this.powerLabel.Location = new System.Drawing.Point(8, 303);
            this.powerLabel.Margin = new System.Windows.Forms.Padding(3);
            this.powerLabel.MaximumSize = new System.Drawing.Size(0, 20);
            this.powerLabel.MinimumSize = new System.Drawing.Size(0, 20);
            this.powerLabel.Name = "powerLabel";
            this.powerLabel.Size = new System.Drawing.Size(63, 20);
            this.powerLabel.TabIndex = 54;
            this.powerLabel.Text = "Мощность:";
            this.powerLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // temperatureInLabel
            // 
            this.temperatureInLabel.AutoSize = true;
            this.temperatureInLabel.Location = new System.Drawing.Point(8, 329);
            this.temperatureInLabel.Margin = new System.Windows.Forms.Padding(3);
            this.temperatureInLabel.MaximumSize = new System.Drawing.Size(0, 20);
            this.temperatureInLabel.MinimumSize = new System.Drawing.Size(0, 20);
            this.temperatureInLabel.Name = "temperatureInLabel";
            this.temperatureInLabel.Size = new System.Drawing.Size(28, 20);
            this.temperatureInLabel.TabIndex = 55;
            this.temperatureInLabel.Text = "Твх:";
            this.temperatureInLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // temperatureOutLabel
            // 
            this.temperatureOutLabel.AutoSize = true;
            this.temperatureOutLabel.Location = new System.Drawing.Point(8, 355);
            this.temperatureOutLabel.Margin = new System.Windows.Forms.Padding(3);
            this.temperatureOutLabel.MaximumSize = new System.Drawing.Size(0, 20);
            this.temperatureOutLabel.MinimumSize = new System.Drawing.Size(0, 20);
            this.temperatureOutLabel.Name = "temperatureOutLabel";
            this.temperatureOutLabel.Size = new System.Drawing.Size(36, 20);
            this.temperatureOutLabel.TabIndex = 56;
            this.temperatureOutLabel.Text = "Твых:";
            this.temperatureOutLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lampAuto
            // 
            this.lampAuto.Active = false;
            this.lampAuto.ActiveColor = System.Drawing.Color.LimeGreen;
            this.lampAuto.BackColor = System.Drawing.Color.Transparent;
            this.lampAuto.Cursor = System.Windows.Forms.Cursors.Default;
            this.lampAuto.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lampAuto.Location = new System.Drawing.Point(10, 10);
            this.lampAuto.Margin = new System.Windows.Forms.Padding(5);
            this.lampAuto.Name = "lampAuto";
            this.lampAuto.Shape = NtoLib.Utils.Shape.Square;
            this.lampAuto.Size = new System.Drawing.Size(90, 20);
            this.lampAuto.TabIndex = 40;
            this.lampAuto.TextOnLamp = "Авто";
            // 
            // lampManual
            // 
            this.lampManual.Active = false;
            this.lampManual.ActiveColor = System.Drawing.Color.LimeGreen;
            this.lampManual.AutoSize = true;
            this.lampManual.BackColor = System.Drawing.Color.Transparent;
            this.lampManual.Cursor = System.Windows.Forms.Cursors.Default;
            this.lampManual.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lampManual.Location = new System.Drawing.Point(112, 10);
            this.lampManual.Margin = new System.Windows.Forms.Padding(5);
            this.lampManual.Name = "lampManual";
            this.lampManual.Shape = NtoLib.Utils.Shape.Square;
            this.lampManual.Size = new System.Drawing.Size(90, 20);
            this.lampManual.TabIndex = 41;
            this.lampManual.TextOnLamp = "Ручной";
            // 
            // blockStartLamp
            // 
            this.blockStartLamp.Active = false;
            this.blockStartLamp.ActiveColor = System.Drawing.Color.Yellow;
            this.blockStartLamp.AutoSize = true;
            this.blockStartLamp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.blockStartLamp.LabelText = "Блокировка запуска";
            this.blockStartLamp.Location = new System.Drawing.Point(8, 38);
            this.blockStartLamp.Name = "blockStartLamp";
            this.blockStartLamp.Size = new System.Drawing.Size(141, 20);
            this.blockStartLamp.TabIndex = 48;
            // 
            // blockStopLamp
            // 
            this.blockStopLamp.Active = false;
            this.blockStopLamp.ActiveColor = System.Drawing.Color.Yellow;
            this.blockStopLamp.AutoSize = true;
            this.blockStopLamp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.blockStopLamp.LabelText = "Блокировка остановки";
            this.blockStopLamp.Location = new System.Drawing.Point(8, 64);
            this.blockStopLamp.Name = "blockStopLamp";
            this.blockStopLamp.Size = new System.Drawing.Size(153, 20);
            this.blockStopLamp.TabIndex = 50;
            // 
            // forceStopLamp
            // 
            this.forceStopLamp.Active = false;
            this.forceStopLamp.ActiveColor = System.Drawing.Color.Yellow;
            this.forceStopLamp.AutoSize = true;
            this.forceStopLamp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.forceStopLamp.LabelText = "Принудительная остановка";
            this.forceStopLamp.Location = new System.Drawing.Point(8, 90);
            this.forceStopLamp.Name = "forceStopLamp";
            this.forceStopLamp.Size = new System.Drawing.Size(176, 20);
            this.forceStopLamp.TabIndex = 50;
            // 
            // noConnectionLamp
            // 
            this.noConnectionLamp.Active = false;
            this.noConnectionLamp.ActiveColor = System.Drawing.Color.OrangeRed;
            this.noConnectionLamp.AutoSize = true;
            this.noConnectionLamp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.noConnectionLamp.LabelText = "Нет связи";
            this.noConnectionLamp.Location = new System.Drawing.Point(8, 142);
            this.noConnectionLamp.Name = "noConnectionLamp";
            this.noConnectionLamp.Size = new System.Drawing.Size(88, 20);
            this.noConnectionLamp.TabIndex = 50;
            // 
            // errorLamp
            // 
            this.errorLamp.Active = false;
            this.errorLamp.ActiveColor = System.Drawing.Color.OrangeRed;
            this.errorLamp.AutoSize = true;
            this.errorLamp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.errorLamp.LabelText = "Ошибка";
            this.errorLamp.Location = new System.Drawing.Point(8, 168);
            this.errorLamp.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
            this.errorLamp.Name = "errorLamp";
            this.errorLamp.Size = new System.Drawing.Size(76, 20);
            this.errorLamp.TabIndex = 50;
            // 
            // safeModeLamp
            // 
            this.safeModeLamp.Active = false;
            this.safeModeLamp.ActiveColor = System.Drawing.Color.Yellow;
            this.safeModeLamp.AutoSize = true;
            this.safeModeLamp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.safeModeLamp.LabelText = "Защитный режим";
            this.safeModeLamp.Location = new System.Drawing.Point(8, 116);
            this.safeModeLamp.Name = "safeModeLamp";
            this.safeModeLamp.Size = new System.Drawing.Size(126, 20);
            this.safeModeLamp.TabIndex = 57;
            // 
            // PumpSettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(321, 443);
            this.ControlBox = false;
            this.Controls.Add(this.lampAuto);
            this.Controls.Add(this.lampManual);
            this.Controls.Add(this.flowLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "PumpSettingForm";
            this.Padding = new System.Windows.Forms.Padding(5);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Utils.Lamp lampManual;
        private Utils.Lamp lampAuto;
        private System.Windows.Forms.Label stateLabel;
        private Utils.LabledLamp blockStartLamp;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private Utils.LabledLamp blockStopLamp;
        private Utils.LabledLamp forceStopLamp;
        private Utils.LabledLamp noConnectionLamp;
        private Utils.LabledLamp errorLamp;
        private System.Windows.Forms.Label temperatureLabel;
        private System.Windows.Forms.Label speedLabel;
        private System.Windows.Forms.Label voltageLabel;
        private System.Windows.Forms.Label currentLabel;
        private System.Windows.Forms.Label powerLabel;
        private System.Windows.Forms.Label temperatureInLabel;
        private System.Windows.Forms.Label temperatureOutLabel;
        private Utils.LabledLamp safeModeLamp;
    }
}