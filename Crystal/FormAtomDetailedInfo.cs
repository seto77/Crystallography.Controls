using System;
using System.ComponentModel;
using System.Text; // 260628Ch
using System.Windows.Forms;

namespace Crystallography.Controls;

public partial class FormAtomDetailedInfo : FormBase
{
    private Atoms atoms = new();

    public FormAtomDetailedInfo()
    {
        InitializeComponent();
        listBox1.Items.Add("No.\tx\t y\t  z\r\n");
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Atoms Atoms
    {
        get => atoms;
        set { atoms = value; SetAtomDetailedInfo(); }
    }

    private void SetAtomDetailedInfo()
    {
        if (atoms?.Atom is not { Length: > 0 }) // 260628Ch
        {
            buttonCopy.Enabled = false;
            return;
        }

        buttonCopy.Enabled = true; // 260628Ch
        for (int i = 0; i < atoms.Atom.Length; i++)
        {
            var a = atoms.Atom[i];
            listBox.Items.Add($"{i + 1}\t{Atoms.GetStringFromDouble(a.X)}\t {Atoms.GetStringFromDouble(a.Y)}\t  {Atoms.GetStringFromDouble(a.Z)}");
        }
    }

    private void listBox_SelectedIndexChanged(object sender, EventArgs e) { }
    private void FormAtomDetailedInfo_Load(object sender, EventArgs e) { }

    private void buttonCopy_Click(object sender, EventArgs e) // 260628Ch 追加
    {
        if (atoms?.Atom is not { Length: > 0 }) return;

        var sb = new StringBuilder("No.\tx\ty\tz"); // 260628Ch: Excel に列として貼り付けやすい tab 区切りでコピーする。
        for (int i = 0; i < atoms.Atom.Length; i++)
        {
            var a = atoms.Atom[i];
            sb.AppendLine();
            //sb.Append(i + 1).Append('\t') // 260703Cl 変更前: GetStringFromDouble は 0.125→"1/8" 等の分数文字列を返し、Excel 貼付で日付 (1月8日) に化ける
            //    .Append(Atoms.GetStringFromDouble(a.X)).Append('\t')
            //    .Append(Atoms.GetStringFromDouble(a.Y)).Append('\t')
            //    .Append(Atoms.GetStringFromDouble(a.Z));
            sb.Append(i + 1).Append('\t') // 260703Cl 生の数値文字列でコピーする (表示用リストボックスは分数のまま)
                .Append(a.X.ToString("0.########")).Append('\t')
                .Append(a.Y.ToString("0.########")).Append('\t')
                .Append(a.Z.ToString("0.########"));
        }

        Clipboard.SetText(sb.ToString(), TextDataFormat.UnicodeText);
    }
}
