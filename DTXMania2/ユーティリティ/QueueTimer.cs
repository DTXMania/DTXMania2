using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DTXMania2
{
    /// <summary>
    ///     一定時間ごとにコールバックを呼び出す。
    /// </summary>
    /// <remarks>
    ///     前回のコールバック処理が完了しているかどうかにかかわらず、期間が経過するたびにコールバックが呼び出されるので注意。
    ///     精度は timeBeginPeriod() に依存するので注意。
    /// </remarks>
    class QueueTimer : IDisposable
    {

        // 生成と終了


        /// <summary>
        ///     タイマーを生成し、指定されたコールバックの呼び出しを開始する。
        /// </summary>
        /// <param name="dueTime">初回のコールバック呼び出しまでの期間。ミリ秒単位。</param>
        /// <param name="period">２回目以降のコールバック呼び出しまでの期間。ミリ秒単位。0 を設定すると、初回のみ呼び出される。</param>
        /// <param name="callbackAction">タイマーによって呼び出されるコールバック処理。</param>
        public QueueTimer( uint dueTime, uint period, Action callbackAction )
        {
            this._callbackAction = callbackAction;

            // タイマーキューを生成。
            this._hTimerQueue = CreateTimerQueue();
            if( _hTimerQueue == IntPtr.Zero )
                throw new InvalidOperationException( "TimerQueueの作成に失敗しました。" );

            // コールバックデリゲートインスタンスを生成。
            // ネイティブライブラリに渡すので、this のメンバとして参照を保持し、GCによる回収を回避する必要がある。
            this._callback = new UnmanagedTimerCallback( this.TimerCallbackFunction );

            // タイマーを生成、コールバックの定期呼び出しを開始。
            CreateTimerQueueTimer(
                out this._hTimer,
                this._hTimerQueue,
                Marshal.GetFunctionPointerForDelegate( this._callback ),
                IntPtr.Zero,
                dueTime,
                period,
                WT_EXECUTEINTIMERTHREAD );
        }

        /// <summary>
        ///     タイマーを停止する。
        /// </summary>
        public void Dispose()
        {
            // タイマーを停止。
            DeleteTimerQueueTimer( this._hTimerQueue, this._hTimer, IntPtr.Zero );
            this._hTimer = IntPtr.Zero;

            // タイマーキューを停止。
            DeleteTimerQueueEx( this._hTimerQueue, IntPtr.Zero );
            this._hTimerQueue = IntPtr.Zero;
        }



        // ローカル


        private IntPtr _hTimerQueue;

        private IntPtr _hTimer;

        private UnmanagedTimerCallback _callback;

        private Action _callbackAction;


        /// <summary>
        ///     ネイティブからのコールバック関数。
        /// </summary>
        /// <param name="lpParameter">無効（未使用）。</param>
        /// <param name="TimerOrWaitFired">常に true。</param>
        private void TimerCallbackFunction( IntPtr lpParameter, bool TimerOrWaitFired )
        {
            this._callbackAction?.Invoke();
        }


        #region " Win32 "
        //----------------
        const uint WT_EXECUTEDEFAULT = 0x00000000;
        const uint WT_EXECUTEINIOTHREAD = 0x00000001;
        const uint WT_EXECUTEONLYONCE = 0x0000008;
        const uint WT_EXECUTELONGFUNCTION = 0x0000010;
        const uint WT_EXECUTEINTIMERTHREAD = 0x0000020;
        const uint WT_EXECUTEINPERSISTENTTHREAD = 0x0000080;
        const uint WT_TRANSFER_IMPERSONATION = 0x0000100;

        [DllImport( "kernel32.dll" )]
        static extern bool CreateTimerQueueTimer( out IntPtr phNewTimer, IntPtr TimerQueue, IntPtr Callback, IntPtr Parameter, uint DueTime, uint Period, uint Flags );

        [DllImport( "kernel32.dll", EntryPoint = "CreateTimerQueue" )]
        static extern IntPtr CreateTimerQueue();

        [DllImport( "kernel32.dll", EntryPoint = "DeleteTimerQueueEx" )]
        static extern bool DeleteTimerQueueEx( IntPtr hTimerQueue, IntPtr hCompletionEvent );

        [DllImport( "kernel32.dll", EntryPoint = "DeleteTimerQueueTimer" )]
        static extern bool DeleteTimerQueueTimer( IntPtr hTimerQueue, IntPtr hTimer, IntPtr hCompletionEvent );

        [UnmanagedFunctionPointer( CallingConvention.Winapi )]
        public delegate void UnmanagedTimerCallback( IntPtr param, bool timerOrWait );
        //----------------
        #endregion
    }
}
