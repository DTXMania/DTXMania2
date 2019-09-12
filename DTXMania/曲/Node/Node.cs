using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania
{
    /// <summary>
    ///		曲ノードの基本クラス。
    /// </summary>
    /// <remarks>
    ///		曲ツリーを構成するすべてのノードは、このクラスを継承する。
    /// </remarks>
    abstract partial class Node : IDisposable
    {

        // プロパティ


        /// <summary>
        ///		ノードのタイトル。
        ///		曲名、BOX名など。
        /// </summary>
        public virtual string タイトル { get; set; } = "(no title)";

        /// <summary>
        ///		ノードのサブタイトル。
        ///		制作者名など。
        /// </summary>
        public virtual string サブタイトル { get; set; } = "";

        /// <summary>
        ///     難易度ラベル。半角英数字。
        /// </summary>
        public virtual string 難易度ラベル { get; set; } = "";

        /// <summary>
        ///     難易度。0.00～9.99。
        /// </summary>
        public virtual double 難易度
        {
            get => this._難易度;
            set => this._難易度 = ( 0.00 > value || 9.99 < value ) ? throw new ArgumentOutOfRangeException() : value;
        }



        // 曲ツリー関連


        /// <summary>
        ///		曲ツリー階層において、親となるノード。
        /// </summary>
        public Node 親ノード { get; set; } = null;

        /// <summary>
        ///		曲ツリー階層において、このノードが持つ子ノードのリスト。
        ///		子ノードを持たない場合は空リスト。
        ///		null 不可。
        /// </summary>
        public SelectableList<Node> 子ノードリスト { get; } = new SelectableList<Node>();

        /// <summary>
        ///		このノードの１つ前に位置する兄弟ノードを示す。
        /// </summary>
        /// <remarks>
        ///		このノードが先頭である（このノードの親ノードの子ノードリストの先頭である）場合は、末尾に位置する兄弟ノードを示す。
        /// </remarks>
        public Node 前のノード
        {
            get
            {
                var index = this.親ノード.子ノードリスト.IndexOf( this );
                Trace.Assert( ( 0 <= index ), "[バグあり] 自分が、自分の親の子ノードリストに存在していません。" );

                index--;

                if( 0 > index )
                    index = this.親ノード.子ノードリスト.Count - 1;    // 先頭なら、末尾へ。

                return this.親ノード.子ノードリスト[ index ];
            }
        }

        /// <summary>
        ///		このノードの１つ後に位置する兄弟ノードを示す。
        /// </summary>
        /// <remarks>
        ///		このノードが末尾である（このノードの親ノードの子ノードリストの末尾である）場合は、先頭に位置する兄弟ノードを示す。
        /// </remarks>
        public Node 次のノード
        {
            get
            {
                var index = this.親ノード.子ノードリスト.IndexOf( this );
                Trace.Assert( ( 0 <= index ), "[バグあり] 自分が、自分の親の子ノードリストに存在していません。" );

                index++;

                if( this.親ノード.子ノードリスト.Count <= index )
                    index = 0;      // 末尾なら、先頭へ。

                return this.親ノード.子ノードリスト[ index ];
            }
        }

        /// <summary>
        ///     自分と子孫を直列に列挙する。
        /// </summary>
        public IEnumerable<Node> Traverse()
        {
            // 幅優先探索。

            // (1) 自分。
            yield return this;

            // (2) 子ノードリスト。SetNode.MusicNodes[] を含む。
            foreach( var child in this.子ノードリスト )
            {
                yield return child;
            }

            // (3) 子ノードのその他。
            foreach( var child in this.子ノードリスト )
            {
                foreach( var n in child.Traverse() )
                {
                    if( n != child )
                        yield return n;
                }
            }
        }



        // ノード画像関連


        /// <summary>
        ///		ノードを表す画像。
        /// </summary>
        /// <remarks>
        ///		派生クラスで、適切な画像を割り当てること。（このクラスでは、生成も Dispose もしない。）
        ///		<see cref="SetNode"/> の場合のみ、扱いが異なる。
        ///		詳細は<see cref="SetNode.ノード画像"/>を参照のこと。
        /// </remarks>
        public virtual テクスチャ ノード画像 { get; set; }

        /// <summary>
        ///		ノードの全体サイズ（設計単位）。
        ///		すべてのノードで同一、固定値。
        /// </summary>
        public static Size2F 全体サイズ => new Size2F( 314f, 220f );

        /// <summary>
        ///		ノードを表す画像の既定画像。
        /// </summary>
        public static テクスチャ 既定のノード画像 { get; private set; }

        /// <summary>
        ///		現行化前のノードを表す画像の既定画像。
        /// </summary>
        public static テクスチャ 現行化前のノード画像 { get; private set; }


        // プレビュー音声関連


        public virtual VariablePath プレビュー音声ファイルの絶対パス { get; set; } = null;

        public void プレビュー音声を再生する()
        {
            if( null != this.プレビュー音声ファイルの絶対パス )
                this._プレビュー音声.再生する( this.プレビュー音声ファイルの絶対パス.変数なしパス );
        }

        public void プレビュー音声を停止する()
        {
            this._プレビュー音声.停止する();
        }

        private Node.PreviewSound _プレビュー音声;  // null なら未使用



        // 生成と終了


        public Node()
        {
            this._プレビュー音声 = new PreviewSound();

            if( 0 == _インスタンス数++ )
            {
                既定のノード画像 = new テクスチャ( @"$(System)images\既定のプレビュー画像.png" );
                現行化前のノード画像 = new テクスチャ( @"$(System)images\現行化待ちのプレビュー画像.png" );
            }
        }

        public virtual void Dispose()
        {
            //this.ノード画像?.Dispose();            --> 生成も解放も派生クラスに任せる。

            this._プレビュー音声?.Dispose();

            if( 0 == --_インスタンス数 )
            {
                既定のノード画像?.Dispose();
                現行化前のノード画像?.Dispose();
            }

            foreach( var child in this.子ノードリスト )
                child.Dispose();
        }



        // 描画


        public virtual void 進行描画する( DeviceContext dc, Matrix ワールド変換行列, bool キャプション表示 = true )
        {
            if( null != this.ノード画像 )
            {
                this.ノード画像.描画する( ワールド変換行列 );
            }
            else
            {
                Node.既定のノード画像.描画する( ワールド変換行列 );
            }
        }



        // private


        private double _難易度 = 0.0f;

        private static int _インスタンス数 = 0;
    }
}
