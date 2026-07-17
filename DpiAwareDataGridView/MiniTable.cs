using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices; // 260715Cl 追加 (Clipboard 失敗時の ExternalException)
using System.Text; // 260715Cl 追加 (コピー文字列の組み立て)
using System.Windows.Forms;
using XamlMath;
using static Crystallography.Localization; // 260715Cl 追加 (コピーメニュー文言の方式② Loc)

namespace Crystallography.Controls;

/// <summary>
/// 数行×数列の「読み取り専用ミニ表」用 DataGridView 派生コントロール。260606Cl 追加。
/// </summary>
/// <remarks>
/// 設計方針 (260606Cl):
/// - <see cref="DpiAwareDataGridView"/> を継承し、構築時点で「読み取り専用・選択なし・スクロールなし・静かな見た目」になる。
///   呼び出し側で MakeQuiet 相当を呼ぶ必要はない (派生による自己構成)。
/// - 配色 / CellStyle / 罫線は本クラスに <b>一元化</b>し、消費側フォームでは設定させない (該当プロパティはデザイナから非表示)。
/// - 表示専用テーブルに無関係な継承プロパティ (AllowUserTo* / ReadOnly / DataSource / 各種 CellStyle 等) は
///   <c>[Browsable(false)]</c> でデザイナのプロパティグリッドから隠す (下部 region)。公開するのは <see cref="Selectable"/> /
///   <see cref="AutoFitHeight"/> など、表示専用テーブルとして意味のあるものだけ。
/// - データ投入は DataSet/バインドを使わず <see cref="SetRows"/> (丸ごと差替) / <see cref="AddRow"/> (1 行追加) で object[] を渡す。
/// - 列ヘッダーをローカライズする表は、列を <b>デザイナで定義</b>する (header/Alignment/Format は列側で設定 → resources.ApplyResources +
///   .resx/.ja.resx の既存翻訳機構に乗る)。記号のみで翻訳不要の表は <see cref="SetColumns"/> でコード生成してもよい。
///   260709Cl 追記: resx を持たずコード側 Loc() で全ラベルをローカライズするフォーム (FormGroupRelations 等、方式②) では、
///   ヘッダーに Loc() の戻り値を渡して <see cref="SetColumns"/> でコード生成する。「デザイナで定義」は resx 機構に乗る
///   フォーム向けの指針であり、方式②のフォームにデザイナ列定義を強制しない (どちらも正)。
/// - DPI 列幅・ヘッダ中央寄せは基底 <see cref="DpiAwareDataGridView"/> 任せ。列を AutoSize にすると基底の列幅 DPI 計算と二重化しない。
/// </remarks>
[ToolboxItem(true)]
public class MiniTable : DpiAwareDataGridView
{
    private const string DefaultDesignTimePreviewText = "Sample";
    private const string DefaultDesignTimePreviewLatexText = @"x+1/2";
    //private static readonly Padding DefaultCellPaddingValue = new(4, 0, 4, 0); // 260708Ch: LaTeX の上下切れを避けるため縦余白も既定化
    private static readonly Padding DefaultCellPaddingValue = new(4, 3, 4, 3); // 260708Ch

    public MiniTable()
    {
        // 構造系は構築時に固定 (デザイン画面にもミニ表の姿で反映される)。
        base.ReadOnly = true;
        base.AllowUserToAddRows = false;
        base.AllowUserToDeleteRows = false;
        base.AllowUserToResizeRows = false;
        base.AllowUserToResizeColumns = false;
        base.AllowUserToOrderColumns = false;
        base.RowHeadersVisible = false;
        base.MultiSelect = false;
        base.VirtualMode = false;
        base.ScrollBars = ScrollBars.None;
        base.BorderStyle = BorderStyle.None;
        base.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        base.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None; // 列ごとの AutoSizeMode を使う
        //base.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells; // 260708Ch: ManualRowHeight=-1/0/1+ で自動/手動を切替可能にする
        ApplyRowHeightSettings(updateContainerHeight: false); // 260708Ch
        base.TabStop = false;
        ApplyCellPadding(); // 260708Ch: 自動列幅にも反映されるセル内余白を既定適用
        DoubleBuffered = true; // 260715Cl 追加: コピー範囲ハイライトの点灯/消灯で全面 Invalidate するためちらつき防止
    }

    #region 公開オプション (表示専用テーブルとして意味のあるものだけ)

    private bool selectable;
    /// <summary>1 行ハイライト (選択) を残すか。既定 false (表示専用で選択色を地色に潰す)。260606Cl 追加。</summary>
    [DefaultValue(false), Category("MiniTable")]
    public bool Selectable
    {
        get => selectable;
        set { selectable = value; if (IsHandleCreated) ApplyTheme(); }
    }

    /// <summary><see cref="SetRows"/> 後にコントロール高さを「ヘッダ + 全行」にフィットさせるか。既定 false。260606Cl 追加。</summary>
    [DefaultValue(false), Category("MiniTable")]
    public bool AutoFitHeight { get; set; }

    /// <summary><see cref="SetRows"/> 後にコントロール幅を「全列の内容幅」にフィットさせるか。既定 false。260607Cl 追加 (実験的)。</summary>
    /// <remarks>内容フィット (AllCells) 主体の表向け。Fill 列を含む表は親幅に追従させる設計なので噛み合わない (無効化推奨)。</remarks>
    [DefaultValue(false), Category("MiniTable")]
    public bool AutoFitWidth { get; set; }

    //private bool autoRowHeight = true; // 260708Ch: ManualRowHeight=-1 を Auto とする仕様へ統合
    ///// <summary>行高を内容に合わせて自動調整するか。false のとき <see cref="ManualRowHeight"/> を使う。260708Ch 追加。</summary>
    //[DefaultValue(true), Category("MiniTable")]
    //[Description("行高を内容に合わせて自動調整します。false のときは ManualRowHeight の固定行高を使います。")]
    //public bool AutoRowHeight
    //{
    //    get => autoRowHeight;
    //    set
    //    {
    //        if (autoRowHeight == value) return;
    //        autoRowHeight = value;
    //        ApplyRowHeightSettings();
    //    }
    //}

    private int manualRowHeight = -1;
    /// <summary>-1 のとき内容に合わせた自動行高、0 のとき RowTemplate.Height、1 以上のとき固定行高 (px)。260708Ch 追加。</summary>
    //[DefaultValue(0), Category("MiniTable")] // 260708Ch: -1=Auto / 0=RowTemplate.Height / 1+=固定値へ仕様変更
    [DefaultValue(-1), Category("MiniTable")]
    [Description("-1 のとき内容に合わせた自動行高、0 のとき RowTemplate.Height、1 以上のとき指定ピクセルの固定行高にします。")]
    public int ManualRowHeight
    {
        get => manualRowHeight;
        set
        {
            //var normalized = Math.Max(0, value); // 260708Ch: -1 を Auto として許可
            var normalized = Math.Max(-1, value); // 260708Ch
            if (manualRowHeight == normalized) return;
            manualRowHeight = normalized;
            ApplyRowHeightSettings();
        }
    }

    private Padding cellPadding = DefaultCellPaddingValue;
    /// <summary>セル内容の内側余白。AutoSize 列幅にも反映される。260708Ch 追加。</summary>
    //[DefaultValue(typeof(Padding), "4, 0, 4, 0"), Category("MiniTable")] // 260708Ch: 上下方向の既定余白を追加
    [DefaultValue(typeof(Padding), "4, 3, 4, 3"), Category("MiniTable")]
    [Description("セル内容の内側余白を指定します。左右の値を大きくすると自動列幅にも余白が加わります。")]
    public Padding CellPadding
    {
        get => cellPadding;
        set
        {
            var normalized = new Padding(
                Math.Max(0, value.Left),
                Math.Max(0, value.Top),
                Math.Max(0, value.Right),
                Math.Max(0, value.Bottom));
            if (cellPadding.Equals(normalized)) return;
            cellPadding = normalized;
            ApplyCellPadding();
            if (Rows.Count > 0 && IsHandleCreated)
            {
                foreach (DataGridViewColumn column in Columns) // 260708Ch: 余白変更時に内容幅列を再測定
                    if (column.Visible && column.AutoSizeMode == DataGridViewAutoSizeColumnMode.AllCells)
                        AutoResizeColumn(column.Index, DataGridViewAutoSizeColumnMode.AllCells);
                ApplyRowHeightSettings(); // 260708Ch: 行高適用+コンテナ高さ更新をここで一括 (FitHeightToRows 二重呼び出しを解消)
                if (AutoFitWidth)
                    FitWidthToColumns();
            }
            Invalidate();
        }
    }

