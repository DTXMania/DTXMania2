using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using FDK;

namespace DTXMania2.old.SystemConfig
{
    /// <summary>
    ///		システム設定。
    ///		全ユーザで共有される項目。
    /// </summary>
    class v002_システム設定
    {
        public const int VERSION = 2;

        public static readonly VariablePath 既定のシステム設定ファイルパス = @"$(AppData)\Configuration.yaml";

        public int Version { get; set; }

        public v002_キーバインディング キー割り当て { get; set; }

        /// <summary>
        ///		曲ファイルを検索するフォルダのリスト。
        /// </summary>
        public List<VariablePath> 曲検索フォルダ { get; protected set; }

        public Point ウィンドウ表示位置Viewerモード用 { get; set; }

        public Size ウィンドウサイズViewerモード用 { get; set; }

        /// <summary>
        ///     チップヒットの判定位置を、判定バーからさらに上下に調整する。
        ///     -99～+99[ms] 。
        /// </summary>
        /// <remarks>
        ///     判定バーの見かけの位置は変わらず、判定位置のみ移動する。
        ///     入力機器の遅延対策であるが、入力機器とのヒモづけは行わないので、
        ///     入力機器が変われば再調整が必要になる場合がある。
        /// </remarks>
        public int 判定位置調整ms { get; set; }



        // 生成と終了


        public static v002_システム設定 読み込む( VariablePath path )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );


            // (1) 読み込み or 新規作成

            var yaml = File.ReadAllText( path.変数なしパス );
            var deserializer = new YamlDotNet.Serialization.Deserializer();
            var config = deserializer.Deserialize<v002_システム設定>( yaml );

            if( 2 != config.Version )
                throw new Exception( "バージョンが違います。" );


            // (2) 読み込み後の処理

            // パスの指定がなければ、とりあえず exe のあるフォルダを検索対象にする。
            if( 0 == config.曲検索フォルダ.Count )
                config.曲検索フォルダ.Add( @"$(Exe)" );

            return config;
        }

        public v002_システム設定()
        {
            this.Version = VERSION;
            this.キー割り当て = new v002_キーバインディング();
            this.曲検索フォルダ = new List<VariablePath>() { @"$(Exe)" };
            this.ウィンドウ表示位置Viewerモード用 = new Point( 100, 100 );
            this.ウィンドウサイズViewerモード用 = new Size( 640, 360 );
            this.判定位置調整ms = 0;
        }

        public void 保存する( VariablePath path )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            var serializer = new YamlDotNet.Serialization.SerializerBuilder()
                .WithTypeInspector( inner => new CommentGatheringTypeInspector( inner ) )
                .WithEmissionPhaseObjectGraphVisitor( args => new CommentsObjectGraphVisitor( args.InnerVisitor ) )
                .Build();

            // ※ 値が既定値であるプロパティは出力されないので注意。
            var yaml = serializer.Serialize( this );
            File.WriteAllText( path.変数なしパス, yaml );

            Log.Info( $"システム設定を保存しました。[{path.変数付きパス}]" );
        }
    }
}
