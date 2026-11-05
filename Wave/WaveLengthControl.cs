using System;
using System.Collections.Generic; // 261105Cl 追加: lastEnergy (線源ごとの最後のエネルギー)
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Crystallography.Controls;

[ToolboxItem(true)] // 260605Cl 追加: 基底 UserControlBase の [ToolboxItem(false)] 継承を打ち消しデザイナのツールボックスに表示
public partial class WaveLengthControl : UserControlBase
{

    public event EventHandler WavelengthChanged;
    public event EventHandler WaveSourceChanged;
    public event EventHandler WavelengthUnitChanged;

    #region プロパティ

    #region FlowDirection プロパティ (コントロールの配置方向)

    /// <summary>コントロールの配置をLeftToRightか、TopDownにするか</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Category("Appearance")]
    [Description("全体の配置方向 (LeftToRight: 横並び / TopDown: 縦並び)。")]                                                              // 260522Cl 追加
    [DefaultValue(FlowDirection.TopDown)] // 260606Cl 追加: 既定値を明示し、既定のままなら消費側 InitializeComponent へ serialize されないようにする (グリッドの太字/Reset も正常化)
    public FlowDirection DirectionWhole
    {
        set
        {
            // 260606Cl: 旧実装は配置方向に応じて this.AutoSize を切替えていたが、3 方向プロパティ + Dock ベースのレイアウトに移行したため AutoSize 操作は廃止 (Dock だけで意図どおり配置される)。
            directionWhole = value;
            flowLayoutPanelWaveSource.Dock = directionWhole == FlowDirection.LeftToRight ? DockStyle.Left : DockStyle.Top;
        }
        get => directionWhole;
    }
    private FlowDirection directionWhole = FlowDirection.TopDown;

    /// <summary>コントロールの配置をLeftToRightか、TopDownにするか</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Category("Appearance")]
    [Description("線種要素の配置方向 (LeftToRight: 横並び / TopDown: 縦並び)。")]                                                              // 260522Cl 追加
    [DefaultValue(FlowDirection.LeftToRight)] // 260606Cl: 既定値 (flowLayoutPanelWaveSource の初期 FlowDirection) を明示
    public FlowDirection DirectionWaveSource
    {
        // 260606Cl 状態の複製を解消: backing field を廃し子パネルの FlowDirection を単一の真実とする (直下 LengthUnit がラジオから導出するのと同形)。
        // 旧: set => directionWaveSource = flowLayoutPanelWaveSource.FlowDirection = value; get => directionWaveSource; (private FlowDirection directionWaveSource = FlowDirection.LeftToRight;)
        set => flowLayoutPanelWaveSource.FlowDirection = value;
        get => flowLayoutPanelWaveSource.FlowDirection;
    }

    /// <summary>コントロールの配置をLeftToRightか、TopDownにするか</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Category("Appearance")]
    [Description("波長&エネルギーコントロールの要素の配置方向 (LeftToRight: 横並び / TopDown: 縦並び)。")]                                                              // 260522Cl 追加
    [DefaultValue(FlowDirection.TopDown)] // 260606Cl: 既定値 (flowLayoutPanelWaveEnergy の初期 FlowDirection) を明示
    public FlowDirection DirectionWaveEnergy
    {
        // 260606Cl 状態の複製を解消: backing field を廃し子パネルの FlowDirection を単一の真実とする (直下 LengthUnit がラジオから導出するのと同形)。
        // 旧: set => directionWaveEnergy = flowLayoutPanelWaveEnergy.FlowDirection = value; get => directionWaveEnergy; (private FlowDirection directionWaveEnergy = FlowDirection.TopDown;)
        set => flowLayoutPanelWaveEnergy.FlowDirection = value;
        get => flowLayoutPanelWaveEnergy.FlowDirection;
    }


    #endregion



