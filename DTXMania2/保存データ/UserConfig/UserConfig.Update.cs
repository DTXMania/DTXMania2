using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using FDK;

namespace DTXMania2
{
    partial class UserConfig
    {
        /// <summary>
        ///     ユーザDBファイルを最新版に更新する。
        /// </summary>
        /// <remarks>
        ///     v012 移行にバージョンアップする場合は、RecordDB.sqlite3 の生成も行う。
        /// </remarks>
        public static void 最新版にバージョンアップする()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            var userdbPath = new VariablePath( @"$(AppData)\UserDB.sqlite3" );
            var userYamls = Directory.EnumerateFiles(
                path: Folder.フォルダ変数の内容を返す( "AppData" ),
                searchPattern: @"User_*.yaml", 
                searchOption: SearchOption.TopDirectoryOnly );

            if( 0 < userYamls.Count() )
            {
                #region " (A) User_*.yaml が存在している "
                //----------------
                // それらを最新版にバージョンアップする。
                foreach( var userYaml in userYamls )
                    _Yamlを最新版にバージョンアップする( userYaml );
                //----------------
                #endregion
            }
            else if( File.Exists( userdbPath.変数なしパス ) )
            {
                #region " (B) User_*.yaml が存在せず UserDB.sqlite3 が存在している "
                //----------------
                // Records レコードは、UserDB(v12)からRecordDBに分離された。
                // ・UserDB v011 までは、UserDB の Records テーブルに格納されている。RecordDB.Records テーブルのレコードのバージョンは v001～v006（全部同一）。
                // ・UserDB v012 以降は、UserDB から独立して、RecordDB.Records テーブルに格納される。Recordsのレコードのバージョンは v007。

                var Users = new List<UserConfig>();
                int userdb_version;

                #region " Users テーブルを最新版にして読み込む。"
                //----------------
                using( var userdb = new SQLiteDB( userdbPath.変数なしパス ) )
                {
                    userdb_version = (int) userdb.UserVersion;
                    using var idsQuery = new SqliteCommand( "SELECT * FROM Users", userdb.Connection );
                    var ids = idsQuery.ExecuteReader();
                    while( ids.Read() )
                        Users.Add( new UserConfig( ids ) );
                }
                //----------------
                #endregion

                #region " Users テーブルを User_*.yaml に出力する。"
                //----------------
                foreach( var user in Users )
                    user.保存する();
                //----------------
                #endregion

                #region " RecordDB.sqlite3 がなければ新規作成する。"
                //----------------
                if( !File.Exists( RecordDB.RecordDBPath.変数なしパス ) )
                {
                    using var recorddb = new RecordDB();   // ファイルがなければ新規作成される
                    using var cmd = new SqliteCommand( $"CREATE TABLE Records { old.RecordDBRecord.v007_RecordDBRecord.ColumnList}", recorddb.Connection );
                    cmd.ExecuteNonQuery();
                    recorddb.UserVersion = old.RecordDBRecord.v007_RecordDBRecord.VERSION;
                    Log.Info( $"RecordDB(v007) を生成しました。" );
                }
                //----------------
                #endregion

                if( 12 > userdb_version )
                {
                    #region " UserDB.Rcords テーブルを読み込み、RecordDB.Records テーブルへ出力する。"
                    //----------------
                    using var userdb = new SQLiteDB( userdbPath.変数なしパス );
                    using var recorddb = new RecordDB();
                    foreach( var user in Users )
                    {
                        using var recordsQuery = new SqliteCommand( $"SELECT * FROM Records WHERE Id = @UserId", userdb.Connection );
                        recordsQuery.Parameters.Add( new SqliteParameter( "@UserId", user.Id ) );
                        var records = recordsQuery.ExecuteReader();
                        while( records.Read() )
                        {
                            var record = new old.RecordDBRecord.v007_RecordDBRecord( records );    // 読み込んで
                            record.InsertTo( recorddb );                                           // 書き込む
                        }
                    }
                    //----------------
                    #endregion
                }
                //----------------
                #endregion
            }
            else
            {
                #region " (C) User_*.yamlも UserDB.sqlite3 も存在しない "
                //----------------

                #region " 新規に User_*.yaml を生成する。"
                //----------------
                var autoPlayer = new UserConfig() {
                    Id = "AutoPlayer",
                    Name = "AutoPlayer",
                    AutoPlay_LeftCymbal = 1,
                    AutoPlay_HiHat = 1,
                    AutoPlay_LeftPedal = 1,
                    AutoPlay_Snare = 1,
                    AutoPlay_Bass = 1,
                    AutoPlay_HighTom = 1,
                    AutoPlay_LowTom = 1,
                    AutoPlay_FloorTom = 1,
                    AutoPlay_RightCymbal = 1,
                    // 他は既定値
                };
                autoPlayer.保存する();

                var guest = new UserConfig() {
                    Id = "Guest",
                    Name = "Guest",
                    // 他は既定値
                };
                guest.保存する();
                //----------------
                #endregion

                #region " 新規に RecordDB.sqlite3 を生成する。"
                //----------------
                var recorddbPath = new VariablePath( @"$(AppData)\RecordDB.sqlite3" );

                // 念のため
                if( File.Exists( recorddbPath.変数なしパス ) )
                    File.Delete( recorddbPath.変数なしパス );

                using var recorddb = new SQLiteDB( recorddbPath.変数なしパス ); // ファイルがなければ新規生成される。
                using var cmd = new SqliteCommand( RecordDBRecord.GetCreateTableSQL(), recorddb.Connection );
                cmd.ExecuteNonQuery();
                recorddb.UserVersion = RecordDBRecord.VERSION;
                
                Log.Info( $"RecordDB を生成しました。" );
                //----------------
                #endregion

                //----------------
                #endregion
            }
        }


