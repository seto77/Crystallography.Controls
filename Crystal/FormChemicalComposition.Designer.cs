namespace Crystallography.Controls
{
    partial class FormChemicalComposition
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
            if (disposing && (components != null))
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
            components = new System.ComponentModel.Container();
            miniTable1 = new MiniTable();
            flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            flowLayoutPanelOrder = new System.Windows.Forms.FlowLayoutPanel();
            radioButtonInputOrder = new System.Windows.Forms.RadioButton();
            radioButtonAtomicNumberOrder = new System.Windows.Forms.RadioButton();
            toolTip = new System.Windows.Forms.ToolTip(components);
            flowLayoutPanelMode = new System.Windows.Forms.FlowLayoutPanel();
            radioButtonElements = new System.Windows.Forms.RadioButton();
            radioButtonOxides = new System.Windows.Forms.RadioButton();
            radioButtonHalides = new System.Windows.Forms.RadioButton();
            radioButtonSulfides = new System.Windows.Forms.RadioButton();
            panelResult = new System.Windows.Forms.Panel();
            panelValence = new System.Windows.Forms.Panel();
            tableLayoutPanelValence = new System.Windows.Forms.TableLayoutPanel();
            labelValenceElement = new System.Windows.Forms.Label();
            labelValenceAtoms = new System.Windows.Forms.Label();
            labelValence = new System.Windows.Forms.Label();
            labelValenceNormalized = new System.Windows.Forms.Label();
            labelValenceTotal = new System.Windows.Forms.Label();
            labelValenceAtomsTotal = new System.Windows.Forms.Label();
            //numericBoxTotalValence = new NumericBox(); // 261026Cl 変更前 (合計の電荷は Label にした。作者指示)
            labelTotalValence = new System.Windows.Forms.Label();
            buttonResetValences = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)miniTable1).BeginInit();
            flowLayoutPanel1.SuspendLayout();
            flowLayoutPanelOrder.SuspendLayout();
            flowLayoutPanelMode.SuspendLayout();
            panelResult.SuspendLayout();
            panelValence.SuspendLayout();
            tableLayoutPanelValence.SuspendLayout();
            SuspendLayout();
            // 
            // miniTable1
            // 
            miniTable1.AllowVerticalScroll = true;
            miniTable1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            miniTable1.Dock = System.Windows.Forms.DockStyle.Fill;
            miniTable1.Location = new System.Drawing.Point(0, 25);
            miniTable1.Name = "miniTable1";
            miniTable1.Size = new System.Drawing.Size(382, 336);
            miniTable1.TabIndex = 1;
            miniTable1.TabStop = false;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.AutoSize = true;
            flowLayoutPanel1.Controls.Add(flowLayoutPanelOrder);
            flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            flowLayoutPanel1.Location = new System.Drawing.Point(4, 4);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new System.Drawing.Size(682, 31);
            flowLayoutPanel1.TabIndex = 1;
            // 
            // flowLayoutPanelOrder
            // 
            flowLayoutPanelOrder.Anchor = System.Windows.Forms.AnchorStyles.Left;
            flowLayoutPanelOrder.AutoSize = true;
            flowLayoutPanelOrder.Controls.Add(radioButtonInputOrder);
            flowLayoutPanelOrder.Controls.Add(radioButtonAtomicNumberOrder);
            flowLayoutPanelOrder.Location = new System.Drawing.Point(3, 3);
            flowLayoutPanelOrder.Margin = new System.Windows.Forms.Padding(3, 3, 12, 3);
            flowLayoutPanelOrder.Name = "flowLayoutPanelOrder";
            flowLayoutPanelOrder.Size = new System.Drawing.Size(203, 25);
            flowLayoutPanelOrder.TabIndex = 0;
            flowLayoutPanelOrder.WrapContents = false;
            // 
            // radioButtonInputOrder
            // 
            radioButtonInputOrder.AutoSize = true;
            radioButtonInputOrder.Checked = true;
            radioButtonInputOrder.Location = new System.Drawing.Point(3, 3);
            radioButtonInputOrder.Name = "radioButtonInputOrder";
            radioButtonInputOrder.Size = new System.Drawing.Size(84, 19);
            radioButtonInputOrder.TabIndex = 0;
            radioButtonInputOrder.TabStop = true;
            radioButtonInputOrder.Text = "Input order";
            radioButtonInputOrder.UseVisualStyleBackColor = true;
            radioButtonInputOrder.CheckedChanged += radioButtonOrder_CheckedChanged;
            // 
            // radioButtonAtomicNumberOrder
            // 
            radioButtonAtomicNumberOrder.AutoSize = true;
            radioButtonAtomicNumberOrder.Location = new System.Drawing.Point(93, 3);
            radioButtonAtomicNumberOrder.Name = "radioButtonAtomicNumberOrder";
            radioButtonAtomicNumberOrder.Size = new System.Drawing.Size(107, 19);
            radioButtonAtomicNumberOrder.TabIndex = 1;
            radioButtonAtomicNumberOrder.Text = "Atomic number";
            radioButtonAtomicNumberOrder.UseVisualStyleBackColor = true;
            radioButtonAtomicNumberOrder.CheckedChanged += radioButtonOrder_CheckedChanged;
            // 
            // toolTip
            // 
            toolTip.AutoPopDelay = 10000;
            toolTip.InitialDelay = 500;
            toolTip.IsBalloon = true;
            toolTip.ReshowDelay = 100;
            // 
            // flowLayoutPanelMode
            // 
            flowLayoutPanelMode.AutoSize = true;
            flowLayoutPanelMode.Controls.Add(radioButtonElements);
            flowLayoutPanelMode.Controls.Add(radioButtonOxides);
            flowLayoutPanelMode.Controls.Add(radioButtonHalides);
            flowLayoutPanelMode.Controls.Add(radioButtonSulfides);
            flowLayoutPanelMode.Dock = System.Windows.Forms.DockStyle.Top;
            flowLayoutPanelMode.Location = new System.Drawing.Point(0, 0);
            flowLayoutPanelMode.Name = "flowLayoutPanelMode";
            flowLayoutPanelMode.Size = new System.Drawing.Size(382, 25);
            flowLayoutPanelMode.TabIndex = 0;
            // 
            // radioButtonElements
            // 
            radioButtonElements.AutoSize = true;
            radioButtonElements.Checked = true;
            radioButtonElements.Location = new System.Drawing.Point(3, 3);
            radioButtonElements.Name = "radioButtonElements";
            radioButtonElements.Size = new System.Drawing.Size(72, 19);
            radioButtonElements.TabIndex = 0;
            radioButtonElements.TabStop = true;
            radioButtonElements.Text = "Elements";
            radioButtonElements.UseVisualStyleBackColor = true;
            radioButtonElements.CheckedChanged += radioButtonMode_CheckedChanged;
            // 
            // radioButtonOxides
            // 
            radioButtonOxides.AutoSize = true;
            radioButtonOxides.Location = new System.Drawing.Point(81, 3);
            radioButtonOxides.Name = "radioButtonOxides";
            radioButtonOxides.Size = new System.Drawing.Size(61, 19);
            radioButtonOxides.TabIndex = 1;
            radioButtonOxides.Text = "Oxides";
            radioButtonOxides.UseVisualStyleBackColor = true;
            radioButtonOxides.CheckedChanged += radioButtonMode_CheckedChanged;
            // 
            // radioButtonHalides
            // 
            radioButtonHalides.AutoSize = true;
            radioButtonHalides.Location = new System.Drawing.Point(148, 3);
            radioButtonHalides.Name = "radioButtonHalides";
            radioButtonHalides.Size = new System.Drawing.Size(64, 19);
            radioButtonHalides.TabIndex = 2;
            radioButtonHalides.Text = "Halides";
            radioButtonHalides.UseVisualStyleBackColor = true;
            radioButtonHalides.CheckedChanged += radioButtonMode_CheckedChanged;
            // 
            // radioButtonSulfides
            // 
            radioButtonSulfides.AutoSize = true;
            radioButtonSulfides.Location = new System.Drawing.Point(218, 3);
            radioButtonSulfides.Name = "radioButtonSulfides";
            radioButtonSulfides.Size = new System.Drawing.Size(65, 19);
            radioButtonSulfides.TabIndex = 3;
            radioButtonSulfides.Text = "Sulfides";
            radioButtonSulfides.UseVisualStyleBackColor = true;
            radioButtonSulfides.CheckedChanged += radioButtonMode_CheckedChanged;
            // 
            // panelResult
            // 
            panelResult.Controls.Add(miniTable1);
            panelResult.Controls.Add(flowLayoutPanelMode);
            panelResult.Dock = System.Windows.Forms.DockStyle.Fill;
            panelResult.Location = new System.Drawing.Point(304, 35);
            panelResult.Name = "panelResult";
            panelResult.Size = new System.Drawing.Size(382, 361);
            panelResult.TabIndex = 3;
            // 
            // panelValence
            // 
            panelValence.AutoScroll = true;
            panelValence.Controls.Add(tableLayoutPanelValence);
            panelValence.Dock = System.Windows.Forms.DockStyle.Left;
            panelValence.Location = new System.Drawing.Point(4, 35);
            panelValence.Name = "panelValence";
            panelValence.Size = new System.Drawing.Size(300, 361);
            panelValence.TabIndex = 2;
            // 
            // tableLayoutPanelValence
            // 
            tableLayoutPanelValence.AutoSize = true;
            tableLayoutPanelValence.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            tableLayoutPanelValence.ColumnCount = 5;
            tableLayoutPanelValence.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            tableLayoutPanelValence.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            tableLayoutPanelValence.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            tableLayoutPanelValence.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            tableLayoutPanelValence.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            tableLayoutPanelValence.Controls.Add(labelValenceElement, 0, 0);
            tableLayoutPanelValence.Controls.Add(labelValenceAtoms, 1, 0);
            tableLayoutPanelValence.Controls.Add(labelValence, 2, 0);
            tableLayoutPanelValence.Controls.Add(labelValenceNormalized, 4, 0);
            tableLayoutPanelValence.Controls.Add(labelValenceTotal, 0, 1);
            tableLayoutPanelValence.Controls.Add(labelValenceAtomsTotal, 1, 1);
            //tableLayoutPanelValence.Controls.Add(numericBoxTotalValence, 2, 1); // 261026Cl 変更前 (合計の電荷は Label にした。作者指示)
            tableLayoutPanelValence.Controls.Add(labelTotalValence, 2, 1);
            tableLayoutPanelValence.Controls.Add(buttonResetValences, 0, 2);
            tableLayoutPanelValence.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanelValence.Name = "tableLayoutPanelValence";
            tableLayoutPanelValence.RowCount = 3;
            tableLayoutPanelValence.RowStyles.Add(new System.Windows.Forms.RowStyle());
            tableLayoutPanelValence.RowStyles.Add(new System.Windows.Forms.RowStyle());
            tableLayoutPanelValence.RowStyles.Add(new System.Windows.Forms.RowStyle());
            tableLayoutPanelValence.Size = new System.Drawing.Size(310, 75);
            tableLayoutPanelValence.TabIndex = 0;
            // 
            // labelValenceElement
            // 
            labelValenceElement.Anchor = System.Windows.Forms.AnchorStyles.Left;
            labelValenceElement.AutoSize = true;
            labelValenceElement.Location = new System.Drawing.Point(3, 0);
            labelValenceElement.Name = "labelValenceElement";
            labelValenceElement.Size = new System.Drawing.Size(49, 15);
            labelValenceElement.TabIndex = 0;
            labelValenceElement.Text = "Element";
            // 
            // labelValenceAtoms
            // 
            labelValenceAtoms.Anchor = System.Windows.Forms.AnchorStyles.Left;
            labelValenceAtoms.AutoSize = true;
            labelValenceAtoms.Location = new System.Drawing.Point(58, 0);
            labelValenceAtoms.Name = "labelValenceAtoms";
            labelValenceAtoms.Size = new System.Drawing.Size(70, 15);
            labelValenceAtoms.TabIndex = 1;
            labelValenceAtoms.Text = "Atoms / cell";
            // 
            // labelValence
            // 
            labelValence.Anchor = System.Windows.Forms.AnchorStyles.Left;
            labelValence.AutoSize = true;
            labelValence.Location = new System.Drawing.Point(134, 0);
            labelValence.Name = "labelValence";
            labelValence.Size = new System.Drawing.Size(47, 15);
            labelValence.TabIndex = 2;
            labelValence.Text = "Valence";
            // 
            // labelValenceNormalized
            // 
            labelValenceNormalized.Anchor = System.Windows.Forms.AnchorStyles.Left;
            labelValenceNormalized.AutoSize = true;
            labelValenceNormalized.Location = new System.Drawing.Point(197, 0);
            labelValenceNormalized.Name = "labelValenceNormalized";
            labelValenceNormalized.Size = new System.Drawing.Size(110, 15);
            labelValenceNormalized.TabIndex = 3;
            labelValenceNormalized.Text = "Atoms (normalized)";
            // 
            // labelValenceTotal
            // 
            labelValenceTotal.Anchor = System.Windows.Forms.AnchorStyles.Left;
            labelValenceTotal.AutoSize = true;
            labelValenceTotal.Location = new System.Drawing.Point(3, 22);
            labelValenceTotal.Name = "labelValenceTotal";
            labelValenceTotal.Size = new System.Drawing.Size(13, 15);
            labelValenceTotal.TabIndex = 4;
            labelValenceTotal.Text = "Σ";
            // 
            // labelValenceAtomsTotal
            // 
            labelValenceAtomsTotal.Anchor = System.Windows.Forms.AnchorStyles.Left;
            labelValenceAtomsTotal.AutoSize = true;
            labelValenceAtomsTotal.Location = new System.Drawing.Point(115, 22);
            labelValenceAtomsTotal.Name = "labelValenceAtomsTotal";
            labelValenceAtomsTotal.Size = new System.Drawing.Size(13, 15);
            labelValenceAtomsTotal.TabIndex = 5;
            labelValenceAtomsTotal.Text = "0";
            ////  // 261026Cl 変更前 (合計の電荷は Label にした。作者指示)
            //// numericBoxTotalValence
            //// 
            //numericBoxTotalValence.Anchor = System.Windows.Forms.AnchorStyles.Right;
            //numericBoxTotalValence.BackColor = System.Drawing.Color.Transparent;
            //numericBoxTotalValence.DecimalPlaces = 4;
            //numericBoxTotalValence.FooterPadding = new System.Windows.Forms.Padding(0, 4, 0, 0);
            //numericBoxTotalValence.HeaderPadding = new System.Windows.Forms.Padding(0, 4, 0, 0);
            //numericBoxTotalValence.Location = new System.Drawing.Point(134, 16);
            //numericBoxTotalValence.Margin = new System.Windows.Forms.Padding(3, 1, 0, 1);
            //numericBoxTotalValence.MaximumSize = new System.Drawing.Size(1000, 100);
            //numericBoxTotalValence.MinimumSize = new System.Drawing.Size(10, 20);
            //numericBoxTotalValence.Name = "numericBoxTotalValence";
            //numericBoxTotalValence.NaNText = "?";
            //numericBoxTotalValence.ReadOnly = true;
            //numericBoxTotalValence.Size = new System.Drawing.Size(60, 27);
            //numericBoxTotalValence.TabIndex = 6;
            //numericBoxTotalValence.TabStop = false;
            //numericBoxTotalValence.TrimEndZero = true;
            //numericBoxTotalValence.ValueBackColor = System.Drawing.SystemColors.Control;
            //numericBoxTotalValence.ValueBoxWidth = 60;
            // 
            // labelTotalValence (261026Cl 追加: 合計の行の全体の電荷。表示専用なので Label (作者指示)。未設定の価数があれば ?)
            // 
            labelTotalValence.Anchor = System.Windows.Forms.AnchorStyles.Left;
            labelTotalValence.AutoSize = true;
            labelTotalValence.Location = new System.Drawing.Point(134, 22);
            labelTotalValence.Margin = new System.Windows.Forms.Padding(6, 1, 0, 1);
            labelTotalValence.Name = "labelTotalValence";
            labelTotalValence.Size = new System.Drawing.Size(13, 15);
            labelTotalValence.TabIndex = 6;
            labelTotalValence.Text = "0";
            // 
            // buttonResetValences
            // 
            buttonResetValences.Anchor = System.Windows.Forms.AnchorStyles.Left;
            buttonResetValences.AutoSize = true;
            tableLayoutPanelValence.SetColumnSpan(buttonResetValences, 3);
            buttonResetValences.Location = new System.Drawing.Point(3, 47);
            buttonResetValences.Name = "buttonResetValences";
            buttonResetValences.Size = new System.Drawing.Size(75, 25);
            buttonResetValences.TabIndex = 7;
            buttonResetValences.Text = "Default";
            buttonResetValences.UseVisualStyleBackColor = true;
            buttonResetValences.Click += buttonResetValences_Click;
            // 
            // FormChemicalComposition
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(560, 360);
            Controls.Add(panelResult);
            Controls.Add(panelValence);
            Controls.Add(flowLayoutPanel1);
            Name = "FormChemicalComposition";
            Padding = new System.Windows.Forms.Padding(4);
            Text = "Details of the chemical composition";
            FormClosing += FormChemicalComposition_FormClosing;
            Load += FormChemicalComposition_Load;
            VisibleChanged += FormChemicalComposition_VisibleChanged;
            ((System.ComponentModel.ISupportInitialize)miniTable1).EndInit();
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.PerformLayout();
            flowLayoutPanelOrder.ResumeLayout(false);
            flowLayoutPanelOrder.PerformLayout();
            flowLayoutPanelMode.ResumeLayout(false);
            flowLayoutPanelMode.PerformLayout();
            panelResult.ResumeLayout(false);
            panelResult.PerformLayout();
            panelValence.ResumeLayout(false);
            panelValence.PerformLayout();
            tableLayoutPanelValence.ResumeLayout(false);
            tableLayoutPanelValence.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MiniTable miniTable1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        //private System.Windows.Forms.Label labelNormalize; // 261024Cl 追加 // 261025Cl 削除: 上部の規格化の欄は廃止 (作者指示。価数の表の「規格化した原子数」の列で指定する)
        //private System.Windows.Forms.ComboBox comboBoxNormalizeElement; // 261024Cl 追加
        //private NumericBox numericBoxNormalizeValue; // 261024Cl 追加
        private System.Windows.Forms.ToolTip toolTip; // 261024Cl 追加
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelMode; // 261025Cl 追加
        private System.Windows.Forms.Panel panelResult; // 261026Cl 追加
        private System.Windows.Forms.RadioButton radioButtonElements; // 261025Cl 追加
        private System.Windows.Forms.RadioButton radioButtonOxides; // 261025Cl 追加
        private System.Windows.Forms.RadioButton radioButtonHalides; // 261025Cl 追加
        private System.Windows.Forms.RadioButton radioButtonSulfides; // 261025Cl 追加
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelOrder; // 261025Cl 追加
        private System.Windows.Forms.RadioButton radioButtonInputOrder; // 261025Cl 追加
        private System.Windows.Forms.RadioButton radioButtonAtomicNumberOrder; // 261025Cl 追加
        private System.Windows.Forms.Panel panelValence; // 261025Cl 追加
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelValence; // 261025Cl 追加
        private System.Windows.Forms.Label labelValenceElement; // 261025Cl 追加
        private System.Windows.Forms.Label labelValenceAtoms; // 261025Cl 追加
        private System.Windows.Forms.Label labelValence; // 261025Cl 追加
        private System.Windows.Forms.Label labelValenceNormalized; // 261025Cl 追加
        private System.Windows.Forms.Label labelValenceTotal; // 261025Cl 追加
        private System.Windows.Forms.Label labelValenceAtomsTotal; // 261025Cl 追加
        private System.Windows.Forms.Button buttonResetValences; // 261025Cl 追加
        //private NumericBox numericBoxTotalValence; // 261025Cl 追加 // 261026Cl 変更前 (合計の電荷は Label にした。作者指示)
        private System.Windows.Forms.Label labelTotalValence; // 261026Cl 追加
    }
}