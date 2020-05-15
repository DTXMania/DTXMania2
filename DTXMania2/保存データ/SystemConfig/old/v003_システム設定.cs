using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using YamlDotNet.Serialization;
using SharpDX;
using FDK;

namespace DTXMania2.old.SystemConfig
{
    using IdKey = DTXMania2.SystemConfig.IdKey;

    class v003_システム設定
    {
        /// <summary>
        ///     このクラスのバージョン。
        /// </summary>
        [YamlMember]
        public int Version { get; set; } = 3;

        /// <summary>
        ///		曲ファイルを検索するフォルダのリスト。
        /// </summary>
        [YamlMember( Alias = "SongSearchFolders" )]
        public List<VariablePath> 曲検索フォルダ { get; protected set; }

        [YamlMember( Alias = "WindowPositionOnViewerMode" )]
        public Point ビュアーモード時のウィンドウ表示位置 { get; set; }

        [YamlMember( Alias = "WindowSizeOnViewerMode" )]
        public Size2 ビュアーモード時のウィンドウサイズ { get; set; }

        /// <summary>
        ///     チップヒットの判定位置を、判定バーからさらに上（負数）または下（正数）に調整する。
        ///     -99～+99[ms] 。
        /// </summary>
        /// <remarks>
        ///     判定バーの見かけの位置は変わらず、判定位置のみ移動する。
        ///     入力機器の遅延対策であるが、入力機器とのヒモづけは行わないので、
        ///     入力機器が変われば再調整が必要になる場合がある。
        /// </remarks>
        [YamlMember( Alias = "JudgePositionAdjustmentOnMilliseconds" )]
        public int 判定位置調整ms { get; set; }

        [YamlMember( Alias = "Fullscreen" )]
        public bool 全画面モードである { get; set; }

        [YamlIgnore]
        public bool ウィンドウモードである
        {
            get => !this.全画面モードである;
            set => this.全画面モードである = !value;
        }

        [YamlIgnore]
        public static readonly VariablePath 既定のシステム設定ファイルパス = @"$(AppData)Configuration.yaml";



        // キーバインディング


        /// <summary>
        ///		MIDI番号(0～7)とMIDIデバイス名のマッピング用 Dictionary。
        /// </summary>
        [YamlMember( Alias = "MidiDeviceIDtoDeviceName" )]
        public Dictionary<int, string> MIDIデバイス番号toデバイス名 { get; protected set; }

        /// <summary>
        ///		キーボードの入力（<see cref="System.Windows.Forms.Keys"/>）からドラム入力へのマッピング用 Dictionary 。
        /// </summary>
        [YamlMember( Alias = "KeyboardToDrums" )]
        public Dictionary<IdKey, ドラム入力種別> キーボードtoドラム { get; protected set; }

        /// <summary>
        ///		ゲームコントローラの入力（Extended Usage）からドラム入力へのマッピング用 Dictionary 。
        /// </summary>
        [YamlMember( Alias = "GameControllerToDrums" )]
        public Dictionary<IdKey, ドラム入力種別> ゲームコントローラtoドラム { get; protected set; }

        /// <summary>
        ///		MIDI入力の入力（MIDIノート番号）からドラム入力へのマッピング用 Dictionary 。
        /// </summary>
        [YamlMember( Alias = "MidiInToDrums" )]
        public Dictionary<IdKey, ドラム入力種別> MIDItoドラム { get; protected set; }

        [YamlMember( Alias = "MinFoolPedalValue" )]
        public int FootPedal最小値 { get; set; }

        [YamlMember( Alias = "MaxFoolPedalValue" )]
        public int FootPedal最大値 { get; set; }



        // 生成と終了


        public static v003_システム設定 読み込む( VariablePath path )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );


            // (1) 読み込み or 新規作成

            var yamlText = File.ReadAllText( path.変数なしパス );
            var deserializer = new Deserializer();
            var config = deserializer.Deserialize<v003_システム設定>( yamlText );

            if( 3 != config.Version )
                throw new Exception( "バージョンが違います。" );

            // (2) 読み込み後の処理

            // パスの指定がなければ、とりあえず exe のあるフォルダを検索対象にする。
            if( 0 == config.曲検索フォルダ.Count )
                config.曲検索フォルダ.Add( @"$(Exe)" );

