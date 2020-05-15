using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX.Direct2D1;

namespace DTXMania2.結果
{
    /// <summary>
    ///     <see cref="達成率"/> と <see cref="達成率更新"/> の共通インターフェース。
    /// </summary>
    abstract class 達成率Base : IDisposable
    {

        // プロパティ


        public virtual bool アニメ完了 { get; }



        // 生成と終了


        public 達成率Base()
        {
        }

        public abstract void Dispose();



        // 進行と描画


        public abstract void 進行描画する( DeviceContext dc, float left, float top, double 達成率0to100 );

        public abstract void アニメを完了する();
    }
}
