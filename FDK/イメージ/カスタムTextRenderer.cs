using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;

namespace FDK
{
    /// <summary>
    ///     縁取り文字やドロップシャドウなどに対応した、自分用のTextRenderer。
    /// </summary>
    public class カスタムTextRenderer : TextRendererBase
    {
        #region " class DrawingEffect "
        //----------------
        /// <summary>
        ///     描画オプション。
        ///     <see cref="TextLayout.SetDrawingEffect"/>で、任意の範囲の文字列に対して指定できる。
        /// </summary>
        public class DrawingEffect : ComObject
        {
            public RenderTarget? renderTarget
            {
                get
                    => ( null != this._wrRenderTarget && this._wrRenderTarget.TryGetTarget( out RenderTarget? rt ) ) ? rt : null;
                set
                    => this._wrRenderTarget = ( null != value ) ? new WeakReference<RenderTarget>( value ) : null;
            }
            public Color4 文字の色 = Color.White;
            public Color4 背景の色 = Color.Transparent;

            public DrawingEffect()
                : this( null! )
            {
            }
            public DrawingEffect( RenderTarget 描画先 )
            {
                this.renderTarget = 描画先;    // null OK
            }

            // COM参照カウンタを増やすのはなんかイヤなので、代わりに弱参照を保持。
            private WeakReference<RenderTarget>? _wrRenderTarget;
        }
        //----------------
        #endregion

        #region " class 縁取りDrawingEffect "
        //----------------
        /// <summary>
        ///     描画オプション（縁取り文字）。
        ///     <see cref="TextLayout.SetDrawingEffect"/>で、任意の範囲の文字列に対して適用される。
        /// </summary>
        public class 縁取りDrawingEffect : DrawingEffect
        {
            public Color4 縁の色 = Color.Black;
            public float 縁の太さ = 3.0f;

            public 縁取りDrawingEffect()
                : this( null! )
            {
            }
            public 縁取りDrawingEffect( RenderTarget 描画先 )
                : base( 描画先 )
            {
            }
        }
        //----------------
        #endregion

        #region " class ドロップシャドウDrawingEffect "
        //----------------
        /// <summary>
        ///     描画オプション（ドロップシャドウ文字）。
        ///     <see cref="TextLayout.SetDrawingEffect"/>で、任意の範囲の文字列に対して適用される。
        /// </summary>
        public class ドロップシャドウDrawingEffect : DrawingEffect
        {
            public Color4 影の色 = Color.Black;
            public float 影の距離 = 1.0f;

            public ドロップシャドウDrawingEffect()
                : this( null! )
            {
            }
            public ドロップシャドウDrawingEffect( RenderTarget 描画先 )
                : base( 描画先 )
            {
            }
        }
        //----------------
        #endregion


        public カスタムTextRenderer( SharpDX.Direct2D1.Factory1 d2dFactory1, DeviceContext d2dDeviceContext, Color4 既定の文字色, Color4 既定の背景色 )
            : base()
        {
            this._D2DFactory1 = d2dFactory1;
            this._D2DDeviceContext = d2dDeviceContext;
            this._既定の文字色 = 既定の文字色;
            this._既定の背景色 = 既定の背景色;
        }

        protected override void Dispose( bool disposing )
        {
            // ↓これをコメントアウトすると Null参照例外が発生する（.NET Core 3.0 のみ？）
            //base.Dispose( disposing );
        }

