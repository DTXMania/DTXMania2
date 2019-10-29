using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CSCore;

namespace DTXMania2
{
    /// <summary>
    ///		指定されたメディアファイルを XA としてデコードして、<see cref="CSCore.IWaveSource"/> オブジェクトを生成する。
    ///		リサンプラーなし版。
    /// </summary>
    unsafe class XAOnMemoryWaveSource : IWaveSource
    {

        // プロパティ


        public bool CanSeek => true; // オンメモリなので常にサポートする。

        public WaveFormat WaveFormat { get; protected set; }

        /// <summary>
        ///		デコード後のオーディオデータのすべての長さ[byte]。
        /// </summary>
        public long Length => this._DecodedWaveData.Length;

        /// <summary>
        ///		現在の再生位置[byte]。
        /// </summary>
        public long Position
        {
            get => this._Position;
            set => this._Position = Utilities.位置をブロック境界単位にそろえて返す( value, this.WaveFormat.BlockAlign );
        }



        // 生成と終了


        /// <summary>
        ///		コンストラクタ。
        ///		指定されたファイルを指定されたフォーマットでデコードし、内部にオンメモリで保管する。
        /// </summary>
        public XAOnMemoryWaveSource( VariablePath ファイルパス, WaveFormat deviceFormat )
        {
            var bjxa = new bjxa.Decoder();
            using var fs = new FileStream( ファイルパス.変数なしパス, FileMode.Open, FileAccess.Read );

            // ヘッダを読み込んでフォーマットを得る。
            var format = bjxa.ReadHeader( fs );

            // WaveFormat プロパティを構築する。
            this.WaveFormat = new WaveFormat(
                (int) format.SamplesRate,
                (int) format.SampleBits,
                (int) format.Channels,
                AudioEncoding.Pcm );

            // デコードする。
            var xabuf = new byte[ format.Blocks * format.BlockSizeXa ];
            var pcmbuf = new short[ format.Blocks * format.BlockSizePcm ];

            if( fs.Read( xabuf, 0, xabuf.Length ) != xabuf.Length )
                throw new Exception( "xaデータの読み込みに失敗しました。" );

            int ret = bjxa.Decode( xabuf, pcmbuf, out long pcm_data );


            // Waveバッファに転送する。
            this._DecodedWaveData = new byte[ pcmbuf.Length * 2 ];
            //this._DecodedWaveData = new byte[ pcm_data * 2 ];   --> バッファが足りない

            Buffer.BlockCopy( pcmbuf, 0, this._DecodedWaveData, 0, this._DecodedWaveData.Length );
        }

        public void Dispose()
        {
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
                Buffer.BlockCopy(
                    src: this._DecodedWaveData,
                    srcOffset: (int) this._Position,
                    dst: buffer,
                    dstOffset: offset,
                    count: count );

                this._Position += count;
            }

            return count;
        }



        // ローカル


        private byte[] _DecodedWaveData;

        private long _Position = 0;
    }
}
