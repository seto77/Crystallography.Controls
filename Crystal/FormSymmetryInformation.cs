#region using, namespace
using System;
using System.Collections.Generic; // 260704Cl
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D; // 260504Cl
using System.Drawing.Text; // 260504Cl
using System.Text.RegularExpressions; // 260427Cl
using System.Windows.Forms;
using static Crystallography.Localization; // 260620Cl 追加: コード側多言語化 Loc() (方式②, §3-B)

namespace Crystallography.Controls;
#endregion
public partial class FormSymmetryInformation : FormBase
{
    #region プロパティ
    /// <summary>表示対象の <see cref="Crystallography.Crystal"/>。<see cref="CrystalControl"/> から委譲。</summary>
    public Crystal Crystal => CrystalControl.Crystal;

    /// <summary>結晶情報を保持する親の <see cref="CrystalControl"/>。Load 時に CrystalChanged を購読する。</summary>
    public CrystalControl CrystalControl;

    /// <summary>4-index (Miller-Bravais) 表記の入力欄 (i-axis) を表示するかどうか。trigonal/hexagonal で true を想定。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool MillerBravais { get => indexControlPlane1.MillerBravais; set => indexControlPlane1.MillerBravais = indexControlPlane2.MillerBravais = value; }

    #endregion 

    #region コンストラクタ、ロード、クローズ
    /// <summary>デザイナ生成のコントロールを初期化する。</summary>
    public FormSymmetryInformation()
    {
        InitializeComponent(); // (260426Ch)
        // 260620Cl 追加: Localizable=false で英語直書きの静的ラベルをコード側 Loc() で多言語化 (方式②)。
        // デザイナ表示は neutral 英語のままにしたいので実行時のみ適用する。
        if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
        {
            LocalizeLabels();
            SetupExtraTables(); // 260704Cl: Operations/Properties/Settings タブの列定義 (Loc ヘッダ) を一度だけ構築
        }
    }

    /// <summary>
    /// Load イベントハンドラ。<see cref="CrystalControl.CrystalChanged"/> を購読し、初期表示として <see cref="ChangeCrystal"/> を呼ぶ。
    /// </summary>
    private void FormCrystallographicInformation_Load(object sender, EventArgs e)
    {
        CrystalControl.CrystalChanged += (_, _) => ChangeCrystal(); // (260426Ch) 1 行 handler をインライン化
        // 260429Cl 追加: GraphicsBox サイズ変更時に図を再描画
        graphicsBoxSymmetryElements.SizeChanged += (_, _) => UpdateDiagrams();
        graphicsBoxGeneralPositions.SizeChanged += (_, _) => UpdateDiagrams();
        ChangeCrystal();
    }

    /// <summary>
    /// FormClosing イベントハンドラ。閉じる代わりに非表示にし、フォームのインスタンスを再利用する。
    /// </summary>
    private void FormCrystallographicInformation_FormClosing(object sender, FormClosingEventArgs e)
    {
        e.Cancel = true;
        Visible = false; // (260426Ch)
    }
    #endregion

