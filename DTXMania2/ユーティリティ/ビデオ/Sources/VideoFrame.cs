using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX.Direct2D1;
using SharpDX.MediaFoundation;

namespace DTXMania2
{
    /// <summary>
    ///     ビデオフレームの定義。
    ///     ビデオフレームとは、<see cref="IVideoSource.Read"/> で返されるオブジェクトのこと。
    /// </summary>
    public class VideoFrame : IDisposable
    {
        public long 表示時刻100ns { get; set; }

        /// <summary>
        ///     <see cref="Bitmap"/> への変換前。
        /// </summary>
        public Sample Sample { get; set; } = null!;

        /// <summary>
        ///     <see cref="Sample"/> からの変換後。Sample とビデオメモリを共有しているので注意。
        /// </summary>
        public Bitmap Bitmap { get; set; } = null!;


        public void Dispose()
        {
            this.Bitmap?.Dispose();
            this.Sample?.Dispose();
        }
    }

}
