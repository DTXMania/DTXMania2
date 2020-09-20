using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FDK;

namespace DTXMania2.曲
{
    abstract class 曲ツリー : IDisposable
    {

        // 曲ツリーは、1つのルートから成るノードの階層構造を持つ。


        public virtual RootNode ルートノード { get; }



        // フォーカスについて：
        //
        // 【フォーカスホルダ】
        // 　・曲ツリーは、ツリー内のいずれかの BoxNode を選択（フォーカス）することができる。
        // 　・この BoxNode を「フォーカスホルダ」と称する。
        //
        // 【フォーカスリスト】
        // 　・フォーカスホルダの子ノードリストは「フォーカスリスト」とされ、選曲画面に表示される対象となる。
        //
        // 【フォーカスノード（フォーカス曲）】
        // 　・フォーカスリストは SelectableList<Node> 型であり、ゼロまたは1個のノードを選択（フォーカス）することができる。
        // 　　これを「フォーカスノード」（または「フォーカス曲」）と称する。
        //
        // 【フォーカス難易度レベル】
        // 　・後述。


        public virtual BoxNode フォーカスホルダ { get; set; }

        public virtual SelectableList<Node> フォーカスリスト => this.フォーカスホルダ.子ノードリスト;

        public virtual Node? フォーカスノード => this.フォーカスリスト.SelectedItem;

        /// <summary>
        ///     指定されたノードをフォーカス（選択）する。
        /// </summary>
        /// <remarks>
        ///     必要に応じて、<see cref="フォーカスホルダ"/>, <see cref="フォーカスリスト"/> も変更される。
        /// </remarks>
        /// <return>結果としてフォーカスノードが変わったら true 。</return>
        public bool フォーカスする( Node focusNode )
        {
            var 変更前のノード = this.フォーカスノード;

            if( focusNode == 変更前のノード )
                return false;   // 同一

            // ホルダを変更。
            this.フォーカスホルダ = focusNode.親ノード ?? 
                throw new Exception( "ルートノードをフォーカスすることはできません。" );

            // チェック。
            if( !this.フォーカスホルダ.子ノードリスト.SelectItem( focusNode ) ||
                this.フォーカスホルダ.子ノードリスト.SelectedItem is null )
                    throw new Exception( "フォーカスノードの選択に失敗しました。" );

            return true;
        }

        /// <summary>
        ///     現在のフォーカスノードの１つ後のノードをフォーカスする。
        /// </summary>
        /// <remarks>
        ///     次のノードが存在しない場合は、フォーカスリストの先頭のノードをフォーカスする。
        /// </remarks>
        /// <return>結果としてフォーカスノードが変わったら true 。</return>
        public bool 次のノードをフォーカスする()
        {
            var 変更前のノード = this.フォーカスノード;

            var focus_index = this.フォーカスリスト.SelectedIndex;
            var next_index = ( 0 > focus_index ) ? 0 : ( focus_index + 1 ) % this.フォーカスリスト.Count;

            this.フォーカスリスト.SelectItem( next_index );

            return ( this.フォーカスノード != 変更前のノード );
        }

        /// <summary>
        ///     現在のフォーカスノードの１つ前ノードをフォーカスする。
        /// </summary>
        /// <remarks>
        ///     前のノードが存在しない場合は、フォーカスリストの末尾のノードをフォーカスする。
        /// </remarks>
        /// <return>結果としてフォーカスノードが変わったら true 。</return>
        public bool 前のノードをフォーカスする()
        {
            var 変更前のノード = this.フォーカスノード;

            var focus_index = this.フォーカスリスト.SelectedIndex;
            var prev_index = ( 0 > focus_index ) ? 0 : ( focus_index - 1 + this.フォーカスリスト.Count ) % this.フォーカスリスト.Count;

            this.フォーカスリスト.SelectItem( prev_index );

            return ( this.フォーカスノード != 変更前のノード );
        }



        // 難易度レベルについて：
        //
        // 【難易度レベル】
        // 　・set.def を使うと、1つの曲に対して最大5つの難易度の譜面を用意することができる。
        // 　　慣例では 0:BASIC, 1:ADVANCED, 2:EXTREME, 3:MASTER, 4:ULTIMATE に相当する。（set.defのL1～L5に対応する。）
        //
        // 【ユーザ希望難易度レベル】
        // 　・ユーザは、選曲画面で、希望の難易度レベルを選択することができる。
        // 　　これを「ユーザ希望難易度レベル」と称する。
        //
        // 【フォーカス難易度レベル】
        // 　・ユーザ希望難易度レベルに「一番近い」フォーカスノードの難易度レベルを、「フォーカス難易度レベル」と称する。
        // 　・すべてのノードは、必ずしも5つの難易度レベルすべてに対応している必要は無い。
        // 　　　- 例えば、EXTREME相当の譜面しか持たない曲も多く存在する。
        // 　　　  この場合、この曲がフォーカスされた時点でのフォーカス難易度レベルは、2 にならざるを得ない。
        // 　・従って、ユーザ希望難易度レベルとフォーカス難易度レベルは、必ずしも一致しない。
        //
        // ※「難易度」「難易度レベル」「難易度ラベル」の違いに注意。
        // 　・難易度 = 0.00 ～ 9.99
        //   ・難易度レベル = 0 ～ 4
        //   ・難易度ラベル = "BASIC", "ADVANCED" など


        public int ユーザ希望難易度レベル { get; protected set; } = 2; // 初期値は 2（MASTER 相当）

        public int フォーカス難易度レベル => this.フォーカスノード switch
        {
            // 未選択
            null => this.ユーザ希望難易度レベル,

            // 曲ノード
            SongNode snode => snode.曲.ユーザ希望難易度に最も近い難易度レベルを返す( this.ユーザ希望難易度レベル ),

            // その他のノード
            _ => this.ユーザ希望難易度レベル,
        };
       
        public void ユーザ希望難易度をひとつ増やす()
        {
            // ユーザ希望難易度を1つ増やす。（4を越えたら0に戻る。）
            // ただし、フォーカス難易度と一致しなかった（フォーカス曲がその難易度レベルの譜面を持っていなかった）場合は、
            // 一致するまで増やし続ける。

            for( int i = 0; i < 5; i++ )   // 最低5回で一周する
            {
                // 1つ増やす。
                this.ユーザ希望難易度レベル = ( this.ユーザ希望難易度レベル + 1 ) % 5;    // 4を越えたら0に戻る

                if( this.フォーカスノード is SongNode snode )
                {
                    // その難易度レベルに対応する譜面があればOK。
                    if( null != snode.曲.譜面リスト[ this.ユーザ希望難易度レベル ] )
                        return;
                }
                else
                {
                    // SongNode 以外は特に条件なし。
                    return;
                }
            }
        }



        // 生成と終了


        public 曲ツリー()
        {
            this.ルートノード = new RootNode();
            this.フォーカスホルダ = this.ルートノード;
        }

        public virtual void Dispose()
        {
            foreach( var node in this.ルートノード.Traverse() )
                node.Dispose();

            Song.現在の難易度レベル = () => throw new NotImplementedException();
        }
    }
}
