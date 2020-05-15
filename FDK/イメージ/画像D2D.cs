using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.IO;
using SharpDX.WIC;

namespace FDK
{
    /// <summary>
    ///     D2Dビットマップを使った画像表示。
    /// </summary>
    public class 画像D2D : IImage, IDisposable
    {

        // プロパティ


        public Bitmap1? Bitmap { get; protected set; } = null;

        public bool 加算合成 { get; set; } = false;

        public Size2F サイズ { get; protected set; }



        // 生成と終了


        public 画像D2D( ImagingFactory2 imagingFactory2, DeviceContext d2dDeviceContext, VariablePath 画像ファイルパス, BitmapProperties1? bitmapProperties1 = null )
        {
            //using var _ = new LogBlock( Log.現在のメソッド名 );

            this.Bitmapを生成する( imagingFactory2, d2dDeviceContext, 画像ファイルパス, bitmapProperties1 );
        }

        protected 画像D2D()
        {
        }

        public virtual void Dispose()
        {
            //using var _ = new LogBlock( Log.現在のメソッド名 );

            this.Bitmap?.Dispose();
            this.Bitmap = null;
        }

        protected void Bitmapを生成する( ImagingFactory2 imagingFactory2, DeviceContext d2dDeviceContext, VariablePath 画像ファイルパス, BitmapProperties1? bitmapProperties1 = null )
        {
            var decoder = (BitmapDecoder) null!;
            var sourceFrame = (BitmapFrameDecode) null!;
            var converter = (FormatConverter) null!;

            try
            {
                // 以下、生成に失敗しても例外は発生しない。ただ描画メソッドで表示されなくなるだけ。

                #region " 事前チェック。"
                //-----------------
                if( string.IsNullOrEmpty( 画像ファイルパス.変数なしパス ) )
                {
                    Log.ERROR( $"画像ファイルパスが null または空文字列です。[{画像ファイルパス.変数付きパス}]" );
                    return;
                }
                if( !File.Exists( 画像ファイルパス.変数なしパス ) )
                {
                    Log.ERROR( $"画像ファイルが存在しません。[{画像ファイルパス.変数付きパス}]" );
                    return;
                }
                //-----------------
                #endregion

                #region " 画像ファイルに対応できるデコーダを見つける。"
                //-----------------
                try
                {
                    decoder = new BitmapDecoder(
                        imagingFactory2,
                        画像ファイルパス.変数なしパス,
                        NativeFileAccess.Read,
                        DecodeOptions.CacheOnLoad );
                }
                catch( SharpDXException e )
                {
                    Log.ERROR( $"画像ファイルに対応するコーデックが見つかりません。(0x{e.HResult:x8})[{画像ファイルパス.変数付きパス}]" );
                    return;
                }
                //-----------------
                #endregion

                #region " 最初のフレームをデコードし、取得する。"
                //-----------------
                try
                {
                    sourceFrame = decoder.GetFrame( 0 );
                }
                catch( SharpDXException e )
                {
                    Log.ERROR( $"画像ファイルの最初のフレームのデコードに失敗しました。(0x{e.HResult:x8})[{画像ファイルパス.変数付きパス}]" );
                    return;
                }
                //-----------------
                #endregion

                #region " 32bitPBGRA へのフォーマットコンバータを生成する。"
                //-----------------
                try
                {
                    // WICイメージングファクトリから新しいコンバータを生成。
                    converter = new FormatConverter( imagingFactory2 );

                    // コンバータに変換元フレームや変換後フォーマットなどを設定。
                    converter.Initialize(
                        sourceRef: sourceFrame,
                        dstFormat: SharpDX.WIC.PixelFormat.Format32bppPBGRA,    // Premultiplied BGRA
                        dither: BitmapDitherType.None,
                        paletteRef: null,
                        alphaThresholdPercent: 0.0,
                        paletteTranslate: BitmapPaletteType.MedianCut );
                }
                catch( SharpDXException e )
                {
                    Log.ERROR( $"32bitPBGRA へのフォーマットコンバータの生成または初期化に失敗しました。(0x{e.HResult:x8})[{画像ファイルパス.変数付きパス}]" );
                    return;
                }
                //-----------------
                #endregion

                #region " コンバータを使って、フレームを WICビットマップ経由で D2D ビットマップに変換する。"
                //-----------------
                try
                {
                    // WIC ビットマップを D2D ビットマップに変換する。
                    this.Bitmap?.Dispose();
                    this.Bitmap = bitmapProperties1 switch
                    {
                        null => Bitmap1.FromWicBitmap( d2dDeviceContext, converter ),
                        _ => Bitmap1.FromWicBitmap( d2dDeviceContext, converter, bitmapProperties1 ),
                    };
                }
                catch( SharpDXException e )
                {
                    Log.ERROR( $"Direct2D1.Bitmap1 への変換に失敗しました。(0x{e.HResult:x8})[{画像ファイルパス.変数付きパス}]" );
                    return;
                }
                //-----------------
                #endregion

                this.サイズ = new Size2F( this.Bitmap.PixelSize.Width, this.Bitmap.PixelSize.Height );
            }
            finally
            {
                converter?.Dispose();
                sourceFrame?.Dispose();
                decoder?.Dispose();
            }
        }



        // 進行と描画