            return config;
        }

        public v003_システム設定()
        {
            this.曲検索フォルダ = new List<VariablePath>() { @"$(Exe)" };
            this.ビュアーモード時のウィンドウ表示位置 = new Point( 100, 100 );
            this.ビュアーモード時のウィンドウサイズ = new Size2( 640, 360 );
            this.判定位置調整ms = 0;
            this.全画面モードである = false;

            this.FootPedal最小値 = 0;
            this.FootPedal最大値 = 90; // VH-11 の Normal Resolution での最大値

            this.MIDIデバイス番号toデバイス名 = new Dictionary<int, string>();

            this.キーボードtoドラム = new Dictionary<IdKey, ドラム入力種別>() {
                { new IdKey( 0, (int) Keys.Q ),      ドラム入力種別.LeftCrash },
                { new IdKey( 0, (int) Keys.Return ), ドラム入力種別.LeftCrash },
                { new IdKey( 0, (int) Keys.A ),      ドラム入力種別.HiHat_Open },
                { new IdKey( 0, (int) Keys.Z ),      ドラム入力種別.HiHat_Close },
                { new IdKey( 0, (int) Keys.S ),      ドラム入力種別.HiHat_Foot },
                { new IdKey( 0, (int) Keys.X ),      ドラム入力種別.Snare },
                { new IdKey( 0, (int) Keys.C ),      ドラム入力種別.Bass },
                { new IdKey( 0, (int) Keys.Space ),  ドラム入力種別.Bass },
                { new IdKey( 0, (int) Keys.V ),      ドラム入力種別.Tom1 },
                { new IdKey( 0, (int) Keys.B ),      ドラム入力種別.Tom2 },
                { new IdKey( 0, (int) Keys.N ),      ドラム入力種別.Tom3 },
                { new IdKey( 0, (int) Keys.M ),      ドラム入力種別.RightCrash },
                { new IdKey( 0, (int) Keys.K ),      ドラム入力種別.Ride },
            };

            this.ゲームコントローラtoドラム = new Dictionary<IdKey, ドラム入力種別>() {
                // 特になし
            };

            this.MIDItoドラム = new Dictionary<IdKey, ドラム入力種別>() {
				// うちの環境(2017.6.11)
				{ new IdKey( 0,  36 ), ドラム入力種別.Bass },
                { new IdKey( 0,  30 ), ドラム入力種別.RightCrash },
                { new IdKey( 0,  29 ), ドラム入力種別.RightCrash },
                { new IdKey( 1,  51 ), ドラム入力種別.RightCrash },
                { new IdKey( 1,  52 ), ドラム入力種別.RightCrash },
                { new IdKey( 1,  57 ), ドラム入力種別.RightCrash },
                { new IdKey( 0,  52 ), ドラム入力種別.RightCrash },
                { new IdKey( 0,  43 ), ドラム入力種別.Tom3 },
                { new IdKey( 0,  58 ), ドラム入力種別.Tom3 },
                { new IdKey( 0,  42 ), ドラム入力種別.HiHat_Close },
                { new IdKey( 0,  22 ), ドラム入力種別.HiHat_Close },
                { new IdKey( 0,  26 ), ドラム入力種別.HiHat_Open },
                { new IdKey( 0,  46 ), ドラム入力種別.HiHat_Open },
                { new IdKey( 0,  44 ), ドラム入力種別.HiHat_Foot },
                { new IdKey( 0, 255 ), ドラム入力種別.HiHat_Control },	// FDK の MidiIn クラスは、FootControl を ノート 255 として扱う。
				{ new IdKey( 0,  48 ), ドラム入力種別.Tom1 },
                { new IdKey( 0,  50 ), ドラム入力種別.Tom1 },
                { new IdKey( 0,  49 ), ドラム入力種別.LeftCrash },
                { new IdKey( 0,  55 ), ドラム入力種別.LeftCrash },
                { new IdKey( 1,  48 ), ドラム入力種別.LeftCrash },
                { new IdKey( 1,  49 ), ドラム入力種別.LeftCrash },
                { new IdKey( 1,  59 ), ドラム入力種別.LeftCrash },
                { new IdKey( 0,  45 ), ドラム入力種別.Tom2 },
                { new IdKey( 0,  47 ), ドラム入力種別.Tom2 },
                { new IdKey( 0,  51 ), ドラム入力種別.Ride },
                { new IdKey( 0,  59 ), ドラム入力種別.Ride },
                { new IdKey( 0,  38 ), ドラム入力種別.Snare },
                { new IdKey( 0,  40 ), ドラム入力種別.Snare },
                { new IdKey( 0,  37 ), ドラム入力種別.Snare },
            };
        }

        public v003_システム設定( v002_システム設定 v2config )
            : this()
        {
            this.曲検索フォルダ = v2config.曲検索フォルダ;
            this.ビュアーモード時のウィンドウサイズ = new Size2( v2config.ウィンドウサイズViewerモード用.Width, v2config.ウィンドウサイズViewerモード用.Height );
            this.ビュアーモード時のウィンドウ表示位置 = new Point( v2config.ウィンドウ表示位置Viewerモード用.X, v2config.ウィンドウ表示位置Viewerモード用.Y );
            this.判定位置調整ms = v2config.判定位置調整ms;

            this.FootPedal最小値 = v2config.キー割り当て.FootPedal最小値;
            this.FootPedal最大値 = v2config.キー割り当て.FootPedal最大値;

            this.MIDIデバイス番号toデバイス名 = v2config.キー割り当て.MIDIデバイス番号toデバイス名;
            this.キーボードtoドラム = v2config.キー割り当て.キーボードtoドラム;
            this.ゲームコントローラtoドラム = new Dictionary<IdKey, ドラム入力種別>();
            this.MIDItoドラム = v2config.キー割り当て.MIDItoドラム;
        }

        public void 保存する( VariablePath path )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            var serializer = new SerializerBuilder()
                .WithTypeInspector( inner => new CommentGatheringTypeInspector( inner ) )
                .WithEmissionPhaseObjectGraphVisitor( args => new CommentsObjectGraphVisitor( args.InnerVisitor ) )
                .Build();

            // ※ 値が既定値であるプロパティは出力されないので注意。
            var yaml = serializer.Serialize( this );
            File.WriteAllText( path.変数なしパス, yaml );

            Log.Info( $"システム設定 を保存しました。[{path.変数付きパス}]" );
        }

        public v003_システム設定 Clone()
        {
            return (v003_システム設定) this.MemberwiseClone();
        }
    }
}
