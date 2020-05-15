using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Data.Sqlite;
using FDK;

namespace DTXMania2.old.SongDBRecord
{
    class v006_SongDBRecord
    {

        // プロパティ


        public const int VERSION = 6;

        /// <summary>
        ///		曲譜面ファイルのハッシュ値。
        ///		正確には一意じゃないけど、主キーとして扱う。
        /// </summary>
        public string HashId { get; set; }

        /// <summary>
        ///		曲のタイトル。
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        ///		曲譜面ファイルへの絶対パス。
        ///		これも一意とする。（テーブル生成SQLで UNIQUE を付与している。）
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        ///		曲譜面ファイルの最終更新時刻の文字列表記。
        ///		文字列の書式は、System.DateTime.ToString("G") と同じ。（例: "08/17/2000 16:32:32"）
        ///		カルチャはシステム既定のものとする。
        /// </summary>
        public string LastWriteTime { get; set; }

        /// <summary>
        ///		曲の難易度。0.00～9.99。
        /// </summary>
        public double Level { get; set; }

        /// <summary>
        ///		最小BPM。null なら未取得。
        /// </summary>
        public double? MinBPM { get; set; }

        /// <summary>
        ///		最大BPM。null なら未取得。
        /// </summary>
        public double? MaxBPM { get; set; }

        /// <summary>
        ///		左シンバルの総ノーツ数。
        /// </summary>
        public int TotalNotes_LeftCymbal { get; set; }

        /// <summary>
        ///		ハイハットの総ノーツ数。
        /// </summary>
        public int TotalNotes_HiHat { get; set; }

        /// <summary>
        ///		左ペダルまたは左バスの総ノーツ数。
        /// </summary>
        public int TotalNotes_LeftPedal { get; set; }

        /// <summary>
        ///		スネアの総ノーツ数。
        /// </summary>
        public int TotalNotes_Snare { get; set; }

        /// <summary>
        ///		バスの総ノーツ数。
        /// </summary>
        public int TotalNotes_Bass { get; set; }

        /// <summary>
        ///		ハイタムの総ノーツ数。
        /// </summary>
        public int TotalNotes_HighTom { get; set; }

        /// <summary>
        ///		ロータムの総ノーツ数。
        /// </summary>
        public int TotalNotes_LowTom { get; set; }

        /// <summary>
        ///		フロアタムの総ノーツ数。
        /// </summary>
        public int TotalNotes_FloorTom { get; set; }

        /// <summary>
        ///		右シンバルの総ノーツ数。
        /// </summary>
        public int TotalNotes_RightCymbal { get; set; }

        /// <summary>
        ///		曲のプレビュー画像。
        ///		曲譜面ファイル（<see cref="Path"/>）からの相対パス。
        /// </summary>
        public string PreImage { get; set; }

        /// <summary>
        ///		曲のアーティスト名。
        /// </summary>
        public string Artist { get; set; }

        /// <summary>
        ///		曲のプレビュー音声ファイルのパス。
        ///		曲譜面ファイル（<see cref="Path"/>）からの相対パス。
        /// </summary>
        public string PreSound { get; set; }

        /// <summary>
        ///     この曲のBGMの再生タイミングを、この時間[ms]分だけ前後にずらす。（負数で早める、正数で遅める）
        /// </summary>
        public int BGMAdjust { get; set; }


        /// <summary>
        ///		テーブルのカラム部分を列挙したSQL。
        /// </summary>
        public static readonly string ColumnList =
            @"( HashId NVARCHAR NOT NULL PRIMARY KEY" +
            @", Title NVARCHAR NOT NULL" +
            @", Path NVARCHAR NOT NULL UNIQUE" +
            @", LastWriteTime NVARCHAR NOT NULL" +
            @", Level NUMERIC NOT NULL" +
            @", MinBPM NUMERIC" +
            @", MaxBPM NUMERIC" +
            @", TotalNotes_LeftCymbal INTEGER NOT NULL" +
            @", TotalNotes_HiHat INTEGER NOT NULL" +
            @", TotalNotes_LeftPedal INTEGER NOT NULL" +
            @", TotalNotes_Snare INTEGER NOT NULL" +
            @", TotalNotes_Bass INTEGER NOT NULL" +
            @", TotalNotes_HighTom INTEGER NOT NULL" +
            @", TotalNotes_LowTom INTEGER NOT NULL" +
            @", TotalNotes_FloorTom INTEGER NOT NULL" +
            @", TotalNotes_RightCymbal INTEGER NOT NULL" +
            @", PreImage NVARCHAR" +
            @", Artist NVARCHAR" +
            @", PreSound NVARCHAR" +
            @", BGMAdjust INTEGER NOT NULL" +
            @")";