    #region ラベルの多言語化 (260620Cl 追加, 方式②)
    /// <summary>
    /// Designer.cs に英語直書き (Localizable=false) されている静的ラベルを、現在の UI カルチャの訳へ差し替える。
    /// コンストラクタの InitializeComponent 直後に 1 回だけ呼ぶ (フォームは閉じても再利用するため再適用不要)。
    /// 用語は出荷済みの <see cref="SymmetryControl"/> / <see cref="AtomControl"/> の訳に合わせ、結晶学用語は IUCr 準拠。
    /// SF (Schoenflies) / HM (Hermann–Mauguin) / Hall の略号は各言語とも据置 (国際表記)。
    /// LaTeX/単一軸文字/bmp・emf/実行時に流し込む labelLaTex* は対象外。
    /// </summary>
    private void LocalizeLabels()
    {
        // --- 共通ボタン / タブ見出し ---
        buttonCopyElements.Text = buttonCopyPositions.Text =
            Loc(en: "Copy", ja: "コピー", de: "Kopieren", fr: "Copier", es: "Copiar", pt: "Copiar", it: "Copia", ru: "Копировать", zhHans: "复制", zhHant: "複製", ko: "복사");
        tabPageGeometrics.Text = Loc(en: "Geometrics Calculation", ja: "幾何計算", de: "Geometrische Berechnung", fr: "Calculs géométriques", es: "Cálculo geométrico", pt: "Cálculo geométrico", it: "Calcoli geometrici", ru: "Геометрические расчёты", zhHans: "几何计算", zhHant: "幾何計算", ko: "기하 계산");
        tabPageWyckoff.Text = Loc(en: "Wyckoff Positions", ja: "ワイコフ位置", de: "Wyckoff-Lagen", fr: "Positions de Wyckoff", es: "Posiciones de Wyckoff", pt: "Posições de Wyckoff", it: "Posizioni di Wyckoff", ru: "Позиции Уайкоффа", zhHans: "Wyckoff 位置", zhHant: "Wyckoff 位置", ko: "와이코프 위치");
        tabPageConditions.Text = Loc(en: "Conditions", ja: "反射条件", de: "Bedingungen", fr: "Conditions", es: "Condiciones", pt: "Condições", it: "Condizioni", ru: "Условия", zhHans: "衍射条件", zhHant: "條件", ko: "조건");
        // 260704Cl 追加: Phase 1 の新規タブ / ボタン
        tabPageOperations.Text = Loc(en: "Operations", ja: "対称操作", de: "Operationen", fr: "Opérations", es: "Operaciones", pt: "Operações", it: "Operazioni", ru: "Операции", zhHans: "对称操作", zhHant: "對稱操作", ko: "대칭 연산");
        tabPageProperties.Text = Loc(en: "Properties", ja: "群の性質", de: "Eigenschaften", fr: "Propriétés", es: "Propiedades", pt: "Propriedades", it: "Proprietà", ru: "Свойства", zhHans: "群性质", zhHant: "群性質", ko: "군의 성질");
        tabPageSettings.Text = Loc(en: "Settings", ja: "設定一覧", de: "Aufstellungen", fr: "Réglages", es: "Configuraciones", pt: "Configurações", it: "Impostazioni", ru: "Установки", zhHans: "设置一览", zhHant: "設定一覽", ko: "설정 목록");
        buttonCopyCif.Text = Loc(en: "Copy (CIF)", ja: "コピー (CIF)", de: "Kopieren (CIF)", fr: "Copier (CIF)", es: "Copiar (CIF)", pt: "Copiar (CIF)", it: "Copia (CIF)", ru: "Копировать (CIF)", zhHans: "复制 (CIF)", zhHant: "複製 (CIF)", ko: "복사 (CIF)");
        buttonGroupRelations.Text = Loc(en: "Group Relations...", ja: "群の関係...", de: "Gruppenrelationen...", fr: "Relations de groupe...", es: "Relaciones de grupo...", pt: "Relações de grupo...", it: "Relazioni di gruppo...", ru: "Групповые отношения...", zhHans: "群关系...", zhHant: "群關係...", ko: "군 관계...");

        // --- Geometrics タブ: 計算結果ラベル ---
        label40.Text = Loc(en: "The axis normal to both planes", ja: "両面に垂直な軸", de: "Achse senkrecht zu beiden Ebenen", fr: "L'axe normal aux deux plans", es: "Eje normal a ambos planos", pt: "O eixo normal a ambos os planos", it: "L'asse normale a entrambi i piani", ru: "Ось, перпендикулярная обеим плоскостям", zhHans: "同时垂直于两晶面的晶轴", zhHant: "垂直於兩晶面的晶軸", ko: "두 면에 수직인 축");
        label42.Text = Loc(en: "The plane normal to both axes", ja: "両軸に垂直な面", de: "Ebene senkrecht zu beiden Achsen", fr: "Le plan normal aux deux axes", es: "Plano normal a ambos ejes", pt: "O plano normal a ambos os eixos", it: "Il piano normale a entrambi gli assi", ru: "Плоскость, перпендикулярная обеим осям", zhHans: "同时垂直于两晶轴的晶面", zhHant: "垂直於兩晶軸的晶面", ko: "두 축에 수직인 면");

        // --- Wyckoff タブ: 旧 DataGridView 列見出し (Mult./Site Sym. は AtomControl の出荷済み訳に一致) ---
        // 260706Ch: Wyckoff タブは miniTableWyckoff (MiniTable.SetColumns、下記 SetupExtraTables 内) へ移行し、
        // 対応する旧 dataGridView1 とその列は孤立コントロールとして削除済み (simplify2 Phase4)。
        //columnMultiplicityDataGridViewTextBoxColumn.HeaderText = Loc(en: "Mult.", ja: "多重度", de: "Mult.", fr: "Mult.", es: "Mult.", pt: "Mult.", it: "Molt.", ru: "Кратн.", zhHans: "多重性", zhHant: "多重度", ko: "중복도");
        //columnWyckoffLetterDataGridViewTextBoxColumn.HeaderText = Loc(en: "Wyck. Let.", ja: "記号", de: "Wyck.-Buchst.", fr: "Lettre Wyck.", es: "Let. Wyck.", pt: "Let. Wyck.", it: "Lett. Wyck.", ru: "Б. Уайк.", zhHans: "Wyck. 字母", zhHant: "Wyck. 字母", ko: "WP 기호");
        //columnSiteSymmetryDataGridViewTextBoxColumn.HeaderText = Loc(en: "Site Sym.", ja: "サイト対称性", de: "Lagesym.", fr: "Sym. site", es: "Sim. sitio", pt: "Sim. sítio", it: "Simm. sito", ru: "Симм. поз.", zhHans: "位置对称性", zhHant: "位置對稱", ko: "자리 대칭");
        //columnCoordinates1DataGridViewTextBoxColumn.HeaderText = Loc(en: "Coordinates", ja: "座標", de: "Koordinaten", fr: "Coordonnées", es: "Coordenadas", pt: "Coordenadas", it: "Coordinate", ru: "Координаты", zhHans: "坐标", zhHant: "座標", ko: "좌표");

        // --- Conditions タブ ---
        label49.Text = Loc(en: "Conditions limiting possible reflections", ja: "反射条件（系統的消滅則）", de: "Auslöschungsbedingungen", fr: "Conditions limitant les réflexions possibles", es: "Condiciones limitantes de las reflexiones posibles", pt: "Condições que limitam as reflexões possíveis", it: "Condizioni che limitano le riflessioni possibili", ru: "Условия, ограничивающие возможные отражения", zhHans: "可能衍射的限制条件", zhHant: "限制可能反射的條件", ko: "가능한 반사를 제한하는 조건");

        // --- Space Group グループ (Space Group/Crystal System は SymmetryControl の出荷済み訳に一致) ---
        groupBoxSpaceGroup.Text = Loc(en: "Space Group", ja: "空間群", de: "Raumgruppe", fr: "Groupe d'espace", es: "Grupo espacial", pt: "Grupo espacial", it: "Gruppo spaziale", ru: "Пространственная группа", zhHans: "空间群", zhHant: "空間群", ko: "공간군");
        label8.Text = Loc(en: "SF symbol:", ja: "SF 記号:", de: "SF-Symbol:", fr: "Symbole SF :", es: "Símbolo SF:", pt: "Símbolo SF:", it: "Simbolo SF:", ru: "Символ SF:", zhHans: "SF 符号:", zhHant: "SF 符號：", ko: "SF 기호:");
        label9.Text = Loc(en: "Hall symbol:", ja: "Hall 記号:", de: "Hall-Symbol:", fr: "Symbole Hall :", es: "Símbolo Hall:", pt: "Símbolo Hall:", it: "Simbolo Hall:", ru: "Символ Hall:", zhHans: "Hall 符号:", zhHant: "Hall 符號：", ko: "Hall 기호:");
        label5.Text = Loc(en: "HM symbol (short):", ja: "HM 記号 (短縮):", de: "HM-Symbol (kurz):", fr: "Symbole HM (court) :", es: "Símbolo HM (corto):", pt: "Símbolo HM (curto):", it: "Simbolo HM (corto):", ru: "Символ HM (краткий):", zhHans: "HM 符号(简):", zhHant: "HM 符號（簡式）：", ko: "HM 기호 (단축형):");
        label6.Text = Loc(en: "HM symbol (full):", ja: "HM 記号 (完全):", de: "HM-Symbol (voll):", fr: "Symbole HM (complet) :", es: "Símbolo HM (completo):", pt: "Símbolo HM (completo):", it: "Simbolo HM (completo):", ru: "Символ HM (полный):", zhHans: "HM 符号(全):", zhHant: "HM 符號（全式）：", ko: "HM 기호 (완전형):");
        label.Text = Loc(en: "Crystal System:", ja: "結晶系:", de: "Kristallsystem:", fr: "Système cristallin :", es: "Sistema cristalino:", pt: "Sistema cristalino:", it: "Sistema cristallino:", ru: "Кристаллическая система:", zhHans: "晶系:", zhHant: "晶系：", ko: "결정계:");

        // --- Point Group グループ ---
        groupBoxPointGroup.Text = Loc(en: "Point Group", ja: "点群", de: "Punktgruppe", fr: "Groupe ponctuel", es: "Grupo puntual", pt: "Grupo pontual", it: "Gruppo puntuale", ru: "Точечная группа", zhHans: "点群", zhHant: "點群", ko: "점군");
        label10.Text = Loc(en: "HM symbol:", ja: "HM 記号:", de: "HM-Symbol:", fr: "Symbole HM :", es: "Símbolo HM:", pt: "Símbolo HM:", it: "Simbolo HM:", ru: "Символ HM:", zhHans: "HM 符号:", zhHant: "HM 符號：", ko: "HM 기호:");
        label11.Text = Loc(en: "SF symbol:", ja: "SF 記号:", de: "SF-Symbol:", fr: "Symbole SF :", es: "Símbolo SF:", pt: "Símbolo SF:", it: "Simbolo SF:", ru: "Символ SF:", zhHans: "SF 符号:", zhHant: "SF 符號：", ko: "SF 기호:");
        label4.Text = Loc(en: "Number:", ja: "番号:", de: "Nummer:", fr: "Numéro :", es: "Número:", pt: "Número:", it: "Numero:", ru: "Номер:", zhHans: "序号:", zhHant: "編號：", ko: "번호:");

        // --- Options グループ ---
        label15.Text = Loc(en: "Options", ja: "オプション", de: "Optionen", fr: "Options", es: "Opciones", pt: "Opções", it: "Opzioni", ru: "Параметры", zhHans: "选项", zhHant: "選項", ko: "옵션");
        label16.Text = Loc(en: "Direction", ja: "投影方向", de: "Richtung", fr: "Direction", es: "Dirección", pt: "Direção", it: "Direzione", ru: "Направление", zhHans: "方向", zhHant: "方向", ko: "방향");
        label12.Text = Loc(en: "Copy format", ja: "コピー形式", de: "Kopierformat", fr: "Format de copie", es: "Formato de copia", pt: "Formato de cópia", it: "Formato di copia", ru: "Формат копирования", zhHans: "复制格式", zhHant: "複製格式", ko: "복사 형식");
    }
    #endregion

    #region 文字列をlatexへ変換
    /// <summary>結晶対称性シンボル文字列を WpfMath 用 math-mode LaTeX へ変換する。</summary>
    /// <param name="str">変換対象。</param>
    /// <param name="sfStyle">Schoenflies 系 (SG_SF, PG_SF) のとき true。</param>
    /// <param name="plain">単語 (CrystalSystem) を <c>\mathrm{}</c> でラップ。</param>
    /// <param name="spaced">HM 系の対称要素軸間に <c>\,</c> を挿入。</param>
    /// <param name="noBar">ExtinctionRule の indices 接頭辞のみ <c>-h/-k/-l</c> を overline 化。</param>
    // 260706Ch: FormGroupRelations の HmToLatex (同種の変換を独自実装していた) から呼べるよう internal 化。
    internal static string ToLatex(string str, bool sfStyle = false, bool plain = false, bool spaced = false, bool noBar = false)
    {
        if (string.IsNullOrEmpty(str)) return string.Empty;
        if (plain || str == "Unknown") return $@"\mathrm{{{str}}}";

        if (sfStyle)
        {
            var caret = str.IndexOf('^');
            var main = caret >= 0 ? str[..caret] : str;
            var sup = caret >= 0 ? str[(caret + 1)..] : "";
            var result = main.Length > 1 ? $"{main[0]}_{{{main[1..]}}}" : main;
            if (sup.Length > 0)
                result += $"^{{{sup}}}";
            return result;
        }

        // 260427Cl: HM 末尾の補助記号 ("Hex"/"Rho" の三方晶系設定 / "(1)"/"(2)" の原点選択) を一旦取り外し、
        // 最後に "\,\,_{...}" (二重 thin space + 下付き) で独立した小さなラベルとして再付与。
        // 先に取り外しておかないと spaced regex が "H" を格子文字、"(" を非ブロックと扱って表示が崩れる。
        string suffix = "";
        var sfxMatch = SuffixRegex().Match(str);
        if (sfxMatch.Success)
        {
            var s = sfxMatch.Value;
            // (N) は math mode の () + digit でアップライト描画されるのでそのまま、Hex/Rho は \mathrm{} で italic 化を防ぐ。
            var inner = s[0] == '(' ? s : $@"\mathrm{{{s}}}";
            suffix = $@"\,\,_{{{inner}}}";
            str = str[..^s.Length];
        }
        // 260427Cl: Hall シンボルの " (例: -R32"c) は WpfMath が解釈できないので '' (二重プライム) に置換。
        str = str.Replace("\"", "''");

        // axis ブロック単位 (格子文字 / 回転軸±スクリュー±鏡面 / 単独鏡面) で \, を挿入。
        // 連続するブロック同士の境目だけにマッチさせるので "=" 区切りや単一ブロックは無視される。
        // (?<!su) は単独鏡面 [mabcdn] に対する保険: 例 "P2sub1=..." で 2sub1 の lookahead が =
        // で失敗した後に、エンジンが "sub" の b を mirror plane として再マッチしないようガードする。
        // これがないと "P\,2sub\,1=..." となり、後段の sub1→_1 置換が効かなくなる。
        if (spaced)
            str = AxisBlockBoundaryRegex().Replace(str, @"$1\,");

        for (int n = 1; n <= 5; n++)
            str = str.Replace($"sub{n}", $"_{n}");

        // ExtinctionRule の indices 接頭辞 (最初の ":" まで) のみ "-h/-k/-l/-2h" 等を overline 化。
        // 例: "h-hl: 2h-l=4n: d⊥[110]" → "h\bar{h}l: 2h-l=4n: d\perp [110]" (条件式の "2h-l" や説明部はそのまま)
        // 260708Ch: 直後の汎用 "-N→\bar{N}" ループより前に処理する必要がある。後回しにすると "-2h" の "-2" だけが
        // 先に食われて \bar{2}h に壊れ、それを直すためだけの補正 regex (IndicesPrefixDigitLetterBarRegex) が要った。
        if (noBar)
        {
            var colonIdx = str.IndexOf(':');
            if (colonIdx > 0)
                str = OverlineIndicesPrefix(str[..colonIdx]) + str[colonIdx..];
        }

        // "-N" (digit) → "\bar{N}". HM では P-3m → P\bar{3}m、ExtinctionRule では [01-1] → [01\bar{1}]。
        // 条件式部の "2h-k=4n" 等は "-letter" なのでこの置換にかからず literal のまま。
        for (int n = 0; n <= 9; n++)
            str = str.Replace($"-{n}", $"\\bar{{{n}}}");
        str = str.Replace("⊥", @"\perp ");
        str = str.Replace("//", @"\parallel "); // 260427Cl: ExtinctionRule の "2sub1//[100]" 等を ∥ 記号で描画
        // 連続する条件式の境目 (例: "...=2n k+l=2n" → "...=2n, k+l=2n") にカンマを挿入。
        // ExtinctionRule の F centering 等でしか出ない \dn[空白][letter] パターン限定なので副作用なし。
        str = ConditionSeparatorRegex().Replace(str, ", ");
        return str + suffix; // 三方晶系 Hex/Rho 接尾辞があれば末尾に下付きで再付与
    }

