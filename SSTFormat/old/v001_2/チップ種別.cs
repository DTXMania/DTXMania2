using System;

namespace SSTFormat.v001_2
{
    /// <summary>
    ///		チップの種別を表す整数値。
    /// </summary>
    /// <remarks>
    ///		互換性を維持するために、将来にわたって不変な int 型の数値を、明確に定義する。
    /// </remarks>
    public enum チップ種別 : int
    {
        Unknown = 0,
        LeftCrash = 1,
        Ride = 2,
        Ride_Cup = 3,
        China = 4,
        Splash = 5,
        HiHat_Open = 6,
        HiHat_HalfOpen = 7,
        HiHat_Close = 8,
        HiHat_Foot = 9,
        Snare = 10,
        Snare_OpenRim = 11,
        Snare_ClosedRim = 12,
        Snare_Ghost = 13,
        Bass = 14,
        Tom1 = 15,
        Tom1_Rim = 16,
        Tom2 = 17,
        Tom2_Rim = 18,
        Tom3 = 19,
        Tom3_Rim = 20,
        RightCrash = 21,
        BPM = 22,
        小節線 = 23,
        拍線 = 24,
        背景動画 = 25,
        小節メモ = 26,
        LeftCymbal_Mute = 27,
        RightCymbal_Mute = 28,
        小節の先頭 = 29,
        // 増減した場合は、チップ.チップの深さ も更新すること。
    }
}
