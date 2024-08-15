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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TableControl));
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.button_add_after = new System.Windows.Forms.Button();
            this.button_add_before = new System.Windows.Forms.Button();
            this.button_del = new System.Windows.Forms.Button();
            this.button_save = new System.Windows.Forms.Button();
            this.button_open = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.DbgMsg = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToResizeRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Arial", 14F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridView1.DefaultCellStyle = dataGridViewCellStyle2;
            this.dataGridView1.Location = new System.Drawing.Point(3, 3);
            this.dataGridView1.MultiSelect = false;
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersWidth = 20;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dataGridView1.Size = new System.Drawing.Size(962, 474);
            this.dataGridView1.TabIndex = 1;
            this.dataGridView1.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.EndCellEdit);
            this.dataGridView1.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellValueChanged);
            this.dataGridView1.CurrentCellDirtyStateChanged += new System.EventHandler(this.dataGridView1_CurrentCellDirtyStateChanged);
            // 
            // button_add_after
            // 
            this.button_add_after.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_add_after.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("button_add_after.BackgroundImage")));
            this.button_add_after.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.button_add_after.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.button_add_after.Location = new System.Drawing.Point(826, 483);
            this.button_add_after.Name = "button_add_after";
            this.button_add_after.Size = new System.Drawing.Size(43, 40);
            this.button_add_after.TabIndex = 13;
            this.button_add_after.UseVisualStyleBackColor = true;
            this.button_add_after.Visible = false;
            this.button_add_after.Click += new System.EventHandler(this.ClickButton_AddLineAfter);
            // 
            // button_add_before
            // 
            this.button_add_before.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_add_before.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("button_add_before.BackgroundImage")));
            this.button_add_before.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.button_add_before.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.button_add_before.Location = new System.Drawing.Point(777, 483);
            this.button_add_before.Name = "button_add_before";
            this.button_add_before.Size = new System.Drawing.Size(43, 40);
            this.button_add_before.TabIndex = 12;
            this.button_add_before.UseVisualStyleBackColor = true;
            this.button_add_before.Visible = false;
            this.button_add_before.Click += new System.EventHandler(this.ClickButton_AddLineBefore);
            // 
            // button_del
            // 
            this.button_del.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_del.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("button_del.BackgroundImage")));
            this.button_del.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.button_del.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.button_del.Location = new System.Drawing.Point(728, 483);
            this.button_del.Name = "button_del";
            this.button_del.Size = new System.Drawing.Size(43, 40);
            this.button_del.TabIndex = 11;
            this.button_del.UseVisualStyleBackColor = true;
            this.button_del.Visible = false;
            this.button_del.Click += new System.EventHandler(this.ClickButton_Delete);
            // 
            // button_save
            // 
            this.button_save.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_save.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("button_save.BackgroundImage")));
            this.button_save.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.button_save.Enabled = false;
            this.button_save.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.button_save.Location = new System.Drawing.Point(924, 483);
            this.button_save.Name = "button_save";
            this.button_save.Size = new System.Drawing.Size(41, 40);
            this.button_save.TabIndex = 9;
            this.button_save.UseVisualStyleBackColor = true;
            this.button_save.Visible = false;
            this.button_save.Click += new System.EventHandler(this.ClickButton_Save);
            // 
            // button_open
            // 
            this.button_open.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_open.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("button_open.BackgroundImage")));
            this.button_open.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.button_open.Enabled = false;
            this.button_open.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.button_open.Location = new System.Drawing.Point(875, 483);
            this.button_open.Name = "button_open";
            this.button_open.Size = new System.Drawing.Size(43, 40);
            this.button_open.TabIndex = 8;
            this.button_open.UseVisualStyleBackColor = true;
            this.button_open.Click += new System.EventHandler(this.ClickButton_Open);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.Filter = "recipe(*.csv)|*.csv";
            this.openFileDialog1.InitialDirectory = "c:\\";
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.Filter = "recipe(*.csv)|*.csv";
            this.saveFileDialog1.InitialDirectory = "c:\\";
            // 
            // DbgMsg
            // 
            this.DbgMsg.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DbgMsg.Font = new System.Drawing.Font("Arial", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.DbgMsg.Location = new System.Drawing.Point(3, 483);
            this.DbgMsg.Name = "DbgMsg";
            this.DbgMsg.Size = new System.Drawing.Size(719, 40);
            this.DbgMsg.TabIndex = 14;
            this.DbgMsg.Text = "Сообщения";
            this.DbgMsg.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // TableControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.button_open);
            this.Controls.Add(this.button_add_after);
            this.Controls.Add(this.button_save);
            this.Controls.Add(this.button_add_before);
            this.Controls.Add(this.button_del);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.DbgMsg);
            this.Name = "TableControl";
            this.Size = new System.Drawing.Size(968, 526);
            this.VisibleChanged += new System.EventHandler(this.HandleVisibleChanged);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }

        private DataGridViewTextBoxColumn Column1;
        private DataGridViewTextBoxColumn Column2;
        private DataGridViewTextBoxColumn Column3;
        private DataGridViewTextBoxColumn Column4;
        private DataGridViewTextBoxColumn Column5;
        private Label DbgMsg;
    }
}