    /// <summary>波長の表示単位 (Å / nm)。表示値とフッタ単位ラベルを切り替えるだけで、物理波長 (WaveLength は常に nm) は不変。</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Category("Appearance")]
    [Description("波長の表示単位 (Angstrom: Å / NanoMeter: nm)。WaveLength プロパティは表示単位に依らず常に nm を返す。")] // 260606Cl 追加
    [DefaultValue(LengthUnitEnum.Angstrom)] // 260606Cl 追加: 既定値 (radioButtonUnitAngstrom.Checked) を明示
    public LengthUnitEnum LengthUnit
    {
        get => radioButtonUnitAngstrom.Checked ? LengthUnitEnum.Angstrom : LengthUnitEnum.NanoMeter;
        set
        {
            if (value == LengthUnitEnum.Angstrom)
                radioButtonUnitAngstrom.Checked = true;
            else
                radioButtonUnitNanoMeter.Checked = true;
        }
    }

    // 260606Cl 追加: numericBoxWaveLength.Value(=表示単位) と物理波長(nm) の換算係数。Å表示=10, nm表示=1 (1 nm = 10 Å)。
    // エネルギー変換関数は nm 入力/出力なので、表示値→nm は ÷係数、nm→表示値は ×係数で一貫させる。
    private double waveLengthUnitPerNm => LengthUnit == LengthUnitEnum.Angstrom ? 10.0 : 1.0;


    private bool monochrome = true;
    /// <summary>単色モードかどうか falseの場合は白色モード</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Category("Appearance")]
    [Description("単色モードかどうか。false の場合は白色光 (連続スペクトル) モード。")]                                                                // 260522Cl 追加
    [DefaultValue(true)] // 260607Cl
    public bool Monochrome
    {
        set
        {
            monochrome = value;
            numericBoxEnergy.Visible = numericBoxWaveLength.Visible = monochrome;
            flowLayoutPanelElement.Visible = WaveSource == WaveSource.Xray && monochrome;
            labelFlatWhite.Visible = !monochrome;
        }
        get => monochrome;
    }

    // 260521Cl: 数値入力部以外 (見出し/フッタ/コンボ/ラジオ/ラベル) のフォント。数値部のフォントは ValueFontSize で調整する。
    [Category("Appearance"), Localizable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Description("数値部以外 (見出し・フッタ・コンボ・ラジオボタン・ラベル) のフォント。数値入力部のフォントサイズは ValueFontSize で調整する。")]
    public Font LabelFont
    {
        set
        {
            numericBoxEnergy.HeaderFont = numericBoxEnergy.FooterFont = value;
            numericBoxWaveLength.HeaderFont = numericBoxWaveLength.FooterFont = value;
            comboBoxXRayElement.Font = comboBoxXrayLine.Font = value;
            radioButtonElectron.Font = radioButtonNeutron.Font = radioButtonXray.Font = value;
            label1.Font = value;
        }
        // 260521Cl 修正: setter は ValueFont を触らなくなった (数値部は ValueFontSize で別管理) ため、
        // getter が numericBoxWaveLength.ValueFont を返すと set/get が一致せずデザイナが誤ったフォントを serialize する。
        // setter が実際に設定する label1.Font を返すよう修正。
        get => label1.Font;
    }

    // 260521Cl: 数値入力部 (Energy / WaveLength) のフォントサイズ。NumericBox.ValueFontSize へ委譲する。
    [Category("Appearance"), Localizable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Description("数値入力部 (Energy / WaveLength) のフォントサイズ (pt)。")]
    [DefaultValue(9.75f)] // 260607Cl
    public float ValueFontSize
    {
        set => numericBoxEnergy.ValueFontSize = numericBoxWaveLength.ValueFontSize = value;
        get => numericBoxWaveLength.ValueFontSize;
    }

    // 260426Cl 修正: public フィールドを private に変更 (外部参照なしを確認済み)
    //public bool showWaveSource = true;
    private bool showWaveSource = true;
    /// <summary>WaveSourceを表示するかどうか</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Category("Appearance")]
    [Description("線源選択 (X-ray / Electron / Neutron) のラジオボタンを表示するかどうか。")]                                                          // 260522Cl 追加
    [DefaultValue(true)] // 260607Cl
    public bool ShowWaveSource
    {
        set => showWaveSource = flowLayoutPanelWaveSource.Visible = value;
        get => showWaveSource;
    }

    // 260426Cl 削除: WaveLengthText の実体は numericBoxWaveLength.Value であり、
    // この public フィールドはどこからも参照されない死コードだったため撤去
    //public string waveLengthText = "0.4";

