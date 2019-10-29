using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DTXMania2.曲
{
    class RandomSelectNode : Node
    {

        // プロパティ


        public override string タイトル => "< RANDOM SELECT >";



        // ランダムセレクト


        public Score 譜面をランダムに選んで返す( RandomSelectNode rsnode )
        {
            // RandomSelect がある階層以降すべての SongNode を取得。
            var songNode配列 = rsnode.親ノード!.Traverse().Where( ( node ) => node is SongNode ).ToArray();
            int songNode数 = songNode配列.Count();

            if( 0 == songNode数 )
                throw new Exception( $"{nameof(SongNode)} が１つも見つかりません。" );

            // 乱数でノードを決定。
            var songNode = songNode配列[ Global.App.乱数.Next( songNode数 ) ] as SongNode;

            return songNode!.曲.フォーカス譜面;
        }
    }
}
