using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable
{

    public partial class TableControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this._table = new System.Windows.Forms.DataGridView();
            this.DbgMsg = new System.Windows.Forms.Label();
            this._buttonOpen = new System.Windows.Forms.Button();
            this._buttonAddAfter = new System.Windows.Forms.Button();
            this._buttonSave = new System.Windows.Forms.Button();
            this._buttonAddBefore = new System.Windows.Forms.Button();
            this._buttonDel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this._table)).BeginInit();
            this.SuspendLayout();
            // 
            // _table
            // 
            this._table.AllowUserToAddRows = false;
            this._table.AllowUserToDeleteRows = false;
            this._table.AllowUserToResizeRows = false;
            this._table.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._table.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Arial", 14F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this._table.DefaultCellStyle = dataGridViewCellStyle1;
            this._table.Location = new System.Drawing.Point(3, 3);
            this._table.MultiSelect = false;
            this._table.Name = "_table";
            this._table.ReadOnly = true;
            this._table.RowHeadersWidth = 20;
            this._table.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this._table.Size = new System.Drawing.Size(962, 474);
            this._table.TabIndex = 1;
            // 
            // DbgMsg
            // 
            this.DbgMsg.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DbgMsg.Font = new System.Drawing.Font("Arial", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.DbgMsg.Location = new System.Drawing.Point(3, 483);
            this.DbgMsg.Name = "DbgMsg";
            this.DbgMsg.Size = new System.Drawing.Size(670, 40);
            this.DbgMsg.TabIndex = 14;
            this.DbgMsg.Text = "DbgMsg";
            this.DbgMsg.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _buttonOpen
            // 
            this._buttonOpen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonOpen.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this._buttonOpen.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this._buttonOpen.Location = new System.Drawing.Point(826, 483);
            this._buttonOpen.Name = "_buttonOpen";
            this._buttonOpen.Size = new System.Drawing.Size(43, 40);
            this._buttonOpen.TabIndex = 8;
            this._buttonOpen.Text = "open";
            this._buttonOpen.UseVisualStyleBackColor = true;
            this._buttonOpen.Click += new System.EventHandler(this.ClickButton_Open);
            // 
            // _buttonAddAfter
            // 
            this._buttonAddAfter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonAddAfter.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this._buttonAddAfter.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this._buttonAddAfter.Location = new System.Drawing.Point(778, 483);
            this._buttonAddAfter.Name = "_buttonAddAfter";
            this._buttonAddAfter.Size = new System.Drawing.Size(43, 40);
            this._buttonAddAfter.TabIndex = 13;
            this._buttonAddAfter.Text = "+>";
            this._buttonAddAfter.UseVisualStyleBackColor = true;
            this._buttonAddAfter.Click += new System.EventHandler(this.ClickButton_AddLineAfter);
            // 
            // _buttonSave
            // 
            this._buttonSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonSave.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this._buttonSave.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this._buttonSave.Location = new System.Drawing.Point(874, 483);
            this._buttonSave.Name = "_buttonSave";
            this._buttonSave.Size = new System.Drawing.Size(43, 40);
            this._buttonSave.TabIndex = 9;
            this._buttonSave.Text = "save";
            this._buttonSave.UseVisualStyleBackColor = true;
            this._buttonSave.Click += new System.EventHandler(this.ClickButton_Save);
            // 
            // _buttonAddBefore
            // 
            this._buttonAddBefore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonAddBefore.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this._buttonAddBefore.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this._buttonAddBefore.Location = new System.Drawing.Point(730, 483);
            this._buttonAddBefore.Name = "_buttonAddBefore";
            this._buttonAddBefore.Size = new System.Drawing.Size(43, 40);
            this._buttonAddBefore.TabIndex = 12;
            this._buttonAddBefore.Text = "<+";
            this._buttonAddBefore.UseVisualStyleBackColor = true;
            this._buttonAddBefore.Click += new System.EventHandler(this.ClickButton_AddLineBefore);
            // 
            // _buttonDel
            // 
            this._buttonDel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonDel.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this._buttonDel.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this._buttonDel.Location = new System.Drawing.Point(682, 483);
            this._buttonDel.Name = "_buttonDel";
            this._buttonDel.Size = new System.Drawing.Size(43, 40);
            this._buttonDel.TabIndex = 11;
            this._buttonDel.Text = "del";
            this._buttonDel.UseVisualStyleBackColor = true;
            this._buttonDel.Click += new System.EventHandler(this.ClickButton_Delete);
            // 
            // TableControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this._buttonOpen);
            this.Controls.Add(this._buttonAddAfter);
            this.Controls.Add(this._buttonSave);
            this.Controls.Add(this._buttonAddBefore);
            this.Controls.Add(this._buttonDel);
            this.Controls.Add(this._table);
            this.Controls.Add(this.DbgMsg);
            this.Name = "TableControl";
            this.Size = new System.Drawing.Size(968, 526);
            ((System.ComponentModel.ISupportInitialize)(this._table)).EndInit();
            this.ResumeLayout(false);

        }
        private Label DbgMsg;
    }
}