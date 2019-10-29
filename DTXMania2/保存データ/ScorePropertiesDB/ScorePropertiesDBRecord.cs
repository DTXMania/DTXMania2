using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Data.Sqlite;

namespace DTXMania2
{
    class ScorePropertiesDBRecord
    {

        // プロパティ


        public static int VERSION = 2;

        /// <summary>
        ///		譜面ファイルの絶対パス。主キー。
        /// </summary>
        public string ScorePath { get; set; }

        /// <summary>
        ///     ユーザID。主キー。
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        ///     評価値。0～5。
        /// </summary>
        public int Rating { get; set; }



        // 生成と終了


        public ScorePropertiesDBRecord()
        {
            this.ScorePath = "unknownscore.sstf";
            this.UserId = "Anonymous";
            this.Rating = 0;
        }

        public ScorePropertiesDBRecord( SqliteDataReader reader )
            : this()
        {
            this.UpdateFrom( reader );
        }

        /// <summary>
        ///     SqliteDataReader からレコードを読み込んでフィールドを更新する。
        /// </summary>
        /// <param name="record">Read() 済みの SqliteDataReader。</param>
        public void UpdateFrom( SqliteDataReader record )
        {
            for( int i = 0; i < record.FieldCount; i++ )
            {
                switch( record.GetName( i ) )
                {
                    case "ScorePath": this.ScorePath = record.GetString( i ); break;
                    case "UserId": this.UserId = record.GetString( i ); break;
                    case "Rating": this.Rating = record.GetInt32( i ); break;
                }
            }
        }

        /// <summary>
        ///     DBにレコードを挿入または更新する。
        /// </summary>
        public void InsertTo( SQLiteDB db, string table = "ScoreProperties" )
        {
            using var cmd = new SqliteCommand(
                $"REPLACE INTO {table} VALUES(" +
                "@ScorePath," +
                "@UserId," +
                "@Rating" +
                ")", db.Connection );

            cmd.Parameters.AddRange( new[] {
                new SqliteParameter( "@ScorePath", this.ScorePath ),
                new SqliteParameter( "@UserId", this.UserId ),
                new SqliteParameter( "@Rating", this.Rating ),
            } );

            cmd.ExecuteNonQuery();
        }

        /// <summary>
        ///     テーブルがなければ作成するSQLを返す。
        /// </summary>
        public static string GetCreateTableSQL( string table = "ScoreProperties" ) =>
            $"CREATE TABLE IF NOT EXISTS {table}" +
            "( ScorePath NVARCHAR NOT NULL" +
            ", UserId NVARCHAR NOT NULL" +
            ", Rating INTEGER NOT NULL" +
            ", PRIMARY KEY(`ScorePath`, `UserId`)" +
            ")";
    }
}
