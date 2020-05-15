using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FDK
{
    /// <summary>
    ///		コンストラクタでブロック開始ログ、Disposeでブロック終了ログを出力するインスタンス。
    /// </summary>
    public class LogBlock : IDisposable
    {
        // "--> 開始"
        public LogBlock( string ブロック名 )
        {
            this._ブロック名 = ブロック名;
            Log.BeginInfo( this._ブロック名 );
        }

        // "<-- 終了"
        public void Dispose()
        {
            Log.EndInfo( this._ブロック名 );
        }


        private string _ブロック名;
    }
}
