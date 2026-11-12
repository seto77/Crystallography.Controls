#region using
using MemoryPack;
using MemoryPack.Compression;
//using Microsoft.Scripting.Utils; // 260717Cl: 未使用 using (ビルド検証済)
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
//using System.Net; //260317Cl WebClient→HttpClient
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices; // 260712Cl 追加: CollectionsMarshal.AsSpan 用
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
//using Windows.Media.Core; //260405Cl 未使用のWinRT参照を削除
//using Windows.UI.StartScreen; //260405Cl 未使用のWinRT参照を削除
//using ZLinq; // 260717Cl: 未使用 using (ビルド検証済)
//using static Community.CsharpSqlite.Sqlite3; // 260717Cl: 未使用 using (ビルド検証済)
//using static System.Windows.Forms.VisualStyles.VisualStyleElement; // 260717Cl: 未使用 using (ビルド検証済)
#endregion

namespace Crystallography.Controls;

[ToolboxItem(true)] // 260605Cl 追加: 基底 UserControlBase の [ToolboxItem(false)] 継承を打ち消しデザイナのツールボックスに表示
public partial class CrystalDatabaseControl : UserControlBase
{
    //260317Cl 追加 HttpClientはstaticで再利用
    private static readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(600) };

    // 260717Cl (/simplify): ReadCOD/DownloadCodWorker に散在していた同一リテラル (URL 2 箇所・パス連結 9 箇所) を集約。
    private const string CodUrlHeader = "https://github.com/seto77/CSManager/raw/master/COD/";
    private static string CodDatabasePath => UserAppDataPath + "COD.cdb3";

    // 261111Cl 追加: 読み込み・保存・COD の取得のどれかが進行中なら true (再入防止)。UI スレッドでだけ読み書きする。
    //  従来の BackgroundWorker では、読み込み中の ReadDatabase は黙って戻り、保存中の SaveDatabase は InvalidOperationException、読み込みと保存は同時に走れた。
    private bool busy;

    #region フィールド、メソッド、イベント
    // (260322Ch) WFO1000 対応: WinForms アナライザが、コントロールの public プロパティにデザイナ直列化の方針 (DesignerSerializationVisibility / Browsable / DefaultValue 等) を明示するよう要求するため。〔260726Cl: 元の日本語が文字化けで失われていたので書き直した〕
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [DefaultValue(false)] // 260607Cl
    public bool AMCSD_Checked {get=> checkBoxAMCSD.Checked; set => checkBoxAMCSD.Checked = value; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [DefaultValue(false)] // 260607Cl
    public bool COD_Checked {get=>checkBoxCOD.Checked;set => checkBoxCOD.Checked = value; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool AMCSD_Has_Read { get; set; }=false;
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool COD_Has_Read { get; set; }=false;

    public static string UserAppDataPath => new DirectoryInfo(Application.UserAppDataPath).Parent.FullName + @"\";
    public void Suspend()
    {
        bindingSource.RaiseListChangedEvents = false;
        bindingSource.SuspendBinding();
    }

    public void Resume()
    {
        bindingSource.RaiseListChangedEvents = true;
        bindingSource.ResumeBinding();
        bindingSource.ResetBindings(false);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SearchFilter { get => bindingSource.Filter; set => bindingSource.Filter = value; }

    public string DataTypeFilter;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [DefaultValue(9.75f)] // 260607Cl
    public float FontSize
    {
        get => dataGridView.Font.Size;
        set => dataGridView.Font = new Font(dataGridView.Font.FontFamily, value);
    }

    readonly Stopwatch sw = new();

    public Crystal Crystal => Crystal2.GetCrystal(Crystal2);

    public Crystal2 Crystal2 => dataSet.DataTableCrystalDatabase.Get(bindingSource.Current);

    public readonly DataSet.DataTableCrystalDatabaseDataTable Table;

    public event EventHandler CrystalChanged;

    public event EventHandler DataBaseChanged;

    public delegate void ProgressChangedEventHandler(object sender, double progress, string message);
    public event ProgressChangedEventHandler ProgressChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [DefaultValue(false)] // 260607Cl
    public bool DatabaseSelection { set => flowLayoutPanelDatabase.Visible = value; get => flowLayoutPanelDatabase.Visible; }

    #endregion

    #region MemoryPackによるシリアライズ、デシリアライズ
    static byte[] serialize(Crystal2[] c)
    {
        using var compressor = new BrotliCompressor(System.IO.Compression.CompressionLevel.SmallestSize, 24);
        //using var compressor = new BrotliCompressor(System.IO.Compression.CompressionLevel.NoCompression);
        MemoryPackSerializer.Serialize(compressor, c);

        //先頭の4バイトは、データの長さを格納する。
        var data = compressor.ToArray();
        var length = BitConverter.GetBytes(data.Length);
        var buffer = new byte[data.Length + 4];
        Buffer.BlockCopy(length, 0, buffer, 0, 4);
        Buffer.BlockCopy(data, 0, buffer, 4, data.Length);
        return buffer;
    }
    
    static Crystal2[] deserialize(Stream stream)
    {
        var buffer1 = new byte[4];
        stream.ReadExactly(buffer1);
        var length = BitConverter.ToInt32(buffer1);

        var buffer2 = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            stream.ReadExactly(buffer2, 0, length);
            using var decompressor = new BrotliDecompressor();// Decompression(require using)
            return MemoryPackSerializer.Deserialize<Crystal2[]>(decompressor.Decompress(buffer2.AsSpan()[0..length]));
        }
        finally { ArrayPool<byte>.Shared.Return(buffer2); }
    }
    #endregion

    #region コンストラクタ
    public CrystalDatabaseControl()
    {
        InitializeComponent();
        Table = dataSet.DataTableCrystalDatabase;

        ControlHelper.EnableDoubleBuffering(dataGridView); // 260717Cl: 6 ファイル重複のリフレクション 1 行を ControlHelper へ集約
    }
    #endregion

    #region データベース読み込み/書き込み関連

    #region バイト書き込み/読み込み
    private static int readInt(FileStream s) => BitConverter.ToInt32(readBytes(s, 4), 0);
    private static int readByte(FileStream s) => s.ReadByte();
    private static long readLong(FileStream s) => BitConverter.ToInt64(readBytes(s, 8), 0);

    private static byte[] readBytes(FileStream s, int length)
    {
        var bytes = GC.AllocateUninitializedArray<byte>(length);
        s.ReadExactly(bytes);
        return bytes;
    }

    private static void writeInt(FileStream s, in int v) => s.Write(BitConverter.GetBytes(v), 0, 4);
    private static void writeLong(FileStream s, in long v) => s.Write(BitConverter.GetBytes(v), 0, 8);
    private static void writeByte(FileStream s, in byte v) => s.WriteByte(v);
    private static void writeBytes(FileStream s, in byte[] v) => s.Write(v, 0, v.Length);

    #endregion

    #region データベース読み込み
    //public void ReadDatabase(string filename) // 261111Cl 変更前: BackgroundWorker で読み込んでいた
    //{
    //    if (ReadDatabaseWorker.IsBusy) return;
    //
    //    Suspend();
    //    this.Enabled = false;
    //    ReadDatabaseWorker.RunWorkerAsync(filename);
    //}

    /// <summary>データベース (*.cdb3) の読み込みを始めて、すぐに戻る (完了は待たない。従来と同じ)。
    /// 261111Cl 変更: 互換のための薄い包み。本体は <see cref="ReadDatabaseAsync"/>。完了を待つときは ReadDatabaseAsync を await する。
    /// async void なので、ProgressChanged の受け手などで起きた例外は、従来 (RunWorkerCompleted) と同じく UI スレッドの未処理例外になる。</summary>
    public async void ReadDatabase(string filename) => await ReadDatabaseAsync(filename);

    /// <summary>261111Cl 追加: データベース (*.cdb3) を読み込む。ファイルの読み込みと行の追加はワーカースレッドで行い、その間は束縛を止めて (Suspend) コントロールを無効にする。
    /// 進捗は ProgressChanged で知らせ、progress を渡せばそこへも送る。読み込みの失敗はダイアログで知らせる (従来と同じ)。</summary>
    /// <returns>読み込めたら true。失敗した、*.cdb3 でない、または別の読み込み・保存・COD の取得が進行中 (このときは何もしない) なら false。</returns>
    public async Task<bool> ReadDatabaseAsync(string filename, IProgress<(double Progress, string Message)> progress = null)
    {
        if (busy) return false; // 再入防止 (従来の ReadDatabase も読み込み中は黙って戻っていた)
        busy = true;
        try { return await readDatabaseCoreAsync(filename, progress); }
        finally { busy = false; }
    }

    /// <summary>261111Cl 追加: 旧 ReadDatabase と ReadDatabaseWorker_RunWorkerCompleted の本体。UI スレッドで呼ぶ。再入防止 (busy) は呼び出し側で行う。</summary>
    private async Task<bool> readDatabaseCoreAsync(string filename, IProgress<(double Progress, string Message)> progress)
    {
        Suspend();
        Enabled = false;
        // Progress<T> は作ったスレッド (UI) の SynchronizationContext へ送る。従来の ReportProgress と同じく、ワーカーからの進捗は UI スレッドで ProgressChanged になる
        var reporter = new Progress<(double Progress, string Message)>(v => notifyProgress(progress, v.Progress, v.Message));
        Exception error = null;
        var read = false;
        try { read = await Task.Run(() => readDatabaseCore(filename, reporter)); }
        catch (Exception ex) { error = ex; }

        // 以下は旧 ReadDatabaseWorker_RunWorkerCompleted と同じ処理を同じ順で行う
        if (error != null)
            MessageBox.Show($"{error}\r\nFailed to load database. Sorry.");
        if (error != null || read)
            bindingSource.Position = 0; // Resume() より前 (従来と同じく束縛の停止中) に行う
        Enabled = true;
        Resume();
        notifyProgress(progress, 1, $"Total loading time: {sw.ElapsedMilliseconds / 1E3:f1} sec."); // PDIndexer はこの "Total" で初回の読み込みの完了を判定している
        return error == null && read;
    }

    /// <summary>261111Cl 追加: 進捗を ProgressChanged で知らせ、progress があればそこへも送る (sender はこのコントロール。従来は BackgroundWorker だった)。</summary>
    private void notifyProgress(IProgress<(double Progress, string Message)> progress, double value, string message)
    {
        ProgressChanged?.Invoke(this, value, message);
        progress?.Report((value, message));
    }

    //readonly Lock lockObj = new();
    //private void ReadDatabaseWorker_DoWork(object sender, DoWorkEventArgs e) // 261111Cl 変更前
    /// <summary>261111Cl 変更: 旧 ReadDatabaseWorker_DoWork の本体。ワーカースレッド (Task.Run) で動く。失敗は例外で返す (従来の e.Error)。</summary>
    /// <returns>*.cdb3 を読んだら true (従来の e.Result = true)。それ以外のファイルは何もせず false。</returns>
    private bool readDatabaseCore(string filename, IProgress<(double Progress, string Message)> progress)
    {
        //var filename = (string)e.Argument; // 261111Cl 変更前
        //try // 261111Cl 変更前: 例外はワーカーで MessageBox を出さず、RunWorkerCompleted の e.Error で UI スレッドから知らせる
        { // 261111Cl (旧 try の中身。インデントは据え置き)
            sw.Restart();
            // 260712Cl 変更: ToLower() の一時文字列生成+CurrentCulture 依存を避け、序数比較の EndsWith に置換
            // if (filename.ToLower().EndsWith("cdb3")) // 260712Cl 変更前
            if (filename.EndsWith("cdb3", StringComparison.OrdinalIgnoreCase))
            {
                using var fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
                int flag = readByte(fs), total = readInt(fs);
                Table.BeginLoadData(); //260322Cl 追加 DataTable一括読み込み高速化
                try //260322Cl 追加
                {
                if (flag == 100)//単一ファイルの時
                {
                    while (fs.Length != fs.Position)
                    {
                        // 261105Cl 修正: AsParallel() だけでは結果の順番が保たれず、行がファイルと違う順 (実行ごとに違う) で入っていた
                        //  (同梱の AMCSD.cdb3 で 21,588 行中 21,000 行以上)。そのまま保存し直すと、圧縮のために雑誌名順に並べてあるファイルの順番が崩れる。
                        //  AsOrdered() でファイルの順に入れる。読み込み時間は誤差の範囲 (AMCSD.cdb3 の実測で中央値 713 → 639 ms)。読み込んでそのまま保存すると元のファイルと同じバイト列になる。
                        //foreach (var r in deserialize(fs).AsParallel().Select(r => Table.CreateRow(r, Crystal2.Serialize(r)))) // 261105Cl 変更前
                        //foreach (var r in deserialize(fs).AsParallel().AsOrdered().Select(r => Table.CreateRow(r, Crystal2.Serialize(r)))) // 261105Cl // 261111Cl 変更前
                        //    Table.Rows.Add(r); // 261111Cl 変更前
                        addRowsInOrder(deserialize(fs)); // 261111Cl 変更: 並列は Serialize だけ。行の作成と追加は直列 (DataTable はスレッド非安全)
                        //ReadDatabaseWorker.ReportProgress(0, report(Table.Rows.Count, total, sw.ElapsedMilliseconds, "Loading database...")); // 261111Cl 変更前
                        progress.Report(report(Table.Rows.Count, total, sw.ElapsedMilliseconds, "Loading database...")); // 261111Cl
                    }
                    GC.Collect();
                }

                else if (flag == 200)//分割ファイルの時
                {
                    var fileNum = readInt(fs);
                    var fileNames = Enumerable.Range(0, fileNum)
                        .Select(i => $"{filename[..^5]}\\{Path.GetFileNameWithoutExtension(filename)}.{i:000}").ToList();
                    fileNames.ForEach(fn =>
                    {
                        using var stream = new FileStream(fn, FileMode.Open);
                        while (stream.Length != stream.Position)
                        {
                            //deserialize(stream).AsParallel().Select(Table.CreateRow).ToList().ForEach(Table.Rows.Add);//この書き方だとメモリ使用量が増える
                            //foreach (var row in deserialize(stream).AsParallel().Select(r => Table.CreateRow(r, Crystal2.Serialize(r)))) // 261105Cl 変更前
                            //foreach (var row in deserialize(stream).AsParallel().AsOrdered().Select(r => Table.CreateRow(r, Crystal2.Serialize(r)))) // 261105Cl: 上と同じくファイルの順に入れる // 261111Cl 変更前
                            //    Table.Add(row); // 261111Cl 変更前
                            addRowsInOrder(deserialize(stream)); // 261111Cl 変更: 上と同じ

                            //ReadDatabaseWorker.ReportProgress(0, report(Table.Rows.Count, total, sw.ElapsedMilliseconds, "Loading database...")); // 261111Cl 変更前
                            progress.Report(report(Table.Rows.Count, total, sw.ElapsedMilliseconds, "Loading database...")); // 261111Cl
                        }
                        GC.Collect();
                    });

                }
                } //260322Cl 追加
                finally { Table.EndLoadData(); } //260322Cl 追加
            }
            else
                //return; // 261111Cl 変更前
                return false; // 261111Cl
        }
        //catch (Exception ex) // 261111Cl 変更前: ワーカースレッドから MessageBox を出していた
        //{
        //    MessageBox.Show($"{ex}\r\nFailed to load database. Sorry.");
        //}

        //bindingSource.Position = 0; // 261111Cl 変更前: ワーカースレッドから BindingSource を触っていた
        //e.Result = true; // 261111Cl 追加: Position = 0 を UI スレッドで行う印 (従来どおり cdb3 以外のファイルでは行わない) // 261111Cl 変更前 (BackgroundWorker の廃止)
        return true; // 261111Cl: Position = 0 を UI スレッドで行う印 (従来どおり cdb3 以外のファイルでは行わない)
    }

    /// <summary>260717Cl 統合 (旧: ReadDatabaseWorker_/SaveDatabaseWorker_/DownloadCodWorker_ProgressChanged):
    /// 3 つの BackgroundWorker で本体が完全同一だったため 1 ハンドラへ (Designer 側 3 箇所の配線も付け替え)。
    /// 261111Cl: BackgroundWorker を使わなくなったので呼ばれない (進捗は notifyProgress)。Designer.cs の配線のためだけに残す。</summary>
    private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        (double progress, string message) = ((double Progress, string Message))e.UserState;
        ProgressChanged?.Invoke(sender, progress, message);
        // 260428Cl Application.DoEvents() を削除 (BackgroundWorker の ProgressChanged は UI スレッドで動作するため不要)
    }

    #region 261111Cl 変更前: 読み込みの完了処理 (本体は readDatabaseCoreAsync の await の後へ移した。Designer.cs の配線用の空のハンドラは末尾)
    //private void ReadDatabaseWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    //{
    //    // 261111Cl 追加: 従来 DoWork の catch でワーカースレッドから出していた失敗のダイアログと、DoWork の最後の Position = 0 (失敗時も行っていた) を UI スレッドで行う。
    //    //  e.Error があるときに e.Result を読むと例外になるので、先に e.Error を見る。
    //    if (e.Error != null) // 261111Cl 追加
    //        MessageBox.Show($"{e.Error}\r\nFailed to load database. Sorry."); // 261111Cl 追加
    //    if (e.Error != null || e.Result is true) // 261111Cl 追加
    //        bindingSource.Position = 0; // 261111Cl 追加: Resume() より前 (従来と同じく束縛の停止中) に行う
    //    this.Enabled = true;
    //    Resume();
    //    ProgressChanged?.Invoke(sender, 1, $"Total loading time: {sw.ElapsedMilliseconds / 1E3:f1} sec.");
    //    // 260428Cl Application.DoEvents() を削除 (RunWorkerCompleted は UI スレッドで動作するため不要)
    //}
    #endregion

    #endregion

    #region データベース書き込み
    //public void SaveDatabase(string fn) // 261111Cl 変更前: BackgroundWorker で保存していた
    //{
    //    Enabled = false;
    //    SaveDatabaseWorker.RunWorkerAsync(fn);
    //}

    /// <summary>データベースの保存を始めて、すぐに戻る (完了は待たない。従来と同じ)。
    /// 261111Cl 変更: 互換のための薄い包み。本体は <see cref="SaveDatabaseAsync"/>。完了を待つときは SaveDatabaseAsync を await する。</summary>
    public async void SaveDatabase(string fn) => await SaveDatabaseAsync(fn);

    /// <summary>261111Cl 追加: データベースを保存する (検索の絞り込みに関わらず全行。並び順は表示中の並べ替え)。書き出しはワーカースレッドで行い、その間はコントロールを無効にする。
    /// 進捗は ProgressChanged で知らせ、progress を渡せばそこへも送る。保存の失敗はダイアログで知らせる (従来と同じ)。</summary>
    /// <returns>保存できたら true。失敗した、または別の読み込み・保存・COD の取得が進行中 (このときは何もしない) なら false。</returns>
    public async Task<bool> SaveDatabaseAsync(string fn, IProgress<(double Progress, string Message)> progress = null)
    {
        if (busy) return false; // 再入防止 (従来は保存中に SaveDatabase を呼ぶと RunWorkerAsync が InvalidOperationException を投げた)
        busy = true;
        try
        {
            Enabled = false;
            var reporter = new Progress<(double Progress, string Message)>(v => notifyProgress(progress, v.Progress, v.Message));
            var sort = (bindingSource.List as DataView)?.Sort ?? ""; // 表示中の並べ替え。従来はワーカースレッドで bindingSource から読んでいた
            Exception error = null;
            try { await Task.Run(() => saveDatabaseCore(fn, sort, reporter)); }
            catch (Exception ex) { error = ex; }

            // 以下は旧 SaveDatabaseWorker_RunWorkerCompleted と同じ処理を同じ順で行う
            Enabled = true;
            if (error != null)
            {
                notifyProgress(progress, 1, $"Failed to save database: {error.GetBaseException().Message}");
                MessageBox.Show($"{error}\r\nFailed to save database. Sorry.");
                return false;
            }
            notifyProgress(progress, 1, $"Total saving time: {sw.ElapsedMilliseconds / 1E3:f1} sec.");
            return true;
        }
        finally { busy = false; }
    }

    //private void SaveDatabaseWorker_DoWork(object sender, DoWorkEventArgs e) // 261111Cl 変更前
    /// <summary>261111Cl 変更: 旧 SaveDatabaseWorker_DoWork の本体。ワーカースレッド (Task.Run) で動く。失敗は例外で返す (従来の e.Error)。sort は表示中の並べ替え。</summary>
    private void saveDatabaseCore(string fn, string sort, IProgress<(double Progress, string Message)> progress)
    {
        sw.Restart();

        //var fn = (string)e.Argument; // 261111Cl 変更前

        //var total = Table.Count; // 261105Cl 変更前
        // 261105Cl 修正: 検索で絞り込み中 (bindingSource.Filter = "Flag = true") に保存すると、件数は全行 (Table.Count) なのに行は絞り込み後の
        //  bindingSource[j] から読んでいたため範囲外の例外になり、ヘッダ (5 バイト) だけのファイルが残ったうえ成功と表示されていた。
        //  絞り込みに関わらず全行を書く。並び順は従来どおり表示中の並べ替え (列見出しのクリック等) に従う。行のバイト列は参照を集めるだけ (複製しない)。
        byte[][] rows; // 261105Cl 追加
        //using (var view = new DataView(Table, "", (bindingSource.List as DataView)?.Sort ?? "", DataViewRowState.CurrentRows)) // 261105Cl 追加 // 261111Cl 変更前
        using (var view = new DataView(Table, "", sort, DataViewRowState.CurrentRows)) // 261111Cl 変更: 並べ替えは UI スレッドで読んで受け取る (SaveDatabaseAsync)
            rows = [.. view.Cast<DataRowView>().Select(r => (byte[])r.Row[0])]; // 261105Cl 追加
        var total = rows.Length; // 261105Cl

        var thresholdBytes = 35_000_000;
        var division = 2500;//分割単位 たぶんパフォーマンスに効く

        //using var fs = new FileStream(fn, FileMode.Create, FileAccess.Write); // 261105Cl 変更前
        // 261105Cl 修正: 対象を先に FileMode.Create で切り詰めてから書いていたため、途中で失敗すると (容量不足、行の読み出しの例外など)
        //  壊れたデータベース (ヘッダだけ、または書きかけ) が残った。同じフォルダの一時ファイル (本体 "<fn>.tmp"、分割ファイルも各 ".tmp") へ書き、
        //  全部書けてから名前を付け替えて置き換える (分割ファイル → 本体の順)。失敗したら一時ファイルを消し、元のファイルはそのまま残す。
        //  例外はそのまま投げ直し、従来どおり RunWorkerCompleted (e.Error) で知らせる。
        var tmpFn = fn + ".tmp"; // 261105Cl 追加
        var parts = new List<(string Tmp, string Final)>(); // 261105Cl 追加: 書いた分割ファイル (一時ファイル名, 置き換え先)
        try // 261105Cl 追加
        { // 261105Cl 追加
        using (var fs = new FileStream(tmpFn, FileMode.Create, FileAccess.Write)) // 261105Cl
        { // 261105Cl 追加

        writeByte(fs, 100);//とりあえず先頭に100 (分割なし)を書き込む
        writeInt(fs, total);//データの個数を書き込む

        var fileCounter = 0;
        var subDir = fn[..^5] + "\\";
        var header = $"{subDir}{Path.GetFileNameWithoutExtension(fn)}.";
        var fileSize = new List<long>();
        //var byteList = new List<byte>(); // 261111Cl 変更前: チャンクを最大 35 MB までここへ写していた

        var counter = 0;
        var bytes = new byte[total / division + 1][];
        //SaveDatabaseWorker.ReportProgress(0, report(counter, bytes.Length, sw.ElapsedMilliseconds, "Saving database...")); // 261111Cl 変更前
        progress.Report(report(counter, bytes.Length, sw.ElapsedMilliseconds, "Saving database...")); // 261111Cl
        Parallel.For(0, bytes.Length, /*new ParallelOptions() { MaxDegreeOfParallelism = 8 },*/ i =>
        {
            var crystal2List = new List<Crystal2>();
            for (int j = i * division; j < total && j < (i + 1) * division; j++)
            {
                //var c = Crystal2.Deserialize((byte[])((DataRowView)bindingSource[j]).Row[0]); // 261105Cl 変更前
                var c = Crystal2.Deserialize(rows[j]); // 261105Cl
                //c.datatype = (byte)Crystal2.DataType.COD;
                crystal2List.Add(c);
            }
            bytes[i] = serialize([.. crystal2List]);

            //SaveDatabaseWorker.ReportProgress(0, report(Interlocked.Increment(ref counter), bytes.Length, sw.ElapsedMilliseconds, "Saving database...")); // 261111Cl 変更前
            progress.Report(report(Interlocked.Increment(ref counter), bytes.Length, sw.ElapsedMilliseconds, "Saving database...")); // 261111Cl
        });

        #region 261111Cl 変更前: チャンクを List<byte> (最大 35 MB) へ写してから書いていた
        //for (int i = 0; i < bytes.Length; i++)
        //{
        //    byteList.AddRange(bytes[i]);

        //    //最後まで来ている時で、かつ閾値以下の容量で、かつこれまで一度も分割もしていない場合
        //    if (i == bytes.Length - 1 && byteList.Count <= thresholdBytes && fileCounter == 0)
        //        // 260712Cl 変更: [.. byteList] は最大 thresholdBytes(~35MB) の List<byte> を毎回 byte[] へ全コピーする。
        //        // CollectionsMarshal.AsSpan で内部配列を直接書き込み、余分な確保+コピーを排除 (同期 Write 中に byteList は不変)。
        //        // fs.Write([.. byteList], 0, byteList.Count);//最初のファイルに書き込んで終了 // 260712Cl 変更前
        //        fs.Write(CollectionsMarshal.AsSpan(byteList));//最初のファイルに書き込んで終了

        //    //最後まで来ている時か、閾値以上の容量の場合
        //    else if (i == bytes.Length - 1 || byteList.Count > thresholdBytes)
        //    {
        //        if (fileCounter == 0)
        //            Directory.CreateDirectory(fn[..^5]);
        //        //using (var fs1 = new FileStream(header + fileCounter.ToString("000"), FileMode.Create, FileAccess.Write)) // 261105Cl 変更前
        //        parts.Add((header + fileCounter.ToString("000") + ".tmp", header + fileCounter.ToString("000"))); // 261105Cl 追加
        //        using (var fs1 = new FileStream(parts[^1].Tmp, FileMode.Create, FileAccess.Write)) // 261105Cl
        //            // 260712Cl 変更: 上記と同じく全コピーを避けて内部配列を直接書き込み
        //            // fs1.Write([.. byteList], 0, byteList.Count); // 260712Cl 変更前
        //            fs1.Write(CollectionsMarshal.AsSpan(byteList));
        //        fileSize.Add(byteList.Count);
        //        byteList.Clear();

        //        fileCounter++;
        //    }
        //}
        #endregion
        // 261111Cl 変更: 各チャンクを FileStream へ直接書く。分割の規則 (累計が閾値を超えたら、そこまでで 1 ファイル。全体が閾値以下なら本体に書く) と
        //  出力のバイト列は従来と同じ。groupBytes が旧 byteList.Count、groupStart はそのファイルの最初のチャンク。
        long groupBytes = 0; // 261111Cl 追加
        int groupStart = 0; // 261111Cl 追加
        for (int i = 0; i < bytes.Length; i++)
        {
            groupBytes += bytes[i].Length;

            //最後まで来ている時で、かつ閾値以下の容量で、かつこれまで一度も分割もしていない場合
            if (i == bytes.Length - 1 && groupBytes <= thresholdBytes && fileCounter == 0)
                for (int k = groupStart; k <= i; k++)
                    fs.Write(bytes[k]);//最初のファイルに書き込んで終了

            //最後まで来ている時か、閾値以上の容量の場合
            else if (i == bytes.Length - 1 || groupBytes > thresholdBytes)
            {
                if (fileCounter == 0)
                    Directory.CreateDirectory(fn[..^5]);
                parts.Add((header + fileCounter.ToString("000") + ".tmp", header + fileCounter.ToString("000"))); // 261105Cl 追加
                using (var fs1 = new FileStream(parts[^1].Tmp, FileMode.Create, FileAccess.Write)) // 261105Cl
                    for (int k = groupStart; k <= i; k++)
                        fs1.Write(bytes[k]);
                fileSize.Add(groupBytes);
                groupBytes = 0;
                groupStart = i + 1;

                fileCounter++;
            }
        }

        //for (int i = 0; i < total; i += division)
        //{
        //    var crystal2List = new List<Crystal2>();
        //    for (int j = i; j < total && j < i + division; j++)
        //    {
        //        var c = DataTableCrystalDatabaseDataTable.deserialize((byte[]) ((DataRowView)bindingSource[j]).Row[0]);
        //        crystal2List.Add(c);
        //    }
        //    byteList.AddRange(serialize(crystal2List.ToArray()));

        //    //最後まで来ている時で、かつ閾値以下の容量で、かつこれまで一度も分割もしていない場合
        //    if (i + division >= total && byteList.Count <= thresholdBytes && fileCounter == 0)
        //        fs.Write([.. byteList], 0, byteList.Count);//最初のファイルに書き込んで終了

        //    //最後まで来ている時か、閾値以上の容量の場合
        //    else if (i + division >= total || byteList.Count > thresholdBytes)
        //    {
        //        if (fileCounter == 0)
        //            Directory.CreateDirectory(fn.Remove(fn.Length - 5, 5));
        //        using (var fs1 = new FileStream(header + fileCounter.ToString("000"), FileMode.Create, FileAccess.Write))
        //            fs1.Write([.. byteList], 0, byteList.Count);
        //        fileSize.Add(byteList.Count);
        //        byteList.Clear();

        //        fileCounter++;
        //    }
        //    SaveDatabaseWorker.ReportProgress(0, report(i, total, sw.ElapsedMilliseconds, "Saving database..."));
        //}

        if (fileCounter > 0)//分割ファイルになった場合
        {
            //分割ファイル数書き込み
            writeInt(fs, fileCounter);
            //ファイルサイズ書き込み
            for (int i = 0; i < fileCounter; i++)
                writeLong(fs, fileSize[i]);
            //チェックサムを書き込み
            for (int i = 0; i < fileCounter; i++)
                //writeBytes(fs, getMD5(header + i.ToString("000"))); // 261105Cl 変更前
                writeBytes(fs, getMD5(parts[i].Tmp)); // 261105Cl: 一時ファイルの MD5 (置き換え後も同じ内容)
            //最後に先頭に戻って200を書き込み
            fs.Position = 0;
            writeByte(fs, 200);
        }
        } // 261105Cl 追加: ここで一時ファイルを閉じる

        foreach (var (tmp, final) in parts) // 261105Cl 追加: 全部書けたので置き換える (同じフォルダ内の名前の付け替え)
            File.Move(tmp, final, true); // 261105Cl 追加
        File.Move(tmpFn, fn, true); // 261105Cl 追加
        } // 261105Cl 追加
        catch // 261105Cl 追加: 一時ファイルを消して投げ直す。置き換えの前に失敗したときは、元のファイルは触っていない
        { // 261105Cl 追加
            foreach (var t in parts.Select(p => p.Tmp).Append(tmpFn)) // 261105Cl 追加
                try { File.Delete(t); } catch (IOException) { } catch (UnauthorizedAccessException) { } // 261105Cl 追加: 後始末の失敗で元の例外を隠さない
            throw; // 261105Cl 追加
        } // 261105Cl 追加
    }

    // 260717Cl: worker_ProgressChanged へ統合 (本体が完全同一だった)。
    //private void SaveDatabaseWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) { … }

    #region 261111Cl 変更前: 保存の完了処理 (本体は SaveDatabaseAsync の await の後へ移した。Designer.cs の配線用の空のハンドラは末尾)
    //private void SaveDatabaseWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    //{
    //    this.Enabled = true;
    //    // 261105Cl 修正: DoWork の例外 (e.Error) を見ずに常に成功と表示していた。失敗は進捗メッセージとダイアログで知らせる (読み込み失敗時と同じ形)。
    //    if (e.Error != null) // 261105Cl 追加
    //    {
    //        ProgressChanged?.Invoke(sender, 1, $"Failed to save database: {e.Error.GetBaseException().Message}"); // 261105Cl 追加
    //        MessageBox.Show($"{e.Error}\r\nFailed to save database. Sorry."); // 261105Cl 追加
    //        return; // 261105Cl 追加
    //    }
    //    ProgressChanged?.Invoke(sender, 1, $"Total saving time: {sw.ElapsedMilliseconds / 1E3:f1} sec.");
    //    // 260428Cl Application.DoEvents() を削除 (RunWorkerCompleted は UI スレッドで動作するため不要)
    //}
    #endregion

    #endregion

    #region データベースの正当性チェック
    /// <summary>分割ファイルがきちんと作成されているかをチェック</summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public (bool Valid, int DataNum, int FileNum, long[] FileSizes, byte[][] CheckSums) CheckDatabaseFiles(string filename, bool checkMD5)
    {
        sw.Restart();
        var nameWithoutExt = Path.GetFileNameWithoutExtension(filename);
        var subDir = Path.GetDirectoryName(filename) + "\\" + nameWithoutExt + "\\";
        try
        {
            int dataNum = 0, fileNum = 0;
            var fileSizes = Array.Empty<long>();
            var checkSums = Array.Empty<byte[]>();

            if (File.Exists(filename))
            {
                //データ個数、ファイル数を取得
                using (var fs = new FileStream(filename, FileMode.Open))
                {
                    var head = readByte(fs);
                    dataNum = readInt(fs);
                    fileNum = readInt(fs);

                    fileSizes = new long[fileNum];
                    for (int i = 0; i < fileNum; i++)
                        fileSizes[i] = readLong(fs);

                    checkSums = new byte[fileNum][];
                    for (int i = 0; i < fileNum; i++)
                        checkSums[i] = readBytes(fs, 16);
                }
                if (checkMD5)
                {
                    //md5をチェック
                    bool flag = true;
                    for (int i = 0; i < fileNum && flag; i++)
                        flag = CrystalDatabaseControl.checkMD5($"{subDir}{nameWithoutExt}.{i:000}", checkSums[i]);
                    return (flag, dataNum, fileNum, fileSizes, checkSums);
                }
                else
                    return (false, dataNum, fileNum, fileSizes, checkSums);
            }
            return (false, 0, 0, null, null);
        }
        catch
        { return (false, 0, 0, null, null); }
    }


    /// <summary>MD5を取得する。ファイルが存在しない場合は null を返す。</summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private static byte[] getMD5(string path)
    {
        if (!File.Exists(path))
            return null;
        using var fs = new FileStream(path, FileMode.Open);
        // 260712Cl 変更: MD5.Create() は IDisposable を返すが未 Dispose だった。静的 MD5.HashData(Stream) で同一ハッシュを算出しつつ破棄漏れを解消。
        // return MD5.Create().ComputeHash(fs); // 260712Cl 変更前
        return MD5.HashData(fs);
    }

    /// <summary>
    /// MD5とファイルが一致するかどうか調べる。
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="md5"></param>
    /// <returns></returns>
    private static bool checkMD5(string path, byte[] md5)
    {
        var _md5 = getMD5(path);
        return _md5 != null && md5.Length == _md5.Length && md5.SequenceEqual(_md5);
    }
    #endregion

    #endregion

    #region CODデータベースのダウロード、読み込み

    #region 261111Cl 変更前: ReadCOD (ダウンロードの完了を Sleep + DoEvents で待ち、UI スレッドで .Result を呼んでいた)
    ///// <summary>CODデータベースのダウンロードや読み込み.</summary>
    ///// <returns>正常に読み込めた場合はtrue, 途中キャンセルされたりした場合はfalse</returns>
    //public bool ReadCOD()
    //{
    //    var (Valid, DataNum, FileNum, FileSizes, CheckSums) = CheckDatabaseFiles(CodDatabasePath, true); // 260717Cl: パス連結を CodDatabasePath へ集約
    //    string urlHeader = CodUrlHeader; // 260717Cl: 同一リテラル 2 箇所を定数へ集約
    //    if (Valid)
    //    {//適切にダウンロードされている場合
    //        try//web上に新しいデータがあるかどうかをチェック
    //        {
    //            //260317Cl WebClient→HttpClient
    //            //if (new WebClient().DownloadData(new Uri(urlHeader + "COD.cdb3")).SequenceEqual(File.ReadAllBytes(CodDatabasePath)))
    //            if (httpClient.GetByteArrayAsync(urlHeader + "COD.cdb3").Result.SequenceEqual(File.ReadAllBytes(CodDatabasePath)))
    //            {//ローカルのCODが最新版の場合
    //                ReadDatabase(CodDatabasePath);
    //                return true;
    //            }
    //            else
    //            {//更新版が存在する場合
    //                var result = MessageBox.Show("Now, new database is available.\r\n  Download and load the new database: YES\r\n" +
    //                    "  Use the current database: No\r\n  Cancel database loading: Cancel", "  New database is available", MessageBoxButtons.YesNoCancel);
    //
    //                if (result == DialogResult.No) //Noの場合 (更新せずに現状を読み込む場合)
    //                    ReadDatabase(CodDatabasePath);
    //
    //                if (result == DialogResult.No || result == DialogResult.Cancel)//NoかCancelの場合
    //                    return false;
    //            }
    //
    //        }
    //        catch //WEBが落ちている場合は、現状のCODを読み込む
    //        {
    //            ReadDatabase(CodDatabasePath);
    //            return true;
    //        }
    //    }
    //    else//CODデータが存在しないか、適切でない場合
    //    {
    //        if (MessageBox.Show("Local COD database is missing.\r\n  Do you download the new database now ?", "Local COD database is missing.",
    //            MessageBoxButtons.YesNo) == DialogResult.No)
    //            return false;
    //    }
    //
    //    //ここまでくる場合は、CODデータが存在しない場合
    //    //CODをダウンロードする
    //    if (DownloadCodWorker.IsBusy) return false;
    //    DownloadCodWorker.RunWorkerAsync(UserAppDataPath);
    //
    //    while (DownloadCodWorker.IsBusy)//ダウンロードが完了するまで待つ
    //    {
    //        Thread.Sleep(100);
    //        Application.DoEvents();
    //    }
    //    //this.Enabled = true;
    //    ReadDatabase(CodDatabasePath);
    //    return true;
    //}
    #endregion

    /// <summary>CODデータベースのダウンロードや読み込みを始めて、すぐに戻る。
    /// 261111Cl 変更: 互換のための薄い包み。本体は <see cref="ReadCODAsync"/>。結果が要るときは ReadCODAsync を await する。
    /// ⚠ 戻り値の意味が変わった。従来は「(ダウンロードの完了を DoEvents で待ってから) 読み込みを始めたら true、キャンセル等なら false」、
    /// 今は「始めたら true、別の読み込み・保存・COD の取得が進行中で始めなかったら false」。4 アプリに呼び出しは無い (261111Cl に確認。CSManager の旧コードはコメントアウト済み)。</summary>
    public bool ReadCOD()
    {
        if (busy) return false;
        run();
        return true;
        async void run() => await ReadCODAsync(); // async void: 例外は従来 (RunWorkerCompleted) と同じく UI スレッドの未処理例外になる
    }

    /// <summary>261111Cl 追加: CODデータベースのダウンロードや読み込み (旧 ReadCOD の async 版)。読み込みの完了まで待つ。
    /// ローカルの COD が無いか壊れていれば、ダウンロードするか尋ねる。ローカルの COD が web 上と違えば、更新するか尋ねる。</summary>
    /// <returns>読み込んだら true。キャンセルされた、ダウンロードに失敗した、または別の読み込み・保存・COD の取得が進行中 (このときは何もしない) なら false。</returns>
    public async Task<bool> ReadCODAsync(IProgress<(double Progress, string Message)> progress = null)
    {
        if (busy) return false; // 再入防止
        busy = true;
        try
        {
            // 全分割ファイルの MD5 を計算するので重い (従来は UI スレッドで計算していた)
            var (Valid, DataNum, FileNum, FileSizes, CheckSums) = await Task.Run(() => CheckDatabaseFiles(CodDatabasePath, true));
            string urlHeader = CodUrlHeader;
            if (Valid)
            {//適切にダウンロードされている場合
                bool? isLatest; //web上に新しいデータがあるかどうか。null は確かめられなかった場合 (WEB が落ちている等)
                try { isLatest = (await httpClient.GetByteArrayAsync(urlHeader + "COD.cdb3")).SequenceEqual(File.ReadAllBytes(CodDatabasePath)); } // ローカルは見出しだけの小さいファイル (従来と同じく同期で読む)
                catch { isLatest = null; }

                if (isLatest != false)
                {//ローカルのCODが最新版の場合か、WEBが落ちている場合は、現状のCODを読み込む
                    await readDatabaseCoreAsync(CodDatabasePath, progress);
                    return true;
                }
                //更新版が存在する場合
                var result = MessageBox.Show("Now, new database is available.\r\n  Download and load the new database: YES\r\n" +
                    "  Use the current database: No\r\n  Cancel database loading: Cancel", "  New database is available", MessageBoxButtons.YesNoCancel);

                if (result == DialogResult.No) //Noの場合 (更新せずに現状を読み込む場合)
                {
                    await readDatabaseCoreAsync(CodDatabasePath, progress);
                    // 261111Cl 修正: 従来は読み込んだのに false を返していた。呼び出し元 (COD のチェックボックス) がチェックを外して COD_Has_Read も立てず、
                    //  COD の行は表に入ったまま検索では隠れ、もう一度チェックすると同じ行が二重に読み込まれた。
                    return true;
                }
                if (result == DialogResult.Cancel)
                    return false;
            }
            else//CODデータが存在しないか、適切でない場合
            {
                if (MessageBox.Show("Local COD database is missing.\r\n  Do you download the new database now ?", "Local COD database is missing.",
                    MessageBoxButtons.YesNo) == DialogResult.No)
                    return false;
            }

            //ここまでくる場合は、CODデータが存在しないか、更新する場合。CODをダウンロードする
            var reporter = new Progress<(double Progress, string Message)>(v => notifyProgress(progress, v.Progress, v.Message));
            try { await Task.Run(() => downloadCodCoreAsync(reporter)); }
            catch
            {
                MessageBox.Show("Failed to download new COD database. Sorry.", "Error", MessageBoxButtons.OK);
                // 261111Cl 修正: 従来はダウンロードに失敗しても続けて COD を読み込み (無い・書きかけのファイルなので多くは読み込みの失敗のダイアログも出た)、true を返して COD_Has_Read を立てていた。
                //  読み込まずに false を返す (チェックが外れ、もう一度チェックすればやり直せる)。
                return false;
            }
            await readDatabaseCoreAsync(CodDatabasePath, progress);
            return true;
        }
        finally { busy = false; }
    }

    //260317Cl WebClient→HttpClient
    //private static readonly HttpClient httpClient = new(); は上部で定義
    //private void DownloadCodWorker_DoWork(object sender, DoWorkEventArgs e) // 261111Cl 変更前
    /// <summary>261111Cl 変更: 旧 DownloadCodWorker_DoWork の本体。Task.Run の中で呼ぶ (UI の SynchronizationContext に戻らない)。失敗は例外で返す (従来の e.Error)。</summary>
    private async Task downloadCodCoreAsync(IProgress<(double Progress, string Message)> progress)
    {

        string urlHeader = CodUrlHeader; // 260717Cl: 同一リテラル 2 箇所を定数へ集約
        //var UserAppDataPath = (string)e.Argument; // 261111Cl 変更前: 引数はいつも静的プロパティ UserAppDataPath だったので、それを直接使う

        //ここからCODをダウンロード
        //try // 261111Cl 変更前: 例外はワーカーで MessageBox を出さず、RunWorkerCompleted の e.Error で UI スレッドから知らせる
        { // 261111Cl (旧 try の中身。インデントは据え置き)
            sw.Restart();
            //260317Cl WebClient→HttpClient
            //new WebClient().DownloadFile(new Uri(urlHeader + "COD.cdb3"), CodDatabasePath);
            //var cdb3Data = httpClient.GetByteArrayAsync(urlHeader + "COD.cdb3").Result; // 261111Cl 変更前
            var cdb3Data = await httpClient.GetByteArrayAsync(urlHeader + "COD.cdb3"); // 261111Cl
            File.WriteAllBytes(CodDatabasePath, cdb3Data);

            Directory.CreateDirectory(UserAppDataPath + "COD");

            var (_, DataNum, FileNum, FileSizes, CheckSums) = CheckDatabaseFiles(CodDatabasePath, false);

            //260317Cl WebClient→HttpClient 並列ダウンロード
            var total = FileSizes.Sum();
            var current = new long[FileNum];
            var completedCount = 0;
            long n = 1;
            var tasks = new Task[FileNum];

            for (int i = 0; i < FileNum; i++)
            {
                var _i = i;//このインスタンスで作成する必要あり
                var url = $"{urlHeader}COD/COD.{_i:000}";
                var filePath = $"{UserAppDataPath}COD\\COD.{_i:000}";
                tasks[_i] = Task.Run(async () =>
                {
                    using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                    using var contentStream = await response.Content.ReadAsStreamAsync();
                    using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                    var buffer = new byte[8192];
                    int read;
                    long bytesRead = 0;
                    while ((read = await contentStream.ReadAsync(buffer)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, read));
                        bytesRead += read;
                        current[_i] = bytesRead;
                        if (Interlocked.Increment(ref n) % 100 == 0)
                            //DownloadCodWorker.ReportProgress(0, report(current.Sum(), total, sw.ElapsedMilliseconds, // 261111Cl 変更前
                            progress.Report(report(current.Sum(), total, sw.ElapsedMilliseconds, // 261111Cl
                            $"Downloading COD database ({current.Sum() / 1E6:f0} MB / {total / 1E6:f0} MB) ..."));
                    }
                    Interlocked.Increment(ref completedCount);
                });
            }

            //Task.WaitAll(tasks); // 261111Cl 変更前
            await Task.WhenAll(tasks); // 261111Cl
        }
        //catch // 261111Cl 変更前: ワーカースレッドから MessageBox を出していた
        //{
        //    MessageBox.Show("Failed to download new COD database. Sorry.", "Error", MessageBoxButtons.OK);
        //}
    }

    // 260717Cl: worker_ProgressChanged へ統合 (本体が完全同一だった)。
    //private void DownloadCodWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) { … }

    #region 261111Cl 変更前: ダウンロードの完了処理 (失敗のダイアログは ReadCODAsync へ移した。Designer.cs の配線用の空のハンドラは末尾)
    //private void DownloadCodWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    //{
    //    // 261111Cl 追加: 従来 DoWork の catch でワーカースレッドから出していたダイアログ。ReadCOD の待ちループ (DoEvents) の中で、読み込みの前に出る (従来と同じ順)
    //    if (e.Error != null) // 261111Cl 追加
    //        MessageBox.Show("Failed to download new COD database. Sorry.", "Error", MessageBoxButtons.OK); // 261111Cl 追加
    //}
    #endregion

    //260317Cl 削除 MyWebClient : WebClient (HttpClientに移行)
    //private class MyWebClient : WebClient { ... }

    #endregion

    #region 進捗状況のレポート
    /// <summary>進捗状況</summary>
    /// <param name="current"></param>
    /// <param name="total"></param>
    /// <param name="elapsedMilliseconds">経過時間</param>
    /// <param name="message">メッセージ</param>
    /// <param name="sleep"></param>
    /// <param name="showPercentage"></param>
    /// <param name="showElapsedTime"></param>
    /// <param name="showRemainTime"></param>
    /// <param name="digit"></param>
    private static (double Progress, string Message) report(long current, long total, long elapsedMilliseconds, string message,
        bool showPercentage = true, bool showElapsedTime = true, bool showRemainTime = true, int digit = 1)
    {
        var ratio = Math.Min(1, (double)current / total);
        var elapsedSec = elapsedMilliseconds / 1E3;
        var format = $"f{digit}";

        if (showPercentage) message += $" Completed: {(ratio * 100).ToString(format)} %.";
        if (showElapsedTime) message += $" Elapsed time: {elapsedSec.ToString(format)} sec.";
        if (showRemainTime) message += $" Remaining time: {(elapsedSec / current * (total - current)).ToString(format)} sec.";

        return (ratio, message);
    }
    #endregion

    #region 結晶の追加、削除、変更
    /// <summary>261111Cl 追加: 重い Crystal2.Serialize だけを並列に行い、行の作成 (NewRow・列の設定) と Rows.Add は直列に、配列の順で行う。
    /// 従来は PLINQ のスレッドが CreateRow (列への設定) を並列に、同時に消費側が Rows.Add をしていた (DataTable はスレッド非安全)。</summary>
    private void addRowsInOrder(Crystal2[] crystals)
    {
        var serialized = new byte[crystals.Length][];
        Parallel.For(0, crystals.Length, i => serialized[i] = Crystal2.Serialize(crystals[i]));
        for (int i = 0; i < crystals.Length; i++)
            Table.Rows.Add(Table.CreateRow(crystals[i], serialized[i]));
    }

    public void AddCrystal(Crystal2 crystal2)
    {
        Table.Add(crystal2);
    }

    public void AddCrystals(IEnumerable<Crystal2> crystal2)
    {
        var originalDataMember = dataGridView.DataMember;
        dataGridView.DataMember = "";

        var originalAutoSizeColumnsMode = dataGridView.AutoSizeColumnsMode;
        dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

        var originalAutoSizeRowsMode = dataGridView.AutoSizeRowsMode;
        dataGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;

        // foreach (var c in crystal2.Where(e => e != null))
        //foreach (var r in crystal2.AsParallel().Where(e => e != null).Select(e => Table.CreateRow(e, Crystal2.Serialize(e)))) // 261105Cl 変更前
        //foreach (var r in crystal2.AsParallel().AsOrdered().Where(e => e != null).Select(e => Table.CreateRow(e, Crystal2.Serialize(e)))) // 261105Cl 修正: ReadDatabase と同じく、渡された順に入れる (CSManager の一括取り込みで順番が実行ごとに変わっていた) // 261111Cl 変更前
        //    Table.Rows.Add(r); // 261111Cl 変更前
        addRowsInOrder([.. crystal2.Where(e => e != null)]); // 261111Cl 変更: 渡された順のまま。並列は Serialize だけ

        dataGridView.DataMember = originalDataMember;
        dataGridView.AutoSizeColumnsMode = originalAutoSizeColumnsMode;
        dataGridView.AutoSizeRowsMode = originalAutoSizeRowsMode;
    }

    public void ChangeCrystal(Crystal2 crystal2)
    {
        int i = bindingSource.IndexOf(bindingSource.Current);
        if (i < 0)
            AddCrystal(crystal2);
        else
            Table.Replace(bindingSource.Current, crystal2);
    }

    public void DeleteCurrentCrystal() => bindingSource.RemoveCurrent();

    public void ClearAll() => Table.Clear();
    #endregion

    #region 選択結晶が変更されたとき 
    private void bindingSource_CurrentChanged(object sender, EventArgs e)
    {
        //CrystalChanged?.Invoke(sender, e); // 261105Cl 変更前
        // 261105Cl 変更 (監査 J2): 検索の結果が 0 件になって選択行が無くなったときは通知しない。
        //  以前は Crystal (= null) を受け取った ReciPro・PDIndexer が編集中の結晶を null にし、表示は前の結晶のまま、回転などで例外になった (CSManager は受け側で null を無視していた)
        //if (bindingSource.Current != null) // 261112Cl 変更前
        // 261112Cl 変更: 読み込み・保存の最中 (busy) も通知しない。読み込みの終わりに先頭行が「現在の行」になるだけで CrystalChanged が 2 回出て、
        //  ReciPro の DB 画面 (FormCrystalDatabase) は主画面の結晶を DB の先頭の結晶で黙って置き換えていた (261111Cl 以前からの挙動)。
        //  DB を足したときの自動の検索のやり直し (SearchCrystalControl が suppressCrystalChanged を立てる) でも出さない。
        //  利用者が行を選んだとき・検索ボタンで行が変わったときは従来どおり出す。
        if (bindingSource.Current != null && !busy && !suppressCrystalChanged)
            CrystalChanged?.Invoke(sender, e);
    }

    /// <summary>261112Cl 追加: true の間は CrystalChanged を出さない (SearchCrystalControl の自動の検索のやり直しで使う)。</summary>
    internal bool suppressCrystalChanged;

    public void RecalculateDensityAndFormula()
    {
        var sw = new Stopwatch();
        sw.Restart();
        for (int i = 0; i < dataSet.DataTableCrystalDatabase.Count; i++)
        {
            var c = dataSet.DataTableCrystalDatabase.Get(i).ToCrystal();
            //c.GetFormulaAndDensity();
            dataSet.DataTableCrystalDatabase.Rows[i]["Formula"] = c.ChemicalFormulaSum;
            dataSet.DataTableCrystalDatabase.Rows[i]["Density"] = c.Density;

            if (i % 200 == 0)
            {
                (double progress, string message) = report(i, dataSet.DataTableCrystalDatabase.Count, sw.ElapsedMilliseconds, "Now recalculating Density and Formula. ");

                ProgressChanged?.Invoke(this, progress, message);
                Application.DoEvents();
            }
        }
    }
    #endregion

    #region resizeイベント
    bool registResizeEvent = false;
    private void CrystalDatabaseControl_Resize(object sender, EventArgs e)
    {
        if (!this.DesignMode && !registResizeEvent)
        {
            //var parent = this.Parent;
            //while (parent is not Form && parent != null)
            //    parent = parent.Parent;
            //if (parent == null)
            //    return;
            //var form = parent as Form;
            var form = FindForm(); // 260717Cl: 手書きの親 Form 走査を BCL の Control.FindForm() へ (同一の親走査)
            if (form == null)
                return;
            form.ResizeBegin += (s, ea) => SuspendLayout();
            form.ResizeEnd += (s, ea) => ResumeLayout();
            registResizeEvent = true;
        }
    }
    #endregion


    #region 261111Cl 変更前: チェックボックスのハンドラ (COD は ReadCOD の中と完了待ちで Sleep + DoEvents を回していた)
    //private void checkBoxAMCSD_CheckedChanged(object sender, EventArgs e)
    //{
    //    Enabled = false;
    //    if (checkBoxAMCSD.Checked && !AMCSD_Has_Read)
    //    {
    //        ReadDatabase(UserAppDataPath + "AMCSD.cdb3");
    //        AMCSD_Has_Read = true;
    //    }
    //
    //    DataBaseChanged?.Invoke(sender, e);
    //    Enabled = true;
    //}
    //
    //private void checkBoxCOD_CheckedChanged(object sender, EventArgs e)
    //{
    //    Enabled = false;
    //    var text = textBox1.Text;
    //    if (checkBoxCOD.Checked)
    //    {
    //        textBox1.Text = "Now, loading COD database...";
    //        Application.DoEvents();
    //        if (!COD_Has_Read)
    //        {
    //            if (ReadCOD())
    //            {
    //                COD_Has_Read = true;
    //                do { Thread.Sleep(100); Application.DoEvents(); } while (!Enabled);
    //            }
    //            else
    //            { checkBoxCOD.Checked = false; Enabled = true; }
    //        }
    //    }
    //    DataBaseChanged?.Invoke(sender, e);
    //    textBox1.Text = text;
    //    Enabled = true;
    //
    //}
    #endregion

    // 261111Cl 変更: async void + try/catch。DataBaseChanged (= SearchCrystalControl の検索のやり直し) を出す時機は次のとおり。
    //  ・表が空のとき: 従来どおり、読み込みを始めた直後に出す (表が空なので検索は何もしない)。読み込みの完了後に出すと検索が走って CrystalChanged が 2 回余分に出て、
    //    PDIndexer (読み込み完了の "Total" の後に CrystalChanged を購読する) では編集中の結晶が DB の先頭の結晶に置き換わる (261111Cl ハーネスで確認)。
    //  ・表に行があるとき (COD を読み込み済みなど): 読み込みの完了を待ってから出す。⚠ 挙動の変化: 従来は読み込み中の表を検索が並列に読み、
    //    検索の完了 (Resume) で束縛が、ワーカーが行を追加している最中に戻っていた (DataTable はスレッド非安全)。COD のチェックと同じ順になる。
    //  ・⚠ 挙動の変化: 従来は読み込み中もコントロールを有効へ戻していた。今は読み込みが終わるまで無効のまま。
    private async void checkBoxAMCSD_CheckedChanged(object sender, EventArgs e)
    {
        try
        {
            Enabled = false;
            var loading = Task.CompletedTask;
            if (checkBoxAMCSD.Checked && !AMCSD_Has_Read && !busy) // 別の読み込み等が進行中なら読まない (AMCSD_Has_Read も立てない。チェックを付け直せば読める)
            {
                AMCSD_Has_Read = true; // 従来どおり、読み込みを始めた時点で立てる (失敗しても読み直さない)
                var searchAfterLoading = Table.Rows.Count > 0; // ワーカーが動く前に UI スレッドで数える
                loading = ReadDatabaseAsync(UserAppDataPath + "AMCSD.cdb3");
                if (searchAfterLoading)
                    await loading;
            }

            DataBaseChanged?.Invoke(sender, e);
            await loading;
        }
        catch (Exception ex) { MessageBox.Show($"{ex}\r\nFailed to load database. Sorry."); }
        finally { Enabled = true; }
    }

    // 261111Cl 変更: async void + try/catch。ReadCOD (Sleep + DoEvents の待ち) の代わりに ReadCODAsync を await する。
    private async void checkBoxCOD_CheckedChanged(object sender, EventArgs e)
    {
        var text = textBox1.Text;
        try
        {
            Enabled = false;
            if (checkBoxCOD.Checked)
            {
                textBox1.Text = "Now, loading COD database...";
                //Application.DoEvents(); // 261111Cl 削除: 最初の await で UI スレッドに戻るので、表示はそこで更新される
                if (!COD_Has_Read)
                {
                    if (await ReadCODAsync()) // 読み込みの完了まで待つ (従来の do { Sleep; DoEvents } while (!Enabled) の役目)
                        COD_Has_Read = true;
                    else
                        checkBoxCOD.Checked = false; // このハンドラがもう一度 (チェックなしで) 呼ばれる。従来と同じ
                }
            }
            // 別の読み込み・保存が進行中なら出さない (ReadCODAsync が断ってチェックを外した場合。COD は何も変わっていないので検索し直す必要が無く、
            //  出すと検索が読み込み中の表を並列に読む)。従来はこの場合、AMCSD の読み込みの完了を DoEvents で待ち、COD を読まないまま COD_Has_Read を立てていた
            if (!busy)
                DataBaseChanged?.Invoke(sender, e);
        }
        catch (Exception ex) { MessageBox.Show($"{ex}\r\nFailed to load COD database. Sorry."); }
        finally
        {
            textBox1.Text = text;
            Enabled = true;
        }
    }

    #region 261111Cl 追加: 使わなくなった BackgroundWorker のハンドラ
    // Designer.cs の ReadDatabaseWorker / SaveDatabaseWorker / DownloadCodWorker の配線 (DoWork・RunWorkerCompleted) のためだけに残す空のハンドラ。
    //  これらの BackgroundWorker はもう使わない (RunWorkerAsync を呼ばない)。部品を VS のデザイナーで消したら、ここと worker_ProgressChanged も消してよい。
    private void ReadDatabaseWorker_DoWork(object sender, DoWorkEventArgs e) { }
    private void ReadDatabaseWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) { }
    private void SaveDatabaseWorker_DoWork(object sender, DoWorkEventArgs e) { }
    private void SaveDatabaseWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) { }
    private void DownloadCodWorker_DoWork(object sender, DoWorkEventArgs e) { }
    private void DownloadCodWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) { }
    #endregion
}


