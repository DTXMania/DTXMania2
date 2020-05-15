using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CSCore;
using NVorbis;

namespace FDK
{
    /// <summary>
    ///		指定されたメディアファイル（動画, 音楽）を Vorbis としてデコードして、<see cref="CSCore.ISampleSource"/> オブジェクトを生成する。
    ///		リサンプラーなし版。
    /// </summary>
    /// <seealso cref="https://github.com/filoe/cscore/blob/master/Samples/NVorbisIntegration/Program.cs"/>
    public class NVorbisOnStreamingSampleSource : ISampleSource
    {

        // プロパティ


        public bool CanSeek
            => this._stream.CanSeek;

        public WaveFormat WaveFormat { get; }

        public long Position
        {
            get
                => ( this.CanSeek ) ? this._vorbisReader.SamplePosition : 0;
            set
                => this._vorbisReader.SamplePosition = ( this.CanSeek ) ?
                    value : throw new InvalidOperationException( "DecodedNVorbisSource is not seekable." );
        }

        public long Length
            => ( this.CanSeek ) ? this._vorbisReader.TotalSamples * this.WaveFormat.Channels : 0;   // TotalSamples はフレーム数を返す。



        // 生成と終了


        public NVorbisOnStreamingSampleSource( Stream stream, WaveFormat deviceFormat )
        {
            if( stream is null )
                throw new ArgumentException( "stream" );
            if( !stream.CanRead )
                throw new ArgumentException( "Stream is not readable.", "stream" );

            this._stream = stream;
            this._vorbisReader = new VorbisReader( stream, true );
            this.WaveFormat = new WaveFormat(
                this._vorbisReader.SampleRate,
                32,                             // 32bit 固定
                this._vorbisReader.Channels,
                AudioEncoding.IeeeFloat );      // IeeeFloat 固定
        }

        public void Dispose()
        {
            this._vorbisReader.Dispose();
        }



        // 出力


        public int Read( float[] buffer, int offset, int count )
        {
            return this._vorbisReader.ReadSamples( buffer, offset, count );
        }



        // ローカル


        private Stream _stream;

        private VorbisReader _vorbisReader;
    }
}