    private static string SiteSymmetryToLatex(string str) // 260706Ch: Wyckoff / MiniTable セル用
        => string.IsNullOrWhiteSpace(str) || str == "-" ? str : ToLatex(str, spaced: true);

    /// <summary>"-h"/"-2h" 等の hkl 指数接頭辞を overline (単一文字は \bar、複数文字は \overline) へ変換する。
    /// 汎用の "-N→\bar{N}" 置換より前に適用する必要がある (でないと "-2h" が "\bar{2}h" に壊れる)。260708Ch 追加
    /// (ToLatex の noBar 分岐と ConditionIndicesToLatex に重複していた同一ロジックを統合)。</summary>
    private static string OverlineIndicesPrefix(string str)
        => IndicesPrefixBarRegex().Replace(str, match => match.Groups[1].Value.Length > 1
            ? $@"\overline{{{match.Groups[1].Value}}}"
            : $@"\bar{{{match.Groups[1].Value}}}");

    private static string ConditionIndicesToLatex(string str) // 260708Ch: Conditions の indices 列専用。-2h は \overline{2h} として扱う。
        => string.IsNullOrWhiteSpace(str) ? str : ToLatex(OverlineIndicesPrefix(str.Trim())); // 260708Ch

    private static string CoordinateToLatex(string str) // 260706Ch: Wyckoff 座標セル用。parse 不能時はセル側が通常描画へ退避する。
    {
        if (string.IsNullOrWhiteSpace(str)) return str;
        //return str.Replace(" ", @"\,"); // 260707Ch: 座標先頭の -x/-y/-z を結晶学標準の overbar 表記へ変換するため下へ移動
        var latex = LeadingNegativeCoordinateRegex().Replace(str, @"$1\bar{$2}"); // 260707Ch: 例 (-x,-y,-z) → (\bar{x},\bar{y},\bar{z})
        return latex.Replace(" ", @"\,");
    }

    // 260708Ch: SeitzNotation.SeitzLatex(in SymmetryOperation) に置き換え。Seitz() が返す文字列を正規表現で
    // 再パースするのではなく、Seitz() と同じ構造データ (Order/Sense/Direction/SeitzTranslation) から直接 LaTeX を
    // 組み立てる形に刷新 (全空間群展開済み7424操作で旧実装と出力が完全一致することを検証済み)。
    //internal static string SeitzToLatex(string str) // 260707Ch: Seitz 記号セル用。-1/-4 と方向 [1-10] の負号を overbar 表記へ。
    //{
    //    if (string.IsNullOrWhiteSpace(str)) return str;
    //    var match = SeitzNotationRegex().Match(str.Trim()); // 260708Ch
    //    if (!match.Success)
    //    {
    //        var fallback = NegativeDigitRegex().Replace(str, @"\bar{$1}"); // 260708Ch
    //        return fallback.Replace(" ", @"\,"); // 260708Ch
    //    }
    //
    //    var rotationText = match.Groups["rotation"].Value; // 260708Ch
    //    var rotation = SeitzRotationToLatex(rotationText); // 260708Ch
    //    var direction = match.Groups["direction"].Success && rotationText is not "1" and not "-1" ? $"_{{{SeitzDirectionToLatex(match.Groups["direction"].Value)}}}" : ""; // 260708Ch
    //    var translation = match.Groups["translation"].Success ? match.Groups["translation"].Value : "0,0,0"; // 260708Ch
    //    return $@"\{{\,{rotation}{direction}\mid {translation.Replace(",", @",\,")}\,\}}"; // 260708Ch
    //}
    //
    //private static string SeitzRotationToLatex(string rotation) // 260708Ch
    //{
    //    var match = SeitzRotationRegex().Match(rotation);
    //    if (!match.Success)
    //        return rotation;
    //
    //    var number = match.Groups["number"].Value;
    //    var body = match.Groups["negative"].Success ? $@"\bar{{{number}}}" : number;
    //    return match.Groups["sense"].Success ? $@"{body}^{{{match.Groups["sense"].Value}}}" : body;
    //}
    //
    //private static string SeitzDirectionToLatex(string direction) // 260708Ch
    //{
    //    var body = direction.Trim('[', ']');
    //    var parts = SeitzDirectionComponentRegex().Matches(body);
    //    if (parts.Count == 0)
    //        return body;
    //
    //    var tokens = new List<string>(parts.Count);
    //    foreach (Match part in parts)
    //    {
    //        var token = part.Value;
    //        tokens.Add(token.StartsWith("-", StringComparison.Ordinal) ? $@"\bar{{{token[1..]}}}" : token);
    //    }
    //    return string.Concat(tokens);
    //}

    [GeneratedRegex(@"(Hex|Rho|\(\d\))$")]
    private static partial Regex SuffixRegex();

    [GeneratedRegex(@"([PABCFIHR]|-?\d(?:sub\d)?(?:/[mabcdn])?|(?<!su)[mabcdn])(?=[PABCFIHR]|-?\d(?:sub\d)?(?:/[mabcdn])?|(?<!su)[mabcdn])")]
    private static partial Regex AxisBlockBoundaryRegex();

    [GeneratedRegex(@"(?<=\dn)\s+(?=[a-zA-Z])")]
    private static partial Regex ConditionSeparatorRegex();

    //[GeneratedRegex(@"-([hkl])")] // 260708Ch: -2h のような係数付き指数もまとめて overbar にする
    [GeneratedRegex(@"-(\d*[hkl])")] // 260708Ch
    private static partial Regex IndicesPrefixBarRegex();

    [GeneratedRegex(@"(^|[(,\s])-(x|y|z)")]
    private static partial Regex LeadingNegativeCoordinateRegex(); // 260707Ch

    // 260708Ch: SeitzToLatex/SeitzRotationToLatex/SeitzDirectionToLatex 削除 (SeitzNotation.SeitzLatex に置き換え) に伴い未使用化。
    //[GeneratedRegex(@"-([0-9])")]
    //private static partial Regex NegativeDigitRegex(); // 260707Ch
    //
    //[GeneratedRegex(@"^(?<rotation>\S+)(?:\s+(?<direction>\[[^\]]+\]))?(?:\s+(?<translation>\S+))?$")]
    //private static partial Regex SeitzNotationRegex(); // 260708Ch
    //
    //[GeneratedRegex(@"^(?<negative>-)?(?<number>\d+)(?<sense>[+-])?$")]
    //private static partial Regex SeitzRotationRegex(); // 260708Ch
    //
    //[GeneratedRegex(@"-?\d")]
    //private static partial Regex SeitzDirectionComponentRegex(); // 260708Ch
    #endregion

    #region 出現則表示
    // 260427Cl: 結晶切替の度に N 個の LabelLaTeX を生成するので Font/Padding は static で共有 (Font は IDisposable だがアプリ生存期間)。
    //private static readonly Font ExtinctionRuleFont = new("Segoe UI", 13F); // 260708Ch: Conditions タブを MiniTable 表示へ変更
    //private static readonly Padding ExtinctionRuleMargin = new(0, 0, 0, 2); // 260708Ch

