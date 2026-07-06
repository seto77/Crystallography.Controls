// 260704Cl 新規: 空間群の group-subgroup 関係ブラウザ (Phase 2)。設計は Pattern A (ツリー+タブ) を骨格に、
// Pattern C のグラフ (Diagram タブ) と Pattern B の軌道分裂・双晶インスペクタを統合した最小公倍数フォーム。
// レイアウトは docking 主体 (ピクセル座標依存を避ける)。ラベル文言はコード側 Loc() で多言語化 (方式②)。
namespace Crystallography.Controls;

partial class FormGroupRelations
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        toolTip = new System.Windows.Forms.ToolTip(components);
        panelToolbar = new System.Windows.Forms.Panel();
        labelBreadcrumb = new System.Windows.Forms.Label();
        buttonHome = new System.Windows.Forms.Button();
        buttonForward = new System.Windows.Forms.Button();
        buttonBack = new System.Windows.Forms.Button();
        panelBanner = new System.Windows.Forms.Panel();
        labelContext = new System.Windows.Forms.Label();
        splitMain = new System.Windows.Forms.SplitContainer();
        treeRelations = new System.Windows.Forms.TreeView();
        tabDetail = new System.Windows.Forms.TabControl();
        tabMatrix = new System.Windows.Forms.TabPage();
        miniTableGenerators = new MiniTable();
        labelLatex3 = new LabelLaTeX();
        labelLatex2 = new LabelLaTeX();
        labelLatex1 = new LabelLaTeX();
        //labelMatrix = new System.Windows.Forms.Label(); // 260706Ch: LabelLaTeX 3 段表示へ置換
        //labelRelTitle = new System.Windows.Forms.Label(); // 260706Ch: labelLatex1 に統合
        tabOrbit = new System.Windows.Forms.TabPage();
        miniTableOrbit = new MiniTable();
        labelOrbitInfo = new System.Windows.Forms.Label();
        tabDomains = new System.Windows.Forms.TabPage();
        miniTableTwins = new MiniTable();
        labelDomains = new System.Windows.Forms.Label();
        tabReflections = new System.Windows.Forms.TabPage();
        miniTableReflections = new MiniTable();
        labelReflInfo = new System.Windows.Forms.Label();
        tabDiagram = new System.Windows.Forms.TabPage();
        pictureBoxGraph = new System.Windows.Forms.PictureBox();
        panelToolbar.SuspendLayout();
        panelBanner.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
        splitMain.Panel1.SuspendLayout();
        splitMain.Panel2.SuspendLayout();
        splitMain.SuspendLayout();
        tabDetail.SuspendLayout();
        tabMatrix.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)miniTableGenerators).BeginInit();
        tabOrbit.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)miniTableOrbit).BeginInit();
        tabDomains.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)miniTableTwins).BeginInit();
        tabReflections.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)miniTableReflections).BeginInit();
        tabDiagram.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)pictureBoxGraph).BeginInit();
        SuspendLayout();
        //
        // toolTip
        //
        toolTip.AutoPopDelay = 12000;
        toolTip.InitialDelay = 500;
        toolTip.IsBalloon = true;
        toolTip.ReshowDelay = 100;
        //
        // panelToolbar
        //
        panelToolbar.Controls.Add(labelBreadcrumb);
        panelToolbar.Controls.Add(buttonHome);
        panelToolbar.Controls.Add(buttonForward);
        panelToolbar.Controls.Add(buttonBack);
        panelToolbar.Dock = System.Windows.Forms.DockStyle.Top;
        panelToolbar.Location = new System.Drawing.Point(0, 0);
        panelToolbar.Name = "panelToolbar";
        panelToolbar.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
        panelToolbar.Size = new System.Drawing.Size(880, 34);
        panelToolbar.TabIndex = 0;
        //
        // labelBreadcrumb
        //
        labelBreadcrumb.AutoEllipsis = true;
        labelBreadcrumb.Location = new System.Drawing.Point(150, 4);
        labelBreadcrumb.Name = "labelBreadcrumb";
        labelBreadcrumb.Size = new System.Drawing.Size(720, 26);
        labelBreadcrumb.TabIndex = 3;
        labelBreadcrumb.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // buttonHome
        //
        buttonHome.AutoSize = true;
        buttonHome.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        buttonHome.Location = new System.Drawing.Point(88, 4);
        buttonHome.Name = "buttonHome";
        buttonHome.Size = new System.Drawing.Size(64, 25);
        buttonHome.TabIndex = 2;
        buttonHome.Text = "⌂ Home";
        buttonHome.UseVisualStyleBackColor = true;
        buttonHome.Click += buttonHome_Click;
        //
        // buttonForward
        //
        buttonForward.AutoSize = true;
        buttonForward.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        buttonForward.Enabled = false;
        buttonForward.Location = new System.Drawing.Point(46, 4);
        buttonForward.Name = "buttonForward";
        buttonForward.Size = new System.Drawing.Size(27, 25);
        buttonForward.TabIndex = 1;
        buttonForward.Text = "→";
        buttonForward.UseVisualStyleBackColor = true;
        buttonForward.Click += buttonForward_Click;
        //
        // buttonBack
        //
        buttonBack.AutoSize = true;
        buttonBack.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        buttonBack.Enabled = false;
        buttonBack.Location = new System.Drawing.Point(6, 4);
        buttonBack.Name = "buttonBack";
        buttonBack.Size = new System.Drawing.Size(27, 25);
        buttonBack.TabIndex = 0;
        buttonBack.Text = "←";
        buttonBack.UseVisualStyleBackColor = true;
        buttonBack.Click += buttonBack_Click;
        //
        // panelBanner
        //
        panelBanner.Controls.Add(labelContext);
        panelBanner.Dock = System.Windows.Forms.DockStyle.Top;
        panelBanner.Location = new System.Drawing.Point(0, 34);
        panelBanner.Name = "panelBanner";
        panelBanner.Size = new System.Drawing.Size(880, 24);
        panelBanner.TabIndex = 1;
        //
        // labelContext
        //
        labelContext.Dock = System.Windows.Forms.DockStyle.Fill;
        labelContext.Location = new System.Drawing.Point(0, 0);
        labelContext.Name = "labelContext";
        labelContext.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
        labelContext.Size = new System.Drawing.Size(880, 24);
        labelContext.TabIndex = 0;
        labelContext.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // splitMain
        //
        splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
        splitMain.Location = new System.Drawing.Point(0, 58);
        splitMain.Name = "splitMain";
        //
        // splitMain.Panel1
        //
        splitMain.Panel1.Controls.Add(treeRelations);
        //
        // splitMain.Panel2
        //
        splitMain.Panel2.Controls.Add(tabDetail);
        splitMain.Size = new System.Drawing.Size(880, 462);
        splitMain.SplitterDistance = 300;
        splitMain.TabIndex = 2;
        //
        // treeRelations
        //
        treeRelations.Dock = System.Windows.Forms.DockStyle.Fill;
        treeRelations.FullRowSelect = true;
        treeRelations.HideSelection = false;
        treeRelations.Location = new System.Drawing.Point(0, 0);
        treeRelations.Name = "treeRelations";
        treeRelations.Size = new System.Drawing.Size(300, 462);
        treeRelations.TabIndex = 0;
        treeRelations.AfterSelect += treeRelations_AfterSelect;
        treeRelations.NodeMouseDoubleClick += treeRelations_NodeMouseDoubleClick;
        //
        // tabDetail
        //
        tabDetail.Controls.Add(tabMatrix);
        tabDetail.Controls.Add(tabOrbit);
        tabDetail.Controls.Add(tabDomains);
        tabDetail.Controls.Add(tabReflections);
        tabDetail.Controls.Add(tabDiagram);
        tabDetail.Dock = System.Windows.Forms.DockStyle.Fill;
        tabDetail.Location = new System.Drawing.Point(0, 0);
        tabDetail.Name = "tabDetail";
        tabDetail.SelectedIndex = 0;
        tabDetail.Size = new System.Drawing.Size(576, 462);
        tabDetail.TabIndex = 0;
        //
        // tabMatrix
        //
        tabMatrix.Controls.Add(miniTableGenerators);
        tabMatrix.Controls.Add(labelLatex3);
        tabMatrix.Controls.Add(labelLatex2);
        tabMatrix.Controls.Add(labelLatex1);
        //tabMatrix.Controls.Add(labelMatrix); // 260706Ch: 旧 labelMatrix は LabelLaTeX へ分割
        //tabMatrix.Controls.Add(labelRelTitle); // 260706Ch: 関係タイトルは labelLatex1 で描画
        tabMatrix.Location = new System.Drawing.Point(4, 24);
        tabMatrix.Name = "tabMatrix";
        tabMatrix.Padding = new System.Windows.Forms.Padding(3);
        tabMatrix.Size = new System.Drawing.Size(568, 434);
        tabMatrix.TabIndex = 0;
        tabMatrix.Text = "Matrix";
        tabMatrix.UseVisualStyleBackColor = true;
        //
        // miniTableGenerators
        //
        miniTableGenerators.AllowVerticalScroll = true;
        miniTableGenerators.Dock = System.Windows.Forms.DockStyle.Fill;
        //miniTableGenerators.Location = new System.Drawing.Point(3, 171); // 260706Ch: LaTeX ラベル拡大に合わせて下げる
        miniTableGenerators.Location = new System.Drawing.Point(3, 209);
        miniTableGenerators.Name = "miniTableGenerators";
        //miniTableGenerators.Size = new System.Drawing.Size(562, 260); // 260706Ch: 上部 LaTeX 領域拡大
        miniTableGenerators.Size = new System.Drawing.Size(562, 222);
        miniTableGenerators.TabIndex = 3;
        miniTableGenerators.TabStop = false;
        //
        // labelLatex3
        //
        labelLatex3.Dock = System.Windows.Forms.DockStyle.Top;
        //labelLatex3.Font = new System.Drawing.Font("Segoe UI", 11F); // 260706Ch: 行列表示を少し大きくする
        labelLatex3.Font = new System.Drawing.Font("Segoe UI", 13F);
        //labelLatex3.Location = new System.Drawing.Point(3, 77); // 260706Ch: labelLatex1/2 拡大分を反映
        labelLatex3.Location = new System.Drawing.Point(3, 93);
        labelLatex3.Name = "labelLatex3";
        labelLatex3.Padding = new System.Windows.Forms.Padding(6, 4, 6, 8);
        //labelLatex3.Size = new System.Drawing.Size(562, 94); // 260706Ch: 大きい数式用に高さを確保
        labelLatex3.Size = new System.Drawing.Size(562, 116);
        labelLatex3.TabIndex = 2;
        labelLatex3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        labelLatex3.Thickness = 0.6D;
        //
        // labelLatex2
        //
        labelLatex2.Dock = System.Windows.Forms.DockStyle.Top;
        //labelLatex2.Font = new System.Drawing.Font("Segoe UI", 10F); // 260706Ch: transformation 表示を少し大きくする
        labelLatex2.Font = new System.Drawing.Font("Segoe UI", 12F);
        //labelLatex2.Location = new System.Drawing.Point(3, 41); // 260706Ch: labelLatex1 拡大分を反映
        labelLatex2.Location = new System.Drawing.Point(3, 49);
        labelLatex2.Name = "labelLatex2";
        labelLatex2.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
        //labelLatex2.Size = new System.Drawing.Size(562, 36); // 260706Ch: 大きい数式用に高さを確保
        labelLatex2.Size = new System.Drawing.Size(562, 44);
        labelLatex2.TabIndex = 1;
        labelLatex2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        labelLatex2.Thickness = 0.6D;
        //
        // labelLatex1
        //
        labelLatex1.Dock = System.Windows.Forms.DockStyle.Top;
        //labelLatex1.Font = new System.Drawing.Font("Segoe UI", 11F); // 260706Ch: relation 表示を少し大きくする
        labelLatex1.Font = new System.Drawing.Font("Segoe UI", 13F);
        labelLatex1.Location = new System.Drawing.Point(3, 3);
        labelLatex1.Name = "labelLatex1";
        labelLatex1.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
        //labelLatex1.Size = new System.Drawing.Size(562, 38); // 260706Ch: 大きい数式用に高さを確保
        labelLatex1.Size = new System.Drawing.Size(562, 46);
        labelLatex1.TabIndex = 0;
        labelLatex1.Text = "\\mathrm{Select\\,a\\,subgroup\\,relation\\,from\\,the\\,tree.}";
        labelLatex1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        labelLatex1.Thickness = 0.6D;
        //
        // tabOrbit
        //
        tabOrbit.Controls.Add(miniTableOrbit);
        tabOrbit.Controls.Add(labelOrbitInfo);
        tabOrbit.Location = new System.Drawing.Point(4, 24);
        tabOrbit.Name = "tabOrbit";
        tabOrbit.Padding = new System.Windows.Forms.Padding(3);
        tabOrbit.Size = new System.Drawing.Size(568, 434);
        tabOrbit.TabIndex = 1;
        tabOrbit.Text = "Orbit splitting";
        tabOrbit.UseVisualStyleBackColor = true;
        //
        // miniTableOrbit
        //
        miniTableOrbit.AllowVerticalScroll = true;
        miniTableOrbit.Dock = System.Windows.Forms.DockStyle.Fill;
        miniTableOrbit.Location = new System.Drawing.Point(3, 37);
        miniTableOrbit.Name = "miniTableOrbit";
        miniTableOrbit.Size = new System.Drawing.Size(562, 394);
        miniTableOrbit.TabIndex = 1;
        miniTableOrbit.TabStop = false;
        //
        // labelOrbitInfo
        //
        labelOrbitInfo.Dock = System.Windows.Forms.DockStyle.Top;
        labelOrbitInfo.Location = new System.Drawing.Point(3, 3);
        labelOrbitInfo.Name = "labelOrbitInfo";
        labelOrbitInfo.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
        labelOrbitInfo.Size = new System.Drawing.Size(562, 34);
        labelOrbitInfo.TabIndex = 0;
        //
        // tabDomains
        //
        tabDomains.Controls.Add(miniTableTwins);
        tabDomains.Controls.Add(labelDomains);
        tabDomains.Location = new System.Drawing.Point(4, 24);
        tabDomains.Name = "tabDomains";
        tabDomains.Padding = new System.Windows.Forms.Padding(3);
        tabDomains.Size = new System.Drawing.Size(568, 434);
        tabDomains.TabIndex = 2;
        tabDomains.Text = "Domains & Twins";
        tabDomains.UseVisualStyleBackColor = true;
        //
        // miniTableTwins
        //
        miniTableTwins.AllowVerticalScroll = true;
        miniTableTwins.Dock = System.Windows.Forms.DockStyle.Fill;
        miniTableTwins.Location = new System.Drawing.Point(3, 99);
        miniTableTwins.Name = "miniTableTwins";
        miniTableTwins.Size = new System.Drawing.Size(562, 332);
        miniTableTwins.TabIndex = 1;
        miniTableTwins.TabStop = false;
        //
        // labelDomains
        //
        labelDomains.Dock = System.Windows.Forms.DockStyle.Top;
        labelDomains.Location = new System.Drawing.Point(3, 3);
        labelDomains.Name = "labelDomains";
        labelDomains.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
        labelDomains.Size = new System.Drawing.Size(562, 96);
        labelDomains.TabIndex = 0;
        //
        // tabReflections
        //
        tabReflections.Controls.Add(miniTableReflections);
        tabReflections.Controls.Add(labelReflInfo);
        tabReflections.Location = new System.Drawing.Point(4, 24);
        tabReflections.Name = "tabReflections";
        tabReflections.Padding = new System.Windows.Forms.Padding(3);
        tabReflections.Size = new System.Drawing.Size(568, 434);
        tabReflections.TabIndex = 3;
        tabReflections.Text = "New reflections";
        tabReflections.UseVisualStyleBackColor = true;
        //
        // miniTableReflections
        //
        miniTableReflections.AllowVerticalScroll = true;
        miniTableReflections.Dock = System.Windows.Forms.DockStyle.Fill;
        miniTableReflections.Location = new System.Drawing.Point(3, 53);
        miniTableReflections.Name = "miniTableReflections";
        miniTableReflections.Size = new System.Drawing.Size(562, 378);
        miniTableReflections.TabIndex = 1;
        miniTableReflections.TabStop = false;
        //
        // labelReflInfo
        //
        labelReflInfo.Dock = System.Windows.Forms.DockStyle.Top;
        labelReflInfo.Location = new System.Drawing.Point(3, 3);
        labelReflInfo.Name = "labelReflInfo";
        labelReflInfo.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
        labelReflInfo.Size = new System.Drawing.Size(562, 50);
        labelReflInfo.TabIndex = 0;
        //
        // tabDiagram
        //
        tabDiagram.Controls.Add(pictureBoxGraph);
        tabDiagram.Location = new System.Drawing.Point(4, 24);
        tabDiagram.Name = "tabDiagram";
        tabDiagram.Padding = new System.Windows.Forms.Padding(3);
        tabDiagram.Size = new System.Drawing.Size(568, 434);
        tabDiagram.TabIndex = 4;
        tabDiagram.Text = "Diagram";
        tabDiagram.UseVisualStyleBackColor = true;
        //
        // pictureBoxGraph
        //
        pictureBoxGraph.BackColor = System.Drawing.Color.White;
        pictureBoxGraph.Dock = System.Windows.Forms.DockStyle.Fill;
        pictureBoxGraph.Location = new System.Drawing.Point(3, 3);
        pictureBoxGraph.Name = "pictureBoxGraph";
        pictureBoxGraph.Size = new System.Drawing.Size(562, 428);
        pictureBoxGraph.TabIndex = 0;
        pictureBoxGraph.TabStop = false;
        pictureBoxGraph.SizeChanged += pictureBoxGraph_SizeChanged;
        pictureBoxGraph.MouseClick += pictureBoxGraph_MouseClick;
        pictureBoxGraph.MouseDoubleClick += pictureBoxGraph_MouseDoubleClick;
        //
        // FormGroupRelations
        //
        AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        ClientSize = new System.Drawing.Size(880, 520);
        Controls.Add(splitMain);
        Controls.Add(panelBanner);
        Controls.Add(panelToolbar);
        Font = new System.Drawing.Font("Segoe UI", 9F);
        MinimumSize = new System.Drawing.Size(620, 400);
        Name = "FormGroupRelations";
        StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        Text = "Group Relations";
        panelToolbar.ResumeLayout(false);
        panelToolbar.PerformLayout();
        panelBanner.ResumeLayout(false);
        splitMain.Panel1.ResumeLayout(false);
        splitMain.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
        splitMain.ResumeLayout(false);
        tabDetail.ResumeLayout(false);
        tabMatrix.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)miniTableGenerators).EndInit();
        tabOrbit.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)miniTableOrbit).EndInit();
        tabDomains.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)miniTableTwins).EndInit();
        tabReflections.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)miniTableReflections).EndInit();
        tabDiagram.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)pictureBoxGraph).EndInit();
        ResumeLayout(false);
    }

    private System.Windows.Forms.ToolTip toolTip;
    private System.Windows.Forms.Panel panelToolbar;
    private System.Windows.Forms.Button buttonBack;
    private System.Windows.Forms.Button buttonForward;
    private System.Windows.Forms.Button buttonHome;
    private System.Windows.Forms.Label labelBreadcrumb;
    private System.Windows.Forms.Panel panelBanner;
    private System.Windows.Forms.Label labelContext;
    private System.Windows.Forms.SplitContainer splitMain;
    private System.Windows.Forms.TreeView treeRelations;
    private System.Windows.Forms.TabControl tabDetail;
    private System.Windows.Forms.TabPage tabMatrix;
    private LabelLaTeX labelLatex1;
    private LabelLaTeX labelLatex2;
    private LabelLaTeX labelLatex3;
    //private System.Windows.Forms.Label labelRelTitle; // 260706Ch: labelLatex1 に統合
    //private System.Windows.Forms.Label labelMatrix; // 260706Ch: LabelLaTeX 3 段表示へ置換
    private MiniTable miniTableGenerators;
    private System.Windows.Forms.TabPage tabOrbit;
    private System.Windows.Forms.Label labelOrbitInfo;
    private MiniTable miniTableOrbit;
    private System.Windows.Forms.TabPage tabDomains;
    private System.Windows.Forms.Label labelDomains;
    private MiniTable miniTableTwins;
    private System.Windows.Forms.TabPage tabReflections;
    private System.Windows.Forms.Label labelReflInfo;
    private MiniTable miniTableReflections;
    private System.Windows.Forms.TabPage tabDiagram;
    private System.Windows.Forms.PictureBox pictureBoxGraph;
}
