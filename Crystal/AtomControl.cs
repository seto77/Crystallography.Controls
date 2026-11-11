//using Microsoft.Scripting.Utils; // 261111Cl 変更前: 未使用 (Split は Crystallography の拡張、Select/Cast は System.Linq。ビルドで確認)
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Crystallography.Controls;

[ToolboxItem(true)] // 260605Cl 追加: 基底 UserControlBase の [ToolboxItem(false)] 継承を打ち消しデザイナのツールボックスに表示
public partial class AtomControl : UserControlBase
{
    #region プロパティ, フィールド, イベントハンドラ
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Crystal Crystal
    {
        get => crystal;
        set
        {
            crystal = value;
            if (crystal == null) return;
            SuspendLayout();
            try // 261111Cl 追加: 例外時も ResumeLayout を必ず呼ぶ
            { // 261111Cl 追加
            table.Clear();
            AddRange(Crystal.Atoms);
            // enabledColumn の Visible が予期せず変わることがあるので appearanceTabVisible で再設定
            dataGridView.Columns["enabledColumn"].Visible = appearanceTabVisible;
            //ResumeLayout(); // 261111Cl 変更前
            } // 261111Cl 追加
            finally { ResumeLayout(); } // 261111Cl 変更: finally へ
        }
    }
    private Crystal crystal = null;

    public int SymmetrySeriesNumber => crystal != null ? crystal.SymmetrySeriesNumber : 0;

