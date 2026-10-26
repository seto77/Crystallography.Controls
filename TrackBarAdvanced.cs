using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Crystallography.Controls
{
    [ToolboxItem(true)] // 260605Cl 追加: 基底 UserControlBase の [ToolboxItem(false)] 継承を打ち消しデザイナのツールボックスに表示
    public partial class TrackBarAdvanced : UserControlBase
    {
        public TrackBarAdvanced()
        {
            InitializeComponent();
            this.Resize += TrackBarAdvanced_Resize;
            //260920Cl 追加: ValueBoxWidth 指定時の numericBox は AutoSize なので、HeaderText/FooterText/Font が
            //  後から変わると外形幅も変わる (ApplyResources はプロパティ設定順が不定)。本体 Resize だけでは追随できないため、
            //  numericBox 自身のサイズ変化を splitter へ伝える。ValueBoxWidth < 0 (従来モード) では何もしないので再帰しない
            numericBox.SizeChanged += (_, _) => { if (numericBox.ValueBoxWidth >= 0) syncSplitterToNumericBox(); };
        }

        /// <summary>260920Cl 追加: 数値欄の幅指定モードで、splitter を numericBox の優先幅 (ヘッダ + 数値欄 + spin + フッタ) へ合わせる。
        /// SplitterDistance が「幅」を意味するのは分割が Vertical (左=数値欄 / 右=トラックバー) のときだけなので、Horizontal では何もしない</summary>
        private void syncSplitterToNumericBox()
        {
            if (splitContainer.Orientation != Orientation.Vertical) return;
            var max = splitContainer.Width - splitContainer.SplitterWidth - splitContainer.Panel2MinSize;
            var w = Math.Max(splitContainer.Panel1MinSize, Math.Min(numericBox.PreferredSize.Width, max)); //レイアウト途中で max が負になりうるので下限も掛ける
            if (splitContainer.SplitterDistance != w) splitContainer.SplitterDistance = w;
        }

        private void TrackBarAdvanced_Resize(object sender, EventArgs e)
        {
            trackBar.Location = new Point(0, 0);
            trackBar.Size = splitContainer.Panel2.ClientSize;

            numericBox.Location = new Point(0, 0);
            //260920Cl 変更 (作者指示「ValueBoxWidth に変更してほしい」): 幅の決まる向きを逆転させた。
            //  ValueBoxWidth >= 0 のときは numericBox 自身が AutoSize で「ヘッダ + 数値欄 + spin + フッタ」の幅を決めるので、
            //  こちらから Width を押し付けない (AutoSize なので代入しても layout に戻される)。代わりに splitter をその幅へ合わせる。
            //旧: numericBox.Width = splitContainer.Panel1.ClientSize.Width;
            if (numericBox.ValueBoxWidth >= 0)
                syncSplitterToNumericBox();
            else
                numericBox.Width = splitContainer.Panel1.ClientSize.Width;

            /* if (Orientation == Orientation.Vertical)
             {
                 MinimumSize = new Size(1, numericBox.Height+2);
                 MaximumSize = new Size(1000, numericBox.Height+2);
             }
             else
             {
                 MinimumSize = new Size(1, numericBox.Height);
                 MaximumSize = new Size(1000, numericBox.Height*3);
             }*/
        }

        // 260920Cl 追加 (/simplify2): 内部の splitContainer / trackBar / numericBox が本体表面を完全に覆う (Dock=Fill + Resize で
        //   両パネルいっぱいに広げる) ため、配置先 Form が toolTip.SetToolTip(this, …) をしても hover してチップが出なかった。
        //   NumericBox / ColorControl / SizeControl と同じく基底の配布機構 (UserControlBase.RelayHostToolTip) へ配布先を渡す。
        //   numericBox は UserControlBase 派生なので、そこからさらに内部の textBox / ラベルへ再帰的に配布される
        protected override Control[] GetToolTipTargets() => [splitContainer, splitContainer.Panel1, splitContainer.Panel2, trackBar, numericBox];

        // 260920Cl 追加: 親がチップを設定したときは内部の汎用チップを抑止して親のバルーンへ一本化する
        protected internal override ToolTip InternalToolTip => toolTip;

        #region プロパティ

        //public int SmallChange { get { return trackBar.SmallChange; } set { trackBar.SmallChange = value; } }
        //public int LargeChange { get { return trackBar.LargeChange; } set { trackBar.LargeChange = value; } }
        //public int TickFrequency { get { return trackBar.TickFrequency; } set { trackBar.TickFrequency = value; } }
        //public int Increment { get { return (int)numericBox.Increment; } set { numericBox.Increment = (decimal)value; } }

        private bool logScrollBar = false;

        /// <summary>スクロールバーがログスケールで動くかどうか</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(false)] // 260607Cl
        public bool LogScrollBar
        {
            set { logScrollBar = value; }
            get { return logScrollBar; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(-1)] // 260607Cl
        public int DecimalPlaces { get { return numericBox.DecimalPlaces; } set { numericBox.DecimalPlaces = value; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Localizable(true)]
        [DefaultValue("")] // 260607Cl
        public string HeaderText { get { return numericBox.HeaderText; } set { numericBox.HeaderText = value; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Localizable(true)]
        [DefaultValue(typeof(Font), "Segoe UI, 9.75pt")] // 260607Cl
        public Font HeaderFont { get { return numericBox.HeaderFont; } set { numericBox.HeaderFont = value; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Localizable(true)]
        [DefaultValue("")] // 260607Cl
        public string FooterText { get { return numericBox.FooterText; } set { numericBox.FooterText = value; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Localizable(true)]
        [DefaultValue(typeof(Font), "Segoe UI, 9.75pt")] // 260607Cl
        public Font FooterFont { get { return numericBox.FooterFont; } set { numericBox.FooterFont = value; } }

        /// <summary>260920Cl 追加 (作者指示): 数値を入れるボックスの幅 (96dpi 論理px)。内側 NumericBox の同名プロパティへそのまま流す。
        /// -1 (既定) なら従来どおり数値欄は残り幅を Fill し、外形幅は <see cref="NumericBoxSize"/> (= splitter 位置) が決める。
        /// 0 以上なら数値欄をこの幅に固定し、外形幅は「ヘッダ + 数値欄 + spin ボタン + フッタ」から自動で決まる
        /// (= splitter 位置がこちらに従う)。ヘッダ/フッタの文字幅が言語ごとに違っても数値欄の幅が変わらないので、
        /// 11 言語ぶんの外形幅をひとつずつ調整しなくてよくなる</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(-1)] // 260920Cl 追加 (NumericBox.ValueBoxWidth の既定と同値)
        [Description("数値を入れるボックスの幅 (論理px)。-1 で残り幅を Fill (外形幅は NumericBoxSize が決める)。0 以上で数値欄を固定し、外形幅はヘッダ + 数値欄 + spin + フッタから自動決定する。")]
        public int ValueBoxWidth
        {
            get => numericBox.ValueBoxWidth;
            set { numericBox.ValueBoxWidth = value; TrackBarAdvanced_Resize(this, EventArgs.Empty); }
        }

        /// <summary>外形幅 (= splitter 位置) の直接指定。260920Cl に <see cref="ValueBoxWidth"/> へ置き換えた旧プロパティ。
        /// ⚠ReciPro 側の呼び出しは全て移行済みだが、IPAnalyzer の FormMain がまだ本プロパティを使っているため public のまま残す。
        /// デザイナのプロパティ グリッドと再シリアライズからは外してあるので、新規の配置では ValueBoxWidth を使うこと。
        /// ValueBoxWidth >= 0 のときはそちらが splitter を決めるので、本プロパティへの代入は上書きされる</summary>
        [Browsable(false)] // 260920Cl 追加
        [EditorBrowsable(EditorBrowsableState.Never)] // 260920Cl 追加
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // 260920Cl 変更: Visible → Hidden
        // [DefaultValue(84)] // 260607Cl (260920Cl: Hidden 化で不要)
        // 260920Cl 変更: SplitterDistance だけを動かすと内側 numericBox の Width が設計時の値 (84) のまま残り、
        //   数値欄を広げても表示が切れたままになる (TrackBarAdvanced_Resize は本体の Resize でしか走らないため)。幅の再配分をここでも呼ぶ。
        // public int NumericBoxSize { get { return splitContainer.SplitterDistance; } set { splitContainer.SplitterDistance = value; } } // 260920Cl 変更前
        public int NumericBoxSize { get { return splitContainer.SplitterDistance; } set { splitContainer.SplitterDistance = value; TrackBarAdvanced_Resize(this, EventArgs.Empty); } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(Orientation.Vertical)] // 260607Cl
        public Orientation Orientation { get { return splitContainer.Orientation; } set { splitContainer.Orientation = value; } }

        // (260322Ch) WFO1000 対応: WinForms アナライザが、コントロールの public プロパティにデザイナ直列化の方針 (DesignerSerializationVisibility / Browsable / DefaultValue 等) を明示するよう要求するため。〔260726Cl: 元の日本語が文字化けで失われていたので書き直した〕
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(27)] // 260607Cl
        public int ControlHeight { get { return this.Height; } set { this.Height = value; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(TickStyle.BottomRight)] // 260607Cl
        public TickStyle TickStyle { get { return trackBar.TickStyle; } set { trackBar.TickStyle = value; } }

        private double _value = 0;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(0.0)] // 260607Cl
        public double Value
        {
            get { return _value; }
            set
            {
                try
                {
                    if (value > Maximum) value = Maximum;
                    else if (value < Minimum) value = Minimum;
                    _value = value;

                    if (!SkipTrackBarEvent)
                    {
                        SkipTrackBarEvent = true;
                        try
                        {
                            if (!LogScrollBar)
                                trackBar.Value = (int)((value - Minimum) / (Maximum - Minimum) * trackBar.Maximum + 0.5);
                            else
                            {
                                if (Minimum < 0 && Maximum > 0)//最大が0以上、最小値が0以下の場合
                                {
                                    double center = trackBar.Maximum * Math.Log(-numericBox.Minimum) / (Math.Log(numericBox.Maximum) + Math.Log(-numericBox.Minimum));

                                    if (value >= 0)
                                        trackBar.Value = (int)(Math.Log(value, Maximum) * (trackBar.Maximum - center) + center + 0.5);
                                    else
                                        trackBar.Value = (int)(center * (1 - Math.Log(-value, -Minimum)) + 0.5);
                                }
                                else
                                {
                                    trackBar.Value = (int)(Math.Log(value - Minimum, Maximum - Minimum) * trackBar.Maximum + 0.5);
                                }
                            }

                            SkipTrackBarEvent = false;
                        }
                        catch { SkipTrackBarEvent = false; }
                    }

                    if (!SkipNumericBoxEvent)
                    {
                        SkipNumericBoxEvent = true;
                        numericBox.Value = value;
                        SkipNumericBoxEvent = false;
                    }
                }
                catch { }
            }
        }

        //private double maximum = 65535;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(double.PositiveInfinity)] // 260607Cl
        public double Maximum
        {
            get => numericBox.Maximum;
            set
            {
                if (value < Minimum)
                    value = Minimum;
                numericBox.Maximum = value;
            }
        }

        //private double minimum = 0;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(double.NegativeInfinity)] // 260607Cl
        public double Minimum
        {
            get => numericBox.Minimum;
            set
            {
                if (value > Maximum)
                    value = Maximum;
                numericBox.Minimum = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(1.0)] // 260607Cl
        public double UpDown_Increment
        { get => numericBox.UpDown_Increment; set => numericBox.UpDown_Increment = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(true)] // 260607Cl
        public bool Smart_Increment
        { get => numericBox.SmartIncrement; set => numericBox.SmartIncrement = value; }

        #endregion プロパティ

        #region イベント

        public delegate bool ValueChangedDelegate(object sender, double value);

        public event ValueChangedDelegate ValueChanged;

        #endregion イベント

        public bool SkipTrackBarEvent = false;

        private void trackBar_ValueChanged(object sender, EventArgs e)
        {
            if (SkipTrackBarEvent) return;
            SkipTrackBarEvent = true;

            if (!logScrollBar)
                Value = (numericBox.Maximum - numericBox.Minimum) * ((double)trackBar.Value / trackBar.Maximum) + numericBox.Minimum;
            else
            {//logモードの時
                if (numericBox.Minimum < 0 && numericBox.Maximum > 0)//最大が0以上、最小値が0以下の場合
                {
                    int center = (int)(trackBar.Maximum * Math.Log(-numericBox.Minimum) / (Math.Log(numericBox.Maximum) + Math.Log(-numericBox.Minimum)) + 0.5);

                    if (trackBar.Value > center)
                        Value = Math.Pow(numericBox.Maximum, (double)(trackBar.Value - center) / (trackBar.Maximum - center));
                    else
                        Value = -Math.Pow(-numericBox.Minimum, (double)(center - trackBar.Value) / (center));
                }
                else
                {
                    Value = Math.Pow((numericBox.Maximum - numericBox.Minimum), (double)trackBar.Value / trackBar.Maximum) + numericBox.Minimum;
                }
            }
            ValueChanged?.Invoke(this, Value);

            SkipTrackBarEvent = false;
        }

        public bool SkipNumericBoxEvent = false;

        private void numericBox_ValueChanged(object sender, EventArgs e)
        {
            if (SkipNumericBoxEvent) return;
            SkipNumericBoxEvent = true;
            Value = numericBox.Value;

            ValueChanged?.Invoke(this, Value);

            SkipNumericBoxEvent = false;
        }

        private void TrackBarAdvanced_Load(object sender, EventArgs e)
        {
            this.TrackBarAdvanced_Resize(sender, e);
        }
    }
}
