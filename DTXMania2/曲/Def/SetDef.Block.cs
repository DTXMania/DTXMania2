using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;

namespace DTXMania2.曲
{
    public partial class SetDef
    {
        /// <summary>
        ///		最大５曲をレベル別に保有できるブロック。
        /// </summary>
        /// <remarks>
        ///		set.def では任意個のブロックを宣言できる。（#TITLE行が登場するたび新しいブロックとみなされる）
        /// </remarks>
        public class Block
        {
            /// <summary>
            ///		スコアのタイトル（#TITLE）を保持する。
            /// </summary>
            public string Title { get; set; } = "(no title)";

            /// <summary>
            ///		スコアのフォント色（#FONTCOLOR）を保持する。
            /// </summary>
            public Color FontColor { get; set; } = Color.White;

            /// <summary>
            ///		スコアファイル名（#LxFILE）を保持する。
            ///		配列は [0～4] で、存在しないレベルは null となる。
            /// </summary>
            public string?[] File { get; } = new string[ 5 ];

            /// <summary>
            ///		スコアのラベル（#LxLABEL）を保持する。
            ///		配列は[0～4] で、存在しないレベルは null となる。
            /// </summary>
            public string?[] Label { get; } = new string[ 5 ];
        }
    }
}
