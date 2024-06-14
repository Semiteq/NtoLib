namespace NtoLib.Valves.Settings
{
    partial class SettingsForm
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
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.openedLamp = new NtoLib.Utils.LabledLamp();
            this.closedLamp = new NtoLib.Utils.LabledLamp();
            this.blockOpeningLamp = new NtoLib.Utils.LabledLamp();
            this.blockClosingLamp = new NtoLib.Utils.LabledLamp();
            this.forceCloseLamp = new NtoLib.Utils.LabledLamp();
            this.labledLamp2 = new NtoLib.Utils.LabledLamp();
            this.notOpenedLamp = new NtoLib.Utils.LabledLamp();
            this.notClosedLamp = new NtoLib.Utils.LabledLamp();
            this.collisionLamp = new NtoLib.Utils.LabledLamp();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel1.Controls.Add(this.openedLamp);
            this.flowLayoutPanel1.Controls.Add(this.closedLamp);
            this.flowLayoutPanel1.Controls.Add(this.blockOpeningLamp);
            this.flowLayoutPanel1.Controls.Add(this.blockClosingLamp);
            this.flowLayoutPanel1.Controls.Add(this.forceCloseLamp);
            this.flowLayoutPanel1.Controls.Add(this.labledLamp2);
            this.flowLayoutPanel1.Controls.Add(this.notOpenedLamp);
            this.flowLayoutPanel1.Controls.Add(this.notClosedLamp);
            this.flowLayoutPanel1.Controls.Add(this.collisionLamp);
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(8, 8);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(5);
            this.flowLayoutPanel1.Size = new System.Drawing.Size(279, 325);
            this.flowLayoutPanel1.TabIndex = 50;
            // 
            // openedLamp
            // 
            this.openedLamp.Active = false;
            this.openedLamp.ActiveColor = System.Drawing.Color.LimeGreen;
            this.openedLamp.AutoSize = true;
            this.openedLamp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.openedLamp.LabelText = "Открыт";
            this.openedLamp.Location = new System.Drawing.Point(10, 10);
            this.openedLamp.Margin = new System.Windows.Forms.Padding(5);
            this.openedLamp.Name = "openedLamp";
            this.openedLamp.Size = new System.Drawing.Size(102, 25);
            this.openedLamp.TabIndex = 51;
            // 
            // closedLamp
            // 
            this.closedLamp.Active = false;
            this.closedLamp.ActiveColor = System.Drawing.Color.LimeGreen;
            this.closedLamp.AutoSize = true;
            this.closedLamp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.closedLamp.LabelText = "Закрыт";
            this.closedLamp.Location = new System.Drawing.Point(10, 45);
            this.closedLamp.Margin = new System.Windows.Forms.Padding(5);
            this.closedLamp.Name = "closedLamp";
            this.closedLamp.Size = new System.Drawing.Size(101, 25);
            this.closedLamp.TabIndex = 52;
            // 
            // blockOpeningLamp
            // 
            this.blockOpeningLamp.Active = false;
            this.blockOpeningLamp.ActiveColor = System.Drawing.Color.Yellow;
            this.blockOpeningLamp.AutoSize = true;
            this.blockOpeningLamp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.blockOpeningLamp.LabelText = "Блокировка открытия";
            this.blockOpeningLamp.Location = new System.Drawing.Point(10, 80);
            this.blockOpeningLamp.Margin = new System.Windows.Forms.Padding(5);
            this.blockOpeningLamp.Name = "blockOpeningLamp";
            this.blockOpeningLamp.Size = new System.Drawing.Size(220, 25);
            this.blockOpeningLamp.TabIndex = 53;
            // 
            // blockClosingLamp
            // 
            this.blockClosingLamp.Active = false;
            this.blockClosingLamp.ActiveColor = System.Drawing.Color.Yellow;
            this.blockClosingLamp.AutoSize = true;
            this.blockClosingLamp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.blockClosingLamp.LabelText = "Блокировка закрытия";
            this.blockClosingLamp.Location = new System.Drawing.Point(10, 115);
            this.blockClosingLamp.Margin = new System.Windows.Forms.Padding(5);
            this.blockClosingLamp.Name = "blockClosingLamp";
            this.blockClosingLamp.Size = new System.Drawing.Size(219, 25);
            this.blockClosingLamp.TabIndex = 54;
            // 
            // forceCloseLamp
            // 
            this.forceCloseLamp.Active = false;
            this.forceCloseLamp.ActiveColor = System.Drawing.Color.Yellow;
            this.forceCloseLamp.AutoSize = true;
            this.forceCloseLamp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.forceCloseLamp.LabelText = "Принудительное закрытие";
            this.forceCloseLamp.Location = new System.Drawing.Point(10, 150);
            this.forceCloseLamp.Margin = new System.Windows.Forms.Padding(5);
            this.forceCloseLamp.Name = "forceCloseLamp";
            this.forceCloseLamp.Size = new System.Drawing.Size(259, 25);
            this.forceCloseLamp.TabIndex = 55;
            // 
            // labledLamp2
            // 
            this.labledLamp2.Active = false;
            this.labledLamp2.ActiveColor = System.Drawing.Color.OrangeRed;
            this.labledLamp2.AutoSize = true;
            this.labledLamp2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.labledLamp2.LabelText = "Нет связи";
            this.labledLamp2.Location = new System.Drawing.Point(10, 185);
            this.labledLamp2.Margin = new System.Windows.Forms.Padding(5);
            this.labledLamp2.Name = "labledLamp2";
            this.labledLamp2.Size = new System.Drawing.Size(121, 25);
            this.labledLamp2.TabIndex = 56;
            // 
            // notOpenedLamp
            // 
            this.notOpenedLamp.Active = false;
            this.notOpenedLamp.ActiveColor = System.Drawing.Color.OrangeRed;
            this.notOpenedLamp.AutoSize = true;
            this.notOpenedLamp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.notOpenedLamp.LabelText = "Не открылся";
            this.notOpenedLamp.Location = new System.Drawing.Point(10, 220);
            this.notOpenedLamp.Margin = new System.Windows.Forms.Padding(5);
            this.notOpenedLamp.Name = "notOpenedLamp";
            this.notOpenedLamp.Size = new System.Drawing.Size(145, 25);
            this.notOpenedLamp.TabIndex = 57;
            // 
            // notClosedLamp
            // 
            this.notClosedLamp.Active = false;
            this.notClosedLamp.ActiveColor = System.Drawing.Color.OrangeRed;
            this.notClosedLamp.AutoSize = true;
            this.notClosedLamp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.notClosedLamp.LabelText = "Не закрылся";
            this.notClosedLamp.Location = new System.Drawing.Point(10, 255);
            this.notClosedLamp.Margin = new System.Windows.Forms.Padding(5);
            this.notClosedLamp.Name = "notClosedLamp";
            this.notClosedLamp.Size = new System.Drawing.Size(144, 25);
            this.notClosedLamp.TabIndex = 58;
            // 
            // collisionLamp
            // 
            this.collisionLamp.Active = false;
            this.collisionLamp.ActiveColor = System.Drawing.Color.OrangeRed;
            this.collisionLamp.AutoSize = true;
            this.collisionLamp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.collisionLamp.LabelText = "Коллизия концевиков";
            this.collisionLamp.Location = new System.Drawing.Point(10, 290);
            this.collisionLamp.Margin = new System.Windows.Forms.Padding(5);
            this.collisionLamp.Name = "collisionLamp";
            this.collisionLamp.Size = new System.Drawing.Size(218, 25);
            this.collisionLamp.TabIndex = 59;
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(305, 348);
            this.ControlBox = false;
            this.Controls.Add(this.flowLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "SettingsForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private Utils.LabledLamp openedLamp;
        private Utils.LabledLamp closedLamp;
        private Utils.LabledLamp blockOpeningLamp;
        private Utils.LabledLamp blockClosingLamp;
        private Utils.LabledLamp forceCloseLamp;
        private Utils.LabledLamp labledLamp2;
        private Utils.LabledLamp notOpenedLamp;
        private Utils.LabledLamp notClosedLamp;
        private Utils.LabledLamp collisionLamp;
    }
}