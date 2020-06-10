using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SharpDX;
using FDK;

namespace DTXMania2.曲
{
    class SongNode : Node
    {

        // プロパティ


        public override string タイトル => this.曲.フォーカス譜面?.譜面.Title ?? "(no title)";

        public override string サブタイトル => this.曲.フォーカス譜面?.譜面.Artist ?? "";

        public override 画像D2D? ノード画像 => this.曲.フォーカス譜面?.プレビュー画像;

        public override 文字列画像D2D? タイトル文字列画像 => this.曲.フォーカス譜面?.タイトル文字列画像;

        public override 文字列画像D2D? サブタイトル文字列画像 => this.曲.フォーカス譜面?.サブタイトル文字列画像;

        public Song 曲 { get; } = null!;



        // 生成と終了


        public SongNode( Song 曲 )
        {
            this.曲 = 曲;
        }
    }
}