    ///// <summary>
    ///// <see cref="flowLayoutPanelExtinctionRule"/> に積む 1 行ぶんの <see cref="LabelLaTeX"/> を生成する (260427Cl 追加)。
    ///// </summary>
    ///// <param name="latex">行に描画する LaTeX 文字列。</param>
    ///// <returns>AutoSize 有効・Segoe UI 13pt・縁取り 0.6 で初期化した <see cref="LabelLaTeX"/>。</returns>
    //private static LabelLaTeX MakeExtinctionRuleLabel(string latex) => new()
    //{
    //    AutoSize = true,
    //    Font = ExtinctionRuleFont,
    //    Margin = ExtinctionRuleMargin,
    //    Thickness = 0.6,
    //    Text = latex,
    //};
    #endregion

    #region ChangeCrystal() 結晶が変更されたとき 

    /// <summary>現在の <see cref="Crystal"/> の対称性情報をフォーム上の各コントロールへ反映する。CrystalControl.CrystalChanged からも呼ばれる。</summary>
    public void ChangeCrystal()
    {
        numericBox_ValueChanged(this, EventArgs.Empty); // (260426Ch) 不要な object/EventArgs 生成を避ける
        SetWyckoffPosition();


        var symmetry = Crystal.Symmetry;

        // 260506Cl 追加: 点群が変わったら、その点群の既定 test point で numericBoxPosition* をリセット。
        // 同じ点群のままで空間群だけが切り替わった場合は、ユーザーが numericBoxPosition* に入れた値を保持する。
        // radioButtonDirection* も同様に、非 ortho → ortho に切り替わった瞬間 (初回含む) だけ既定 C に戻す。
        // 両者まとめて SkipEvent でガードし、ChangeCrystal 末尾の UpdateDiagrams() に再描画を一本化する。
        SkipEvent = true;
        try
        {
            if (symmetry.PointGroupNumber != _previousPointGroupNumber)
            {
                _previousPointGroupNumber = symmetry.PointGroupNumber;
                var (tx, ty, tz) = SymmetryDiagramPositions.GetTestPoint(symmetry);
                numericBoxPositionA.Value = tx;
                numericBoxPositionB.Value = ty;
                numericBoxPositionC.Value = tz;
            }

            bool isOrtho = symmetry.CrystalSystemNumber == 3;
            radioButtonDirectionA.Enabled = radioButtonDirectionB.Enabled = radioButtonDirectionC.Enabled = isOrtho;
            if (!isOrtho)
                SetSelectedDirection(SymmetryDiagramCommon.ResolveProjectionAxis(symmetry, ProjectionAxis.C));
            else if (!_previousIsOrtho)
                SetSelectedDirection(ProjectionAxis.C);
            _previousIsOrtho = isOrtho;
        }
        finally { SkipEvent = false; }

        labelLaTexNumber.Text = $"{symmetry.SpaceGroupNumber}: {symmetry.SpaceGroupSubNumber}";

        // 260427Cl 追加: LabelLaTeX 各種への流し込み (richTextBox 群と並走表示)。
        // 空間群・点群 HM 系 (HM, HM_full, PG_HM, LG) は対称要素軸ごとに thin space で区切る。
        // Hall は表記体系が異なるため spaced 指定なし。
        labelLaTexSG_HM.Text = ToLatex(symmetry.SpaceGroupHMStr, spaced: true);
        labelLaTexHM_full.Text = ToLatex(symmetry.SpaceGroupHMfullStr, spaced: true);
        labelLaTexSG_SF.Text = ToLatex(symmetry.SpaceGroupSFStr, sfStyle: true);
        labelLaTexSG_Hall.Text = ToLatex(symmetry.SpaceGroupHallStr, spaced: true);//やっぱりスペースはあった方がいい
        labelLaTexPG_HM.Text = ToLatex(symmetry.PointGroupHMStr, spaced: true);
        labelLaTexPG_SF.Text = ToLatex(symmetry.PointGroupSFStr, sfStyle: true);
        //labelLaTexLG.Text = ToLatex(symmetry.LaueGroupStr, spaced: true);
        labelLaTexCS.Text = ToLatex(symmetry.CrystalSystemStr, plain: true);

        // 260427Cl 追加: ExtinctionRule は 1 行 1 LabelLaTeX で FlowLayoutPanel に積む (AutoScroll でスクロール)。
        // hkl 算術式中の "-h"/"-1" は字面通りに残したいので noBar:true。
        //flowLayoutPanelExtinctionRule.SuspendLayout(); // 260708Ch: Conditions タブを MiniTable 表示へ変更
        //// Controls.Clear() は子を Dispose しないため、LabelLaTeX が保持する Bitmap が GC まで残る。
        //// Control.Dispose() は内部で Parent.Controls.Remove(this) を呼ぶため後ろから index で回す
        //// (前から foreach するとコレクション変更中の列挙で例外になる)。Dispose 後は Controls が空になるので Clear 不要。
        //for (int i = flowLayoutPanelExtinctionRule.Controls.Count - 1; i >= 0; i--)
        //    flowLayoutPanelExtinctionRule.Controls[i].Dispose();
        var rules = symmetry.ExtinctionRuleStr;
        var conditionRows = new List<object[]>(); // 260708Ch: LabelLaTeX の動的追加ではなく MiniTable 行として投入
        if (rules == null || rules.Length == 0)
            //flowLayoutPanelExtinctionRule.Controls.Add(MakeExtinctionRuleLabel(@"\mathrm{No\ Condition}")); // 260708Ch
            conditionRows.Add([@"\mathrm{No\ Condition}", "", ""]); // 260708Ch
        else
            foreach (var rule in rules)
                //flowLayoutPanelExtinctionRule.Controls.Add(MakeExtinctionRuleLabel(ToLatex(rule, noBar: true))); // 260708Ch
            {
                var parts = rule.Split(':', 3); // 260708Ch: 旧表示でコロン区切りだった 3 情報を MiniTable の 3 列へ分割
                if (parts.Length == 3)
                    conditionRows.Add([
                        //IndicesPrefixBarRegex().Replace(ToLatex(parts[0].Trim()), @"\bar{$1}"), // 260708Ch: -2h を \bar{2h} として扱う
                        ConditionIndicesToLatex(parts[0]), // 260708Ch
                        ToLatex(parts[1].Trim()),
                        ToLatex(parts[2].Trim())]); // 260708Ch
                else
                    conditionRows.Add([ToLatex(rule, noBar: true), "", ""]); // 260708Ch: 念のため旧形式でない文字列も表示
            }
        //flowLayoutPanelExtinctionRule.ResumeLayout(true); // 260708Ch
        miniTableConditions.SetRows(conditionRows); // 260708Ch

        // 260704Cl 追加: Operations / Properties / Settings タブの再構築
        // 260705Cl 修正: 3 表の内容は SymmetrySeriesNumber のみに依存するため、格子定数スピナーや原子編集などで
        // CrystalChanged が連続発火しても空間群が同じなら再構築しない (変更検出ガード)。
        if (_extraTablesSeriesNumber != Crystal.SymmetrySeriesNumber)
        {
            _extraTablesSeriesNumber = Crystal.SymmetrySeriesNumber;
            SetOperationsTable();
            SetPropertiesTable(symmetry);
            SetSettingsTable(symmetry);
        }

        // 260429Cl 追加: 対称要素・一般位置の図を再描画
        UpdateDiagrams();
    }

    // 260429Cl 追加: 前回 render 時の状態を保持して、SizeChanged 多発・初期 Load 時の重複 render を抑制
    private int _renderedSeriesNumber = -1;
    // 260506Cl 改: 各図のキャッシュキーに axis を畳み込む。Gen 側はさらに test point を含める。
    // 初期値はデフォルト (Empty Size, A) だが、初回呼び出しは _renderedSeriesNumber=-1 で seriesChanged=true となり強制再描画されるので問題ない。
    private (Size Size, ProjectionAxis Axis) _renderedKeyElem;
    private (Size Size, ProjectionAxis Axis, double X, double Y, double Z) _renderedKeyGen;
    // 260506Cl 追加: 点群追跡 (-1=未設定)、直前の ortho 状態 (false 初期値で初回 ortho 進入時に既定 C へ落とす)、および ChangeCrystal 中のイベント抑止フラグ。
    private int _previousPointGroupNumber = -1;
    private bool _previousIsOrtho;
    private bool SkipEvent;

    /// <summary>(260506Cl 追加) radioButtonDirection* の現在の選択を <see cref="ProjectionAxis"/> として取得。</summary>
    private ProjectionAxis SelectedDirection =>
        radioButtonDirectionA.Checked ? ProjectionAxis.A :
        radioButtonDirectionB.Checked ? ProjectionAxis.B : ProjectionAxis.C;