    /// <summary>波長をÅ単位のテキスト形式で取得/設定。表示単位 (LengthUnit) に依らず常にÅで入出力する。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string WaveLengthText
    {
        set
        {
            // 260606Cl 堅牢化: value は常にÅ文字列。WaveLength(nm) = Å/10 経由で設定し表示単位に依らず正しくスケールされるようにする。
            // 260426Cl 修正: 空文字や数値変換失敗で発生する例外型に絞り込み
            //numericBoxWaveLength.Value = Convert.ToDouble(value); // 260606Cl 旧: 表示単位に直接代入でスケールずれ
            try
            {
                WaveLength = Convert.ToDouble(value) / 10.0; // Å → nm
                comboBoxXRayElement.SelectedIndex = 0;
            }
            catch (FormatException) { }
            catch (OverflowException) { }
        }
        // 260606Cl 堅牢化: 表示単位に依らず常にÅ文字列を返す (WaveLength は nm → ×10 で Å へ変換)
        //get => numericBoxWaveLength.Text; // 260606Cl 旧: nm表示時に nm テキストを返してしまいずれが生じた
        get => (WaveLength * 10.0).ToString();
    }

    /// <summary>波長をnm単位のdoubleで取得/設定</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Category("Behavior")]
    [Description("波長 (nm 単位)。")]                                                                                                                  // 260522Cl 追加
    [DefaultValue(0.0)] // 260607Cl
    public double WaveLength
    {
        set
        {
            if (value > 0)
            {
                skipEvent = true;
                if (radioButtonXray.Checked)
                    comboBoxXRayElement.SelectedIndex = 0;
                skipEvent = false;

                // 260606Cl 修正: 表示単位 (Å/nm) を考慮し、固定係数 10 を waveLengthUnitPerNm に置換 (WaveLength は常に nm)
                //numericBoxWaveLength.Value = value * 10.0;
                numericBoxWaveLength.Value = value * waveLengthUnitPerNm;
            }
        }
        //get => numericBoxWaveLength.Value / 10.0; // 260606Cl 修正
        get => numericBoxWaveLength.Value / waveLengthUnitPerNm;
    }

    WaveSource waveSource = WaveSource.Xray;
    /// <summary>線源を取得/設定</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Category("Behavior")]                                                                                                                            // 260522Cl 追加: カテゴリ欠落を補完
    [Description("線源の種類 (X-ray / Electron / Neutron)。")]                                                                                         // 260522Cl 追加
    [DefaultValue(WaveSource.Xray)] // 260607Cl
    public WaveSource WaveSource
    {
        set
        {
            // 260426Cl 修正: インデントずれを修正
            waveSource = value;
            if (waveSource == WaveSource.Xray)
                radioButtonXray.Checked = true;
            else if (waveSource == WaveSource.Electron)
                radioButtonElectron.Checked = true;
            else
                radioButtonNeutron.Checked = true;
        }
        get
        {
            if (radioButtonXray.Checked)
                return WaveSource.Xray;
            else if (radioButtonElectron.Checked)
                return WaveSource.Electron;
            else
                return WaveSource.Neutron; // radioButtonNeutron.Checked
        }
    }

    //private int _XrayWaveSourceElementNumber = 0; // 260717Cl: 書き込み専用の死にフィールド (getter は comboBox.SelectedIndex を返す) のため削除

    /// <summary>X線の線源の元素を取得/設定</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Category("Behavior")]
    [Description("X線線源の元素 (元素コンボボックスのインデックス。0 は Custom = 任意波長)。")]                                                        // 260522Cl 追加
    public int XrayWaveSourceElementNumber
    {
        set
        {
            if (value < comboBoxXRayElement.Items.Count && value >= 0)
            {
                comboBoxXRayElement.SelectedIndex = value;
                //_XrayWaveSourceElementNumber = value; // 260717Cl: 死にフィールド削除に伴い除去
            }
        }
        get
        {
            // 260426Cl 整理: Invoke の args 引数 (null) を省略
            if (InvokeRequired)
                return (int)Invoke(new Func<int>(() => XrayWaveSourceElementNumber));
            else
                return comboBoxXRayElement.SelectedIndex;
        }
    }

