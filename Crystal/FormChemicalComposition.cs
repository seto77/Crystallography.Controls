using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using static Crystallography.Localization; // 261024Cl 追加: コード側多言語化 Loc() (方式②。このフォームは訳文を resx に持たない)
using Mode = Crystallography.CompositionConverter.Mode; // 261025Cl 追加
using RowKind = Crystallography.CompositionConverter.RowKind; // 261025Cl 追加

namespace Crystallography.Controls
{
    /// <summary>化学組成の詳細を表示するフォーム。CrystalControl の右クリックメニューから開く。
    /// CrystalControl が初めて使う時に 1 個だけ生成して使い回し (破棄されていたら作り直す)、閉じても破棄せず非表示に戻す (261105Cl: 説明を実装に合わせた。旧「FormBeamInteraction / FormStrain と同じ方式」= コンストラクタで生成)。
    /// 表には元素ごとの重量%・原子%と、指定した元素の原子数で規格化した原子数を出す (例: O = 24)。261024Cl 追加
    /// <para>261025Cl 拡張: 表示モード (元素 / 酸化物 / ハロゲン化物 / 硫化物) と並び順 (登録順 / 原子番号順) を選べる。
    /// 左の価数の表に 元素 | 単位胞中の原子数 | 価数 (分数可。手入力した値は Crystal.ElementValences に保存) | Σ=0 | 規格化した原子数 と、
    /// 単位胞あたりの全体の電荷を出し、CompositionConverter で形式的に換算する (仕様は作者決定 261025/261026)。</para></summary>
    //public partial class FormChemicalComposition : Form // 261024Cl 変更前
    public partial class FormChemicalComposition : FormBase // 261024Cl 変更: 他の Controls フォームと同じく FormBase を継承 (UiFont / ダークモード補正 / F1 ヘルプ / 再起動・シャットダウン時の閉じキャンセル解除)
    {
        /// <summary>このフォームを持つ CrystalControl。CrystalControl.FormChemicalComposition が初めて使われた時に生成して設定する。261024Cl 追加 (261026Cl 説明を更新)</summary>
        public CrystalControl CrystalControl;

        /// <summary>コードからラジオ・価数欄・規格化の欄を書き換えている間、そのイベントをユーザー操作として扱わない。261024Cl 追加 (261026Cl 説明を更新)</summary>
        private bool skipEvent = false;

        /// <summary>規格化の値をユーザーが入力したか。false の間は、基準元素の「組成式 1 単位あたりの原子数」を自動で入れる
        /// (=初期状態の規格化列は組成式そのものになる)。基準元素が結晶から無くなると false に戻る
        /// (規格化の列の欄をクリックして基準を移した時は、その欄の値を引き継ぐのでそのまま)。261024Cl 追加 (261026Cl 説明を更新)</summary>
        private bool valueEditedByUser = false;

        /// <summary>規格化の基準の元素 (元素記号) と、その原子数の指定値。価数の表の「規格化した原子数」の列で指定する
        /// (以前は上部の元素コンボと数値欄が持っていたが、作者指示で欄を廃止した)。261025Cl 追加</summary>
        private string normalizeBasis = null;
        private double normalizeTarget = double.NaN;

        /// <summary>ユーザーが換算表示 (酸化物・ハロゲン化物・硫化物) を選んでいるか。O を含まない結晶へ移って自動で元素表示になっても消さず、
        /// 使えるモードが戻れば換算表示に戻す。261025Cl 追加</summary>
        private bool wantConverted = false;

        /// <summary>O を含まない結晶で好むモード (ユーザーが最後に選んだハロゲン化物か硫化物。未選択は Elements)。261025Cl 追加</summary>
        private Mode preferredOFree = Mode.Elements;

        /// <summary>いま表示しているモード。261025Cl 追加</summary>
        private Mode currentMode = Mode.Elements;

        /// <summary>価数欄 (原子番号ごと)。元素の顔ぶれが変わった分だけ作る/捨てる (入力中のフォーカスを保つため)。261025Cl 追加</summary>
        private readonly Dictionary<int, NumericBox> valenceBoxes = [];

        /// <summary>価数欄の右の「Σ=0」ボタン (原子番号ごと)。261025Cl 追加</summary>
        private readonly Dictionary<int, Button> balanceButtons = [];

        /// <summary>価数の表の元素ごとの行のコントロール (列の順: 元素, 単位胞中の原子数, 価数, Σ=0, 規格化した原子数)。261025Cl 追加</summary>
        private readonly Dictionary<int, Control[]> valenceRows = [];

        /// <summary>組成の表の見出しの幅 (見出しの文字列ごと。各行を 1 行の見出しとして測った最大)。見出しは文字列・フォント・DPI でしか変わらないので、
        /// 打鍵のたびに測り直さない (測るには見出しの書き換えと WrapMode の切り替えが要り、そのたびに表の配置がやり直しになる)。261026Cl 追加</summary>
        private readonly Dictionary<string, int> headerWidths = [];
        private (int Dpi, System.Drawing.Font Font) headerWidthsKey;

        // 261025Cl 追加: モードで切り替える文言 (LocalizeLabels で現在の UI 言語の訳を入れる)
        //private string elementHeader, componentHeader, totalText, excessText, deficitText, componentFallbackText; // 261026Cl 変更前 (componentFallbackText・fixedValenceTip は使わなくなった)
        //private string elementWeightTip, elementAtomsTip, componentWeightTip, componentAtomsTip, valenceTip, fixedValenceTip, balanceTip, atomsPerCellTip, normalizedBasisTip, normalizedOtherTip;
        private string elementHeader, componentHeader, totalText, excessText, deficitText;
        private string elementWeightTip, elementAtomsTip, componentWeightTip, componentAtomsTip, valenceTip, balanceTip, atomsPerCellTip, normalizedBasisTip, normalizedOtherTip;
        private string componentTip; // 261026Cl 追加: 換算モードの先頭列 (成分) のツールチップ

