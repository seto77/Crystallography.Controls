using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using XamlMath;

namespace Crystallography.Controls;

/// <summary>MiniTable の LaTeX セルで "1/2" 形式の分数をどう描くか。260707Ch 追加。</summary>
public enum LatexFractionStyle
{
    Horizontal,
    Vertical,
    Slanted,
}

/// <summary>
/// DataGridView セル内に LaTeX を bitmap 描画する読み取り専用セル。260706Ch 追加。
/// </summary>
public partial class DataGridViewLatexTextBoxCell : DataGridViewTextBoxCell
{
    private Bitmap cachedBitmap;
    private string cachedKey;

    public double Thickness { get; set; } = 0.6;
    public float FontSizeInPoints { get; set; } // 260707Ch 追加
    public LatexFractionStyle FractionStyle { get; set; } // 260707Ch 追加
    public TexStyle TexStyle { get; set; } = TexStyle.Display;

    /// <summary>列のセルテンプレートをこの LaTeX 描画セルへ差し替える。既存行があれば各セルも複製して置き換える。</summary>
    /// <param name="column">対象列。</param>
    /// <param name="thickness">数式の縁取り太さ (device-independent pixel)。0 以下なら通常描画。</param>
    /// <param name="texStyle">LaTeX の TeX style。</param>
    /// <param name="fontSizeInPoints">数式描画に使うフォントサイズ (pt)。0 以下ならセルの Font を使う。</param>
    /// <param name="fractionStyle">"1/2" 形式の分数の描画スタイル。</param>
    // 旧シグネチャ: public static void ApplyToColumn(DataGridViewColumn column, double thickness = 0.6, TexStyle texStyle = TexStyle.Display)
    public static void ApplyToColumn(DataGridViewColumn column, double thickness = 0.6, TexStyle texStyle = TexStyle.Display, float fontSizeInPoints = 0f, LatexFractionStyle fractionStyle = LatexFractionStyle.Horizontal) // 260707Ch: fontSizeInPoints/fractionStyle をデフォルト引数で追加
    {
        if (column == null) return;
        //var template = new DataGridViewLatexTextBoxCell { Thickness = thickness, TexStyle = texStyle }; // 260707Ch: MiniTable の LaTeX 文字サイズ指定も受け取る
        //var template = new DataGridViewLatexTextBoxCell { Thickness = thickness, TexStyle = texStyle, FontSizeInPoints = Math.Max(0f, fontSizeInPoints) }; // 260707Ch: 分数表記プロパティも受け取る
        var template = new DataGridViewLatexTextBoxCell { Thickness = thickness, TexStyle = texStyle, FontSizeInPoints = Math.Max(0f, fontSizeInPoints), FractionStyle = fractionStyle };
        column.CellTemplate = template;

        var grid = column.DataGridView;
        if (grid == null || column.Index < 0) return;
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow) continue;
            var old = row.Cells[column.Index];
            var cell = (DataGridViewCell)template.Clone();
            cell.Value = old.Value;
            cell.Style = old.Style;
            cell.Tag = old.Tag;
            cell.ToolTipText = old.ToolTipText;
            row.Cells[column.Index] = cell;
        }
        grid.InvalidateColumn(column.Index);
    }

    public override object Clone()
    {
        var cell = (DataGridViewLatexTextBoxCell)base.Clone();
        cell.Thickness = Thickness;
        cell.FontSizeInPoints = FontSizeInPoints; // 260707Ch
        cell.FractionStyle = FractionStyle; // 260707Ch
        cell.TexStyle = TexStyle;
        return cell;
    }

    protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex,
        DataGridViewElementStates cellState, object value, object formattedValue, string errorText,
        DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle,
        DataGridViewPaintParts paintParts)
    {
        var text = Convert.ToString(formattedValue ?? value) ?? "";
        var font = cellStyle.Font ?? DataGridView?.Font ?? Control.DefaultFont;
        var selected = (cellState & DataGridViewElementStates.Selected) != 0;
        var foreColor = selected ? cellStyle.SelectionForeColor : cellStyle.ForeColor;
        var bitmap = TryGetBitmap(text, font, FontSizeInPoints, foreColor, GetDpi(graphics)); // 260708Ch: Font 複製はキャッシュ miss 確定後まで TryGetBitmap 内に遅延
        if (bitmap == null)
        {
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText,
                cellStyle, advancedBorderStyle, paintParts);
            return;
        }

        base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText,
            cellStyle, advancedBorderStyle, paintParts & ~DataGridViewPaintParts.ContentForeground);

        var bounds = GetContentBounds(cellBounds, cellStyle, advancedBorderStyle);
        var target = Align(bitmap.Size, bounds, cellStyle.Alignment);
        // 260706Ch: target は常に bitmap.Size と同じ(Align はサイズを変えず位置だけ揃える)ため実質等倍描画。
        // 補間は一切不要かつ、GDI+ の HighQualityBicubic はこの premultiplied-alpha ビットマップに対して
        // 斑点状のノイズを生じさせる既知の不具合があるため、等倍ブリット (NearestNeighbor) に留める。
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        graphics.DrawImage(bitmap, target);
    }

    protected override Size GetPreferredSize(Graphics graphics, DataGridViewCellStyle cellStyle, int rowIndex, Size constraintSize)
    {
        if (rowIndex < 0)
            return base.GetPreferredSize(graphics, cellStyle, rowIndex, constraintSize);

        var text = Convert.ToString(GetValue(rowIndex)) ?? "";
        var font = cellStyle.Font ?? DataGridView?.Font ?? Control.DefaultFont;
        // 260706Ch: Paint() の選択時 SelectionForeColor 分岐と揃えないと、選択セルで GetPreferredSize と
        // Paint のキャッシュキー (色を含む) が毎回不一致になり、選択のたびに高コストな WPF 再レンダリングが走っていた。
        var foreColor = Selected ? cellStyle.SelectionForeColor : cellStyle.ForeColor;
        var bitmap = TryGetBitmap(text, font, FontSizeInPoints, foreColor, GetDpi(graphics)); // 260708Ch: Font 複製はキャッシュ miss 確定後まで TryGetBitmap 内に遅延
        if (bitmap == null)
            return base.GetPreferredSize(graphics, cellStyle, rowIndex, constraintSize);

        //return new Size(bitmap.Width + cellStyle.Padding.Horizontal + 6, bitmap.Height + cellStyle.Padding.Vertical + 6); // 260708Ch: MiniTable.CellPadding で余白を調整可能にする
        return new Size(bitmap.Width + cellStyle.Padding.Horizontal, bitmap.Height + cellStyle.Padding.Vertical); // 260708Ch
    }

    // 260706Ch: DpiAwareDataGridView.CurrentDpi (GetDpiForWindow ベース) を優先し、Graphics.DpiX はフォールバックに留める。
    // (260518Cl のコメント通り、Control.DeviceDpi/Graphics.DpiX はスケーリングされたモニタでも 96 を返すことがある)
    private double GetDpi(Graphics graphics)
        => DataGridView is DpiAwareDataGridView dpiAware ? dpiAware.CurrentDpi : graphics?.DpiX ?? 96.0;

    /// <summary>指定サイズが基準フォントと異なれば複製フォントを返す (呼び出し側で disposeFont=true のとき Dispose 要)。
    /// MiniTable.GetLatexPreviewFont と同一ロジックだったため統合 (260708Ch)。</summary>
    internal static Font ResolveFont(Font baseFont, float desiredSizeInPoints, out bool disposeFont)
    {
        disposeFont = false;
        if (desiredSizeInPoints <= 0f || Math.Abs(baseFont.SizeInPoints - desiredSizeInPoints) < 0.01f)
            return baseFont;

        disposeFont = true;
        return new Font(baseFont.FontFamily, desiredSizeInPoints, baseFont.Style, GraphicsUnit.Point);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            cachedBitmap?.Dispose();
        base.Dispose(disposing);
    }

    private Bitmap TryGetBitmap(string text, Font baseFont, float desiredSizeInPoints, Color color, double dpi)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        // 260708Ch: FontSizeInPoints 指定時の複製 Font 生成 (ResolveFont) はキャッシュヒットでも毎回発生していたため、
        // FormatFractions と同様にキー確定 (miss 判定) 後まで遅延する。キーには実フォントでなく希望サイズの値を使う。
        var sizeInPoints = desiredSizeInPoints > 0f ? desiredSizeInPoints : baseFont.SizeInPoints;
        var key = $"{text}\n{baseFont.FontFamily.Name}|{sizeInPoints}|{(int)baseFont.Style}|{color.ToArgb()}|{dpi:0.##}|{Thickness}|{TexStyle}|{FractionStyle}";
        // 260706Ch: key が一致すれば cachedBitmap が null (=前回パース失敗) でもそのまま返す。
        // 以前は失敗時に cachedKey を null に戻していたため、同じ不正な LaTeX 文字列を持つセルは
        // 再描画のたびに RenderLatexBitmap を呼び直し、例外を繰り返していた。
        if (cachedKey == key)
            return cachedBitmap;

        cachedBitmap?.Dispose();
        cachedBitmap = null;
        cachedKey = key;
        var font = ResolveFont(baseFont, desiredSizeInPoints, out var disposeFont); // 260708Ch
        try
        {
            cachedBitmap = LabelLaTeX.RenderLatexBitmap(FormatFractions(text, FractionStyle), font, color, dpi, TexStyle, Thickness);
        }
        catch
        {
            // cachedKey はそのまま残し、同じ入力の再パース/再例外を抑止する。
        }
        finally
        {
            if (disposeFont)
                font.Dispose(); // 260708Ch
        }
        return cachedBitmap;
    }

    /// <summary>"1/2" 形式の分数を <see cref="FractionStyle"/> に応じて LaTeX の \frac / 上付き下付き表記へ変換する。260707Ch 追加。</summary>
    internal static string FormatFractions(string text, LatexFractionStyle style)
    {
        if (string.IsNullOrWhiteSpace(text) || style == LatexFractionStyle.Horizontal)
            return text;

        return PlainFractionRegex().Replace(text, match =>
        {
            var numerator = match.Groups[1].Value;
            var denominator = match.Groups[2].Value;
            if (style == LatexFractionStyle.Vertical)
                return numerator.StartsWith("-", StringComparison.Ordinal)
                    ? $@"-\frac{{{numerator[1..]}}}{{{denominator}}}"
                    : $@"\frac{{{numerator}}}{{{denominator}}}";

            return numerator.StartsWith("-", StringComparison.Ordinal)
                ? $@"-{{}}^{{{numerator[1..]}}}\!/_{{{denominator}}}"
                : $@"{{}}^{{{numerator}}}\!/_{{{denominator}}}";
        });
    }

    [GeneratedRegex(@"(?<![\\\w}])(-?\d+)\/(\d+)(?![\w{])")]
    private static partial Regex PlainFractionRegex();

    /// <summary>LaTeX ソース文字列をコピー用の読みやすいプレーンテキストへ展開する。260715Cl 追加。</summary>
    /// <remarks>
    /// MiniTable のセルに現れる結晶学表記のコマンド群 (\bar, \overline, \mathrm, \frac, \perp, \parallel,
    /// \mid, \, など) を対象にした軽量変換で、汎用 LaTeX パーサではない。上線 (\bar{3}) は CIF や文献の
    /// 入力慣用に合わせて ASCII の "-3" へ展開する (FormSymmetryInformation.ToLatex 系変換のほぼ逆向き)。
    /// 例: "x,\,\bar{y},\,z" → "x, -y, z" / "\{\,2_{100}\mid 0,0,0\,\}" → "{ 2_100 | 0,0,0 }"。
    /// </remarks>
    public static string LatexToPlainText(string latex)
    {
        if (string.IsNullOrWhiteSpace(latex))
            return latex ?? "";

        var s = latex;
        // Seitz 記号などのリテラル波括弧 \{ \} は、後段のグルーピング波括弧除去に巻き込まれないよう退避する。
        s = s.Replace(@"\{", "\u0001").Replace(@"\}", "\u0002");
        s = LatexFracRegex().Replace(s, "$1/$2");                        // \frac{A}{B} → A/B
        s = LatexBarRegex().Replace(s, "-$1");                           // \bar{X} / \overline{X} → -X
        s = LatexMathrmRegex().Replace(s, "$1");                         // \mathrm{X} → X
        s = s.Replace(@"\perp ", "⊥").Replace(@"\perp", "⊥");            // ToLatex が挿入する後続スペースごと戻す
        s = s.Replace(@"\parallel ", "∥").Replace(@"\parallel", "∥");
        s = s.Replace(@"\mid", " | ");
        s = s.Replace(@"\!", "").Replace(@"\;", " ").Replace(@"\:", " ").Replace(@"\,", " ").Replace(@"\ ", " ");
        s = s.Replace("{", "").Replace("}", "");                         // グルーピング波括弧は除去 (^{X}/_{X} も ^X/_X になる)
        s = LatexUnknownCommandRegex().Replace(s, "$1");                 // 未知の \cmd はコマンド名だけ残す
        s = s.Replace('\u0001', '{').Replace('\u0002', '}');
        return LatexSpaceRunRegex().Replace(s, " ").Trim();              // 連続空白を 1 個へ圧縮
    }

    [GeneratedRegex(@"\\frac\{([^{}]*)\}\{([^{}]*)\}")]
    private static partial Regex LatexFracRegex(); // 260715Cl 追加

    [GeneratedRegex(@"\\(?:bar|overline)\{([^{}]*)\}")]
    private static partial Regex LatexBarRegex(); // 260715Cl 追加

    [GeneratedRegex(@"\\mathrm\{([^{}]*)\}")]
    private static partial Regex LatexMathrmRegex(); // 260715Cl 追加

    [GeneratedRegex(@"\\([A-Za-z]+)")]
    private static partial Regex LatexUnknownCommandRegex(); // 260715Cl 追加

    [GeneratedRegex(@" {2,}")]
    private static partial Regex LatexSpaceRunRegex(); // 260715Cl 追加

    private Rectangle GetContentBounds(Rectangle cellBounds, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle)
    {
        var border = BorderWidths(advancedBorderStyle);
        //var bounds = new Rectangle( // 260708Ch: 固定 3px ではなく DataGridViewCellStyle.Padding を使う
        //    cellBounds.Left + border.Left + cellStyle.Padding.Left + 3,
        //    cellBounds.Top + border.Top + cellStyle.Padding.Top + 3,
        //    Math.Max(0, cellBounds.Width - border.Left - border.Width - cellStyle.Padding.Horizontal - 6),
        //    Math.Max(0, cellBounds.Height - border.Top - border.Height - cellStyle.Padding.Vertical - 6));
        var bounds = new Rectangle( // 260708Ch
            cellBounds.Left + border.Left + cellStyle.Padding.Left,
            cellBounds.Top + border.Top + cellStyle.Padding.Top,
            Math.Max(0, cellBounds.Width - border.Left - border.Width - cellStyle.Padding.Horizontal),
            Math.Max(0, cellBounds.Height - border.Top - border.Height - cellStyle.Padding.Vertical));
        return bounds;
    }

    // 260717Cl: private → internal 化 (MiniTable に一字一句同一の Align が重複していたため、
    // ResolveFont/FormatFractions と同様に本クラスへ一本化して共有)。
    internal static Rectangle Align(Size content, Rectangle bounds, DataGridViewContentAlignment alignment)
    {
        var x = alignment switch
        {
            DataGridViewContentAlignment.TopCenter or DataGridViewContentAlignment.MiddleCenter or DataGridViewContentAlignment.BottomCenter
                => bounds.Left + (bounds.Width - content.Width) / 2,
            DataGridViewContentAlignment.TopRight or DataGridViewContentAlignment.MiddleRight or DataGridViewContentAlignment.BottomRight
                => bounds.Right - content.Width,
            _ => bounds.Left,
        };
        var y = alignment switch
        {
            DataGridViewContentAlignment.MiddleLeft or DataGridViewContentAlignment.MiddleCenter or DataGridViewContentAlignment.MiddleRight
                => bounds.Top + (bounds.Height - content.Height) / 2,
            DataGridViewContentAlignment.BottomLeft or DataGridViewContentAlignment.BottomCenter or DataGridViewContentAlignment.BottomRight
                => bounds.Bottom - content.Height,
            _ => bounds.Top,
        };
        return new Rectangle(x, y, content.Width, content.Height);
    }
}
