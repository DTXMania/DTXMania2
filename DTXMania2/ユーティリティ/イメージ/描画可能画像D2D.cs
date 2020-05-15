using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using FDK;

namespace DTXMania2
{
    /// <summary>
    ///		レンダーターゲットとしても描画可能なビットマップを扱うクラス。
    /// </summary>
    class 描画可能画像D2D : FDK.描画可能画像D2D
    {
        public 描画可能画像D2D( VariablePath 画像ファイルパス )
            : base( Global.WicImagingFactory2, Global.既定のD2D1DeviceContext, 画像ファイルパス )
        {
        }

        public 描画可能画像D2D( Size2F サイズ )
            : base( Global.既定のD2D1DeviceContext, サイズ )
        {
        }

        public 描画可能画像D2D( float width, float height )
            : this( new Size2F( width, height ) )
        {
        }
    }
}