        public FormChemicalComposition()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime) // 261024Cl 追加 (FormGroupRelations と同じ方式②)
                LocalizeLabels();
        }

        private void FormChemicalComposition_Load(object sender, EventArgs e)
        {
            // CrystalControl は外部から代入される参照 (このフォームの子コントロールでない) ためコードで配線 (FormStrain と同じ)。
            // --capture-form などで単体生成された時は null なので配線しない。261024Cl 追加
            if (CrystalControl != null)
            {
                CrystalControl.CrystalChanged += crystalControl_CrystalChanged;
                // 261105Cl 追加: このフォームだけが破棄された時 (シャットダウンの問い合わせで閉じた後に取り消された等) に、
                // CrystalControl の呼び出しリストに破棄済みのフォームを残さない (CrystalControl.FormChemicalComposition は次に使う時に作り直す)
                var owner = CrystalControl;
                Disposed += (_, _) => owner.CrystalChanged -= crystalControl_CrystalChanged;
            }
        }

        /// <summary>×ボタンで閉じても破棄せず非表示にするだけにする (次に開くときも同じインスタンスを使う)。261024Cl 追加</summary>
        private void FormChemicalComposition_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Visible = false;
        }

        /// <summary>隠れている間の結晶変更は反映していないので、表示された時点で作り直す。261024Cl 追加</summary>
        private void FormChemicalComposition_VisibleChanged(object sender, EventArgs e)
        {
            if (Visible)
                UpdateComposition();
        }

        /// <summary>CrystalControl 側で結晶が変わった時 (表示中のみ) に作り直す。261024Cl 追加</summary>
        private void crystalControl_CrystalChanged(object sender, EventArgs e)
        {
            if (Visible)
                UpdateComposition();
        }

        #region 組成の表示 261024Cl 追加 (261025Cl 拡張)

        /// <summary>結晶の元素 (登録順 = 原子リストに最初に出た順)。Crystal.ElementName / ElementNum から作る。261025Cl 追加</summary>
        private static List<(int Z, string Symbol, double N)> InputOrderElements(Crystal crystal)
        {
            var names = crystal?.ElementName;
            var nums = crystal?.ElementNum;
            if (names == null || nums == null || names.Length == 0 || names.Length != nums.Length)
                return [];
            return [.. names.Select((symbol, i) => (AtomStatic.AtomicNumber(symbol), symbol, nums[i]))];
        }

        /// <summary>表示順 (登録順 or 原子番号の昇順)。組成の表・価数の表に共通。原子リストそのものは並べ替えない。261025Cl 追加</summary>
        private List<(int Z, string Symbol, double N)> DisplayOrder(List<(int Z, string Symbol, double N)> elements)
            => radioButtonAtomicNumberOrder.Checked ? [.. elements.OrderBy(e => e.Z)] : elements;

        /// <summary>モードとラジオの対応。261025Cl 追加</summary>
        private (Mode Mode, RadioButton Radio)[] ModeRadios()
            => [(Mode.Elements, radioButtonElements), (Mode.Oxides, radioButtonOxides), (Mode.Halides, radioButtonHalides), (Mode.Sulfides, radioButtonSulfides)];

        /// <summary>基準元素の「組成式 1 単位あたりの原子数」(規格化の値の既定)。Z が求まらない時は単位胞あたりの数。261024Cl 追加</summary>
        //private static double PerFormulaUnit(Crystal crystal, int index) // 261025Cl 変更前 (並び順を選べるようにしたので、コンボの index ではなく元素記号で引く)
        //    => Math.Round(crystal.ElementNum[index] / (crystal.ChemicalFormulaZ > 0 ? crystal.ChemicalFormulaZ : 1), 6);
        private static double PerFormulaUnit(Crystal crystal, string symbol)
        {
            var i = Array.IndexOf(crystal.ElementName, symbol);
            //return i < 0 ? double.NaN : Math.Round(crystal.ElementNum[i] / (crystal.ChemicalFormulaZ > 0 ? crystal.ChemicalFormulaZ : 1), 6); // 261026Cl 変更前
            // 261026Cl 変更: 丸めない (丸めると倍率に 1e-7 ほどの誤差が乗り、384 が 383.9999 と出る。欄の表示は小数 4 桁なので丸めは要らない)
            return i < 0 ? double.NaN : crystal.ElementNum[i] / (crystal.ChemicalFormulaZ > 0 ? crystal.ChemicalFormulaZ : 1);
        }

        /// <summary>結晶が変わった時: 規格化の基準を確かめ (同じ元素が残っていれば引き継ぐ)、表示モードと価数の表を合わせて、組成の表を更新する。261024Cl 追加</summary>
        private void UpdateComposition()
        {
            var crystal = CrystalControl?.Crystal;
            var elements = InputOrderElements(crystal);
            var display = DisplayOrder(elements);
            var valid = elements.Count > 0;
            var zn = elements.Select(e => (e.Z, e.N)).ToList();

            //var previous = comboBoxNormalizeElement.SelectedItem as string; // 261025Cl 変更前 (上部の元素コンボと数値欄を廃止。基準と値はフィールドに持つ)
            skipEvent = true;
            try
            {
                //comboBoxNormalizeElement.BeginUpdate();
                //comboBoxNormalizeElement.Items.Clear();
                //if (valid)
                //{
                //    comboBoxNormalizeElement.Items.AddRange([.. display.Select(e => (object)e.Symbol)]);
                //    var basis = previous != null && elements.Any(e => e.Symbol == previous) ? previous : null;
                //    if (basis == null)
                //    {
                //        // 基準元素が無くなった (or 初回) → 原子数 > 0 の酸素があれば酸素、無ければ原子数 > 0 の (登録順で) 先頭の元素 (全部 0 なら先頭)。
                //        // 占有率 0 のサイトしか無い元素を基準にすると規格化列が全部空欄になるため。値も自動に戻す
                //        basis = elements.FirstOrDefault(e => e.Symbol == "O" && e.N > 0).Symbol
                //             ?? elements.FirstOrDefault(e => e.N > 0).Symbol
                //             ?? elements[0].Symbol;
                //        valueEditedByUser = false;
                //    }
                //    comboBoxNormalizeElement.SelectedItem = basis;
                //    if (!valueEditedByUser)
                //        numericBoxNormalizeValue.Value = PerFormulaUnit(crystal, basis);
                //}
                //comboBoxNormalizeElement.EndUpdate();
                //comboBoxNormalizeElement.Enabled = numericBoxNormalizeValue.Enabled = valid;
                if (valid)
                {
                    if (normalizeBasis == null || !elements.Any(e => e.Symbol == normalizeBasis))
                    {
                        // 基準元素が無くなった (or 初回) → 原子数 > 0 の酸素があれば酸素、無ければ原子数 > 0 の (登録順で) 先頭の元素 (全部 0 なら先頭)。
                        // 占有率 0 のサイトしか無い元素を基準にすると規格化列が全部空欄になるため。値も自動に戻す
                        normalizeBasis = elements.FirstOrDefault(e => e.Symbol == "O" && e.N > 0).Symbol
                             ?? elements.FirstOrDefault(e => e.N > 0).Symbol
                             ?? elements[0].Symbol;
                        valueEditedByUser = false;
                    }
                    if (!valueEditedByUser)
                        normalizeTarget = PerFormulaUnit(crystal, normalizeBasis);
                }

                // 261025Cl 追加: 表示モード。使えるラジオだけ表示し (作者決定)、ユーザーの希望 (wantConverted / preferredOFree) から実際のモードを決める
                currentMode = Mode.Elements;
                if (valid && wantConverted)
                    currentMode = CompositionConverter.IsAvailable(Mode.Oxides, zn) ? Mode.Oxides
                        : preferredOFree != Mode.Elements && CompositionConverter.IsAvailable(preferredOFree, zn) ? preferredOFree
                        : CompositionConverter.IsAvailable(Mode.Halides, zn) ? Mode.Halides
                        : CompositionConverter.IsAvailable(Mode.Sulfides, zn) ? Mode.Sulfides
                        : Mode.Elements;
                foreach (var (mode, radio) in ModeRadios())
                {
                    radio.Visible = mode == Mode.Elements || (valid && CompositionConverter.IsAvailable(mode, zn));
                    radio.Checked = mode == currentMode;
                }
            }
            finally { skipEvent = false; }

            //UpdateValenceBoxes(crystal, display); // 261105Cl 変更前 (価数欄の値は FillTable が入れるので、結晶は要らなくなった)
            UpdateValenceBoxes(display);
            FillTable();
        }

        // 261026Cl 変更前: 元素ごとに価数を求めていた (表示 1 回に 元素数² 回ほど呼ばれ、硫化物の S の釣り合わせと相互に呼び合っていた)。
        // Valences (全元素を 1 度に。規則は CompositionConverter.ResolveValences) に置き換えた
        ///// <summary>適用する価数 (固定 &gt; 保存した手入力 &gt; Fe なら 2 &gt; イオン散乱因子 &gt; 既定 &gt; 未設定 NaN)。261025Cl 追加</summary>
        //private double AppliedValence(Crystal crystal, int z)
        //{
        //    var saved = double.NaN;
        //    foreach (var ev in crystal?.ElementValences ?? [])
        //        if (ev.Z == z)
        //            saved = ev.Valence;
        //    // 261025Cl 追加 (作者指示): 硫化物モードの S は、手入力が無ければ既定 (−2) を使わず、他の元素の価数から電荷が釣り合う値にする
        //    // (FeS2 なら Fe 2 → S −1 で FeS2 100%。二硫化物などで S−S 結合があっても過不足が出ない)。
        //    // 釣り合う値が求まらない (他に未設定の元素がある) か負にならない時は、従来どおり既定値へ
        //    if (currentMode == Mode.Sulfides && z == 16 && double.IsNaN(saved))
        //    {
        //        var balanced = BalancingValence(crystal, InputOrderElements(crystal), 16);
        //        if (balanced < 0)
        //            return balanced;
        //    }
        //    return CompositionConverter.ResolveValence(currentMode, z, saved, CompositionConverter.IonicValence(crystal, z));
        //}

        /// <summary>全元素の適用する価数 (原子番号 → 価数)。手入力 (Crystal.ElementValences) &gt; Fe なら 2 &gt; イオン散乱因子 &gt; 既定 &gt; 未設定 NaN。
        /// 硫化物モードの S は、手入力が無ければ電荷が釣り合う値 (作者指示 261025)。規則は CompositionConverter.ResolveValences。
        /// 表示 1 回につき 1 回だけ求める。261026Cl 追加 (旧 AppliedValence)</summary>
        private Dictionary<int, double> Valences(Crystal crystal, List<(int Z, string Symbol, double N)> elements)
        {
            var saved = new Dictionary<int, double>();
            foreach (var ev in crystal?.ElementValences ?? [])
                saved[ev.Z] = ev.Valence; // 同じ原子番号が複数あれば後のもの (旧 AppliedValence と同じ)
            return CompositionConverter.ResolveValences(currentMode, [.. elements.Select(e => (e.Z, e.N))],
                z => saved.TryGetValue(z, out var v) ? v : double.NaN, z => CompositionConverter.IonicValence(crystal, z));
        }

        /// <summary>価数の表 (元素 | 単位胞中の原子数 | 価数 | Σ=0 | 規格化した原子数) を表示順に並べる。価数と規格化した原子数は直後の FillTable で入れる
        /// (261105Cl 変更: 以前は価数もここで入れていたが、FillTable が必ず同じ値を入れ直していた)。
        /// 元素ごとの行のコントロールは原子番号で使い回す (入力中のフォーカスを保つため)。261025Cl 追加
        /// <para>261025Cl 変更: 上部の規格化の欄を廃止したので、元素モードでも表を出す (規格化の列で基準を指定するため)。
        /// 元素モードでは価数の列 (見出し・価数・Σ=0・合計の電荷・既定に戻す) だけを隠す。</para></summary>
        //private void UpdateValenceBoxes(Crystal crystal, List<(int Z, string Symbol, double N)> display) // 261105Cl 変更前
        private void UpdateValenceBoxes(List<(int Z, string Symbol, double N)> display)
        {
            var converted = currentMode != Mode.Elements;
            //var valences = Valences(crystal, display); // 261026Cl 追加: 全元素の価数を 1 度に求める // 261105Cl 削除 (価数欄の値は FillTable が入れる)
            //panelValence.Visible = converted; // 261025Cl 変更前
            //if (!converted)
            //    return;
            panelValence.Visible = display.Count > 0;

            // 元素記号として解釈できない名前 (質量も 0) には行を作らない。同じ原子番号は 1 行にまとめる (GroupBy は最初に出た順を保つ)
            var rows = display.Where(e => e.Z > 0).GroupBy(e => e.Z).Select(g => (Z: g.Key, g.First().Symbol, N: g.Sum(e => e.N))).ToList();
            var table = tableLayoutPanelValence;
            table.SuspendLayout();
            skipEvent = true;
            try
            {
                // ToolTip は破棄したコントロールを内部の表に持ち続けるので外す。261026Cl 変更: NumericBox は内部のテキストボックス・ラベルにも
                // 同じ ToolTip を配っている (SetHostToolTip) ので、子まで外す
                void clearToolTips(Control c)
                {
                    toolTip.SetToolTip(c, null);
                    foreach (Control child in c.Controls)
                        clearToolTips(child);
                }
                foreach (var z in valenceRows.Keys.Where(z => !rows.Any(r => r.Z == z)).ToList())
                {
                    foreach (var c in valenceRows[z])
                    {
                        table.Controls.Remove(c);
                        //toolTip.SetToolTip(c, null); // ToolTip は破棄したコントロールを内部の表に持ち続けるので外す // 261026Cl 変更前
                        clearToolTips(c);
                        c.Dispose();
                    }
                    valenceRows.Remove(z);
                    valenceBoxes.Remove(z);
                    balanceButtons.Remove(z);
                }

                // 行 = 見出し + 元素ごと + 合計 (Σ | 単位胞中の原子数の総和 | 合計の電荷) + 「既定に戻す」。どの行も内容の高さ
                table.RowStyles.Clear();
                for (int i = 0; i < rows.Count + 3; i++)
                    table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                table.RowCount = rows.Count + 3;
                //foreach (var c in new Control[] { labelValenceTotal, labelValenceAtomsTotal, numericBoxTotalValence }) // 261026Cl 変更前
                foreach (var c in new Control[] { labelValenceTotal, labelValenceAtomsTotal, labelTotalValence }) // 261026Cl 変更: 合計の電荷は Label (作者指示)
                    table.SetRow(c, rows.Count + 1);
                table.SetRow(buttonResetValences, rows.Count + 2);
                labelValenceAtomsTotal.Text = rows.Sum(r => r.N).ToString("0.####", CultureInfo.CurrentCulture);
                //labelValence.Visible = numericBoxTotalValence.Visible = buttonResetValences.Visible = converted; // 261025Cl 追加: 元素モードでは価数の列を隠す // 261026Cl 変更前
                labelValence.Visible = labelTotalValence.Visible = buttonResetValences.Visible = converted; // 261025Cl 追加: 元素モードでは価数の列を隠す (261026Cl: 合計の電荷は Label)

                for (int i = 0; i < rows.Count; i++)
                {
                    var (z, symbol, n) = rows[i];
                    if (!valenceRows.TryGetValue(z, out var cells))
                    {
                        var box = new NumericBox
                        {
                            Anchor = AnchorStyles.Right, // 列の右端に寄せて右隣の「Σ=0」と接するようにする (作者指示)。上下は指定しない = セルの中で縦中央
                            //Margin = new Padding(3, 3, 0, 3), // 261026Cl 変更前
                            Margin = new Padding(3, 1, 0, 1), // 261026Cl 変更: 行間を詰める (作者指示)
                            ShowFraction = true,     // 8/3 のような分数で表示・入力する (作者決定)
                            DecimalPlaces = -1,      // 分数として表示するかの判定を 1e-10 で行う (値と違う分数を見せない)
                            NaNText = "?",           // 未設定
                            ValueBoxWidth = 44,
                            SkipEventDuringInput = false, // 打鍵ごとに確定する。確定を待つと、打ちかけのまま結晶を切り替えた時に別の結晶へ保存される
                            Tag = z,
                        };
                        box.ValueChanged += valenceBox_ValueChanged;
                        var button = new Button // 表示名「Σ=0」は作者決定
                        {
                            Text = "Σ=0",
                            AutoSize = true,
                            AutoSizeMode = AutoSizeMode.GrowAndShrink,
                            Anchor = AnchorStyles.Left,
                            //Margin = new Padding(1, 3, 6, 3), // 左の価数欄に寄せる (作者指示) // 261026Cl 変更前
                            Margin = new Padding(1, 1, 6, 1), // 左の価数欄に寄せる (作者指示)。261026Cl 変更: 行間を詰める (作者指示)
                            Padding = new Padding(0), // 261026Cl 追加: 高さを価数欄に揃えるため余白を除く
                            UseVisualStyleBackColor = true,
                            Tag = z,
                        };
                        button.Click += buttonBalance_Click;
                        toolTip.SetToolTip(button, balanceTip);
                        // 規格化した原子数 (ヘッダー・フッターなし。作者指示)。入力できるのは規格化の基準の元素の欄だけで、
                        // 他の欄はクリックするとその元素が基準になる (FillTable が ReadOnly を切り替える)
                        var normalized = new NumericBox
                        {
                            Anchor = AnchorStyles.Left,
                            DecimalPlaces = 4,
                            TrimEndZero = true,
                            ReadOnly = true,
                            ValueBoxWidth = 60,
                            SkipEventDuringInput = false, // 打鍵ごとに確定
                            Margin = new Padding(3, 1, 3, 1), // 261026Cl 追加: 行間を詰める (作者指示)
                            NaNText = "", // 261105Cl 追加: 倍率が決まらない時 (基準の欄に 0 を打った途中、原子数 0 の元素が基準 等) は空欄 (FillTable の説明どおり。未設定だと「NaN」、ru では「не число」と出ていた)
                            Tag = z,
                        };
                        normalized.ValueChanged += normalizedBox_ValueChanged;
                        normalized.Click2 += normalizedBox_Click2;
                        cells =
                        [
                            new Label { AutoSize = true, Anchor = AnchorStyles.Left },  // 元素 (欄のヘッダーの代わり。作者指示)
                            //new Label { AutoSize = true, Anchor = AnchorStyles.Right }, // 単位胞中の原子数 // 261026Cl 変更前
                            new Label { AutoSize = true, Anchor = AnchorStyles.Left }, // 単位胞中の原子数。261026Cl 変更: 左寄せ (作者指示)
                            box,
                            button,
                            normalized,
                        ];
                        for (int c = 0; c < cells.Length; c++)
                        {
                            table.Controls.Add(cells[c], c, i + 1);
                            UiFont.Apply(cells[c]); // FormBase.OnLoad の一括適用より後に作るので個別に当てる
                        }
                        button.MaximumSize = new System.Drawing.Size(0, box.Height); // 261026Cl 追加: ボタンが価数欄より高くなって行が広がらないように (幅は制限しない)
                        // 261026Cl 追加: 変わらないツールチップは行を作る時に 1 度だけ付ける (以前は表示のたびに付け直していた)。
                        // 規格化の欄だけは基準かどうかで変わるので FillTable が切り替える (初めは基準でない側)
                        toolTip.SetToolTip(cells[1], atomsPerCellTip);
                        box.SetHostToolTip(toolTip, valenceTip);
                        normalized.SetHostToolTip(toolTip, normalizedOtherTip);
                        valenceRows[z] = cells;
                        valenceBoxes[z] = box;
                        balanceButtons[z] = button;
                    }
                    foreach (var c in cells)
                        table.SetRow(c, i + 1);
                    cells[0].Text = symbol;
                    cells[1].Text = n.ToString("0.####", CultureInfo.CurrentCulture);
                    //toolTip.SetToolTip(cells[1], atomsPerCellTip); // 261026Cl 変更前 (行を作る時に 1 度だけ付ける)
                    //var isFixed = !double.IsNaN(CompositionConverter.FixedValence(currentMode, z)); // 261026Cl 削除: 固定の価数は無い (FixedValence は廃止)
                    //valenceBoxes[z].ReadOnly = isFixed;
                    //balanceButtons[z].Visible = !isFixed; // 固定の陰イオンは動かせないのでボタンを出さない // 261025Cl 変更前
                    valenceBoxes[z].Visible = converted; // 261025Cl 追加: 価数の列は換算モードだけ (元素モードでは列ごと縮む)
                    //balanceButtons[z].Visible = converted && !isFixed; // 固定の陰イオンは動かせないのでボタンを出さない (261025Cl 以降、固定の価数は無い) // 261026Cl 変更前
                    balanceButtons[z].Visible = converted;
                    //valenceBoxes[z].Value = AppliedValence(crystal, z); // 261026Cl 変更前
                    //valenceBoxes[z].Value = valences[z]; // 261105Cl 削除 (FillTable が入れる。新しく作った欄もフォーカスが無いので必ず入る)
                    //// 使い回す欄は固定 ⇔ 編集可 が結晶・モードで変わるので、内部のテキストボックスにも配り直す (SetToolTip だけだと古い文言が残る) // 261026Cl 変更前 (固定が無くなり文言は変わらないので、行を作る時に 1 度だけ)
                    //valenceBoxes[z].SetHostToolTip(toolTip, isFixed ? fixedValenceTip : valenceTip);
                }
            }
            finally
            {
                skipEvent = false;
                table.ResumeLayout();
            }
            // 入れ物の幅 = 表 + 縦スクロールバー + 組成の表との間隔。元素が多くて縦にあふれた時にスクロールバーが右端の列に重ならず、横スクロールバーも出ないように
            panelValence.Width = table.GetPreferredSize(System.Drawing.Size.Empty).Width + SystemInformation.GetVerticalScrollBarWidthForDpi(DeviceDpi) + LogicalToDeviceUnits(6);
        }

        /// <summary>元素ごとの重量%・原子%・規格化した原子数 (換算モードでは成分ごとの重量%・原子数と補正・合計・過不足) を表に出す。261024Cl 追加 (261025Cl 拡張)
        /// <para>原子数は Crystal.ElementNum (= 単位胞あたりの 多重度×占有率 の合計) を使う。重量は標準原子量 × 原子数で、
        /// Crystal.GetFormulaAndDensity の密度計算と同じ式。換算の定義は CompositionConverter を参照。</para></summary>
        private void FillTable()
        {
            var crystal = CrystalControl?.Crystal;
            var elements = InputOrderElements(crystal);
            if (elements.Count == 0)
            {
                miniTable1.ClearRows();
                //numericBoxTotalValence.Value = double.NaN; // 261026Cl 変更前
                labelTotalValence.Text = ""; // 261026Cl 変更: 合計の電荷は Label (作者指示)
                return;
            }
            var display = DisplayOrder(elements);

            // 規格化の倍率 = 指定値 / 基準元素の原子数。基準元素の原子数が 0 (占有率 0 等) や指定値が 0 以下の時は空欄にする
            //var basis = comboBoxNormalizeElement.SelectedItem as string; // 261025Cl 変更前 (上部の欄を廃止。基準と値はフィールドに持つ)
            var basis = normalizeBasis;
            var nBasis = elements.FirstOrDefault(e => e.Symbol == basis).N;
            //var target = numericBoxNormalizeValue.Value; // 261025Cl 変更前
            var target = normalizeTarget;
            var scale = basis != null && nBasis > 0 && target > 0 ? target / nBasis : double.NaN;

            //var result = CompositionConverter.Convert(currentMode, [.. display.Select(e => (e.Z, e.N))], z => AppliedValence(crystal, z), scale); // 261026Cl 変更前
            var zn = display.Select(e => (e.Z, e.N)).ToList(); // 261026Cl 追加
            var valences = Valences(crystal, display); // 261026Cl 追加: 全元素の価数を 1 度に求める (換算・電荷・Σ=0 で共用)
            var result = CompositionConverter.Convert(currentMode, zn, z => valences.TryGetValue(z, out var v) ? v : double.NaN, scale);
            var converted = result.Mode != Mode.Elements;
            var baseSymbol = converted ? AtomStatic.AtomicName(result.BaseZ) : null;
            string symbolOf(int z) => display.FirstOrDefault(e => e.Z == z).Symbol ?? AtomStatic.AtomicName(z);
            static object num(double v) => double.IsNaN(v) ? null : v;

            var rows = new List<object[]>();
            foreach (var r in result.Rows)
                switch (r.Kind)
                {
                    case RowKind.Element:
                        rows.Add([symbolOf(r.Z), num(r.WeightPercent), num(r.AtomPercent), num(r.Atoms)]);
                        break;
                    //case RowKind.Component: // 小さい整数比で書けない価数 (2.105 など) は「Fe–O 化合物 (価数 2.105)」のように書く // 261026Cl 変更前
                    //    rows.Add([r.Formula ?? string.Format(componentFallbackText, symbolOf(r.Z), r.Valence.ToString("0.###", CultureInfo.CurrentCulture), baseSymbol),
                    //        num(r.WeightPercent), null, num(r.Atoms)]);
                    //    break;
                    case RowKind.Component: // 261026Cl 変更: 組成式は常に Convert が入れる (小さい整数比で書けない価数は「MnO1.107」のような小数の添字)
                        rows.Add([r.Formula, num(r.WeightPercent), null, num(r.Atoms)]);
                        break;
                    case RowKind.MassOnly or RowKind.Anion:
                        rows.Add([symbolOf(r.Z), num(r.WeightPercent), null, num(r.Atoms)]);
                        break;
                    case RowKind.Unset:
                        rows.Add([symbolOf(r.Z) + " (?)", num(r.WeightPercent), null, num(r.Atoms)]);
                        break;
                    case RowKind.Correction: // 例 −O≡F,Cl (化学記号なので翻訳しない)
                        rows.Add(["−" + baseSymbol + "≡" + string.Join(",", result.CorrectionAnions.Select(symbolOf)), num(r.WeightPercent), null, num(r.Atoms)]);
                        break;
                    case RowKind.Total:
                        rows.Add([totalText + (result.HasUnset ? " (?)" : ""), num(r.WeightPercent), null, null]);
                        break;
                    case RowKind.Residual:
                        // 参考行 (合計に加えない)。価数が未設定の元素があると意味をなさないので出さない。
                        // wt% と原子数の両方が表示桁 (F3) で 0 になる時も出さない (分数の価数で浮動小数の残り 1e-14 が出るため)
                        if (result.HasUnset || (Math.Abs(r.WeightPercent) < 0.0005 && !(Math.Abs(r.Atoms) >= 0.0005)))
                            break;
                        //rows.Add([string.Format(r.WeightPercent > 0 ? excessText : deficitText, baseSymbol), r.WeightPercent, null, num(r.Atoms)]); // 261105Cl 変更前
                        // 261105Cl 変更: 片方だけが表示桁を超えた時に、もう片方 (表示桁で 0) が「-0.000」と出ないように +0 にそろえる
                        rows.Add([string.Format(r.WeightPercent > 0 ? excessText : deficitText, baseSymbol), Math.Abs(r.WeightPercent) < 0.0005 ? 0.0 : r.WeightPercent, null,
                            num(Math.Abs(r.Atoms) < 0.0005 ? 0 : r.Atoms)]);
                        break;
                }
            //miniTable1.SetRows(rows); // 261026Cl 変更前
            // 261026Cl 変更: 行の作り直しで縦スクロールが先頭に戻らないようにする (価数・規格化の欄の打鍵のたびに作り直すので、元素が多い表で位置を見失う)
            var firstRow = miniTable1.FirstDisplayedScrollingRowIndex;
            miniTable1.SetRows(rows);
            if (firstRow > 0 && firstRow < miniTable1.Rows.Count)
                miniTable1.FirstDisplayedScrollingRowIndex = firstRow;

            // 261025Cl 追加: 列の見出しとツールチップをモードに合わせる (換算モードは at% を出さない。作者決定)
            miniTable1.Columns[0].HeaderText = converted ? componentHeader : elementHeader;
            miniTable1.Columns[0].ToolTipText = converted ? componentTip : ""; // 261026Cl 追加: 成分の列の (?)・小数の添字・−B≡X の意味
            miniTable1.Columns[2].Visible = !converted;
            miniTable1.Columns[1].ToolTipText = converted ? componentWeightTip : elementWeightTip;
            miniTable1.Columns[3].ToolTipText = converted ? componentAtomsTip : elementAtomsTip;

            //// 261025Cl 追加: 合計価数 (Σ 価数 × 規格化した原子数)。分数の価数で残る浮動小数の誤差 (1e-14 程度) は 0 にする // 261025Cl 変更前
            //var charge = result.ChargeSum;
            //if (Math.Abs(charge) < 1e-9 * Math.Max(1, display.Sum(e => Math.Abs(e.N * scale))))
            //    charge = 0;
            // 261025Cl 変更 (作者決定): 全体の電荷は単位胞あたり (Σ 単位胞中の原子数 × 価数)。隣の列の単位胞中の原子数と単位をそろえ、規格化には左右されない。
            // 価数が未設定の元素 (原子数 ≠ 0) があれば NaN (= ?)。分数の価数で残る浮動小数の誤差 (1e-14 程度) は 0 にする
            //var charge = 0.0; // 261026Cl 変更前 (同じ和を Convert も求めていた。261026Cl から Convert の ChargeSum が単位胞あたりになったのでそれを使う)
            //foreach (var (z, _, n) in elements)
            //    if (n != 0)
            //        charge += n * AppliedValence(crystal, z);
            var charge = result.ChargeSum;
            if (Math.Abs(charge) < 1e-9 * Math.Max(1, elements.Sum(e => Math.Abs(e.N))))
                charge = 0;
            //numericBoxTotalValence.Value = converted ? charge : double.NaN; // 261026Cl 変更前
            // 261026Cl 変更: 表示専用なので Label (作者指示)。書式は旧 NumericBox と同じ (小数 4 桁まで、末尾の 0 は省く。未設定は ?)
            //labelTotalValence.Text = !converted ? "" : double.IsNaN(charge) ? "?" : charge.ToString("0.####", CultureInfo.CurrentCulture); // 261026Cl 変更前
            var shown = Math.Round(charge, 4); // 261026Cl 変更: 表示桁で 0 になる小さな負の値を「-0」と見せない
            if (shown == 0)
                shown = 0;
            labelTotalValence.Text = !converted ? "" : double.IsNaN(charge) ? "?" : shown.ToString("0.####", CultureInfo.CurrentCulture);

            // 261025Cl 追加: 「Σ=0」ボタンは、この元素の原子数が正で、他の元素の価数がすべて決まっている時だけ押せる
            foreach (var (z, button) in balanceButtons)
                //button.Enabled = converted && double.IsFinite(BalancingValence(crystal, elements, z)); // 261026Cl 変更前
                button.Enabled = converted && double.IsFinite(CompositionConverter.BalancingValence(zn, valences, z)); // 261026Cl 変更: 計算は換算側へ移した

            // 261025Cl 追加: 価数の表の「規格化した原子数」の列。入力できるのは基準の元素の欄だけで、値は指定値 (他の欄は 原子数 × 倍率)
            var basisZ = basis != null ? AtomStatic.AtomicNumber(basis) : 0;
            var skipped = skipEvent;
            skipEvent = true;
            try
            {
                foreach (var (z, cells) in valenceRows)
                {
                    // 261026Cl 追加: 価数欄を最新の値にそろえる。硫化物モードの S (手入力なし) は他の元素の価数から決まるので、
                    // 他の欄を変えると変わる (以前は表だけ変わり、S の欄は古い値のままだった)。入力中の欄は触らない
                    var valenceBox = (NumericBox)cells[2];
                    if (!valenceBox.ContainsFocus && valences.TryGetValue(z, out var valence) && !valenceBox.Value.Equals(valence))
                        valenceBox.Value = valence;
                    var normalized = (NumericBox)cells[4];
                    var isBasis = z == basisZ;
                    //normalized.ReadOnly = !isBasis; // 261026Cl 変更前
                    if (normalized.ReadOnly == isBasis) // 261026Cl 変更: 基準が移った欄だけ、入力の可否とツールチップを切り替える (以前は打鍵のたびに全欄で付け直していた)
                    {
                        normalized.ReadOnly = !isBasis;
                        normalized.SetHostToolTip(toolTip, isBasis ? normalizedBasisTip : normalizedOtherTip);
                    }
                    //normalized.Value = isBasis ? target : elements.Where(e => e.Z == z).Sum(e => e.N) * scale; // 261105Cl 変更前
                    // 261105Cl 変更: 同じ値なら入れない (NumericBox.Value の同値判定は NaN 同士を別の値とみなすので、空欄の欄に打鍵ごと NaN を入れ直していた)
                    var normalizedValue = isBasis ? target : elements.Where(e => e.Z == z).Sum(e => e.N) * scale;
                    if (!normalized.Value.Equals(normalizedValue))
                        normalized.Value = normalizedValue;
                    //normalized.SetHostToolTip(toolTip, isBasis ? normalizedBasisTip : normalizedOtherTip); // 261026Cl 変更前
                }
            }
            finally { skipEvent = skipped; }

            // DataGridView は先頭列の見出しだけ描画時に左の境界線ぶん狭くなるが、推奨幅はその分を含めないため末尾が「…」になる (実測)。
            // 列は自動幅を切ってあるので、推奨幅 + 2px をここで入れる (261026Cl: 先頭列だけでなく全列。フォントは表示時に UiFont で確定する。
            // AllCells のままだと MinimumWidth も効かなかった)
            // その前に基底 DpiAwareDataGridView の初回 DPI 処理 (BeginInvoke 待ち) を済ませる。済む前に入れた物理 px の幅は
            // 96dpi の論理値として記録され、後から dpi/96 倍され直して先頭列が広がる (高 DPI の PerMonitorV2 アプリ = ReciPro の初回表示)
            //var first = miniTable1.Columns[0]; // 261026Cl 変更前 (先頭列だけ幅を入れていた)
            //miniTable1.ApplyDpiScaling();
            //first.Width = first.GetPreferredWidth(DataGridViewAutoSizeColumnMode.AllCells, true) + 2;
            // 261026Cl 変更: 見出しを 2 行にしたので、全列の幅をここで入れる (LocalizeLabels 参照)。幅 = max(セルの内容幅, 見出しの各行の幅) + 2px。
            // 見出しの各行の幅は、その行だけを 1 行の見出し (WrapMode=False) にして測る (+2px は上の「…」対策と同じ)
            //miniTable1.ApplyDpiScaling(); // 261026Cl 変更前 (打鍵のたびに呼んでいた。整数の論理幅との往復で 1px 減り、下で戻す、を毎回繰り返していた)
            var headerStyle = miniTable1.ColumnHeadersDefaultCellStyle;
            //headerStyle.WrapMode = DataGridViewTriState.False; // 261026Cl 変更前 (打鍵のたびに見出しを書き換えて測っていた)
            //foreach (DataGridViewColumn column in miniTable1.Columns)
            //{
            //    if (!column.Visible)
            //        continue;
            //    var header = column.HeaderText;
            //    var width = column.GetPreferredWidth(DataGridViewAutoSizeColumnMode.AllCellsExceptHeader, true);
            //    foreach (var line in header.Split('\n'))
            //    {
            //        column.HeaderText = line;
            //        width = Math.Max(width, column.GetPreferredWidth(DataGridViewAutoSizeColumnMode.ColumnHeader, true));
            //    }
            //    column.HeaderText = header;
            //    column.Width = width + 2;
            //}
            //headerStyle.WrapMode = DataGridViewTriState.True;
            // 261026Cl 変更: 見出しの幅は 文字列・フォント・DPI でしか変わらないので覚えておき、変わった時だけ測る (測るには見出しの書き換えと
            // WrapMode の切り替えが要り、そのたびに表の配置がやり直しになる)。幅は変わった時だけ入れる
            var key = (DeviceDpi, headerStyle.Font ?? miniTable1.Font);
            if (headerWidthsKey != key)
            {
                miniTable1.ApplyDpiScaling(); // 261026Cl 変更: 初回と DPI・フォントが変わった時だけ (上の説明の「初回 DPI 処理」を済ませるため)
                headerWidths.Clear();
                headerWidthsKey = key;
            }
            foreach (DataGridViewColumn column in miniTable1.Columns)
            {
                if (!column.Visible)
                    continue;
                var header = column.HeaderText;
                if (!headerWidths.TryGetValue(header, out var headerWidth))
                {
                    headerStyle.WrapMode = DataGridViewTriState.False;
                    foreach (var line in header.Split('\n'))
                    {
                        column.HeaderText = line;
                        headerWidth = Math.Max(headerWidth, column.GetPreferredWidth(DataGridViewAutoSizeColumnMode.ColumnHeader, true));
                    }
                    column.HeaderText = header;
                    headerStyle.WrapMode = DataGridViewTriState.True;
                    headerWidths[header] = headerWidth;
                }
                var width = Math.Max(headerWidth, column.GetPreferredWidth(DataGridViewAutoSizeColumnMode.AllCellsExceptHeader, true)) + 2;
                if (column.Width != width)
                    column.Width = width;
            }

            // 261026Cl 追加: 内容がはみ出す時だけフォームの幅を広げる (縮めはしない。ユーザーが広げた幅も保つ)。
            // 既定の幅 560 は en/ja/zh/ko 向け。de/fr/es/pt/it/ru は見出しが長く、そのままだと右の表が切れる (実測で最大 674)
            if (WindowState == FormWindowState.Normal)
            {
                //var vScroll = miniTable1.Controls.OfType<VScrollBar>().Any(v => v.Visible) ? SystemInformation.GetVerticalScrollBarWidthForDpi(DeviceDpi) : 0; // 261026Cl 変更前
                //var right = Math.Max(miniTable1.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).Sum(c => c.Width) + 2 + vScroll, flowLayoutPanelMode.PreferredSize.Width);
                var right = Math.Max(miniTable1.PreferredContentWidth, flowLayoutPanelMode.PreferredSize.Width); // 261026Cl 変更: 表の内容の幅は MiniTable に聞く (DataGridView の内部のスクロールバーを直接見ない)
                var need = Padding.Horizontal + Math.Max((panelValence.Visible ? panelValence.Width : 0) + right, flowLayoutPanel1.PreferredSize.Width);
                if (ClientSize.Width < need)
                    ClientSize = new System.Drawing.Size(need, ClientSize.Height);
            }
        }

        // 261025Cl 削除: 上部の規格化の欄 (元素コンボと数値欄) を廃止したので、そのイベントハンドラも使わない (作者指示)
        //private void comboBoxNormalizeElement_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    if (skipEvent)
        //        return;
        //    // 基準元素を選び直したら、値はその元素の「組成式 1 単位あたりの原子数」に戻す (その後ユーザーが例えば 24 と入力する)
        //    var crystal = CrystalControl?.Crystal;
        //    //var index = comboBoxNormalizeElement.SelectedIndex; // 261025Cl 変更前 (コンボの並びが表示順になったので元素記号で引く)
        //    //if (crystal?.ElementNum != null && index >= 0 && index < crystal.ElementNum.Length)
        //    if (crystal?.ElementName != null && comboBoxNormalizeElement.SelectedItem is string symbol)
        //    {
        //        skipEvent = true;
        //        //try { numericBoxNormalizeValue.Value = PerFormulaUnit(crystal, index); } // 261025Cl 変更前
        //        try { numericBoxNormalizeValue.Value = PerFormulaUnit(crystal, symbol); }
        //        finally { skipEvent = false; }
        //    }
        //    valueEditedByUser = false;
        //    FillTable();
        //}
        //
        //private void numericBoxNormalizeValue_ValueChanged(object sender, EventArgs e)
        //{
        //    if (skipEvent)
        //        return;
        //    valueEditedByUser = true;
        //    FillTable();
        //}

        /// <summary>表示モードのラジオ。ユーザーの希望を覚えてから作り直す。261025Cl 追加</summary>
        private void radioButtonMode_CheckedChanged(object sender, EventArgs e)
        {
            if (skipEvent || sender is not RadioButton { Checked: true } radio)
                return;
            var mode = ModeRadios().First(m => m.Radio == radio).Mode;
            wantConverted = mode != Mode.Elements;
            if (mode is Mode.Halides or Mode.Sulfides)
                preferredOFree = mode;
            UpdateComposition();
        }

        /// <summary>並び順のラジオ。261025Cl 追加</summary>
        private void radioButtonOrder_CheckedChanged(object sender, EventArgs e)
        {
            if (skipEvent || sender is not RadioButton { Checked: true })
                return;
            UpdateComposition();
        }

        /// <summary>価数欄をユーザーが変えたら、その結晶の Crystal.ElementValences に保存する (CrystalChanged は出さない: 回折などの再計算を起こさないため)。261025Cl 追加</summary>
        private void valenceBox_ValueChanged(object sender, EventArgs e)
        {
            //if (skipEvent || sender is not NumericBox { Tag: int z } box || box.ReadOnly) // 261026Cl 変更前 (価数欄は読み取り専用にならなくなった)
            if (skipEvent || sender is not NumericBox { Tag: int z } box)
                return;
            var crystal = CrystalControl?.Crystal;
            if (crystal == null)
                return;
            if (currentMode == Mode.Elements || !InputOrderElements(crystal).Any(el => el.Z == z)) // 261025Cl 追加: 表示中の結晶に無い元素の値は保存しない (念のため)
                return;
            var list = (crystal.ElementValences ?? []).Where(ev => ev.Z != z).ToList();
            if (double.IsFinite(box.Value))
                list.Add(new Crystal.ElementValence(z, box.Value));
            crystal.ElementValences = [.. list.OrderBy(ev => ev.Z)];
            FillTable();
        }

        // 261026Cl 削除: 「Σ=0」の価数の計算は CompositionConverter.BalancingValence へ移した (硫化物モードの S の釣り合わせと共用)
        ///// <summary>「Σ=0」ボタンで入れる価数。他の元素の価数はそのままで、合計価数が 0 になるこの元素の価数 (= −Σ_他 n v / n_この元素)。
        ///// 他の元素 (原子数 &gt; 0) に未設定の価数がある時や、この元素の原子数が 0 以下の時は NaN。261025Cl 追加</summary>
        //private double BalancingValence(Crystal crystal, List<(int Z, string Symbol, double N)> elements, int z)
        //{
        //    var n = elements.Where(e => e.Z == z).Sum(e => e.N);
        //    if (!(n > 0))
        //        return double.NaN;
        //    // 他の元素の Σ 原子数 × 価数 (合計の電荷と同じ数え方。原子数 0 の元素は数えない)。O 自身も対象になるので換算 (Convert) は通さない
        //    double others = 0;
        //    foreach (var (zz, _, nn) in elements)
        //    {
        //        if (zz == z || nn == 0)
        //            continue;
        //        var vv = AppliedValence(crystal, zz);
        //        if (double.IsNaN(vv))
        //            return double.NaN;
        //        others += nn * vv;
        //    }
        //    var v = -others / n;
        //    return v == 0 ? 0 : v; // −0 を「-0」と見せない
        //}

        /// <summary>「Σ=0」ボタン: 全体の電荷が 0 になるように、この元素の価数を入れる。手入力と同じく Crystal.ElementValences に保存する。261025Cl 追加</summary>
        private void buttonBalance_Click(object sender, EventArgs e)
        {
            if (sender is not Button { Tag: int z } || !valenceBoxes.TryGetValue(z, out var box))
                return;
            //var v = BalancingValence(CrystalControl?.Crystal, InputOrderElements(CrystalControl?.Crystal), z); // 261026Cl 変更前
            var elements = InputOrderElements(CrystalControl?.Crystal);
            var v = CompositionConverter.BalancingValence([.. elements.Select(el => (el.Z, el.N))], Valences(CrystalControl?.Crystal, elements), z); // 261026Cl 変更: 計算は換算側へ移した
            if (double.IsFinite(v))
                box.Value = v; // valenceBox_ValueChanged が保存して表を作り直す
        }

        /// <summary>価数の表の「規格化した原子数」の欄 (入力できるのは基準の元素の欄だけ) に入力した時: 規格化の値として使う。261025Cl 追加</summary>
        private void normalizedBox_ValueChanged(object sender, EventArgs e)
        {
            if (skipEvent || sender is not NumericBox { ReadOnly: false } box)
                return;
            //numericBoxNormalizeValue.Value = box.Value; // numericBoxNormalizeValue_ValueChanged が「ユーザーが入力した」と記録して表を作り直す // 261025Cl 変更前 (上部の欄を廃止)
            normalizeTarget = box.Value;
            valueEditedByUser = true;
            FillTable();
        }

        /// <summary>「規格化した原子数」の読み取り専用の欄をクリックした時: その元素を規格化の基準にして、その欄を入力できるようにする (作者指示)。
        /// 値はその欄に出ていた値を引き継ぐので、倍率 (= 他の欄の値) は変わらない。261025Cl 追加</summary>
        private void normalizedBox_Click2(object sender, EventArgs e)
        {
            var control = sender as Control; // Click2 の sender は NumericBox の中のテキストボックス
            while (control != null && control is not NumericBox)
                control = control.Parent;
            if (control is not NumericBox { ReadOnly: true, Tag: int z } normalized || !valenceRows.TryGetValue(z, out var cells))
                return;
            var symbol = cells[0].Text;
            //skipEvent = true; // 261025Cl 変更前 (上部の欄を廃止。基準と値はフィールドに持つ)
            //try
            //{
            //    comboBoxNormalizeElement.SelectedItem = symbol;
            //    if (double.IsFinite(normalized.Value) && normalized.Value > 0)
            //        numericBoxNormalizeValue.Value = normalized.Value; // 「ユーザーが入力した」かどうか (valueEditedByUser) はそのまま
            //    else
            //    {
            //        numericBoxNormalizeValue.Value = PerFormulaUnit(CrystalControl?.Crystal, symbol); // 倍率が決まっていなかった時は組成式 1 単位あたりの数から
            //        valueEditedByUser = false;
            //    }
            //}
            //finally { skipEvent = false; }
            normalizeBasis = symbol;
            if (double.IsFinite(normalized.Value) && normalized.Value > 0)
                normalizeTarget = normalized.Value; // 「ユーザーが入力した」かどうか (valueEditedByUser) はそのまま
            else
            {
                normalizeTarget = PerFormulaUnit(CrystalControl?.Crystal, symbol); // 倍率が決まっていなかった時は組成式 1 単位あたりの数から
                valueEditedByUser = false;
            }
            FillTable();
        }

        /// <summary>「既定に戻す」: この結晶で手入力した価数のうち、いま表示している元素の分を消す (表示していない元素の保存値は残す)。261025Cl 追加 (261026Cl 説明を更新)</summary>
        private void buttonResetValences_Click(object sender, EventArgs e)
        {
            var crystal = CrystalControl?.Crystal;
            if (crystal == null)
                return;
            //var editable = InputOrderElements(crystal).Select(el => el.Z).Where(z => double.IsNaN(CompositionConverter.FixedValence(currentMode, z))).ToHashSet(); // 261026Cl 変更前 (固定の価数は無くなった)
            var editable = InputOrderElements(crystal).Select(el => el.Z).ToHashSet();
            crystal.ElementValences = [.. (crystal.ElementValences ?? []).Where(ev => !editable.Contains(ev.Z))];
            UpdateComposition();
        }

        #endregion

        #region 多言語化 (方式②: コード側 Loc) 261024Cl 追加

        private void LocalizeLabels()
        {
            const DataGridViewContentAlignment L = DataGridViewContentAlignment.MiddleLeft;
            const DataGridViewContentAlignment R = DataGridViewContentAlignment.MiddleRight;

            //Text = Loc(en: "Details of the chemical composition", ja: "化学組成の詳細", de: "Details der chemischen Zusammensetzung", fr: "Détails de la composition chimique", es: "Detalles de la composición química", pt: "Detalhes da composição química", it: "Dettagli della composizione chimica", ru: "Подробности химического состава", zhHans: "化学组成详情", zhHant: "化學組成詳情", ko: "화학 조성 상세"); // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
            Text = Loc(en: "Details of the chemical composition", ja: "化学組成の詳細", de: "Details der chemischen Zusammensetzung", fr: "Détails de la composition chimique", es: "Detalles de la composición química", pt: "Detalhes da composição química", it: "Dettagli della composizione chimica", ru: "Подробный химический состав", zhHans: "化学组成详情", zhHant: "化學組成詳情", ko: "화학 조성 상세");
            //labelNormalize.Text = Loc(en: "Normalize to", ja: "規格化の基準", de: "Normieren auf", fr: "Normaliser à", es: "Normalizar a", pt: "Normalizar para", it: "Normalizza a", ru: "Нормировать на", zhHans: "归一化基准", zhHant: "歸一化基準", ko: "정규화 기준"); // 261025Cl 削除: 上部の規格化の欄は廃止 (作者指示)

            //miniTable1.SetColumns( // 261025Cl 変更前 (先頭列の見出しをモードで切り替えるため、訳を elementHeader に持つ)
            //    new MiniTable.Col(Loc(en: "Element", ...), L),
            elementHeader = Loc(en: "Element", ja: "元素", de: "Element", fr: "Élément", es: "Elemento", pt: "Elemento", it: "Elemento", ru: "Элемент", zhHans: "元素", zhHant: "元素", ko: "원소");
            miniTable1.SetColumns(
                new MiniTable.Col(elementHeader, L),
                new MiniTable.Col(Loc(en: "wt%", ja: "重量%", de: "Gew.-%", fr: "% mass.", es: "% en peso", pt: "% em massa", it: "% in peso", ru: "мас. %", zhHans: "质量%", zhHant: "質量%", ko: "질량%"), R, "F3"),
                //new MiniTable.Col(Loc(en: "at%", ja: "原子数%", de: "Atom-%", fr: "% at.", es: "% atómico", pt: "% atômica", it: "% atomica", ru: "ат. %", zhHans: "原子%", zhHant: "原子%", ko: "원자%"), R, "F3"), // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                new MiniTable.Col(Loc(en: "at%", ja: "原子%", de: "Atom-%", fr: "% at.", es: "% atómico", pt: "% atômica", it: "% atomica", ru: "ат. %", zhHans: "原子%", zhHant: "原子%", ko: "원자%"), R, "F3"),
                //new MiniTable.Col(Loc(en: "Atoms (normalized)", ja: "原子数 (規格化)", de: "Atome (normiert)", fr: "Atomes (normalisés)", es: "Átomos (normalizados)", pt: "Átomos (normalizados)", it: "Atomi (normalizzati)", ru: "Число атомов (нормир.)", zhHans: "原子数 (归一化)", zhHant: "原子數 (歸一化)", ko: "원자 수 (정규화)"), R, "F3")); // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                new MiniTable.Col(Loc(en: "Atoms (normalized)", ja: "原子数 (規格化)", de: "Atome (normiert)", fr: "Atomes (normalisés)", es: "Átomos (normalizados)", pt: "Átomos (normalizados)", it: "Atomi (normalizzati)", ru: "Число атомов (нормир.)", zhHans: "原子数 (归一化)", zhHant: "原子數 (正規化)", ko: "원자 수 (정규화)"), R, "F3"));
            // 数値が短いので AllCells の自動幅だと見出しが折り返す (CJK は 1 文字ごとに折れる)。(261026Cl まで) 見出しは 1 行のまま列幅を見出しに合わせていた
            //miniTable1.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False; // 261026Cl 変更前
            //miniTable1.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.None; // 先頭列の幅は FillTable で決める (見出しの「…」対策)
            // 261026Cl 変更: 見出しは「 (」で改行して 2 行にし、列幅を減らす (作者指示。例「原子数\n(規格化)」)。改行を描くには WrapMode=True が要るが、
            // 自動幅 (AllCells) のままだと折り返してよい見出しが 1 文字ずつ折れる (CJK) ので、全列の自動幅を切り、幅は FillTable で入れる
            miniTable1.Columns[3].HeaderText = miniTable1.Columns[3].HeaderText.Replace(" (", "\n(");
            miniTable1.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            foreach (DataGridViewColumn column in miniTable1.Columns)
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            //miniTable1.Columns[1].ToolTipText = Loc( // 261025Cl 変更前 (モードで切り替えるため elementWeightTip に持つ)
            elementWeightTip = Loc(
                en: "Mass fraction of each element,\ncalculated from standard atomic weights.",
                //ja: "各元素の質量分率。\n標準原子量から計算します。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                ja: "各元素の重量分率。\n標準原子量から計算します。",
                de: "Massenanteil jedes Elements,\nberechnet aus Standard-Atomgewichten.",
                fr: "Fraction massique de chaque élément,\ncalculée à partir des masses atomiques standard.",
                es: "Fracción en masa de cada elemento,\ncalculada a partir de los pesos atómicos estándar.",
                pt: "Fração mássica de cada elemento,\ncalculada a partir dos pesos atômicos padrão.",
                it: "Frazione in massa di ciascun elemento,\ncalcolata dai pesi atomici standard.",
                ru: "Массовая доля каждого элемента,\nрассчитанная по стандартным атомным массам.",
                zhHans: "各元素的质量分数，\n由标准原子量计算。",
                zhHant: "各元素的質量分率，\n由標準原子量計算。",
                ko: "각 원소의 질량 분율.\n표준 원자량으로 계산합니다.");
            //miniTable1.Columns[2].ToolTipText = Loc( // 261026Cl 変更前 (多言語対応: 11 言語にそろえ、訳語を統一。作者指示)
                //en: "Fraction of the number of atoms of each element\n(multiplicity × occupancy, summed over all sites).",
                //ja: "各元素の原子数の割合\n(全サイトの 多重度 × 占有率 の合計から計算)。",
                //de: "Anteil der Atomanzahl jedes Elements\n(Multiplizität × Besetzung, über alle Lagen summiert).",
                //fr: "Fraction du nombre d'atomes de chaque élément\n(multiplicité × occupation, sommée sur tous les sites).",
                //es: "Fracción del número de átomos de cada elemento\n(multiplicidad × ocupación, sumada en todos los sitios).",
                //pt: "Fração do número de átomos de cada elemento\n(multiplicidade × ocupação, somada em todos os sítios).",
                //it: "Frazione del numero di atomi di ciascun elemento\n(molteplicità × occupazione, sommata su tutti i siti).",
                //ru: "Доля числа атомов каждого элемента\n(кратность × заселённость, сумма по всем позициям).",
                //zhHans: "各元素原子数的比例\n(所有位置的 多重度 × 占有率 之和)。",
                //zhHant: "各元素原子數的比例\n(所有位置的 多重度 × 佔有率 之和)。",
                //ko: "각 원소의 원자 수 비율\n(모든 자리의 다중도 × 점유율 합계).");
            miniTable1.Columns[2].ToolTipText = Loc( // 261026Cl 変更: zh-Hans 多重性 (AtomControl の既存訳)
                en: "Fraction of the number of atoms of each element\n(multiplicity × occupancy, summed over all sites).",
                ja: "各元素の原子数の割合\n(全サイトの 多重度 × 占有率 の合計から計算)。",
                de: "Anteil der Atomanzahl jedes Elements\n(Multiplizität × Besetzung, über alle Lagen summiert).",
                fr: "Fraction du nombre d'atomes de chaque élément\n(multiplicité × occupation, sommée sur tous les sites).",
                es: "Fracción del número de átomos de cada elemento\n(multiplicidad × ocupación, sumada en todos los sitios).",
                pt: "Fração do número de átomos de cada elemento\n(multiplicidade × ocupação, somada em todos os sítios).",
                it: "Frazione del numero di atomi di ciascun elemento\n(molteplicità × occupazione, sommata su tutti i siti).",
                ru: "Доля числа атомов каждого элемента\n(кратность × заселённость, сумма по всем позициям).",
                //zhHans: "各元素原子数的比例\n(所有位置的 多重性 × 占有率 之和)。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                zhHans: "各元素原子数所占的比例\n(由所有位置的 多重性 × 占有率 之和计算)。",
                //zhHant: "各元素原子數的比例\n(所有位置的 多重度 × 佔有率 之和)。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                zhHant: "各元素原子數所佔的比例\n(由所有位置的 多重度 × 佔有率 之和計算)。",
                ko: "각 원소의 원자 수 비율\n(모든 자리의 다중도 × 점유율 합계).");
            //miniTable1.Columns[3].ToolTipText = Loc( // 261025Cl 変更前 (モードで切り替えるため elementAtomsTip に持つ)
            //elementAtomsTip = Loc( // 261026Cl 変更前 (ツールチップ監査の反映)
                //en: "Number of atoms of each element, scaled so that\nthe selected element has the specified number.",
                //ja: "選んだ元素の原子数が指定した値になるように\n各元素の原子数を規格化した値。",
                //de: "Atomanzahl jedes Elements, so skaliert, dass\ndas gewählte Element die angegebene Anzahl hat.",
                //fr: "Nombre d'atomes de chaque élément, mis à l'échelle\npour que l'élément choisi ait le nombre indiqué.",
                //es: "Número de átomos de cada elemento, escalado para que\nel elemento elegido tenga el número indicado.",
                //pt: "Número de átomos de cada elemento, escalado para que\no elemento escolhido tenha o número indicado.",
                //it: "Numero di atomi di ciascun elemento, scalato in modo che\nl'elemento scelto abbia il numero indicato.",
                //ru: "Число атомов каждого элемента, масштабированное так,\nчтобы у выбранного элемента было заданное число.",
                ////zhHans: "按所选元素的原子数等于指定值\n对各元素原子数进行归一化后的值。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                //zhHans: "各元素的原子数，经归一化使所选元素的\n原子数等于指定值。",
                ////zhHant: "依所選元素的原子數等於指定值\n對各元素原子數進行歸一化後的值。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                //zhHant: "各元素的原子數，經正規化使所選元素的\n原子數等於指定值。",
                //ko: "선택한 원소의 원자 수가 지정한 값이 되도록\n각 원소의 원자 수를 정규화한 값.");
            elementAtomsTip = Loc( // 261026Cl 変更: 基準は左の表で選ぶ (上部のコンボは廃止)
                en: "Number of atoms of each element, scaled so that the basis element\nhas the number entered in the left table (click a box there to\nchange the basis).",
                ja: "各元素の原子数。基準の元素の原子数が左の表で入れた値に\nなるように規格化します (左の表の欄をクリックすると\n基準の元素を変えられます)。",
                de: "Atomanzahl jedes Elements, so skaliert, dass das Bezugselement die\nin der linken Tabelle eingegebene Anzahl hat (dort ein Feld anklicken,\num den Bezug zu ändern).",
                fr: "Nombre d'atomes de chaque élément, mis à l'échelle pour que l'élément\nde référence ait le nombre saisi dans le tableau de gauche (cliquez sur\nune case de ce tableau pour changer de référence).",
                es: "Número de átomos de cada elemento, escalado para que el elemento de\nreferencia tenga el número introducido en la tabla de la izquierda\n(haga clic en una casilla de esa tabla para cambiar la referencia).",
                pt: "Número de átomos de cada elemento, escalado para que o elemento de\nreferência tenha o número digitado na tabela à esquerda (clique em\numa caixa dessa tabela para mudar a referência).",
                it: "Numero di atomi di ciascun elemento, scalato in modo che l'elemento di\nriferimento abbia il numero inserito nella tabella a sinistra (fare clic\nsu una casella di quella tabella per cambiare il riferimento).",
                ru: "Число атомов каждого элемента, пересчитанное так, чтобы у базового\nэлемента было число, введённое в таблице слева (щёлкните поле\nв этой таблице, чтобы сменить базовый элемент).",
                zhHans: "各元素的原子数，按基准元素的原子数等于左侧表中输入的值\n进行归一化 (单击左侧表中的栏可更换基准元素)。",
                zhHant: "各元素的原子數，依基準元素的原子數等於左側表中輸入的值\n進行正規化 (按一下左側表中的欄可更換基準元素)。",
                ko: "각 원소의 원자 수. 기준 원소의 원자 수가 왼쪽 표에 입력한 값이\n되도록 정규화합니다 (왼쪽 표의 칸을 클릭하면 기준 원소를\n바꿀 수 있습니다).");
            miniTable1.Columns[1].ToolTipText = elementWeightTip; // 261025Cl 追加
            miniTable1.Columns[3].ToolTipText = elementAtomsTip; // 261025Cl 追加

            //toolTip.SetToolTip(comboBoxNormalizeElement, Loc( // 261025Cl 削除: 上部の規格化の欄は廃止 (作者指示)
                //en: "Element used as the basis for normalizing\nthe number of atoms (e.g. O).",
                //ja: "原子数を規格化する基準の元素 (例: O)。",
                //de: "Element, auf das die Atomanzahl\nnormiert wird (z. B. O).",
                //fr: "Élément servant de base à la normalisation\ndu nombre d'atomes (p. ex. O).",
                //es: "Elemento que sirve de base para normalizar\nel número de átomos (p. ej. O).",
                //pt: "Elemento usado como base para normalizar\no número de átomos (ex.: O).",
                //it: "Elemento usato come base per normalizzare\nil numero di atomi (es. O).",
                //ru: "Элемент, на который нормируется\nчисло атомов (например, O).",
                //zhHans: "作为原子数归一化基准的元素 (例: O)。",
                //zhHant: "作為原子數歸一化基準的元素 (例: O)。",
                //ko: "원자 수를 정규화하는 기준 원소 (예: O)."));
            //toolTip.SetToolTip(numericBoxNormalizeValue, Loc( // 261025Cl 削除: 上部の規格化の欄は廃止 (作者指示)
                //en: "Number of atoms of the basis element (e.g. 24 for O = 24).\nThe other elements are scaled by the same factor.\nInitially the number per formula unit.",
                //ja: "基準元素の原子数 (例: O = 24 なら 24)。\n他の元素も同じ倍率で換算します。\n初期値は組成式 1 単位あたりの原子数です。",
                //de: "Atomanzahl des Bezugselements (z. B. 24 für O = 24).\nDie anderen Elemente werden mit demselben Faktor skaliert.\nAnfangs die Anzahl pro Formeleinheit.",
                //fr: "Nombre d'atomes de l'élément de base (p. ex. 24 pour O = 24).\nLes autres éléments sont mis à l'échelle du même facteur.\nInitialement, le nombre par unité formulaire.",
                //es: "Número de átomos del elemento base (p. ej. 24 para O = 24).\nLos demás elementos se escalan por el mismo factor.\nInicialmente, el número por unidad fórmula.",
                //pt: "Número de átomos do elemento base (ex.: 24 para O = 24).\nOs demais elementos são escalados pelo mesmo fator.\nInicialmente, o número por unidade de fórmula.",
                //it: "Numero di atomi dell'elemento di base (es. 24 per O = 24).\nGli altri elementi sono scalati dello stesso fattore.\nInizialmente il numero per unità di formula.",
                //ru: "Число атомов базового элемента (например, 24 для O = 24).\nОстальные элементы масштабируются тем же множителем.\nИсходно — число на формульную единицу.",
                //zhHans: "基准元素的原子数 (例: O = 24 时为 24)。\n其他元素按相同倍率换算。\n初始值为每个化学式单位的原子数。",
                //zhHant: "基準元素的原子數 (例: O = 24 時為 24)。\n其他元素按相同倍率換算。\n初始值為每個化學式單位的原子數。",
                //ko: "기준 원소의 원자 수 (예: O = 24 이면 24).\n다른 원소도 같은 배율로 환산합니다.\n초기값은 화학식 단위당 원자 수입니다."));

            // ---- 261025Cl 追加: 表示モード・並び順・価数・合計価数・換算の行 ----
            radioButtonElements.Text = Loc(en: "Elements", ja: "元素", de: "Elemente", fr: "Éléments", es: "Elementos", pt: "Elementos", it: "Elementi", ru: "Элементы", zhHans: "元素", zhHant: "元素", ko: "원소");
            radioButtonOxides.Text = Loc(en: "Oxides", ja: "酸化物", de: "Oxide", fr: "Oxydes", es: "Óxidos", pt: "Óxidos", it: "Ossidi", ru: "Оксиды", zhHans: "氧化物", zhHant: "氧化物", ko: "산화물");
            radioButtonHalides.Text = Loc(en: "Halides", ja: "ハロゲン化物", de: "Halogenide", fr: "Halogénures", es: "Haluros", pt: "Haletos", it: "Alogenuri", ru: "Галогениды", zhHans: "卤化物", zhHant: "鹵化物", ko: "할로겐화물");
            radioButtonSulfides.Text = Loc(en: "Sulfides", ja: "硫化物", de: "Sulfide", fr: "Sulfures", es: "Sulfuros", pt: "Sulfetos", it: "Solfuri", ru: "Сульфиды", zhHans: "硫化物", zhHant: "硫化物", ko: "황화물");
            //radioButtonInputOrder.Text = Loc(en: "Input order", ja: "登録順", de: "Eingabereihenfolge", fr: "Ordre de saisie", es: "Orden de entrada", pt: "Ordem de entrada", it: "Ordine di inserimento", ru: "Порядок ввода", zhHans: "输入顺序", zhHant: "輸入順序", ko: "입력 순서"); // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
            radioButtonInputOrder.Text = Loc(en: "Input order", ja: "登録順", de: "Eingabereihenfolge", fr: "Ordre de saisie", es: "Orden de entrada", pt: "Ordem de entrada", it: "Ordine di inserimento", ru: "В порядке ввода", zhHans: "输入顺序", zhHant: "輸入順序", ko: "입력 순서");
            //radioButtonAtomicNumberOrder.Text = Loc(en: "Atomic number", ja: "原子番号順", de: "Ordnungszahl", fr: "Numéro atomique", es: "Número atómico", pt: "Número atômico", it: "Numero atomico", ru: "По атомному номеру", zhHans: "原子序数", zhHant: "原子序數", ko: "원자 번호순"); // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
            radioButtonAtomicNumberOrder.Text = Loc(en: "Atomic number", ja: "原子番号順", de: "Ordnungszahl", fr: "Numéro atomique", es: "Número atómico", pt: "Número atômico", it: "Numero atomico", ru: "По атомному номеру", zhHans: "原子序数", zhHant: "原子序", ko: "원자 번호순");
            // 価数の表の見出し (元素 | 単位胞中の原子数 | 価数)
            labelValenceElement.Text = elementHeader;
            //labelValenceAtoms.Text = Loc(en: "Atoms / cell", ja: "単位胞中の原子数", de: "Atome / Zelle", fr: "Atomes / maille", es: "Átomos / celda", pt: "Átomos / célula", it: "Atomi / cella", ru: "Атомов в ячейке", zhHans: "晶胞中原子数", zhHant: "晶胞中原子數", ko: "단위 세포 내 원자 수"); // 261026Cl 変更前
            // 261026Cl 変更: 2 行にする (作者指示 ja「原子数\n/単位胞」、en「Atoms\n/Cell」)。261026Cl 全言語を 2 行にした
            //labelValenceAtoms.Text = Loc(en: "Atoms\n/Cell", ja: "原子数\n/単位胞", de: "Atome / Zelle", fr: "Atomes / maille", es: "Átomos / celda", pt: "Átomos / célula", it: "Atomi / cella", ru: "Атомов в ячейке", zhHans: "晶胞中原子数", zhHant: "晶胞中原子數", ko: "단위 세포 내 원자 수"); // 261026Cl 変更前 (多言語対応: 11 言語にそろえ、訳語を統一。作者指示)
            labelValenceAtoms.Text = Loc( // 261026Cl 変更: 2 行の見出し (作者指示 ja「原子数\n/単位胞」、en「Atoms\n/Cell」) を全言語に
                en: "Atoms\n/Cell",
                ja: "原子数\n/単位胞",
                de: "Atome\n/Zelle",
                fr: "Atomes\n/maille",
                es: "Átomos\n/celda",
                pt: "Átomos\n/célula",
                it: "Atomi\n/cella",
                ru: "Атомов\nна ячейку",
                zhHans: "原子数\n/晶胞",
                zhHant: "原子數\n/晶胞",
                ko: "원자 수\n/단위 격자");
            //labelValence.Text = Loc(en: "Valence", ja: "価数", de: "Wertigkeit", fr: "Valence", es: "Valencia", pt: "Valência", it: "Valenza", ru: "Валентность", zhHans: "价态", zhHant: "價態", ko: "원자가"); // 261026Cl 変更前 (多言語対応: 11 言語にそろえ、訳語を統一。作者指示)
            labelValence.Text = Loc( // 261026Cl 変更: de Valenz・zh 价数/價數 (ChemicalFormulaInputControl の既存訳にそろえる)
                en: "Valence",
                ja: "価数",
                de: "Valenz",
                fr: "Valence",
                es: "Valencia",
                pt: "Valência",
                it: "Valenza",
                ru: "Валентность",
                zhHans: "价数",
                zhHant: "價數",
                ko: "원자가");
            //atomsPerCellTip = Loc( // 261026Cl 変更前 (多言語対応: 11 言語にそろえ、訳語を統一。作者指示)
                //en: "Number of atoms of each element in the unit cell\n(multiplicity × occupancy, summed over all sites).",
                //ja: "単位胞中の各元素の原子数\n(全サイトの 多重度 × 占有率 の合計)。",
                //de: "Atomanzahl jedes Elements in der Elementarzelle\n(Multiplizität × Besetzung, über alle Lagen summiert).",
                //fr: "Nombre d'atomes de chaque élément dans la maille\n(multiplicité × occupation, sommée sur tous les sites).",
                //es: "Número de átomos de cada elemento en la celda unidad\n(multiplicidad × ocupación, sumada en todos los sitios).",
                //pt: "Número de átomos de cada elemento na célula unitária\n(multiplicidade × ocupação, somada em todos os sítios).",
                //it: "Numero di atomi di ciascun elemento nella cella elementare\n(molteplicità × occupazione, sommata su tutti i siti).",
                //ru: "Число атомов каждого элемента в элементарной ячейке\n(кратность × заселённость, сумма по всем позициям).",
                //zhHans: "晶胞中各元素的原子数\n(所有位置的 多重度 × 占有率 之和)。",
                //zhHant: "晶胞中各元素的原子數\n(所有位置的 多重度 × 佔有率 之和)。",
                //ko: "단위 세포 내 각 원소의 원자 수\n(모든 자리의 다중도 × 점유율 합계).");
            atomsPerCellTip = Loc( // 261026Cl 変更: zh-Hans 多重性 (AtomControl の既存訳)・ko 단위 격자 (CrystalControl の既存訳)
                en: "Number of atoms of each element in the unit cell\n(multiplicity × occupancy, summed over all sites).",
                ja: "単位胞中の各元素の原子数\n(全サイトの 多重度 × 占有率 の合計)。",
                de: "Atomanzahl jedes Elements in der Elementarzelle\n(Multiplizität × Besetzung, über alle Lagen summiert).",
                fr: "Nombre d'atomes de chaque élément dans la maille\n(multiplicité × occupation, sommée sur tous les sites).",
                es: "Número de átomos de cada elemento en la celda unidad\n(multiplicidad × ocupación, sumada en todos los sitios).",
                pt: "Número de átomos de cada elemento na célula unitária\n(multiplicidade × ocupação, somada em todos os sítios).",
                it: "Numero di atomi di ciascun elemento nella cella elementare\n(molteplicità × occupazione, sommata su tutti i siti).",
                ru: "Число атомов каждого элемента в элементарной ячейке\n(кратность × заселённость, сумма по всем позициям).",
                zhHans: "晶胞中各元素的原子数\n(所有位置的 多重性 × 占有率 之和)。",
                zhHant: "晶胞中各元素的原子數\n(所有位置的 多重度 × 佔有率 之和)。",
                ko: "단위 격자 내 각 원소의 원자 수\n(모든 자리의 다중도 × 점유율 합계).");
            toolTip.SetToolTip(labelValenceAtoms, atomsPerCellTip);
            //labelValenceNormalized.Text = miniTable1.Columns[3].HeaderText; // 組成の表の「原子数 (規格化)」と同じ見出し // 261026Cl 変更前
            // 261026Cl 変更: 「原子数\n/単位胞」と同じ要領で 2 行にする (作者指示)。組成の表の見出し「原子数 (規格化)」の「 (」で改行するので全言語で使える
            //labelValenceNormalized.Text = miniTable1.Columns[3].HeaderText.Replace(" (", "\n("); // 261026Cl 変更前 (見出しは既に 2 行なので置換は効いていなかった)
            labelValenceNormalized.Text = miniTable1.Columns[3].HeaderText; // 組成の表の見出し (前半で「 (」の位置で 2 行にしたもの) と同じ
            toolTip.SetToolTip(labelValenceNormalized, Loc( // 261026Cl 追加: 規格化の基準はこの列でしか選べないので見出しで説明する
                en: "Number of atoms, normalized so that the basis element has the number\nentered in its box (initially the number per formula unit).\nClick another element's box to make that element the basis.",
                ja: "規格化した原子数。基準の元素の原子数が、その欄に入れた値\n(初期値は組成式 1 単位あたりの数) になるように換算します。\n他の元素の欄をクリックすると、その元素が基準になります。",
                de: "Normierte Atomanzahl: so skaliert, dass das Bezugselement die in\nseinem Feld eingegebene Anzahl hat (anfangs die Anzahl pro\nFormeleinheit). Auf das Feld eines anderen Elements klicken,\num dieses zum Bezug zu machen.",
                fr: "Nombre d'atomes normalisé : mis à l'échelle pour que l'élément de\nréférence ait le nombre saisi dans sa case (au départ, le nombre par\nunité formulaire). Cliquez sur la case d'un autre élément\npour en faire la référence.",
                es: "Número de átomos normalizado: escalado para que el elemento de\nreferencia tenga el número introducido en su casilla (al principio,\nel número por unidad fórmula). Haga clic en la casilla de otro\nelemento para usarlo como referencia.",
                pt: "Número de átomos normalizado: escalado para que o elemento de\nreferência tenha o número digitado em sua caixa (inicialmente,\no número por unidade de fórmula). Clique na caixa de outro\nelemento para usá-lo como referência.",
                it: "Numero di atomi normalizzato: scalato in modo che l'elemento di\nriferimento abbia il numero inserito nella sua casella (inizialmente\nil numero per unità di formula). Fare clic sulla casella di un altro\nelemento per renderlo il riferimento.",
                ru: "Нормированное число атомов: пересчитано так, чтобы у базового\nэлемента было число, введённое в его поле (исходно — число\nна формульную единицу). Щёлкните поле другого элемента,\nчтобы сделать его базовым.",
                zhHans: "归一化后的原子数：按基准元素的原子数等于其栏中输入的值进行换算\n(初始值为每个化学式单位的原子数)。\n单击其他元素的栏可将该元素设为基准。",
                zhHant: "正規化後的原子數：依基準元素的原子數等於其欄中輸入的值進行換算\n(初始值為每個化學式單位的原子數)。\n按一下其他元素的欄可將該元素設為基準。",
                ko: "정규화한 원자 수. 기준 원소의 원자 수가 그 칸에 입력한 값\n(초기값은 화학식 단위당 원자 수)이 되도록 환산합니다.\n다른 원소의 칸을 클릭하면 그 원소가 기준이 됩니다."));
            normalizedBasisTip = Loc(
                en: "Basis of the normalization: the other elements are scaled\nso that this element has the number entered here.",
                //ja: "規格化の基準。ここに入れた原子数になるように、\n他の元素の原子数を換算します。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                ja: "規格化の基準。この元素の原子数がここに入れた値になるように、\n他の元素の原子数を換算します。",
                de: "Bezug der Normierung: Die anderen Elemente werden so skaliert,\ndass dieses Element die hier eingegebene Anzahl hat.",
                fr: "Base de la normalisation : les autres éléments sont mis à l'échelle\npour que cet élément ait le nombre saisi ici.",
                es: "Base de la normalización: los demás elementos se escalan\npara que este elemento tenga el número introducido aquí.",
                pt: "Base da normalização: os demais elementos são escalados\npara que este elemento tenha o número digitado aqui.",
                it: "Base della normalizzazione: gli altri elementi sono scalati\nin modo che questo elemento abbia il numero inserito qui.",
                ru: "База нормировки: остальные элементы масштабируются так,\nчтобы у этого элемента было введённое здесь число.",
                //zhHans: "归一化基准。按此处输入的原子数\n换算其他元素的原子数。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                zhHans: "归一化基准：换算其他元素的原子数，\n使此元素的原子数等于此处输入的值。",
                //zhHant: "歸一化基準。依此處輸入的原子數\n換算其他元素的原子數。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                zhHant: "正規化基準：換算其他元素的原子數，\n使此元素的原子數等於此處輸入的值。",
                //ko: "정규화 기준. 여기에 입력한 원자 수가 되도록\n다른 원소의 원자 수를 환산합니다."); // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                ko: "정규화 기준. 이 원소의 원자 수가 여기에 입력한 값이 되도록\n다른 원소의 원자 수를 환산합니다.");
            normalizedOtherTip = Loc(
                en: "Normalized number of atoms.\nClick to make this element the basis of the normalization.",
                ja: "規格化した原子数。\nクリックすると、この元素を規格化の基準にします。",
                //de: "Normierte Atomzahl.\nKlicken, um dieses Element zum Bezug der Normierung zu machen.", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                de: "Normierte Atomanzahl.\nKlicken, um dieses Element zum Bezug der Normierung zu machen.",
                fr: "Nombre d'atomes normalisé.\nCliquer pour faire de cet élément la base de la normalisation.",
                es: "Número de átomos normalizado.\nHaga clic para usar este elemento como base de la normalización.",
                pt: "Número de átomos normalizado.\nClique para usar este elemento como base da normalização.",
                it: "Numero di atomi normalizzato.\nFare clic per usare questo elemento come base della normalizzazione.",
                ru: "Нормированное число атомов.\nЩёлкните, чтобы сделать этот элемент базой нормировки.",
                zhHans: "归一化后的原子数。\n单击可将此元素设为归一化基准。",
                //zhHant: "歸一化後的原子數。\n按一下可將此元素設為歸一化基準。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                zhHant: "正規化後的原子數。\n按一下可將此元素設為正規化基準。",
                ko: "정규화한 원자 수.\n클릭하면 이 원소를 정규화 기준으로 합니다.");
            //var sumTip = Loc( // 全体の電荷は単位胞あたり (作者決定)。いまは英語だけ (作者指示) // 261026Cl 変更前 (多言語対応: 11 言語にそろえ、訳語を統一。作者指示)
                //en: "Totals per unit cell: number of atoms and total charge\n(sum of valence × atoms in the unit cell).");
            var sumTip = Loc( // 261026Cl 変更: 11 言語 (全体の電荷は単位胞あたり)
                en: "Totals per unit cell: number of atoms and total charge\n(sum of valence × atoms in the unit cell).",
                ja: "単位胞あたりの合計: 原子数の総和と全体の電荷\n(価数 × 単位胞中の原子数 の和)。",
                de: "Summen pro Elementarzelle: Atomanzahl und Gesamtladung\n(Summe aus Valenz × Atomanzahl in der Zelle).",
                //fr: "Totaux par maille : nombre d'atomes et charge totale\n(somme valence × nombre d'atomes dans la maille).", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                fr: "Totaux par maille : nombre d'atomes et charge totale\n(somme des produits valence × nombre d'atomes dans la maille).",
                es: "Totales por celda unidad: número de átomos y carga total\n(suma de valencia × átomos en la celda).",
                pt: "Totais por célula unitária: número de átomos e carga total\n(soma de valência × átomos na célula).",
                it: "Totali per cella elementare: numero di atomi e carica totale\n(somma di valenza × atomi nella cella).",
                ru: "Итоги на элементарную ячейку: число атомов и суммарный заряд\n(сумма произведений валентности на число атомов в ячейке).",
                //zhHans: "每个晶胞的合计: 原子数之和与总电荷\n(价数 × 晶胞中原子数 之和)。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                zhHans: "每个晶胞的合计：原子数之和与总电荷\n(价数 × 晶胞中原子数 之和)。",
                //zhHant: "每個晶胞的合計: 原子數之和與總電荷\n(價數 × 晶胞中原子數 之和)。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                zhHant: "每個晶胞的合計：原子數之和與總電荷\n(價數 × 晶胞中原子數 之和)。",
                ko: "단위 격자당 합계: 원자 수의 총합과 전체 전하\n(원자가 × 단위 격자 내 원자 수의 합).");
            toolTip.SetToolTip(labelValenceTotal, sumTip);
            toolTip.SetToolTip(labelValenceAtomsTotal, sumTip);
            //buttonResetValences.Text = Loc(en: "Default", ja: "既定に戻す", de: "Standard", fr: "Par défaut", es: "Predeterminado", pt: "Padrão", it: "Predefinito", ru: "По умолчанию", zhHans: "恢复默认", zhHant: "恢復預設", ko: "기본값"); // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
            buttonResetValences.Text = Loc(en: "Default", ja: "既定に戻す", de: "Zurücksetzen", fr: "Réinitialiser", es: "Restablecer", pt: "Restaurar padrão", it: "Ripristina predefiniti", ru: "По умолчанию", zhHans: "恢复默认", zhHant: "恢復預設", ko: "기본값 복원");

            //componentHeader = Loc(en: "Component", ja: "成分", de: "Komponente", fr: "Composant", es: "Componente", pt: "Componente", it: "Componente", ru: "Компонент", zhHans: "组分", zhHant: "組分", ko: "성분"); // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
            componentHeader = Loc(en: "Component", ja: "成分", de: "Komponente", fr: "Composant", es: "Componente", pt: "Componente", it: "Componente", ru: "Компонент", zhHans: "组分", zhHant: "成分", ko: "성분");
            totalText = Loc(en: "Total", ja: "合計", de: "Summe", fr: "Total", es: "Total", pt: "Total", it: "Totale", ru: "Сумма", zhHans: "合计", zhHant: "合計", ko: "합계");
            //excessText = Loc(en: "{0} excess (ref.)", ja: "{0} 過剰 (参考)", de: "{0}-Überschuss (Ref.)", fr: "Excès de {0} (réf.)", es: "Exceso de {0} (ref.)", pt: "Excesso de {0} (ref.)", it: "Eccesso di {0} (rif.)", ru: "Избыток {0} (справ.)", zhHans: "{0} 过剩 (参考)", zhHant: "{0} 過剩 (參考)", ko: "{0} 과잉 (참고)"); // 261026Cl 変更前
            //deficitText = Loc(en: "{0} deficit (ref.)", ja: "{0} 不足 (参考)", de: "{0}-Defizit (Ref.)", fr: "Déficit en {0} (réf.)", es: "Déficit de {0} (ref.)", pt: "Déficit de {0} (ref.)", it: "Difetto di {0} (rif.)", ru: "Недостаток {0} (справ.)", zhHans: "{0} 不足 (参考)", zhHant: "{0} 不足 (參考)", ko: "{0} 부족 (참고)"); // 261026Cl 変更前
            // 261026Cl 変更 (訳文レビュー): de/fr の「(Ref.)」「(réf.)」は出典・基準値の意味に読めるので、参考の意の「(Info)」「(indicatif)」に
            excessText = Loc(en: "{0} excess (ref.)", ja: "{0} 過剰 (参考)", de: "{0}-Überschuss (Info)", fr: "Excès en {0} (indicatif)", es: "Exceso de {0} (ref.)", pt: "Excesso de {0} (ref.)", it: "Eccesso di {0} (rif.)", ru: "Избыток {0} (справ.)", zhHans: "{0} 过剩 (参考)", zhHant: "{0} 過剩 (參考)", ko: "{0} 과잉 (참고)");
            deficitText = Loc(en: "{0} deficit (ref.)", ja: "{0} 不足 (参考)", de: "{0}-Defizit (Info)", fr: "Déficit en {0} (indicatif)", es: "Déficit de {0} (ref.)", pt: "Déficit de {0} (ref.)", it: "Difetto di {0} (rif.)", ru: "Недостаток {0} (справ.)", zhHans: "{0} 不足 (参考)", zhHant: "{0} 不足 (參考)", ko: "{0} 부족 (참고)");
            //componentFallbackText = Loc(en: "{0}–{2} compound (valence {1})", ja: "{0}–{2} 化合物 (価数 {1})", de: "{0}–{2}-Verbindung (Wertigkeit {1})", fr: "Composé {0}–{2} (valence {1})", es: "Compuesto {0}–{2} (valencia {1})", pt: "Composto {0}–{2} (valência {1})", it: "Composto {0}–{2} (valenza {1})", ru: "Соединение {0}–{2} (валентность {1})", zhHans: "{0}–{2} 化合物 (价态 {1})", zhHant: "{0}–{2} 化合物 (價態 {1})", ko: "{0}–{2} 화합물 (원자가 {1})"); // 261026Cl 削除: 成分名は常に Convert が入れる (小数の添字) ので使わない

            componentWeightTip = Loc(
                en: "Weight % of each component relative to the crystal mass\n(not renormalized). The total is after the −B≡X correction;\nthe excess/deficit row is for reference and not included.",
                ja: "各成分の重量% (結晶の質量に対する値。再規格化しません)。\n合計は −B≡X の補正後。過不足の行は参考で、合計に含めません。",
                //de: "Gew.-% jeder Komponente bezogen auf die Kristallmasse\n(nicht renormiert). Die Summe gilt nach der −B≡X-Korrektur;\ndie Überschuss-/Defizitzeile dient nur zur Information.", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                de: "Gew.-% jeder Komponente bezogen auf die Kristallmasse\n(nicht renormiert). Die Summe enthält die −B≡X-Korrektur;\ndie Überschuss-/Defizitzeile dient nur zur Information\nund geht nicht in die Summe ein.",
                //fr: "% massique de chaque composant par rapport à la masse du cristal\n(non renormalisé). Le total est après la correction −B≡X ;\nla ligne excès/déficit est indicative et non incluse.", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                fr: "% massique de chaque composant par rapport à la masse du cristal\n(non renormalisé). Le total tient compte de la correction −B≡X ;\nla ligne excès/déficit, indicative, n'entre pas dans le total.",
                //es: "% en peso de cada componente respecto a la masa del cristal\n(sin renormalizar). El total es tras la corrección −B≡X;\nla fila de exceso/déficit es de referencia y no se incluye.", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                es: "% en peso de cada componente respecto a la masa del cristal\n(sin renormalizar). El total se calcula tras la corrección −B≡X;\nla fila de exceso/déficit es de referencia y no entra en el total.",
                //pt: "% em massa de cada componente em relação à massa do cristal\n(sem renormalização). O total é após a correção −B≡X;\na linha de excesso/déficit é de referência e não entra no total.", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                pt: "% em massa de cada componente em relação à massa do cristal\n(sem renormalização). O total é calculado após a correção −B≡X;\na linha de excesso/déficit é de referência e não entra no total.",
                //it: "% in peso di ciascun componente rispetto alla massa del cristallo\n(non rinormalizzato). Il totale è dopo la correzione −B≡X;\nla riga di eccesso/difetto è di riferimento e non è inclusa.", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                it: "% in peso di ciascun componente rispetto alla massa del cristallo\n(senza rinormalizzazione). Il totale include la correzione −B≡X;\nla riga di eccesso/difetto è solo indicativa e non è inclusa.",
                ru: "Мас. % каждого компонента от массы кристалла\n(без перенормировки). Сумма — после поправки −B≡X;\nстрока избытка/недостатка справочная и в сумму не входит.",
                //zhHans: "各组分相对于晶体质量的质量% (不再归一化)。\n合计为 −B≡X 校正后的值；过剩/不足行仅供参考，不计入合计。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                zhHans: "各组分相对于晶体质量的质量% (未重新归一化)。\n合计为 −B≡X 校正后的值；过剩/不足行仅供参考，不计入合计。",
                //zhHant: "各組分相對於晶體質量的質量% (不再歸一化)。\n合計為 −B≡X 校正後的值；過剩/不足列僅供參考，不計入合計。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                zhHant: "各成分相對於晶體質量的質量% (未重新正規化)。\n合計為 −B≡X 校正後的值；過剩/不足列僅供參考，不計入合計。",
                ko: "결정 질량에 대한 각 성분의 질량% (재정규화하지 않음).\n합계는 −B≡X 보정 후 값이며, 과잉/부족 행은 참고용으로\n합계에 넣지 않습니다.");
            componentAtomsTip = Loc(
                en: "Normalized number of atoms of the element in each row.\nCorrection and excess/deficit rows: equivalent atoms\nof the basis anion.",
                ja: "各行の元素の原子数 (規格化)。\n補正と過不足の行は、基準陰イオンの原子数に換算した値。",
                //de: "Normierte Atomzahl des Elements in jeder Zeile.\nKorrektur- und Überschuss-/Defizitzeile: äquivalente\nAtomzahl des Bezugsanions.", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                de: "Normierte Atomanzahl des Elements in jeder Zeile.\nKorrektur- und Überschuss-/Defizitzeile: äquivalente\nAtomanzahl des Bezugsanions.",
                //fr: "Nombre d'atomes normalisé de l'élément de chaque ligne.\nLignes de correction et d'excès/déficit : nombre d'atomes\néquivalent de l'anion de base.", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                fr: "Nombre d'atomes normalisé de l'élément de chaque ligne.\nLignes de correction et d'excès/déficit : nombre d'atomes\néquivalent de l'anion de référence.",
                es: "Número de átomos normalizado del elemento de cada fila.\nFilas de corrección y exceso/déficit: átomos equivalentes\ndel anión de base.",
                pt: "Número de átomos normalizado do elemento de cada linha.\nLinhas de correção e de excesso/déficit: átomos equivalentes\ndo ânion de base.",
                //it: "Numero di atomi normalizzato dell'elemento di ogni riga.\nRighe di correzione e di eccesso/difetto: atomi equivalenti\ndell'anione di base.", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                it: "Numero di atomi normalizzato dell'elemento di ogni riga.\nRighe di correzione e di eccesso/difetto: atomi equivalenti\ndell'anione di riferimento.",
                ru: "Нормированное число атомов элемента в каждой строке.\nСтроки поправки и избытка/недостатка: эквивалентное\nчисло атомов базового аниона.",
                //zhHans: "各行元素的原子数 (归一化)。\n校正行和过剩/不足行为换算成基准阴离子的原子数。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                zhHans: "各行元素的原子数 (归一化)。\n校正行和过剩/不足行的值为换算成基准阴离子的原子数。",
                //zhHant: "各列元素的原子數 (歸一化)。\n校正列與過剩/不足列為換算成基準陰離子的原子數。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                zhHant: "各列元素的原子數 (正規化)。\n校正列與過剩/不足列的值為換算成基準陰離子的原子數。",
                ko: "각 행 원소의 원자 수 (정규화).\n보정 행과 과잉/부족 행은 기준 음이온의 원자 수로\n환산한 값.");
            componentTip = Loc( // 261026Cl 追加: 換算モードの先頭列 (成分) の見方
                en: "Each element with a positive valence as its compound with the basis\nanion (e.g. Fe2O3; decimal subscripts if no small integer ratio fits).\n(?) = valence not set. Elements with zero or negative valence stay as\nelements; −B≡X subtracts the basis anion B equivalent to the anion X\n(e.g. −O≡F).",
                ja: "正の価数の元素は、基準陰イオンとの化合物で表します\n(例: Fe2O3。小さい整数比で書けない時は小数の添字)。\n(?) は価数が未設定。価数が 0 以下の元素は元素のまま。\n−B≡X は、陰イオン X と当量の基準陰イオン B を差し引く補正です\n(例: −O≡F)。",
                de: "Jedes Element mit positiver Valenz als Verbindung mit dem Bezugsanion\n(z. B. Fe2O3; Dezimalindizes, wenn kein kleines ganzzahliges Verhältnis\npasst). (?) = Valenz nicht festgelegt. Elemente mit Valenz null oder\nnegativ bleiben Elemente; −B≡X zieht die dem Anion X äquivalente\nMenge des Bezugsanions B ab (z. B. −O≡F).",
                fr: "Chaque élément de valence positive sous forme de composé avec l'anion\nde référence (p. ex. Fe2O3 ; indices décimaux si aucun petit rapport\nentier ne convient). (?) = valence non définie. Les éléments de valence\nnulle ou négative restent des éléments ; −B≡X retranche l'anion de\nréférence B équivalent à l'anion X (p. ex. −O≡F).",
                es: "Cada elemento con valencia positiva como compuesto con el anión de\nbase (p. ej. Fe2O3; subíndices decimales si no cabe una relación entera\npequeña). (?) = valencia sin definir. Los elementos con valencia cero\no negativa quedan como elementos; −B≡X resta el anión de base B\nequivalente al anión X (p. ej. −O≡F).",
                pt: "Cada elemento com valência positiva como composto com o ânion de\nbase (ex.: Fe2O3; índices decimais se nenhuma razão inteira pequena\nservir). (?) = valência não definida. Elementos com valência zero ou\nnegativa permanecem como elementos; −B≡X subtrai o ânion de base B\nequivalente ao ânion X (ex.: −O≡F).",
                it: "Ogni elemento con valenza positiva come composto con l'anione di\nriferimento (es. Fe2O3; pedici decimali se non c'è un piccolo rapporto\nintero). (?) = valenza non impostata. Gli elementi con valenza nulla o\nnegativa restano elementi; −B≡X sottrae l'anione di riferimento B\nequivalente all'anione X (es. −O≡F).",
                ru: "Каждый элемент с положительной валентностью — в виде соединения\nс базовым анионом (например, Fe2O3; дробные индексы, если нет малого\nцелочисленного отношения). (?) — валентность не задана. Элементы\nс нулевой или отрицательной валентностью остаются элементами;\n−B≡X вычитает базовый анион B, эквивалентный аниону X (например, −O≡F).",
                zhHans: "价数为正的元素以其与基准阴离子的化合物表示\n(例: Fe2O3；无法写成小整数比时用小数下标)。\n(?) 表示价数未设定。价数为 0 或负的元素保持为元素。\n−B≡X 是扣除与阴离子 X 等当量的基准阴离子 B 的校正 (例: −O≡F)。",
                zhHant: "價數為正的元素以其與基準陰離子的化合物表示\n(例: Fe2O3；無法寫成小整數比時用小數下標)。\n(?) 表示價數未設定。價數為 0 或負的元素保持為元素。\n−B≡X 是扣除與陰離子 X 等當量的基準陰離子 B 的校正 (例: −O≡F)。",
                ko: "양의 원자가를 가진 원소는 기준 음이온과의 화합물로 나타냅니다\n(예: Fe2O3. 작은 정수비로 쓸 수 없으면 소수 첨자).\n(?)는 원자가 미설정. 원자가가 0 이하인 원소는 원소 그대로입니다.\n−B≡X는 음이온 X와 당량인 기준 음이온 B를 빼는 보정입니다\n(예: −O≡F).");
            //valenceTip = Loc( // 261026Cl 変更前 (多言語対応: 11 言語にそろえ、訳語を統一。作者指示)
                //en: "Valence (oxidation state) used for the conversion.\nFractions such as 8/3 are accepted. Entered values are saved\nwith the crystal in the crystal list; ? = not set.",
                //ja: "換算に使う価数 (酸化数)。8/3 のような分数も入力できます。\n入力した値は結晶とともに結晶リストへ保存されます。? は未設定。",
                //de: "Wertigkeit (Oxidationsstufe) für die Umrechnung.\nBrüche wie 8/3 sind möglich. Eingaben werden mit dem Kristall\nin der Kristallliste gespeichert; ? = nicht festgelegt.",
                //fr: "Valence (degré d'oxydation) utilisée pour la conversion.\nLes fractions comme 8/3 sont acceptées. Les valeurs saisies sont\nenregistrées avec le cristal dans la liste ; ? = non définie.",
                //es: "Valencia (estado de oxidación) usada en la conversión.\nSe aceptan fracciones como 8/3. Los valores introducidos se guardan\ncon el cristal en la lista de cristales; ? = sin definir.",
                //pt: "Valência (estado de oxidação) usada na conversão.\nFrações como 8/3 são aceitas. Os valores digitados são salvos\ncom o cristal na lista de cristais; ? = não definida.",
                //it: "Valenza (stato di ossidazione) usata per la conversione.\nSono ammesse frazioni come 8/3. I valori inseriti vengono salvati\ncon il cristallo nell'elenco dei cristalli; ? = non impostata.",
                //ru: "Валентность (степень окисления) для пересчёта.\nДопускаются дроби, например 8/3. Введённые значения сохраняются\nвместе с кристаллом в списке кристаллов; ? — не задана.",
                //zhHans: "换算所用的价态 (氧化态)。可输入 8/3 之类的分数。\n输入的值随晶体保存到晶体列表中；? 表示未设定。",
                //zhHant: "換算所用的價態 (氧化態)。可輸入 8/3 之類的分數。\n輸入的值會隨晶體儲存到晶體列表中；? 表示未設定。",
                //ko: "환산에 사용하는 원자가 (산화수). 8/3 같은 분수도 입력할 수\n있습니다. 입력한 값은 결정과 함께 결정 목록에 저장됩니다.\n? 는 미설정.");
            //valenceTip = Loc( // 261026Cl 変更: 保存先の説明を正確に (XML の結晶リスト。CSManager などのデータベースには入らない)・訳語の統一 // 261026Cl 変更前 (ツールチップ監査の反映)
                //en: "Valence (oxidation state) used for the conversion; ? = not set.\nFractions such as 8/3 are accepted. Entered values are kept\nwith the crystal and saved in XML crystal lists.",
                //ja: "換算に使う価数 (酸化数)。? は未設定。\n8/3 のような分数も入力できます。入力した値は結晶に記録され、\n結晶リストを XML で保存すると一緒に保存されます。",
                //de: "Valenz (Oxidationsstufe) für die Umrechnung; ? = nicht festgelegt.\nBrüche wie 8/3 sind möglich. Eingaben bleiben beim Kristall\nund werden in XML-Kristalllisten gespeichert.",
                ////fr: "Valence (degré d'oxydation) utilisée pour la conversion ;\n? = non définie. Les fractions comme 8/3 sont acceptées.\nLes valeurs saisies restent attachées au cristal\net sont enregistrées dans les listes XML de cristaux.", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                //fr: "Valence (état d'oxydation) utilisée pour la conversion ;\n? = non définie. Les fractions telles que 8/3 sont acceptées.\nLes valeurs saisies restent attachées au cristal\net sont enregistrées dans les listes XML de cristaux.",
                //es: "Valencia (estado de oxidación) usada en la conversión;\n? = sin definir. Se aceptan fracciones como 8/3.\nLos valores introducidos se conservan con el cristal\ny se guardan en las listas XML de cristales.",
                ////pt: "Valência (estado de oxidação) usada na conversão;\n? = não definida. Frações como 8/3 são aceitas.\nOs valores digitados ficam com o cristal\ne são salvos nas listas XML de cristais.", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                //pt: "Valência (estado de oxidação) usada na conversão;\n? = não definida. Frações como 8/3 são aceitas.\nOs valores digitados ficam associados ao cristal\ne são salvos nas listas XML de cristais.",
                //it: "Valenza (stato di ossidazione) usata per la conversione;\n? = non impostata. Sono ammesse frazioni come 8/3.\nI valori inseriti restano associati al cristallo\ne vengono salvati negli elenchi XML dei cristalli.",
                //ru: "Валентность (степень окисления) для пересчёта; ? — не задана.\nДопускаются дроби, например 8/3. Введённые значения хранятся\nвместе с кристаллом и сохраняются в XML-списках кристаллов.",
                ////zhHans: "换算所用的价数 (氧化态)；? 表示未设定。\n可输入 8/3 之类的分数。输入的值随晶体记录，\n以 XML 保存晶体列表时一并保存。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                //zhHans: "换算所用的价数 (氧化态)；? 表示未设定。\n可输入 8/3 之类的分数。输入的值会随晶体一起记录，\n以 XML 保存晶体列表时一并保存。",
                //zhHant: "換算所用的價數 (氧化態)；? 表示未設定。\n可輸入 8/3 之類的分數。輸入的值會隨晶體記錄，\n以 XML 儲存晶體清單時一併儲存。",
                ////ko: "환산에 사용하는 원자가 (산화수). ? 는 미설정.\n8/3 같은 분수도 입력할 수 있습니다. 입력한 값은 결정에\n기록되며, 결정 목록을 XML로 저장할 때 함께 저장됩니다."); // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                //ko: "환산에 사용하는 원자가 (산화수). ?는 미설정을 뜻합니다.\n8/3 같은 분수도 입력할 수 있습니다. 입력한 값은 결정에\n기록되며, 결정 목록을 XML로 저장할 때 함께 저장됩니다.");
            valenceTip = Loc( // 261026Cl 変更: 入力しない時の価数の決まり方と、Σ=0 が使えない条件 (使えない時のボタンにはチップが出ない) を足す
                en: "Valence (oxidation state) used for the conversion; ? = not set.\nFractions such as 8/3 are accepted. Unless entered: Fe = 2, otherwise\nthe ion chosen for the X-ray scattering factor, otherwise a common\noxidation state. Entered values are kept with the crystal and saved\nin XML crystal lists. Σ=0 is unavailable while another valence is ?.",
                ja: "換算に使う価数 (酸化数)。? は未設定。8/3 のような分数も入力できます。\n入力しない時は、Fe は 2、それ以外は X 線散乱因子に選んだイオンの価数、\n無ければよく見られる酸化数になります。入力した値は結晶に記録され、\n結晶リストを XML で保存すると一緒に保存されます。\n他の元素の価数に ? があると Σ=0 は使えません。",
                de: "Valenz (Oxidationsstufe) für die Umrechnung; ? = nicht festgelegt.\nBrüche wie 8/3 sind möglich. Ohne Eingabe: Fe = 2, sonst die Ladung\ndes für den Röntgen-Streufaktor gewählten Ions, sonst eine übliche\nOxidationsstufe. Eingaben bleiben beim Kristall und werden in\nXML-Kristalllisten gespeichert. Σ=0 ist nicht verfügbar, solange\neine andere Valenz ? ist.",
                fr: "Valence (état d'oxydation) utilisée pour la conversion ;\n? = non définie. Les fractions telles que 8/3 sont acceptées.\nSans saisie : Fe = 2, sinon la charge de l'ion choisi pour le facteur\nde diffusion des rayons X, sinon un état d'oxydation courant.\nLes valeurs saisies restent attachées au cristal\net sont enregistrées dans les listes XML de cristaux.\nΣ=0 est indisponible si une autre valence vaut ?.",
                es: "Valencia (estado de oxidación) usada en la conversión;\n? = sin definir. Se aceptan fracciones como 8/3.\nSi no se introduce: Fe = 2; si no, la carga del ion elegido para el\nfactor de dispersión de rayos X; si no, un estado de oxidación habitual.\nLos valores introducidos se conservan con el cristal\ny se guardan en las listas XML de cristales.\nΣ=0 no está disponible si otra valencia es ?.",
                pt: "Valência (estado de oxidação) usada na conversão;\n? = não definida. Frações como 8/3 são aceitas.\nSe não for digitada: Fe = 2; senão, a carga do íon escolhido para o\nfator de espalhamento de raios X; senão, um estado de oxidação comum.\nOs valores digitados ficam associados ao cristal\ne são salvos nas listas XML de cristais.\nΣ=0 fica indisponível se outra valência for ?.",
                it: "Valenza (stato di ossidazione) usata per la conversione;\n? = non impostata. Sono ammesse frazioni come 8/3.\nSe non inserita: Fe = 2, altrimenti la carica dello ione scelto per il\nfattore di diffusione dei raggi X, altrimenti uno stato di ossidazione\ncomune. I valori inseriti restano associati al cristallo\ne vengono salvati negli elenchi XML dei cristalli.\nΣ=0 non è disponibile se un'altra valenza è ?.",
                ru: "Валентность (степень окисления) для пересчёта; ? — не задана.\nДопускаются дроби, например 8/3. Если не задана: Fe = 2, иначе\nзаряд иона, выбранного для фактора рассеяния рентгеновских лучей,\nиначе обычная степень окисления. Введённые значения хранятся\nвместе с кристаллом и сохраняются в XML-списках кристаллов.\nΣ=0 недоступна, пока валентность другого элемента не задана (?).",
                zhHans: "换算所用的价数 (氧化态)；? 表示未设定。可输入 8/3 之类的分数。\n未输入时：Fe 为 2，否则为 X 射线散射因子所选离子的价数，\n再否则为常见的氧化态。输入的值会随晶体一起记录，\n以 XML 保存晶体列表时一并保存。\n其他元素中有价数为 ? 时，Σ=0 不可用。",
                zhHant: "換算所用的價數 (氧化態)；? 表示未設定。可輸入 8/3 之類的分數。\n未輸入時：Fe 為 2，否則為 X 射線散射因子所選離子的價數，\n再否則為常見的氧化態。輸入的值會隨晶體記錄，\n以 XML 儲存晶體清單時一併儲存。\n其他元素中有價數為 ? 時，Σ=0 無法使用。",
                ko: "환산에 사용하는 원자가 (산화수). ?는 미설정을 뜻합니다.\n8/3 같은 분수도 입력할 수 있습니다. 입력하지 않으면 Fe는 2,\n그 밖에는 X선 산란 인자로 고른 이온의 원자가, 없으면 흔한\n산화수가 됩니다. 입력한 값은 결정에 기록되며, 결정 목록을 XML로\n저장할 때 함께 저장됩니다. 다른 원소의 원자가가 ?이면 Σ=0을\n사용할 수 없습니다.");
            toolTip.SetToolTip(labelValence, valenceTip); // 261026Cl 追加: 「価数」の見出しも価数欄と同じチップ
            //balanceTip = Loc( // 261026Cl 変更前 (多言語対応: 11 言語にそろえ、訳語を統一。作者指示)
                //en: "Set this element's valence so that the sum Σ becomes 0\n(charge balance), keeping the other valences as they are.\nSaved like a typed value. Unavailable while another valence is ?.",
                //ja: "他の元素の価数はそのままで、合計 Σ が 0 (電荷が釣り合う) になるように\nこの元素の価数を決めます。値は手入力と同じく保存されます。\n他の元素の価数に ? (未設定) があると使えません。",
                //de: "Wertigkeit dieses Elements so setzen, dass die Summe Σ = 0 wird\n(Ladungsausgleich); die übrigen Wertigkeiten bleiben unverändert.\nWird wie eine Eingabe gespeichert. Nicht verfügbar, solange eine\nandere Wertigkeit ? ist.",
                //fr: "Fixe la valence de cet élément pour que la somme Σ soit nulle\n(équilibre des charges), sans changer les autres valences.\nEnregistrée comme une saisie. Indisponible si une autre valence vaut ?.",
                //es: "Ajusta la valencia de este elemento para que la suma Σ sea 0\n(balance de cargas), sin cambiar las demás valencias.\nSe guarda como un valor introducido. No disponible si otra valencia es ?.",
                //pt: "Ajusta a valência deste elemento para que a soma Σ seja 0\n(balanço de cargas), sem alterar as demais valências.\nSalva como um valor digitado. Indisponível se outra valência for ?.",
                //it: "Imposta la valenza di questo elemento in modo che la somma Σ sia 0\n(bilancio delle cariche), senza cambiare le altre valenze.\nSalvata come un valore inserito. Non disponibile se un'altra valenza è ?.",
                //ru: "Подобрать валентность этого элемента так, чтобы сумма Σ стала 0\n(баланс зарядов), не меняя остальные валентности.\nСохраняется как введённое значение. Недоступно, пока другая\nвалентность — ?.",
                //zhHans: "保持其他元素的价态不变，确定此元素的价态，使合计 Σ 为 0 (电荷平衡)。\n该值与手动输入一样会被保存。其他元素的价态为 ? (未设定) 时不可用。",
                //zhHant: "保持其他元素的價態不變，決定此元素的價態，使合計 Σ 為 0 (電荷平衡)。\n此值與手動輸入一樣會被儲存。其他元素的價態為 ? (未設定) 時無法使用。",
                //ko: "다른 원소의 원자가는 그대로 두고, 합계 Σ 가 0 (전하 균형) 이 되도록\n이 원소의 원자가를 정합니다. 값은 직접 입력한 것과 같이 저장됩니다.\n다른 원소의 원자가가 ? (미설정) 이면 사용할 수 없습니다.");
            balanceTip = Loc( // 261026Cl 変更: 「全体の電荷 Σ」(最下行) に合わせた言い方・訳語の統一・折り返し
                en: "Set this element's valence so that the total charge Σ becomes 0,\nkeeping the other valences as they are. Saved like a typed value.\nUnavailable while another valence is ?.",
                ja: "他の元素の価数はそのままで、全体の電荷 Σ が 0 になるように\nこの元素の価数を決めます。値は手入力と同じく保存されます。\n他の元素の価数に ? (未設定) があると使えません。",
                de: "Valenz dieses Elements so setzen, dass die Gesamtladung Σ = 0 wird;\ndie übrigen Valenzen bleiben unverändert. Wird wie eine Eingabe\ngespeichert. Nicht verfügbar, solange eine andere Valenz ? ist.",
                fr: "Fixe la valence de cet élément pour que la charge totale Σ\nsoit nulle, sans changer les autres valences. Enregistrée\ncomme une saisie. Indisponible si une autre valence vaut ?.",
                es: "Ajusta la valencia de este elemento para que la carga total Σ\nsea 0, sin cambiar las demás valencias. Se guarda como un\nvalor introducido. No disponible si otra valencia es ?.",
                pt: "Ajusta a valência deste elemento para que a carga total Σ\nseja 0, sem alterar as demais valências. Salva como um\nvalor digitado. Indisponível se outra valência for ?.",
                it: "Imposta la valenza di questo elemento in modo che la carica\ntotale Σ sia 0, senza cambiare le altre valenze. Salvata come\nun valore inserito. Non disponibile se un'altra valenza è ?.",
                //ru: "Подобрать валентность этого элемента так, чтобы суммарный\nзаряд Σ стал 0, не меняя остальные валентности. Сохраняется\nкак введённое значение. Недоступно, пока другая валентность — ?.", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                ru: "Подобрать валентность этого элемента так, чтобы суммарный\nзаряд Σ стал равен 0, не меняя остальных валентностей.\nСохраняется как значение, введённое вручную.\nНедоступно, пока у другого элемента валентность не задана (?).",
                //zhHans: "保持其他元素的价数不变，确定此元素的价数，\n使总电荷 Σ 为 0。该值与手动输入一样会被保存。\n其他元素的价数为 ? (未设定) 时不可用。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                zhHans: "保持其他元素的价数不变，确定此元素的价数，\n使总电荷 Σ 为 0。该值与手动输入一样会被保存。\n其他元素中有价数为 ? (未设定) 时不可用。",
                //zhHant: "保持其他元素的價數不變，決定此元素的價數，\n使總電荷 Σ 為 0。此值與手動輸入一樣會被儲存。\n其他元素的價數為 ? (未設定) 時無法使用。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                zhHant: "保持其他元素的價數不變，決定此元素的價數，\n使總電荷 Σ 為 0。此值與手動輸入一樣會被儲存。\n其他元素中有價數為 ? (未設定) 時無法使用。",
                //ko: "다른 원소의 원자가는 그대로 두고, 전체 전하 Σ 가 0 이 되도록\n이 원소의 원자가를 정합니다. 값은 직접 입력한 것과 같이\n저장됩니다. 다른 원소의 원자가가 ? (미설정) 이면 사용할 수 없습니다."); // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                ko: "다른 원소의 원자가는 그대로 두고, 전체 전하 Σ가 0이 되도록\n이 원소의 원자가를 정합니다. 값은 직접 입력한 값처럼 저장됩니다.\n다른 원소의 원자가가 ?(미설정)이면 사용할 수 없습니다.");
            //fixedValenceTip = Loc(en: "Fixed in this mode.", ja: "このモードでは固定です。", de: "In diesem Modus fest.", fr: "Fixée dans ce mode.", es: "Fija en este modo.", pt: "Fixa neste modo.", it: "Fissa in questa modalità.", ru: "В этом режиме фиксирована.", zhHans: "此模式下固定。", zhHant: "此模式下固定。", ko: "이 모드에서는 고정입니다."); // 261026Cl 削除: 固定の価数は無くなった

            toolTip.SetToolTip(radioButtonElements, Loc(
                en: "Weight %, atomic % and normalized number of atoms\nof each element.",
                //ja: "各元素の重量%・原子数%・規格化した原子数を表示します。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                ja: "各元素の重量%・原子%・規格化した原子数を表示します。",
                //de: "Gew.-%, Atom-% und normierte Atomzahl\njedes Elements.", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                de: "Gew.-%, Atom-% und normierte Atomanzahl\njedes Elements.",
                fr: "% massique, % atomique et nombre d'atomes normalisé\nde chaque élément.",
                es: "% en peso, % atómico y número de átomos normalizado\nde cada elemento.",
                pt: "% em massa, % atômica e número de átomos normalizado\nde cada elemento.",
                it: "% in peso, % atomica e numero di atomi normalizzato\ndi ciascun elemento.",
                ru: "Мас. %, ат. % и нормированное число атомов\nкаждого элемента.",
                zhHans: "显示各元素的质量%、原子%和归一化原子数。",
                //zhHant: "顯示各元素的質量%、原子%與歸一化原子數。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                zhHant: "顯示各元素的質量%、原子%與正規化後的原子數。",
                ko: "각 원소의 질량%, 원자%, 정규화한 원자 수를 표시합니다."));
            //toolTip.SetToolTip(radioButtonOxides, Loc( // 261026Cl 変更前 (多言語対応: 11 言語にそろえ、訳語を統一。作者指示)
                //en: "Formal conversion to oxides with the valences below (O = −2).\nShown when the crystal contains oxygen.",
                //ja: "下の価数で酸化物に形式的に換算します (O = −2)。\n酸素を含む結晶で表示されます。",
                //de: "Formale Umrechnung in Oxide mit den Wertigkeiten unten\n(O = −2). Wird angezeigt, wenn der Kristall Sauerstoff enthält.",
                //fr: "Conversion formelle en oxydes avec les valences ci-dessous\n(O = −2). Affiché si le cristal contient de l'oxygène.",
                //es: "Conversión formal a óxidos con las valencias de abajo (O = −2).\nSe muestra si el cristal contiene oxígeno.",
                //pt: "Conversão formal em óxidos com as valências abaixo (O = −2).\nMostrado quando o cristal contém oxigênio.",
                //it: "Conversione formale in ossidi con le valenze sotto (O = −2).\nMostrato se il cristallo contiene ossigeno.",
                //ru: "Формальный пересчёт на оксиды с валентностями ниже (O = −2).\nПоказывается, если кристалл содержит кислород.",
                //zhHans: "用下方的价态形式换算为氧化物 (O = −2)。\n晶体含氧时显示。",
                //zhHant: "以下方的價態形式換算為氧化物 (O = −2)。\n晶體含氧時顯示。",
                //ko: "아래 원자가로 산화물로 형식 환산합니다 (O = −2).\n결정에 산소가 있을 때 표시됩니다."));
            toolTip.SetToolTip(radioButtonOxides, Loc( // 261026Cl 変更: O は固定でなく既定 −2 (入力で変えられる)・訳語の統一
                en: "Formal conversion to oxides with the valences in the table\n(O = −2 by default). Shown when the crystal contains oxygen.",
                ja: "表の価数で酸化物に形式的に換算します (O の既定は −2)。\n酸素を含む結晶で表示されます。",
                de: "Formale Umrechnung in Oxide mit den Valenzen der Tabelle\n(O standardmäßig −2). Wird angezeigt, wenn der Kristall\nSauerstoff enthält.",
                fr: "Conversion formelle en oxydes avec les valences du tableau\n(O = −2 par défaut). Affiché si le cristal contient de l'oxygène.",
                es: "Conversión formal a óxidos con las valencias de la tabla\n(O = −2 por defecto). Se muestra si el cristal contiene oxígeno.",
                //pt: "Conversão formal em óxidos com as valências da tabela\n(O = −2 por padrão). Mostrado quando o cristal contém oxigênio.", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                pt: "Conversão formal em óxidos com as valências da tabela\n(O = −2 por padrão). Aparece quando o cristal contém oxigênio.",
                it: "Conversione formale in ossidi con le valenze della tabella\n(O = −2 predefinito). Mostrato se il cristallo contiene ossigeno.",
                ru: "Формальный пересчёт на оксиды с валентностями из таблицы\n(по умолчанию O = −2). Показывается, если кристалл\nсодержит кислород.",
                //zhHans: "用表中的价数形式换算为氧化物 (O 默认为 −2)。\n晶体含氧时显示。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                zhHans: "按表中的价数，形式上换算为氧化物 (O 默认为 −2)。\n晶体含氧时显示。",
                //zhHant: "以表中的價數形式換算為氧化物 (O 預設為 −2)。\n晶體含氧時顯示。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                zhHant: "依表中的價數，形式上換算為氧化物 (O 預設為 −2)。\n晶體含氧時顯示。",
                //ko: "표의 원자가로 산화물로 형식 환산합니다 (O 기본값 −2).\n결정에 산소가 있을 때 표시됩니다.")); // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                ko: "표의 원자가를 사용해 산화물로 형식적으로 환산합니다\n(O의 기본값은 −2). 결정에 산소가 있을 때 표시됩니다."));
            //toolTip.SetToolTip(radioButtonHalides, Loc( // 261026Cl 変更前 (多言語対応: 11 言語にそろえ、訳語を統一。作者指示)
                //en: "Formal conversion to halides (F, Cl, Br, I = −1; S = −2).\nThe halogen with the lowest atomic number is the basis.\nShown when the crystal contains no oxygen.",
                //ja: "ハロゲン化物に形式的に換算します (F, Cl, Br, I = −1、S = −2)。\n原子番号の最も小さいハロゲンを基準にします。\n酸素を含まない結晶で表示されます。",
                //de: "Formale Umrechnung in Halogenide (F, Cl, Br, I = −1; S = −2).\nBezug ist das Halogen mit der kleinsten Ordnungszahl.\nWird angezeigt, wenn der Kristall keinen Sauerstoff enthält.",
                //fr: "Conversion formelle en halogénures (F, Cl, Br, I = −1 ; S = −2).\nL'halogène de plus petit numéro atomique sert de base.\nAffiché si le cristal ne contient pas d'oxygène.",
                //es: "Conversión formal a haluros (F, Cl, Br, I = −1; S = −2).\nLa base es el halógeno de menor número atómico.\nSe muestra si el cristal no contiene oxígeno.",
                //pt: "Conversão formal em haletos (F, Cl, Br, I = −1; S = −2).\nA base é o halogênio de menor número atômico.\nMostrado quando o cristal não contém oxigênio.",
                //it: "Conversione formale in alogenuri (F, Cl, Br, I = −1; S = −2).\nLa base è l'alogeno con il numero atomico più basso.\nMostrato se il cristallo non contiene ossigeno.",
                //ru: "Формальный пересчёт на галогениды (F, Cl, Br, I = −1; S = −2).\nОснова — галоген с наименьшим атомным номером.\nПоказывается, если кристалл не содержит кислорода.",
                //zhHans: "形式换算为卤化物 (F, Cl, Br, I = −1；S = −2)。\n以原子序数最小的卤素为基准。\n晶体不含氧时显示。",
                //zhHant: "形式換算為鹵化物 (F, Cl, Br, I = −1；S = −2)。\n以原子序數最小的鹵素為基準。\n晶體不含氧時顯示。",
                //ko: "할로겐화물로 형식 환산합니다 (F, Cl, Br, I = −1, S = −2).\n원자 번호가 가장 작은 할로겐을 기준으로 합니다.\n결정에 산소가 없을 때 표시됩니다."));
            //toolTip.SetToolTip(radioButtonHalides, Loc( // 261026Cl 変更: ハロゲン・S は固定でなく既定値 // 261026Cl 変更前 (ツールチップ監査の反映)
                //en: "Formal conversion to halides (by default F, Cl, Br, I = −1;\nS = −2). The halogen with the lowest atomic number is the basis.\nShown when the crystal contains no oxygen.",
                //ja: "ハロゲン化物に形式的に換算します (既定は F, Cl, Br, I = −1、S = −2)。\n原子番号の最も小さいハロゲンを基準にします。\n酸素を含まない結晶で表示されます。",
                //de: "Formale Umrechnung in Halogenide (standardmäßig F, Cl, Br, I = −1;\nS = −2). Bezug ist das Halogen mit der kleinsten Ordnungszahl.\nWird angezeigt, wenn der Kristall keinen Sauerstoff enthält.",
                //fr: "Conversion formelle en halogénures (par défaut F, Cl, Br, I = −1 ;\nS = −2). L'halogène de plus petit numéro atomique sert de base.\nAffiché si le cristal ne contient pas d'oxygène.",
                //es: "Conversión formal a haluros (por defecto F, Cl, Br, I = −1;\nS = −2). La base es el halógeno de menor número atómico.\nSe muestra si el cristal no contiene oxígeno.",
                ////pt: "Conversão formal em haletos (por padrão F, Cl, Br, I = −1;\nS = −2). A base é o halogênio de menor número atômico.\nMostrado quando o cristal não contém oxigênio.", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                //pt: "Conversão formal em haletos (por padrão F, Cl, Br, I = −1;\nS = −2). A base é o halogênio de menor número atômico.\nAparece quando o cristal não contém oxigênio.",
                ////it: "Conversione formale in alogenuri (predefinito F, Cl, Br, I = −1;\nS = −2). La base è l'alogeno con il numero atomico più basso.\nMostrato se il cristallo non contiene ossigeno.", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                //it: "Conversione formale in alogenuri (predefinito F, Cl, Br, I = −1;\nS = −2). L'anione di riferimento è l'alogeno con il numero\natomico più basso.\nMostrato se il cristallo non contiene ossigeno.",
                ////ru: "Формальный пересчёт на галогениды (по умолчанию F, Cl, Br, I = −1;\nS = −2). Основа — галоген с наименьшим атомным номером.\nПоказывается, если кристалл не содержит кислорода.", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                //ru: "Формальный пересчёт на галогениды (по умолчанию F, Cl, Br, I = −1;\nS = −2). Базовый анион — галоген с наименьшим атомным номером.\nПоказывается, если кристалл не содержит кислорода.",
                //zhHans: "形式换算为卤化物 (默认 F, Cl, Br, I = −1；S = −2)。\n以原子序数最小的卤素为基准。\n晶体不含氧时显示。",
                ////zhHant: "形式換算為鹵化物 (預設 F, Cl, Br, I = −1；S = −2)。\n以原子序數最小的鹵素為基準。\n晶體不含氧時顯示。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                //zhHant: "形式換算為鹵化物 (預設 F, Cl, Br, I = −1；S = −2)。\n以原子序最小的鹵素為基準。\n晶體不含氧時顯示。",
                ////ko: "할로겐화물로 형식 환산합니다 (기본값 F, Cl, Br, I = −1, S = −2).\n원자 번호가 가장 작은 할로겐을 기준으로 합니다.\n결정에 산소가 없을 때 표시됩니다.")); // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                //ko: "할로겐화물로 형식적으로 환산합니다\n(기본값: F, Cl, Br, I = −1, S = −2).\n원자 번호가 가장 작은 할로겐을 기준으로 합니다.\n결정에 산소가 없을 때 표시됩니다."));
            toolTip.SetToolTip(radioButtonHalides, Loc( // 261026Cl 変更: 表示する条件を正確に (陰イオンを含み、O を含まない)
                en: "Formal conversion to halides (by default F, Cl, Br, I = −1;\nS = −2). The halogen with the lowest atomic number is the basis.\nShown when the crystal contains F, Cl, Br or I\nand no oxygen.",
                ja: "ハロゲン化物に形式的に換算します (既定は F, Cl, Br, I = −1、S = −2)。\n原子番号の最も小さいハロゲンを基準にします。\nF, Cl, Br, I のどれかを含み、酸素を含まない結晶で表示されます。",
                de: "Formale Umrechnung in Halogenide (standardmäßig F, Cl, Br, I = −1;\nS = −2). Bezug ist das Halogen mit der kleinsten Ordnungszahl.\nWird angezeigt, wenn der Kristall F, Cl, Br oder I,\naber keinen Sauerstoff enthält.",
                fr: "Conversion formelle en halogénures (par défaut F, Cl, Br, I = −1 ;\nS = −2). L'halogène de plus petit numéro atomique sert de base.\nAffiché si le cristal contient F, Cl, Br ou I\nmais pas d'oxygène.",
                es: "Conversión formal a haluros (por defecto F, Cl, Br, I = −1;\nS = −2). La base es el halógeno de menor número atómico.\nSe muestra si el cristal contiene F, Cl, Br o I\ny no contiene oxígeno.",
                pt: "Conversão formal em haletos (por padrão F, Cl, Br, I = −1;\nS = −2). A base é o halogênio de menor número atômico.\nAparece quando o cristal contém F, Cl, Br ou I\ne não contém oxigênio.",
                it: "Conversione formale in alogenuri (predefinito F, Cl, Br, I = −1;\nS = −2). L'anione di riferimento è l'alogeno con il numero\natomico più basso.\nMostrato se il cristallo contiene F, Cl, Br o I\nma non ossigeno.",
                ru: "Формальный пересчёт на галогениды (по умолчанию F, Cl, Br, I = −1;\nS = −2). Базовый анион — галоген с наименьшим атомным номером.\nПоказывается, если кристалл содержит F, Cl, Br или I\nи не содержит кислорода.",
                zhHans: "形式换算为卤化物 (默认 F, Cl, Br, I = −1；S = −2)。\n以原子序数最小的卤素为基准。\n晶体含 F, Cl, Br, I 之一且不含氧时显示。",
                zhHant: "形式換算為鹵化物 (預設 F, Cl, Br, I = −1；S = −2)。\n以原子序最小的鹵素為基準。\n晶體含 F, Cl, Br, I 之一且不含氧時顯示。",
                ko: "할로겐화물로 형식적으로 환산합니다\n(기본값: F, Cl, Br, I = −1, S = −2).\n원자 번호가 가장 작은 할로겐을 기준으로 합니다.\n결정에 F, Cl, Br, I 중 하나가 있고 산소가 없을 때 표시됩니다."));
            //toolTip.SetToolTip(radioButtonSulfides, Loc( // 261025Cl 変更前 (S は −2 固定だった)
                //en: "Formal conversion to sulfides (S = −2; F, Cl, Br, I = −1).\nShown when the crystal contains no oxygen.",
                //ja: "硫化物に形式的に換算します (S = −2、F, Cl, Br, I = −1)。\n酸素を含まない結晶で表示されます。",
                //de: "Formale Umrechnung in Sulfide (S = −2; F, Cl, Br, I = −1).\nWird angezeigt, wenn der Kristall keinen Sauerstoff enthält.",
                //fr: "Conversion formelle en sulfures (S = −2 ; F, Cl, Br, I = −1).\nAffiché si le cristal ne contient pas d'oxygène.",
                //es: "Conversión formal a sulfuros (S = −2; F, Cl, Br, I = −1).\nSe muestra si el cristal no contiene oxígeno.",
                //pt: "Conversão formal em sulfetos (S = −2; F, Cl, Br, I = −1).\nMostrado quando o cristal não contém oxigênio.",
                //it: "Conversione formale in solfuri (S = −2; F, Cl, Br, I = −1).\nMostrato se il cristallo non contiene ossigeno.",
                //ru: "Формальный пересчёт на сульфиды (S = −2; F, Cl, Br, I = −1).\nПоказывается, если кристалл не содержит кислорода.",
                //zhHans: "形式换算为硫化物 (S = −2；F, Cl, Br, I = −1)。\n晶体不含氧时显示。",
                //zhHant: "形式換算為硫化物 (S = −2；F, Cl, Br, I = −1)。\n晶體不含氧時顯示。",
                //ko: "황화물로 형식 환산합니다 (S = −2, F, Cl, Br, I = −1).\n결정에 산소가 없을 때 표시됩니다."));
            // 261025Cl 変更: 硫化物モードの S は、手入力が無ければ電荷が釣り合う値になる (作者指示)。261026Cl 11 言語にした
            //toolTip.SetToolTip(radioButtonSulfides, Loc( // 261026Cl 変更前 (多言語対応: 11 言語にそろえ、訳語を統一。作者指示)
                //en: "Formal conversion to sulfides. Unless entered, the valence of S\nis the one that balances the charges (e.g. FeS2 with Fe = 2 → S = −1);\nF, Cl, Br, I = −1 by default. Shown when the crystal contains no oxygen."));
            //toolTip.SetToolTip(radioButtonSulfides, Loc( // 261026Cl 変更: 11 言語 (S は入力しなければ電荷が釣り合う値) // 261026Cl 変更前 (ツールチップ監査の反映)
                //en: "Formal conversion to sulfides. Unless entered, the valence of S\nis the one that balances the charges (e.g. FeS2, Fe = 2 → S = −1);\nF, Cl, Br, I = −1 by default.\nShown when the crystal contains no oxygen.",
                //ja: "硫化物に形式的に換算します。S の価数は、入力しなければ\n電荷が釣り合う値になります (例: FeS2 で Fe = 2 → S = −1)。\nF, Cl, Br, I の既定は −1。酸素を含まない結晶で表示されます。",
                //de: "Formale Umrechnung in Sulfide. Ohne Eingabe ist die Valenz von S\nder Wert, der die Ladungen ausgleicht (z. B. FeS2, Fe = 2 → S = −1);\nF, Cl, Br, I standardmäßig −1.\nWird angezeigt, wenn der Kristall keinen Sauerstoff enthält.",
                //fr: "Conversion formelle en sulfures. Sans saisie, la valence de S\nest celle qui équilibre les charges (p. ex. FeS2, Fe = 2 → S = −1) ;\nF, Cl, Br, I = −1 par défaut.\nAffiché si le cristal ne contient pas d'oxygène.",
                //es: "Conversión formal a sulfuros. Si no se introduce, la valencia de S\nes la que equilibra las cargas (p. ej. FeS2, Fe = 2 → S = −1);\nF, Cl, Br, I = −1 por defecto.\nSe muestra si el cristal no contiene oxígeno.",
                ////pt: "Conversão formal em sulfetos. Se não for digitada, a valência de S\né a que equilibra as cargas (ex.: FeS2, Fe = 2 → S = −1);\nF, Cl, Br, I = −1 por padrão.\nMostrado quando o cristal não contém oxigênio.", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                //pt: "Conversão formal em sulfetos. Se não for digitada, a valência de S\né a que equilibra as cargas (ex.: FeS2, Fe = 2 → S = −1);\nF, Cl, Br, I = −1 por padrão.\nAparece quando o cristal não contém oxigênio.",
                //it: "Conversione formale in solfuri. Se non inserita, la valenza di S\nè quella che bilancia le cariche (es. FeS2, Fe = 2 → S = −1);\nF, Cl, Br, I = −1 predefinito.\nMostrato se il cristallo non contiene ossigeno.",
                //ru: "Формальный пересчёт на сульфиды. Если валентность S не задана,\nберётся значение, при котором заряды скомпенсированы\n(например, FeS2, Fe = 2 → S = −1); F, Cl, Br, I = −1 по умолчанию.\nПоказывается, если кристалл не содержит кислорода.",
                //zhHans: "形式换算为硫化物。未输入时，S 的价数取使电荷平衡的值\n(例: FeS2 中 Fe = 2 → S = −1)；F, Cl, Br, I 默认为 −1。\n晶体不含氧时显示。",
                //zhHant: "形式換算為硫化物。未輸入時，S 的價數取使電荷平衡的值\n(例: FeS2 中 Fe = 2 → S = −1)；F, Cl, Br, I 預設為 −1。\n晶體不含氧時顯示。",
                ////ko: "황화물로 형식 환산합니다. 입력하지 않으면 S 의 원자가는\n전하가 균형을 이루는 값이 됩니다 (예: FeS2, Fe = 2 → S = −1).\nF, Cl, Br, I 의 기본값은 −1. 결정에 산소가 없을 때 표시됩니다.")); // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                //ko: "황화물로 형식적으로 환산합니다. 입력하지 않으면 S의 원자가는\n전하가 균형을 이루는 값이 됩니다 (예: FeS2, Fe = 2 → S = −1).\nF, Cl, Br, I의 기본값은 −1. 결정에 산소가 없을 때 표시됩니다."));
            toolTip.SetToolTip(radioButtonSulfides, Loc( // 261026Cl 変更: 表示する条件を正確に (陰イオンを含み、O を含まない)
                en: "Formal conversion to sulfides. Unless entered, the valence of S\nis the one that balances the charges (e.g. FeS2, Fe = 2 → S = −1);\nF, Cl, Br, I = −1 by default.\nShown when the crystal contains S and no oxygen.",
                ja: "硫化物に形式的に換算します。S の価数は、入力しなければ\n電荷が釣り合う値になります (例: FeS2 で Fe = 2 → S = −1)。\nF, Cl, Br, I の既定は −1。\nS を含み、酸素を含まない結晶で表示されます。",
                de: "Formale Umrechnung in Sulfide. Ohne Eingabe ist die Valenz von S\nder Wert, der die Ladungen ausgleicht (z. B. FeS2, Fe = 2 → S = −1);\nF, Cl, Br, I standardmäßig −1.\nWird angezeigt, wenn der Kristall S, aber keinen Sauerstoff enthält.",
                fr: "Conversion formelle en sulfures. Sans saisie, la valence de S\nest celle qui équilibre les charges (p. ex. FeS2, Fe = 2 → S = −1) ;\nF, Cl, Br, I = −1 par défaut.\nAffiché si le cristal contient S mais pas d'oxygène.",
                es: "Conversión formal a sulfuros. Si no se introduce, la valencia de S\nes la que equilibra las cargas (p. ej. FeS2, Fe = 2 → S = −1);\nF, Cl, Br, I = −1 por defecto.\nSe muestra si el cristal contiene S y no contiene oxígeno.",
                pt: "Conversão formal em sulfetos. Se não for digitada, a valência de S\né a que equilibra as cargas (ex.: FeS2, Fe = 2 → S = −1);\nF, Cl, Br, I = −1 por padrão.\nAparece quando o cristal contém S e não contém oxigênio.",
                it: "Conversione formale in solfuri. Se non inserita, la valenza di S\nè quella che bilancia le cariche (es. FeS2, Fe = 2 → S = −1);\nF, Cl, Br, I = −1 predefinito.\nMostrato se il cristallo contiene S ma non ossigeno.",
                ru: "Формальный пересчёт на сульфиды. Если валентность S не задана,\nберётся значение, при котором заряды скомпенсированы\n(например, FeS2, Fe = 2 → S = −1); F, Cl, Br, I = −1 по умолчанию.\nПоказывается, если кристалл содержит S\nи не содержит кислорода.",
                zhHans: "形式换算为硫化物。未输入时，S 的价数取使电荷平衡的值\n(例: FeS2 中 Fe = 2 → S = −1)；F, Cl, Br, I 默认为 −1。\n晶体含 S 且不含氧时显示。",
                zhHant: "形式換算為硫化物。未輸入時，S 的價數取使電荷平衡的值\n(例: FeS2 中 Fe = 2 → S = −1)；F, Cl, Br, I 預設為 −1。\n晶體含 S 且不含氧時顯示。",
                ko: "황화물로 형식적으로 환산합니다. 입력하지 않으면 S의 원자가는\n전하가 균형을 이루는 값이 됩니다 (예: FeS2, Fe = 2 → S = −1).\nF, Cl, Br, I의 기본값은 −1. 결정에 S가 있고 산소가 없을 때 표시됩니다."));
            //toolTip.SetToolTip(radioButtonInputOrder, Loc( // 261026Cl 変更前 (多言語対応: 11 言語にそろえ、訳語を統一。作者指示)
                //en: "Elements in order of first appearance in the atom list.",
                //ja: "原子リストに最初に現れた順に並べます。",
                //de: "Elemente in der Reihenfolge ihres ersten Auftretens\nin der Atomliste.",
                //fr: "Éléments dans l'ordre de leur première apparition\ndans la liste des atomes.",
                //es: "Elementos en el orden en que aparecen por primera vez\nen la lista de átomos.",
                //pt: "Elementos na ordem em que aparecem pela primeira vez\nna lista de átomos.",
                //it: "Elementi nell'ordine della loro prima comparsa\nnell'elenco degli atomi.",
                //ru: "Элементы в порядке первого появления в списке атомов.",
                //zhHans: "按在原子列表中首次出现的顺序排列。",
                //zhHant: "依在原子列表中首次出現的順序排列。",
                //ko: "원자 목록에 처음 나온 순서로 정렬합니다."));
            toolTip.SetToolTip(radioButtonInputOrder, Loc( // 261026Cl 変更: zh-Hant 原子清單
                en: "Elements in order of first appearance in the atom list.",
                ja: "原子リストに最初に現れた順に並べます。",
                de: "Elemente in der Reihenfolge ihres ersten Auftretens\nin der Atomliste.",
                fr: "Éléments dans l'ordre de leur première apparition\ndans la liste des atomes.",
                es: "Elementos en el orden en que aparecen por primera vez\nen la lista de átomos.",
                pt: "Elementos na ordem em que aparecem pela primeira vez\nna lista de átomos.",
                it: "Elementi nell'ordine della loro prima comparsa\nnell'elenco degli atomi.",
                ru: "Элементы в порядке первого появления в списке атомов.",
                //zhHans: "按在原子列表中首次出现的顺序排列。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                zhHans: "按元素在原子列表中首次出现的顺序排列。",
                //zhHant: "依在原子清單中首次出現的順序排列。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                zhHant: "依元素在原子清單中首次出現的順序排列。",
                ko: "원자 목록에 처음 나온 순서로 정렬합니다."));
            toolTip.SetToolTip(radioButtonAtomicNumberOrder, Loc(
                en: "Elements in ascending order of atomic number.",
                ja: "原子番号の小さい順に並べます。",
                de: "Elemente nach aufsteigender Ordnungszahl.",
                fr: "Éléments par numéro atomique croissant.",
                es: "Elementos por número atómico creciente.",
                pt: "Elementos em ordem crescente de número atômico.",
                it: "Elementi in ordine crescente di numero atomico.",
                ru: "Элементы по возрастанию атомного номера.",
                zhHans: "按原子序数从小到大排列。",
                //zhHant: "依原子序數由小到大排列。", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                zhHant: "依元素的原子序由小到大排列。",
                ko: "원자 번호가 작은 순서로 정렬합니다."));
            //toolTip.SetToolTip(buttonResetValences, Loc( // 261026Cl 変更前 (多言語対応: 11 言語にそろえ、訳語を統一。作者指示)
                //en: "Discard the valences entered for this crystal\nand return to the default values.",
                //ja: "この結晶で入力した価数を消し、既定値に戻します。",
                //de: "Für diesen Kristall eingegebene Wertigkeiten verwerfen\nund die Standardwerte verwenden.",
                //fr: "Supprimer les valences saisies pour ce cristal\net revenir aux valeurs par défaut.",
                //es: "Descarta las valencias introducidas para este cristal\ny vuelve a los valores predeterminados.",
                //pt: "Descarta as valências digitadas para este cristal\ne volta aos valores padrão.",
                //it: "Elimina le valenze inserite per questo cristallo\ne torna ai valori predefiniti.",
                //ru: "Удалить введённые для этого кристалла валентности\nи вернуться к значениям по умолчанию.",
                //zhHans: "清除为此晶体输入的价态，恢复为默认值。",
                //zhHant: "清除為此晶體輸入的價態，恢復為預設值。",
                //ko: "이 결정에서 입력한 원자가를 지우고 기본값으로 되돌립니다."));
            toolTip.SetToolTip(buttonResetValences, Loc( // 261026Cl 変更: de Valenzen・zh 价数/價數
                en: "Discard the valences entered for this crystal\nand return to the default values.",
                ja: "この結晶で入力した価数を消し、既定値に戻します。",
                de: "Für diesen Kristall eingegebene Valenzen verwerfen\nund die Standardwerte verwenden.",
                fr: "Supprimer les valences saisies pour ce cristal\net revenir aux valeurs par défaut.",
                es: "Descarta las valencias introducidas para este cristal\ny vuelve a los valores predeterminados.",
                pt: "Descarta as valências digitadas para este cristal\ne volta aos valores padrão.",
                it: "Elimina le valenze inserite per questo cristallo\ne torna ai valori predefiniti.",
                ru: "Удалить введённые для этого кристалла валентности\nи вернуться к значениям по умолчанию.",
                zhHans: "清除为此晶体输入的价数，恢复为默认值。",
                zhHant: "清除為此晶體輸入的價數，恢復為預設值。",
                //ko: "이 결정에서 입력한 원자가를 지우고 기본값으로 되돌립니다.")); // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                ko: "이 결정에 입력한 원자가를 지우고 기본값으로 되돌립니다."));
            //toolTip.SetToolTip(numericBoxTotalValence, Loc( // 261025Cl 変更前 (規格化した原子数で数えていた)
            //    en: "Sum of valence × normalized atoms over all elements.\n0 when the charges balance; ? when a valence is not set.",
            //    ja: "全元素の 価数 × 規格化した原子数 の和。\n電荷が釣り合えば 0。価数が未設定の元素があると ?。",
            //    de: "Summe aus Wertigkeit × normierter Atomzahl aller Elemente.\n0 bei ausgeglichener Ladung; ? wenn eine Wertigkeit fehlt.",
            //    fr: "Somme valence × nombre d'atomes normalisé de tous les éléments.\n0 si les charges s'équilibrent ; ? si une valence n'est pas définie.",
            //    es: "Suma de valencia × número de átomos normalizado de todos\nlos elementos. 0 si las cargas se equilibran; ? si falta una valencia.",
            //    pt: "Soma de valência × número de átomos normalizado de todos\nos elementos. 0 quando as cargas se equilibram; ? se faltar uma valência.",
            //    it: "Somma di valenza × numero di atomi normalizzato di tutti\ngli elementi. 0 se le cariche si bilanciano; ? se manca una valenza.",
            //    ru: "Сумма валентность × нормированное число атомов по всем элементам.\n0, если заряды скомпенсированы; ?, если валентность не задана.",
            //    zhHans: "所有元素的 价态 × 归一化原子数 之和。\n电荷平衡时为 0；有未设定的价态时为 ?。",
            //    zhHant: "所有元素的 價態 × 歸一化原子數 之和。\n電荷平衡時為 0；有未設定的價態時為 ?。",
            //    ko: "모든 원소의 원자가 × 정규화한 원자 수의 합.\n전하가 균형을 이루면 0, 미설정 원자가가 있으면 ?."));
            // 261025Cl 変更: 全体の電荷は単位胞あたり (作者決定)。261026Cl 11 言語にした
            //toolTip.SetToolTip(labelTotalValence, Loc( // 261026Cl 変更: 合計の電荷は Label (旧 numericBoxTotalValence) // 261026Cl 変更前 (多言語対応: 11 言語にそろえ、訳語を統一。作者指示)
                //en: "Total charge of the unit cell\n(sum of valence × atoms in the unit cell over all elements).\n0 when the charges balance; ? when a valence is not set."));
            toolTip.SetToolTip(labelTotalValence, Loc( // 261026Cl 変更: 11 言語 (単位胞あたりの全体の電荷)
                en: "Total charge of the unit cell\n(sum of valence × atoms in the unit cell over all elements).\n0 when the charges balance; ? when a valence is not set.",
                ja: "単位胞全体の電荷\n(全元素の 価数 × 単位胞中の原子数 の和)。\n電荷が釣り合えば 0。価数が未設定の元素があると ?。",
                //de: "Gesamtladung der Elementarzelle\n(Summe aus Valenz × Atomanzahl in der Zelle über alle Elemente).\n0 bei ausgeglichener Ladung; ? wenn eine Valenz fehlt.", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                de: "Gesamtladung der Elementarzelle\n(Summe aus Valenz × Atomanzahl in der Zelle über alle Elemente).\n0 bei ausgeglichener Ladung; ?, wenn eine Valenz fehlt.",
                //fr: "Charge totale de la maille\n(somme valence × nombre d'atomes dans la maille, tous éléments).\n0 si les charges s'équilibrent ; ? si une valence n'est pas définie.", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                fr: "Charge totale de la maille (somme, sur tous les éléments,\ndes produits valence × nombre d'atomes dans la maille).\n0 si les charges s'équilibrent ; ? si une valence n'est pas définie.",
                //es: "Carga total de la celda unidad\n(suma de valencia × átomos en la celda de todos los elementos).\n0 si las cargas se equilibran; ? si falta una valencia.", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                es: "Carga total de la celda unidad\n(suma de valencia × átomos en la celda sobre todos los elementos).\n0 si las cargas se equilibran; ? si falta una valencia.",
                //pt: "Carga total da célula unitária\n(soma de valência × átomos na célula de todos os elementos).\n0 quando as cargas se equilibram; ? se faltar uma valência.", // 261026Cl 変更前 (訳文レビューの反映: 訳語の統一・文法・言い回し)
                pt: "Carga total da célula unitária\n(soma de valência × átomos na célula sobre todos os elementos).\n0 quando as cargas se equilibram; ? se faltar uma valência.",
                it: "Carica totale della cella elementare\n(somma di valenza × atomi nella cella per tutti gli elementi).\n0 se le cariche si bilanciano; ? se manca una valenza.",
                ru: "Суммарный заряд элементарной ячейки (сумма произведений\nвалентности на число атомов в ячейке по всем элементам).\n0, если заряды скомпенсированы; ?, если валентность не задана.",
                zhHans: "晶胞的总电荷\n(所有元素的 价数 × 晶胞中原子数 之和)。\n电荷平衡时为 0；有未设定的价数时为 ?。",
                zhHant: "晶胞的總電荷\n(所有元素的 價數 × 晶胞中原子數 之和)。\n電荷平衡時為 0；有未設定的價數時為 ?。",
                ko: "단위 격자 전체의 전하\n(모든 원소의 원자가 × 단위 격자 내 원자 수의 합).\n전하가 균형을 이루면 0, 미설정 원자가가 있으면 ?."));
        }

        #endregion
    }
}