    /// <summary>(260506Cl 追加) <paramref name="axis"/> に該当する radioButtonDirection* を Checked に。
    /// 呼び出し側で <see cref="SkipEvent"/>=true にしておくこと (CheckedChanged 連鎖で UpdateDiagrams が走らないように)。</summary>
    private void SetSelectedDirection(ProjectionAxis axis)
    {
        radioButtonDirectionA.Checked = axis == ProjectionAxis.A;
        radioButtonDirectionB.Checked = axis == ProjectionAxis.B;
        radioButtonDirectionC.Checked = axis == ProjectionAxis.C;
    }

    /// <summary>260429Cl 追加: graphicsBoxSymmetryElements / graphicsBoxGeneralPositions を再描画する。
    /// ChangeCrystal および両 GraphicsBox の Resize から呼ばれる。Crystal 変化があれば両図、
    /// サイズだけが変わった場合はその箱だけを再描画して無駄な render を避ける。
    /// (260506Cl) 一般位置図は test point 変化でも再描画。対称要素図は test point に依存しないので不要。</summary>
    private void UpdateDiagrams()
    {
        int sn = Crystal.SymmetrySeriesNumber;
        bool seriesChanged = sn != _renderedSeriesNumber;
        _renderedSeriesNumber = sn;
        var axis = SelectedDirection;

        var keyElem = (graphicsBoxSymmetryElements.ClientSize, axis);
        if (seriesChanged || keyElem != _renderedKeyElem)
        {
            graphicsBoxSymmetryElements.Image?.Dispose();
            graphicsBoxSymmetryElements.Image = SymmetryDiagramElements.RenderSymmetryElements(sn, keyElem.ClientSize, axis);
            _renderedKeyElem = keyElem;
        }

        var testPoint = (numericBoxPositionA.Value, numericBoxPositionB.Value, numericBoxPositionC.Value);
        var keyGen = (graphicsBoxGeneralPositions.ClientSize, axis, testPoint.Item1, testPoint.Item2, testPoint.Item3);
        if (seriesChanged || keyGen != _renderedKeyGen)
        {
            graphicsBoxGeneralPositions.Image?.Dispose();
            graphicsBoxGeneralPositions.Image = SymmetryDiagramPositions.RenderGeneralPositions(sn, keyGen.ClientSize, axis, testPoint);
            _renderedKeyGen = keyGen;
        }
    }

    /// <summary>260506Cl 追加: numericBoxPosition* の ValueChanged に紐付く handler。
    /// ユーザーが test point を変更したら一般位置図のみを再描画する。
    /// ChangeCrystal 経由の reset 中は <see cref="SkipEvent"/> で抑止して 3 連発を防ぐ。</summary>
    private void numericBoxPosition_ValueChanged(object sender, EventArgs e)
    {
        if (SkipEvent) return;
        UpdateDiagrams();
    }

    /// <summary>260506Cl 追加: radioButtonDirection* の CheckedChanged に紐付く handler。
    /// 投影軸が切り替わったら対称要素図と一般位置図の両方を再描画する。
    /// ユーザー操作では radio ペアの off→on と on→off で CheckedChanged が二重発火するので、
    /// Checked 側 (= 新しく ON になった方) だけ反応させて UpdateDiagrams を 1 回に絞る。</summary>
    private void radioButtonDirection_CheckedChanged(object sender, EventArgs e)
    {
        if (SkipEvent) return;
        if (sender is RadioButton rb && !rb.Checked) return;
        UpdateDiagrams();
    }
    #endregion

    #region 面間隔の計算、軸間、軸面間の角度計算
    /// <summary>
    /// 平面 (h₁k₁l₁, h₂k₂l₂) と軸 (u₁v₁w₁, u₂v₂w₂) の入力欄が変化したとき、
    /// 面間距離・軸長・面間角・軸間角・面と軸のなす角・zone axis を再計算して各表示欄を更新する (260427Cl)。
    /// </summary>
    private void numericBox_ValueChanged(object sender, EventArgs e)
    {
        (int h,int k,int l) plane1 = indexControlPlane1.Values, plane2 = indexControlPlane2.Values;
        (int u, int v, int w) axis1 =indexControlAxis1.Values, axis2 = indexControlAxis2.Values;

        numericBoxLengthPlane1.Value = Crystal.GetLengthPlane(plane1.h, plane1.k, plane1.l) * 10; // (260427Ch)
        numericBoxLengthPlane2.Value = Crystal.GetLengthPlane(plane2.h, plane2.k, plane2.l) * 10; // (260427Ch)
        numericBoxLengthAxis1.Value = Crystal.GetLengthAxis(axis1.u, axis1.v, axis1.w) * 10; // (260427Ch)
        numericBoxLengthAxis2.Value = Crystal.GetLengthAxis(axis2.u, axis2.v, axis2.w) * 10; // (260427Ch)

        numericBoxAnglePlanes.Value = Crystal.GetAnglePlanes(plane1.h, plane1.k, plane1.l, plane2.h, plane2.k, plane2.l) * 180 / Math.PI; // (260427Ch)
        numericBoxAngleAxes.Value = Crystal.GetAngleAxes(axis1.u, axis1.v, axis1.w, axis2.u, axis2.v, axis2.w) * 180 / Math.PI; // (260427Ch)
        numericBoxAnglePlaneAxis1.Value = Crystal.GetAnglePlaneAxis(plane1.h, plane1.k, plane1.l, axis1.u, axis1.v, axis1.w) * 180 / Math.PI; // (260427Ch)
        numericBoxAnglePlaneAxis2.Value = Crystal.GetAnglePlaneAxis(plane2.h, plane2.k, plane2.l, axis2.u, axis2.v, axis2.w) * 180 / Math.PI; // (260427Ch)

        textBoxZoneAxis.Text = $"[{Crystal.GetZoneAxis(plane1.h, plane1.k, plane1.l, plane2.h, plane2.k, plane2.l)} ]";
        textBoxZonePlane.Text = $"({Crystal.GetZoneAxis(axis1.u, axis1.v, axis1.w, axis2.u, axis2.v, axis2.w)} )";
    }
    #endregion

    #region ワイコフ位置の設定
    /// <summary>現在の空間群の lattice centering と Wyckoff position を Wyckoff MiniTable へ書き込む。260706Ch 変更。</summary>
    /// <remarks> 1 ポジションあたり座標が 4 個を超える場合は 4 個ずつ複数行に分割して追加する。</remarks>
    private void SetWyckoffPosition()
    {
        //var table = dataSet.Tables[0]; // (260426Ch)
        //table.Clear();
        var rows = new List<object[]>(); // 260706Ch: DataGridView/DataSet ではなく MiniTable へ直接投入
        var centeringRow = Crystal.Symmetry.LatticeTypeStr switch
        {
            //"P" => new object[] { "-", "-", "-", "(0,0,0)+", "", "", "" },
            "P" => new object[] { "-", "-", "-", CoordinateToLatex("(0,0,0)+"), "", "", "" }, // 260706Ch
            //"A" => ["-", "-", "-", "(0,0,0)+", "(0,1/2,1/2)+", "", ""],
            //"B" => ["-", "-", "-", "(0,0,0)+", "(1/2,0,1/2)+", "", ""],
            //"C" => ["-", "-", "-", "(0,0,0)+", "(1/2,1/2,0)+", "", ""],
            //"F" => ["-", "-", "-", "(0,0,0)+", "(0,1/2,1/2)+", "(1/2,0,1/2)+", "(1/2,1/2,0)+"], // (260426Ch) 3 番目の F centering 座標 typo を修正
            //"I" => ["-", "-", "-", "(0,0,0)+", "(1/2,1/2,1/2)+", "", ""],
            //"H" => ["-", "-", "-", "(0,0,0)+", "(1/3,2/3,2/3)+", "(2/3,1/3,1/3)+", ""],
            "A" => ["-", "-", "-", CoordinateToLatex("(0,0,0)+"), CoordinateToLatex("(0,1/2,1/2)+"), "", ""], // 260706Ch
            "B" => ["-", "-", "-", CoordinateToLatex("(0,0,0)+"), CoordinateToLatex("(1/2,0,1/2)+"), "", ""], // 260706Ch
            "C" => ["-", "-", "-", CoordinateToLatex("(0,0,0)+"), CoordinateToLatex("(1/2,1/2,0)+"), "", ""], // 260706Ch
            "F" => ["-", "-", "-", CoordinateToLatex("(0,0,0)+"), CoordinateToLatex("(0,1/2,1/2)+"), CoordinateToLatex("(1/2,0,1/2)+"), CoordinateToLatex("(1/2,1/2,0)+")], // 260706Ch
            "I" => ["-", "-", "-", CoordinateToLatex("(0,0,0)+"), CoordinateToLatex("(1/2,1/2,1/2)+"), "", ""], // 260706Ch
            "H" => ["-", "-", "-", CoordinateToLatex("(0,0,0)+"), CoordinateToLatex("(1/3,2/3,2/3)+"), CoordinateToLatex("(2/3,1/3,1/3)+"), ""], // 260706Ch
            _ => null
        };
        if (centeringRow != null)
            //table.Rows.Add(centeringRow);
            rows.Add(centeringRow); // 260706Ch

        Crystal.Symmetry = SymmetryStatic.Symmetries[Crystal.SymmetrySeriesNumber];

        foreach (var position in SymmetryStatic.WyckoffPositions[Crystal.SymmetrySeriesNumber])
        {
            var positions = position.PositionStr;
            int len = positions.Length;
            for (int j = 0; j < len; j += 4)
            {
                var row = new object[7];
                if (j == 0)
                {
                    row[0] = position.Multiplicity;
                    //row[1] = position.WyckoffLetter; // 260708Ch: Wyckoff letter 列も LaTeX セルで描画する
                    //row[1] = $@"\mathrm{{{position.WyckoffLetter}}}"; // 260708Ch: Wyckoff letter は結晶学表記に合わせて italic に戻す
                    row[1] = position.WyckoffLetter; // 260708Ch
                    //row[2] = position.SiteSymmetry;
                    row[2] = SiteSymmetryToLatex(position.SiteSymmetry); // 260706Ch
                }
                else
                {
                    row[0] = row[1] = row[2] = "";
                }
                for (int offset = 0; offset < 4; offset++)
                    //row[3 + offset] = j + offset < len ? positions[j + offset] : "";
                    row[3 + offset] = j + offset < len ? CoordinateToLatex(positions[j + offset]) : ""; // 260706Ch

                //table.Rows.Add(row);
                rows.Add(row); // 260706Ch
            }
        }

        miniTableWyckoff.SetRows(rows); // 260706Ch
    }
    #endregion

