using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Crystallography.Controls;

public partial class FormIsotopeComposition : FormBase
{
    public FormIsotopeComposition() => InitializeComponent();

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int AtomNumber { get; set; } = 0;

    private double[] isotopicComposition = null;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double[] IsotopicComposition
    {
        set
        {
            isotopicComposition = value;
            var abundance = AtomStatic.IsotopeAbundance[AtomNumber];
            dataGridView.Rows.Clear();
            int n = 0;
            bool keep = isotopicComposition != null && isotopicComposition.Length == abundance.Count;
            foreach (int z in abundance.Keys)
                dataGridView.Rows.Add([z.ToString(), abundance[z], keep ? isotopicComposition[n++] : abundance[z]]);
        }
        get
        {
            isotopicComposition = new double[AtomStatic.IsotopeAbundance[AtomNumber].Count];
            for (int i = 0; i < dataGridView.Rows.Count; i++)
                isotopicComposition[i] = double.TryParse(dataGridView[2, i].Value?.ToString(), out var d) ? d : 0;
            return isotopicComposition;
        }
    }

    private void button1_Click(object sender, EventArgs e) { }
    private void dataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e) { }

    private void dataGridView_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 1) return;
        //if (!double.TryParse(e.FormattedValue?.ToString(), out var d) || d > 100) // 261105Cl 変更前
        if (!double.TryParse(e.FormattedValue?.ToString(), out var d) || d > 100 || d < 0) // 261105Cl 変更: 負の存在比もはじく (下の案内文の 0〜100 と合わせる)
        {
            //MessageBox.Show("Incorrect Value!"); // 261105Cl 変更前
            // 261105Cl 変更 (監査 A25): 英語だけの固定文だったので 11 言語に。入れられる範囲も書く
            MessageBox.Show(Crystallography.Localization.Loc(
                en: "Enter an abundance between 0 and 100 (%).",
                ja: "存在比は 0〜100 (%) の数値で入力してください。",
                de: "Geben Sie eine Häufigkeit zwischen 0 und 100 (%) ein.",
                fr: "Saisissez une abondance comprise entre 0 et 100 (%).",
                es: "Introduzca una abundancia entre 0 y 100 (%).",
                pt: "Insira uma abundância entre 0 e 100 (%).",
                it: "Inserire un'abbondanza compresa tra 0 e 100 (%).",
                ru: "Введите распространённость от 0 до 100 (%).",
                zhHans: "请输入 0 到 100 (%) 之间的丰度。",
                zhHant: "請輸入 0 到 100 (%) 之間的豐度。",
                ko: "존재비는 0~100(%) 사이의 숫자로 입력하십시오."),
                Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning); // 共有の部品なので題はアプリ名 (このフォームの Text は "FormIsotopeComposition" のまま)
            e.Cancel = true;
        }
    }
}
