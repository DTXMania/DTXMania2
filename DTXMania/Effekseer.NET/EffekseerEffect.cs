using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;

namespace DTXMania
{
    class EffekseerEffect : IDisposable
    {

        // 生成と終了


        public EffekseerEffect( EffekseerManager manager, VariablePath path, float magnification = 1.0f, string materialPath = null )
        {
            this._Manager = new WeakReference<EffekseerManager>( manager );
            this._Effect = EffekseerNET.Effect.Create( manager.Manager, path.変数なしパス, magnification, materialPath );

            Log.Info( $"Effekseerエフェクトを生成しました。[{path.変数付きパス}]" );
        }

        public virtual void Dispose()
        {
            this._Effect?.Dispose();
            this._Manager = null;
        }



        // 進行と描画


        public void Play( float x, float y, float z )
        {
            this.Stop();

            if( this._Manager.TryGetTarget( out var manager ) )
            {
                this._EffectHandle = manager.Manager.Play( this._Effect, x, y, z );
            }
        }

        public void Stop()
        {
            if( -1 != this._EffectHandle &&
                this._Manager.TryGetTarget( out var manager ) )
            {
                manager.Manager.StopEffect( this._EffectHandle );
                this._EffectHandle = -1;
            }
        }



        // ローカル


        private WeakReference<EffekseerManager> _Manager;

        private EffekseerNET.Effect _Effect;

        private int _EffectHandle = -1;
    }
}
