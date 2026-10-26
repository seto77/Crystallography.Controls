namespace Crystallography.Controls
{
    partial class FormPeriodicTable
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
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナで生成されたコード

        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            //260802Cl 復元 / 261024Cl 再復元: VS の再シリアライズで消える宣言。ツールチップは各言語の
            //FormPeriodicTable.<lang>.resx から resources.GetString で引くため、この行が無いと多言語化が切れる。
            //(このフォームは Localizable=false なので、デザイナーで保存し直すたびに直書きへ戻る点に注意)
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormPeriodicTable));
            toolTip = new System.Windows.Forms.ToolTip(components);
            buttonLa = new System.Windows.Forms.Button();
            buttonAc = new System.Windows.Forms.Button();
            labelLa = new System.Windows.Forms.Label();
            labelAc = new System.Windows.Forms.Label();
            buttonMustInclude = new System.Windows.Forms.Button();
            buttonMustExclude = new System.Windows.Forms.Button();
            label3 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            buttonMayInclude = new System.Windows.Forms.Button();
            buttonOK = new System.Windows.Forms.Button();
            flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            flowLayoutPanel4 = new System.Windows.Forms.FlowLayoutPanel();
            flowLayoutPanel5 = new System.Windows.Forms.FlowLayoutPanel();
            flowLayoutPanel1.SuspendLayout();
            flowLayoutPanel2.SuspendLayout();
            flowLayoutPanel3.SuspendLayout();
            flowLayoutPanel4.SuspendLayout();
            flowLayoutPanel5.SuspendLayout();
            SuspendLayout();
            // 
            // toolTip
            // 
            toolTip.AutoPopDelay = 10000;
            toolTip.InitialDelay = 500;
            toolTip.IsBalloon = true;
            toolTip.ReshowDelay = 100;
            // 
            // buttonLa
            // 
            buttonLa.BackColor = System.Drawing.Color.MistyRose;
            buttonLa.Enabled = false;
            buttonLa.Font = new System.Drawing.Font("Segoe UI", 9F);
            buttonLa.ForeColor = System.Drawing.Color.MediumBlue;
            buttonLa.Location = new System.Drawing.Point(95, 124);
            buttonLa.Margin = new System.Windows.Forms.Padding(0);
            buttonLa.Name = "buttonLa";
            buttonLa.Size = new System.Drawing.Size(29, 25);
            buttonLa.TabIndex = 0;
            buttonLa.Text = "La";
            toolTip.SetToolTip(buttonLa, resources.GetString("buttonLa.ToolTip")); // 261024Cl 復元 (VS の再シリアライズで英語直書きになっていた。文言は resx と同一)
            buttonLa.UseVisualStyleBackColor = false;
            // 
            // buttonAc
            // 
            buttonAc.BackColor = System.Drawing.Color.MistyRose;
            buttonAc.Enabled = false;
            buttonAc.Font = new System.Drawing.Font("Segoe UI", 9F);
            buttonAc.ForeColor = System.Drawing.Color.MediumBlue;
            buttonAc.Location = new System.Drawing.Point(95, 149);
            buttonAc.Margin = new System.Windows.Forms.Padding(0);
            buttonAc.Name = "buttonAc";
            buttonAc.Size = new System.Drawing.Size(29, 27);
            buttonAc.TabIndex = 0;
            buttonAc.Text = "Ac";
            toolTip.SetToolTip(buttonAc, resources.GetString("buttonAc.ToolTip")); // 261024Cl 復元 (VS の再シリアライズで英語直書きになっていた。文言は resx と同一)
            buttonAc.UseVisualStyleBackColor = false;
            // 
            // labelLa
            // 
            labelLa.AutoSize = true;
            labelLa.Font = new System.Drawing.Font("Segoe UI", 9F);
            labelLa.ForeColor = System.Drawing.Color.Crimson;
            labelLa.Location = new System.Drawing.Point(9, 187);
            labelLa.Margin = new System.Windows.Forms.Padding(0);
            labelLa.Name = "labelLa";
            labelLa.Size = new System.Drawing.Size(81, 15);
            labelLa.TabIndex = 1;
            labelLa.Text = "La: lanthanide";
            toolTip.SetToolTip(labelLa, resources.GetString("labelLa.ToolTip")); // 261024Cl 復元 (VS の再シリアライズで英語直書きになっていた。文言は resx と同一)
            // 
            // labelAc
            // 
            labelAc.AutoSize = true;
            labelAc.Font = new System.Drawing.Font("Segoe UI", 9F);
            labelAc.ForeColor = System.Drawing.Color.Crimson;
            labelAc.Location = new System.Drawing.Point(9, 202);
            labelAc.Margin = new System.Windows.Forms.Padding(0);
            labelAc.Name = "labelAc";
            labelAc.Size = new System.Drawing.Size(69, 15);
            labelAc.TabIndex = 1;
            labelAc.Text = "Ac: actinide";
            toolTip.SetToolTip(labelAc, resources.GetString("labelAc.ToolTip")); // 261024Cl 復元 (VS の再シリアライズで英語直書きになっていた。文言は resx と同一)
            // 
            // buttonMustInclude
            // 
            buttonMustInclude.BackColor = System.Drawing.Color.LightBlue;
            buttonMustInclude.Font = new System.Drawing.Font("Segoe UI", 9F);
            buttonMustInclude.Location = new System.Drawing.Point(0, 0);
            buttonMustInclude.Margin = new System.Windows.Forms.Padding(0);
            buttonMustInclude.Name = "buttonMustInclude";
            buttonMustInclude.Size = new System.Drawing.Size(25, 25);
            buttonMustInclude.TabIndex = 0;
            toolTip.SetToolTip(buttonMustInclude, resources.GetString("buttonMustInclude.ToolTip")); // 261024Cl 復元 (VS の再シリアライズで英語直書きになっていた。文言は resx と同一)
            buttonMustInclude.UseVisualStyleBackColor = false;
            buttonMustInclude.Click += buttonMustInclude_Click;
            // 
            // buttonMustExclude
            // 
            buttonMustExclude.BackColor = System.Drawing.Color.LightCoral;
            buttonMustExclude.Font = new System.Drawing.Font("Segoe UI", 9F);
            buttonMustExclude.Location = new System.Drawing.Point(0, 0);
            buttonMustExclude.Margin = new System.Windows.Forms.Padding(0);
            buttonMustExclude.Name = "buttonMustExclude";
            buttonMustExclude.Size = new System.Drawing.Size(25, 25);
            buttonMustExclude.TabIndex = 0;
            toolTip.SetToolTip(buttonMustExclude, resources.GetString("buttonMustExclude.ToolTip")); // 261024Cl 復元 (VS の再シリアライズで英語直書きになっていた。文言は resx と同一)
            buttonMustExclude.UseVisualStyleBackColor = false;
            buttonMustExclude.Click += buttonMustExclude_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new System.Drawing.Font("Segoe UI", 9F);
            label3.Location = new System.Drawing.Point(25, 0);
            label3.Margin = new System.Windows.Forms.Padding(0);
            label3.Name = "label3";
            label3.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
            label3.Size = new System.Drawing.Size(76, 19);
            label3.TabIndex = 1;
            label3.Text = "must include";
            toolTip.SetToolTip(label3, resources.GetString("label3.ToolTip")); // 261024Cl 復元 (VS の再シリアライズで英語直書きになっていた。文言は resx と同一)
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new System.Drawing.Font("Segoe UI", 9F);
            label4.Location = new System.Drawing.Point(25, 0);
            label4.Margin = new System.Windows.Forms.Padding(0);
            label4.Name = "label4";
            label4.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
            label4.Size = new System.Drawing.Size(77, 19);
            label4.TabIndex = 1;
            label4.Text = "must exclude";
            toolTip.SetToolTip(label4, resources.GetString("label4.ToolTip")); // 261024Cl 復元 (VS の再シリアライズで英語直書きになっていた。文言は resx と同一)
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new System.Drawing.Font("Segoe UI", 9F);
            label5.Location = new System.Drawing.Point(25, 0);
            label5.Margin = new System.Windows.Forms.Padding(0);
            label5.Name = "label5";
            label5.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
            label5.Size = new System.Drawing.Size(107, 19);
            label5.TabIndex = 1;
            label5.Text = "may or not include";
            toolTip.SetToolTip(label5, resources.GetString("label5.ToolTip")); // 261024Cl 復元 (VS の再シリアライズで英語直書きになっていた。文言は resx と同一)
            // 
            // buttonMayInclude
            // 
            buttonMayInclude.BackColor = System.Drawing.Color.LightYellow;
            buttonMayInclude.Font = new System.Drawing.Font("Segoe UI", 9F);
            buttonMayInclude.Location = new System.Drawing.Point(0, 0);
            buttonMayInclude.Margin = new System.Windows.Forms.Padding(0);
            buttonMayInclude.Name = "buttonMayInclude";
            buttonMayInclude.Size = new System.Drawing.Size(25, 25);
            buttonMayInclude.TabIndex = 0;
            toolTip.SetToolTip(buttonMayInclude, resources.GetString("buttonMayInclude.ToolTip")); // 261024Cl 復元 (VS の再シリアライズで英語直書きになっていた。文言は resx と同一)
            buttonMayInclude.UseVisualStyleBackColor = false;
            buttonMayInclude.Click += buttonMayInclude_Click;
            // 
            // buttonOK
            // 
            buttonOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            buttonOK.Font = new System.Drawing.Font("Segoe UI", 9F);
            buttonOK.Location = new System.Drawing.Point(476, 257);
            buttonOK.Margin = new System.Windows.Forms.Padding(0);
            buttonOK.Name = "buttonOK";
            buttonOK.Size = new System.Drawing.Size(118, 28);
            buttonOK.TabIndex = 0;
            buttonOK.Text = "OK";
            buttonOK.UseVisualStyleBackColor = true;
            buttonOK.Click += buttonOK_Click;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Anchor = System.Windows.Forms.AnchorStyles.Left;
            flowLayoutPanel1.AutoSize = true;
            flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            flowLayoutPanel1.Controls.Add(buttonMustInclude);
            flowLayoutPanel1.Controls.Add(label3);
            flowLayoutPanel1.Location = new System.Drawing.Point(0, 25);
            flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new System.Drawing.Size(101, 25);
            flowLayoutPanel1.TabIndex = 2;
            // 
            // flowLayoutPanel2
            // 
            flowLayoutPanel2.AutoSize = true;
            flowLayoutPanel2.Controls.Add(buttonMayInclude);
            flowLayoutPanel2.Controls.Add(label5);
            flowLayoutPanel2.Location = new System.Drawing.Point(3, 13);
            flowLayoutPanel2.Margin = new System.Windows.Forms.Padding(3, 13, 3, 3);
            flowLayoutPanel2.Name = "flowLayoutPanel2";
            flowLayoutPanel2.Size = new System.Drawing.Size(132, 25);
            flowLayoutPanel2.TabIndex = 2;
            // 
            // flowLayoutPanel3
            // 
            flowLayoutPanel3.AutoSize = true;
            flowLayoutPanel3.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            flowLayoutPanel3.Controls.Add(buttonMustExclude);
            flowLayoutPanel3.Controls.Add(label4);
            flowLayoutPanel3.Location = new System.Drawing.Point(0, 0);
            flowLayoutPanel3.Margin = new System.Windows.Forms.Padding(0);
            flowLayoutPanel3.Name = "flowLayoutPanel3";
            flowLayoutPanel3.Size = new System.Drawing.Size(102, 25);
            flowLayoutPanel3.TabIndex = 2;
            // 
            // flowLayoutPanel4
            // 
            flowLayoutPanel4.AutoSize = true;
            flowLayoutPanel4.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            flowLayoutPanel4.Controls.Add(flowLayoutPanel2);
            flowLayoutPanel4.Controls.Add(flowLayoutPanel5);
            flowLayoutPanel4.Location = new System.Drawing.Point(91, 3);
            flowLayoutPanel4.Name = "flowLayoutPanel4";
            flowLayoutPanel4.Size = new System.Drawing.Size(240, 50);
            flowLayoutPanel4.TabIndex = 3;
            flowLayoutPanel4.WrapContents = false;
            // 
            // flowLayoutPanel5
            // 
            flowLayoutPanel5.AutoSize = true;
            flowLayoutPanel5.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            flowLayoutPanel5.Controls.Add(flowLayoutPanel3);
            flowLayoutPanel5.Controls.Add(flowLayoutPanel1);
            flowLayoutPanel5.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            flowLayoutPanel5.Location = new System.Drawing.Point(138, 0);
            flowLayoutPanel5.Margin = new System.Windows.Forms.Padding(0);
            flowLayoutPanel5.Name = "flowLayoutPanel5";
            flowLayoutPanel5.Size = new System.Drawing.Size(102, 50);
            flowLayoutPanel5.TabIndex = 4;
            // 
            // FormPeriodicTable
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            captureExtender.SetCapture(this, true);
            ClientSize = new System.Drawing.Size(595, 286);
            Controls.Add(flowLayoutPanel4);
            Controls.Add(buttonLa);
            Controls.Add(buttonOK);
            Controls.Add(buttonAc);
            Controls.Add(labelAc);
            Controls.Add(labelLa);
            Font = new System.Drawing.Font("Segoe UI", 9F);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            Margin = new System.Windows.Forms.Padding(64, 32, 64, 32);
            Name = "FormPeriodicTable";
            StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            Text = "Periodic Table";
            FormClosing += FormPeriodicTable_FormClosing;
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.PerformLayout();
            flowLayoutPanel2.ResumeLayout(false);
            flowLayoutPanel2.PerformLayout();
            flowLayoutPanel3.ResumeLayout(false);
            flowLayoutPanel3.PerformLayout();
            flowLayoutPanel4.ResumeLayout(false);
            flowLayoutPanel4.PerformLayout();
            flowLayoutPanel5.ResumeLayout(false);
            flowLayoutPanel5.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolTip toolTip; // (260531Ch)

        private System.Windows.Forms.Button buttonLa;
        private System.Windows.Forms.Button buttonAc;
        private System.Windows.Forms.Label labelLa;
        private System.Windows.Forms.Label labelAc;
        private System.Windows.Forms.Button buttonMustInclude;
        private System.Windows.Forms.Button buttonMustExclude;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button buttonMayInclude;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel4;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel5;
    }
}