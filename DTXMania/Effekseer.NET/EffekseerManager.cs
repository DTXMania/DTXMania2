using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using FDK;

namespace DTXMania
{
    class EffekseerManager : IDisposable
    {
        public EffekseerNET.Manager Manager { get; protected set; } = null;

        public EffekseerNET.Renderer Renderer { get; protected set; } = null;

        public Vector3 カメラの位置
        {
            get => this._カメラの位置;
            set
            {
                this._カメラの位置 = value;
                this._カメラを反映する();
            }
        }

        public Vector3 カメラの注視点
        {
            get => this._カメラの注視点;
            set
            {
                this._カメラの注視点 = value;
                this._カメラを反映する();
            }
        }

        public Vector3 カメラの上方向
        {
            get => this._カメラの上方向;
            set
            {
                this._カメラの上方向 = value;
                this._カメラを反映する();
            }
        }



        // 生成と終了


        public EffekseerManager()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                // 描画用インスタンスを生成する。
                this.Renderer = EffekseerNET.Renderer.Create(
                    DXResources.Instance.D3D11Device1,
                    DXResources.Instance.D3D11Device1.ImmediateContext,
                    squareMaxCount: 2000,
                    depthFunc: SharpDX.Direct3D11.Comparison.Less );

                // エフェクト管理用インスタンスを生成する。
                this.Manager = EffekseerNET.Manager.Create( instance_max: 2000, autoFlip: true );

                // 描画用インスタンスから描画機能を設定する。
                this.Manager.SetSpriteRenderer( this.Renderer.CreateSpriteRenderer() );
                this.Manager.SetRibbonRenderer( this.Renderer.CreateRibbonRenderer() );
                this.Manager.SetRingRenderer( this.Renderer.CreateRingRenderer() );
                this.Manager.SetTrackRenderer( this.Renderer.CreateTrackRenderer() );
                this.Manager.SetModelRenderer( this.Renderer.CreateModelRenderer() );

                // 描画用インスタンスからテクスチャの読込機能を設定する。
                // 独自拡張可能、現在はファイルから読み込んでいる。
                this.Manager.SetTextureLoader( this.Renderer.CreateTextureLoader() );
                this.Manager.SetModelLoader( this.Renderer.CreateModelLoader() );

                // カメラを等倍3D平面に設定する。
                const float 視野角deg = 45.0f;
                var dz = (float) ( DXResources.Instance.設計画面サイズ.Height / ( 4.0 * Math.Tan( MathUtil.DegreesToRadians( 視野角deg / 2.0f ) ) ) );
                this._カメラの位置 = new Vector3( 0f, 0f, -2f * dz );
                this._カメラの注視点 = new Vector3( 0f, 0f, 0f );
                this._カメラの上方向 = new Vector3( 0f, 1f, 0f );
                this._カメラを反映する();

                // 投影行列を等倍3D平面に設定する。
                var 射影行列 = Matrix.PerspectiveFovRH(
                    MathUtil.DegreesToRadians( 視野角deg ),
                    DXResources.Instance.設計画面サイズ.Width / DXResources.Instance.設計画面サイズ.Height,   // アスペクト比
                    -dz,                                            // 前方投影面までの距離
                    +dz );                                          // 後方投影面までの距離

                this.Renderer.SetProjectionMatrix( 射影行列 );
            }
        }

        public virtual void Dispose()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                // 全エフェクトを停止する。
                this.Manager.StopAllEffects();

                // 先にエフェクト管理用インスタンスを破棄する。
                this.Manager?.Destroy();

                // 次に描画用インスタンスを破棄する。
                this.Renderer?.Destroy();
            }
        }



        // 進行と描画


        public void 進行描画する()
        {
            this.Manager.Update( deltaFrame: 1f );

            var d3ddc = DXResources.Instance.D3D11Device1.ImmediateContext;
            d3ddc.HullShader.Set( null );
            d3ddc.DomainShader.Set( null );
            d3ddc.GeometryShader.Set( null );

            this.Renderer.BeginRendering();
            this.Manager.Draw();
            this.Renderer.EndRendering();
        }


        private Vector3 _カメラの位置 = new Vector3( 0f, 0f, -10f );

        private Vector3 _カメラの注視点 = new Vector3( 0f, 0f, 0f );

        private Vector3 _カメラの上方向 = new Vector3( 0f, 1f, 0f );


        private void _カメラを反映する()
        {
            // 右手系
            this.Renderer.SetCameraMatrix( Matrix.LookAtRH( this._カメラの位置, this._カメラの注視点, this._カメラの上方向 ) );
        }
    }
}
