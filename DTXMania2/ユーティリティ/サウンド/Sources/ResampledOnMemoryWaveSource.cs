using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSCore;
using CSCore.DSP;

namespace DTXMania2
{
    /// <summary>
    ///		指定されたメディアファイルをデコードし、リサンプルして、
    ///		<see cref="CSCore.IWaveSource"/> オブジェクトを生成する。
    /// </summary>
    class ResampledOnMemoryWaveSource : IWaveSource
    {

        // プロパティ


        public bool CanSeek => true;    // オンメモリなので常にサポートできる。

        /// <summary>
        /// 	デコード＆リサンプル後のオーディオデータのフォーマット。
        /// </summary>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        ///		現在の再生位置[byte]。
        /// </summary>
        public long Position
        {
            get => this._Position;
            set => this._Position = Utilities.位置をブロック境界単位にそろえて返す( value, this.WaveFormat.BlockAlign );
        }

        /// <summary>
        ///		デコード後のオーディオデータのすべての長さ[byte]。
        /// </summary>
        public long Length
            => this._DecodedWaveData.Length;



        // 生成と終了


        /// <summary>
        ///     コンストラクタ。
        ///     指定された <see cref="IWaveSource"/> をリサンプルする。
        /// </summary>
        public ResampledOnMemoryWaveSource( IWaveSource waveSource, WaveFormat deviceFormat, double 再生速度 = 1.0 )
        {
            // サウンドデバイスには、それに合わせたサンプルレートで報告する。
            this.WaveFormat = new WaveFormat(
                deviceFormat.SampleRate,
                32,
                deviceFormat.Channels,
                AudioEncoding.IeeeFloat );

            // しかしサウンドデータは、指定された再生速度を乗じたサンプルレートで生成する。
            var waveFormtForResampling = new WaveFormat(
                (int) ( this.WaveFormat.SampleRate / 再生速度 ),
                this.WaveFormat.BitsPerSample,
                this.WaveFormat.Channels,
                AudioEncoding.IeeeFloat );

            // リサンプルを行う。
            using( var resampler = new DmoResampler( waveSource, waveFormtForResampling ) )
            {
                long サイズbyte = resampler.Length;
                this._DecodedWaveData = new MemoryTributary( (int) サイズbyte );

                //resampler.Read( this._DecodedWaveData, 0, (int) サイズbyte );
                //　→ 一気にReadすると、内部の Marshal.AllocCoTaskMem() に OutOfMemory例外を出されることがある。
                // 　　よって、２秒ずつ分解しながら受け取る。
                int sizeOf2秒 = Utilities.位置をブロック境界単位にそろえて返す( resampler.WaveFormat.BytesPerSecond * 2, resampler.WaveFormat.BlockAlign );
                long 変換残サイズbyte = サイズbyte;
                while( 0 < 変換残サイズbyte )
                {
                    int 今回の変換サイズbyte = (int) Utilities.位置をブロック境界単位にそろえて返す( Math.Min( sizeOf2秒, 変換残サイズbyte ), resampler.WaveFormat.BlockAlign );

                    var 中間バッファ = new byte[ 今回の変換サイズbyte ];
                    int 変換できたサイズbyte = resampler.Read( 中間バッファ, 0, 今回の変換サイズbyte );

                    if( 0 == 変換できたサイズbyte )
                        break;  // 強制脱出

                    this._DecodedWaveData.Write( 中間バッファ, 0, 変換できたサイズbyte );

                    変換残サイズbyte -= 変換できたサイズbyte;
                }
            }

            this._DecodedWaveData.Position = 0;
        }

        public void Dispose()
        {
            this._DecodedWaveData.Dispose();
        }



        // 出力


        /// <summary>
        ///		連続したデータを読み込み、<see cref="Position"/> を読み込んだ数だけ進める。
        /// </summary>
        /// <param name="buffer">読み込んだデータを格納するための配列。</param>
        /// <param name="offset"><paramref name="buffer"/> に格納を始める位置。</param>
        /// <param name="count">読み込む最大のデータ数。</param>
        /// <returns><paramref name="buffer"/> に読み込んだデータの総数。</returns>
        public int Read( byte[] buffer, int offset, int count )
        {
            // ※ 音がめちゃくちゃになるとうざいので、このメソッド内では例外を出さないこと。
            if( ( null == this._DecodedWaveData ) || ( null == buffer ) )
                return 0;

            long 読み込み可能な最大count = ( this.Length - this._Position );

            if( count > 読み込み可能な最大count )
                count = (int) 読み込み可能な最大count;

            if( 0 < count )
            {
                this._DecodedWaveData.Position = this._Position;
                this._DecodedWaveData.Read( buffer, offset, count );

                this._Position += count;
            }

            return count;
        }



        // ローカル


        private long _Position = 0;

        private MemoryTributary _DecodedWaveData;
    }
}
