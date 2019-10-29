using System;

namespace SSTFormat.v003
{
    /// <summary>
    ///		チップの種別を表す整数値。
    /// </summary>
    /// <remarks>
    ///		互換性を維持するために、将来にわたって不変な int 型の数値を、明確に定義する。
    /// </remarks>
    public enum チップ種別 : int
    {
        /// <summary>
        ///     未知。
        /// </summary>
        Unknown = 0,

        #region " シンバル類 "
        //----------------

        /// <summary>
        ///     クラッシュシンバル（左）。
        /// </summary>
        LeftCrash = 1,

        /// <summary>
        ///     クラッシュシンバル（右）。
        /// </summary>
        RightCrash = 21,

        /// <summary>
        ///     シンバルミュート（左）。v1.2以降。
        /// </summary>
        LeftCymbal_Mute = 27,

        /// <summary>
        ///     シンバルミュート（右）。v1.2以降。
        /// </summary>
        RightCymbal_Mute = 28,

        /// <summary>
        ///     ライドシンバル。
        /// </summary>
        Ride = 2,

        /// <summary>
        ///     ライドシンバルのカップ。
        /// </summary>
        Ride_Cup = 3,

        /// <summary>
        ///     チャイナシンバル。
        /// </summary>
        China = 4,

        /// <summary>
        ///     スプラッシュシンバル。
        /// </summary>
        Splash = 5,

        //----------------
        #endregion

        #region " ハイハット類 "
        //----------------

        /// <summary>
        ///     ハイハットオープン。
        /// </summary>
        HiHat_Open = 6,

        /// <summary>
        ///     ハイハットハーフオープン（半開き）。
        /// </summary>
        HiHat_HalfOpen = 7,

        /// <summary>
        ///     ハイハットクローズ。
        /// </summary>
        HiHat_Close = 8,

        /// <summary>
        ///     フットハイハット。フットスプラッシュではない。
        /// </summary>
        HiHat_Foot = 9,

        //----------------
        #endregion

        #region " パッド類 "
        //----------------

        /// <summary>
        ///     スネア。
        /// </summary>
        Snare = 10,

        /// <summary>
        ///     スネアのオープンリムショット。
        /// </summary>
        Snare_OpenRim = 11,

        /// <summary>
        ///     スネアのクローズドリムショット。
        /// </summary>
        Snare_ClosedRim = 12,

        /// <summary>
        ///     スネアのゴーストショット。
        /// </summary>
        Snare_Ghost = 13,

        /// <summary>
        ///     バスドラム（右）。
        /// </summary>
        Bass = 14,

        /// <summary>
        ///     バスドラム（左）。v3.1 以降。
        /// </summary>
        LeftBass = 38,

        /// <summary>
        ///     ハイタム。
        /// </summary>
        Tom1 = 15,

        /// <summary>
        ///     ハイタムのリムショット。
        /// </summary>
        Tom1_Rim = 16,

        /// <summary>
        ///     ロータム。
        /// </summary>
        Tom2 = 17,

        /// <summary>
        ///     ロータムのリムショット。
        /// </summary>
        Tom2_Rim = 18,

        /// <summary>
        ///     フロアタム。
        /// </summary>
        Tom3 = 19,

        /// <summary>
        ///     フロアタムのリムショット。
        /// </summary>
        Tom3_Rim = 20,

        //----------------
        #endregion

        #region " オートサウンド類 "
        //----------------

        /// <summary>
        ///     動画の再生を開始する。
        /// </summary>
        /// <remarks>
        ///     再生する動画の識別子は <see cref="スコア.背景動画ID"/> に格納される。
        ///     背景動画が音声も持つ場合には、音声の再生も開始される。
        /// </remarks>
        背景動画 = 25,

        /// <summary>
        ///     BGMの再生を開始する。v3.0以降。
        /// </summary>
        /// <remarks>
        ///     再生するサウンドのファイル名は、<see cref="チップ.チップサブID"/> をキーとする <see cref="スコア.WAVリスト"/> に格納される。
        /// </remarks>
        BGM = 30,

        /// <summary>
        ///     効果音１の再生を開始する。v3.0以降。
        /// </summary>
        /// <remarks>
        ///     再生するサウンドのファイル名は、<see cref="チップ.チップサブID"/> をキーとする <see cref="スコア.WAVリスト"/> に格納される。
        /// </remarks>
        SE1 = 31,

        /// <summary>
        ///     効果音２の再生を開始する。v3.0以降。
        /// </summary>
        /// <remarks>
        ///     再生するサウンドのファイル名は、<see cref="チップ.チップサブID"/> をキーとする <see cref="スコア.WAVリスト"/> に格納される。
        /// </remarks>
        SE2 = 32,

        /// <summary>
        ///     効果音３の再生を開始する。v3.0以降。
        /// </summary>
        /// <remarks>
        ///     再生するサウンドのファイル名は、<see cref="チップ.チップサブID"/> をキーとする <see cref="スコア.WAVリスト"/> に格納される。
        /// </remarks>
        SE3 = 33,

        /// <summary>
        ///     効果音４の再生を開始する。v3.0以降。
        /// </summary>
        /// <remarks>
        ///     再生するサウンドのファイル名は、<see cref="チップ.チップサブID"/> をキーとする <see cref="スコア.WAVリスト"/> に格納される。
        /// </remarks>
        SE4 = 34,

        /// <summary>
        ///     効果音５の再生を開始する。v3.0以降。
        /// </summary>
        /// <remarks>
        ///     再生するサウンドのファイル名は、<see cref="チップ.チップサブID"/> をキーとする <see cref="スコア.WAVリスト"/> に格納される。
        /// </remarks>
        SE5 = 35,

        /// <summary>
        ///     ギター音の再生を開始する。v3.0以降。
        /// </summary>
        /// <remarks>
        ///     再生するサウンドのファイル名は、<see cref="チップ.チップサブID"/> をキーとする <see cref="スコア.WAVリスト"/> に格納される。
        /// </remarks>
        GuitarAuto = 36,

        /// <summary>
        ///     ベース音の再生を開始する。v3.0以降。
        /// </summary>
        /// <remarks>
        ///     再生するサウンドのファイル名は、<see cref="チップ.チップサブID"/> をキーとする <see cref="スコア.WAVリスト"/> に格納される。
        /// </remarks>
        BassAuto = 37,

        //----------------
        #endregion

        #region " 制御類 "
        //----------------

        /// <summary>
        ///     BPM（Beat per Minutes；１分間の拍数）を変更する。
        /// </summary>
        /// <remarks>
        ///     BPM 値は、<see cref="チップ.BPM"/> に格納される。
        /// </remarks>
        BPM = 22,

        /// <summary>
        ///     小節の先頭に置かれることが保証される以外に意味はない。v1.2以降。
        /// </summary>
        小節の先頭 = 29,

        //----------------
        #endregion

        #region " アクセサリ類 "
        //----------------

        /// <summary>
        ///     小節線を配置する。
        /// </summary>
        小節線 = 23,

        /// <summary>
        ///     拍線を配置する。
        /// </summary>
        拍線 = 24,

        /// <summary>
        ///     小節に対するメモ（文字列）を保持する。
        /// </summary>
        /// <remarks>
        ///     メモは、チップとは関係なく、<see cref="スコア.小節メモリスト"/> に格納される。
        ///     そのため、この種別は非推奨。
        /// </remarks>
        小節メモ = 26,

        //----------------
        #endregion

        // ※現時点の最終値: 38
    }
}
