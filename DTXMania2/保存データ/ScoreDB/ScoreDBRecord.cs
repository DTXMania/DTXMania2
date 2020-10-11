using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using FDK;
using SSTF=SSTFormat.v004;

namespace DTXMania2
{
    class ScoreDBRecord
    {

        // プロパティ


        public const int VERSION = 7;

        /// <summary>
        ///		曲譜面ファイルへの絶対パス。主キー。
        /// </summary>
        public string ScorePath { get; set; }

        /// <summary>
        ///		曲のタイトル。
        /// </summary>
        public string Title { get; set; }

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
        ///		曲譜面ファイル（<see cref="ScorePath"/>）からの相対パス。
        /// </summary>
        public string PreImage { get; set; }

        /// <summary>
        ///		曲のアーティスト名。
        /// </summary>
        public string Artist { get; set; }

        /// <summary>
        ///		曲のプレビュー音声ファイルのパス。
        ///		曲譜面ファイル（<see cref="ScorePath"/>）からの相対パス。
        /// </summary>
        public string PreSound { get; set; }

        /// <summary>
        ///     この曲のBGMの再生タイミングを、この時間[ms]分だけ前後にずらす。（負数で早める、正数で遅める）
        /// </summary>
        public int BGMAdjust { get; set; }



        // 生成と終了


