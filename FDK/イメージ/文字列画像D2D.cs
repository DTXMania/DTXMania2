using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace FDK
{
    /// <summary>
    ///		DirectWrite を使った Direct2D1ビットマップ。
    /// </summary>
    /// <remarks>
    ///		<see cref="表示文字列"/> メンバが更新されると、次回の描画時に新しいビットマップが生成される。
    /// </remarks>
    public class 文字列画像D2D : IImage, IDisposable
    {

        // プロパティ


        /// <summary>
        ///		表示する文字列。
        ///     このメンバを更新すると、次回の描画時に画像が更新される。
        /// </summary>
        public string 表示文字列
        {
            get
                => this._表示文字列;
            set
            {
                if( value != this._表示文字列 )
                {
                    this._表示文字列 = value;
                    this._TextLayoutを更新せよ = true;
                }
            }
        }

        /// <summary>
        ///     文字列描画に使うフォントの名前。
        ///     このメンバを更新すると、次回の描画時に画像が更新される。
        /// </summary>
        public string フォント名
        {
            get
                => this._フォント名;
            set
            {
                if( value != this._フォント名 )
                {
                    this._フォント名 = value;
                    this._TextFormatを更新せよ = true;
                }
            }
        }

        /// <summary>
        ///     文字列描画に使うフォントのサイズ。
        ///     このメンバを更新すると、次回の描画時に画像が更新される。
        /// </summary>
        public float フォントサイズpt
        {
            get
                => this._フォントサイズpt;
            set
            {
                if( value != this._フォントサイズpt )
                {
                    this._フォントサイズpt = value;
                    this._TextFormatを更新せよ = true;
                }
            }
        }

        /// <summary>
        ///     文字列描画に使うフォントの太さ。
        ///     このメンバを更新すると、次回の描画時に画像が更新される。
        /// </summary>
        public FontWeight フォントの太さ
        {
            get
                => this._フォントの太さ;
            set
            {
                if( value != this._フォントの太さ )
                {
                    this._フォントの太さ = value;
                    this._TextFormatを更新せよ = true;
                }
            }
        }

        /// <summary>
        ///     文字列描画に使うフォントのスタイル。
        ///     このメンバを更新すると、次回の描画時に画像が更新される。
        /// </summary>
        public FontStyle フォントスタイル
        {
            get
                => this._フォントスタイル;
            set
            {
                if( value != this._フォントスタイル )
                {
                    this._フォントスタイル = value;
                    this._TextFormatを更新せよ = true;
                }
            }
        }

        /// <summary>
        ///     文字列の前景色。
        ///     このメンバを更新すると、次回の描画時に画像が更新される。
        /// </summary>
        public Color4 前景色
        {
            get
                => this._前景色;
            set
            {
                if( value != this._前景色 )
                {
                    this._前景色 = value;
                    this._ビットマップを更新せよ = true;
                }
            }
        }

        /// <summary>
        ///     文字列の背景色。
        ///     このメンバを更新すると、次回の描画時に画像が更新される。
        /// </summary>
        public Color4 背景色
        {
            get
                => this._背景色;
            set
            {
                if( value != this._背景色 )
                {
                    this._背景色 = value;
                    this._ビットマップを更新せよ = true;
                }
            }
        }

        public enum 効果
        {
            /// <summary>
            ///		文字を前景色で描画する。
            /// </summary>
            通常,

            /// <summary>
            ///		文字は前景色で、影は背景色で描画する。
            /// </summary>
            ドロップシャドウ,

            /// <summary>
            ///		文字は前景色で、縁は背景色で描画する。
            /// </summary>
            縁取り,
        }

        /// <summary>
        ///     文字列の描画効果。
        ///     このメンバを更新すると、次回の描画時に画像が更新される。
        /// </summary>
        public 効果 描画効果
        {
            get
                => this._描画効果;
            set
            {
                if( value != this._描画効果 )
                {
                    this._描画効果 = value;
                    this._ビットマップを更新せよ = true;
                }
            }
        }

        /// <summary>
        ///		縁取りのサイズ。<see cref="描画効果"/>が <see cref="効果.縁取り"/> のときのみ有効。
        ///     このメンバを更新すると、次回の描画時に画像が更新される。
        /// </summary>
        public float 縁のサイズdpx
        {
            get
                => this._縁のサイズdpx;
            set
            {
                if( value != this._縁のサイズdpx )
                {
                    this._縁のサイズdpx = value;
                    this._ビットマップを更新せよ = true;
                }
            }
        }

        /// <summary>
        ///     文字列の改行処理。
        ///     このメンバを更新すると、次回の描画時に画像が更新される。
        /// </summary>
        public WordWrapping WordWrapping
        {
            get
                => this._WordWrapping;
            set
            {
                if( value != this._WordWrapping )
                {
                    this._WordWrapping = value;
                    this._TextFormatを更新せよ = true;
                }
            }
        }

        /// <summary>
        ///     文字列の縦方向の位置そろえ。
        ///     このメンバを更新すると、次回の描画時に画像が更新される。
        /// </summary>
        public ParagraphAlignment ParagraphAlignment
        {
            get
                => this._ParagraphAlignment;
            set
            {
                if( value != this._ParagraphAlignment )
                {
                    this._ParagraphAlignment = value;
                    this._TextFormatを更新せよ = true;
                }
            }
        }

        /// <summary>
        ///     文字列の横方向の位置そろえ。
        ///     このメンバを更新すると、次回の描画時に画像が更新される。
        /// </summary>
        public TextAlignment TextAlignment
        {
            get
                => this._TextAlignment;
            set
            {
                if( value != this._TextAlignment )
                {
                    this._TextAlignment = value;
                    this._TextFormatを更新せよ = true;
                }
            }
        }

        public float LineSpacing { get; protected set; }

        public float Baseline { get; protected set; }

        public bool 加算合成 { get; set; } = false;

        /// <summary>
        ///     TextLayout を作る際に使われるレイアウトサイズ[dpx]。
        ///     右揃えや下揃えなどを使う場合には、このサイズを基準にして、文字列の折り返しなどのレイアウトが行われる。
        /// </summary>
        public Size2F レイアウトサイズdpx { get; set; }

        /// <summary>
        ///     実際に作成されたビットマップ画像のサイズ[dpx]。
        ///     右揃えや下揃えなどを指定している場合は、このサイズは <see cref="レイアウトサイズdpx"/> に等しい。
        ///     指定していない場合は、このサイズは <see cref="_表示文字列のサイズdpx"/> に等しい。
        /// </summary>
        public Size2F 画像サイズdpx { get; protected set; } = Size2F.Zero;

        public RectangleF? 転送元矩形 { get; set; } = null;



        // 生成と終了


        public 文字列画像D2D( SharpDX.DirectWrite.Factory dwFactory, SharpDX.Direct2D1.Factory1 d2dFactory1, DeviceContext d2dDeviceContext, Size2F 設計画面サイズdpx )
        {
            //using var _ = new LogBlock( Log.現在のメソッド名 );

            // 必要なプロパティは呼び出し元で設定すること。

            this._DWFactory = dwFactory;
            this._Bitmap = null!;
            this._TextFormat = null!;
            this._TextLayout = null!;
            this._TextRenderer = new カスタムTextRenderer( d2dFactory1, d2dDeviceContext, Color.White, Color.Transparent );    // ビットマップの生成前に。

            this._ビットマップを更新せよ = true;
            this._TextFormatを更新せよ = true;
            this._TextLayoutを更新せよ = true;

            if( this.レイアウトサイズdpx == Size2F.Zero )
                this.レイアウトサイズdpx = 設計画面サイズdpx; // 初期サイズ
        }

        public virtual void Dispose()
        {
            //using var _ = new LogBlock( Log.現在のメソッド名 );
            
            this._TextRenderer.Dispose();
            this._TextLayout?.Dispose();
            this._TextFormat?.Dispose();
            this._Bitmap?.Dispose();
        }

        public void ビットマップを生成または更新する( DeviceContext dc )
        {
            float pt2px( float dpi, float pt ) => dpi * pt / 72f;

            if( this._TextFormatを更新せよ )
            {
                #region " テキストフォーマットを更新する。"
                //----------------
                this._TextFormat?.Dispose();
                this._TextFormat = new TextFormat(
                    this._DWFactory,
                    this.フォント名,
                    this.フォントの太さ,
                    this.フォントスタイル,
                    this.フォントサイズpt ) {
                    TextAlignment = this.TextAlignment,
                    ParagraphAlignment = this.ParagraphAlignment,
                    WordWrapping = this.WordWrapping,
                    FlowDirection = FlowDirection.TopToBottom,
                    ReadingDirection = ReadingDirection.LeftToRight,
                };

                // 行間は、プロパティではなくメソッドで設定する。
                this.LineSpacing = pt2px( dc.DotsPerInch.Width, this.フォントサイズpt );

                // baseline の適切な比率は、lineSpacing の 80 %。（MSDNより）
                this.Baseline = this.LineSpacing * 0.8f;

                // TextFormat に、行間とベースラインを設定する。
                this._TextFormat.SetLineSpacing( LineSpacingMethod.Uniform, this.LineSpacing, this.Baseline );
                //----------------
                #endregion
            }

            if( this._TextLayoutを更新せよ )
            {
                #region " テキストレイアウトを更新する。"
                //----------------
                this._TextLayout?.Dispose();
                this._TextLayout = new TextLayout(
                    this._DWFactory,
                    this.表示文字列,
                    this._TextFormat,
                    this.レイアウトサイズdpx.Width,
                    this.レイアウトサイズdpx.Height );

                // レイアウトが変わったのでサイズも更新する。

                this._表示文字列のサイズdpx = new Size2F(
                    this._TextLayout.Metrics.WidthIncludingTrailingWhitespace,
                    this._TextLayout.Metrics.Height );
                //----------------
                #endregion
            }

            if( this._ビットマップを更新せよ ||
                this._TextFormatを更新せよ ||
                this._TextLayoutを更新せよ )
            {
                #region " 古いビットマップレンダーターゲットを解放し、新しく生成する。"
                //----------------
                this.画像サイズdpx = ( this.ParagraphAlignment != ParagraphAlignment.Near || this.TextAlignment != TextAlignment.Leading ) ?
                    this.レイアウトサイズdpx :      // レイアウトにレイアウトサイズが必要
                    this._表示文字列のサイズdpx;    // レイアウトサイズは不要

                if( this.描画効果 == 効果.縁取り )
                {
                    // 縁取りを使うなら少し画像サイズを (+16, +16) 増やす。
                    this.画像サイズdpx = new Size2F(
                        this.画像サイズdpx.Width + 16f,
                        this.画像サイズdpx.Height + 16f );

                    this._表示文字列のサイズdpx = new Size2F(
                        this._表示文字列のサイズdpx.Width + 16f,
                        this._表示文字列のサイズdpx.Height + 16f );
                }

                this._Bitmap?.Dispose();
                this._Bitmap = new SharpDX.Direct2D1.BitmapRenderTarget( dc, CompatibleRenderTargetOptions.None, this.画像サイズdpx );
                //----------------
                #endregion

                #region " ビットマップレンダーターゲットをクリアし、テキストを描画する。"
                //----------------
                var rt = this._Bitmap;
                D2DBatch.Draw( rt, () => {

                    using var 前景色ブラシ = new SolidColorBrush( rt, this.前景色 );
                    using var 背景色ブラシ = new SolidColorBrush( rt, this.背景色 );

                    rt.Clear( Color.Transparent );

                    rt.AntialiasMode = AntialiasMode.PerPrimitive;
                    rt.TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Default;
                    rt.Transform = Matrix3x2.Identity;  // 等倍描画。(dpx to dpx)

                    switch( this.描画効果 )
                    {
                        case 効果.通常:
                        {
                            using var de = new カスタムTextRenderer.DrawingEffect( rt ) { 文字の色 = this.前景色 };
                            this._TextLayout.Draw( de, this._TextRenderer, 0f, 0f );
                            break;
                        }

                        case 効果.ドロップシャドウ:
                        {
                            using var de = new カスタムTextRenderer.ドロップシャドウDrawingEffect( rt ) { 文字の色 = this.前景色, 影の色 = this.背景色, 影の距離 = 3.0f };
                            this._TextLayout.Draw( de, this._TextRenderer, 0f, 0f );
                            break;
                        }
                        case 効果.縁取り:
                        {
                            using var de = new カスタムTextRenderer.縁取りDrawingEffect( rt ) { 文字の色 = this.前景色, 縁の色 = this.背景色, 縁の太さ = this.縁のサイズdpx };
                            this._TextLayout.Draw( de, this._TextRenderer, 8f, 8f ); // 描画位置をずらす(+8,+8)
                            break;
                        }
                    }

                } );
                //----------------
                #endregion
            }

            this._ビットマップを更新せよ = false;
            this._TextFormatを更新せよ = false;
            this._TextLayoutを更新せよ = false;
        }



        // 進行と描画


        public void 描画する( DeviceContext dc, float 左位置, float 上位置, float 不透明度0to1 = 1.0f, float X方向拡大率 = 1.0f, float Y方向拡大率 = 1.0f, Matrix? 変換行列3D = null )
        {
            var 変換行列2D =
                Matrix3x2.Scaling( X方向拡大率, Y方向拡大率 ) *   // 拡大縮小
                Matrix3x2.Translation( 左位置, 上位置 );          // 移動

            this.描画する( dc, 変換行列2D, 変換行列3D, 不透明度0to1 );
        }

        public void 描画する( DeviceContext dc, Matrix3x2? 変換行列2D = null, Matrix? 変換行列3D = null, float 不透明度0to1 = 1.0f )
        {
            if( string.IsNullOrEmpty( this.表示文字列 ) )
                return;

            if( this._ビットマップを更新せよ ||
                this._TextFormatを更新せよ ||
                this._TextLayoutを更新せよ )
            {
                this.ビットマップを生成または更新する( dc );
            }

            if( null == this._Bitmap )
                return;

            D2DBatch.Draw( dc, () => {

                dc.Transform = ( 変換行列2D ?? Matrix3x2.Identity ) * dc.Transform;
                dc.PrimitiveBlend = ( this.加算合成 ) ? PrimitiveBlend.Add : PrimitiveBlend.SourceOver;

                using var bmp = this._Bitmap.Bitmap;

                dc.DrawBitmap(
                    bitmap: bmp,
                    destinationRectangle: null,
                    opacity: 不透明度0to1,
                    interpolationMode: InterpolationMode.Linear,
                    sourceRectangle: this.転送元矩形,
                    erspectiveTransformRef: 変換行列3D );

            } );
        }



        // ローカル


        private readonly SharpDX.DirectWrite.Factory _DWFactory;

        private string _表示文字列 = "";

        private string _フォント名 = "メイリオ";

        private float _フォントサイズpt = 20f;

        private FontWeight _フォントの太さ = FontWeight.Normal;

        private FontStyle _フォントスタイル = FontStyle.Normal;

        private Color4 _前景色 = Color4.White;

        private Color4 _背景色 = Color4.Black;

        private 効果 _描画効果 = 効果.通常;

        private float _縁のサイズdpx = 6f;

        private WordWrapping _WordWrapping = WordWrapping.Wrap;

        private ParagraphAlignment _ParagraphAlignment = ParagraphAlignment.Near;

        private TextAlignment _TextAlignment = TextAlignment.Leading;

            
        private TextFormat _TextFormat = null!;

        private TextLayout _TextLayout = null!;

        private readonly カスタムTextRenderer _TextRenderer;

        /// <summary>
        ///     TextLayout で作成された文字列のサイズ[dpx]。
        ///     <see cref="画像サイズ"/> と同じか、それより小さい。
        /// </summary>
        private Size2F _表示文字列のサイズdpx = Size2F.Zero;

        private SharpDX.Direct2D1.BitmapRenderTarget _Bitmap;


        private bool _ビットマップを更新せよ;

        private bool _TextFormatを更新せよ;

        private bool _TextLayoutを更新せよ;
    }
}
