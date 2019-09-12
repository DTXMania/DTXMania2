using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using SharpDX;
using FDK;

namespace DTXMania
{
    // 過去のバージョンを指定する
    using システム設定02 = 設定.システム設定.old.システム設定02;
    using システム設定03 = システム設定;  // 最新版

    /// <summary>
    ///		全ユーザで共有される項目。
    /// </summary>
    /// <remarks>
    ///		ユーザ別の項目は<see cref="ユーザ設定"/>で管理すること。
    /// </remarks>
    class システム設定
    {

        // プロパティ


        /// <summary>
        ///     このクラスのバージョン。
        /// </summary>
        [YamlMember]
        public int Version { get; set; } = 3;   // 最新バージョンを指定する

        /// <summary>
        ///		曲ファイルを検索するフォルダのリスト。
        /// </summary>
        [YamlMember( Alias = "SongSearchFolders" )]
        public List<VariablePath> 曲検索フォルダ { get; protected set; } = null;

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


        // プロパティ・キーバインディング


        /// <summary>
        ///		入力コードのマッピング用 Dictionary のキーとなる型。
        /// </summary>
        /// <remarks>
        ///		入力は、デバイスID（入力デバイスの内部識別用ID; FDKのIInputEvent.DeviceIDと同じ）と、
        ///		キー（キーコード、ノート番号などデバイスから得られる入力値）の組で定義される。
        /// </remarks>
        public struct IdKey : IYamlConvertible
        {
            public int deviceId { get; set; }
            public int key { get; set; }

            public IdKey( int deviceId, int key )
            {
                this.deviceId = deviceId;
                this.key = key;
            }
            public IdKey( InputEvent ie )
            {
                this.deviceId = ie.DeviceID;
                this.key = ie.Key;
            }
            public IdKey( string 文字列 )
            {
                // 変なの食わせたらそのまま例外発出する。
                string[] v = 文字列.Split( new char[] { ',' } );

                this.deviceId = int.Parse( v[ 0 ] );
                this.key = int.Parse( v[ 1 ] );
            }
            public override string ToString()
                => $"{this.deviceId},{this.key}";

            void IYamlConvertible.Read( IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer )
            {
                var devkey = (string) nestedObjectDeserializer( typeof( string ) );

                string 正規表現パターン = $@"^(\d+),(\d+)$";  // \d は10進数数字
                var m = Regex.Match( devkey, 正規表現パターン, RegexOptions.IgnoreCase );

                if( m.Success && ( 3 <= m.Groups.Count ) )
                {
                    this.deviceId = int.Parse( m.Groups[ 1 ].Value );
                    this.key = int.Parse( m.Groups[ 2 ].Value );
                }
            }
            void IYamlConvertible.Write( IEmitter emitter, ObjectSerializer nestedObjectSerializer )
            {
                nestedObjectSerializer( $"{this.deviceId},{this.key}" );
            }
        }

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



        // static


        public static readonly VariablePath システム設定ファイルパス = @"$(AppData)Configuration.yaml";

        public static システム設定 読み込む()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                // (1) 読み込み or 新規作成

                var config = (システム設定) null;

                try
                {
                    if( File.Exists( システム設定ファイルパス.変数なしパス ) )
                    {
                        string yamlText;

                        #region " システム設定ファイル（YAML）を文字列として一括で読み込む。"
                        //----------------
                        yamlText = File.ReadAllText( システム設定ファイルパス.変数なしパス );
                        //----------------
                        #endregion

                        int version = 0;

                        #region " ルートノード 'Version' を検索し、バージョン値を取得する。"
                        //----------------
                        {
                            var yamlStream = new YamlStream();
                            yamlStream.Load( new StringReader( yamlText ) );
                            var rootMapping = (YamlMappingNode) yamlStream.Documents[ 0 ].RootNode;
                            var versionNode = new YamlScalarNode( "Version" );
                            if( rootMapping.Children.ContainsKey( versionNode ) )
                            {
                                var versionValue = rootMapping.Children[ versionNode ] as YamlScalarNode;
                                version = int.Parse( versionValue.Value );  // 取得
                            }
                        }
                        //----------------
                        #endregion

                        object curConfig = null;

                        #region " 対応するバージョンのクラスでデシリアライズする。"
                        //----------------
                        var deserializer = new Deserializer();
                        switch( version )
                        {
                            case 1:
                            case 2:
                                curConfig = deserializer.Deserialize<システム設定02>( yamlText );
                                break;

                            case 3:
                                curConfig = deserializer.Deserialize<システム設定03>( yamlText );
                                break;

                            default:
                                Log.ERROR( $"未対応のバージョンです。新規に作成して保存します。[{システム設定ファイルパス.変数付きパス}]" );
                                curConfig = new システム設定();
                                break;
                        }
                        //----------------
                        #endregion

                        #region " 最新バージョンまでマイグレーションする。"
                        //----------------
                        while( !( curConfig is システム設定 ) )
                            curConfig = _バージョンを１つ上にマイグレーションする( curConfig );
                        //----------------
                        #endregion

                        config = curConfig as システム設定;
                    }
                    else
                    {
                        Log.Info( $"ファイルが存在しないため、新規に作成します。[{システム設定ファイルパス.変数付きパス}]" );
                        config = new システム設定();
                        config.保存する();
                    }
                }
                catch( YamlException e )
                {
                    Log.ERROR( $"{e.Message}" );
                    if( null != e.InnerException )
                        Log.ERROR( $"{e.InnerException.Message}" );
                    Log.ERROR( $"ファイルの内容に誤りがあります。新規に作成して保存します。[{システム設定ファイルパス.変数付きパス}]" );
                    config = new システム設定();
                    config.保存する();
                }