    #region Operations / Properties / Settings タブ (260704Cl 追加, Phase 1-D/E/G/H)

    // 260705Cl 追加: 3 表を最後に構築した SymmetrySeriesNumber。表の内容はこれのみに依存するため、変化時だけ再構築する。
    private int _extraTablesSeriesNumber = -1;

    // 260705Cl: tooltip 行 index を控えるフィールド _propertyTooltips は SetPropertiesTable 内で完結していたため削除 (ローカル化)。
    //private readonly List<(int Row, string Tip)> _propertyTooltips = new(4);

    /// <summary>3 つの新規タブ (Operations/Properties/Settings) の列定義を一度だけ構築する。ヘッダは Loc() で多言語化 (方式②)。</summary>
    private void SetupExtraTables()
    {
        const DataGridViewContentAlignment L = DataGridViewContentAlignment.MiddleLeft;
        const DataGridViewContentAlignment R = DataGridViewContentAlignment.MiddleRight;
        const DataGridViewContentAlignment C = DataGridViewContentAlignment.MiddleCenter;

        miniTableWyckoff.SetColumns( // 260706Ch: Wyckoff タブも MiniTable + LaTeX セルへ移行
            new MiniTable.Col(Loc(en: "Mult.", ja: "多重度", de: "Mult.", fr: "Mult.", es: "Mult.", pt: "Mult.", it: "Mult.", ru: "Кратн.", zhHans: "重数", zhHant: "重數", ko: "다중도"), R),
            //new MiniTable.Col(Loc(en: "Wyck. Let.", ja: "記号", de: "Wyck.-Buchst.", fr: "Lettre Wyck.", es: "Letra Wyck.", pt: "Letra Wyck.", it: "Let. Wyck.", ru: "Буква", zhHans: "Wyck. 字母", zhHant: "Wyck. 字母", ko: "Wyck. 문자"), C), // 260708Ch: Wyckoff letter も LaTeX 描画へ
            new MiniTable.Col(Loc(en: "Wyck. Let.", ja: "記号", de: "Wyck.-Buchst.", fr: "Lettre Wyck.", es: "Letra Wyck.", pt: "Letra Wyck.", it: "Let. Wyck.", ru: "Буква", zhHans: "Wyck. 字母", zhHant: "Wyck. 字母", ko: "Wyck. 문자"), C, Latex: true), // 260708Ch
            new MiniTable.Col(Loc(en: "Site Sym.", ja: "サイト対称性", de: "Lagesym.", fr: "Sym. site", es: "Sim. sitio", pt: "Sim. sítio", it: "Simm. sito", ru: "Симм. поз.", zhHans: "位置对称", zhHant: "位置對稱", ko: "자리 대칭"), C, Latex: true),
            new MiniTable.Col(Loc(en: "Coordinates", ja: "座標", de: "Koordinaten", fr: "Coordonnées", es: "Coordenadas", pt: "Coordenadas", it: "Coordinate", ru: "Координаты", zhHans: "坐标", zhHant: "座標", ko: "좌표"), L, Latex: true),
            new MiniTable.Col("", L, Latex: true),
            new MiniTable.Col("", L, Latex: true),
            new MiniTable.Col("", L, Fill: true, Latex: true));

        miniTableConditions.SetColumns( // 260708Ch: Conditions タブを MiniTable + LaTeX セルへ移行
            //new MiniTable.Col(Loc(en: "Condition", ja: "条件", de: "Bedingung", fr: "Condition", es: "Condición", pt: "Condição", it: "Condizione", ru: "Условие", zhHans: "条件", zhHant: "條件", ko: "조건"), L, Fill: true, Latex: true)); // 260708Ch: コロン区切りの 3 情報を列分割
            new MiniTable.Col(Loc(en: "Indices", ja: "指数", de: "Indizes", fr: "Indices", es: "Índices", pt: "Índices", it: "Indici", ru: "Индексы", zhHans: "指数", zhHant: "指數", ko: "지수"), C, Latex: true), // 260708Ch
            new MiniTable.Col(Loc(en: "Condition", ja: "条件", de: "Bedingung", fr: "Condition", es: "Condición", pt: "Condição", it: "Condizione", ru: "Условие", zhHans: "条件", zhHant: "條件", ko: "조건"), L, Latex: true), // 260708Ch
            new MiniTable.Col(Loc(en: "Element", ja: "要素", de: "Element", fr: "Élément", es: "Elemento", pt: "Elemento", it: "Elemento", ru: "Элемент", zhHans: "要素", zhHant: "要素", ko: "요소"), L, Fill: true, Latex: true)); // 260708Ch

        miniTableOperations.SetColumns(
            new MiniTable.Col("#", R),
            //new MiniTable.Col(Loc(en: "Coordinates", ja: "座標", de: "Koordinaten", fr: "Coordonnées", es: "Coordenadas", pt: "Coordenadas", it: "Coordinate", ru: "Координаты", zhHans: "坐标", zhHant: "座標", ko: "좌표"), L),
            //new MiniTable.Col("Seitz", L),
            new MiniTable.Col(Loc(en: "Coordinates", ja: "座標", de: "Koordinaten", fr: "Coordonnées", es: "Coordenadas", pt: "Coordenadas", it: "Coordinate", ru: "Координаты", zhHans: "坐标", zhHant: "座標", ko: "좌표"), L, Latex: true), // 260706Ch
            new MiniTable.Col("Seitz", L, Latex: true), // 260706Ch
            new MiniTable.Col(Loc(en: "Type", ja: "種類", de: "Typ", fr: "Type", es: "Tipo", pt: "Tipo", it: "Tipo", ru: "Тип", zhHans: "类型", zhHant: "類型", ko: "종류"), L, Fill: true));

        miniTableProperties.SetColumns(
            new MiniTable.Col(Loc(en: "Property", ja: "項目", de: "Eigenschaft", fr: "Propriété", es: "Propiedad", pt: "Propriedade", it: "Proprietà", ru: "Свойство", zhHans: "项目", zhHant: "項目", ko: "항목"), L),
            new MiniTable.Col(Loc(en: "Value", ja: "値", de: "Wert", fr: "Valeur", es: "Valor", pt: "Valor", it: "Valore", ru: "Значение", zhHans: "值", zhHant: "值", ko: "값"), L, Fill: true));

        miniTableSettings.SetColumns(
            new MiniTable.Col("", C),
            //new MiniTable.Col(Loc(en: "HM symbol", ja: "HM 記号", de: "HM-Symbol", fr: "Symbole HM", es: "Símbolo HM", pt: "Símbolo HM", it: "Simbolo HM", ru: "Символ HM", zhHans: "HM 符号", zhHant: "HM 符號", ko: "HM 기호"), L),
            //new MiniTable.Col(Loc(en: "Hall symbol", ja: "Hall 記号", de: "Hall-Symbol", fr: "Symbole Hall", es: "Símbolo Hall", pt: "Símbolo Hall", it: "Simbolo Hall", ru: "Символ Hall", zhHans: "Hall 符号", zhHant: "Hall 符號", ko: "Hall 기호"), L, Fill: true));
            new MiniTable.Col(Loc(en: "HM symbol", ja: "HM 記号", de: "HM-Symbol", fr: "Symbole HM", es: "Símbolo HM", pt: "Símbolo HM", it: "Simbolo HM", ru: "Символ HM", zhHans: "HM 符号", zhHant: "HM 符號", ko: "HM 기호"), L, Latex: true), // 260706Ch
            new MiniTable.Col(Loc(en: "Hall symbol", ja: "Hall 記号", de: "Hall-Symbol", fr: "Symbole Hall", es: "Símbolo Hall", pt: "Símbolo Hall", it: "Simbolo Hall", ru: "Символ Hall", zhHans: "Hall 符号", zhHant: "Hall 符號", ko: "Hall 기호"), L, Fill: true, Latex: true)); // 260706Ch
    }

