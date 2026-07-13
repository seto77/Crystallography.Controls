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
using System.Drawing.Imaging; // 260713Cl 追加 (③-2): ColorMatrix/ImageAttributes で lost/retained ティント
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
    //private readonly Stack<int> _back = new();
    //private readonly Stack<int> _forward = new();
    // 260711Cl (codex R12): 履歴を series 番号から hop (経由した関係+向き) へ拡張。生の履歴は chain でなく
    // walk なので、Diagram の多階層表示には BuildSelectedBranch() で単調な「選択パス」へ還元して使う。
    // branch 再構成に oldest→newest の走査が要るため List (末尾=最新、スタック的に使用)。
    private readonly List<NavigationHop> _back = [];
    private readonly List<NavigationHop> _forward = [];

    /// <summary>ナビゲーション 1 回分。Via=null は境界 (Home / 明示ロードなど関係を経由しない遷移)。260711Cl 追加 (codex R12)。</summary>
    private sealed record NavigationHop(int FromSeries, int ToSeries, GroupRelation Via, bool SupergroupUp);

    // 現在閲覧中の群の関係 (グラフ・詳細タブ共有)。260705Cl: TSubgroup/TSubgroupFinder.TSupergroup を
    // 統合した共通 DTO GroupRelation に置換 (Phase 2e)。
    private GroupRelation[] _subs = [];
    /// <summary>260705Cl 追加 (Phase 2c Step4): 現在閲覧中の群の maximal k-部分群 (klassengleiche)。
    /// t- (_subs) と異なり Orbit splitting / New reflections タブは未対応 (ShowRelationDetail でガード表示)。</summary>
    private GroupRelation[] _ksubs = [];
    private IReadOnlyList<GroupRelation> _supers = [];
    private bool _supersPending; // 260705Cl 追加: 超群索引をバックグラウンド構築中 (ツリーに「計算中…」を表示)
    /// <summary>260708Cl 追加 (Phase 2d 後段): 現在閲覧中の群の minimal k-超群 (KSubgroupFinder の逆引き、同型含む)。</summary>
    private GroupRelation[] _ksupers = [];
    private bool _ksupersPending; // 260708Cl 追加: k-超群逆引きをバックグラウンド構築中
    private GroupRelation _selectedRelation;   // ツリー/グラフで選択中の関係 (null 可)

    // 260709Cl 追加 (Phase 3、codex R11): isomorphic の normalizer 軌道 (系列) 表示と高指数拡張列挙。
    /// <summary>index ≤ 4 の共役類 (GetMaximalKSubgroups) に対する軌道 ID (classId 引き)。null = 未計算 (pending/失敗)。</summary>
    private int[] _isoOrbits;
    /// <summary>index 5..スピナー値の拡張同型 (Kind=Isomorphic) と各軌道 ID。_ksubs とは分離して保持
    /// (Diagram は index ≤ 4 の骨格のみ描く方針のため、混ぜない)。</summary>
    private readonly List<(GroupRelation Rel, int Orbit)> _isoExtra = [];
    private bool _isoPending;
    private bool _isoFailed;
    private int _isoGeneration; // 世代ガード (スピナー連打・ナビゲーション競合で古い結果を捨てる)
    private readonly Timer _isoDebounce = new() { Interval = 300 }; // スピナー変更の debounce (codex R11)

    // グラフのヒットテスト用ノード矩形 (画面座標) と対応 series。
    //private readonly List<(Rectangle Rect, int Series)> _graphNodes = []; // 260709Cl: k-/isomorphic 辺の追加に伴い GraphNode へ拡張。
    // series ベースの逆引きは (a) 同じ子タイプが t と k の両方に現れると曖昧、(b) isomorphic (子 series == 現在 series) が
    // 「series == _currentSeries は無視」ガードに弾かれ選択不能、の 2 つの実害があった (codex 相談 R8)。
    private readonly List<GraphNode> _graphNodes = [];

    /// <summary>260709Cl 追加: Diagram のヒットテスト用ノード。同一 (対象 series, Kind, index) の非共役クラスは
    /// 1 ノードに集約する (Relations に全クラス、先頭が代表。ツリーが類ごとの詳細を担う)。
    /// 現在ノード・overflow (+N) ノードは Relations が空。</summary>
    private sealed class GraphNode
    {
        public Rectangle Rect;
        /// <summary>dblclick の遷移先 series (-1 = 遷移不可)。</summary>
        public int TargetSeries = -1;
        /// <summary>集約された共役類 (空 = 現在ノード / overflow ノード)。クリック選択は先頭を代表に使う。</summary>
        public GroupRelation[] Relations = [];
        /// <summary>true = 上段 (Minimal supergroups) 由来。Matrix タブの (P,p)⁻¹ 表示向き判定に明示的に渡す
        /// (self-isomorphic では Parent==Child==現在 series となり従来の推定が破綻するため。codex 相談 R8)。</summary>
        public bool ViewFromChild;
    }

    // 260708Cl 追加: Diagram ノードの HM/点群記号 LaTeX ビットマップキャッシュ (記号文字列ごと。色・フォント・dpi は全ノード共通)。
    private readonly Dictionary<string, Bitmap> _hmLatexCache = [];
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
            // 260708Cl 追加: Diagram の HM 記号 LaTeX ビットマップキャッシュをフォーム破棄時に解放。
            Disposed += (_, _) => { foreach (var b in _hmLatexCache.Values) b?.Dispose(); _hmLatexCache.Clear(); };
            // 260709Cl 追加 (Phase 3): 同型 index スピナーの debounce タイマ (Tick で 1 回だけ再計算)。
            _isoDebounce.Tick += (_, _) => { _isoDebounce.Stop(); if (_currentSeries >= 0) StartIsoComputation(); };
            Disposed += (_, _) => _isoDebounce.Dispose();
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
    /// <summary>--capture 用: ノードを選択する (AfterSelect 経由で詳細タブ全部が populate される)。
    /// プレースホルダ「ツリーから選択してください」でなく実データの見た目を確認できるようにする。
    /// 260705Cl 修正 (Phase 2e): 超群 (Minimal supergroups、(P,p)⁻¹ 表示の新機能) を優先的に選ぶ。索引が
    /// バックグラウンド構築中なら最大 10 秒待つ (通常のフォームオープンでは待たない、--capture 専用の同期待ち)。</summary>
    public void PrepareCaptureForGuiAudit()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (!TSubgroupFinder.SupergroupIndexReady && sw.ElapsedMilliseconds < 10000)
        {
            System.Threading.Thread.Sleep(100);
            Application.DoEvents(); // ContinueWith (TaskScheduler.FromCurrentSynchronizationContext) をポンプする
        }

        // 260705Cl 追加: 代表結晶 (spinel Fd-3m) 自身に t-超群が無いため、素通しだと超群ビューが一度も撮れない。
        // 実際の dblclick と同じ経路で部分群へ一段下り、超群一覧に確実にデータが載る状態にする
        // (索引は全 230 タイプ共通で構築済みなので追加の待ちは不要)。
        if (_supers.Count == 0 && _subs.Length > 0 && _subs[0].ChildSeriesNumber >= 0)
            NavigateTo(_subs[0].ChildSeriesNumber);

        TreeNode fallback = null;
        foreach (TreeNode root in treeRelations.Nodes)
            foreach (TreeNode category in root.Nodes)
                foreach (TreeNode leaf in category.Nodes)
                    if (leaf.Tag is NodeTag { Relation: not null } tag)
                    {
                        if (tag.Kind == NodeKind.Supergroup) { treeRelations.SelectedNode = leaf; return; }
                        fallback ??= leaf;
                    }
        if (fallback != null)
            treeRelations.SelectedNode = fallback;
    }

    /// <summary>--capture 用: Diagram タブ (Bärnighausen グラフ) へのクロップ撮影のためタブ切替を公開する。</summary>
    public TabControl CaptureTabControl => tabDetail;
    #endregion

    #region ナビゲーション
    /// <summary>閲覧対象を切り替え、ツリー・詳細・グラフを再構築する。
    /// 260711Cl シグネチャ変更 (旧: NavigateTo(int seriesNumber, bool pushHistory = true)):
    /// 経由した関係 via を履歴 hop に記録する (codex R12、多階層 Bärnighausen 表示用)。向き (上昇/下降) は
    /// 呼び出し元に持たせず「遷移先が via の親側か」で導出する (フラグ渡しの取り違えを構造的に排除)。</summary>
    private void NavigateTo(int seriesNumber, bool pushHistory = true, GroupRelation via = null)
    {
        if (seriesNumber < 0 || seriesNumber >= SymmetryStatic.TotalSpaceGroupNumber)
            return;
        if (pushHistory && _currentSeries >= 0)
        {
            //_back.Push(_currentSeries);
            bool up = via != null && via.ParentSeriesNumber == seriesNumber; // 260711Cl: 親側へ向かう遷移 = 上昇
            _back.Add(new NavigationHop(_currentSeries, seriesNumber, via, up));
            _forward.Clear();
        }
        _currentSeries = seriesNumber;

        var sym = SymmetryStatic.Symmetries[seriesNumber];
        _subs = TSubgroupFinder.GetMaximalTSubgroups(seriesNumber);
        _ksubs = KSubgroupFinder.GetMaximalKSubgroups(seriesNumber); // 260705Cl 追加 (Phase 2c Step4)
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
            // 260708Cl 修正: ContinueWith(FromCurrentSynchronizationContext) は先行タスクの完了スレッド
            // (スレッドプール) 上でインライン実行されることがあり (診断ハーネスで非 UI スレッド実行を実測)、
            // 非 UI スレッドの BuildTree がツリーを半構築のまま放置する実バグ (ツリー空白) になった。
            // BeginInvoke で明示的に UI スレッドへマーシャリングする。
            //Task.Run(() => TSubgroupFinder.GetMinimalTSupergroups(it)).ContinueWith(t =>
            //{
            //    _supersPending = false;
            //    if (!t.IsFaulted && !IsDisposed && _currentSeries == seriesNumber)
            //    {
            //        _supers = t.Result;
            //        if (Visible) { BuildTree(); RenderGraph(); }
            //    }
            //}, TaskScheduler.FromCurrentSynchronizationContext());
            // 260708Cl (/simplify): Task.Run + マーシャリング足場を k-超群側と共通の ComputeThenApplyOnUi へ集約。
            ComputeThenApplyOnUi(() => TSubgroupFinder.GetMinimalTSupergroups(it), result =>
            {
                _supersPending = false;
                if (result != null && !IsDisposed && _currentSeries == seriesNumber)
                {
                    _supers = result;
                    // 260705Cl 追加: フォームが非表示 (Hide) の間はツリー/グラフを再構築しない。ユーザーが見ていない
                    // UI を触っても意味が無く、非表示中の control 更新でハンドル生成に失敗する例外を実際に観測した
                    // (--capture ハーネスでの検証)。次に NavigateTo/LoadSpaceGroup されたとき最新の _supers で再構築される。
                    if (Visible)
                    {
                        BuildTree();
                        RenderGraph();
                    }
                }
            });
        }
        // 260708Cl 追加 (Phase 2d 後段): k-超群逆引き。初回は同じ結晶類の全タイプの k-部分群計算を伴い重い
        // 場合があるため、t-超群索引と同じ「バックグラウンド構築 + 計算中…ノード + 完了時差し替え」方式を踏襲。
        if (KSubgroupFinder.KSupergroupsReady(sym.SpaceGroupNumber))
        {
            _ksupers = KSubgroupFinder.GetMinimalKSupergroups(sym.SpaceGroupNumber);
            _ksupersPending = false;
        }
        else
        {
            _ksupers = [];
            _ksupersPending = true;
            int itK = sym.SpaceGroupNumber;
            // 260708Cl: t-超群側と同じく BeginInvoke で UI スレッドへ明示マーシャリング (共通足場 ComputeThenApplyOnUi)。
            ComputeThenApplyOnUi(() => KSubgroupFinder.GetMinimalKSupergroups(itK), result =>
            {
                _ksupersPending = false;
                if (result != null && !IsDisposed && _currentSeries == seriesNumber)
                {
                    _ksupers = result;
                    //if (Visible)
                    //    BuildTree(); // k-超群は Diagram に描かないため RenderGraph は不要
                    if (Visible) // 260709Cl: Diagram にも k-超群辺を描くようになったため RenderGraph も呼ぶ
                    {
                        BuildTree();
                        RenderGraph();
                    }
                }
            });
        }
        _selectedRelation = null;

        // 260709Cl 追加 (Phase 3): isomorphic の normalizer 軌道 (系列) と高指数拡張をバックグラウンド計算。
        StartIsoComputation();

        BuildTree();
        UpdateBreadcrumb();
        UpdateNavButtons();
        RenderGraph();
        _pgFocus = null; // 260712Cl 追加 (③-4): 群が変わったら点群図の注視をリセットして再描画
        RenderPointGroups();
        ShowRelationDetail(null);
    }

    /// <summary>260709Cl 追加 (Phase 3、codex R11): isomorphic の normalizer 軌道 (index ≤ 4 分) と
    /// 高指数拡張列挙 (5..スピナー値) をバックグラウンドで計算し、完了時にツリーの同型枝を 2 階層
    /// (軌道 → G-共役類) へ差し替える。世代 ID と series の二重ガードで古い結果を棄却。</summary>
    private void StartIsoComputation()
    {
        int gen = ++_isoGeneration;
        int sn = _currentSeries;
        int isoMax = (int)numericIsoMax.Value;
        _isoOrbits = null;
        _isoExtra.Clear();
        _isoPending = true;
        _isoFailed = false;
        ComputeThenApplyOnUi(() =>
        {
            var orbits = KSubgroupFinder.GetNormalizerOrbits(sn);
            var extra = new List<(GroupRelation Rel, int Orbit)>();
            for (int n = 5; n <= isoMax; n++)
            {
                var rels = KSubgroupFinder.GetMaximalKSubgroupsAt(sn, n); // 非素数冪 index は即時空 (codex R11)
                if (rels.Length == 0) continue;
                var orbs = KSubgroupFinder.GetNormalizerOrbitsAt(sn, n);
                foreach (var r in rels)
                    extra.Add((r, orbs[r.ConjugacyClassId]));
            }
            return Tuple.Create(orbits, extra);
        }, result =>
        {
            if (gen != _isoGeneration || IsDisposed || _currentSeries != sn)
                return; // 古い世代 / 別の群へ移動済み — 棄却
            _isoPending = false;
            if (result == null)
            {
                _isoFailed = true; // 計算失敗は空集合と区別して表示する (codex R11)
            }
            else
            {
                _isoOrbits = result.Item1;
                _isoExtra.AddRange(result.Item2);
            }
            if (Visible)
                BuildTree();
        });
    }

    /// <summary>260709Cl 追加 (Phase 3): スピナー変更は debounce (300 ms) してから再計算 (連打・長押し対策)。</summary>
    private void numericIsoMax_ValueChanged(object sender, EventArgs e)
    {
        _isoDebounce.Stop();
        _isoDebounce.Start();
    }

    /// <summary>260708Cl 追加 (/simplify): バックグラウンド計算 → UI スレッドへ BeginInvoke で結果反映する共通足場
    /// (t-/k- 超群逆引きで共有。旧: 同一の Task.Run + ガード + マーシャリングを 2 箇所へコピーしていた)。
    /// compute が失敗したときは null を渡して applyOnUi を必ず呼ぶ (pending 解除のため)。フォーム破棄・
    /// ハンドル未生成との競合は握りつぶす (マーシャリング先が消えただけなので何もしないのが正しい)。</summary>
    private void ComputeThenApplyOnUi<T>(Func<T> compute, Action<T> applyOnUi) where T : class
    {
        Task.Run(() =>
        {
            T result = null;
            try { result = compute(); }
            catch { /* 計算失敗時も applyOnUi (pending 解除) は行う */ }
            try
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke(() => applyOnUi(result));
            }
            catch (ObjectDisposedException) { } // フォーム破棄との競合は無視
            catch (InvalidOperationException) { } // ハンドル破棄との競合は無視
        });
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
        // 260711Cl (codex R12): hop を _back→_forward へ移し FromSeries を表示 (kind/index/向きが往復後も残る)。
        var hop = _back[^1];
        _back.RemoveAt(_back.Count - 1);
        _forward.Add(hop);
        NavigateTo(hop.FromSeries, pushHistory: false);
    }

    private void buttonForward_Click(object sender, EventArgs e)
    {
        if (_forward.Count == 0) return;
        var hop = _forward[^1]; // 260711Cl (codex R12)
        _forward.RemoveAt(_forward.Count - 1);
        _back.Add(hop);
        NavigateTo(hop.ToSeries, pushHistory: false);
    }

    /// <summary>260711Cl 追加 (codex R12): 閲覧履歴 (walk — 上昇・往復・Home を含み得る) を、Diagram の
    /// 多階層 Bärnighausen 表示に使える単調な「選択パス」(現在群の祖先チェーン、最古→直近親) へ還元する。
    /// 各要素の Via はその祖先から 1 段下 (次の要素、末尾なら現在群) への下降関係。
    /// 規則: 下降 hop = 末尾へ追加 / 既知祖先への上昇 = そこまで切り詰め / 未知超群への上昇・境界 (Via=null) = re-root。</summary>
    private List<(int Series, GroupRelation Via)> BuildSelectedBranch()
    {
        var branch = new List<(int Series, GroupRelation Via)>();
        foreach (var hop in _back)
        {
            if (hop.Via == null)
            {
                branch.Clear(); // 境界 (Home / 関係を経由しない遷移)
            }
            else if (!hop.SupergroupUp)
            {
                branch.Add((hop.FromSeries, hop.Via)); // 下降: 親 (From) を祖先列の末尾へ
            }
            else
            {
                int idx = branch.FindLastIndex(b => b.Series == hop.ToSeries);
                if (idx >= 0)
                    branch.RemoveRange(idx, branch.Count - idx); // 既知祖先へ戻った (その祖先が新しい現在群)
                else
                    branch.Clear(); // 未知の超群へ昇った: re-root
            }
        }
        return branch;
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
        // 260705Cl 修正 (Phase 2c Step4): k- (klassengleiche) を実データ化。
        // 260708Cl (Phase 2d): 同型 (Kind=Isomorphic、ITA IIc) を isomorphic カテゴリへ分離して実データ化 (codex R7 合意)。
        var kNode = subRoot.Nodes.Add("k — klassengleiche");
        var kOnly = _ksubs.Where(s => s.Kind == GroupRelationKind.K).ToArray();
        if (kOnly.Length == 0)
            kNode.Nodes.Add(NoneNode());
        else
            foreach (var s in kOnly)
                kNode.Nodes.Add(MakeSubNode(s));
        var iNode = subRoot.Nodes.Add(Loc(en: "isomorphic (series)", ja: "同型 (系列)", de: "isomorph (Serie)", fr: "isomorphes (série)", es: "isomorfos (serie)", pt: "isomorfos (série)", it: "isomorfi (serie)", ru: "изоморфные (серия)", zhHans: "同型 (系列)", zhHant: "同型 (系列)", ko: "동형 (계열)"));
        //iNode.Nodes.Add(PendingNode()); // 260708Cl: 実データ化
        // 260709Cl (Phase 3): スピナー (numericIsoMax、2〜27) による高指数拡張と、normalizer 軌道 (系列) ごとの
        // 2 階層表示を実データ化 (R7 の保留を解除。軌道束ね = Phase 2 GetNormalizerOrbits(At)、codex R9-R11)。
        // 注記は現在の上限を動的表示。「任意の素数 index」への一般化はしない (空間群ごとに許される p と
        // 変換式が異なる、codex R7)。
        int isoMax = (int)numericIsoMax.Value;
        iNode.Nodes.Add(new TreeNode(string.Format(Loc(en: "index ≤ {0} shown — isomorphic series continue to higher indices", ja: "index ≤ {0} のみ表示 — 同型系列はより高い指数へ続きます", de: "nur Index ≤ {0} — isomorphe Serien setzen sich zu höheren Indizes fort", fr: "index ≤ {0} uniquement — les séries isomorphes continuent aux indices supérieurs", es: "solo índice ≤ {0} — las series isomorfas continúan en índices mayores", pt: "apenas índice ≤ {0} — as séries isomorfas continuam em índices maiores", it: "solo indice ≤ {0} — le serie isomorfe continuano a indici superiori", ru: "только индекс ≤ {0} — изоморфные серии продолжаются при больших индексах", zhHans: "仅显示 index ≤ {0} — 同型系列延伸至更高指数", zhHant: "僅顯示 index ≤ {0} — 同型系列延伸至更高指數", ko: "index ≤ {0}만 표시 — 동형 계열은 더 높은 지수로 이어집니다"), isoMax)) { ForeColor = SystemColors.GrayText });
        var isoOnly = _ksubs.Where(s => s.Kind == GroupRelationKind.Isomorphic).ToArray();
        if (_isoOrbits == null)
        {
            // 軌道が未計算 (バックグラウンド処理中 or 失敗): index ≤ 4 の類を従来どおりフラット表示 (codex R11)
            foreach (var s in isoOnly)
                iNode.Nodes.Add(MakeSubNode(s));
            if (_isoPending)
                iNode.Nodes.Add(ComputingNode());
            else if (_isoFailed)
                iNode.Nodes.Add(FailedNode());
            else if (isoOnly.Length == 0)
                iNode.Nodes.Add(NoneNode());
        }
        else
        {
            // 2 階層: (index, 軌道) ごとにグループ化し、複数類の軌道は「軌道ノード + 子に各類」、1 類は直置き。
            // 軌道代表は最小 ConjugacyClassId (決定的、codex R11)。
            var isoAll = isoOnly.Select(s => (Rel: s, Orbit: _isoOrbits[s.ConjugacyClassId])).Concat(_isoExtra);
            int shown = 0;
            foreach (var grp in isoAll.GroupBy(x => (x.Rel.Index, x.Orbit)).OrderBy(g => g.Key.Index).ThenBy(g => g.Key.Orbit))
            {
                shown++;
                var members = grp.OrderBy(x => x.Rel.ConjugacyClassId).Select(x => x.Rel).ToArray();
                if (members.Length == 1)
                {
                    iNode.Nodes.Add(MakeSubNode(members[0]));
                    continue;
                }
                var rep = members[0];
                string label = $"{SeitzNotation.PrettyHM(rep.ChildLabel)}   [{rep.Index}]   — " +
                    string.Format(Loc(en: "{0} classes (normalizer-equivalent)", ja: "同値な {0} 類 (normalizer)", de: "{0} Klassen (Normalisator-äquivalent)", fr: "{0} classes (équivalentes par normalisateur)", es: "{0} clases (equivalentes por normalizador)", pt: "{0} classes (equivalentes por normalizador)", it: "{0} classi (equivalenti per normalizzatore)", ru: "{0} классов (эквивалентны по нормализатору)", zhHans: "{0} 个类 (正规化子等价)", zhHant: "{0} 個類 (正規化子等價)", ko: "{0}개 류 (normalizer 동치)"), members.Length);
                var orbitNode = new TreeNode(label) { Tag = new NodeTag { Kind = NodeKind.Subgroup, Relation = rep, TargetSeries = rep.ChildSeriesNumber } };
                foreach (var mm in members)
                    orbitNode.Nodes.Add(MakeSubNode(mm));
                iNode.Nodes.Add(orbitNode);
            }
            if (shown == 0)
                iNode.Nodes.Add(NoneNode());
        }

        // --- Minimal supergroups ---
        var superRoot = treeRelations.Nodes.Add(Loc(en: "Minimal supergroups", ja: "極小超群", de: "Minimale Obergruppen", fr: "Supergroupes minimaux", es: "Supergrupos minimales", pt: "Supergrupos minimais", it: "Supergruppi minimali", ru: "Минимальные надгруппы", zhHans: "极小超群", zhHant: "極小超群", ko: "극소 초군"));
        var tsNode = superRoot.Nodes.Add("t — translationengleiche");
        if (_supersPending) // 260705Cl 追加: バックグラウンド構築中の表示
            tsNode.Nodes.Add(ComputingNode());
        else if (_supers.Count == 0)
            tsNode.Nodes.Add(NoneNode());
        else
            // 260705Cl 修正: GroupRelation 統合により超群側も P/p/Operations 等の全データを持つため、
            // MakeSubNode と同様に Relation を Tag へ積んでツリー選択可能にする (Phase 2e)。
            foreach (var s in _supers)
                tsNode.Nodes.Add(MakeSuperNode(s));
        var ksNode = superRoot.Nodes.Add("k — klassengleiche");
        //ksNode.Nodes.Add(PendingNode()); // 260708Cl: 実データ化 (Phase 2d 後段、KSubgroupFinder.GetMinimalKSupergroups 逆引き)
        // 260709Cl: _ksupers には Kind=K と Kind=Isomorphic が混在する (GetMinimalKSupergroups は同型を含む逆引き)。
        // 部分群側と同様に isomorphic を専用枝へ分離する (Diagram の i ラベルとの分類整合。codex 相談 R8)。
        var isSupNode = superRoot.Nodes.Add(Loc(en: "isomorphic (series)", ja: "同型 (系列)", de: "isomorph (Serie)", fr: "isomorphes (série)", es: "isomorfos (serie)", pt: "isomorfos (série)", it: "isomorfi (serie)", ru: "изоморфные (серия)", zhHans: "同型 (系列)", zhHant: "同型 (系列)", ko: "동형 (계열)"));
        if (_ksupersPending)
        {
            ksNode.Nodes.Add(ComputingNode());
            isSupNode.Nodes.Add(ComputingNode()); // 260709Cl
        }
        //else if (_ksupers.Length == 0)
        //    ksNode.Nodes.Add(NoneNode());
        //else
        //    foreach (var s in _ksupers)
        //        ksNode.Nodes.Add(MakeSuperNode(s));
        else // 260709Cl: Kind で振り分け
        {
            var kSupOnly = _ksupers.Where(s => s.Kind == GroupRelationKind.K).ToArray();
            if (kSupOnly.Length == 0)
                ksNode.Nodes.Add(NoneNode());
            else
                foreach (var s in kSupOnly)
                    ksNode.Nodes.Add(MakeSuperNode(s));
            var isoSupOnly = _ksupers.Where(s => s.Kind == GroupRelationKind.Isomorphic).ToArray();
            // 部分群側と同じ「index ≤ 4 のみ」の注記 (同型系列は超群方向にも際限なく続く)。
            isSupNode.Nodes.Add(new TreeNode(Loc(en: "index ≤ 4 only — isomorphic series continue to higher indices", ja: "index ≤ 4 のみ表示 — 同型系列はより高い指数へ続きます", de: "nur Index ≤ 4 — isomorphe Serien setzen sich zu höheren Indizes fort", fr: "index ≤ 4 uniquement — les séries isomorphes continuent aux indices supérieurs", es: "solo índice ≤ 4 — las series isomorfas continúan en índices mayores", pt: "apenas índice ≤ 4 — as séries isomorfas continuam em índices maiores", it: "solo indice ≤ 4 — le serie isomorfe continuano a indici superiori", ru: "только индекс ≤ 4 — изоморфные серии продолжаются при больших индексах", zhHans: "仅显示 index ≤ 4 — 同型系列延伸至更高指数", zhHant: "僅顯示 index ≤ 4 — 同型系列延伸至更高指數", ko: "index ≤ 4만 표시 — 동형 계열은 더 높은 지수로 이어집니다")) { ForeColor = SystemColors.GrayText });
            if (isoSupOnly.Length == 0)
                isSupNode.Nodes.Add(NoneNode());
            else
                foreach (var s in isoSupOnly)
                    isSupNode.Nodes.Add(MakeSuperNode(s));
        }

        // 260708Cl: 同一タイプ・同一 index の非共役クラスはラベルが同一で区別できない (実 GUI 目視で
        // Pm-3m の k に "Fm-3m [2] No.225" が 2 行並んだ、改修計画 §4.4)。重複ラベルへ類番号を付ける。
        //foreach (var category in new[] { tNode, kNode, iNode, tsNode, ksNode })
        //foreach (var category in new[] { tNode, kNode, iNode, tsNode, ksNode, isSupNode }) // 260709Cl: isomorphic 超群枝を追加
        var dupTargets = new List<TreeNode> { tNode, kNode, iNode, tsNode, ksNode, isSupNode }; // 260709Cl (Phase 3)
        dupTargets.AddRange(iNode.Nodes.Cast<TreeNode>().Where(n => n.Nodes.Count > 0)); // 軌道ノードの下の類も対象
        foreach (var category in dupTargets)
            foreach (var g in category.Nodes.Cast<TreeNode>().Where(n => n.Tag != null).GroupBy(n => n.Text).Where(g => g.Count() > 1))
            {
                int i = 1;
                foreach (var n in g)
                    n.Text += $"   · {Loc(en: "class", ja: "類", de: "Klasse", fr: "classe", es: "clase", pt: "classe", it: "classe", ru: "класс", zhHans: "类", zhHant: "類", ko: "클래스")} {i++}";
            }

        treeRelations.EndUpdate();
        subRoot.Expand(); tNode.Expand(); superRoot.Expand(); tsNode.Expand();
    }

    private TreeNode MakeSubNode(GroupRelation s)
    {
        string conj = s.ConjugateCount > 1 ? $" ×{s.ConjugateCount}" : "";
        string label = s.ChildSeriesNumber >= 0
            ? $"{SeitzNotation.PrettyHM(s.ChildLabel)}   [{s.Index}]{conj}   No.{SymmetryStatic.Symmetries[s.ChildSeriesNumber].SpaceGroupNumber}"
            : $"{s.PointGroupHM}   [{s.Index}]{conj}   " + Loc(en: "(unresolved)", ja: "(未同定)", de: "(ungelöst)", fr: "(non résolu)", es: "(sin resolver)", pt: "(não resolvido)", it: "(non risolto)", ru: "(не определено)", zhHans: "(未识别)", zhHant: "(未識別)", ko: "(미확인)");
        return new TreeNode(label) { Tag = new NodeTag { Kind = NodeKind.Subgroup, Relation = s, TargetSeries = s.ChildSeriesNumber } };
    }

    /// <summary>260705Cl 追加 (Phase 2e): 超群ノード。GroupRelation 統合により全データを持つため MakeSubNode 同様に選択可能。
    /// TargetSeries = ParentSeriesNumber (= その超群自身。dblclick でそこへ遷移)。</summary>
    private TreeNode MakeSuperNode(GroupRelation s)
    {
        string conj = s.ConjugateCount > 1 ? $" ×{s.ConjugateCount}" : "";
        var sup = SymmetryStatic.Symmetries[s.ParentSeriesNumber];
        string label = $"{SeitzNotation.PrettyHM(sup.SpaceGroupHMStr)}   [{s.Index}]{conj}   No.{sup.SpaceGroupNumber}";
        return new TreeNode(label) { Tag = new NodeTag { Kind = NodeKind.Supergroup, Relation = s, TargetSeries = s.ParentSeriesNumber } };
    }

    // 260708Cl: 全カテゴリ (t/k/isomorphic 部分群・t/k 超群) が実データ化され、プレースホルダは不要になったため削除。
    //private TreeNode PendingNode() => new(Loc(en: "Phase 2 data pending", ja: "Phase 2 データ待ち", de: "Phase-2-Daten ausstehend", fr: "Données Phase 2 à venir", es: "Datos de Fase 2 pendientes", pt: "Dados da Fase 2 pendentes", it: "Dati Fase 2 in attesa", ru: "Данные фазы 2 ожидаются", zhHans: "Phase 2 数据待补", zhHant: "Phase 2 資料待補", ko: "Phase 2 데이터 대기")) { ForeColor = SystemColors.GrayText };
    private TreeNode NoneNode() => new(Loc(en: "none", ja: "なし", de: "keine", fr: "aucun", es: "ninguno", pt: "nenhum", it: "nessuno", ru: "нет", zhHans: "无", zhHant: "無", ko: "없음")) { ForeColor = SystemColors.GrayText };
    private TreeNode ComputingNode() => new(Loc(en: "computing…", ja: "計算中…", de: "wird berechnet…", fr: "calcul en cours…", es: "calculando…", pt: "calculando…", it: "calcolo in corso…", ru: "вычисляется…", zhHans: "计算中…", zhHant: "計算中…", ko: "계산 중…")) { ForeColor = SystemColors.GrayText }; // 260705Cl 追加
    private TreeNode FailedNode() => new(Loc(en: "computation failed", ja: "計算に失敗しました", de: "Berechnung fehlgeschlagen", fr: "échec du calcul", es: "el cálculo falló", pt: "o cálculo falhou", it: "calcolo non riuscito", ru: "вычисление не удалось", zhHans: "计算失败", zhHant: "計算失敗", ko: "계산 실패")) { ForeColor = SystemColors.GrayText }; // 260709Cl 追加 (codex R11: 失敗を空集合と区別)

    private enum NodeKind { Subgroup, Supergroup }
    private sealed class NodeTag
    {
        public NodeKind Kind;
        public GroupRelation Relation;   // Subgroup/Supergroup 共通 (260705Cl: 超群側も選択可能化。Phase 2 データ待ちの Pending/None/Computing ノードは null)
        public int TargetSeries = -1;
    }

    private void treeRelations_AfterSelect(object sender, TreeViewEventArgs e)
    {
        // 260705Cl 修正: 超群ノードも GroupRelation を持つため選択可能にする (Kind 判定を廃し Relation の有無だけで分岐)。
        if (e.Node.Tag is NodeTag { Relation: not null } tag)
        {
            //ShowRelationDetail(tag.Relation);
            ShowRelationDetail(tag.Relation, tag.Kind == NodeKind.Supergroup); // 260709Cl: 選択元 (超群側か) を明示 (self-isomorphic の向き判定)
            RenderGraph(); // 選択ノードのハイライト更新
        }
    }

    private void treeRelations_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
    {
        //if (e.Node.Tag is NodeTag tag && tag.TargetSeries >= 0)
        //    NavigateTo(tag.TargetSeries);
        // 260711Cl (codex R12): 経由関係と向きを履歴 hop に記録。series 同一 (self-isomorphic) への dblclick は
        // 「同型だが異なる埋め込み」を series では表現できないため履歴に積まない (選択のみで扱う)。
        if (e.Node.Tag is NodeTag tag && tag.TargetSeries >= 0 && tag.TargetSeries != _currentSeries)
            NavigateTo(tag.TargetSeries, via: tag.Relation);
    }
    #endregion

    #region 詳細タブの流し込み
    /// <summary>選択された部分群/超群関係の詳細を各タブに表示する。null なら空表示。260705Cl 修正 (Phase 2e):
    /// 超群 (Minimal supergroups) 側の関係は s.ParentSeriesNumber が _currentSeries と異なる (= 超群自身)。
    /// この場合は「子基準系から見た」向きに変換を反転表示する (viewFromChild)。
    /// 260709Cl シグネチャ変更 (旧: ShowRelationDetail(GroupRelation s)): fromSupergroupSide で選択元
    /// (超群側 = true) を明示できるようにした。self-isomorphic 関係 (同型、Parent==Child==現在 series) では
    /// 従来の「ParentSeriesNumber != _currentSeries」推定が破綻し、超群側から選んでも (P,p) が
    /// 部分群向きで表示される実バグがあった (codex 相談 R8)。null = 従来の推定 (互換)。</summary>
    private void ShowRelationDetail(GroupRelation s, bool? fromSupergroupSide = null)
    {
        _selectedRelation = s;
        if (s == null)
        {
            //labelRelTitle.Text = Loc(en: "Select a subgroup relation from the tree.", ja: "ツリーから部分群関係を選択してください。", de: "Wählen Sie eine Untergruppenrelation im Baum.", fr: "Sélectionnez une relation de sous-groupe dans l'arbre.", es: "Seleccione una relación de subgrupo en el árbol.", pt: "Selecione uma relação de subgrupo na árvore.", it: "Seleziona una relazione di sottogruppo nell'albero.", ru: "Выберите отношение подгруппы в дереве.", zhHans: "请从树中选择子群关系。", zhHant: "請從樹中選擇子群關係。", ko: "트리에서 부분군 관계를 선택하세요.");
            //labelMatrix.Text = "";
            labelLatex1.Text = @"\mathrm{Select\,a\,subgroup\,relation\,from\,the\,tree.}"; // 260706Ch: labelMatrix から LabelLaTeX 3 段表示へ移行
            labelLatex2.Text = "";
            labelLatex3.Text = "";
            miniTableGenerators.ClearRows();
            labelOrbitInfo.Text = ""; miniTableOrbit.ClearRows();
            labelDomains.Text = ""; miniTableTwins.ClearRows();
            labelReflInfo.Text = ""; miniTableReflections.ClearRows();
            RenderElements(); // 260713Cl (③-2): 選択解除時は「選択してください」注記を描く
            return;
        }

        //bool viewFromChild = s.ParentSeriesNumber != _currentSeries; // true = Minimal supergroups 側から選択
        // 260709Cl: 選択元が分かる呼び出し (ツリー/Diagram) からは明示値を使い、推定は互換フォールバックに降格。
        bool viewFromChild = fromSupergroupSide ?? (s.ParentSeriesNumber != _currentSeries);
        //var parent = SymmetryStatic.Symmetries[_currentSeries];
        //string otherName = viewFromChild
        //    ? SeitzNotation.PrettyHM(SymmetryStatic.Symmetries[s.ParentSeriesNumber].SpaceGroupHMStr)
        //    : (s.ChildSeriesNumber >= 0 ? SeitzNotation.PrettyHM(s.ChildLabel) : s.PointGroupHM);
        //string arrow = viewFromChild ? "←" : "→";
        //string supergroupTag = viewFromChild ? "  " + Loc(en: "(supergroup)", ja: "(超群)", de: "(Obergruppe)", fr: "(supergroupe)", es: "(supergrupo)", pt: "(supergrupo)", it: "(supergruppo)", ru: "(надгруппа)", zhHans: "(超群)", zhHant: "(超群)", ko: "(초군)") : "";
        //string kindTag = s.Kind == GroupRelationKind.K ? "k" : "t"; // 260705Cl 追加 (Phase 2c Step4)
        //labelRelTitle.Text = $"{SeitzNotation.PrettyHM(parent.SpaceGroupHMStr)}  {arrow}  {otherName}{supergroupTag}    ·    {kindTag}, index {s.Index}";
        labelLatex1.Text = BuildRelationLatex(s); // 260706Ch: 常に「超群 → 部分群」の向きで表示

        FillMatrixTab(s, viewFromChild);
        FillOrbitTab(s);
        FillDomainsTab(s);
        FillReflectionsTab(s);
        RenderElements(); // 260713Cl (③-2): 対称要素 lost/retained 重ね描き
    }

    private void FillMatrixTab(GroupRelation s, bool viewFromChild)
    {
        //var sb = new StringBuilder(); // 260706Ch: 旧 labelMatrix 文字列表示は labelLatex2/3 へ分割
        // 260705Cl 修正 (Phase 2e): Minimal supergroups 側から選択した場合は (P,p)⁻¹ を表示する
        // (格納値は逆引き元である supergroup 自身の部分群表のもの = 子基準系→親基準系の向き)。
        var (P, p) = viewFromChild ? s.GetInverseTransform() : (s.TransformP, s.TransformShift);
        if (P != null)
        {
            // 260706Ch: 以下 2 行の 11 言語 Loc() 文言 (Transformation to supergroup/child basis の説明文と
            // 「以下の操作は超群自身の設定で表示しています。」注記) は BuildTransformationLatex の英語ハードコード
            // \mathrm{} へ置き換えられ、非ラテン文字言語 (ja/zh/ko/ru 等) の翻訳が失われている (i18n 退行、要修正)。
            // WpfMath の LaTeX パーサは \mathrm{} 内でも CJK/Cyrillic を解釈できず TexParseException を投げるため、
            // 単純に Loc() の戻り値をそのまま埋め込むことはできない (検証済み)。対応方針は要検討 (別途ユーザー確認)。
            //sb.AppendLine(viewFromChild
            //    ? Loc(en: "Transformation to supergroup basis  (a',b',c') = (a,b,c)·P⁻¹  (derived from the supergroup's own subgroup table)", ja: "超群基底への変換  (a',b',c') = (a,b,c)·P⁻¹  (超群自身の部分群表から算出)", de: "Transformation zur Obergruppenbasis  (a',b',c') = (a,b,c)·P⁻¹  (aus der eigenen Untergruppentabelle der Obergruppe abgeleitet)", fr: "Transformation vers la base du supergroupe  (a',b',c') = (a,b,c)·P⁻¹  (dérivée de la table des sous-groupes du supergroupe)", es: "Transformación a la base del supergrupo  (a',b',c') = (a,b,c)·P⁻¹  (derivada de la tabla de subgrupos del propio supergrupo)", pt: "Transformação para a base do supergrupo  (a',b',c') = (a,b,c)·P⁻¹  (derivada da tabela de subgrupos do próprio supergrupo)", it: "Trasformazione alla base del supergruppo  (a',b',c') = (a,b,c)·P⁻¹  (derivata dalla tabella dei sottogruppi del supergruppo stesso)", ru: "Преобразование к базису надгруппы  (a',b',c') = (a,b,c)·P⁻¹  (получено из таблицы подгрупп самой надгруппы)", zhHans: "到超群基底的变换  (a',b',c') = (a,b,c)·P⁻¹  (由超群自身的子群表推得)", zhHant: "到超群基底的變換  (a',b',c') = (a,b,c)·P⁻¹  (由超群自身的子群表推得)", ko: "초군 기저 변환  (a',b',c') = (a,b,c)·P⁻¹  (초군 자체의 부분군 표에서 산출)")
            //    : Loc(en: "Transformation to child basis  (a',b',c') = (a,b,c)·P", ja: "子基底への変換  (a',b',c') = (a,b,c)·P", de: "Transformation zur Kindbasis  (a',b',c') = (a,b,c)·P", fr: "Transformation vers la base fille  (a',b',c') = (a,b,c)·P", es: "Transformación a base hija  (a',b',c') = (a,b,c)·P", pt: "Transformação para base filha  (a',b',c') = (a,b,c)·P", it: "Trasformazione alla base figlia  (a',b',c') = (a,b,c)·P", ru: "Преобразование к базису подгруппы  (a',b',c') = (a,b,c)·P", zhHans: "到子基底的变换  (a',b',c') = (a,b,c)·P", zhHant: "到子基底的變換  (a',b',c') = (a,b,c)·P", ko: "자식 기저 변환  (a',b',c') = (a,b,c)·P"));
            //sb.AppendLine();
            //for (int r = 0; r < 3; r++)
            //    sb.AppendLine($"   | {Frac(P[r * 3]),6}  {Frac(P[r * 3 + 1]),6}  {Frac(P[r * 3 + 2]),6} |     p{(r == 1 ? " =" : "  ")} {Frac(p[r]),6}");
            //if (viewFromChild)
            //{
            //    sb.AppendLine();
            //    sb.AppendLine(Loc(en: "Operations below are expressed in the supergroup's own setting.", ja: "以下の操作は超群自身の設定で表示しています。", de: "Die Operationen unten sind in der eigenen Aufstellung der Obergruppe angegeben.", fr: "Les opérations ci-dessous sont exprimées dans le repère propre du supergroupe.", es: "Las operaciones siguientes se expresan en el propio ajuste del supergrupo.", pt: "As operações abaixo são expressas na própria configuração do supergrupo.", it: "Le operazioni sottostanti sono espresse nella impostazione propria del supergruppo.", ru: "Операции ниже приведены в собственной установке надгруппы.", zhHans: "以下操作以超群自身的设置表示。", zhHant: "以下操作以超群自身的設定表示。", ko: "아래 연산은 초군 자체의 설정으로 표시됩니다."));
            //}
            labelLatex2.Text = BuildTransformationLatex(viewFromChild);
            labelLatex3.Text = BuildMatrixLatex(P, p);
        }
        else
        {
            // 260706Ch: 同上。以下は英語ハードコードで、旧 11 言語 Loc() 文言からの退行。
            //sb.AppendLine(Loc(en: "Type not resolved from the operation catalogue; point group " + s.PointGroupHM + " only.", ja: "変換カタログから型を同定できませんでした (点群 " + s.PointGroupHM + " のみ)。", de: "Typ nicht aufgelöst; nur Punktgruppe " + s.PointGroupHM + ".", fr: "Type non résolu ; seulement le groupe ponctuel " + s.PointGroupHM + ".", es: "Tipo no resuelto; solo grupo puntual " + s.PointGroupHM + ".", pt: "Tipo não resolvido; apenas grupo pontual " + s.PointGroupHM + ".", it: "Tipo non risolto; solo gruppo puntuale " + s.PointGroupHM + ".", ru: "Тип не определён; только точечная группа " + s.PointGroupHM + ".", zhHans: "未能识别类型；仅点群 " + s.PointGroupHM + "。", zhHant: "未能識別類型；僅點群 " + s.PointGroupHM + "。", ko: "유형 미확인; 점군 " + s.PointGroupHM + "만."));
            labelLatex2.Text = $@"\mathrm{{Type\,not\,resolved;\,point\,group}}\,\, {HmToLatex(s.PointGroupHM)}\,\, \mathrm{{only.}}";
            labelLatex3.Text = "";
        }
        // 260706Ch: 「この類の共役部分群数: N」(s.ConjugateCount) の表示行は labelLatex2/3 への移行時に
        // 表示先を失ったまま残っている。UI 上この情報は現在どこにも表示されていない (機能欠落、要確認)。
        //sb.AppendLine();
        //sb.Append(Loc(en: "Conjugate subgroups in this class: ", ja: "この類の共役部分群数: ", de: "Konjugierte Untergruppen dieser Klasse: ", fr: "Sous-groupes conjugués de cette classe : ", es: "Subgrupos conjugados de esta clase: ", pt: "Subgrupos conjugados desta classe: ", it: "Sottogruppi coniugati di questa classe: ", ru: "Сопряжённых подгрупп в классе: ", zhHans: "本类共轭子群数: ", zhHant: "本類共軛子群數: ", ko: "이 클래스의 켤레 부분군 수: ") + s.ConjugateCount);
        //labelMatrix.Text = sb.ToString();

        // Retained / Lost generators
        var rows = new List<object[]>();
        string retStr = Loc(en: "retained", ja: "保持", de: "erhalten", fr: "conservé", es: "conservado", pt: "mantido", it: "mantenuto", ru: "сохранено", zhHans: "保持", zhHant: "保持", ko: "유지");
        string lostStr = Loc(en: "lost", ja: "消失", de: "verloren", fr: "perdu", es: "perdido", pt: "perdido", it: "perso", ru: "утрачено", zhHans: "消失", zhHant: "消失", ko: "소실");
        foreach (var op in s.Representatives)
            //rows.Add([FormSymmetryInformation.SeitzToLatex(SeitzNotation.Seitz(op)), SeitzNotation.GeometricType(op), retStr]); // 260708Ch: SeitzNotation.SeitzLatex に一本化 (構造化データから直接生成)
            rows.Add([SeitzNotation.SeitzLatex(op), SeitzNotation.GeometricType(op), retStr]); // 260708Ch
        foreach (var op in s.CosetRepresentatives)
            //rows.Add([FormSymmetryInformation.SeitzToLatex(SeitzNotation.Seitz(op)), SeitzNotation.GeometricType(op), lostStr]); // 260708Ch
            rows.Add([SeitzNotation.SeitzLatex(op), SeitzNotation.GeometricType(op), lostStr]); // 260708Ch
        miniTableGenerators.SetRows(rows);
    }

    // 260708Cl: k-/isomorphic の全タブ実データ化に伴い KNotSupportedMessage() (ガード文言) は不要になったため削除。
    //private static string KNotSupportedMessage() => Loc(en: "Not yet available for klassengleiche (k-) relations — needs dedicated logic for the coarser translation lattice (planned for a later phase).", ja: "klassengleiche (k-) 関係ではまだ未対応です (並進格子が粗くなるため専用ロジックが必要、今後のPhaseで対応予定)。", ...);

    private void FillOrbitTab(GroupRelation s)
    {
        //if (s.Kind == GroupRelationKind.Isomorphic) // 260708Cl: ガード解除 — 同型は klassengleiche の特殊例で k ロジックがそのまま正しい
        //{
        //    labelOrbitInfo.Text = KNotSupportedMessage();
        //    miniTableOrbit.ClearRows();
        //    return;
        //}
        // 260708Cl (Phase 2d): k- も実データ化。k- は並進喪失で軌道が分裂し、多重度は拡大した部分群胞基準。
        labelOrbitInfo.Text = s.ChildSeriesNumber < 0
            ? Loc(en: "Child type unresolved — orbit letters unavailable.", ja: "子の型が未同定のため Wyckoff 文字は表示できません。", de: "Kindtyp ungelöst — keine Lagesymbole.", fr: "Type fille non résolu — lettres indisponibles.", es: "Tipo hija sin resolver — letras no disponibles.", pt: "Tipo filho não resolvido — letras indisponíveis.", it: "Tipo figlio non risolto — lettere non disponibili.", ru: "Тип подгруппы не определён — буквы недоступны.", zhHans: "子类型未识别 — 无法显示字母。", zhHant: "子類型未識別 — 無法顯示字母。", ko: "자식 유형 미확인 — 문자 표시 불가.")
            : s.Kind != GroupRelationKind.T // 260708Cl: Isomorphic も k 文言 (旧: == GroupRelationKind.K)
                ? Loc(en: "How each parent Wyckoff orbit splits as lattice translations are lost (sampled with a generic point); multiplicities are given in the enlarged subgroup cell.", ja: "並進対称の喪失に伴い親の各 Wyckoff 軌道がどう分裂するか (generic 点でのサンプル計算)。多重度は拡大した部分群胞基準です。", de: "Wie sich jede Wyckoff-Lage des Elters beim Verlust von Gittertranslationen aufspaltet (Stichprobe mit generischem Punkt); Multiplizitäten in der vergrößerten Untergruppenzelle.", fr: "Comment chaque orbite de Wyckoff du parent se scinde lorsque des translations de réseau sont perdues (échantillon, point générique) ; les multiplicités sont données dans la maille agrandie du sous-groupe.", es: "Cómo se divide cada órbita de Wyckoff del padre al perderse traslaciones de red (muestreo con punto genérico); las multiplicidades se dan en la celda ampliada del subgrupo.", pt: "Como cada órbita de Wyckoff do pai se divide quando translações de rede são perdidas (amostragem com ponto genérico); as multiplicidades são dadas na célula ampliada do subgrupo.", it: "Come ogni orbita di Wyckoff del genitore si suddivide con la perdita di traslazioni reticolari (campione, punto generico); le molteplicità sono date nella cella ingrandita del sottogruppo.", ru: "Как расщепляется каждая орбита Уайкоффа родителя при потере трансляций решётки (выборка, общая точка); кратности даны в увеличенной ячейке подгруппы.", zhHans: "随着点阵平移的丧失，母群各 Wyckoff 轨道如何分裂 (通用点采样)；多重度以扩大的子群胞为准。", zhHant: "隨著點陣平移的喪失，母群各 Wyckoff 軌道如何分裂 (通用點取樣)；多重度以擴大的子群胞為準。", ko: "격자 병진이 사라지면서 부모의 각 Wyckoff 궤도가 어떻게 분열하는지 (일반점 샘플링); 다중도는 확대된 부분군 셀 기준입니다.")
                : Loc(en: "How each Wyckoff orbit of the parent splits (sampled with a generic point).", ja: "親の各 Wyckoff 軌道の分裂 (generic 点でのサンプル計算)。", de: "Aufspaltung jeder Wyckoff-Lage des Elters (Stichprobe mit generischem Punkt).", fr: "Éclatement de chaque orbite de Wyckoff du parent (échantillon, point générique).", es: "División de cada órbita de Wyckoff del padre (muestreo con punto genérico).", pt: "Divisão de cada órbita de Wyckoff do pai (amostragem com ponto genérico).", it: "Suddivisione di ogni orbita di Wyckoff del genitore (campione, punto generico).", ru: "Расщепление каждой орбиты Уайкоффа родителя (выборка, общая точка).", zhHans: "母群各 Wyckoff 轨道的分裂 (通用点采样)。", zhHant: "母群各 Wyckoff 軌道的分裂 (通用點取樣)。", ko: "부모의 각 Wyckoff 궤도 분열 (일반점 샘플링).");

        // 260705Cl 修正 (Phase 2e): _currentSeries でなく s.ParentSeriesNumber を使う。Maximal subgroups 側では
        // 常に一致するが、Minimal supergroups 側では s.ParentSeriesNumber (= 超群自身) が正しい分裂元になる。
        var wycks = SymmetryStatic.WyckoffPositions[s.ParentSeriesNumber];
        var split = TSubgroupFinder.GetOrbitSplitting(s.ParentSeriesNumber, s);
        var rows = new List<object[]>();
        for (int w = 0; w < wycks.Length; w++)
        {
            //string parent = $"{wycks[w].Multiplicity}{wycks[w].WyckoffLetter}  {wycks[w].SiteSymmetry}";
            //string child = string.Join(" + ", split[w].Select(p =>
            //    p.ChildWyckoffLetter != null ? $"{p.ChildMultiplicity}{p.ChildWyckoffLetter}" : $"{p.CountInParentCell}·"));
            //string sites = s.ChildSeriesNumber >= 0
            //    ? string.Join(", ", split[w].Select(p => p.ChildSiteSymmetry).Distinct())
            //    : "";
            string parent = WyckoffLatex(wycks[w].Multiplicity, wycks[w].WyckoffLetter, wycks[w].SiteSymmetry); // 260706Ch
            string child = string.Join(" + ", split[w].Select(p =>
                p.ChildWyckoffLetter != null ? WyckoffLatex(p.ChildMultiplicity, p.ChildWyckoffLetter) : $@"{p.CountInParentCell}\cdot")); // 260706Ch
            string sites = s.ChildSeriesNumber >= 0
                ? string.Join(", ", split[w].Select(p => p.ChildSiteSymmetry).Distinct().Select(HmToLatex)) // 260706Ch
                : "";
            rows.Add([parent, child, split[w].Length.ToString(), sites]);
        }
        miniTableOrbit.SetRows(rows);
    }

    private void FillDomainsTab(GroupRelation s)
    {
        // 260708Cl (Phase 2d): k- (klassengleiche) は点群が変わらないため方位 (双晶) ドメインは常に 1。
        // 並進対称の喪失 [T:T′]=index により反位相 (並進) ドメインが index 個生じ、ドメインを結ぶのは失われた
        // 格子並進 (CosetRepresentatives、純並進操作) である。これらは方位を変えないので基本反射は重なり合い、
        // 超格子反射で位相が干渉する。t- 前提の共役類ベースの計数式は流用しない。
        if (s.Kind != GroupRelationKind.T) // 260708Cl: Isomorphic も k ロジック (旧: == GroupRelationKind.K)
        {
            int totalK = s.Index;
            var sbK = new StringBuilder();
            sbK.AppendLine(Loc(en: "Domain states on this transition:", ja: "この転移でのドメイン状態:", de: "Domänenzustände bei diesem Übergang:", fr: "États de domaine pour cette transition :", es: "Estados de dominio en esta transición:", pt: "Estados de domínio nesta transição:", it: "Stati di dominio in questa transizione:", ru: "Доменные состояния при этом переходе:", zhHans: "此相变的畴态:", zhHant: "此相變的疇態:", ko: "이 전이의 도메인 상태:"));
            sbK.AppendLine($"   {Loc(en: "Total", ja: "総数", de: "Gesamt", fr: "Total", es: "Total", pt: "Total", it: "Totale", ru: "Всего", zhHans: "总数", zhHant: "總數", ko: "전체")} = {totalK}      " +
                           $"{Loc(en: "orientation", ja: "方位", de: "Orientierung", fr: "orientation", es: "orientación", pt: "orientação", it: "orientazione", ru: "ориентация", zhHans: "取向", zhHant: "取向", ko: "방위")} = 1      " +
                           $"{Loc(en: "antiphase", ja: "反位相", de: "Antiphase", fr: "antiphase", es: "antifase", pt: "antifase", it: "antifase", ru: "антифаза", zhHans: "反相", zhHant: "反相", ko: "반위상")} = {totalK}");
            sbK.AppendLine();
            sbK.Append(Loc(en: "Antiphase (translation) domains are related by the lattice translations lost in the subgroup; they keep the same orientation, so the fundamental reflections coincide while the superlattice reflections interfere.", ja: "反位相 (並進) ドメインは、部分群で失われた格子並進によって関係づけられます。方位は同一なので基本反射は重なり合い、超格子反射で位相が干渉します。", de: "Antiphasen- (Translations-)Domänen sind durch die in der Untergruppe verlorenen Gittertranslationen verbunden; sie behalten dieselbe Orientierung, sodass die Grundreflexe zusammenfallen, während die Überstrukturreflexe interferieren.", fr: "Les domaines d'antiphase (de translation) sont reliés par les translations de réseau perdues dans le sous-groupe ; ils conservent la même orientation, de sorte que les réflexions fondamentales coïncident tandis que les réflexions de surstructure interfèrent.", es: "Los dominios de antifase (de traslación) están relacionados por las traslaciones de red perdidas en el subgrupo; mantienen la misma orientación, por lo que las reflexiones fundamentales coinciden mientras que las reflexiones de superestructura interfieren.", pt: "Os domínios de antifase (de translação) estão relacionados pelas translações de rede perdidas no subgrupo; mantêm a mesma orientação, de modo que as reflexões fundamentais coincidem enquanto as reflexões de superestrutura interferem.", it: "I domini di antifase (di traslazione) sono legati dalle traslazioni reticolari perse nel sottogruppo; mantengono la stessa orientazione, quindi le riflessioni fondamentali coincidono mentre le riflessioni di superstruttura interferiscono.", ru: "Антифазные (трансляционные) домены связаны трансляциями решётки, утраченными в подгруппе; они сохраняют одинаковую ориентацию, поэтому основные рефлексы совпадают, а сверхструктурные отражения интерферируют.", zhHans: "反相 (平移) 畴由子群中失去的点阵平移相联系；它们取向相同，故基本反射重合，而超结构反射发生干涉。", zhHant: "反相 (平移) 疇由子群中失去的點陣平移相聯繫；它們取向相同，故基本反射重合，而超結構反射發生干涉。", ko: "반위상 (병진) 도메인은 부분군에서 잃어버린 격자 병진으로 연결됩니다. 방위가 동일하므로 기본 반사는 겹치지만 초격자 반사에서 위상이 간섭합니다."));
            labelDomains.Text = sbK.ToString();

            var rowsK = new List<object[]>();
            //foreach (var op in s.CosetRepresentatives)
            //    rowsK.Add([SeitzNotation.SeitzLatex(op), SeitzNotation.GeometricType(op)]); // 260708Cl: SeitzLatex/GeometricType は親胞 mod1 で並進を還元するため、IIb (胞拡大) の失われた整数格子並進 (例 {1|1,0,0}) が {1|0,0,0} Identity と表示される実バグ (実 GUI 目視で発覚)
            var (Pinv, _) = s.GetInverseTransform(); // 260708Cl: 未同定 (TransformP=null) なら (null, null)
            foreach (var op in s.CosetRepresentatives)
            {
                // 260708Cl: 無還元の親並進をそのまま組版し、右列に部分群胞座標での反位相ベクトル Frac(P⁻¹·t) を添える。
                var t = op.SeitzTranslation;
                string seitz = $@"\{{ 1 \mid {Frac(t.U)},\,{Frac(t.V)},\,{Frac(t.W)} \}}";
                string desc = Loc(en: "Lost lattice translation", ja: "失われた格子並進", de: "Verlorene Gittertranslation", fr: "Translation de réseau perdue", es: "Traslación de red perdida", pt: "Translação de rede perdida", it: "Traslazione reticolare persa", ru: "Утраченная трансляция решётки", zhHans: "失去的点阵平移", zhHant: "失去的點陣平移", ko: "잃어버린 격자 병진");
                if (Pinv != null)
                {
                    double cx = Frac01(Pinv[0] * t.U + Pinv[1] * t.V + Pinv[2] * t.W);
                    double cy = Frac01(Pinv[3] * t.U + Pinv[4] * t.V + Pinv[5] * t.W);
                    double cz = Frac01(Pinv[6] * t.U + Pinv[7] * t.V + Pinv[8] * t.W);
                    desc += $"  →  ({Frac(cx)}, {Frac(cy)}, {Frac(cz)}) " + Loc(en: "in the subgroup cell", ja: "(部分群胞座標)", de: "in der Untergruppenzelle", fr: "dans la maille du sous-groupe", es: "en la celda del subgrupo", pt: "na célula do subgrupo", it: "nella cella del sottogruppo", ru: "в ячейке подгруппы", zhHans: "(子群胞坐标)", zhHant: "(子群胞座標)", ko: "(부분군 셀 기준)");
                }
                rowsK.Add([seitz, desc]);
            }
            if (rowsK.Count == 0)
                rowsK.Add([Loc(en: "(single domain)", ja: "(単一ドメイン)", de: "(Einzeldomäne)", fr: "(domaine unique)", es: "(dominio único)", pt: "(domínio único)", it: "(dominio singolo)", ru: "(один домен)", zhHans: "(单畴)", zhHant: "(單疇)", ko: "(단일 도메인)"), ""]);
            miniTableTwins.SetRows(rowsK);
            return;
        }
        //if (s.Kind != GroupRelationKind.T) // 260708Cl: 上の分岐が != T になり到達不能のため削除 (isomorphic ガード)
        //{
        //    labelDomains.Text = KNotSupportedMessage();
        //    miniTableTwins.ClearRows();
        //    return;
        //}
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
            //rows.Add([FormSymmetryInformation.SeitzToLatex(SeitzNotation.Seitz(op)), SeitzNotation.GeometricType(op)]); // 260708Ch: SeitzNotation.SeitzLatex に一本化 (構造化データから直接生成)
            rows.Add([SeitzNotation.SeitzLatex(op), SeitzNotation.GeometricType(op)]); // 260708Ch
        if (rows.Count == 0)
            rows.Add([Loc(en: "(single domain)", ja: "(単一ドメイン)", de: "(Einzeldomäne)", fr: "(domaine unique)", es: "(dominio único)", pt: "(domínio único)", it: "(dominio singolo)", ru: "(один домен)", zhHans: "(单畴)", zhHant: "(單疇)", ko: "(단일 도메인)"), ""]);
        miniTableTwins.SetRows(rows);
    }

    private void FillReflectionsTab(GroupRelation s)
    {
        //if (s.Kind == GroupRelationKind.Isomorphic) // 260708Cl: ガード解除 — 同型は k ロジックで実データ化
        //{
        //    labelReflInfo.Text = KNotSupportedMessage();
        //    miniTableReflections.ClearRows();
        //    return;
        //}
        // 260708Cl (Phase 2d): k- は超格子反射を実データ化。子が未同定なら子の消滅則を判定できず予測不可。
        if (s.Kind != GroupRelationKind.T && s.ChildSeriesNumber < 0) // 260708Cl: Isomorphic も対象 (旧: == GroupRelationKind.K)
        {
            labelReflInfo.Text = Loc(en: "Child type unresolved — new reflections cannot be predicted.", ja: "子の型が未同定のため新規反射を予測できません。", de: "Kindtyp ungelöst — neue Reflexe nicht vorhersagbar.", fr: "Type fille non résolu — nouvelles réflexions imprévisibles.", es: "Tipo hija sin resolver — no se pueden predecir nuevas reflexiones.", pt: "Tipo filho não resolvido — não é possível prever novas reflexões.", it: "Tipo figlio non risolto — nuove riflessioni non prevedibili.", ru: "Тип подгруппы не определён — новые отражения нельзя предсказать.", zhHans: "子类型未识别 — 无法预测新反射。", zhHant: "子類型未識別 — 無法預測新反射。", ko: "자식 유형 미확인 — 새 반사를 예측할 수 없습니다.");
            miniTableReflections.ClearRows();
            return;
        }
        // 260705Cl 修正 (Phase 2e): FillOrbitTab と同じ理由で s.ParentSeriesNumber を使う。
        //var refl = TSubgroupFinder.GetNewReflections(s.ParentSeriesNumber, s, 4);
        int maxIdx = (int)numericReflMax.Value; // 260709Cl: 探索窓 |h|,|k|,|l| ≤ n をスピナーで調整可能に (旧: 4 固定)
        var refl = TSubgroupFinder.GetNewReflections(s.ParentSeriesNumber, s, maxIdx);
        if (s.Kind != GroupRelationKind.T) // 260708Cl: Isomorphic も k 文言 (旧: == GroupRelationKind.K)
            // 260708Cl (Phase 2d): k- は胞拡大で超格子反射が現れる。右端列は超格子=親の分数指数 "(…)"、消滅則解除=解除された親の消滅則。
            labelReflInfo.Text = refl.Length == 0
                // 260709Cl: 探索窓の "≤ 4" ハードコードを {0} (スピナー値) へ
                ? string.Format(Loc(en: "No new reflections on the subgroup cell (|h,k,l| ≤ {0}).", ja: "部分群胞での新規反射はありません (|h,k,l| ≤ {0})。", de: "Keine neuen Reflexe in der Untergruppenzelle (|h,k,l| ≤ {0}).", fr: "Aucune nouvelle réflexion sur la maille du sous-groupe (|h,k,l| ≤ {0}).", es: "Sin nuevas reflexiones en la celda del subgrupo (|h,k,l| ≤ {0}).", pt: "Sem novas reflexões na célula do subgrupo (|h,k,l| ≤ {0}).", it: "Nessuna nuova riflessione nella cella del sottogruppo (|h,k,l| ≤ {0}).", ru: "Нет новых отражений в ячейке подгруппы (|h,k,l| ≤ {0}).", zhHans: "子群胞中无新反射 (|h,k,l| ≤ {0})。", zhHant: "子群胞中無新反射 (|h,k,l| ≤ {0})。", ko: "부분군 셀에 새로운 반사가 없습니다 (|h,k,l| ≤ {0})."), maxIdx)
                : string.Format(Loc(en: "{0} new reflections (up to symmetry) appear on the enlarged subgroup cell. The last column gives the parent fractional index in parentheses for superlattice reflections, or the lifted parent extinction rule for released ones. Intensity still depends on the structure factor.", ja: "{0} 本 (対称等価を除く) の新規反射が拡大した部分群胞に現れます。右端の列は、超格子反射では親の分数指数を括弧付きで、消滅則解除では解除された親の消滅則を示します。強度は構造因子に依存します。", de: "{0} neue Reflexe (bis auf Symmetrie) erscheinen in der vergrößerten Untergruppenzelle. Die letzte Spalte zeigt den Elternindex in Klammern (Überstrukturreflexe) bzw. die aufgehobene Auslöschungsregel des Elters (freigegebene Reflexe). Die Intensität hängt weiter vom Strukturfaktor ab.", fr: "{0} nouvelles réflexions (à symétrie près) apparaissent sur la maille agrandie du sous-groupe. La dernière colonne indique l'indice fractionnaire du parent entre parenthèses (réflexions de surstructure) ou la règle d'extinction du parent levée (réflexions libérées). L'intensité dépend du facteur de structure.", es: "{0} nuevas reflexiones (salvo simetría) aparecen en la celda ampliada del subgrupo. La última columna muestra el índice fraccionario del padre entre paréntesis (reflexiones de superestructura) o la regla de extinción del padre levantada (reflexiones liberadas). La intensidad depende del factor de estructura.", pt: "{0} novas reflexões (a menos de simetria) aparecem na célula ampliada do subgrupo. A última coluna mostra o índice fracionário do pai entre parênteses (reflexões de superestrutura) ou a regra de extinção do pai levantada (reflexões liberadas). A intensidade depende do fator de estrutura.", it: "{0} nuove riflessioni (a meno di simmetria) compaiono nella cella ingrandita del sottogruppo. L'ultima colonna riporta l'indice frazionario del genitore tra parentesi (riflessioni di superstruttura) o la regola di estinzione del genitore rimossa (riflessioni liberate). L'intensità dipende dal fattore di struttura.", ru: "{0} новых отражений (с точностью до симметрии) появляются в увеличенной ячейке подгруппы. В последнем столбце указан дробный индекс родителя в скобках (сверхструктурные отражения) или снятое правило погасания родителя (освобождённые отражения). Интенсивность зависит от структурного фактора.", zhHans: "{0} 个 (对称等价除外) 新反射出现在扩大的子群胞上。最后一列对超结构反射给出括号内的母群分数指数，对释放反射给出被解除的母群消光条件。强度仍取决于结构因子。", zhHant: "{0} 個 (對稱等價除外) 新反射出現在擴大的子群胞上。最後一列對超結構反射給出括號內的母群分數指數，對釋放反射給出被解除的母群消光條件。強度仍取決於結構因子。", ko: "{0}개 (대칭 제외) 새 반사가 확대된 부분군 셀에 나타납니다. 마지막 열은 초격자 반사의 경우 괄호 안의 부모 분수 지수를, 해제된 반사의 경우 해제된 부모 소멸 규칙을 보여줍니다. 강도는 구조 인자에 따릅니다."), refl.Length);
        else
            labelReflInfo.Text = refl.Length == 0
                // 260705Cl 修正: t-部分群は格子周期を変えないため「超構造(superstructure)」反射ではない
                // (真の超格子反射は k-部分群で初めて生じる。codex レビューで指摘)。全言語でタブ見出し (New reflections)
                // と揃う中立表現に統一。
                // 260709Cl: 探索窓の "≤ 4" ハードコードを {0} (スピナー値) へ
                ? string.Format(Loc(en: "No new reflections: the subgroup lifts no systematic absence of the parent (|h,k,l| ≤ {0}).", ja: "新規反射なし: この部分群は親の系統的消滅を解除しません (|h,k,l| ≤ {0})。", de: "Keine neuen Reflexe: Die Untergruppe hebt keine Auslöschung des Elters auf (|h,k,l| ≤ {0}).", fr: "Aucune nouvelle réflexion : le sous-groupe ne lève aucune extinction du parent (|h,k,l| ≤ {0}).", es: "Sin nuevas reflexiones: el subgrupo no levanta ninguna ausencia del padre (|h,k,l| ≤ {0}).", pt: "Sem novas reflexões: o subgrupo não levanta nenhuma ausência do pai (|h,k,l| ≤ {0}).", it: "Nessuna nuova riflessione: il sottogruppo non rimuove assenze del genitore (|h,k,l| ≤ {0}).", ru: "Нет новых отражений: подгруппа не снимает погасаний родителя (|h,k,l| ≤ {0}).", zhHans: "无新反射: 该子群未解除母群的系统消光 (|h,k,l| ≤ {0})。", zhHant: "無新反射: 該子群未解除母群的系統消光 (|h,k,l| ≤ {0})。", ko: "새로운 반사 없음: 이 부분군은 부모의 소멸을 해제하지 않습니다 (|h,k,l| ≤ {0})."), maxIdx)
                : string.Format(Loc(en: "{0} reflections (up to symmetry) become allowed. Intensity still depends on the structure factor.", ja: "{0} 本 (対称等価を除く) の反射が許容になります。強度は構造因子に依存します。", de: "{0} Reflexe (bis auf Symmetrie) werden erlaubt. Die Intensität hängt weiter vom Strukturfaktor ab.", fr: "{0} réflexions (à symétrie près) deviennent autorisées. L'intensité dépend du facteur de structure.", es: "{0} reflexiones (salvo simetría) se permiten. La intensidad depende del factor de estructura.", pt: "{0} reflexões (a menos de simetria) tornam-se permitidas. A intensidade depende do fator de estrutura.", it: "{0} riflessioni (a meno di simmetria) diventano permesse. L'intensità dipende dal fattore di struttura.", ru: "{0} отражений (с точностью до симметрии) становятся разрешёнными. Интенсивность зависит от структурного фактора.", zhHans: "{0} 个 (对称等价除外) 反射变为允许。强度仍取决于结构因子。", zhHant: "{0} 個 (對稱等價除外) 反射變為允許。強度仍取決於結構因子。", ko: "{0}개 (대칭 제외) 반사가 허용됩니다. 강도는 구조 인자에 따릅니다."), refl.Length);

        var rows = new List<object[]>();
        foreach (var r in refl)
            rows.Add([$"{r.H} {r.K} {r.L}", r.EquivCount.ToString(), r.ParentRule]); // 260705Cl: 素通し helper Hkl をインライン化
        miniTableReflections.SetRows(rows);
    }

    /// <summary>260709Cl 追加: 反射探索窓 (|h|,|k|,|l| ≤ n) の変更で New reflections タブを再計算する。</summary>
    private void numericReflMax_ValueChanged(object sender, EventArgs e)
    {
        if (_selectedRelation != null)
            FillReflectionsTab(_selectedRelation);
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

    // 260709Cl 全面改修: k-/isomorphic 辺を追加 (codex 相談 R8。旧実装は t- のみ描画していた)。
    //private void DrawGraph(Graphics g, int w, int h)
    //{
    //    // 3 段レイアウト: 上=超群, 中=現在, 下=部分群。ノード = 群、辺 = t 関係 (index ラベル)。
    //    var cur = SymmetryStatic.Symmetries[_currentSeries];
    //    int midY = h / 2, topY = (int)(h * 0.16), botY = (int)(h * 0.84);
    //    var nodeSize = new Size(88, 40);
    //    var curRect = NodeRect(w / 2, midY, nodeSize);
    //    var superList = _supers.Take(6).ToList();
    //    var superRects = SpreadRects(superList.Count, w, topY, nodeSize);
    //    var subList = _subs.ToList();
    //    var subRects = SpreadRects(subList.Count, w, botY, nodeSize);
    //    using var edgePen = new Pen(Color.FromArgb(150, 160, 175), 1.4f);
    //    using var edgeFont = new Font("Segoe UI", 8f, FontStyle.Bold);
    //    using var labelBg = new SolidBrush(Color.White);
    //    using var tBrush = new SolidBrush(Color.FromArgb(47, 111, 179));
    //    for (int i = 0; i < superRects.Count; i++)
    //    {
    //        DrawEdge(g, edgePen, Center(superRects[i]), Center(curRect));
    //        DrawEdgeLabel(g, edgeFont, tBrush, labelBg, Mid(Center(superRects[i]), Center(curRect)), $"t{superList[i].Index}");
    //    }
    //    for (int i = 0; i < subRects.Count; i++)
    //    {
    //        DrawEdge(g, edgePen, Center(curRect), Center(subRects[i]));
    //        DrawEdgeLabel(g, edgeFont, tBrush, labelBg, Mid(Center(curRect), Center(subRects[i])), $"t{subList[i].Index}");
    //    }
    //    for (int i = 0; i < superRects.Count; i++)
    //    {
    //        bool selSuper = _selectedRelation != null && ReferenceEquals(_selectedRelation, superList[i]);
    //        DrawNode(g, superRects[i], SymmetryStatic.Symmetries[superList[i].ParentSeriesNumber], false, selSuper, superList[i].ParentSeriesNumber);
    //    }
    //    for (int i = 0; i < subRects.Count; i++)
    //    {
    //        bool sel = _selectedRelation != null && ReferenceEquals(_selectedRelation, subList[i]);
    //        DrawNode(g, subRects[i], subList[i].ChildSeriesNumber >= 0 ? SymmetryStatic.Symmetries[subList[i].ChildSeriesNumber] : default, false, sel,
    //                 subList[i].ChildSeriesNumber, subList[i].ChildSeriesNumber < 0 ? subList[i].PointGroupHM : null);
    //    }
    //    DrawNode(g, curRect, cur, true, false, _currentSeries);
    //}

    /// <summary>関係種別ごとの辺ラベル色 (白背景で判別しやすく色覚多様性に配慮した 3 色、codex R8 推奨)。</summary>
    private static Color KindColor(GroupRelationKind kind) => kind switch
    {
        GroupRelationKind.K => Color.FromArgb(0, 121, 107),          // teal
        GroupRelationKind.Isomorphic => Color.FromArgb(166, 90, 0),  // burnt orange
        _ => Color.FromArgb(47, 111, 179),                           // t = 既存の青
    };

    private static char KindChar(GroupRelationKind kind) => kind switch { GroupRelationKind.K => 'k', GroupRelationKind.Isomorphic => 'i', _ => 't' };

    /// <summary>260709Cl 追加: 関係リストを Diagram ノード単位へ集約する。既知の子 (series ≥ 0) は
    /// (対象 series, Kind, Index) が同じ非共役クラスを 1 ノードにまとめる (ツリーが類ごとの詳細を担う)。
    /// 未同定 (ChildSeriesNumber = -1) は異なるクラスを同一視しないよう集約しない (codex R8)。挿入順を保つ。</summary>
    private static List<List<GroupRelation>> AggregateForGraph(IEnumerable<GroupRelation> rels, bool bySupergroup)
    {
        var agg = new List<List<GroupRelation>>();
        var map = new Dictionary<(int Series, GroupRelationKind Kind, int Index), List<GroupRelation>>();
        foreach (var r in rels)
        {
            int series = bySupergroup ? r.ParentSeriesNumber : r.ChildSeriesNumber;
            if (series >= 0 && map.TryGetValue((series, r.Kind, r.Index), out var list))
                list.Add(r);
            else
            {
                var l = new List<GroupRelation> { r };
                agg.Add(l);
                if (series >= 0)
                    map[(series, r.Kind, r.Index)] = l;
            }
        }
        return agg;
    }

    /// <summary>260709Cl 追加: 1 段 (上段/下段) に表示するノードをカテゴリ (t → k → iso) 順に max 個まで選ぶ。
    /// あふれる場合は "+N" overflow ノード用に 1 枠空け、各カテゴリ最低 1 枠を保証してから先頭カテゴリ優先で
    /// 埋める (t 優先のみだと k/iso が一切見えない群が出る、codex R8)。</summary>
    private static List<List<GroupRelation>> SelectRow(List<List<GroupRelation>>[] categories, int max, out int overflow)
    {
        int total = categories.Sum(c => c.Count);
        if (total <= max)
        {
            overflow = 0;
            return [.. categories.SelectMany(c => c)];
        }
        max = Math.Max(categories.Count(c => c.Count > 0), max - 1); // "+N" 分を確保 (最低でも各カテゴリ 1 枠)
        var take = new int[categories.Length];
        int used = 0;
        for (int i = 0; i < categories.Length; i++)
        {
            //take[i] = Math.Min(categories[i].Count, 1);
            take[i] = Math.Min(categories[i].Count, 2); // 260709Cl: 最低保証 2 枠 (Pm-3m の k 3 タイプ中 1 つしか見えなかった)
            used += take[i];
        }
        // 保証合計が max を超えたら後方カテゴリから 1 枠へ切り詰める
        for (int i = categories.Length - 1; i >= 0 && used > max; i--)
            while (take[i] > 1 && used > max) { take[i]--; used--; }
        for (int i = 0; i < categories.Length && used < max; i++)
        {
            int add = Math.Min(categories[i].Count - take[i], max - used);
            take[i] += add;
            used += add;
        }
        var sel = new List<List<GroupRelation>>();
        for (int i = 0; i < categories.Length; i++)
            sel.AddRange(categories[i].Take(take[i]));
        overflow = total - sel.Count;
        return sel;
    }

    private void DrawGraph(Graphics g, int w, int h)
    {
        // 3 段レイアウト: 上=極小超群, 中=現在, 下=極大部分群。どの種別 (t/k/isomorphic) も「一段」の関係なので
        // 同じ段に混在させ (Bärnighausen 図の慣行)、辺ラベル t2 / k2 / i3 と色で種別を示す。
        var cur = SymmetryStatic.Symmetries[_currentSeries];
        //int midY = h / 2, topY = (int)(h * 0.16), botY = (int)(h * 0.84);
        // 260711Cl (codex R12): 選択パス (BuildSelectedBranch = 履歴を単調化した祖先チェーン) の分だけ
        // 行を追加する動的レイアウト。branch = [最古祖先, …, 直近親]。直近親は超群行に混ぜて強調
        // (focus+context)、それより上の祖先 (最大 3、超過は「⋮ +N」) を親ノードの真上に縦積みする。
        var branch = BuildSelectedBranch();
        int ancShown = Math.Min(Math.Max(branch.Count - 1, 0), 3);
        int ancMore = Math.Max(branch.Count - 1 - ancShown, 0);
        int rows = ancShown + 3;
        int yTop = (int)(h * (branch.Count > 0 ? 0.08 : 0.16)), ySpan = (int)(h * (branch.Count > 0 ? 0.84 : 0.68));
        int RowY(int i) => yTop + ySpan * i / (rows - 1); // branch 無し: 0.16h/0.50h/0.84h = 従来レイアウトと一致
        int topY = RowY(ancShown), midY = RowY(ancShown + 1), botY = RowY(ancShown + 2);
        //var nodeSize = new Size(88, 40);
        //int maxPerRow = Math.Max(3, (w - 40) / (nodeSize.Width + 10)); // 横あふれ防止 (幅から動的に算出)
        // 260709Cl: 混雑時はノード幅を 88→66 px に縮小して 1 行の容量を稼ぐ (フォーム既定幅で Pm-3m の
        // k-部分群 3 タイプ中 2 つが "+N" に隠れた)。選択は縮小幅の容量で行い、結果が標準幅でも
        // 収まるなら標準幅で描く。
        int maxPerRow = Math.Max(3, (w - 40) / (66 + 8)); // 横あふれ防止 (縮小幅の容量、幅から動的に算出)

        // 上段: t-超群 → k-超群 → isomorphic 超群 (_ksupers は Kind 混在なので分離)
        var topRow = SelectRow(
            [AggregateForGraph(_supers, bySupergroup: true),
             AggregateForGraph(_ksupers.Where(s => s.Kind == GroupRelationKind.K), bySupergroup: true),
             AggregateForGraph(_ksupers.Where(s => s.Kind == GroupRelationKind.Isomorphic), bySupergroup: true)],
            maxPerRow, out int topOverflow);
        // 下段: t-部分群 → k-部分群 → isomorphic 部分群
        var botRow = SelectRow(
            [AggregateForGraph(_subs, bySupergroup: false),
             AggregateForGraph(_ksubs.Where(s => s.Kind == GroupRelationKind.K), bySupergroup: false),
             AggregateForGraph(_ksubs.Where(s => s.Kind == GroupRelationKind.Isomorphic), bySupergroup: false)],
            maxPerRow, out int botOverflow);
        // 260711Cl (codex R12): 選択パスの直近親は必ず超群行に表示する (SelectRow の枠あふれ・逆引き未完了で
        // リストに無い場合は、経由した下降関係そのものを超群側視点で流用してノード化する)。
        if (branch.Count > 0 && !topRow.Any(grp => grp[0].ParentSeriesNumber == branch[^1].Series))
        {
            var parentRel = _supers.FirstOrDefault(s => s.ParentSeriesNumber == branch[^1].Series)
                ?? _ksupers.FirstOrDefault(s => s.ParentSeriesNumber == branch[^1].Series)
                ?? branch[^1].Via;
            topRow.Insert(0, [parentRel]);
        }

        int rowMax = Math.Max(topRow.Count + (topOverflow > 0 ? 1 : 0), botRow.Count + (botOverflow > 0 ? 1 : 0));
        var nodeSize = rowMax <= Math.Max(3, (w - 40) / (88 + 10)) ? new Size(88, 40) : new Size(66, 40);
        var curRect = NodeRect(w / 2, midY, nodeSize);

        var superRects = SpreadRects(topRow.Count + (topOverflow > 0 ? 1 : 0), w, topY, nodeSize);
        var subRects = SpreadRects(botRow.Count + (botOverflow > 0 ? 1 : 0), w, botY, nodeSize);

        using var edgeFont = new Font("Segoe UI", 8f, FontStyle.Bold);
        using var labelBg = new SolidBrush(Color.White);

        // 辺 + ラベル (線は種別色の薄色、ラベル文字は不透明の種別色。色だけに依存させず t/k/i の文字を必ず併記)
        void DrawRelationEdge(List<GroupRelation> group, Point nodeCenter, bool toCurrent)
        {
            var kind = group[0].Kind;
            using var pen = new Pen(Color.FromArgb(110, KindColor(kind)), 1.4f);
            using var fg = new SolidBrush(KindColor(kind));
            var (a, b) = toCurrent ? (nodeCenter, Center(curRect)) : (Center(curRect), nodeCenter);
            DrawEdge(g, pen, a, b);
            // 非共役類を集約したノードは類数を添える (×n は ConjugateCount と紛れるため「n cls」表記、codex R8)。
            string label = $"{KindChar(kind)}{group[0].Index}" + (group.Count > 1 ? $" ·{group.Count}{Loc(en: "cls", ja: "類", de: "Kl.", fr: "cl.", es: "cl.", pt: "cl.", it: "cl.", ru: "кл.", zhHans: "类", zhHant: "類", ko: "류")}" : "");
            DrawEdgeLabel(g, edgeFont, fg, labelBg, Mid(a, b), label);
        }
        for (int i = 0; i < topRow.Count; i++)
            DrawRelationEdge(topRow[i], Center(superRects[i]), toCurrent: true);
        for (int i = 0; i < botRow.Count; i++)
            DrawRelationEdge(botRow[i], Center(subRects[i]), toCurrent: false);

        // ノード (ハイライトは集約内のどの類が選択されていても点灯)
        for (int i = 0; i < topRow.Count; i++)
        {
            var group = topRow[i];
            bool sel = _selectedRelation != null && group.Any(r => ReferenceEquals(r, _selectedRelation));
            int series = group[0].ParentSeriesNumber;
            DrawNode(g, superRects[i], SymmetryStatic.Symmetries[series], false, sel, series);
            _graphNodes.Add(new GraphNode { Rect = superRects[i], TargetSeries = series, Relations = [.. group], ViewFromChild = true });
        }
        for (int i = 0; i < botRow.Count; i++)
        {
            var group = botRow[i];
            bool sel = _selectedRelation != null && group.Any(r => ReferenceEquals(r, _selectedRelation));
            int series = group[0].ChildSeriesNumber;
            DrawNode(g, subRects[i], series >= 0 ? SymmetryStatic.Symmetries[series] : default, false, sel,
                     series, series < 0 ? group[0].PointGroupHM : null);
            _graphNodes.Add(new GraphNode { Rect = subRects[i], TargetSeries = series, Relations = [.. group], ViewFromChild = false });
        }
        // overflow ノード ("+N"、ヒットテスト対象外 — 全リストはツリー側で見る)
        if (topOverflow > 0)
            DrawOverflowNode(g, superRects[^1], topOverflow);
        if (botOverflow > 0)
            DrawOverflowNode(g, subRects[^1], botOverflow);

        DrawNode(g, curRect, cur, true, false, _currentSeries);
        _graphNodes.Add(new GraphNode { Rect = curRect, TargetSeries = _currentSeries }); // 現在ノード (選択なし・遷移 no-op)

        // 260711Cl (codex R12): 選択パス (祖先チェーン) の描画。直近親 (超群行内) を強調枠+親→現在の辺を
        // パス色で強調し、その真上に祖先を縦積み (辺ラベル = 経由した関係の kind+index)。祖先ノードは
        // クリック=経由関係の詳細表示、dblclick=その祖先へ遷移 (BuildSelectedBranch が切り詰めを処理)。
        if (branch.Count > 0)
        {
            int pi = topRow.FindIndex(grp => grp[0].ParentSeriesNumber == branch[^1].Series);
            if (pi >= 0) // (強制包含済みなので常に成立するはずだが防御的に)
            {
                var pathColor = Color.FromArgb(120, 87, 166); // パス色 (紫系 — t/k/i の 3 色と衝突しない)
                using var pathPen = new Pen(pathColor, 2.4f);
                using var pathEdge = new Pen(Color.FromArgb(150, pathColor), 2.2f);
                using var pathFg = new SolidBrush(pathColor);
                var parentRect = superRects[pi];
                // 親→現在の辺 (既存の細い辺の上へ強調重ね描き)
                var viaLast = branch[^1].Via;
                DrawEdge(g, pathEdge, Center(parentRect), Center(curRect));
                DrawEdgeLabel(g, edgeFont, pathFg, labelBg, Mid(Center(parentRect), Center(curRect)), $"{KindChar(viaLast.Kind)}{viaLast.Index}");
                using (var pp = Rounded(parentRect, 8))
                    g.DrawPath(pathPen, pp);
                // 祖先の縦積み (branch[^2] が親の 1 段上、以降さかのぼる)
                var below = parentRect;
                for (int a = 0; a < ancShown; a++)
                {
                    var (ancSeries, ancVia) = branch[branch.Count - 2 - a];
                    var rect = NodeRect(Center(parentRect).X, RowY(ancShown - 1 - a), nodeSize);
                    DrawEdge(g, pathEdge, Center(rect), Center(below));
                    DrawEdgeLabel(g, edgeFont, pathFg, labelBg, Mid(Center(rect), Center(below)), $"{KindChar(ancVia.Kind)}{ancVia.Index}");
                    DrawNode(g, rect, SymmetryStatic.Symmetries[ancSeries], false, false, ancSeries);
                    using (var ap = Rounded(rect, 8))
                        g.DrawPath(pathPen, ap);
                    _graphNodes.Add(new GraphNode { Rect = rect, TargetSeries = ancSeries, Relations = [ancVia], ViewFromChild = false });
                    below = rect;
                }
                if (ancMore > 0)
                {
                    using var moreFont = new Font("Segoe UI", 8f);
                    string more = $"⋮ +{ancMore}";
                    var sz = g.MeasureString(more, moreFont);
                    using var moreFg = new SolidBrush(SystemColors.GrayText);
                    g.DrawString(more, moreFont, moreFg, Center(below).X - sz.Width / 2, below.Y - sz.Height - 3);
                }
            }
        }

        // 右下の恒常注記: isomorphic 辺があるときのみ「i: index ≤ 4 のみ」(詳細な説明はツリー側の注記が担う)
        bool hasIso = _ksubs.Any(s => s.Kind == GroupRelationKind.Isomorphic) || _ksupers.Any(s => s.Kind == GroupRelationKind.Isomorphic);
        using var noteFont = new Font("Segoe UI", 7.5f);
        using var noteFg = new SolidBrush(SystemColors.GrayText);
        if (hasIso)
        {
            string note = "i: " + Loc(en: "index ≤ 4 only", ja: "index ≤ 4 のみ", de: "nur Index ≤ 4", fr: "index ≤ 4 uniquement", es: "solo índice ≤ 4", pt: "apenas índice ≤ 4", it: "solo indice ≤ 4", ru: "только индекс ≤ 4", zhHans: "仅 index ≤ 4", zhHant: "僅 index ≤ 4", ko: "index ≤ 4만");
            var sz = g.MeasureString(note, noteFont);
            g.DrawString(note, noteFont, noteFg, w - sz.Width - 6, h - sz.Height - 4);
        }
        // k-超群がバックグラウンド構築中なら右上に注記 (完了時に ComputeThenApplyOnUi 経由で再描画される)
        if (_ksupersPending)
        {
            string note = "k: " + Loc(en: "computing…", ja: "計算中…", de: "wird berechnet…", fr: "calcul en cours…", es: "calculando…", pt: "calculando…", it: "calcolo in corso…", ru: "вычисляется…", zhHans: "计算中…", zhHant: "計算中…", ko: "계산 중…");
            var sz = g.MeasureString(note, noteFont);
            g.DrawString(note, noteFont, noteFg, w - sz.Width - 6, 4);
        }
    }

    /// <summary>260709Cl 追加: 表示枠にあふれた関係数を示す "+N" ノード (灰色破線、クリック不可)。</summary>
    private static void DrawOverflowNode(Graphics g, Rectangle rect, int count)
    {
        using var border = new Pen(Color.FromArgb(170, 178, 190), 1.2f) { DashStyle = DashStyle.Dash };
        using var path = Rounded(rect, 8);
        g.DrawPath(border, path);
        using var font = new Font("Segoe UI", 10f, FontStyle.Bold);
        using var fg = new SolidBrush(SystemColors.GrayText);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString($"+{count}", font, fg, (RectangleF)rect, sf);
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
        //_graphNodes.Add((rect, series)); // 260709Cl: ヒットテスト登録は DrawGraph 側 (GraphNode 化で Relation/選択元も持つため)
        using var fill = new SolidBrush(Color.White);
        using var border = new Pen(isCurrent ? Color.FromArgb(47, 111, 179) : isSelected ? Color.FromArgb(44, 122, 123) : Color.FromArgb(180, 188, 200), isCurrent || isSelected ? 2.2f : 1.3f);
        using var path = Rounded(rect, 8);
        // 260705Cl: 私製 Inflate を BCL の Rectangle.Inflate に置換し、halo 側 GraphicsPath の Dispose 漏れも修正。
        //if (isCurrent) { using var halo = new SolidBrush(Color.FromArgb(220, 234, 251)); g.FillPath(halo, Rounded(Inflate(rect, 3), 10)); }
        if (isCurrent) { using var halo = new SolidBrush(Color.FromArgb(220, 234, 251)); using var haloPath = Rounded(Rectangle.Inflate(rect, 3, 3), 10); g.FillPath(halo, haloPath); }
        g.FillPath(fill, path);
        g.DrawPath(border, path);

        using var noFont = new Font("Segoe UI", 7.5f);
        using var subFg = new SolidBrush(Color.FromArgb(103, 113, 126));
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }; // 260705Cl: Dispose 漏れ修正
        // 260708Cl: 空間群/点群記号を PrettyHM の Unicode 近似テキストでなく LaTeX 数式ビットマップで描画する。
        // 下付き (P4_2/mnm の 4_2)・オーバーライン (R\bar{3}m) が正しく組版される。Matrix/Orbit タブと同じ
        // LabelLaTeX.RenderLatexBitmap 経路。ビットマップは記号ごとにキャッシュ (色・フォントは全ノード共通)。
        string hmLatex = series >= 0 ? HmToLatex(sym.SpaceGroupHMStr) : (fallbackLabel != null ? HmToLatex(fallbackLabel) : "?");
        var hmArea = new Rectangle(rect.X + 4, rect.Y + 2, rect.Width - 8, rect.Height - 16);
        var hmBmp = GetHmBitmap(hmLatex);
        if (hmBmp != null)
            DrawBitmapFit(g, hmBmp, hmArea);
        else
        {
            // WpfMath が解釈できない記号はプレーンテキスト描画へフォールバック。
            using var hmFont = new Font("Segoe UI", 10f, FontStyle.Bold);
            using var fg = new SolidBrush(Color.FromArgb(26, 32, 41));
            g.DrawString(series >= 0 ? SeitzNotation.PrettyHM(sym.SpaceGroupHMStr) : fallbackLabel ?? "?", hmFont, fg, (RectangleF)hmArea, sf);
        }
        string no = series >= 0 ? "No. " + sym.SpaceGroupNumber : "";
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

    /// <summary>260708Cl 追加: Diagram ノードの HM/点群記号を LaTeX ビットマップ化してキャッシュから返す。
    /// 描画先の Bitmap Graphics は既定 96 dpi なので、GDI DrawString で描く "No." 副題と縮尺を揃えるため 96 dpi で組版する。
    /// WpfMath が解釈できない記号 (理論上は無いが防御的に) は null を返し、呼び出し側でテキスト描画へフォールバックする。</summary>
    private Bitmap GetHmBitmap(string latex)
    {
        if (!_hmLatexCache.TryGetValue(latex, out var bmp))
        {
            try
            {
                using var f = new Font("Segoe UI", 10f, FontStyle.Bold);
                bmp = LabelLaTeX.RenderLatexBitmap(latex, f, Color.FromArgb(26, 32, 41), 96.0);
            }
            catch { bmp = null; }
            _hmLatexCache[latex] = bmp; // 失敗 (null) もキャッシュして再パースを避ける
        }
        return bmp;
    }

    /// <summary>260708Cl 追加: ビットマップをアスペクト比を保って area に収まるよう中央描画する (拡大はしない)。</summary>
    private static void DrawBitmapFit(Graphics g, Bitmap bmp, Rectangle area)
    {
        if (area.Width <= 0 || area.Height <= 0 || bmp.Width <= 0 || bmp.Height <= 0) return;
        double scale = Math.Min(1.0, Math.Min((double)area.Width / bmp.Width, (double)area.Height / bmp.Height));
        int dw = Math.Max(1, (int)Math.Round(bmp.Width * scale));
        int dh = Math.Max(1, (int)Math.Round(bmp.Height * scale));
        var dest = new Rectangle(area.X + (area.Width - dw) / 2, area.Y + (area.Height - dh) / 2, dw, dh);
        var old = g.InterpolationMode;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.DrawImage(bmp, dest);
        g.InterpolationMode = old;
    }

    // 260709Cl シグネチャ変更 (旧: private int HitTestGraph(Point p) — series を返していた):
    // GraphNode を直接返す。series 逆引きは t/k 重複・isomorphic (子 series == 現在) で曖昧だった (codex R8)。
    //private int HitTestGraph(Point p)
    //{
    //    foreach (var (rect, series) in _graphNodes)
    //        if (rect.Contains(p) && series >= 0)
    //            return series;
    //    return -1;
    //}
    private GraphNode HitTestGraph(Point p)
    {
        foreach (var node in _graphNodes)
            if (node.Rect.Contains(p))
                return node;
        return null;
    }

    private void pictureBoxGraph_SizeChanged(object sender, EventArgs e)
    {
        if (_currentSeries >= 0) RenderGraph();
    }

    private void pictureBoxGraph_MouseClick(object sender, MouseEventArgs e)
    {
        //int series = HitTestGraph(e.Location);
        //if (series < 0 || series == _currentSeries) return;
        //// クリック = その関係を選択 (詳細タブ更新)。260705Cl 修正 (Phase 2e): 部分群 (_subs) だけでなく
        //// 超群 (_supers, ParentSeriesNumber で識別) ノードも選択可能にする。
        //GroupRelation rel = _subs.FirstOrDefault(s => s.ChildSeriesNumber == series)
        //    ?? _supers.FirstOrDefault(s => s.ParentSeriesNumber == series);
        //if (rel != null) { ShowRelationDetail(rel); RenderGraph(); }
        // 260709Cl: クリック = ノードの代表関係を選択 (集約ノードは先頭類)。選択元 (上段/下段) も明示して
        // self-isomorphic の Matrix 向きを正しくする。現在ノード・overflow ノード (Relations 空) は何もしない。
        var node = HitTestGraph(e.Location);
        if (node?.Relations is { Length: > 0 } rels)
        {
            ShowRelationDetail(rels[0], node.ViewFromChild);
            RenderGraph();
        }
    }

    private void pictureBoxGraph_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        //int series = HitTestGraph(e.Location);
        //if (series >= 0 && series != _currentSeries)
        //    NavigateTo(series);
        var node = HitTestGraph(e.Location); // 260709Cl: GraphNode 化 (isomorphic は TargetSeries == 現在 series なので自然に no-op)
        if (node != null && node.TargetSeries >= 0 && node.TargetSeries != _currentSeries)
            //NavigateTo(node.TargetSeries);
            NavigateTo(node.TargetSeries, via: node.Relations is { Length: > 0 } rels ? rels[0] : null); // 260711Cl (codex R12)
    }
    #endregion

    #region 点群 Hasse 図 (Point groups タブ) — 260712Cl 追加 (③-4、codex R12)
    // 32 の幾何結晶類 (点群型) の包含 poset (PointGroupCatalog、被覆辺 80 本)。ITA Fig. 10.1.3.2 の慣行に
    // ならい縦軸 = 位数 (log スケール)、左タワー = 六方/三方、右タワー = 立方/正方/斜方/単斜/三斜の
    // 固定 2 タワー配置。クリック = その型を注視して下位集合 (青)・上位集合 (橙) を強調するだけで、
    // 空間群ナビゲーションはしない (点群型は特定の空間群に対応しないため — codex R12)。

    /// <summary>点群 Hasse 図のヒットテスト矩形 (描画順に登録)。</summary>
    private readonly List<(Rectangle Rect, string Name)> _pgNodes = [];
    /// <summary>クリックで注視中の点群型名 (null = 現在の空間群の点群)。</summary>
    private string _pgFocus = null;

    /// <summary>32 型の固定 x 座標 (0..1)。y は位数から log スケールで決まるため x のみ持つ。</summary>
    private static readonly Dictionary<string, float> _pgX = new()
    {
        ["m-3m"] = 0.62f,
        ["6/mmm"] = 0.15f, ["m-3"] = 0.44f, ["432"] = 0.62f, ["-43m"] = 0.80f,
        ["4/mmm"] = 0.70f,
        ["-3m"] = 0.05f, ["622"] = 0.16f, ["6mm"] = 0.27f, ["-6m2"] = 0.38f, ["6/m"] = 0.49f, ["23"] = 0.62f,
        ["mmm"] = 0.56f, ["422"] = 0.66f, ["4mm"] = 0.755f, ["-42m"] = 0.85f, ["4/m"] = 0.945f,
        ["32"] = 0.05f, ["3m"] = 0.16f, ["-6"] = 0.27f, ["6"] = 0.38f, ["-3"] = 0.49f,
        ["222"] = 0.60f, ["mm2"] = 0.70f, ["2/m"] = 0.80f, ["4"] = 0.89f, ["-4"] = 0.965f,
        ["3"] = 0.27f,
        ["2"] = 0.55f, ["m"] = 0.70f, ["-1"] = 0.84f,
        ["1"] = 0.62f,
    };

    /// <summary>focus 型の下位集合 (部分群型) と上位集合 (超群型) を被覆辺の推移閉包で求める (どちらも focus 自身を含む)。</summary>
    internal static (HashSet<string> Down, HashSet<string> Up) PointGroupClosure(string focus)
    {
        var down = new HashSet<string> { focus };
        var q = new Queue<string>(down);
        while (q.Count > 0)
        {
            var x = q.Dequeue();
            foreach (var e in PointGroupCatalog.CoverEdges)
                if (e.Parent == x && down.Add(e.Child))
                    q.Enqueue(e.Child);
        }
        var up = new HashSet<string> { focus };
        q = new Queue<string>(up);
        while (q.Count > 0)
        {
            var x = q.Dequeue();
            foreach (var e in PointGroupCatalog.CoverEdges)
                if (e.Child == x && up.Add(e.Parent))
                    q.Enqueue(e.Parent);
        }
        return (down, up);
    }

    private void RenderPointGroups()
    {
        if (_currentSeries < 0) return;
        int w = Math.Max(50, pictureBoxPointGroups.ClientSize.Width);
        int h = Math.Max(50, pictureBoxPointGroups.ClientSize.Height);
        var bmp = new Bitmap(w, h);
        _pgNodes.Clear();
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Color.White);
            DrawPointGroups(g, w, h);
        }
        var old = pictureBoxPointGroups.Image;
        pictureBoxPointGroups.Image = bmp;
        old?.Dispose();
    }

    private void DrawPointGroups(Graphics g, int w, int h)
    {
        string curPg = PointGroupCatalog.NormalizedName(_currentSeries);
        string focus = _pgFocus ?? curPg;
        var (down, up) = PointGroupClosure(focus);
        var orderOf = PointGroupCatalog.Types.ToDictionary(t => t.Name, t => t.Order);
        float padT = 46, padB = 14, padL = 8, padR = 8;
        var nodeSize = new Size(Math.Clamp((w - 16) / 12, 40, 54), 21);
        double logMax = Math.Log2(48);
        Point Pos(string nm) => new(
            (int)(padL + _pgX[nm] * (w - padL - padR - nodeSize.Width) + nodeSize.Width / 2f),
            (int)(padT + (float)(1 - Math.Log2(orderOf[nm]) / logMax) * (h - padT - padB - nodeSize.Height) + nodeSize.Height / 2f));

        var downColor = Color.FromArgb(47, 111, 179); // 部分群方向 (Diagram の t- と同じ青)
        var upColor = Color.FromArgb(166, 90, 0);     // 超群方向 (同 burnt orange — 白背景で判別しやすい既存 2 色を流用)
        var focusColor = Color.FromArgb(44, 122, 123); // 注視ノード (既存の選択色 teal)

        // 辺: 非強調 (薄グレー) → 強調 (down 青 / up 橙) の順に重ね描き
        using (var dimPen = new Pen(Color.FromArgb(228, 232, 238), 1f))
        using (var downPen = new Pen(Color.FromArgb(170, downColor), 1.8f))
        using (var upPen = new Pen(Color.FromArgb(170, upColor), 1.8f))
        {
            foreach (var e in PointGroupCatalog.CoverEdges)
                if (!(down.Contains(e.Parent) && down.Contains(e.Child)) && !(up.Contains(e.Parent) && up.Contains(e.Child)))
                    g.DrawLine(dimPen, Pos(e.Parent), Pos(e.Child));
            foreach (var e in PointGroupCatalog.CoverEdges)
                if (down.Contains(e.Parent) && down.Contains(e.Child))
                    g.DrawLine(downPen, Pos(e.Parent), Pos(e.Child));
                else if (up.Contains(e.Parent) && up.Contains(e.Child))
                    g.DrawLine(upPen, Pos(e.Parent), Pos(e.Child));
        }
        // 注視ノードに接する被覆辺のみ index (位数比) ラベルを表示
        using (var edgeFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
        using (var labelBg = new SolidBrush(Color.White))
            foreach (var e in PointGroupCatalog.CoverEdges)
            {
                if (e.Parent != focus && e.Child != focus) continue;
                using var fg = new SolidBrush(e.Parent == focus ? downColor : upColor);
                DrawEdgeLabel(g, edgeFont, fg, labelBg, Mid(Pos(e.Parent), Pos(e.Child)), e.Index.ToString());
            }
        // ノード (関与しない型はグレーアウト)
        using var nodeFont = new Font("Segoe UI", 8.25f, FontStyle.Bold);
        using var nodeFontSmall = new Font("Segoe UI", 7f, FontStyle.Bold); // 260712Cl: 6/mmm 等の長い名前が折り返さないよう縮小版
        using var sf = new StringFormat(StringFormatFlags.NoWrap) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        foreach (var t in PointGroupCatalog.Types)
        {
            var c = Pos(t.Name);
            var rect = new Rectangle(c.X - nodeSize.Width / 2, c.Y - nodeSize.Height / 2, nodeSize.Width, nodeSize.Height);
            bool involved = down.Contains(t.Name) || up.Contains(t.Name);
            bool isFocus = t.Name == focus;
            var border = isFocus ? focusColor : down.Contains(t.Name) ? downColor : up.Contains(t.Name) ? upColor : Color.FromArgb(190, 197, 208);
            if (t.Name == curPg) // 現在の空間群の点群は注視と独立に常時ハロー (Diagram の現在ノードと同じ水色)
            {
                using var halo = new SolidBrush(Color.FromArgb(220, 234, 251));
                using var hp = Rounded(Rectangle.Inflate(rect, 3, 3), 8);
                g.FillPath(halo, hp);
            }
            using var fill = new SolidBrush(involved ? Color.White : Color.FromArgb(250, 250, 252));
            using var pen = new Pen(border, isFocus ? 2.2f : involved ? 1.6f : 1.1f);
            using var path = Rounded(rect, 6);
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
            using var tfg = new SolidBrush(involved ? Color.Black : SystemColors.GrayText);
            //g.DrawString(t.Name, nodeFont, tfg, (RectangleF)rect, sf);
            var f = g.MeasureString(t.Name, nodeFont).Width <= rect.Width - 4 ? nodeFont : nodeFontSmall; // 260712Cl: 折り返し防止
            g.DrawString(t.Name, f, tfg, (RectangleF)rect, sf);
            _pgNodes.Add((rect, t.Name));
        }
        // 左上の情報行: 注視型の要約 + 下位/上位集合の型数 (自身を除く)
        var info = PointGroupCatalog.Types.First(t => t.Name == focus);
        string sgStr = Loc(en: "space-group types", ja: "空間群型", de: "Raumgruppentypen", fr: "types de groupes d'espace", es: "tipos de grupos espaciales", pt: "tipos de grupos espaciais", it: "tipi di gruppi spaziali", ru: "типов пространственных групп", zhHans: "空间群类型", zhHant: "空間群類型", ko: "공간군 유형");
        string subStr = Loc(en: "subgroup types", ja: "部分群型", de: "Untergruppentypen", fr: "types de sous-groupes", es: "tipos de subgrupos", pt: "tipos de subgrupos", it: "tipi di sottogruppi", ru: "типов подгрупп", zhHans: "子群类型", zhHant: "子群類型", ko: "부분군 유형");
        string supStr = Loc(en: "supergroup types", ja: "超群型", de: "Obergruppentypen", fr: "types de supergroupes", es: "tipos de supergrupos", pt: "tipos de supergrupos", it: "tipi di supergruppi", ru: "типов надгрупп", zhHans: "超群类型", zhHant: "超群類型", ko: "초군 유형");
        using var infoFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var infoFont2 = new Font("Segoe UI", 8f);
        using var infoFg = new SolidBrush(Color.Black);
        using var downFg = new SolidBrush(downColor);
        using var upFg = new SolidBrush(upColor);
        g.DrawString($"{focus}  ({info.Schoenflies})   |G| = {info.Order}   ·   {info.SpaceGroupTypeCount} {sgStr}", infoFont, infoFg, 6, 5);
        string dTxt = $"↓ {down.Count - 1} {subStr}";
        g.DrawString(dTxt, infoFont2, downFg, 6, 22);
        g.DrawString($"↑ {up.Count - 1} {supStr}", infoFont2, upFg, 6 + g.MeasureString(dTxt, infoFont2).Width + 14, 22);
    }

    private void pictureBoxPointGroups_SizeChanged(object sender, EventArgs e)
    {
        if (_currentSeries >= 0) RenderPointGroups();
    }

    private void pictureBoxPointGroups_MouseClick(object sender, MouseEventArgs e)
    {
        // ノードクリック = その型を注視、余白クリック = 現在の空間群の点群へ戻す (default タプルの Name は null)
        _pgFocus = _pgNodes.FirstOrDefault(n => n.Rect.Contains(e.Location)).Name;
        RenderPointGroups();
    }
    #endregion

    #region 対称要素 lost/retained 重ね描き (Elements タブ) — 260713Cl 追加 (③-2、codex 相談)
    // 選択した関係 G → H について、G の空間群対称要素図 (ITA 風) の上に、H で保持される要素 (retained) を
    // 黒、失われる要素 (lost) を赤で色分け表示する。実装は「2 パス + ビットマップ ColorMatrix ティント」:
    //   pass1 = 親 G の完全な要素テーブルを赤にティントして下地に (lost baseline)、
    //   pass2 = H.Operations から SymmetryElementsTable.FromOperations で再構築した H の要素テーブルを
    //           黒 (無ティント) で上書き。retained は黒が赤を覆い、lost は赤が残る。4→2 は赤4回+黒2回。
    // 既存の 2299 行の描画器 (SymmetryDiagramElements) には tableOverride 引数 1 個を足しただけで、色分けは
    // 後段のビットマップ処理で行う (描画器は無改修)。
    // v1 は translationengleiche (t-) 部分群のみ: t- では T_H = T_G なので H 要素の Mod1 折り畳みが厳密に正しい。
    // k-/isomorphic は胞が拡大し (T_H ⊂ T_G)、親胞への折り畳みで別胞の retained コピーが lost 位置に重なる
    // 誤りが生じ得るため、v1 では対象外の注記を表示する (codex R 相談で確定)。

    private static readonly Color ElemLostColor = Color.FromArgb(206, 66, 56);   // lost = 赤 (失われる対称要素)
    private static readonly Color ElemRetainedColor = Color.FromArgb(20, 20, 20); // retained = 黒 (H で保持)

    private void RenderElements()
    {
        int w = Math.Max(50, pictureBoxElements.ClientSize.Width);
        int h = Math.Max(50, pictureBoxElements.ClientSize.Height);
        var bmp = new Bitmap(w, h);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);
            try { DrawElements(g, w, h); }
            catch (Exception ex) { DrawElementsCenteredNote(g, w, h, "render error: " + ex.Message, Color.FromArgb(160, 60, 60)); } // 260713Cl: 例外を空白でなく可視化
        }
        var old = pictureBoxElements.Image;
        pictureBoxElements.Image = bmp;
        old?.Dispose();
    }

    private void DrawElements(Graphics g, int w, int h)
    {
        var s = _selectedRelation;
        if (s == null)
        {
            DrawElementsCenteredNote(g, w, h, Loc(en: "Select a subgroup relation to see which symmetry elements are retained or lost.", ja: "部分群関係を選択すると、どの対称要素が保持・消失するかを表示します。", de: "Wählen Sie eine Untergruppenrelation, um zu sehen, welche Symmetrieelemente erhalten bleiben oder verloren gehen.", fr: "Sélectionnez une relation de sous-groupe pour voir quels éléments de symétrie sont conservés ou perdus.", es: "Seleccione una relación de subgrupo para ver qué elementos de simetría se conservan o se pierden.", pt: "Selecione uma relação de subgrupo para ver quais elementos de simetria são mantidos ou perdidos.", it: "Seleziona una relazione di sottogruppo per vedere quali elementi di simmetria sono mantenuti o persi.", ru: "Выберите отношение подгруппы, чтобы увидеть, какие элементы симметрии сохраняются или утрачиваются.", zhHans: "选择一个子群关系以查看哪些对称要素被保持或失去。", zhHant: "選擇一個子群關係以查看哪些對稱要素被保持或失去。", ko: "부분군 관계를 선택하면 어떤 대칭 요소가 유지되거나 사라지는지 표시됩니다."), SystemColors.GrayText);
            return;
        }
        // 260713Cl (③-2 k- 対応): t- と「胞が拡大しない k-」(= centering 除去、SublatticeBasis の行列式が親慣用胞 1 個分)
        // は親胞1セルにそのまま重ね描く (T_H=同一慣用胞なので mod-1 membership が正しく、中心化が生む screw/glide が
        // 失われるのが赤で出る)。胞が拡大する関係 (isomorphic 全般・拡大 k-) は tiling が要るので現状は注記。
        bool sameCell = s.Kind == GroupRelationKind.T
            || (s.SublatticeBasis != null && Math.Abs(Det3(s.SublatticeBasis) - 1.0) < 1e-6);
        if (!sameCell)
        {
            DrawElementsCenteredNote(g, w, h, Loc(
                en: "The symmetry-element overlay is shown for relations that keep the conventional cell (translationengleiche subgroups and centring-removing klassengleiche ones). This relation enlarges the cell, so the lost lattice symmetry is shown on the Domains & Twins and New reflections tabs instead.",
                ja: "対称要素の重ね描きは、慣用胞が変わらない関係 (translationengleiche 部分群と、中心化を外す klassengleiche 部分群) について表示します。この関係は胞が拡大するため、失われる格子対称は Domains & Twins / New reflections タブをご覧ください。",
                de: "Die Symmetrieelement-Überlagerung wird für Relationen gezeigt, die die konventionelle Zelle beibehalten (translationengleiche Untergruppen und zentrierungsentfernende klassengleiche). Diese Relation vergrößert die Zelle; die verlorene Gittersymmetrie wird daher auf den Registerkarten Domänen & Zwillinge und Neue Reflexe gezeigt.",
                fr: "La superposition des éléments de symétrie est affichée pour les relations qui conservent la maille conventionnelle (sous-groupes translationengleiche et klassengleiche qui suppriment un centrage). Cette relation agrandit la maille ; la symétrie de réseau perdue est donc montrée sur les onglets Domaines & Macles et Nouvelles réflexions.",
                es: "La superposición de elementos de simetría se muestra para las relaciones que conservan la celda convencional (subgrupos translationengleiche y klassengleiche que eliminan un centrado). Esta relación amplía la celda; por tanto, la simetría de red perdida se muestra en las pestañas Dominios y maclas y Nuevas reflexiones.",
                pt: "A sobreposição de elementos de simetria é mostrada para relações que mantêm a célula convencional (subgrupos translationengleiche e klassengleiche que removem uma centragem). Esta relação amplia a célula; por isso a simetria de rede perdida é mostrada nas abas Domínios e geminações e Novas reflexões.",
                it: "La sovrapposizione degli elementi di simmetria è mostrata per le relazioni che mantengono la cella convenzionale (sottogruppi translationengleiche e klassengleiche che rimuovono una centratura). Questa relazione ingrandisce la cella; la simmetria reticolare persa è quindi mostrata nelle schede Domini e geminazioni e Nuove riflessioni.",
                ru: "Наложение элементов симметрии показывается для отношений, сохраняющих условную ячейку (translationengleiche подгруппы и klassengleiche, снимающие центрировку). Это отношение увеличивает ячейку, поэтому утраченная симметрия решётки показана на вкладках «Домены и двойники» и «Новые отражения».",
                zhHans: "对称要素重叠适用于保持惯用胞的关系 (translationengleiche 子群，以及去除心式的 klassengleiche 子群)。此关系会扩大胞，因此失去的点阵对称请参见「畴与双晶」和「新反射」选项卡。",
                zhHant: "對稱要素重疊適用於保持慣用胞的關係 (translationengleiche 子群，以及去除心式的 klassengleiche 子群)。此關係會擴大胞，因此失去的點陣對稱請參見「疇與雙晶」與「新反射」索引標籤。",
                ko: "대칭 요소 겹쳐 그리기는 관용 셀을 유지하는 관계 (translationengleiche 부분군과 중심화를 제거하는 klassengleiche 부분군) 에 대해 표시됩니다. 이 관계는 셀을 확대하므로, 잃어버린 격자 대칭은 도메인·쌍정 및 새 반사 탭을 참조하세요."), SystemColors.GrayText);
            return;
        }

        int parentSn = s.ParentSeriesNumber;
        var parentSym = SymmetryStatic.Symmetries[parentSn];
        var axis = SymmetryDiagramCommon.ResolveProjectionAxis(parentSym, ProjectionAxis.C); // 親設定で投影を決める
        // 260713Cl (codex approach a): baseline (G) を親の展開済み操作から 1 つ構築し、retained (H) は
        // **baseline 自身の対称要素を H メンバーシップで絞った部分テーブル** (FilterByOperationMembership) にする。
        // FromOperations は非単調 (要素導出が全操作集合に依存) なので、別テーブルを独立構築すると同一要素の代表点が
        // ずれ赤ゴーストになる (PART 14a で C/A/I/R 系多数)。baseline の raw 要素を再利用すれば代表点がピクセル厳密に
        // 一致し、絞った raw 軸から主軸を再導出するので 4→2 の降格 (赤4回+黒2回) も正しく出る。
        // メンバーシップ = 各要素が表す操作 (署名: 線形部 R + 並進 mod1) が H.Operations の署名集合に含まれるか。
        // t- では T_H=T_G なので mod1 判定で厳密。
        var gTable = SymmetryElementsTable.FromOperations(SymmetryElementsTable.ExpandedOperations(parentSn), parentSn);
        if (gTable == null) { DrawElementsCenteredNote(g, w, h, "—", SystemColors.GrayText); return; }
        var hTable = gTable.FilterByOperationMembership(s.Operations); // 260713Cl: baseline 自身を H メンバーシップで色分け (署名判定は engine 内)

        // 上部ラベル領域を空けて図を描く (図は topBand の下から)。
        int topBand = 40;
        var diagRect = new Rectangle(0, topBand, w, Math.Max(50, h - topBand));

        // pass1: 親 G の完全テーブル → 赤ティント。pass2: H テーブル → 黒。透明ビットマップに描いて合成。
        using (var bmpParent = RenderElementsLayer(diagRect.Width, diagRect.Height, parentSn, axis, gTable))
        using (var attrParent = new ImageAttributes())
        {
            attrParent.SetColorMatrix(TintMatrix(ElemLostColor));
            g.DrawImage(bmpParent, diagRect, 0, 0, bmpParent.Width, bmpParent.Height, GraphicsUnit.Pixel, attrParent);
        }
        using (var bmpChild = RenderElementsLayer(diagRect.Width, diagRect.Height, parentSn, axis, hTable))
        using (var attrChild = new ImageAttributes())
        {
            attrChild.SetColorMatrix(TintMatrix(ElemRetainedColor));
            g.DrawImage(bmpChild, diagRect, 0, 0, bmpChild.Width, bmpChild.Height, GraphicsUnit.Pixel, attrChild);
        }

        // 上部: 関係ラベル + 凡例 (retained=黒 / lost=赤) + 投影方向。
        using var titleFont = new Font("Segoe UI", 8.75f, FontStyle.Bold);
        using var legendFont = new Font("Segoe UI", 8f);
        using var titleFg = new SolidBrush(Color.Black);
        string projName = axis switch { ProjectionAxis.A => "a", ProjectionAxis.B => "b", _ => "c" };
        string parentName = SeitzNotation.PrettyHM(parentSym.SpaceGroupHMStr);
        string childName = s.ChildSeriesNumber >= 0 ? SeitzNotation.PrettyHM(s.ChildLabel) : s.PointGroupHM;
        char kindChar = s.Kind switch { GroupRelationKind.K => 'k', GroupRelationKind.Isomorphic => 'i', _ => 't' }; // 260713Cl: 関係種別 (旧: t 固定)
        g.DrawString($"{parentName}  →  {childName}    ·    {kindChar}{s.Index}    ·    ⟂ {projName}", titleFont, titleFg, 6, 5);
        // 凡例スウォッチ
        float lx = 6, ly = 23;
        using (var retPen = new Pen(ElemRetainedColor, 2.4f))
            g.DrawLine(retPen, lx, ly + 6, lx + 20, ly + 6);
        using (var retFg = new SolidBrush(ElemRetainedColor))
            g.DrawString(Loc(en: "retained in", ja: "保持", de: "erhalten in", fr: "conservé dans", es: "conservado en", pt: "mantido em", it: "mantenuto in", ru: "сохранено в", zhHans: "保持于", zhHant: "保持於", ko: "유지") + $" {childName}", legendFont, retFg, lx + 24, ly);
        float lx2 = lx + 24 + g.MeasureString(Loc(en: "retained in", ja: "保持", de: "erhalten in", fr: "conservé dans", es: "conservado en", pt: "mantido em", it: "mantenuto in", ru: "сохранено в", zhHans: "保持于", zhHant: "保持於", ko: "유지") + $" {childName}", legendFont).Width + 22;
        using (var lostPen = new Pen(ElemLostColor, 2.4f))
            g.DrawLine(lostPen, lx2, ly + 6, lx2 + 20, ly + 6);
        using (var lostFg = new SolidBrush(ElemLostColor))
            g.DrawString(Loc(en: "lost", ja: "消失", de: "verloren", fr: "perdu", es: "perdido", pt: "perdido", it: "perso", ru: "утрачено", zhHans: "消失", zhHant: "消失", ko: "소실"), legendFont, lostFg, lx2 + 24, ly);
    }

    /// <summary>260713Cl 追加: 透明背景のビットマップへ対称要素図を1パス描画して返す (ティントは呼び出し側)。
    /// tableOverride 非 null なら H の要素テーブルを親レイアウトで描く。</summary>
    private static Bitmap RenderElementsLayer(int w, int h, int seriesNumber, ProjectionAxis axis, SymmetryElementsTable tableOverride)
    {
        var bmp = new Bitmap(Math.Max(1, w), Math.Max(1, h));
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit; // 透明背景では ClearType 不可 (グレースケール AA)
        // g.Clear しない = 透明のまま (黒線/白ハロー/グレー AA のみが乗る)。
        SymmetryDiagramElements.DrawSymmetryElements(g, bmp.Size, seriesNumber, axis, tableOverride);
        return bmp;
    }

    /// <summary>260713Cl 追加: グレースケールのインク (黒線+白ハロー+グレー AA) を「黒→target・白→白・グレー→線形補間」
    /// で着色する ColorMatrix (out = target + L·(white−target)、L はグレースケール値)。α は保持。</summary>
    private static ColorMatrix TintMatrix(Color target)
    {
        float tr = target.R / 255f, tg = target.G / 255f, tb = target.B / 255f;
        return new ColorMatrix(
        [
            [1 - tr, 0,      0,      0, 0],
            [0,      1 - tg, 0,      0, 0],
            [0,      0,      1 - tb, 0, 0],
            [0,      0,      0,      1, 0],
            [tr,     tg,     tb,     0, 1],
        ]);
    }

    // 260713Cl: OperationSignature (操作署名) は SymmetryElementsTable.FilterByOperationMembership 側へ移設 (engine に集約)。

    /// <summary>260713Cl 追加: row-major 9 要素の 3×3 行列の行列式。SublatticeBasis (T_H 基底、親慣用胞単位) の
    /// 体積比判定に使う (=1 なら慣用胞不変=centering 除去、>1 なら胞拡大)。</summary>
    private static double Det3(double[] m) =>
        m[0] * (m[4] * m[8] - m[5] * m[7]) - m[1] * (m[3] * m[8] - m[5] * m[6]) + m[2] * (m[3] * m[7] - m[4] * m[6]);

    private static void DrawElementsCenteredNote(Graphics g, int w, int h, string text, Color color)
    {
        using var font = new Font("Segoe UI", 9.5f);
        using var fg = new SolidBrush(color);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        var rect = new RectangleF(w * 0.1f, h * 0.1f, w * 0.8f, h * 0.8f);
        g.DrawString(text, font, fg, rect, sf);
    }

    private void pictureBoxElements_SizeChanged(object sender, EventArgs e)
    {
        if (_currentSeries >= 0) RenderElements();
    }
    #endregion

    #region テーブル列定義 / ラベル多言語化 / 整形
    private void SetupTables()
    {
        const DataGridViewContentAlignment L = DataGridViewContentAlignment.MiddleLeft;
        const DataGridViewContentAlignment R = DataGridViewContentAlignment.MiddleRight;

        miniTableGenerators.SetColumns(
            //new MiniTable.Col("Seitz", L),
            new MiniTable.Col("Seitz", L, Latex: true), // 260706Ch: Matrix タブの MiniTable も LaTeX 描画へ
            new MiniTable.Col(Loc(en: "Type", ja: "種類", de: "Typ", fr: "Type", es: "Tipo", pt: "Tipo", it: "Tipo", ru: "Тип", zhHans: "类型", zhHant: "類型", ko: "종류"), L, Fill: true),
            new MiniTable.Col(Loc(en: "Status", ja: "状態", de: "Status", fr: "État", es: "Estado", pt: "Estado", it: "Stato", ru: "Статус", zhHans: "状态", zhHant: "狀態", ko: "상태"), L));

        miniTableOrbit.SetColumns(
            //new MiniTable.Col(Loc(en: "Parent", ja: "親", de: "Eltern", fr: "Parent", es: "Padre", pt: "Pai", it: "Genitore", ru: "Родитель", zhHans: "母群", zhHant: "母群", ko: "부모"), L),
            //new MiniTable.Col(Loc(en: "→ Child", ja: "→ 子", de: "→ Kind", fr: "→ Fille", es: "→ Hija", pt: "→ Filho", it: "→ Figlio", ru: "→ Подгруппа", zhHans: "→ 子群", zhHant: "→ 子群", ko: "→ 자식"), L, Fill: true),
            new MiniTable.Col(Loc(en: "Parent", ja: "親", de: "Eltern", fr: "Parent", es: "Padre", pt: "Pai", it: "Genitore", ru: "Родитель", zhHans: "母群", zhHant: "母群", ko: "부모"), L, Latex: true), // 260706Ch
            new MiniTable.Col(Loc(en: "→ Child", ja: "→ 子", de: "→ Kind", fr: "→ Fille", es: "→ Hija", pt: "→ Filho", it: "→ Figlio", ru: "→ Подгруппа", zhHans: "→ 子群", zhHant: "→ 子群", ko: "→ 자식"), L, Fill: true, Latex: true), // 260706Ch
            new MiniTable.Col(Loc(en: "Split", ja: "分裂数", de: "Teile", fr: "Parts", es: "Partes", pt: "Partes", it: "Parti", ru: "Части", zhHans: "分裂", zhHant: "分裂", ko: "분열"), R),
            //new MiniTable.Col(Loc(en: "Site sym.", ja: "サイト対称", de: "Lagesym.", fr: "Sym. site", es: "Sim. sitio", pt: "Sim. sítio", it: "Simm. sito", ru: "Симм. поз.", zhHans: "位置对称", zhHant: "位置對稱", ko: "자리 대칭"), L));
            new MiniTable.Col(Loc(en: "Site sym.", ja: "サイト対称", de: "Lagesym.", fr: "Sym. site", es: "Sim. sitio", pt: "Sim. sítio", it: "Simm. sito", ru: "Симм. поз.", zhHans: "位置对称", zhHant: "位置對稱", ko: "자리 대칭"), L, Latex: true)); // 260706Ch

        miniTableTwins.SetColumns(
            //new MiniTable.Col("Seitz", L),
            new MiniTable.Col("Seitz", L, Latex: true), // 260706Ch
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
        // 260712Cl 追加 (③-4): 点群 Hasse 図タブ
        tabPointGroups.Text = Loc(en: "Point groups", ja: "点群", de: "Punktgruppen", fr: "Groupes ponctuels", es: "Grupos puntuales", pt: "Grupos pontuais", it: "Gruppi puntuali", ru: "Точечные группы", zhHans: "点群", zhHant: "點群", ko: "점군");
        // 260713Cl 追加 (③-2): 対称要素 lost/retained タブ
        tabElements.Text = Loc(en: "Elements", ja: "対称要素", de: "Elemente", fr: "Éléments", es: "Elementos", pt: "Elementos", it: "Elementi", ru: "Элементы", zhHans: "对称要素", zhHant: "對稱要素", ko: "대칭 요소");
        toolTip.SetToolTip(pictureBoxElements, Loc(
            en: "For the selected t-subgroup relation, overlays the parent's symmetry-element diagram (ITA style): elements retained in the subgroup are drawn in black, elements lost are drawn in red. A 4-fold that degrades to a 2-fold shows as a red 4-fold with a black 2-fold on top.",
            ja: "選択した t-部分群関係について、親の対称要素図 (ITA 風) を重ね描きします。部分群で保持される要素は黒、失われる要素は赤で描かれます。4回軸が2回軸に退化する場合は赤い4回記号の上に黒い2回記号が重なります。",
            de: "Überlagert für die gewählte t-Untergruppenrelation das Symmetrieelement-Diagramm des Elters (ITA-Stil): in der Untergruppe erhaltene Elemente werden schwarz, verlorene rot gezeichnet. Eine zu einer 2-zähligen Achse reduzierte 4-zählige erscheint als rote 4-zählige mit schwarzer 2-zähliger darüber.",
            fr: "Pour la relation de sous-groupe t- sélectionnée, superpose le diagramme des éléments de symétrie du parent (style ITA) : les éléments conservés dans le sous-groupe sont en noir, les éléments perdus en rouge. Un axe 4 réduit à un axe 2 apparaît comme un axe 4 rouge avec un axe 2 noir par-dessus.",
            es: "Para la relación de subgrupo t- seleccionada, superpone el diagrama de elementos de simetría del padre (estilo ITA): los elementos conservados en el subgrupo se dibujan en negro y los perdidos en rojo. Un eje 4 que se degrada a un eje 2 se muestra como un eje 4 rojo con un eje 2 negro encima.",
            pt: "Para a relação de subgrupo t- selecionada, sobrepõe o diagrama de elementos de simetria do pai (estilo ITA): os elementos mantidos no subgrupo são desenhados em preto e os perdidos em vermelho. Um eixo 4 que se reduz a um eixo 2 aparece como um eixo 4 vermelho com um eixo 2 preto por cima.",
            it: "Per la relazione di sottogruppo t- selezionata, sovrappone il diagramma degli elementi di simmetria del genitore (stile ITA): gli elementi mantenuti nel sottogruppo sono in nero, quelli persi in rosso. Un asse 4 che degrada a un asse 2 appare come un asse 4 rosso con un asse 2 nero sopra.",
            ru: "Для выбранного отношения t-подгруппы накладывает диаграмму элементов симметрии родителя (стиль ITA): элементы, сохранённые в подгруппе, рисуются чёрным, утраченные — красным. Ось 4-го порядка, понизившаяся до 2-го, показывается как красная ось 4 с чёрной осью 2 поверх.",
            zhHans: "对于所选的 t-子群关系，叠加母群的对称要素图 (ITA 风格)：子群中保持的要素以黑色绘制，失去的要素以红色绘制。退化为二次轴的四次轴显示为红色四次记号上叠加黑色二次记号。",
            zhHant: "對於所選的 t-子群關係，疊加母群的對稱要素圖 (ITA 風格)：子群中保持的要素以黑色繪製，失去的要素以紅色繪製。退化為二次軸的四次軸顯示為紅色四次記號上疊加黑色二次記號。",
            ko: "선택한 t-부분군 관계에 대해 부모의 대칭 요소 도표 (ITA 방식) 를 겹쳐 그립니다. 부분군에서 유지되는 요소는 검정, 사라지는 요소는 빨강으로 그려집니다. 2회축으로 퇴화하는 4회축은 빨간 4회 기호 위에 검은 2회 기호로 표시됩니다."));
        toolTip.SetToolTip(pictureBoxPointGroups, Loc(
            en: "Hasse diagram of the 32 crystallographic point-group types (vertical axis: group order). The current point group is haloed. Click a node to highlight its subgroup types (blue) and supergroup types (orange); numbers on the edges are the index (order ratio). Click empty space to return to the current group.",
            ja: "32 の結晶学的点群型のハッセ図 (縦軸は群の位数)。現在の点群はハローで表示されます。ノードをクリックすると部分群型 (青) と超群型 (橙) を強調し、辺の数字は index (位数比) です。余白をクリックすると現在の点群に戻ります。",
            de: "Hasse-Diagramm der 32 kristallographischen Punktgruppentypen (vertikale Achse: Gruppenordnung). Die aktuelle Punktgruppe ist hervorgehoben. Klicken Sie auf einen Knoten, um Untergruppentypen (blau) und Obergruppentypen (orange) hervorzuheben; die Zahlen an den Kanten sind der Index (Ordnungsverhältnis). Klick auf leere Fläche kehrt zur aktuellen Gruppe zurück.",
            fr: "Diagramme de Hasse des 32 types de groupes ponctuels cristallographiques (axe vertical : ordre du groupe). Le groupe ponctuel actuel est entouré d'un halo. Cliquez sur un nœud pour mettre en évidence ses types de sous-groupes (bleu) et de supergroupes (orange) ; les nombres sur les arêtes sont l'indice (rapport des ordres). Cliquez sur un espace vide pour revenir au groupe actuel.",
            es: "Diagrama de Hasse de los 32 tipos de grupos puntuales cristalográficos (eje vertical: orden del grupo). El grupo puntual actual está resaltado con halo. Haga clic en un nodo para resaltar sus tipos de subgrupos (azul) y supergrupos (naranja); los números en las aristas son el índice (razón de órdenes). Haga clic en un espacio vacío para volver al grupo actual.",
            pt: "Diagrama de Hasse dos 32 tipos de grupos pontuais cristalográficos (eixo vertical: ordem do grupo). O grupo pontual atual é destacado com halo. Clique num nó para destacar seus tipos de subgrupos (azul) e supergrupos (laranja); os números nas arestas são o índice (razão de ordens). Clique num espaço vazio para voltar ao grupo atual.",
            it: "Diagramma di Hasse dei 32 tipi di gruppi puntuali cristallografici (asse verticale: ordine del gruppo). Il gruppo puntuale attuale è evidenziato con alone. Clic su un nodo per evidenziare i tipi di sottogruppi (blu) e supergruppi (arancione); i numeri sugli spigoli sono l'indice (rapporto degli ordini). Clic su uno spazio vuoto per tornare al gruppo attuale.",
            ru: "Диаграмма Хассе 32 кристаллографических типов точечных групп (вертикальная ось — порядок группы). Текущая точечная группа выделена ореолом. Щёлкните узел, чтобы подсветить типы подгрупп (синий) и надгрупп (оранжевый); числа на рёбрах — индекс (отношение порядков). Щелчок по пустому месту возвращает к текущей группе.",
            zhHans: "32 种晶体学点群类型的 Hasse 图 (纵轴为群的阶)。当前点群带有光晕。单击节点可强调其子群类型 (蓝) 与超群类型 (橙)；边上的数字为 index (阶之比)。单击空白处返回当前点群。",
            zhHant: "32 種晶體學點群類型的 Hasse 圖 (縱軸為群的階)。目前點群帶有光暈。單擊節點可強調其子群類型 (藍) 與超群類型 (橙)；邊上的數字為 index (階之比)。單擊空白處返回目前點群。",
            ko: "32 결정학적 점군 유형의 Hasse 도표 (세로축은 군의 위수). 현재 점군은 후광으로 표시됩니다. 노드를 클릭하면 부분군 유형 (파랑) 과 초군 유형 (주황) 을 강조하며, 변의 숫자는 index (위수 비) 입니다. 여백을 클릭하면 현재 점군으로 돌아갑니다."));
        toolTip.SetToolTip(buttonBack, Loc(en: "Back", ja: "戻る", de: "Zurück", fr: "Précédent", es: "Atrás", pt: "Voltar", it: "Indietro", ru: "Назад", zhHans: "后退", zhHant: "後退", ko: "뒤로"));
        toolTip.SetToolTip(buttonForward, Loc(en: "Forward", ja: "進む", de: "Vor", fr: "Suivant", es: "Adelante", pt: "Avançar", it: "Avanti", ru: "Вперёд", zhHans: "前进", zhHant: "前進", ko: "앞으로"));
        toolTip.SetToolTip(pictureBoxGraph, Loc(en: "Click a node to inspect, double-click to browse into it.", ja: "ノードをクリックで詳細、ダブルクリックでその群へ移動。", de: "Knoten anklicken zum Ansehen, Doppelklick zum Öffnen.", fr: "Cliquez sur un nœud pour l'inspecter, double-cliquez pour y naviguer.", es: "Haga clic en un nodo para inspeccionar, doble clic para navegar.", pt: "Clique num nó para inspecionar, duplo clique para navegar.", it: "Clic su un nodo per ispezionare, doppio clic per aprirlo.", ru: "Клик по узлу — детали, двойной клик — перейти.", zhHans: "单击节点查看，双击进入该群。", zhHant: "單擊節點查看，雙擊進入該群。", ko: "노드를 클릭하면 상세, 더블클릭하면 이동."));
        // 260709Cl 追加 (Phase 3): 同型部分群の index 上限スピナー (「部分群」と明示 — 超群側は拡張対象外、codex R11)
        labelIsoMax.Text = Loc(en: "Isomorphic subgroups:  index ≤", ja: "同型部分群:  index ≤", de: "Isomorphe Untergruppen:  Index ≤", fr: "Sous-groupes isomorphes :  index ≤", es: "Subgrupos isomorfos:  índice ≤", pt: "Subgrupos isomorfos:  índice ≤", it: "Sottogruppi isomorfi:  indice ≤", ru: "Изоморфные подгруппы:  индекс ≤", zhHans: "同型子群:  index ≤", zhHant: "同型子群:  index ≤", ko: "동형 부분군:  index ≤");
        toolTip.SetToolTip(numericIsoMax, Loc(en: "Upper bound for enumerating maximal isomorphic subgroups (tree). Classes equivalent under the affine normalizer are grouped into one row, as in ITA A1. The Diagram keeps showing index ≤ 4 only.", ja: "同型極大部分群を列挙する指数の上限 (ツリー)。affine normalizer で同値な類は ITA A1 と同様に 1 行へまとめられます。系統図は index ≤ 4 のみの表示のままです。", de: "Obergrenze für die Aufzählung maximaler isomorpher Untergruppen (Baum). Unter dem affinen Normalisator äquivalente Klassen werden wie in ITA A1 zu einer Zeile gruppiert. Das Diagramm zeigt weiterhin nur Index ≤ 4.", fr: "Borne supérieure pour l'énumération des sous-groupes isomorphes maximaux (arbre). Les classes équivalentes sous le normalisateur affine sont regroupées en une ligne, comme dans ITA A1. Le diagramme continue de ne montrer que index ≤ 4.", es: "Límite superior para enumerar subgrupos isomorfos maximales (árbol). Las clases equivalentes bajo el normalizador afín se agrupan en una fila, como en ITA A1. El diagrama sigue mostrando solo índice ≤ 4.", pt: "Limite superior para enumerar subgrupos isomorfos maximais (árvore). Classes equivalentes sob o normalizador afim são agrupadas em uma linha, como na ITA A1. O diagrama continua mostrando apenas índice ≤ 4.", it: "Limite superiore per enumerare i sottogruppi isomorfi massimali (albero). Le classi equivalenti sotto il normalizzatore affine sono raggruppate in una riga, come in ITA A1. Il diagramma continua a mostrare solo indice ≤ 4.", ru: "Верхняя граница перечисления максимальных изоморфных подгрупп (дерево). Классы, эквивалентные относительно аффинного нормализатора, объединяются в одну строку, как в ITA A1. Диаграмма по-прежнему показывает только индекс ≤ 4.", zhHans: "枚举极大同型子群的指数上限 (树)。在仿射正规化子下等价的类会像 ITA A1 一样合并为一行。系统图仍只显示 index ≤ 4。", zhHant: "列舉極大同型子群的指數上限 (樹)。在仿射正規化子下等價的類會如 ITA A1 般合併為一列。系統圖仍僅顯示 index ≤ 4。", ko: "극대 동형 부분군을 열거하는 지수 상한 (트리). 아핀 normalizer 로 동치인 류는 ITA A1 과 같이 한 행으로 묶입니다. 계통도는 여전히 index ≤ 4만 표시합니다."));
        // 260709Cl 追加: 反射探索窓スピナー
        labelReflMax.Text = Loc(en: "Search window:  |h|, |k|, |l|  ≤", ja: "探索範囲:  |h|, |k|, |l|  ≤", de: "Suchfenster:  |h|, |k|, |l|  ≤", fr: "Fenêtre de recherche :  |h|, |k|, |l|  ≤", es: "Ventana de búsqueda:  |h|, |k|, |l|  ≤", pt: "Janela de busca:  |h|, |k|, |l|  ≤", it: "Finestra di ricerca:  |h|, |k|, |l|  ≤", ru: "Окно поиска:  |h|, |k|, |l|  ≤", zhHans: "搜索范围:  |h|, |k|, |l|  ≤", zhHant: "搜尋範圍:  |h|, |k|, |l|  ≤", ko: "탐색 범위:  |h|, |k|, |l|  ≤");
        toolTip.SetToolTip(numericReflMax, Loc(en: "Upper bound of the reflection index search. Larger values can list many more reflections.", ja: "新規反射を探索する指数の上限。大きくすると行数が大幅に増えることがあります。", de: "Obergrenze der Reflexindex-Suche. Größere Werte können deutlich mehr Reflexe auflisten.", fr: "Borne supérieure de la recherche d'indices. Des valeurs plus grandes peuvent lister beaucoup plus de réflexions.", es: "Límite superior de la búsqueda de índices. Valores mayores pueden listar muchas más reflexiones.", pt: "Limite superior da busca de índices. Valores maiores podem listar muitas mais reflexões.", it: "Limite superiore della ricerca degli indici. Valori maggiori possono elencare molte più riflessioni.", ru: "Верхняя граница поиска индексов отражений. Большие значения могут дать значительно больше строк.", zhHans: "反射指数搜索的上限。较大的值可能列出更多反射。", zhHant: "反射指數搜尋的上限。較大的值可能列出更多反射。", ko: "반사 지수 탐색의 상한. 값을 키우면 행 수가 크게 늘 수 있습니다."));
    }

    // 260705Cl: FormSymmetryInformation と一字一句同一だった PrettyHM を SeitzNotation.PrettyHM へ集約 (実装は移設)。
    // 260705Cl: Hkl(int) は int 補間で足りる素通し helper だったためインライン化 (FillReflectionsTab)。
    //private static string Hkl(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);

    // 260706Ch 追加: Matrix タブの上部表示を LabelLaTeX 3 段に分割するための軽量 LaTeX 生成。
    private static string BuildRelationLatex(GroupRelation s)
    {
        var parent = s.ParentSeriesNumber >= 0 ? SymmetryStatic.Symmetries[s.ParentSeriesNumber].SpaceGroupHMStr : "?";
        var child = s.ChildSeriesNumber >= 0 ? SymmetryStatic.Symmetries[s.ChildSeriesNumber].SpaceGroupHMStr : s.ChildLabel;
        string kind = s.Kind switch { GroupRelationKind.K => "k", GroupRelationKind.Isomorphic => "i", _ => "t" };
        return $@"{HmToLatex(parent)}\,\, \rightarrow\,\, {HmToLatex(child)}\,\,\,\, \mathrm{{kind}}={kind}\,\, \mathrm{{index}}={s.Index}";
    }

    private static string BuildTransformationLatex(bool viewFromChild)
        => viewFromChild
            ? @"\mathrm{Transformation\,to\,supergroup\,basis}\,\, (a',b',c')=(a,b,c)P^{-1}"
            : @"\mathrm{Transformation\,to\,subgroup\,basis}\,\, (a',b',c')=(a,b,c)P";

    private static string BuildMatrixLatex(double[] P, double[] p)
    {
        string[] rows =
        [
            $"{LatexFrac(P[0])} & {LatexFrac(P[1])} & {LatexFrac(P[2])}",
            $"{LatexFrac(P[3])} & {LatexFrac(P[4])} & {LatexFrac(P[5])}",
            $"{LatexFrac(P[6])} & {LatexFrac(P[7])} & {LatexFrac(P[8])}",
        ];
        string[] shift =
        [
            p != null && p.Length > 0 ? LatexFrac(p[0]) : "?",
            p != null && p.Length > 1 ? LatexFrac(p[1]) : "?",
            p != null && p.Length > 2 ? LatexFrac(p[2]) : "?",
        ];
        return $@"P=\left(\matrix{{{string.Join(@" \\ ", rows)}}}\right)\,\,\,\, p=\left(\matrix{{{string.Join(@" \\ ", shift)}}}\right)";
    }

    // 260706Ch: FormSymmetryInformation.ToLatex と同種の変換 (sub->下付き・-N->\bar{N}) を独自実装していたが、
    // 実装がドリフトしていた (subN 置換範囲が n<=6 と n<=5 で不一致) ため削除し、既存の ToLatex(spaced:true) へ統合。
    private static string HmToLatex(string hm)
        => string.IsNullOrEmpty(hm) ? "?" : FormSymmetryInformation.ToLatex(hm, spaced: true);

    private static string WyckoffLatex(int multiplicity, string letter, string siteSymmetry = null) // 260706Ch: OrbitSplitting MiniTable 用
    {
        var label = $@"{multiplicity}\mathrm{{{letter}}}";
        return string.IsNullOrWhiteSpace(siteSymmetry) ? label : $@"{label}\,\, {HmToLatex(siteSymmetry)}";
    }

    private static string LatexFrac(double d)
    {
        var f = Frac(d);
        //var slash = f.IndexOf('/'); // 260706Ch: \frac は行列全体が縦に伸びるため横方向分数へ変更
        //return slash > 0 ? $@"\frac{{{f[..slash]}}}{{{f[(slash + 1)..]}}}" : f;
        return f; // 260706Ch: 1/2 のような横方向分数として描画する
    }

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

    /// <summary>260708Cl 追加: 値を [0,1) へ還元する (Domains タブの部分群胞座標の反位相ベクトル用)。</summary>
    private static double Frac01(double d) { d -= Math.Floor(d); return d > 1 - 1e-6 ? 0 : d; }
    #endregion
}
