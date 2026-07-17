using System;
using System.ComponentModel; // 260717Cl 追加: 属性の冗長な完全修飾を除去するため
using System.Collections.Generic;
using System.Drawing;
using System.Linq; // 260717Cl 追加: CheckedCrystalList の Cast 用
using System.Windows.Forms;

namespace Crystallography.Controls;

public partial class FormCrystalSelection : FormBase
{
    public FormCrystalSelection()
    {
        InitializeComponent();
        ShowCrystalInformation = false;
    }

    private bool saveMode = true;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SaveMode
    {
        set
        {
            saveMode = value; loadMode = !value;
            buttonLoadOrSave.Text = saveMode ? "Save" : "Load";
        }
        get => saveMode;
    }

    private bool loadMode = false;

    // (260322Ch) WFO1000: Microsoft ??????????????????? ???????????
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool LoadMode
    {
        set => SaveMode = !value; // 260717Cl: SaveMode setter と鏡像重複していた同期ロジックを委譲へ (結果の状態は同一)
        get => loadMode;
    }

    private bool showCrystalInformation = false;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowCrystalInformation
    {
        set
        {
            showCrystalInformation = value;
            if (showCrystalInformation)
            {
                buttonExpand.Text = "<<<<<<<<<<";
                panel1.Anchor = AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left;
                crystalControl1.Anchor = AnchorStyles.None;

                crystalControl1.Size = new Size(crystalControl1.Width, this.ClientSize.Height);

                this.ClientSize = new Size(panel1.Width + 4 + crystalControl1.Width, this.ClientSize.Height);
                crystalControl1.Location = new Point(panel1.Width + 2, 0);
                crystalControl1.Visible = true;

                panel1.Anchor = AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                crystalControl1.Anchor = AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Right;
            }
            else
            {
                buttonExpand.Text = ">>>>>>>>>>";
                panel1.Anchor = AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left;
                this.ClientSize = new Size(panel1.Width, this.ClientSize.Height);
                crystalControl1.Visible = false;
                panel1.Anchor = AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            }
        }
        get => showCrystalInformation;
    }

    public Crystal[] CheckedCrystalList
        => [.. checkedListBox1.CheckedItems.Cast<Crystal>()]; // 260717Cl: 手動ループ+二重配列化を collection expression へ (出力同一)

    public void SetCrystalList(List<Crystal> crystals)
    {
        foreach (Crystal c in crystals)
            checkedListBox1.Items.Add(c, true);
    }

    private void buttonCheckAll_Click(object sender, EventArgs e)
    {
        for (int i = 0; i < checkedListBox1.Items.Count; i++)
            checkedListBox1.SetItemChecked(i, true);
    }

    private void buttonUncheckAll_Click(object sender, EventArgs e)
    {
        for (int i = 0; i < checkedListBox1.Items.Count; i++)
            checkedListBox1.SetItemChecked(i, false);
    }

    private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (checkedListBox1.SelectedIndex >= 0)
            crystalControl1.Crystal = (Crystal)checkedListBox1.SelectedItem;
    }

    private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
    {
        var list = CheckedCrystalList; // 260717Cl: 呼ぶたび配列を再構築する getter の二重評価を 1 回に
        if (list.Length > e.Index && list[e.Index].Reserved)
            e.NewValue = CheckState.Checked;
    }

    private void buttonExpand_Click(object sender, EventArgs e) => ShowCrystalInformation = !showCrystalInformation;
}