    /// <summary>現在の空間群の一般位置の全対称操作を Operations タブに流し込む (中心化展開済み)。</summary>
    private void SetOperationsTable()
    {
        // 260705Cl: 「SeriesNumber 付替え展開」を TSubgroupFinder.GetExpandedOps に一本化 (buttonCopyCif_Click / SymmetryProperties と共用)。
        //int sn = Crystal.SymmetrySeriesNumber;
        //var raw = SymmetryStatic.WyckoffPositions[sn][0].PositionOperations;
        //if (raw == null) { miniTableOperations.ClearRows(); return; }
        var ops = TSubgroupFinder.GetExpandedOps(Crystal.SymmetrySeriesNumber);
        if (ops.Length == 0) { miniTableOperations.ClearRows(); return; }

        var rows = new List<object[]>(ops.Length);
        for (int i = 0; i < ops.Length; i++)
            //rows.Add([i + 1, CoordinateToLatex(SeitzNotation.Triplet(ops[i])), SeitzToLatex(SeitzNotation.Seitz(ops[i])), SeitzNotation.GeometricType(ops[i])]); // 260708Ch: SeitzNotation.SeitzLatex に一本化 (構造化データから直接生成)
            rows.Add([i + 1, CoordinateToLatex(SeitzNotation.Triplet(ops[i])), SeitzNotation.SeitzLatex(ops[i]), SeitzNotation.GeometricType(ops[i])]); // 260708Ch
        miniTableOperations.SetRows(rows);
    }

    /// <summary>群論的性質 + 物性の対称性許容を Properties タブに流し込む。物性行には 1 行説明の tooltip を付ける。</summary>
    private void SetPropertiesTable(Symmetry symmetry)
    {
        var p = new SymmetryProperties(symmetry);

        string yes = Loc(en: "Yes", ja: "はい", de: "Ja", fr: "Oui", es: "Sí", pt: "Sim", it: "Sì", ru: "Да", zhHans: "是", zhHant: "是", ko: "예");
        string no = Loc(en: "No", ja: "いいえ", de: "Nein", fr: "Non", es: "No", pt: "Não", it: "No", ru: "Нет", zhHans: "否", zhHant: "否", ko: "아니오");
        string allowed = Loc(en: "Allowed", ja: "許容", de: "Erlaubt", fr: "Autorisé", es: "Permitido", pt: "Permitido", it: "Permesso", ru: "Разрешено", zhHans: "允许", zhHant: "允許", ko: "허용");
        string forbidden = Loc(en: "Forbidden", ja: "禁止", de: "Verboten", fr: "Interdit", es: "Prohibido", pt: "Proibido", it: "Vietato", ru: "Запрещено", zhHans: "禁止", zhHant: "禁止", ko: "금지");
        string YN(bool b) => b ? yes : no;
        string AF(bool b) => b ? allowed : forbidden;

        // 260705Cl: Loc への単純パススルーだったローカル関数 P を削除 (Loc の引数順は en, ja, de, fr, es, pt,
        // it, ru, zhHans, zhHant, ko で同一のため、各行から位置引数で直接呼ぶ)。

        string partner = p.HasEnantiomorph ? $"No. {p.EnantiomorphPartnerNumber}" : "—";
        string polarVal = p.IsPolar ? $"{yes} ({p.PolarDirectionStr})" : no;

        object[][] rows =
        [
            [Loc("General-position multiplicity", "一般位置の多重度", "Zähligkeit der allg. Lage", "Multiplicité position générale", "Multiplicidad posición general", "Multiplicidade posição geral", "Molteplicità posizione generale", "Кратность общей позиции", "一般位置多重性", "一般位置多重度", "일반 위치 중복도"), p.GeneralMultiplicity],
            [Loc("Point-group order", "点群の位数", "Ordnung der Punktgruppe", "Ordre du groupe ponctuel", "Orden del grupo puntual", "Ordem do grupo pontual", "Ordine del gruppo puntuale", "Порядок точечной группы", "点群阶数", "點群階數", "점군 위수"), p.PointGroupOrder],
            [Loc("Centrosymmetric", "中心対称", "Zentrosymmetrisch", "Centrosymétrique", "Centrosimétrico", "Centrossimétrico", "Centrosimmetrico", "Центросимметричная", "中心对称", "中心對稱", "중심대칭"), YN(p.IsCentrosymmetric)],
            [Loc("Sohncke (chiral) group", "Sohncke 群 (キラル)", "Sohncke-Gruppe (chiral)", "Groupe de Sohncke (chiral)", "Grupo de Sohncke (quiral)", "Grupo de Sohncke (quiral)", "Gruppo di Sohncke (chirale)", "Группа Зонке (хиральная)", "Sohncke 群 (手性)", "Sohncke 群 (手性)", "손케 군 (카이랄)"), YN(p.IsSohncke)],
            [Loc("Symmorphic", "Symmorphic", "Symmorph", "Symmorphique", "Simórfico", "Simórfico", "Simmorfico", "Симморфная", "简单型", "簡單型", "심모픽"), YN(p.IsSymmorphic)],
            [Loc("Polar", "極性", "Polar", "Polaire", "Polar", "Polar", "Polare", "Полярная", "极性", "極性", "극성"), polarVal],
            [Loc("Enantiomorphic partner", "掌性対の相手", "Enantiomorphes Paar", "Partenaire énantiomorphe", "Pareja enantiomorfa", "Par enantiomorfo", "Coppia enantiomorfa", "Энантиоморфная пара", "对映体伙伴", "對映體夥伴", "거울상 짝"), partner],
            [Loc("Crystal family", "結晶族", "Kristallfamilie", "Famille cristalline", "Familia cristalina", "Família cristalina", "Famiglia cristallina", "Кристаллическое семейство", "晶族", "晶族", "결정족"), p.CrystalFamilyStr],
            [Loc("Lattice system", "格子系", "Gittersystem", "Système réticulaire", "Sistema reticular", "Sistema reticular", "Sistema reticolare", "Решёточная система", "格子系", "格子系", "격자계"), p.LatticeSystemStr],
            [Loc("Bravais type", "ブラベー型", "Bravais-Typ", "Type de Bravais", "Tipo de Bravais", "Tipo de Bravais", "Tipo di Bravais", "Тип Браве", "布拉维型", "布拉維型", "브라베 형"), p.BravaisTypeStr],
            [Loc("Arithmetic crystal class", "算術結晶類", "Arithmetische Kristallklasse", "Classe cristalline arithmétique", "Clase cristalina aritmética", "Classe cristalina aritmética", "Classe cristallina aritmetica", "Арифметический класс", "算术晶类", "算術晶類", "산술 결정류"), p.ArithmeticCrystalClassStr],
            [Loc("Patterson symmetry", "Patterson 対称", "Patterson-Symmetrie", "Symétrie de Patterson", "Simetría de Patterson", "Simetria de Patterson", "Simmetria di Patterson", "Симметрия Паттерсона", "Patterson 对称", "Patterson 對稱", "패터슨 대칭"), p.PattersonSymmetryStr],
            // --- 物性 (点群対称性で許容されるか) ---
            [Loc("Pyroelectric / ferroelectric", "焦電性 / 強誘電性", "Pyroelektrisch / ferroelektrisch", "Pyroélectrique / ferroélectrique", "Piroeléctrico / ferroeléctrico", "Piroelétrico / ferroelétrico", "Piroelettrico / ferroelettrico", "Пироэлектрик / сегнетоэлектрик", "热电 / 铁电", "熱電 / 鐵電", "초전 / 강유전"), AF(p.PyroelectricAllowed)],
            [Loc("Piezoelectric", "圧電性", "Piezoelektrisch", "Piézoélectrique", "Piezoeléctrico", "Piezoelétrico", "Piezoelettrico", "Пьезоэлектрик", "压电", "壓電", "압전"), AF(p.PiezoelectricAllowed)],
            [Loc("Second-harmonic generation", "第二高調波発生 (SHG)", "Frequenzverdopplung (SHG)", "Génération de seconde harmonique", "Generación de segundo armónico", "Geração de segundo harmónico", "Generazione di seconda armonica", "Генерация второй гармоники", "二次谐波 (SHG)", "二次諧波 (SHG)", "제2고조파 발생"), AF(p.SHGAllowed)],
            [Loc("Optical activity", "旋光性", "Optische Aktivität", "Activité optique", "Actividad óptica", "Atividade óptica", "Attività ottica", "Оптическая активность", "旋光性", "旋光性", "선광성"), AF(p.OpticalActivityAllowed)],
        ];
        miniTableProperties.SetRows(rows);

        // 物性 4 行 (末尾) の value セルに 1 行説明を貼る (教育用途)。260705Cl: フィールド _propertyTooltips を廃しローカル配列で末尾 4 行に適用。
        string[] tips =
        [
            Loc(en: "A non-zero spontaneous polarization is allowed only in the 10 polar point groups.", ja: "自発分極は 10 個の極性点群でのみ許容されます。", de: "Eine spontane Polarisation ist nur in den 10 polaren Punktgruppen erlaubt.", fr: "Une polarisation spontanée n'est permise que dans les 10 groupes polaires.", es: "La polarización espontánea solo se permite en los 10 grupos polares.", pt: "A polarização espontânea só é permitida nos 10 grupos polares.", it: "La polarizzazione spontanea è permessa solo nei 10 gruppi polari.", ru: "Спонтанная поляризация возможна только в 10 полярных группах.", zhHans: "自发极化仅在 10 个极性点群中允许。", zhHant: "自發極化僅在 10 個極性點群中允許。", ko: "자발 분극은 10개의 극성 점군에서만 허용됩니다."),
            Loc(en: "Requires a non-centrosymmetric point group (except 432); 20 classes.", ja: "非中心対称の点群 (432 を除く) で許容されます。20 類。", de: "Erfordert eine nicht-zentrosymmetrische Punktgruppe (außer 432); 20 Klassen.", fr: "Nécessite un groupe non centrosymétrique (sauf 432) ; 20 classes.", es: "Requiere un grupo no centrosimétrico (excepto 432); 20 clases.", pt: "Requer um grupo não centrossimétrico (exceto 432); 20 classes.", it: "Richiede un gruppo non centrosimmetrico (tranne 432); 20 classi.", ru: "Требует нецентросимметричной группы (кроме 432); 20 классов.", zhHans: "需非中心对称点群 (432 除外)；共 20 类。", zhHant: "需非中心對稱點群 (432 除外)；共 20 類。", ko: "비중심대칭 점군(432 제외)에서 허용; 20류."),
            Loc(en: "χ⁽²⁾ is a rank-3 polar tensor with the same symmetry condition as piezoelectricity.", ja: "χ⁽²⁾ は 3 階の極性テンソルで、許容条件は圧電性と同じです。", de: "χ⁽²⁾ ist ein polarer Tensor 3. Stufe mit derselben Bedingung wie Piezoelektrizität.", fr: "χ⁽²⁾ est un tenseur polaire de rang 3, même condition que la piézoélectricité.", es: "χ⁽²⁾ es un tensor polar de rango 3, misma condición que la piezoelectricidad.", pt: "χ⁽²⁾ é um tensor polar de posto 3, mesma condição da piezoeletricidade.", it: "χ⁽²⁾ è un tensore polare di rango 3, stessa condizione della piezoelettricità.", ru: "χ⁽²⁾ — полярный тензор 3-го ранга, условие как у пьезоэлектричества.", zhHans: "χ⁽²⁾ 为三阶极性张量，条件与压电性相同。", zhHant: "χ⁽²⁾ 為三階極性張量，條件與壓電性相同。", ko: "χ⁽²⁾는 3계 극성 텐서로 압전성과 같은 조건입니다."),
            Loc(en: "The gyration (axial rank-2) tensor is non-zero in 15 gyrotropic point groups.", ja: "旋光テンソル (2 階軸性) は 15 個の旋光点群で非零です。", de: "Der Gyrationstensor (axial, 2. Stufe) ist in 15 gyrotropen Punktgruppen ungleich null.", fr: "Le tenseur de gyration (axial rang 2) est non nul dans 15 groupes gyrotropes.", es: "El tensor de giración (axial rango 2) es no nulo en 15 grupos girotrópicos.", pt: "O tensor de giração (axial posto 2) é não nulo em 15 grupos girotrópicos.", it: "Il tensore di girazione (assiale rango 2) è non nullo in 15 gruppi girotropi.", ru: "Тензор гирации (аксиальный 2-го ранга) ненулевой в 15 гиротропных группах.", zhHans: "旋光张量 (二阶轴性) 在 15 个旋光点群中非零。", zhHant: "旋光張量 (二階軸性) 在 15 個旋光點群中非零。", ko: "자이레이션(축성 2계) 텐서는 15개 자이로트로픽 점군에서 0이 아닙니다."),
        ];
        for (int i = 0; i < tips.Length; i++)
            miniTableProperties.Rows[rows.Length - tips.Length + i].Cells[1].ToolTipText = tips[i];
    }

