using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DTXMania2
{
    /// <summary>
    ///		入力データの種別を、ドラム様式で定義したもの。
    ///		DTXMania2 では、演奏用の入力データは、すべてこのドラム入力種別へマッピングされる。
    /// </summary>
    enum ドラム入力種別
    {
        Unknown,
        LeftCrash,
        Ride,
        //Ride_Cup,			--> Ride として扱う。（打ち分けない。）
        China,
        Splash,
        HiHat_Open,
        //HiHat_HalfOpen,	--> HiHat_Open として扱う。（打ち分けない。）
        HiHat_Close,
        HiHat_Foot,     //  --> フットスプラッシュ
        HiHat_Control,  //	--> ハイハット開度
        Snare,
        Snare_OpenRim,
        Snare_ClosedRim,
        //Snare_Ghost,		--> ヒット判定しない。
        Bass,
        Tom1,
        Tom1_Rim,
        Tom2,
        Tom2_Rim,
        Tom3,
        Tom3_Rim,
        RightCrash,
        //LeftCymbal_Mute,	--> （YAMAHAでは）入力信号じゃない
        //RightCymbal_Mute,	--> （YAMAHAでは）入力信号じゃない
        Pause_Resume,
    }
}
