using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Data.Sqlite;
using FDK;

namespace DTXMania2.old.ScorePropertiesDB
{
    class v001_SongPropertiesDBRecord
    {

        // プロパティ


        public static int VERSION = 1;

        /// <summary>
        ///     ユーザID。
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        ///		曲譜面ファイルのハッシュ値。
        ///		正確には一意じゃないけど、主キーとして扱う。
        /// </summary>
        public string SongHashId { get; set; }

        /// <summary>
        ///     評価値。0～5。
        /// </summary>
        public int Rating { get; set; }

        /// <summary>
        ///		テーブルのカラム部分を列挙したSQL。
        /// </summary>
        public static readonly string ColumnList =
            @"( UserId NVARCHAR NOT NULL" +
            @", SongHashId NVARCHAR NOT NULL" +
            @", Rating INTEGER NOT NULL" +
            @", PRIMARY KEY(`UserId`,`SongHashId`)" +
            @")";



        // 生成と終了


        public v001_SongPropertiesDBRecord()
        {
            this.UserId = "Anonymous";
            this.SongHashId = "";
            this.Rating = 0;
        }

        public v001_SongPropertiesDBRecord( SqliteDataReader reader )
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
                    case "UserId": this.UserId = record.GetString( i ); break;
                    case "SongHashId": this.SongHashId = record.GetString( i ); break;
                    case "Rating": this.Rating = record.GetInt32( i ); break;
                }
            }
        }

        /// <summary>
        ///     DBにレコードを挿入または更新する。
        /// </summary>
        public virtual void InsertTo( SQLiteDB db )
        {
            using var cmd = new SqliteCommand(
                "REPLACE INTO SongProperties VALUES(" +
                $"'{this.UserId}'," +
                $"'{this.SongHashId}'" +
                $"{this.Rating}," +
                ")", db.Connection );

            cmd.ExecuteNonQuery();
        }
    }
}
