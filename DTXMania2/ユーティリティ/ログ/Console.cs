using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DTXMania2
{
    /// <summary>
    ///     ウィンドウアプリケーションから、自身が起動されたコンソールにメッセージを出力する。
    /// </summary>
    class Console : IDisposable
    {
        public StreamWriter? Out { get; protected set; }


        public Console()
        {
            var プロセスID = UInt32.MaxValue;   // 自分自身

            if( AttachConsole( プロセスID ) )   // 自分を起動したコンソールに接続。
            {
                this._StreamOut = System.Console.OpenStandardOutput();
                this.Out = new StreamWriter( this._StreamOut, Encoding.GetEncoding( "shift_jis" ) );
            }
            else
            {
                Log.ERROR( "FDK.Console: コンソールのアタッチに失敗しました。" );
                this._StreamOut = null;
                this.Out = null;
            }
        }

        ~Console()
        {
            this.Dispose( false );
        }

        public void Dispose()
        {
            this.Dispose( true );
            GC.SuppressFinalize( this );
        }

        public virtual void Dispose( bool disposeManaged )
        {
            if( disposeManaged )
            {
                if( null != this._StreamOut )
                {
                    this.Out?.Flush();

                    FreeConsole();      // コンソールから接続解除。

                    this._StreamOut = null;
                    this.Out = null;
                }
            }
        }


        private Stream? _StreamOut;


        [DllImport( "kernel32.dll" )]
        private static extern bool AttachConsole( uint dwProcessId );

        [DllImport( "kernel32.dll" )]
        private static extern bool FreeConsole();
    }
}