    private bool allowVerticalScroll;
    /// <summary>行数がコントロール高さを超えるとき縦スクロールバーを許可するか (opt-in)。既定 false (=表示専用で非表示)。
    /// 固定高さのセルに置き行数が増減する表 (Beam Interaction のスカラ/線表など) で true にする。260606Cl 追加。</summary>
    [DefaultValue(false), Category("MiniTable")]
    public bool AllowVerticalScroll
    {
        get => allowVerticalScroll;
        set { allowVerticalScroll = value; base.ScrollBars = value ? ScrollBars.Vertical : ScrollBars.None; }
    }

    private double latexThickness = 0.6;
    /// <summary>LaTeX セルの縁取り太さ (device-independent pixel)。0 のとき通常描画。260707Ch 追加。</summary>
    [DefaultValue(0.6D), Category("MiniTable LaTeX")]
    [Description("LaTeX セルの縁取り太さを device-independent pixel 単位で指定します。0 のときは通常描画です。")]
    public double LatexThickness
    {
        get => latexThickness;
        set
        {
            var normalized = Math.Max(0.0, value);
            if (latexThickness == normalized) return;
            latexThickness = normalized;
            ApplyLatexOptionsToColumns();
        }
    }

    private float latexFontSizeInPoints;
    /// <summary>LaTeX セルだけに使う文字サイズ (pt)。0 以下なら MiniTable/セルの Font を使う。260707Ch 追加。</summary>
    [DefaultValue(0f), Category("MiniTable LaTeX")]
    [Description("LaTeX セルだけに使う文字サイズ (pt) を指定します。0 のときは MiniTable/セルの Font を使います。")]
    public float LatexFontSizeInPoints
    {
        get => latexFontSizeInPoints;
        set
        {
            var normalized = Math.Max(0f, value);
            if (Math.Abs(latexFontSizeInPoints - normalized) < 0.01f) return;
            latexFontSizeInPoints = normalized;
            ApplyLatexOptionsToColumns();
        }
    }

    private TexStyle latexTexStyle = TexStyle.Display;
    /// <summary>LaTeX セルの TeX style。260707Ch 追加。</summary>
    [DefaultValue(TexStyle.Display), Category("MiniTable LaTeX")]
    [Description("LaTeX セルの TeX style を指定します。")]
    public TexStyle LatexTexStyle
    {
        get => latexTexStyle;
        set
        {
            if (latexTexStyle == value) return;
            latexTexStyle = value;
            ApplyLatexOptionsToColumns();
        }
    }

    private LatexFractionStyle latexFractionStyle = LatexFractionStyle.Horizontal;
    /// <summary>LaTeX セル中の "1/2" 形式の分数を横・縦・斜めのどれで描くか。260707Ch 追加。</summary>
    [DefaultValue(LatexFractionStyle.Horizontal), Category("MiniTable LaTeX")]
    [Description("LaTeX セル中の 1/2 形式の分数を Horizontal, Vertical, Slanted のどれで描くか指定します。")]
    public LatexFractionStyle LatexFractionStyle
    {
        get => latexFractionStyle;
        set
        {
            if (latexFractionStyle == value) return;
            latexFractionStyle = value;
            ApplyLatexOptionsToColumns();
        }
    }

    private bool designTimePreviewEnabled = true;
    /// <summary>デザイナ上だけダミー行を描画するか。実行時データや Designer.cs には影響しない。260707Ch 追加。</summary>
    [DefaultValue(true), Category("MiniTable Design")]
    [Description("デザイン時だけダミー行を描画します。Rows/Columns には追加しないため実行時データには影響しません。")]
    public bool DesignTimePreviewEnabled
    {
        get => designTimePreviewEnabled;
        set { if (designTimePreviewEnabled == value) return; designTimePreviewEnabled = value; Invalidate(); }
    }

    private string designTimePreviewText = DefaultDesignTimePreviewText;
    /// <summary>デザイナ上の通常セルに表示するダミー文字列。260707Ch 追加。</summary>
    [DefaultValue(DefaultDesignTimePreviewText), Category("MiniTable Design")]
    [Description("デザイン時プレビューの通常セルに表示するダミー文字列を指定します。")]
    public string DesignTimePreviewText
    {
        get => designTimePreviewText;
        set { designTimePreviewText = value ?? string.Empty; Invalidate(); }
    }

    private string designTimePreviewLatexText = DefaultDesignTimePreviewLatexText;
    /// <summary>デザイナ上の LaTeX セルに表示するダミー数式。260707Ch 追加。</summary>
    [DefaultValue(DefaultDesignTimePreviewLatexText), Category("MiniTable Design")]
    [Description("デザイン時プレビューの LaTeX セルに表示するダミー数式を指定します。")]
    public string DesignTimePreviewLatexText
    {
        get => designTimePreviewLatexText;
        set { designTimePreviewLatexText = value ?? string.Empty; Invalidate(); }
    }

    #endregion

