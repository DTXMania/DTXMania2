using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSCore;

namespace DTXMania2
{
    /// <summary>
    ///     多重再生ができるSound。
    /// </summary>
    class PolySound : IDisposable
    {

        // プロパティ


        /// <summary>
        ///		音量。0.0(無音)～1.0(原音)～...上限なし
        /// </summary>
        /// <remarks>
        ///		このクラスではなく、<see cref="Mixer"/>クラスから参照して使用する。
        /// </remarks>
        public float Volume
        {
            get => this.サウンドリスト[ 0 ].Volume;    // 代表
            set
            {
                float vol = Math.Max( value, 0f );

                foreach( var sound in this.サウンドリスト )    // 全部同じ音量に
                    sound.Volume = vol;
            }
        }

        public bool いずれかが再生中である
            => this.サウンドリスト.Any( ( sound ) => sound.再生中である );



        // 生成と終了


        public PolySound( SoundDevice device, ISampleSource sampleSource, int 多重度 = 4 )
        {
            this.多重度 = 多重度;
            this.サウンドリスト = new Sound[ 多重度 ];

            // 多重度数だけ Sound を同じソースで生成。
            for( int i = 0; i < 多重度; i++ )
                this.サウンドリスト[ i ] = new Sound( device, sampleSource );
        }

        public void Dispose()
        {
            foreach( var sound in this.サウンドリスト )
                sound.Dispose();
        }



        // 再生制御


        public void Play( long 再生開始位置frame = 0, bool ループ再生する = false )
        {
            // サウンドを再生する。
            this.サウンドリスト[ this._次に再生するサウンドのインデックス ].Play( 再生開始位置frame, ループ再生する );

            // サウンドローテーション。
            if( !ループ再生する )  // ループ再生時はローテーションしない。
                this._次に再生するサウンドのインデックス = ( this._次に再生するサウンドのインデックス + 1 ) % this.多重度;
        }

        public void Play( double 再生開始位置sec, bool ループ再生する = false )
            => this.Play( this._秒ToFrame( 再生開始位置sec ), ループ再生する );

        public void Stop()
        {
            foreach( var sound in this.サウンドリスト )
                sound.Stop();
        }



        // ローカル


        protected int 多重度;

        protected Sound[] サウンドリスト;

        private int _次に再生するサウンドのインデックス = 0;


        private long _秒ToFrame( double 時間sec )
            => this.サウンドリスト[ 0 ].秒ToFrame( 時間sec );
    }
}
