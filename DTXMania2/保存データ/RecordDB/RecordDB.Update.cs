using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Data.Sqlite;
using FDK;

namespace DTXMania2
{
    partial class RecordDB
    {
        public static void 最新版にバージョンアップする()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            // Records レコードは、UserDB(v12)からRecordDBに分離された。
            // ・UserDB v011 までは、UserDB の Records テーブルに格納されている。RecordDB.Records テーブルのレコードのバージョンは v001～v006（全部同一）。
            // ・UserDB v012 以降は、UserDB から独立して、RecordDB.Records テーブルに格納される。Recordsのレコードのバージョンは v007。

            if( File.Exists( RecordDBPath.変数なしパス ) )
            {
                #region " (A) RecordDB.sqlite3 が存在している → 最新版にバージョンアップ。"
                //----------------
                using var recorddb = new RecordDB();
                int version = (int) recorddb.UserVersion;
                while( version < RecordDBRecord.VERSION )
                {
                    // RecordDB は v007 から。
                    switch( version )
                    {
                        case 0: // 念のため（あったら無限ループになるため）
                        {
                            #region " 0 → 最新版 "
                            //----------------
                            // テーブルを新規に作る。
                            foreach( var query in new[] {
                                    "PRAGMA foreign_keys = OFF",
                                    RecordDBRecord.GetCreateTableSQL(),
                                    "PRAGMA foreign_keys = ON" } )
                            {
                                using var cmd = new SqliteCommand( query, recorddb.Connection );
                                cmd.ExecuteNonQuery();
                            }
                            version = RecordDBRecord.VERSION;
                            recorddb.UserVersion = version;
                            Log.Info( $"RecordDB をバージョン {version} を作成しました。" );
                            break;
                            //----------------
                            #endregion
                        }
                        case 7:
                        {
                            #region " 7 → 8 "
                            //----------------
                            // テーブルを作り直す。REAL値が既にずれてるので、データ移行はしない。
                            foreach( var query in new[] {
                                "PRAGMA foreign_keys = OFF",
                                "DROP TABLE Records",
                                $"CREATE TABLE Records {old.RecordDBRecord.v008_RecordDBRecord.ColumnList}",
                                "PRAGMA foreign_keys = ON" } )
                            {
                                using var cmd = new SqliteCommand( query, recorddb.Connection );
                                cmd.ExecuteNonQuery();
                            }
                            version = old.RecordDBRecord.v008_RecordDBRecord.VERSION;
                            recorddb.UserVersion = version;
                            Log.Info( $"RecordDB をバージョン {version} に更新しました。データ移行はしません。" );
                            break;
                            //----------------
                            #endregion
                        }
                        case 8:
                        {
                            #region " 8 → 最新版 "
                            //----------------
                            // テーブルを作り直す。今までの成績は消失する。
                            foreach( var query in new[] {
                                "PRAGMA foreign_keys = OFF",
                                "DROP TABLE Records",
                                RecordDBRecord.GetCreateTableSQL(),
                                "PRAGMA foreign_keys = ON" } )
                            {
                                using var cmd = new SqliteCommand( query, recorddb.Connection );
                                cmd.ExecuteNonQuery();
                            }
                            version = RecordDBRecord.VERSION;
                            recorddb.UserVersion = version;
                            Log.Info( $"RecordDB をバージョン {version} に更新しました。データ移行はしません。" );
                            break;
                            //----------------
                            #endregion
                        }
                    }
                }
                //----------------
                #endregion
            }
            else
            {
                #region " (B) RecordDB.sqlite3 が存在しない → 何もしない "
                //----------------
                // UserDB 側の Update で RecordDB.sqlite3 が生成されるのを待つ。
                //----------------
                #endregion
            }
        }
    }
}