    /// <summary>X線の線源のLineを取得/設定</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Category("Behavior")]                                                                                                                            // 260522Cl 追加: カテゴリ欠落を補完
    [Description("X線線源の特性X線ライン (Kα1 など)。")]                                                                                              // 260522Cl 追加
    [DefaultValue(XrayLine.Ka1)] // 260607Cl
    public XrayLine XrayWaveSourceLine
    {
        set => comboBoxXrayLine.SelectedItem = value;
        get
        {
            if (comboBoxXrayLine.SelectedItem == null)
                return XrayLine.Ka1;
            else
                return (XrayLine)comboBoxXrayLine.SelectedItem;
        }
    }

    /// <summary>
    /// 線源のエネルギー (kV)を取得/設定
    /// X線と電子は単位はkev,中性子はmev
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Category("Behavior")]
    [Description("線源のエネルギー (X線・電子線は keV/kV、中性子は meV)。")]                                                                           // 260522Cl 追加
    [DefaultValue(0.0)] // 260607Cl
    public double Energy
    {
        set
        {
            if (value > 0)
                numericBoxEnergy.Value = value;
        }
        get => numericBoxEnergy.Value;
    }



    /// <summary>電子線加速電圧(kV)をテキスト形式で取得/設定</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string EnergyText
    {
        set
        {
            // 260426Cl 修正: catch を具体的な例外型へ絞り込み
            try
            {
                numericBoxEnergy.Value = Convert.ToDouble(value);
            }
            catch (FormatException) { }
            catch (OverflowException) { }
        }
        get => numericBoxEnergy.Value.ToString();
    }

    /// <summary></summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public WaveProperty Property
    {
        set
        {
            if (value == null)
                return;

            WaveSource = value.Source;
            Energy = value.Energy;
            //XrayWaveSourceLine = value.XrayLine; // 261105Cl 変更前
            //XrayWaveSourceElementNumber = value.XrayElementNumber; // 261105Cl 変更前
            //if (WaveSource == Crystallography.WaveSource.Xray && XrayWaveSourceElementNumber == 0) // 261105Cl 変更前
            //    WaveLength = value.WaveLength; // 261105Cl 変更前
            // 261105Cl 変更 (監査 J4): 元素を先に選ぶ。元素を変えると線種の一覧が作り直されて先頭の線 (Kα) が選ばれるので、
            //  以前は線種を先に入れると、元素が変わったときに保存した線種 (Kβ1 など) が Kα に戻っていた
            XrayWaveSourceElementNumber = value.XrayElementNumber; // 261105Cl
            XrayWaveSourceLine = value.XrayLine; // 261105Cl
            // 261105Cl 変更 (監査 J3): 電子線・中性子でも波長 (nm) を戻す。ReciPro の多結晶回折が保存する .ipa は Energy = 0 で
            //  Energy の setter が何もしないため、以前は電子線・中性子の波長が復元されず、前の値のままだった。
            //  波長があればそれを正とする (Energy は線源ごとに keV・meV と単位が違い、保存した側の単位を確かめられない)
            if ((WaveSource != Crystallography.WaveSource.Xray || XrayWaveSourceElementNumber == 0) && value.WaveLength > 0) // 261105Cl
                WaveLength = value.WaveLength; // 261105Cl
        }
        get
        {
            return new WaveProperty(WaveSource, WaveLength, XrayWaveSourceElementNumber, XrayWaveSourceLine, Energy);
        }
    }

    #endregion プロパティ

    public WaveLengthControl()
    {
        InitializeComponent();
        // (260322Ch) Designer ではコンポーネント生成後の runtime 初期化だけ抑止する
        if (DesignMode)
            return;

        comboBoxXRayElement.SelectedIndex = 0;
    }

    /// <summary>X線のElementが変更されたとき</summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void comboBoxXRayElement_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (!radioButtonXray.Checked) return;

        var dpiScale = DeviceDpi / 96f; // 260331Cl 追加: DPIスケーリング対応
        if (comboBoxXRayElement.SelectedIndex == 0)//Customが選択されたとき
        {
            comboBoxXrayLine.Visible = false;
            //comboBoxXRayElement.Width = 100; // 260331Cl 変更: DPIスケーリング対応
            comboBoxXRayElement.Width = (int)(100 * dpiScale);
            numericBoxEnergy.Enabled = true;
            numericBoxWaveLength.Enabled = true;
        }
        else
        {
            //comboBoxXRayElement.Width = 70; // 260331Cl 変更: DPIスケーリング対応
            comboBoxXRayElement.Width = (int)(70 * dpiScale);
            comboBoxXrayLine.Visible = true;
            numericBoxEnergy.Enabled = false;

            numericBoxWaveLength.Enabled = false;

            comboBoxXrayLine.Items.Clear();
            //XrayLine[] temp = (XrayLine[])Enum.GetValues(typeof(XrayLine));
            var temp = Enum.GetValues<XrayLine>(); // 260717Cl: ジェネリック版でキャストと typeof を除去
            for (int i = 0; i < temp.Length; i++)
            {
                if (!double.IsNaN(AtomStatic.CharacteristicXrayWavelength(comboBoxXRayElement.SelectedIndex, temp[i])))
                    comboBoxXrayLine.Items.Add(temp[i]);
            }
            if (comboBoxXrayLine.Items.Count == 0)
                comboBoxXrayLine.Enabled = false;
            else
            {
                comboBoxXrayLine.Enabled = true;
                comboBoxXrayLine.SelectedIndex = 0;
                WavelengthChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>X線のラインが変更したとき</summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void comboBoxXrayLine_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (this.Enabled && comboBoxXrayLine.SelectedItem != null)
        {
            var d = AtomStatic.CharacteristicXrayWavelength(comboBoxXRayElement.SelectedIndex, (XrayLine)comboBoxXrayLine.SelectedItem);
            if (!double.IsNaN(d))
            {
                skipEvent = true;
                // 260606Cl 修正: CharacteristicXrayWavelength は Å を返す。表示単位へ換算する (Å→nm = d/10、nm→表示値 = ×waveLengthUnitPerNm)
                //numericBoxWaveLength.Value = d;
                numericBoxWaveLength.Value = d / 10.0 * waveLengthUnitPerNm;
                //numericBoxEnergy.Value = UniversalConstants.Convert.WavelengthToXrayEnergy(numericBoxWaveLength.Value / 10) / 1000; // 260606Cl 修正
                numericBoxEnergy.Value = UniversalConstants.Convert.WavelengthToXrayEnergy(numericBoxWaveLength.Value / waveLengthUnitPerNm) / 1000;
                skipEvent = false;
                WavelengthChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>現状の原子番号、線種で、特性X線の波長とエネルギーをリセット</summary>
    // 260426Cl 整理: sender に空 object、引数に new EventArgs() を渡していた箇所を素直な値へ
    public void SetCharacteristicXray() => comboBoxXrayLine_SelectedIndexChanged(this, EventArgs.Empty);

    // 261105Cl 追加 (監査 J18): 線源ごとの最後のエネルギー (X 線は keV、電子線は keV = kV、中性子は meV) と、X 線の元素・線種。
    //  線源を切り替えたら、出ていく線源の値をここに覚え、入る線源の値を戻す。以前は波長を保ったまま線源だけ替えたので、
    //  X 線 (Cu Kα, 0.154 nm) から電子線へ切り替えると約 0.063 keV の電子になっていた。
    //  一度も使っていない線源は既定値: X 線 = Cu Kα (BeamInteraction・横軸の部品の Designer の既定と同じ)、
    //  電子線 = 200 keV (回折シミュレータ・Spot ID v2 の既定)、中性子 = 25.3 meV (熱中性子、λ ≈ 0.18 nm。BeamInteraction の撮影用の値と同じ)。
    //  記憶はこの部品が生きている間だけ (各アプリが保存・読込する Property には入れない。Property の形式は変えない)
    private readonly Dictionary<WaveSource, double> lastEnergy = new() // 261105Cl 追加
    {
        [WaveSource.Xray] = 8.04114721, // Cu Kα (元素と線種で決まるので、元素 = Custom のときだけ使う)
        [WaveSource.Electron] = 200,
        [WaveSource.Neutron] = 25.3,
    };
    private int lastXrayElement = 29; // 261105Cl 追加: Cu
    private XrayLine lastXrayLine = XrayLine.Ka; // 261105Cl 追加
    private WaveSource shownSource = WaveSource.Xray; // 261105Cl 追加: 今の欄の値がどの線源のものか (Designer の既定は X 線)

    /// <summary>線源が変更されたとき</summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void radioButtonWaveSource_CheckedChanged(object sender, EventArgs e)
    {
        // 261105Cl 追加 (監査 J18): 切り替えのたびに外れた側と入った側の 2 回呼ばれる (どちらの時点でもラジオは新しい状態)。
        //  入った側の 1 回だけ処理する (以前は同じ処理を 2 回して WaveSourceChanged も 2 回出ていた)
        if (sender is RadioButton { Checked: false }) return; // 261105Cl

        // 261105Cl 追加 (監査 J18): 出ていく線源の値を覚え、入る線源の値を戻す。
        //  ハンドルができる前 (各アプリの InitializeComponent・コンストラクタで初期値を入れている間) は従来どおり波長を保つ:
        //  Designer は Energy・WaveLength を WaveSource より先に書くので、そこで値を戻すと各アプリの既定 (EBSD の 20 keV など) が消える。
        //  ハンドルができた後はユーザの切り替えも、コードからの WaveSource の代入 (横軸の部品のラジオ・マクロ・ファイルの読込) も同じに扱う。
        //  ファイルの読込は WaveSource の後にエネルギー・波長を入れ直すので、読み込んだ値が残る。VS のデザイナの上では従来どおり (Designer.cs の値を変えない)
        var newSource = WaveSource; // 261105Cl
        bool restore = IsHandleCreated && !DesignMode && newSource != shownSource; // 261105Cl
        if (restore) // 261105Cl
        {
            if (double.IsFinite(numericBoxEnergy.Value) && numericBoxEnergy.Value > 0) // 0・∞ (波長 0 の欄) は覚えない
                lastEnergy[shownSource] = numericBoxEnergy.Value;
            if (shownSource == WaveSource.Xray)
            {
                lastXrayElement = comboBoxXRayElement.SelectedIndex;
                lastXrayLine = XrayWaveSourceLine;
            }
        }
        shownSource = newSource; // 261105Cl

        if (radioButtonXray.Checked)
        {
            //flowLayoutPanelElement.Visible = true; // 261105Cl 変更前
            flowLayoutPanelElement.Visible = monochrome; // 261105Cl 変更 (監査 J17): 白色 (Back-Laue) では元素・線種を出さない (Monochrome の setter と同じ条件)
            numericBoxEnergy.FooterText = "keV";
            //comboBoxXRayElement_SelectedIndexChanged(sender, e); // 261105Cl 変更前
            // 261105Cl 変更 (監査 J18): 覚えていた元素を選ぶ (元素の欄の SelectedIndexChanged が線種の一覧を作り直す)。元素が同じなら従来どおり作り直すだけ
            if (restore && lastXrayElement >= 0 && lastXrayElement < comboBoxXRayElement.Items.Count && comboBoxXRayElement.SelectedIndex != lastXrayElement) // 261105Cl
                comboBoxXRayElement.SelectedIndex = lastXrayElement; // 261105Cl
            else // 261105Cl
                comboBoxXRayElement_SelectedIndexChanged(sender, e);
            if (restore && comboBoxXRayElement.SelectedIndex != 0) // 261105Cl: 一覧の作り直しで先頭の線 (Kα) になるので、覚えていた線種に戻す
                XrayWaveSourceLine = lastXrayLine; // 261105Cl
        }
        else
        {
            flowLayoutPanelElement.Visible = false;
            //numericBoxEnergy.Visible = true; // 261105Cl 変更前
            numericBoxEnergy.Visible = monochrome; // 261105Cl 変更 (監査 J17): 白色のときに隠したエネルギー欄を、線源の切り替えで出さない
            numericBoxEnergy.Enabled = true;
            numericBoxWaveLength.Enabled = true;

            if (radioButtonElectron.Checked)
                numericBoxEnergy.FooterText = "keV";
            else if (radioButtonNeutron.Checked)
                numericBoxEnergy.FooterText = "meV";
        }
        //numericBoxWaveLength_ValueChanged(sender, e); // 261105Cl 変更前: 常に波長を保ってエネルギーを計算し直していた
        // 261105Cl 変更 (監査 J18): 戻すときはエネルギーを入れて、この線源の式で波長を計算し直す (特性 X 線は上で線種が波長を決めている)
        if (restore && (newSource != WaveSource.Xray || comboBoxXRayElement.SelectedIndex == 0)) // 261105Cl
        {
            skipEvent = true; // 値が変わったときの ValueChanged を止め、下で 1 回だけ計算する (値が同じでも計算させるため)
            numericBoxEnergy.Value = lastEnergy[newSource];
            skipEvent = false;
            numericBoxEnergy_ValueChanged(sender, e); // 波長を計算し WavelengthChanged を出す
        }
        else // 261105Cl
            numericBoxWaveLength_ValueChanged(sender, e);
        WaveSourceChanged?.Invoke(sender, e);
    }

    // 261105Cl 追加 (監査 K2/J20): 線種の一覧は XrayLine の値 (Ka1 など) をそのまま項目にしているので、表示だけ Kα1・Kβ1 にする
    //  (BeamInteraction の蛍光の表と同じ表記。KbI2 → KβI2 は resx の項目の表記と同じ)。enum の名前は a・b がギリシャ文字の印で、ほかに小文字は無い。
    //  項目 (SelectedItem) は enum のままなので、XrayWaveSourceLine の読み書きと保存する値は変わらない
    private void comboBoxXrayLine_Format(object sender, ListControlConvertEventArgs e)
    {
        if (e.ListItem is XrayLine line)
            e.Value = line.ToString().Replace('a', 'α').Replace('b', 'β');
    }

    private bool skipEvent = false;

    /// <summary>波長が直接変更されたとき</summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void numericBoxWaveLength_ValueChanged(object sender, EventArgs e)
    {
        if (skipEvent) return;

        skipEvent = true;
        if (radioButtonXray.Checked)
            numericBoxEnergy.Value = UniversalConstants.Convert.WavelengthToXrayEnergy(numericBoxWaveLength.Value / waveLengthUnitPerNm) / 1000;
        else if (radioButtonElectron.Checked)
            numericBoxEnergy.Value = UniversalConstants.Convert.WaveLengthToElectronEnergy(numericBoxWaveLength.Value / waveLengthUnitPerNm);
        else
            numericBoxEnergy.Value = UniversalConstants.Convert.WaveLengthToNeutronEnergy(numericBoxWaveLength.Value / waveLengthUnitPerNm) / 1.0E6;
        skipEvent = false;
        WavelengthChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>エネルギーが直接変更されたとき</summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void numericBoxEnergy_ValueChanged(object sender, EventArgs e)
    {
        if (skipEvent) return;
        skipEvent = true;
        if (radioButtonXray.Checked) //X線の時
            numericBoxWaveLength.Value = UniversalConstants.Convert.EnergyToXrayWaveLength(numericBoxEnergy.Value * 1000) * waveLengthUnitPerNm;
        else if (radioButtonElectron.Checked)//電子線の時
            numericBoxWaveLength.Value = UniversalConstants.Convert.EnergyToElectronWaveLength(numericBoxEnergy.Value) * waveLengthUnitPerNm;
        else//中性子
            numericBoxWaveLength.Value = UniversalConstants.Convert.EnergyToNeutronWaveLength(numericBoxEnergy.Value * 1.0E6) * waveLengthUnitPerNm;
        skipEvent = false;
        WavelengthChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>波長の表示単位 (Å / nm) が切り替わったとき。表示値を新単位へ換算し、フッタ単位ラベルを更新する。</summary>
    // 260606Cl 追加実装: Å/nm の2択ラジオで Å 側の CheckedChanged のみ購読しているため、どちらへ切り替えても本ハンドラが1回だけ発火する。
    // ハンドラ発火時点で LengthUnit は既に新しい状態を返す。物理波長 (nm) は不変に保ちたいので表示値だけを再スケールする:
    //   Angstrom になった (nm→Å) → ×10、NanoMeter になった (Å→nm) → ÷10。
    private void radioButtonUnitAngstrom_CheckedChanged(object sender, EventArgs e)
    {
        skipEvent = true; // 物理波長は不変なので、エネルギー再計算と WavelengthChanged の発火を抑止する
        numericBoxWaveLength.Value *= LengthUnit == LengthUnitEnum.Angstrom ? 10.0 : 0.1;
        numericBoxWaveLength.FooterText = LengthUnit == LengthUnitEnum.Angstrom ? "Å" : "nm";
        skipEvent = false;

        WavelengthUnitChanged?.Invoke(this, EventArgs.Empty);
    }
}

