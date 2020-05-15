using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;

namespace FDK
{
    public interface IVideoSource : IDisposable
    {
        /// <summary>
        ///     ビデオフレームのサイズ[width, height]。
        /// </summary>
        Size2F フレームサイズ { get; }

        /// <summary>
        ///     指定した時刻からデコードを開始する。
        /// </summary>
        void Start( double 再生開始時刻sec );

        /// <summary>
        ///     次に読みだされるフレームがあれば、その表示予定時刻[100ns単位]を返す。
        ///     フレームがなければ、ブロックせずにすぐ 負数 を返す。
        /// </summary>
        long Peek();

        /// <summary>
        ///     フレームを１つ読みだす。
        ///     再生中の場合、フレームが取得できるまでブロックする。
        ///     再生が終了している場合は null を返す。
        ///     取得したフレームは、使用が終わったら、呼び出し元で Dispose すること。
        /// </summary>
        VideoFrame? Read();

        /// <summary>
        ///     デコードを終了する。
        /// </summary>
        void Stop();

        /// <summary>
        ///     デコードを一時停止する。
        /// </summary>
        void Pause();

        /// <summary>
        ///     デコードを再開する。
        ///     <see cref="Pause"/>で停止しているときのみ有効。
        /// </summary>
        void Resume();
    }
}
