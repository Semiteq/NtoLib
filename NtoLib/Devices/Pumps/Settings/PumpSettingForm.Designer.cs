using NtoLib.Devices.Render.Common;

namespace NtoLib.Devices.Pumps.Settings
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
            this.blockStartLamp = new NtoLib.Devices.Render.Common.LabeledLamp();
            this.blockStopLamp = new NtoLib.Devices.Render.Common.LabeledLamp();
            this.forceStopLamp = new NtoLib.Devices.Render.Common.LabeledLamp();
            this.safeModeLamp = new NtoLib.Devices.Render.Common.LabeledLamp();
            this.noConnectionLamp = new NtoLib.Devices.Render.Common.LabeledLamp();
            this.errorLamp = new NtoLib.Devices.Render.Common.LabeledLamp();
            this.warningLamp = new NtoLib.Devices.Render.Common.LabeledLamp();
            this.temperatureLabel = new NtoLib.Devices.Render.Common.LabeledValue();
            this.speedLabel = new NtoLib.Devices.Render.Common.LabeledValue();
            this.voltageLabel = new NtoLib.Devices.Render.Common.LabeledValue();
            this.currentLabel = new NtoLib.Devices.Render.Common.LabeledValue();
            this.temperatureInLabel = new NtoLib.Devices.Render.Common.LabeledValue();
            this.temperatureOutLabel = new NtoLib.Devices.Render.Common.LabeledValue();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // stateLabel
            // 
            this.stateLabel.Font = new System.Drawing.Font("Arial", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.stateLabel.Location = new System.Drawing.Point(8, 5);
            this.stateLabel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 10);
            this.stateLabel.Name = "stateLabel";
            this.stateLabel.Size = new System.Drawing.Size(367, 25);
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
            this.flowLayoutPanel1.Controls.Add(this.warningLamp);
            this.flowLayoutPanel1.Controls.Add(this.temperatureLabel);
            this.flowLayoutPanel1.Controls.Add(this.speedLabel);
            this.flowLayoutPanel1.Controls.Add(this.voltageLabel);
            this.flowLayoutPanel1.Controls.Add(this.currentLabel);
            this.flowLayoutPanel1.Controls.Add(this.temperatureInLabel);
            this.flowLayoutPanel1.Controls.Add(this.temperatureOutLabel);
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.flowLayoutPanel1.Location = new System.Drawing.Point(8, 8);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(5);
            this.flowLayoutPanel1.Size = new System.Drawing.Size(384, 498);
            this.flowLayoutPanel1.TabIndex = 49;
            // 
            // blockStartLamp
            // 
            this.blockStartLamp.Active = false;
            this.blockStartLamp.ActiveColor = System.Drawing.Color.Yellow;
            this.blockStartLamp.AutoSize = true;
            this.blockStartLamp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.blockStartLamp.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.blockStartLamp.LabelText = "Блокировка запуска";
            this.blockStartLamp.Location = new System.Drawing.Point(10, 45);
            this.blockStartLamp.Margin = new System.Windows.Forms.Padding(5);
            this.blockStartLamp.Name = "blockStartLamp";
            this.blockStartLamp.Size = new System.Drawing.Size(205, 25);
            this.blockStartLamp.TabIndex = 48;
            // 
            // blockStopLamp
            // 
            this.blockStopLamp.Active = false;
            this.blockStopLamp.ActiveColor = System.Drawing.Color.Yellow;
            this.blockStopLamp.AutoSize = true;
            this.blockStopLamp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.blockStopLamp.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.blockStopLamp.LabelText = "Блокировка остановки";
            this.blockStopLamp.Location = new System.Drawing.Point(10, 80);
            this.blockStopLamp.Margin = new System.Windows.Forms.Padding(5);
            this.blockStopLamp.Name = "blockStopLamp";
            this.blockStopLamp.Size = new System.Drawing.Size(227, 25);
            this.blockStopLamp.TabIndex = 50;
            // 
            // forceStopLamp
            // 
            this.forceStopLamp.Active = false;
            this.forceStopLamp.ActiveColor = System.Drawing.Color.Yellow;
            this.forceStopLamp.AutoSize = true;
            this.forceStopLamp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.forceStopLamp.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.forceStopLamp.LabelText = "Принудительная остановка";
            this.forceStopLamp.Location = new System.Drawing.Point(10, 115);
            this.forceStopLamp.Margin = new System.Windows.Forms.Padding(5);
            this.forceStopLamp.Name = "forceStopLamp";
            this.forceStopLamp.Size = new System.Drawing.Size(265, 25);
            this.forceStopLamp.TabIndex = 50;
            // 
            // safeModeLamp
            // 
            this.safeModeLamp.Active = false;
            this.safeModeLamp.ActiveColor = System.Drawing.Color.Yellow;
            this.safeModeLamp.AutoSize = true;
            this.safeModeLamp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.safeModeLamp.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.safeModeLamp.LabelText = "Защитный режим";
            this.safeModeLamp.Location = new System.Drawing.Point(10, 150);
            this.safeModeLamp.Margin = new System.Windows.Forms.Padding(5);
            this.safeModeLamp.Name = "safeModeLamp";
            this.safeModeLamp.Size = new System.Drawing.Size(183, 25);
            this.safeModeLamp.TabIndex = 57;
            // 
            // noConnectionLamp
            // 
            this.noConnectionLamp.Active = false;
            this.noConnectionLamp.ActiveColor = System.Drawing.Color.OrangeRed;
            this.noConnectionLamp.AutoSize = true;
            this.noConnectionLamp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.noConnectionLamp.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.noConnectionLamp.LabelText = "Нет связи";
            this.noConnectionLamp.Location = new System.Drawing.Point(10, 185);
            this.noConnectionLamp.Margin = new System.Windows.Forms.Padding(5);
            this.noConnectionLamp.Name = "noConnectionLamp";
            this.noConnectionLamp.Size = new System.Drawing.Size(121, 25);
            this.noConnectionLamp.TabIndex = 50;
            // 
            // errorLamp
            // 
            this.errorLamp.Active = false;
            this.errorLamp.ActiveColor = System.Drawing.Color.OrangeRed;
            this.errorLamp.AutoSize = true;
            this.errorLamp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.errorLamp.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.errorLamp.LabelText = "Ошибка";
            this.errorLamp.Location = new System.Drawing.Point(10, 220);
            this.errorLamp.Margin = new System.Windows.Forms.Padding(5);
            this.errorLamp.Name = "errorLamp";
            this.errorLamp.Size = new System.Drawing.Size(105, 25);
            this.errorLamp.TabIndex = 50;
            // 
            // warningLamp
            // 
            this.warningLamp.Active = false;
            this.warningLamp.ActiveColor = System.Drawing.Color.Yellow;
            this.warningLamp.AutoSize = true;
            this.warningLamp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.warningLamp.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.warningLamp.LabelText = "Предупреждение";
            this.warningLamp.Location = new System.Drawing.Point(10, 255);
            this.warningLamp.Margin = new System.Windows.Forms.Padding(5, 5, 5, 15);
            this.warningLamp.Name = "warningLamp";
            this.warningLamp.Size = new System.Drawing.Size(181, 25);
            this.warningLamp.TabIndex = 64;
            // 
            // temperatureLabel
            // 
            this.temperatureLabel.AutoSize = true;
            this.temperatureLabel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.temperatureLabel.LabelText = "Температура:";
            this.temperatureLabel.Location = new System.Drawing.Point(9, 299);
            this.temperatureLabel.Margin = new System.Windows.Forms.Padding(4);
            this.temperatureLabel.Name = "temperatureLabel";
            this.temperatureLabel.Size = new System.Drawing.Size(366, 25);
            this.temperatureLabel.TabIndex = 50;
            this.temperatureLabel.ValueText = "C°";
            // 
            // speedLabel
            // 
            this.speedLabel.AutoSize = true;
            this.speedLabel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.speedLabel.LabelText = "Скорость:";
            this.speedLabel.Location = new System.Drawing.Point(9, 332);
            this.speedLabel.Margin = new System.Windows.Forms.Padding(4);
            this.speedLabel.Name = "speedLabel";
            this.speedLabel.Size = new System.Drawing.Size(366, 25);
            this.speedLabel.TabIndex = 58;
            this.speedLabel.ValueText = "%";
            // 
            // voltageLabel
            // 
            this.voltageLabel.AutoSize = true;
            this.voltageLabel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.voltageLabel.LabelText = "Напряжение:";
            this.voltageLabel.Location = new System.Drawing.Point(9, 365);
            this.voltageLabel.Margin = new System.Windows.Forms.Padding(4);
            this.voltageLabel.Name = "voltageLabel";
            this.voltageLabel.Size = new System.Drawing.Size(366, 25);
            this.voltageLabel.TabIndex = 59;
            this.voltageLabel.ValueText = "В";
            // 
            // currentLabel
            // 
            this.currentLabel.AutoSize = true;
            this.currentLabel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.currentLabel.LabelText = "Ток:";
            this.currentLabel.Location = new System.Drawing.Point(9, 398);
            this.currentLabel.Margin = new System.Windows.Forms.Padding(4);
            this.currentLabel.Name = "currentLabel";
            this.currentLabel.Size = new System.Drawing.Size(366, 25);
            this.currentLabel.TabIndex = 60;
            this.currentLabel.ValueText = "А";
            // 
            // temperatureInLabel
            // 
            this.temperatureInLabel.AutoSize = true;
            this.temperatureInLabel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.temperatureInLabel.LabelText = "Т1:";
            this.temperatureInLabel.Location = new System.Drawing.Point(9, 431);
            this.temperatureInLabel.Margin = new System.Windows.Forms.Padding(4);
            this.temperatureInLabel.Name = "temperatureInLabel";
            this.temperatureInLabel.Size = new System.Drawing.Size(366, 25);
            this.temperatureInLabel.TabIndex = 62;
            this.temperatureInLabel.ValueText = "К";
            // 
            // temperatureOutLabel
            // 
            this.temperatureOutLabel.AutoSize = true;
            this.temperatureOutLabel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.temperatureOutLabel.LabelText = "Т2:";
            this.temperatureOutLabel.Location = new System.Drawing.Point(9, 464);
            this.temperatureOutLabel.Margin = new System.Windows.Forms.Padding(4);
            this.temperatureOutLabel.Name = "temperatureOutLabel";
            this.temperatureOutLabel.Size = new System.Drawing.Size(366, 25);
            this.temperatureOutLabel.TabIndex = 63;
            this.temperatureOutLabel.ValueText = "К";
            // 
            // PumpSettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(398, 546);
            this.ControlBox = false;
            this.Controls.Add(this.flowLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "PumpSettingForm";
            this.Padding = new System.Windows.Forms.Padding(5, 5, 5, 0);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label stateLabel;
        private LabeledLamp blockStartLamp;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private LabeledLamp blockStopLamp;
        private LabeledLamp forceStopLamp;
        private LabeledLamp noConnectionLamp;
        private LabeledLamp errorLamp;
        private LabeledLamp safeModeLamp;
        private LabeledValue temperatureLabel;
        private LabeledValue speedLabel;
        private LabeledValue voltageLabel;
        private LabeledValue currentLabel;
        private LabeledValue temperatureInLabel;
        private LabeledValue temperatureOutLabel;
        private LabeledLamp warningLamp;
    }
}