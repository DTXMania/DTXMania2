﻿using FDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DTXMania2_.曲
{
    class RandomSelectNode : Node
    {

        // プロパティ


        public override string タイトル => "< RANDOM SELECT >";



        // ランダムセレクト


        /// <summary>
        ///     このノードの属するノードリストからランダムに SongNode とフォーカス譜面を選択して返す。
        ///     曲がなければ null を返す。
        /// </summary>
        public (Score 譜面, SongNode 曲)? 譜面をランダムに選んで返す()
        {
            for( int retry = 0; retry < 10; retry++ )
            {
                // RandomSelect がある階層以降すべての SongNode を取得。
                var songNode配列 = this.親ノード!.Traverse().Where( ( node ) => node is SongNode ).ToArray();
                int songNode数 = songNode配列.Count();

                if( 0 == songNode数 )
                    break;

                // 乱数でノードを決定。
                var songNode = songNode配列[ Global.App.乱数.Next( songNode数 ) ] as SongNode;

                if( null != songNode?.曲.フォーカス譜面 )
                    return (songNode.曲.フォーカス譜面, songNode);
            }

            Log.ERROR( $"{nameof( SongNode )} が１つも見つかりません。" );
            return null;
        }
    }
}
