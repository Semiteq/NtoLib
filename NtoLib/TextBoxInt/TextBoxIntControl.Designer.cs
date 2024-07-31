namespace NtoLib.TextBoxInt
{
    partial class TextBoxIntControl
    {
        /// <summary> 
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if(disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором компонентов

        /// <summary> 
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.textBox = new NtoLib.Utils.TextBoxInt();
            this.SuspendLayout();
            // 
            // textBox
            // 
            this.textBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox.Font = new System.Drawing.Font("Arial", 17.7F);
            this.textBox.ForeColor = System.Drawing.Color.Black;
            this.textBox.Location = new System.Drawing.Point(1, 1);
            this.textBox.Margin = new System.Windows.Forms.Padding(1);
            this.textBox.Name = "textBox";
            this.textBox.Size = new System.Drawing.Size(148, 28);
            this.textBox.TabIndex = 0;
            this.textBox.Text = "0";
            this.textBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // TextBoxIntControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.Black;
            this.Controls.Add(this.textBox);
            this.Name = "TextBoxIntControl";
            this.Size = new System.Drawing.Size(150, 30);
            this.Resize += new System.EventHandler(this.HandleResize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Utils.TextBoxInt textBox;
    }
}
