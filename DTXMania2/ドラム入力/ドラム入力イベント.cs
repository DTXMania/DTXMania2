using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DTXMania2
{
    /// <summary>
    ///		単一のドラム入力イベントを表す。
    /// </summary>
    class ドラム入力イベント
    {
        // Key
        public InputEvent InputEvent { get; protected set; }

        // Value
        public ドラム入力種別 Type { get; protected set; }


        public ドラム入力イベント( InputEvent 入力イベント, ドラム入力種別 ドラム入力種別 )
        {
            this.InputEvent = 入力イベント;
            this.Type = ドラム入力種別;
        }
    }
}
