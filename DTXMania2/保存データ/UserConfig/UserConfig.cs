using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Data.Sqlite;
using YamlDotNet.Serialization;
using FDK;

namespace DTXMania2
{
    partial class UserConfig
    {

        // プロパティ


        public const int VERSION = 15;    // このクラスのバージョン

        /// <summary>
        ///     このクラスのバージョン。
        /// </summary>
        [YamlMember]
        public int Version { get; set; }

        /// <summary>
        ///		ユーザを一意に識別する文字列。主キーなので変更不可。
        /// </summary>
        [YamlMember]
        public string Id { get; set; }

        /// <summary>
        ///		ユーザ名。変更可。
        /// </summary>
        [YamlMember]
        public string Name { get; set; }

        /// <summary>
        ///		譜面スクロール速度の倍率。1.0で等倍。
        /// </summary>
        [YamlMember]
        public double ScrollSpeed { get; set; }

        /// <summary>
        ///		起動直後の表示モード。
        ///		0: ウィンドウモード、その他: 全画面モード。
        /// </summary>
        [YamlMember]
        public int Fullscreen { get; set; }

        /// <summary>
        ///		シンバルフリーモード。
        ///		0: OFF, その他: ON
        /// </summary>
        [YamlMember]
        public int CymbalFree { get; set; }

        /// <summary>
        ///		Ride の表示位置。
        ///		0: 右, 1: 左
        /// </summary>
        [YamlMember]
        public int RideLeft { get; set; }

        /// <summary>
        ///		China の表示位置。
        ///		0: 右, 1: 左
        /// </summary>
        [YamlMember]
        public int ChinaLeft { get; set; }

        /// <summary>
        ///		Splash の表示位置。
        ///		0: 右, 1: 左
        /// </summary>
        [YamlMember]
        public int SplashLeft { get; set; }

        /// <summary>
        ///		ユーザ入力時にドラム音を発声するか？
        ///		0: OFF, その他: ON
        /// </summary>
        [YamlMember]
        public int DrumSound { get; set; }

        /// <summary>
        ///     レーンの透過度[%]。
        ///     0:完全不透明 ～ 100:完全透明
        /// </summary>
        [YamlMember]
        public int LaneTrans { get; set; }

        /// <summary>
        ///		演奏時に再生される背景動画を表示するか？
        ///		0: OFF, その他: ON
        /// </summary>
        [YamlMember]
        public int BackgroundMovie { get; set; }

        /// <summary>
        ///     演奏速度。
        ///     1.0 で通常速度。
        /// </summary>
        [YamlMember]
        public double PlaySpeed { get; set; }

        /// <summary>
        ///		小節線・拍線の表示
        ///		0: OFF, その他: ON
        /// </summary>
        [YamlMember]
        public int ShowPartLine { get; set; }

        /// <summary>
        ///		小節番号の表示
        ///		0: OFF, その他: ON
        /// </summary>
        [YamlMember]
        public int ShowPartNumber { get; set; }

        /// <summary>
        ///		スコア指定の背景画像の表示
        ///		0: OFF, その他: ON
        /// </summary>
        [YamlMember]
        public int ShowScoreWall { get; set; }

        /// <summary>
        ///		演奏中の背景動画の表示サイズ
        ///		0: 全画面, 1: 中央寄せ
        /// </summary>
        [YamlMember]
        public int BackgroundMovieSize { get; set; }

        /// <summary>
        ///		判定FAST/SLOWの表示
        ///		0: OFF, その他: ON
        /// </summary>
        [YamlMember]
        public int ShowFastSlow { get; set; }

        /// <summary>
        ///     音量によるノーとサイズの変化
        ///     0: OFF, その他: ON
        /// </summary>
        [YamlMember]
        public int NoteSizeByVolume { get; set; }

        /// <summary>
        ///     ダークモード。
        ///     0: OFF, 1:HALF, 2:FULL
        /// </summary>
        [YamlMember]
        public int Dark { get; set; }