        /// <summary>
        ///     グリフ実行を描画する際に、<see cref="TextLayout.Draw"/> から呼び出されるコールバック。
        /// </summary>
        /// <param name="clientDrawingContext"><see cref="TextLayout.Draw(object, TextRenderer, float, float)"/>で渡された、アプリ定義の描画コンテキスト。</param>
        /// <param name="baselineOriginX">グリフ実行のベースライン原点のピクセル位置（X座標）。</param>
        /// <param name="baselineOriginY">グリフ実行のベースライン原点のピクセル位置（Y座標）。</param>
        /// <param name="measuringMode">実行中にグリフを測定する方法。他のプロパティとともに、描画モードを決定するために使われる。</param>
        /// <param name="glyphRun">描画するグリフ実行。</param>
        /// <param name="glyphRunDescription">グリフ実行記述。オプション。この実行に関連する文字のプロパティを持つ。</param>
        /// <param name="clientDrawingEffect">非 null なら、TextLayout.SetDrawingEffect() で設定されている IUnknown 派生オブジェクトが格納されている。</param>
        /// <returns>成功すれば <see cref="Result.Ok"/>, 失敗したら <see cref="Result.Fail"/>。</returns>
        public override Result DrawGlyphRun( object clientDrawingContext, float baselineOriginX, float baselineOriginY, MeasuringMode measuringMode, GlyphRun glyphRun, GlyphRunDescription glyphRunDescription, ComObject clientDrawingEffect )
        {
            using var パスジオメトリ = new PathGeometry( this._D2DFactory1 );

            // グリフ実行の輪郭をパスジオメトリとして取得。
            using( var sink = パスジオメトリ.Open() )
            {
                glyphRun.FontFace.GetGlyphRunOutline(
                    glyphRun.FontSize,
                    glyphRun.Indices,
                    glyphRun.Advances,
                    glyphRun.Offsets,
                    glyphRun.Indices?.Count() ?? 0,
                    glyphRun.IsSideways,
                    ( 1 == ( glyphRun.BidiLevel % 2 ) ),    // 奇数ならtrue
                    sink );
                sink.Close();
            }

            // ジオメトリを描画する。
            var 変換行列 = Matrix3x2.Translation( baselineOriginX, baselineOriginY );   // ベースラインの原点まで移動
            using var 原点移動済みパスジオメトリ = new TransformedGeometry( this._D2DFactory1, パスジオメトリ, 変換行列 );
            bool drawingEffectを解放する = false;

            #region " DrawingEffect の指定があれば受け取る。なければ既定の値で作る。"
            //----------------
            var drawingEffect =
                ( clientDrawingContext as DrawingEffect ) ??    // Context 優先にしておく
                ( clientDrawingEffect as DrawingEffect );

            if( drawingEffect is null )
            {
                drawingEffect = new DrawingEffect( this._D2DDeviceContext ) {
                    文字の色 = this._既定の文字色,
                    背景の色 = this._既定の背景色,
                };

                drawingEffectを解放する = true;
            }
            //----------------
            #endregion

            var renderTarget =
                drawingEffect.renderTarget ??    // 指定されたレンダーターゲットを使う。
                this._D2DDeviceContext;          // 指定がなければ既定のDC。

            this._現在の変換行列 = renderTarget.Transform;
            this._現在のDPI = renderTarget.DotsPerInch.Width;

            #region " 背景を描画。"
            //----------------
            if( drawingEffect.背景の色 != Color.Transparent )
            {
                using var 背景ブラシ = new SolidColorBrush( renderTarget, drawingEffect.背景の色 );

                float 送り幅の合計 = 0f;
                foreach( float 送り幅 in glyphRun.Advances )
                    送り幅の合計 += 送り幅;

                var rc = new RectangleF() {
                    Left = baselineOriginX,
                    Top = baselineOriginY - glyphRun.FontSize * glyphRun.FontFace.Metrics.Ascent / glyphRun.FontFace.Metrics.DesignUnitsPerEm,
                    Right = baselineOriginX + 送り幅の合計,
                    Bottom = baselineOriginY + glyphRun.FontSize * glyphRun.FontFace.Metrics.Descent / glyphRun.FontFace.Metrics.DesignUnitsPerEm,
                };

                // 塗りつぶす。
                renderTarget.FillRectangle( rc, 背景ブラシ );
            }
            //----------------
            #endregion

            #region " 文字を描画。"
            //----------------
            switch( drawingEffect )
            {
                case 縁取りDrawingEffect effect:
                    using( var 縁ブラシ = new SolidColorBrush( renderTarget, effect.縁の色 ) )
                    using( var 文字ブラシ = new SolidColorBrush( renderTarget, effect.文字の色 ) )
                    using( var strokeStyle = new StrokeStyle( this._D2DFactory1, new StrokeStyleProperties() { LineJoin = LineJoin.Miter } ) )    // 突き抜け防止
                    {
                        renderTarget.DrawGeometry( 原点移動済みパスジオメトリ, 縁ブラシ, effect.縁の太さ, strokeStyle );
                        renderTarget.FillGeometry( 原点移動済みパスジオメトリ, 文字ブラシ );
                    }
                    break;

                case ドロップシャドウDrawingEffect effect:
                    using( var 影ブラシ = new SolidColorBrush( renderTarget, effect.影の色 ) )
                    using( var 文字ブラシ = new SolidColorBrush( renderTarget, effect.文字の色 ) )
                    {
                        var 影の変換行列 = 変換行列 * Matrix3x2.Translation( effect.影の距離, effect.影の距離 );
                        using var 影のパスジオメトリ = new TransformedGeometry( this._D2DFactory1, パスジオメトリ, 影の変換行列 );
                        renderTarget.FillGeometry( 影のパスジオメトリ, 影ブラシ );
                        renderTarget.FillGeometry( 原点移動済みパスジオメトリ, 文字ブラシ );
                    }
                    break;

                case DrawingEffect effect:
                    using( var 文字ブラシ = new SolidColorBrush( renderTarget, effect.文字の色 ) )
                    {
                        renderTarget.FillGeometry( 原点移動済みパスジオメトリ, 文字ブラシ );
                    }
                    break;

                default:
                    throw new ArgumentException( "未知の DrawingEffect が指定されました。" );
            }
            //----------------
            #endregion

            if( drawingEffectを解放する )
                drawingEffect.Dispose();    // 作った場合は解放する。

            return Result.Ok;
        }

