using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using static Crystallography.Localization; // 261105Cl 追加 (Loc)

namespace Crystallography.Controls;

[ToolboxItem(true)] // 260605Cl 追加: 基底 UserControlBase の [ToolboxItem(false)] 継承を打ち消しデザイナのツールボックスに表示
public partial class ElasticityControl : UserControlBase
{
    //public ElasticityControl() => InitializeComponent(); // 261105Cl 変更前
    // 261105Cl 変更: 拘束の注記 (labelConstraint) と入力欄の可否を初期の結晶系 (三斜晶) に合わせる
    public ElasticityControl()
    {
        InitializeComponent();
        SetElasticity(changeValues: false);
    }

    public delegate void MyEventHandler(object sender, EventArgs e);

    public event MyEventHandler ValueChanged;

    private int symmetrySeriesNumber = 1;

    /// <summary>結晶系 (拘束) を決める空間群の通し番号。
    /// 261105Cl 変更: 設定しても欄の値は変えない (読み込んだ値・保存済みの値を黙って 0 にしたり書き換えたりしない)。
    /// 入力欄の可否と拘束の注記だけを更新し、拘束は利用者が値を編集したときに掛ける。満たしていなければ注記で知らせる</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [DefaultValue(1)] // 260607Cl
    public int SymmetrySeriesNumber
    {
        get => symmetrySeriesNumber;
        //set { symmetrySeriesNumber = value; SetElasticity(); } // 261105Cl 変更前
        set
        {
            // 261105Cl 変更: 範囲外は拘束なし (三斜晶) として扱う
            symmetrySeriesNumber = value >= 0 && value < SymmetryStatic.Symmetries.Length ? value : 1;
            SetElasticity(changeValues: false);
        }
    }

    /// <summary>261105Cl 追加: 今の結晶系による拘束の種類</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Elasticity.SymmetryConstraint Constraint => Elasticity.GetSymmetryConstraint(SymmetryStatic.Symmetries[SymmetrySeriesNumber]);

    /// <summary>261105Cl 追加: 欄の値が今の結晶系の拘束を満たしているか (満たさないのは、拘束の無い時期に入れた値や別の座標の値を読み込んだとき)</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SatisfiesConstraint { get; private set; } = true;

    /// <summary>261105Cl 追加: (i, j) 成分 (0 始まり、上三角) の入力欄</summary>
    private NumericBox[,] Boxes => boxes ??= new NumericBox[,]
    {
        { numericBox11, numericBox12, numericBox13, numericBox14, numericBox15, numericBox16 },
        { null, numericBox22, numericBox23, numericBox24, numericBox25, numericBox26 },
        { null, null, numericBox33, numericBox34, numericBox35, numericBox36 },
        { null, null, null, numericBox44, numericBox45, numericBox46 },
        { null, null, null, null, numericBox55, numericBox56 },
        { null, null, null, null, null, numericBox66 },
    };
    private NumericBox[,] boxes; // 261105Cl 追加

    /// <summary>261105Cl 追加: プログラムからラジオボタンを切り替える間、CheckedChanged の変換を止める</summary>
    private bool skipRadio = false;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [DefaultValue(Elasticity.Mode.Stiffness)] // 260607Cl
    public Elasticity.Mode Mode
    {
        get => radioButtonCompliance.Checked ? Elasticity.Mode.Compliance : Elasticity.Mode.Stiffness;
        set
        {
            radioButtonStiffness.Checked = value == Elasticity.Mode.Stiffness;
            radioButtonCompliance.Checked = value == Elasticity.Mode.Compliance;
        }
    }

