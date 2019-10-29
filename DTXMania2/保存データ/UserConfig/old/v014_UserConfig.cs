using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Data.Sqlite;
using YamlDotNet.Serialization;

namespace DTXMania2.old.UserConfig
{
    class v014_UserConfig
    {
        public const int VERSION = 14;    // このクラスのバージョン

        public static readonly VariablePath UserConfigPath = @"$(AppData)\UserDB.sqlite3";



        // プロパティ


        /// <summary>
        ///     このクラスのバージョン。
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        ///		ユーザを一意に識別する文字列。主キーなので変更不可。
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///		ユーザ名。変更可。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///		譜面スクロール速度の倍率。1.0で等倍。
        /// </summary>
        public double ScrollSpeed { get; set; }

        /// <summary>
        ///		起動直後の表示モード。
        ///		0: ウィンドウモード、その他: 全画面モード。
        /// </summary>
        public int Fullscreen { get; set; }

        /// <summary>
        ///		シンバルフリーモード。
        ///		0: OFF, その他: ON
        /// </summary>
        public int CymbalFree { get; set; }

        /// <summary>
        ///		Ride の表示位置。
        ///		0: 右, 1: 左
        /// </summary>
        public int RideLeft { get; set; }

        /// <summary>
        ///		China の表示位置。
        ///		0: 右, 1: 左
        /// </summary>
        public int ChinaLeft { get; set; }

        /// <summary>
        ///		Splash の表示位置。
        ///		0: 右, 1: 左
        /// </summary>
        public int SplashLeft { get; set; }

        /// <summary>
        ///		ユーザ入力時にドラム音を発声するか？
        ///		0: OFF, その他: ON
        /// </summary>
        public int DrumSound { get; set; }

        /// <summary>
        ///     レーンの透過度[%]。
        ///     0:完全不透明 ～ 100:完全透明
        /// </summary>
        public int LaneTrans { get; set; }

        /// <summary>
        ///		演奏時に再生される背景動画を表示するか？
        ///		0: OFF, その他: ON
        /// </summary>
        public int BackgroundMovie { get; set; }

        /// <summary>
        ///     演奏速度。
        ///     1.0 で通常速度。
        /// </summary>
        public double PlaySpeed { get; set; }

        /// <summary>
        ///		小節線・拍線の表示
        ///		0: OFF, その他: ON
        /// </summary>
        public int ShowPartLine { get; set; }

        /// <summary>
        ///		小節番号の表示
        ///		0: OFF, その他: ON
        /// </summary>
        public int ShowPartNumber { get; set; }

        /// <summary>
        ///		スコア指定の背景画像の表示
        ///		0: OFF, その他: ON
        /// </summary>
        public int ShowScoreWall { get; set; }

        /// <summary>
        ///		演奏中の背景動画の表示サイズ
        ///		0: 全画面, 1: 中央寄せ
        /// </summary>
        public int BackgroundMovieSize { get; set; }

        /// <summary>
        ///		判定FAST/SLOWの表示
        ///		0: OFF, その他: ON
        /// </summary>
        public int ShowFastSlow { get; set; }

        /// <summary>
        ///     音量によるノーとサイズの変化
        ///     0: OFF, その他: ON
        /// </summary>
        public int NoteSizeByVolume { get; set; }

        /// <summary>
        ///     ダークモード。
        ///     0: OFF, 1:HALF, 2:FULL
        /// </summary>
        public int Dark { get; set; }

        // AutoPlay

        /// <summary>
        ///		左シンバルレーンの AutoPlay 。
        ///		0: OFF, その他: ON。
        /// </summary>
        public int AutoPlay_LeftCymbal { get; set; }

        /// <summary>
        ///		ハイハットレーンの AutoPlay 。
        ///		0: OFF, その他: ON。
        /// </summary>
        public int AutoPlay_HiHat { get; set; }

        /// <summary>
        ///		左ペダルレーンの AutoPlay 。
        ///		0: OFF, その他: ON。
        /// </summary>
        public int AutoPlay_LeftPedal { get; set; }

        /// <summary>
        ///		スネアレーンの AutoPlay 。
        ///		0: OFF, その他: ON。
        /// </summary>
        public int AutoPlay_Snare { get; set; }

        /// <summary>
        ///		バスレーンの AutoPlay 。
        ///		0: OFF, その他: ON。
        /// </summary>
        public int AutoPlay_Bass { get; set; }

        /// <summary>
        ///		ハイタムレーンの AutoPlay 。
        ///		0: OFF, その他: ON。
        /// </summary>
        public int AutoPlay_HighTom { get; set; }

        /// <summary>
        ///		ロータムレーンの AutoPlay 。
        ///		0: OFF, その他: ON。
        /// </summary>
        public int AutoPlay_LowTom { get; set; }

        /// <summary>
        ///		フロアタムレーンの AutoPlay 。
        ///		0: OFF, その他: ON。
        /// </summary>
        public int AutoPlay_FloorTom { get; set; }

        /// <summary>
        ///		右シンバルレーンの AutoPlay 。
        ///		0: OFF, その他: ON。
        /// </summary>
        public int AutoPlay_RightCymbal { get; set; }

        // 最大ヒット距離

        /// <summary>
        ///		Perfect の最大ヒット距離[秒]。
        /// </summary>
        public double MaxRange_Perfect { get; set; }

        /// <summary>
        ///		Great の最大ヒット距離[秒]。
        /// </summary>
        public double MaxRange_Great { get; set; }

        /// <summary>
        ///		Good の最大ヒット距離[秒]。
        /// </summary>
        public double MaxRange_Good { get; set; }

        /// <summary>
        ///		Ok の最大ヒット距離[秒]。
        /// </summary>
        public double MaxRange_Ok { get; set; }



        // 生成と終了


        public v014_UserConfig()
        {
            this.Version = VERSION;
            this.Id = "Anonymous";
            this.Name = "Anonymous";
            this.ScrollSpeed = 1.0;
            this.Fullscreen = 0;
            this.CymbalFree = 1;
            this.RideLeft = 0;
            this.ChinaLeft = 0;
            this.SplashLeft = 1;
            this.DrumSound = 1;
            this.LaneTrans = 40;
            this.BackgroundMovie = 1;
            this.PlaySpeed = 1.0;
            this.ShowPartLine = 1;
            this.ShowPartNumber = 1;
            this.ShowScoreWall = 1;
            this.BackgroundMovieSize = 0;
            this.ShowFastSlow = 0;
            this.NoteSizeByVolume = 1;
            this.Dark = 0;
            this.AutoPlay_LeftCymbal = 1;
            this.AutoPlay_HiHat = 1;
            this.AutoPlay_LeftPedal = 1;
            this.AutoPlay_Snare = 1;
            this.AutoPlay_Bass = 1;
            this.AutoPlay_HighTom = 1;
            this.AutoPlay_LowTom = 1;
            this.AutoPlay_FloorTom = 1;
            this.AutoPlay_RightCymbal = 1;
            this.MaxRange_Perfect = 0.034;
            this.MaxRange_Great = 0.067;
            this.MaxRange_Good = 0.084;
            this.MaxRange_Ok = 0.117;
        }
    }
}
