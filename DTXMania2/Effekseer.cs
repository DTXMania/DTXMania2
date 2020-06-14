using System;
using System.Collections.Generic;
using System.Text;
using FDK;

namespace DTXMania2
{
    class Effekseer : IDisposable
    {

        // プロパティ


        public EffekseerNET.Manager Manager { get; private set; } = null!;
        
        public EffekseerRendererDX11NET.Renderer Renderer { get; private set; } = null!;



        // 生成と終了


        public Effekseer( SharpDX.Direct3D11.Device1 d3dDevice1, SharpDX.Direct3D11.DeviceContext d3ddc, float width, float height )
        {
            Renderer = EffekseerRendererDX11NET.Renderer.Create( d3dDevice1.NativePointer, d3ddc.NativePointer, squareMaxCount: 8000 );
            Manager = EffekseerNET.Manager.Create( instance_max: 8000 );

            Manager.SetSpriteRenderer( Renderer.CreateSpriteRenderer() );
            Manager.SetRibbonRenderer( Renderer.CreateRibbonRenderer() );
            Manager.SetRingRenderer( Renderer.CreateRingRenderer() );
            Manager.SetTrackRenderer( Renderer.CreateTrackRenderer() );
            Manager.SetModelRenderer( Renderer.CreateModelRenderer() );

            Manager.SetTextureLoader( Renderer.CreateTextureLoader() );
            Manager.SetModelLoader( Renderer.CreateModelLoader() );
            Manager.SetMaterialLoader( Renderer.CreateMaterialLoader() );

            Renderer.SetProjectionMatrix(
                new EffekseerNET.Matrix44().PerspectiveFovRH(
                    10.0f / 180.0f * MathF.PI,  // 視野角10°；2Dにあわせるため、なるべく小さくして歪みを少なくする。
                    width / height,
                    1.0f, 500.0f ) );

            Renderer.SetCameraMatrix(
                new EffekseerNET.Matrix44().LookAtRH(
                    eye: new EffekseerNET.Vector3D( 0.0f, 0f, 50.0f ),
                    at: new EffekseerNET.Vector3D( 0.0f, 0.0f, 0.0f ),
                    up: new EffekseerNET.Vector3D( 0.0f, 1.0f, 0.0f ) ) );

            this._前回の更新時刻 = QPCTimer.生カウント;
        }

        public virtual void Dispose()
        {
            Manager.Destroy();  // Dispose じゃないので注意
            Renderer.Destroy(); // 
        }



        // 進行と描画


        public void 進行する()
        {
            long 現在時刻 = QPCTimer.生カウント;
            double 経過時間sec = QPCTimer.生カウント相対値を秒へ変換して返す( 現在時刻 - this._前回の更新時刻 );
            this._前回の更新時刻 = 現在時刻;

            Manager.Update( (float) ( 経過時間sec * 60.0 ) );   // Effekseerは毎秒60フレームで固定
        }

        public void 描画する()
        {
            Global.GraphicResources.既定のD3D11DeviceContext.OutputMerger.SetRenderTargets( Global.GraphicResources.既定のD3D11DepthStencilView, Global.GraphicResources.既定のD3D11RenderTargetView );
            Global.GraphicResources.既定のD3D11DeviceContext.Rasterizer.SetViewports( Global.GraphicResources.既定のD3D11ViewPort );

            Renderer.BeginRendering();
            Manager.Draw();
            Renderer.EndRendering();
        }

        public void 描画する( int effectHandle )
        {
            Global.GraphicResources.既定のD3D11DeviceContext.OutputMerger.SetRenderTargets( Global.GraphicResources.既定のD3D11DepthStencilView, Global.GraphicResources.既定のD3D11RenderTargetView );
            Global.GraphicResources.既定のD3D11DeviceContext.Rasterizer.SetViewports( Global.GraphicResources.既定のD3D11ViewPort );

            Renderer.BeginRendering();
            Manager.DrawHandle( effectHandle );
            Renderer.EndRendering();
        }



        // ローカル


        private long _前回の更新時刻 = 0;
    }
}
