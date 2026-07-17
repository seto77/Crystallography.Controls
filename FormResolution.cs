using System;
using System.ComponentModel; // 260717Cl 追加: 属性の冗長な完全修飾を除去するため
using System.Windows.Forms;

namespace Crystallography.Controls
{
    public partial class FormResolution : FormBase
    {
        public FormResolution()
        {
            InitializeComponent();
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int ResolutionWidth
        {
            set { numericUpDownWidth.Value = (decimal)value; }
            get { return (int)numericUpDownWidth.Value; }
        }

        // (260322Ch) WFO1000: Microsoft ??????????????????? ???????????
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int ResolutionHeight
        {
            set { numericUpDownHeight.Value = (decimal)value; }
            get { return (int)numericUpDownHeight.Value; }
        }

        private bool SkipEvent = false;

        private void numericUpDownWidth_ValueChanged(object sender, EventArgs e)
        {
            if (SkipEvent) return;
            SkipEvent = true;
            if (checkBoxKeepAspect.Checked)
                numericUpDownHeight.Value = numericUpDownWidth.Value;
            SkipEvent = false;
        }

        private void numericUpDownHeight_ValueChanged(object sender, EventArgs e)
        {
            if (SkipEvent) return;
            SkipEvent = true;
            if (checkBoxKeepAspect.Checked)
                numericUpDownWidth.Value = numericUpDownHeight.Value;
            SkipEvent = false;
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
        }
    }
}
