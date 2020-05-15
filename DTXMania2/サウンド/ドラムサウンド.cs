using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CSCore;
using FDK;
using SSTFormat.v004;

namespace DTXMania2
{
    /// <summary>
    ///     SSTFにおける既定のドラムサウンド。
    /// </summary>
    class ドラムサウンド : IDisposable
    {
        private const int 多重度 = 4;



        // 生成と終了


        public ドラムサウンド( SoundDevice device )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._SoundDevice = new WeakReference<SoundDevice>( device );
            this._チップtoコンテキスト = new Dictionary<(チップ種別 chipType, int サブチップID), ドラムサウンドコンテキスト>();
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._すべて解放する();
        }

        public void すべて生成する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._すべて解放する();

            lock( this._Sound利用権 )
            {
                // SSTの既定のサウンドを、subChipId = 0 としてプリセット登録する。
                var soundList = new List<(チップ種別 type, VariablePath path)>() {
                    ( チップ種別.LeftCrash, @"$(DrumSounds)\LeftCrash.wav" ),
                    ( チップ種別.Ride, @"$(DrumSounds)\Ride.wav" ),
                    ( チップ種別.Ride_Cup, @"$(DrumSounds)\RideCup.wav" ),
                    ( チップ種別.China, @"$(DrumSounds)\China.wav" ),
                    ( チップ種別.Splash,  @"$(DrumSounds)\Splash.wav" ),
                    ( チップ種別.HiHat_Open, @"$(DrumSounds)\HiHatOpen.wav" ),
                    ( チップ種別.HiHat_HalfOpen, @"$(DrumSounds)\HiHatHalfOpen.wav" ),
                    ( チップ種別.HiHat_Close, @"$(DrumSounds)\HiHatClose.wav" ),
                    ( チップ種別.HiHat_Foot, @"$(DrumSounds)\HiHatFoot.wav" ),
                    ( チップ種別.Snare, @"$(DrumSounds)\Snare.wav" ),
                    ( チップ種別.Snare_OpenRim, @"$(DrumSounds)\SnareOpenRim.wav" ),
                    ( チップ種別.Snare_ClosedRim, @"$(DrumSounds)\SnareClosedRim.wav" ),
                    ( チップ種別.Snare_Ghost, @"$(DrumSounds)\SnareGhost.wav" ),
                    ( チップ種別.Bass, @"$(DrumSounds)\Bass.wav" ),
                    ( チップ種別.Tom1, @"$(DrumSounds)\Tom1.wav" ),
                    ( チップ種別.Tom1_Rim, @"$(DrumSounds)\Tom1Rim.wav" ),
                    ( チップ種別.Tom2, @"$(DrumSounds)\Tom2.wav" ),
                    ( チップ種別.Tom2_Rim, @"$(DrumSounds)\Tom2Rim.wav" ),
                    ( チップ種別.Tom3, @"$(DrumSounds)\Tom3.wav" ),
                    ( チップ種別.Tom3_Rim, @"$(DrumSounds)\Tom3Rim.wav" ),
                    ( チップ種別.RightCrash, @"$(DrumSounds)\RightCrash.wav" ),
                    ( チップ種別.LeftCymbal_Mute, @"$(DrumSounds)\LeftCymbalMute.wav" ),
                    ( チップ種別.RightCymbal_Mute, @"$(DrumSounds)\RightCymbalMute.wav" ),
                };

                foreach( var sound in soundList )
                {
                    if( !File.Exists( sound.path.変数なしパス ) )
                    {
                        Log.ERROR( $"サウンドファイルが存在しません。[{sound.path.変数付きパス}]" );
                        continue;
                    }

                    if( this._SoundDevice.TryGetTarget( out var device ) )
                    {
                        // サウンドファイルを読み込んでデコードする。
                        var sampleSource = SampleSourceFactory.Create( device, sound.path, 1.0 ); // ドラムサウンドは常に 1.0
                        if( sampleSource is null )
                        {
                            Log.ERROR( $"サウンドの生成に失敗しました。[{sound.path.変数付きパス}]" );
                            continue;
                        }

                        // コンテキストを作成する。
                        var context = new ドラムサウンドコンテキスト( sampleSource, ドラムサウンド.多重度 );

                        // 多重度分のサウンドを生成する。
                        for( int i = 0; i < context.Sounds.Length; i++ )
                            context.Sounds[ i ] = new Sound( device, context.SampleSource );

                        // コンテキストを辞書に追加する。
                        if( this._チップtoコンテキスト.ContainsKey( (sound.type, 0) ) )
                        {
                            // すでに辞書に存在してるなら、解放してから削除する。
                            this._チップtoコンテキスト[ (sound.type, 0) ].Dispose();
                            this._チップtoコンテキスト.Remove( (sound.type, 0) );
                        }
                        this._チップtoコンテキスト.Add( (sound.type, 0), context );

                        Log.Info( $"ドラムサウンドを生成しました。[({sound.type.ToString()},0) = {sound.path.変数付きパス}]" );
                    }
                }
            }
        }

        private void _すべて解放する()
        {
            lock( this._Sound利用権 )
            {
                foreach( var kvp in this._チップtoコンテキスト )
                    kvp.Value.Dispose();

                this._チップtoコンテキスト.Clear();
            }
        }



        // 再生


        public void 再生する( チップ種別 chipType, int subChipId, bool 発声前に消音する = false, 消音グループ種別 groupType = 消音グループ種別.Unknown, float 音量0to1 = 1f )
        {
            lock( this._Sound利用権 )
            {
                if( this._チップtoコンテキスト.TryGetValue( (chipType, subChipId), out var context ) )
                {
                    // 必要あれば、指定された消音グループ種別に属するドラムサウンドをすべて停止する。
                    if( 発声前に消音する && groupType != 消音グループ種別.Unknown )
                    {
                        var 停止するコンテキストs = this._チップtoコンテキスト.Where( ( kvp ) => ( kvp.Value.最後に発声したときの消音グループ種別 == groupType ) );

                        foreach( var wavContext in 停止するコンテキストs )
                            foreach( var sound in wavContext.Value.Sounds )
                                sound.Stop();
                    }

                    // 発声する。
                    context.発声する( groupType, 音量0to1 );
                }
                else
                {
                    // コンテキストがないなら何もしない。
                }
            }
        }



        // ローカル


        private class ドラムサウンドコンテキスト : IDisposable
        {
            public ISampleSource SampleSource { get; }
            public Sound[] Sounds { get; }
            public 消音グループ種別 最後に発声したときの消音グループ種別 { get; protected set; }

            public ドラムサウンドコンテキスト( ISampleSource sampleSource, int 多重度 = 4 )
            {
                this._多重度 = 多重度;
                this.SampleSource = sampleSource;
                this.Sounds = new Sound[ 多重度 ];
                this.最後に発声したときの消音グループ種別 = 消音グループ種別.Unknown;
            }
            public void Dispose()
            {
                for( int i = 0; i < this.Sounds.Length; i++ )
                {
                    if( this.Sounds[ i ] is null )
                        continue;

                    if( this.Sounds[ i ].再生中である )
                        this.Sounds[ i ].Stop();

                    this.Sounds[ i ].Dispose();
                }

                this.SampleSource.Dispose();
            }
            public void 発声する( 消音グループ種別 type, float 音量 )
            {
                this.最後に発声したときの消音グループ種別 = type;

                // サウンドを再生する。
                if( null != this.Sounds[ this._次に再生するSound番号 ] )
                {
                    this.Sounds[ this._次に再生するSound番号 ].Volume = 
                        ( 0f > 音量 ) ? 0f :
                        ( 1f < 音量 ) ? 1f : 音量;
                    this.Sounds[ this._次に再生するSound番号 ].Play();
                }

                // サウンドローテーション。
                this._次に再生するSound番号 = ( this._次に再生するSound番号 + 1 ) % this._多重度;
            }

            private readonly int _多重度;
            private int _次に再生するSound番号 = 0;
        };

        private Dictionary<(チップ種別 chipType, int サブチップID), ドラムサウンドコンテキスト> _チップtoコンテキスト;

        private WeakReference<SoundDevice> _SoundDevice;

        private readonly object _Sound利用権 = new object();
    }
}
