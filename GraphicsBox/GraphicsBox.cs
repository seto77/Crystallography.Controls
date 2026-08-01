using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Crystallography.Controls;

/// <summary>PictureBox の標準機能に、呼び出し側が継続して描画できる描画バッファを加えたコントロール。</summary>
[Serializable]
public class GraphicsBox : PictureBox
{
    //260801Cl 削除: 独自イベント MouseWheeled を廃止した。Control.MouseWheel と同じ内容を再公開していただけで、
    //しかも 5 アプリを通じて購読者が 1 つも無かった (ホイールで何も起きない状態だった)。
    //ホイールを使うホストは標準の MouseWheel イベントを購読すること。
    //旧: public delegate void evMouseWheeled(object sender, MouseEventArgs e);
    //旧: public event evMouseWheeled MouseWheeled;

    private Bitmap graphicsLayerBitmap = null; // (260322Ch) 描画バッファの内容を保持する
    private Graphics graphicsLayer = null; // (260322Ch) 呼び出し側が使い回せる描画バッファ用 Graphics を保持する

    /// <summary>260801Cl 追加: マウスホイールで拡大縮小するかどうか。既定 false。
    /// このコントロールは表示範囲 (倍率・中心) を持たないため、拡大縮小の実体はホスト側にある。
    /// true のときだけ標準の MouseWheel イベントを通知するので、ホストはそれを購読して自分の表示範囲を変える。
    /// 既定を false にしてあるのは、姉妹アプリを含む既存の利用箇所の挙動を変えないため。</summary>
    [DefaultValue(false)]
    [Category("Behavior")]
    public bool MouseWheelZoom { get; set; } = false;

    /// <summary>260801Cl 追加: ホイール 1 ノッチあたりの倍率。ScalablePictureBox (pictureBox_MouseWheel) と同じ ×2 / ×0.5。
    /// 倍率の意味 (大きいほど拡大) はホストの表示範囲の持ち方に依存するので、逆数を使うかはホスト側で判断する。</summary>
    public static double GetWheelZoomFactor(int delta) => delta > 0 ? 2.0 : 0.5;

    /// <summary>GraphicBox の既定コンストラクタ。</summary>
    public GraphicsBox()
    {
        InitializeGraphicBox();
    }

    /// <summary>コンテナへ自動登録する互換コンストラクタ。</summary>
    public GraphicsBox(IContainer container)
        : this()
    {
        container?.Add(this); // (260322Ch) designer 生成コードからそのまま使えるようにする
    }

    /// <summary>呼び出し側が描画バッファへの描画に使う Graphics を返す。返された Graphics は破棄しないこと。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Graphics Graphics
    {
        get
        {
            EnsureGraphicsLayer();
            return graphicsLayer;
        }
    }

    /// <summary>Font を別名で公開するプロパティ。</summary>
    [Category("Appearance")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Font Fonts
    {
        get => Font;
        set => Font = value;
    }

    /// <summary>
    /// 現在表示される継承元のPictureBoxのImageと描画バッファを合成したスナップショットを返す。
    /// 呼び出し側で Dispose すること。
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Bitmap RenderedImage => CreateRenderedBitmap();

    /// <summary>現在表示される合成画像を新しい Bitmap として生成する。</summary>
    public Bitmap CreateRenderedBitmap()
    {
        var width = Math.Max(1, ClientSize.Width);
        var height = Math.Max(1, ClientSize.Height);
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);

        // bitmap = new Bitmap(width, height); // (260322Ch) 旧案: 既定 pixel format のまま確保していた
        // DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size)); // (260322Ch) 旧案: Control 任せでスナップショットを取っていた
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        using var args = new PaintEventArgs(graphics, new Rectangle(Point.Empty, bitmap.Size));
        InvokePaintBackground(this, args);
        InvokePaint(this, args); // (260322Ch) 継承元のPictureBoxのImageと描画バッファの両方を確実に合成する
        return bitmap;
    }

    /// <summary>描画バッファだけをクリアして再描画する。</summary>
    public void ClearGraphicsLayer()
    {
        EnsureGraphicsLayer();
        graphicsLayer.Clear(Color.Transparent); // (260322Ch) 継承元のPictureBoxのImage表示は残し、描画バッファだけ消す
        Invalidate();
    }

    /// <summary>描画バッファの Bitmap を取得する。呼び出し側で破棄しないこと。</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Bitmap GraphicsLayerBitmap
    {
        get
        {
            EnsureGraphicsLayer();
            return graphicsLayerBitmap;
        }
    }

    /// <summary>描画バッファを必要に応じて作り直す。</summary>
    private void EnsureGraphicsLayer()
    {
        var width = Math.Max(1, ClientSize.Width);
        var height = Math.Max(1, ClientSize.Height);
        if (graphicsLayerBitmap != null && graphicsLayerBitmap.Width == width && graphicsLayerBitmap.Height == height && graphicsLayer != null)
            return;

        RecreateGraphicsLayer(true);
    }

