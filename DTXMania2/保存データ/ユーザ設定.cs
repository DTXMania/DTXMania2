using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FDK;

namespace DTXMania2
{
    class ユーザ設定
    {
        /// <summary>
        ///		ユーザID。
        ///		null ならこのインスタンスはどのユーザにも割り当てられていないことを示す。
        /// </summary>
        public string? ID => this._UserConfig.Id;

        public string 名前 => this._UserConfig.Name;

        public double 譜面スクロール速度
        {
            get => this._UserConfig.ScrollSpeed;
            set => this._UserConfig.ScrollSpeed = value;
        }

        public bool シンバルフリーモードである
        {
            get => ( 0 != this._UserConfig.CymbalFree );
            set => this._UserConfig.CymbalFree = value ? 1 : 0;
        }

        public bool AutoPlayがすべてONである
            => this.AutoPlay.All( ( kvp ) => kvp.Value );

        public Dictionary<演奏.AutoPlay種別, bool> AutoPlay { get; protected set; }

        /// <summary>
        ///		チップがヒット判定バーから（上または下に）どれだけ離れていると Perfect ～ Ok 判定になるのかの定義。秒単位。
        /// </summary>
        public Dictionary<演奏.判定種別, double> 最大ヒット距離sec { get; set; }

        public 演奏.ドラムチッププロパティリスト ドラムチッププロパティリスト { get; protected set; }

        public 演奏.表示レーンの左右 表示レーンの左右
        {
            get
            {
                return new 演奏.表示レーンの左右() {
                    Rideは左 = ( this._UserConfig.RideLeft != 0 ),
                    Chinaは左 = ( this._UserConfig.ChinaLeft != 0 ),
                    Splashは左 = ( this._UserConfig.SplashLeft != 0 ),
                };
            }
            set
            {
                this._UserConfig.RideLeft = ( value.Rideは左 ) ? 1 : 0;
                this._UserConfig.ChinaLeft = ( value.Chinaは左 ) ? 1 : 0;
                this._UserConfig.SplashLeft = ( value.Splashは左 ) ? 1 : 0;
            }
        }

        public bool ドラムの音を発声する
        {
            get => ( 0 != this._UserConfig.DrumSound );
            set => this._UserConfig.DrumSound = value ? 1 : 0;
        }

        public int レーンの透明度
        {
            get => this._UserConfig.LaneTrans;
            set => this._UserConfig.LaneTrans = value;
        }

        public bool 演奏中に動画を表示する
        {
            get => ( 0 != this._UserConfig.BackgroundMovie );
            set => this._UserConfig.BackgroundMovie = value ? 1 : 0;
        }

        public double 再生速度
        {
            get => this._UserConfig.PlaySpeed;
            set => this._UserConfig.PlaySpeed = value;
        }

        public bool 演奏中に小節線と拍線を表示する
        {
            get => ( 0 != this._UserConfig.ShowPartLine );
            set => this._UserConfig.ShowPartLine = value ? 1 : 0;
        }

        public bool 演奏中に小節番号を表示する
        {
            get => ( 0 != this._UserConfig.ShowPartNumber );
            set => this._UserConfig.ShowPartNumber = value ? 1 : 0;
        }

        public bool スコア指定の背景画像を表示する
        {
            get => ( 0 != this._UserConfig.ShowScoreWall );
            set => this._UserConfig.ShowScoreWall = value ? 1 : 0;
        }

        public 演奏.動画の表示サイズ 動画の表示サイズ
        {
            get => (演奏.動画の表示サイズ) this._UserConfig.BackgroundMovieSize;
            set => this._UserConfig.BackgroundMovieSize = (int) value;
        }

        public bool 演奏中に判定FastSlowを表示する
        {
            get => ( 0 != this._UserConfig.ShowFastSlow );
            set => this._UserConfig.ShowFastSlow = value ? 1 : 0;
        }

        public bool 音量に応じてチップサイズを変更する
        {
            get => ( 0 != this._UserConfig.NoteSizeByVolume );
            set => this._UserConfig.NoteSizeByVolume = value ? 1 : 0;
        }

        public 演奏.ダーク種別 ダーク
        {
            get => (演奏.ダーク種別) this._UserConfig.Dark;
            set => this._UserConfig.Dark = (int) value;
        }



        // 生成と終了


        public ユーザ設定()
        {
            this.AutoPlay = new Dictionary<演奏.AutoPlay種別, bool>() {
                { 演奏.AutoPlay種別.Unknown, true },
                { 演奏.AutoPlay種別.LeftCrash, false },
                { 演奏.AutoPlay種別.HiHat, false },
                { 演奏.AutoPlay種別.Foot, false },
                { 演奏.AutoPlay種別.Snare, false },
                { 演奏.AutoPlay種別.Bass, false },
                { 演奏.AutoPlay種別.Tom1, false },
                { 演奏.AutoPlay種別.Tom2, false },
                { 演奏.AutoPlay種別.Tom3, false },
                { 演奏.AutoPlay種別.RightCrash, false },
            };
            this.最大ヒット距離sec = new Dictionary<演奏.判定種別, double>() {
                { 演奏.判定種別.PERFECT, 0.034 },
                { 演奏.判定種別.GREAT, 0.067 },
                { 演奏.判定種別.GOOD, 0.084 },
                { 演奏.判定種別.OK, 0.117 },
            };
            this.ドラムチッププロパティリスト = new 演奏.ドラムチッププロパティリスト(
                new 演奏.表示レーンの左右() { 
                    Chinaは左 = false,
                    Rideは左 = false, 
                    Splashは左 = true,
                },
                演奏.入力グループプリセット種別.基本形 );
            this._UserConfig = new UserConfig();
            this._UserConfigから反映する();
        }