    private readonly DataSet.DataTableAtomDataTable table;
    //260703Cl 変更前: 行 index を 3値センチネル (UseCurrentRow=-1 / NoRow=-2 / 行番号) で持ち回っていた。
    //          行は MouseDown での CurrentCell 移動により bindingSource.Position が正となるため、必要な状態は「空白領域の右クリックだった」の 1 bit のみ。
    //private const int AtomGridContextMenuUseCurrentRow = -1; // 260628Ch
    //private const int AtomGridContextMenuNoRow = -2; // 260628Ch
    //private int atomGridContextMenuRowIndex = AtomGridContextMenuUseCurrentRow; // 260628Ch
    private bool atomGridEmptyAreaRightClick = false; // 260703Cl

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SkipEvent { get; set; } = false;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [DefaultValue(false)] // 260607Cl
    public bool AtomicPositionError
    {
        get => atmicPositionError;
        set
        {
            atmicPositionError = value;
            //260715Cl 整理: err 列の表示切替が ColumnStyles 幅操作から Visible 切替に変わり、両分岐が bool 値違いのみになったため if/else を畳んだ (直下の DebyeWallerError と同形)
            //if (!value)
            //{
            //    tableLayoutPanel1.ColumnStyles[4].SizeType = tableLayoutPanel1.ColumnStyles[7].SizeType = SizeType.Absolute;
            //    tableLayoutPanel1.ColumnStyles[4].Width = tableLayoutPanel1.ColumnStyles[7].Width = 0;
            //}
            //else foreach (int i in (int[])[1, 3, 4, 6, 7])
            //{
            //    tableLayoutPanel1.ColumnStyles[i].SizeType = SizeType.Percent;
            //    tableLayoutPanel1.ColumnStyles[i].Width = 20;
            //}
            numericBoxXerr.Visible = numericBoxYerr.Visible = numericBoxZerr.Visible = numericBoxOccerr.Visible = value;
            numericBoxXerr.TabStop = numericBoxYerr.TabStop = numericBoxZerr.TabStop = numericBoxOccerr.TabStop = value;
        }
    }
    private bool atmicPositionError = false;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [DefaultValue(false)] // 260607Cl
    public bool DebyeWallerError
    {
        get => debyeWallerError;
        set
        {
            debyeWallerError = value;
            numericBoxBisoerr.Visible = numericBoxB11err.Visible = numericBoxB12err.Visible =
                numericBoxB13err.Visible = numericBoxB22err.Visible = numericBoxB23err.Visible = numericBoxB33err.Visible = value;
            if (value) numericBoxBiso.Visible = true;
        }
    }
    private bool debyeWallerError = false;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [DefaultValue(true)] // 260607Cl
    public bool UseIsotropy
    {
        get => radioButtonIsotoropy.Checked;
        set { if (value) radioButtonIsotoropy.Checked = true; else radioButtonAnisotropy.Checked = true; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [DefaultValue(false)] // 260607Cl
    public bool UseTypeU
    {
        get => radioButtonDebyeWallerTypeU.Checked;
        set { if (value) radioButtonDebyeWallerTypeU.Checked = true; else radioButtonDebyeWallerTypeB.Checked = true; }
    }

    #region 温度因子 プロパティ
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(0.0)] public double Iso { get => numericBoxBiso.Value; set => numericBoxBiso.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(0.0)] public double IsoErr { get => numericBoxBisoerr.Value; set => numericBoxBisoerr.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(0.0)] public double Aniso11 { get => numericBoxB11.Value; set => numericBoxB11.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(0.0)] public double Aniso11Err { get => numericBoxB11err.Value; set => numericBoxB11err.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(0.0)] public double Aniso12 { get => numericBoxB12.Value; set => numericBoxB12.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(0.0)] public double Aniso12Err { get => numericBoxB12err.Value; set => numericBoxB12err.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(0.0)] public double Aniso13 { get => numericBoxB13.Value; set => numericBoxB13.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(0.0)] public double Aniso13Err { get => numericBoxB13err.Value; set => numericBoxB13err.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(0.0)] public double Aniso22 { get => numericBoxB22.Value; set => numericBoxB22.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(0.0)] public double Aniso22Err { get => numericBoxB22err.Value; set => numericBoxB22err.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(0.0)] public double Aniso23 { get => numericBoxB23.Value; set => numericBoxB23.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(0.0)] public double Aniso23Err { get => numericBoxB23err.Value; set => numericBoxB23err.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(0.0)] public double Aniso33 { get => numericBoxB33.Value; set => numericBoxB33.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(0.0)] public double Aniso33Err { get => numericBoxB33err.Value; set => numericBoxB33err.Value = value; } // 260607Cl
    #endregion

    #region 原子位置 プロパティ
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(0.0)] public double X { get => numericBoxX.Value; set => numericBoxX.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(0.0)] public double XErr { get => numericBoxXerr.Value; set => numericBoxXerr.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(0.0)] public double Y { get => numericBoxY.Value; set => numericBoxY.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(0.0)] public double YErr { get => numericBoxYerr.Value; set => numericBoxYerr.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(0.0)] public double Z { get => numericBoxZ.Value; set => numericBoxZ.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(0.0)] public double ZErr { get => numericBoxZerr.Value; set => numericBoxZerr.Value = value; } // 260607Cl
    #endregion

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(0.0)] public double Occ { get => numericBoxOcc.Value; set => numericBoxOcc.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(0.0)] public double OccErr { get => numericBoxOccerr.Value; set => numericBoxOccerr.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue("")] public string Label { get => textBoxLabel.Text; set => textBoxLabel.Text = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] public int AtomNo { get => comboBoxAtom.SelectedIndex + 1; set => comboBoxAtom.SelectedIndex = value - 1; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(-1)] public int AtomSubNoXray { get => comboBoxScatteringFactorXray.SelectedIndex; set => comboBoxScatteringFactorXray.SelectedIndex = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Atom")] [DefaultValue(-1)] public int AtomSubNoElectron { get => comboBoxScatteringFactorElectron.SelectedIndex; set => comboBoxScatteringFactorElectron.SelectedIndex = value; } // 260607Cl

    private double[] isotopicComposition;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double[] IsotopicComposition
    {
        get => isotopicComposition;
        set
        {
            isotopicComposition = value;
            comboBoxNeutron.SelectedIndex = (isotopicComposition == null || isotopicComposition.Length != AtomStatic.IsotopeAbundance[AtomNo].Count) ? 0 : 1;
            comboBoxNeutron_SelectedIndexChanged(this, EventArgs.Empty);
        }
    }

    #region マテリアル プロパティ
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Material properties")] [DefaultValue(0f)] public float Ambient { get => (float)numericBoxAmbient.Value; set => numericBoxAmbient.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Material properties")] [DefaultValue(0f)] public float Diffusion { get => (float)numericBoxDiffusion.Value; set => numericBoxDiffusion.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Material properties")] [DefaultValue(0f)] public float Specular { get => (float)numericBoxSpecular.Value; set => numericBoxSpecular.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Material properties")] [DefaultValue(0f)] public float Shininess { get => (float)numericBoxShininess.Value; set => numericBoxShininess.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Material properties")] [DefaultValue(0f)] public float Emission { get => (float)numericBoxEmission.Value; set => numericBoxEmission.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Material properties")] [DefaultValue(0f)] public float Alpha { get => (float)numericBoxAlpha.Value; set => numericBoxAlpha.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Material properties")] [DefaultValue(0.0)] public double Radius { get => numericBoxAtomRadius.Value; set => numericBoxAtomRadius.Value = value; } // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Material properties")] [DefaultValue(typeof(Color), "240, 240, 240")] public Color AtomColor { get => colorControlAtomColor.Color; set => colorControlAtomColor.Color = value; } // 260607Cl: 既定はテーマ非依存の固定 (240,240,240)
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Material properties")] [DefaultValue(false)] public bool ShowLabel { get => checkBoxShowLabel.Checked; set => checkBoxShowLabel.Checked = value; } // 260607Cl
    #endregion

    #region Tab の表示/非表示 プロパティ
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Tab")] [DefaultValue(true)] public bool ElementAndPositionTabVisible { get => elementAndPositionTabVisible; set { elementAndPositionTabVisible = value; setTabPages(); } } // 260607Cl
    private bool elementAndPositionTabVisible = true;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Tab")] [DefaultValue(true)] public bool OriginShiftVisible { get => originShiftTabVisible; set { originShiftTabVisible = value; setTabPages(); } } // 260607Cl
    private bool originShiftTabVisible = true;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Tab")] [DefaultValue(true)] public bool DebyeWallerTabVisible { get => debyeWallerTabVisible; set { debyeWallerTabVisible = value; setTabPages(); } } // 260607Cl
    private bool debyeWallerTabVisible = true;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Tab")] [DefaultValue(true)] public bool ScatteringFactorTabVisible { get => scatteringFactorTabVisible; set { scatteringFactorTabVisible = value; setTabPages(); } } // 260607Cl
    private bool scatteringFactorTabVisible = true;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Tab")] [DefaultValue(true)] public bool AppearanceTabVisible { get => appearanceTabVisible; set { appearanceTabVisible = value; setTabPages(); } } // 260607Cl
    private bool appearanceTabVisible = true;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] [Category("Tab")] [DefaultValue(0)] public int SelectedTabIndex { get => tabControl.SelectedIndex; set => tabControl.SelectedIndex = value; } // 260607Cl
    #endregion

    /// <summary>原子のパラメータが変更された時のイベント</summary>
    public event EventHandler ItemsChanged;

    /// <summary>GLEnabled チェックが変更された時のみのイベント (現状 FormStructure のみが受け取る)</summary>
    public event EventHandler GLEnableChanged;

    #endregion

    public AtomControl()
    {
        InitializeComponent();
        if (DesignMode) return; // (260322Ch) Designer 安定化: InitializeComponent 後に design-time 追加初期化だけ止める
        SkipEvent = true;
        table = dataSet.DataTableAtom;
        comboBoxAtom.SelectedIndex = 0;
        comboBoxNeutron.SelectedIndex = 0;

        // 一部 numericBox の Up/Down が消えるので強制 ON
        numericBoxAmbient.ShowUpDown = numericBoxDiffusion.ShowUpDown = numericBoxSpecular.ShowUpDown = numericBoxShininess.ShowUpDown =
            numericBoxEmission.ShowUpDown = numericBoxAlpha.ShowUpDown = numericBoxAtomRadius.ShowUpDown = true;

        dataGridView.Columns["enabledColumn"].Visible = false;
        SkipEvent = false;

        ControlHelper.EnableDoubleBuffering(dataGridView); // 260717Cl: 6 ファイル重複のリフレクション 1 行を ControlHelper へ集約
    }

    private void setTabPages()
    {
        tabControl.TabPages.Clear();
        if (ElementAndPositionTabVisible) tabControl.TabPages.Add(tabPageElementAndPosition);
        if (originShiftTabVisible) tabControl.TabPages.Add(tabPageOriginShift);
        if (DebyeWallerTabVisible) tabControl.TabPages.Add(tabPageDebyeWaller);
        if (ScatteringFactorTabVisible) tabControl.TabPages.Add(tabPageScatteringFactor);
        if (AppearanceTabVisible) tabControl.TabPages.Add(tabPageAppearance);
    }

    private void radioButtonIsotoropy_CheckedChanged(object sender, EventArgs e)
    {
        flowLayoutPanelAniso1.Visible = flowLayoutPanelAniso2.Visible = !radioButtonIsotoropy.Checked;
        flowLayoutPanelIso.Visible = radioButtonIsotoropy.Checked;
        //labelDimension.Text = radioButtonDebyeWallerTypeB.Checked && radioButtonAnisotropy.Checked ? "None" : "Å²"; // 261105Cl 変更前
        labelDimension.Text = DimensionText(); // 261105Cl 変更
    }

    private void radioButtonDebyeWallerTypeU_CheckedChanged(object sender, EventArgs e)
    {
        // 261105Cl 追加: 表記を切り替えたら、表示中の値と誤差を同じ物理量のまま新しい表記へ換算する。以前は見出しだけが変わり、
        // そのまま Change すると B の値が U として (またはその逆に) 使われていた。等方は B = 8π²U (Å²)、異方性は U_ij (Å²) ⇔ β_ij (無次元) = 2π² a*_i a*_j U_ij。
        // SetToInterface の中 (これから原子の値を入れ直す) では換算しない。この CheckedChanged は U 側にだけ配線されているので、切替 1 回につき 1 回呼ばれる
        if (!settingInterface && Crystal != null)
            SetDsfToInterface(GetDsfFromInterface(!radioButtonDebyeWallerTypeU.Checked), radioButtonDebyeWallerTypeU.Checked);

        var prefix = radioButtonDebyeWallerTypeU.Checked ? "U" : "B";
        numericBoxBiso.HeaderText = $"{prefix}iso";
        //numericBoxB11.HeaderText = $"{prefix}11"; // 261105Cl 変更前 (6 行)
        //numericBoxB22.HeaderText = $"{prefix}22";
        //numericBoxB33.HeaderText = $"{prefix}33";
        //numericBoxB12.HeaderText = $"{prefix}12";
        //numericBoxB23.HeaderText = $"{prefix}23";
        //numericBoxB13.HeaderText = $"{prefix}13";
        // 261105Cl 変更: B 表記の異方性の 6 成分は無次元の β_ij (AMCSD の B(i,j) と同じ。β_ij = ¼ a*_i a*_j B_ij) なので、見出しを β にする
        var anisoPrefix = radioButtonDebyeWallerTypeU.Checked ? "U" : "β";
        numericBoxB11.HeaderText = $"{anisoPrefix}11";
        numericBoxB22.HeaderText = $"{anisoPrefix}22";
        numericBoxB33.HeaderText = $"{anisoPrefix}33";
        numericBoxB12.HeaderText = $"{anisoPrefix}12";
        numericBoxB23.HeaderText = $"{anisoPrefix}23";
        numericBoxB13.HeaderText = $"{anisoPrefix}13";
        //labelDimension.Text = radioButtonDebyeWallerTypeB.Checked && radioButtonAnisotropy.Checked ? "None" : "Å²"; // 261105Cl 変更前
        labelDimension.Text = DimensionText(); // 261105Cl 変更
    }

    /// <summary>261105Cl 追加: 単位の表示。B 表記の異方性 (β) は無次元。以前の "None" は単位が無いのか値が無いのか紛らわしかった</summary>
    private string DimensionText() => radioButtonDebyeWallerTypeB.Checked && radioButtonAnisotropy.Checked
        ? (System.Globalization.CultureInfo.CurrentUICulture.Name == "ja" ? "無次元" : "dimensionless") : "Å²";

    /// <summary>261105Cl 追加: SetToInterface の実行中は true (B/U の切替で値を換算しない)</summary>
    private bool settingInterface = false;

    private void comboBoxAtom_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (SkipEvent) return;
        if (comboBoxAtom.SelectedIndex < 0) return;

        // X 線散乱因子のメソッド一覧をコンボに追加
        comboBoxScatteringFactorXray.Items.Clear();
        foreach (var wk in AtomStatic.XrayScatteringWK[AtomNo])
            comboBoxScatteringFactorXray.Items.Add(wk.Method);
        comboBoxScatteringFactorXray.SelectedIndex = 0;

        // 電子散乱因子のメソッド一覧
        comboBoxScatteringFactorElectron.Items.Clear();
        foreach (var p in AtomStatic.ElectronScatteringPeng[AtomNo])
            comboBoxScatteringFactorElectron.Items.Add(p.Method);
        comboBoxScatteringFactorElectron.SelectedIndex = 0;

        // 中性子散乱長
        // 261105Cl 追加 (監査 A18): 元素を変えたら、前の元素のカスタムの同位体組成を捨てる (以前は残り、同位体の数が同じ元素では
        // 「Custom」にすると前の元素の組成がそのまま使われた)。SetToInterface では元素を入れた後に原子の組成を入れ直すので影響しない
        isotopicComposition = null;
        comboBoxNeutron.SelectedIndex = 0;
        comboBoxNeutron_SelectedIndexChanged(this, EventArgs.Empty);

        // 原子サイズ・色
        var (radius, argb) = AtomStatic.GetVesta(AtomNo);
        Radius = radius;
        AtomColor = Color.FromArgb(argb);
    }

    private void comboBoxAtomSub_SelectedIndexChanged(object sender, EventArgs e) { }

    private void checkBoxAtomicPositionError_CheckedChanged(object sender, EventArgs e) =>
        AtomicPositionError = checkBoxDetailAtomicPositionError.Checked;

    private void checkBoxDebyeWallerError_CheckedChanged(object sender, EventArgs e) =>
        DebyeWallerError = checkBoxDetailsDebyeWallerError.Checked;

    #region 中性子関連
    private void comboBoxNeutron_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (SkipEvent) return;

        buttonEditIsotopeAbundance.Enabled = comboBoxNeutron.SelectedIndex == 1;

        richTextBoxIsotope.Clear();
        int n = 0;
        var abundance = AtomStatic.IsotopeAbundance[AtomNo];
        bool useCustomIso = comboBoxNeutron.SelectedIndex == 1 && isotopicComposition != null && isotopicComposition.Length == abundance.Count;
        //var fontSmall = new Font(WineCompat.Resolve("Tahoma"), 6f, FontStyle.Regular); //260610Cl Wine時フォント切替 // (260611Ch) 旧: 未解放
        using var fontSmall = new Font(WineCompat.Resolve("Tahoma"), 6f, FontStyle.Regular); //260610Cl Wine時フォント切替 // (260611Ch)
        using var fontNormal = new Font(WineCompat.Resolve("Tahoma"), 9f, FontStyle.Regular); //260610Cl Wine時フォント切替 // (260611Ch)
        var elementName = AtomStatic.AtomicName(AtomNo);
        foreach (int z in abundance.Keys)
        {
            richTextBoxIsotope.SelectionColor = Color.DarkBlue;
            if (richTextBoxIsotope.Text != "")
                richTextBoxIsotope.SelectedText = ", ";

            richTextBoxIsotope.SelectionCharOffset = 3;
            richTextBoxIsotope.SelectionFont = fontSmall;
            richTextBoxIsotope.SelectedText = z.ToString();

            richTextBoxIsotope.SelectionCharOffset = 0;
            richTextBoxIsotope.SelectionFont = fontNormal;
            richTextBoxIsotope.SelectedText = $"{elementName}: ";

            richTextBoxIsotope.SelectionColor = Color.Black;
            richTextBoxIsotope.SelectedText = (useCustomIso ? isotopicComposition[n++] : abundance[z]).ToString();

            richTextBoxIsotope.SelectionColor = Color.DarkBlue;
            richTextBoxIsotope.SelectionFont = fontNormal;
            richTextBoxIsotope.SelectedText = "%";
        }
    }

    private void buttonEditIsotopeAbundance_Click(object sender, EventArgs e)
    {
        var f = new FormIsotopeComposition { AtomNumber = AtomNo, IsotopicComposition = isotopicComposition };
        if (f.ShowDialog() == DialogResult.OK)
            IsotopicComposition = f.IsotopicComposition;
    }
    #endregion

    #region データベース操作
    public void Add(Atoms atoms)
    {
        if (atoms != null) table.Add(atoms);
        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AddRange(IEnumerable<Atoms> atoms)
    {
        if (atoms == null) return;
        SkipEvent = true;
        dataGridView.SuspendLayout();
        //foreach (var a in atoms) table.Add(a); // 261111Cl 変更前
        // 261111Cl 追加: 1 行ごとの ListChanged (表の行の追加とレイアウト) をやめ、追加し終えてから表を 1 回作り直す。呼び出し元は全部 table.Clear() の直後なので、選択行は従来どおり先頭 (CurrencyManager は Reset で空→0 行目)。
        //  ResetBindings は SkipEvent の間に行い、その PositionChanged では SetToInterface しない (最後の明示の呼び出しが 1 回だけ行う。従来と同じ)
        var raise = bindingSource.RaiseListChangedEvents; // 261111Cl 追加
        bindingSource.RaiseListChangedEvents = false; // 261111Cl 追加
        try { foreach (var a in atoms) table.Add(a); } // 261111Cl
        finally // 261111Cl 追加
        {
            bindingSource.RaiseListChangedEvents = raise;
            if (raise) bindingSource.ResetBindings(false);
        }
        dataGridView.ResumeLayout();
        SkipEvent = false;
        ItemsChanged?.Invoke(this, EventArgs.Empty);
        bindingSource_PositionChanged(this, EventArgs.Empty);
    }

    public void Delete(int i) { table.Remove(i); ItemsChanged?.Invoke(this, EventArgs.Empty); }
    public void Replace(Atoms atoms, int i) { table.Replace(atoms, i); ItemsChanged?.Invoke(this, EventArgs.Empty); }
    public void Clear() { table.Rows.Clear(); ItemsChanged?.Invoke(this, EventArgs.Empty); }

    /// <summary>指定した空間群番号に従って全ての原子情報を再設定。
    /// 261105Cl 変更 (監査 A11): 引数の空間群を使い、表の原子は複製してから設定し直す。以前は引数を無視して今の結晶の空間群 (SymmetrySeriesNumber) を掛けていた。
    /// 表の原子は結晶リストの元の結晶の原子と同じオブジェクトなので、複製せずに引数の空間群を掛けると Replace 前でも元の結晶が書き換わる (監査 #22)</summary>
    public void ResetSymmetry(int symmetrySeriesNumber)
    {
        for (int i = 0; i < table.Rows.Count; i++)
        {
            //var a = table.Get(i); // 261105Cl 変更前 (2 行)
            //a.ResetSymmetry(SymmetrySeriesNumber);
            var a = (Atoms)table.Get(i).Clone(); // 261105Cl 変更
            a.ResetSymmetry(symmetrySeriesNumber); // 261105Cl 変更
            table.Replace(a, i);
        }
        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    public Atoms[] GetAll() => table.GetAll();
    #endregion

    #region Atom クラスを画面下部から生成 / 表示
    public void SetToInterface(Atoms atoms)
    {
        Label = atoms.Label; AtomNo = atoms.AtomicNumber;
        AtomSubNoXray = atoms.SubNumberXray;
        AtomSubNoElectron = atoms.SubNumberElectron;
        IsotopicComposition = atoms.Isotope;

        X = atoms.X; XErr = atoms.X_err;
        Y = atoms.Y; YErr = atoms.Y_err;
        Z = atoms.Z; ZErr = atoms.Z_err;
        Occ = atoms.Occ; OccErr = atoms.Occ_err;

        settingInterface = true; // 261105Cl 追加: 次の UseTypeU の代入で値を換算しない (値はこの後に入れる)
        UseIsotropy = atoms.Dsf.UseIso;
        UseTypeU = atoms.Dsf.OriginalType == DiffuseScatteringFactor.Type.U;
        settingInterface = false; // 261105Cl 追加

        //var d = atoms.Dsf; // 261105Cl 変更前 (17 行。SetDsfToInterface へ移した。B/U の切替の換算でも使う)
        //Iso = (UseTypeU ? d.Uiso : d.Biso) * 100;
        //Aniso11 = UseTypeU ? d.U11 * 100 : d.B11;
        //Aniso12 = UseTypeU ? d.U12 * 100 : d.B12;
        //Aniso13 = UseTypeU ? d.U31 * 100 : d.B31;
        //Aniso22 = UseTypeU ? d.U22 * 100 : d.B22;
        //Aniso23 = UseTypeU ? d.U23 * 100 : d.B23;
        //Aniso33 = UseTypeU ? d.U33 * 100 : d.B33;
        //
        //IsoErr = (UseTypeU ? d.Uiso_err : d.Biso_err) * 100;
        //Aniso11Err = UseTypeU ? d.U11_err * 100 : d.B11_err;
        //Aniso12Err = UseTypeU ? d.U12_err * 100 : d.B12_err;
        //Aniso13Err = UseTypeU ? d.U31_err * 100 : d.B31_err;
        //Aniso22Err = UseTypeU ? d.U22_err * 100 : d.B22_err;
        //Aniso23Err = UseTypeU ? d.U23_err * 100 : d.B23_err;
        //Aniso33Err = UseTypeU ? d.U33_err * 100 : d.B33_err;
        SetDsfToInterface(atoms.Dsf, UseTypeU); // 261105Cl 変更

        Ambient = atoms.Ambient;
        Diffusion = atoms.Diffusion;
        Emission = atoms.Emission;
        Shininess = atoms.Shininess;
        Specular = atoms.Specular;

        Radius = atoms.Radius;
        AtomColor = Color.FromArgb(atoms.Argb);
        Alpha = Color.FromArgb(atoms.Argb).A / 255f;
        ShowLabel = atoms.ShowLabel;
    }

    /// <summary>261105Cl 追加 (SetToInterface から移した): 温度因子を画面へ。typeU なら U (Å²)、でなければ等方は B (Å²)・異方性は β (無次元)</summary>
    private void SetDsfToInterface(DiffuseScatteringFactor d, bool typeU)
    {
        Iso = (typeU ? d.Uiso : d.Biso) * 100;
        Aniso11 = typeU ? d.U11 * 100 : d.B11;
        Aniso12 = typeU ? d.U12 * 100 : d.B12;
        Aniso13 = typeU ? d.U31 * 100 : d.B31;
        Aniso22 = typeU ? d.U22 * 100 : d.B22;
        Aniso23 = typeU ? d.U23 * 100 : d.B23;
        Aniso33 = typeU ? d.U33 * 100 : d.B33;

        IsoErr = (typeU ? d.Uiso_err : d.Biso_err) * 100;
        Aniso11Err = typeU ? d.U11_err * 100 : d.B11_err;
        Aniso12Err = typeU ? d.U12_err * 100 : d.B12_err;
        Aniso13Err = typeU ? d.U31_err * 100 : d.B31_err;
        Aniso22Err = typeU ? d.U22_err * 100 : d.B22_err;
        Aniso23Err = typeU ? d.U23_err * 100 : d.B23_err;
        Aniso33Err = typeU ? d.U33_err * 100 : d.B33_err;
    }

    /// <summary>261105Cl 追加 (GetFromInterface から移した): 画面の温度因子を、typeU の表記で書かれた値として読む</summary>
    private DiffuseScatteringFactor GetDsfFromInterface(bool typeU)
    {
        double[] aniso = typeU
            ? [Aniso11 / 100, Aniso22 / 100, Aniso33 / 100, Aniso12 / 100, Aniso23 / 100, Aniso13 / 100]
            : [Aniso11, Aniso22, Aniso33, Aniso12, Aniso23, Aniso13];

        double[] anisoErr = typeU
            ? [Aniso11Err / 100, Aniso22Err / 100, Aniso33Err / 100, Aniso12Err / 100, Aniso23Err / 100, Aniso13Err / 100]
            : [Aniso11Err, Aniso22Err, Aniso33Err, Aniso12Err, Aniso23Err, Aniso13Err];

        return new DiffuseScatteringFactor(typeU ? DiffuseScatteringFactor.Type.U : DiffuseScatteringFactor.Type.B,
            UseIsotropy, Iso / 100, IsoErr / 100, aniso, anisoErr, Crystal.CellValue);
    }

    private Atoms GetFromInterface()
    {
        //double[] aniso = UseTypeU // 261105Cl 変更前 (10 行。GetDsfFromInterface へ移した)
        //    ? [Aniso11 / 100, Aniso22 / 100, Aniso33 / 100, Aniso12 / 100, Aniso23 / 100, Aniso13 / 100]
        //    : [Aniso11, Aniso22, Aniso33, Aniso12, Aniso23, Aniso13];
        //
        //double[] anisoErr = UseTypeU
        //    ? [Aniso11Err / 100, Aniso22Err / 100, Aniso33Err / 100, Aniso12Err / 100, Aniso23Err / 100, Aniso13Err / 100]
        //    : [Aniso11Err, Aniso22Err, Aniso33Err, Aniso12Err, Aniso23Err, Aniso13Err];
        //
        //var dsf = new DiffuseScatteringFactor(UseTypeU ? DiffuseScatteringFactor.Type.U : DiffuseScatteringFactor.Type.B,
        //    UseIsotropy, Iso / 100, IsoErr / 100, aniso, anisoErr, Crystal.CellValue);
        var dsf = GetDsfFromInterface(UseTypeU); // 261105Cl 変更
        var material = new Material(AtomColor.ToArgb(), (Ambient, Diffusion, Specular, Shininess, Emission), Alpha);

        //return new Atoms(Label, AtomNo, AtomSubNoXray, AtomSubNoElectron, IsotopicComposition, // 261105Cl 変更前
        // 261105Cl 変更 (監査 A18): 「Natural isotope abundance」を選んでいるときは、前に設定したカスタムの同位体組成を渡さない
        // (以前は渡していたので、Natural に戻しても中性子の散乱長にカスタムの組成が使われ続けた)
        return new Atoms(Label, AtomNo, AtomSubNoXray, AtomSubNoElectron, comboBoxNeutron.SelectedIndex == 1 ? IsotopicComposition : null,
            SymmetrySeriesNumber, new Vector3D(X, Y, Z), new Vector3D(XErr, YErr, ZErr), Occ, OccErr, dsf,
            material, (float)Radius, true, ShowLabel);
    }
    #endregion

    #region 原子追加・削除などのボタン
    private void buttonAdd_Click(object sender, EventArgs e)
    {
        var atoms = GetFromInterface();
        if (atoms != null)
        {
            Add(atoms);
            bindingSource.Position = bindingSource.Count - 1;
        }
    }

    // 260717Cl (/simplify): Change 系 3 ボタンで重複していた「現在行取得→Replace→Position 復元」の足場を
    // ReplaceCurrent へ集約 (各ボタンはタブ別の追加コピー処理だけを渡す)。
    private void buttonChange_Click(object sender, EventArgs e)
        => ReplaceCurrent();

    private void buttonChangeToSameElement_Click(object sender, EventArgs e)
        => ReplaceCurrent((atoms, pos) =>
        {
            if (tabControl.SelectedTab == tabPageAppearance)
                CopyAppearance(atoms, pos);
            else if (tabControl.SelectedTab == tabPageDebyeWaller)
                CopyDebyeWaller(atoms, pos, true);
        });

    private void buttonApplyToAllElements_Click(object sender, EventArgs e)
        => ReplaceCurrent((atoms, pos) =>
        {
            if (tabControl.SelectedTab == tabPageDebyeWaller)
                CopyDebyeWaller(atoms, pos, false);
        });

    /// <summary>260717Cl 追加 (/simplify): 現在行を interface の内容で置換する共通足場 (旧: 3 ハンドラに同型コピペ)。</summary>
    private void ReplaceCurrent(Action<Atoms, int> extra = null)
    {
        var pos = bindingSource.Position;
        if (pos < 0) return;
        var atoms = GetFromInterface();
        atoms.GLEnabled = table.Get(pos).GLEnabled; // 261105Cl 追加: 表示/非表示 (表の左端のチェック) は画面下部に欄が無いので、置換前の行から引き継ぐ (GetFromInterface は常に true で作るため、隠した原子が Change で再表示されていた)
        Replace(atoms, pos);
        extra?.Invoke(atoms, pos);
        bindingSource.Position = pos;
    }

    public void CopyAppearance(Atoms atoms, int i)
    {
        foreach (var a in dataSet.DataTableAtom.GetAll().Where(a => a.AtomicNumber == atoms.AtomicNumber))
        {
            a.Texture = atoms.Texture;
            a.Radius = atoms.Radius;
            a.Argb = atoms.Argb;
            a.ShowLabel = atoms.ShowLabel;
        }
        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void CopyDebyeWaller(Atoms atoms, int i, bool onlySameElements)
    {
        var others = onlySameElements
            ? dataSet.DataTableAtom.GetAll().Where(a => a.AtomicNumber == atoms.AtomicNumber)
            : dataSet.DataTableAtom.GetAll();
        foreach (var a in others) a.Dsf = atoms.Dsf;
        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void buttonDelete_Click(object sender, EventArgs e)
    {
        int pos = bindingSource.Position;
        if (pos < 0) return;
        SkipEvent = true;
        Delete(pos);
        SkipEvent = false;
        bindingSource.Position = bindingSource.Count > pos ? pos : pos - 1;
    }

    private void buttonUp_Click(object sender, EventArgs e)
    {
        int n = bindingSource.Position;
        if (n <= 0) return;
        table.MoveItem(n, n - 1);
        // 261025Cl 追加 (作者決定): 並べ替えも変更として通知し、Crystal.Atoms の順 (化学組成フォームの「登録順」、GL 表示チェックの行番号) をすぐ揃える。
        // 旧来は通知せず、次の変更まで Crystal 側が古い順のままだった (並べ替え後の GL チェックが別の原子に効くバグの原因)。
        // 通知で CrystalControl が原子リストを入れ直して選択が先頭に戻るので、選択位置は通知の「後」に設定する (Change/Delete ボタンと同じ順)
        ItemsChanged?.Invoke(this, EventArgs.Empty);
        bindingSource.Position = n - 1;
    }

    private void buttonDown_Click(object sender, EventArgs e)
    {
        int n = bindingSource.Position;
        if (n >= bindingSource.Count - 1) return;
        table.MoveItem(n, n + 1);
        ItemsChanged?.Invoke(this, EventArgs.Empty); // 261025Cl 追加 (buttonUp_Click と同じ理由。選択位置は通知の後に設定する)
        bindingSource.Position = n + 1;
    }
    #endregion

    private void bindingSource_PositionChanged(object sender, EventArgs e)
    {
        if (SkipEvent) return;
        if (bindingSource.Position >= 0 && bindingSource.Count > 0)
            SetToInterface(table.Get(bindingSource.Position));
    }

    private void listBoxAtoms_MouseUp(object sender, MouseEventArgs e) { }
    private void listBoxAtoms_MouseLeave(object sender, EventArgs e) { }

    private void buttonOriginShift_Click(object sender, EventArgs e)
    {
        var button = sender as Button;
        var shift = button.Name.Contains("Custom")
            ? new Vector3DBase(numericBoxOriginShiftX.Value, numericBoxOriginShiftY.Value, numericBoxOriginShiftZ.Value)
            : new Vector3DBase((button.Tag as string).Split(" ", true).Select(s => s.ToDouble()).ToArray()) * (radioButtonOriginShiftPlus.Checked ? 1 : -1);

        SkipEvent = true;
        var atomArray = GetAll();
        for (int i = 0; i < atomArray.Length; i++)
        {
            var atoms = Deep.Copy(atomArray[i]);
            atoms.X += shift.X;
            atoms.Y += shift.Y;
            atoms.Z += shift.Z; // 260426Cl: 旧コードは shift.Y を Z に加算していた typo
            atoms.ResetSymmetry(SymmetrySeriesNumber);
            table.Replace(atoms, i);
        }
        SkipEvent = false;
        bindingSource_PositionChanged(sender, e);
        ItemsChanged?.Invoke(this, e);
    }

    private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
    {
        buttonApplyToSameElement.Visible = tabControl.SelectedTab == tabPageAppearance || tabControl.SelectedTab == tabPageDebyeWaller;
        buttonApplyToAllElements.Visible = tabControl.SelectedTab == tabPageDebyeWaller;
    }

    private void dataGridViewAtom_CellValueChanged(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex != 0) return;
        var enabled = (bool)dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
        Crystal.Atoms[e.RowIndex].GLEnabled = table.Get(e.RowIndex).GLEnabled = enabled;
        GLEnableChanged?.Invoke(this, EventArgs.Empty);
    }

    private void dataGridView_CurrentCellDirtyStateChanged(object sender, EventArgs e)
    {
        if (dataGridView.CurrentCellAddress.X == 0 && dataGridView.IsCurrentCellDirty)
            dataGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }

    private void dataGridView_MouseDown(object sender, MouseEventArgs e) // 260628Ch 追加
    {
        if (e.Button != MouseButtons.Right) return;

        //atomGridContextMenuRowIndex = AtomGridContextMenuNoRow; // 260628Ch → 260703Cl bool 1 個に縮退
        var hit = dataGridView.HitTest(e.X, e.Y);
        //if (hit.RowIndex < 0 || hit.RowIndex >= dataGridView.Rows.Count) return; // 260703Cl HitTest は Rows.Count 以上を返さないため後半は到達不能だった
        atomGridEmptyAreaRightClick = hit.RowIndex < 0; // 260703Cl
        if (atomGridEmptyAreaRightClick) return;

        //atomGridContextMenuRowIndex = hit.RowIndex; // 260628Ch → 260703Cl
        var columnIndex = hit.ColumnIndex;
        //if (columnIndex < 0 || !dataGridView.Columns[columnIndex].Visible) // 260703Cl HitTest は非表示列をヒットしないため後半は到達不能だった (負値は行ヘッダクリック時)
        if (columnIndex < 0)
            columnIndex = dataGridView.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.Visible)?.Index ?? -1; // 260628Ch

        dataGridView.ClearSelection();
        if (columnIndex >= 0)
            dataGridView.CurrentCell = dataGridView.Rows[hit.RowIndex].Cells[columnIndex]; // 260703Cl CurrentCell 移動で bindingSource.Position もこの行に追従する
        dataGridView.Rows[hit.RowIndex].Selected = true;
    }

    private void contextMenuStripAtom_Opening(object sender, CancelEventArgs e) // 260628Ch 追加
    {
        //var rowIndex = atomGridContextMenuRowIndex == AtomGridContextMenuUseCurrentRow ? bindingSource.Position : atomGridContextMenuRowIndex; // 260628Ch → 260703Cl
        var rowIndex = bindingSource.Position; // 260703Cl マウス開きは MouseDown で Position が追従済み・キーボード開きは常に現在行

        var canShow = !atomGridEmptyAreaRightClick // 260703Cl
            && rowIndex >= 0
            && rowIndex < table.Rows.Count
            && table.Get(rowIndex)?.Atom is { Length: > 0 }; // 260628Ch
        atomGridEmptyAreaRightClick = false; // 260703Cl 消費してリセット (次にキーボード等で開いたときに現在行で動くように)
        showEquivalentAtomPositionsToolStripMenuItem.Enabled = canShow;
        e.Cancel = !canShow;
        //atomGridContextMenuRowIndex = canShow ? rowIndex : AtomGridContextMenuUseCurrentRow; // 260628Ch → 260703Cl 使用後も行番号が残り、後からキーボードで開くと過去のマウス行を指す問題があった
    }

    private void showEquivalentAtomPositionsToolStripMenuItem_Click(object sender, EventArgs e) // 260628Ch 追加
    {
        //var rowIndex = atomGridContextMenuRowIndex >= 0 ? atomGridContextMenuRowIndex : bindingSource.Position; // 260628Ch → 260703Cl
        var rowIndex = bindingSource.Position; // 260703Cl
        if (rowIndex < 0 || rowIndex >= table.Rows.Count) return;

        var atoms = table.Get(rowIndex);
        if (atoms == null) return;

        var form = new FormAtomDetailedInfo
        {
            StartPosition = FormStartPosition.Manual,
            Location = Cursor.Position,
        };
        form.Atoms = atoms;
        form.Show(this);
    }
}