        /// <summary>
        ///     インラインオブジェクトを描画する際に、<see cref="TextLayout.Draw"/>から呼び出されるコールバック。
        /// </summary>
        /// <param name="clientDrawingContext"></param>
        /// <param name="originX"></param>
        /// <param name="originY"></param>
        /// <param name="inlineObject"></param>
        /// <param name="isSideways"></param>
        /// <param name="isRightToLeft"></param>
        /// <param name="clientDrawingEffect"></param>
        /// <returns></returns>
        public override Result DrawInlineObject( object clientDrawingContext, float originX, float originY, InlineObject inlineObject, bool isSideways, bool isRightToLeft, ComObject clientDrawingEffect )
        {
            // 未対応。
            return base.DrawInlineObject( clientDrawingContext, originX, originY, inlineObject, isSideways, isRightToLeft, clientDrawingEffect );
        }

        /// <summary>
        ///     打ち消し文字を描画する際に、<see cref="TextLayout.Draw"/>から呼び出されるコールバック。
        /// </summary>
        /// <param name="clientDrawingContext"></param>
        /// <param name="baselineOriginX"></param>
        /// <param name="baselineOriginY"></param>
        /// <param name="strikethrough"></param>
        /// <param name="clientDrawingEffect"></param>
        /// <returns></returns>
        public override Result DrawStrikethrough( object clientDrawingContext, float baselineOriginX, float baselineOriginY, ref Strikethrough strikethrough, ComObject clientDrawingEffect )
        {
            // 未対応。
            return base.DrawStrikethrough( clientDrawingContext, baselineOriginX, baselineOriginY, ref strikethrough, clientDrawingEffect );
        }

        /// <summary>
        ///     下線付き文字を描画する際に、<see cref="TextLayout.Draw"/>から呼び出されるコールバック。
        /// </summary>
        /// <param name="clientDrawingContext"></param>
        /// <param name="baselineOriginX"></param>
        /// <param name="baselineOriginY"></param>
        /// <param name="underline"></param>
        /// <param name="clientDrawingEffect"></param>
        /// <returns></returns>
        public override Result DrawUnderline( object clientDrawingContext, float baselineOriginX, float baselineOriginY, ref Underline underline, ComObject clientDrawingEffect )
        {
            // 未対応。
            return base.DrawUnderline( clientDrawingContext, baselineOriginX, baselineOriginY, ref underline, clientDrawingEffect );
        }

        /// <summary>
        ///     ピクセルスナッピングが無効か否かを表す。
        ///     サブピクセルバーティカルプレースメントのアニメーションを行わない限り、推奨される既定値は false である。
        /// </summary>
        /// <param name="clientDrawingContext"></param>
        /// <returns>ピクセルスナッピングが無効なら true 、有効なら false。</returns>
        public override bool IsPixelSnappingDisabled( object clientDrawingContext ) => false;

        /// <summary>
        ///     抽象座標から DIP への変形行列を返す。
        /// </summary>
        /// <param name="clientDrawingContext"></param>
        /// <returns></returns>
        public override RawMatrix3x2 GetCurrentTransform( object clientDrawingContext ) => this._現在の変換行列;

        /// <summary>
        ///     DIP ごとの物理ピクセル数を返す。
        /// </summary>
        /// <param name="clientDrawingContext"></param>
        /// <returns></returns>
        public override float GetPixelsPerDip( object clientDrawingContext ) => this._現在のDPI;


        private SharpDX.Direct2D1.Factory1 _D2DFactory1;

        private SharpDX.Direct2D1.DeviceContext _D2DDeviceContext;

        private Color4 _既定の文字色;

        private Color4 _既定の背景色;

        private Matrix3x2 _現在の変換行列 = Matrix3x2.Identity;

        private float _現在のDPI = 96f;
    }
}
