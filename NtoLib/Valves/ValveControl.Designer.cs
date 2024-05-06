namespace NtoLib.Valves
{
    partial class ValveControl
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
            this.SuspendLayout();
            // 
            // ValveControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.DoubleBuffered = false;
            this.Name = "ValveControl";
            this.Size = new System.Drawing.Size(60, 50);
            this.VisibleChanged += new System.EventHandler(this.HandleVisibleChanged);
            this.DoubleClick += new System.EventHandler(this.HandleDoubleClick);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.HandleMouseDown);
            this.MouseLeave += new System.EventHandler(this.StopHoldTimer);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.HandleMouseUp);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