        public ScoreDBRecord()
        {
            this.ScorePath = "";
            this.Title = "(no title)";
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

        public ScoreDBRecord( SqliteDataReader reader )
            : this()
        {
            this.UpdateFrom( reader );
        }

        public ScoreDBRecord( VariablePath 譜面ファイルの絶対パス, ユーザ設定 userConfig )
        {
            // 譜面を読み込む。（ノーツ数やBPMを算出するため、ヘッダだけじゃなくすべてを読み込む。）
            var 譜面 = SSTF.スコア.ファイルから生成する( 譜面ファイルの絶対パス.変数なしパス );

            var ノーツ数マップ = _ノーツ数を算出して返す( 譜面, userConfig );
            var (最小BPM, 最大BPM) = _最小最大BPMを調べて返す( 譜面 );

            // 読み込んだ譜面から反映する。
            this.ScorePath = 譜面ファイルの絶対パス.変数なしパス;
            this.Title = 譜面.曲名;
            this.LastWriteTime = File.GetLastWriteTime( this.ScorePath ).ToString( "G" );
            this.Level = 譜面.難易度;
            this.MinBPM = 最小BPM;
            this.MaxBPM = 最大BPM;
            this.TotalNotes_LeftCymbal = ノーツ数マップ[ 演奏.表示レーン種別.LeftCymbal ];
            this.TotalNotes_HiHat = ノーツ数マップ[ 演奏.表示レーン種別.HiHat ];
            this.TotalNotes_LeftPedal = ノーツ数マップ[ 演奏.表示レーン種別.Foot ];
            this.TotalNotes_Snare = ノーツ数マップ[ 演奏.表示レーン種別.Snare ];
            this.TotalNotes_Bass = ノーツ数マップ[ 演奏.表示レーン種別.Bass ];
            this.TotalNotes_HighTom = ノーツ数マップ[ 演奏.表示レーン種別.Tom1 ];
            this.TotalNotes_LowTom = ノーツ数マップ[ 演奏.表示レーン種別.Tom2 ];
            this.TotalNotes_FloorTom = ノーツ数マップ[ 演奏.表示レーン種別.Tom3 ];
            this.TotalNotes_RightCymbal = ノーツ数マップ[ 演奏.表示レーン種別.RightCymbal ];
            this.PreImage = string.IsNullOrEmpty( 譜面.プレビュー画像ファイル名 ) ? "" : 譜面.プレビュー画像ファイル名;
            this.Artist = 譜面.アーティスト名;
            this.PreSound = string.IsNullOrEmpty( 譜面.プレビュー音声ファイル名 ) ? "" : 譜面.プレビュー音声ファイル名;
            this.BGMAdjust = 0;
        }

        /// <summary>
        ///     指定したインスタンスの内容を自身にコピーする。
        /// </summary>
        /// <param name="record">コピー元インスタンス。</param>
        public void UpdateFrom( ScoreDBRecord record )
        {
            this.ScorePath = record.ScorePath;
            this.Title = record.Title;
            this.LastWriteTime = record.LastWriteTime;
            this.Level = record.Level;
            this.MinBPM = record.MinBPM;
            this.MaxBPM = record.MaxBPM;
            this.TotalNotes_LeftCymbal = record.TotalNotes_LeftCymbal;
            this.TotalNotes_HiHat = record.TotalNotes_HiHat;
            this.TotalNotes_LeftPedal = record.TotalNotes_LeftPedal;
            this.TotalNotes_Snare = record.TotalNotes_Snare;
            this.TotalNotes_Bass = record.TotalNotes_Bass;
            this.TotalNotes_HighTom = record.TotalNotes_HighTom;
            this.TotalNotes_LowTom = record.TotalNotes_LowTom;
            this.TotalNotes_FloorTom = record.TotalNotes_FloorTom;
            this.PreImage = record.PreImage;
            this.Artist = record.Artist;
            this.PreSound = record.PreSound;
            this.BGMAdjust = record.BGMAdjust;
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
                    case "Title": this.Title = record.GetString( i ); break;
                    case "LastWriteTime": this.LastWriteTime = record.GetString( i ); break;
                    case "Level": this.Level = record.GetDouble( i ); break;
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
        public void ReplaceTo( SQLiteDB scoredb, string table = "Scores" )
        {
            using var cmd = new SqliteCommand(
                $"REPLACE INTO {table} VALUES" +
                "( @ScorePath" +
                ", @Title" +
                ", @LastWriteTime" +
                ", @Level" +
                ", @MinBPM" +
                ", @MaxBPM" +
                ", @TotalNotes_LeftCymbal" +
                ", @TotalNotes_HiHat"+
                ", @TotalNotes_LeftPedal"+
                ", @TotalNotes_Snare"+
                ", @TotalNotes_Bass"+
                ", @TotalNotes_HighTom"+
                ", @TotalNotes_LowTom"+
                ", @TotalNotes_FloorTom"+
                ", @TotalNotes_RightCymbal"+
                ", @PreImage"+
                ", @Artist"+
                ", @PreSound"+
                ", @BGMAdjust" +
                ")", scoredb.Connection );

            cmd.Parameters.AddRange( new[] {
                new SqliteParameter( "@ScorePath", this.ScorePath ),
                new SqliteParameter( "@Title", this.Title ),
                new SqliteParameter( "@LastWriteTime", this.LastWriteTime ),
                new SqliteParameter( "@Level", this.Level ),
                new SqliteParameter( "@MinBPM", this.MinBPM ),
                new SqliteParameter( "@MaxBPM", this.MaxBPM ),
                new SqliteParameter( "@TotalNotes_LeftCymbal", this.TotalNotes_LeftCymbal ),
                new SqliteParameter( "@TotalNotes_HiHat", this.TotalNotes_HiHat ),
                new SqliteParameter( "@TotalNotes_LeftPedal", this.TotalNotes_LeftPedal ),
                new SqliteParameter( "@TotalNotes_Snare", this.TotalNotes_Snare ),
                new SqliteParameter( "@TotalNotes_Bass", this.TotalNotes_Bass ),
                new SqliteParameter( "@TotalNotes_HighTom", this.TotalNotes_HighTom ),
                new SqliteParameter( "@TotalNotes_LowTom", this.TotalNotes_LowTom ),
                new SqliteParameter( "@TotalNotes_FloorTom", this.TotalNotes_FloorTom ),
                new SqliteParameter( "@TotalNotes_RightCymbal", this.TotalNotes_RightCymbal ),
                new SqliteParameter( "@PreImage", this.PreImage ),
                new SqliteParameter( "@Artist", this.Artist ),
                new SqliteParameter( "@PreSound", this.PreSound ),
                new SqliteParameter( "@BGMAdjust", this.BGMAdjust ),
            } );

            cmd.ExecuteNonQuery();
        }

        /// <summary>
        ///     テーブルがなければ作成するSQLを返す。
        /// </summary>
        public static string GetCreateTableSQL( string table = "Scores" ) =>
            $"CREATE TABLE IF NOT EXISTS {table}" +
            @"( ScorePath NVARCHAR NOT NULL PRIMARY KEY" +
            @", Title NVARCHAR NOT NULL" +
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



        // ローカル


        private Dictionary<演奏.表示レーン種別, int> _ノーツ数を算出して返す( SSTF.スコア score, ユーザ設定 userConfig )
        {
            // ノーツ数マップを初期化。
            var ノーツ数マップ = new Dictionary<演奏.表示レーン種別, int>();
            foreach( 演奏.表示レーン種別? lane in Enum.GetValues( typeof( 演奏.表示レーン種別 ) ) )
            {
                if( lane.HasValue )
                    ノーツ数マップ.Add( lane.Value, 0 );
            }

            // 譜面内のすべてのチップについて……
            foreach( var chip in score.チップリスト )
            {
                var ドラムチッププロパティ = userConfig.ドラムチッププロパティリスト[ chip.チップ種別 ];

                // 1. AutoPlay ON のチップは、すべてが ON である場合を除いて、カウントしない。
                if( userConfig.AutoPlay[ ドラムチッププロパティ.AutoPlay種別 ] )
                {
                    if( !( userConfig.AutoPlayがすべてONである ) )
                        continue;
                }

                // 2. AutoPlay OFF 時でも、ユーザヒットの対象にならないチップはカウントしない。
                if( !( ドラムチッププロパティ.AutoPlayOFF_ユーザヒット ) )
                    continue;

                // カウント。
                ノーツ数マップ[ ドラムチッププロパティ.表示レーン種別 ]++;
            }

            return ノーツ数マップ;
        }

        private static (double 最小BPM, double 最大BPM) _最小最大BPMを調べて返す( SSTF.スコア score )
        {
            var result = (最小BPM: double.MaxValue, 最大BPM: double.MinValue);

            var BPMchips = score.チップリスト.Where( ( c ) => ( c.チップ種別 == SSTF.チップ種別.BPM ) );
            foreach( var chip in BPMchips )
            {
                result.最小BPM = Math.Min( result.最小BPM, chip.BPM );
                result.最大BPM = Math.Max( result.最大BPM, chip.BPM );
            }

            if( result.最小BPM == double.MaxValue || result.最大BPM == double.MinValue )    // BPMチップがひとつもなかった
            {
                double 初期BPM = SSTF.スコア.初期BPM;
                result = (初期BPM, 初期BPM);
            }

            return result;
        }
    }
}
