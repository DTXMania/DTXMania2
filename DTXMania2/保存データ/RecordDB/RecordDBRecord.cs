using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Data.Sqlite;
using FDK;

namespace DTXMania2
{
    class RecordDBRecord
    {

        // プロパティ


        public const int VERSION = 9;

        /// <summary>
        ///		譜面ファイルの絶対パス。主キー。
        /// </summary>
        public string ScorePath { get; set; }

        /// <summary>
        ///		ユーザを一意に識別するID。主キー。
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        ///		スコア（得点）。
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        ///		カウントマップラインのデータ。
        ///		１ブロックを１文字（'0':0～'C':12）で表し、<see cref="DTXMania.ステージ.演奏.カウントマップライン.カウントマップの最大要素数"/> 個の文字が並ぶ。
        ///		もし不足分があれば、'0' とみなされる。
        /// </summary>
        public string CountMap { get; set; }

        /// <summary>
        ///		達成率。
        /// </summary>
        public double Achievement { get; set; }



        // 生成と終了


        public RecordDBRecord()
        {
            this.ScorePath = "";
            this.UserId = "Anonymous";
            this.Score = 0;
            this.CountMap = "";
            this.Achievement = 0.0;
        }

        public RecordDBRecord( SqliteDataReader reader )
            : this()
        {
            this.UpdateFrom( reader );
        }

        /// <summary>
        ///     <see cref="SqliteDataReader"/>から現在のレコードを読み込んでフィールドを更新する。
        /// </summary>
        /// <param name="record">Read() 済みの <see cref="SqliteDataReader"/>。</param>
        public void UpdateFrom( SqliteDataReader record )
        {
            for( int i = 0; i < record.FieldCount; i++ )
            {
                switch( record.GetName( i ) )
                {
                    case "ScorePath": this.ScorePath = record.GetString( i ); break;
                    case "UserId": this.UserId = record.GetString( i ); break;
                    case "Score": this.Score = record.GetInt32( i ); break;
                    case "CountMap": this.CountMap = record.GetString( i ); break;
                    case "Achievement": this.Achievement = record.GetDouble( i ); break;
                }
            }
        }

        /// <summary>
        ///     DBにレコードを挿入または更新する。
        /// </summary>
        public virtual void InsertTo( SQLiteDB db, string table = "Records" )
        {
            using var cmd = new SqliteCommand(
                $"REPLACE INTO {table} VALUES" +
                "( @SongPath" +
                ", @UserId" +
                ", @Score" +
                ", @CountMap" +
                ", @Achievement" +
                ")", db.Connection );

            cmd.Parameters.AddRange( new[] {
                new SqliteParameter( "@SongPath", this.ScorePath ),
                new SqliteParameter( "@UserId", this.UserId ),
                new SqliteParameter( "@Score", this.Score ),
                new SqliteParameter( "@CountMap", this.CountMap ),
                new SqliteParameter( "@Achievement", this.Achievement ),
            } );

            cmd.ExecuteNonQuery();
        }

        /// <summary>
        ///     テーブルがなければ作成するSQLを返す。
        /// </summary>
        public static string GetCreateTableSQL( string table = "Records" ) =>
            $"CREATE TABLE IF NOT EXISTS {table}" +
            "( ScorePath NVARCHAR NOT NULL" +
            ", UserId NVARCHAR NOT NULL" +
            ", Score INTEGER NOT NULL" +
            ", CountMap NVARCHAR NOT NULL" +
            ", Achievement NUMERIC NOT NULL" +
            ", PRIMARY KEY(`ScorePath`,`UserId`)" +
            ")";
    }
}