    #region 配色 / CellStyle の一元化

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e); // 基底: DPI 列幅スケーリング + ヘッダ中央寄せ
        ApplyTheme();
    }

    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        if (IsHandleCreated)
            base.BackgroundColor = ResolveBackground();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (ShouldDrawDesignTimePreview())
            DrawDesignTimePreview(e.Graphics); // 260707Ch: Rows を増やさずデザイン時だけ見本行を重ね描き
        DrawCopyHighlight(e.Graphics); // 260715Cl 追加: コピーメニュー表示中の対象範囲ハイライト
    }

    /// <summary>
    /// DataGridView.BackgroundColor は不透明色必須。親が透明 (例: 視覚スタイル下の TabPage は BackColor=Transparent) の場合は
    /// 例外になるため Control にフォールバックする。260606Cl 追加。
    /// </summary>
    private Color ResolveBackground()
    {
        var c = Parent?.BackColor ?? SystemColors.Control;
        return c.A == 255 ? c : SystemColors.Control;
    }

    /// <summary>配色・CellStyle・選択色をここ 1 箇所で適用する (デザイナから上書きさせない)。260606Cl 追加。</summary>
    private void ApplyTheme()
    {
        base.GridColor = SystemColors.ControlLight;
        base.BackgroundColor = ResolveBackground();
        base.TabStop = Selectable;

        base.DefaultCellStyle.BackColor = SystemColors.Window;
        base.DefaultCellStyle.ForeColor = SystemColors.ControlText;

        // 控えめな交互行色 (ハイコントラストでは無効化)
        var alt = SystemInformation.HighContrast ? SystemColors.Window : Color.FromArgb(248, 248, 248);
        base.AlternatingRowsDefaultCellStyle.BackColor = alt;

        if (Selectable)
        {
            base.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            base.DefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;
            base.AlternatingRowsDefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            base.AlternatingRowsDefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;
        }
        else
        {
            base.DefaultCellStyle.SelectionBackColor = base.DefaultCellStyle.BackColor;
            base.DefaultCellStyle.SelectionForeColor = base.DefaultCellStyle.ForeColor;
            base.AlternatingRowsDefaultCellStyle.SelectionBackColor = alt;
            base.AlternatingRowsDefaultCellStyle.SelectionForeColor = base.DefaultCellStyle.ForeColor;
        }
        ApplyCellPadding(); // 260708Ch: ApplyTheme 後も MiniTable.CellPadding を維持
    }

    private void ApplyCellPadding()
    {
        // 260708Ch: CellPadding はデザイナ値=96dpi論理値として扱い、実行時 DPI に応じてスケールする
        // (DpiAwareDataGridView が列幅/行ヘッダ幅にしている処理と同じ考え方。Padding は 0 を許すため
        // 列幅用の FromLogicalPixels の「最小 1px」floor はそのまま使わず ScaleForDpi で個別に丸める)。
        var dpi = CurrentDpi;
        var scaled = new Padding(
            ScaleForDpi(CellPadding.Left, dpi),
            ScaleForDpi(CellPadding.Top, dpi),
            ScaleForDpi(CellPadding.Right, dpi),
            ScaleForDpi(CellPadding.Bottom, dpi));
        base.DefaultCellStyle.Padding = scaled; // 260708Ch
        base.AlternatingRowsDefaultCellStyle.Padding = scaled; // 260708Ch
    }

    /// <summary>96dpi 論理ピクセルを現在 DPI の物理ピクセルへ変換する。0 はそのまま 0 を返す (Padding 用途で
    /// 「余白なし」を維持するため、列幅用の DpiAwareDataGridView.FromLogicalPixels の「最小 1px」floor は適用しない)。260708Ch 追加。</summary>
    private static int ScaleForDpi(int logicalPixels, int dpi)
        => logicalPixels <= 0 ? logicalPixels : Math.Max(1, (int)Math.Round(logicalPixels * dpi / 96.0));

    private void ApplyRowHeightSettings(bool updateContainerHeight = true)
    {
        //base.AutoSizeRowsMode = AutoRowHeight ? DataGridViewAutoSizeRowsMode.AllCells : DataGridViewAutoSizeRowsMode.None; // 260708Ch: AutoRowHeight 廃止
        var auto = ManualRowHeight < 0; // 260708Ch
        base.AutoSizeRowsMode = auto ? DataGridViewAutoSizeRowsMode.AllCells : DataGridViewAutoSizeRowsMode.None; // 260708Ch
        if (!auto)
            ApplyManualRowHeights();
        else if (Rows.Count > 0 && IsHandleCreated)
            AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells); // 260708Ch

        // 260708Ch: 行高は上で適用済みなので、コンテナ高さの再計算だけ行う (FitHeightToRows を呼ぶと
        // AutoResizeRows/ApplyManualRowHeights が二重に走っていた)。
        if (updateContainerHeight && AutoFitHeight && Rows.Count > 0 && IsHandleCreated)
            AdjustContainerHeightToRows();
        Invalidate();
    }

    /// <summary>ManualRowHeight&gt;0 なら固定値、0 なら RowTemplate.Height を使う実効行高。260708Ch 追加 (3 箇所の重複計算式を集約)。</summary>
    private int ResolveFixedRowHeight() => Math.Max(1, ManualRowHeight > 0 ? ManualRowHeight : RowTemplate.Height);

    private void ApplyManualRowHeights()
    {
        var height = ScaleForDpi(ResolveFixedRowHeight(), CurrentDpi); // 260708Ch: ManualRowHeight もデザイナ値=96dpi論理値として扱う
        base.RowTemplate.Height = height; // 260708Ch
        foreach (DataGridViewRow row in Rows)
            if (!row.IsNewRow)
                row.Height = height; // 260708Ch
    }

    private void ApplyLatexOptionsToColumns()
    {
        foreach (DataGridViewColumn column in Columns)
            if (column.CellTemplate is DataGridViewLatexTextBoxCell)
                //DataGridViewLatexTextBoxCell.ApplyToColumn(column, LatexThickness, LatexTexStyle, LatexFontSizeInPoints); // 260707Ch: 分数表記プロパティも適用
                DataGridViewLatexTextBoxCell.ApplyToColumn(column, LatexThickness, LatexTexStyle, LatexFontSizeInPoints, LatexFractionStyle); // 260707Ch

        if (Rows.Count > 0 && IsHandleCreated)
        {
            ApplyRowHeightSettings(); // 260708Ch: 行高適用+コンテナ高さ更新をここで一括
            if (AutoFitWidth)
                FitWidthToColumns();
        }
        Invalidate();
    }

    #endregion

    #region デザイン時プレビュー

    private bool ShouldDrawDesignTimePreview()
        => DesignTimePreviewEnabled
        && Rows.Count == 0
        && !IsDisposed
        && (DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime || Site?.DesignMode == true);

    private void DrawDesignTimePreview(Graphics g)
    {
        var visibleColumns = VisibleColumnsInDisplayOrder(); //260715Cl 変更: 可視列の列挙をコピー系ヘルパーと共通化
        //var visibleColumns = Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).OrderBy(c => c.DisplayIndex).ToArray();//260715Cl 変更前
        var rowHeight = ManualRowHeight >= 0
            ? ScaleForDpi(ResolveFixedRowHeight(), CurrentDpi) // 260708Ch: 実行時と同じ DPI スケールをデザイン時プレビューにも適用
            : Math.Max(RowTemplate.Height, Font.Height + 8); // 260708Ch
        var top = ColumnHeadersVisible ? ColumnHeadersHeight : 0;

        if (visibleColumns.Length == 0)
        {
            DrawDesignTimePreviewWithoutColumns(g, rowHeight);
            return;
        }

        var rowCount = Math.Min(2, Math.Max(0, (ClientSize.Height - top) / Math.Max(1, rowHeight)));
        for (int row = 0; row < rowCount; row++)
        {
            foreach (var column in visibleColumns)
            {
                var columnBounds = GetColumnDisplayRectangle(column.Index, true);
                if (columnBounds.Width <= 0) continue;
                var cellBounds = new Rectangle(columnBounds.Left, top + row * rowHeight, columnBounds.Width, rowHeight);
                DrawDesignTimePreviewCell(g, cellBounds, column, row);
            }
        }
    }

    private void DrawDesignTimePreviewWithoutColumns(Graphics g, int rowHeight)
    {
        var width = ClientSize.Width;
        if (width <= 4 || ClientSize.Height <= 4) return;

        var headerHeight = ColumnHeadersVisible ? ColumnHeadersHeight : 0;
        var firstWidth = Math.Max(40, width / 2);
        var columns = new[]
        {
            new Rectangle(0, 0, firstWidth, headerHeight),
            new Rectangle(firstWidth, 0, Math.Max(1, width - firstWidth), headerHeight),
        };

        if (headerHeight > 0)
        {
            DrawPreviewHeader(g, columns[0], "Text");
            DrawPreviewHeader(g, columns[1], "LaTeX");
        }

        var rowCount = Math.Min(2, Math.Max(0, (ClientSize.Height - headerHeight) / Math.Max(1, rowHeight)));
        for (int row = 0; row < rowCount; row++)
        {
            DrawPreviewCell(g, new Rectangle(columns[0].Left, headerHeight + row * rowHeight, columns[0].Width, rowHeight), DesignTimePreviewText, false, DefaultCellStyle.Alignment, Font, ForeColor, row);
            DrawPreviewCell(g, new Rectangle(columns[1].Left, headerHeight + row * rowHeight, columns[1].Width, rowHeight), DesignTimePreviewLatexText, true, DataGridViewContentAlignment.MiddleLeft, Font, ForeColor, row);
        }
    }

    private void DrawDesignTimePreviewCell(Graphics g, Rectangle bounds, DataGridViewColumn column, int row)
    {
        var style = column.DefaultCellStyle;
        var alignment = style.Alignment == DataGridViewContentAlignment.NotSet ? DefaultCellStyle.Alignment : style.Alignment;
        if (alignment == DataGridViewContentAlignment.NotSet)
            alignment = DataGridViewContentAlignment.MiddleLeft;

        var font = style.Font ?? DefaultCellStyle.Font ?? Font;
        var foreColor = ResolveStyleColor(style.ForeColor, ResolveStyleColor(DefaultCellStyle.ForeColor, ForeColor));
        var latex = column.CellTemplate is DataGridViewLatexTextBoxCell;
        DrawPreviewCell(g, bounds, latex ? DesignTimePreviewLatexText : DesignTimePreviewText, latex, alignment, font, foreColor, row);
    }

    private void DrawPreviewHeader(Graphics g, Rectangle bounds, string text)
    {
        if (bounds.Height <= 0 || bounds.Width <= 0) return;

        using var back = new SolidBrush(SystemColors.Control);
        using var pen = new Pen(GridColor);
        g.FillRectangle(back, bounds);
        g.DrawRectangle(pen, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1);
        TextRenderer.DrawText(g, text, Font, Rectangle.Inflate(bounds, -3, -1), SystemColors.ControlText,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private void DrawPreviewCell(Graphics g, Rectangle bounds, string text, bool latex, DataGridViewContentAlignment alignment, Font font, Color foreColor, int row)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var backColor = row % 2 == 0
            ? ResolveStyleColor(DefaultCellStyle.BackColor, SystemColors.Window)
            : ResolveStyleColor(AlternatingRowsDefaultCellStyle.BackColor, ResolveStyleColor(DefaultCellStyle.BackColor, SystemColors.Window));

        using var back = new SolidBrush(backColor);
        using var pen = new Pen(GridColor);
        g.FillRectangle(back, bounds);
        g.DrawRectangle(pen, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1);

        //var contentBounds = Rectangle.Inflate(bounds, -4, -3); // 260708Ch: デザイン時プレビューも MiniTable.CellPadding に追従
        var contentBounds = new Rectangle( // 260708Ch
            bounds.Left + CellPadding.Left,
            bounds.Top + CellPadding.Top,
            Math.Max(0, bounds.Width - CellPadding.Horizontal),
            Math.Max(0, bounds.Height - CellPadding.Vertical));
        if (latex && TryDrawPreviewLatex(g, text, font, foreColor, contentBounds, alignment))
            return;

        TextRenderer.DrawText(g, text ?? string.Empty, font, contentBounds, foreColor, GetTextFormatFlags(alignment));
    }

    private bool TryDrawPreviewLatex(Graphics g, string text, Font font, Color foreColor, Rectangle bounds, DataGridViewContentAlignment alignment)
    {
        if (string.IsNullOrWhiteSpace(text) || bounds.Width <= 0 || bounds.Height <= 0)
            return false;

        var latexFont = DataGridViewLatexTextBoxCell.ResolveFont(font, LatexFontSizeInPoints, out var disposeFont); // 260708Ch: DataGridViewLatexTextBoxCell と共有の静的ヘルパーに統合
        Bitmap bitmap = null;
        try
        {
            bitmap = LabelLaTeX.RenderLatexBitmap(DataGridViewLatexTextBoxCell.FormatFractions(text, LatexFractionStyle), latexFont, foreColor, CurrentDpi, LatexTexStyle, LatexThickness); // 260707Ch
            if (bitmap == null)
                return false;
            g.DrawImage(bitmap, DataGridViewLatexTextBoxCell.Align(bitmap.Size, bounds, alignment)); // 260717Cl: 重複していた Align を共有版へ
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            bitmap?.Dispose();
            if (disposeFont)
                latexFont.Dispose();
        }
    }

    private static Color ResolveStyleColor(Color color, Color fallback)
        => color.IsEmpty ? fallback : color;

    private static TextFormatFlags GetTextFormatFlags(DataGridViewContentAlignment alignment)
    {
        var flags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter;
        flags |= alignment switch
        {
            DataGridViewContentAlignment.TopCenter or DataGridViewContentAlignment.MiddleCenter or DataGridViewContentAlignment.BottomCenter
                => TextFormatFlags.HorizontalCenter,
            DataGridViewContentAlignment.TopRight or DataGridViewContentAlignment.MiddleRight or DataGridViewContentAlignment.BottomRight
                => TextFormatFlags.Right,
            _ => TextFormatFlags.Left,
        };
        return flags;
    }

    // 260717Cl: DataGridViewLatexTextBoxCell.Align と一字一句同一だったため削除し、共有版 (internal 化) へ一本化。
    //private static Rectangle Align(Size content, Rectangle bounds, DataGridViewContentAlignment alignment) { … }

    #endregion

    #region データ投入 (DataSet/バインド不使用)

    /// <summary>1 行を追加する。値は double/int/string を並べて渡す。260606Cl 追加。</summary>
    /// <remarks>例: <c>AddRow("Fe", 26, 26.0, -1.13, 3.20)</c> あるいは <c>AddRow(objectArray)</c>。返り値は追加行の index。
    /// 書式・右寄せは列の DefaultCellStyle が効く。NaN/Infinity はそのまま文字列化される (空欄は null/"" を渡す)。</remarks>
    public int AddRow(params object[] values)
    {
        if (values == null || (Columns.Count > 0 && values.Length != Columns.Count))
            throw new ArgumentException("Row value count does not match column count.", nameof(values));
        var index = Rows.Add(values); // 260708Ch
        if (ManualRowHeight >= 0) // 260708Ch
            Rows[index].Height = ScaleForDpi(ResolveFixedRowHeight(), CurrentDpi); // 260708Ch
        return index; // 260708Ch
    }

    /// <summary>全行を <paramref name="rows"/> で丸ごと差し替える (結晶切替などのたびに呼ぶ)。260606Cl 追加。</summary>
    public void SetRows(IEnumerable<object[]> rows)
    {
        SuspendLayout();
        var savedAutoSizeRowsMode = base.AutoSizeRowsMode; // 260709Cl: 投入中の行ごと autosize を止める (最後に ApplyRowHeightSettings が一括適用)
        try
        {
            base.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None; // 260709Cl
            Rows.Clear();
            //foreach (var row in rows)
            //{
            //    if (row.Length != Columns.Count)
            //        throw new ArgumentException("Row value count does not match column count.", nameof(rows));
            //    Rows.Add(row);
            //}
            // 260709Cl: 1 行ずつ Rows.Add すると AllCells 自動列幅・自動行高の再計算が行ごとに走り O(n²) になる
            // (行数の多い表で顕在化)。DataGridViewRow を先に構築して AddRange で一括投入する (再計算は 1 回)。
            var buf = new List<DataGridViewRow>();
            foreach (var row in rows)
            {
                if (row.Length != Columns.Count)
                    throw new ArgumentException("Row value count does not match column count.", nameof(rows));
                var r = new DataGridViewRow();
                r.CreateCells(this, row);
                buf.Add(r);
            }
            Rows.AddRange([.. buf]);
            ClearSelection();
            CurrentCell = null;
        }
        finally
        {
            base.AutoSizeRowsMode = savedAutoSizeRowsMode; // 260709Cl
            ResumeLayout();
        }
        ApplyRowHeightSettings(); // 260708Ch: 行高適用+コンテナ高さ更新をここで一括 (FitHeightToRows 二重呼び出しを解消)
        if (AutoFitWidth)
            FitWidthToColumns();
    }

    /// <summary>全行を消す。260606Cl 追加。</summary>
    public void ClearRows() => Rows.Clear();

    /// <summary>コントロール高さを「ヘッダ + 全行」に縮める (ScrollBars=None 前提)。手動 <see cref="AddRow"/> 後は明示的に呼ぶ。260606Cl 追加。</summary>
    public void FitHeightToRows()
    {
        if (ManualRowHeight < 0) // 260708Ch
            AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells); // 260708Ch
        else
            ApplyManualRowHeights(); // 260708Ch
        AdjustContainerHeightToRows();
    }

    /// <summary>行高の再適用はせず、コントロール高さだけを現在の行高構成に合わせる。260708Ch 追加
    /// (ApplyRowHeightSettings が行高適用の直後に呼ぶための分離。FitHeightToRows と二重に行高を適用しないため)。</summary>
    private void AdjustContainerHeightToRows()
    {
        var chrome = Height - ClientSize.Height; // 枠ぶん
        Height = chrome
            + (ColumnHeadersVisible ? ColumnHeadersHeight : 0)
            + Rows.GetRowsHeight(DataGridViewElementStates.Visible)
            + 2;
    }

    /// <summary>コントロール幅を「現在の全 (可視) 列幅の合計」に合わせる (横スクロールなし前提)。<see cref="FitHeightToRows"/> の幅版。
    /// 手動 <see cref="AddRow"/> 後は明示的に呼ぶ。260607Cl 追加 (実験的)。</summary>
    /// <remarks>
    /// 各列は自身の <see cref="DataGridViewColumn.AutoSizeMode"/> 通りに既に幅が決まっており、本メソッドはその合計に枠を足して
    /// コントロール幅へ反映するだけ。よって列ごとの指定がそのまま効く:
    /// <list type="bullet">
    /// <item>None + Width=N : 絶対 N px を固定で寄与。</item>
    /// <item>AllCells : 内容幅で寄与 (この合計に縮める対象)。</item>
    /// <item>Fill : 「残り幅を埋める」ため常にコントロール幅へ追従し合計は client 幅と一致 → 縮まない。Fill 列を含む表では無効化推奨。</item>
    /// </list>
    /// 一律内容幅に潰す <c>AutoResizeColumns(AllCells)</c> は None の絶対幅や Fill の按分を壊すため呼ばない。
    /// </remarks>
    public void FitWidthToColumns()
    {
        var chrome = Width - ClientSize.Width; // 枠 + (表示中の) 縦スクロールバー
        Width = chrome
            + (RowHeadersVisible ? RowHeadersWidth : 0)
            + Columns.GetColumnsWidth(DataGridViewElementStates.Visible)
            + 2;
    }

    #endregion

    #region コピー用コンテキストメニュー (右クリック) 260715Cl 追加

    private enum CopyHighlightScope { None, Cell, Row, Table }

    private ContextMenuStrip copyMenu;
    private ToolStripMenuItem copyCellItem, copyCellPlainItem, copyRowItem, copyRowPlainItem,
        copyTableItem, copyTablePlainItem, copyTableTabularItem, copyTableImageItem;
    private int contextRow = -1, contextColumn = -1; // 右クリックされたセル位置 (セル外は -1)
    private CopyHighlightScope highlightScope;

    /// <summary>右クリックでクリップボードへのコピーメニューを表示するか。260715Cl 追加。</summary>
    /// <remarks>消費側フォームが <see cref="Control.ContextMenuStrip"/> を独自に設定している場合、内蔵メニューはそちらへ譲って表示しない。</remarks>
    [DefaultValue(true), Category("MiniTable")]
    [Description("右クリックでクリップボードへのコピーメニューを表示します。ContextMenuStrip を独自に設定した場合は表示されません。")]
    public bool EnableCopyContextMenu { get; set; } = true;

    protected override void OnMouseUp(MouseEventArgs e) // コンテキストメニューの Windows 流儀に合わせボタンを離した時点で表示する
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Right || !EnableCopyContextMenu || ContextMenuStrip != null || DesignMode)
            return;

        var hit = HitTest(e.X, e.Y);
        contextRow = hit.RowIndex;
        contextColumn = hit.ColumnIndex;
        EnsureCopyMenu();
        UpdateCopyMenu();
        SetCopyHighlight(DefaultCopyHighlight()); // メニューが開いた時点から対象セルを示す (どのセルを右クリックしたかの視覚フィードバック)
        copyMenu.Show(this, e.Location);
    }

    private CopyHighlightScope DefaultCopyHighlight()
        => contextRow >= 0 && contextColumn >= 0 ? CopyHighlightScope.Cell : CopyHighlightScope.None;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            copyMenu?.Dispose(); // 260715Cl 追加: 内蔵コンテキストメニューはデザイナ所有でないため自前で破棄
        base.Dispose(disposing);
    }

    private void EnsureCopyMenu()
    {
        if (copyMenu != null)
            return;

        copyCellItem = CreateCopyItem(CopyHighlightScope.Cell, (_, _) => CopyCell(expand: false));
        copyCellPlainItem = CreateCopyItem(CopyHighlightScope.Cell, (_, _) => CopyCell(expand: true));
        copyRowItem = CreateCopyItem(CopyHighlightScope.Row, (_, _) => CopyRow(expand: false));
        copyRowPlainItem = CreateCopyItem(CopyHighlightScope.Row, (_, _) => CopyRow(expand: true));
        copyTableItem = CreateCopyItem(CopyHighlightScope.Table, (_, _) => CopyTable(expand: false));
        copyTablePlainItem = CreateCopyItem(CopyHighlightScope.Table, (_, _) => CopyTable(expand: true));
        copyTableTabularItem = CreateCopyItem(CopyHighlightScope.Table, (_, _) => CopyTableTabular());
        copyTableImageItem = CreateCopyItem(CopyHighlightScope.Table, (_, _) => CopyTableImage());

        copyMenu = new ContextMenuStrip { ShowImageMargin = false, ShowItemToolTips = true };
        copyMenu.Items.AddRange(new ToolStripItem[]
        {
            copyCellItem, copyCellPlainItem, new ToolStripSeparator(),
            copyRowItem, copyRowPlainItem, new ToolStripSeparator(),
            copyTableItem, copyTablePlainItem, copyTableTabularItem, copyTableImageItem
        });
        copyMenu.Closed += (_, _) => SetCopyHighlight(CopyHighlightScope.None);
    }

    /// <summary>コピーメニュー 1 項目を生成する。ホバー中は対象範囲 (セル/行/表) をハイライトし、離れたら既定 (対象セル) へ戻す。</summary>
    private ToolStripMenuItem CreateCopyItem(CopyHighlightScope scope, EventHandler onClick)
    {
        var item = new ToolStripMenuItem();
        item.Click += onClick;
        item.MouseEnter += (_, _) => SetCopyHighlight(scope);
        item.MouseLeave += (_, _) => SetCopyHighlight(DefaultCopyHighlight());
        return item;
    }

    private void SetCopyHighlight(CopyHighlightScope scope)
    {
        if (highlightScope == scope)
            return;
        //260715Cl 変更: メニュー項目ホバー毎の全面 Invalidate をやめ、旧・新ハイライト矩形の合併領域のみ無効化 (行数の多い表での全セル再ペイント抑制)
        var prev = GetCopyScopeRect(highlightScope);
        highlightScope = scope;
        var next = GetCopyScopeRect(scope);
        // 空矩形を Union に混ぜると (0,0) 起点まで無効領域が膨らむため、非空側のみ採用する
        var dirty = prev.IsEmpty ? next : next.IsEmpty ? prev : Rectangle.Union(prev, next);
        if (dirty.Width > 0 && dirty.Height > 0)
        {
            dirty.Inflate(2, 2); // 枠線 (幅 1.5〜2px) の描き残し防止
            Invalidate(dirty);
        }
    }

    /// <summary>メニューを開くたびに文言 (現在の UI カルチャ)・有効状態・ツールチップを設定し直す。実行時の言語切替に追従する。</summary>
    private void UpdateCopyMenu()
    {
        var hasLatex = Columns.Cast<DataGridViewColumn>().Any(c => c.Visible && c.CellTemplate is DataGridViewLatexTextBoxCell);
        var hasCell = contextRow >= 0 && contextColumn >= 0;
        var hasRow = contextRow >= 0;
        var hasRows = Rows.Count > 0;

        var cell = Loc(en: "Copy cell", ja: "セルをコピー", de: "Zelle kopieren", fr: "Copier la cellule", es: "Copiar celda", pt: "Copiar célula", it: "Copia cella", ru: "Копировать ячейку", zhHans: "复制单元格", zhHant: "複製儲存格", ko: "셀 복사");
        var row = Loc(en: "Copy row", ja: "行をコピー", de: "Zeile kopieren", fr: "Copier la ligne", es: "Copiar fila", pt: "Copiar linha", it: "Copia riga", ru: "Копировать строку", zhHans: "复制行", zhHant: "複製列", ko: "행 복사");
        var table = Loc(en: "Copy table", ja: "表をコピー", de: "Tabelle kopieren", fr: "Copier le tableau", es: "Copiar tabla", pt: "Copiar tabela", it: "Copia tabella", ru: "Копировать таблицу", zhHans: "复制表格", zhHant: "複製表格", ko: "표 복사");
        // 数式列を持たない表では raw/展開の対比が存在しないため、サフィックスなしの素朴な文言にして展開系項目を隠す。
        var raw = hasLatex ? Loc(en: " (raw LaTeX)", ja: " (LaTeX のまま)", de: " (LaTeX-Quelltext)", fr: " (source LaTeX)", es: " (código LaTeX)", pt: " (código LaTeX)", it: " (sorgente LaTeX)", ru: " (исходный LaTeX)", zhHans: " (LaTeX 源码)", zhHant: " (LaTeX 原始碼)", ko: " (LaTeX 원본)") : "";
        var expand = Loc(en: " (expand formulas to text)", ja: " (数式をテキストに展開)", de: " (Formeln als Text)", fr: " (formules en texte)", es: " (fórmulas como texto)", pt: " (fórmulas como texto)", it: " (formule come testo)", ru: " (формулы как текст)", zhHans: " (公式展开为文本)", zhHant: " (公式展開為文字)", ko: " (수식을 텍스트로 변환)");

        copyCellItem.Text = cell + raw;
        copyCellPlainItem.Text = cell + expand;
        copyRowItem.Text = row + raw;
        copyRowPlainItem.Text = row + expand;
        copyTableItem.Text = table + raw;
        copyTablePlainItem.Text = table + expand;
        copyTableTabularItem.Text = Loc(en: "Copy table as LaTeX tabular", ja: "表を LaTeX 表 (tabular) としてコピー", de: "Tabelle als LaTeX-tabular kopieren", fr: "Copier le tableau en tabular LaTeX", es: "Copiar tabla como tabular de LaTeX", pt: "Copiar tabela como tabular LaTeX", it: "Copia tabella come tabular LaTeX", ru: "Копировать таблицу как LaTeX tabular", zhHans: "以 LaTeX tabular 复制表格", zhHant: "以 LaTeX tabular 複製表格", ko: "표를 LaTeX tabular로 복사");
        copyTableImageItem.Text = Loc(en: "Copy table as image", ja: "表を画像としてコピー", de: "Tabelle als Bild kopieren", fr: "Copier le tableau en image", es: "Copiar tabla como imagen", pt: "Copiar tabela como imagem", it: "Copia tabella come immagine", ru: "Копировать таблицу как изображение", zhHans: "以图像复制表格", zhHant: "以影像複製表格", ko: "표를 이미지로 복사");

        copyCellPlainItem.Visible = copyRowPlainItem.Visible = copyTablePlainItem.Visible = hasLatex;
        copyCellItem.Enabled = copyCellPlainItem.Enabled = hasCell;
        copyRowItem.Enabled = copyRowPlainItem.Enabled = hasRow;
        copyTableItem.Enabled = copyTablePlainItem.Enabled = copyTableTabularItem.Enabled = copyTableImageItem.Enabled = hasRows;

        // セル系 2 項目のツールチップ末尾に、実際にコピーされる内容のプレビューを付ける
        // (生ソースと展開後を開いたまま見比べられる。Selectable=false で選択表示がない表の対象確認も兼ねる)。
        var willCopy = Loc(en: "Will copy:", ja: "コピーされる内容:", de: "Wird kopiert:", fr: "Sera copié :", es: "Se copiará:", pt: "Será copiado:", it: "Verrà copiato:", ru: "Будет скопировано:", zhHans: "将复制:", zhHant: "將複製:", ko: "복사될 내용:");
        static string Preview(string s) => s.Length > 40 ? s[..40] + "…" : s;
        string PreviewLine(bool expanded) => hasCell ? $"\n{willCopy} \"{Preview(GetCellText(contextRow, contextColumn, expanded))}\"" : "";

        copyCellItem.ToolTipText = Loc(
            en: "Copies the cell as-is; formulas stay in LaTeX source form.",
            ja: "セルの内容をそのままコピーします。数式は LaTeX ソースのままです。",
            de: "Kopiert die Zelle unverändert; Formeln bleiben LaTeX-Quelltext.",
            fr: "Copie la cellule telle quelle ; les formules restent en source LaTeX.",
            es: "Copia la celda tal cual; las fórmulas quedan como código LaTeX.",
            pt: "Copia a célula como está; as fórmulas permanecem em código LaTeX.",
            it: "Copia la cella così com'è; le formule restano in sorgente LaTeX.",
            ru: "Копирует ячейку как есть; формулы остаются в виде исходного LaTeX.",
            zhHans: "按原样复制单元格；公式保持 LaTeX 源码。",
            zhHant: "按原樣複製儲存格；公式保持 LaTeX 原始碼。",
            ko: "셀 내용을 그대로 복사합니다. 수식은 LaTeX 원본으로 유지됩니다.") + PreviewLine(false);
        copyCellPlainItem.ToolTipText = Loc(
            en: "Expands formulas to plain text before copying (e.g. \\bar{3} → -3).",
            ja: "数式を読みやすいテキストに展開してコピーします (例: \\bar{3} → -3)。",
            de: "Wandelt Formeln vor dem Kopieren in Klartext um (z. B. \\bar{3} → -3).",
            fr: "Convertit les formules en texte brut avant la copie (ex. \\bar{3} → -3).",
            es: "Convierte las fórmulas a texto plano antes de copiar (p. ej. \\bar{3} → -3).",
            pt: "Converte as fórmulas em texto simples antes de copiar (ex.: \\bar{3} → -3).",
            it: "Converte le formule in testo semplice prima di copiare (es. \\bar{3} → -3).",
            ru: "Преобразует формулы в обычный текст перед копированием (напр. \\bar{3} → -3).",
            zhHans: "复制前将公式展开为纯文本 (例: \\bar{3} → -3)。",
            zhHant: "複製前將公式展開為純文字 (例: \\bar{3} → -3)。",
            ko: "복사 전에 수식을 일반 텍스트로 변환합니다 (예: \\bar{3} → -3).") + PreviewLine(true);
        copyRowItem.ToolTipText = Loc(
            en: "Copies this row as tab-separated text; formulas stay in LaTeX.",
            ja: "この行をタブ区切りテキストでコピーします。数式は LaTeX ソースのままです。",
            de: "Kopiert diese Zeile als tabulatorgetrennten Text; Formeln bleiben LaTeX.",
            fr: "Copie cette ligne en texte séparé par tabulations ; formules en LaTeX.",
            es: "Copia esta fila como texto separado por tabulaciones; fórmulas en LaTeX.",
            pt: "Copia esta linha como texto separado por tabulações; fórmulas em LaTeX.",
            it: "Copia questa riga come testo separato da tabulazioni; formule in LaTeX.",
            ru: "Копирует строку как текст с табуляцией; формулы остаются в LaTeX.",
            zhHans: "以制表符分隔文本复制该行；公式保持 LaTeX。",
            zhHant: "以定位字元分隔文字複製該列；公式保持 LaTeX。",
            ko: "이 행을 탭 구분 텍스트로 복사합니다. 수식은 LaTeX로 유지됩니다.");
        copyRowPlainItem.ToolTipText = Loc(
            en: "Copies this row as tab-separated text, expanding formulas to plain text.",
            ja: "この行をタブ区切りテキストでコピーします。数式はテキストに展開します。",
            de: "Kopiert diese Zeile als tabulatorgetrennten Text; Formeln als Klartext.",
            fr: "Copie cette ligne en texte séparé par tabulations, formules en texte brut.",
            es: "Copia esta fila como texto separado por tabulaciones, fórmulas como texto.",
            pt: "Copia esta linha como texto separado por tabulações, fórmulas como texto.",
            it: "Copia questa riga come testo separato da tabulazioni, formule come testo.",
            ru: "Копирует строку как текст с табуляцией, формулы — обычным текстом.",
            zhHans: "以制表符分隔文本复制该行，公式展开为纯文本。",
            zhHant: "以定位字元分隔文字複製該列，公式展開為純文字。",
            ko: "이 행을 탭 구분 텍스트로 복사하며 수식을 텍스트로 변환합니다.");
        copyTableItem.ToolTipText = Loc(
            en: "Copies the whole table as tab-separated text with headers (pastes into Excel). Formulas stay in LaTeX.",
            ja: "表全体をヘッダー付きのタブ区切りテキストでコピーします (Excel 等へ貼り付け可)。数式は LaTeX ソースのままです。",
            de: "Kopiert die ganze Tabelle mit Kopfzeile als tabulatorgetrennten Text (für Excel). Formeln bleiben LaTeX.",
            fr: "Copie tout le tableau avec en-têtes en texte tabulé (collable dans Excel). Formules en LaTeX.",
            es: "Copia toda la tabla con encabezados como texto tabulado (pegable en Excel). Fórmulas en LaTeX.",
            pt: "Copia toda a tabela com cabeçalhos como texto tabulado (colável no Excel). Fórmulas em LaTeX.",
            it: "Copia l'intera tabella con intestazioni come testo tabulato (incollabile in Excel). Formule in LaTeX.",
            ru: "Копирует всю таблицу с заголовками как текст с табуляцией (для Excel). Формулы остаются в LaTeX.",
            zhHans: "以带表头的制表符分隔文本复制整个表格 (可粘贴到 Excel)。公式保持 LaTeX。",
            zhHant: "以帶表頭的定位字元分隔文字複製整個表格 (可貼上到 Excel)。公式保持 LaTeX。",
            ko: "표 전체를 머리글 포함 탭 구분 텍스트로 복사합니다 (Excel에 붙여넣기 가능). 수식은 LaTeX로 유지됩니다.");
        copyTablePlainItem.ToolTipText = Loc(
            en: "Copies the whole table as tab-separated text with headers, expanding formulas to plain text.",
            ja: "表全体をヘッダー付きのタブ区切りテキストでコピーします。数式はテキストに展開します。",
            de: "Kopiert die ganze Tabelle mit Kopfzeile als tabulatorgetrennten Text; Formeln als Klartext.",
            fr: "Copie tout le tableau avec en-têtes en texte tabulé, formules en texte brut.",
            es: "Copia toda la tabla con encabezados como texto tabulado, fórmulas como texto.",
            pt: "Copia toda a tabela com cabeçalhos como texto tabulado, fórmulas como texto.",
            it: "Copia l'intera tabella con intestazioni come testo tabulato, formule come testo.",
            ru: "Копирует всю таблицу с заголовками, формулы — обычным текстом.",
            zhHans: "以带表头的制表符分隔文本复制整个表格，公式展开为纯文本。",
            zhHant: "以帶表頭的定位字元分隔文字複製整個表格，公式展開為純文字。",
            ko: "표 전체를 머리글 포함 탭 구분 텍스트로 복사하며 수식을 텍스트로 변환합니다.");
        copyTableTabularItem.ToolTipText = Loc(
            en: "Copies the whole table as a LaTeX tabular environment, ready to paste into a manuscript.",
            ja: "表全体を LaTeX の tabular 環境としてコピーします (論文原稿への貼り付け用)。",
            de: "Kopiert die ganze Tabelle als LaTeX-tabular-Umgebung (für Manuskripte).",
            fr: "Copie tout le tableau comme environnement tabular LaTeX (pour un manuscrit).",
            es: "Copia toda la tabla como entorno tabular de LaTeX (para un manuscrito).",
            pt: "Copia toda a tabela como ambiente tabular LaTeX (para um manuscrito).",
            it: "Copia l'intera tabella come ambiente tabular LaTeX (per un manoscritto).",
            ru: "Копирует всю таблицу как окружение LaTeX tabular (для рукописи).",
            zhHans: "将整个表格复制为 LaTeX tabular 环境 (用于论文稿件)。",
            zhHant: "將整個表格複製為 LaTeX tabular 環境 (用於論文稿件)。",
            ko: "표 전체를 LaTeX tabular 환경으로 복사합니다 (논문 원고용).");
        copyTableImageItem.ToolTipText = Loc(
            en: "Copies the table as an image, exactly as rendered (for slides and documents).",
            ja: "表を見たままの画像としてコピーします (スライドや文書への貼り付け用)。",
            de: "Kopiert die Tabelle als Bild, genau wie dargestellt (für Folien und Dokumente).",
            fr: "Copie le tableau en image, tel qu'affiché (pour diapositives et documents).",
            es: "Copia la tabla como imagen, tal como se muestra (para diapositivas y documentos).",
            pt: "Copia a tabela como imagem, exatamente como exibida (para slides e documentos).",
            it: "Copia la tabella come immagine, esattamente come visualizzata (per slide e documenti).",
            ru: "Копирует таблицу как изображение, как она отображается (для слайдов и документов).",
            zhHans: "以显示原样将表格复制为图像 (用于幻灯片和文档)。",
            zhHant: "以顯示原樣將表格複製為影像 (用於簡報和文件)。",
            ko: "표를 화면에 표시된 그대로 이미지로 복사합니다 (슬라이드/문서용).");
    }

    /// <summary>可視列を表示順に返す (コピー系・デザイン時プレビューの共通経路)。</summary>
    private DataGridViewColumn[] VisibleColumnsInDisplayOrder()
        => Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).OrderBy(c => c.DisplayIndex).ToArray();

    /// <summary>セル 1 個分のコピー文字列。LaTeX セルはソース (expand=true なら平文へ展開)、通常セルは表示文字列 (Format 適用後)。</summary>
    private string GetCellText(int rowIndex, int columnIndex, bool expand)
    {
        var cell = Rows[rowIndex].Cells[columnIndex];
        if (cell is DataGridViewLatexTextBoxCell)
        {
            var text = Convert.ToString(cell.Value) ?? "";
            return expand ? DataGridViewLatexTextBoxCell.LatexToPlainText(text) : text;
        }
        return Convert.ToString(cell.FormattedValue) ?? "";
    }

    private void CopyCell(bool expand)
    {
        if (contextRow < 0 || contextRow >= Rows.Count || contextColumn < 0 || contextColumn >= Columns.Count)
            return; // メニュー表示中に SetRows/SetColumns で作り替えられた場合の保険
        SetClipboardText(GetCellText(contextRow, contextColumn, expand));
    }

    private void CopyRow(bool expand)
    {
        if (contextRow < 0 || contextRow >= Rows.Count)
            return;
        SetClipboardText(string.Join("\t", VisibleColumnsInDisplayOrder().Select(c => GetCellText(contextRow, c.Index, expand))));
    }

    private void CopyTable(bool expand)
    {
        var cols = VisibleColumnsInDisplayOrder();
        if (cols.Length == 0)
            return;
        var sb = new StringBuilder();
        sb.AppendLine(string.Join("\t", cols.Select(c => c.HeaderText)));
        foreach (DataGridViewRow r in Rows)
            sb.AppendLine(string.Join("\t", cols.Select(c => GetCellText(r.Index, c.Index, expand))));
        SetClipboardText(sb.ToString());
    }

    private void CopyTableTabular()
    {
        var cols = VisibleColumnsInDisplayOrder();
        if (cols.Length == 0)
            return;

        static string ColSpec(DataGridViewColumn c) => c.DefaultCellStyle.Alignment switch
        {
            DataGridViewContentAlignment.TopCenter or DataGridViewContentAlignment.MiddleCenter or DataGridViewContentAlignment.BottomCenter => "c",
            DataGridViewContentAlignment.TopRight or DataGridViewContentAlignment.MiddleRight or DataGridViewContentAlignment.BottomRight => "r",
            _ => "l"
        };

        var sb = new StringBuilder();
        sb.AppendLine(@"\begin{tabular}{" + string.Concat(cols.Select(ColSpec)) + "}");
        sb.AppendLine(@"\hline");
        sb.AppendLine(string.Join(" & ", cols.Select(c => EscapeTabular(c.HeaderText))) + @" \\");
        sb.AppendLine(@"\hline");
        foreach (DataGridViewRow r in Rows)
            sb.AppendLine(string.Join(" & ", cols.Select(c => TabularCellText(r.Index, c.Index))) + @" \\");
        sb.AppendLine(@"\hline");
        sb.AppendLine(@"\end{tabular}");
        SetClipboardText(sb.ToString());
    }

    /// <summary>tabular 用のセル文字列。LaTeX セルは math mode ($...$) で囲み、通常セルは特殊文字をエスケープする。</summary>
    private string TabularCellText(int rowIndex, int columnIndex)
    {
        //260715Cl 変更: セル文字列の取得規約 (LaTeX セル=Value / 通常セル=FormattedValue) を GetCellText へ一本化
        var text = GetCellText(rowIndex, columnIndex, expand: false);
        if (Rows[rowIndex].Cells[columnIndex] is DataGridViewLatexTextBoxCell)
            return string.IsNullOrWhiteSpace(text) ? "" : "$" + text + "$";
        return EscapeTabular(text);
    }

    private static string EscapeTabular(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "";
        var sb = new StringBuilder(s.Length + 8);
        foreach (var ch in s)
            sb.Append(ch switch
            {
                '\\' => @"\textbackslash{}",
                '&' => @"\&",
                '%' => @"\%",
                '$' => @"\$",
                '#' => @"\#",
                '_' => @"\_",
                '{' => @"\{",
                '}' => @"\}",
                '~' => @"\textasciitilde{}",
                '^' => @"\textasciicircum{}",
                _ => ch.ToString()
            });
        return sb.ToString();
    }

    private void CopyTableImage()
    {
        if (Rows.Count == 0)
            return;
        SetCopyHighlight(CopyHighlightScope.None); // ハイライトを画像に写り込ませない (Closed より先に Click が処理される場合の保険)
        using var bmp = new Bitmap(Math.Max(1, Width), Math.Max(1, Height));
        DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
        //260715Cl 変更: コントロール全体ではなく表のコンテンツ領域 (ハイライトが示す範囲) だけを画像化し、余白の写り込みを防ぐ
        var rect = Rectangle.Intersect(GetCopyScopeRect(CopyHighlightScope.Table), new Rectangle(0, 0, bmp.Width, bmp.Height));
        if (rect.Width <= 0 || rect.Height <= 0)
            return;
        using var cropped = bmp.Clone(rect, bmp.PixelFormat);
        try { Clipboard.SetImage(cropped); }
        catch (ExternalException) { } // 他プロセスがクリップボードを占有している場合は静かに諦める
    }

    private static void SetClipboardText(string text)
    {
        try { Clipboard.SetDataObject(text, true); }
        catch (ExternalException) { } // 他プロセスがクリップボードを占有している場合は静かに諦める
    }

    /// <summary>コピー対象範囲 (セル/行/表) のクライアント座標矩形。表はヘッダ+全行のコンテンツ領域で、
    /// ハイライト描画と画像コピーの共通定義 (ハイライトが示す範囲=コピーされる範囲)。260715Cl 追加</summary>
    private Rectangle GetCopyScopeRect(CopyHighlightScope scope)
    {
        var content = new Rectangle(0, 0,
            Math.Min(ClientSize.Width, Columns.GetColumnsWidth(DataGridViewElementStates.Visible)),
            Math.Min(ClientSize.Height, (ColumnHeadersVisible ? ColumnHeadersHeight : 0) + Rows.GetRowsHeight(DataGridViewElementStates.Visible)));
        return scope switch
        {
            CopyHighlightScope.None => Rectangle.Empty,
            CopyHighlightScope.Cell => contextRow >= 0 && contextRow < Rows.Count && contextColumn >= 0 && contextColumn < Columns.Count
                ? GetCellDisplayRectangle(contextColumn, contextRow, true) : Rectangle.Empty,
            CopyHighlightScope.Row => contextRow >= 0 && contextRow < Rows.Count
                ? Rectangle.Intersect(GetRowDisplayRectangle(contextRow, true), content) : Rectangle.Empty,
            _ => content
        };
    }

    /// <summary>コピー対象範囲 (セル/行/表) の半透明ハイライトを重ね描きする。選択状態 (CurrentCell/Selected) には一切触れない。</summary>
    private void DrawCopyHighlight(Graphics g)
    {
        if (highlightScope == CopyHighlightScope.None)
            return;

        var rect = GetCopyScopeRect(highlightScope); //260715Cl 変更: 矩形算出を GetCopyScopeRect へ集約 (Row/Table で別イディオムだったのを統一)
        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        // ハイコントラストでは半透明フィルが効きにくいため枠線のみ太めに描く。
        if (!SystemInformation.HighContrast)
        {
            using var fill = new SolidBrush(Color.FromArgb(48, SystemColors.Highlight));
            g.FillRectangle(fill, rect);
        }
        using var pen = new Pen(SystemColors.Highlight, SystemInformation.HighContrast ? 2f : 1.5f);
        g.DrawRectangle(pen, rect.X + 0.75f, rect.Y + 0.75f, rect.Width - 1.5f, rect.Height - 1.5f);
    }

    #endregion

    #region 列のコード生成 (記号のみ・翻訳不要の表向け。翻訳要る表はデザイナで列定義する)

    /// <summary>ミニ表の 1 列分の定義 (<see cref="SetColumns"/> 用)。260606Cl 追加。</summary>
    /// <param name="Header">列見出し。コード生成のため resx に載らない (=翻訳されない)。翻訳要る表はデザイナ列を使う。</param>
    /// <param name="Align">セルの配置。数値列は MiddleRight、テキスト列は MiddleLeft/MiddleCenter。</param>
    /// <param name="Format">セルの DefaultCellStyle.Format (例 "g4")。値は double/int のまま渡し、表示時に整形させる。</param>
    /// <param name="Fill">true の列だけ残り幅を吸収 (Fill)。他列は内容幅 (AllCells)。Fill は 0 または 1 列。</param>
    /// <param name="Latex">true の列はセルの値を LaTeX 文字列として bitmap 描画する (<see cref="DataGridViewLatexTextBoxCell"/>)。260706Ch 追加。</param>
    public readonly record struct Col(
        string Header,
        DataGridViewContentAlignment Align = DataGridViewContentAlignment.MiddleLeft,
        string Format = null,
        bool Fill = false,
        bool Latex = false);

    /// <summary>列をコード生成する (記号のみ・翻訳不要の表向け)。1 回だけ呼ぶ。260606Cl 追加。</summary>
    public void SetColumns(params Col[] cols)
    {
        if (cols == null || cols.Length == 0)
            throw new ArgumentException("At least one column is required.", nameof(cols));
        if (cols.Count(c => c.Fill) > 1)
            throw new ArgumentException("Only one Fill column is expected.", nameof(cols));

        SuspendLayout();
        try
        {
            Rows.Clear();
            Columns.Clear();
            for (int i = 0; i < cols.Length; i++)
            {
                var col = cols[i];
                var c = new DataGridViewTextBoxColumn
                {
                    Name = "Column" + i,
                    HeaderText = col.Header,
                    ReadOnly = true,
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                    AutoSizeMode = col.Fill ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.AllCells,
                };
                c.DefaultCellStyle.Alignment = col.Align;
                if (!string.IsNullOrEmpty(col.Format))
                    c.DefaultCellStyle.Format = col.Format;
                if (col.Latex) // 260706Ch: MiniTable に LaTeX レンダリング列を追加
                    //DataGridViewLatexTextBoxCell.ApplyToColumn(c); // 260707Ch: 固定値ではなく MiniTable の LaTeX プロパティを使う
                    //DataGridViewLatexTextBoxCell.ApplyToColumn(c, LatexThickness, LatexTexStyle, LatexFontSizeInPoints); // 260707Ch: 分数表記プロパティも適用
                    DataGridViewLatexTextBoxCell.ApplyToColumn(c, LatexThickness, LatexTexStyle, LatexFontSizeInPoints, LatexFractionStyle); // 260707Ch
                Columns.Add(c);
            }
        }
        finally
        {
            ResumeLayout();
        }
    }

    #endregion

    #region デザイナ非表示プロパティ (表示専用テーブルでは固定 / 一元化のため触らせない) 260606Cl

    // --- ユーザー操作・編集系 (常に固定) ---
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new bool AllowUserToAddRows { get => base.AllowUserToAddRows; set => base.AllowUserToAddRows = value; }
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new bool AllowUserToDeleteRows { get => base.AllowUserToDeleteRows; set => base.AllowUserToDeleteRows = value; }
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new bool AllowUserToResizeColumns { get => base.AllowUserToResizeColumns; set => base.AllowUserToResizeColumns = value; }
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new bool AllowUserToResizeRows { get => base.AllowUserToResizeRows; set => base.AllowUserToResizeRows = value; }
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new bool AllowUserToOrderColumns { get => base.AllowUserToOrderColumns; set => base.AllowUserToOrderColumns = value; }
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new bool ReadOnly { get => base.ReadOnly; set => base.ReadOnly = value; }
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new bool MultiSelect { get => base.MultiSelect; set => base.MultiSelect = value; }
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new DataGridViewEditMode EditMode { get => base.EditMode; set => base.EditMode = value; }

    // --- データバインド・仮想化 (ミニ表は非バインド・非仮想専用) ---
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new object DataSource { get => base.DataSource; set => base.DataSource = value; }
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new string DataMember { get => base.DataMember; set => base.DataMember = value; }
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new bool VirtualMode { get => base.VirtualMode; set => base.VirtualMode = value; }

    // --- 配色・罫線・サイズ (ApplyTheme / コンストラクタで一元化) ---
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new bool RowHeadersVisible { get => base.RowHeadersVisible; set => base.RowHeadersVisible = value; }
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new ScrollBars ScrollBars { get => base.ScrollBars; set => base.ScrollBars = value; }
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new BorderStyle BorderStyle { get => base.BorderStyle; set => base.BorderStyle = value; }
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new DataGridViewCellBorderStyle CellBorderStyle { get => base.CellBorderStyle; set => base.CellBorderStyle = value; }
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new Color BackgroundColor { get => base.BackgroundColor; set => base.BackgroundColor = value; }
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new Color GridColor { get => base.GridColor; set => base.GridColor = value; }
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new DataGridViewAutoSizeColumnsMode AutoSizeColumnsMode { get => base.AutoSizeColumnsMode; set => base.AutoSizeColumnsMode = value; }
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new DataGridViewAutoSizeRowsMode AutoSizeRowsMode { get => base.AutoSizeRowsMode; set => base.AutoSizeRowsMode = value; }
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new bool EnableHeadersVisualStyles { get => base.EnableHeadersVisualStyles; set => base.EnableHeadersVisualStyles = value; }

    // --- CellStyle 一元化 (すべて ApplyTheme で管理。デザイナでは触らせない) ---
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new DataGridViewCellStyle DefaultCellStyle { get => base.DefaultCellStyle; set => base.DefaultCellStyle = value; }
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new DataGridViewCellStyle AlternatingRowsDefaultCellStyle { get => base.AlternatingRowsDefaultCellStyle; set => base.AlternatingRowsDefaultCellStyle = value; }
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new DataGridViewCellStyle RowsDefaultCellStyle { get => base.RowsDefaultCellStyle; set => base.RowsDefaultCellStyle = value; }
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new DataGridViewCellStyle ColumnHeadersDefaultCellStyle { get => base.ColumnHeadersDefaultCellStyle; set => base.ColumnHeadersDefaultCellStyle = value; }
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new DataGridViewCellStyle RowHeadersDefaultCellStyle { get => base.RowHeadersDefaultCellStyle; set => base.RowHeadersDefaultCellStyle = value; }

    #endregion
}
