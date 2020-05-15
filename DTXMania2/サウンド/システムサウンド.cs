using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CSCore;
using FDK;

namespace DTXMania2
{
    class システムサウンド : IDisposable
    {

        // 生成と終了


        public システムサウンド( SoundDevice device )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._SoundDevice = new WeakReference<SoundDevice>( device );
            this._種別toサウンドマップ = new Dictionary<システムサウンド種別, (ISampleSource source, PolySound sound)>();
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            foreach( var kvp in this._種別toサウンドマップ )
                this.解放する( kvp.Key );
        }

        /// <summary>
        ///     すべてのシステムサウンドを生成する。
        /// </summary>
        /// <remarks>
        ///     既に生成済みのサウンドは再生成しない。
        /// </remarks>
        public void すべて生成する()
        {
            foreach( システムサウンド種別? type in Enum.GetValues( typeof( システムサウンド種別 ) ) )
            {
                if( type.HasValue )
                    this.読み込む( type.Value );
            }
        }

        /// <summary>
        ///     指定されたシステムサウンドを生成する。
        /// </summary>
        /// <remarks>
        ///     既に生成済みのサウンドは再生成しない。
        /// </remarks>
        public void 読み込む( システムサウンド種別 type )
        {
            // 既に生成済みなら何もしない。
            if( this._種別toサウンドマップ.ContainsKey( type ) )
                return;

            // ファイル名は、<Alias>.ogg とする。
            var path = new VariablePath( $@"$(SystemSounds)\{type.GetAlias()}.ogg" );

            // ファイルがないなら無視。
            if( !File.Exists( path.変数なしパス ) )
            {
                Log.ERROR( $"システムサウンドファイルが見つかりません。スキップします。[{path.変数付きパス}]" );
                return;
            }

            if( this._SoundDevice.TryGetTarget( out var device ) )
            {
                // サンプルソースを読み込む。
                var sampleSource = SampleSourceFactory.Create( device, path.変数なしパス, 1.0 ) ??  // システムサウンドは常に再生速度 = 1.0
                    throw new Exception( $"システムサウンドの読み込みに失敗しました。[{path.変数付きパス}]" );

                // サウンドを生成してマップに登録。
                var sound = new PolySound( device, sampleSource, 2 );
                this._種別toサウンドマップ[ type ] = (sampleSource, sound);
            }

            Log.Info( $"システムサウンドを読み込みました。[{path.変数付きパス}]" );
        }

        /// <summary>
        ///     指定されたシステムサウンドを解放する。
        /// </summary>
        public void 解放する( システムサウンド種別 type )
        {
            if( this._種別toサウンドマップ.ContainsKey( type ) )
            {
                this._種別toサウンドマップ[ type ].sound.Dispose();
                this._種別toサウンドマップ[ type ].source.Dispose();
                this._種別toサウンドマップ.Remove( type );
            }
        }



        // 再生と停止


        public void 再生する( システムサウンド種別 type, bool ループ再生する = false )
        {
            if( this._種別toサウンドマップ.TryGetValue( type, out var map ) )
                map.sound?.Play( 0, ループ再生する );
        }

        public void 停止する( システムサウンド種別 type )
        {
            if( this._種別toサウンドマップ.TryGetValue( type, out var map ) )
                map.sound?.Stop();
        }

        public bool 再生中( システムサウンド種別 type )
        {
            return this._種別toサウンドマップ.TryGetValue( type, out var map ) && map.sound.いずれかが再生中である;
        }



        // ローカル


        private WeakReference<SoundDevice> _SoundDevice;

        private Dictionary<システムサウンド種別, (ISampleSource source, PolySound sound)> _種別toサウンドマップ;

    }
}
