using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FDK;

namespace DTXMania2
{
    /// <summary>
    ///     文字盤（１枚のビットマップ）の一部の矩形を文字として、文字列を表示する。
    /// </summary>
    class フォント画像D2D : FDK.フォント画像D2D
    {
        public フォント画像D2D( VariablePath 文字盤の画像ファイルパス, VariablePath 文字盤の矩形リストファイルパス, float 文字幅補正dpx = 0f, float 不透明度 = 1f )
            : base( 
                  Global.WicImagingFactory2, 
                  Global.既定のD2D1DeviceContext, 
                  文字盤の画像ファイルパス, 文字盤の矩形リストファイルパス, 文字幅補正dpx, 不透明度 )
        {
        }
    }
}
