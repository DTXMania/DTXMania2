using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DTXMania2
{
    /// <summary>
    ///     名前付きパイプライン送受信用ストリームクラス。
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/ja-jp/dotnet/standard/io/how-to-use-named-pipes-for-network-interprocess-communication"/>
    class StreamStringForNamedPipe
    {
        private Stream ioStream;
        private UnicodeEncoding streamEncoding;

        /// <summary>
        ///     バイトストリーム用の送受信インスタンスを生成する。
        /// </summary>
        /// <param name="ioStream">送受信に使用するバイトストリーム。</param>
        public StreamStringForNamedPipe( Stream ioStream )
        {
            this.ioStream = ioStream;
            this.streamEncoding = new UnicodeEncoding();
        }

        /// <summary>
        ///     バイトストリームから文字列を受信する。
        /// </summary>
        public string ReadString()
        {
            // 最初に受信する2バイトをデータ長とする。
            int len;
            len = this.ioStream.ReadByte() << 8;
            len += this.ioStream.ReadByte();
            
            // 次いで、データ本体を受信。
            var inBuffer = new byte[ len ];
            this.ioStream.Read( inBuffer, 0, len );

            // 受信した byte[] を string にして返す。
            return this.streamEncoding.GetString( inBuffer );
        }

        /// <summary>
        ///     バイトストリームへ文字列を送信する。
        /// </summary>
        /// <param name="送信文字列"></param>
        /// <returns></returns>
        public int WriteString( string 送信文字列 )
        {
            var outBuffer = streamEncoding.GetBytes( 送信文字列 );

            // 最初にデータ長として2バイトを送信する。
            int len = outBuffer.Length;
            if( len > UInt16.MaxValue )
                throw new Exception( "送信文字列が長すぎます。" );
            this.ioStream.WriteByte( (byte) ( len >> 8 ) );
            this.ioStream.WriteByte( (byte) ( len & 0xFF ) );
            
            // 次いで、データ本体を送信。
            this.ioStream.Write( outBuffer, 0, len );
            this.ioStream.Flush();

            // 送信したデータ数[byte]を返す。
            return outBuffer.Length + 2;
        }
    }
}
