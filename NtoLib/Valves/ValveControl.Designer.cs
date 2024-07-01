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
            this.spriteBox = new System.Windows.Forms.PictureBox();
            this.buttonTable = new System.Windows.Forms.TableLayoutPanel();
            this.buttonClose = new NtoLib.Utils.LabledButton();
            this.buttonOpen = new NtoLib.Utils.LabledButton();
            this.buttonOpenSmoothly = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.spriteBox)).BeginInit();
            this.buttonTable.SuspendLayout();
            this.SuspendLayout();
            // 
            // spriteBox
            // 
            this.spriteBox.Location = new System.Drawing.Point(0, 0);
            this.spriteBox.Name = "spriteBox";
            this.spriteBox.Size = new System.Drawing.Size(106, 103);
            this.spriteBox.TabIndex = 0;
            this.spriteBox.TabStop = false;
            this.spriteBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.HandleMouseDown);
            this.spriteBox.MouseLeave += new System.EventHandler(this.StopHoldTimer);
            this.spriteBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.HandleMouseUp);
            // 
            // buttonTable
            // 
            this.buttonTable.ColumnCount = 3;
            this.buttonTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.buttonTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.buttonTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.buttonTable.Controls.Add(this.buttonClose, 2, 2);
            this.buttonTable.Controls.Add(this.buttonOpen, 0, 0);
            this.buttonTable.Controls.Add(this.buttonOpenSmoothly, 1, 1);
            this.buttonTable.Location = new System.Drawing.Point(120, 13);
            this.buttonTable.Margin = new System.Windows.Forms.Padding(0);
            this.buttonTable.Name = "buttonTable";
            this.buttonTable.RowCount = 3;
            this.buttonTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.buttonTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.buttonTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.buttonTable.Size = new System.Drawing.Size(454, 351);
            this.buttonTable.TabIndex = 1;
            // 
            // buttonClose
            // 
            this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonClose.BackColor = System.Drawing.Color.AntiqueWhite;
            this.buttonClose.Location = new System.Drawing.Point(303, 235);
            this.buttonClose.Margin = new System.Windows.Forms.Padding(1);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.OnButton = false;
            this.buttonClose.Size = new System.Drawing.Size(150, 115);
            this.buttonClose.TabIndex = 2;
            this.buttonClose.UseVisualStyleBackColor = false;
            // 
            // buttonOpen
            // 
            this.buttonOpen.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOpen.BackColor = System.Drawing.Color.AntiqueWhite;
            this.buttonOpen.Location = new System.Drawing.Point(1, 1);
            this.buttonOpen.Margin = new System.Windows.Forms.Padding(1);
            this.buttonOpen.Name = "buttonOpen";
            this.buttonOpen.OnButton = true;
            this.buttonOpen.Size = new System.Drawing.Size(149, 115);
            this.buttonOpen.TabIndex = 2;
            this.buttonOpen.UseVisualStyleBackColor = false;
            // 
            // buttonOpenSmoothly
            // 
            this.buttonOpenSmoothly.BackColor = System.Drawing.Color.AntiqueWhite;
            this.buttonOpenSmoothly.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonOpenSmoothly.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonOpenSmoothly.Font = new System.Drawing.Font("Arial", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.buttonOpenSmoothly.Location = new System.Drawing.Point(152, 118);
            this.buttonOpenSmoothly.Margin = new System.Windows.Forms.Padding(1);
            this.buttonOpenSmoothly.Name = "buttonOpenSmoothly";
            this.buttonOpenSmoothly.Size = new System.Drawing.Size(149, 115);
            this.buttonOpenSmoothly.TabIndex = 2;
            this.buttonOpenSmoothly.TabStop = false;
            this.buttonOpenSmoothly.Text = "S";
            this.buttonOpenSmoothly.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.buttonOpenSmoothly.UseVisualStyleBackColor = false;
            // 
            // ValveControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.Controls.Add(this.buttonTable);
            this.Controls.Add(this.spriteBox);
            this.DoubleBuffered = false;
            this.Name = "ValveControl";
            this.Size = new System.Drawing.Size(725, 436);
            this.VisibleChanged += new System.EventHandler(this.HandleVisibleChanged);
            this.Resize += new System.EventHandler(this.HandleResize);
            ((System.ComponentModel.ISupportInitialize)(this.spriteBox)).EndInit();
            this.buttonTable.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox spriteBox;
        private System.Windows.Forms.TableLayoutPanel buttonTable;
        private System.Windows.Forms.Button buttonOpenSmoothly;
        private Utils.LabledButton buttonOpen;
        private Utils.LabledButton buttonClose;
    }
}