        public static ユーザ設定 読み込む( string id )
        {
            var config = new ユーザ設定();
            config._UserConfig = UserConfig.読み込む( id );
            config._UserConfigから反映する();
            return config;
        }

        public void 保存する()
        {
            this._UserConfigへ反映する();
            this._UserConfig.保存する();
        }



        // ローカル


        private UserConfig _UserConfig;

        private void _UserConfigから反映する()
        {
            this.AutoPlay = new Dictionary<演奏.AutoPlay種別, bool>() {
                { 演奏.AutoPlay種別.Unknown, true },
                { 演奏.AutoPlay種別.LeftCrash, ( this._UserConfig.AutoPlay_LeftCymbal != 0 ) },
                { 演奏.AutoPlay種別.HiHat, ( this._UserConfig.AutoPlay_HiHat != 0 ) },
                { 演奏.AutoPlay種別.Foot, ( this._UserConfig.AutoPlay_LeftPedal != 0 ) },
                { 演奏.AutoPlay種別.Snare, ( this._UserConfig.AutoPlay_Snare != 0 ) },
                { 演奏.AutoPlay種別.Bass, ( this._UserConfig.AutoPlay_Bass != 0 ) },
                { 演奏.AutoPlay種別.Tom1, ( this._UserConfig.AutoPlay_HighTom != 0 ) },
                { 演奏.AutoPlay種別.Tom2, ( this._UserConfig.AutoPlay_LowTom != 0 ) },
                { 演奏.AutoPlay種別.Tom3, ( this._UserConfig.AutoPlay_FloorTom != 0 ) },
                { 演奏.AutoPlay種別.RightCrash, ( this._UserConfig.AutoPlay_RightCymbal != 0 ) },
            };

            this.最大ヒット距離sec = new HookedDictionary<演奏.判定種別, double>() {
                { 演奏.判定種別.PERFECT, this._UserConfig.MaxRange_Perfect },
                { 演奏.判定種別.GREAT, this._UserConfig.MaxRange_Great },
                { 演奏.判定種別.GOOD, this._UserConfig.MaxRange_Good },
                { 演奏.判定種別.OK, this._UserConfig.MaxRange_Ok },
            };

            this.ドラムチッププロパティリスト.反映する( this.表示レーンの左右 );
            this.ドラムチッププロパティリスト.反映する( ( this.シンバルフリーモードである ) ? 演奏.入力グループプリセット種別.シンバルフリー : 演奏.入力グループプリセット種別.基本形 );
        }

        private void _UserConfigへ反映する()
        {
            this._UserConfig.AutoPlay_LeftCymbal = this.AutoPlay[ 演奏.AutoPlay種別.LeftCrash ] ? 1 : 0;
            this._UserConfig.AutoPlay_HiHat = this.AutoPlay[ 演奏.AutoPlay種別.HiHat ] ? 1 : 0;
            this._UserConfig.AutoPlay_LeftPedal = this.AutoPlay[ 演奏.AutoPlay種別.Foot ] ? 1 : 0;
            this._UserConfig.AutoPlay_Snare = this.AutoPlay[ 演奏.AutoPlay種別.Snare ] ? 1 : 0;
            this._UserConfig.AutoPlay_Bass = this.AutoPlay[ 演奏.AutoPlay種別.Bass ] ? 1 : 0;
            this._UserConfig.AutoPlay_HighTom = this.AutoPlay[ 演奏.AutoPlay種別.Tom1 ] ? 1 : 0;
            this._UserConfig.AutoPlay_LowTom = this.AutoPlay[ 演奏.AutoPlay種別.Tom2 ] ? 1 : 0;
            this._UserConfig.AutoPlay_FloorTom = this.AutoPlay[ 演奏.AutoPlay種別.Tom3 ] ? 1 : 0;
            this._UserConfig.AutoPlay_RightCymbal = this.AutoPlay[ 演奏.AutoPlay種別.RightCrash ] ? 1 : 0;

            this._UserConfig.MaxRange_Perfect = this.最大ヒット距離sec[ 演奏.判定種別.PERFECT ];
            this._UserConfig.MaxRange_Great = this.最大ヒット距離sec[ 演奏.判定種別.GREAT ];
            this._UserConfig.MaxRange_Good = this.最大ヒット距離sec[ 演奏.判定種別.GOOD ];
            this._UserConfig.MaxRange_Ok = this.最大ヒット距離sec[ 演奏.判定種別.OK ];

            this._UserConfig.CymbalFree = ( this.ドラムチッププロパティリスト.入力グループプリセット種別 == 演奏.入力グループプリセット種別.シンバルフリー ) ? 1 : 0;
        }
    }
}