        /// <summary>
        ///		画像を描画する。
        /// </summary>
        /// <param name="dc">描画に使うデバイスコンテキスト。</param>
        /// <param name="左位置">画像の描画先範囲の左上隅X座標。</param>
        /// <param name="上位置">画像の描画先範囲の左上隅Y座標。</param>
        /// <param name="不透明度0to1">不透明度。(0:透明～1:不透明)</param>
        /// <param name="X方向拡大率">画像の横方向の拡大率。</param>
        /// <param name="Y方向拡大率">画像の縦方向の拡大率。</param>
        /// <param name="転送元矩形">画像の転送元範囲。</param>
        /// <param name="描画先矩形を整数境界に合わせる">true なら、描画先の転送先矩形の座標を float から int に丸める。</param>
        /// <param name="変換行列3D">射影行列。</param>
        /// <param name="レイヤーパラメータ></param>
        /// <remarks>
        ///		Direct2D の転送先矩形は float で指定できるが、非整数の値（＝物理ピクセル単位じゃない座標）を渡すと、表示画像がプラスマイナス1pxの範囲で乱れる。
        ///		これにより、数px程度の大きさの画像を移動させるとチカチカする原因になる。
        ///		それが困る場合には、<paramref name="描画先矩形を整数境界に合わせる"/> に true を指定すること。
        ///		ただし、これを true にした場合、タイルのように並べて描画した場合に1pxずれる場合がある。この場合は false にすること。
        /// </remarks>
        public virtual void 描画する( DeviceContext dc, float 左位置, float 上位置, float 不透明度0to1 = 1.0f, float X方向拡大率 = 1.0f, float Y方向拡大率 = 1.0f, RectangleF? 転送元矩形 = null, bool 描画先矩形を整数境界に合わせる = false, Matrix? 変換行列3D = null, LayerParameters1? レイヤーパラメータ = null )
        {
            if( this.Bitmap is null )
                return;

            D2DBatch.Draw( dc, () => {

                dc.PrimitiveBlend = ( this.加算合成 ) ? PrimitiveBlend.Add : PrimitiveBlend.SourceOver;

                転送元矩形 ??= new RectangleF( 0f, 0f, this.サイズ.Width, this.サイズ.Height );

                var 転送先矩形 = new RectangleF(
                    x: 左位置,
                    y: 上位置,
                    width: 転送元矩形.Value.Width * X方向拡大率,
                    height: 転送元矩形.Value.Height * Y方向拡大率 );

                if( 描画先矩形を整数境界に合わせる )
                {
                    転送先矩形.X = (float) Math.Round( 転送先矩形.X );
                    転送先矩形.Y = (float) Math.Round( 転送先矩形.Y );
                    転送先矩形.Width = (float) Math.Round( 転送先矩形.Width );
                    転送先矩形.Height = (float) Math.Round( 転送先矩形.Height );
                }


                // レイヤーパラメータの指定があれば、描画前に Layer を作成して、Push する。
                var layer = (Layer) null!;
                if( レイヤーパラメータ.HasValue )
                {
                    layer = new Layer( dc );    // 因果関係は分からないが、同じBOX内の曲が増えるとこの行の負荷が増大するので、必要時にしか生成しないこと。
                    dc.PushLayer( レイヤーパラメータ.Value, layer );
                }

                // D2Dレンダーターゲットに Bitmap を描画する。
                dc.DrawBitmap(
                    bitmap: this.Bitmap,
                    destinationRectangle: 転送先矩形,
                    opacity: 不透明度0to1,
                    interpolationMode: InterpolationMode.Linear,
                    sourceRectangle: 転送元矩形,
                    erspectiveTransformRef: 変換行列3D ); // null 指定可。

                // レイヤーパラメータの指定があれば、描画後に Pop する。
                if( null != layer )
                    dc.PopLayer();
                layer?.Dispose();

            } );
        }

        /// <summary>
        ///		画像を描画する。
        /// </summary>
        /// <param name="変換行列2D">Transform に適用する行列。</param>
        /// <param name="変換行列3D">射影行列。</param>
        /// <param name="不透明度0to1">不透明度。(0:透明～1:不透明)</param>
        /// <param name="転送元矩形">描画する画像範囲。</param>
        public virtual void 描画する( DeviceContext dc, Matrix3x2? 変換行列2D = null, Matrix? 変換行列3D = null, float 不透明度0to1 = 1.0f, RectangleF? 転送元矩形 = null, LayerParameters1? レイヤーパラメータ = null )
        {
            if( this.Bitmap is null )
                return;

            D2DBatch.Draw( dc, () => {

                var pretrans = dc.Transform;

                dc.Transform = ( 変換行列2D ?? Matrix3x2.Identity ) * pretrans;
                dc.PrimitiveBlend = ( this.加算合成 ) ? PrimitiveBlend.Add : PrimitiveBlend.SourceOver;

                // レイヤーパラメータの指定があれば、描画前に Layer を作成して、Push する。
                
                var layer = (Layer) null!;
                if( レイヤーパラメータ.HasValue )
                {
                    layer = new Layer( dc );    // 因果関係は分からないが、同じBOX内の曲が増えるとこの行の負荷が増大するので、必要時にしか生成しないこと。
                    dc.PushLayer( (LayerParameters1) レイヤーパラメータ, layer );
                }

                // D2Dレンダーターゲットに this.Bitmap を描画する。
                dc.DrawBitmap(
                    bitmap: this.Bitmap,
                    destinationRectangle: null,
                    opacity: 不透明度0to1,
                    interpolationMode: InterpolationMode.Linear,
                    sourceRectangle: 転送元矩形,
                    erspectiveTransformRef: 変換行列3D ); // null 指定可。

                // レイヤーパラメータの指定があれば、描画後に Pop する。
                if( null != レイヤーパラメータ )
                    dc.PopLayer();
                layer?.Dispose();
            } );
        }
    }
}
