using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SSTFormat.v004
{
    public static class SSTFプロパティ
    {
        // チップの属するレーン

        public static readonly Dictionary<チップ種別, レーン種別> チップtoレーンマップ = new Dictionary<チップ種別, レーン種別>() {
            #region " *** "
            //----------------
            { チップ種別.Unknown,            レーン種別.Unknown },
            { チップ種別.LeftCrash,          レーン種別.LeftCrash },
            { チップ種別.Ride,               レーン種別.Ride },
            { チップ種別.Ride_Cup,           レーン種別.Ride },
            { チップ種別.China,              レーン種別.China },
            { チップ種別.Splash,             レーン種別.Splash },
            { チップ種別.HiHat_Open,         レーン種別.HiHat },
            { チップ種別.HiHat_HalfOpen,     レーン種別.HiHat },
            { チップ種別.HiHat_Close,        レーン種別.HiHat },
            { チップ種別.HiHat_Foot,         レーン種別.Foot },
            { チップ種別.Snare,              レーン種別.Snare },
            { チップ種別.Snare_OpenRim,      レーン種別.Snare },
            { チップ種別.Snare_ClosedRim,    レーン種別.Snare },
            { チップ種別.Snare_Ghost,        レーン種別.Snare },
            { チップ種別.Bass,               レーン種別.Bass },
            { チップ種別.LeftBass,           レーン種別.Bass },
            { チップ種別.Tom1,               レーン種別.Tom1 },
            { チップ種別.Tom1_Rim,           レーン種別.Tom1 },
            { チップ種別.Tom2,               レーン種別.Tom2 },
            { チップ種別.Tom2_Rim,           レーン種別.Tom2 },
            { チップ種別.Tom3,               レーン種別.Tom3 },
            { チップ種別.Tom3_Rim,           レーン種別.Tom3 },
            { チップ種別.RightCrash,         レーン種別.RightCrash },
            { チップ種別.BPM,                レーン種別.BPM },
            { チップ種別.小節線,             レーン種別.Unknown },
            { チップ種別.拍線,               レーン種別.Unknown },
            { チップ種別.背景動画,           レーン種別.BGV },
            { チップ種別.LeftCymbal_Mute,    レーン種別.LeftCrash },
            { チップ種別.RightCymbal_Mute,   レーン種別.RightCrash },
            { チップ種別.小節の先頭,         レーン種別.Unknown },
            { チップ種別.小節メモ,           レーン種別.Unknown },
            { チップ種別.BGM,                レーン種別.BGM },
            { チップ種別.GuitarAuto,         レーン種別.Unknown },    // 以下、出力未対応
            { チップ種別.BassAuto,           レーン種別.Unknown },
            { チップ種別.SE1,                レーン種別.Unknown },
            { チップ種別.SE2,                レーン種別.Unknown },
            { チップ種別.SE3,                レーン種別.Unknown },
            { チップ種別.SE4,                レーン種別.Unknown },
            { チップ種別.SE5,                レーン種別.Unknown },
            { チップ種別.SE6,                レーン種別.Unknown },
            { チップ種別.SE7,                レーン種別.Unknown },
            { チップ種別.SE8,                レーン種別.Unknown },
            { チップ種別.SE9,                レーン種別.Unknown },
            { チップ種別.SE10,               レーン種別.Unknown },
            { チップ種別.SE11,               レーン種別.Unknown },
            { チップ種別.SE12,               レーン種別.Unknown },
            { チップ種別.SE13,               レーン種別.Unknown },
            { チップ種別.SE14,               レーン種別.Unknown },
            { チップ種別.SE15,               レーン種別.Unknown },
            { チップ種別.SE16,               レーン種別.Unknown },
            { チップ種別.SE17,               レーン種別.Unknown },
            { チップ種別.SE18,               レーン種別.Unknown },
            { チップ種別.SE19,               レーン種別.Unknown },
            { チップ種別.SE20,               レーン種別.Unknown },
            { チップ種別.SE21,               レーン種別.Unknown },
            { チップ種別.SE22,               レーン種別.Unknown },
            { チップ種別.SE23,               レーン種別.Unknown },
            { チップ種別.SE24,               レーン種別.Unknown },
            { チップ種別.SE25,               レーン種別.Unknown },
            { チップ種別.SE26,               レーン種別.Unknown },
            { チップ種別.SE27,               レーン種別.Unknown },
            { チップ種別.SE28,               レーン種別.Unknown },
            { チップ種別.SE29,               レーン種別.Unknown },
            { チップ種別.SE30,               レーン種別.Unknown },
            { チップ種別.SE31,               レーン種別.Unknown },
            { チップ種別.SE32,               レーン種別.Unknown },
            //----------------
            #endregion
        };


        // チップの深さ

        /// <summary>
        ///     チップの深さを数値で表したもの。
        /// </summary>
        /// <remarks>
        ///     ２つのチップが同じ位置に配置されている場合、チップの深さが「小さいほうが後」になる。
        ///     例えば、深さ10と20のチップでは、深さ20のチップが先に描かれ、次に深さ10のチップが描かれる。
        ///     すなわち、チップが重なっている場合には、深さ10のチップが20のチップの上に重なる。
        /// </remarks>
        public static readonly Dictionary<チップ種別, int> チップの深さ = new Dictionary<チップ種別, int>() {
            #region " *** "
            //-----------------
            { チップ種別.Ride_Cup,           50 },
            { チップ種別.HiHat_Open,         50 },
            { チップ種別.HiHat_HalfOpen,     50 },
            { チップ種別.HiHat_Close,        50 },
            { チップ種別.HiHat_Foot,         50 },
            { チップ種別.Snare,              50 },
            { チップ種別.Snare_OpenRim,      50 },
            { チップ種別.Snare_ClosedRim,    50 },
            { チップ種別.Snare_Ghost,        50 },
            { チップ種別.Tom1,               50 },
            { チップ種別.Tom1_Rim,           50 },
            { チップ種別.BPM,                50 },
            { チップ種別.Ride,               60 },
            { チップ種別.Splash,             60 },
            { チップ種別.Tom2,               60 },
            { チップ種別.Tom2_Rim,           60 },
            { チップ種別.LeftCrash,          70 },
            { チップ種別.China,              70 },
            { チップ種別.Tom3,               70 },
            { チップ種別.Tom3_Rim,           70 },
            { チップ種別.RightCrash,         70 },
            { チップ種別.Bass,               74 },
            { チップ種別.LeftBass,           75 },
            { チップ種別.LeftCymbal_Mute,    76 },
            { チップ種別.RightCymbal_Mute,   76 },
            { チップ種別.小節線,             80 },
            { チップ種別.拍線,               85 },
            { チップ種別.背景動画,           90 },
            { チップ種別.小節の先頭,         99 },
            { チップ種別.小節メモ,           99 },
            { チップ種別.GuitarAuto,         99 },
            { チップ種別.BassAuto,           99 },
            { チップ種別.BGM,                99 },
            { チップ種別.SE1,                99 },
            { チップ種別.SE2,                99 },
            { チップ種別.SE3,                99 },
            { チップ種別.SE4,                99 },
            { チップ種別.SE5,                99 },
            { チップ種別.SE6,                99 },
            { チップ種別.SE7,                99 },
            { チップ種別.SE8,                99 },
            { チップ種別.SE9,                99 },
            { チップ種別.SE10,               99 },
            { チップ種別.SE11,               99 },
            { チップ種別.SE12,               99 },
            { チップ種別.SE13,               99 },
            { チップ種別.SE14,               99 },
            { チップ種別.SE15,               99 },
            { チップ種別.SE16,               99 },
            { チップ種別.SE17,               99 },
            { チップ種別.SE18,               99 },
            { チップ種別.SE19,               99 },
            { チップ種別.SE20,               99 },
            { チップ種別.SE21,               99 },
            { チップ種別.SE22,               99 },
            { チップ種別.SE23,               99 },
            { チップ種別.SE24,               99 },
            { チップ種別.SE25,               99 },
            { チップ種別.SE26,               99 },
            { チップ種別.SE27,               99 },
            { チップ種別.SE28,               99 },
            { チップ種別.SE29,               99 },
            { チップ種別.SE30,               99 },
            { チップ種別.SE31,               99 },
            { チップ種別.SE32,               99 },
            { チップ種別.Unknown,            99 },
            //-----------------
            #endregion
        };
    }
}
