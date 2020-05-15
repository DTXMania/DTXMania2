using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Data.Sqlite;
using FDK;

namespace DTXMania2
{
    partial class ScoreDB
    {
        /// <summary>
        ///     DBを最新版にアップデートする。
        /// </summary>
        public static void Update()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            using var db = new ScoreDB();
            int version = (int) db.UserVersion;

            // v007 から、SognDB.sqlite3 が ScoreDB.sqlire3 に改名された。
            var songdbPath = new VariablePath( @"$(AppData)\SongDB.sqlite3" );
            using var songdb = File.Exists( songdbPath.変数なしパス ) ? new SQLiteDB( songdbPath.変数なしパス ) : null; // なければ null
            int songdb_version = (int) ( songdb?.UserVersion ?? -1 );    // なければ負数

            while( version < ScoreDBRecord.VERSION )
            {
                switch( version )
                {
                    case 0:
                        #region " 0 → 最新版 "
                        //----------------
                        // ScoreDB.sqlite3 に最新版のテーブルを新規に作成する。
                        foreach( var query in new[] {
                                "PRAGMA foreign_keys = OFF",
                                ScoreDBRecord.GetCreateTableSQL(),
                                "PRAGMA foreign_keys = ON",
                            } )
                        {
                            using var cmd = new SqliteCommand( query, db.Connection );
                            cmd.ExecuteNonQuery();
                        }

                        if( songdb is null )    // SongDB.sqlite3 がある？
                        {
                            version = ScoreDBRecord.VERSION;
                            db.UserVersion = version;
                            break;
                        }

                        //  SongDB.sqlite3 が存在するので、データ移行する。
                        switch( songdb_version )
                        {
                            case 0:
                            case 1:
                            case 2:
                            case 3:
                            case 4:
                            case 5:
                                // 何もしない。1～5 については、REAL 型の数値がすでにずれているため、データ移行しない。
                                break;

                            case 6:
                                #region " 6 → 最新版 "
                                //----------------
                                {
                                    // ScoreDB 側に、最新バージョンのテーブルを作成する。

                                    foreach( var query in new[] {
                                            "PRAGMA foreign_keys = OFF",
                                            ScoreDBRecord.GetCreateTableSQL(),
                                        } )
                                    {
                                        using var cmd = new SqliteCommand( query, db.Connection );
                                        cmd.ExecuteNonQuery();
                                    }

                                    // v006 のレコードを読み込み、v007 テーブルに出力する。DBが違うので注意。

                                    using( var cmdv007Begin = new SqliteCommand( "BEGIN", db.Connection ) )
                                        cmdv007Begin.ExecuteNonQuery();

                                    using var cmdv006query = new SqliteCommand( "SELECT * FROM Songs", songdb.Connection );
                                    var v006result = cmdv006query.ExecuteReader();
                                    while( v006result.Read() )
                                    {
                                        var v006rc = new old.SongDBRecord.v006_SongDBRecord( v006result );
                                        var v007rc = new ScoreDBRecord() {
                                            ScorePath = v006rc.Path,
                                            // 以下変更なし。
                                            Title = v006rc.Title,
                                            LastWriteTime = v006rc.LastWriteTime,
                                            Level = v006rc.Level,
                                            MinBPM = v006rc.MinBPM,
                                            MaxBPM = v006rc.MaxBPM,
                                            TotalNotes_LeftCymbal = v006rc.TotalNotes_LeftCymbal,
                                            TotalNotes_HiHat = v006rc.TotalNotes_HiHat,
                                            TotalNotes_LeftPedal = v006rc.TotalNotes_LeftPedal,
                                            TotalNotes_Snare = v006rc.TotalNotes_Snare,
                                            TotalNotes_Bass = v006rc.TotalNotes_Bass,
                                            TotalNotes_HighTom = v006rc.TotalNotes_HighTom,
                                            TotalNotes_LowTom = v006rc.TotalNotes_LowTom,
                                            TotalNotes_FloorTom = v006rc.TotalNotes_FloorTom,
                                            TotalNotes_RightCymbal = v006rc.TotalNotes_RightCymbal,
                                            PreImage = v006rc.PreImage,
                                            Artist = v006rc.Artist,
                                            PreSound = v006rc.PreSound,
                                            BGMAdjust = v006rc.BGMAdjust,
                                        };
                                        v007rc.ReplaceTo( db );
                                    }

                                    using( var cmdv007End = new SqliteCommand( "END", db.Connection ) )
                                        cmdv007End.ExecuteNonQuery();

                                    using( var cmdKeysOn = new SqliteCommand( "PRAGMA foreign_keys = ON", db.Connection ) )
                                        cmdKeysOn.ExecuteNonQuery();

                                    version = ScoreDBRecord.VERSION;
                                    db.UserVersion = version;
                                    Log.Info( $"ScoreDB をバージョン {version} に更新しました。" );
                                }
                                //----------------
                                #endregion
                                break;
                        }
                        //----------------
                        #endregion
                        break;

                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                        #region " 1～5 → 6（BreakingChange）"
                        //----------------
                        {
                            // テーブルを作り直す。REAL値が既にずれてるので、データ移行はしない。
                            foreach( var query in new[] {
                                "PRAGMA foreign_keys = OFF",
                                "DROP TABLE Songs",
                                $"CREATE TABLE Songs {old.SongDBRecord.v006_SongDBRecord.ColumnList}",
                                "PRAGMA foreign_keys = ON",
                            } )
                            {
                                using var cmd = new SqliteCommand( query, db.Connection );
                                cmd.ExecuteNonQuery();
                            }
                            version = old.SongDBRecord.v006_SongDBRecord.VERSION;
                            db.UserVersion = version;
                            Log.Info( $"SongDB をバージョン {version} に更新しました。" );
                        }
                        //----------------
                        #endregion
                        break;
                }
            }
        }
    }
}
