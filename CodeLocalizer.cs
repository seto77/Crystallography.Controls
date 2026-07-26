using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace Crystallography.Controls;

// 260621Cl 追加 (§2.5 横展開): Localization の中央テーブル (型名→項目) に基づき、Localizable=false の
// フォーム/UC が Designer.cs に英語直書きした可視ラベルを実行時に現在の UI カルチャへ差し替える。
// 対象プロパティ: Control.Text / DataGridView 列の HeaderText / ToolStripItem(メニュー)の Text。
// FormBase.OnLoad と UserControlBase.OnLoad から Apply(this) を呼ぶ。デザイン時は何もしない。
// 詳細は .project-guidance/ReciPro/ReciPro_多言語UI保守.md §3-B(方式②)/§12.7。
/// <summary>コード側多言語化テーブル (<see cref="Localization"/>) をコントロールツリーへ適用するヘルパー。</summary>
public static class CodeLocalizer
{
    /// <summary><paramref name="root"/> (Form / UserControl) の型に登録された訳を、配下のコントロール・
    /// メニュー項目・DataGridView 列に適用する。未登録の型・デザイン時は何もしない。</summary>
    public static void Apply(Control root)
    {
        if (root == null || LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            return;
        // 260625Cl: 4アプリ共有の中央レジストリで同名フォーム (PDIndexer.FormMain vs ReciPro.FormMain 等) が
        //   衝突しないよう FullName 優先で引き、無ければ単純名へフォールバック (ReciPro 既存の単純名キーを温存)。
        var t = root.GetType();
        var entries = Localization.Get(t.FullName) ?? Localization.Get(t.Name);
        if (entries == null)
            return;

        var ctrlByName = new Dictionary<string, Control>();
        var itemByName = new Dictionary<string, ToolStripItem>();
        var grids = new List<DataGridView>();
        Collect(root, ctrlByName, itemByName, grids);
        CollectDetachedMenus(root, itemByName);

        foreach (var e in entries)
        {
            if (e.Prop == "HeaderText")
            {
                // 260726Cl 変更: 旧実装は DataGridView 列しか見ておらず、NumericBox / SizeControl /
                //   TrackBarAdvanced / ColorControl のような「HeaderText を持つ自作コントロール」宛の
                //   エントリが 23 件すべて無言で捨てられていた (訳は書かれているのに UI は英語のまま)。
                //   名前付きコントロール優先 → 見つからなければ従来どおり DataGridView 列を探す。
                //   ※ DataGridViewColumn は Control ではないので ctrlByName には入らない = 両者は衝突しない。
                if (ctrlByName.TryGetValue(e.Ctrl, out var hc) && TrySetHeaderText(hc, e.Resolve()))
                    continue;

                var applied = false;
                foreach (var g in grids)
                {
                    var col = FindColumn(g, e.Ctrl);
                    if (col != null) { col.HeaderText = e.Resolve(); applied = true; break; }
                }
                if (!applied)
                    NoteUnresolved(t, e);
            }
            else // "Text"
            {
                if (e.Ctrl == "this")
                    root.Text = e.Resolve();
                else if (ctrlByName.TryGetValue(e.Ctrl, out var c))
                    c.Text = e.Resolve();
                else if (itemByName.TryGetValue(e.Ctrl, out var it))
                    it.Text = e.Resolve();
                else
                    NoteUnresolved(t, e);
            }
        }
    }

    // コントロールツリーを再帰し、名前→Control / 名前→ToolStripItem / DataGridView を収集する。
    // MenuStrip/StatusStrip/ToolStrip (ToolStrip 派生) の Items、各コントロールの ContextMenuStrip、
    // ToolStripDropDownItem の入れ子メニューも辿る (メニュー項目は Controls ツリー外のため別途必要)。
    private static void Collect(Control c, Dictionary<string, Control> ctrlByName,
                                Dictionary<string, ToolStripItem> itemByName, List<DataGridView> grids)
    {
        if (!string.IsNullOrEmpty(c.Name))
            ctrlByName[c.Name] = c;
        if (c is DataGridView dgv)
            grids.Add(dgv);
        if (c is ToolStrip ts)
            CollectItems(ts.Items, itemByName);
        if (c.ContextMenuStrip != null)
            CollectItems(c.ContextMenuStrip.Items, itemByName);
        // 260726Cl: 入れ子の UserControlBase は自分の OnLoad で Apply(this) を呼び、自分の型の訳を自分で当てる。
        //   host 側からも降りていくと「名前一致」で UC 内部の同名コントロールを上書きしてしまい、
        //   どちらか一方が別のラベルの文字になる (Collect が後勝ちで辞書を上書きするため)。
        //   実害例: FormPolycrystallineDiffractionSimulator の tabPage4「Refinement option」が
        //   入れ子 DiffractionPatternControl の tabPage4 に当たり、「模擬パターン|マスク|背景」タブの
        //   見出しの上に別の文字が重なって表示されていた (label28「Scale 1」が UC 側の「°」を潰す例も同様)。
        //   全 11 言語で同じ症状。UC 配下は UC 自身に任せ、host は自分のコントロールだけを対象にする。
        //   既知の穴: UserControlBase を継承しない入れ子 UserControl (IndexControl・GLControlAlpha) は素通りする。
        //   現状それらの内部コントロール名は訳テーブルに 1 件も無いので実害は無い。
        // 260726Cl 修正: ただし **UC インスタンス自身の Name は登録する**。旧実装は UC を丸ごと飛ばしていたため、
        //   NumericBox / SizeControl / TrackBarAdvanced (いずれも UserControlBase 派生) が辞書に載らず、
        //   それらを指す HeaderText エントリ 23 件が到達不能だった。
        //   ここで登録するのは「host の Designer が付けた host スコープの名前」であり、上記の衝突は
        //   UC の *内部* コントロール名 (tabPage4 等) の話なので、この 1 行では再発しない。
        foreach (Control ch in c.Controls)
        {
            if (ch is UserControlBase)
            {
                if (!string.IsNullOrEmpty(ch.Name))
                    ctrlByName[ch.Name] = ch;
            }
            else
                Collect(ch, ctrlByName, itemByName, grids);
        }
    }

    // 260726Cl 追加: 名前付きコントロールが公開する書き込み可能な string HeaderText へ訳を当てる。
    //   NumericBox / SizeControl / TrackBarAdvanced / ColorControl などが該当する。型を列挙せず
    //   リフレクションにしたのは、訳テーブル自体が「コントロール名」で引く緩い設計であり、
    //   HeaderText を持つコントロールが将来増えても追随させる必要がないため。
    //   Apply は OnLoad (UI スレッド) からのみ呼ばれるのでキャッシュは非同期化しない。
    private static readonly Dictionary<System.Type, System.Reflection.PropertyInfo> _headerTextProps = new();

    private static bool TrySetHeaderText(Control c, string value)
    {
        var t = c.GetType();
        if (!_headerTextProps.TryGetValue(t, out var pi))
        {
            pi = t.GetProperty("HeaderText",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (pi != null && (pi.PropertyType != typeof(string) || !pi.CanWrite))
                pi = null;
            _headerTextProps[t] = pi;
        }
        if (pi == null)
            return false;
        pi.SetValue(c, value);
        return true;
    }

    // 260726Cl 追加: 当てられなかったエントリを記録する。HeaderText が 23 件も黙って捨てられていたのに
    //   1 か月以上気づけなかったのは、失敗が完全に無言だったため。--diagnose 等から拾えるようにしておく。
    private static readonly List<string> _unresolved = new();

    /// <summary>訳を当てられなかったエントリ ("型名.コントロール名.プロパティ")。既定では空。</summary>
    public static IReadOnlyList<string> UnresolvedEntries => _unresolved;

    private static void NoteUnresolved(System.Type root, Localization.Entry e)
    {
        var key = $"{root.Name}.{e.Ctrl}.{e.Prop}";
        if (_unresolved.Contains(key))
            return;
        _unresolved.Add(key);
        System.Diagnostics.Debug.WriteLine($"[CodeLocalizer] 未解決エントリ: {key}");
    }

    // 260726Cl 追加: どのコントロールの ContextMenuStrip プロパティにも割り当てられていない
    //   ContextMenuStrip を root のフィールドから拾う。Designer が作った菜単でも、コード側が
    //   `menu.Show(pictureBox, x, y)` で自前表示する流儀だと Control.ContextMenuStrip が null のままで、
    //   コントロールツリーからは到達できない。
    //   実害: GraphControl の右クリックメニュー (Log scale X/Y・Scale line X/Y) が全 11 言語で英語のままだった
    //   (--diagnose の未解決エントリ報告で発覚)。root だけを見るので走査コストは Apply あたり 1 回。
    private static void CollectDetachedMenus(Control root, Dictionary<string, ToolStripItem> itemByName)
    {
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
                  | System.Reflection.BindingFlags.NonPublic;
        foreach (var f in root.GetType().GetFields(flags))
        {
            if (!typeof(ContextMenuStrip).IsAssignableFrom(f.FieldType))
                continue;
            if (f.GetValue(root) is ContextMenuStrip cms)
                CollectItems(cms.Items, itemByName);
        }
    }

    private static void CollectItems(ToolStripItemCollection items, Dictionary<string, ToolStripItem> map)
    {
        foreach (ToolStripItem it in items)
        {
            if (!string.IsNullOrEmpty(it.Name))
                map[it.Name] = it;
            if (it is ToolStripDropDownItem ddi && ddi.HasDropDownItems)
                CollectItems(ddi.DropDownItems, map);
        }
    }

    private static DataGridViewColumn FindColumn(DataGridView g, string name)
    {
        foreach (DataGridViewColumn col in g.Columns)
            if (col.Name == name)
                return col;
        return null;
    }
}