    /// <summary>同一空間群タイプ (同じ IT 番号) の全設定を Settings タブに一覧表示する (Phase 1 は閲覧のみ)。</summary>
    private void SetSettingsTable(Symmetry symmetry)
    {
        int itno = symmetry.SpaceGroupNumber;
        int cur = Crystal.SymmetrySeriesNumber;
        var rows = new List<object[]>();
        for (int s = 0; s < SymmetryStatic.TotalSpaceGroupNumber; s++)
        {
            var sym = SymmetryStatic.Symmetries[s];
            if (sym.SpaceGroupNumber != itno) continue;
            //rows.Add([s == cur ? "▶" : "", SeitzNotation.PrettyHM(sym.SpaceGroupHMStr), sym.SpaceGroupHallStr]);
            rows.Add([s == cur ? "▶" : "", ToLatex(sym.SpaceGroupHMStr, spaced: true), ToLatex(sym.SpaceGroupHallStr, spaced: true)]); // 260706Ch
        }
        miniTableSettings.SetRows(rows);
    }

    // 260705Cl: FormGroupRelations と一字一句同一だった PrettyHM を SeitzNotation.PrettyHM へ集約 (実装は移設)。

    /// <summary>Operations タブの全対称操作を CIF の _space_group_symop_operation_xyz ループとしてクリップボードへコピー。</summary>
    private void buttonCopyCif_Click(object sender, EventArgs e)
    {
        // 260705Cl: 展開処理を TSubgroupFinder.GetExpandedOps に一本化。
        //int sn = Crystal.SymmetrySeriesNumber;
        //var raw = SymmetryStatic.WyckoffPositions[sn][0].PositionOperations;
        //if (raw == null || raw.Length == 0) return;
        //var ops = raw.Select(o => new SymmetryOperation(o, sn)).ToList();
        var ops = TSubgroupFinder.GetExpandedOps(Crystal.SymmetrySeriesNumber);
        if (ops.Length == 0) return;
        Clipboard.SetText(SeitzNotation.ToCifSymopLoop(ops));
    }

    // 260704Cl 追加 (Phase 2): group-subgroup 関係ブラウザ。閉じても Hide されるので 1 インスタンスを再利用する。
    private FormGroupRelations _formGroupRelations;

    private void buttonGroupRelations_Click(object sender, EventArgs e) => ShowGroupRelations();

    /// <summary>Crystal.SymmetrySeriesNumber で FormGroupRelations を開く (ボタン押下と --capture の crystal-dependent
    /// 子フォーム列挙 (FormMain.EnumerateCaptureCrystalDependentForms) で共用)。260705Cl 追加。</summary>
    public FormGroupRelations ShowGroupRelations()
    {
        if (_formGroupRelations == null || _formGroupRelations.IsDisposed)
            _formGroupRelations = new FormGroupRelations();
        _formGroupRelations.LoadSpaceGroup(Crystal.SymmetrySeriesNumber, isCurrentCrystal: true);
        if (_formGroupRelations.WindowState == FormWindowState.Minimized)
            _formGroupRelations.WindowState = FormWindowState.Normal;
        _formGroupRelations.Show();
        _formGroupRelations.BringToFront();
        return _formGroupRelations;
    }
    #endregion

    #region 対称要素図 / 一般位置図のクリップボードコピー (260504Cl 追加)

    private void buttonCopySymmetryElements_Click(object sender, EventArgs e)
    {
        int sn = Crystal.SymmetrySeriesNumber;
        var size = graphicsBoxSymmetryElements.ClientSize;
        var axis = SelectedDirection;
        if (radioButtonEmf.Checked)
            CopyAsMetafile(g => SymmetryDiagramElements.DrawSymmetryElements(g, size, sn, axis));
        else
            Clipboard.SetDataObject(SymmetryDiagramElements.RenderSymmetryElements(sn, size, axis), true);
    }

    private void buttonCopyGeneralPositions_Click(object sender, EventArgs e)
    {
        int sn = Crystal.SymmetrySeriesNumber;
        var size = graphicsBoxGeneralPositions.ClientSize;
        var axis = SelectedDirection;
        var testPoint = (numericBoxPositionA.Value, numericBoxPositionB.Value, numericBoxPositionC.Value);
        if (radioButtonEmf.Checked)
            CopyAsMetafile(g => SymmetryDiagramPositions.DrawGeneralPositions(g, size, sn, axis, testPoint));
        else
            Clipboard.SetDataObject(SymmetryDiagramPositions.RenderGeneralPositions(sn, size, axis, testPoint), true);
    }

    /// <summary>EMF+ クリップボードコピーの共通設定 (背景白・AntiAlias) を済ませてから <paramref name="drawDiagram"/> を呼ぶ。</summary>
    private void CopyAsMetafile(Action<Graphics> drawDiagram)
        => ClipboardMetafileHelper.PutDrawingOnClipboardAsEnhMetafile(Handle, g =>
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.Clear(Color.White);
            drawDiagram(g);
        });
    #endregion
}
