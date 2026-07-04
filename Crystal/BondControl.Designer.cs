namespace Crystallography.Controls
{
    partial class BondInputControl
    {
        /// <summary>必要なデザイナー変数です。</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>使用中のリソースをすべてクリーンアップします。</summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region コンポーネント デザイナーで生成されたコード

        /// <summary> 
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を 
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BondInputControl));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            checkBoxShowPolyhedron = new System.Windows.Forms.CheckBox();
            comboBoxBondingAtom1 = new System.Windows.Forms.ComboBox();
            comboBoxBondingAtom2 = new System.Windows.Forms.ComboBox();
            label58 = new System.Windows.Forms.Label();
            label57 = new System.Windows.Forms.Label();
            label39 = new System.Windows.Forms.Label();
            label40 = new System.Windows.Forms.Label();
            groupBoxPolyhedron = new System.Windows.Forms.GroupBox();
            flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            checkBoxShowInnerBonds = new System.Windows.Forms.CheckBox();
            checkBoxShowCenterAtom = new System.Windows.Forms.CheckBox();
            checkBoxShowVertexAtoms = new System.Windows.Forms.CheckBox();
            flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            numericBoxPolyhedronAlpha = new NumericBox();
            checkBoxShowEdges = new System.Windows.Forms.CheckBox();
            numericBoxEdgeWidth = new NumericBox();
            groupBoxBonds = new System.Windows.Forms.GroupBox();
            flowLayoutPanel8 = new System.Windows.Forms.FlowLayoutPanel();
            numericBoxBondRadius = new NumericBox();
            numericBoxBondAlpha = new NumericBox();
            flowLayoutPanel7 = new System.Windows.Forms.FlowLayoutPanel();
            numericBoxBondMinLength = new NumericBox();
            numericBoxBondMaxLength = new NumericBox();
            flowLayoutPanel4 = new System.Windows.Forms.FlowLayoutPanel();
            flowLayoutPanel5 = new System.Windows.Forms.FlowLayoutPanel();
            flowLayoutPanel6 = new System.Windows.Forms.FlowLayoutPanel();
            checkBoxShowBonds = new System.Windows.Forms.CheckBox();
            buttonAddBond = new System.Windows.Forms.Button();
            buttonChangeBond = new System.Windows.Forms.Button();
            buttonDeleteBond = new System.Windows.Forms.Button();
            dataGridView = new DpiAwareDataGridView();
            enabledDataGridViewCheckBoxColumn1 = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            centerDataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            vertexDataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            minLenDataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            maxLenDataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            showBondsDataGridViewCheckBoxColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            showPolyhedronDataGridViewCheckBoxColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            bindingSource = new System.Windows.Forms.BindingSource(components);
            dataSet = new DataSet();
            panel1 = new System.Windows.Forms.Panel();
            flowLayoutPanel10 = new System.Windows.Forms.FlowLayoutPanel();
            colorControlPolyhedron = new ColorControl();
            colorControlBond = new ColorControl();
            colorControlEdges = new ColorControl();
            panel2 = new System.Windows.Forms.Panel();
            toolTip = new System.Windows.Forms.ToolTip(components);
            groupBoxPolyhedron.SuspendLayout();
            flowLayoutPanel3.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            flowLayoutPanel2.SuspendLayout();
            groupBoxBonds.SuspendLayout();
            flowLayoutPanel8.SuspendLayout();
            flowLayoutPanel7.SuspendLayout();
            flowLayoutPanel4.SuspendLayout();
            flowLayoutPanel5.SuspendLayout();
            flowLayoutPanel6.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView).BeginInit();
            ((System.ComponentModel.ISupportInitialize)bindingSource).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataSet).BeginInit();
            panel1.SuspendLayout();
            flowLayoutPanel10.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // checkBoxShowPolyhedron
            // 
            resources.ApplyResources(checkBoxShowPolyhedron, "checkBoxShowPolyhedron");
            checkBoxShowPolyhedron.Checked = true;
            checkBoxShowPolyhedron.CheckState = System.Windows.Forms.CheckState.Checked;
            checkBoxShowPolyhedron.Name = "checkBoxShowPolyhedron";
            toolTip.SetToolTip(checkBoxShowPolyhedron, resources.GetString("checkBoxShowPolyhedron.ToolTip"));
            checkBoxShowPolyhedron.UseVisualStyleBackColor = true;
            checkBoxShowPolyhedron.CheckedChanged += checkBoxShowPolyhedron_CheckedChanged;
            // 
            // comboBoxBondingAtom1
            // 
            comboBoxBondingAtom1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(comboBoxBondingAtom1, "comboBoxBondingAtom1");
            comboBoxBondingAtom1.Name = "comboBoxBondingAtom1";
            toolTip.SetToolTip(comboBoxBondingAtom1, resources.GetString("comboBoxBondingAtom1.ToolTip"));
            // 
            // comboBoxBondingAtom2
            // 
            comboBoxBondingAtom2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(comboBoxBondingAtom2, "comboBoxBondingAtom2");
            comboBoxBondingAtom2.Items.AddRange(new object[] { resources.GetString("comboBoxBondingAtom2.Items") });
            comboBoxBondingAtom2.Name = "comboBoxBondingAtom2";
            toolTip.SetToolTip(comboBoxBondingAtom2, resources.GetString("comboBoxBondingAtom2.ToolTip"));
            // 
            // label58
            // 
            resources.ApplyResources(label58, "label58");
            label58.Name = "label58";
            toolTip.SetToolTip(label58, resources.GetString("label58.ToolTip"));
            // 
            // label57
            // 
            resources.ApplyResources(label57, "label57");
            label57.Name = "label57";
            toolTip.SetToolTip(label57, resources.GetString("label57.ToolTip"));
            // 
            // label39
            // 
            resources.ApplyResources(label39, "label39");
            label39.Name = "label39";
            toolTip.SetToolTip(label39, resources.GetString("label39.ToolTip"));
            // 
            // label40
            // 
            resources.ApplyResources(label40, "label40");
            label40.Name = "label40";
            toolTip.SetToolTip(label40, resources.GetString("label40.ToolTip"));
            // 
            // groupBoxPolyhedron
            // 
            groupBoxPolyhedron.Controls.Add(flowLayoutPanel3);
            resources.ApplyResources(groupBoxPolyhedron, "groupBoxPolyhedron");
            groupBoxPolyhedron.Name = "groupBoxPolyhedron";
            groupBoxPolyhedron.TabStop = false;
            // 
            // flowLayoutPanel3
            // 
            flowLayoutPanel3.Controls.Add(flowLayoutPanel1);
            flowLayoutPanel3.Controls.Add(flowLayoutPanel2);
            resources.ApplyResources(flowLayoutPanel3, "flowLayoutPanel3");
            flowLayoutPanel3.Name = "flowLayoutPanel3";
            // 
            // flowLayoutPanel1
            // 
            resources.ApplyResources(flowLayoutPanel1, "flowLayoutPanel1");
            flowLayoutPanel1.Controls.Add(checkBoxShowInnerBonds);
            flowLayoutPanel1.Controls.Add(checkBoxShowCenterAtom);
            flowLayoutPanel1.Controls.Add(checkBoxShowVertexAtoms);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            // 
            // checkBoxShowInnerBonds
            // 
            resources.ApplyResources(checkBoxShowInnerBonds, "checkBoxShowInnerBonds");
            checkBoxShowInnerBonds.Checked = true;
            checkBoxShowInnerBonds.CheckState = System.Windows.Forms.CheckState.Checked;
            checkBoxShowInnerBonds.Name = "checkBoxShowInnerBonds";
            toolTip.SetToolTip(checkBoxShowInnerBonds, resources.GetString("checkBoxShowInnerBonds.ToolTip"));
            checkBoxShowInnerBonds.UseVisualStyleBackColor = true;
            // 
            // checkBoxShowCenterAtom
            // 
            resources.ApplyResources(checkBoxShowCenterAtom, "checkBoxShowCenterAtom");
            checkBoxShowCenterAtom.Checked = true;
            checkBoxShowCenterAtom.CheckState = System.Windows.Forms.CheckState.Checked;
            checkBoxShowCenterAtom.Name = "checkBoxShowCenterAtom";
            toolTip.SetToolTip(checkBoxShowCenterAtom, resources.GetString("checkBoxShowCenterAtom.ToolTip"));
            checkBoxShowCenterAtom.UseVisualStyleBackColor = true;
            // 
            // checkBoxShowVertexAtoms
            // 
            resources.ApplyResources(checkBoxShowVertexAtoms, "checkBoxShowVertexAtoms");
            checkBoxShowVertexAtoms.Checked = true;
            checkBoxShowVertexAtoms.CheckState = System.Windows.Forms.CheckState.Checked;
            checkBoxShowVertexAtoms.Name = "checkBoxShowVertexAtoms";
            toolTip.SetToolTip(checkBoxShowVertexAtoms, resources.GetString("checkBoxShowVertexAtoms.ToolTip"));
            checkBoxShowVertexAtoms.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel2
            // 
            resources.ApplyResources(flowLayoutPanel2, "flowLayoutPanel2");
            flowLayoutPanel2.Controls.Add(numericBoxPolyhedronAlpha);
            flowLayoutPanel2.Controls.Add(checkBoxShowEdges);
            flowLayoutPanel2.Controls.Add(numericBoxEdgeWidth);
            flowLayoutPanel2.Name = "flowLayoutPanel2";
            // 
            // numericBoxPolyhedronAlpha
            // 
            numericBoxPolyhedronAlpha.BackColor = System.Drawing.SystemColors.Control;
            numericBoxPolyhedronAlpha.DecimalPlaces = 1;
            resources.ApplyResources(numericBoxPolyhedronAlpha, "numericBoxPolyhedronAlpha");
            numericBoxPolyhedronAlpha.Maximum = 1D;
            numericBoxPolyhedronAlpha.Minimum = 0D;
            numericBoxPolyhedronAlpha.Name = "numericBoxPolyhedronAlpha";
            numericBoxPolyhedronAlpha.RadianValue = 0.012217304763960306D;
            numericBoxPolyhedronAlpha.ShowUpDown = true;
            numericBoxPolyhedronAlpha.SkipEventDuringInput = false;
            numericBoxPolyhedronAlpha.SmartIncrement = true;
            numericBoxPolyhedronAlpha.ThousandsSeparator = true;
            toolTip.SetToolTip(numericBoxPolyhedronAlpha, resources.GetString("numericBoxPolyhedronAlpha.ToolTip"));
            numericBoxPolyhedronAlpha.UpDown_Increment = 0.1D;
            numericBoxPolyhedronAlpha.Value = 0.7D;
            numericBoxPolyhedronAlpha.ValueBoxWidth = 35;
            numericBoxPolyhedronAlpha.ValueFontSize = 9F;
            // 
            // checkBoxShowEdges
            // 
            resources.ApplyResources(checkBoxShowEdges, "checkBoxShowEdges");
            checkBoxShowEdges.Checked = true;
            checkBoxShowEdges.CheckState = System.Windows.Forms.CheckState.Checked;
            checkBoxShowEdges.Name = "checkBoxShowEdges";
            toolTip.SetToolTip(checkBoxShowEdges, resources.GetString("checkBoxShowEdges.ToolTip"));
            checkBoxShowEdges.UseVisualStyleBackColor = true;
            checkBoxShowEdges.CheckedChanged += checkBoxShowEdges_CheckedChanged;
            // 
            // numericBoxEdgeWidth
            // 
            numericBoxEdgeWidth.BackColor = System.Drawing.SystemColors.Control;
            numericBoxEdgeWidth.DecimalPlaces = 1;
            resources.ApplyResources(numericBoxEdgeWidth, "numericBoxEdgeWidth");
            numericBoxEdgeWidth.Maximum = 1D;
            numericBoxEdgeWidth.Minimum = 0D;
            numericBoxEdgeWidth.Name = "numericBoxEdgeWidth";
            numericBoxEdgeWidth.RadianValue = 0.012217304763960306D;
            numericBoxEdgeWidth.ShowUpDown = true;
            numericBoxEdgeWidth.SkipEventDuringInput = false;
            numericBoxEdgeWidth.SmartIncrement = true;
            numericBoxEdgeWidth.ThousandsSeparator = true;
            toolTip.SetToolTip(numericBoxEdgeWidth, resources.GetString("numericBoxEdgeWidth.ToolTip"));
            numericBoxEdgeWidth.UpDown_Increment = 0.1D;
            numericBoxEdgeWidth.Value = 0.7D;
            numericBoxEdgeWidth.ValueBoxWidth = 40;
            numericBoxEdgeWidth.ValueFontSize = 9F;
            // 
            // groupBoxBonds
            // 
            groupBoxBonds.Controls.Add(flowLayoutPanel8);
            groupBoxBonds.Controls.Add(flowLayoutPanel7);
            groupBoxBonds.Controls.Add(flowLayoutPanel4);
            resources.ApplyResources(groupBoxBonds, "groupBoxBonds");
            groupBoxBonds.Name = "groupBoxBonds";
            groupBoxBonds.TabStop = false;
            // 
            // flowLayoutPanel8
            // 
            resources.ApplyResources(flowLayoutPanel8, "flowLayoutPanel8");
            flowLayoutPanel8.Controls.Add(numericBoxBondRadius);
            flowLayoutPanel8.Controls.Add(numericBoxBondAlpha);
            flowLayoutPanel8.Name = "flowLayoutPanel8";
            // 
            // numericBoxBondRadius
            // 
            numericBoxBondRadius.BackColor = System.Drawing.SystemColors.Control;
            numericBoxBondRadius.DecimalPlaces = 3;
            resources.ApplyResources(numericBoxBondRadius, "numericBoxBondRadius");
            numericBoxBondRadius.Maximum = 9.9D;
            numericBoxBondRadius.Minimum = 0.1D;
            numericBoxBondRadius.Name = "numericBoxBondRadius";
            numericBoxBondRadius.RadianValue = 0.0017453292519943296D;
            numericBoxBondRadius.ShowUpDown = true;
            numericBoxBondRadius.SkipEventDuringInput = false;
            numericBoxBondRadius.SmartIncrement = true;
            numericBoxBondRadius.ThousandsSeparator = true;
            toolTip.SetToolTip(numericBoxBondRadius, resources.GetString("numericBoxBondRadius.ToolTip"));
            numericBoxBondRadius.UpDown_Increment = 0.02D;
            numericBoxBondRadius.Value = 0.1D;
            numericBoxBondRadius.ValueBoxWidth = 40;
            numericBoxBondRadius.ValueFontSize = 9F;
            // 
            // numericBoxBondAlpha
            // 
            numericBoxBondAlpha.BackColor = System.Drawing.SystemColors.Control;
            numericBoxBondAlpha.DecimalPlaces = 1;
            resources.ApplyResources(numericBoxBondAlpha, "numericBoxBondAlpha");
            numericBoxBondAlpha.Maximum = 1D;
            numericBoxBondAlpha.Minimum = 0D;
            numericBoxBondAlpha.Name = "numericBoxBondAlpha";
            numericBoxBondAlpha.RadianValue = 0.012217304763960306D;
            numericBoxBondAlpha.ShowUpDown = true;
            numericBoxBondAlpha.SkipEventDuringInput = false;
            numericBoxBondAlpha.SmartIncrement = true;
            numericBoxBondAlpha.ThousandsSeparator = true;
            toolTip.SetToolTip(numericBoxBondAlpha, resources.GetString("numericBoxBondAlpha.ToolTip"));
            numericBoxBondAlpha.UpDown_Increment = 0.1D;
            numericBoxBondAlpha.Value = 0.7D;
            numericBoxBondAlpha.ValueBoxWidth = 40;
            numericBoxBondAlpha.ValueFontSize = 9F;
            // 
            // flowLayoutPanel7
            // 
            resources.ApplyResources(flowLayoutPanel7, "flowLayoutPanel7");
            flowLayoutPanel7.Controls.Add(numericBoxBondMinLength);
            flowLayoutPanel7.Controls.Add(numericBoxBondMaxLength);
            flowLayoutPanel7.Name = "flowLayoutPanel7";
            // 
            // numericBoxBondMinLength
            // 
            numericBoxBondMinLength.BackColor = System.Drawing.SystemColors.Control;
            numericBoxBondMinLength.DecimalPlaces = 3;
            resources.ApplyResources(numericBoxBondMinLength, "numericBoxBondMinLength");
            numericBoxBondMinLength.Maximum = 9.9D;
            numericBoxBondMinLength.Minimum = 0D;
            numericBoxBondMinLength.Name = "numericBoxBondMinLength";
            numericBoxBondMinLength.RadianValue = 0.0017453292519943296D;
            numericBoxBondMinLength.ShowUpDown = true;
            numericBoxBondMinLength.SkipEventDuringInput = false;
            numericBoxBondMinLength.SmartIncrement = true;
            numericBoxBondMinLength.ThousandsSeparator = true;
            toolTip.SetToolTip(numericBoxBondMinLength, resources.GetString("numericBoxBondMinLength.ToolTip"));
            numericBoxBondMinLength.UpDown_Increment = 0.1D;
            numericBoxBondMinLength.Value = 0.1D;
            numericBoxBondMinLength.ValueBoxWidth = 45;
            numericBoxBondMinLength.ValueFontSize = 9F;
            // 
            // numericBoxBondMaxLength
            // 
            numericBoxBondMaxLength.BackColor = System.Drawing.SystemColors.Control;
            numericBoxBondMaxLength.DecimalPlaces = 3;
            resources.ApplyResources(numericBoxBondMaxLength, "numericBoxBondMaxLength");
            numericBoxBondMaxLength.Maximum = 9.9D;
            numericBoxBondMaxLength.Minimum = 0.1D;
            numericBoxBondMaxLength.Name = "numericBoxBondMaxLength";
            numericBoxBondMaxLength.RadianValue = 0.027925268031909273D;
            numericBoxBondMaxLength.ShowUpDown = true;
            numericBoxBondMaxLength.SkipEventDuringInput = false;
            numericBoxBondMaxLength.SmartIncrement = true;
            numericBoxBondMaxLength.ThousandsSeparator = true;
            toolTip.SetToolTip(numericBoxBondMaxLength, resources.GetString("numericBoxBondMaxLength.ToolTip"));
            numericBoxBondMaxLength.UpDown_Increment = 0.1D;
            numericBoxBondMaxLength.Value = 1.6D;
            numericBoxBondMaxLength.ValueBoxWidth = 45;
            numericBoxBondMaxLength.ValueFontSize = 9F;
            // 
            // flowLayoutPanel4
            // 
            resources.ApplyResources(flowLayoutPanel4, "flowLayoutPanel4");
            flowLayoutPanel4.Controls.Add(label39);
            flowLayoutPanel4.Controls.Add(flowLayoutPanel5);
            flowLayoutPanel4.Controls.Add(label40);
            flowLayoutPanel4.Controls.Add(flowLayoutPanel6);
            flowLayoutPanel4.Name = "flowLayoutPanel4";
            // 
            // flowLayoutPanel5
            // 
            resources.ApplyResources(flowLayoutPanel5, "flowLayoutPanel5");
            flowLayoutPanel5.Controls.Add(label57);
            flowLayoutPanel5.Controls.Add(comboBoxBondingAtom1);
            flowLayoutPanel5.Name = "flowLayoutPanel5";
            // 
            // flowLayoutPanel6
            // 
            resources.ApplyResources(flowLayoutPanel6, "flowLayoutPanel6");
            flowLayoutPanel6.Controls.Add(label58);
            flowLayoutPanel6.Controls.Add(comboBoxBondingAtom2);
            flowLayoutPanel6.Name = "flowLayoutPanel6";
            // 
            // checkBoxShowBonds
            // 
            resources.ApplyResources(checkBoxShowBonds, "checkBoxShowBonds");
            checkBoxShowBonds.Checked = true;
            checkBoxShowBonds.CheckState = System.Windows.Forms.CheckState.Checked;
            checkBoxShowBonds.Name = "checkBoxShowBonds";
            toolTip.SetToolTip(checkBoxShowBonds, resources.GetString("checkBoxShowBonds.ToolTip"));
            checkBoxShowBonds.UseVisualStyleBackColor = true;
            checkBoxShowBonds.CheckedChanged += checkBoxShowBonds_CheckedChanged;
            // 
            // buttonAddBond
            // 
            buttonAddBond.BackColor = System.Drawing.Color.SteelBlue;
            resources.ApplyResources(buttonAddBond, "buttonAddBond");
            buttonAddBond.ForeColor = System.Drawing.Color.White;
            buttonAddBond.Name = "buttonAddBond";
            toolTip.SetToolTip(buttonAddBond, resources.GetString("buttonAddBond.ToolTip"));
            buttonAddBond.UseVisualStyleBackColor = false;
            buttonAddBond.Click += buttonAdd_Click;
            // 
            // buttonChangeBond
            // 
            buttonChangeBond.BackColor = System.Drawing.Color.SteelBlue;
            resources.ApplyResources(buttonChangeBond, "buttonChangeBond");
            buttonChangeBond.ForeColor = System.Drawing.Color.White;
            buttonChangeBond.Name = "buttonChangeBond";
            toolTip.SetToolTip(buttonChangeBond, resources.GetString("buttonChangeBond.ToolTip"));
            buttonChangeBond.UseVisualStyleBackColor = false;
            buttonChangeBond.Click += buttonChange_Click;
            // 
            // buttonDeleteBond
            // 
            resources.ApplyResources(buttonDeleteBond, "buttonDeleteBond");
            buttonDeleteBond.BackColor = System.Drawing.Color.IndianRed;
            buttonDeleteBond.ForeColor = System.Drawing.Color.White;
            buttonDeleteBond.Name = "buttonDeleteBond";
            toolTip.SetToolTip(buttonDeleteBond, resources.GetString("buttonDeleteBond.ToolTip"));
            buttonDeleteBond.UseVisualStyleBackColor = false;
            buttonDeleteBond.Click += buttonDelete_Click;
            // 
            // dataGridView
            // 
            dataGridView.AllowUserToAddRows = false;
            dataGridView.AllowUserToDeleteRows = false;
            dataGridView.AllowUserToResizeColumns = false;
            dataGridView.AllowUserToResizeRows = false;
            dataGridView.AutoGenerateColumns = false;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { enabledDataGridViewCheckBoxColumn1, centerDataGridViewTextBoxColumn1, vertexDataGridViewTextBoxColumn1, minLenDataGridViewTextBoxColumn1, maxLenDataGridViewTextBoxColumn1, showBondsDataGridViewCheckBoxColumn, showPolyhedronDataGridViewCheckBoxColumn });
            dataGridView.DataSource = bindingSource;
            resources.ApplyResources(dataGridView, "dataGridView");
            dataGridView.MultiSelect = false;
            dataGridView.Name = "dataGridView";
            dataGridView.RowHeadersVisible = false;
            dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            toolTip.SetToolTip(dataGridView, resources.GetString("dataGridView.ToolTip"));
            dataGridView.CellValueChanged += dataGridView_CellValueChanged;
            dataGridView.CurrentCellDirtyStateChanged += dataGridView_CurrentCellDirtyStateChanged;
            // 
            // enabledDataGridViewCheckBoxColumn1
            // 
            enabledDataGridViewCheckBoxColumn1.DataPropertyName = "Enabled";
            resources.ApplyResources(enabledDataGridViewCheckBoxColumn1, "enabledDataGridViewCheckBoxColumn1");
            enabledDataGridViewCheckBoxColumn1.Name = "enabledDataGridViewCheckBoxColumn1";
            // 
            // centerDataGridViewTextBoxColumn1
            // 
            centerDataGridViewTextBoxColumn1.DataPropertyName = "Center";
            resources.ApplyResources(centerDataGridViewTextBoxColumn1, "centerDataGridViewTextBoxColumn1");
            centerDataGridViewTextBoxColumn1.Name = "centerDataGridViewTextBoxColumn1";
            centerDataGridViewTextBoxColumn1.ReadOnly = true;
            centerDataGridViewTextBoxColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // vertexDataGridViewTextBoxColumn1
            // 
            vertexDataGridViewTextBoxColumn1.DataPropertyName = "Vertex";
            resources.ApplyResources(vertexDataGridViewTextBoxColumn1, "vertexDataGridViewTextBoxColumn1");
            vertexDataGridViewTextBoxColumn1.Name = "vertexDataGridViewTextBoxColumn1";
            vertexDataGridViewTextBoxColumn1.ReadOnly = true;
            vertexDataGridViewTextBoxColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // minLenDataGridViewTextBoxColumn1
            // 
            minLenDataGridViewTextBoxColumn1.DataPropertyName = "Min len.";
            resources.ApplyResources(minLenDataGridViewTextBoxColumn1, "minLenDataGridViewTextBoxColumn1");
            minLenDataGridViewTextBoxColumn1.Name = "minLenDataGridViewTextBoxColumn1";
            minLenDataGridViewTextBoxColumn1.ReadOnly = true;
            minLenDataGridViewTextBoxColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // maxLenDataGridViewTextBoxColumn1
            // 
            maxLenDataGridViewTextBoxColumn1.DataPropertyName = "Max len.";
            resources.ApplyResources(maxLenDataGridViewTextBoxColumn1, "maxLenDataGridViewTextBoxColumn1");
            maxLenDataGridViewTextBoxColumn1.Name = "maxLenDataGridViewTextBoxColumn1";
            maxLenDataGridViewTextBoxColumn1.ReadOnly = true;
            maxLenDataGridViewTextBoxColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // showBondsDataGridViewCheckBoxColumn
            // 
            showBondsDataGridViewCheckBoxColumn.DataPropertyName = "Show bonds";
            resources.ApplyResources(showBondsDataGridViewCheckBoxColumn, "showBondsDataGridViewCheckBoxColumn");
            showBondsDataGridViewCheckBoxColumn.Name = "showBondsDataGridViewCheckBoxColumn";
            // 
            // showPolyhedronDataGridViewCheckBoxColumn
            // 
            showPolyhedronDataGridViewCheckBoxColumn.DataPropertyName = "Show Polyhedron";
            resources.ApplyResources(showPolyhedronDataGridViewCheckBoxColumn, "showPolyhedronDataGridViewCheckBoxColumn");
            showPolyhedronDataGridViewCheckBoxColumn.Name = "showPolyhedronDataGridViewCheckBoxColumn";
            // 
            // bindingSource
            // 
            bindingSource.DataMember = "DataTableBond";
            bindingSource.DataSource = dataSet;
            bindingSource.CurrentChanged += bindingSource_PositionChanged;
            bindingSource.PositionChanged += bindingSource_PositionChanged;
            // 
            // dataSet
            // 
            dataSet.DataSetName = "DataSet";
            dataSet.Namespace = "http://tempuri.org/DataSet1.xsd";
            dataSet.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema;
            // 
            // panel1
            // 
            resources.ApplyResources(panel1, "panel1");
            panel1.Controls.Add(buttonDeleteBond);
            panel1.Controls.Add(buttonChangeBond);
            panel1.Controls.Add(buttonAddBond);
            panel1.Controls.Add(flowLayoutPanel10);
            panel1.Name = "panel1";
            // 
            // flowLayoutPanel10
            // 
            resources.ApplyResources(flowLayoutPanel10, "flowLayoutPanel10");
            flowLayoutPanel10.Controls.Add(colorControlPolyhedron);
            flowLayoutPanel10.Controls.Add(colorControlBond);
            flowLayoutPanel10.Controls.Add(colorControlEdges);
            flowLayoutPanel10.Name = "flowLayoutPanel10";
            // 
            // colorControlPolyhedron
            // 
            resources.ApplyResources(colorControlPolyhedron, "colorControlPolyhedron");
            colorControlPolyhedron.BackColor = System.Drawing.SystemColors.Control;
            colorControlPolyhedron.BoxSize = new System.Drawing.Size(20, 20);
            colorControlPolyhedron.Name = "colorControlPolyhedron";
            // 
            // colorControlBond
            // 
            resources.ApplyResources(colorControlBond, "colorControlBond");
            colorControlBond.BackColor = System.Drawing.SystemColors.Control;
            colorControlBond.BoxSize = new System.Drawing.Size(20, 20);
            colorControlBond.Name = "colorControlBond";
            // 
            // colorControlEdges
            // 
            resources.ApplyResources(colorControlEdges, "colorControlEdges");
            colorControlEdges.BackColor = System.Drawing.SystemColors.Control;
            colorControlEdges.BoxSize = new System.Drawing.Size(20, 20);
            colorControlEdges.Name = "colorControlEdges";
            // 
            // panel2
            // 
            panel2.Controls.Add(checkBoxShowBonds);
            panel2.Controls.Add(groupBoxBonds);
            panel2.Controls.Add(checkBoxShowPolyhedron);
            panel2.Controls.Add(groupBoxPolyhedron);
            resources.ApplyResources(panel2, "panel2");
            panel2.Name = "panel2";
            // 
            // toolTip
            // 
            toolTip.AutoPopDelay = 10000;
            toolTip.InitialDelay = 500;
            toolTip.IsBalloon = true;
            toolTip.ReshowDelay = 100;
            // 
            // BondInputControl
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            Controls.Add(dataGridView);
            Controls.Add(panel1);
            Controls.Add(panel2);
            Name = "BondInputControl";
            groupBoxPolyhedron.ResumeLayout(false);
            flowLayoutPanel3.ResumeLayout(false);
            flowLayoutPanel3.PerformLayout();
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.PerformLayout();
            flowLayoutPanel2.ResumeLayout(false);
            flowLayoutPanel2.PerformLayout();
            groupBoxBonds.ResumeLayout(false);
            groupBoxBonds.PerformLayout();
            flowLayoutPanel8.ResumeLayout(false);
            flowLayoutPanel8.PerformLayout();
            flowLayoutPanel7.ResumeLayout(false);
            flowLayoutPanel7.PerformLayout();
            flowLayoutPanel4.ResumeLayout(false);
            flowLayoutPanel4.PerformLayout();
            flowLayoutPanel5.ResumeLayout(false);
            flowLayoutPanel5.PerformLayout();
            flowLayoutPanel6.ResumeLayout(false);
            flowLayoutPanel6.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView).EndInit();
            ((System.ComponentModel.ISupportInitialize)bindingSource).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataSet).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            flowLayoutPanel10.ResumeLayout(false);
            flowLayoutPanel10.PerformLayout();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private ColorControl colorControlBond;
        private System.Windows.Forms.CheckBox checkBoxShowPolyhedron;
        private System.Windows.Forms.ComboBox comboBoxBondingAtom1;
        private System.Windows.Forms.ComboBox comboBoxBondingAtom2;
        private System.Windows.Forms.Label label58;
        private System.Windows.Forms.Label label57;
        private System.Windows.Forms.Label label39;
        private System.Windows.Forms.Label label40;
        private System.Windows.Forms.GroupBox groupBoxPolyhedron;
        private ColorControl colorControlPolyhedron;
        private System.Windows.Forms.CheckBox checkBoxShowEdges;
        private ColorControl colorControlEdges;
        private System.Windows.Forms.CheckBox checkBoxShowInnerBonds;
        private System.Windows.Forms.CheckBox checkBoxShowVertexAtoms;
        private System.Windows.Forms.CheckBox checkBoxShowCenterAtom;
        private NumericBox numericBoxPolyhedronAlpha;
        private NumericBox numericBoxEdgeWidth;
        private NumericBox numericBoxBondMinLength;
        private NumericBox numericBoxBondMaxLength;
        private NumericBox numericBoxBondRadius;
        private NumericBox numericBoxBondAlpha;
        private System.Windows.Forms.GroupBox groupBoxBonds;
        private System.Windows.Forms.Button buttonAddBond;
        private System.Windows.Forms.Button buttonChangeBond;
        private System.Windows.Forms.Button buttonDeleteBond;
        private DataSet dataSet;
        private System.Windows.Forms.BindingSource bindingSource;
        // private System.Windows.Forms.DataGridView dataGridView; // 260518Cl 旧実装
        private DpiAwareDataGridView dataGridView; // 260518Cl
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.CheckBox checkBoxShowBonds;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel7;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel4;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel5;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel6;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel8;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel10;
        private System.Windows.Forms.DataGridViewCheckBoxColumn enabledDataGridViewCheckBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn centerDataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn vertexDataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn minLenDataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn maxLenDataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewCheckBoxColumn showBondsDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn showPolyhedronDataGridViewCheckBoxColumn;
    }
}
