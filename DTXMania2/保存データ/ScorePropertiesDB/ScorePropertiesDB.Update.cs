using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Data.Sqlite;
using FDK;

namespace DTXMania2
{
    partial class ScorePropertiesDB
    {
        /// <summary>
        ///     DBを最新版にアップデートする。
        /// </summary>
		public static void Update()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            // DBのバージョンを取得。
            using var db = new ScorePropertiesDB();
            int version = (int) db.UserVersion;

            while( version < ScorePropertiesDBRecord.VERSION )
            {
                switch( version )
                {
                    case 0:
                    case 1:
                        #region " 0, 1 → 最新版 "
                        //----------------
                        {
                            // 最新バージョンのテーブルを作成する。内容は空。
                            using var cmdCreate = new SqliteCommand( ScorePropertiesDBRecord.GetCreateTableSQL(), db.Connection );
                            cmdCreate.ExecuteNonQuery();

                            version = ScorePropertiesDBRecord.VERSION;
                            db.UserVersion = version;
                            Log.Info( $"ScorePropertiesDB バージョン {version} を生成しました。" );
                        }
                        //----------------
                        #endregion
                        break;
                }
            }
        }
    }
}
