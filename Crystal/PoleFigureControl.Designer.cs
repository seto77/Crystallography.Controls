namespace Crystallography.Controls
{
    partial class PoleFigureControl
    {
        /// <summary>必要なデザイナー変数です。</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>使用中のリソースをすべてクリーンアップします。</summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
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
        // groupBox1 -> groupBoxProjectionScheme
        // groupBox2 -> groupBoxProjectionObject
        // groupBox3 -> groupBoxAppearance
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container(); // (260531Ch)
            toolTip = new System.Windows.Forms.ToolTip(components); // (260531Ch)
            toolTip.IsBalloon = true; // 260531Cl 追加: バルーン表示に統一
            toolTip.AutoPopDelay = 10000; // 260601Cl 追加: 長文表示時間を延長(共通標準値)
            toolTip.InitialDelay = 500; // 260601Cl 追加
            toolTip.ReshowDelay = 100; // 260601Cl 追加
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PoleFigureControl));
            this.radioButtonAxes = new System.Windows.Forms.RadioButton();
            this.radioButtonPlanes = new System.Windows.Forms.RadioButton();
            //this.numericUpDown2 = new System.Windows.Forms.NumericUpDown(); // 261105Cl 変更前
            this.numericUpDown2 = new Crystallography.Controls.NumericBox(); // 261105Cl 変更 (監査 B20): ReciPro 共通の数値入力部品に揃える (名前は resx と外部の参照を保つため据え置き)
            //this.numericUpDown3 = new System.Windows.Forms.NumericUpDown(); // 261105Cl 変更前
            this.numericUpDown3 = new Crystallography.Controls.NumericBox(); // 261105Cl 変更 (監査 B20): ReciPro 共通の数値入力部品に揃える (名前は resx と外部の参照を保つため据え置き)
            this.labelV = new System.Windows.Forms.Label();
            //this.numericUpDown1 = new System.Windows.Forms.NumericUpDown(); // 261105Cl 変更前
            this.numericUpDown1 = new Crystallography.Controls.NumericBox(); // 261105Cl 変更 (監査 B20): ReciPro 共通の数値入力部品に揃える (名前は resx と外部の参照を保つため据え置き)
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            //this.numericUpDownResolution = new System.Windows.Forms.NumericUpDown(); // 261105Cl 変更前
            this.numericUpDownResolution = new Crystallography.Controls.NumericBox(); // 261105Cl 変更 (監査 B20): ReciPro 共通の数値入力部品に揃える (名前は resx と外部の参照を保つため据え置き)
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            //this.numericUpDownFullscale = new System.Windows.Forms.NumericUpDown(); // 261105Cl 変更前
            this.numericUpDownFullscale = new Crystallography.Controls.NumericBox(); // 261105Cl 変更 (監査 B20): ReciPro 共通の数値入力部品に揃える (名前は resx と外部の参照を保つため据え置き)
            this.label4 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.radioButtonPoleFigure = new System.Windows.Forms.RadioButton();
            this.radioButtonInversePoleFigure = new System.Windows.Forms.RadioButton();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.groupBoxAppearance = new System.Windows.Forms.GroupBox();
            this.label7 = new System.Windows.Forms.Label();
            this.comboBoxScale = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.comboBoxColor = new System.Windows.Forms.ComboBox();
            this.groupBoxProjectionObject = new System.Windows.Forms.GroupBox();
            this.labelU = new System.Windows.Forms.Label();
            this.labelW = new System.Windows.Forms.Label();
            this.groupBoxProjectionScheme = new System.Windows.Forms.GroupBox();
            //((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).BeginInit(); // 261105Cl 削除: NumericBox は ISupportInitialize でない
            //((System.ComponentModel.ISupportInitialize)(this.numericUpDown3)).BeginInit(); // 261105Cl 削除: NumericBox は ISupportInitialize でない
            //((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit(); // 261105Cl 削除: NumericBox は ISupportInitialize でない
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            //((System.ComponentModel.ISupportInitialize)(this.numericUpDownResolution)).BeginInit(); // 261105Cl 削除: NumericBox は ISupportInitialize でない
            //((System.ComponentModel.ISupportInitialize)(this.numericUpDownFullscale)).BeginInit(); // 261105Cl 削除: NumericBox は ISupportInitialize でない
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBoxAppearance.SuspendLayout();
            this.groupBoxProjectionObject.SuspendLayout();
            this.groupBoxProjectionScheme.SuspendLayout();
            this.SuspendLayout();
            // 
            // radioButtonAxes
            // 
            resources.ApplyResources(this.radioButtonAxes, "radioButtonAxes");
            this.toolTip.SetToolTip(this.radioButtonAxes, resources.GetString("radioButtonAxes.ToolTip")); // 260531Cl
            this.radioButtonAxes.Checked = true;
            this.radioButtonAxes.Name = "radioButtonAxes";
            this.radioButtonAxes.TabStop = true;
            this.radioButtonAxes.CheckedChanged += new System.EventHandler(this.radioButtonAxes_CheckedChanged);
            // 
            // radioButtonPlanes
            // 
            resources.ApplyResources(this.radioButtonPlanes, "radioButtonPlanes");
            this.toolTip.SetToolTip(this.radioButtonPlanes, resources.GetString("radioButtonPlanes.ToolTip")); // 260531Cl
            this.radioButtonPlanes.Name = "radioButtonPlanes";
            // 
            // numericUpDown2
            // 
            resources.ApplyResources(this.numericUpDown2, "numericUpDown2");
            this.toolTip.SetToolTip(this.numericUpDown2, resources.GetString("numericUpDown2.ToolTip")); // 260531Cl
            //this.numericUpDown2.Maximum = new decimal(new int[] { // 261105Cl 変更前 (NumericUpDown の範囲。以下 NumericBox で同じ値)
            //20,
            //0,
            //0,
            //0});
            //this.numericUpDown2.Minimum = new decimal(new int[] {
            //20,
            //0,
            //0,
            //-2147483648});
            this.numericUpDown2.DecimalPlaces = 0; // 261105Cl 追加 (以下 NumericBox の設定。範囲 -20〜20 の整数・スピンは旧と同じ)
            this.numericUpDown2.Maximum = 20D; // 261105Cl
            this.numericUpDown2.Minimum = -20D; // 261105Cl
            this.numericUpDown2.ShowUpDown = true; // 261105Cl
            this.numericUpDown2.UpDown_Increment = 1D; // 261105Cl
            this.numericUpDown2.ValueTextAlign = System.Windows.Forms.HorizontalAlignment.Center; // 261105Cl (旧 resx の TextAlign=Center)
            this.numericUpDown2.Name = "numericUpDown2";
            //this.numericUpDown2.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged); // 261105Cl 変更前
            this.numericUpDown2.ValueChanged += this.numericUpDown1_ValueChanged; // 261105Cl 変更 (NumericBox.ValueChanged は MyEventHandler)
            // 
            // numericUpDown3
            // 
            resources.ApplyResources(this.numericUpDown3, "numericUpDown3");
            this.toolTip.SetToolTip(this.numericUpDown3, resources.GetString("numericUpDown3.ToolTip")); // 260531Cl
            //this.numericUpDown3.Maximum = new decimal(new int[] { // 261105Cl 変更前 (NumericUpDown の範囲。以下 NumericBox で同じ値)
            //20,
            //0,
            //0,
            //0});
            //this.numericUpDown3.Minimum = new decimal(new int[] {
            //20,
            //0,
            //0,
            //-2147483648});
            this.numericUpDown3.DecimalPlaces = 0; // 261105Cl 追加 (以下 NumericBox の設定。範囲 -20〜20 の整数・スピンは旧と同じ)
            this.numericUpDown3.Maximum = 20D; // 261105Cl
            this.numericUpDown3.Minimum = -20D; // 261105Cl
            this.numericUpDown3.ShowUpDown = true; // 261105Cl
            this.numericUpDown3.UpDown_Increment = 1D; // 261105Cl
            this.numericUpDown3.ValueTextAlign = System.Windows.Forms.HorizontalAlignment.Center; // 261105Cl (旧 resx の TextAlign=Center)
            this.numericUpDown3.Name = "numericUpDown3";
            //this.numericUpDown3.Value = new decimal(new int[] { // 261105Cl 変更前
            //1,
            //0,
            //0,
            //0});
            this.numericUpDown3.Value = 1D; // 261105Cl 変更
            //this.numericUpDown3.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged); // 261105Cl 変更前
            this.numericUpDown3.ValueChanged += this.numericUpDown1_ValueChanged; // 261105Cl 変更 (NumericBox.ValueChanged は MyEventHandler)
            // 
            // labelV
            // 
            resources.ApplyResources(this.labelV, "labelV");
            this.toolTip.SetToolTip(this.labelV, resources.GetString("labelV.ToolTip")); // 260531Cl
            this.labelV.Name = "labelV";
            // 
            // numericUpDown1
            // 
            resources.ApplyResources(this.numericUpDown1, "numericUpDown1");
            this.toolTip.SetToolTip(this.numericUpDown1, resources.GetString("numericUpDown1.ToolTip")); // 260531Cl
            //this.numericUpDown1.Maximum = new decimal(new int[] { // 261105Cl 変更前 (NumericUpDown の範囲。以下 NumericBox で同じ値)
            //20,
            //0,
            //0,
            //0});
            //this.numericUpDown1.Minimum = new decimal(new int[] {
            //20,
            //0,
            //0,
            //-2147483648});
            this.numericUpDown1.DecimalPlaces = 0; // 261105Cl 追加 (以下 NumericBox の設定。範囲 -20〜20 の整数・スピンは旧と同じ)
            this.numericUpDown1.Maximum = 20D; // 261105Cl
            this.numericUpDown1.Minimum = -20D; // 261105Cl
            this.numericUpDown1.ShowUpDown = true; // 261105Cl
            this.numericUpDown1.UpDown_Increment = 1D; // 261105Cl
            this.numericUpDown1.ValueTextAlign = System.Windows.Forms.HorizontalAlignment.Center; // 261105Cl (旧 resx の TextAlign=Center)
            this.numericUpDown1.Name = "numericUpDown1";
            //this.numericUpDown1.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged); // 261105Cl 変更前
            this.numericUpDown1.ValueChanged += this.numericUpDown1_ValueChanged; // 261105Cl 変更 (NumericBox.ValueChanged は MyEventHandler)
            // 
            // pictureBox
            // 
            resources.ApplyResources(this.pictureBox, "pictureBox");
            this.pictureBox.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.TabStop = false;
            this.pictureBox.DoubleClick += new System.EventHandler(this.pictureBox_DoubleClick);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.toolTip.SetToolTip(this.label1, resources.GetString("label1.ToolTip")); // 260531Cl
            this.label1.Name = "label1";
            // 
            // numericUpDownResolution
            // 
            resources.ApplyResources(this.numericUpDownResolution, "numericUpDownResolution");
            this.toolTip.SetToolTip(this.numericUpDownResolution, resources.GetString("numericUpDownResolution.ToolTip")); // 260531Cl
            this.numericUpDownResolution.DecimalPlaces = 2;
            //this.numericUpDownResolution.Maximum = new decimal(new int[] { // 261105Cl 変更前 (NumericUpDown の範囲 0.01〜60・初期値 3。以下 NumericBox で同じ値)
            //60,
            //0,
            //0,
            //0});
            //this.numericUpDownResolution.Minimum = new decimal(new int[] {
            //1,
            //0,
            //0,
            //131072});
            this.numericUpDownResolution.Maximum = 60D; // 261105Cl
            this.numericUpDownResolution.Minimum = 0.01D; // 261105Cl
            this.numericUpDownResolution.Name = "numericUpDownResolution";
            this.numericUpDownResolution.ShowUpDown = true; // 261105Cl
            this.numericUpDownResolution.ValueTextAlign = System.Windows.Forms.HorizontalAlignment.Center; // 261105Cl
            //this.numericUpDownResolution.Value = new decimal(new int[] { // 261105Cl 変更前
            //3,
            //0,
            //0,
            //0});
            this.numericUpDownResolution.Value = 3D; // 261105Cl
            //this.numericUpDownResolution.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged); // 261105Cl 変更前
            this.numericUpDownResolution.ValueChanged += this.numericUpDown1_ValueChanged; // 261105Cl 変更 (NumericBox.ValueChanged は MyEventHandler)
            this.numericUpDownResolution.Click += new System.EventHandler(this.numericUpDownResolution_Click);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.toolTip.SetToolTip(this.label2, resources.GetString("label2.ToolTip")); // 260531Cl
            this.label2.Name = "label2";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.toolTip.SetToolTip(this.label3, resources.GetString("label3.ToolTip")); // 260531Cl
            this.label3.Name = "label3";
            // 
            // numericUpDownFullscale
            // 
            resources.ApplyResources(this.numericUpDownFullscale, "numericUpDownFullscale");
            this.toolTip.SetToolTip(this.numericUpDownFullscale, resources.GetString("numericUpDownFullscale.ToolTip")); // 260531Cl
            this.numericUpDownFullscale.DecimalPlaces = 1;
            //this.numericUpDownFullscale.Increment = new decimal(new int[] { // 261105Cl 変更前 (NumericUpDown の刻み 0.1・範囲 -9〜9・初期値 1。以下 NumericBox で同じ値)
            //1,
            //0,
            //0,
            //65536});
            //this.numericUpDownFullscale.Maximum = new decimal(new int[] {
            //9,
            //0,
            //0,
            //0});
            //this.numericUpDownFullscale.Minimum = new decimal(new int[] {
            //9,
            //0,
            //0,
            //-2147483648});
            this.numericUpDownFullscale.Maximum = 9D; // 261105Cl
            this.numericUpDownFullscale.Minimum = -9D; // 261105Cl
            this.numericUpDownFullscale.Name = "numericUpDownFullscale";
            this.numericUpDownFullscale.ShowUpDown = true; // 261105Cl
            this.numericUpDownFullscale.UpDown_Increment = 0.1D; // 261105Cl
            this.numericUpDownFullscale.ValueTextAlign = System.Windows.Forms.HorizontalAlignment.Center; // 261105Cl
            //this.numericUpDownFullscale.Value = new decimal(new int[] { // 261105Cl 変更前
            //1,
            //0,
            //0,
            //0});
            this.numericUpDownFullscale.Value = 1D; // 261105Cl
            //this.numericUpDownFullscale.ValueChanged += new System.EventHandler(this.numericUpDown4_ValueChanged); // 261105Cl 変更前
            this.numericUpDownFullscale.ValueChanged += this.numericUpDown4_ValueChanged; // 261105Cl 変更 (NumericBox.ValueChanged は MyEventHandler)
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.toolTip.SetToolTip(this.label4, resources.GetString("label4.ToolTip")); // 260531Cl
            this.label4.Name = "label4";
            // 
            // pictureBox1
            // 
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            // 
            // radioButtonPoleFigure
            // 
            resources.ApplyResources(this.radioButtonPoleFigure, "radioButtonPoleFigure");
            this.toolTip.SetToolTip(this.radioButtonPoleFigure, resources.GetString("radioButtonPoleFigure.ToolTip")); // 260531Cl
            this.radioButtonPoleFigure.Checked = true;
            this.radioButtonPoleFigure.Name = "radioButtonPoleFigure";
            this.radioButtonPoleFigure.TabStop = true;
            this.radioButtonPoleFigure.CheckedChanged += new System.EventHandler(this.radioButtonPoleFigure_CheckedChanged);
            // 
            // radioButtonInversePoleFigure
            // 
            resources.ApplyResources(this.radioButtonInversePoleFigure, "radioButtonInversePoleFigure");
            this.toolTip.SetToolTip(this.radioButtonInversePoleFigure, resources.GetString("radioButtonInversePoleFigure.ToolTip")); // 260531Cl
            this.radioButtonInversePoleFigure.Name = "radioButtonInversePoleFigure";
            // 
            // textBox1
            // 
            resources.ApplyResources(this.textBox1, "textBox1");
            this.toolTip.SetToolTip(this.textBox1, resources.GetString("textBox1.ToolTip")); // 260531Cl
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            // 
            // tabControl1
            // 
            resources.ApplyResources(this.tabControl1, "tabControl1");
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
            // 
            // tabPage1
            // 
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.BackColor = System.Drawing.SystemColors.Control;
            this.tabPage1.Controls.Add(this.groupBoxAppearance);
            this.tabPage1.Controls.Add(this.groupBoxProjectionObject);
            this.tabPage1.Controls.Add(this.groupBoxProjectionScheme);
            this.tabPage1.Controls.Add(this.pictureBox1);
            this.tabPage1.Controls.Add(this.pictureBox);
            this.tabPage1.Name = "tabPage1";
            // 
            // groupBoxAppearance
            // 
            resources.ApplyResources(this.groupBoxAppearance, "groupBoxAppearance");
            this.groupBoxAppearance.Controls.Add(this.label7);
            this.groupBoxAppearance.Controls.Add(this.comboBoxScale);
            this.groupBoxAppearance.Controls.Add(this.numericUpDownResolution);
            this.groupBoxAppearance.Controls.Add(this.textBox1);
            this.groupBoxAppearance.Controls.Add(this.label9);
            this.groupBoxAppearance.Controls.Add(this.comboBoxColor);
            this.groupBoxAppearance.Controls.Add(this.label1);
            this.groupBoxAppearance.Controls.Add(this.label2);
            this.groupBoxAppearance.Controls.Add(this.label4);
            this.groupBoxAppearance.Controls.Add(this.numericUpDownFullscale);
            this.groupBoxAppearance.Controls.Add(this.label3);
            this.groupBoxAppearance.Name = "groupBoxAppearance";
            this.groupBoxAppearance.TabStop = false;
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.toolTip.SetToolTip(this.label7, resources.GetString("label7.ToolTip")); // 260531Cl
            this.label7.Name = "label7";
            // 
            // comboBoxScale
            // 
            resources.ApplyResources(this.comboBoxScale, "comboBoxScale");
            this.toolTip.SetToolTip(this.comboBoxScale, resources.GetString("comboBoxScale.ToolTip")); // 260531Cl
            this.comboBoxScale.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxScale.FormattingEnabled = true;
            this.comboBoxScale.Items.AddRange(new object[] {
            resources.GetString("comboBoxScale.Items"),
            resources.GetString("comboBoxScale.Items1")});
            this.comboBoxScale.Name = "comboBoxScale";
            this.comboBoxScale.SelectedIndexChanged += new System.EventHandler(this.Combobox_SelectedIndexChanged);
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.toolTip.SetToolTip(this.label9, resources.GetString("label9.ToolTip")); // 260531Cl
            this.label9.Name = "label9";
            // 
            // comboBoxColor
            // 
            resources.ApplyResources(this.comboBoxColor, "comboBoxColor");
            this.toolTip.SetToolTip(this.comboBoxColor, resources.GetString("comboBoxColor.ToolTip")); // 260531Cl
            this.comboBoxColor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxColor.FormattingEnabled = true;
            this.comboBoxColor.Items.AddRange(new object[] {
            resources.GetString("comboBoxColor.Items"),
            resources.GetString("comboBoxColor.Items1")});
            this.comboBoxColor.Name = "comboBoxColor";
            this.comboBoxColor.SelectedIndexChanged += new System.EventHandler(this.Combobox_SelectedIndexChanged);
            // 
            // groupBoxProjectionObject
            // 
            resources.ApplyResources(this.groupBoxProjectionObject, "groupBoxProjectionObject");
            this.groupBoxProjectionObject.Controls.Add(this.radioButtonAxes);
            this.groupBoxProjectionObject.Controls.Add(this.radioButtonPlanes);
            this.groupBoxProjectionObject.Controls.Add(this.numericUpDown2);
            this.groupBoxProjectionObject.Controls.Add(this.numericUpDown1);
            this.groupBoxProjectionObject.Controls.Add(this.numericUpDown3);
            this.groupBoxProjectionObject.Controls.Add(this.labelU);
            this.groupBoxProjectionObject.Controls.Add(this.labelW);
            this.groupBoxProjectionObject.Controls.Add(this.labelV);
            this.groupBoxProjectionObject.Name = "groupBoxProjectionObject";
            this.groupBoxProjectionObject.TabStop = false;
            // 
            // labelU
            // 
            resources.ApplyResources(this.labelU, "labelU");
            this.toolTip.SetToolTip(this.labelU, resources.GetString("labelU.ToolTip")); // 260531Cl
            this.labelU.Name = "labelU";
            // 
            // labelW
            // 
            resources.ApplyResources(this.labelW, "labelW");
            this.toolTip.SetToolTip(this.labelW, resources.GetString("labelW.ToolTip")); // 260531Cl
            this.labelW.Name = "labelW";
            // 
            // groupBoxProjectionScheme
            // 
            resources.ApplyResources(this.groupBoxProjectionScheme, "groupBoxProjectionScheme");
            this.groupBoxProjectionScheme.Controls.Add(this.radioButtonInversePoleFigure);
            this.groupBoxProjectionScheme.Controls.Add(this.radioButtonPoleFigure);
            this.groupBoxProjectionScheme.Name = "groupBoxProjectionScheme";
            this.groupBoxProjectionScheme.TabStop = false;
            // 
            // PoleFigureControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.tabControl1);
            this.Name = "PoleFigureControl";
            this.Load += new System.EventHandler(this.PoleFigureControl_Load);
            //((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).EndInit(); // 261105Cl 削除
            //((System.ComponentModel.ISupportInitialize)(this.numericUpDown3)).EndInit(); // 261105Cl 削除
            //((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit(); // 261105Cl 削除
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            //((System.ComponentModel.ISupportInitialize)(this.numericUpDownResolution)).EndInit(); // 261105Cl 削除
            //((System.ComponentModel.ISupportInitialize)(this.numericUpDownFullscale)).EndInit(); // 261105Cl 削除
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.groupBoxAppearance.ResumeLayout(false);
            this.groupBoxAppearance.PerformLayout();
            this.groupBoxProjectionObject.ResumeLayout(false);
            this.groupBoxProjectionObject.PerformLayout();
            this.groupBoxProjectionScheme.ResumeLayout(false);
            this.groupBoxProjectionScheme.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolTip toolTip; // (260531Ch)
        private System.Windows.Forms.RadioButton radioButtonAxes;
        private System.Windows.Forms.RadioButton radioButtonPlanes;
        //public System.Windows.Forms.NumericUpDown numericUpDown2; // 261105Cl 変更前
        public Crystallography.Controls.NumericBox numericUpDown2; // 261105Cl 変更 (監査 B20)
        //public System.Windows.Forms.NumericUpDown numericUpDown3; // 261105Cl 変更前
        public Crystallography.Controls.NumericBox numericUpDown3; // 261105Cl 変更 (監査 B20)
        private System.Windows.Forms.Label labelV;
        //public System.Windows.Forms.NumericUpDown numericUpDown1; // 261105Cl 変更前
        public Crystallography.Controls.NumericBox numericUpDown1; // 261105Cl 変更 (監査 B20)
        private System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.Label label1;
        //public System.Windows.Forms.NumericUpDown numericUpDownResolution; // 261105Cl 変更前
        public Crystallography.Controls.NumericBox numericUpDownResolution; // 261105Cl 変更 (監査 B20)
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        //public System.Windows.Forms.NumericUpDown numericUpDownFullscale; // 261105Cl 変更前
        public Crystallography.Controls.NumericBox numericUpDownFullscale; // 261105Cl 変更 (監査 B20)
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.RadioButton radioButtonPoleFigure;
        private System.Windows.Forms.RadioButton radioButtonInversePoleFigure;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.GroupBox groupBoxAppearance;
        private System.Windows.Forms.GroupBox groupBoxProjectionObject;
        private System.Windows.Forms.GroupBox groupBoxProjectionScheme;
        private System.Windows.Forms.Label label7;
        public System.Windows.Forms.ComboBox comboBoxScale;
        private System.Windows.Forms.Label label9;
        public System.Windows.Forms.ComboBox comboBoxColor;
        private System.Windows.Forms.Label labelU;
        private System.Windows.Forms.Label labelW;
    }
}
