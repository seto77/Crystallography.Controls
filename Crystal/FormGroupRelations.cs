// 260704Cl 新規: 空間群の group-subgroup 関係ブラウザ (Phase 2)。
// Pattern A (ツリー+タブ詳細) を骨格に、Pattern C (Bärnighausen グラフ=Diagram タブ) と
// Pattern B (軌道分裂・双晶インスペクタ) を統合。translationengleiche (t-) 部分群/超群は
// TSubgroupFinder が既存の対称操作データから実行時に厳密計算する (型同定は操作集合の完全一致で検証済み)。
// klassengleiche (k-) / isomorphic は将来の KSubgroupFinder (実行時エンジン、Phase 2c) 待ちのプレースホルダ表示に留める。
// (旧: 埋め込み CSV パイプライン。t- の実行時化成功を受け k- も同方式へ方針変更。詳細=.project-guidance/ReciPro_FormGroupRelations改修計画.md)
#region using, namespace
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks; // 260705Cl 追加: 超群索引のバックグラウンド構築
using System.Windows.Forms;
using static Crystallography.Localization; // コード側多言語化 Loc() (方式②)
#endregion

namespace Crystallography.Controls;

public partial class FormGroupRelations : FormBase
{
    #region フィールド
    /// <summary>現在の結晶の空間群 (通し番号)。Home / コンテキストバナー判定の基準。</summary>
    private int _crystalSeries = -1;
    /// <summary>いま閲覧中の空間群 (通し番号)。</summary>
    private int _currentSeries = -1;
    private readonly Stack<int> _back = new();
    private readonly Stack<int> _forward = new();

    // 現在閲覧中の群の関係 (グラフ・詳細タブ共有)。
    private TSubgroup[] _subs = [];
    private IReadOnlyList<TSubgroupFinder.TSupergroup> _supers = [];
    private bool _supersPending; // 260705Cl 追加: 超群索引をバックグラウンド構築中 (ツリーに「計算中…」を表示)
    private TSubgroup _selectedRelation;   // ツリー/グラフで選択中の t-部分群 (null 可)

    // グラフのヒットテスト用ノード矩形 (画面座標) と対応 series。
    private readonly List<(Rectangle Rect, int Series)> _graphNodes = [];
    #endregion

