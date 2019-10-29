using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace DTXMania2
{
    static class 画面キャプチャ
    {
        /// <summary>
        ///		現在のバックバッファの内容を Bitmap に複写して返す。
        ///		すべての描画が終わったあと、Present() する前に呼び出すこと。
        /// </summary>
        /// <returns>Bitmap。使用後は解放すること。</returns>
        public static Bitmap 取得する()
        {
            // バックバッファの情報を取得する。
            Texture2DDescription backBufferDesc;
            using( var backBuffer = Global.DXGISwapChain1.GetBackBuffer<Texture2D>( 0 ) )
                backBufferDesc = backBuffer.Description;

            // CPUがアクセス可能な Texture2D バッファをGPU上に作成する。
            using var captureTexture = new Texture2D(
                Global.D3D11Device1,
                new Texture2DDescription() {
                    ArraySize = 1,
                    BindFlags = BindFlags.None,
                    CpuAccessFlags = CpuAccessFlags.Read,   // CPUからアクセスできる。
                    Format = backBufferDesc.Format,
                    Height = backBufferDesc.Height,
                    Width = backBufferDesc.Width,
                    MipLevels = 1,
                    OptionFlags = ResourceOptionFlags.None,
                    SampleDescription = new SampleDescription( 1, 0 ),
                    Usage = ResourceUsage.Staging,  // GPU から CPU への copy ができる。
                } );

            // RenderTarget から Texture2D バッファに、GPU上で画像データをコピーする。
            using( var resource = Global.既定のD3D11RenderTargetView.Resource )
                Global.D3D11Device1.ImmediateContext.CopyResource( resource, captureTexture );

            // Texture2D の本体（DXGIサーフェス）から Bitmap を生成する。
            using var dxgiSurface = captureTexture.QueryInterface<Surface>();
            var dataRect = dxgiSurface.Map( SharpDX.DXGI.MapFlags.Read, out DataStream dataStream );
            var bitmap = new Bitmap(
                Global.既定のD2D1DeviceContext,
                new Size2( captureTexture.Description.Width, captureTexture.Description.Height ),
                new DataPointer( dataStream.DataPointer, (int) dataStream.Length ),
                dataRect.Pitch,
                new BitmapProperties(
                    new PixelFormat( Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Ignore ),
                    Global.既定のD2D1DeviceContext.DotsPerInch.Width,
                    Global.既定のD2D1DeviceContext.DotsPerInch.Width ) );
            dxgiSurface.Unmap();

            return bitmap;
        }
    }
}
