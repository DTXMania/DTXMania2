using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;

namespace DTXMania2
{
    /// <summary>
    ///		D3Dテクスチャとメモリを共有するD2Dビットマップを持つテクスチャ。
    ///		D2Dビットマップに対して描画を行えば、それをD3Dテクスチャとして表示することができる。
    /// </summary>
    class 描画可能画像 : 画像
    {

        // 生成と終了


        /// <summary>
        ///     指定した画像ファイルから描画可能画像を作成する。
        /// </summary>
        public 描画可能画像( VariablePath 画像ファイルパス, BindFlags bindFlags = BindFlags.ShaderResource )
            : base( 画像ファイルパス, bindFlags | BindFlags.RenderTarget )
        {
            //using var _ = new LogBlock( Log.現在のメソッド名 );

            this._Bitmap = this._作成したテクスチャとデータを共有するビットマップターゲットを作成する();
        }

        /// <summary>
        ///     指定したサイズの、空の描画可能画像を作成する。
        /// </summary>
        public 描画可能画像( Size2F サイズ, BindFlags bindFlags = BindFlags.ShaderResource )
            : base( サイズ, bindFlags | BindFlags.RenderTarget )
        {
            //using var _ = new LogBlock( Log.現在のメソッド名 );

            this._Bitmap = this._作成したテクスチャとデータを共有するビットマップターゲットを作成する();
        }

        public override void Dispose()
        {
            //using var _ = new LogBlock( Log.現在のメソッド名 );

            this._Bitmap.Dispose();
            base.Dispose();
        }



        // 進行と描画


        public void 画像へ描画する( Action<SharpDX.Direct2D1.DeviceContext> 描画アクション )
        {
            var dc = Global.既定のD2D1DeviceContext;

            Global.D2DBatchDraw( dc, () => {

                dc.Target = this._Bitmap;           // 描画先
                dc.Transform = Matrix3x2.Identity;  // 等倍描画（dpx to dpx）
                dc.PrimitiveBlend = PrimitiveBlend.SourceOver;

                描画アクション( dc );

                dc.Target = Global.既定のD2D1RenderBitmap1;

            } );
        }



        // ローカル


        private Bitmap1 _Bitmap;

        private Bitmap1 _作成したテクスチャとデータを共有するビットマップターゲットを作成する()
        {
            using var dxgiSurface = this.Texture.QueryInterfaceOrNull<SharpDX.DXGI.Surface>();

            var bmpProp = new BitmapProperties1() {
                PixelFormat = new PixelFormat( dxgiSurface.Description.Format, AlphaMode.Premultiplied ),
                BitmapOptions = BitmapOptions.Target | BitmapOptions.CannotDraw,
            };

            return new Bitmap1( Global.既定のD2D1DeviceContext, dxgiSurface, bmpProp );
        }
    }
}