    /// <summary>描画バッファを再作成する。必要に応じて従前内容を左上基準で引き継ぐ。</summary>
    private void RecreateGraphicsLayer(bool preserveContents)
    {
        var width = Math.Max(1, ClientSize.Width);
        var height = Math.Max(1, ClientSize.Height);

        var previousBitmap = graphicsLayerBitmap;
        var previousGraphics = graphicsLayer;

        var nextBitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
        var nextGraphics = Graphics.FromImage(nextBitmap);
        nextGraphics.Clear(Color.Transparent); // (260322Ch) 透過の描画バッファとして保持し、背景や継承元のPictureBoxのImageを隠さない

        if (preserveContents && previousBitmap != null)
            nextGraphics.DrawImageUnscaled(previousBitmap, 0, 0); // (260322Ch) resize 後も描画内容をできるだけ残す

        graphicsLayerBitmap = nextBitmap;
        graphicsLayer = nextGraphics;

        previousGraphics?.Dispose();
        previousBitmap?.Dispose();
    }

    /// <summary>コントロール初期状態を設定する。</summary>
    private void InitializeGraphicBox()
    {
        // 260717Cl: SetStyle はフラグ enum の OR 指定を受け付けるため 5 連呼びを 1 呼び出しへ統合。
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true); // Selectable: (260322Ch) マウス操作後に Focus を受けてホイールを取りやすくする
        DoubleBuffered = true;
    }

    /// <summary>描画バッファ合成後、最前面へ一時図形 (ラバーバンド等) を描くためのイベント。260723Cl 追加</summary>
    public event PaintEventHandler PaintOverlay;

    /// <summary>PictureBox の描画後に描画バッファを重ねる。</summary>
    protected override void OnPaint(PaintEventArgs pe)
    {
        base.OnPaint(pe);
        // 260723Cl 変更: graphicsLayerBitmap == null の早期 return をやめ、PaintOverlay を常に最後へ通知する
        if (graphicsLayerBitmap != null)
            pe.Graphics.DrawImageUnscaled(graphicsLayerBitmap, 0, 0); // (260322Ch) 継承元のPictureBoxのImageと描画バッファを合成して表示する
        PaintOverlay?.Invoke(this, pe); // 260723Cl 追加: 永続描画バッファより手前に描画する
    }

    /// <summary>サイズ変更時に描画バッファを再作成する。</summary>
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        RecreateGraphicsLayer(true);
        Invalidate();
    }

    /// <summary>ハンドル作成後に描画バッファを確保する。</summary>
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        RecreateGraphicsLayer(true);
    }

    /// <summary>クリック時にフォーカスを受けてホイール入力を受けやすくする。</summary>
    protected override void OnMouseDown(MouseEventArgs e)
    {
        // Focus(); // (260322Ch) 旧案: 条件を見ずに Focus を呼んでいた
        if (CanFocus)
            Focus();
        base.OnMouseDown(e);
    }

    /// <summary>260801Cl 変更: MouseWheelZoom が true のときだけホイール入力をホストへ通知する。
    /// 旧実装は常に独自イベント MouseWheeled を発火していたが購読者が存在せず、実質何も起きていなかった。
    /// 旧: base.OnMouseWheel(e); MouseWheeled?.Invoke(this, e);</summary>
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (MouseWheelZoom)
            base.OnMouseWheel(e); // 標準の MouseWheel イベント。ホストが購読して自分の表示範囲を変える
    }

    /// <summary>260801Cl 追加: このコントロールにフォーカスがあるとき、+ / - をホイールと同じ拡大縮小として扱う。
    /// ホイール 1 ノッチ分の MouseEventArgs を合成して OnMouseWheel に流すので、ホスト側の処理は 1 つで済み、
    /// キーとホイールで挙動がずれない。位置はカーソルではなくコントロール中心にする (キー操作でカーソル位置に
    /// 引きずられると意図しない方向へ移動するため)。
    /// テンキーの + / - (Add / Subtract) と、メインキーボードの ;+ / -= (Oemplus / OemMinus) の両方を受ける。
    /// 修飾キー付きは完全一致で除外する。ProcessCmdKey で拾うのは、+ / - が ContainerControl の前処理で
    /// 消費される場合があるため (修飾なし矢印と同じ理由)。</summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (MouseWheelZoom && Focused)
        {
            int delta = keyData switch
            {
                Keys.Add or Keys.Oemplus or (Keys.Oemplus | Keys.Shift) => SystemInformation.MouseWheelScrollDelta,
                Keys.Subtract or Keys.OemMinus => -SystemInformation.MouseWheelScrollDelta,
                _ => 0,
            };
            if (delta != 0)
            {
                var center = new Point(ClientSize.Width / 2, ClientSize.Height / 2);
                OnMouseWheel(new MouseEventArgs(MouseButtons.None, 0, center.X, center.Y, delta));
                return true;
            }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>描画バッファを破棄する。</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            graphicsLayer?.Dispose();
            graphicsLayerBitmap?.Dispose();
            graphicsLayer = null;
            graphicsLayerBitmap = null;
        }

        base.Dispose(disposing);
    }
}
