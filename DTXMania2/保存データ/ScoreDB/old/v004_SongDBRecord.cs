using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DTXMania2.old.SongDBRecord
{
    class v004_SongDBRecord
    {
        public const int VERSION = 4;

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



        // 生成と終了


        public v004_SongDBRecord()
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
        }
    }
}
