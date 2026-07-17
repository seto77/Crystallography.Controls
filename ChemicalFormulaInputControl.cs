using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Crystallography.Controls;

[ToolboxItem(true)] // 260605Cl 追加: 基底 UserControlBase の [ToolboxItem(false)] 継承を打ち消しデザイナのツールボックスに表示
public partial class ChemicalFormulaInputControl : UserControlBase
{
    private bool standardMode = true;

    [DefaultValue(true)]
    public bool StandardMode
    {
        set
        {
            standardMode = value;
            flowLayoutPanelComposition.Visible = value;
        }
        get { return standardMode; }
    }

    private bool weightMode = true;
    [DefaultValue(true)]

    public bool WeightMode
    {
        set
        {
            weightMode = value;
            flowLayoutPanelMolarRatio.Visible = !value;
            flowLayoutPanelWeight.Visible = value;
        }
        get { return weightMode; }
    }

    public ChemicalFormulaInputControl()
    {
        InitializeComponent();
        comboBoxCompound.SelectedIndex = 0;
        comboBoxElement.SelectedIndex = 0;
        //comboBoxLine.SelectedIndex = 0;
    }

    private void comboBoxElement_SelectedIndexChanged(object sender, EventArgs e)
    {
        int z = comboBoxElement.SelectedIndex + 1;

        // 260717Cl: 23 連の if-else チェーンを or/範囲パターンの switch 式へ集約 (元素→価数のマッピングは不変)。
        //if (z == 1) numericBoxValence.Value = 1;
        //else if (z == 5) ... 3; (6,14→4 / 7,15→5 / 16→6 / 25..30→2 / 33→3 / 34→4 / 43..45→4 / 46→2 / 47→4 / 48→2 /
        //  49→3 / 50→4 / 51→3 / 52→4 / 53→5 / 75→7 / 76→8 / 84→4 / 87→1 / 91→5)
        //else if (AtomStatic.XrayScatteringWK[z][^1].Valence > 0) numericBoxValence.Value = AtomStatic.XrayScatteringWK[z][^1].Valence;
        //else numericBoxValence.Value = 0;
        numericBoxValence.Value = z switch
        {
            1 or 87 => 1,
            (>= 25 and <= 30) or 46 or 48 => 2,
            5 or 33 or 49 or 51 => 3,
            6 or 14 or 34 or (>= 43 and <= 45) or 47 or 50 or 52 or 84 => 4,
            7 or 15 or 53 or 91 => 5,
            16 => 6,
            75 => 7,
            76 => 8,
            _ => AtomStatic.XrayScatteringWK[z][^1].Valence > 0 ? AtomStatic.XrayScatteringWK[z][^1].Valence : 0,
        };
        numericBoxValence_ValueChanged(sender, e);
    }

    private void numericBoxValence_ValueChanged(object sender, EventArgs e)
    {
        int z = comboBoxElement.SelectedIndex + 1;
        if (comboBoxCompound.SelectedIndex != comboBoxCompound.Items.Count - 1)
        {
            //string[] s = comboBoxCompound.Text.Split(new char[] { ' ' });
            //double accesoryValence = Convert.ToDouble((s[1].Substring(s[1].Length - 1, 1) + s[1].Substring(0, s[1].Length - 1)));
            //ElementProperty ep = new ElementProperty(z, numericBoxValence.Value, checkBoxCompound.Checked,"", s[0], accesoryValence, 0, 0, XrayLine.Ka, 0, 0);
            var m = new Molecule(z, numericBoxValence.Value);

            textBoxCompoundForm.Text = Molecule.CombineCationAndAnion(m, Molecule.DefinedIon[comboBoxCompound.SelectedIndex]);
        }
    }

    private void comboBoxCompound_SelectedIndexChanged(object sender, EventArgs e)
    {
        textBoxCompoundForm.ReadOnly = comboBoxCompound.SelectedIndex != comboBoxCompound.Items.Count - 1;
        numericBoxValence_ValueChanged(sender, e);
    }

    private void checkBoxIsCompound_CheckedChanged(object sender, EventArgs e)
    {
        flowLayoutPanelOxide.Visible = checkBoxCompound.Checked;
    }

    public Molecule GetMolecule()
    {
        int z = comboBoxElement.SelectedIndex + 1;

        string accesoryFormula = "";
        double accesoryValence = 0;

        if (comboBoxCompound.SelectedIndex != comboBoxCompound.Items.Count - 1)
        {
            string[] s = comboBoxCompound.Text.Split([' ']);
            accesoryFormula = s[0];
            accesoryValence = Convert.ToDouble(string.Concat(s[1].AsSpan(s[1].Length - 1, 1), s[1].AsSpan()[0..^1]));
        }
        //ElementProperty ep = new ElementProperty(
        //    z, numericBoxValence.Value, checkBoxCompound.Checked, textBoxCompoundForm.Text,
        //    accesoryFormula, accesoryValence, numericBoxWeight.Value / 100.0, numericBoxMolarRatio.Value,
        //    XrayLine.Ka, numericBoxTotalCount.Value, numericBoxCountTime.Value);
        //return ep;
        return null;
    }

    //public void SetMolecule(Molecule ep)
    //{
    //    //comboBoxElement.SelectedIndex = ep.AtomicNumber - 1;
    //    //numericBoxValence.Value = ep.Valence;

    //    //numericBoxCountTime.Value = ep.CountTime;
    //}
}
