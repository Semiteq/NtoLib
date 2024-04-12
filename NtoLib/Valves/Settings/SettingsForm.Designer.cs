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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            this.label1 = new System.Windows.Forms.Label();
            this.lampOpened = new NtoLib.Utils.Lamp();
            this.lampClosed = new NtoLib.Utils.Lamp();
            this.lampBlockOpening = new NtoLib.Utils.Lamp();
            this.lampBlockClosing = new NtoLib.Utils.Lamp();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.lampConnectionNotOk = new NtoLib.Utils.Lamp();
            this.lampNotOpened = new NtoLib.Utils.Lamp();
            this.lampNotClosed = new NtoLib.Utils.Lamp();
            this.lampCollision = new NtoLib.Utils.Lamp();
            this.label5 = new System.Windows.Forms.Label();
            this.lampAutoMode = new NtoLib.Utils.Lamp();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(37, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(150, 20);
            this.label1.TabIndex = 1;
            this.label1.Text = "Концевик открытия";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lampOpened
            // 
            this.lampOpened.Active = false;
            this.lampOpened.ActiveColor = System.Drawing.Color.LimeGreen;
            this.lampOpened.BackColor = System.Drawing.Color.Transparent;
            this.lampOpened.Cursor = System.Windows.Forms.Cursors.Default;
            this.lampOpened.Location = new System.Drawing.Point(9, 9);
            this.lampOpened.Margin = new System.Windows.Forms.Padding(5);
            this.lampOpened.Name = "lampOpened";
            this.lampOpened.Size = new System.Drawing.Size(20, 20);
            this.lampOpened.TabIndex = 0;
            // 
            // lampClosed
            // 
            this.lampClosed.Active = false;
            this.lampClosed.ActiveColor = System.Drawing.Color.LimeGreen;
            this.lampClosed.BackColor = System.Drawing.Color.Transparent;
            this.lampClosed.Cursor = System.Windows.Forms.Cursors.Default;
            this.lampClosed.Location = new System.Drawing.Point(9, 39);
            this.lampClosed.Margin = new System.Windows.Forms.Padding(5);
            this.lampClosed.Name = "lampClosed";
            this.lampClosed.Size = new System.Drawing.Size(20, 20);
            this.lampClosed.TabIndex = 2;
            // 
            // lampBlockOpening
            // 
            this.lampBlockOpening.Active = false;
            this.lampBlockOpening.ActiveColor = System.Drawing.Color.Yellow;
            this.lampBlockOpening.BackColor = System.Drawing.Color.Transparent;
            this.lampBlockOpening.Cursor = System.Windows.Forms.Cursors.Default;
            this.lampBlockOpening.Location = new System.Drawing.Point(9, 69);
            this.lampBlockOpening.Margin = new System.Windows.Forms.Padding(5);
            this.lampBlockOpening.Name = "lampBlockOpening";
            this.lampBlockOpening.Size = new System.Drawing.Size(20, 20);
            this.lampBlockOpening.TabIndex = 4;
            // 
            // lampBlockClosing
            // 
            this.lampBlockClosing.Active = false;
            this.lampBlockClosing.ActiveColor = System.Drawing.Color.Yellow;
            this.lampBlockClosing.BackColor = System.Drawing.Color.Transparent;
            this.lampBlockClosing.Cursor = System.Windows.Forms.Cursors.Default;
            this.lampBlockClosing.Location = new System.Drawing.Point(9, 99);
            this.lampBlockClosing.Margin = new System.Windows.Forms.Padding(5);
            this.lampBlockClosing.Name = "lampBlockClosing";
            this.lampBlockClosing.Size = new System.Drawing.Size(20, 20);
            this.lampBlockClosing.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(37, 39);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(150, 20);
            this.label2.TabIndex = 9;
            this.label2.Text = "Концевик закрытия";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(37, 69);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(150, 20);
            this.label3.TabIndex = 10;
            this.label3.Text = "Блокировка закрытия";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(37, 99);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(150, 20);
            this.label4.TabIndex = 11;
            this.label4.Text = "Блокировка открытия";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lampConnectionNotOk
            // 
            this.lampConnectionNotOk.Active = false;
            this.lampConnectionNotOk.ActiveColor = System.Drawing.Color.OrangeRed;
            this.lampConnectionNotOk.BackColor = System.Drawing.Color.Transparent;
            this.lampConnectionNotOk.Cursor = System.Windows.Forms.Cursors.Default;
            this.lampConnectionNotOk.Location = new System.Drawing.Point(9, 159);
            this.lampConnectionNotOk.Margin = new System.Windows.Forms.Padding(5);
            this.lampConnectionNotOk.Name = "lampConnectionNotOk";
            this.lampConnectionNotOk.Size = new System.Drawing.Size(20, 20);
            this.lampConnectionNotOk.TabIndex = 12;
            // 
            // lampNotOpened
            // 
            this.lampNotOpened.Active = false;
            this.lampNotOpened.ActiveColor = System.Drawing.Color.OrangeRed;
            this.lampNotOpened.BackColor = System.Drawing.Color.Transparent;
            this.lampNotOpened.Cursor = System.Windows.Forms.Cursors.Default;
            this.lampNotOpened.Location = new System.Drawing.Point(9, 189);
            this.lampNotOpened.Margin = new System.Windows.Forms.Padding(5);
            this.lampNotOpened.Name = "lampNotOpened";
            this.lampNotOpened.Size = new System.Drawing.Size(20, 20);
            this.lampNotOpened.TabIndex = 13;
            // 
            // lampNotClosed
            // 
            this.lampNotClosed.Active = false;
            this.lampNotClosed.ActiveColor = System.Drawing.Color.OrangeRed;
            this.lampNotClosed.BackColor = System.Drawing.Color.Transparent;
            this.lampNotClosed.Cursor = System.Windows.Forms.Cursors.Default;
            this.lampNotClosed.Location = new System.Drawing.Point(9, 219);
            this.lampNotClosed.Margin = new System.Windows.Forms.Padding(5);
            this.lampNotClosed.Name = "lampNotClosed";
            this.lampNotClosed.Size = new System.Drawing.Size(20, 20);
            this.lampNotClosed.TabIndex = 14;
            // 
            // lampCollision
            // 
            this.lampCollision.Active = false;
            this.lampCollision.ActiveColor = System.Drawing.Color.OrangeRed;
            this.lampCollision.BackColor = System.Drawing.Color.Transparent;
            this.lampCollision.Cursor = System.Windows.Forms.Cursors.Default;
            this.lampCollision.Location = new System.Drawing.Point(9, 249);
            this.lampCollision.Margin = new System.Windows.Forms.Padding(5);
            this.lampCollision.Name = "lampCollision";
            this.lampCollision.Size = new System.Drawing.Size(20, 20);
            this.lampCollision.TabIndex = 15;
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(37, 159);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(150, 20);
            this.label5.TabIndex = 16;
            this.label5.Text = "Нет соединения";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lampAutoMode
            // 
            this.lampAutoMode.Active = false;
            this.lampAutoMode.ActiveColor = System.Drawing.Color.Yellow;
            this.lampAutoMode.BackColor = System.Drawing.Color.Transparent;
            this.lampAutoMode.Cursor = System.Windows.Forms.Cursors.Default;
            this.lampAutoMode.Location = new System.Drawing.Point(9, 129);
            this.lampAutoMode.Margin = new System.Windows.Forms.Padding(5);
            this.lampAutoMode.Name = "lampAutoMode";
            this.lampAutoMode.Size = new System.Drawing.Size(20, 20);
            this.lampAutoMode.TabIndex = 17;
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(37, 119);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(150, 40);
            this.label6.TabIndex = 18;
            this.label6.Text = "Блокировка автоматическим режимом";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(37, 189);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(150, 20);
            this.label7.TabIndex = 19;
            this.label7.Text = "Не открылся";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label8
            // 
            this.label8.Location = new System.Drawing.Point(37, 219);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(150, 20);
            this.label8.TabIndex = 20;
            this.label8.Text = "Не закрылся";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label9
            // 
            this.label9.Location = new System.Drawing.Point(37, 249);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(150, 20);
            this.label9.TabIndex = 21;
            this.label9.Text = "Коллизия концевиков";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(193, 280);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.lampAutoMode);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.lampCollision);
            this.Controls.Add(this.lampNotClosed);
            this.Controls.Add(this.lampNotOpened);
            this.Controls.Add(this.lampConnectionNotOk);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lampOpened);
            this.Controls.Add(this.lampBlockClosing);
            this.Controls.Add(this.lampBlockOpening);
            this.Controls.Add(this.lampClosed);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SettingsForm";
            this.Text = "SettingsForm";
            this.ResumeLayout(false);

        }

        #endregion

        private Utils.Lamp lampOpened;
        private System.Windows.Forms.Label label1;
        private Utils.Lamp lampClosed;
        private Utils.Lamp lampBlockOpening;
        private Utils.Lamp lampBlockClosing;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private Utils.Lamp lampConnectionNotOk;
        private Utils.Lamp lampNotOpened;
        private Utils.Lamp lampNotClosed;
        private Utils.Lamp lampCollision;
        private System.Windows.Forms.Label label5;
        private Utils.Lamp lampAutoMode;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
    }
}