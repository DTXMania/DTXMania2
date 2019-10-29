using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DTXMania2
{
    /// <summary>
    ///		指定されたストリームに <see cref="Trace"/> の出力を複製するリスナー。
    /// </summary>
    class TraceLogListener : TraceListener
    {
        public TraceLogListener( StreamWriter stream )
        {
            this._streamWriter = stream;
        }

        public override void Flush()
            => this._streamWriter?.Flush();

        public override void Write( string message )
            => this._streamWriter?.Write( this.SanitizeUsername( message ) );

        public override void WriteLine( string message )
            => this._streamWriter?.WriteLine( this.SanitizeUsername( message ) );

        protected override void Dispose( bool disposing )
        {
            this._streamWriter?.Close();

            base.Dispose( disposing );
        }



        // ローカル


        private StreamWriter _streamWriter;

        /// <summary>
        ///		もしユーザー名の情報が出力に存在する場合は、伏字にする。
        /// </summary>
        private string SanitizeUsername( string message )
        {
            string userprofile = Environment.GetFolderPath( Environment.SpecialFolder.UserProfile );

            if( message.Contains( userprofile ) )
            {
                char delimiter = Path.DirectorySeparatorChar;
                string[] u = userprofile.Split( delimiter );
                int c = u[ u.Length - 1 ].Length;     // ユーザー名の文字数
                u[ u.Length - 1 ] = "*".PadRight( c, '*' );
                string sanitizedusername = string.Join( delimiter.ToString(), u );
                message = message.Replace( userprofile, sanitizedusername );
            }

            return message;
        }
    }
}
