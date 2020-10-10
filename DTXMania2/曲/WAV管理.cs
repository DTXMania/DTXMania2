using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CSCore;
using FDK;
using SSTF=SSTFormat.v004;

namespace DTXMania2
{
    /// <summary>
    ///		<see cref="スコア.WAVリスト"/> の各サウンドインスタンスを管理する。
    ///		サウンドの作成には <see cref="App.WAVキャッシュ"/> を使用する。
    /// </summary>
    class WAV管理 : IDisposable
    {

        // 生成と終了


        /// <param name="多重度">
        ///     １サウンドの最大多重発声数。1以上。
        /// </param>
        public WAV管理( int 多重度 = 4 )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            if( 1 > 多重度 )
                throw new ArgumentException( $"多重度が1未満に設定されています。[{多重度}]" );

            this._既定の多重度 = 多重度;
            this._WAV情報リスト = new Dictionary<int, WAV情報>();
            this._一時停止中の音声のリスト = new List<Sound>();
        }

        public void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._一時停止中の音声のリスト.Clear();  // 参照なので要素はDisposeしない

            if( null != this._WAV情報リスト )
            {
                foreach( var value in this._WAV情報リスト.Values )
                    value.Dispose();

                this._WAV情報リスト.Clear();
            }
        }



        // 登録、発声停止、再開


        /// <summary>
        ///		指定したWAV番号にサウンドファイルを登録する。
        /// </summary>
        /// <remarks>
        ///     サウンドの生成に失敗した場合には登録を行わない。
        /// </remarks>
        /// <param name="wav番号">登録する番号。0～1295。すでに登録されている場合は上書き更新される。</param>
        /// <param name="サウンドファイル">登録するサウンドファイルのパス。</param>
        public void 登録する( int wav番号, VariablePath サウンドファイル, bool 多重再生する, bool BGMである )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            if( ( 0 > wav番号 ) || ( 36 * 36 <= wav番号 ) )
                throw new ArgumentOutOfRangeException( $"WAV番号が範囲(0～1295)を超えています。[{wav番号}]" );

            if( !( File.Exists( サウンドファイル.変数なしパス ) ) )
            {
                Log.WARNING( $"サウンドファイルが存在しません。[{サウンドファイル.変数付きパス}]" );
                return;
            }


            // 先に ISampleSource を生成する。

            var サンプルソース = Global.App.WAVキャッシュ.作成する( サウンドファイル );

            if( サンプルソース is null )
            {
                Log.WARNING( $"サウンドのデコードに失敗しました。[{サウンドファイル.変数付きパス}" );
                return;
            }


            // サウンドを登録する。

            if( this._WAV情報リスト.ContainsKey( wav番号 ) )
                this._WAV情報リスト[ wav番号 ].Dispose();  // すでに登録済みなら先に解放する。

            int 多重度 = ( 多重再生する ) ? this._既定の多重度 : 1;

            this._WAV情報リスト[ wav番号 ] = new WAV情報( wav番号, 多重度, BGMである );
            this._WAV情報リスト[ wav番号 ].サウンドを生成する( Global.App.サウンドデバイス, サンプルソース );

            Log.Info( $"サウンドを読み込みました。[{サウンドファイル.変数付きパス}]" );
        }

        /// <summary>
        ///		指定した番号のWAVを、指定したチップ種別として発声する。
        /// </summary>
        /// <param name="音量">0:無音～1:原音</param>
        public void 発声する( int WAV番号, SSTF.チップ種別 chipType, bool 発声前に消音する, 消音グループ種別 muteGroupType, bool BGM以外も再生する, float 音量 = 1f, double 再生開始時刻sec = 0.0 )
        {
            if( !( this._WAV情報リスト.ContainsKey( WAV番号 ) ) ||
                ( !( BGM以外も再生する ) && !( this._WAV情報リスト[ WAV番号 ].BGMである ) ) )
                return;


            // 指定があれば消音する。

            if( 発声前に消音する && muteGroupType != 消音グループ種別.Unknown )
            {
                // 発声時に指定された消音グループ種別に属するWAVサウンドをすべて停止する。
                var 停止するWavContexts = this._WAV情報リスト.Where( ( kvp ) => ( kvp.Value.最後に発声したときの消音グループ種別 == muteGroupType ) );

                foreach( var kvp in 停止するWavContexts )
                    kvp.Value.発声を停止する();
            }


            // 発声する。

            if( this._WAV情報リスト[ WAV番号 ].BGMである )        // BGM なら BGMAdjust を適用する
            {
                再生開始時刻sec += Global.App.演奏譜面.譜面.BGMAdjust / 1000.0;
                再生開始時刻sec = Math.Max( 0.0, 再生開始時刻sec );
            }

            this._WAV情報リスト[ WAV番号 ].発声する( muteGroupType, 音量, 再生開始時刻sec );
        }

        public void すべての発声を停止する()
        {
            foreach( var kvp in this._WAV情報リスト )
                kvp.Value.発声を停止する();
        }

        public void 再生位置を移動する( int WAV番号, double 移動量sec )
        {
            if( this._WAV情報リスト.ContainsKey( WAV番号 ) )
                this._WAV情報リスト[ WAV番号 ].再生位置を移動する( 移動量sec );
        }

        public void すべてのBGMの再生位置を移動する( double 移動量sec )
        {
            var BGMs = this._WAV情報リスト.Where( ( kvp ) => kvp.Value.BGMである );

            foreach( var kvp in BGMs )
                kvp.Value.再生位置を移動する( 移動量sec );
        }

        public void 再生中の音声をすべて一時停止する()
        {
            this._一時停止中の音声のリスト.Clear();

            foreach( var kvp in this._WAV情報リスト )
            {
                foreach( var sound in kvp.Value.Sounds )
                {
                    if( sound.再生中である )
                    {
                        sound.Pause();
                        this._一時停止中の音声のリスト.Add( sound );
                    }
                }
            }
        }

        public void 一時停止中の音声をすべて再開する()
        {
            foreach( var sound in this._一時停止中の音声のリスト )
                sound.Resume();
        }



        // ローカル


        private readonly List<Sound> _一時停止中の音声のリスト;

        private readonly int _既定の多重度;

        /// <summary>
        ///		全WAVの管理DB。KeyはWAV番号。
        /// </summary>
        private readonly Dictionary<int, WAV情報> _WAV情報リスト;

        /// <summary>
        ///		WAV ごとの管理情報。
        /// </summary>
        private class WAV情報 : IDisposable
        {
            /// <summary>
            ///		0～1295。
            /// </summary>
            public int WAV番号 { get; }

            /// <summary>
            ///		この WAV に対応するサウンド。
            /// </summary>
            /// <remarks>
            ///		サウンドデータとして <see cref="ISampleSource"/> を使用し、多重度の数だけ存在することができる。
            /// </remarks>
            public Sound[] Sounds { get; }

            public bool BGMである { get; }

            public 消音グループ種別 最後に発声したときの消音グループ種別 { get; protected set; } = 消音グループ種別.Unknown;


            public WAV情報( int wav番号, int 多重度, bool BGMである )
            {
                if( ( 0 > wav番号 ) || ( 36 * 36 <= wav番号 ) )
                    throw new ArgumentOutOfRangeException( "WAV番号が不正です。" );

                this.WAV番号 = wav番号;
                this.Sounds = new Sound[ 多重度 ];
                this.BGMである = BGMである;
            }

            public void Dispose()
            {
                foreach( var sd in this.Sounds )
                    sd.Dispose();
            }

            /// <summary>
            ///     多重度の数だけ Sound を生成する。ただしソースは共通。
            /// </summary>
            public void サウンドを生成する( SoundDevice device, ISampleSource sampleSource )
            {
                for( int i = 0; i < this.Sounds.Length; i++ )
                    this.Sounds[ i ] = new Sound( device, sampleSource );
            }

            /// <summary>
            ///		指定したチップ種別扱いでWAVサウンドを発声する。
            /// </summary>
            /// <param name="音量">0:無音～1:原音</param>
            public void 発声する( 消音グループ種別 muteGroupType, float 音量, double 再生開始時刻sec = 0.0 )
            {
                this.最後に発声したときの消音グループ種別 = muteGroupType;

                // 発声。
                音量 =
                    ( 0f > 音量 ) ? 0f :
                    ( 1f < 音量 ) ? 1f : 音量;
                this.Sounds[ this._次に再生するSound番号 ].Volume = 音量;
                this.Sounds[ this._次に再生するSound番号 ].Play( 再生開始時刻sec );

                // サウンドローテーション。
                this._現在再生中のSound番号 = this._次に再生するSound番号;
                this._次に再生するSound番号 = ( this._次に再生するSound番号 + 1 ) % this.Sounds.Length;
            }

            public void 発声を停止する()
            {
                foreach( var sound in this.Sounds )
                    sound.Stop();
            }

            public void 再生位置を移動する( double 移動量sec )
            {
                if( 0 > this._現在再生中のSound番号 )
                    return;

                var sound = this.Sounds[ this._現在再生中のSound番号 ];

                sound.Position += sound.秒ToFrame( 移動量sec ) * sound.WaveFormat.Channels;
            }


            private int _現在再生中のSound番号 = -1;

            private int _次に再生するSound番号 = 0;
        }
    }
}
