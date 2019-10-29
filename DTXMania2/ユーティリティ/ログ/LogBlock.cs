using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DTXMania2
{
    /// <summary>
    ///		コンストラクタでブロック開始ログ、Disposeでブロック終了ログを出力するインスタンス。
    /// </summary>
    class LogBlock : IDisposable
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
