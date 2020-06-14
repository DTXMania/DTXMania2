using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FDK;

namespace DTXMania2.曲
{
    // hack: box.def の PreImage 他への対応

    class BoxNode : Node
    {

        // プロパティ


        public SelectableList<Node> 子ノードリスト { get; } = new SelectableList<Node>();



        // 生成と終了


        public BoxNode( string BOX名 )
        {
            this.タイトル = BOX名;
        }

        public BoxNode( BoxDef def )
        {
            this.タイトル = def.TITLE;
            this.サブタイトル = def.ARTIST ?? "";
        }



        // その他


        /// <summary>
        ///     子孫を直列に列挙する。
        /// </summary>
        public IEnumerable<Node> Traverse()
        {
            // 幅優先探索。

            // (1) すべての子ノード。
            foreach( var child in this.子ノードリスト )
                yield return child;

            // (2) すべてのBOXノードの子ノード。
            foreach( var child in this.子ノードリスト )
            {
                if( child is BoxNode box )
                {
                    foreach( var coc in box.Traverse() )
                        yield return coc;
                }
            }
        }
    }
}
