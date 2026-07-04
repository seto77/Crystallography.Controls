namespace Crystallography.Controls
{
    partial class SymmetryControl
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
        // (260323Ch) renamed numeric container controls:
        // groupBox4 -> groupBoxCellConstants
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SymmetryControl));
            groupBoxCellConstants = new System.Windows.Forms.GroupBox();
            radioButtonNanoMeter = new System.Windows.Forms.RadioButton();
            radioButtonAngstrom = new System.Windows.Forms.RadioButton();
            label1 = new System.Windows.Forms.Label();
            checkBoxShowError = new System.Windows.Forms.CheckBox();
            tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            labelLaTex1 = new LabelLaTeX();
            label48 = new System.Windows.Forms.Label();
            label46 = new System.Windows.Forms.Label();
            numericBoxBeta = new NumericBox();
            numericBoxAlpha = new NumericBox();
            label47 = new System.Windows.Forms.Label();
            numericBoxGammaErr = new NumericBox();
            numericBoxAlphaErr = new NumericBox();
            numericBoxBetaErr = new NumericBox();
            numericBoxA = new NumericBox();
            labelLengthUnitC = new System.Windows.Forms.Label();
            numericBoxGamma = new NumericBox();
            labelLengthUnitB = new System.Windows.Forms.Label();
            numericBoxBErr = new NumericBox();
            numericBoxB = new NumericBox();
            numericBoxC = new NumericBox();
            numericBoxCErr = new NumericBox();
            numericBoxAErr = new NumericBox();
            labelLengthUnitA = new System.Windows.Forms.Label();
            labelLaTex2 = new LabelLaTeX();
            labelLaTex3 = new LabelLaTeX();
            labelLaTex4 = new LabelLaTeX();
            labelLaTex5 = new LabelLaTeX();
            labelLaTex6 = new LabelLaTeX();
            groupBoxSymmetry = new System.Windows.Forms.GroupBox();
            comboBoxSpaceGroup = new System.Windows.Forms.ComboBox();
            comboBoxPointGroup = new System.Windows.Forms.ComboBox();
            comboBoxCrystalSystem = new System.Windows.Forms.ComboBox();
            label20 = new System.Windows.Forms.Label();
            label17 = new System.Windows.Forms.Label();
            label19 = new System.Windows.Forms.Label();
            textBoxSearch = new System.Windows.Forms.TextBox();
            label21 = new System.Windows.Forms.Label();
            comboBoxSearchResult = new System.Windows.Forms.ComboBox();
            panel1 = new System.Windows.Forms.Panel();
            toolTip = new System.Windows.Forms.ToolTip(components);
            flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            groupBoxCellConstants.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            groupBoxSymmetry.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            flowLayoutPanel2.SuspendLayout();
            SuspendLayout();
            // 
            // groupBoxCellConstants
            // 
            groupBoxCellConstants.Controls.Add(flowLayoutPanel1);
            groupBoxCellConstants.Controls.Add(checkBoxShowError);
            groupBoxCellConstants.Controls.Add(tableLayoutPanel1);
            resources.ApplyResources(groupBoxCellConstants, "groupBoxCellConstants");
            groupBoxCellConstants.Name = "groupBoxCellConstants";
            groupBoxCellConstants.TabStop = false;
            // 
            // radioButtonNanoMeter
            // 
            resources.ApplyResources(radioButtonNanoMeter, "radioButtonNanoMeter");
            radioButtonNanoMeter.Name = "radioButtonNanoMeter";
            toolTip.SetToolTip(radioButtonNanoMeter, resources.GetString("radioButtonNanoMeter.ToolTip"));
            radioButtonNanoMeter.UseVisualStyleBackColor = true;
            radioButtonNanoMeter.CheckedChanged += radioButtonNanoMeter_CheckedChanged;
            // 
            // radioButtonAngstrom
            // 
            resources.ApplyResources(radioButtonAngstrom, "radioButtonAngstrom");
            radioButtonAngstrom.Checked = true;
            radioButtonAngstrom.Name = "radioButtonAngstrom";
            radioButtonAngstrom.TabStop = true;
            toolTip.SetToolTip(radioButtonAngstrom, resources.GetString("radioButtonAngstrom.ToolTip"));
            radioButtonAngstrom.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            resources.ApplyResources(label1, "label1");
            label1.Name = "label1";
            toolTip.SetToolTip(label1, resources.GetString("label1.ToolTip"));
            // 
            // checkBoxShowError
            // 
            resources.ApplyResources(checkBoxShowError, "checkBoxShowError");
            checkBoxShowError.Name = "checkBoxShowError";
            toolTip.SetToolTip(checkBoxShowError, resources.GetString("checkBoxShowError.ToolTip"));
            checkBoxShowError.UseVisualStyleBackColor = true;
            checkBoxShowError.CheckedChanged += checkBoxShowError_CheckedChanged;
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(tableLayoutPanel1, "tableLayoutPanel1");
            tableLayoutPanel1.Controls.Add(labelLaTex1, 4, 0);
            tableLayoutPanel1.Controls.Add(label48, 7, 2);
            tableLayoutPanel1.Controls.Add(label46, 7, 1);
            tableLayoutPanel1.Controls.Add(numericBoxBeta, 5, 1);
            tableLayoutPanel1.Controls.Add(numericBoxAlpha, 5, 0);
            tableLayoutPanel1.Controls.Add(label47, 7, 0);
            tableLayoutPanel1.Controls.Add(numericBoxGammaErr, 6, 2);
            tableLayoutPanel1.Controls.Add(numericBoxAlphaErr, 6, 0);
            tableLayoutPanel1.Controls.Add(numericBoxBetaErr, 6, 1);
            tableLayoutPanel1.Controls.Add(numericBoxA, 1, 0);
            tableLayoutPanel1.Controls.Add(labelLengthUnitC, 3, 2);
            tableLayoutPanel1.Controls.Add(numericBoxGamma, 5, 2);
            tableLayoutPanel1.Controls.Add(labelLengthUnitB, 3, 1);
            tableLayoutPanel1.Controls.Add(numericBoxBErr, 2, 1);
            tableLayoutPanel1.Controls.Add(numericBoxB, 1, 1);
            tableLayoutPanel1.Controls.Add(numericBoxC, 1, 2);
            tableLayoutPanel1.Controls.Add(numericBoxCErr, 2, 2);
            tableLayoutPanel1.Controls.Add(numericBoxAErr, 2, 0);
            tableLayoutPanel1.Controls.Add(labelLengthUnitA, 3, 0);
            tableLayoutPanel1.Controls.Add(labelLaTex2, 4, 1);
            tableLayoutPanel1.Controls.Add(labelLaTex3, 4, 2);
            tableLayoutPanel1.Controls.Add(labelLaTex4, 0, 0);
            tableLayoutPanel1.Controls.Add(labelLaTex5, 0, 1);
            tableLayoutPanel1.Controls.Add(labelLaTex6, 0, 2);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // labelLaTex1
            // 
            resources.ApplyResources(labelLaTex1, "labelLaTex1");
            labelLaTex1.Name = "labelLaTex1";
            labelLaTex1.Thickness = 0.5D;
            // 
            // label48
            // 
            resources.ApplyResources(label48, "label48");
            label48.Name = "label48";
            // 
            // label46
            // 
            resources.ApplyResources(label46, "label46");
            label46.Name = "label46";
            // 
            // numericBoxBeta
            // 
            numericBoxBeta.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(numericBoxBeta, "numericBoxBeta");
            numericBoxBeta.Name = "numericBoxBeta";
            numericBoxBeta.RestrictLimitValue = false;
            numericBoxBeta.RoundErrorAccuracy = 12;
            numericBoxBeta.SkipEventDuringInput = false;
            numericBoxBeta.SmartIncrement = true;
            toolTip.SetToolTip(numericBoxBeta, resources.GetString("numericBoxBeta.ToolTip"));
            numericBoxBeta.ValueFontSize = 9F;
            numericBoxBeta.ValueChanged += numericBoxCellConstants_ValueChanged;
            // 
            // numericBoxAlpha
            // 
            numericBoxAlpha.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(numericBoxAlpha, "numericBoxAlpha");
            numericBoxAlpha.Name = "numericBoxAlpha";
            numericBoxAlpha.RestrictLimitValue = false;
            numericBoxAlpha.RoundErrorAccuracy = 12;
            numericBoxAlpha.SkipEventDuringInput = false;
            numericBoxAlpha.SmartIncrement = true;
            toolTip.SetToolTip(numericBoxAlpha, resources.GetString("numericBoxAlpha.ToolTip"));
            numericBoxAlpha.ValueFontSize = 9F;
            numericBoxAlpha.ValueChanged += numericBoxCellConstants_ValueChanged;
            // 
            // label47
            // 
            resources.ApplyResources(label47, "label47");
            label47.Name = "label47";
            // 
            // numericBoxGammaErr
            // 
            numericBoxGammaErr.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(numericBoxGammaErr, "numericBoxGammaErr");
            numericBoxGammaErr.Name = "numericBoxGammaErr";
            numericBoxGammaErr.RestrictLimitValue = false;
            numericBoxGammaErr.RoundErrorAccuracy = 12;
            numericBoxGammaErr.SkipEventDuringInput = false;
            numericBoxGammaErr.SmartIncrement = true;
            numericBoxGammaErr.TabStop = false;
            toolTip.SetToolTip(numericBoxGammaErr, resources.GetString("numericBoxGammaErr.ToolTip"));
            numericBoxGammaErr.ValueFontSize = 9F;
            numericBoxGammaErr.ValueChanged += numericBoxCellConstants_ValueChanged;
            // 
            // numericBoxAlphaErr
            // 
            numericBoxAlphaErr.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(numericBoxAlphaErr, "numericBoxAlphaErr");
            numericBoxAlphaErr.Name = "numericBoxAlphaErr";
            numericBoxAlphaErr.RestrictLimitValue = false;
            numericBoxAlphaErr.RoundErrorAccuracy = 12;
            numericBoxAlphaErr.SkipEventDuringInput = false;
            numericBoxAlphaErr.SmartIncrement = true;
            numericBoxAlphaErr.TabStop = false;
            toolTip.SetToolTip(numericBoxAlphaErr, resources.GetString("numericBoxAlphaErr.ToolTip"));
            numericBoxAlphaErr.ValueFontSize = 9F;
            numericBoxAlphaErr.ValueChanged += numericBoxCellConstants_ValueChanged;
            // 
            // numericBoxBetaErr
            // 
            numericBoxBetaErr.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(numericBoxBetaErr, "numericBoxBetaErr");
            numericBoxBetaErr.Name = "numericBoxBetaErr";
            numericBoxBetaErr.RestrictLimitValue = false;
            numericBoxBetaErr.RoundErrorAccuracy = 12;
            numericBoxBetaErr.SkipEventDuringInput = false;
            numericBoxBetaErr.SmartIncrement = true;
            numericBoxBetaErr.TabStop = false;
            toolTip.SetToolTip(numericBoxBetaErr, resources.GetString("numericBoxBetaErr.ToolTip"));
            numericBoxBetaErr.ValueFontSize = 9F;
            numericBoxBetaErr.ValueChanged += numericBoxCellConstants_ValueChanged;
            // 
            // numericBoxA
            // 
            numericBoxA.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(numericBoxA, "numericBoxA");
            numericBoxA.Name = "numericBoxA";
            numericBoxA.RestrictLimitValue = false;
            numericBoxA.RoundErrorAccuracy = 10;
            numericBoxA.SkipEventDuringInput = false;
            numericBoxA.SmartIncrement = true;
            toolTip.SetToolTip(numericBoxA, resources.GetString("numericBoxA.ToolTip"));
            numericBoxA.ValueFontSize = 9F;
            numericBoxA.ValueChanged += numericBoxCellConstants_ValueChanged;
            // 
            // labelLengthUnitC
            // 
            resources.ApplyResources(labelLengthUnitC, "labelLengthUnitC");
            labelLengthUnitC.Name = "labelLengthUnitC";
            toolTip.SetToolTip(labelLengthUnitC, resources.GetString("labelLengthUnitC.ToolTip"));
            // 
            // numericBoxGamma
            // 
            numericBoxGamma.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(numericBoxGamma, "numericBoxGamma");
            numericBoxGamma.Name = "numericBoxGamma";
            numericBoxGamma.RestrictLimitValue = false;
            numericBoxGamma.RoundErrorAccuracy = 12;
            numericBoxGamma.SkipEventDuringInput = false;
            numericBoxGamma.SmartIncrement = true;
            toolTip.SetToolTip(numericBoxGamma, resources.GetString("numericBoxGamma.ToolTip"));
            numericBoxGamma.ValueFontSize = 9F;
            numericBoxGamma.ValueChanged += numericBoxCellConstants_ValueChanged;
            // 
            // labelLengthUnitB
            // 
            resources.ApplyResources(labelLengthUnitB, "labelLengthUnitB");
            labelLengthUnitB.Name = "labelLengthUnitB";
            toolTip.SetToolTip(labelLengthUnitB, resources.GetString("labelLengthUnitB.ToolTip"));
            // 
            // numericBoxBErr
            // 
            numericBoxBErr.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(numericBoxBErr, "numericBoxBErr");
            numericBoxBErr.Name = "numericBoxBErr";
            numericBoxBErr.RestrictLimitValue = false;
            numericBoxBErr.RoundErrorAccuracy = 12;
            numericBoxBErr.SkipEventDuringInput = false;
            numericBoxBErr.SmartIncrement = true;
            numericBoxBErr.TabStop = false;
            toolTip.SetToolTip(numericBoxBErr, resources.GetString("numericBoxBErr.ToolTip"));
            numericBoxBErr.ValueFontSize = 9F;
            numericBoxBErr.ValueChanged += numericBoxCellConstants_ValueChanged;
            // 
            // numericBoxB
            // 
            numericBoxB.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(numericBoxB, "numericBoxB");
            numericBoxB.Name = "numericBoxB";
            numericBoxB.RestrictLimitValue = false;
            numericBoxB.RoundErrorAccuracy = 12;
            numericBoxB.SkipEventDuringInput = false;
            numericBoxB.SmartIncrement = true;
            toolTip.SetToolTip(numericBoxB, resources.GetString("numericBoxB.ToolTip"));
            numericBoxB.ValueFontSize = 9F;
            numericBoxB.ValueChanged += numericBoxCellConstants_ValueChanged;
            // 
            // numericBoxC
            // 
            numericBoxC.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(numericBoxC, "numericBoxC");
            numericBoxC.Name = "numericBoxC";
            numericBoxC.RestrictLimitValue = false;
            numericBoxC.RoundErrorAccuracy = 12;
            numericBoxC.SkipEventDuringInput = false;
            numericBoxC.SmartIncrement = true;
            toolTip.SetToolTip(numericBoxC, resources.GetString("numericBoxC.ToolTip"));
            numericBoxC.ValueFontSize = 9F;
            numericBoxC.ValueChanged += numericBoxCellConstants_ValueChanged;
            // 
            // numericBoxCErr
            // 
            numericBoxCErr.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(numericBoxCErr, "numericBoxCErr");
            numericBoxCErr.Name = "numericBoxCErr";
            numericBoxCErr.RestrictLimitValue = false;
            numericBoxCErr.RoundErrorAccuracy = 12;
            numericBoxCErr.SkipEventDuringInput = false;
            numericBoxCErr.SmartIncrement = true;
            numericBoxCErr.TabStop = false;
            toolTip.SetToolTip(numericBoxCErr, resources.GetString("numericBoxCErr.ToolTip"));
            numericBoxCErr.ValueFontSize = 9F;
            numericBoxCErr.ValueChanged += numericBoxCellConstants_ValueChanged;
            // 
            // numericBoxAErr
            // 
            numericBoxAErr.BackColor = System.Drawing.SystemColors.Control;
            resources.ApplyResources(numericBoxAErr, "numericBoxAErr");
            numericBoxAErr.Name = "numericBoxAErr";
            numericBoxAErr.RestrictLimitValue = false;
            numericBoxAErr.RoundErrorAccuracy = 12;
            numericBoxAErr.SkipEventDuringInput = false;
            numericBoxAErr.SmartIncrement = true;
            numericBoxAErr.TabStop = false;
            toolTip.SetToolTip(numericBoxAErr, resources.GetString("numericBoxAErr.ToolTip"));
            numericBoxAErr.ValueFontSize = 9F;
            numericBoxAErr.ValueChanged += numericBoxCellConstants_ValueChanged;
            // 
            // labelLengthUnitA
            // 
            resources.ApplyResources(labelLengthUnitA, "labelLengthUnitA");
            labelLengthUnitA.Name = "labelLengthUnitA";
            toolTip.SetToolTip(labelLengthUnitA, resources.GetString("labelLengthUnitA.ToolTip"));
            // 
            // labelLaTex2
            // 
            resources.ApplyResources(labelLaTex2, "labelLaTex2");
            labelLaTex2.Name = "labelLaTex2";
            labelLaTex2.Thickness = 0.5D;
            // 
            // labelLaTex3
            // 
            resources.ApplyResources(labelLaTex3, "labelLaTex3");
            labelLaTex3.Name = "labelLaTex3";
            labelLaTex3.Thickness = 0.5D;
            // 
            // labelLaTex4
            // 
            resources.ApplyResources(labelLaTex4, "labelLaTex4");
            labelLaTex4.Name = "labelLaTex4";
            labelLaTex4.Thickness = 0.5D;
            // 
            // labelLaTex5
            // 
            resources.ApplyResources(labelLaTex5, "labelLaTex5");
            labelLaTex5.Name = "labelLaTex5";
            labelLaTex5.Thickness = 0.5D;
            // 
            // labelLaTex6
            // 
            resources.ApplyResources(labelLaTex6, "labelLaTex6");
            labelLaTex6.Name = "labelLaTex6";
            labelLaTex6.Thickness = 0.5D;
            // 
            // groupBoxSymmetry
            // 
            groupBoxSymmetry.Controls.Add(flowLayoutPanel2);
            groupBoxSymmetry.Controls.Add(tableLayoutPanel2);
            resources.ApplyResources(groupBoxSymmetry, "groupBoxSymmetry");
            groupBoxSymmetry.Name = "groupBoxSymmetry";
            groupBoxSymmetry.TabStop = false;
            // 
            // comboBoxSpaceGroup
            // 
            resources.ApplyResources(comboBoxSpaceGroup, "comboBoxSpaceGroup");
            comboBoxSpaceGroup.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            comboBoxSpaceGroup.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            comboBoxSpaceGroup.DropDownWidth = 250;
            comboBoxSpaceGroup.Name = "comboBoxSpaceGroup";
            toolTip.SetToolTip(comboBoxSpaceGroup, resources.GetString("comboBoxSpaceGroup.ToolTip"));
            comboBoxSpaceGroup.DrawItem += comboBoxSpaceGroup_DrawItem;
            comboBoxSpaceGroup.SelectedIndexChanged += comboBoxSpaceGroup_SelectedIndexChanged;
            // 
            // comboBoxPointGroup
            // 
            resources.ApplyResources(comboBoxPointGroup, "comboBoxPointGroup");
            comboBoxPointGroup.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            comboBoxPointGroup.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            comboBoxPointGroup.Name = "comboBoxPointGroup";
            toolTip.SetToolTip(comboBoxPointGroup, resources.GetString("comboBoxPointGroup.ToolTip"));
            comboBoxPointGroup.DrawItem += comboBoxSpaceGroup_DrawItem;
            comboBoxPointGroup.SelectedIndexChanged += comboBoxPointGroup_SelectedIndexChanged;
            // 
            // comboBoxCrystalSystem
            // 
            resources.ApplyResources(comboBoxCrystalSystem, "comboBoxCrystalSystem");
            comboBoxCrystalSystem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            comboBoxCrystalSystem.Items.AddRange(new object[] { resources.GetString("comboBoxCrystalSystem.Items"), resources.GetString("comboBoxCrystalSystem.Items1"), resources.GetString("comboBoxCrystalSystem.Items2"), resources.GetString("comboBoxCrystalSystem.Items3"), resources.GetString("comboBoxCrystalSystem.Items4"), resources.GetString("comboBoxCrystalSystem.Items5"), resources.GetString("comboBoxCrystalSystem.Items6"), resources.GetString("comboBoxCrystalSystem.Items7") });
            comboBoxCrystalSystem.Name = "comboBoxCrystalSystem";
            toolTip.SetToolTip(comboBoxCrystalSystem, resources.GetString("comboBoxCrystalSystem.ToolTip"));
            comboBoxCrystalSystem.SelectedIndexChanged += comboBoxCrystalSystem_SelectedIndexChanged;
            // 
            // label20
            // 
            resources.ApplyResources(label20, "label20");
            label20.Name = "label20";
            toolTip.SetToolTip(label20, resources.GetString("label20.ToolTip"));
            // 
            // label17
            // 
            resources.ApplyResources(label17, "label17");
            label17.Name = "label17";
            toolTip.SetToolTip(label17, resources.GetString("label17.ToolTip"));
            // 
            // label19
            // 
            resources.ApplyResources(label19, "label19");
            label19.Name = "label19";
            toolTip.SetToolTip(label19, resources.GetString("label19.ToolTip"));
            // 
            // textBoxSearch
            // 
            resources.ApplyResources(textBoxSearch, "textBoxSearch");
            textBoxSearch.Name = "textBoxSearch";
            toolTip.SetToolTip(textBoxSearch, resources.GetString("textBoxSearch.ToolTip"));
            textBoxSearch.TextChanged += textBoxSearch_TextChanged;
            // 
            // label21
            // 
            resources.ApplyResources(label21, "label21");
            label21.Name = "label21";
            toolTip.SetToolTip(label21, resources.GetString("label21.ToolTip"));
            // 
            // comboBoxSearchResult
            // 
            resources.ApplyResources(comboBoxSearchResult, "comboBoxSearchResult");
            comboBoxSearchResult.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            comboBoxSearchResult.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            comboBoxSearchResult.DropDownWidth = 200;
            comboBoxSearchResult.Name = "comboBoxSearchResult";
            toolTip.SetToolTip(comboBoxSearchResult, resources.GetString("comboBoxSearchResult.ToolTip"));
            comboBoxSearchResult.DrawItem += comboBoxSpaceGroup_DrawItem;
            comboBoxSearchResult.SelectedIndexChanged += comboBoxSearchResult_SelectedIndexChanged;
            // 
            // panel1
            // 
            resources.ApplyResources(panel1, "panel1");
            panel1.Name = "panel1";
            // 
            // toolTip
            // 
            toolTip.AutoPopDelay = 10000;
            toolTip.InitialDelay = 500;
            toolTip.IsBalloon = true;
            toolTip.ReshowDelay = 100;
            // 
            // flowLayoutPanel1
            // 
            resources.ApplyResources(flowLayoutPanel1, "flowLayoutPanel1");
            flowLayoutPanel1.Controls.Add(label1);
            flowLayoutPanel1.Controls.Add(radioButtonAngstrom);
            flowLayoutPanel1.Controls.Add(radioButtonNanoMeter);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(tableLayoutPanel2, "tableLayoutPanel2");
            tableLayoutPanel2.Controls.Add(label19, 0, 0);
            tableLayoutPanel2.Controls.Add(comboBoxSpaceGroup, 1, 2);
            tableLayoutPanel2.Controls.Add(comboBoxCrystalSystem, 1, 0);
            tableLayoutPanel2.Controls.Add(label20, 0, 2);
            tableLayoutPanel2.Controls.Add(comboBoxPointGroup, 1, 1);
            tableLayoutPanel2.Controls.Add(label21, 0, 1);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // flowLayoutPanel2
            // 
            resources.ApplyResources(flowLayoutPanel2, "flowLayoutPanel2");
            flowLayoutPanel2.Controls.Add(label17);
            flowLayoutPanel2.Controls.Add(textBoxSearch);
            flowLayoutPanel2.Controls.Add(comboBoxSearchResult);
            flowLayoutPanel2.Name = "flowLayoutPanel2";
            // 
            // SymmetryControl
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            Controls.Add(groupBoxCellConstants);
            Controls.Add(panel1);
            Controls.Add(groupBoxSymmetry);
            Name = "SymmetryControl";
            groupBoxCellConstants.ResumeLayout(false);
            groupBoxCellConstants.PerformLayout();
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            groupBoxSymmetry.ResumeLayout(false);
            groupBoxSymmetry.PerformLayout();
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.PerformLayout();
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel2.PerformLayout();
            flowLayoutPanel2.ResumeLayout(false);
            flowLayoutPanel2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private System.Windows.Forms.GroupBox groupBoxCellConstants;
        private NumericBox numericBoxGammaErr;
        private NumericBox numericBoxBetaErr;
        private NumericBox numericBoxAlphaErr;
        private NumericBox numericBoxAlpha;
        private NumericBox numericBoxGamma;
        private NumericBox numericBoxBeta;
        private System.Windows.Forms.Label label46;
        private System.Windows.Forms.Label label47;
        private System.Windows.Forms.Label label48;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private NumericBox numericBoxAErr;
        private NumericBox numericBoxCErr;
        private NumericBox numericBoxBErr;
        private NumericBox numericBoxA;
        private NumericBox numericBoxB;
        private NumericBox numericBoxC;
        private System.Windows.Forms.GroupBox groupBoxSymmetry;
        public System.Windows.Forms.ComboBox comboBoxSpaceGroup;
        public System.Windows.Forms.ComboBox comboBoxPointGroup;
        public System.Windows.Forms.ComboBox comboBoxCrystalSystem;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label19;
        public System.Windows.Forms.TextBox textBoxSearch;
        private System.Windows.Forms.Label label21;
        public System.Windows.Forms.ComboBox comboBoxSearchResult;
        private System.Windows.Forms.CheckBox checkBoxShowError;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.RadioButton radioButtonAngstrom;
        private System.Windows.Forms.RadioButton radioButtonNanoMeter;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label labelLengthUnitA;
        private System.Windows.Forms.Label labelLengthUnitB;
        private System.Windows.Forms.Label labelLengthUnitC;
        private LabelLaTeX labelLaTex1;
        private LabelLaTeX labelLaTex2;
        private LabelLaTeX labelLaTex3;
        private LabelLaTeX labelLaTex4;
        private LabelLaTeX labelLaTex5;
        private LabelLaTeX labelLaTex6;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
    }
}