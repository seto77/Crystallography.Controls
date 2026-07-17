// 260717Cl 新規 (/simplify): コントロール共通の小ヘルパー置き場。
using System;
using System.Reflection;
using System.Windows.Forms;

namespace Crystallography.Controls;

internal static class ControlHelper
{
    /// <summary>Miller-Bravais 指数の i 列 (i = −(h+k)) を CellFormatting で計算表示する共通処理
    /// (DataTable には i を保持しない)。260717Cl 追加: FormBeamInteraction / BoundControl / LatticePlaneControl の
    /// 3 箇所に同型ハンドラがコピーされていたため集約。</summary>
    internal static void FormatMillerBravaisI(DataGridView grid, DataGridViewCellFormattingEventArgs e,
        DataGridViewColumn iColumn, DataGridViewColumn hColumn, DataGridViewColumn kColumn)
    {
        if (e.RowIndex < 0 || grid.Columns[e.ColumnIndex] != iColumn) return;
        var row = grid.Rows[e.RowIndex];
        var h = Convert.ToInt32(row.Cells[hColumn.Index].Value);
        var k = Convert.ToInt32(row.Cells[kColumn.Index].Value);
        e.Value = (-h - k).ToString(); // (260424Ch) TextBoxCell の表示値は string にして DataGridView の型不一致を避ける
        e.FormattingApplied = true;
    }

    /// <summary>非公開の Control.DoubleBuffered をリフレクションで有効化する (DataGridView 等のちらつき防止)。
    /// 260717Cl 追加: 同一のリフレクション 1 行が 6 ファイル (AtomControl/BondControl/BoundControl/CrystalControl/
    /// FormBeamInteraction/CrystalDatabaseControl) にコピーされていたため集約。DoubleBuffered は Control の
    /// protected プロパティなので typeof(Control) 起点の取得で全派生に効く (旧: typeof(DataGridView)/typeof(UserControl) 起点と同一動作)。</summary>
    internal static void EnableDoubleBuffering(Control control)
        => typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(control, true, null);
}