        // AutoPlay

        /// <summary>
        ///		左シンバルレーンの AutoPlay 。
        ///		0: OFF, その他: ON。
        /// </summary>
        [YamlMember]
        public int AutoPlay_LeftCymbal { get; set; }

        /// <summary>
        ///		ハイハットレーンの AutoPlay 。
        ///		0: OFF, その他: ON。
        /// </summary>
        [YamlMember]
        public int AutoPlay_HiHat { get; set; }

        /// <summary>
        ///		左ペダルレーンの AutoPlay 。
        ///		0: OFF, その他: ON。
        /// </summary>
        [YamlMember]
        public int AutoPlay_LeftPedal { get; set; }

        /// <summary>
        ///		スネアレーンの AutoPlay 。
        ///		0: OFF, その他: ON。
        /// </summary>
        [YamlMember]
        public int AutoPlay_Snare { get; set; }

        /// <summary>
        ///		バスレーンの AutoPlay 。
        ///		0: OFF, その他: ON。
        /// </summary>
        [YamlMember]
        public int AutoPlay_Bass { get; set; }

        /// <summary>
        ///		ハイタムレーンの AutoPlay 。
        ///		0: OFF, その他: ON。
        /// </summary>
        [YamlMember]
        public int AutoPlay_HighTom { get; set; }

        /// <summary>
        ///		ロータムレーンの AutoPlay 。
        ///		0: OFF, その他: ON。
        /// </summary>
        [YamlMember]
        public int AutoPlay_LowTom { get; set; }

        /// <summary>
        ///		フロアタムレーンの AutoPlay 。
        ///		0: OFF, その他: ON。
        /// </summary>
        [YamlMember]
        public int AutoPlay_FloorTom { get; set; }

        /// <summary>
        ///		右シンバルレーンの AutoPlay 。
        ///		0: OFF, その他: ON。
        /// </summary>
        [YamlMember]
        public int AutoPlay_RightCymbal { get; set; }

        // 最大ヒット距離

        /// <summary>
        ///		Perfect の最大ヒット距離[秒]。
        /// </summary>
        [YamlMember]
        public double MaxRange_Perfect { get; set; }

        /// <summary>
        ///		Great の最大ヒット距離[秒]。
        /// </summary>
        [YamlMember]
        public double MaxRange_Great { get; set; }

        /// <summary>
        ///		Good の最大ヒット距離[秒]。
        /// </summary>
        [YamlMember]
        public double MaxRange_Good { get; set; }

        /// <summary>
        ///		Ok の最大ヒット距離[秒]。
        /// </summary>
        [YamlMember]
        public double MaxRange_Ok { get; set; }



        // 生成と終了


        public UserConfig()
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

        public UserConfig( SqliteDataReader user )
            : this()
        {
            this.UpdateFrom( user );
        }

        public static UserConfig 読み込む( string userId )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            var path = new VariablePath( @$"$(AppData)\User_{userId}.yaml" );
            var yamlText = File.ReadAllText( path.変数なしパス );
            var deserializer = new Deserializer();
            var config = deserializer.Deserialize<UserConfig>( yamlText );

            if( VERSION != config.Version )
            {
                Log.Info( $"ユーザ設定ファイル[ID={userId}]を新規に作成します。" );
                config = new UserConfig() {
                    Id = userId,
                    Name = userId,
                };
            }

            return config;
        }

        /// <summary>
        ///     ファイルに保存する。
        /// </summary>
        public void 保存する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            var serializer = new SerializerBuilder()
                .WithTypeInspector( inner => new CommentGatheringTypeInspector( inner ) )
                .WithEmissionPhaseObjectGraphVisitor( args => new CommentsObjectGraphVisitor( args.InnerVisitor ) )
                .Build();