        // 生成と終了


        public v006_SongDBRecord()
        {
            this.HashId = "";
            this.Title = "(no title)";
            this.Path = "";
            this.LastWriteTime = DateTime.Now.ToString( "G" );
            this.Level = 5.00;
            this.MinBPM = null;
            this.MaxBPM = null;
            this.TotalNotes_LeftCymbal = 0;
            this.TotalNotes_HiHat = 0;
            this.TotalNotes_LeftPedal = 0;
            this.TotalNotes_Snare = 0;
            this.TotalNotes_Bass = 0;
            this.TotalNotes_HighTom = 0;
            this.TotalNotes_LowTom = 0;
            this.TotalNotes_FloorTom = 0;
            this.TotalNotes_RightCymbal = 0;
            this.PreImage = "";
            this.Artist = "";
            this.PreSound = "";
            this.BGMAdjust = 0;
        }

        public v006_SongDBRecord( SqliteDataReader reader )
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
                    case "HashId": this.HashId = record.GetString( i ); break;
                    case "Title": this.Title = record.GetString( i ); break;
                    case "Path": this.Path = record.GetString( i ); break;
                    case "LastWriteTime": this.LastWriteTime = record.GetString( i ); break;
                    case "MinBPM": this.MinBPM = record.GetDouble( i ); break;
                    case "MaxBPM": this.MaxBPM = record.GetDouble( i ); break;
                    case "TotalNotes_LeftCymbal": this.TotalNotes_LeftCymbal = record.GetInt32( i ); break;
                    case "TotalNotes_HiHat": this.TotalNotes_HiHat = record.GetInt32( i ); break;
                    case "TotalNotes_LeftPedal": this.TotalNotes_LeftPedal = record.GetInt32( i ); break;
                    case "TotalNotes_Snare": this.TotalNotes_Snare = record.GetInt32( i ); break;
                    case "TotalNotes_Bass": this.TotalNotes_Bass = record.GetInt32( i ); break;
                    case "TotalNotes_HighTom": this.TotalNotes_HighTom = record.GetInt32( i ); break;
                    case "TotalNotes_LowTom": this.TotalNotes_LowTom = record.GetInt32( i ); break;
                    case "TotalNotes_FloorTom": this.TotalNotes_FloorTom = record.GetInt32( i ); break;
                    case "TotalNotes_RightCymbal": this.TotalNotes_RightCymbal = record.GetInt32( i ); break;
                    case "PreImage": this.PreImage = record.GetString( i ); break;
                    case "Artist": this.Artist = record.GetString( i ); break;
                    case "PreSound": this.PreSound = record.GetString( i ); break;
                    case "BGMAdjust": this.BGMAdjust = record.GetInt32( i ); break;
                }
            }
        }

        /// <summary>
        ///     DBにレコードを挿入または更新する。
        /// </summary>
        public void ReplaceTo( SQLiteDB db )
        {
            using var cmd = new SqliteCommand(
                "REPLACE INTO Records VALUES(" +
                $"'{this.HashId}'," +
                $"'{this.Title}'," +
                $"'{this.Path}'," +
                $"'{this.LastWriteTime}'," +
                $"{this.Level}," +
                $"{this.MinBPM}," +
                $"{this.MaxBPM}," +
                $"{this.TotalNotes_LeftCymbal}," +
                $"{this.TotalNotes_HiHat}," +
                $"{this.TotalNotes_LeftPedal}," +
                $"{this.TotalNotes_Snare}," +
                $"{this.TotalNotes_Bass}," +
                $"{this.TotalNotes_HighTom}," +
                $"{this.TotalNotes_LowTom}," +
                $"{this.TotalNotes_FloorTom}," +
                $"{this.TotalNotes_RightCymbal}," +
                $"'{this.PreImage}'," +
                $"'{this.Artist}'," +
                $"'{this.PreSound}'," +
                $"{this.BGMAdjust}" +
                ")", db.Connection );

            cmd.ExecuteNonQuery();
        }
    }
}
