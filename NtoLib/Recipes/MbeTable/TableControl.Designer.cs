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
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.button_add_after = new System.Windows.Forms.Button();
            this.button_add_before = new System.Windows.Forms.Button();
            this.button_del = new System.Windows.Forms.Button();
            this.DbgMsg = new System.Windows.Forms.TextBox();
            this.button_save = new System.Windows.Forms.Button();
            this.button_open = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.TimeRecalculate = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToResizeRows = false;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Arial", 14F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridView1.DefaultCellStyle = dataGridViewCellStyle1;
            this.dataGridView1.Location = new System.Drawing.Point(141, 3);
            this.dataGridView1.MultiSelect = false;
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dataGridView1.Size = new System.Drawing.Size(490, 342);
            this.dataGridView1.TabIndex = 1;
            this.dataGridView1.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.EndCellEdit);
            this.dataGridView1.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellValueChanged);
            this.dataGridView1.CurrentCellDirtyStateChanged += new System.EventHandler(this.dataGridView1_CurrentCellDirtyStateChanged);
            // 
            // Column1
            // 
            this.Column1.MinimumWidth = 6;
            this.Column1.Name = "Column1";
            this.Column1.Width = 125;
            // 
            // Column2
            // 
            this.Column2.MinimumWidth = 6;
            this.Column2.Name = "Column2";
            this.Column2.Width = 125;
            // 
            // Column3
            // 
            this.Column3.MinimumWidth = 6;
            this.Column3.Name = "Column3";
            this.Column3.Width = 125;
            // 
            // Column4
            // 
            this.Column4.MinimumWidth = 6;
            this.Column4.Name = "Column4";
            this.Column4.Width = 125;
            // 
            // Column5
            // 
            this.Column5.MinimumWidth = 6;
            this.Column5.Name = "Column5";
            this.Column5.Width = 125;
            // 
            // button_add_after
            // 
            this.button_add_after.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.button_add_after.Location = new System.Drawing.Point(196, 426);
            this.button_add_after.Name = "button_add_after";
            this.button_add_after.Size = new System.Drawing.Size(43, 40);
            this.button_add_after.TabIndex = 13;
            this.button_add_after.Text = "ADD+";
            this.button_add_after.UseVisualStyleBackColor = true;
            this.button_add_after.Visible = false;
            this.button_add_after.Click += new System.EventHandler(this.ClickButton_AddLineAfter);
            // 
            // button_add_before
            // 
            this.button_add_before.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.button_add_before.Location = new System.Drawing.Point(148, 426);
            this.button_add_before.Name = "button_add_before";
            this.button_add_before.Size = new System.Drawing.Size(43, 40);
            this.button_add_before.TabIndex = 12;
            this.button_add_before.Text = "ADD-";
            this.button_add_before.UseVisualStyleBackColor = true;
            this.button_add_before.Visible = false;
            this.button_add_before.Click += new System.EventHandler(this.ClickButton_AddLineBefore);
            // 
            // button_del
            // 
            this.button_del.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.button_del.Location = new System.Drawing.Point(99, 426);
            this.button_del.Name = "button_del";
            this.button_del.Size = new System.Drawing.Size(43, 40);
            this.button_del.TabIndex = 11;
            this.button_del.Text = "DEL";
            this.button_del.UseVisualStyleBackColor = true;
            this.button_del.Visible = false;
            this.button_del.Click += new System.EventHandler(this.ClickButton_Delete);
            // 
            // DbgMsg
            // 
            this.DbgMsg.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.DbgMsg.Font = new System.Drawing.Font("Arial", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.DbgMsg.Location = new System.Drawing.Point(394, 432);
            this.DbgMsg.Multiline = true;
            this.DbgMsg.Name = "DbgMsg";
            this.DbgMsg.ReadOnly = true;
            this.DbgMsg.Size = new System.Drawing.Size(369, 87);
            this.DbgMsg.TabIndex = 10;
            this.DbgMsg.Text = "Подготовка таблицы. Загрузка структуры таблицы.  Успешно. Таблица подготовлена.";
            // 
            // button_save
            // 
            this.button_save.Enabled = false;
            this.button_save.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.button_save.Location = new System.Drawing.Point(52, 426);
            this.button_save.Name = "button_save";
            this.button_save.Size = new System.Drawing.Size(41, 40);
            this.button_save.TabIndex = 9;
            this.button_save.Text = "SAVE";
            this.button_save.UseVisualStyleBackColor = true;
            this.button_save.Visible = false;
            this.button_save.Click += new System.EventHandler(this.ClickButton_Save);
            // 
            // button_open
            // 
            this.button_open.Enabled = false;
            this.button_open.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.button_open.Location = new System.Drawing.Point(3, 426);
            this.button_open.Name = "button_open";
            this.button_open.Size = new System.Drawing.Size(43, 40);
            this.button_open.TabIndex = 8;
            this.button_open.Text = "OPEN";
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
            // TimeRecalculate
            // 
            this.TimeRecalculate.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.TimeRecalculate.Location = new System.Drawing.Point(245, 426);
            this.TimeRecalculate.Name = "TimeRecalculate";
            this.TimeRecalculate.Size = new System.Drawing.Size(43, 40);
            this.TimeRecalculate.TabIndex = 14;
            this.TimeRecalculate.Text = "Time";
            this.TimeRecalculate.UseVisualStyleBackColor = true;
            // 
            // TableControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.TimeRecalculate);
            this.Controls.Add(this.button_add_after);
            this.Controls.Add(this.button_add_before);
            this.Controls.Add(this.button_del);
            this.Controls.Add(this.DbgMsg);
            this.Controls.Add(this.button_save);
            this.Controls.Add(this.button_open);
            this.Controls.Add(this.dataGridView1);
            this.Name = "TableControl";
            this.Size = new System.Drawing.Size(968, 526);
            this.Load += new System.EventHandler(this.MainTable_Load);
            this.SizeChanged += new System.EventHandler(this.MainTable_SizeChanged);
            this.VisibleChanged += new System.EventHandler(this.HandleVisibleChanged);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private DataGridViewTextBoxColumn Column1;
        private DataGridViewTextBoxColumn Column2;
        private DataGridViewTextBoxColumn Column3;
        private DataGridViewTextBoxColumn Column4;
        private DataGridViewTextBoxColumn Column5;
        private Button TimeRecalculate;
    }
}