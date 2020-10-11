using System;
using System.Collections.Generic;
using System.Diagnostics;
using FDK;
using SSTF=SSTFormat.v004;

namespace DTXMania2.演奏
{
    class ドラムチッププロパティリスト
    {

        // プロパティ


        /// <summary>
        ///		チップ種別をキーとする対応表。
        /// </summary>
        public Dictionary<SSTF.チップ種別, ドラムチッププロパティ> チップtoプロパティ { get; protected set; }

        /// <summary>
        ///     インデクサによるプロパティの取得。
        /// </summary>
        public ドラムチッププロパティ this[ SSTF.チップ種別 chipType ] => this.チップtoプロパティ[ chipType ];

        public 表示レーンの左右 表示レーンの左右 { get; protected set; }

        public 入力グループプリセット種別 入力グループプリセット種別 { get; protected set; }



        // 生成と終了


        public ドラムチッププロパティリスト( 表示レーンの左右 表示レーンの左右, 入力グループプリセット種別 入力グループプリセット種別 )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this.表示レーンの左右 = 表示レーンの左右;
            this.入力グループプリセット種別 = 入力グループプリセット種別;

            this.チップtoプロパティ = new Dictionary<SSTF.チップ種別, ドラムチッププロパティ>() {

                // ※以下、コメントアウト(=...)されている初期化子は、「後で反映する」の意。

                #region " チップ種別.Unknown "
                //----------------
                [ SSTF.チップ種別.Unknown ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.Unknown,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = false,
                    消音グループ種別 = 消音グループ種別.Unknown,
                    AutoPlayON_自動ヒット_再生 = false,
                    AutoPlayON_自動ヒット_非表示 = false,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.LeftCrash "
                //----------------
                [ SSTF.チップ種別.LeftCrash ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.LeftCrash,
                    レーン種別 = SSTF.レーン種別.LeftCrash,
                    表示レーン種別 = 表示レーン種別.LeftCymbal,
                    表示チップ種別 = 表示チップ種別.LeftCymbal,
                    ドラム入力種別 = ドラム入力種別.LeftCrash,
                    AutoPlay種別 = AutoPlay種別.LeftCrash,
                    //入力グループ種別 = ...
                    発声前消音 = false,
                    消音グループ種別 = 消音グループ種別.LeftCymbal,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = true,
                    AutoPlayON_Miss判定 = true,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = true,
                    AutoPlayOFF_ユーザヒット_非表示 = true,
                    AutoPlayOFF_ユーザヒット_判定 = true,
                    AutoPlayOFF_Miss判定 = true,
                },
                //----------------
                #endregion
                #region " チップ種別.Ride "
                //----------------
                [ SSTF.チップ種別.Ride ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.Ride,
                    レーン種別 = SSTF.レーン種別.Ride,
                    //表示レーン種別 = ...
                    表示チップ種別 = ( this.表示レーンの左右.Rideは左 ) ? 表示チップ種別.LeftRide : 表示チップ種別.RightRide,
                    ドラム入力種別 = ドラム入力種別.Ride,
                    //AutoPlay種別 = ...
                    //入力グループ種別 = ...
                    発声前消音 = false,
                    //消音グループ種別 = ...
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = true,
                    AutoPlayON_Miss判定 = true,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = true,
                    AutoPlayOFF_ユーザヒット_非表示 = true,
                    AutoPlayOFF_ユーザヒット_判定 = true,
                    AutoPlayOFF_Miss判定 = true,
                },
                //----------------
                #endregion
                #region " チップ種別.Ride_Cup "
                //----------------
                [ SSTF.チップ種別.Ride_Cup ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.Ride_Cup,
                    レーン種別 = SSTF.レーン種別.Ride,
                    //表示レーン種別 = ...
                    表示チップ種別 = ( this.表示レーンの左右.Rideは左 ) ? 表示チップ種別.LeftRide_Cup : 表示チップ種別.RightRide_Cup,
                    ドラム入力種別 = ドラム入力種別.Ride,
                    //AutoPlay種別 = ...
                    //入力グループ種別 = ...
                    発声前消音 = false,
                    //消音グループ種別 = ...
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = true,
                    AutoPlayON_Miss判定 = true,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = true,
                    AutoPlayOFF_ユーザヒット_非表示 = true,
                    AutoPlayOFF_ユーザヒット_判定 = true,
                    AutoPlayOFF_Miss判定 = true,
                },
                //----------------
                #endregion
                #region " チップ種別.China "
                //----------------
                [ SSTF.チップ種別.China ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.China,
                    レーン種別 = SSTF.レーン種別.China,
                    //表示レーン種別 = ...
                    表示チップ種別 = ( this.表示レーンの左右.Chinaは左 ) ? 表示チップ種別.LeftChina : 表示チップ種別.RightChina,
                    ドラム入力種別 = ドラム入力種別.China,
                    //AutoPlay種別 = ...
                    //入力グループ種別 = ...
                    発声前消音 = false,
                    //消音グループ種別 = ...
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = true,
                    AutoPlayON_Miss判定 = true,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = true,
                    AutoPlayOFF_ユーザヒット_非表示 = true,
                    AutoPlayOFF_ユーザヒット_判定 = true,
                    AutoPlayOFF_Miss判定 = true,
                },
                //----------------
                #endregion
                #region " チップ種別.Splash "
                //----------------
                [ SSTF.チップ種別.Splash ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.Splash,
                    レーン種別 = SSTF.レーン種別.Splash,
                    //表示レーン種別 = ...
                    表示チップ種別 = ( this.表示レーンの左右.Splashは左 ) ? 表示チップ種別.LeftSplash : 表示チップ種別.RightSplash,
                    ドラム入力種別 = ドラム入力種別.Splash,
                    //AutoPlay種別 = ...
                    //入力グループ種別 = ...
                    発声前消音 = false,
                    //消音グループ種別 = ...
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = true,
                    AutoPlayON_Miss判定 = true,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = true,
                    AutoPlayOFF_ユーザヒット_非表示 = true,
                    AutoPlayOFF_ユーザヒット_判定 = true,
                    AutoPlayOFF_Miss判定 = true,
                },
                //----------------
                #endregion
                #region " チップ種別.HiHat_Open "
                //----------------
                [ SSTF.チップ種別.HiHat_Open ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.HiHat_Open,
                    レーン種別 = SSTF.レーン種別.HiHat,
                    表示レーン種別 = 表示レーン種別.HiHat,
                    表示チップ種別 = 表示チップ種別.HiHat_Open,
                    ドラム入力種別 = ドラム入力種別.HiHat_Open,
                    AutoPlay種別 = AutoPlay種別.HiHat,
                    //入力グループ種別 = ...
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.HiHat,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = true,
                    AutoPlayON_Miss判定 = true,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = true,
                    AutoPlayOFF_ユーザヒット_非表示 = true,
                    AutoPlayOFF_ユーザヒット_判定 = true,
                    AutoPlayOFF_Miss判定 = true,
                },
                //----------------
                #endregion
                #region " チップ種別.HiHat_HalfOpen "
                //----------------
                [ SSTF.チップ種別.HiHat_HalfOpen ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.HiHat_HalfOpen,
                    レーン種別 = SSTF.レーン種別.HiHat,
                    表示レーン種別 = 表示レーン種別.HiHat,
                    表示チップ種別 = 表示チップ種別.HiHat_HalfOpen,
                    ドラム入力種別 = ドラム入力種別.HiHat_Open,
                    AutoPlay種別 = AutoPlay種別.HiHat,
                    //入力グループ種別 = ...
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.HiHat,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = true,
                    AutoPlayON_Miss判定 = true,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = true,
                    AutoPlayOFF_ユーザヒット_非表示 = true,
                    AutoPlayOFF_ユーザヒット_判定 = true,
                    AutoPlayOFF_Miss判定 = true,
                },
                //----------------
                #endregion
                #region " チップ種別.HiHat_Close "
                //----------------
                [ SSTF.チップ種別.HiHat_Close ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.HiHat_Close,
                    レーン種別 = SSTF.レーン種別.HiHat,
                    表示レーン種別 = 表示レーン種別.HiHat,
                    表示チップ種別 = 表示チップ種別.HiHat,
                    ドラム入力種別 = ドラム入力種別.HiHat_Close,
                    AutoPlay種別 = AutoPlay種別.HiHat,
                    //入力グループ種別 = ...
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.HiHat,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = true,
                    AutoPlayON_Miss判定 = true,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = true,
                    AutoPlayOFF_ユーザヒット_非表示 = true,
                    AutoPlayOFF_ユーザヒット_判定 = true,
                    AutoPlayOFF_Miss判定 = true,
                },
                //----------------
                #endregion
                #region " チップ種別.HiHat_Foot "
                //----------------
                [ SSTF.チップ種別.HiHat_Foot ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.HiHat_Foot,
                    レーン種別 = SSTF.レーン種別.Foot,
                    表示レーン種別 = 表示レーン種別.Foot,
                    表示チップ種別 = 表示チップ種別.Foot,
                    ドラム入力種別 = ドラム入力種別.HiHat_Foot,
                    AutoPlay種別 = AutoPlay種別.Foot,
                    //入力グループ種別 = ...
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.HiHat,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = true,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = true,
                    AutoPlayOFF_ユーザヒット_非表示 = true,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.Snare "
                //----------------
                [ SSTF.チップ種別.Snare ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.Snare,
                    レーン種別 = SSTF.レーン種別.Snare,
                    表示レーン種別 = 表示レーン種別.Snare,
                    表示チップ種別 = 表示チップ種別.Snare,
                    ドラム入力種別 = ドラム入力種別.Snare,
                    AutoPlay種別 = AutoPlay種別.Snare,
                    入力グループ種別 = 入力グループ種別.Snare,
                    発声前消音 = false,
                    消音グループ種別 = 消音グループ種別.Unknown,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = true,
                    AutoPlayON_Miss判定 = true,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = true,
                    AutoPlayOFF_ユーザヒット_非表示 = true,
                    AutoPlayOFF_ユーザヒット_判定 = true,
                    AutoPlayOFF_Miss判定 = true,
                },
                //----------------
                #endregion
                #region " チップ種別.Snare_OpenRim "
                //----------------
                [ SSTF.チップ種別.Snare_OpenRim ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.Snare_OpenRim,
                    レーン種別 = SSTF.レーン種別.Snare,
                    表示レーン種別 = 表示レーン種別.Snare,
                    表示チップ種別 = 表示チップ種別.Snare_OpenRim,
                    ドラム入力種別 = ドラム入力種別.Snare_OpenRim,
                    AutoPlay種別 = AutoPlay種別.Snare,
                    入力グループ種別 = 入力グループ種別.Snare,
                    発声前消音 = false,
                    消音グループ種別 = 消音グループ種別.Unknown,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = true,
                    AutoPlayON_Miss判定 = true,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = true,
                    AutoPlayOFF_ユーザヒット_非表示 = true,
                    AutoPlayOFF_ユーザヒット_判定 = true,
                    AutoPlayOFF_Miss判定 = true,
                },
                //----------------
                #endregion
                #region " チップ種別.Snare_ClosedRim "
                //----------------
                [ SSTF.チップ種別.Snare_ClosedRim ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.Snare_ClosedRim,
                    レーン種別 = SSTF.レーン種別.Snare,
                    表示レーン種別 = 表示レーン種別.Snare,
                    表示チップ種別 = 表示チップ種別.Snare_ClosedRim,
                    ドラム入力種別 = ドラム入力種別.Snare_ClosedRim,
                    AutoPlay種別 = AutoPlay種別.Snare,
                    入力グループ種別 = 入力グループ種別.Snare,
                    発声前消音 = false,
                    消音グループ種別 = 消音グループ種別.Unknown,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = true,
                    AutoPlayON_Miss判定 = true,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = true,
                    AutoPlayOFF_ユーザヒット_非表示 = true,
                    AutoPlayOFF_ユーザヒット_判定 = true,
                    AutoPlayOFF_Miss判定 = true,
                },
                //----------------
                #endregion
                #region " チップ種別.Snare_Ghost "
                //----------------
                [ SSTF.チップ種別.Snare_Ghost ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.Snare_Ghost,
                    レーン種別 = SSTF.レーン種別.Snare,
                    表示レーン種別 = 表示レーン種別.Snare,
                    表示チップ種別 = 表示チップ種別.Snare_Ghost,
                    ドラム入力種別 = ドラム入力種別.Snare,
                    AutoPlay種別 = AutoPlay種別.Snare,
                    入力グループ種別 = 入力グループ種別.Snare,
                    発声前消音 = false,
                    消音グループ種別 = 消音グループ種別.Unknown,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = true,
                    AutoPlayOFF_ユーザヒット_非表示 = true,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.Bass "
                //----------------
                [ SSTF.チップ種別.Bass ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.Bass,
                    レーン種別 = SSTF.レーン種別.Bass,
                    表示レーン種別 = 表示レーン種別.Bass,
                    表示チップ種別 = 表示チップ種別.Bass,
                    ドラム入力種別 = ドラム入力種別.Bass,
                    AutoPlay種別 = AutoPlay種別.Bass,
                    入力グループ種別 = 入力グループ種別.Bass,
                    発声前消音 = false,
                    消音グループ種別 = 消音グループ種別.Unknown,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = true,
                    AutoPlayON_Miss判定 = true,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = true,
                    AutoPlayOFF_ユーザヒット_非表示 = true,
                    AutoPlayOFF_ユーザヒット_判定 = true,
                    AutoPlayOFF_Miss判定 = true,
                },
                //----------------
                #endregion
                #region " チップ種別.LeftBass "
                //----------------
                [ SSTF.チップ種別.LeftBass ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.LeftBass,
                    レーン種別 = SSTF.レーン種別.Bass,
                    表示レーン種別 = 表示レーン種別.Bass,
                    表示チップ種別 = 表示チップ種別.Bass,
                    ドラム入力種別 = ドラム入力種別.Bass,
                    AutoPlay種別 = AutoPlay種別.Bass,
                    //入力グループ種別 = 入力グループ種別.Bass,
                    発声前消音 = false,
                    消音グループ種別 = 消音グループ種別.Unknown,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = true,
                    AutoPlayON_Miss判定 = true,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = true,
                    AutoPlayOFF_ユーザヒット_非表示 = true,
                    AutoPlayOFF_ユーザヒット_判定 = true,
                    AutoPlayOFF_Miss判定 = true,
                },
                //----------------
                #endregion
                #region " チップ種別.Tom1 "
                //----------------
                [ SSTF.チップ種別.Tom1 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.Tom1,
                    レーン種別 = SSTF.レーン種別.Tom1,
                    表示レーン種別 = 表示レーン種別.Tom1,
                    表示チップ種別 = 表示チップ種別.Tom1,
                    ドラム入力種別 = ドラム入力種別.Tom1,
                    AutoPlay種別 = AutoPlay種別.Tom1,
                    入力グループ種別 = 入力グループ種別.Tom1,
                    発声前消音 = false,
                    消音グループ種別 = 消音グループ種別.Unknown,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = true,
                    AutoPlayON_Miss判定 = true,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = true,
                    AutoPlayOFF_ユーザヒット_非表示 = true,
                    AutoPlayOFF_ユーザヒット_判定 = true,
                    AutoPlayOFF_Miss判定 = true,
                },
                //----------------
                #endregion
                #region " チップ種別.Tom1_Rim "
                //----------------
                [ SSTF.チップ種別.Tom1_Rim ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.Tom1_Rim,
                    レーン種別 = SSTF.レーン種別.Tom1,
                    表示レーン種別 = 表示レーン種別.Tom1,
                    表示チップ種別 = 表示チップ種別.Tom1_Rim,
                    ドラム入力種別 = ドラム入力種別.Tom1_Rim,
                    AutoPlay種別 = AutoPlay種別.Tom1,
                    入力グループ種別 = 入力グループ種別.Tom1,
                    発声前消音 = false,
                    消音グループ種別 = 消音グループ種別.Unknown,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = true,
                    AutoPlayON_Miss判定 = true,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = true,
                    AutoPlayOFF_ユーザヒット_非表示 = true,
                    AutoPlayOFF_ユーザヒット_判定 = true,
                    AutoPlayOFF_Miss判定 = true,
                },
                //----------------
                #endregion
                #region " チップ種別.Tom2 "
                //----------------
                [ SSTF.チップ種別.Tom2 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.Tom2,
                    レーン種別 = SSTF.レーン種別.Tom2,
                    表示レーン種別 = 表示レーン種別.Tom2,
                    表示チップ種別 = 表示チップ種別.Tom2,
                    ドラム入力種別 = ドラム入力種別.Tom2,
                    AutoPlay種別 = AutoPlay種別.Tom2,
                    入力グループ種別 = 入力グループ種別.Tom2,
                    発声前消音 = false,
                    消音グループ種別 = 消音グループ種別.Unknown,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = true,
                    AutoPlayON_Miss判定 = true,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = true,
                    AutoPlayOFF_ユーザヒット_非表示 = true,
                    AutoPlayOFF_ユーザヒット_判定 = true,
                    AutoPlayOFF_Miss判定 = true,
                },
                //----------------
                #endregion
                #region " チップ種別.Tom2_Rim "
                //----------------
                [ SSTF.チップ種別.Tom2_Rim ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.Tom2_Rim,
                    レーン種別 = SSTF.レーン種別.Tom2,
                    表示レーン種別 = 表示レーン種別.Tom2,
                    表示チップ種別 = 表示チップ種別.Tom2_Rim,
                    ドラム入力種別 = ドラム入力種別.Tom2_Rim,
                    AutoPlay種別 = AutoPlay種別.Tom2,
                    入力グループ種別 = 入力グループ種別.Tom2,
                    発声前消音 = false,
                    消音グループ種別 = 消音グループ種別.Unknown,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = true,
                    AutoPlayON_Miss判定 = true,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = true,
                    AutoPlayOFF_ユーザヒット_非表示 = true,
                    AutoPlayOFF_ユーザヒット_判定 = true,
                    AutoPlayOFF_Miss判定 = true,
                },
                //----------------
                #endregion
                #region " チップ種別.Tom3 "
                //----------------
                [ SSTF.チップ種別.Tom3 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.Tom3,
                    レーン種別 = SSTF.レーン種別.Tom3,
                    表示レーン種別 = 表示レーン種別.Tom3,
                    表示チップ種別 = 表示チップ種別.Tom3,
                    ドラム入力種別 = ドラム入力種別.Tom3,
                    AutoPlay種別 = AutoPlay種別.Tom3,
                    入力グループ種別 = 入力グループ種別.Tom3,
                    発声前消音 = false,
                    消音グループ種別 = 消音グループ種別.Unknown,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = true,
                    AutoPlayON_Miss判定 = true,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = true,
                    AutoPlayOFF_ユーザヒット_非表示 = true,
                    AutoPlayOFF_ユーザヒット_判定 = true,
                    AutoPlayOFF_Miss判定 = true,
                },
                //----------------
                #endregion
                #region " チップ種別.Tom3_Rim "
                //----------------
                [ SSTF.チップ種別.Tom3_Rim ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.Tom3_Rim,
                    レーン種別 = SSTF.レーン種別.Tom3,
                    表示レーン種別 = 表示レーン種別.Tom3,
                    表示チップ種別 = 表示チップ種別.Tom3_Rim,
                    ドラム入力種別 = ドラム入力種別.Tom3_Rim,
                    AutoPlay種別 = AutoPlay種別.Tom3,
                    入力グループ種別 = 入力グループ種別.Tom3,
                    発声前消音 = false,
                    消音グループ種別 = 消音グループ種別.Unknown,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = true,
                    AutoPlayON_Miss判定 = true,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = true,
                    AutoPlayOFF_ユーザヒット_非表示 = true,
                    AutoPlayOFF_ユーザヒット_判定 = true,
                    AutoPlayOFF_Miss判定 = true,
                },
                //----------------
                #endregion
                #region " チップ種別.RightCrash "
                //----------------
                [ SSTF.チップ種別.RightCrash ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.RightCrash,
                    レーン種別 = SSTF.レーン種別.RightCrash,
                    表示レーン種別 = 表示レーン種別.RightCymbal,
                    表示チップ種別 = 表示チップ種別.RightCymbal,
                    ドラム入力種別 = ドラム入力種別.RightCrash,
                    AutoPlay種別 = AutoPlay種別.RightCrash,
                    //入力グループ種別 = ...
                    発声前消音 = false,
                    消音グループ種別 = 消音グループ種別.RightCymbal,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = true,
                    AutoPlayON_Miss判定 = true,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = true,
                    AutoPlayOFF_ユーザヒット_非表示 = true,
                    AutoPlayOFF_ユーザヒット_判定 = true,
                    AutoPlayOFF_Miss判定 = true,
                },
                //----------------
                #endregion
                #region " チップ種別.BPM "
                //----------------
                [ SSTF.チップ種別.BPM ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.BPM,
                    レーン種別 = SSTF.レーン種別.BPM,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = false,
                    消音グループ種別 = 消音グループ種別.Unknown,
                    AutoPlayON_自動ヒット_再生 = false,
                    AutoPlayON_自動ヒット_非表示 = false,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.小節線 "
                //----------------
                [ SSTF.チップ種別.小節線 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.小節線,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = false,
                    消音グループ種別 = 消音グループ種別.Unknown,
                    AutoPlayON_自動ヒット_再生 = false,
                    AutoPlayON_自動ヒット_非表示 = false,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.拍線 "
                //----------------
                [ SSTF.チップ種別.拍線 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.拍線,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = false,
                    消音グループ種別 = 消音グループ種別.Unknown,
                    AutoPlayON_自動ヒット_再生 = false,
                    AutoPlayON_自動ヒット_非表示 = false,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.背景動画 "
                //----------------
                [ SSTF.チップ種別.背景動画 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.背景動画,
                    レーン種別 = SSTF.レーン種別.BGV,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = false,
                    消音グループ種別 = 消音グループ種別.Unknown,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.小節メモ "
                //----------------
                [ SSTF.チップ種別.小節メモ ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.小節メモ,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = false,
                    消音グループ種別 = 消音グループ種別.Unknown,
                    AutoPlayON_自動ヒット_再生 = false,
                    AutoPlayON_自動ヒット_非表示 = false,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.LeftCymbal_Mute "
                //----------------
                [ SSTF.チップ種別.LeftCymbal_Mute ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.LeftCymbal_Mute,
                    レーン種別 = SSTF.レーン種別.LeftCrash,
                    表示レーン種別 = 表示レーン種別.LeftCymbal,
                    表示チップ種別 = 表示チップ種別.LeftCymbal_Mute,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.LeftCrash,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.LeftCymbal,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.RightCymbal_Mute "
                //----------------
                [ SSTF.チップ種別.RightCymbal_Mute ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.RightCymbal_Mute,
                    レーン種別 = SSTF.レーン種別.RightCrash,
                    表示レーン種別 = 表示レーン種別.RightCymbal,
                    表示チップ種別 = 表示チップ種別.RightCymbal_Mute,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.RightCrash,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.RightCymbal,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.小節の先頭 "
                //----------------
                [ SSTF.チップ種別.小節の先頭 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.小節の先頭,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = false,
                    消音グループ種別 = 消音グループ種別.Unknown,
                    AutoPlayON_自動ヒット_再生 = false,
                    AutoPlayON_自動ヒット_非表示 = false,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = false,
                    AutoPlayOFF_自動ヒット_非表示 = false,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.BGM "
                //----------------
                [ SSTF.チップ種別.BGM ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.BGM,
                    レーン種別 = SSTF.レーン種別.BGM,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = false,
                    消音グループ種別 = 消音グループ種別.Unknown,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE1 "
                //----------------
                [ SSTF.チップ種別.SE1 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE1,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE1,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE2 "
                //----------------
                [ SSTF.チップ種別.SE2 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE2,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE2,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE3 "
                //----------------
                [ SSTF.チップ種別.SE3 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE3,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE3,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE4 "
                //----------------
                [ SSTF.チップ種別.SE4 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE4,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE4,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE5 "
                //----------------
                [ SSTF.チップ種別.SE5 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE5,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE5,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.GuitarAuto "
                //----------------
                [ SSTF.チップ種別.GuitarAuto ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.GuitarAuto,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.Guitar,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.BassAuto "
                //----------------
                [ SSTF.チップ種別.BassAuto ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.BassAuto,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.Bass,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE6 "
                //----------------
                [ SSTF.チップ種別.SE6 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE6,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE6,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE7 "
                //----------------
                [ SSTF.チップ種別.SE7 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE7,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE7,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE8 "
                //----------------
                [ SSTF.チップ種別.SE8 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE8,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE8,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE9 "
                //----------------
                [ SSTF.チップ種別.SE9 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE9,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE9,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE10 "
                //----------------
                [ SSTF.チップ種別.SE10 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE10,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE10,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE11 "
                //----------------
                [ SSTF.チップ種別.SE11 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE11,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE11,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE12 "
                //----------------
                [ SSTF.チップ種別.SE12 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE12,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE12,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE13 "
                //----------------
                [ SSTF.チップ種別.SE13 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE13,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE13,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE14 "
                //----------------
                [ SSTF.チップ種別.SE14 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE14,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE14,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE15 "
                //----------------
                [ SSTF.チップ種別.SE15 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE15,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE15,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE16 "
                //----------------
                [ SSTF.チップ種別.SE16 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE16,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE16,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE17 "
                //----------------
                [ SSTF.チップ種別.SE17 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE17,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE17,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE18 "
                //----------------
                [ SSTF.チップ種別.SE18 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE18,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE18,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE19 "
                //----------------
                [ SSTF.チップ種別.SE19 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE19,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE19,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE20 "
                //----------------
                [ SSTF.チップ種別.SE20 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE20,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE20,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE21 "
                //----------------
                [ SSTF.チップ種別.SE21 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE21,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE21,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE22 "
                //----------------
                [ SSTF.チップ種別.SE22 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE22,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE22,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE23 "
                //----------------
                [ SSTF.チップ種別.SE23 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE23,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE23,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE24 "
                //----------------
                [ SSTF.チップ種別.SE24 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE24,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE24,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE25 "
                //----------------
                [ SSTF.チップ種別.SE25 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE25,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE25,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE26 "
                //----------------
                [ SSTF.チップ種別.SE26 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE26,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE26,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE27 "
                //----------------
                [ SSTF.チップ種別.SE27 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE27,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE27,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE28 "
                //----------------
                [ SSTF.チップ種別.SE28 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE28,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE28,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE29 "
                //----------------
                [ SSTF.チップ種別.SE29 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE29,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE29,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE30 "
                //----------------
                [ SSTF.チップ種別.SE30 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE30,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE30,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE31 "
                //----------------
                [ SSTF.チップ種別.SE31 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE31,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE31,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
                #region " チップ種別.SE32 "
                //----------------
                [ SSTF.チップ種別.SE32 ] = new ドラムチッププロパティ() {
                    チップ種別 = SSTF.チップ種別.SE32,
                    レーン種別 = SSTF.レーン種別.Unknown,
                    表示レーン種別 = 表示レーン種別.Unknown,
                    表示チップ種別 = 表示チップ種別.Unknown,
                    ドラム入力種別 = ドラム入力種別.Unknown,
                    AutoPlay種別 = AutoPlay種別.Unknown,
                    入力グループ種別 = 入力グループ種別.Unknown,
                    発声前消音 = true,
                    消音グループ種別 = 消音グループ種別.SE32,
                    AutoPlayON_自動ヒット_再生 = true,
                    AutoPlayON_自動ヒット_非表示 = true,
                    AutoPlayON_自動ヒット_判定 = false,
                    AutoPlayON_Miss判定 = false,
                    AutoPlayOFF_自動ヒット_再生 = true,
                    AutoPlayOFF_自動ヒット_非表示 = true,
                    AutoPlayOFF_自動ヒット_判定 = false,
                    AutoPlayOFF_ユーザヒット_再生 = false,
                    AutoPlayOFF_ユーザヒット_非表示 = false,
                    AutoPlayOFF_ユーザヒット_判定 = false,
                    AutoPlayOFF_Miss判定 = false,
                },
                //----------------
                #endregion
            };

            this.反映する( 表示レーンの左右 );
            this.反映する( 入力グループプリセット種別 );
        }

        /// <summary>
        ///     表示レーンの左右に依存するメンバに対して一括設定を行う。
        ///     依存しないメンバには何もしない。
        /// </summary>
        public void 反映する( 表示レーンの左右 position )
        {
            this.表示レーンの左右 = position;

            foreach( var kvp in this.チップtoプロパティ )
            {
                switch( kvp.Key )
                {
                    case SSTF.チップ種別.Ride:
                        kvp.Value.表示レーン種別 = ( this.表示レーンの左右.Rideは左 ) ? 表示レーン種別.LeftCymbal : 表示レーン種別.RightCymbal;
                        kvp.Value.AutoPlay種別 = ( this.表示レーンの左右.Rideは左 ) ? AutoPlay種別.LeftCrash : AutoPlay種別.RightCrash;
                        kvp.Value.消音グループ種別 = ( this.表示レーンの左右.Rideは左 ) ? 消音グループ種別.LeftCymbal : 消音グループ種別.RightCymbal;
                        kvp.Value.表示チップ種別 = ( this.表示レーンの左右.Rideは左 ) ? 表示チップ種別.LeftRide : 表示チップ種別.RightRide;
                        break;

                    case SSTF.チップ種別.Ride_Cup:
                        kvp.Value.表示レーン種別 = ( this.表示レーンの左右.Rideは左 ) ? 表示レーン種別.LeftCymbal : 表示レーン種別.RightCymbal;
                        kvp.Value.AutoPlay種別 = ( this.表示レーンの左右.Rideは左 ) ? AutoPlay種別.LeftCrash : AutoPlay種別.RightCrash;
                        kvp.Value.消音グループ種別 = ( this.表示レーンの左右.Rideは左 ) ? 消音グループ種別.LeftCymbal : 消音グループ種別.RightCymbal;
                        kvp.Value.表示チップ種別 = ( this.表示レーンの左右.Rideは左 ) ? 表示チップ種別.LeftRide_Cup : 表示チップ種別.RightRide_Cup;
                        break;

                    case SSTF.チップ種別.China:
                        kvp.Value.表示レーン種別 = ( this.表示レーンの左右.Chinaは左 ) ? 表示レーン種別.LeftCymbal : 表示レーン種別.RightCymbal;
                        kvp.Value.AutoPlay種別 = ( this.表示レーンの左右.Chinaは左 ) ? AutoPlay種別.LeftCrash : AutoPlay種別.RightCrash;
                        kvp.Value.消音グループ種別 = ( this.表示レーンの左右.Chinaは左 ) ? 消音グループ種別.LeftCymbal : 消音グループ種別.RightCymbal;
                        kvp.Value.表示チップ種別 = ( this.表示レーンの左右.Chinaは左 ) ? 表示チップ種別.LeftChina : 表示チップ種別.RightChina;
                        break;

                    case SSTF.チップ種別.Splash:
                        kvp.Value.表示レーン種別 = ( this.表示レーンの左右.Splashは左 ) ? 表示レーン種別.LeftCymbal : 表示レーン種別.RightCymbal;
                        kvp.Value.AutoPlay種別 = ( this.表示レーンの左右.Splashは左 ) ? AutoPlay種別.LeftCrash : AutoPlay種別.RightCrash;
                        kvp.Value.消音グループ種別 = ( this.表示レーンの左右.Splashは左 ) ? 消音グループ種別.LeftCymbal : 消音グループ種別.RightCymbal;
                        kvp.Value.表示チップ種別 = ( this.表示レーンの左右.Splashは左 ) ? 表示チップ種別.LeftSplash : 表示チップ種別.RightSplash;
                        break;
                }
            }
        }

        /// <summary>
        ///     指定されたプリセットに依存する入力グループ種別を一括設定する。
        ///     依存しないメンバには何もしない。
        /// </summary>
        public void 反映する( 入力グループプリセット種別 preset )
        {
            this.入力グループプリセット種別 = preset;

            foreach( var kvp in this.チップtoプロパティ )
            {
                switch( this.入力グループプリセット種別 )
                {
                    case 入力グループプリセット種別.シンバルフリー:

                        switch( kvp.Key )
                        {
                            case SSTF.チップ種別.LeftCrash:
                            case SSTF.チップ種別.Ride:
                            case SSTF.チップ種別.Ride_Cup:
                            case SSTF.チップ種別.China:
                            case SSTF.チップ種別.Splash:
                            case SSTF.チップ種別.HiHat_Open:
                            case SSTF.チップ種別.HiHat_HalfOpen:
                            case SSTF.チップ種別.HiHat_Close:
                            case SSTF.チップ種別.HiHat_Foot:
                            case SSTF.チップ種別.RightCrash:
                                kvp.Value.入力グループ種別 = 入力グループ種別.Cymbal;
                                break;

                            case SSTF.チップ種別.LeftBass:
                                kvp.Value.入力グループ種別 = 入力グループ種別.Bass;
                                break;
                        }
                        break;

                    case 入力グループプリセット種別.基本形:

                        switch( kvp.Key )
                        {
                            case SSTF.チップ種別.LeftCrash:
                                kvp.Value.入力グループ種別 = 入力グループ種別.LeftCymbal;
                                break;

                            case SSTF.チップ種別.Ride:
                            case SSTF.チップ種別.Ride_Cup:
                                kvp.Value.入力グループ種別 = 入力グループ種別.Ride;
                                break;

                            case SSTF.チップ種別.China:
                                kvp.Value.入力グループ種別 = 入力グループ種別.China;
                                break;

                            case SSTF.チップ種別.Splash:
                                kvp.Value.入力グループ種別 = 入力グループ種別.Splash;
                                break;

                            case SSTF.チップ種別.HiHat_Open:
                            case SSTF.チップ種別.HiHat_HalfOpen:
                            case SSTF.チップ種別.HiHat_Close:
                            case SSTF.チップ種別.HiHat_Foot:
                                kvp.Value.入力グループ種別 = 入力グループ種別.HiHat;
                                break;

                            case SSTF.チップ種別.RightCrash:
                                kvp.Value.入力グループ種別 = 入力グループ種別.RightCymbal;
                                break;

                            case SSTF.チップ種別.LeftBass:
                                kvp.Value.入力グループ種別 = 入力グループ種別.Bass;
                                break;
                        }
                        break;

                    default:
                        throw new Exception( $"未知の入力グループプリセット種別です。[{this.入力グループプリセット種別.ToString()}]" );
                }
            }
        }
    }
}
