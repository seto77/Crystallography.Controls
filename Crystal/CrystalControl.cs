using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Crystallography.Controls;

[TypeConverter(typeof(DefinitionOrderTypeConverter))]
[ToolboxItem(true)] // 260605Cl 追加: 基底 UserControlBase の [ToolboxItem(false)] 継承を打ち消しデザイナのツールボックスに表示
public partial class CrystalControl : UserControlBase
{
    #region プロパティ、フィールド、イベントハンドラ

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SkipEvent { get; set; } = false;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [DefaultValue(false)] // 260607Cl
    public bool SymmetryInformationVisible { get => FormSymmetryInformation.Visible; set => FormSymmetryInformation.Visible = value; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [DefaultValue(false)] // 260607Cl
    public bool BeamInteractionVisible { get => FormBeamInteraction.Visible; set => FormBeamInteraction.Visible = value; }

    public bool StrainControlVisible => formStrain.Visible;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [DefaultValue(true)] // 260607Cl
    public bool ColorControlVisible { get => colorControl.Visible; set => colorControl.Visible = value; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [DefaultValue(0)] // 260607Cl
    public int SymmetrySeriesNumber { get => symmetryControl.SymmetrySeriesNumber; set => symmetryControl.SymmetrySeriesNumber = value; }

    #region Tab ページの表示/非表示

    [DefaultValue(true)] // 260607Cl
    //[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public bool VisibleBasicInfoTab { get => visibleBasicInfoTab; set { visibleBasicInfoTab = value; setTabPages(); } }
    //private bool visibleBasicInfoTab = true;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public bool VisibleBasicInfoTab { get; set { field = value; setTabPages(); } } = true; // (261106Ch) field へ集約
    [DefaultValue(true)] // 260607Cl
    //[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public bool VisibleAtomTab { get => visibleAtomTab; set { visibleAtomTab = value; setTabPages(); } }
    //private bool visibleAtomTab = true;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public bool VisibleAtomTab { get; set { field = value; setTabPages(); } } = true; // (261106Ch) field へ集約
    [DefaultValue(true)] // 260607Cl
    //[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public bool VisibleElasticityTab { get => visibleElasticityTab; set { visibleElasticityTab = value; setTabPages(); } }
    //private bool visibleElasticityTab = true;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public bool VisibleElasticityTab { get; set { field = value; setTabPages(); } } = true; // (261106Ch) field へ集約
    [DefaultValue(true)] // 260607Cl
    //[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public bool VisibleBondsPolyhedraTab { get => visibleBondsPolyhedraTab; set { visibleBondsPolyhedraTab = value; setTabPages(); } }
    //private bool visibleBondsPolyhedraTab = true;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public bool VisibleBondsPolyhedraTab { get; set { field = value; setTabPages(); } } = true; // (261106Ch) field へ集約
    [DefaultValue(true)] // 260607Cl
    //[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public bool VisibleReferenceTab { get => visibleReferenceTab; set { visibleReferenceTab = value; setTabPages(); } }
    //private bool visibleReferenceTab = true;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public bool VisibleReferenceTab { get; set { field = value; setTabPages(); } } = true; // (261106Ch) field へ集約
    [DefaultValue(true)] // 260607Cl
    //[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public bool VisibleEOSTab { get => visibleEOSTab; set { visibleEOSTab = value; setTabPages(); } }
    //private bool visibleEOSTab = true;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public bool VisibleEOSTab { get; set { field = value; setTabPages(); } } = true; // (261106Ch) field へ集約
    [DefaultValue(false)] // 260607Cl
    //[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public bool VisibleStressStrainTab { get => visibleStressStrainTab; set { visibleStressStrainTab = value; setTabPages(); } }
    //private bool visibleStressStrainTab = false;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public bool VisibleStressStrainTab { get; set { field = value; setTabPages(); } } = false; // (261106Ch) field へ集約
    [DefaultValue(false)] // 260607Cl
    //[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public bool VisiblePolycrystallineTab { get => visiblePolycrystallineTab; set { visiblePolycrystallineTab = value; setTabPages(); } }
    //private bool visiblePolycrystallineTab = false;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public bool VisiblePolycrystallineTab { get; set { field = value; setTabPages(); } } = false; // (261106Ch) field へ集約
    [DefaultValue(false)] // 260607Cl
    //[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public bool VisibleBoundTab { get => visibleBoundTab; set { visibleBoundTab = value; setTabPages(); } }
    //private bool visibleBoundTab = false;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public bool VisibleBoundTab { get; set { field = value; setTabPages(); } } = false; // (261106Ch) field へ集約
    [DefaultValue(false)] // 260607Cl
    //[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public bool VisibleLatticePlaneTab { get => visibleLatticePlaneTab; set { visibleLatticePlaneTab = value; setTabPages(); } }
    //private bool visibleLatticePlaneTab = false;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public bool VisibleLatticePlaneTab { get; set { field = value; setTabPages(); } } = false; // (261106Ch) field へ集約

    private void setTabPages()
    {
        tabControl.TabPages.Clear();
        if (VisibleBasicInfoTab) tabControl.TabPages.Add(tabPageBasicInfo);
        if (VisibleAtomTab) tabControl.TabPages.Add(tabPageAtom);
        //if (visibleBondsPolyhedraTab) tabControl.TabPages.Add(tabPageBondsPolyhedra);
        if (VisibleBondsPolyhedraTab) tabControl.TabPages.Add(tabPageBondsPolyhedra); // (261106Ch) field を保持するプロパティから読む
        if (VisibleBoundTab) tabControl.TabPages.Add(tabPageBounds);
        if (VisibleLatticePlaneTab) tabControl.TabPages.Add(tabPageLatticePlane);
        if (VisibleReferenceTab) tabControl.TabPages.Add(tabPageReference);
        if (VisibleEOSTab) tabControl.TabPages.Add(tabPageEOS);
        if (VisibleElasticityTab) tabControl.TabPages.Add(tabPageElasticity);
        if (VisibleStressStrainTab) tabControl.TabPages.Add(tabPageStrainStress);
        if (VisiblePolycrystallineTab) tabControl.TabPages.Add(tabPagePolycrystalline);
    }

    #endregion

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Crystal Crystal
    {
        get => crystal;
        set
        {
            crystal = value;
            if (crystal == null) return;
            // 261105Cl 追加 (監査 A13): 読み込み時の格子定数を持たない結晶 (CIF・AMC・クリップボード・データベースから来たもの) は、初めて表示した時の値を
            // 「Revert cell constants」の戻り先にする (結晶リストの XML から読んだ結晶は ConvertToCrystalList が保存済み)
            if (crystal.InitialA == 0)
                crystal.SaveInitialCellConstants();

            Enabled = !crystal.FlexibleMode;
            SetToInterface();
            // 原子位置チェック (strain control で選択した後、原子位置がおかしくなる問題の修正 (2017/05/29))
            if (crystal.ChemicalFormulaZ == 1)
            {
                foreach (var a in crystal.Atoms)
                    a.ResetSymmetry(SymmetrySeriesNumber);
                crystal.GetFormulaAndDensity();
                SetToInterface();
            }
            CrystalChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private Crystal crystal;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public (double A, double B, double C, double Alpha, double Beta, double Gamma) CellConstants
    {
        get => symmetryControl.CellConstants;
        set => symmetryControl.CellConstants = value;
    }

    [DefaultValue(0.0)] // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public double A { get => symmetryControl.A; set => symmetryControl.A = value; }
    [DefaultValue(0.0)] // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public double B { get => symmetryControl.B; set => symmetryControl.B = value; }
    [DefaultValue(0.0)] // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public double C { get => symmetryControl.C; set => symmetryControl.C = value; }
    [DefaultValue(0.0)] // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public double Alpha { get => symmetryControl.Alpha; set => symmetryControl.Alpha = value; }
    [DefaultValue(0.0)] // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public double Beta { get => symmetryControl.Beta; set => symmetryControl.Beta = value; }
    [DefaultValue(0.0)] // 260607Cl
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] public double Gamma { get => symmetryControl.Gamma; set => symmetryControl.Gamma = value; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [DefaultValue(0)] // 260607Cl
    public int DefaultTabNumber { get => tabControl.SelectedIndex; set => tabControl.SelectedIndex = value; }

    public event EventHandler CrystalChanged;

    public FormBeamInteraction FormBeamInteraction;
    public FormSymmetryInformation FormSymmetryInformation;
    public FormStrain formStrain;
    //public FormChemicalComposition FormChemicalComposition; // 261024Cl 追加: 右クリック「Details of the chemical composition」で開く // 261026Cl 変更前
    /// <summary>右クリック「Details of the chemical composition」で開くフォーム。261024Cl 追加。
    /// 261026Cl 変更: 初めて使う時に作る (CrystalControl は 4 アプリで多数作られ、このフォームは開かれないことが多いので、コンストラクタで毎回作らない)</summary>
    //public FormChemicalComposition FormChemicalComposition => formChemicalComposition ??= new FormChemicalComposition { CrystalControl = this, Visible = false }; // 261026Cl 変更前
    // 261026Cl 変更: デザイナーのプロパティグリッドがこの getter を読んでフォームを作らないよう隠す。破棄済みなら作り直す (FormSymmetryInformation と同じ)
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public FormChemicalComposition FormChemicalComposition
        => formChemicalComposition is { IsDisposed: false } ? formChemicalComposition
        : formChemicalComposition = new FormChemicalComposition { CrystalControl = this, Visible = false };
    private FormChemicalComposition formChemicalComposition; // 261026Cl 追加

    #endregion

    public CrystalControl()
    {
        InitializeComponent();

        FormBeamInteraction = new FormBeamInteraction { CrystalControl = this, Visible = false };
        FormSymmetryInformation = new FormSymmetryInformation { CrystalControl = this, Visible = false };
        formStrain = new FormStrain { CrystalControl = this, Visible = false };
        //FormChemicalComposition = new FormChemicalComposition { CrystalControl = this, Visible = false }; // 261024Cl 追加 // 261026Cl 変更前 (初めて使う時に作る。上のプロパティ参照)
        // 261026Cl 追加: 化学組成フォームは閉じても隠すだけなので Application.OpenForms に残り、この CrystalControl と結晶を参照し続ける。
        // 一時ダイアログ (FormCrystalSelection など) の CrystalControl が破棄された時に一緒に破棄する (Dispose は FormClosing のキャンセルを通らない)
        Disposed += (_, _) => formChemicalComposition?.Dispose();

        FormBeamInteraction.VisibleChanged += formBeamInteraction_VisibleChanged;
        FormSymmetryInformation.VisibleChanged += formSymmetryInformation_VisibleChanged;

        symmetryControl_LengthUnitChanged(this, EventArgs.Empty); // 260729Cl 追加: 体積・数密度の単位表示を初期化 (Designer/resx の Å 前提とコード側を一致させる)
    }

    private void CrystalForm_Load(object sender, EventArgs e)
    {
        textBoxTitle.Size = new Size(tabPageReference.Width - textBoxTitle.Location.X - 2, tabPageReference.Height - textBoxTitle.Location.Y - 2);
        ControlHelper.EnableDoubleBuffering(this); // 260717Cl: 6 ファイル重複のリフレクション 1 行を ControlHelper へ集約
    }

    public event EventHandler BeamInteraction_VisibleChanged;
    public event EventHandler SymmetryInformation_VisibleChanged;

    #region Crystal クラスを画面下部から生成 / にセット

    /// <summary>画面下部に入力された内容から Crystal を生成する</summary>
    public void GenerateFromInterface()
    {
        if (SkipEvent) return;
        SkipEvent = true;

        var (A, B, C, Alpha, Beta, Gamma) = symmetryControl.CellConstants;
        if (A < 0 || B < 0 || C < 0 || Alpha > Math.PI || Beta > Math.PI || Gamma > Math.PI)
        {
            SkipEvent = false;
            //MessageBox.Show("Input valid cell constants"); // 261105Cl 変更前
            // 261105Cl 変更 (監査 A25): 英語だけの固定文だったので 11 言語に。何が正しくないかも書く
            MessageBox.Show(Crystallography.Localization.Loc(
                en: "Invalid cell constants: the lengths must not be negative and the angles must not exceed 180°.",
                ja: "格子定数が正しくありません。長さは負にできず、角度は 180° 以下です。",
                de: "Ungültige Gitterkonstanten: Längen dürfen nicht negativ sein und Winkel 180° nicht überschreiten.",
                fr: "Paramètres de maille non valides : les longueurs ne peuvent pas être négatives et les angles ne peuvent pas dépasser 180°.",
                es: "Constantes de celda no válidas: las longitudes no pueden ser negativas y los ángulos no pueden superar 180°.",
                pt: "Constantes de célula inválidas: os comprimentos não podem ser negativos e os ângulos não podem exceder 180°.",
                it: "Costanti di cella non valide: le lunghezze non possono essere negative e gli angoli non possono superare 180°.",
                ru: "Недопустимые параметры ячейки: длины не могут быть отрицательными, а углы не могут превышать 180°.",
                zhHans: "晶胞常数无效：长度不能为负，角度不能超过 180°。",
                zhHant: "晶胞常數無效：長度不能為負，角度不能超過 180°。",
                ko: "격자 상수가 올바르지 않습니다. 길이는 음수일 수 없고 각도는 180°를 넘을 수 없습니다."),
                Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // 対称性が変更された可能性があるので原子も改めて設定し直す
        atomControl.ResetSymmetry(SymmetrySeriesNumber);

        var rot = crystal != null ? crystal.RotationMatrix : new Matrix3D();
        var valences = crystal?.ElementValences ?? []; // 261025Cl 追加: 化学組成フォームで手入力した価数は UI に無いので、作り直しても消えないよう旧 crystal から引き継ぐ
        var previous = crystal; // 261105Cl 追加 (監査 A13・A19): UI に無い値 (読み込み時の格子定数・応力) を、作り直す前の結晶から引き継ぐ

        crystal = new Crystal(
            symmetryControl.CellConstants, symmetryControl.CellConstantsErr,
            //SymmetrySeriesNumber, textBoxName.Text, colorControl.Color, rot, atomControl.GetAll(), // 261105Cl 変更前
            // 261105Cl 変更: 原子表の原子 (= 結晶リストの元の結晶の原子のことがある) を複製して渡す。新しい結晶のコンストラクタが原子の
            // 温度因子に新しい格子を設定する (Crystal.SyncDsfCell) ので、共有したままだと Replace 前でも元の結晶の係数が変わる
            SymmetrySeriesNumber, textBoxName.Text, colorControl.Color, rot, [.. atomControl.GetAll().Select(a => (Atoms)a.Clone())],
            (textBoxMemo.Text, textBoxAuthor.Text, textBoxJournal.Text, textBoxTitle.Text),
            bondControl.GetAll(), boundControl.GetAll(), latticePlaneControl.GetAll(), eosControl.EOScondition);

        //crystal.ElasticStiffness = elasticityControl1.Stiffness.ToArray(); // 261105Cl 変更前
        // 261105Cl 変更: コンプライアンスで入力中に逆行列が無い (独立な定数が欠けている) と Stiffness が null で例外になっていた。そのときは 0 (未設定) にする
        crystal.ElasticStiffness = (elasticityControl1.Stiffness ?? new DenseMatrix(6, 6)).ToArray();
        // 261106Cl 追加 (作者の決定 C8-3): コンプライアンスで入力・表示しているときは、その値も結晶に持たせる (全部 0 なら持たない)。
        // 逆行列が無いと剛性は全部 0 になり、以前は入力した値が失われていた。持たせた値は計算にも使い (PolyCrystal)、次の SetToInterface で欄に戻す
        crystal.ElasticCompliance = elasticityControl1.Mode == Elasticity.Mode.Compliance && elasticityControl1.Compliance is { } compliance && compliance.Enumerate().Any(v => v != 0)
            ? compliance.ToArray() : null;
        crystal.ElementValences = [.. valences]; // 261025Cl 追加 (上の valences を複製して持たせる)

        crystal.AngleResolution = (double)numericUpDownAngleResolution.Value;
        crystal.SubDivision = (int)numericUpDownAngleSubDivision.Value;
        crystal.GrainSize = (double)numericUpDownCrystallineSize.Value;
        if (poleFigureControl.Crystal != null)
            crystal.Crystallites = poleFigureControl.Crystal.Crystallites;

        crystal.Strain = new Matrix3D(
            numericBoxStrain11.Value, numericBoxStrain12.Value, numericBoxStrain13.Value,
            numericBoxStrain12.Value, numericBoxStrain22.Value, numericBoxStrain23.Value,
            numericBoxStrain13.Value, numericBoxStrain23.Value, numericBoxStrain33.Value);
        crystal.HillCoefficient = numericBoxHill.Value;
        // 261105Cl 追加 (監査 A19、作者の決定「弾性定数の修正と一緒に配線する」): 応力の欄を読む (2020 年以前から読まれず、入れても無視されていた)。
        // 欄は SetToInterface が結晶の値にしておくので、欄が隠れている画面 (主画面・PDIndexer など) でも結晶の応力はそのまま引き継がれる。
        // 単位は弾性定数と同じ (GPa)。座標は歪みと同じ試料 (多結晶) の座標で、多結晶回折の計算 (Crystallite.SetDiffractedPixels) が結晶子ごとに回して使う
        crystal.Stress = new Matrix3D(
            numericBoxStress11.Value, numericBoxStress12.Value, numericBoxStress13.Value,
            numericBoxStress12.Value, numericBoxStress22.Value, numericBoxStress23.Value,
            numericBoxStress13.Value, numericBoxStress23.Value, numericBoxStress33.Value);

        if (previous != null)
        {
            // 261105Cl 追加 (監査 A13): 「Revert cell constants」の戻り先を引き継ぐ。以前は格子定数を一度でも編集すると (結晶を作り直すので) 失われ、Revert が何もしなかった
            (crystal.InitialA, crystal.InitialB, crystal.InitialC, crystal.InitialAlpha, crystal.InitialBeta, crystal.InitialGamma) = (previous.InitialA, previous.InitialB, previous.InitialC, previous.InitialAlpha, previous.InitialBeta, previous.InitialGamma);
            (crystal.InitialA_err, crystal.InitialB_err, crystal.InitialC_err, crystal.InitialAlpha_err, crystal.InitialBeta_err, crystal.InitialGamma_err) = (previous.InitialA_err, previous.InitialB_err, previous.InitialC_err, previous.InitialAlpha_err, previous.InitialBeta_err, previous.InitialGamma_err);
            // 261105Cl 追加 (監査 A19): 応力は画面の欄から読んでいない (欄は配線されていない) ので、結晶の値を消さないよう引き継ぐ
            //crystal.Stress = new Matrix3D(previous.Stress ?? new Matrix3D(0, 0, 0, 0, 0, 0, 0, 0, 0)); // 261105Cl 変更前 (上で応力の欄を読むようにしたので不要)
        }

        SkipEvent = false;
        SetToInterface(false);
        CrystalChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>現在の Crystal によって画面下部のテキストボックスなどを設定する</summary>
    /// <param name="ChangeCellParameter">CellParameter コントロールも更新する場合 true</param>
    public void SetToInterface(bool ChangeCellParameter = true)
    {
        if (SkipEvent) return;
        SkipEvent = true;
        SuspendLayout();

        colorControl.Color = Color.FromArgb(Crystal.Argb);
        textBoxName.Text = Crystal.Name;
        textBoxMemo.Text = Crystal.Note;

        textBoxAuthor.Text = Crystal.PublAuthorName;
        textBoxJournal.Text = Crystal.Journal;
        textBoxFormula.Text = Crystal.ChemicalFormulaSum;
        textBoxTitle.Text = Crystal.PublSectionTitle;

        numericBoxDensity.Value = Crystal.Density;
        numericBoxVolume.Value = Crystal.Volume * volumeScale; // 260729Cl 変更: symmetryControl の Å/nm ラジオに追従する
        //numericBoxVolumeAng.Value = Crystal.Volume * 1000;
        //numericBoxCellVolumeNm.Value = Crystal.Volume;
        numericBoxMolarVolume.Value = Crystal.Volume * UniversalConstants.A / Crystal.ChemicalFormulaZ * 1E-21;
        numericBoxZnumber.Value = Crystal.ChemicalFormulaZ;
        numericBoxMolarMass.Value = numericBoxDensity.Value * numericBoxMolarVolume.Value;
        numericBoxCellMass.Value = numericBoxDensity.Value * Crystal.Volume * Ang3PerNm3; // 260729Cl 変更: 表示単位に依らず Å³ 基準で計算する (旧: numericBoxVolumeAng.Value)
        //numericBoxCellMass.Value = numericBoxDensity.Value * numericBoxVolumeAng.Value;

        // 260606Cl 追加: 平均原子番号(個数/質量加重)・平均原子量・原子数密度・電子数密度。
        // ElementNum は Multiplicity*Occ (占有率込み) なので、これらの量も占有率を反映する。
        double sumN = 0, sumNZ = 0, sumNA = 0, sumNAZ = 0;
        if (Crystal.ElementName != null)
            for (int i = 0; i < Crystal.ElementName.Length; i++)
            {
                int z = AtomStatic.AtomicNumber(Crystal.ElementName[i]);
                double n = Crystal.ElementNum[i], aw = AtomStatic.AtomicWeight(z);
                sumN += n; sumNZ += n * z; sumNA += n * aw; sumNAZ += n * aw * z;
            }
        numericBoxMeanZnumber.Value = sumN > 0 ? sumNZ / sumN : 0;        // 平均原子番号 (個数平均)
        numericBoxMeanZmass.Value = sumNA > 0 ? sumNAZ / sumNA : 0;       // 平均原子番号 (質量平均)
        numericBoxMeanAtomicWeight.Value = sumN > 0 ? sumNA / sumN : 0;   // 平均原子量 (原子あたり, g/mol。Molar Mass=式単位あたり とは別物)
        numericBoxAtomicNumberDensity.Value = Crystal.Volume > 0 ? sumN / (Crystal.Volume * volumeScale) : 0;   // 260729Cl 変更: atoms/Å³ または atoms/nm³
        numericBoxElectronDensity.Value = Crystal.Volume > 0 ? sumNZ / (Crystal.Volume * volumeScale) : 0;     // 260729Cl 変更: electrons/Å³ または electrons/nm³
        //numericBoxAtomicNumberDensity.Value = Crystal.Volume > 0 ? sumN / Crystal.Volume : 0;   // atoms/nm³
        //numericBoxElectronDensity.Value = Crystal.Volume > 0 ? sumNZ / Crystal.Volume : 0;      // electrons/nm³

        SymmetrySeriesNumber = Crystal.SymmetrySeriesNumber; // setter 内でコンボボックスをセットする (20170526)

        if (ChangeCellParameter)
        {
            symmetryControl.CellConstants = (Crystal.A, Crystal.B, Crystal.C, Crystal.Alpha, Crystal.Beta, Crystal.Gamma);
            symmetryControl.CellConstantsErr = (Crystal.A_err, Crystal.B_err, Crystal.C_err, Crystal.Alpha_err, Crystal.Beta_err, Crystal.Gamma_err);
        }

        atomControl.Crystal = crystal;
        bondControl.Crystal = crystal;
        boundControl.Crystal = crystal;
        latticePlaneControl.Crystal = crystal;
        eosControl.Crystal = crystal;

        // 261105Cl 追加 (監査 B1): 弾性定数の欄に結晶系を渡す (以前は渡しておらず、ずっと三斜晶扱いで拘束が働かなかった)。
        // 欄は値を変えずに入力の可否と注記だけを更新し、拘束は利用者が値を編集したときに掛ける (保存済みの値を黙って 0 にしない)
        elasticityControl1.SymmetrySeriesNumber = crystal.SymmetrySeriesNumber;
        //elasticityControl1.Stiffness = DenseMatrix.OfArray(crystal.ElasticStiffness); // 261106Cl 変更前: 毎回剛性の表示に戻していた
        // 261106Cl 変更 (作者の決定 C8-2・C8-3): 剛性/コンプライアンスの表示の選択を保つ (結晶を作り直すたび・結晶を選び直すたびに剛性の表示に戻っていた)。
        //  ・コンプライアンスの表示中: 結晶が持つコンプライアンス (入力した値) か、無ければ剛性の逆行列を出す。剛性が全部 0 なら 0 のコンプライアンス
        //  ・剛性の表示中でも、結晶が逆行列の無いコンプライアンスだけを持つ (剛性が全部 0) ときは、その値を見せるためコンプライアンスの表示にする
        //  ・剛性に逆行列が無く (0 でない) コンプライアンスで表せないときは、従来どおり剛性の表示にする
        var stiffness = DenseMatrix.OfArray(crystal.ElasticStiffness);
        var stiffnessInverse = stiffness.TryInverse();
        if (crystal.ElasticCompliance is { } storedCompliance && (elasticityControl1.Mode == Elasticity.Mode.Compliance || stiffnessInverse == null))
            elasticityControl1.Compliance = DenseMatrix.OfArray(storedCompliance);
        else if (elasticityControl1.Mode == Elasticity.Mode.Compliance && (stiffnessInverse != null || stiffness.Enumerate().All(v => v == 0)))
            elasticityControl1.Compliance = stiffnessInverse ?? new DenseMatrix(6, 6);
        else
            elasticityControl1.Stiffness = stiffness;

        numericUpDownAngleResolution.Value = Math.Min((decimal)crystal.AngleResolution, numericUpDownAngleResolution.Maximum);
        numericUpDownAngleSubDivision.Value = crystal.SubDivision;
        poleFigureControl.Crystal = crystal;

        // 261105Cl 追加 (監査 A19): 歪み・Hill 係数・結晶子サイズの欄も結晶の値にする。以前は更新せず、GenerateFromInterface (Replace・編集のたび) が
        // 前の結晶の値 (粉末シミュレータ) や欄の既定値 (主画面では欄が隠れていて、Hill = 1・歪み 0・サイズ 100 nm) を結晶に書き込んでいた
        var strain = crystal.Strain ?? new Matrix3D(0, 0, 0, 0, 0, 0, 0, 0, 0);
        (numericBoxStrain11.Value, numericBoxStrain12.Value, numericBoxStrain13.Value) = (strain.E11, strain.E12, strain.E13);
        (numericBoxStrain22.Value, numericBoxStrain23.Value, numericBoxStrain33.Value) = (strain.E22, strain.E23, strain.E33);
        numericBoxHill.Value = crystal.HillCoefficient;
        numericUpDownCrystallineSize.Value = Math.Clamp((decimal)crystal.GrainSize, numericUpDownCrystallineSize.Minimum, numericUpDownCrystallineSize.Maximum);
        // 261105Cl 追加 (監査 A19): 応力の欄も結晶の値にする (GenerateFromInterface が欄を読むので、これが無いと欄の値で結晶の応力を上書きする)
        var stress = crystal.Stress ?? new Matrix3D(0, 0, 0, 0, 0, 0, 0, 0, 0);
        (numericBoxStress11.Value, numericBoxStress12.Value, numericBoxStress13.Value) = (stress.E11, stress.E12, stress.E13);
        (numericBoxStress22.Value, numericBoxStress23.Value, numericBoxStress33.Value) = (stress.E22, stress.E23, stress.E33);

        SkipEvent = false;
        ResumeLayout();
    }

    /// <summary>260731Cl 追加 (GitHub issue #65): 色ボックスの変更をその場で Crystal.Argb に書き戻し、CrystalChanged で各ビューへ通知する。
    /// 従来は GenerateFromInterface() 経由でしか読まれず、結晶を選び直すと SetToInterface() が色ボックスを初期色で上書きして変更が失われていた。</summary>
    private void colorControl_ColorChanged(object sender, EventArgs e)
    {
        if (SkipEvent || crystal == null) return;
        crystal.Argb = colorControl.Color.ToArgb();
        CrystalChanged?.Invoke(this, EventArgs.Empty);
    }
    #endregion

    #region 長さ単位 (Å / nm) の切り替え  260729Cl 追加
    // 旧実装では Å³ 用 (numericBoxVolumeAng) と nm³ 用 (numericBoxCellVolumeNm) の 2 つを並べていたが、
    // symmetryControl の Å/nm ラジオに追従する 1 つ (numericBoxVolume) に統合した。原子数密度・電子数密度も同様。

    /// <summary>1 nm³ = 1000 Å³。Crystal.Volume の単位は nm³ なので、体積表示・数密度・単位胞質量の換算はすべてこれ 1 つで決まる</summary>
    private const double Ang3PerNm3 = 1000.0;

    /// <summary>格子定数の欄の Å/nm ラジオ (symmetryControl.LengthUnit) の get/set。261105Cl 追加:
    /// 配置先のアプリが長さの表示単位をこの切り替えに揃えるため (ReciPro の回折シミュレータのスポットのラベルの d など)。
    /// Designer には書き出さない (symmetryControl 側が既定値 Å を持つ)</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // 261105Cl 追加
    public LengthUnitEnum LengthUnit { get => symmetryControl.LengthUnit; set => symmetryControl.LengthUnit = value; } // 261105Cl 追加

    /// <summary>格子定数の欄の Å/nm が切り替わったときに発生する。261105Cl 追加 (配置先のアプリが単位付きの表示を描き直すため)</summary>
    public event EventHandler LengthUnitChanged; // 261105Cl 追加

    /// <summary>Crystal.Volume (nm³) を現在の表示単位へ換算する係数 (symmetryControl の Å/nm ラジオに従う)</summary>
    private double volumeScale => symmetryControl.LengthUnit == LengthUnitEnum.Angstrom ? Ang3PerNm3 : 1.0;
    //private bool angstromUnit => symmetryControl.LengthUnit == LengthUnitEnum.Angstrom;         // 260730Cl 削除: 参照 2 箇所のみだったので volumeScale へ畳んだ
    //private string volumeToolTipAng;                                                            // 260730Cl 削除: 表示中チップからの双方向トークン置換に変えてキャッシュ不要に

    /// <summary>symmetryControl の Å/nm ラジオに追従して、体積・数密度の表示単位を切り替える。
    /// 物理量は不変なので、表示値は Ang3PerNm3 倍 / その逆数倍に読み替えるだけでよい。
    /// コンストラクタからも一度呼び、Designer/resx の Å 前提と実行時の表示を必ず一致させる。
    /// 260730Cl: FooterText から「切替前の表示単位」を導出して冪等化 (同じ単位で二度呼ばれても値が千倍に壊れない)。
    /// ツールチップは表示中の文字列の単位トークンを双方向置換する (全 11 言語とも訳文が "Å³" を 1 回だけ含み "nm³" を含まないことを確認済。訳を変えるときは注意)。</summary>
    private void symmetryControl_LengthUnitChanged(object sender, EventArgs e)
    {
        var ang = symmetryControl.LengthUnit == LengthUnitEnum.Angstrom;
        var wasAng = numericBoxVolume.FooterText == "Å³"; // resx 既定も下の設定値も "Å³"/"nm³" の二択なので、これが切替前の単位を表す

        flowLayoutPanelCellProperties.SuspendLayout(); // 幅・フッタ・書式を続けて変えるので、フローの再配置は最後に 1 回だけ
        try
        {
            numericBoxVolume.FooterText = ang ? "Å³" : "nm³";
            numericBoxAtomicNumberDensity.FooterText = numericBoxElectronDensity.FooterText = ang ? "Å⁻³" : "nm⁻³";

            // Å 側は nm 側より 3 桁細かくして有効数字を保つ。体積の nm³ だけは旧 numericBoxCellVolumeNm の有効数字 9 桁を踏襲。
            numericBoxVolume.FormatSpecifier = ang ? "f4" : "g9";
            numericBoxAtomicNumberDensity.FormatSpecifier = ang ? "f7" : "f4";
            numericBoxElectronDensity.FormatSpecifier = ang ? "f6" : "f3";
            // 数値欄幅も単位ごとに変える。体積は旧 2 ボックスの幅をそれぞれ踏襲。数密度の Å⁻³ は
            // "0.0998860" が幅 60 (実測 58px > 内寸 56px) でクリップしたので Å 側だけ広げる。
            numericBoxVolume.ValueBoxWidth = ang ? 64 : 86;
            numericBoxAtomicNumberDensity.ValueBoxWidth = numericBoxElectronDensity.ValueBoxWidth = ang ? 68 : 60;

            var tip = toolTip.GetToolTip(numericBoxVolume); // 未設定時は "" が返るので null ガード不要
            numericBoxVolume.SetHostToolTip(toolTip, ang ? tip.Replace("nm³", "Å³") : tip.Replace("Å³", "nm³"));
            //numericBoxVolume.SetHostToolTip(toolTip, ang ? volumeToolTipAng : volumeToolTipAng?.Replace("Å³", "nm³"));                              // 260730Cl 変更前

            if (wasAng != ang) // 260730Cl 追加: 実際に単位が切り替わったときだけ表示値をリスケール
            {
                // 体積は Å 表示で ×Ang3PerNm3、数密度は体積の逆数なので割る
                var factor = ang ? Ang3PerNm3 : 1 / Ang3PerNm3;
                numericBoxVolume.Value *= factor;
                numericBoxAtomicNumberDensity.Value /= factor;
                numericBoxElectronDensity.Value /= factor;
            }
        }
        finally { flowLayoutPanelCellProperties.ResumeLayout(true); }
        LengthUnitChanged?.Invoke(this, EventArgs.Empty); // 261105Cl 追加: 配置先へ知らせる (コンストラクタからの初期化の呼び出しでは購読者がまだいない)
    }
    #endregion

    //public void ReadCrystal(string filename) // 261105Cl 変更前
    //{
    //    try { Crystal = ConvertCrystalData.ConvertToCrystal(filename); }
    //    catch (Exception ex) { if (AssemblyState.IsDebug) MessageBox.Show(ex.ToString()); }
    //}
    /// <summary>CIF・AMC のファイルから結晶を読む。261105Cl 変更 (監査 #28): 読めなければ今の結晶はそのまま (以前は黙って null を入れ、
    /// その後の回転などで例外になっていた)。showError なら、読めなかったことを画面に出す (マクロからの呼び出しでは出さない)</summary>
    /// <returns>261105Cl 追加: 読めたら true</returns>
    public bool ReadCrystal(string filename, bool showError = false)
    {
        Crystal c = null;
        try { c = ConvertCrystalData.ConvertToCrystal(filename); }
        catch (Exception ex) { if (AssemblyState.IsDebug) MessageBox.Show(ex.ToString()); }
        if (c == null)
        {
            if (showError)
                MessageBox.Show(System.Globalization.CultureInfo.CurrentUICulture.Name == "ja"
                    ? $"{System.IO.Path.GetFileName(filename)} から結晶を読み込めませんでした。今の結晶はそのままです。"
                    : $"Could not read a crystal from {System.IO.Path.GetFileName(filename)}. The current crystal is unchanged.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning); // 共有の部品なので題はアプリ名
            return false;
        }
        Crystal = c;
        return true;
    }

    #region ドラッグドロップ
    public void FormCrystal_DragDrop(object sender, DragEventArgs e)
    {
        var fileName = (string[])e.Data.GetData(DataFormats.FileDrop, false);
        if (fileName.Length == 1)
            //ReadCrystal(fileName[0]); // 261105Cl 変更前
            ReadCrystal(fileName[0], showError: true); // 261105Cl 変更
    }

    private void FormCrystal_DragEnter(object sender, DragEventArgs e) =>
        e.Effect = e.Data.GetData(DataFormats.FileDrop) != null ? DragDropEffects.Copy : DragDropEffects.None;
    #endregion

    //260801Cl 削除: Ctrl+Shift+R (crystal.Reserved のトグル) を廃止 (作者判断)。
    //KeyPreview で KeyDown を上げるのは Form だけなので、UserControl 自身の KeyDown に配線されたこのハンドラは
    //構造的に一度も発火していなかった (CrystalControl は子コントロールを多数持つコンテナで、フォーカスは常に子側にある)。
    //ReciPro のマニュアル (0-main-window / 21-shortcuts) には全 11 言語で「アプリ全体で有効」と記載されていたので、文書側も要修正。
    //旧:
    //private void CrystalControl_KeyDown(object sender, KeyEventArgs e)
    //{
    //    if (e.Control && e.Shift && e.KeyCode == Keys.R)
    //        crystal.Reserved = !crystal.Reserved;
    //}

    /// <summary>外部から呼び出される EOS 計算</summary>
    public void CalculateEOS() => eosControl.CalculatePressure();

    #region Polycrystalline 関連

    private void buttonGenerateRandomOrientations_Click(object sender, EventArgs e)
    {
        if (Crystal.Crystallites == null)
            Crystal.SetCrystallites();
        poleFigureControl.Crystal = Crystal;
    }

    public void DrawPoleFigure() => poleFigureControl.Draw(true);

    private void numericUpDownAngleResolution_ValueChanged(object sender, EventArgs e)
    {
        if (SkipEvent) return;
        GenerateFromInterface();
    }
    #endregion

    #region poleFigure の右クリックメニュー

    // BinaryFormatter が使えなくなったので read/save は無効化 (2022/11/10)
    //260801Cl: 中身は 2022/11/10 の BinaryFormatter 廃止で空になったまま。メニューは表示・クリックできるのに無反応だったので、
    //Designer 側で Visible = false にして眠らせた (作者判断)。別形式で実装し直すときに Visible を戻すこと。
    //(261026Cl 追記: VS の再シリアライズで、この Visible = false は CrystalControl.resx の loadToolStripMenuItem.Visible / saveToolStripMenuItem.Visible へ移った)
    private void loadToolStripMenuItem_Click(object sender, EventArgs e) { }
    private void saveToolStripMenuItem_Click(object sender, EventArgs e) { }

    /// <summary>ctf ファイルで出力 (Channel5 互換)</summary>
    private void asCTFFilecomatibleToCHANNEL5FileToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (crystal.Crystallites == null) return;
        const int maxCrystallites = 499900;

        var dlg = new SaveFileDialog { Filter = "*.ctf|*.ctf" };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        using var sw = new StreamWriter(dlg.FileName); // 260717Cl: 例外時にライタがリークする旧様式を using var へ (隣の txt 出力と同スタイル)
        sw.WriteLine("Channel Text File");
        sw.WriteLine("Prj\t OutPut from Recipro");
        sw.WriteLine("Author\t[Unknown]");
        sw.WriteLine("JobMode\tInteractive");
        sw.WriteLine($"NoMeas\t{maxCrystallites}");
        sw.WriteLine("AcqE1\t0");
        sw.WriteLine("AcqE2\t0");
        sw.WriteLine("AcqE3\t0");
        sw.WriteLine("Euler angles refer to Sample Coordinate system (CS0)!\tMag\t0\tCoverage\t0\tDevice\t0\tKV\t0\tTiltAngle\t0\tTiltAxis\t0");
        sw.WriteLine("Phases\t1");
        sw.WriteLine($"0.000;0.000;0.000\t90;90;90\t{Crystal.Name}\t3\t0\t3803863129_5.0.6.3\t1060505527\t[{crystal.Name}]");
        sw.WriteLine("Phase\tX\tY\tBands\tError\tEuler1\tEuler2\tEuler3\tMAD\tBC\tBS");

        double sum = 0;
        for (int i = 0; i < Crystal.Crystallites.TotalCrystalline; i++)
            sum += crystal.Crystallites.Density[i] * crystal.Crystallites.SolidAngle[i];

        double tempSum = 0;
        int seed = 0;
        for (int i = 0; i < maxCrystallites; i++)
        {
            double partialSum = (i + 0.5) / maxCrystallites * sum;
            while (tempSum + Crystal.Crystallites.Density[seed] * Crystal.Crystallites.SolidAngle[seed] < partialSum && seed < Crystal.Crystallites.Density.Length)
            {
                tempSum += Crystal.Crystallites.Density[seed] * Crystal.Crystallites.SolidAngle[seed];
                seed++;
            }

            var (Phi, Theta, Psi) = Euler.FromMatrix(Crystal.Crystallites.Rotations[seed]);
            string str = "";
            foreach (double angle in (double[])[Phi, Theta, Psi])
            {
                double d = (angle > 0 ? angle : angle + 2 * Math.PI) / Math.PI * 180;
                str += d switch
                {
                    >= 100 => $"{d:000.00}\t",
                    >= 10 => $"{d:00.000}\t",
                    _ => $"{d:0.0000}\t"
                };
            }
            sw.WriteLine($"1\t0\t0\t0\t0\t{str}0\t0\t0");
        }
        //sw.Close(); // 260717Cl: using var 化に伴い不要
    }

    /// <summary>txt ファイルで出力 (オイラー角と密度)</summary>
    private void asTXTFileAllEulerAngleAndDensityToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (crystal.Crystallites == null) return;

        var dlg = new SaveFileDialog { Filter = "*.txt|*.txt" };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        using var sw = new StreamWriter(dlg.FileName);
        sw.WriteLine($"Sample Name:\t{Crystal.Name}");
        sw.WriteLine($"Cell constants:\t{Crystal.A}\t{Crystal.B}\t{Crystal.C}\t{crystal.Alpha / Math.PI * 180}\t{crystal.Beta / Math.PI * 180}\t{crystal.Gamma / Math.PI * 180}");
        sw.WriteLine($"Space group:\t{Crystal.Symmetry.SpaceGroupHMfullStr}");
        sw.WriteLine();
        sw.WriteLine("Euler angles refer to Sample Coordinate system");
        sw.WriteLine("No.\tEuler1\tEuler2\tEuler3\tDensity");

        double sum = 0;
        var density = new double[Crystal.Crystallites.TotalCrystalline];
        var index = new int[Crystal.Crystallites.TotalCrystalline];
        for (int i = 0; i < Crystal.Crystallites.TotalCrystalline; i++)
        {
            density[i] = crystal.Crystallites.Density[i] * crystal.Crystallites.SolidAngle[i];
            index[i] = i;
            sum += density[i];
        }
        if (sender == asTXTFileallEulerAngleAndDensitySortedToolStripMenuItem)
        {
            Array.Sort(density, index);
            density = [.. density.Reverse()];
            index = [.. index.Reverse()];
        }

        for (int i = 0; i < Crystal.Crystallites.TotalCrystalline; i++)
        {
            var (Phi, Theta, Psi) = Euler.FromMatrix(Crystal.Crystallites.Rotations[index[i]]);
            string str = $"{i}\t";
            foreach (double angle in (double[])[Phi, Theta, Psi])
            {
                double d = (angle > 0 ? angle : angle + 2 * Math.PI) / Math.PI * 180;
                str += d.ToString("000.0000") + "\t";
            }
            str += (density[i] / sum * crystal.Crystallites.TotalCrystalline).ToString();
            sw.WriteLine(str);
        }
    }
    #endregion

    #region 子コントロールの変化イベント
    private void atomControl_AtomsChanged(object sender, EventArgs e) => GenerateFromInterface();

    private void symmetryControl_ItemChanged(object sender, EventArgs e)
    {
        GenerateFromInterface();

        // 同じ空間群番号の候補が複数あって P1 / P-1 でない時のみボタン有効化
        var array = SymmetryStatic.NumArray;
        var sn = symmetryControl.SymmetrySeriesNumber;
        buttonChangeAxesOriginSetting.Enabled = convertToAnotherSpacegroupToolStripMenuItem.Enabled = array[sn][1] > 2 && array.Count(e => e[1] == array[sn][1]) > 1;
    }

    private void bondControl_ItemsChanged(object sender, EventArgs e)
    {
        // bondControl の ItemChanged は StructureViewer などに直接登録されているのでここでは何もしない
    }

    private void elasticityControl1_ValueChanged(object sender, EventArgs e)
    {
        if (SkipEvent) return;
        if (elasticityControl1.Mode == Elasticity.Mode.Compliance)
            formStrain.Compliance = elasticityControl1.Compliance;
        else
            formStrain.Stiffness = elasticityControl1.Stiffness;
        formStrain.ElasticityMode = elasticityControl1.Mode;
    }
    #endregion

    #region SymmetryInformation / BeamInteraction の表示
    private void buttonSymmetryInfo_Click(object sender, EventArgs e) => FormSymmetryInformation.Visible = !FormSymmetryInformation.Visible;
    private void formSymmetryInformation_VisibleChanged(object sender, EventArgs e) => SymmetryInformation_VisibleChanged?.Invoke(sender, e);

    private void buttonBeamInteraction_Click(object sender, EventArgs e) => FormBeamInteraction.Visible = !FormBeamInteraction.Visible;
    private void formBeamInteraction_VisibleChanged(object sender, EventArgs e) => BeamInteraction_VisibleChanged?.Invoke(sender, e);
    #endregion

    #region リサイズ中は描画停止
    private bool registResizeEvent = false;
    private void CrystalControl_Resize_1(object sender, EventArgs e)
    {
        if (DesignMode || registResizeEvent) return;
        var parent = Parent;
        while (parent is not Form && parent != null)
            parent = parent.Parent;
        if (parent is not Form form) return;
        form.ResizeBegin += (s, ea) => SuspendLayout();
        form.ResizeEnd += (s, ea) => ResumeLayout();
        registResizeEvent = true;
    }
    #endregion

    #region 右クリックメニュー
    private void importCrystalFromCIFAMCToolStripMenuItem_Click(object sender, EventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = " *.cif; *.amc | *.cif;*.amc" };
        if (dlg.ShowDialog() == DialogResult.OK)
            //ReadCrystal(dlg.FileName); // 261105Cl 変更前
            ReadCrystal(dlg.FileName, showError: true); // 261105Cl 変更
    }

    public void exportThisCrystalAsCIFToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (Crystal == null) return;
        var dlg = new SaveFileDialog { Filter = " *.cif| *.cif", FileName = $"{Crystal.Name}.cif" };
        if (dlg.ShowDialog() == DialogResult.OK)
            Crystal.ExportCIF(dlg.FileName);
    }

    // 260717Cl: ボタン側 (buttonBeamInteraction_Click / buttonSymmetryInfo_Click) と同一トグルの二重定義だったため削除し、
    // Designer の配線をボタン側ハンドラへ付け替え。
    //private void beamInteractionToolStripMenuItem_Click(object sender, EventArgs e) => FormBeamInteraction.Visible = !FormBeamInteraction.Visible;
    //private void symmetryInformationToolStripMenuItem_Click(object sender, EventArgs e) => FormSymmetryInformation.Visible = !FormSymmetryInformation.Visible;

    private void sendThisCrystalToOtherSoftwareToolStripMenuItem_Click(object sender, EventArgs e)
    {
        GenerateFromInterface();
        if (crystal != null)
            // 260723Cl 変更: 生の Crystal2 オブジェクトを SetDataObject すると .NET 9+ の BinaryFormatter 廃止で
            // クロスプロセス転送がサイレント失敗する。受信側 (ReciPro/PDIndexer の WndProc) は
            // 「先頭1バイト=Crystal2.ID + Brotli圧縮MemoryPack」の byte[] を期待しているため、
            // CSManager の送信側 (FormMain.cs) と同じ形式へ統一。
            //Clipboard.SetDataObject(Crystal2.FromCrystal(crystal), true, 3, 10);
        //Clipboard.SetDataObject(MemoryPackEx.Serialize(Crystal2.ID, Crystal2.FromCrystal(crystal)), true, 3, 10); // 261105Cl 変更前
        {
            // 261105Cl 変更 (監査 #23): 送った内容を覚えておき、受け側 (ReciPro の WndProc) が自分の送信を受け取って編集中の結晶を
            // 送信用の Crystal2 (表示・EOS などを持たない) から作り直したものに置き換えないようにする。クリップボードの形式は変えない
            var bytes = MemoryPackEx.Serialize(Crystal2.ID, Crystal2.FromCrystal(crystal));
            LastSentToClipboard = bytes;
            Clipboard.SetDataObject(bytes, true, 3, 10);
        }
    }

    /// <summary>261105Cl 追加 (監査 #23): このプロセスが「他のソフトへ送る」で最後にクリップボードに置いた内容</summary>
    public static byte[] LastSentToClipboard { get; private set; }

    private void resetToolStripMenuItem_Click(object sender, EventArgs e)
    {
        for (int i = 0; i < crystal.Atoms.Length; i++)
            crystal.Atoms[i].Dsf = new DiffuseScatteringFactor(DiffuseScatteringFactor.Type.B, true, 0, 0, null, null, Crystal.CellValue);
    }

    private void revertCellConstantsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        crystal.RevertInitialCellConstants();
        Crystal = crystal;
    }

    private void strainControlToolStripMenuItem_Click(object sender, EventArgs e)
    {
        formStrain.crystal ??= Crystal;
        formStrain.Visible = !formStrain.Visible;
    }

    /// <summary>化学組成の詳細フォームを開く。ShowDialog ではなく Visible=true で開き、閉じるとフォーム側で非表示に戻る。
    /// 既に開いている場合は前面に出し、最小化されていれば元に戻す。261024Cl 追加 (Designer が生成した空ハンドラを、この region へ移して実装)</summary>
    private void detailsOfTheChemicalCompositionToolStripMenuItem_Click(object sender, EventArgs e)
    {
        FormChemicalComposition.Visible = true;
        // 最小化中 (または最小化のまま閉じて隠れた後) は Activate だけでは戻らない。表示した「後」に Normal へ戻すこと
        if (FormChemicalComposition.WindowState == FormWindowState.Minimized)
            FormChemicalComposition.WindowState = FormWindowState.Normal;
        FormChemicalComposition.Activate();
    }

    /// <summary>空間群 P1 に変換</summary>
    private void convertToP1ToolStripMenuItem_Click(object sender, EventArgs e) => toSuperStructure(1, 1, 1);

    /// <summary>超構造に変換</summary>
    private void convertToSuperstructureToolStripMenuItem_Click(object sender, EventArgs e)
    {
        var dlg = new FormSuperStructure();
        if (dlg.ShowDialog() == DialogResult.OK)
            toSuperStructure(dlg.A, dlg.B, dlg.C);
    }

    private void convertToAnotherSpacegroupToolStripMenuItem_Click(object sender, EventArgs e) => ChangeAxesOriginSetting();
    private void buttonChangeAxesOriginSetting_Click(object sender, EventArgs e) => ChangeAxesOriginSetting();
    #endregion

    #region 空間群を変換する関数群

    /// <summary>超構造に変換</summary>
    private void toSuperStructure(int _u, int _v, int _w)
    {
        GenerateFromInterface();
        crystal.SymmetrySeriesNumber = 1;

        // 261105Cl 追加: 温度因子の異方性テンソルを、各等価位置を作った対称操作と超構造の倍率で変換する (β' = M β Mᵀ、M = diag(1/u, 1/v, 1/w)·R)。
        // 以前は代表原子の Dsf をそのまま (同じオブジェクトを共有して) 全等価位置に配っていたため、P1 展開・超構造で異方性の DW が誤っていた
        var oldCell = crystal.CellValue;
        var newCell = (crystal.A * _u, crystal.B * _v, crystal.C * _w, crystal.Alpha, crystal.Beta, crystal.Gamma);
        var temp_atoms = new List<Atoms>();
        foreach (var atoms in Crystal.Atoms)
        {
            int n = 0;
            foreach (var atom in atoms.Atom)
            {
                // 対称操作の線形部 R (列 = R e_i)。位置は R x + t で作られている (構造因子の計算と同じ規約。tools/AdpCheck transform で確認)
                var m = new double[3, 3];
                (double X, double Y, double Z)[] cols = [atom.Operation.ApplyMatrix(1, 0, 0), atom.Operation.ApplyMatrix(0, 1, 0), atom.Operation.ApplyMatrix(0, 0, 1)];
                for (int j = 0; j < 3; j++)
                    (m[0, j], m[1, j], m[2, j]) = (cols[j].X / _u, cols[j].Y / _v, cols[j].Z / _w);

                for (double u = 0; u < _u; u++)
                    for (double v = 0; v < _v; v++)
                        for (double w = 0; w < _w; w++)
                        {
                            temp_atoms.Add(new Atoms(
                                $"{atoms.Label.TrimEnd()}_{n}",
                                atoms.AtomicNumber, atoms.SubNumberXray, atoms.SubNumberElectron, atoms.Isotope,
                                1,
                                new Vector3DBase((atom.X + u) / _u, (atom.Y + v) / _v, (atom.Z + w) / _w),
                                new Vector3DBase(atoms.X_err / _u, atoms.Y_err / _v, atoms.Z_err / _w),
                                //atoms.Occ, atoms.Occ_err, atoms.Dsf, atoms.Material, // 261105Cl 変更前
                                atoms.Occ, atoms.Occ_err, atoms.Dsf?.Transform(m, oldCell, newCell), atoms.Material, // 261105Cl 変更: 変換した温度因子 (原子ごとに別のオブジェクト)
                                atoms.Radius, atoms.GLEnabled, atoms.ShowLabel));
                            n++;
                        }
            }
        }
        crystal.A *= _u;
        crystal.B *= _v;
        crystal.C *= _w;
        crystal.Atoms = [.. temp_atoms];

        SetToInterface(true);
        GenerateFromInterface();
    }

    public void ChangeAxesOriginSetting()
    {
        var seriesNum = symmetryControl.SymmetrySeriesNumber;
        var spNum = SymmetryStatic.NumArray[seriesNum][1];

        // 自分自身を除く、同じ空間群番号のものを候補に挙げる
        var list = new List<(int SeriesNum, string Notation)>();
        foreach (var n in SymmetryStatic.NumArray)
            if (spNum == n[1] && seriesNum != n[0])
                list.Add((n[0], SymmetryStatic.StrArray[n[0]][3]));

        var dlg = new FormAnotherSpaceGroup { Candidates = [.. list] };
        if (dlg.ShowDialog() == DialogResult.OK)
            toAnotherSpaceGroup(dlg.SeriesNum);
    }

    /// <summary>別の空間群に変換 (原子位置/格子定数のエラーや熱散漫散乱因子の変換は未対応)</summary>
    public void toAnotherSpaceGroup(int destNum)
    {
        var srcNum = symmetryControl.SymmetrySeriesNumber;
        var crystalSystem = SymmetryStatic.NumArray[srcNum][5];
        var sgNum = SymmetryStatic.NumArray[srcNum][1];
        var srcExtra = SymmetryStatic.StrArray[srcNum][0];
        var dstExtra = SymmetryStatic.StrArray[destNum][0];

        if (crystalSystem == 2) // monoclinic: 軸変換のみ
        {
            if (srcExtra.Length == 1) srcExtra += "1";
            if (dstExtra.Length == 1) dstExtra += "1";

            var cry = Deep.Copy(Crystal);
            cry.SymmetrySeriesNumber = destNum;
            cry.SetAxis();
            foreach (var a in cry.Atoms)
            {
                (a.X, a.Y, a.Z) = exchangeAtomPositionMonoclinic(a.X, a.Y, a.Z, srcExtra, false);
                (a.X, a.Y, a.Z) = exchangeAtomPositionMonoclinic(a.X, a.Y, a.Z, dstExtra, true);
            }
            var (A, B, C) = convertAxisMonoclinic(cry.A_Axis, cry.B_Axis, cry.C_Axis, srcExtra, false);
            (A, B, C) = convertAxisMonoclinic(A, B, C, dstExtra, true);
            cry.A = A.Length; cry.B = B.Length; cry.C = C.Length;
            cry.Alpha = Vector3D.AngleBetVectors(B, C);
            cry.Beta = Vector3D.AngleBetVectors(C, A);
            cry.Gamma = Vector3D.AngleBetVectors(A, B);
            // 261105Cl 追加: 温度因子の異方性テンソルも、原子座標と同じ軸の取り替えで変換する
            transformDsf(cry, linearPartOf(p =>
            {
                var q = exchangeAtomPositionMonoclinic(p.X, p.Y, p.Z, srcExtra, false);
                return exchangeAtomPositionMonoclinic(q.X, q.Y, q.Z, dstExtra, true);
            }), Crystal.CellValue);
            crystal = cry;
        }

        if (crystalSystem == 3) // Orthorhombic: 軸変換 + Origin Choice 変換
        {
            var src = convOrtho(srcExtra);
            var dst = convOrtho(dstExtra);

            var cry = Deep.Copy(Crystal);
            cry.SymmetrySeriesNumber = destNum;
            foreach (var a in cry.Atoms)
            {
                (a.X, a.Y, a.Z) = exchangeOrtho(a.X, a.Y, a.Z, src.Setting);
                (a.X_err, a.Y_err, a.Z_err) = exchangeOrtho(a.X_err, a.Y_err, a.Z_err, src.Setting);
                if (src.Origin != dst.Origin)
                    (a.X, a.Y, a.Z) = shift(a.X, a.Y, a.Z, sgNum, src.Origin == 1);
                (a.X, a.Y, a.Z) = exchangeOrtho(a.X, a.Y, a.Z, dst.Setting, true);
                (a.X_err, a.Y_err, a.Z_err) = exchangeOrtho(a.X_err, a.Y_err, a.Z_err, dst.Setting, true);
            }
            (cry.A, cry.B, cry.C) = exchangeOrtho(cry.A, cry.B, cry.C, src.Setting, false, true);
            (cry.A, cry.B, cry.C) = exchangeOrtho(cry.A, cry.B, cry.C, dst.Setting, true, true);
            // 261105Cl 追加: 温度因子の異方性テンソルも、原子座標と同じ軸の入れ替えで変換する (原点の移動は並進なので影響しない)
            transformDsf(cry, linearPartOf(p =>
            {
                var q = exchangeOrtho(p.X, p.Y, p.Z, src.Setting);
                return exchangeOrtho(q.X, q.Y, q.Z, dst.Setting, true);
            }), Crystal.CellValue);
            crystal = cry;
        }

        if (crystalSystem is 4 or 7) // Tetragonal / Cubic: Origin Choice 変換のみ
        {
            var src = convOrtho(srcExtra);
            var cry = Deep.Copy(Crystal);
            cry.SymmetrySeriesNumber = destNum;
            foreach (var a in cry.Atoms)
                (a.X, a.Y, a.Z) = shift(a.X, a.Y, a.Z, sgNum, src.Origin == 1);
            crystal = cry;
        }

        if (crystalSystem == 5) // trigonal: Rhombo / Hexa の変換
        {
            var cry = Deep.Copy(Crystal);
            cry.SymmetrySeriesNumber = destNum;
            cry.SetAxis();

            Vector3DBase srcA = cry.A_Axis, srcB = cry.B_Axis, srcC = cry.C_Axis, dstA, dstB, dstC;
            // 261105Cl 変更: 座標の変換を関数にして、温度因子のテンソルの変換 (線形部) にも使う
            Func<(double X, double Y, double Z), (double X, double Y, double Z)> f;
            if (srcExtra == "H")
            {
                //foreach (var a in cry.Atoms) // 261105Cl 変更前 (3 行)
                //    (a.X, a.Y, a.Z) = (a.X + a.Z, -a.X + a.Y + a.Z, a.Y + a.Z);
                //(dstA, dstB, dstC) = ((2 * srcA + srcB + srcC) / 3, (-srcA + srcB + srcC) / 3, (-srcA - srcB + srcC) / 3);
                // 261105Cl 変更: 六方軸 → 菱面体軸 (obverse) の式の誤りを直す。逆変換 a_H = a_R − b_R, b_H = b_R − c_R, c_H = a_R + b_R + c_R から
                // z_R = −y_H + z_H、c_R = (−a_H − 2b_H + c_H)/3 (以前は z_R = y_H + z_H、c_R = (−a_H − b_H + c_H)/3 で、y≠0 の原子と c・α・β が誤っていた)。
                // 下の R → H の式 (逆向き) とこれで互いに逆になる
                f = p => (p.X + p.Z, -p.X + p.Y + p.Z, -p.Y + p.Z);
                (dstA, dstB, dstC) = ((2 * srcA + srcB + srcC) / 3, (-srcA + srcB + srcC) / 3, (-srcA - 2 * srcB + srcC) / 3);
            }
            else
            {
                //foreach (var a in cry.Atoms) // 261105Cl 変更前 (2 行)
                //    (a.X, a.Y, a.Z) = ((2 * a.X - a.Y - a.Z) / 3, (a.X + a.Y - 2 * a.Z) / 3, (a.X + a.Y + a.Z) / 3);
                f = p => ((2 * p.X - p.Y - p.Z) / 3, (p.X + p.Y - 2 * p.Z) / 3, (p.X + p.Y + p.Z) / 3);
                (dstA, dstB, dstC) = (srcA - srcB, srcB - srcC, srcA + srcB + srcC);
            }
            foreach (var a in cry.Atoms) // 261105Cl 追加
                (a.X, a.Y, a.Z) = f((a.X, a.Y, a.Z));
            cry.A = dstA.Length; cry.B = dstB.Length; cry.C = dstC.Length;
            cry.Alpha = Vector3DBase.AngleBetVectors(dstB, dstC);
            cry.Beta = Vector3DBase.AngleBetVectors(dstC, dstA);
            cry.Gamma = Vector3DBase.AngleBetVectors(dstA, dstB);
            transformDsf(cry, linearPartOf(f), Crystal.CellValue); // 261105Cl 追加: 温度因子の異方性テンソルも同じ軸の取り替えで変換する
            crystal = cry;
        }

        SetToInterface(true);
        GenerateFromInterface();
    }

    /// <summary>261105Cl 追加: 分率座標の変換 f (アフィン) の線形部 M を返す (列 j = f(e_j) − f(0))。原点の移動 (並進) は打ち消される</summary>
    private static double[,] linearPartOf(Func<(double X, double Y, double Z), (double X, double Y, double Z)> f)
    {
        var o = f((0, 0, 0));
        (double X, double Y, double Z)[] cols = [f((1, 0, 0)), f((0, 1, 0)), f((0, 0, 1))];
        var m = new double[3, 3];
        for (int j = 0; j < 3; j++)
            (m[0, j], m[1, j], m[2, j]) = (cols[j].X - o.X, cols[j].Y - o.Y, cols[j].Z - o.Z);
        return m;
    }

    /// <summary>261105Cl 追加: 軸の取り替えに合わせて、全原子の温度因子の異方性テンソルを変換する (β' = M β Mᵀ)。
    /// cry の格子は変換後の値に設定済みであること。oldCell は変換前の格子 (Deep.Copy 後の Dsf は格子を持たないので明示する)</summary>
    private static void transformDsf(Crystal cry, double[,] m, (double A, double B, double C, double Alpha, double Beta, double Gamma) oldCell)
    {
        foreach (var a in cry.Atoms)
            if (a.Dsf != null)
                a.Dsf = a.Dsf.Transform(m, oldCell, cry.CellValue);
    }

    private const double one4th = 1.0 / 4.0, one8th = 1.0 / 8.0, three8th = 3.0 / 8.0;

    private static (double X, double Y, double Z) shift(double x, double y, double z, int sgNum, bool to2nd)
        => sgNum switch
        {
            48 or 86 or 126 or 201 or 222 or 224 => to2nd ? (x + one4th, y + one4th, z + one4th) : (x - one4th, y - one4th, z - one4th),
            50 or 59 or 125 => to2nd ? (x + one4th, y + one4th, z) : (x - one4th, y - one4th, z),
            68 => to2nd ? (x, y + one4th, z + one4th) : (x, y - one4th, z - one4th),
            70 => to2nd ? (x + one8th, y + one8th, z + one8th) : (x - one8th, y - one8th, z - one8th),
            85 => to2nd ? (x + one4th, y - one4th, z) : (x - one4th, y + one4th, z),
            88 => to2nd ? (x, y + one4th, z + one8th) : (x, y - one4th, z - one8th),
            129 or 130 => to2nd ? (x - one4th, y + one4th, z) : (x + one4th, y - one4th, z),
            133 or 137 or 138 => to2nd ? (x - one4th, y + one4th, z - one4th) : (x + one4th, y - one4th, z + one4th),
            134 => to2nd ? (x + one4th, y - one4th, z + one4th) : (x - one4th, y + one4th, z - one4th),
            141 => to2nd ? (x, y - one4th, z + one8th) : (x, y + one4th, z - one8th),
            142 => to2nd ? (x, y + one4th, z + three8th) : (x, y - one4th, z - three8th),
            203 or 227 or 228 => to2nd ? (x + one8th, y + one8th, z + one8th) : (x - one8th, y - one8th, z - one8th),
            _ => (x, y, z)
        };

    /// <summary>原子位置を変換 (Monoclinic 用)</summary>
    private static (double X, double Y, double Z) exchangeAtomPositionMonoclinic(double x, double y, double z, string setting, bool forward = true)
        => setting switch
        {
            "b2" => forward ? (-z, y, x - z) : (z - x, y, -x),
            "b3" => forward ? (z - x, y, -x) : (-z, y, x - z),
            "-b1" => forward ? (-z, y, x) : (z, y, -x),
            "-b2" => forward ? (x + z, y, z) : (x - z, y, z),
            "-b3" => forward ? (-x, y, -z - x) : (-x, y, x - z),

            "c1" => forward ? (z, x, y) : (y, z, x),
            "c2" => forward ? (x - z, -z, y) : (x - y, z, -y),
            "c3" => forward ? (-x, z - x, y) : (-x, z, y - x),
            "-c1" => forward ? (x, -z, y) : (x, z, -y),
            "-c2" => forward ? (z, x + z, y) : (y - x, z, x),
            "-c3" => forward ? (-z - x, -x, y) : (-y, z, y - x),

            "a1" => forward ? (y, z, x) : (z, x, y),
            "a2" => forward ? (y, x - z, -z) : (y - z, x, -z),
            "a3" => forward ? (y, -x, z - x) : (-y, x, z - y),
            "-a1" => forward ? (y, x, -z) : (y, x, -z),
            "-a2" => forward ? (y, z, x + z) : (z - y, x, y),
            "-a3" => forward ? (y, -z - x, -x) : (-z, x, z - y),
            _ => (x, y, z),
        };

    private static (Vector3DBase A, Vector3DBase B, Vector3DBase C) convertAxisMonoclinic(Vector3DBase a, Vector3DBase b, Vector3DBase c, string setting, bool forward = true)
        => setting switch
        {
            "b2" => forward ? (-c - a, b, a) : (c, b, -c - a),
            "b3" => forward ? (c, b, -c - a) : (-c - a, b, a),
            "-b1" => forward ? (-c, b, a) : (c, b, -a),
            "-b2" => forward ? (a, b, c - a) : (a, b, c + a),
            "-b3" => forward ? (c - a, b, -c) : (-c - a, b, -c),

            "c1" => forward ? (c, a, b) : (b, c, a),
            "c2" => forward ? (a, -c - a, b) : (a, c, -a - b),
            "c3" => forward ? (-c - a, c, b) : (-a - b, c, b),
            "-c1" => forward ? (a, -c, b) : (a, c, -b),
            "-c2" => forward ? (c - a, a, b) : (b, c, a + b),
            "-c3" => forward ? (-c, c - a, b) : (-a - b, c, -a),

            "a1" => forward ? (b, c, a) : (c, a, b),
            "a2" => forward ? (b, a, -c - a) : (b, a, -b - c),
            "a3" => forward ? (b, -c - a, c) : (-b - c, a, c),
            "-a1" => forward ? (b, a, -c) : (b, a, -c),
            "-a2" => forward ? (b, c - a, a) : (c, a, b + c),
            "-a3" => forward ? (b, -c, c - a) : (-b - c, a, -b),
            _ => (a, b, c),
        };

    private static (double X, double Y, double Z) exchangeOrtho(double x, double y, double z, int[] setting, bool inverse = false, bool abs = false)
    {
        double[] src = [x, y, z], dst = new double[3];
        for (int i = 0; i < 3; i++)
        {
            if (!inverse)
                dst[Math.Abs(setting[i]) - 1] = setting[i] > 0 ? src[i] : -src[i];
            else
                dst[i] = setting[i] > 0 ? src[Math.Abs(setting[i]) - 1] : -src[Math.Abs(setting[i]) - 1];
        }
        return abs ? (Math.Abs(dst[0]), Math.Abs(dst[1]), Math.Abs(dst[2])) : (dst[0], dst[1], dst[2]);
    }

    /// <summary>extra 文字列を解析 (例: "2ba-c" → (2, {2, 1, -3}))</summary>
    private static (int Origin, int[] Setting) convOrtho(string s)
    {
        if (s.Length == 0) return (1, [1, 2, 3]);
        int origin = 1;
        if (s[0] is '1' or '2') // OriginChoice 変換があるのは 48, 50, 59, 68, 70 など
        {
            origin = s[0] == '1' ? 1 : 2;
            s = s[1..];
        }
        if (s.Length == 0) return (origin, [1, 2, 3]);

        var setting = new int[3];
        for (int i = 0; i < 3; i++)
        {
            setting[i] = s[0] != '-' ? 1 : -1;
            if (s[0] == '-') s = s[1..];
            setting[i] *= s[0] switch { 'a' => 1, 'b' => 2, _ => 3 };
            s = s[1..];
        }
        return (origin, setting);
    }
    #endregion

    //private void buttonStressSet_Click(object sender, EventArgs e) => GenerateFromInterface(); // 261105Cl 変更前
    /// <summary>261105Cl 変更: 歪み・応力・Hill 係数を結晶に入れたあと、応力が歪みに効かない場合 (弾性定数が未設定で剛性行列の逆行列が無い・Hill 係数が 0) に知らせる。
    /// 計算は止めない (弾性定数が無いとき応力からの歪みは 0。<see cref="Elasticity.GetStrainTriclinic"/>)。
    /// 261106Cl 変更 (作者の決定 C6: Hill 係数の既定を 0.5 に): 使われる歪みは h × (応力から計算した歪み) + (1 − h) × (入力した歪み)。
    /// 入力した応力・歪みの一方が使われない (h = 0 の応力、h = 1 の歪み) か、一方だけを入れて h 倍・(1 − h) 倍に縮む (0 &lt; h &lt; 1。既定の 0.5 で半分) ときにも知らせる。
    /// 両方を入れて 0 &lt; h &lt; 1 なら意図した Hill の混合なので知らせない</summary>
    private void buttonStressSet_Click(object sender, EventArgs e)
    {
        GenerateFromInterface();
        //if (crystal?.Stress == null || crystal.Stress.IsZero()) return; // 261106Cl 変更前
        //if (DenseMatrix.OfArray(crystal.ElasticStiffness).TryInverse() == null)
        // 261106Cl 変更: 歪みだけを入れた場合も見る。弾性定数はコンプライアンスで入れた値 (Crystal.ElasticCompliance) があれば設定済みとみなす
        if (crystal == null) return;
        bool hasStress = crystal.Stress != null && !crystal.Stress.IsZero(), hasStrain = crystal.Strain != null && !crystal.Strain.IsZero();
        double h = crystal.HillCoefficient;
        string hText = h.ToString("0.###"), restText = (1 - h).ToString("0.###"); // 261106Cl 追加: メッセージに出す h と 1 − h
        if (hasStress && h != 0 && crystal.ElasticCompliance == null && DenseMatrix.OfArray(crystal.ElasticStiffness).TryInverse() == null)
            MessageBox.Show(Crystallography.Localization.Loc(
                en: "The elastic constants are not set (the stiffness matrix cannot be inverted), so the stress is not converted to strain: the strain from the stress is 0 until they are entered.",
                ja: "弾性定数が未設定 (剛性行列の逆行列がない) のため、応力を歪みに換算できません。弾性定数を入れるまで、応力による歪みは 0 です。",
                de: "Die elastischen Konstanten sind nicht gesetzt (die Steifigkeitsmatrix ist nicht invertierbar), daher wird die Spannung nicht in Dehnung umgerechnet: Die Dehnung aus der Spannung ist 0, bis sie eingegeben werden.",
                fr: "Les constantes élastiques ne sont pas définies (la matrice de rigidité n'est pas inversible) ; la contrainte n'est donc pas convertie en déformation : la déformation due à la contrainte reste 0 jusqu'à leur saisie.",
                es: "Las constantes elásticas no están definidas (la matriz de rigidez no es invertible), por lo que la tensión no se convierte en deformación: la deformación debida a la tensión es 0 hasta que se introduzcan.",
                pt: "As constantes elásticas não estão definidas (a matriz de rigidez não é invertível), por isso a tensão não é convertida em deformação: a deformação devida à tensão é 0 até que sejam introduzidas.",
                it: "Le costanti elastiche non sono impostate (la matrice di rigidezza non è invertibile), quindi lo sforzo non viene convertito in deformazione: la deformazione dovuta allo sforzo è 0 finché non vengono inserite.",
                ru: "Упругие константы не заданы (матрица жёсткости необратима), поэтому напряжение не пересчитывается в деформацию: деформация от напряжения равна 0, пока они не введены.",
                zhHans: "弹性常数未设置 (刚度矩阵不可逆)，因此无法将应力换算为应变：在输入弹性常数之前，由应力产生的应变为 0。",
                zhHant: "彈性常數未設定 (勁度矩陣不可逆)，因此無法將應力換算為應變：在輸入彈性常數之前，由應力產生的應變為 0。",
                ko: "탄성 상수가 설정되지 않아 (강성 행렬의 역행렬이 없음) 응력을 변형으로 환산할 수 없습니다. 탄성 상수를 입력할 때까지 응력에 의한 변형은 0입니다."),
                Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //else if (crystal.HillCoefficient == 0) // 261106Cl 変更前
        else if (hasStress && h == 0) // 261106Cl 変更
            MessageBox.Show(Crystallography.Localization.Loc(
                en: "The Hill coefficient is 0, so the stress is not used (only the imposed strain is). Set it to 1 to use the strain from the stress.",
                ja: "Hill 係数が 0 のため、応力は使われません (入力した歪みだけが使われます)。応力からの歪みを使うには 1 にしてください。",
                de: "Der Hill-Koeffizient ist 0, daher wird die Spannung nicht verwendet (nur die vorgegebene Dehnung). Setzen Sie ihn auf 1, um die Dehnung aus der Spannung zu verwenden.",
                fr: "Le coefficient de Hill vaut 0 ; la contrainte n'est donc pas utilisée (seule la déformation imposée l'est). Mettez-le à 1 pour utiliser la déformation issue de la contrainte.",
                es: "El coeficiente de Hill es 0, por lo que la tensión no se usa (solo la deformación impuesta). Póngalo en 1 para usar la deformación obtenida de la tensión.",
                pt: "O coeficiente de Hill é 0, por isso a tensão não é usada (apenas a deformação imposta). Defina-o como 1 para usar a deformação obtida da tensão.",
                it: "Il coefficiente di Hill è 0, quindi lo sforzo non viene usato (solo la deformazione imposta). Impostalo a 1 per usare la deformazione ricavata dallo sforzo.",
                ru: "Коэффициент Хилла равен 0, поэтому напряжение не используется (только заданная деформация). Установите 1, чтобы использовать деформацию от напряжения.",
                zhHans: "Hill 系数为 0，因此不使用应力 (只使用输入的应变)。要使用由应力得到的应变，请设为 1。",
                zhHant: "Hill 係數為 0，因此不使用應力 (只使用輸入的應變)。要使用由應力得到的應變，請設為 1。",
                ko: "Hill 계수가 0이므로 응력은 사용되지 않습니다 (입력한 변형만 사용됩니다). 응력으로부터의 변형을 사용하려면 1로 설정하십시오."),
                Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        // 261106Cl 追加: h = 1 では入力した歪みが使われない
        else if (hasStrain && h == 1)
            MessageBox.Show(Crystallography.Localization.Loc(
                en: "The Hill coefficient is 1, so the entered strain is not used (only the strain from the stress is). Set it to 0 to use the entered strain.",
                ja: "Hill 係数が 1 のため、入力した歪みは使われません (応力から計算した歪みだけが使われます)。入力した歪みを使うには 0 にしてください。",
                de: "Der Hill-Koeffizient ist 1, daher wird die vorgegebene Dehnung nicht verwendet (nur die Dehnung aus der Spannung). Setzen Sie ihn auf 0, um die vorgegebene Dehnung zu verwenden.",
                fr: "Le coefficient de Hill vaut 1 ; la déformation imposée n'est donc pas utilisée (seule la déformation issue de la contrainte l'est). Mettez-le à 0 pour utiliser la déformation imposée.",
                es: "El coeficiente de Hill es 1, por lo que la deformación impuesta no se usa (solo la deformación obtenida de la tensión). Póngalo en 0 para usar la deformación impuesta.",
                pt: "O coeficiente de Hill é 1, por isso a deformação imposta não é usada (apenas a deformação obtida da tensão). Defina-o como 0 para usar a deformação imposta.",
                it: "Il coefficiente di Hill è 1, quindi la deformazione imposta non viene usata (solo la deformazione ricavata dallo sforzo). Impostalo a 0 per usare la deformazione imposta.",
                ru: "Коэффициент Хилла равен 1, поэтому заданная деформация не используется (только деформация от напряжения). Установите 0, чтобы использовать заданную деформацию.",
                zhHans: "Hill 系数为 1，因此不使用输入的应变 (只使用由应力得到的应变)。要使用输入的应变，请设为 0。",
                zhHant: "Hill 係數為 1，因此不使用輸入的應變 (只使用由應力得到的應變)。要使用輸入的應變，請設為 0。",
                ko: "Hill 계수가 1이므로 입력한 변형은 사용되지 않습니다 (응력으로부터의 변형만 사용됩니다). 입력한 변형을 사용하려면 0으로 설정하십시오."),
                Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        // 261106Cl 追加: 応力だけを入れたとき、応力からの歪みは h 倍になる (既定の 0.5 で半分。入力した歪み 0 と混ぜるため)
        else if (hasStress && !hasStrain && h != 1)
            MessageBox.Show(Crystallography.Localization.Loc(
                en: $"Only the stress is entered, so the strain used is {hText} × the strain from the stress (Hill coefficient {hText}; the entered strain is 0). To use the stress alone (Reuss model), set the Hill coefficient to 1. For the Hill average, also enter the strain of the Voigt model.",
                ja: $"応力だけが入力されているため、使われる歪みは応力から計算した歪みの {hText} 倍です (Hill 係数 {hText}、入力した歪みは 0)。応力だけを使う (Reuss モデル) には Hill 係数を 1 に、Hill 平均にするには Voigt モデルの歪みも入力してください。",
                de: $"Es ist nur die Spannung eingegeben, daher ist die verwendete Dehnung {hText} × die Dehnung aus der Spannung (Hill-Koeffizient {hText}; die vorgegebene Dehnung ist 0). Um nur die Spannung zu verwenden (Reuss-Modell), setzen Sie den Hill-Koeffizienten auf 1. Für das Hill-Mittel geben Sie auch die Dehnung des Voigt-Modells ein.",
                fr: $"Seule la contrainte est saisie ; la déformation utilisée vaut donc {hText} × la déformation issue de la contrainte (coefficient de Hill {hText} ; la déformation imposée est 0). Pour utiliser la contrainte seule (modèle de Reuss), mettez le coefficient de Hill à 1. Pour la moyenne de Hill, saisissez aussi la déformation du modèle de Voigt.",
                es: $"Solo se ha introducido la tensión, por lo que la deformación usada es {hText} × la deformación obtenida de la tensión (coeficiente de Hill {hText}; la deformación impuesta es 0). Para usar solo la tensión (modelo de Reuss), ponga el coeficiente de Hill en 1. Para el promedio de Hill, introduzca también la deformación del modelo de Voigt.",
                pt: $"Apenas a tensão foi introduzida, por isso a deformação usada é {hText} × a deformação obtida da tensão (coeficiente de Hill {hText}; a deformação imposta é 0). Para usar apenas a tensão (modelo de Reuss), defina o coeficiente de Hill como 1. Para a média de Hill, introduza também a deformação do modelo de Voigt.",
                it: $"È inserito solo lo sforzo, quindi la deformazione usata è {hText} × la deformazione ricavata dallo sforzo (coefficiente di Hill {hText}; la deformazione imposta è 0). Per usare solo lo sforzo (modello di Reuss), imposta il coefficiente di Hill a 1. Per la media di Hill, inserisci anche la deformazione del modello di Voigt.",
                ru: $"Задано только напряжение, поэтому используемая деформация равна {hText} × деформация от напряжения (коэффициент Хилла {hText}; заданная деформация равна 0). Чтобы использовать только напряжение (модель Ройсса), установите коэффициент Хилла 1. Для среднего по Хиллу задайте также деформацию модели Фойгта.",
                zhHans: $"只输入了应力，因此使用的应变是由应力得到的应变的 {hText} 倍 (Hill 系数 {hText}，输入的应变为 0)。若只使用应力 (Reuss 模型)，请将 Hill 系数设为 1；若要 Hill 平均，请同时输入 Voigt 模型的应变。",
                zhHant: $"只輸入了應力，因此使用的應變是由應力得到的應變的 {hText} 倍 (Hill 係數 {hText}，輸入的應變為 0)。若只使用應力 (Reuss 模型)，請將 Hill 係數設為 1；若要 Hill 平均，請同時輸入 Voigt 模型的應變。",
                ko: $"응력만 입력되어 있으므로 사용되는 변형은 응력으로부터의 변형의 {hText}배입니다 (Hill 계수 {hText}, 입력한 변형은 0). 응력만 사용하려면 (Reuss 모델) Hill 계수를 1로, Hill 평균을 사용하려면 Voigt 모델의 변형도 입력하십시오."),
                Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        // 261106Cl 追加: 歪みだけを入れたとき、入力した歪みは (1 − h) 倍になる (既定の 0.5 で半分。応力からの歪み 0 と混ぜるため)
        else if (hasStrain && !hasStress && h != 0)
            MessageBox.Show(Crystallography.Localization.Loc(
                en: $"Only the strain is entered, so the strain used is {restText} × the entered strain (1 − Hill coefficient {hText}; the stress is 0). To use the entered strain alone (Voigt model), set the Hill coefficient to 0. For the Hill average, also enter the stress of the Reuss model.",
                ja: $"歪みだけが入力されているため、使われる歪みは入力した歪みの {restText} 倍です (1 − Hill 係数 {hText}、応力は 0)。入力した歪みだけを使う (Voigt モデル) には Hill 係数を 0 に、Hill 平均にするには Reuss モデルの応力も入力してください。",
                de: $"Es ist nur die Dehnung eingegeben, daher ist die verwendete Dehnung {restText} × die vorgegebene Dehnung (1 − Hill-Koeffizient {hText}; die Spannung ist 0). Um nur die vorgegebene Dehnung zu verwenden (Voigt-Modell), setzen Sie den Hill-Koeffizienten auf 0. Für das Hill-Mittel geben Sie auch die Spannung des Reuss-Modells ein.",
                fr: $"Seule la déformation est saisie ; la déformation utilisée vaut donc {restText} × la déformation imposée (1 − coefficient de Hill {hText} ; la contrainte est 0). Pour utiliser la déformation imposée seule (modèle de Voigt), mettez le coefficient de Hill à 0. Pour la moyenne de Hill, saisissez aussi la contrainte du modèle de Reuss.",
                es: $"Solo se ha introducido la deformación, por lo que la deformación usada es {restText} × la deformación impuesta (1 − coeficiente de Hill {hText}; la tensión es 0). Para usar solo la deformación impuesta (modelo de Voigt), ponga el coeficiente de Hill en 0. Para el promedio de Hill, introduzca también la tensión del modelo de Reuss.",
                pt: $"Apenas a deformação foi introduzida, por isso a deformação usada é {restText} × a deformação imposta (1 − coeficiente de Hill {hText}; a tensão é 0). Para usar apenas a deformação imposta (modelo de Voigt), defina o coeficiente de Hill como 0. Para a média de Hill, introduza também a tensão do modelo de Reuss.",
                it: $"È inserita solo la deformazione, quindi la deformazione usata è {restText} × la deformazione imposta (1 − coefficiente di Hill {hText}; lo sforzo è 0). Per usare solo la deformazione imposta (modello di Voigt), imposta il coefficiente di Hill a 0. Per la media di Hill, inserisci anche lo sforzo del modello di Reuss.",
                ru: $"Задана только деформация, поэтому используемая деформация равна {restText} × заданная деформация (1 − коэффициент Хилла {hText}; напряжение равно 0). Чтобы использовать только заданную деформацию (модель Фойгта), установите коэффициент Хилла 0. Для среднего по Хиллу задайте также напряжение модели Ройсса.",
                zhHans: $"只输入了应变，因此使用的应变是输入应变的 {restText} 倍 (1 − Hill 系数 {hText}，应力为 0)。若只使用输入的应变 (Voigt 模型)，请将 Hill 系数设为 0；若要 Hill 平均，请同时输入 Reuss 模型的应力。",
                zhHant: $"只輸入了應變，因此使用的應變是輸入應變的 {restText} 倍 (1 − Hill 係數 {hText}，應力為 0)。若只使用輸入的應變 (Voigt 模型)，請將 Hill 係數設為 0；若要 Hill 平均，請同時輸入 Reuss 模型的應力。",
                ko: $"변형만 입력되어 있으므로 사용되는 변형은 입력한 변형의 {restText}배입니다 (1 − Hill 계수 {hText}, 응력은 0). 입력한 변형만 사용하려면 (Voigt 모델) Hill 계수를 0으로, Hill 평균을 사용하려면 Reuss 모델의 응력도 입력하십시오."),
                Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>Miller-Bravais 指数を有効化</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool MillerBravais
    {
        set
        {
            var enabled = value && crystal != null && crystal.MillerBravaisCapable; // 260517Cl 共通式の一度きり評価、crystal null 防御
            FormSymmetryInformation.MillerBravais = FormBeamInteraction.MillerBravais = enabled;
            latticePlaneControl.MillerBravaisIndex = boundControl.MillerBravaisIndex = enabled;
        }
    }
}