                // (2) 読み込み後の処理

                // パスの指定がなければ、とりあえず exe のあるフォルダを検索対象にする。
                if( 0 == config.曲検索フォルダ.Count )
                    config.曲検索フォルダ.Add( @"$(Exe)" );

                return config;
            }
        }

        private static object _バージョンを１つ上にマイグレーションする( object curConfig )
        {
            object nextConfig = null;

            switch( curConfig )
            {
                case システム設定02 sc02:
                    #region " 2 → 3 "
                    //----------------
                    // 変更点:
                    // ・「全画面モードである」プロパティを追加。
                    // ・キーバインディングクラスを入力管理クラスに統合。 
                    // ・メンバ名を日本語から英語に変更（YamlMember の Alias で指定）
                    {
                        var sc03 = new システム設定03() {
                            曲検索フォルダ = sc02.曲検索フォルダ,
                            ビュアーモード時のウィンドウ表示位置 = sc02.ウィンドウ表示位置Viewerモード用.ToSharpDXPoint(),
                            ビュアーモード時のウィンドウサイズ = sc02.ウィンドウサイズViewerモード用.ToSharpDXSize2(),
                            判定位置調整ms = sc02.判定位置調整ms,
                            MIDIデバイス番号toデバイス名 = sc02.キー割り当て.MIDIデバイス番号toデバイス名,
                            //キーボードtoドラム = sc02.キー割り当て.キーボードtoドラム,  --> 型が変わっているので変換する
                            //MIDItoドラム = sc02.キー割り当て.MIDItoドラム,              --> 型が変わっているので変換する
                            FootPedal最小値 = sc02.キー割り当て.FootPedal最小値,
                            FootPedal最大値 = sc02.キー割り当て.FootPedal最大値,
                            全画面モードである = false,  // 移行時点ではウィンドウモード固定
                        };

                        sc03.キーボードtoドラム = new Dictionary<IdKey, ドラム入力種別>();
                        foreach( var kvp in sc02.キー割り当て.キーボードtoドラム )
                        {
                            var idKey = new IdKey( kvp.Key.deviceId, kvp.Key.key );
                            sc03.キーボードtoドラム.Add( idKey, kvp.Value );
                        }
                        sc03.MIDItoドラム = new Dictionary<IdKey, ドラム入力種別>();
                        foreach( var kvp in sc02.キー割り当て.MIDItoドラム )
                        {
                            var idKey = new IdKey( kvp.Key.deviceId, kvp.Key.key );
                            sc03.MIDItoドラム.Add( idKey, kvp.Value );
                        }

                        nextConfig = sc03;
                    }
                    //----------------
                    #endregion
                    break;

                case システム設定03 sc03:
                    break;  // 最新バージョン

                default:
                    throw new Exception( $"未対応のバージョンのシステム設定です。" );
            }

            return nextConfig;
        }


        // 生成と終了


        public システム設定()
        {
            this.曲検索フォルダ = new List<VariablePath>() { @"$(Exe)" };
            this.ビュアーモード時のウィンドウ表示位置 = new Point( 100, 100 );
            this.ビュアーモード時のウィンドウサイズ = new Size2( 640, 360 );
            this.判定位置調整ms = 0;

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

        public void 保存する()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                var serializer = new SerializerBuilder()
                    .WithTypeInspector( inner => new FDK.シリアライズ.YAML.CommentGatheringTypeInspector( inner ) )
                    .WithEmissionPhaseObjectGraphVisitor( args => new FDK.シリアライズ.YAML.CommentsObjectGraphVisitor( args.InnerVisitor ) )
                    .EmitDefaults()
                    .Build();

                // ※ 値が既定値であるプロパティは出力されないので注意。
                var yaml = serializer.Serialize( this );
                File.WriteAllText( システム設定ファイルパス.変数なしパス, yaml );

                Log.Info( $"システム設定 を保存しました。[{システム設定ファイルパス.変数付きパス}]" );
            }
        }

        public システム設定 Clone()
        {
            return (システム設定) this.MemberwiseClone();
        }
    }
}
