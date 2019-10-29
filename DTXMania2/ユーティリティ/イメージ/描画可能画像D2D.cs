using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct2D1;

namespace DTXMania2
{
    /// <summary>
    ///		レンダーターゲットとしても描画可能なビットマップを扱うクラス。
    /// </summary>
    class 描画可能画像D2D : 画像D2D
    {

        // 生成と終了


        public 描画可能画像D2D( VariablePath 画像ファイルパス )
            : base( 画像ファイルパス, new BitmapProperties1 { BitmapOptions = BitmapOptions.Target } )
        {
            //using var _ = new LogBlock( Log.現在のメソッド名 );
        }

        public 描画可能画像D2D( Size2F サイズ )
            : base()
        {
            //using var _ = new LogBlock( Log.現在のメソッド名 );

            // 空のビットマップを生成する。
            this.Bitmap = new Bitmap1(
                Global.既定のD2D1DeviceContext,
                new Size2( (int) サイズ.Width, (int) サイズ.Height ),
                new BitmapProperties1() {
                    PixelFormat = new PixelFormat( Global.既定のD2D1DeviceContext.PixelFormat.Format, AlphaMode.Premultiplied ),
                    BitmapOptions = BitmapOptions.Target,
                } );

            this.サイズ = サイズ;
        }

        public 描画可能画像D2D( float width, float height )
            : this( new Size2F( width, height ) )
        {
        }



        // 進行と描画


        /// <summary>
        ///		生成済み画像（ビットマップ）に対するユーザアクションによる描画を行う。
        /// </summary>
        /// <remarks>
        ///		ユーザアクション内では BeginDraw(), EndDraw() の呼び出しは（呼び出しもとでやるので）不要。
        /// </remarks>
        /// <param name="描画アクション">Bitmap に対して行いたい操作。</param>
        public void 画像へ描画する( Action<DeviceContext> 描画アクション )
        {
            var dc = Global.既定のD2D1DeviceContext;

            Global.D2DBatchDraw( dc, () => {

                dc.Target = this.Bitmap;            // 描画先
                dc.Transform = Matrix3x2.Identity;  // 等倍（dpx to dpx）
                dc.PrimitiveBlend = PrimitiveBlend.SourceOver;

                描画アクション( dc );

                dc.Target = Global.既定のD2D1RenderBitmap1;

            } );
        }
    }
}
