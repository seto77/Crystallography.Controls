using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace Crystallography.Controls;

/// <summary>
/// 260820Cl 追加: GUI 自動キャプチャ / 文字溢れ診断の共通ハーネス。ReciPro / IPAnalyzer / PDIndexer で
/// 各アプリに丸ごとコピーされていた GuiCapture (撮影エンジン・診断ロジック・3 種の CLI モード) を 1 か所に集約した。
/// ホストアプリは本クラスを継承し、フォーム列挙・親情報の注入・代表状態づくり・モード別ショット等の
/// 仮想フックだけを override する (ReciPro/GuiCapture.cs 参照)。
///
/// 提供する CLI モード (Program.cs から呼ぶ):
///   --capture [dir] [culture]                 … <see cref="Run"/>               全フォームを CopyFromScreen で撮る
///   --diagnose [culture] [inflate%]           … <see cref="Diagnose"/>          文字の切れ/重なりを画面外で測って TSV 出力
///   --capture-form &lt;Type&gt; &lt;out.png&gt; [culture] … <see cref="CaptureSingleForm"/> 1 フォームを DrawToBitmap で画面なし撮影
///
/// 撮影方式: 各フォームを画面内 (0,0) に最前面表示し、<see cref="Graphics.CopyFromScreen(Point, Point, Size)"/> で実描画をそのまま撮る。
/// 以前は画面外 (-32000,-32000) + <see cref="Control.DrawToBitmap(Bitmap, Rectangle)"/> 方式だったが、
/// DrawToBitmap (WM_PRINT) ではタブヘッダー・GraphicsBox の GDI 描画・GPU(OpenGL) 描画が正しく取れず、
/// 重なり合うコントロールの z-order も反転していた (FormCaptureGUI.cs の CopyFromScreen 注記参照)。
/// そこで対話ツール FormCaptureGUI と同じ CopyFromScreen 方式へ統一した。通常起動 (引数なし) では一切実行されない。
///
/// 本ライブラリ所有のフォーム (FormMacro / FormBeamInteraction / FormGroupRelations) の代表状態づくりとモード別ショットは
/// 基底の既定実装 (<see cref="PrepareCaptureState"/> / <see cref="CaptureExtraShots"/>) が行うので、各アプリは何も書かなくてよい
/// (旧: 各アプリの GuiCapture が FormMacro の private checkBoxSamples を reflection でトグルしていた。Controls を触らない
/// 方針の下でキャプチャ対象に Controls 自身のフォームが含まれるという矛盾の産物)。
/// 各メソッド先頭の日付コメント (260521Cl〜260807Cl) は ReciPro/GuiCapture.cs にあった当時のまま残している。
/// </summary>
public abstract class GuiCaptureHarness
{
    #region ホストアプリが設定/override する部分

    /// <summary>
    /// 260522Cl 追加: --capture で言語を強制指定 (en/ja) した場合のカルチャ。
    /// メインフォームの ctor がレジストリ値で CurrentUICulture を上書きするため、各フォーム構築前に再設定する。
    /// </summary>
    public static System.Globalization.CultureInfo ForcedUICulture;

    // 260524Cl 追加: CopyFromScreen 方式の待機時間。--capture は Application.Run を回さず DoEvents で描画を進めるため、
    // Show / タブ切替 / 結晶選択の後に「描画が画面へ反映される」まで明示的に待ってから撮る必要がある。
    protected const int FirstPaintSettleMs = 350; // 初回表示後、フォーム全体が描画されるまでの待ち

    protected const int PrepareSettleMs = 450;    // 結晶選択 (spinel) や Trajectory.Simulate 後の再計算・再描画待ち

    protected const int TabSwitchSettleMs = 180;  // クロップ時にタブを切り替えた後の再描画待ち

    /// <summary>ホストアプリのメインフォーム型。列挙の先頭に構築し、他フォームへの親情報供給元として最後まで保持する。</summary>
    protected abstract Type MainFormType { get; }

    /// <summary>撮影対象フォームを探すアセンブリ (既定: メインフォームのアセンブリ)。</summary>
    protected virtual Assembly FormAssembly => MainFormType.Assembly;

    /// <summary>アプリ名 (既定: アセンブリ名)。環境変数名や temp ディレクトリ名に使う。</summary>
    protected virtual string AppName => FormAssembly.GetName().Name;

    /// <summary>
    /// 260602Cl 追加 (ReciPro) / 260820Cl 一般化: 「この型名 (部分一致・カンマ区切り) だけ撮る」絞り込み用の環境変数名。
    /// 既定は <c>{APPNAME}_CAPTURE_ONLY</c> (ReciPro なら RECIPRO_CAPTURE_ONLY)。1 フォームだけ撮り直したいとき
    /// (例: FormImageSimulator のモード別全体画像) に、全フォーム再撮影による差分 churn と実行時間・途中クラッシュの
    /// リスクを避けるための開発者向けトグル。メインフォームは後続フォームへ親情報を供給するため、フィルタ対象外でも必ず構築する。
    /// </summary>
    protected virtual string CaptureOnlyEnvVar => AppName.ToUpperInvariant() + "_CAPTURE_ONLY";