        // ローカル


        /// <summary>
        ///     YAMLファイルを最新版にバージョンアップする。
        /// </summary>
        private static void _Yamlを最新版にバージョンアップする( VariablePath path )
        {
            int version = 0;

            #region " YAML階層のルートノード 'Version' を検索し、バージョン値を取得する。"
            //----------------
            {
                var yamlText = File.ReadAllText( path.変数なしパス );
                var yamlStream = new YamlStream();
                yamlStream.Load( new StringReader( yamlText ) );
                var rootMapping = (YamlMappingNode) yamlStream.Documents[ 0 ].RootNode;
                var versionNode = new YamlScalarNode( "Version" );
                if( rootMapping.Children.ContainsKey( versionNode ) )
                {
                    var versionValue = rootMapping.Children[ versionNode ] as YamlScalarNode;
                    version = int.Parse( versionValue?.Value ?? "1" );  // 取得
                }
            }
            //----------------
            #endregion

            while( version < UserConfig.VERSION )
            {
                switch( version )
                {
                    case 14:
                    {
                        #region " 14 → 最新版 "
                        //----------------
                        // 変更がないので、全メンバを代入でコピーするよりも、v14をシリアライズ → v15でデシリアライズ するほうを選ぶ。
                        var v14yamlText = File.ReadAllText( path.変数なしパス );
                        var v14deserializer = new Deserializer();
                        var v14config = v14deserializer.Deserialize<old.UserConfig.v014_UserConfig>( v14yamlText );
                        var v14serializer = new SerializerBuilder()
                            .WithTypeInspector( inner => new CommentGatheringTypeInspector( inner ) )
                            .WithEmissionPhaseObjectGraphVisitor( args => new CommentsObjectGraphVisitor( args.InnerVisitor ) )
                            .Build();
                        var v14yaml = v14serializer.Serialize( v14config );

                        var v15deserializer = new Deserializer();
                        var v15config = v15deserializer.Deserialize<UserConfig>( v14yamlText ); // 変更なし
                        v15config.Version = UserConfig.VERSION;
                        v15config.保存する();

                        version = v15config.Version;
                        break;
                        //----------------
                        #endregion
                    }
                }
            }
        }
    }
}
