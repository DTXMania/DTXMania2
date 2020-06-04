﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DTXMania2_
{
    /// <summary>
    ///		DirectWrite を使った Direct2D1ビットマップ。
    /// </summary>
    /// <remarks>
    ///		<see cref="表示文字列"/> メンバを更新すれば、次回の描画時に新しいビットマップが生成される。
    /// </remarks>
    class 文字列画像D2D : FDK.文字列画像D2D
    {
        public 文字列画像D2D()
            : base( Global.DWriteFactory, Global.D2D1Factory1, Global.既定のD2D1DeviceContext, Global.設計画面サイズ )
        {
        }
    }
}