    #region コンストラクタ / 初期化
    public FormGroupRelations()
    {
        InitializeComponent();
        HelpPage = "2-symmetry-information"; // F1: Symmetry Information マニュアル
        if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
        {
            LocalizeLabels();
            SetupTables();
            // ×ボタンでは閉じずに非表示にしてインスタンスを再利用する (他の派生フォームと同じ方針)。
            FormClosing += (_, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    Hide();
                }
            };
        }
    }

    /// <summary>ブラウザを指定空間群で開く (エントリポイント)。<paramref name="isCurrentCrystal"/>=true の呼び出しで
    /// Home / コンテキスト基準を更新する。冪等: 履歴は都度リセットする。</summary>
    public void LoadSpaceGroup(int seriesNumber, bool isCurrentCrystal)
    {
        // 260705Cl 追加: Show() 前 (ハンドル未生成) に呼ばれると、NavigateTo→BuildTree の TreeNode.Expand() が
        // ネイティブ側へ反映されずルートノードが折りたたまれたまま表示される実バグ (--capture で目視発覚)。
        // Control.CreateControl() は Visible=false だと何もしないため、対象コントロールの Handle を直接参照して
        // (Visible に関係なくハンドル生成を強制する) 先に用意しておく。
        if (!treeRelations.IsHandleCreated)
            _ = treeRelations.Handle;
        if (isCurrentCrystal)
            _crystalSeries = seriesNumber;
        _back.Clear();
        _forward.Clear();
        NavigateTo(seriesNumber, pushHistory: false);
    }
    #endregion

    #region --capture 用 (GuiCapture の代表状態撮影) 260705Cl 追加
    /// <summary>--capture 用: 最初の t-部分群ノードを選択する (AfterSelect 経由で詳細タブ全部が populate される)。
    /// プレースホルダ「ツリーから選択してください」でなく実データの見た目を確認できるようにする。</summary>
    public void PrepareCaptureForGuiAudit()
    {
        foreach (TreeNode root in treeRelations.Nodes)
            foreach (TreeNode category in root.Nodes)
                foreach (TreeNode leaf in category.Nodes)
                    if (leaf.Tag is NodeTag { Relation: not null })
                    {
                        treeRelations.SelectedNode = leaf;
                        return;
                    }
    }

    /// <summary>--capture 用: Diagram タブ (Bärnighausen グラフ) へのクロップ撮影のためタブ切替を公開する。</summary>
    public TabControl CaptureTabControl => tabDetail;
    #endregion

    #region ナビゲーション
    /// <summary>閲覧対象を切り替え、ツリー・詳細・グラフを再構築する。</summary>
    private void NavigateTo(int seriesNumber, bool pushHistory = true)
    {
        if (seriesNumber < 0 || seriesNumber >= SymmetryStatic.TotalSpaceGroupNumber)
            return;
        if (pushHistory && _currentSeries >= 0)
        {
            _back.Push(_currentSeries);
            _forward.Clear();
        }
        _currentSeries = seriesNumber;

        var sym = SymmetryStatic.Symmetries[seriesNumber];
        _subs = TSubgroupFinder.GetMaximalTSubgroups(seriesNumber);
        // 260705Cl 修正: 超群逆引きは初回呼び出しで全 230 タイプの部分群索引を同期構築し (Release 実測 ~8 s)、
        // フォーム初回オープンで UI をブロックしていた。未構築ならバックグラウンドで構築し、完了時に
        // まだ同じ群を表示していればツリーとグラフだけ差し替える。構築済みなら従来どおり即時取得。
        //_supers = TSubgroupFinder.GetMinimalTSupergroups(sym.SpaceGroupNumber);
        if (TSubgroupFinder.SupergroupIndexReady)
        {
            _supers = TSubgroupFinder.GetMinimalTSupergroups(sym.SpaceGroupNumber);
            _supersPending = false;
        }
        else
        {
            _supers = [];
            _supersPending = true;
            int it = sym.SpaceGroupNumber;
            Task.Run(() => TSubgroupFinder.GetMinimalTSupergroups(it)).ContinueWith(t =>
            {
                _supersPending = false;
                if (!t.IsFaulted && !IsDisposed && _currentSeries == seriesNumber)
                {
                    _supers = t.Result;
                    // 260705Cl 追加: フォームが非表示 (Hide) の間はツリー/グラフを再構築しない。ユーザーが見ていない
                    // UI を触っても意味が無く、非表示中の control 更新でハンドル生成に失敗する例外を実際に観測した
                    // (--capture ハーネスでの検証)。次に NavigateTo/LoadSpaceGroup されたとき最新の _supers で再構築される。
                    if (Visible)
                    {
                        BuildTree();
                        RenderGraph();
                    }
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        _selectedRelation = null;

        BuildTree();
        UpdateBreadcrumb();
        UpdateNavButtons();
        RenderGraph();
        ShowRelationDetail(null);
    }

    private void UpdateNavButtons()
    {
        buttonBack.Enabled = _back.Count > 0;
        buttonForward.Enabled = _forward.Count > 0;
    }

    private void UpdateBreadcrumb()
    {
        var sym = SymmetryStatic.Symmetries[_currentSeries];
        labelBreadcrumb.Text = $"{SeitzNotation.PrettyHM(sym.SpaceGroupHMStr)}   (No. {sym.SpaceGroupNumber})";

        bool isCurrent = _currentSeries == _crystalSeries;
        if (isCurrent)
        {
            labelContext.Text = "● " + Loc(en: "Viewing the current crystal's space group.", ja: "現在の結晶の空間群を表示しています。", de: "Zeigt die Raumgruppe des aktuellen Kristalls.", fr: "Affichage du groupe d'espace du cristal actuel.", es: "Mostrando el grupo espacial del cristal actual.", pt: "Mostrando o grupo espacial do cristal atual.", it: "Visualizzazione del gruppo spaziale del cristallo attuale.", ru: "Показана пространственная группа текущего кристалла.", zhHans: "正在显示当前晶体的空间群。", zhHant: "正在顯示目前晶體的空間群。", ko: "현재 결정의 공간군을 표시 중입니다.");
            labelContext.BackColor = Color.FromArgb(224, 240, 230);
            labelContext.ForeColor = Color.FromArgb(30, 110, 70);
        }
        else
        {
            // 260705Cl: LoadSpaceGroup(…, isCurrentCrystal: false) 経路 (現状未使用) では _crystalSeries が -1 のまま
            // ここへ到達し Symmetries[-1] で落ちるためガード (default の HM は null → PrettyHM がそのまま返し空表示)。
            //var cs = SymmetryStatic.Symmetries[_crystalSeries];
            var cs = _crystalSeries >= 0 ? SymmetryStatic.Symmetries[_crystalSeries] : default;
            labelContext.Text = "▲ " + string.Format(Loc(
                en: "Viewing {0} — not the current crystal ({1}).",
                ja: "{0} を表示中 — 現在の結晶 ({1}) とは異なります。",
                de: "Zeige {0} — nicht der aktuelle Kristall ({1}).",
                fr: "Affichage de {0} — pas le cristal actuel ({1}).",
                es: "Mostrando {0} — no es el cristal actual ({1}).",
                pt: "Mostrando {0} — não é o cristal atual ({1}).",
                it: "Visualizzazione di {0} — non il cristallo attuale ({1}).",
                ru: "Показано {0} — не текущий кристалл ({1}).",
                zhHans: "正在显示 {0} — 非当前晶体 ({1})。",
                zhHant: "正在顯示 {0} — 非目前晶體 ({1})。",
                ko: "{0} 표시 중 — 현재 결정({1})이 아닙니다."),
                SeitzNotation.PrettyHM(SymmetryStatic.Symmetries[_currentSeries].SpaceGroupHMStr), SeitzNotation.PrettyHM(cs.SpaceGroupHMStr));
            labelContext.BackColor = Color.FromArgb(246, 236, 214);
            labelContext.ForeColor = Color.FromArgb(150, 100, 20);
        }
    }

    private void buttonBack_Click(object sender, EventArgs e)
    {
        if (_back.Count == 0) return;
        _forward.Push(_currentSeries);
        NavigateTo(_back.Pop(), pushHistory: false);
    }

    private void buttonForward_Click(object sender, EventArgs e)
    {
        if (_forward.Count == 0) return;
        _back.Push(_currentSeries);
        NavigateTo(_forward.Pop(), pushHistory: false);
    }

    private void buttonHome_Click(object sender, EventArgs e)
    {
        if (_crystalSeries >= 0 && _crystalSeries != _currentSeries)
            NavigateTo(_crystalSeries);
    }
    #endregion

    #region ツリー構築
    private void BuildTree()
    {
        treeRelations.BeginUpdate();
        treeRelations.Nodes.Clear();

        // --- Maximal subgroups ---
        var subRoot = treeRelations.Nodes.Add(Loc(en: "Maximal subgroups", ja: "極大部分群", de: "Maximale Untergruppen", fr: "Sous-groupes maximaux", es: "Subgrupos maximales", pt: "Subgrupos maximais", it: "Sottogruppi massimali", ru: "Максимальные подгруппы", zhHans: "极大子群", zhHant: "極大子群", ko: "극대 부분군"));
        var tNode = subRoot.Nodes.Add("t — translationengleiche");
        foreach (var s in _subs)
            tNode.Nodes.Add(MakeSubNode(s));
        // k / isomorphic は Phase 2 データ待ち
        var kNode = subRoot.Nodes.Add("k — klassengleiche");
        kNode.Nodes.Add(PendingNode());
        var iNode = subRoot.Nodes.Add(Loc(en: "isomorphic (series)", ja: "同型 (系列)", de: "isomorph (Serie)", fr: "isomorphes (série)", es: "isomorfos (serie)", pt: "isomorfos (série)", it: "isomorfi (serie)", ru: "изоморфные (серия)", zhHans: "同型 (系列)", zhHant: "同型 (系列)", ko: "동형 (계열)"));
        iNode.Nodes.Add(PendingNode());

        // --- Minimal supergroups ---
        var superRoot = treeRelations.Nodes.Add(Loc(en: "Minimal supergroups", ja: "極小超群", de: "Minimale Obergruppen", fr: "Supergroupes minimaux", es: "Supergrupos minimales", pt: "Supergrupos minimais", it: "Supergruppi minimali", ru: "Минимальные надгруппы", zhHans: "极小超群", zhHant: "極小超群", ko: "극소 초군"));
        var tsNode = superRoot.Nodes.Add("t — translationengleiche");
        if (_supersPending) // 260705Cl 追加: バックグラウンド構築中の表示
            tsNode.Nodes.Add(ComputingNode());
        else if (_supers.Count == 0)
            tsNode.Nodes.Add(NoneNode());
        else
            foreach (var s in _supers)
            {
                var n = new TreeNode($"{SeitzNotation.PrettyHM(SymmetryStatic.Symmetries[s.SupergroupSeriesNumber].SpaceGroupHMStr)}   [{s.Index}]   No.{SymmetryStatic.Symmetries[s.SupergroupSeriesNumber].SpaceGroupNumber}")
                { Tag = new NodeTag { Kind = NodeKind.Supergroup, TargetSeries = s.SupergroupSeriesNumber } };
                tsNode.Nodes.Add(n);
            }
        var ksNode = superRoot.Nodes.Add("k — klassengleiche");
        ksNode.Nodes.Add(PendingNode());

        treeRelations.EndUpdate();
        subRoot.Expand(); tNode.Expand(); superRoot.Expand(); tsNode.Expand();
    }

    private TreeNode MakeSubNode(TSubgroup s)
    {
        string conj = s.ConjugateCount > 1 ? $" ×{s.ConjugateCount}" : "";
        string label = s.ChildSeriesNumber >= 0
            ? $"{SeitzNotation.PrettyHM(s.ChildLabel)}   [{s.Index}]{conj}   No.{SymmetryStatic.Symmetries[s.ChildSeriesNumber].SpaceGroupNumber}"
            : $"{s.PointGroupHM}   [{s.Index}]{conj}   " + Loc(en: "(unresolved)", ja: "(未同定)", de: "(ungelöst)", fr: "(non résolu)", es: "(sin resolver)", pt: "(não resolvido)", it: "(non risolto)", ru: "(не определено)", zhHans: "(未识别)", zhHant: "(未識別)", ko: "(미확인)");
        return new TreeNode(label) { Tag = new NodeTag { Kind = NodeKind.Subgroup, Relation = s, TargetSeries = s.ChildSeriesNumber } };
    }

    private TreeNode PendingNode() => new(Loc(en: "Phase 2 data pending", ja: "Phase 2 データ待ち", de: "Phase-2-Daten ausstehend", fr: "Données Phase 2 à venir", es: "Datos de Fase 2 pendientes", pt: "Dados da Fase 2 pendentes", it: "Dati Fase 2 in attesa", ru: "Данные фазы 2 ожидаются", zhHans: "Phase 2 数据待补", zhHant: "Phase 2 資料待補", ko: "Phase 2 데이터 대기")) { ForeColor = SystemColors.GrayText };
    private TreeNode NoneNode() => new(Loc(en: "none", ja: "なし", de: "keine", fr: "aucun", es: "ninguno", pt: "nenhum", it: "nessuno", ru: "нет", zhHans: "无", zhHant: "無", ko: "없음")) { ForeColor = SystemColors.GrayText };
    private TreeNode ComputingNode() => new(Loc(en: "computing…", ja: "計算中…", de: "wird berechnet…", fr: "calcul en cours…", es: "calculando…", pt: "calculando…", it: "calcolo in corso…", ru: "вычисляется…", zhHans: "计算中…", zhHant: "計算中…", ko: "계산 중…")) { ForeColor = SystemColors.GrayText }; // 260705Cl 追加

    private enum NodeKind { Subgroup, Supergroup }
    private sealed class NodeTag
    {
        public NodeKind Kind;
        public TSubgroup Relation;   // Subgroup のみ
        public int TargetSeries = -1;
    }

    private void treeRelations_AfterSelect(object sender, TreeViewEventArgs e)
    {
        if (e.Node.Tag is NodeTag tag && tag.Kind == NodeKind.Subgroup && tag.Relation != null)
        {
            //_selectedRelation = tag.Relation; // 260705Cl: 直後の ShowRelationDetail が設定するため冗長
            ShowRelationDetail(tag.Relation);
            RenderGraph(); // 選択ノードのハイライト更新
        }
    }

    private void treeRelations_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Node.Tag is NodeTag tag && tag.TargetSeries >= 0)
            NavigateTo(tag.TargetSeries);
    }
    #endregion

    #region 詳細タブの流し込み
    /// <summary>選択された t-部分群関係の詳細を各タブに表示する。null なら空表示。</summary>
    private void ShowRelationDetail(TSubgroup s)
    {
        _selectedRelation = s;
        if (s == null)
        {
            labelRelTitle.Text = Loc(en: "Select a subgroup relation from the tree.", ja: "ツリーから部分群関係を選択してください。", de: "Wählen Sie eine Untergruppenrelation im Baum.", fr: "Sélectionnez une relation de sous-groupe dans l'arbre.", es: "Seleccione una relación de subgrupo en el árbol.", pt: "Selecione uma relação de subgrupo na árvore.", it: "Seleziona una relazione di sottogruppo nell'albero.", ru: "Выберите отношение подгруппы в дереве.", zhHans: "请从树中选择子群关系。", zhHant: "請從樹中選擇子群關係。", ko: "트리에서 부분군 관계를 선택하세요.");
            labelMatrix.Text = "";
            miniTableGenerators.ClearRows();
            labelOrbitInfo.Text = ""; miniTableOrbit.ClearRows();
            labelDomains.Text = ""; miniTableTwins.ClearRows();
            labelReflInfo.Text = ""; miniTableReflections.ClearRows();
            return;
        }

        var parent = SymmetryStatic.Symmetries[_currentSeries];
        string childName = s.ChildSeriesNumber >= 0 ? SeitzNotation.PrettyHM(s.ChildLabel) : s.PointGroupHM;
        labelRelTitle.Text = $"{SeitzNotation.PrettyHM(parent.SpaceGroupHMStr)}  →  {childName}    ·    t, index {s.Index}";

        FillMatrixTab(s);
        FillOrbitTab(s);
        FillDomainsTab(s);
        FillReflectionsTab(s);
    }

    private void FillMatrixTab(TSubgroup s)
    {
        var sb = new StringBuilder();
        if (s.TransformP != null)
        {
            var P = s.TransformP; var p = s.TransformShift;
            sb.AppendLine(Loc(en: "Transformation to child basis  (a',b',c') = (a,b,c)·P", ja: "子基底への変換  (a',b',c') = (a,b,c)·P", de: "Transformation zur Kindbasis  (a',b',c') = (a,b,c)·P", fr: "Transformation vers la base fille  (a',b',c') = (a,b,c)·P", es: "Transformación a base hija  (a',b',c') = (a,b,c)·P", pt: "Transformação para base filha  (a',b',c') = (a,b,c)·P", it: "Trasformazione alla base figlia  (a',b',c') = (a,b,c)·P", ru: "Преобразование к базису подгруппы  (a',b',c') = (a,b,c)·P", zhHans: "到子基底的变换  (a',b',c') = (a,b,c)·P", zhHant: "到子基底的變換  (a',b',c') = (a,b,c)·P", ko: "자식 기저 변환  (a',b',c') = (a,b,c)·P"));
            sb.AppendLine();
            for (int r = 0; r < 3; r++)
                sb.AppendLine($"   | {Frac(P[r * 3]),6}  {Frac(P[r * 3 + 1]),6}  {Frac(P[r * 3 + 2]),6} |     p{(r == 1 ? " =" : "  ")} {Frac(p[r]),6}");
        }
        else
        {
            sb.AppendLine(Loc(en: "Type not resolved from the operation catalogue; point group " + s.PointGroupHM + " only.", ja: "変換カタログから型を同定できませんでした (点群 " + s.PointGroupHM + " のみ)。", de: "Typ nicht aufgelöst; nur Punktgruppe " + s.PointGroupHM + ".", fr: "Type non résolu ; seulement le groupe ponctuel " + s.PointGroupHM + ".", es: "Tipo no resuelto; solo grupo puntual " + s.PointGroupHM + ".", pt: "Tipo não resolvido; apenas grupo pontual " + s.PointGroupHM + ".", it: "Tipo non risolto; solo gruppo puntuale " + s.PointGroupHM + ".", ru: "Тип не определён; только точечная группа " + s.PointGroupHM + ".", zhHans: "未能识别类型；仅点群 " + s.PointGroupHM + "。", zhHant: "未能識別類型；僅點群 " + s.PointGroupHM + "。", ko: "유형 미확인; 점군 " + s.PointGroupHM + "만."));
        }
        sb.AppendLine();
        sb.Append(Loc(en: "Conjugate subgroups in this class: ", ja: "この類の共役部分群数: ", de: "Konjugierte Untergruppen dieser Klasse: ", fr: "Sous-groupes conjugués de cette classe : ", es: "Subgrupos conjugados de esta clase: ", pt: "Subgrupos conjugados desta classe: ", it: "Sottogruppi coniugati di questa classe: ", ru: "Сопряжённых подгрупп в классе: ", zhHans: "本类共轭子群数: ", zhHant: "本類共軛子群數: ", ko: "이 클래스의 켤레 부분군 수: ") + s.ConjugateCount);
        labelMatrix.Text = sb.ToString();

        // Retained / Lost generators
        var rows = new List<object[]>();
        string retStr = Loc(en: "retained", ja: "保持", de: "erhalten", fr: "conservé", es: "conservado", pt: "mantido", it: "mantenuto", ru: "сохранено", zhHans: "保持", zhHant: "保持", ko: "유지");
        string lostStr = Loc(en: "lost", ja: "消失", de: "verloren", fr: "perdu", es: "perdido", pt: "perdido", it: "perso", ru: "утрачено", zhHans: "消失", zhHant: "消失", ko: "소실");
        foreach (var op in s.Representatives)
            rows.Add([SeitzNotation.Seitz(op), SeitzNotation.GeometricType(op), retStr]);
        foreach (var op in s.CosetRepresentatives)
            rows.Add([SeitzNotation.Seitz(op), SeitzNotation.GeometricType(op), lostStr]);
        miniTableGenerators.SetRows(rows);
    }

    private void FillOrbitTab(TSubgroup s)
    {
        labelOrbitInfo.Text = s.ChildSeriesNumber >= 0
            ? Loc(en: "How each Wyckoff orbit of the parent splits (sampled with a generic point).", ja: "親の各 Wyckoff 軌道の分裂 (generic 点でのサンプル計算)。", de: "Aufspaltung jeder Wyckoff-Lage des Elters (Stichprobe mit generischem Punkt).", fr: "Éclatement de chaque orbite de Wyckoff du parent (échantillon, point générique).", es: "División de cada órbita de Wyckoff del padre (muestreo con punto genérico).", pt: "Divisão de cada órbita de Wyckoff do pai (amostragem com ponto genérico).", it: "Suddivisione di ogni orbita di Wyckoff del genitore (campione, punto generico).", ru: "Расщепление каждой орбиты Уайкоффа родителя (выборка, общая точка).", zhHans: "母群各 Wyckoff 轨道的分裂 (通用点采样)。", zhHant: "母群各 Wyckoff 軌道的分裂 (通用點取樣)。", ko: "부모의 각 Wyckoff 궤도 분열 (일반점 샘플링).")
            : Loc(en: "Child type unresolved — orbit letters unavailable.", ja: "子の型が未同定のため Wyckoff 文字は表示できません。", de: "Kindtyp ungelöst — keine Lagesymbole.", fr: "Type fille non résolu — lettres indisponibles.", es: "Tipo hija sin resolver — letras no disponibles.", pt: "Tipo filho não resolvido — letras indisponíveis.", it: "Tipo figlio non risolto — lettere non disponibili.", ru: "Тип подгруппы не определён — буквы недоступны.", zhHans: "子类型未识别 — 无法显示字母。", zhHant: "子類型未識別 — 無法顯示字母。", ko: "자식 유형 미확인 — 문자 표시 불가.");

        var wycks = SymmetryStatic.WyckoffPositions[_currentSeries];
        var split = TSubgroupFinder.GetOrbitSplitting(_currentSeries, s);
        var rows = new List<object[]>();
        for (int w = 0; w < wycks.Length; w++)
        {
            string parent = $"{wycks[w].Multiplicity}{wycks[w].WyckoffLetter}  {wycks[w].SiteSymmetry}";
            string child = string.Join(" + ", split[w].Select(p =>
                p.ChildWyckoffLetter != null ? $"{p.ChildMultiplicity}{p.ChildWyckoffLetter}" : $"{p.CountInParentCell}·"));
            string sites = s.ChildSeriesNumber >= 0
                ? string.Join(", ", split[w].Select(p => p.ChildSiteSymmetry).Distinct())
                : "";
            rows.Add([parent, child, split[w].Length.ToString(), sites]);
        }
        miniTableOrbit.SetRows(rows);
    }

    private void FillDomainsTab(TSubgroup s)
    {
        // 260705Cl 修正: t-部分群は並進を失わないため、反位相 (並進) ドメインは定義上常に 1 で、全ドメイン状態が
        // 方位 (双晶) 状態。旧実装の Index/ConjugateCount は「同一部分群 H を共有する状態数」(normalizer 因子。
        // 例: Pm-3m→P4mm の +P/−P 180° ドメイン) であり、反位相ドメイン数ではない。
        //int orientation = s.ConjugateCount;                 // 方位バリアント数 (共役類の大きさ)
        //int translation = Math.Max(1, total / Math.Max(1, orientation)); // 反位相 (並進) バリアント
        int total = s.Index;                                 // 全ドメイン状態数
        int orientation = total;                             // t では全状態が方位 (双晶) 状態
        int translation = 1;                                 // 反位相 (並進) 状態は t では常に 1

        var sb = new StringBuilder();
        sb.AppendLine(Loc(en: "Domain states on this transition:", ja: "この転移でのドメイン状態:", de: "Domänenzustände bei diesem Übergang:", fr: "États de domaine pour cette transition :", es: "Estados de dominio en esta transición:", pt: "Estados de domínio nesta transição:", it: "Stati di dominio in questa transizione:", ru: "Доменные состояния при этом переходе:", zhHans: "此相变的畴态:", zhHant: "此相變的疇態:", ko: "이 전이의 도메인 상태:"));
        sb.AppendLine($"   {Loc(en: "Total", ja: "総数", de: "Gesamt", fr: "Total", es: "Total", pt: "Total", it: "Totale", ru: "Всего", zhHans: "总数", zhHant: "總數", ko: "전체")} = {total}      " +
                      $"{Loc(en: "orientation", ja: "方位", de: "Orientierung", fr: "orientation", es: "orientación", pt: "orientação", it: "orientazione", ru: "ориентация", zhHans: "取向", zhHant: "取向", ko: "방위")} = {orientation}      " +
                      $"{Loc(en: "antiphase", ja: "反位相", de: "Antiphase", fr: "antiphase", es: "antifase", pt: "antifase", it: "antifase", ru: "антифаза", zhHans: "反相", zhHant: "反相", ko: "반위상")} = {translation}");
        sb.AppendLine();
        sb.Append(Loc(en: "Twin laws below are the reciprocal-space rotations that overlap the diffraction patterns of orientation domains.", ja: "下の双晶則は、方位ドメインの回折図形を重ねる逆空間回転です。", de: "Die Zwillingsgesetze unten sind die reziproken Rotationen, die die Beugungsbilder der Orientierungsdomänen überlagern.", fr: "Les lois de macle ci-dessous sont les rotations en espace réciproque qui superposent les clichés de diffraction des domaines d'orientation.", es: "Las leyes de macla siguientes son las rotaciones en espacio recíproco que superponen los patrones de difracción de los dominios de orientación.", pt: "As leis de geminação abaixo são as rotações no espaço recíproco que sobrepõem os padrões de difração dos domínios de orientação.", it: "Le leggi di geminazione sotto sono le rotazioni nello spazio reciproco che sovrappongono i pattern di diffrazione dei domini di orientazione.", ru: "Законы двойникования ниже — повороты в обратном пространстве, совмещающие дифракционные картины ориентационных доменов.", zhHans: "下方双晶律是使取向畴衍射图样重叠的倒易空间旋转。", zhHant: "下方雙晶律是使取向疇繞射圖樣重疊的倒易空間旋轉。", ko: "아래 쌍정 법칙은 방위 도메인의 회절 도형을 겹치는 역공간 회전입니다."));
        labelDomains.Text = sb.ToString();

        var rows = new List<object[]>();
        foreach (var op in s.CosetRepresentatives)
            rows.Add([SeitzNotation.Seitz(op), SeitzNotation.GeometricType(op)]);
        if (rows.Count == 0)
            rows.Add([Loc(en: "(single domain)", ja: "(単一ドメイン)", de: "(Einzeldomäne)", fr: "(domaine unique)", es: "(dominio único)", pt: "(domínio único)", it: "(dominio singolo)", ru: "(один домен)", zhHans: "(单畴)", zhHant: "(單疇)", ko: "(단일 도메인)"), ""]);
        miniTableTwins.SetRows(rows);
    }

    private void FillReflectionsTab(TSubgroup s)
    {
        var refl = TSubgroupFinder.GetNewReflections(_currentSeries, s, 4);
        labelReflInfo.Text = refl.Length == 0
            // 260705Cl 修正: t-部分群は格子周期を変えないため「超構造(superstructure)」反射ではない
            // (真の超格子反射は k-部分群で初めて生じる。codex レビューで指摘)。全言語でタブ見出し (New reflections)
            // と揃う中立表現に統一。
            ? Loc(en: "No new reflections: the subgroup lifts no systematic absence of the parent (|h,k,l| ≤ 4).", ja: "新規反射なし: この部分群は親の系統的消滅を解除しません (|h,k,l| ≤ 4)。", de: "Keine neuen Reflexe: Die Untergruppe hebt keine Auslöschung des Elters auf (|h,k,l| ≤ 4).", fr: "Aucune nouvelle réflexion : le sous-groupe ne lève aucune extinction du parent (|h,k,l| ≤ 4).", es: "Sin nuevas reflexiones: el subgrupo no levanta ninguna ausencia del padre (|h,k,l| ≤ 4).", pt: "Sem novas reflexões: o subgrupo não levanta nenhuma ausência do pai (|h,k,l| ≤ 4).", it: "Nessuna nuova riflessione: il sottogruppo non rimuove assenze del genitore (|h,k,l| ≤ 4).", ru: "Нет новых отражений: подгруппа не снимает погасаний родителя (|h,k,l| ≤ 4).", zhHans: "无新反射: 该子群未解除母群的系统消光 (|h,k,l| ≤ 4)。", zhHant: "無新反射: 該子群未解除母群的系統消光 (|h,k,l| ≤ 4)。", ko: "새로운 반사 없음: 이 부분군은 부모의 소멸을 해제하지 않습니다 (|h,k,l| ≤ 4).")
            : string.Format(Loc(en: "{0} reflections (up to symmetry) become allowed. Intensity still depends on the structure factor.", ja: "{0} 本 (対称等価を除く) の反射が許容になります。強度は構造因子に依存します。", de: "{0} Reflexe (bis auf Symmetrie) werden erlaubt. Die Intensität hängt weiter vom Strukturfaktor ab.", fr: "{0} réflexions (à symétrie près) deviennent autorisées. L'intensité dépend du facteur de structure.", es: "{0} reflexiones (salvo simetría) se permiten. La intensidad depende del factor de estructura.", pt: "{0} reflexões (a menos de simetria) tornam-se permitidas. A intensidade depende do fator de estrutura.", it: "{0} riflessioni (a meno di simmetria) diventano permesse. L'intensità dipende dal fattore di struttura.", ru: "{0} отражений (с точностью до симметрии) становятся разрешёнными. Интенсивность зависит от структурного фактора.", zhHans: "{0} 个 (对称等价除外) 反射变为允许。强度仍取决于结构因子。", zhHant: "{0} 個 (對稱等價除外) 反射變為允許。強度仍取決於結構因子。", ko: "{0}개 (대칭 제외) 반사가 허용됩니다. 강도는 구조 인자에 따릅니다."), refl.Length);

        var rows = new List<object[]>();
        foreach (var r in refl)
            rows.Add([$"{r.H} {r.K} {r.L}", r.EquivCount.ToString(), r.ParentRule]); // 260705Cl: 素通し helper Hkl をインライン化
        miniTableReflections.SetRows(rows);
    }
    #endregion

    #region Bärnighausen グラフ (Diagram タブ)
    private void RenderGraph()
    {
        int w = Math.Max(50, pictureBoxGraph.ClientSize.Width);
        int h = Math.Max(50, pictureBoxGraph.ClientSize.Height);
        var bmp = new Bitmap(w, h);
        _graphNodes.Clear();
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Color.White);
            DrawGraph(g, w, h);
        }
        var old = pictureBoxGraph.Image;
        pictureBoxGraph.Image = bmp;
        old?.Dispose();
    }

    private void DrawGraph(Graphics g, int w, int h)
    {
        // 3 段レイアウト: 上=超群, 中=現在, 下=部分群。ノード = 群、辺 = t 関係 (index ラベル)。
        var cur = SymmetryStatic.Symmetries[_currentSeries];
        int midY = h / 2, topY = (int)(h * 0.16), botY = (int)(h * 0.84);
        var nodeSize = new Size(88, 40);

        var curRect = NodeRect(w / 2, midY, nodeSize);
        // supergroups (上)
        var superList = _supers.Take(6).ToList();
        var superRects = SpreadRects(superList.Count, w, topY, nodeSize);
        // subgroups (下)
        var subList = _subs.ToList();
        var subRects = SpreadRects(subList.Count, w, botY, nodeSize);

        using var edgePen = new Pen(Color.FromArgb(150, 160, 175), 1.4f);
        using var edgeFont = new Font("Segoe UI", 8f, FontStyle.Bold);
        using var labelBg = new SolidBrush(Color.White);
        using var tBrush = new SolidBrush(Color.FromArgb(47, 111, 179));

        // edges: super -> current
        for (int i = 0; i < superRects.Count; i++)
        {
            DrawEdge(g, edgePen, Center(superRects[i]), Center(curRect));
            DrawEdgeLabel(g, edgeFont, tBrush, labelBg, Mid(Center(superRects[i]), Center(curRect)), $"t{superList[i].Index}");
        }
        // edges: current -> sub
        for (int i = 0; i < subRects.Count; i++)
        {
            DrawEdge(g, edgePen, Center(curRect), Center(subRects[i]));
            DrawEdgeLabel(g, edgeFont, tBrush, labelBg, Mid(Center(curRect), Center(subRects[i])), $"t{subList[i].Index}");
        }

        // nodes
        for (int i = 0; i < superRects.Count; i++)
            DrawNode(g, superRects[i], SymmetryStatic.Symmetries[superList[i].SupergroupSeriesNumber], false, false, superList[i].SupergroupSeriesNumber);
        for (int i = 0; i < subRects.Count; i++)
        {
            bool sel = _selectedRelation != null && ReferenceEquals(_selectedRelation, subList[i]);
            DrawNode(g, subRects[i], subList[i].ChildSeriesNumber >= 0 ? SymmetryStatic.Symmetries[subList[i].ChildSeriesNumber] : default, false, sel,
                     subList[i].ChildSeriesNumber, subList[i].ChildSeriesNumber < 0 ? subList[i].PointGroupHM : null);
        }
        DrawNode(g, curRect, cur, true, false, _currentSeries);
    }

    private List<Rectangle> SpreadRects(int count, int w, int y, Size size)
    {
        var list = new List<Rectangle>();
        if (count == 0) return list;
        int margin = 20;
        int usable = w - 2 * margin;
        for (int i = 0; i < count; i++)
        {
            int cx = count == 1 ? w / 2 : margin + (int)((i + 0.5) * usable / count);
            list.Add(NodeRect(cx, y, size));
        }
        return list;
    }

    private static Rectangle NodeRect(int cx, int cy, Size s) => new(cx - s.Width / 2, cy - s.Height / 2, s.Width, s.Height);
    private static Point Center(Rectangle r) => new(r.X + r.Width / 2, r.Y + r.Height / 2);
    private static Point Mid(Point a, Point b) => new((a.X + b.X) / 2, (a.Y + b.Y) / 2);

    private static void DrawEdge(Graphics g, Pen pen, Point a, Point b) => g.DrawLine(pen, a, b);
    private static void DrawEdgeLabel(Graphics g, Font f, Brush fg, Brush bg, Point at, string text)
    {
        var sz = g.MeasureString(text, f);
        var rect = new RectangleF(at.X - sz.Width / 2 - 2, at.Y - sz.Height / 2, sz.Width + 4, sz.Height);
        g.FillRectangle(bg, rect);
        g.DrawString(text, f, fg, rect.X + 2, rect.Y);
    }

    private void DrawNode(Graphics g, Rectangle rect, Symmetry sym, bool isCurrent, bool isSelected, int series, string fallbackLabel = null)
    {
        _graphNodes.Add((rect, series));
        using var fill = new SolidBrush(Color.White);
        using var border = new Pen(isCurrent ? Color.FromArgb(47, 111, 179) : isSelected ? Color.FromArgb(44, 122, 123) : Color.FromArgb(180, 188, 200), isCurrent || isSelected ? 2.2f : 1.3f);
        using var path = Rounded(rect, 8);
        // 260705Cl: 私製 Inflate を BCL の Rectangle.Inflate に置換し、halo 側 GraphicsPath の Dispose 漏れも修正。
        //if (isCurrent) { using var halo = new SolidBrush(Color.FromArgb(220, 234, 251)); g.FillPath(halo, Rounded(Inflate(rect, 3), 10)); }
        if (isCurrent) { using var halo = new SolidBrush(Color.FromArgb(220, 234, 251)); using var haloPath = Rounded(Rectangle.Inflate(rect, 3, 3), 10); g.FillPath(halo, haloPath); }
        g.FillPath(fill, path);
        g.DrawPath(border, path);

        using var hmFont = new Font("Segoe UI", 10f, FontStyle.Bold);
        using var noFont = new Font("Segoe UI", 7.5f);
        using var fg = new SolidBrush(Color.FromArgb(26, 32, 41));
        using var subFg = new SolidBrush(Color.FromArgb(103, 113, 126));
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }; // 260705Cl: Dispose 漏れ修正
        string hm = series >= 0 ? SeitzNotation.PrettyHM(sym.SpaceGroupHMStr) : fallbackLabel ?? "?";
        string no = series >= 0 ? "No. " + sym.SpaceGroupNumber : "";
        g.DrawString(hm, hmFont, fg, new RectangleF(rect.X, rect.Y + 2, rect.Width, rect.Height - 14), sf);
        if (no.Length > 0)
            g.DrawString(no, noFont, subFg, new RectangleF(rect.X, rect.Bottom - 15, rect.Width, 13), sf);
    }

    private static GraphicsPath Rounded(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
    //private static Rectangle Inflate(Rectangle r, int by) => new(r.X - by, r.Y - by, r.Width + 2 * by, r.Height + 2 * by); // 260705Cl: BCL Rectangle.Inflate に置換

    private int HitTestGraph(Point p)
    {
        foreach (var (rect, series) in _graphNodes)
            if (rect.Contains(p) && series >= 0)
                return series;
        return -1;
    }

    private void pictureBoxGraph_SizeChanged(object sender, EventArgs e)
    {
        if (_currentSeries >= 0) RenderGraph();
    }

    private void pictureBoxGraph_MouseClick(object sender, MouseEventArgs e)
    {
        int series = HitTestGraph(e.Location);
        if (series < 0 || series == _currentSeries) return;
        // クリック = その関係を選択 (詳細タブ更新)。部分群ノードなら関係も特定。
        var rel = _subs.FirstOrDefault(s => s.ChildSeriesNumber == series);
        if (rel != null) { ShowRelationDetail(rel); RenderGraph(); }
    }

    private void pictureBoxGraph_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        int series = HitTestGraph(e.Location);
        if (series >= 0 && series != _currentSeries)
            NavigateTo(series);
    }
    #endregion

    #region テーブル列定義 / ラベル多言語化 / 整形
    private void SetupTables()
    {
        const DataGridViewContentAlignment L = DataGridViewContentAlignment.MiddleLeft;
        const DataGridViewContentAlignment R = DataGridViewContentAlignment.MiddleRight;

        miniTableGenerators.SetColumns(
            new MiniTable.Col("Seitz", L),
            new MiniTable.Col(Loc(en: "Type", ja: "種類", de: "Typ", fr: "Type", es: "Tipo", pt: "Tipo", it: "Tipo", ru: "Тип", zhHans: "类型", zhHant: "類型", ko: "종류"), L, Fill: true),
            new MiniTable.Col(Loc(en: "Status", ja: "状態", de: "Status", fr: "État", es: "Estado", pt: "Estado", it: "Stato", ru: "Статус", zhHans: "状态", zhHant: "狀態", ko: "상태"), L));

        miniTableOrbit.SetColumns(
            new MiniTable.Col(Loc(en: "Parent", ja: "親", de: "Eltern", fr: "Parent", es: "Padre", pt: "Pai", it: "Genitore", ru: "Родитель", zhHans: "母群", zhHant: "母群", ko: "부모"), L),
            new MiniTable.Col(Loc(en: "→ Child", ja: "→ 子", de: "→ Kind", fr: "→ Fille", es: "→ Hija", pt: "→ Filho", it: "→ Figlio", ru: "→ Подгруппа", zhHans: "→ 子群", zhHant: "→ 子群", ko: "→ 자식"), L, Fill: true),
            new MiniTable.Col(Loc(en: "Split", ja: "分裂数", de: "Teile", fr: "Parts", es: "Partes", pt: "Partes", it: "Parti", ru: "Части", zhHans: "分裂", zhHant: "分裂", ko: "분열"), R),
            new MiniTable.Col(Loc(en: "Site sym.", ja: "サイト対称", de: "Lagesym.", fr: "Sym. site", es: "Sim. sitio", pt: "Sim. sítio", it: "Simm. sito", ru: "Симм. поз.", zhHans: "位置对称", zhHant: "位置對稱", ko: "자리 대칭"), L));

        miniTableTwins.SetColumns(
            new MiniTable.Col("Seitz", L),
            new MiniTable.Col(Loc(en: "Twin operation", ja: "双晶操作", de: "Zwillingsoperation", fr: "Opération de macle", es: "Operación de macla", pt: "Operação de geminação", it: "Operazione di geminazione", ru: "Операция двойникования", zhHans: "双晶操作", zhHant: "雙晶操作", ko: "쌍정 연산"), L, Fill: true));

        miniTableReflections.SetColumns(
            new MiniTable.Col("h k l", L),
            new MiniTable.Col(Loc(en: "Equiv.", ja: "等価数", de: "Äquiv.", fr: "Équiv.", es: "Equiv.", pt: "Equiv.", it: "Equiv.", ru: "Эквив.", zhHans: "等价数", zhHant: "等價數", ko: "등가수"), R),
            new MiniTable.Col(Loc(en: "Absent in parent (rule)", ja: "親での消滅則", de: "Im Eltern verboten (Regel)", fr: "Absent dans le parent (règle)", es: "Ausente en el padre (regla)", pt: "Ausente no pai (regra)", it: "Assente nel genitore (regola)", ru: "Погасание в родителе (правило)", zhHans: "母群消光条件", zhHant: "母群消光條件", ko: "부모 소멸 규칙"), L, Fill: true));
    }

    private void LocalizeLabels()
    {
        Text = Loc(en: "Group Relations", ja: "群の関係", de: "Gruppenrelationen", fr: "Relations de groupe", es: "Relaciones de grupo", pt: "Relações de grupo", it: "Relazioni di gruppo", ru: "Групповые отношения", zhHans: "群关系", zhHant: "群關係", ko: "군 관계");
        buttonBack.Text = "←"; buttonForward.Text = "→";
        buttonHome.Text = "⌂ " + Loc(en: "Home", ja: "現結晶", de: "Start", fr: "Accueil", es: "Inicio", pt: "Início", it: "Home", ru: "Домой", zhHans: "当前", zhHant: "目前", ko: "현재");
        tabMatrix.Text = Loc(en: "Matrix", ja: "変換行列", de: "Matrix", fr: "Matrice", es: "Matriz", pt: "Matriz", it: "Matrice", ru: "Матрица", zhHans: "变换矩阵", zhHant: "變換矩陣", ko: "변환 행렬");
        tabOrbit.Text = Loc(en: "Orbit splitting", ja: "軌道分裂", de: "Bahnaufspaltung", fr: "Éclatement d'orbite", es: "División de órbita", pt: "Divisão de órbita", it: "Suddivisione orbita", ru: "Расщепление орбит", zhHans: "轨道分裂", zhHant: "軌道分裂", ko: "궤도 분열");
        tabDomains.Text = Loc(en: "Domains & Twins", ja: "ドメイン・双晶", de: "Domänen & Zwillinge", fr: "Domaines & Macles", es: "Dominios y maclas", pt: "Domínios e geminações", it: "Domini e geminazioni", ru: "Домены и двойники", zhHans: "畴与双晶", zhHant: "疇與雙晶", ko: "도메인·쌍정");
        // 260705Cl 修正: ja/zh/ko が「超構造 (superstructure)」を含意していたが、t-部分群は格子周期を変えないため
        // 不正確 (codex レビューで指摘)。他言語 (New reflections 等) と揃う中立表現へ。
        //tabReflections.Text = Loc(en: "New reflections", ja: "超構造反射", de: "Neue Reflexe", fr: "Nouvelles réflexions", es: "Nuevas reflexiones", pt: "Novas reflexões", it: "Nuove riflessioni", ru: "Новые отражения", zhHans: "超结构反射", zhHant: "超結構反射", ko: "초구조 반사");
        tabReflections.Text = Loc(en: "New reflections", ja: "新規反射", de: "Neue Reflexe", fr: "Nouvelles réflexions", es: "Nuevas reflexiones", pt: "Novas reflexões", it: "Nuove riflessioni", ru: "Новые отражения", zhHans: "新反射", zhHant: "新反射", ko: "새 반사");
        tabDiagram.Text = Loc(en: "Diagram", ja: "系統図", de: "Diagramm", fr: "Diagramme", es: "Diagrama", pt: "Diagrama", it: "Diagramma", ru: "Диаграмма", zhHans: "系统图", zhHant: "系統圖", ko: "계통도");
        toolTip.SetToolTip(buttonBack, Loc(en: "Back", ja: "戻る", de: "Zurück", fr: "Précédent", es: "Atrás", pt: "Voltar", it: "Indietro", ru: "Назад", zhHans: "后退", zhHant: "後退", ko: "뒤로"));
        toolTip.SetToolTip(buttonForward, Loc(en: "Forward", ja: "進む", de: "Vor", fr: "Suivant", es: "Adelante", pt: "Avançar", it: "Avanti", ru: "Вперёд", zhHans: "前进", zhHant: "前進", ko: "앞으로"));
        toolTip.SetToolTip(pictureBoxGraph, Loc(en: "Click a node to inspect, double-click to browse into it.", ja: "ノードをクリックで詳細、ダブルクリックでその群へ移動。", de: "Knoten anklicken zum Ansehen, Doppelklick zum Öffnen.", fr: "Cliquez sur un nœud pour l'inspecter, double-cliquez pour y naviguer.", es: "Haga clic en un nodo para inspeccionar, doble clic para navegar.", pt: "Clique num nó para inspecionar, duplo clique para navegar.", it: "Clic su un nodo per ispezionare, doppio clic per aprirlo.", ru: "Клик по узлу — детали, двойной клик — перейти.", zhHans: "单击节点查看，双击进入该群。", zhHant: "單擊節點查看，雙擊進入該群。", ko: "노드를 클릭하면 상세, 더블클릭하면 이동."));
    }

    // 260705Cl: FormSymmetryInformation と一字一句同一だった PrettyHM を SeitzNotation.PrettyHM へ集約 (実装は移設)。
    // 260705Cl: Hkl(int) は int 補間で足りる素通し helper だったためインライン化 (FillReflectionsTab)。
    //private static string Hkl(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>有理数を短い分数/整数文字列へ (行列・原点表示用)。</summary>
    private static string Frac(double d)
    {
        d = Math.Round(d, 6);
        if (Math.Abs(d - Math.Round(d)) < 1e-6) return ((int)Math.Round(d)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (int den in new[] { 2, 3, 4, 6, 8, 12 })
        {
            double x = d * den;
            if (Math.Abs(x - Math.Round(x)) < 1e-4)
                return $"{(int)Math.Round(x)}/{den}";
        }
        return d.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }
    #endregion
}
