using System;
using System.Drawing;
using System.Windows.Forms;
using XamlMath;

namespace Crystallography.Controls;

/// <summary>
/// DataGridView セル内に LaTeX を bitmap 描画する読み取り専用セル。260706Ch 追加。
/// </summary>
public class DataGridViewLatexTextBoxCell : DataGridViewTextBoxCell
{
    private Bitmap cachedBitmap;
    private string cachedKey;

    public double Thickness { get; set; } = 0.6;
    public TexStyle TexStyle { get; set; } = TexStyle.Display;

    /// <summary>列のセルテンプレートをこの LaTeX 描画セルへ差し替える。既存行があれば各セルも複製して置き換える。</summary>
    /// <param name="column">対象列。</param>
    /// <param name="thickness">数式の縁取り太さ (device-independent pixel)。0 以下なら通常描画。</param>
    /// <param name="texStyle">LaTeX の TeX style。</param>
    public static void ApplyToColumn(DataGridViewColumn column, double thickness = 0.6, TexStyle texStyle = TexStyle.Display)
    {
        if (column == null) return;
        var template = new DataGridViewLatexTextBoxCell { Thickness = thickness, TexStyle = texStyle };
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
        var bitmap = TryGetBitmap(text, font, foreColor, GetDpi(graphics));
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
        var bitmap = TryGetBitmap(text, font, foreColor, GetDpi(graphics));
        if (bitmap == null)
            return base.GetPreferredSize(graphics, cellStyle, rowIndex, constraintSize);

        return new Size(bitmap.Width + cellStyle.Padding.Horizontal + 6, bitmap.Height + cellStyle.Padding.Vertical + 6);
    }

    // 260706Ch: DpiAwareDataGridView.CurrentDpi (GetDpiForWindow ベース) を優先し、Graphics.DpiX はフォールバックに留める。
    // (260518Cl のコメント通り、Control.DeviceDpi/Graphics.DpiX はスケーリングされたモニタでも 96 を返すことがある)
    private double GetDpi(Graphics graphics)
        => DataGridView is DpiAwareDataGridView dpiAware ? dpiAware.CurrentDpi : graphics?.DpiX ?? 96.0;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            cachedBitmap?.Dispose();
        base.Dispose(disposing);
    }

    private Bitmap TryGetBitmap(string text, Font font, Color color, double dpi)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var key = $"{text}\n{font.FontFamily.Name}|{font.SizeInPoints}|{(int)font.Style}|{color.ToArgb()}|{dpi:0.##}|{Thickness}|{TexStyle}";
        // 260706Ch: key が一致すれば cachedBitmap が null (=前回パース失敗) でもそのまま返す。
        // 以前は失敗時に cachedKey を null に戻していたため、同じ不正な LaTeX 文字列を持つセルは
        // 再描画のたびに RenderLatexBitmap を呼び直し、例外を繰り返していた。
        if (cachedKey == key)
            return cachedBitmap;

        cachedBitmap?.Dispose();
        cachedBitmap = null;
        cachedKey = key;
        try
        {
            cachedBitmap = LabelLaTeX.RenderLatexBitmap(text, font, color, dpi, TexStyle, Thickness);
        }
        catch
        {
            // cachedKey はそのまま残し、同じ入力の再パース/再例外を抑止する。
        }
        return cachedBitmap;
    }

    private Rectangle GetContentBounds(Rectangle cellBounds, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle)
    {
        var border = BorderWidths(advancedBorderStyle);
        var bounds = new Rectangle(
            cellBounds.Left + border.Left + cellStyle.Padding.Left + 3,
            cellBounds.Top + border.Top + cellStyle.Padding.Top + 3,
            Math.Max(0, cellBounds.Width - border.Left - border.Width - cellStyle.Padding.Horizontal - 6),
            Math.Max(0, cellBounds.Height - border.Top - border.Height - cellStyle.Padding.Vertical - 6));
        return bounds;
    }

    private static Rectangle Align(Size content, Rectangle bounds, DataGridViewContentAlignment alignment)
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
