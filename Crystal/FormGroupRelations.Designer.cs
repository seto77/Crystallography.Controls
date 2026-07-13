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
        labelIsoMax = new System.Windows.Forms.Label(); // 260709Cl 追加 (Phase 3): 同型部分群の index 上限
        numericIsoMax = new System.Windows.Forms.NumericUpDown(); // 260709Cl 追加 (Phase 3)
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
        tabOrbit = new System.Windows.Forms.TabPage();
        miniTableOrbit = new MiniTable();
        labelOrbitInfo = new System.Windows.Forms.Label();
        tabDomains = new System.Windows.Forms.TabPage();
        miniTableTwins = new MiniTable();
        labelDomains = new System.Windows.Forms.Label();
        tabReflections = new System.Windows.Forms.TabPage();
        miniTableReflections = new MiniTable();
        panelReflSearch = new System.Windows.Forms.FlowLayoutPanel(); // 260709Cl 追加: 反射探索窓 (|h|,|k|,|l| ≤ n) 調整 UI
        labelReflMax = new System.Windows.Forms.Label(); // 260709Cl 追加
        numericReflMax = new System.Windows.Forms.NumericUpDown(); // 260709Cl 追加
        labelReflInfo = new System.Windows.Forms.Label();
        tabDiagram = new System.Windows.Forms.TabPage();
        pictureBoxGraph = new System.Windows.Forms.PictureBox();
        tabPointGroups = new System.Windows.Forms.TabPage(); // 260712Cl 追加 (③-4): 点群 Hasse 図
        pictureBoxPointGroups = new System.Windows.Forms.PictureBox(); // 260712Cl 追加
        tabElements = new System.Windows.Forms.TabPage(); // 260713Cl 追加 (③-2): 対称要素 lost/retained
        pictureBoxElements = new System.Windows.Forms.PictureBox(); // 260713Cl 追加
        panelToolbar.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numericIsoMax).BeginInit(); // 260709Cl 追加
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
        panelReflSearch.SuspendLayout(); // 260709Cl 追加
        ((System.ComponentModel.ISupportInitialize)numericReflMax).BeginInit(); // 260709Cl 追加
        tabDiagram.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)pictureBoxGraph).BeginInit();
        tabPointGroups.SuspendLayout(); // 260712Cl 追加
        ((System.ComponentModel.ISupportInitialize)pictureBoxPointGroups).BeginInit(); // 260712Cl 追加
        tabElements.SuspendLayout(); // 260713Cl 追加
        ((System.ComponentModel.ISupportInitialize)pictureBoxElements).BeginInit(); // 260713Cl 追加
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
        panelToolbar.Controls.Add(numericIsoMax); // 260709Cl 追加 (Phase 3)
        panelToolbar.Controls.Add(labelIsoMax); // 260709Cl 追加 (Phase 3)
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
        //labelBreadcrumb.Size = new System.Drawing.Size(720, 26);
        labelBreadcrumb.Size = new System.Drawing.Size(420, 26); // 260709Cl: 右側に同型 index スピナーを配置するため短縮
        labelBreadcrumb.TabIndex = 3;
        labelBreadcrumb.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // labelIsoMax (260709Cl 追加、Phase 3)
        //
        labelIsoMax.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        labelIsoMax.AutoEllipsis = true;
        labelIsoMax.Location = new System.Drawing.Point(576, 4);
        labelIsoMax.Name = "labelIsoMax";
        labelIsoMax.Size = new System.Drawing.Size(250, 26);
        labelIsoMax.TabIndex = 4;
        labelIsoMax.Text = "Isomorphic subgroups:  index ≤";
        labelIsoMax.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
        //
        // numericIsoMax (260709Cl 追加、Phase 3)
        //
        numericIsoMax.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        numericIsoMax.Location = new System.Drawing.Point(832, 5);
        numericIsoMax.Maximum = new decimal(new int[] { 27, 0, 0, 0 });
        numericIsoMax.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
        numericIsoMax.Name = "numericIsoMax";
        numericIsoMax.Size = new System.Drawing.Size(44, 23);
        numericIsoMax.TabIndex = 5;
        numericIsoMax.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
        numericIsoMax.Value = new decimal(new int[] { 4, 0, 0, 0 });
        numericIsoMax.ValueChanged += numericIsoMax_ValueChanged;
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
        tabDetail.Controls.Add(tabPointGroups); // 260712Cl 追加
        tabDetail.Controls.Add(tabElements); // 260713Cl 追加
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
        miniTableGenerators.CellPadding = new System.Windows.Forms.Padding(6, 2, 6, 2);
        miniTableGenerators.ColumnHeadersHeight = 26;
        miniTableGenerators.Dock = System.Windows.Forms.DockStyle.Fill;
        miniTableGenerators.LatexFontSizeInPoints = 10F;
        miniTableGenerators.LatexFractionStyle = LatexFractionStyle.Slanted; // 260708Ch
        miniTableGenerators.LatexThickness = 0.2D;
        miniTableGenerators.Location = new System.Drawing.Point(3, 209);
        miniTableGenerators.ManualRowHeight = 26;
        miniTableGenerators.Name = "miniTableGenerators";
        miniTableGenerators.RowTemplate.Height = 26;
        miniTableGenerators.Size = new System.Drawing.Size(562, 222);
        miniTableGenerators.TabIndex = 3;
        miniTableGenerators.TabStop = false;
        // 
        // labelLatex3
        // 
        labelLatex3.Dock = System.Windows.Forms.DockStyle.Top;
        labelLatex3.Font = new System.Drawing.Font("Segoe UI", 13F);
        labelLatex3.Location = new System.Drawing.Point(3, 93);
        labelLatex3.Name = "labelLatex3";
        labelLatex3.Padding = new System.Windows.Forms.Padding(6, 4, 6, 8);
        labelLatex3.Size = new System.Drawing.Size(562, 116);
        labelLatex3.TabIndex = 2;
        labelLatex3.Thickness = 0.6D;
        // 
        // labelLatex2
        // 
        labelLatex2.Dock = System.Windows.Forms.DockStyle.Top;
        labelLatex2.Font = new System.Drawing.Font("Segoe UI", 12F);
        labelLatex2.Location = new System.Drawing.Point(3, 49);
        labelLatex2.Name = "labelLatex2";
        labelLatex2.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
        labelLatex2.Size = new System.Drawing.Size(562, 44);
        labelLatex2.TabIndex = 1;
        labelLatex2.Thickness = 0.6D;
        // 
        // labelLatex1
        // 
        labelLatex1.Dock = System.Windows.Forms.DockStyle.Top;
        labelLatex1.Font = new System.Drawing.Font("Segoe UI", 13F);
        labelLatex1.Location = new System.Drawing.Point(3, 3);
        labelLatex1.Name = "labelLatex1";
        labelLatex1.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
        labelLatex1.Size = new System.Drawing.Size(562, 46);
        labelLatex1.TabIndex = 0;
        labelLatex1.Text = "\\mathrm{Select\\,a\\,subgroup\\,relation\\,from\\,the\\,tree.}";
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
        miniTableOrbit.CellPadding = new System.Windows.Forms.Padding(6, 2, 6, 2);
        miniTableOrbit.ColumnHeadersHeight = 26;
        miniTableOrbit.Dock = System.Windows.Forms.DockStyle.Fill;
        miniTableOrbit.LatexFontSizeInPoints = 10F;
        miniTableOrbit.LatexThickness = 0.2D;
        miniTableOrbit.Location = new System.Drawing.Point(3, 37);
        miniTableOrbit.ManualRowHeight = 26;
        miniTableOrbit.Name = "miniTableOrbit";
        miniTableOrbit.RowTemplate.Height = 26;
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
        miniTableTwins.CellPadding = new System.Windows.Forms.Padding(6, 2, 6, 2);
        miniTableTwins.ColumnHeadersHeight = 26;
        miniTableTwins.Dock = System.Windows.Forms.DockStyle.Fill;
        miniTableTwins.LatexFontSizeInPoints = 10F;
        miniTableTwins.LatexFractionStyle = LatexFractionStyle.Slanted; // 260708Ch
        miniTableTwins.LatexThickness = 0.2D;
        miniTableTwins.Location = new System.Drawing.Point(3, 99);
        miniTableTwins.ManualRowHeight = 26;
        miniTableTwins.Name = "miniTableTwins";
        miniTableTwins.RowTemplate.Height = 26;
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
        tabReflections.Controls.Add(panelReflSearch); // 260709Cl 追加 (Dock=Top は後から Add した方が外側: labelReflInfo の下・表の上に入る)
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
        miniTableReflections.CellPadding = new System.Windows.Forms.Padding(6, 2, 6, 2);
        miniTableReflections.ColumnHeadersHeight = 26;
        miniTableReflections.Dock = System.Windows.Forms.DockStyle.Fill;
        miniTableReflections.LatexFontSizeInPoints = 10F;
        miniTableReflections.LatexThickness = 0.2D;
        miniTableReflections.Location = new System.Drawing.Point(3, 53);
        miniTableReflections.ManualRowHeight = 26;
        miniTableReflections.Name = "miniTableReflections";
        miniTableReflections.RowTemplate.Height = 26;
        miniTableReflections.Size = new System.Drawing.Size(562, 378);
        miniTableReflections.TabIndex = 1;
        miniTableReflections.TabStop = false;
        //
        // panelReflSearch (260709Cl 追加: 反射探索窓の調整 UI)
        //
        panelReflSearch.AutoSize = true;
        panelReflSearch.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        panelReflSearch.Controls.Add(labelReflMax);
        panelReflSearch.Controls.Add(numericReflMax);
        panelReflSearch.Dock = System.Windows.Forms.DockStyle.Top;
        panelReflSearch.Location = new System.Drawing.Point(3, 53);
        panelReflSearch.Name = "panelReflSearch";
        panelReflSearch.Padding = new System.Windows.Forms.Padding(3, 0, 3, 2);
        panelReflSearch.Size = new System.Drawing.Size(562, 29);
        panelReflSearch.TabIndex = 2;
        panelReflSearch.WrapContents = false;
        //
        // labelReflMax (260709Cl 追加)
        //
        labelReflMax.AutoSize = true;
        labelReflMax.Location = new System.Drawing.Point(6, 0);
        labelReflMax.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
        labelReflMax.Name = "labelReflMax";
        labelReflMax.Size = new System.Drawing.Size(120, 15);
        labelReflMax.TabIndex = 0;
        labelReflMax.Text = "Search window  |h|, |k|, |l| ≤";
        //
        // numericReflMax (260709Cl 追加)
        //
        numericReflMax.Location = new System.Drawing.Point(132, 2);
        numericReflMax.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
        numericReflMax.Maximum = new decimal(new int[] { 8, 0, 0, 0 });
        numericReflMax.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
        numericReflMax.Name = "numericReflMax";
        numericReflMax.Size = new System.Drawing.Size(44, 23);
        numericReflMax.TabIndex = 1;
        numericReflMax.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
        numericReflMax.Value = new decimal(new int[] { 4, 0, 0, 0 });
        numericReflMax.ValueChanged += numericReflMax_ValueChanged;
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
        // tabPointGroups (260712Cl 追加 ③-4: 32 点群型の Hasse 図)
        //
        tabPointGroups.Controls.Add(pictureBoxPointGroups);
        tabPointGroups.Location = new System.Drawing.Point(4, 24);
        tabPointGroups.Name = "tabPointGroups";
        tabPointGroups.Padding = new System.Windows.Forms.Padding(3);
        tabPointGroups.Size = new System.Drawing.Size(568, 434);
        tabPointGroups.TabIndex = 5;
        tabPointGroups.Text = "Point groups";
        tabPointGroups.UseVisualStyleBackColor = true;
        //
        // pictureBoxPointGroups (260712Cl 追加)
        //
        pictureBoxPointGroups.BackColor = System.Drawing.Color.White;
        pictureBoxPointGroups.Dock = System.Windows.Forms.DockStyle.Fill;
        pictureBoxPointGroups.Location = new System.Drawing.Point(3, 3);
        pictureBoxPointGroups.Name = "pictureBoxPointGroups";
        pictureBoxPointGroups.Size = new System.Drawing.Size(562, 428);
        pictureBoxPointGroups.TabIndex = 0;
        pictureBoxPointGroups.TabStop = false;
        pictureBoxPointGroups.SizeChanged += pictureBoxPointGroups_SizeChanged;
        pictureBoxPointGroups.MouseClick += pictureBoxPointGroups_MouseClick;
        //
        // tabElements (260713Cl 追加 ③-2: 対称要素 lost/retained 重ね描き)
        //
        tabElements.Controls.Add(pictureBoxElements);
        tabElements.Location = new System.Drawing.Point(4, 24);
        tabElements.Name = "tabElements";
        tabElements.Padding = new System.Windows.Forms.Padding(3);
        tabElements.Size = new System.Drawing.Size(568, 434);
        tabElements.TabIndex = 6;
        tabElements.Text = "Elements";
        tabElements.UseVisualStyleBackColor = true;
        //
        // pictureBoxElements (260713Cl 追加)
        //
        pictureBoxElements.BackColor = System.Drawing.Color.White;
        pictureBoxElements.Dock = System.Windows.Forms.DockStyle.Fill;
        pictureBoxElements.Location = new System.Drawing.Point(3, 3);
        pictureBoxElements.Name = "pictureBoxElements";
        pictureBoxElements.Size = new System.Drawing.Size(562, 428);
        pictureBoxElements.TabIndex = 0;
        pictureBoxElements.TabStop = false;
        pictureBoxElements.SizeChanged += pictureBoxElements_SizeChanged;
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
        ((System.ComponentModel.ISupportInitialize)numericIsoMax).EndInit(); // 260709Cl 追加
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
        tabReflections.PerformLayout(); // 260709Cl: panelReflSearch (AutoSize) のため
        ((System.ComponentModel.ISupportInitialize)miniTableReflections).EndInit();
        panelReflSearch.ResumeLayout(false); // 260709Cl 追加
        panelReflSearch.PerformLayout(); // 260709Cl 追加
        ((System.ComponentModel.ISupportInitialize)numericReflMax).EndInit(); // 260709Cl 追加
        tabDiagram.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)pictureBoxGraph).EndInit();
        tabPointGroups.ResumeLayout(false); // 260712Cl 追加
        ((System.ComponentModel.ISupportInitialize)pictureBoxPointGroups).EndInit(); // 260712Cl 追加
        tabElements.ResumeLayout(false); // 260713Cl 追加
        ((System.ComponentModel.ISupportInitialize)pictureBoxElements).EndInit(); // 260713Cl 追加
        ResumeLayout(false);
    }

    private System.Windows.Forms.ToolTip toolTip;
    private System.Windows.Forms.Panel panelToolbar;
    private System.Windows.Forms.Label labelIsoMax; // 260709Cl 追加 (Phase 3)
    private System.Windows.Forms.NumericUpDown numericIsoMax; // 260709Cl 追加 (Phase 3)
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
    private System.Windows.Forms.FlowLayoutPanel panelReflSearch; // 260709Cl 追加
    private System.Windows.Forms.Label labelReflMax; // 260709Cl 追加
    private System.Windows.Forms.NumericUpDown numericReflMax; // 260709Cl 追加
    private System.Windows.Forms.TabPage tabDiagram;
    private System.Windows.Forms.PictureBox pictureBoxGraph;
    private System.Windows.Forms.TabPage tabPointGroups; // 260712Cl 追加 (③-4)
    private System.Windows.Forms.PictureBox pictureBoxPointGroups; // 260712Cl 追加
    private System.Windows.Forms.TabPage tabElements; // 260713Cl 追加 (③-2)
    private System.Windows.Forms.PictureBox pictureBoxElements; // 260713Cl 追加
}
