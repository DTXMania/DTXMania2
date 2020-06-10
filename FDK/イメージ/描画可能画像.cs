using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;

namespace FDK
{
    /// <summary>
    ///		D2Dビットマップとメモリを共有するD3Dテクスチャを持つ画像。
    ///		D2Dビットマップに対してD2Dで描画を行い、それをD3Dテクスチャとして表示する。
    /// </summary>
    public class 描画可能画像 : 画像
    {

        // プロパティ


        /// <summary>
        ///     画像のD3Dテクスチャとメモリを共有するD2Dビットマップ。
        /// </summary>
        public Bitmap1 Bitmap { get; protected set; } = null!;



        // 生成と終了


        /// <summary>
        ///     指定した画像ファイルから描画可能画像を作成する。
        /// </summary>
        public 描画可能画像( SharpDX.Direct3D11.Device1 d3dDevice1, SharpDX.Direct2D1.DeviceContext d2dDeviceContext, VariablePath 画像ファイルパス, BindFlags bindFlags = BindFlags.ShaderResource )
            : base( d3dDevice1, 画像ファイルパス, bindFlags | BindFlags.RenderTarget )
        {
            //using var _ = new LogBlock( Log.現在のメソッド名 );

            this.Bitmap = this._作成したテクスチャとデータを共有するビットマップターゲットを作成する( d2dDeviceContext );
        }

        /// <summary>
        ///     指定したサイズの、空の描画可能画像を作成する。
        /// </summary>
        public 描画可能画像( SharpDX.Direct3D11.Device1 d3dDevice1, SharpDX.Direct2D1.DeviceContext d2dDeviceContext, Size2F サイズ, BindFlags bindFlags = BindFlags.ShaderResource )
            : base( d3dDevice1, サイズ, bindFlags | BindFlags.RenderTarget )
        {
            //using var _ = new LogBlock( Log.現在のメソッド名 );

            this.Bitmap = this._作成したテクスチャとデータを共有するビットマップターゲットを作成する( d2dDeviceContext );
        }

        public override void Dispose()
        {
            //using var _ = new LogBlock( Log.現在のメソッド名 );

            this.Bitmap.Dispose();

            base.Dispose();
        }



        // ローカル


        private Bitmap1 _作成したテクスチャとデータを共有するビットマップターゲットを作成する( SharpDX.Direct2D1.DeviceContext d2dDeviceContext )
        {
            using var dxgiSurface = this.Texture.QueryInterfaceOrNull<SharpDX.DXGI.Surface>();

            var bmpProp = new BitmapProperties1() {
                PixelFormat = new PixelFormat( dxgiSurface.Description.Format, AlphaMode.Premultiplied ),
                BitmapOptions = BitmapOptions.Target | BitmapOptions.CannotDraw,
            };

            return new Bitmap1( d2dDeviceContext, dxgiSurface, bmpProp );
        }
    }
}