    private Matrix<double> compliance = new DenseMatrix(6, 6);
    private Matrix<double> stiffness = new DenseMatrix(6, 6);
    private bool skip = false;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Matrix<double> Compliance
    {
        get => compliance;
        set => SetMatrix(value, isStiffness: false); // 260717Cl: Stiffness setter との鏡像コピペを SetMatrix へ統合
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Matrix<double> Stiffness
    {
        get => stiffness;
        set => SetMatrix(value, isStiffness: true); // 260717Cl: 同上
    }

    /// <summary>260717Cl 追加 (/simplify): Compliance/Stiffness setter で鏡像重複していた
    /// 「検証→radio→代入→ボックス反映→SetElasticity→逆行列→通知」の共通本体 (処理順は旧 setter と同一)。</summary>
    private void SetMatrix(Matrix<double> value, bool isStiffness)
    {
        if (!IsValid6x6(value)) return;
        //(isStiffness ? radioButtonStiffness : radioButtonCompliance).Checked = true; // 261105Cl 変更前
        // 261105Cl 変更: ここでのラジオの切り替えは表示の種類を合わせるだけ。CheckedChanged の逆行列への変換 (直後に value で上書きされる) を走らせない
        skipRadio = true;
        (isStiffness ? radioButtonStiffness : radioButtonCompliance).Checked = true;
        skipRadio = false;
        if (isStiffness) stiffness = value; else compliance = value;
        ApplyMatrixToBoxes(value);
        //SetElasticity(); // 261105Cl 変更前: 結晶系の拘束で値を書き換えていた (0 にする成分・他から決まる成分)
        SetElasticity(changeValues: false); // 261105Cl 変更: 読み込んだ値は変えない (入力欄の可否と注記だけ)
        if (isStiffness) compliance = stiffness.TryInverse(); else stiffness = compliance.TryInverse();
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private static bool IsValid6x6(Matrix<double> m) => m != null && m.RowCount == 6 && m.ColumnCount == 6;

    private void ApplyMatrixToBoxes(Matrix<double> v)
    {
        skip = true;
        numericBox11.Value = v[0, 0]; numericBox12.Value = v[0, 1]; numericBox13.Value = v[0, 2];
        numericBox14.Value = v[0, 3]; numericBox15.Value = v[0, 4]; numericBox16.Value = v[0, 5];
        numericBox22.Value = v[1, 1]; numericBox23.Value = v[1, 2]; numericBox24.Value = v[1, 3];
        numericBox25.Value = v[1, 4]; numericBox26.Value = v[1, 5];
        numericBox33.Value = v[2, 2]; numericBox34.Value = v[2, 3]; numericBox35.Value = v[2, 4]; numericBox36.Value = v[2, 5];
        numericBox44.Value = v[3, 3]; numericBox45.Value = v[3, 4]; numericBox46.Value = v[3, 5];
        numericBox55.Value = v[4, 4]; numericBox56.Value = v[4, 5];
        numericBox66.Value = v[5, 5];
        skip = false;
    }

    #region 261105Cl 変更前の SetElasticity (結晶系ごとに入力欄を無効にして 0 を入れ、直後に numericBoxElasticiry_ValueChanged で他の成分を埋めていた)
    //private void SetElasticity()
    //{
    //    var sym = SymmetryStatic.Symmetries[SymmetrySeriesNumber];
    //
    //    // 全 numericBox を Enabled=true に戻す
    //    NumericBox[] all = [
    //        numericBox13, numericBox23,
    //        numericBox14, numericBox24, numericBox34,
    //        numericBox15, numericBox25, numericBox35, numericBox45,
    //        numericBox16, numericBox26, numericBox36, numericBox46, numericBox56,
    //        numericBox22, numericBox33, numericBox55, numericBox66 ];
    //    foreach (var nb in all) nb.Enabled = true;
    //
    //    // 結晶系ごとに不要な要素を Enabled=false にしつつ 0 を入れる
    //    void Disable(params NumericBox[] boxes)
    //    {
    //        foreach (var b in boxes) { b.Enabled = false; b.Value = 0; }
    //    }
    //
    //    switch (sym.CrystalSystemStr)
    //    {
    //        case "monoclinic":
    //            switch (sym.MainAxis)
    //            {
    //                case "a":
    //                    Disable(numericBox15, numericBox25, numericBox35,
    //                            numericBox16, numericBox26, numericBox36,
    //                            numericBox45, numericBox46);
    //                    break;
    //                case "b":
    //                    Disable(numericBox14, numericBox24, numericBox34,
    //                            numericBox16, numericBox26, numericBox36,
    //                            numericBox45, numericBox56);
    //                    break;
    //                case "c":
    //                    Disable(numericBox14, numericBox24, numericBox34,
    //                            numericBox15, numericBox25, numericBox35,
    //                            numericBox46, numericBox56);
    //                    break;
    //            }
    //            break;
    //
    //        case "orthorhombic":
    //            Disable(numericBox14, numericBox24, numericBox34,
    //                    numericBox15, numericBox25, numericBox35, numericBox45,
    //                    numericBox16, numericBox26, numericBox36, numericBox46, numericBox56);
    //            break;
    //
    //        case "tetragonal":
    //            if (sym.PointGroupHMStr is "4" or "-4" or "4/m")
    //            {
    //                // 16 は Enabled のまま (値が 26 = -16 の制約源として使われる)
    //                // 22/23/55/26 は disable してゼロ化しても直後の ValueChanged 側で再充填される
    //                Disable(numericBox22, numericBox23, numericBox55, numericBox26,
    //                        numericBox14, numericBox24, numericBox34,
    //                        numericBox15, numericBox25, numericBox35, numericBox45,
    //                        numericBox36, numericBox46, numericBox56);
    //            }
    //            else
    //            {
    //                Disable(numericBox22, numericBox23, numericBox14, numericBox24, numericBox34,
    //                        numericBox15, numericBox25, numericBox35, numericBox45, numericBox55,
    //                        numericBox16, numericBox26, numericBox36, numericBox46, numericBox56);
    //            }
    //            break;
    //
    //        case "trigonal":
    //            if (sym.PointGroupHMStr is "3" or "-3")
    //            {
    //                Disable(numericBox22, numericBox23, numericBox24, numericBox34,
    //                        numericBox25, numericBox35, numericBox45, numericBox55,
    //                        numericBox16, numericBox26, numericBox36, numericBox46, numericBox56,
    //                        numericBox66);
    //            }
    //            else
    //            {
    //                Disable(numericBox22, numericBox23, numericBox24, numericBox34,
    //                        numericBox15, numericBox25, numericBox35, numericBox45, numericBox55,
    //                        numericBox16, numericBox26, numericBox36, numericBox46, numericBox56,
    //                        numericBox66);
    //            }
    //            break;
    //
    //        case "hexagonal":
    //            Disable(numericBox22, numericBox23, numericBox14, numericBox24, numericBox34,
    //                    numericBox15, numericBox25, numericBox35, numericBox45,
    //                    numericBox16, numericBox26, numericBox36, numericBox46, numericBox56,
    //                    numericBox55, numericBox66);
    //            break;
    //
    //        case "cubic":
    //            Disable(numericBox13, numericBox23, numericBox14, numericBox24, numericBox34,
    //                    numericBox15, numericBox25, numericBox35, numericBox45,
    //                    numericBox16, numericBox26, numericBox36, numericBox46, numericBox56,
    //                    numericBox22, numericBox33, numericBox55, numericBox66);
    //            break;
    //    }
    //    numericBoxElasticiry_ValueChanged(numericBox11, EventArgs.Empty);
    //}
    #endregion

    /// <summary>261105Cl 変更 (旧シグネチャ: private void SetElasticity()): 結晶系 (Laue 群) の拘束を入力欄に反映する。
    /// 拘束の式は <see cref="Elasticity.ApplySymmetryConstraint"/> (ReciPro の直交座標・コンプライアンスの係数・三方晶の 2 回軸の向き・菱面体の軸は拘束なし)。
    /// changeValues = false (結晶を読み込んだとき・結晶系を変えたとき) は値を変えず、入力欄の可否と注記だけを更新する。
    /// true (利用者が値を編集したとき) は 0 になる成分と他の成分から決まる成分を書き換える。行列の計算と ValueChanged は呼び出し側が行う。</summary>
    private void SetElasticity(bool changeValues = true)
    {
        var constraint = Constraint;
        var current = new double[6, 6];
        for (int i = 0; i < 6; i++)
            for (int j = i; j < 6; j++)
                current[i, j] = Boxes[i, j].Value;
        var (constrained, isFixed) = Elasticity.ApplySymmetryConstraint(current, constraint, Mode);

        skip = true;
        double max = 0, diff = 0;
        for (int i = 0; i < 6; i++)
            for (int j = i; j < 6; j++)
            {
                Boxes[i, j].Enabled = !isFixed[i, j];
                if (changeValues && isFixed[i, j])
                    Boxes[i, j].Value = constrained[i, j];
                max = Math.Max(max, Math.Abs(current[i, j]));
                diff = Math.Max(diff, Math.Abs(Boxes[i, j].Value - constrained[i, j]));
            }
        skip = false;
        // 数値欄の丸め・計算の誤差は許す (最大の成分の 1e-6)
        SatisfiesConstraint = diff <= max * 1e-6;

        var laue = constraint switch
        {
            Elasticity.SymmetryConstraint.MonoclinicX => "2/m (2 ∥ x)",
            Elasticity.SymmetryConstraint.MonoclinicY => "2/m (2 ∥ y)",
            Elasticity.SymmetryConstraint.MonoclinicZ => "2/m (2 ∥ z)",
            Elasticity.SymmetryConstraint.Orthorhombic => "mmm",
            Elasticity.SymmetryConstraint.Tetragonal4m => "4/m",
            Elasticity.SymmetryConstraint.Tetragonal4mmm => "4/mmm",
            Elasticity.SymmetryConstraint.Trigonal3 => "-3",
            Elasticity.SymmetryConstraint.Trigonal3m2x => "-3m (2 ∥ x)",
            Elasticity.SymmetryConstraint.Trigonal3m2y => "-3m (2 ∥ y)",
            Elasticity.SymmetryConstraint.Hexagonal => "6/m, 6/mmm",
            Elasticity.SymmetryConstraint.Cubic => "m-3, m-3m",
            Elasticity.SymmetryConstraint.Rhombohedral => Loc("none (rhombohedral axes)", "なし (菱面体の軸)", "keine (rhomboedrische Achsen)", "aucune (axes rhomboédriques)",
                "ninguna (ejes romboédricos)", "nenhuma (eixos romboédricos)", "nessuno (assi romboedrici)", "нет (ромбоэдрические оси)", "无 (菱面体轴)", "無 (菱面體軸)", "없음 (능면체 축)"),
            _ => Loc("none (triclinic)", "なし (三斜晶)", "keine (triklin)", "aucune (triclinique)", "ninguna (triclínico)", "nenhuma (triclínico)", "nessuno (triclino)",
                "нет (триклинная)", "无 (三斜)", "無 (三斜)", "없음 (삼사정계)"),
        };
        labelConstraint.Text = Loc("Constraint: ", "拘束: ", "Bedingung: ", "Contrainte : ", "Restricción: ", "Restrição: ", "Vincolo: ", "Ограничение: ", "约束: ", "約束: ", "제약: ") + laue
            + (SatisfiesConstraint ? "" : "\r\n" + Loc(
                "⚠ The values do not satisfy it. They are kept until you edit one.",
                "⚠ 今の値は拘束を満たしていません。値を編集するまでそのまま保ちます。",
                "⚠ Die Werte erfüllen sie nicht. Sie bleiben, bis Sie einen ändern.",
                "⚠ Les valeurs ne la respectent pas. Conservées jusqu'à une modification.",
                "⚠ Los valores no la cumplen. Se conservan hasta que edite uno.",
                "⚠ Os valores não a satisfazem. São mantidos até editar um.",
                "⚠ I valori non lo rispettano. Restano finché non ne modifichi uno.",
                "⚠ Значения ему не отвечают. Они сохранятся, пока вы их не измените.",
                "⚠ 当前值不满足约束。在编辑某个值之前保持不变。",
                "⚠ 目前的值不滿足約束。在編輯某個值之前保持不變。",
                "⚠ 현재 값이 제약을 만족하지 않습니다. 값을 편집할 때까지 그대로 둡니다."));
    }

    private void numericBoxElasticiry_ValueChanged(object sender, EventArgs e)
    {
        if (skip) return;
        if (!((NumericBox)sender).Enabled) return;
        #region 261105Cl 変更前 (剛性の式をコンプライアンスにも使い、三方晶 32・3m・-3m は向きによらず c14 型、菱面体の軸でも拘束していた)
        //var sym = SymmetryStatic.Symmetries[SymmetrySeriesNumber];
        //switch (sym.CrystalSystemStr)
        //{
        //    case "tetragonal":
        //        numericBox22.Value = numericBox11.Value;
        //        numericBox23.Value = numericBox13.Value;
        //        numericBox55.Value = numericBox44.Value;
        //        if (sym.PointGroupHMStr is "4" or "-4" or "4/m")
        //            numericBox26.Value = -numericBox16.Value;
        //        break;
        //
        //    case "trigonal":
        //        numericBox22.Value = numericBox11.Value;
        //        numericBox23.Value = numericBox13.Value;
        //        numericBox24.Value = -numericBox14.Value;
        //        numericBox56.Value = numericBox14.Value;
        //        numericBox55.Value = numericBox44.Value;
        //        numericBox66.Value = (numericBox11.Value - numericBox12.Value) / 2; // 260426Cl: 元コードは ... - numericBox11 で常に 0 になる typo
        //        if (sym.PointGroupHMStr is "3" or "-3")
        //        {
        //            numericBox25.Value = -numericBox15.Value;
        //            numericBox46.Value = -numericBox15.Value;
        //        }
        //        break;
        //
        //    case "hexagonal":
        //        numericBox22.Value = numericBox11.Value;
        //        numericBox66.Value = (numericBox11.Value - numericBox12.Value) / 2;
        //        numericBox23.Value = numericBox13.Value;
        //        numericBox55.Value = numericBox44.Value;
        //        break;
        //
        //    case "cubic":
        //        numericBox22.Value = numericBox33.Value = numericBox11.Value;
        //        numericBox55.Value = numericBox66.Value = numericBox44.Value;
        //        numericBox13.Value = numericBox23.Value = numericBox12.Value;
        //        break;
        //}
        #endregion
        // 261105Cl 変更: 拘束は Elasticity.ApplySymmetryConstraint に集約し、利用者が値を編集したときにだけ掛ける
        SetElasticity(changeValues: true);

        var mtx = new DenseMatrix(6, 6);
        mtx[0, 0] = numericBox11.Value;
        mtx[0, 1] = mtx[1, 0] = numericBox12.Value;
        mtx[0, 2] = mtx[2, 0] = numericBox13.Value;
        mtx[0, 3] = mtx[3, 0] = numericBox14.Value;
        mtx[0, 4] = mtx[4, 0] = numericBox15.Value;
        mtx[0, 5] = mtx[5, 0] = numericBox16.Value;

        mtx[1, 1] = numericBox22.Value;
        mtx[1, 2] = mtx[2, 1] = numericBox23.Value;
        mtx[1, 3] = mtx[3, 1] = numericBox24.Value;
        mtx[1, 4] = mtx[4, 1] = numericBox25.Value;
        mtx[1, 5] = mtx[5, 1] = numericBox26.Value;

        mtx[2, 2] = numericBox33.Value;
        mtx[2, 3] = mtx[3, 2] = numericBox34.Value;
        mtx[2, 4] = mtx[4, 2] = numericBox35.Value;
        mtx[2, 5] = mtx[5, 2] = numericBox36.Value;

        mtx[3, 3] = numericBox44.Value;
        mtx[3, 4] = mtx[4, 3] = numericBox45.Value;
        mtx[3, 5] = mtx[5, 3] = numericBox46.Value;

        mtx[4, 4] = numericBox55.Value;
        mtx[4, 5] = mtx[5, 4] = numericBox56.Value;

        mtx[5, 5] = numericBox66.Value;

        if (radioButtonStiffness.Checked)
        {
            stiffness = mtx;
            compliance = mtx.TryInverse();
        }
        else
        {
            compliance = mtx;
            stiffness = mtx.TryInverse();
        }
        ValueChanged?.Invoke(this, e);
    }

    private void radioButtonStiffness_CheckedChanged(object sender, EventArgs e)
    {
        if (skipRadio) return; // 261105Cl 追加
        //if (radioButtonStiffness.Checked)
        //    Stiffness = compliance.TryInverse();
        //else
        //    Compliance = stiffness.TryInverse(); // 261105Cl 変更前
        // 261105Cl 変更: 逆行列が無い (全部 0、または独立な定数が欠けている) と、旧は何もせず、表示中の数値をもう一方の定数として読み替えていた。
        // そのときは 0 の行列にする (0 でない値を消すときは知らせる)
        var source = radioButtonStiffness.Checked ? compliance : stiffness;
        var inverse = source?.TryInverse();
        if (inverse == null)
        {
            if (source != null && source.Enumerate().Any(v => v != 0))
                MessageBox.Show(Loc(
                    "The matrix cannot be inverted (some independent constants are 0 or missing), so it cannot be converted. The constants are cleared.",
                    "行列の逆行列がない (独立な定数に 0 や未入力がある) ため変換できません。定数を消去します。",
                    "Die Matrix ist nicht invertierbar (einige unabhängige Konstanten sind 0 oder fehlen) und kann nicht umgerechnet werden. Die Konstanten werden gelöscht.",
                    "La matrice n'est pas inversible (des constantes indépendantes sont nulles ou manquantes) ; la conversion est impossible. Les constantes sont effacées.",
                    "La matriz no es invertible (hay constantes independientes nulas o sin introducir), por lo que no se puede convertir. Se borran las constantes.",
                    "A matriz não é invertível (há constantes independentes nulas ou em falta), por isso não pode ser convertida. As constantes são apagadas.",
                    "La matrice non è invertibile (alcune costanti indipendenti sono 0 o mancanti), quindi non può essere convertita. Le costanti vengono cancellate.",
                    "Матрица необратима (некоторые независимые константы равны 0 или не заданы), поэтому её нельзя преобразовать. Константы очищаются.",
                    "矩阵不可逆 (部分独立常数为 0 或未输入)，无法转换。将清除这些常数。",
                    "矩陣不可逆 (部分獨立常數為 0 或未輸入)，無法轉換。將清除這些常數。",
                    "행렬의 역행렬이 없어 (일부 독립 상수가 0이거나 입력되지 않음) 변환할 수 없습니다. 상수를 지웁니다."),
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            inverse = new DenseMatrix(6, 6);
        }
        if (radioButtonStiffness.Checked)
            Stiffness = inverse;
        else
            Compliance = inverse;
        ValueChanged?.Invoke(this, e);
    }
}
