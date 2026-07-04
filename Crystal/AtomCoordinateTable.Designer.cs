namespace Crystallography.Controls
{
    partial class AtomCoordinateTable
    {
        /// <summary>必要なデザイナ変数です。</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>使用中のリソースをすべてクリーンアップします。</summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            //if (bmp != null) bmp.Dispose(); // (260611Ch) 旧: Graphics フィールドが未解放
            g?.Dispose(); // (260611Ch)
            bmp?.Dispose(); // (260611Ch)
            base.Dispose(disposing);
        }

        #region コンポーネント デザイナで生成されたコード

        /// <summary> 
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を 
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            toolTip = new System.Windows.Forms.ToolTip(components);
            comboBox = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            dataGridView = new DpiAwareDataGridView();
            atomLabelDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            lengthÅDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataSet = new System.Data.DataSet();
            dataTable1 = new System.Data.DataTable();
            dataColumn1 = new System.Data.DataColumn();
            dataColumn2 = new System.Data.DataColumn();
            numericUpDownWidth = new NumericBox();
            numericUpDownMaxLength = new NumericBox();
            pictureBox = new System.Windows.Forms.PictureBox();
            flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)dataGridView).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataSet).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataTable1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox).BeginInit();
            flowLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // toolTip
            // 
            toolTip.AutoPopDelay = 10000;
            toolTip.InitialDelay = 500;
            toolTip.IsBalloon = true;
            toolTip.ReshowDelay = 100;
            // 
            // comboBox
            // 
            comboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            comboBox.FormattingEnabled = true;
            comboBox.Location = new System.Drawing.Point(86, 3);
            comboBox.Margin = new System.Windows.Forms.Padding(0, 3, 3, 4);
            comboBox.Name = "comboBox";
            comboBox.Size = new System.Drawing.Size(121, 23);
            comboBox.TabIndex = 0;
            toolTip.SetToolTip(comboBox, "Select the target atom; neighboring\r\natoms and their interatomic distances\r\nare listed and plotted around this atom.");
            comboBox.SelectedIndexChanged += comboBox_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            label1.Location = new System.Drawing.Point(3, 6);
            label1.Margin = new System.Windows.Forms.Padding(3, 6, 3, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(80, 17);
            label1.TabIndex = 1;
            label1.Text = "Target Atom";
            toolTip.SetToolTip(label1, "Select the target atom; neighboring\r\natoms and their interatomic distances\r\nare listed and plotted around this atom.");
            // 
            // dataGridView
            // 
            dataGridView.AllowUserToAddRows = false;
            dataGridView.AllowUserToDeleteRows = false;
            dataGridView.AllowUserToResizeColumns = false;
            dataGridView.AutoGenerateColumns = false;
            dataGridView.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { atomLabelDataGridViewTextBoxColumn, lengthÅDataGridViewTextBoxColumn });
            dataGridView.DataMember = "Table1";
            dataGridView.DataSource = dataSet;
            dataGridView.Dock = System.Windows.Forms.DockStyle.Left;
            dataGridView.Location = new System.Drawing.Point(0, 30);
            dataGridView.Name = "dataGridView";
            dataGridView.ReadOnly = true;
            dataGridView.RowHeadersVisible = false;
            dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dataGridView.Size = new System.Drawing.Size(224, 171);
            dataGridView.TabIndex = 2;
            toolTip.SetToolTip(dataGridView, "List of neighboring atoms with their\r\ndistance (in angstroms, Å) from the target\r\natom, sorted by increasing distance.");
            // 
            // atomLabelDataGridViewTextBoxColumn
            // 
            atomLabelDataGridViewTextBoxColumn.DataPropertyName = "Atom Label";
            atomLabelDataGridViewTextBoxColumn.HeaderText = "Atom Label";
            atomLabelDataGridViewTextBoxColumn.Name = "atomLabelDataGridViewTextBoxColumn";
            atomLabelDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // lengthÅDataGridViewTextBoxColumn
            // 
            lengthÅDataGridViewTextBoxColumn.DataPropertyName = "Length (Å)";
            lengthÅDataGridViewTextBoxColumn.HeaderText = "Length (Å)";
            lengthÅDataGridViewTextBoxColumn.Name = "lengthÅDataGridViewTextBoxColumn";
            lengthÅDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // dataSet
            // 
            dataSet.DataSetName = "NewDataSet";
            dataSet.Tables.AddRange(new System.Data.DataTable[] { dataTable1 });
            // 
            // dataTable1
            // 
            dataTable1.Columns.AddRange(new System.Data.DataColumn[] { dataColumn1, dataColumn2 });
            dataTable1.TableName = "Table1";
            // 
            // dataColumn1
            // 
            dataColumn1.ColumnName = "Atom Label";
            // 
            // dataColumn2
            // 
            dataColumn2.ColumnName = "Length (Å)";
            // 
            // numericUpDownWidth
            // 
            numericUpDownWidth.BackColor = System.Drawing.Color.Transparent;
            numericUpDownWidth.DecimalPlaces = 2;
            numericUpDownWidth.FooterPadding = new System.Windows.Forms.Padding(0, 4, 0, 0);
            numericUpDownWidth.FooterText = "Å";
            numericUpDownWidth.HeaderPadding = new System.Windows.Forms.Padding(0, 4, 0, 0);
            numericUpDownWidth.HeaderText = "Bar Width"; // 260704Cl 旧 label2 由来の誤字 "Bar Wdth" を修正
            numericUpDownWidth.Location = new System.Drawing.Point(214, 0);
            numericUpDownWidth.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            numericUpDownWidth.Maximum = 100D;
            numericUpDownWidth.MaximumSize = new System.Drawing.Size(1000, 100);
            numericUpDownWidth.Minimum = 0.01D;
            numericUpDownWidth.MinimumSize = new System.Drawing.Size(10, 20);
            numericUpDownWidth.Name = "numericUpDownWidth";
            numericUpDownWidth.RadianValue = 0.0017453292519943296D;
            numericUpDownWidth.ShowUpDown = true;
            numericUpDownWidth.Size = new System.Drawing.Size(139, 25);
            numericUpDownWidth.TabIndex = 4;
            toolTip.SetToolTip(numericUpDownWidth, "Half-width of the distance-histogram\r\nbars, in angstroms (Å); larger values\r\nbroaden each bar so nearby peaks merge.");
            numericUpDownWidth.UpDown_Increment = 0.01D;
            numericUpDownWidth.Value = 0.1D;
            numericUpDownWidth.ValueBoxWidth = 45;
            numericUpDownWidth.ValueChanged += numericUpDownWidth_ValueChanged;
            // 
            // numericUpDownMaxLength
            // 
            numericUpDownMaxLength.BackColor = System.Drawing.Color.Transparent;
            numericUpDownMaxLength.DecimalPlaces = 1;
            numericUpDownMaxLength.FooterPadding = new System.Windows.Forms.Padding(0, 4, 0, 0);
            numericUpDownMaxLength.FooterText = "Å";
            numericUpDownMaxLength.HeaderPadding = new System.Windows.Forms.Padding(0, 4, 0, 0);
            numericUpDownMaxLength.HeaderText = "Max. distance";
            numericUpDownMaxLength.Location = new System.Drawing.Point(357, 0);
            numericUpDownMaxLength.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            numericUpDownMaxLength.Maximum = 100D;
            numericUpDownMaxLength.MaximumSize = new System.Drawing.Size(1000, 100);
            numericUpDownMaxLength.Minimum = 0D;
            numericUpDownMaxLength.MinimumSize = new System.Drawing.Size(10, 20);
            numericUpDownMaxLength.Name = "numericUpDownMaxLength";
            numericUpDownMaxLength.RadianValue = 0.087266462599716474D;
            numericUpDownMaxLength.ShowUpDown = true;
            numericUpDownMaxLength.Size = new System.Drawing.Size(161, 25);
            numericUpDownMaxLength.TabIndex = 4;
            toolTip.SetToolTip(numericUpDownMaxLength, "Maximum interatomic distance to search,\r\nin angstroms (Å); only atoms within this\r\nradius of the target atom are listed.");
            numericUpDownMaxLength.UpDown_Increment = 0.5D;
            numericUpDownMaxLength.Value = 5D;
            numericUpDownMaxLength.ValueBoxWidth = 40;
            numericUpDownMaxLength.ValueChanged += numericUpDownMaxLength_ValueChanged;
            // 
            // pictureBox
            // 
            pictureBox.BackColor = System.Drawing.Color.White;
            pictureBox.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            pictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            pictureBox.Location = new System.Drawing.Point(224, 30);
            pictureBox.Name = "pictureBox";
            pictureBox.Padding = new System.Windows.Forms.Padding(3);
            pictureBox.Size = new System.Drawing.Size(331, 171);
            pictureBox.TabIndex = 3;
            pictureBox.TabStop = false;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.AutoSize = true;
            flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            flowLayoutPanel1.Controls.Add(label1);
            flowLayoutPanel1.Controls.Add(comboBox);
            flowLayoutPanel1.Controls.Add(numericUpDownWidth);
            flowLayoutPanel1.Controls.Add(numericUpDownMaxLength);
            flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new System.Drawing.Size(555, 30);
            flowLayoutPanel1.TabIndex = 5;
            flowLayoutPanel1.WrapContents = false;
            // 
            // AtomCoordinateTable
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            Controls.Add(pictureBox);
            Controls.Add(dataGridView);
            Controls.Add(flowLayoutPanel1);
            Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "AtomCoordinateTable";
            Size = new System.Drawing.Size(555, 201);
            Resize += AtomCoordinateTable_Resize_1;
            ((System.ComponentModel.ISupportInitialize)dataGridView).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataSet).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataTable1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox).EndInit();
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolTip toolTip; // (260531Ch)

        private System.Windows.Forms.ComboBox comboBox;
        private System.Windows.Forms.Label label1;
        // private System.Windows.Forms.DataGridView dataGridView; // 260518Cl 旧実装
        private DpiAwareDataGridView dataGridView; // 260518Cl
        private System.Windows.Forms.DataGridViewTextBoxColumn atomLabelDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn lengthÅDataGridViewTextBoxColumn;
        private System.Data.DataSet dataSet;
        private System.Data.DataTable dataTable1;
        private System.Data.DataColumn dataColumn1;
        private System.Data.DataColumn dataColumn2;
        private System.Windows.Forms.PictureBox pictureBox;
        //private System.Windows.Forms.NumericUpDown numericUpDownWidth; // 260704Cl 旧実装: NumericUpDown → NumericBox 置換
        private NumericBox numericUpDownWidth; // 260704Cl
        //private System.Windows.Forms.NumericUpDown numericUpDownMaxLength; // 260704Cl 旧実装
        private NumericBox numericUpDownMaxLength; // 260704Cl
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
    }
}
