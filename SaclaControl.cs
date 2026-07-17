using System;
using System.ComponentModel; // 260717Cl 追加: 属性の冗長な完全修飾を除去するため
using System.Windows.Forms;

namespace Crystallography.Controls
{
    [ToolboxItem(true)] // 260605Cl 追加: 基底 UserControlBase の [ToolboxItem(false)] 継承を打ち消しデザイナのツールボックスに表示
    public partial class SaclaControl : UserControlBase
    {
        public delegate void MyEventHandler(object sender, EventArgs e);

        public event MyEventHandler ValueChanged;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public double PixelSize { set { numericBoxPixelSize.Value = value; } get { return numericBoxPixelSize.Value; } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public double PixelWidth { set { numericBoxPixelWidth.Value = value; } get { return numericBoxPixelWidth.Value; } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public double PixelHeight { set { numericBoxPixelHeight.Value = value; } get { return numericBoxPixelHeight.Value; } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public double TauRadian { set { numericBoxTau.RadianValue = value; } get { return numericBoxTau.RadianValue; } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public double TauDegree { set { numericBoxTau.Value = value; } get { return numericBoxTau.Value; } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public double PhiRadian { set { numericBoxPhi.RadianValue = value; } get { return numericBoxPhi.RadianValue; } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public double PhiDegree { set { numericBoxPhi.Value = value; } get { return numericBoxPhi.Value; } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public PointD Foot { set { numericBoxFootX.Value = value.X; numericBoxFootY.Value = value.Y; } get { return new PointD(numericBoxFootX.Value, numericBoxFootY.Value); } }
        // (260322Ch) WFO1000: Microsoft ??????????????????? ???????????
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public double CameraLength2 { set { numericBoxDistance.Value = value; } get { return numericBoxDistance.Value; } }

        public SaclaControl()
        {
            InitializeComponent();
        }

        private void numericBoxPixelWidth_ValueChanged(object sender, EventArgs e)
        {
            ValueChanged?.Invoke(sender, e);
        }
    }
}
