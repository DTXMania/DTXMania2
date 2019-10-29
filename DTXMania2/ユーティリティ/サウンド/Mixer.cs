using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSCore;

namespace DTXMania2
{
    /// <summary>
    ///		オーディオミキサー。
    ///		自身が <see cref="ISampleSource"/> であり、そのまま AudioClient のレンダリングターゲットに指定することで、無限の出力を生成する。
    /// </summary>
    class Mixer : ISampleSource
    {

        // プロパティ


        /// <summary>
        ///		音量。0.0(無音)～1.0(原音)～... 上限なし
        /// </summary>
        public float Volume
        {
            get => this._Volume;
            set => this._Volume =
                    ( 0.0f > value ) ? throw new ArgumentOutOfRangeException() :
                    //( 1.0f < value ) ? throw new ArgumentOutOfRangeException() :	--> 上限なし。
                    value;
        }

        /// <summary>
        ///		ミキサーのフォーマット。
        /// </summary>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        ///		ミキサーはループするので、Position には 非対応。
        /// </summary>
        public long Position
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        /// <summary>
        ///		ミキサーはシークできない。
        /// </summary>
        public bool CanSeek => false;

        /// <summary>
        ///		ミキサーは無限にループするので、長さの概念はない。
        /// </summary>
        public long Length => throw new NotSupportedException();



        // 生成と終了


        /// <summary>
        ///		指定したフォーマットを持つミキサーを生成する。
        /// </summary>
        public Mixer( WaveFormat deviceWaveFormat )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            // ミキサーのフォーマットは、デバイスのフォーマットをそのまま使う。
            this.WaveFormat = ( deviceWaveFormat.Clone() as WaveFormat ) ?? throw new Exception( "WaveFormat ではありません。" );
        }

        /// <summary>
        ///		ミキサに登録されているサウンドをすべて停止し解放する。
        /// </summary>
        public void Dispose()
        {
            lock( this._スレッド間同期 )
            {
                //foreach( var sound in this._Sounds )
                //	sound.Dispose();	--> Dispose()する ＝ Stop()する ＝ this._SoundsからRemoveするということなので、foreachは使えない。

                var sound = (Sound?) null;
                while( null != ( sound = this._Sounds.Last?.Value ) )
                    sound.Dispose();

                this._Sounds.Clear();   // すでに空のはずだが念のため。
            }
        }



        // サウンド管理


        /// <summary>
        ///		<see cref="Sound"/> をミキサーに追加する。
        ///		追加されると同時に、<see cref="Sound"/> の再生が開始される。
        /// </summary>
        public void AddSound( Sound sound )
        {
            if( sound is null )
                throw new ArgumentNullException();

            lock( this._スレッド間同期 )
            {
                // すでに登録済み（まだ再生中）なら削除する。
                if( this._Sounds.Contains( sound ) )
                    this._Sounds.Remove( sound );   // 再生も止まる。

                // Soundのフォーマットがミキサーのフォーマットと適合するかをチェック。
                if( !( this._フォーマットがミキサーと互換性がある( sound.WaveFormat ) ) )
                {
                    // 違った場合の変換はサポートしない。
                    throw new ArgumentException( "ミキサーと同じチャンネル数、サンプルレート、かつ 32bit float 型である必要があります。" );
                }

                // サウンドリストに登録。
                this._Sounds.AddLast( sound );
            }
        }

        /// <summary>
        ///		<see cref="Sound"/> をミキサーから除外する。
        ///		除外されると同時に、<see cref="Sound"/> の再生は終了する。
        /// </summary>
        public void RemoveSound( Sound sound )
        {
            lock( this._スレッド間同期 )
            {
                if( this._Sounds.Contains( sound ) )
                    this._Sounds.Remove( sound );
            }
        }

