using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX.Animation;

namespace DTXMania2
{
    /// <summary>
    ///     Windows Animation API の wrapper。
    /// </summary>
    class Animation : IDisposable
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
        }

        public void Dispose()
        {
            this.TrasitionLibrary.Dispose();
            this.Timer.Dispose();
            this.Manager.Dispose();
        }



        // 進行と描画


        // 生成スレッドと同じスレッドで呼び出すこと！
        public void 進行する()
        {
            this.Manager.Update( this.Timer.Time );
        }
    }
}