    /// <summary>実行ファイル (bin/...) からリポルート (docs/ や references/ を持つ) を辿る。辿れなければ null。
    /// bin → ...\App\App → ...\App (リポルート)。</summary>
    protected static DirectoryInfo RepoRoot()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null && dir.Name != "bin")
            dir = dir.Parent;
        return dir?.Parent?.Parent;
    }

    /// <summary>260525Cl 追加 (ReciPro) / 260820Cl 一般化: --capture の出力先を省略したときの既定ディレクトリ
    /// (docs/src/assets/cap-{culture}-auto。Pages 正本化に伴い自動キャプチャも docs/src 側へ保存する)。
    /// 260617Cl: en/ja 固定から SupportedCultures 駆動へ (新言語は cap-de-auto 等)。リポルートを辿れなければ temp にフォールバックする。</summary>
    protected virtual string DefaultOutputDir(CultureInfo culture)
    {
        var langDir = "cap-" + SupportedCultures.Resolve(culture.Name).Name + "-auto";
        var repoRoot = RepoRoot();
        return repoRoot != null
            ? Path.Combine(repoRoot.FullName, "docs", "src", "assets", langDir)
            : Path.Combine(Path.GetTempPath(), AppName.ToLowerInvariant() + "-capture-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + langDir);
    }

    /// <summary>
    /// 260524Cl 追加 (ReciPro) / 260820Cl 一般化: --capture 用のサンプルデータ (リポジトリの references フォルダ内、git 管理外) を探す。
    /// 存在すればフルパス、無ければ null (呼び出し側はサンプル無しの代表状態で撮る)。
    /// </summary>
    protected static string FindReferenceFile(string relativePath)
    {
        var root = RepoRoot();
        if (root == null) return null;
        try
        {
            var candidate = Path.Combine(root.FullName, "references", relativePath);
            return File.Exists(candidate) ? Path.GetFullPath(candidate) : null;
        }
        catch { return null; /* 不正パスは無視 */ }
    }

    /// <summary>撮影/診断の対象フォーム型。既定はアセンブリ内の parameterless ctor を持つ Form 派生型 (メインフォームを先頭に名前順)。
    /// メインフォームを先頭に構築する (他フォームが静的にメインフォームを参照する場合に備える)。</summary>
    protected virtual IEnumerable<Type> EnumerateFormTypes()
        => FormAssembly.GetTypes()
            .Where(t => typeof(Form).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(t => t == MainFormType ? 0 : 1).ThenBy(t => t.Name);

    /// <summary>--capture の標準列挙から外す型 (例: 親未配線の単独生成では空表示になり、別経路で撮るフォーム)。--diagnose には効かない。</summary>
    protected virtual bool SkipInEnumeration(Type type) => false;

    /// <summary>reflection 単独生成した子フォームへメインフォーム / 親情報を注入する (Show 時の NRE 回避＋依存描画の配線)。
    /// <paramref name="main"/> はメインフォーム未構築なら null。</summary>
    protected virtual void WireDependencies(Form form, Form main) { }

    /// <summary>
    /// メインフォームが Load で配線済みのマクロエディタ (引数付き ctor のため reflection 単独生成できない)。null なら撮らない。
    /// 260524Cl (ReciPro): メインフォーム直後 (= 列挙の最初) に撮る。末尾まで待つと GL 多用フォームの NativeWindow finalize で
    /// 稀にプロセスが落ちて撮り損ねるため、GL ウィンドウが溜まる前に先に保存しておく。
    /// </summary>
    protected virtual FormMacro GetMacroEditor(Form main) => null;

    /// <summary>
    /// (260523Ch) フォームを Show しただけではマニュアル用の代表状態にならない画面を、撮影直前に整える。
    /// ここは通常 UI 初期化ではなくキャプチャ用の薄い分岐置き場なので、対象フォームは必要最小限に留める。
    /// メインフォームにも呼ばれる (代表結晶/代表画像の選択など、後続フォームへ供給する状態をここで作る)。
    /// 既定実装は本ライブラリ所有のフォーム (FormMacro = サンプルマクロ表示 / FormGroupRelations = 代表部分群の選択) を扱う。
    /// override 側は自アプリのフォームを処理したあと <c>base.PrepareCaptureState</c> を呼ぶこと。
    /// </summary>
    protected virtual void PrepareCaptureState(Form form, Action<string> trace)
    {
        try
        {
            switch (form)
            {
                case FormMacro macroForm:
                    // 260524Cl: マクロエディタはサンプルマクロを表示した代表状態で撮る。
                    // 260820Cl: 旧 TryShowMacroSamples (private な checkBoxSamples を reflection でトグル) を FormMacro 側の internal フックに置換。
                    macroForm.PrepareCaptureForGuiAudit();
                    Application.DoEvents();
                    trace($"{form.GetType().Name}\tINFO\tprepared macro editor (samples)");
                    break;
                case FormGroupRelations groupRelations:
                    // 260705Cl 追加: 既定 (未選択) はプレースホルダのみなので、最初の t-部分群を選んだ代表状態で撮る。
                    groupRelations.PrepareCaptureForGuiAudit();
                    Application.DoEvents();
                    trace($"{form.GetType().Name}\tINFO\tselected first t-subgroup for detail tabs");
                    break;
            }
        }
        catch (Exception ex)
        {
            trace($"{form.GetType().Name}\tWARN\tPrepareCapture: {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// 全体画像とコントロール/メニュークロップのあとに撮る追加ショット (モード別の全体画像、線種×タブのクロップ等)。
    /// 既定実装は本ライブラリ所有の FormBeamInteraction (線種×タブ) と FormGroupRelations (詳細タブ) を撮る。
    /// override 側は自アプリのフォームを処理したあと <c>base.CaptureExtraShots</c> を呼ぶこと。
    /// </summary>
    protected virtual void CaptureExtraShots(Form form, string name, string outDir, Action<string> trace)
    {
        // 260608Cl 追加: FormBeamInteraction は線種 (X線/電子線/中性子線) でタブ内容が大きく変わる (減衰・輸送/散乱因子の曲線・表が
        // 別物、蛍光は X線専用) ため、線種×タブごとに TabControl 全体をクロップ撮影する。
        if (form is FormBeamInteraction beamInteractionForModeShots)
            CaptureBeamInteractionModeShots(beamInteractionForModeShots, name, outDir, trace);

        // 260705Cl 追加: FormGroupRelations は既定 (未選択) だとプレースホルダ表示のみなので、代表の部分群を選んだ
        // 詳細タブと Diagram タブ (Bärnighausen グラフ) を追加でクロップ撮影する。
        if (form is FormGroupRelations groupRelationsForModeShots)
            CaptureGroupRelationsDiagramShot(groupRelationsForModeShots, name, outDir, trace);
    }

    /// <summary>
    /// 標準列挙の 1 フォームを撮り終えた直後に呼ばれる。メインフォーム経由でしか得られない配線済み子フォームを
    /// この時点で撮るために使う。既定実装はメインフォーム直後に <see cref="GetMacroEditor"/> を撮る。
    /// </summary>
    protected virtual void AfterFormCaptured(Form form, CaptureSession session)
    {
        if (!ReferenceEquals(form, session.Main)) return;
        var macro = GetMacroEditor(form);
        if (macro != null && session.ShouldCapture("FormMacro"))
            session.Capture(macro, "FormMacro");
    }

    /// <summary>
    /// 260523Cl 追加 (ReciPro) / 260820Cl 一般化: 親情報が必要で reflection 列挙では撮れない子フォーム
    /// (メインフォームが保持する配線済みインスタンス) を、標準列挙のあとに撮る。--diagnose でも同じ列挙を測る。
    /// 既に撮ったインスタンス (例: <see cref="GetMacroEditor"/>) は自動的に飛ばす。
    /// </summary>
    protected virtual IEnumerable<Form> EnumerateDependentForms(Form main) => Enumerable.Empty<Form>();

    /// <summary>依存子フォームを撮る直前 (例: 代表結晶の切り替え)。--capture のみ。</summary>
    protected virtual void BeforeDependentFormCapture(Form child, Form main, Action<string> trace) { }

    /// <summary>依存子フォームを撮った直後 (例: 可視状態の復元)。--capture のみ。</summary>
    protected virtual void AfterDependentFormCapture(Form child, Form main, Action<string> trace) { }

    /// <summary>依存子フォームを撮影後に Close するか。メインフォーム所有のフォームを閉じずに可視状態だけ戻したいアプリは false。</summary>
    protected virtual bool CloseDependentFormAfterCapture => true;

    /// <summary>依存子フォームを全て撮ったあと (例: 代表結晶を既定へ戻す)。--capture のみ。</summary>
    protected virtual void AfterDependentForms(Form main, Action<string> trace) { }

    /// <summary>
    /// 260524Cl 追加 (ReciPro) / 260820Cl フック化: フォーム内の GPU 描画コントロールを通常描画して可視バッファへ最新シーンを出す。
    /// CopyFromScreen は画面の front buffer を読むため、撮影前に GL シーンを画面へ反映しておく必要がある。
    /// 本ライブラリは Crystallography.OpenGL を参照しないので既定は何もしない (ReciPro が GLControlAlpha.Render() を注入する)。
    /// <see cref="Settle"/> と <see cref="WaitUntilScreenStable"/> から毎回呼ばれる。
    /// </summary>
    protected virtual void RenderGpuControls(Form form, Action<string> trace) { }

    /// <summary>
    /// 260601Cl 追加 (IPAnalyzer) / 260820Cl フック化: フォーム内の全 TabControl について各 TabPage を順に選択して
    /// TabControl 全体 (タブ見出し込み) を撮るか。Designer で Capture=true を付けて回るのを待たずにタブ単位のクロップを
    /// 得たいアプリ (IPAnalyzer) が true にする。命名は Capture=true クロップと同じ規則 (<see cref="BuildCapturePath"/>)。
    /// 既定 false (ReciPro は opt-in の Capture=true 運用で、自動化すると既存画像が大量に増える)。
    /// </summary>
    protected virtual bool CaptureAllTabPagesEnabled => false;

    /// <summary>
    /// 全体画像を CopyFromScreen する直前に呼ばれる。ToolStripPanel 上の menuStrip/toolStrip が DoEvents だけでは
    /// 初回描画されず左上がグレーで写るアプリ (PDIndexer) が、ここで再描画を強制する。既定は何もしない。
    /// </summary>
    protected virtual void BeforeFullFormCapture(Form form, Action<string> trace) { }

    /// <summary>
    /// --capture 中に UI スレッドで起きた未処理例外の記録。既定は型名・メッセージ・スタック先頭数フレームをログへ出し、
    /// 260802Cl (ReciPro): GDI+ の "Parameter is not valid." (ArgumentException) なら開いている全フォームの PictureBox を舐めて
    /// Image が破棄済みのものを名前付きで報告する (描画中の例外はスタックに「どのコントロールか」が出ないため)。
    /// </summary>
    protected virtual void OnThreadException(Exception exception, Action<string> trace)
    {
        //260802Cl 変更: 型名とメッセージだけでは発生箇所が分からず調査できなかったので、スタックの先頭数フレームも残す
        //(旧: Trace($"\tThreadException\t{e.Exception.GetType().Name}: {e.Exception.Message}"))。
        //GDI+ の "Parameter is not valid." のように、メッセージが完全に無情報な例外がここに来る
        var frames = (exception.StackTrace ?? "").Split('\n').Take(6).Select(s => s.Trim());
        trace($"\tThreadException\t{exception.GetType().Name}: {exception.Message}\t{string.Join(" | ", frames)}");
        //260802Cl 追加: 描画中の例外はスタックに「どのコントロールか」が出ない (PictureBox.OnPaint までしか分からない)。
        //GDI+ の "Parameter is not valid." は Image が破棄済みのときの典型なので、開いている全フォームの
        //PictureBox を舐めて Image が生きているか実際に触って確かめ、壊れているものを名前付きで報告する
        if (exception is ArgumentException)
            foreach (Form form in Application.OpenForms)
                foreach (var pb in EnumerateControls(form).OfType<PictureBox>())
                {
                    var img = pb.Image;
                    if (img is null) continue;
                    try { _ = img.Width; }//破棄済み Bitmap は Width で ArgumentException
                    catch (Exception ex)
                    {
                        var path = pb.Name;
                        for (var c = pb.Parent; c is not null; c = c.Parent)
                            path = (string.IsNullOrEmpty(c.Name) ? c.GetType().Name : c.Name) + "." + path;
                        var spb = pb.Parent as ScalablePictureBox;
                        trace("\tThreadException\t  -> broken Image on " + path
                            + $" spb#{(spb is null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(spb))}"
                            + $" pb#{(spb?.PseudoBitmap is null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(spb.PseudoBitmap))}"
                            + $" cli={pb.ClientSize.Width}x{pb.ClientSize.Height}"
                            + "\t" + ex.GetType().Name + ": " + ex.Message);
                    }
                }
    }

    #endregion

    #region --capture: 全フォームを CopyFromScreen で撮る

    /// <summary>260820Cl 追加: 1 回の --capture 実行の状態 (出力先・ログ・成否カウンタ・絞り込み・撮影済みインスタンス)。フックへ渡す。</summary>
    protected sealed class CaptureSession
    {
        private readonly GuiCaptureHarness owner;
        private readonly string[] captureOnly;

        internal CaptureSession(GuiCaptureHarness owner, string outDir, Action<string> trace, string[] captureOnly)
        {
            this.owner = owner;
            this.captureOnly = captureOnly;
            OutDir = outDir;
            Trace = trace;
        }

        public string OutDir { get; }
        public Action<string> Trace { get; }
        /// <summary>構築済みのメインフォーム (未構築なら null)。</summary>
        public Form Main { get; internal set; }
        public int Ok { get; internal set; }
        public int Fail { get; internal set; }
        /// <summary>撮影済みのフォームインスタンス (依存子フォームの二重撮影防止)。</summary>
        public HashSet<Form> Captured { get; } = new();

        /// <summary>絞り込み (環境変数) に照らして、この型名を撮るか。</summary>
        public bool ShouldCapture(string typeName) => captureOnly.Length == 0
            || captureOnly.Any(s => typeName.Contains(s, StringComparison.OrdinalIgnoreCase));

        /// <summary>1 フォームを撮る。失敗しても例外を外へ出さず Fail を数えて次へ進む。</summary>
        public void Capture(Form form, string name, bool closeAfterCapture = true)
        {
            try
            {
                owner.CaptureForm(form, name, OutDir, Trace, closeAfterCapture);
                Captured.Add(form);
                Ok++;
            }
            catch (Exception ex)
            {
                Fail++;
                Trace($"{name}\tFAIL\t{ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// --capture の本体。アプリ内の parameterless ctor を持つ Form を順に構築し、フォーム単位の PNG を保存する。
    /// メインフォームは他フォームの代表状態を作るため最後まで保持する。通常起動からは呼ばない開発者向け経路。
    /// </summary>
    public void Run(string outDir)
    {
        // 260525Cl: 引数省略時の既定保存先は docs/src/assets/cap-{culture}-auto (Pages 正本化に伴い画像も docs/src 側へ集約)。
        outDir ??= DefaultOutputDir(ForcedUICulture ?? Thread.CurrentThread.CurrentUICulture);
        Directory.CreateDirectory(outDir);

        var log = new List<string>();
        void Trace(string s)
        {
            var line = $"{DateTime.Now:HH:mm:ss.fff}\t{s}";
            log.Add(line);
            Console.WriteLine(line);
        }

        // フォームの Load / VisibleChanged 等で投げられた例外を握りつぶす。
        // これをしないと WinForms 標準の未処理例外ダイアログ (モーダル) が出てハーネスがハングする
        // (例: ReciPro の FormCTF を親なしで構築すると get_ImageMode が NullReferenceException)。
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => OnThreadException(e.Exception, Trace);

        Trace($"capture start -> {outDir}");

        // 260524Cl 追加: CopyFromScreen は物理画面を読むため、RDP セッションが非表示・最小化・フォーカス喪失だと
        // "ハンドルが無効です" で失敗する。毎回最初に必ず注意喚起を出す (ユーザー要望)。
        Trace("==================================================================================");
        Trace("[CAUTION] Capture uses CopyFromScreen. Keep the screen VISIBLE and FOCUSED until done.");
        Trace("          Over Remote Desktop (RDP): keep the RDP window in the foreground, and do NOT");
        Trace("          minimize or disconnect. A hidden/minimized session yields blank/failed shots.");
        Trace("[注意] 画面キャプチャ中はウィンドウを前面・表示のまま保ってください。RDP の場合は RDP ウィンドウを");
        Trace("       前面に出したまま最小化・切断しないでください (非表示だと撮影が失敗/真っ黒になります)。");
        Trace("==================================================================================");

        // 起動時に画面が取得可能か 8x8 で試し、不可ならその場で警告する (全フォーム失敗の前に気付けるように)。
        using (var probe = CaptureScreen(new Rectangle(0, 0, 8, 8), null, Trace, "screen-probe"))
        {
            if (probe == null)
                Trace("[CAUTION] Screen capture is currently UNAVAILABLE. Bring the (RDP) session to the foreground now.");
        }

        // 260602Cl 追加: 環境変数 (<see cref="CaptureOnlyEnvVar"/>、カンマ区切りの型名部分一致) が指定されたら、その対象だけを撮る。
        var captureOnly = (Environment.GetEnvironmentVariable(CaptureOnlyEnvVar) ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var session = new CaptureSession(this, outDir, Trace, captureOnly);
        if (captureOnly.Length > 0)
            Trace($"{CaptureOnlyEnvVar} = {string.Join(",", captureOnly)}");

        var types = EnumerateFormTypes().ToList();
        if (captureOnly.Length > 0) // メインフォームは親情報供給に必須なので常に残す
            types = types.Where(t => t == MainFormType || session.ShouldCapture(t.Name)).ToList();
        Trace($"{types.Count} form types (parameterless ctor)");

        foreach (var type in types)
        {
            if (SkipInEnumeration(type))
                continue;

            Form form = null;
            try
            {
                // 260522Cl: 直前のフォーム (特にメインフォーム) がレジストリ値でカルチャを書き換えても、強制指定があれば戻す。
                if (ForcedUICulture != null)
                    Thread.CurrentThread.CurrentUICulture = ForcedUICulture;
                form = (Form)Activator.CreateInstance(type);
                if (type == MainFormType)
                    session.Main = form; // (260523Ch) reflection 順の先頭で作ったメインフォームを後続フォームの親情報として再利用する
                else
                    WireDependencies(form, session.Main); // 260617Cl: 子フォームへの親情報注入 (--capture と --diagnose で共用)

                if (session.ShouldCapture(type.Name))
                {
                    CaptureForm(form, type.Name, outDir, Trace, closeAfterCapture: !ReferenceEquals(form, session.Main));
                    session.Captured.Add(form);
                    session.Ok++;
                    AfterFormCaptured(form, session); // 既定: メインフォーム直後にマクロエディタを撮る
                }
                else if (ReferenceEquals(form, session.Main))
                {
                    // 260602Cl: メインフォームがフィルタ対象外でも、後続フォームの Simulate 等が親情報を要るため、
                    // 表示して代表状態づくりだけ済ませ、開いたまま保持する (撮影・保存はしない)。
                    try { form.Show(); } catch (Exception ex) { Trace($"{type.Name}\tWARN\tShow: {ex.GetType().Name}: {ex.Message}"); }
                    Settle(form, FirstPaintSettleMs, Trace);
                    PrepareCaptureState(form, Trace);
                    Settle(form, PrepareSettleMs, Trace);
                }
            }
            catch (Exception ex)
            {
                session.Fail++;
                Trace($"{type.Name}\tFAIL\t{ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                if (!ReferenceEquals(form, session.Main))
                {
                    try { form?.Dispose(); } catch { /* 破棄時例外は無視 */ }
                }
            }
        }

        // 260523Cl 追加: 親情報が必要で reflection 列挙では撮れない子フォームを、メインフォームが持つ配線済みインスタンス経由で撮る。
        if (session.Main != null)
        {
            foreach (var child in EnumerateDependentForms(session.Main))
            {
                if (child == null || child.IsDisposed || session.Captured.Contains(child))
                    continue;
                if (!session.ShouldCapture(child.GetType().Name)) // 260602Cl: フィルタ対象外の依存子フォームは撮らない
                    continue;
                try
                {
                    BeforeDependentFormCapture(child, session.Main, Trace);
                    Application.DoEvents();
                    CaptureForm(child, child.GetType().Name, outDir, Trace, closeAfterCapture: CloseDependentFormAfterCapture);
                    session.Captured.Add(child);
                    session.Ok++;
                    AfterDependentFormCapture(child, session.Main, Trace);
                }
                catch (Exception ex)
                {
                    session.Fail++;
                    Trace($"{child.GetType().Name}\tFAIL\t{ex.GetType().Name}: {ex.Message}");
                }
            }
            AfterDependentForms(session.Main, Trace);
        }

        // 260726Cl: Close() は FormClosing → レジストリ書込を発火させ、--capture で強制したカルチャ (ru 等) を UI 言語として
        //   レジストリへ焼き付けてしまう (11 言語ぶん撮ると最後の言語で普段のアプリが起動する)。設定ファイルの上書きも同様。
        //   Dispose は FormClosing を発火しない。
        try { session.Main?.Dispose(); } catch { /* 破棄時例外は無視 */ }

        // 260726Cl 追加: --diagnose と同じ「中央訳テーブルの未解決エントリ」報告。
        //   capture はクロップ時に祖先 TabPage を選択するので、未選択タブ上の UserControl も OnLoad が走る。
        //   つまり diagnose より観測範囲が広く、こちらでしか見えない未解決がある
        //   (実例: AtomCoordinateTable は tabPageCoordinateInformation 上にあり diagnose では素通りする)。
        var unresolved = CodeLocalizer.UnresolvedEntries;
        if (unresolved.Count > 0)
        {
            Trace($"CodeLocalizer: 未解決エントリ {unresolved.Count} 件 (訳テーブルにあるがコントロールに当たらない)");
            foreach (var u in unresolved)
                Trace($"  未解決\t{u}");
        }
        Trace($"done: ok={session.Ok} fail={session.Fail}, unresolved localization entries={unresolved.Count}");
        File.WriteAllLines(Path.Combine(outDir, "_capture-log.tsv"), log);
    }

    /// <summary>
    /// 1 つの Form を画面内に最前面表示して撮影する (260524Cl: DrawToBitmap から CopyFromScreen 方式へ変更)。
    /// Show → 最前面化 → 描画待ち → 代表状態準備 → 再描画待ち の後、ウィンドウ全体を CopyFromScreen で撮り、
    /// 続けて Capture=true のコントロール単位クロップを撮る。closeAfterCapture=false は後続フォームの準備にメインフォームを使うための例外。
    /// </summary>
    protected void CaptureForm(Form form, string name, string outDir, Action<string> trace, bool closeAfterCapture = true)
    {
        form.StartPosition = FormStartPosition.Manual;
        form.ShowInTaskbar = false;
        form.Location = new Point(0, 0); // CopyFromScreen で実描画を撮るため画面内に表示する
        // Show() で Visible=true にしないと子コントロールが描画されない。
        // Load 等の例外は ThreadException ハンドラへ流れるためモーダル化せず、ハングしない。
        // ただし Show() の呼び出しスタック上で同期的に投げられる例外もあるため、try で囲んで
        // 例外が出てもハンドル/レイアウト生成済みなら撮影を試みる (部分的にでも撮る)。
        try { form.Show(); }
        catch (Exception ex) { trace($"{name}\tWARN\tShow: {ex.GetType().Name}: {ex.Message}"); }

        BringToFront(form);
        Settle(form, FirstPaintSettleMs, trace);
        PrepareCaptureState(form, trace); // (260523Ch) 代表状態づくり (ReciPro: FormMain は spinel 選択、FormTrajectory は Simulate 相当)
        Settle(form, PrepareSettleMs, trace);

        // 260524Cl: prepare 中に子フォーム生成や DoEvents で他アプリ (IDE 等) が前面を奪い、CopyFromScreen が別ウィンドウを
        // 撮ってしまうことがある (例: FormDiffractionSimulator.Draw 後に VS Code が前面化)。撮影直前に再度最前面化する。
        BringToFront(form);
        Settle(form, TabSwitchSettleMs, trace);
        BeforeFullFormCapture(form, trace); // 260820Cl: ホストアプリの撮影直前処理 (PDIndexer: ToolStrip 再描画)

        var bounds = GetWindowVisualBounds(form); // タイトルバー等の非クライアント領域も含むウィンドウ全体 (影は除く)
        var bmp = CaptureScreen(bounds, form, trace, name, retryIfSolid: true);
        var captured = bmp != null;
        if (captured)
            using (bmp) bmp.Save(Path.Combine(outDir, name + ".png"), ImageFormat.Png);
        else
            trace($"{name}\tWARN\tfull-form capture failed (RDP screen hidden/minimized?)"); // 撮れなくても次のフォームへ進む
        var cropCount = CaptureControlCrops(form, name, outDir, trace); // 260523Cl: Capture=true のコントロール単位クロップ
        var tabCount = CaptureAllTabPagesEnabled ? CaptureAllTabPages(form, name, outDir, trace) : 0; // 260601Cl (IPAnalyzer): 全 TabPage を自動でタブ単位クロップ
        var menuCount = CaptureToolStripItemCrops(form, name, outDir, trace); // 260527Cl: Capture=true の ToolStripItem (メニュー展開) クロップ
        trace($"{name}\t{(captured ? "OK" : "PARTIAL")}\t{bounds.Width}x{bounds.Height}\tCrops={cropCount}"
            + (CaptureAllTabPagesEnabled ? $"\tTabs={tabCount}" : "") + $"\tMenus={menuCount}");

        // 260602Cl〜: モード別・タブ別の追加ショット (ホストアプリのフォームは override 側、本ライブラリのフォームは既定実装)。
        CaptureExtraShots(form, name, outDir, trace);

        if (closeAfterCapture)
        {
            form.TopMost = false; // (260524Cl) 後続フォームの最前面化を妨げないよう閉じる前に解除
            form.Close();
        }
    }

    /// <summary>
    /// 260807Cl 追加: 特殊撮影 (モード別・タブ別) 5 種に共通する定型を 1 箇所へ集約する。
    /// 各メソッドで固有なのは「状態づくり (<paramref name="apply"/>)」と「何を撮るか (<paramref name="capture"/>)」だけで、
    /// 残り — レイアウト反映待ち・最前面化・計算完了待ち・保存・失敗しても次へ進む・後始末 — は全て同じだった。
    /// <paramref name="prepare"/> を渡すと計算を起動して <see cref="WaitUntilScreenStable"/> で完了を待つ
    /// (重い計算を伴うモード撮影用)。<paramref name="restore"/> は例外時も必ず走る。
    /// </summary>
    protected void CaptureVariant(Form form, string name, string outDir, Action<string> trace,
        Action apply, Func<Bitmap> capture, Action prepare = null, bool waitStable = false, Action restore = null)
    {
        try
        {
            apply();
            Settle(form, TabSwitchSettleMs, trace); // レイアウト反映を待つ
            BringToFront(form);
            prepare?.Invoke();                      // 現在の状態で計算を起動 (同期のものも非同期のものもある)
            if (waitStable)
                WaitUntilScreenStable(form, trace); // 計算完了 (= 画面が止まる) まで待つ
            BringToFront(form);
            Settle(form, TabSwitchSettleMs, trace);

            var bmp = capture();
            if (bmp != null)
                using (bmp) bmp.Save(Path.Combine(outDir, name + ".png"), ImageFormat.Png);
            else
                trace($"{name}\tWARN\tvariant capture failed");
        }
        catch (Exception ex)
        {
            // 1 バリエーションの失敗で残りを諦めない (ハーネス全体の「可能な限り次へ進む」方針)。
            trace($"{name}\tWARN\tvariant shot: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            try { restore?.Invoke(); } catch { /* 後始末の失敗で撮影結果を捨てない */ }
        }
    }

    /// <summary>
    /// 260608Cl 追加: FormBeamInteraction を「線種 (X線/電子線/中性子線) × タブ」ごとに撮る。
    /// 各タブの内容は線種で大きく変わる (減衰・輸送/散乱因子は曲線・表が別物、蛍光は X線専用で電子/中性子では消える) ため、
    /// 線源を切り替え → 表示中の各 TabPage を選択 → TabControl 全体 (タブ見出し込み) をクロップし、
    /// <c>FormBeamInteraction-{xray|electron|neutron}-{reflections|attenuations|scattering|fluorescence}.png</c> として保存する。
    /// マニュアルの各タブ節 (X線/電子線/中性子線の content-tab) から参照する。標準の全体画像・波長コントロールクロップは
    /// CaptureForm 側で既に撮れている。線種で TabPages が増減する (蛍光は X線のみ) ので ToArray でスナップショットして列挙する。
    /// </summary>
    private void CaptureBeamInteractionModeShots(FormBeamInteraction form, string baseName, string outDir, Action<string> trace)
    {
        var beams = new[]
        {
            (Crystallography.WaveSource.Xray, "xray"),
            (Crystallography.WaveSource.Electron, "electron"),
            (Crystallography.WaveSource.Neutron, "neutron"),
        };
        foreach (var (src, beamSuffix) in beams)
        {
            try
            {
                form.SetCaptureBeam(src);              // 線源切替 → ApplyBeamDependentVisibility で蛍光タブの増減 (X線のみ) も反映
                Settle(form, TabSwitchSettleMs, trace);
                BringToFront(form);
                var tc = form.CaptureTabControl;
                foreach (var tab in tc.TabPages.Cast<TabPage>().ToArray()) // 線種で増減するのでスナップショット
                {
                    var name = baseName + "-" + beamSuffix + "-" + BeamInteractionTabKey(tab.Name);
                    CaptureVariant(form, name, outDir, trace, //260807Cl /simplify2: 定型部を CaptureVariant へ集約
                        apply: () => tc.SelectedTab = tab, // SelectedIndexChanged → UpdateAllTabs で当該タブを計算
                        capture: () => CaptureScreen(new Rectangle(GetScreenLocation(tc), tc.Size), form, trace, name, retryIfSolid: true));
                }
            }
            catch (System.Exception ex) { trace($"{baseName}-{beamSuffix}\tWARN\tbeam mode: {ex.GetType().Name}: {ex.Message}"); }
        }
        try { form.SetCaptureBeam(Crystallography.WaveSource.Xray); } catch { /* 撮影後 close 前に既定 (X線) へ戻す */ }
    }

    /// <summary>260705Cl 追加: FormGroupRelations の残りの詳細タブ (既定選択の Matrix は全体画像で撮れている) を
    /// クロップ撮影する。PrepareCaptureForGuiAudit (代表部分群の選択) は PrepareCaptureState (既定実装) で既に実行済み。</summary>
    private void CaptureGroupRelationsDiagramShot(FormGroupRelations form, string baseName, string outDir, Action<string> trace)
    {
        try
        {
            var tc = form.CaptureTabControl;
            //foreach (var tabName in new[] { "tabOrbit", "tabDomains", "tabReflections", "tabDiagram" })
            foreach (var tabName in new[] { "tabOrbit", "tabDomains", "tabReflections", "tabDiagram", "tabPointGroups", "tabElements" }) // 260712Cl: 点群 Hasse 図タブ (③-4)、260713Cl: 対称要素タブ (③-2)
            {
                var tab = tc.TabPages.Cast<TabPage>().FirstOrDefault(t => t.Name == tabName);
                if (tab == null) { trace($"{baseName}-{tabName}\tWARN\t{tabName} not found"); continue; }
                var name = baseName + "-" + tabName;
                //260807Cl /simplify2: tabElements だけは「一時拡大 → PictureBox の Image を直接保存」で
                //CaptureVariant の枠 (画面撮影して 1 枚保存) に収まらないため、こちらは従来どおり手続きで書く。
                //それ以外のタブは CaptureVariant へ寄せる (下)
                tc.SelectedTab = tab;
                Settle(form, TabSwitchSettleMs, trace);
                BringToFront(form);
                Settle(form, TabSwitchSettleMs, trace);
                // 260713Cl: tabElements は 2 パス重ね描き (透明ビットマップ×2 + ColorMatrix) で GDI 負荷が高く、
                // RDP の CopyFromScreen が「ハンドル作成エラー (Win32Exception)」で失敗しやすい。pictureBox の
                // Image を直接クローン保存すれば screen-capture 依存を外せて確実 (プログラム描画なので画素も厳密)。
                // 260717Cl: 既定フォームサイズだと左右分割後の各図が小さく、立方晶 (spinel) の要素/一般位置が
                // 判読不能になるため、この direct-image 撮影の間だけフォームを一時拡大する (他タブの CopyFromScreen
                // 撮影サイズは従来どおり)。
                if (tabName == "tabElements" && tab.Controls.Find("pictureBoxElements", true).FirstOrDefault() is PictureBox pbElem && pbElem.Image != null)
                {
                    var originalSize = form.Size; // 260717Cl 追加 (一時拡大)
                    try
                    {
                        form.Size = new Size(1250, 860);
                        Settle(form, TabSwitchSettleMs, trace); // SizeChanged → RenderElements 再描画を反映
                        using var clone = new Bitmap(pbElem.Image);
                        clone.Save(Path.Combine(outDir, name + ".png"), ImageFormat.Png);
                        trace($"{name}\tOK\tdirect image {pbElem.Image.Width}x{pbElem.Image.Height}");
                    }
                    finally
                    {
                        form.Size = originalSize;
                        Settle(form, TabSwitchSettleMs, trace);
                    }
                    continue;
                }
                CaptureVariant(form, name, outDir, trace,
                    apply: () => tc.SelectedTab = tab, // 既に選択済みだが、CaptureVariant の安定待ちに載せるため再指定
                    capture: () => CaptureScreen(new Rectangle(GetScreenLocation(tc), tc.Size), form, trace, name, retryIfSolid: true));
            }
        }
        catch (System.Exception ex) { trace($"{baseName}-diagram\tWARN\tdiagram shot: {ex.GetType().Name}: {ex.Message}"); }
    }

    /// <summary>
    /// 260807Cl 追加 (/simplify2 フォローアップ): 固定サイズのテキスト系コントロールを採寸し、
    /// 表示枠に収まらないものを撮影ログへ出す。--capture は 11 言語ぶん全フォームを回るので、
    /// 「ある言語でだけラベルが切れる/描かれない」を撮影のついでに全面検出できる。
    ///
    /// きっかけ: 菊池の注記ラベルが ja でだけ完全に消えた事故 (resx の Font に type 属性が無く
    /// 9.75pt のまま残り、行高 17px が 16px の枠に入らず GDI が 1 行も描かなかった)。
    /// 幅超過は AutoEllipsis で「…」になるが、高さ超過は無言で消えるので目視では気づけない。
    ///
    /// AutoSize / 複数行 / 空文字 / 非テキスト系は対象外 (自前で伸びるか、そもそも切れない)。
    /// 判定は報告のみ — 撮影は続行する。
    /// </summary>
    private static void ReportTextOverflow(Form form, string formName, Action<string> trace)
    {
        foreach (var c in EnumerateControls(form))
        {
            // ⚠対象は「Text をそのまま TextRenderer で描く標準コントロール」だけに絞る。
            //   自前描画のもの (例 LabelLaTeX: Text は "\alpha" のような LaTeX ソースで、描かれる字形とは別物) を
            //   混ぜると誤検出だらけになり、本当の溢れが埋もれる
            if (c is not (Label or CheckBox or RadioButton or Button) || c.GetType().Assembly != typeof(Label).Assembly)
                continue;
            if (c.AutoSize || string.IsNullOrEmpty(c.Text) || c.IsDisposed || c.Width <= 0 || c.Height <= 0 || c.Font == null)
                continue;
            if (c.Text.Contains('\n'))
                continue; // 明示的な複数行は行数ぶんの高さを持つ前提なので 1 行採寸では判定できない

            try
            {
                // NoPadding 必須: 既定の MeasureText は左右に余白を足すので、そのまま比べると
                // 「%」1 文字のラベル等が軒並み数 px 超過に見える (実際には収まっている)
                var need = TextRenderer.MeasureText(c.Text, c.Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                // チェック/ラジオはボックスとその余白 (約 20px) がテキストの外側に要る
                var extra = c is CheckBox or RadioButton ? 20 : 0;
                var overW = need.Width + extra - c.Width;
                var overH = need.Height - c.Height;
                // ⚠高さ超過を鳴らすのは AutoEllipsis の Label だけ。この組み合わせだけが
                //   「行が枠に入らないと『…』すら出さず丸ごと描かれない」挙動になる (ja の注記ラベルが消えた事故)。
                //   AutoEllipsis 無しの Label や Button は 1〜2px 足りなくても普通にクリップ表示されるので、
                //   そこまで拾うと既存フォームの無害な 1px 不足で埋まって本物が見えなくなる
                var heightMatters = c is Label { AutoEllipsis: true } && overH > 0;
                if (overW > 2 || heightMatters)
                    trace($"{formName}.{c.Name}\tWARN\ttext does not fit: '{c.Text}' " +
                          $"box={c.Width}x{c.Height} needs={need.Width + extra}x{need.Height}" +
                          (heightMatters ? $"\t*** {overH}px TOO SHORT (the line may not be drawn at all) ***" : $"\t(+{overW}px wide)"));
            }
            catch (Exception ex) { trace($"{formName}.{c.Name}\tWARN\ttext measure: {ex.GetType().Name}: {ex.Message}"); }
        }
    }

    /// <summary>TabPage 名 → ファイル名サフィックス。260608Cl 追加。</summary>
    private static string BeamInteractionTabKey(string tabPageName) => tabPageName switch
    {
        "tabPageReflections" => "reflections",
        "tabPageAttenuations" => "attenuations",
        "tabPageScatteringFactors" => "scattering",
        "tabPageFluorescence" => "fluorescence",
        _ => tabPageName,
    };

    /// <summary>260524Cl 追加: CopyFromScreen の前に対象フォームを通常表示・最前面・アクティブ化する。</summary>
    protected static void BringToFront(Form form)
    {
        try
        {
            if (form.WindowState != FormWindowState.Normal)
                form.WindowState = FormWindowState.Normal;
            form.TopMost = true; // 無人実行中に他ウィンドウが被って映り込むのを防ぐ
            form.BringToFront();
            form.Activate();
            if (form.IsHandleCreated)
                SetForegroundWindow(form.Handle); // RDP でフォーカスが他へ移っても撮影対象を前面へ取り戻す
        }
        catch { /* 表示状態変更時の例外は無視 (撮影は後段で最善努力) */ }
    }

    /// <summary>
    /// 260524Cl 追加: 指定ミリ秒のあいだ DoEvents を回して描画を画面へ反映させる。
    /// --capture は Application.Run を回さないため、CopyFromScreen の前にこの明示的な描画待ちが要る。
    /// GPU (OpenGL) 領域は通常の Invalidate では更新されないことがあるので、毎回 <see cref="RenderGpuControls"/> で可視バッファへ最新シーンを出す。
    /// </summary>
    protected void Settle(Form form, int ms, Action<string> trace)
    {
        try { form.Refresh(); } catch { /* Refresh 時例外は無視 */ }
        RenderGpuControls(form, trace); // GL 描画はホストアプリのフック (ReciPro: GLControlAlpha.Render())
        var until = Environment.TickCount64 + Math.Max(ms, 0);//260718Cl TickCount→TickCount64 (32bit の ~24.9 日ラップ回避)
        do
        {
            Application.DoEvents();
            System.Threading.Thread.Sleep(15);
        } while (Environment.TickCount64 < until);
    }

    private const int CaptureMaxAttempts = 5; // 260524Cl: CopyFromScreen 失敗時の最大試行回数

    /// <summary>
    /// 260524Cl 追加 / 堅牢化: 画面上の指定矩形を CopyFromScreen で撮ってビットマップ化する。
    /// RDP セッションが非表示・最小化・フォーカス喪失だと CopyFromScreen は <see cref="System.ComponentModel.Win32Exception"/>
    /// ("ハンドルが無効です") を投げたり全面単色を返したりする。そこで失敗時は foregroundForm を取り直して待ち、
    /// 数回まで再試行する。最終的に撮れなければ null を返し、呼び出し側は画像を保存せず次へ進む (1 枚の失敗で全体を止めない)。
    /// retryIfSolid=true (フォーム全体) では全面単色も「実描画が読めていない」とみなして再試行・null 化する
    /// (黒画像で既存 Wiki 画像を上書きしないため)。クロップ (retryIfSolid=false) の単色は呼び出し側 IsSolidColor が正規にスキップする。
    /// </summary>
    protected static Bitmap CaptureScreen(Rectangle screenRect, Form foregroundForm = null, Action<string> trace = null, string label = null, bool retryIfSolid = false)
    {
        int w = Math.Max(screenRect.Width, 1), h = Math.Max(screenRect.Height, 1);
        for (int attempt = 1; attempt <= CaptureMaxAttempts; attempt++)
        {
            Bitmap bmp = null;
            try
            {
                bmp = new Bitmap(w, h);
                using (var g = Graphics.FromImage(bmp))
                    g.CopyFromScreen(screenRect.Location, Point.Empty, new Size(w, h));

                if (!retryIfSolid || !IsSolidColor(bmp))
                    return bmp; // 成功

                bmp.Dispose(); // 全面単色 = RDP で実描画が読めていない可能性。破棄して再試行。
                trace?.Invoke($"{label}\tWARN\tCopyFromScreen blank attempt {attempt}/{CaptureMaxAttempts}");
            }
            catch (Exception ex)
            {
                bmp?.Dispose();
                trace?.Invoke($"{label}\tWARN\tCopyFromScreen attempt {attempt}/{CaptureMaxAttempts}: {ex.GetType().Name}: {ex.Message}");
            }

            if (attempt == CaptureMaxAttempts)
                break;
            if (foregroundForm != null)
                BringToFront(foregroundForm); // RDP の一時的なフォーカス喪失対策にフォアグラウンドを取り直す
            System.Threading.Thread.Sleep(400 * attempt); // 線形バックオフ
            Application.DoEvents();
        }
        return null;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out RECT rect, int size);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd); // 260524Cl: CopyFromScreen 前に対象ウィンドウを確実に前面へ

    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    /// <summary>
    /// 260524Cl 追加: CopyFromScreen で撮るウィンドウ全体の矩形を求める。
    /// WinForms の <see cref="Control.Bounds"/> (GetWindowRect 由来) は Win10/11 の不可視リサイズ枠を含むため
    /// 実際の描画ウィンドウより一回り大きく、そのまま CopyFromScreen すると下端などに背後のデスクトップが写り込む。
    /// DWM の実視覚矩形 (DWMWA_EXTENDED_FRAME_BOUNDS、影は除く) が取れればそれを使い、失敗時のみ Bounds に戻す。
    /// </summary>
    protected static Rectangle GetWindowVisualBounds(Form form)
    {
        try
        {
            if (form.IsHandleCreated
                && DwmGetWindowAttribute(form.Handle, DWMWA_EXTENDED_FRAME_BOUNDS, out var r, System.Runtime.InteropServices.Marshal.SizeOf<RECT>()) == 0
                && r.Right > r.Left && r.Bottom > r.Top)
                return new Rectangle(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
        }
        catch { /* P/Invoke 失敗時は Bounds にフォールバック */ }
        return form.Bounds;
    }

    /// <summary>260524Cl 追加: コントロールの実際の左上スクリーン座標を求める (FormCaptureGUI と同一規則)。</summary>
    protected static Point GetScreenLocation(Control control)
        => control is Form ? control.Bounds.Location
         : control.Parent != null ? control.Parent.PointToScreen(control.Location)
         : control.PointToScreen(Point.Empty);

    /// <summary>
    /// 260523Cl 追加 / 260524Cl 改修: Designer で <c>Capture=true</c> を付けたコントロール単位のクロップを、対話 UI を出さずに生成する。
    /// 各対象を CopyFromScreen で個別に撮る (FormCaptureGUI と同方式)。命名は手動キャプチャと同じ規則
    /// (form.Name 起点、SplitterPanel/ToolStripPanel/無名は除外) で、既存の Wiki 画像 raw URL を壊さない。
    /// Capture=true 判定は <see cref="CaptureExtender.IsCaptureEnabled"/>、対象列挙・パス命名・空白判定はここで行う。
    /// </summary>
    /// <returns>保存できたクロップ数。</returns>
    private int CaptureControlCrops(Form form, string name, string outDir, Action<string> trace)
    {
        ReportTextOverflow(form, name, trace); //260807Cl 追加 (/simplify2 フォローアップ)
        var count = 0;
        // Capture=true のコントロールを列挙する。ToolStripItem (メニュー展開) は別ウィンドウで EnumerateControls に
        // 現れないため、<see cref="CaptureToolStripItemCrops"/> が別途撮る。
        foreach (var control in EnumerateControls(form))
        {
            if (control is Form || string.IsNullOrEmpty(control.Name) || control.IsDisposed || control.Width <= 0 || control.Height <= 0)
                continue;
            if (!CaptureExtender.IsCaptureEnabled(control))
                continue;

            try
            {
                // タブを選択し直したときだけ再描画を待つ (毎回の長い待機を避ける)。
                if (EnsureAncestorTabsSelected(control))
                    Settle(form, TabSwitchSettleMs, trace);

                Bitmap crop;
                if (!IsEffectivelyVisible(form, control))
                {
                    // 260524Cl: 既定で Visible=false の Capture=true コントロール (例: 歳差モードでのみ表示される
                    // flowLayoutPanelPED) は通常表示に現れないため、一時的に可視化・最前面化して単体で撮る。
                    crop = RenderHiddenControl(form, control, trace);
                    if (crop == null)
                        continue;
                }
                else
                {
                    // TabPage は親 TabControl 全体 (タブ見出し込み) を撮る (FormCaptureGUI と同じ見た目)。
                    var region = control is TabPage tabPage && tabPage.Parent is TabControl tabControl ? (Control)tabControl : control;
                    region.Refresh();
                    // 260527Cl: 大きい二重バッファ領域 (例 FormSymmetryInformation.tableLayoutPanel1 内の対称要素/一般位置の図) は
                    // region.Refresh() 直後の 1 回 DoEvents では描画が画面 (front buffer) へ反映されず単色で撮れることがある
                    // (全体像は撮れているのにクロップだけ空白になる)。全体像と同じく Settle で描画を反映させ、
                    // さらに retryIfSolid=true で単色フレームを掴んだら数回撮り直す (本当に単色の領域は最終的に null=スキップ)。
                    Settle(form, TabSwitchSettleMs, trace); // Refresh + RenderGpuControls + DoEvents ループ
                    crop = CaptureScreen(new Rectangle(GetScreenLocation(region), region.Size), form, trace, $"{name}.{control.Name}", retryIfSolid: true);
                    if (crop == null)
                        continue; // RDP 画面が一時的に取得不能 or 何度撮っても単色なら、このクロップは諦めて次へ
                }

                using (crop)
                {
                    if (IsSolidColor(crop))
                        continue; // Visible=false のパネル等で単色になったクロップは保存しない

                    var fileName = SanitizeFileName(BuildCapturePath(form, control)) + ".png";
                    crop.Save(Path.Combine(outDir, fileName), ImageFormat.Png);
                    count++;
                }
            }
            catch (Exception ex)
            {
                // 1 コントロールの失敗で残りのクロップを諦めない (ハーネス全体の「可能な限り次へ進む」方針)。
                trace($"{name}\tWARN\tcrop {control.Name}: {ex.GetType().Name}: {ex.Message}");
            }
        }
        return count;
    }

    /// <summary>
    /// 260601Cl 追加 (IPAnalyzer) / 260820Cl ハーネスへ移動: フォーム内の全 TabControl について、各 TabPage を順に選択して
    /// TabControl 全体 (タブ見出し込み) を撮る。Capture=true の付与を待たずにタブ単位のクロップを得るための拡張
    /// (マニュアルの Property タブ等で粒度を確保)。命名は Capture=true クロップと同じ規則 (BuildCapturePath) なので、本文の参照先と一致する。
    /// <see cref="CaptureAllTabPagesEnabled"/> が true のアプリでのみ呼ばれる。
    /// </summary>
    /// <returns>保存できたタブクロップ数。</returns>
    private int CaptureAllTabPages(Form form, string name, string outDir, Action<string> trace)
    {
        var count = 0;
        foreach (var tabControl in EnumerateControls(form).OfType<TabControl>())
        {
            if (tabControl.IsDisposed || string.IsNullOrEmpty(tabControl.Name) || tabControl.Width <= 0 || tabControl.Height <= 0)
                continue;
            foreach (TabPage tabPage in tabControl.TabPages)
            {
                if (string.IsNullOrEmpty(tabPage.Name))
                    continue;
                try
                {
                    EnsureAncestorTabsSelected(tabPage); // この TabPage と祖先タブをすべて選択・可視化する
                    if (!IsEffectivelyVisible(form, tabControl))
                        continue; // 親が非表示で出せないタブは諦める
                    tabControl.BringToFront();
                    Settle(form, TabSwitchSettleMs, trace);

                    var crop = CaptureScreen(new Rectangle(GetScreenLocation(tabControl), tabControl.Size), form, trace, $"{name}.{tabPage.Name}", retryIfSolid: true);
                    if (crop == null)
                        continue;
                    using (crop)
                    {
                        if (IsSolidColor(crop))
                            continue;
                        var fileName = SanitizeFileName(BuildCapturePath(form, tabPage)) + ".png";
                        crop.Save(Path.Combine(outDir, fileName), ImageFormat.Png);
                        count++;
                    }
                }
                catch (Exception ex)
                {
                    trace($"{name}\tWARN\ttab {tabPage.Name}: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }
        return count;
    }

    /// <summary>
    /// 260527Cl 追加: Designer で <c>Capture=true</c> を付けた ToolStripItem (メニュー項目等) のドロップダウンを
    /// 非対話で撮る。対話ツール FormCaptureGUI.CaptureToolStripItem と同じ方式 (祖先含めて ShowDropDown し、
    /// 開いた DropDown / ContextMenuStrip / Owner ToolStrip を CopyFromScreen) ・同じ命名規則 (form.Name 起点で
    /// owner ToolStrip の Control パス + 項目の OwnerItem 連鎖の名前を "." 連結) で生成する。撮影後は開いた
    /// ドロップダウンを閉じ、後続フォーム/クロップの撮影を妨げないようにする。CaptureControlCrops は Control しか
    /// 列挙しない (メニュードロップダウンは別ウィンドウ) ため、ここで補完する。
    /// </summary>
    /// <returns>保存できたメニュークロップ数。</returns>
    private static int CaptureToolStripItemCrops(Form form, string name, string outDir, Action<string> trace)
    {
        var count = 0;
        foreach (var item in EnumerateToolStripItems(form))
        {
            if (string.IsNullOrEmpty(item.Name) || !CaptureExtender.IsCaptureEnabled(item))
                continue;

            try
            {
                var host = EnsureToolStripCaptureHostVisible(item);
                if (host == null || host.IsDisposed || host.Width <= 0 || host.Height <= 0)
                    continue;

                host.Refresh();
                Application.DoEvents();
                System.Threading.Thread.Sleep(150); // ドロップダウンが画面へ出るまで待つ

                // 260726Cl 追加: 待機中に背後の再描画 (FormImageSimulator の非同期 Simulate 完了など) でドロップダウンが
                // 閉じられることがある。閉じたまま撮ると、その座標にある背後のフォームが写った PNG が
                // 「それらしい見た目」で保存されてしまう (実例: FormImageSimulator の File メニューにシミュレーション像が
                // 写っていた。CaptureScreen の再試行は全面単色のときしか効かないので検出できない)。開き直して確認する。
                for (var retry = 0; retry < 3 && host is ToolStripDropDown && !host.Visible; retry++)
                {
                    host = EnsureToolStripCaptureHostVisible(item);
                    if (host == null || host.IsDisposed) break;
                    host.Refresh();
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(250);
                }
                if (host is ToolStripDropDown && !host.Visible)
                {
                    trace($"{name}\tWARN\tmenu-crop {item.Name}: drop-down dismissed before capture"); // 誤った画像は保存しない
                    continue;
                }

                var crop = CaptureScreen(new Rectangle(host.PointToScreen(Point.Empty), host.Size), form, trace, $"{name}.{item.Name}");
                if (crop != null)
                    using (crop)
                    {
                        if (!IsSolidColor(crop))
                        {
                            var fileName = SanitizeFileName(BuildToolStripItemCapturePath(form, item)) + ".png";
                            crop.Save(Path.Combine(outDir, fileName), ImageFormat.Png);
                            count++;
                        }
                    }
            }
            catch (Exception ex)
            {
                trace($"{name}\tWARN\tmenu-crop {item.Name}: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                try { CloseToolStripDropDowns(item); } catch { /* ドロップダウンのクローズ失敗は無視 */ }
            }
        }
        return count;
    }

    /// <summary>260527Cl 追加: フォーム内の全 ToolStripItem を列挙する (Controls 配下の ToolStrip + designer field の ContextMenuStrip 等。ドロップダウン項目も再帰)。</summary>
    private static IEnumerable<ToolStripItem> EnumerateToolStripItems(Form form)
    {
        var toolStrips = new HashSet<ToolStrip>();
        foreach (var toolStrip in EnumerateControls(form).OfType<ToolStrip>())
            toolStrips.Add(toolStrip);
        // Controls 配下にない ToolStrip (ContextMenuStrip 等) を designer field から拾う (FormCaptureGUI.GetOwnedToolStrips と同趣旨)
        for (var type = form.GetType(); type != null; type = type.BaseType)
            foreach (var field in type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly))
                if (typeof(ToolStrip).IsAssignableFrom(field.FieldType) && field.GetValue(form) is ToolStrip ownedToolStrip)
                    toolStrips.Add(ownedToolStrip);

        var visited = new HashSet<ToolStripItem>();
        foreach (var toolStrip in toolStrips)
            foreach (var item in EnumerateToolStripItems(toolStrip.Items, visited))
                yield return item;
    }

    private static IEnumerable<ToolStripItem> EnumerateToolStripItems(ToolStripItemCollection items, HashSet<ToolStripItem> visited)
    {
        foreach (ToolStripItem item in items)
        {
            if (!visited.Add(item)) continue;
            yield return item;
            if (item is ToolStripDropDownItem dropDownItem && dropDownItem.HasDropDownItems)
                foreach (var child in EnumerateToolStripItems(dropDownItem.DropDownItems, visited))
                    yield return child;
        }
    }

    /// <summary>260527Cl 追加: 対象項目を撮るためのホスト (開いた DropDown / ContextMenuStrip / Owner ToolStrip) を可視化して返す (FormCaptureGUI と同方式)。</summary>
    private static ToolStrip EnsureToolStripCaptureHostVisible(ToolStripItem item)
    {
        EnsureAncestorDropDownsVisible(item);

        if (item is ToolStripDropDownItem dropDownItem && dropDownItem.HasDropDownItems)
        {
            if (!dropDownItem.DropDown.Visible)
            {
                dropDownItem.ShowDropDown(); // File のような親メニュー項目はドロップダウン全体を開いてから撮る
                dropDownItem.DropDown.Refresh();
                Application.DoEvents();
                System.Threading.Thread.Sleep(200);
            }
            return dropDownItem.DropDown;
        }

        if (item.Owner is ContextMenuStrip contextMenuStrip)
        {
            if (!contextMenuStrip.Visible && contextMenuStrip.SourceControl != null)
            {
                contextMenuStrip.Show(contextMenuStrip.SourceControl, new Point(0, contextMenuStrip.SourceControl.Height));
                Application.DoEvents();
                System.Threading.Thread.Sleep(200);
            }
            return contextMenuStrip;
        }

        return item.Owner is ToolStripDropDown toolStripDropDown ? toolStripDropDown : item.Owner;
    }

    /// <summary>260527Cl 追加: 対象項目の祖先ドロップダウンを順に開く (ネストしたサブメニュー対応)。</summary>
    internal static void EnsureAncestorDropDownsVisible(ToolStripItem item)
    {
        if (item.OwnerItem is not ToolStripDropDownItem ownerItem) return;
        EnsureAncestorDropDownsVisible(ownerItem);
        if (!ownerItem.DropDown.Visible)
        {
            ownerItem.ShowDropDown();
            ownerItem.DropDown.Refresh();
            Application.DoEvents();
            System.Threading.Thread.Sleep(200);
        }
    }

    /// <summary>260527Cl 追加: 撮影のために開いたドロップダウンを子→親の順に閉じる (後続の撮影を妨げないため)。</summary>
    private static void CloseToolStripDropDowns(ToolStripItem item)
    {
        for (var current = item; current != null; current = current.OwnerItem)
            if (current is ToolStripDropDownItem dropDownItem && dropDownItem.HasDropDownItems && dropDownItem.DropDown.Visible)
                dropDownItem.HideDropDown();
        Application.DoEvents();
    }

    /// <summary>
    /// 260527Cl 追加: ToolStripItem のキャプチャ用パス (= クロップのファイル名 stem)。owner ToolStrip までの Control パス
    /// (BuildCapturePath。ToolStripPanel 等は除外) に、項目の OwnerItem 連鎖の名前を "." 連結する。
    /// FormCaptureGUI の対話キャプチャと同じ stem になるようにし、本文の `[ここに画像 fileToolStripMenuItem]` 等の指定で解決できるようにする。
    /// </summary>
    private static string BuildToolStripItemCapturePath(Form form, ToolStripItem item)
    {
        var segments = new List<string>();
        for (var current = item; current != null; current = current.OwnerItem)
            segments.Add(string.IsNullOrEmpty(current.Name) ? current.GetType().Name : current.Name);
        segments.Reverse();

        var top = item;
        while (top.OwnerItem != null)
            top = top.OwnerItem;
        var prefix = top.Owner != null ? BuildCapturePath(form, top.Owner) : form.Name;
        return prefix + "." + string.Join(".", segments);
    }

    /// <summary>
    /// 260523Cl 追加 / 260524Cl 改修: コントロールの祖先 TabPage を順に選択し、クロップ時に可視化する。
    /// いずれかのタブ選択を実際に変更したら true (呼び出し側が再描画待ちを入れるため)。
    /// 260524Cl: TabControl を BringToFront して、重なる兄弟コントロール (例: FormStereonet/FormDiffractionSimulator の
    /// stereonet graphicsBox。これらのフォームはタブクリック時に同様の前後入れ替えを行う) より前面に出し、
    /// タブ内容がその背後描画で隠れて撮れない問題を防ぐ。全体像は crop より前に撮るので影響しない。
    /// </summary>
    private static bool EnsureAncestorTabsSelected(Control control)
    {
        var changed = false;
        for (var c = control; c != null; c = c.Parent)
        {
            if (c is TabPage tabPage && tabPage.Parent is TabControl tabControl)
            {
                if (tabControl.SelectedTab != tabPage)
                {
                    tabControl.SelectedTab = tabPage;
                    changed = true;
                }
                tabControl.BringToFront(); // 重なる graphicsBox 等より前面へ (フォーム側のタブ前後入れ替えロジックと同じ z-order 効果)
                tabControl.Refresh();
            }
        }
        return changed;
    }

    /// <summary>260524Cl 追加: control から form まで全て Visible なら true。途中に Visible=false があれば false。</summary>
    private static bool IsEffectivelyVisible(Form form, Control control)
    {
        for (var c = control; c != null && !ReferenceEquals(c, form); c = c.Parent)
            if (!c.Visible)
                return false;
        return true;
    }

    /// <summary>
    /// 260524Cl 改修: 既定で非表示の Capture=true コントロールを撮るため、自身と非表示の祖先を一時的に
    /// Visible=true・最前面にして CopyFromScreen し、撮影後に必ず元の可視状態へ戻す (後続クロップに影響させない)。
    /// 例: 歳差モードでのみ表示される FormDiffractionSimulator の flowLayoutPanelPED。
    /// </summary>
    protected Bitmap RenderHiddenControl(Form form, Control control, Action<string> trace)
    {
        var toggled = new List<Control>();
        for (var c = control; c != null && c is not Form; c = c.Parent)
            if (!c.Visible) { c.Visible = true; toggled.Add(c); }
        try
        {
            control.BringToFront();
            control.PerformLayout();
            Settle(form, TabSwitchSettleMs, trace);

            var rect = new Rectangle(GetScreenLocation(control), control.Size);
            // 260726Cl 追加: モード依存の groupBox (例 POTENTIAL モードでのみ表示される groupBoxPotentialOption) は、
            // 可視化してもフォームの表示領域からはみ出す位置に置かれることがある。そのまま CopyFromScreen すると
            // はみ出した部分に背後のウィンドウ (FormMain 等) が写り込む。画面上に収まらないときだけ、コントロール
            // 自身に描かせる (DrawToBitmap) 方式へ切り替える。収まるときは従来どおり CopyFromScreen のままにして、
            // 既存クロップ (GL コントロール等 DrawToBitmap が苦手なもの) の見た目を変えない。
            if (!GetWindowVisualBounds(form).Contains(rect))
            {
                try
                {
                    var rendered = new Bitmap(control.Width, control.Height);
                    control.DrawToBitmap(rendered, new Rectangle(Point.Empty, control.Size));
                    trace($"{control.Name}\tINFO\toff-screen control rendered with DrawToBitmap");
                    return rendered;
                }
                catch (Exception ex)
                {
                    trace($"{control.Name}\tWARN\tDrawToBitmap: {ex.GetType().Name}: {ex.Message}"); // 失敗したら従来方式へ
                }
            }
            return CaptureScreen(rect, form, trace, control.Name);
        }
        finally
        {
            for (var i = toggled.Count - 1; i >= 0; i--) // 逆順 (子→親) で元の非表示へ戻す
                toggled[i].Visible = false;
            Application.DoEvents();
        }
    }

    /// <summary>
    /// 260523Cl 追加: コントロールのキャプチャ用パス (= クロップのファイル名 stem) を組み立てる。
    /// form.Name を起点に名前付き祖先の Name を "." で連結する。SplitContainer の SplitterPanel、
    /// ToolStripContainer の ToolStripPanel / ContentPanel、無名コントロールはパスに含めない。
    /// FormCaptureGUI の対話キャプチャと同じ命名規則なので、既存の Wiki 画像 raw URL を壊さない。
    /// </summary>
    protected static string BuildCapturePath(Form form, Control control)
    {
        var segments = new List<string>();
        for (var c = control; c != null && !ReferenceEquals(c, form); c = c.Parent)
        {
            if (string.IsNullOrEmpty(c.Name) || c is SplitterPanel || c is ToolStripPanel || c is ToolStripContentPanel)
                continue; // SplitContainer/ToolStripContainer の入れ物パネルと無名コントロールはパスに出さない
            segments.Add(c.Name);
        }
        segments.Add(form.Name);
        segments.Reverse();
        return string.Join(".", segments);
    }

    /// <summary>260523Cl 追加: ファイル名に使えない文字を '_' へ置換する (FormCaptureGUI と同一規則)。</summary>
    protected internal static string SanitizeFileName(string name)
    {
        foreach (var ch in Path.GetInvalidFileNameChars())
            name = name.Replace(ch, '_');
        return name;
    }

    /// <summary>
    /// 260523Cl 追加: クロップ内側 (上下左右 5px 除外) が一様色なら true。
    /// Visible=false のパネル等で灰色一色になったクロップを検出し、無意味なファイルの保存を防ぐ。
    /// </summary>
    protected internal static bool IsSolidColor(Bitmap bmp)
    {
        const int margin = 5;
        int x0 = margin, y0 = margin, x1 = bmp.Width - margin, y1 = bmp.Height - margin;
        if (x1 <= x0 || y1 <= y0)
            return true; // margin で内側が残らない極小クロップは単色扱い

        var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var row = new int[bmp.Width];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0 + y0 * data.Stride, row, 0, bmp.Width);
            int first = row[x0];
            for (int y = y0; y < y1; y++)
            {
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, bmp.Width);
                for (int x = x0; x < x1; x++)
                    if (row[x] != first)
                        return false;
            }
            return true;
        }
        finally
        {
            bmp.UnlockBits(data);
        }
    }

    private const int StabilizePollMs = 5000;  // 260524Cl: 重い計算フォームを撮る前に、この間隔で画面を撮り比べる

    private const int StabilizeMaxPolls = 72;  // 上限 (5秒 × 72 = 6分)。計算が終わらなくてもこの時点で撮る

    /// <summary>
    /// 260524Cl 追加: 重い計算フォーム用の単純な完了判定。<see cref="StabilizePollMs"/> ごとにウィンドウ全体を撮り、
    /// 直前の撮影と画素が完全一致したら「計算が終わって画面が止まった」とみなして戻る (凝った完了判定はしない)。
    /// 進捗バーや経過時間ラベルが動いている間は一致しないので、それらが止まる = 計算完了で抜ける。
    /// キャレット点滅で一致しないのを避けるため、待機前にアクティブコントロールを外す。上限に達したらそのまま戻る。
    /// </summary>
    protected void WaitUntilScreenStable(Form form, Action<string> trace)
    {
        try { form.ActiveControl = null; } catch { /* キャレット点滅で画面が一致しなくなるのを避ける */ }

        Bitmap previous = null;
        try
        {
            for (var poll = 1; poll <= StabilizeMaxPolls; poll++)
            {
                var until = Environment.TickCount64 + StabilizePollMs; // StabilizePollMs ぶん描画を進める //260718Cl TickCount→TickCount64
                do { Application.DoEvents(); System.Threading.Thread.Sleep(30); } while (Environment.TickCount64 < until);
                RenderGpuControls(form, trace); // GL 結果 (EBSD MasterPattern3D 等) を可視バッファへ反映

                var current = CaptureScreen(GetWindowVisualBounds(form));
                if (current == null)
                    continue; // 一時的に撮れなければ次のポーリングへ

                if (previous != null && BitmapsEqual(previous, current))
                {
                    current.Dispose();
                    trace($"{form.Name}\tINFO\tscreen stable after ~{poll * StabilizePollMs / 1000}s");
                    return;
                }
                previous?.Dispose();
                previous = current;
            }
            trace($"{form.Name}\tINFO\tscreen not stable within {StabilizeMaxPolls * StabilizePollMs / 1000}s; capturing as-is");
        }
        finally
        {
            previous?.Dispose();
        }
    }

    /// <summary>260524Cl 追加: 同サイズの 2 枚のビットマップが画素単位で完全一致するか。WaitUntilScreenStable の変化判定用。</summary>
    private static bool BitmapsEqual(Bitmap a, Bitmap b)
    {
        if (a.Width != b.Width || a.Height != b.Height)
            return false;

        var rect = new Rectangle(0, 0, a.Width, a.Height);
        var da = a.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var db = b.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var bytes = Math.Abs(da.Stride) * a.Height;
            var bufferA = new byte[bytes];
            var bufferB = new byte[bytes];
            System.Runtime.InteropServices.Marshal.Copy(da.Scan0, bufferA, 0, bytes);
            System.Runtime.InteropServices.Marshal.Copy(db.Scan0, bufferB, 0, bytes);
            return bufferA.AsSpan().SequenceEqual(bufferB);
        }
        finally
        {
            a.UnlockBits(da);
            b.UnlockBits(db);
        }
    }

    /// <summary>
    /// (260523Ch) フォーム配下の全コントロールを深さ優先で列挙する。
    /// Capture 対象や GPU 描画コントロールは Panel / SplitContainer / TabPage などの奥に入っているため、Controls 直下だけでは拾えない。
    /// </summary>
    protected static IEnumerable<Control> EnumerateControls(Control root)
    {
        yield return root;

        foreach (Control child in root.Controls)
        {
            foreach (var descendant in EnumerateControls(child))
                yield return descendant;
        }
    }

    #endregion

    #region --diagnose: 多言語化のオーバーフロー/重なり診断

    // 260617Cl 追加: 多言語化方針 Phase 1 のオーバーフロー/重なり診断ツール。
    // 目的: 翻訳で文字列長が変わったときに「ラベル/ボタンが切れる・重なる＝読めなくなる」箇所を、目視でなく機械的に検出する。
    //   各テキスト保持コントロールについて TextRenderer.MeasureText の必要幅と実幅 (AutoSize=False) を比較して切れを、
    //   AutoSize=True は plain panel 内での兄弟 Bounds 交差で重なりを検出し、TSV へ出力する。
    //   ToolStrip/メニューの固定幅項目 (AutoSize=False) も検査する (auto-size 項目は内容に合わせるので対象外)。
    // 疑似ローカライズ: inflate (例 1.4) を与えると「実テキストが N 倍に伸びたら切れるか」を、実翻訳が無くても先出しできる。
    //   AutoSize コントロールは伸びれば自分が成長するので、inflate が効くのは固定幅 (=真の切れリスク) のコントロール。
    // 起動: Program.cs の --diagnose [culture] [inflatePercent] から呼ぶ (--capture と異なり CopyFromScreen を使わず画面外で測る)。


    // 切れ/はみ出しの許容誤差 (MeasureText とレンダラの差・丸め)。これ以下は無視。
    private const int OverflowTolerancePx = 2;

    // Warning と Error の境 (不足ピクセル)。codex 合意の「2px 以内=丸め、3〜5px 超=error」に沿う。
    private const int OverflowErrorPx = 6;

    // 260726Cl: 数値欄が「最長値に対して広すぎる」と見なす余り幅。1 文字ぶん強 (≒10px) を超えたら報告する。
    private const int ValueBoxSlackPx = 10;

    /// <summary>全フォームを画面外に構築してテキストの切れ/重なりを測り、TSV を outFile へ書き出す。</summary>
    public void Diagnose(string outFile, double inflate = 1.0)
    {
        var culture = (ForcedUICulture ?? Thread.CurrentThread.CurrentUICulture).Name;
        var rows = new List<string>
        {
            // Actual/Needed は幅判定では px 幅、Label の折り返し判定では px 高さ (Reason に明記)。
            string.Join("\t", "Culture", "Form", "Control", "Type", "Text", "Font", "Actual", "Needed", "Deficit", "Severity", "Reason")
        };
        void Trace(string s) => Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff}\t{s}");

        // フォーム Load/Show で投げられる例外を握りつぶす (未処理例外のモーダルダイアログでハングしないため)。
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Trace($"ThreadException\t{e.Exception.GetType().Name}: {e.Exception.Message}");
        Trace($"diagnose start (culture={culture}, inflate={inflate:0.00}) -> {outFile}");

        int forms = 0;
        Form main = null;
        foreach (var type in EnumerateFormTypes()) // --capture と同じ列挙 (メインフォームを先頭に)
        {
            Form form = null;
            try
            {
                if (ForcedUICulture != null)
                    Thread.CurrentThread.CurrentUICulture = ForcedUICulture;
                form = (Form)Activator.CreateInstance(type);
                if (type == MainFormType) main = form;
                else WireDependencies(form, main);

                ShowOffScreen(form, Trace);
                if (ReferenceEquals(form, main))
                    PrepareCaptureState(form, Trace); // 代表結晶/代表画像の選択 (依存子フォーム供給に必須)
                Settle(form, 60, Trace);

                DiagnoseForm(form, type.Name, culture, inflate, rows);
                forms++;
            }
            catch (Exception ex) { Trace($"{type.Name}\tFAIL\t{ex.GetType().Name}: {ex.Message}"); }
            finally
            {
                if (!ReferenceEquals(form, main)) { try { form?.Dispose(); } catch { /* 破棄時例外は無視 */ } }
            }
        }

        // reflection 列挙では作れない依存子フォーム。メインフォームが保持する配線済みインスタンスを画面外表示して測る。
        if (main != null)
        {
            foreach (var child in EnumerateDependentForms(main))
            {
                if (child == null || child.IsDisposed) continue;
                try
                {
                    ShowOffScreen(child, Trace);
                    Settle(child, 60, Trace);
                    DiagnoseForm(child, child.GetType().Name, culture, inflate, rows);
                    forms++;
                    try { child.Hide(); } catch { /* メインフォームが所有・破棄するので Hide のみ */ }
                }
                catch (Exception ex) { Trace($"{child.GetType().Name}\tFAIL\t{ex.GetType().Name}: {ex.Message}"); }
            }
        }

        // 260726Cl: Close() は FormClosing → レジストリ書込を発火させ、
        //   (a) 強制カルチャ (--diagnose ru なら "ru") を UI 言語としてレジストリへ焼き付け、
        //   (b) ShowOffScreen が入れた画面外 Bounds (-32000,-32000) を保存し、
        //   (c) 設定ファイル (結晶リスト等) まで上書きしてしまう。
        //   11 言語ぶん回すと最後の言語で普段のアプリが起動するようになる実害があった。
        //   Form.Dispose() は FormClosing/FormClosed を発火しないので、破棄だけ行う
        //   (直後に Program.cs が Environment.Exit(0) するため、通常終了処理は不要)。
        try { main?.Dispose(); } catch { /* 破棄時例外は無視 */ }

        // 260726Cl 追加: 中央訳テーブル (LocalizationData) のうち、どのコントロールにも当てられなかった
        //   エントリを報告する。HeaderText 23 件が「訳は書いてあるのに UI は英語のまま」で 1 か月以上
        //   気づかれなかったのは、CodeLocalizer の失敗が完全に無言だったため。全フォームを構築する
        //   この診断が唯一の網羅的な観測点なので、ここで表に出す。
        //   (CodeLocalizer.Apply は各フォームの OnLoad で走るので、ここに来た時点で全件試行済み)
        var unresolved = CodeLocalizer.UnresolvedEntries;
        if (unresolved.Count > 0)
        {
            Trace($"CodeLocalizer: 未解決エントリ {unresolved.Count} 件 (訳テーブルにあるがコントロールに当たらない)");
            foreach (var u in unresolved)
                Trace($"  未解決\t{u}");
        }

        var full = Path.GetFullPath(outFile);
        Directory.CreateDirectory(Path.GetDirectoryName(full));
        File.WriteAllLines(full, rows);
        int findings = rows.Count - 1;
        int errors = rows.Skip(1).Count(r => r.Contains("\tError\t"));
        Trace($"diagnose done: {forms} forms, {findings} findings ({errors} error), "
            + $"{unresolved.Count} unresolved localization entries -> {full}");
    }

    private static void ShowOffScreen(Form form, Action<string> trace)
    {
        form.StartPosition = FormStartPosition.Manual;
        form.ShowInTaskbar = false;
        form.Location = new Point(-32000, -32000); // 画面外 (診断は CopyFromScreen を使わないので可)
        try { form.Show(); } catch (Exception ex) { trace($"{form.GetType().Name}\tWARN Show\t{ex.GetType().Name}: {ex.Message}"); }
    }

    private static void DiagnoseForm(Form form, string name, string culture, double inflate, List<string> rows)
    {
        // 260618Cl 追加: 診断側で UiFont.Apply を冪等に再適用してフォント sweep を保証する。
        //   FormBase.OnLoad は base.OnLoad(e) の後に UiFont.Apply(this) を呼ぶが、base.OnLoad が
        //   Load ハンドラ内で例外 (GL 無効時の NRE 等。FormStructureViewer/FormEBSD で発生) を投げると
        //   Apply に到達せず、フォントが resx(Segoe UI) のまま測定され CJK overflow を過少報告する。
        //   ここで再適用すれば Load 失敗フォームでも実カルチャのフォントで測れる
        //   (正常 Load 済みは Resolve が同一インスタンスを返すので no-op)。
        try { Crystallography.Controls.UiFont.Apply(form); } catch { /* 部分構築フォームは測れる範囲で測る */ }
        try { form.PerformLayout(); } catch { /* レイアウト例外は無視して測れるものだけ測る */ }
        // EnumerateControls (root を含む) / EnumerateToolStripItems(Form) は --capture 側 (上記、capture と共用) を再利用。
        foreach (var c in EnumerateControls(form))
        {
            // 260726Cl: DiagnoseWidget は ListBox.ItemHeight (LB_GETITEMHEIGHT)・ComboBox.Items の列挙・
            //   GetItemText など投げ得るプロパティに触るようになった。1 コントロールの例外を素通しすると
            //   Diagnose 側の per-form catch まで飛んで、そのフォームの残りが 1 件も測られずに消える。
            try
            {
                DiagnoseControl(c, name, culture, inflate, rows);
                DiagnoseWidget(c, name, culture, inflate, rows); // 260726Cl 追加: 複合/リスト系コントロールの切れ
            }
            catch { /* 測れないコントロールは飛ばし、同じフォームの残りは測る */ }
        }
        foreach (var it in EnumerateToolStripItems(form))
            DiagnoseToolStripItem(it, name, culture, inflate, rows);
    }

    private static void DiagnoseControl(Control c, string form, string culture, double inflate, List<string> rows)
    {
        if (!c.Visible || string.IsNullOrWhiteSpace(c.Text)) return; // 空白のみのラベル (スペーサ) は対象外
        // テキストを表示する代表的なリーフ型のみ。ButtonBase=Button/CheckBox/RadioButton。
        if (c is not (Label or ButtonBase or GroupBox or LinkLabel)) return;
        // 260617Cl: NumericBox 系は自己管理 (数値欄の最低幅を死守し、ヘッダは固定幅+ellipsis+tooltip) のため内部 (labelHeader/textBox) は測らない。
        // 260620Cl: ColorControl も同様に自己管理する複合コントロール (内部 labelHeader/labelFooter/pictureBox)。
        //   HeaderText「Color」等が groupBox 詰まり (伸長 NumericBox 隣接) で内部ラベルとして誤検出され、
        //   deficit が文字長と無相関になる (例 ru「Цвет」最短なのに最大 deficit) ため除外する。
        //   注: ColorControl 内部 footer の長文オーバーフローは culture resx の FooterText 翻訳で個別対処済。
        // 260623Cl: WaveLengthControl も自己管理する複合UC (UC自体 AutoSize=true ＋ 単位行 flowLayoutPanel1 AutoSize=true・WrapContents 既定)。
        //   内部 label2「Unit」/radioButtonUnitAngstrom「Å」/radioButtonUnitNanoMeter「nm」が、訳語伸長時に
        //   AutoSize 連鎖＋フロー折返しを診断が追えず ClippedByParent:FormBeamInteraction(=AutoSize祖先を遡った先のフォーム) と
        //   誤検出される (def が文字長と無相関・nm「nm」最短なのに def114)。単位「Å/nm」は短縮不可で実体は自己解決するため除外。
        // for (var a = c.Parent; a != null; a = a.Parent)
        //     if (a.GetType().Name.Contains("NumericBox")) return;  // 260617Cl 旧
        //     if (a.GetType().Name.Contains("NumericBox") || a.GetType().Name.Contains("ColorControl") || a.GetType().Name.Contains("WaveLengthControl")) return;  // 260620Cl / 260623Cl WaveLengthControl 追加
        if (IsSelfManagedComposite(c)) return;  // 260726Cl: 同じ祖先走査が DiagnoseWidget 側と二重化していたので集約

        // 260617Cl: 擬似ローカライズ (inflate>1) の伸長予測は「翻訳される語」にのみ意味がある。記号/単位/短いインデックス
        //   (° ± ∓ % θ mm kV l1 等) は翻訳されず伸びないので擬似モードでは予測対象外 (実カルチャ inflate=1.0 は実テキストを測るので素通し)。
        if (inflate > 1.0 && !IsLikelyTranslatable(c.Text)) return;

        // int glyph = c is CheckBox or RadioButton ? 18 : c is ButtonBase ? 12 : c is GroupBox ? 8 : 4; // 260726Cl 旧: GroupBox/既定アームは下記のとおり到達不能になった

        // 260726Cl 追加: GroupBox のタイトルは AutoSize の有無に関わらず幅で切れる。
        //   WinForms の GroupBox.GetPreferredSize は子のレイアウトしか見ずキャプション幅を考慮しないため、
        //   AutoSize=true でもタイトルは伸びない。さらに Dock=Top だと幅は親に固定される。
        //   実例: ru の FormMain groupBoxCurrentDirection (AutoSize=true・Dock=Top・幅142) は
        //   「Текущая ориентация」の末尾「я」が切れているのに、従来の AutoSize 分岐では素通りしていた。
        if (c is GroupBox)
        {
            // GroupBoxRenderer はキャプションを左 13px・右 8px 内側の矩形へ WordBreak 付き (パディング込み) で描くので、
            // 実効幅は Width−21。入り切らない語は 2 行目へ回って本文領域に重なる/消えるため、1px 足りないだけでも
            // 視覚的な損失は大きい。よって他の判定より厳しく「0 超」で報告する。
            // 実測較正: de「Aktuelle Orientierung」(幅142・2語目が丸ごと消える) と
            //   ja「回折波の数」(幅74・2 行目「数」が本文に重なる) の両方をこの式が拾う。
            int neededTitle = Needed(c.Text, c.Font, inflate, 21);
            int titleDeficit = neededTitle - c.Width;
            if (titleDeficit > 0)
                rows.Add(Row(culture, form, c.Name, c.GetType().Name, c.Text, c.Font, c.Width, neededTitle, titleDeficit,
                    Sev(titleDeficit), "TextClipped"));
            // 260726Cl: 固定幅 GroupBox はキャプション規則 (pad=21) が旧 TextClipped (glyph=8) の上位互換なのでここで終了。
            //   AutoSize GroupBox は下の AutoSize 分岐 (WouldCollide/ClippedByParent) へ落とす。無条件 return にすると
            //   本改修前まで効いていたこの 2 判定が AutoSize GroupBox から丸ごと消えてしまう。
            if (!c.AutoSize) return;
        }

        if (c.AutoSize)
        {
            // 260726Cl 追加: Dock=Top/Bottom/Fill・MaximumSize・TableLayoutPanel の列幅などで幅を押さえられている
            //   AutoSize コントロールは、文字ぶんに伸びられないので実際には切れる。WinForms 自身が返す
            //   PreferredSize (この文字を出すのに要る大きさ) と実幅を比べれば、押さえ込みの原因に依らず検出できる。
            //   PreferredSize は ButtonBase 系でキャッシュされず毎回テキストを測り直すのでローカルへ 1 回だけ取る。
            int preferred = c.PreferredSize.Width, shortfall = preferred - c.Width;
            if (shortfall > OverflowTolerancePx)
            {
                rows.Add(Row(culture, form, c.Name, c.GetType().Name, c.Text, c.Font, c.Width, preferred, shortfall,
                    Sev(shortfall), $"AutoSizeConstrained(Dock={c.Dock})"));
                return;
            }

            // AutoSize は文字に合わせて伸びるので自テキストには「切れ」ない。代わりに 2 つを見る:
            //   (1) WouldCollide: 直親が固定(再配置/成長しない)なら、伸びた分だけ右隣兄弟へ食い込むか予測。
            //   (2) ClippedByParent: 祖先を遡り、最初の固定祖先のクライアント右端で切れるか
            //       (AutoSize/Flow の祖先は子に合わせ成長/再配置するので、その祖先の右端を上位へ持ち上げて評価)。
            var p = c.Parent;
            if (p == null) return;
            // 260617Cl: 「翻訳で伸びた分 (inflation 増分) だけ右へ食い込むか」を現状幅 (c.Right) 基準で予測する。
            //   c.Right + 増分なら inflate=1.0 で増分0 → deficit≤tol となり baseline はクリーン。
            int growth = (int)Math.Ceiling(TextRenderer.MeasureText(c.Text, c.Font).Width * (inflate - 1.0));
            int grownRight = c.Right + growth;

            // (1) 右隣兄弟との衝突は、直親が再配置/成長しない場合のみ予測する (Flow/Table/AutoSize 親は吸収する)。
            if (p is not FlowLayoutPanel and not TableLayoutPanel && !p.AutoSize)
            {
                Control nearest = null;
                foreach (Control s in p.Controls)
                {
                    if (ReferenceEquals(s, c) || !s.Visible || s.Width == 0) continue;
                    // 260726Cl 検討メモ: 「c が既に伸びて s に食い込んでいる」ケースを拾おうと条件を
                    //   `s.Left <= c.Left` へ緩めたが、意図的に重ねてある兄弟 (排他表示のボタン対、複数列に
                    //   またがるヘッダラベル「h k l」など) を大量に誤検出したため元へ戻した。
                    //   既に重なっている組は ChildOverflowsParent / ClippedByParent 側で拾う方針。
                    if (s.Left < c.Right - OverflowTolerancePx) continue;  // 右隣のみ (左/既に重なるものは除外)
                    if (s.Bottom <= c.Top || s.Top >= c.Bottom) continue;  // 垂直に重ならない = 別の行
                    if (nearest == null || s.Left < nearest.Left) nearest = s;
                }
                if (nearest != null)
                {
                    int deficit = grownRight - nearest.Left;
                    if (deficit > OverflowTolerancePx)
                    {
                        rows.Add(Row(culture, form, c.Name, c.GetType().Name, c.Text, c.Font, c.Width, c.Width + growth, deficit,
                            Sev(deficit), $"WouldCollide:{nearest.Name}"));
                        return;
                    }
                }
            }

            // (2) 260618Cl 追加: 祖先のクライアント右端で切れるか。groupBox 内で唯一/最右の AutoSize コントロール
            //   や、AutoSize FlowLayoutPanel が固定 groupBox を食み出す例 (ja の「等角投影 (Wulff)」ラジオ) を拾う。
            //   従来は Flow/Table/AutoSize 親を丸ごと早期 return しており、これらの入れ子クリップを見逃していた。
            var (clipDeficit, clipper) = AncestorRightClip(c, grownRight);
            if (clipDeficit > OverflowTolerancePx)
                rows.Add(Row(culture, form, c.Name, c.GetType().Name, c.Text, c.Font, c.Width, c.Width + growth, clipDeficit,
                    Sev(clipDeficit), $"ClippedByParent:{clipper}"));
        }
        else if (c is Label or LinkLabel)
        {
            // 固定サイズの Label は幅内で折り返す。幅でなく「折り返した行数 × 行高 がラベル高さを超えるか」で切れを見る。
            // inflate 倍に伸びたテキストが何行になるかを 1 行幅から見積もる (baseline で 1 行に収まるラベルは出ない)。
            // 260617Cl: 折り返しには改行機会 (空白/CJK文字間) が要る。空白の無い単一トークン (記号/単位/変数名:
            //   ° ± ∓ % mm kV l1 θ 等) は幅が足りなくても折り返せず (クリップするだけ) 2 行にならない。
            //   これらは翻訳もされないので、WrapsBeyondHeight の誤検出 (幅 < 自テキスト幅のラベル) を防ぐ。
            if (!HasWrapOpportunity(c.Text)) return;
            ReportWrap(TextRenderer.MeasureText(c.Text, c.Font), 0);
        }
        else
        {
            // ここへ来るのは非 AutoSize の ButtonBase のみ (Label/LinkLabel は上の分岐、GroupBox は更に上で return)。
            int glyph = c is CheckBox or RadioButton ? 18 : 12; // 260726Cl: グリフ/枠の余白概算 (旧 glyph 式の GroupBox/既定アームは到達不能だった)
            var one = TextRenderer.MeasureText(c.Text, c.Font);
            // 260726Cl 追加: Button/CheckBox/RadioButton は高さに 2 行以上入るなら WinForms が幅で折り返して描く。
            //   従来は一律「1 行が幅に収まるか」で測っていたため、2 行前提でデザインされたコントロールが
            //   全言語で偽陽性になっていた (FormPolycrystallineDiffractionSimulator の radioButtonZigzagScan は
            //   205x52 = 2 行ぶんの高さがあり、en では実際には折り返して収まっている)。
            if (c.Height >= one.Height * 2 && HasWrapOpportunity(c.Text))
            {
                ReportWrap(one, glyph);
                return;
            }

            // 固定サイズの Button/CheckBox/RadioButton: 1 行テキストが幅に収まるか。
            int neededW = (int)Math.Ceiling(one.Width * inflate) + glyph;
            int deficit = neededW - c.Width;
            if (deficit <= OverflowTolerancePx) return;
            rows.Add(Row(culture, form, c.Name, c.GetType().Name, c.Text, c.Font, c.Width, neededW, deficit,
                Sev(deficit), "TextClipped"));
        }

        // 260726Cl 追加: 「幅で折り返した結果、高さが足りるか」の共通判定 (Label は glyph=0、ButtonBase はグリフ幅ぶん狭い)。
        //   Label 側と ButtonBase 側に同じ 9 行が二重化しており、折返し式の再較正が 2 箇所必要になっていたので集約。
        void ReportWrap(Size one, int glyph)
        {
            int availW = Math.Max(1, c.Width - glyph);
            int lines = Math.Max(1, (int)Math.Ceiling(one.Width * inflate / availW));
            int neededH = lines * one.Height;
            int deficit = neededH - c.Height;
            if (deficit <= OverflowTolerancePx) return;
            rows.Add(Row(culture, form, c.Name, c.GetType().Name, c.Text, c.Font, c.Height, neededH, deficit,
                Sev(deficit), $"WrapsBeyondHeight({lines}lines)"));
        }
    }

    private static void DiagnoseToolStripItem(ToolStripItem it, string form, string culture, double inflate, List<string> rows)
    {
        if (!it.Visible || string.IsNullOrWhiteSpace(it.Text) || it.Width <= 0) return;
        if (it.AutoSize) return; // auto-size 項目は内容に合わせるので切れない。固定幅 (status label 等) のみ対象。
        if (it.DisplayStyle is ToolStripItemDisplayStyle.Image or ToolStripItemDisplayStyle.None) return; // テキスト非表示

        int imageW = it.Image != null && it.DisplayStyle == ToolStripItemDisplayStyle.ImageAndText ? it.Image.Width + 4 : 0;
        int neededW = (int)Math.Ceiling(TextRenderer.MeasureText(it.Text, it.Font).Width * inflate) + imageW + 12;
        int deficit = neededW - it.Width;
        if (deficit <= OverflowTolerancePx) return;
        rows.Add(Row(culture, form, it.Name, it.GetType().Name, it.Text, it.Font, it.Width, neededW, deficit,
            Sev(deficit), "ToolStripTextClipped"));
    }

    // 260618Cl 追加: c の右端が、いずれかの祖先のクライアント右端で切れるか (＝親にクリップされるか) を遡って判定。
    //   AutoSize/AutoSize-FlowLayoutPanel の祖先は子に合わせて成長/再配置するので切らず、その祖先自身の右端
    //   (予測はみ出し分を足して) を上位へ持ち上げ、最初の「固定 (AutoSize でない)」祖先で確定する。
    //   AutoScroll 祖先はスクロール可なのでクリップなし。grownRight は c.Parent のクライアント座標での予測右端。
    private static (int deficit, string clipper) AncestorRightClip(Control c, int grownRight)
    {
        int right = grownRight;
        for (var p = c.Parent; p != null; p = p.Parent)
        {
            if (p is ScrollableControl { AutoScroll: true }) return (0, "");
            int deficit = right - p.ClientSize.Width;
            if (!p.AutoSize)
                return deficit > OverflowTolerancePx ? (deficit, p.Name) : (0, "");
            // p は AutoSize で c を吸収 (c は p に収まる)。c "自身" の右端を p の親座標へ変換 (p.Left を足す) して
            // 継続する。コンテナの右端 (p.Right) でなく c の右端を追うことで、行の中央にある通過コントロール
            // (例: 「×」「px」) を誤検出せず、実際に祖先右端を越える最右コントロールだけを拾う。
            right += p.Left;
        }
        return (0, "");
    }

    // 260617Cl 追加: テキストが (幅不足時に) 複数行へ折り返せる改行機会を持つか。
    //   空白で折り返し可。CJK/かなは文字間で折り返せるので 2 文字以上あれば可。それ以外の単一トークン
    //   (° ± ∓ % mm kV l1 θ 等の記号/単位/変数名) は折り返せない (クリップするだけ) → WrapsBeyondHeight 対象外。
    private static bool HasWrapOpportunity(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        int cjk = 0;
        foreach (char ch in text)
        {
            if (char.IsWhiteSpace(ch)) return true;
            if (ch >= 0x3040) cjk++; // ひらがな以降 (かな/CJK 漢字/ハングル等) は文字間で折り返し可
        }
        return cjk >= 2;
    }

    // 260617Cl 追加: テキストが翻訳されうる語を含むか (擬似ローカライズの伸長予測の前提)。
    //   連続するアルファベット 3 文字以上、または CJK/かな文字を含めば「語」とみなす。
    //   記号(° ± ∓ % θ)/単位(mm kV Å)/短いインデックス(l1 l2 X:)は false = 翻訳されず擬似伸長は無意味。
    private static bool IsLikelyTranslatable(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        int run = 0;
        foreach (char ch in text)
        {
            if (ch >= 0x3040) return true; // かな/CJK 漢字/ハングル等は短くても語
            if (char.IsLetter(ch)) { if (++run >= 3) return true; }
            else run = 0;
        }
        return false;
    }

    // ────────────────────────────────────────────────────────────────────────────────────────
    // 260726Cl 追加 (作者要望「全コントロール・全言語で文字溢れ/切れを確認」): DiagnoseControl が対象に
    // していない複合・リスト系コントロールの切れ検査。DiagnoseControl は Label/ButtonBase/GroupBox/LinkLabel
    // しか見ないため、次の種別は 11 言語分の翻訳が伸びても検出できていなかった。
    //   ① DataGridView : 列見出しが列幅に収まるか (固定幅列は "…" で切れる。AutoSize 列は自然に deficit≤0)
    //   ② TabControl   : タブ見出しの合計幅がクライアント幅を超えるか (超えると矢印スクロール/行増で見出しが隠れる)
    //   ③ ComboBox / ListBox / CheckedListBox : 項目テキストがリスト幅に収まるか
    //   ④ ListView (Details) : 列見出しが列幅に収まるか
    //   ⑤ ToolStrip    : 項目がオーバーフロー (») へ押し出されていないか
    //   ⑥ AutoEllipsis : NumericBox/ColorControl/WaveLengthControl の固定幅ヘッダ/フッタが "…" で切れていないか
    // ⑥ は DiagnoseControl が「自己管理コントロール」として意図的に除外している領域だが、HeaderWidth を
    // 固定した箇所は実際に ellipsis で読めなくなる。折り返し (WrapsBeyondHeight) ではなく幅のみの規則で測る。
    private static void DiagnoseWidget(Control c, string form, string culture, double inflate, List<string> rows)
    {
        if (!c.Visible || c.Width <= 0 || c.Height <= 0) return;

        DiagnoseContainer(c, form, culture, rows); // 260726Cl: 折り返し/親はみ出し (テキスト種別に依らない構造的な溢れ)

        switch (c)
        {
            case DataGridView dgv when dgv.ColumnHeadersVisible:
                // 見出しが折り返す設定 (WrapMode=True かつ見出し高さ AutoSize) なら高さが伸びて切れないので対象外。
                if (dgv.ColumnHeadersDefaultCellStyle.WrapMode == DataGridViewTriState.True
                    && dgv.ColumnHeadersHeightSizeMode == DataGridViewColumnHeadersHeightSizeMode.AutoSize)
                    break;
                // 260726Cl: DataGridViewCell.Style の getter は未設定だと空 Style を「生成して」返す (診断が対象を書き換える)
                //   ので HasStyle で守る。フォールバックは列に依らないのでループ外へ。
                var headerFont = dgv.ColumnHeadersDefaultCellStyle.Font ?? dgv.Font;
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (!col.Visible || string.IsNullOrWhiteSpace(col.HeaderText)) continue;
                    var f = (col.HeaderCell is { HasStyle: true } hc ? hc.Style.Font : null) ?? headerFont;
                    // 見出しセルの内側余白 (左右パディング 2px×2 + 罫線) ぶんを足して必要幅とする。
                    Report(rows, culture, form, $"{c.Name}.{col.Name}", "DataGridViewColumn", col.HeaderText, f,
                        col.Width, Needed(col.HeaderText, f, inflate, 6), "GridHeaderClipped", inflate);
                }
                break;

            case TabControl { TabCount: > 0 } tc when tc.Alignment is TabAlignment.Top or TabAlignment.Bottom:
                // 個々のタブ見出し (SizeMode=Fixed だと文字がタブ幅を超える)。Normal は文字に合わせるので deficit≤0。
                for (int i = 0; i < tc.TabCount; i++)
                {
                    var page = tc.TabPages[i];
                    if (string.IsNullOrWhiteSpace(page.Text) || SafeTabRect(tc, i) is not { } rect) continue;
                    int imageW = page.ImageIndex >= 0 || !string.IsNullOrEmpty(page.ImageKey) ? 20 : 0;
                    // 260726Cl: タブ矩形は SizeMode=Normal では文字幅ぴったりに作られるので余白は足さない
                    // (足すと全タブが一律 +6px の偽陽性になる。実測で en の全タブが deficit=6 になった)。
                    Report(rows, culture, form, $"{c.Name}.{page.Name}", "TabPage", page.Text, tc.Font,
                        rect.Width, Needed(page.Text, tc.Font, inflate, imageW), "TabTextClipped", inflate);
                }
                // タブ列全体がコントロール幅を超えると、Multiline=false では左右の矢印が出て右側タブが隠れる。
                if (!tc.Multiline)
                {
                    int right = 0;
                    for (int i = 0; i < tc.TabCount; i++)
                        if (SafeTabRect(tc, i) is { } r) right = Math.Max(right, r.Right);
                    Report(rows, culture, form, c.Name, "TabControl", TabTexts(tc), tc.Font,
                        tc.ClientSize.Width, right, "TabHeadersOverflow", inflate);
                }
                else if (tc.RowCount > 1)
                {
                    // Multiline は行を増やして全タブを見せるので文字は切れないが、ページ領域が縮む (レイアウト崩れ要因)。
                    rows.Add(Row(culture, form, c.Name, "TabControl", TabTexts(tc), tc.Font,
                        1, tc.RowCount, tc.RowCount - 1, "Warning", $"TabHeaderRows({tc.RowCount})"));
                }
                break;

            case ComboBox cb:
                // 一覧 (DropDownWidth。既定は ComboBox 幅と同じ) に項目が収まるか。項目数が MaxDropDownItems を
                // 超えるときは縦スクロールバーぶん狭くなる。Windows はドロップダウンを自動で広げないので、
                // ここで不足すると一覧でも閉じた表示部でも文字が切れる。
                // 260726Cl: ComboBox は描画余白が分かっているので、MeasureText の既定パディング (≒5px) を含まない
                //   生の文字幅 (NoPadding) で測る。実測キャプチャ 2 点で較正済み:
                //     ・ScalablePictureBoxAdvanced の comboBoxGradient (幅68) の "Positive " は全部見える
                //     ・FormImageSimulator の comboBoxScaleColorScale (幅72) の "Gray scale" は "Gray scal" と切れる
                //   → 閉じた表示部の実効幅は「Width − ドロップダウンボタン(17) − 内側余白(4)」で両者と整合する。
                int listAvail = cb.DropDownWidth
                    - (cb.Items.Count > cb.MaxDropDownItems ? SystemInformation.VerticalScrollBarWidth : 0) - 6;
                // 260726Cl: ドロップダウンボタンの幅は Win32 で SM_CXVSCROLL (= VerticalScrollBarWidth)。
                //   旧コードは HorizontalScrollBarArrowWidth (SM_CXHSCROLL) を使っており、既定テーマ 96dpi で
                //   偶然どちらも 17px だったため 2 点の実測較正が通っていただけ。スクロールバー幅を変えた環境で
                //   全 ComboBox の判定が一律ずれるので、上の一覧側と同じメトリックへ揃える。
                int closedAvail = cb.Width - SystemInformation.VerticalScrollBarWidth - 4;
                var cbPath = ParentPath(cb); // 260726Cl: ループ不変なので巻き上げ
                foreach (var item in cb.Items)
                {
                    var s = ItemText(cb, item);
                    int need = NeededRaw(s, cb.Font, inflate);
                    Report(rows, culture, form, cbPath, "ComboBoxItem", s, cb.Font, listAvail, need, "ListItemClipped", inflate);
                    Report(rows, culture, form, cbPath, "ComboBoxItem", s, cb.Font, closedAvail, need, "ComboBoxTextClipped", inflate);
                }
                break;

            // 260726Cl: CheckedListBox は ListBox 派生で、判定はチェックボックスのグリフ幅 (18px) 分しか違わないので
            //   ReportListItems へ集約 (旧: 有効幅の式と ItemText 呼び出しが両アームに二重化し、しかもループ不変式を
            //   項目ごとに再評価していた)。CheckedListBox 側に HorizontalScrollbar のガードが無い非対称は、
            //   検出結果 (baseline) を変えないため現状のまま残す。
            case CheckedListBox clb:
                ReportListItems(clb, 18, "CheckedListBoxItem");
                break;

            // 260726Cl: MultiColumn も除外する。多段組では実効幅が ColumnWidth になり、スクロールバーも水平なので
            //   ReportListItems の「ClientSize.Width − 縦スクロールバー」モデルが成立しない (FormMain の結晶リストが該当)。
            //   そもそも中身はユーザーデータ (結晶名) で翻訳対象ではなく、測っても baseline が環境依存になる。
            case ListBox { HorizontalScrollbar: false, MultiColumn: false } lb: // 水平スクロールバー有りなら読めるので対象外
                ReportListItems(lb, 0, "ListBoxItem");
                break;

            case ListView { View: View.Details } lv:
                foreach (ColumnHeader col in lv.Columns)
                    Report(rows, culture, form, $"{c.Name}.{col.Name}", "ColumnHeader", col.Text, lv.Font,
                        col.Width, Needed(col.Text, lv.Font, inflate, 8), "ListViewHeaderClipped", inflate);
                break;

            case ToolStrip { IsDropDown: false } ts:
                foreach (ToolStripItem it in ts.Items)
                    if (it.Visible && it.Placement == ToolStripItemPlacement.Overflow && !string.IsNullOrWhiteSpace(it.Text))
                        rows.Add(Row(culture, form, it.Name, it.GetType().Name, it.Text, it.Font,
                            ts.ClientSize.Width, ts.ClientSize.Width + it.Width, it.Width, "Error", $"PushedToOverflow:{ts.Name}"));
                break;

            // 260726Cl 追加: NumericBox の数値欄 (ValueBoxWidth で固定した TextBox) に、取り得る最長の値が収まるか。
            //   DiagnoseControl は NumericBox 内部を「自己管理」として除外しているが、隣のヘッダへ幅を回すために
            //   ValueBoxWidth を手で詰めた箇所は実際に数字が切れる。デザイナ上は既定値 (例 400) しか出ないので
            //   気付けず、Maximum を入れて初めて分かる ＝ 目視レビューでは絶対に落ちる種類の不具合。
            //   翻訳とは無関係 (数字は全言語共通) だが、フォント差で ja/zh の方が広くなるため全カルチャで測る意味がある。
            case NumericBox nb when nb.ValueBoxWidth >= 0:
                var box = EnumerateControls(nb).OfType<TextBox>().FirstOrDefault();
                if (box == null || box.ClientSize.Width <= 0) break;
                // NumericBox.setText と同じ書式 (FormatSpecifier 優先、無ければ DecimalPlaces、それも無ければ general)。
                var fmt = !string.IsNullOrEmpty(nb.FormatSpecifier) ? nb.FormatSpecifier
                        : nb.DecimalPlaces >= 0 ? $"f{nb.DecimalPlaces}" : "";
                string widest = "";
                foreach (var v in new[] { nb.Maximum, nb.Minimum, nb.Value })
                {
                    string s;
                    try { s = v.ToString(fmt, System.Globalization.CultureInfo.CurrentCulture); }
                    catch (FormatException) { s = v.ToString(System.Globalization.CultureInfo.CurrentCulture); }
                    if (NeededRaw(s, box.Font, 1.0) > NeededRaw(widest, box.Font, 1.0)) widest = s;
                }
                // 数字は翻訳されないので inflate は掛けない (擬似ローカライズでも実寸で測る)。
                // TextBox の内側余白は左右 1px 程度 + キャレット 1px を見込む。
                int needValue = NeededRaw(widest, box.Font, 1.0) + 3;
                //   桁数が有界 = 小数部が固定 (DecimalPlaces>=0 か FormatSpecifier 指定) かつ
                //   Maximum/Minimum が両方とも有限であること。既定は ±∞ なので、範囲を設定していない
                //   コントロール (計算結果を表示するだけの欄やグラフ軸) は「今表示している値」しか根拠が無く、
                //   別の結晶・別の軸範囲になれば桁が伸びる。Reason に出して、幅の増減を判断できるようにする。
                bool bounded = (nb.DecimalPlaces >= 0 || !string.IsNullOrEmpty(nb.FormatSpecifier))
                    && double.IsFinite(nb.Maximum) && double.IsFinite(nb.Minimum);
                Report(rows, culture, form, ParentPath(nb), "NumericBox(value)", widest, box.Font,
                    box.ClientSize.Width, needValue, bounded ? "ValueBoxClipped(bounded)" : "ValueBoxClipped(open)");

                // 260726Cl 追加: 逆に「取り得る最長値に対して数値欄が広すぎる」ケース。隣のヘッダに回せる幅が
                //   死んでいる (訳語が伸びる言語ほど効く) ので余りも報告する。縮小提案は bounded のときだけ:
                //   開いた範囲を「今たまたま短い値」を根拠に縮めると、後で計算値が入ったときに切れる。
                //   Error/Warning ではなく Info なので既存のトリアージ (Error 集計) には混ざらない。
                int slack = box.ClientSize.Width - needValue;
                if (bounded && slack > ValueBoxSlackPx)
                    rows.Add(Row(culture, form, ParentPath(nb), "NumericBox(value)", widest, box.Font,
                        box.ClientSize.Width, needValue, slack, "Info", $"ValueBoxOversized(w={nb.ValueBoxWidth})"));
                break;

            case Label { AutoSize: false, AutoEllipsis: true } lbl when IsSelfManagedComposite(lbl):
                Report(rows, culture, form, ParentPath(lbl), "Label(AutoEllipsis)", lbl.Text, lbl.Font,
                    lbl.Width - lbl.Padding.Horizontal, Needed(lbl.Text, lbl.Font, inflate, 0), "EllipsisClipped", inflate);
                break;
        }

        // 260726Cl 追加: ListBox / CheckedListBox 共通の項目幅判定。有効幅 (スクロールバーの有無を含む) は
        //   ループ不変なので 1 回だけ求める。glyph は CheckedListBox のチェックボックス幅。
        void ReportListItems(ListBox list, int glyph, string type)
        {
            bool scroll = list.Items.Count > list.ClientSize.Height / Math.Max(list.ItemHeight, 1);
            int avail = list.ClientSize.Width - glyph - (scroll ? SystemInformation.VerticalScrollBarWidth : 0);
            foreach (var item in list.Items)
            {
                var s = ItemText(list, item);
                Report(rows, culture, form, list.Name, type, s, list.Font, avail, NeededRaw(s, list.Font, inflate),
                    "ListItemClipped", inflate);
            }
        }
    }

    /// <summary>
    /// 260726Cl 追加: テキスト種別に依らない「構造的な溢れ」を 2 つ検査する。
    ///  (a) FlowWrapped     : FlowLayoutPanel が意図せず折り返しているか。特に FlowDirection=TopDown で
    ///      列が 2 本になるのは、訳語が伸びて高さが足りなくなったときの典型 (WrapContents 既定 true)。
    ///      FormEBSD の吸収オプション行が独/仏/伊/西/葡/露で 2 列目へ回り込み、groupBox 右外へ出て
    ///      「非局所吸収モデル」「TDS 背景」チェックボックスが画面から消えていた実例がこれ。
    ///  (b) ChildOverflowsParent : 子の右端/下端が親のクライアント領域を越えているか。Dock 追従・AutoScroll・
    ///      AutoSize 親は自分で吸収するので対象外。Label/Button 以外 (NumericBox 行や入れ子パネル) の溢れを拾う。
    /// </summary>
    private static void DiagnoseContainer(Control c, string form, string culture, List<string> rows)
    {
        // (a) FlowLayoutPanel の折り返し。
        //   Controls コレクションの順＝フローの並び順なので、「次の子が前の子より手前へ戻ったら折り返し」で数える。
        //   兄弟の Margin/高さ違いで Top がずれるだけの並びを行数と誤認しないため、Distinct ではなくこの順序判定を使う。
        if (c is FlowLayoutPanel { WrapContents: true } flow) // WrapContents=false は折り返さないので対象外
        {
            var kids = flow.Controls.Cast<Control>().Where(k => k.Visible).ToList();
            bool topDown = flow.FlowDirection is FlowDirection.TopDown or FlowDirection.BottomUp;
            // 260726Cl: 逆向きフロー (RightToLeft/BottomUp) は主軸座標が単調に「戻る」ので、素朴な
            //   「前の子より手前へ戻ったら折り返し」判定だと子の数ぶん行数を数えてしまう。
            //   実害: FormMain.flowLayoutPanelCrystalOrder (RightToLeft・子5) が全言語で
            //   FlowWrapped(rows=5) の偽検出になっていた。向きに応じて不等号を反転する。
            bool reversed = flow.FlowDirection is FlowDirection.RightToLeft or FlowDirection.BottomUp;
            int lines = 1; // 先頭の子で 1 行目。子が 0 でも下の lines > 1 が偽になるので特別扱いは要らない
            for (int i = 1; i < kids.Count; i++)
            {
                int cur = topDown ? kids[i].Top : kids[i].Left, prev = topDown ? kids[i - 1].Top : kids[i - 1].Left;
                if (reversed ? cur >= prev : cur <= prev)
                    lines++;
            }
            if (lines > 1)
                rows.Add(Row(culture, form, flow.Name, "FlowLayoutPanel",
                    string.Join(" / ", kids.Select(k => k.Name)), flow.Font, 1, lines, lines - 1,
                    topDown ? "Error" : "Warning",   // TopDown の複数列はほぼ常に不具合、LeftToRight の複数行は意図的なこともある
                    $"FlowWrapped({(topDown ? "cols" : "rows")}={lines},{flow.FlowDirection})"));
        }

        // (b) 子の親はみ出し
        if (c.AutoSize || c is ScrollableControl { AutoScroll: true } || c is TabControl or SplitContainer) return;

        // (b-1) 260726Cl 追加: Dock=Left/Right (または Top/Bottom) で並べた子の合計が親のクライアント領域を超えると、
        //   最後に置いた子が丸ごと画面から消える (Dock 並びは折り返さない)。実例: ru の FormImageSimulator
        //   groupBoxDisplay で「Масштабная линейка」まで出て「Длина」の数値欄と最後の「Цвет」が消えていた。
        //   Dock 付きの子は (b-2) の個別判定 (Dock==None のみ) では拾えないので、合計幅/高さで見る。
        // 260726Cl: TableLayoutPanel / FlowLayoutPanel は Dock を主軸配置に使わない (セル/フローが配置を決める)
        //   ので、Dock 付きの子を積み上げて合計する意味が無い。TLP では別セルの子まで合算して誤検出になる。
        if (c is not (TableLayoutPanel or FlowLayoutPanel))
        {
            int dockW = 0, dockH = 0;
            foreach (Control ch in c.Controls)
            {
                if (!ch.Visible) continue;
                // 260726Cl: Margin を足していたが、WinForms の Dock レイアウト (DefaultLayout) は Margin を
                //   消費しない (Margin は Flow/Table 専用)。子の数に比例した過大評価になっていた。
                //   実例: FormImageSimulator.panelSerialThickness は NumericBox 3 個 × Margin 4px = 12px 過大で
                //   「11px はみ出し」と出ていた (＝作者コメントの「en で 11px 出るが実表示は正常」の正体)。
                //   その偽陽性を打ち消すために DockOverflowTolerancePx=20 という粗い閾値が要り、結果として
                //   7〜19px の実オーバーフローを取りこぼしていたので、Margin を外して閾値も他判定と揃える。
                if (ch.Dock is DockStyle.Left or DockStyle.Right) dockW += ch.Width;
                else if (ch.Dock is DockStyle.Top or DockStyle.Bottom) dockH += ch.Height;
            }
            // Dock レイアウトの原資は ClientSize ではなく DisplayRectangle (= クライアント領域 − Padding)。
            // 旧: 2 要素のタプル配列を毎回確保して回していたが、sum==0 のガードは deficit≤閾値 に必ず吸収されるので
            // 到達不能だった。素直な if 2 本にする。
            var avail = c.DisplayRectangle.Size;
            if (dockW - avail.Width > OverflowErrorPx)
                rows.Add(Row(culture, form, c.Name, c.GetType().Name, c.Text, c.Font,
                    avail.Width, dockW, dockW - avail.Width, "Error", "DockRowOverflow(X)"));
            if (dockH - avail.Height > OverflowErrorPx)
                rows.Add(Row(culture, form, c.Name, c.GetType().Name, c.Text, c.Font,
                    avail.Height, dockH, dockH - avail.Height, "Error", "DockRowOverflow(Y)"));
        }

        foreach (Control ch in c.Controls)
        {
            if (!ch.Visible || ch.Dock != DockStyle.None || ch.Width <= 0 || ch.Height <= 0) continue;
            int dx = ch.Right - c.ClientSize.Width, dy = ch.Bottom - c.ClientSize.Height;
            // 260726Cl: 構造的なはみ出しは丸め誤差が大きい (spin ボタンや NumericBox が 3〜5px 下へ出るのは設計どおり)
            // ため、テキスト判定より緩い OverflowErrorPx を閾値にして Error 相当だけを拾う。
            // 縦方向は FlowLayoutPanel (折り返しで下へ押し出される) のときだけ見る。自作 UserControl は
            // 実行時に自分でリサイズするため、画面外構築の時点では子が下へ大きく出ていて偽陽性になる
            // (FormPolycrystallineDiffractionSimulator の diffractionPatternControl で 480〜513px の誤検出)。
            if (c is not FlowLayoutPanel) dy = int.MinValue;
            bool overX = dx > OverflowErrorPx, overY = dy > OverflowErrorPx;
            if (!overX && !overY) continue;
            // 260726Cl: ここへ来た時点で Math.Max(dx, dy) > OverflowErrorPx が確定するので severity は常に Error
            //   (旧コードの三項は Warning 側が到達不能だった)。軸判定も文字列比較でなく overX で持つ。
            // 260726Cl: Deficit は Actual/Needed と同じ軸で取る (旧 Math.Max(dx,dy) だと XY 同時はみ出しかつ
            //   dy > dx のとき Needed − Actual ≠ Deficit になり、TSV を機械集計したとき値が食い違う)。
            rows.Add(Row(culture, form, ch.Name, ch.GetType().Name, ch.Text, ch.Font,
                overX ? c.ClientSize.Width : c.ClientSize.Height,
                overX ? ch.Right : ch.Bottom, overX ? dx : dy,
                "Error", $"ChildOverflowsParent:{c.Name}({(overX && overY ? "XY" : overX ? "X" : "Y")})"));
        }
    }

    /// <summary>260726Cl 追加: 必要幅 = 1 行テキスト幅 × inflate + 枠/余白。</summary>
    private static int Needed(string text, Font font, double inflate, int pad)
        => (int)Math.Ceiling(TextRenderer.MeasureText(text ?? "", font).Width * inflate) + pad;

    /// <summary>260726Cl 追加: MeasureText の既定パディング (グリフはみ出し用の左右余白 ≒5px) を含まない生の文字幅。
    /// 描画余白が判っている ComboBox/ListBox の項目判定に使う (既定パディング込みだと一律 5px 過大に出る)。</summary>
    private static int NeededRaw(string text, Font font, double inflate)
        => (int)Math.Ceiling(TextRenderer.MeasureText(text ?? "", font,
            new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width * inflate);

    /// <summary>260726Cl 追加: 同名コントロールが複数フォーム/UserControl に居るため、親名を付けて識別できるようにする。</summary>
    private static string ParentPath(Control c)
        => string.IsNullOrEmpty(c.Parent?.Name) ? c.Name : $"{c.Parent.Name}.{c.Name}";

    /// <summary>260726Cl 追加: 不足px から severity を決める (2px 以内=丸め・6px 超=Error の合意に沿う)。</summary>
    private static string Sev(int deficit) => deficit > OverflowErrorPx ? "Error" : "Warning";

    /// <summary>260726Cl 追加: 不足px を判定して行を積む共通処理 (擬似ローカライズ時は翻訳されうる語のみ対象)。
    /// 260726Cl: 擬似ローカライズ判定を static フィールド (inflatePseudo) から引数へ戻した。呼び出し元は全て
    ///   DiagnoseWidget で inflate を引数に持っており、静的可変状態にする理由が無かった (DiagnoseControl の
    ///   同等判定もインラインの inflate &gt; 1.0 で書かれており非対称だった)。</summary>
    private static void Report(List<string> rows, string culture, string form, string ctrl, string type,
        string text, Font font, int actual, int needed, string reason, double inflate = 1.0)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (inflate > 1.0 && !IsLikelyTranslatable(text)) return;
        // 260726Cl: 有効幅が 0 以下 (極端に狭いコンボや Padding が Width を超える AutoEllipsis ラベル) だと
        //   deficit が水増しされて必ず Error になる。測れないものは測らない。
        if (actual <= 0) return;
        int deficit = needed - actual;
        if (deficit <= OverflowTolerancePx) return;
        rows.Add(Row(culture, form, ctrl, type, text, font, actual, needed, deficit, Sev(deficit), reason));
    }

    /// <summary>260726Cl 追加: GetTabRect は再入レイアウト中に例外を投げ得るので安全に取る。
    /// 取れなければ null を返し、呼び出し側はそのタブを飛ばす (旧: Width/Right で別々の番兵を返す 2 メソッド)。</summary>
    private static Rectangle? SafeTabRect(TabControl tc, int index)
    { try { return tc.GetTabRect(index); } catch { return null; } }

    /// <summary>260726Cl 追加: TabControl 全体の行 (TabHeadersOverflow) 用に、全タブ見出しを 1 セルへまとめる。</summary>
    private static string TabTexts(TabControl tc)
        => string.Join(" | ", tc.TabPages.Cast<TabPage>().Select(p => p.Text));

    /// <summary>260726Cl 追加: ComboBox/ListBox の項目の表示文字列 (DisplayMember 解決込み)。null は空文字に潰す。</summary>
    private static string ItemText(ListControl list, object item)
        => list.GetItemText(item) ?? "";

    /// <summary>260726Cl 追加: 自己管理複合コントロール (NumericBox/ColorControl/WaveLengthControl) の内部か。
    /// DiagnoseControl の除外と DiagnoseWidget の AutoEllipsis 判定で共用する。
    /// 260726Cl: 型名の Contains 判定から型パターンへ変更。派生 (NumericBoxWithMenu) が継承で自動的に入り、
    ///   3 型以外に名前が部分一致するクラスは全リポに存在しないので判定は等価。</summary>
    private static bool IsSelfManagedComposite(Control c)
    {
        for (var a = c.Parent; a != null; a = a.Parent)
            if (a is NumericBox or ColorControl or WaveLengthControl) // 同一アセンブリ (Crystallography.Controls)
                return true;
        return false;
    }

    private static string Row(string culture, string form, string ctrl, string type, string text, Font font,
        int actualW, int neededW, int deficit, string severity, string reason)
        // 260726Cl: 書式は CurrentUICulture でなく CurrentCulture で決まる。診断は各言語で走らせて TSV を
        //   突き合わせるので、小数点が "," になる環境だと Font 列が "Yu Gothic UI 9,75pt" になり全行が差分化する。
        //   機械比較できるよう InvariantCulture を明示する。
        => string.Join("\t", culture, form, ctrl, type,
            (text ?? "").Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' '),
            $"{font.Name} {font.Size.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}pt",
            actualW, neededW, deficit, severity, reason);

    #endregion

    #region --capture-form: 単一フォームの画面なし撮影

    // 260807Cl 新規作成: 単一フォームを**画面に出さずに**撮る開発者向けモード (`--capture-form`)。
    //
    // Run (--capture) は CopyFromScreen なので対話デスクトップが必須で、RDP 切断中や
    // 非対話セッションでは撮影が全滅する (新規フォームの目視確認ができずに実際に詰まった)。
    // DrawToBitmap は画面なしで動くので「フォームが構築でき、レイアウトが崩れていない」ことの
    // 自動確認に使える。⚠GL / GraphicsBox など WM_PRINT に応じない描画は白く抜けるため、
    // その種のフォームには使わないこと (そちらは従来どおり --capture)。

    /// <summary>App.exe --capture-form &lt;FormTypeName&gt; &lt;out.png&gt; [culture]</summary>
    public void CaptureSingleForm(string typeName, string outPath)
    {
        var type = FormAssembly.GetTypes()
            .FirstOrDefault(t => typeof(Form).IsAssignableFrom(t) && !t.IsAbstract && t.Name == typeName
                && t.GetConstructor(Type.EmptyTypes) != null);
        if (type == null)
        {
            Console.Error.WriteLine($"--capture-form: no Form type named '{typeName}' with a parameterless constructor");
            Environment.ExitCode = 2;
            return;
        }
        //260809Cl 追加: --capture と同じ「代表状態づくり」を通す。従来はフォームを Show しただけだったので
        //FormALCHEMI のように「計算しないと中身が空」のフォームでは使えなかった。メインフォームを先に作って
        //代表状態 (ReciPro なら spinel 選択) を作り、子フォームへ親情報を注入してから PrepareCaptureState を呼ぶ。
        //⚠メインフォーム自体は Close しない (FormClosing がレジストリへ UI 言語を焼き付けるため。Run 末尾の 260726Cl 注記)。
        void Trace(string s) => Console.WriteLine("--capture-form: " + s);
        Form main = null;
        if (type != MainFormType)
        {
            main = (Form)Activator.CreateInstance(MainFormType);
            main.StartPosition = FormStartPosition.Manual;
            main.Location = new Point(-32000, -32000);
            main.ShowInTaskbar = false;
            main.Show();
            Application.DoEvents();
            PrepareCaptureState(main, Trace);
            Application.DoEvents();
        }

        using var form = (Form)Activator.CreateInstance(type);
        if (main != null) WireDependencies(form, main);
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-32000, -32000);//ハンドルは作るが画面には出さない
        form.ShowInTaskbar = false;
        form.Show();
        Application.DoEvents();
        PrepareCaptureState(form, Trace);
        Application.DoEvents();
        using var bmp = new Bitmap(Math.Max(1, form.Width), Math.Max(1, form.Height));
        form.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
        var dir = Path.GetDirectoryName(Path.GetFullPath(outPath));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        bmp.Save(outPath, ImageFormat.Png);
        form.Hide();
        Console.WriteLine($"--capture-form: {typeName} -> {Path.GetFullPath(outPath)} ({bmp.Width}x{bmp.Height})");
    }

    #endregion
}
