﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace DTXMania2_
{
    /// <summary>
    ///     タスク間で送受信されるメッセージ。
    /// </summary>
    /// <remarks>
    ///     DTXMania2 では、主に「GUIフォーム」タスクと「進行描画」タスクが並列に動作している。
    ///     これらの間でのメッセージを送受信するための仕組みを提供する。
    /// </remarks>
    class TaskMessage
    {
        /// <summary>
        ///     メッセージの宛先となるタスク。
        /// </summary>
        public enum タスク名    // 必要に応じて付け足すこと。
        {
            GUIフォーム,
            進行描画,
        }

        /// <summary>
        ///     タスクメッセージの内容。
        /// </summary>
        public enum 内容名     // 必要に応じて付け足すこと。
        {
            終了指示,
            サイズ変更,
        }

        /// <summary>
        ///     このメッセージの宛先。
        /// </summary>
        public タスク名 宛先 { get; }

        /// <summary>
        ///     このメッセージの内容。
        ///     書式は任意。
        /// </summary>
        public 内容名 内容 { get; }

        /// <summary>
        ///     メッセージの引数。
        ///     オプション。
        /// </summary>
        public object[]? 引数 { get; }

        /// <summary>
        ///     メッセージが受信され、対応する処理が完了した場合に Set されるイベント。
        ///     オプション。
        /// </summary>
        public ManualResetEventSlim 完了通知 { get; }


        /// <summary>
        ///     コンストラクタ。
        /// </summary>
        public TaskMessage( タスク名 宛先, 内容名 内容, object[]? 引数 = null )
        {
            this.宛先 = 宛先;
            this.内容 = 内容;
            this.引数 = 引数;
            this.完了通知 = new ManualResetEventSlim( false );
        }
    }
}
