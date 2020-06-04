﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania2_
{
    /// <summary>
    ///     D2Dビットマップを使った画像表示。
    /// </summary>
    class 画像D2D : FDK.画像D2D
    {
        public 画像D2D( VariablePath 画像ファイルパス, BitmapProperties1? bitmapProperties1 = null )
            : base( Global.WicImagingFactory2, Global.既定のD2D1DeviceContext, 画像ファイルパス, bitmapProperties1 )
        {
        }

        protected 画像D2D()
            : base()
        {
        }
    }
}
