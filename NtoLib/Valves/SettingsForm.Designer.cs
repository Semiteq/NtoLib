namespace NtoLib.Valves
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
            this.label1 = new System.Windows.Forms.Label();
            this.lampOpened = new NtoLib.Utils.Lamp();
            this.lampClosed = new NtoLib.Utils.Lamp();
            this.label2 = new System.Windows.Forms.Label();
            this.lampBlockOpening = new NtoLib.Utils.Lamp();
            this.label3 = new System.Windows.Forms.Label();
            this.lampBlockClosing = new NtoLib.Utils.Lamp();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(32, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(107, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Концевик открытия";
            // 
            // lampOpened
            // 
            this.lampOpened.Active = false;
            this.lampOpened.ActiveColor = System.Drawing.Color.LimeGreen;
            this.lampOpened.BackColor = System.Drawing.Color.Transparent;
            this.lampOpened.Cursor = System.Windows.Forms.Cursors.Default;
            this.lampOpened.Location = new System.Drawing.Point(9, 9);
            this.lampOpened.Margin = new System.Windows.Forms.Padding(0);
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
            this.lampClosed.Location = new System.Drawing.Point(9, 41);
            this.lampClosed.Margin = new System.Windows.Forms.Padding(0);
            this.lampClosed.Name = "lampClosed";
            this.lampClosed.Size = new System.Drawing.Size(20, 20);
            this.lampClosed.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(32, 48);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(108, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Концевик закрытия";
            // 
            // lampBlockOpening
            // 
            this.lampBlockOpening.Active = false;
            this.lampBlockOpening.ActiveColor = System.Drawing.Color.Yellow;
            this.lampBlockOpening.BackColor = System.Drawing.Color.Transparent;
            this.lampBlockOpening.Cursor = System.Windows.Forms.Cursors.Default;
            this.lampBlockOpening.Location = new System.Drawing.Point(9, 74);
            this.lampBlockOpening.Name = "lampBlockOpening";
            this.lampBlockOpening.Size = new System.Drawing.Size(20, 20);
            this.lampBlockOpening.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(32, 81);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(119, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Блокировка открытия";
            // 
            // lampBlockClosing
            // 
            this.lampBlockClosing.Active = false;
            this.lampBlockClosing.ActiveColor = System.Drawing.Color.Yellow;
            this.lampBlockClosing.BackColor = System.Drawing.Color.Transparent;
            this.lampBlockClosing.Cursor = System.Windows.Forms.Cursors.Default;
            this.lampBlockClosing.Location = new System.Drawing.Point(9, 111);
            this.lampBlockClosing.Name = "lampBlockClosing";
            this.lampBlockClosing.Size = new System.Drawing.Size(20, 20);
            this.lampBlockClosing.TabIndex = 6;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(32, 118);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(120, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Блокировка закрытия";
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(285, 450);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.lampBlockClosing);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.lampBlockOpening);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lampClosed);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lampOpened);
            this.Name = "SettingsForm";
            this.Text = "SettingsForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Utils.Lamp lampOpened;
        private System.Windows.Forms.Label label1;
        private Utils.Lamp lampClosed;
        private System.Windows.Forms.Label label2;
        private Utils.Lamp lampBlockOpening;
        private System.Windows.Forms.Label label3;
        private Utils.Lamp lampBlockClosing;
        private System.Windows.Forms.Label label4;
    }
}