namespace NtoLib.Pumps
{
    partial class PumpControl
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
            this.buttonTable = new System.Windows.Forms.TableLayoutPanel();
            this.spriteBox = new System.Windows.Forms.PictureBox();
            this.buttonOpen = new NtoLib.Utils.LabledButton();
            this.buttonClose = new NtoLib.Utils.LabledButton();
            this.buttonTable.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.spriteBox)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonTable
            // 
            this.buttonTable.ColumnCount = 2;
            this.buttonTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.buttonTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.buttonTable.Controls.Add(this.buttonOpen, 0, 0);
            this.buttonTable.Controls.Add(this.buttonClose, 1, 1);
            this.buttonTable.Location = new System.Drawing.Point(112, 3);
            this.buttonTable.Margin = new System.Windows.Forms.Padding(0);
            this.buttonTable.Name = "buttonTable";
            this.buttonTable.RowCount = 2;
            this.buttonTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.buttonTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.buttonTable.Size = new System.Drawing.Size(103, 103);
            this.buttonTable.TabIndex = 3;
            // 
            // spriteBox
            // 
            this.spriteBox.Location = new System.Drawing.Point(3, 3);
            this.spriteBox.Name = "spriteBox";
            this.spriteBox.Size = new System.Drawing.Size(106, 103);
            this.spriteBox.TabIndex = 2;
            this.spriteBox.TabStop = false;
            this.spriteBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.HandleMouseDown);
            this.spriteBox.MouseLeave += new System.EventHandler(this.StopHoldTimer);
            this.spriteBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.HandleMouseUp);
            // 
            // buttonOpen
            // 
            this.buttonOpen.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOpen.BackColor = System.Drawing.Color.AntiqueWhite;
            this.buttonOpen.ForeColor = System.Drawing.Color.LimeGreen;
            this.buttonOpen.Location = new System.Drawing.Point(1, 1);
            this.buttonOpen.Margin = new System.Windows.Forms.Padding(1);
            this.buttonOpen.Name = "buttonOpen";
            this.buttonOpen.Size = new System.Drawing.Size(49, 49);
            this.buttonOpen.SymbolOnButton = NtoLib.Utils.SymbolType.On;
            this.buttonOpen.TabIndex = 2;
            this.buttonOpen.UseVisualStyleBackColor = false;
            this.buttonOpen.Click += new System.EventHandler(this.HandleStartClick);
            // 
            // buttonClose
            // 
            this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonClose.BackColor = System.Drawing.Color.AntiqueWhite;
            this.buttonClose.ForeColor = System.Drawing.Color.Red;
            this.buttonClose.Location = new System.Drawing.Point(52, 52);
            this.buttonClose.Margin = new System.Windows.Forms.Padding(1);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(50, 50);
            this.buttonClose.SymbolOnButton = NtoLib.Utils.SymbolType.Off;
            this.buttonClose.TabIndex = 2;
            this.buttonClose.UseVisualStyleBackColor = false;
            this.buttonClose.Click += new System.EventHandler(this.HandleStopClick);
            // 
            // PumpControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.buttonTable);
            this.Controls.Add(this.spriteBox);
            this.Name = "PumpControl";
            this.Size = new System.Drawing.Size(220, 110);
            this.VisibleChanged += new System.EventHandler(this.HandleVisibleChanged);
            this.Resize += new System.EventHandler(this.HandleResize);
            this.buttonTable.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.spriteBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel buttonTable;
        private Utils.LabledButton buttonOpen;
        private Utils.LabledButton buttonClose;
        private System.Windows.Forms.PictureBox spriteBox;
    }
}
