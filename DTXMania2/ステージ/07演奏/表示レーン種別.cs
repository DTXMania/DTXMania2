using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DTXMania2.演奏
{
    /// <summary>
    ///		チップや判定文字列の表示先となるレーンの種別。
    /// </summary>
    enum 表示レーン種別
    {
        Unknown,
        LeftCymbal,
        HiHat,
        Foot,   // 左ペダル
        Snare,
        Bass,
        Tom1,
        Tom2,
        Tom3,
        RightCymbal,
    }
}