        /// <summary>
        ///		<see cref="Sound"/> がミキサーに登録されているかを調べる。
        /// </summary>
        /// <returns>
        ///		<see cref="Sound"/> がミキサーに追加済みなら true 。
        ///	</returns>
        public bool Contains( Sound sound )
        {
            if( sound is null )
                return false;

            lock( this._スレッド間同期 )
            {
                return this._Sounds.Contains( sound );
            }
        }



        // 出力


        /// <summary>
        ///		バッファにサウンドサンプルを出力する。
        /// </summary>
        /// <returns>
        ///		実際に出力したサンプル数。
        ///	</returns>
        public int Read( float[] 出力バッファ, int 出力バッファの出力開始位置, int 出力サンプル数 )
        {
            // ミキサに登録されている Sound の入力と、このメソッドが出力するデータは、いずれも常に 32bit-float である。
            // これは this.WaveFormat.WaveFormatTag とは無関係なので注意。（this.WaveFormat は、チャンネル数とサンプルレートしか見てない。）

            if( 0 >= 出力サンプル数 )
                return 0;

            lock( this._スレッド間同期 )
            {
                // 中間バッファが十分あることを確認する。足りなければ新しく確保して戻ってくる。

                this._中間バッファ = this._中間バッファ.CheckBuffer( 出力サンプル数 ); // サンプル数であり、フレーム数（サンプル数×チャンネル数）ではない。


                // まずは無音で埋める。

                Array.Clear( 出力バッファ, 0, 出力サンプル数 );


                // その上に、ミキサに登録されているすべての Sound を加算合成する。

                if( 0 < this._Sounds.Count )
                {
                    var 再生終了したSound一覧 = new List<Sound>();

                    foreach( var sound in this._Sounds )
                    {
                        // 中間バッファにサウンドデータを受け取る。

                        int 受け取ったサンプル数 = sound.Read( this._中間バッファ, 0, 出力サンプル数 );


                        if( 0 < 受け取ったサンプル数 )
                        {
                            // 中間バッファから出力バッファへ合成する。

                            for( int i = 出力バッファの出力開始位置, n = 0; n < 受け取ったサンプル数; i++, n++ )
                            {
                                float data = this._中間バッファ[ n ] // 原音
                                    * sound.Volume                  // 個別音量（Sound）
                                    * this._Volume;                 // マスタ音量（ミキサ）

                                // 先に無音を出力済みなので、上書きかどうかを気にしないで常に加算。

                                出力バッファ[ i ] += data;
                            }
                        }
                        else
                        {
                            // 再生終了。

                            再生終了したSound一覧.Add( sound );
                        }
                    }

                    
                    // 再生が終了したSoundをサウンドリストから削除する。

                    foreach( var sound in 再生終了したSound一覧 )
                        sound.Stop();   // この中で自分でRemoveする

                    再生終了したSound一覧.Clear();
                }
            }

            return 出力サンプル数;
        }



        // ローカル


        private readonly LinkedList<Sound> _Sounds = new LinkedList<Sound>();

        private float _Volume = 1.0f;

        private float[]? _中間バッファ = null;

        private readonly object _スレッド間同期 = new object();


        private bool _フォーマットがミキサーと互換性がある( WaveFormat waveFormat )
        {
            // チャンネル数が違うと NG
            if( waveFormat.Channels != this.WaveFormat.Channels )
                return false;

            // サンプルレートが違うと NG
            if( waveFormat.SampleRate != this.WaveFormat.SampleRate )
                return false;

            // 以下、ミキサーフォーマットは IEEE Float であると想定。

            // IeeeFloat なら OK
            if( waveFormat.WaveFormatTag == AudioEncoding.IeeeFloat )
                return true;

            // Extensible である場合、
            if( waveFormat.WaveFormatTag == AudioEncoding.Extensible )
            {
                var waveFormatEx = waveFormat as WaveFormatExtensible ?? throw new Exception( "WaveFormatExtensible" );

                // サブフォーマットが IEEE Float なら OK
                if( waveFormatEx.SubFormat == AudioSubTypes.IeeeFloat )
                    return true;
            }

            // それ以外は NG
            return false;
        }
    }
}