            // ※ 値が既定値であるプロパティは出力されないので注意。
            var path = new VariablePath( @$"$(AppData)\User_{this.Id}.yaml" );
            var yaml = serializer.Serialize( this );
            File.WriteAllText( path.変数なしパス, yaml );

            Log.Info( $"ユーザ設定を保存しました。[{path.変数付きパス}]" );
        }

        /// <summary>
        ///     SqliteDataReader からレコードを読み込んでフィールドを更新する。
        /// </summary>
        /// <param name="user">Read() 済みの SqliteDataReader。</param>
        public void UpdateFrom( SqliteDataReader user )
        {
            for( int i = 0; i < user.FieldCount; i++ )
            {
                switch( user.GetName( i ) )
                {
                    case "Id": this.Id = user.GetString( i ); break;
                    case "Name": this.Name = user.GetString( i ); break;
                    case "ScrollSpeed": this.ScrollSpeed = user.GetDouble( i ); break;
                    case "Fullscreen": this.Fullscreen = user.GetInt32( i ); break;
                    case "CymbalFree": this.CymbalFree = user.GetInt32( i ); break;
                    case "RideLeft": this.RideLeft = user.GetInt32( i ); break;      // v004
                    case "ChinaLeft": this.ChinaLeft = user.GetInt32( i ); break;    // v004
                    case "SplashLeft": this.SplashLeft = user.GetInt32( i ); break;  // v004
                    case "DrumSound": this.DrumSound = user.GetInt32( i ); break;    // v005
                    case "LaneTrans": this.LaneTrans = user.GetInt32( i ); break;    // v007 
                    case "BackgroundMovie": this.BackgroundMovie = user.GetInt32( i ); break;    // v008
                    case "PlaySpeed": this.PlaySpeed = user.GetDouble( i ); break;           // v009
                    case "ShowPartLine": this.ShowPartLine = user.GetInt32( i ); break;      // v009
                    case "ShowPartNumber": this.ShowPartNumber = user.GetInt32( i ); break;  // v009
                    case "ShowScoreWall": this.ShowScoreWall = user.GetInt32( i ); break;              // v010
                    case "BackgroundMovieSize": this.BackgroundMovieSize = user.GetInt32( i ); break;  // v010
                    case "ShowFastSlow": this.ShowFastSlow = user.GetInt32( i ); break;      // v011
                    case "NoteSizeByVolume": this.NoteSizeByVolume = user.GetInt32( i ); break;      // v013
                    case "Dark": this.Dark = user.GetInt32( i ); break;                              // v013
                    case "AutoPlay_LeftCymbal": this.AutoPlay_LeftCymbal = user.GetInt32( i ); break;
                    case "AutoPlay_HiHat": this.AutoPlay_HiHat = user.GetInt32( i ); break;
                    case "AutoPlay_LeftPedal": this.AutoPlay_LeftPedal = user.GetInt32( i ); break;
                    case "AutoPlay_Snare": this.AutoPlay_Snare = user.GetInt32( i ); break;
                    case "AutoPlay_Bass": this.AutoPlay_Bass = user.GetInt32( i ); break;
                    case "AutoPlay_HighTom": this.AutoPlay_HighTom = user.GetInt32( i ); break;
                    case "AutoPlay_LowTom": this.AutoPlay_LowTom = user.GetInt32( i ); break;
                    case "AutoPlay_FloorTom": this.AutoPlay_FloorTom = user.GetInt32( i ); break;
                    case "AutoPlay_RightCymbal": this.AutoPlay_RightCymbal = user.GetInt32( i ); break;
                    case "MaxRange_Perfect": this.MaxRange_Perfect = user.GetDouble( i ); break;
                    case "MaxRange_Great": this.MaxRange_Great = user.GetDouble( i ); break;
                    case "MaxRange_Good": this.MaxRange_Good = user.GetDouble( i ); break;
                    case "MaxRange_Ok": this.MaxRange_Ok = user.GetDouble( i ); break;
                }
            }
        }
    }
}
