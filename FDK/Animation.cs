using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX.Animation;

namespace FDK
{
    /// <summary>
    ///     Windows Animation API の wrapper。
    /// </summary>
    public class Animation : IDisposable
    {

        // プロパティ


        public Manager Manager { get; protected set; }

        public Timer Timer { get; protected set; }

        public TransitionLibrary TrasitionLibrary { get; protected set; }



        // 生成と終了


        public Animation()
        {
            this.Manager = new Manager();
            this.Timer = new Timer();
            this.TrasitionLibrary = new TransitionLibrary();

            this._スレッドID = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        public void Dispose()
        {
            this.TrasitionLibrary.Dispose();
            this.Timer.Dispose();
            this.Manager.Dispose();
        }



        // 進行と描画


        public void 進行する()
        {
            Debug.Assert( System.Threading.Thread.CurrentThread.ManagedThreadId == this._スレッドID, "生成スレッドではありません。生成スレッドと同じスレッドで呼び出すこと！" );
            this.Manager.Update( this.Timer.Time );
        }



        // ローカル


        private int _スレッドID;
    }
}
