using System;
using System.Collections.Generic;
using System.Diagnostics;
using FDK;

namespace DTXMania2.曲
{
    class Node : IDisposable
    {

        // ツリーノードプロパティ


        public virtual string タイトル { get; set; } = "(no title)";

        public virtual string? サブタイトル { get; set; } = null;

        public virtual VariablePath? ノード画像ファイルの絶対パス { get; set; } = null;



        // 画像プロパティ


        public virtual 画像D2D? ノード画像
        {
            get { lock( this._排他 ) return this._ノード画像; }
            set { lock( this._排他 ) this._ノード画像 = value; }
        }

        public virtual 文字列画像D2D? タイトル文字列画像
        {
            get { lock( this._排他 ) return this._タイトル文字列画像; }
            set { lock( this._排他 ) this._タイトル文字列画像 = value; }
        }

        public virtual 文字列画像D2D? サブタイトル文字列画像
        {
            get { lock( this._排他 ) return this._サブタイトル文字列画像; }
            set { lock( this._排他 ) this._サブタイトル文字列画像 = value; }
        }

        /// <summary>
        ///     このノードの現行化が終了していれば true。
        /// </summary>
        /// <remarks>
        ///     画像プロパティは、このフラグが true のときのみ有効。
        ///     <see cref="SongNode"/> については、このフラグが true であれば、
        ///     対応する <see cref="Node"/> の現行化も完了している。
        /// </remarks>
        public virtual bool 現行化済み
        {
            get { lock( this._排他 ) return this._現行化済み; }
            set { lock( this._排他 ) this._現行化済み = value; }
        }



        // ツリー構造プロパティ


        /// <summary>
        ///     このノードの親ノード。
        /// </summary>
        /// <remarks>
        ///     null なら親ノードはルートノードである。
        /// </remarks>
        public virtual BoxNode? 親ノード { get; set; } = null;

        /// <summary>
        ///		このノードの１つ前に位置する兄弟ノードを示す。
        /// </summary>
        /// <remarks>
        ///		このノードが先頭である（このノードの親ノードの子ノードリストの先頭である）場合は、末尾に位置する兄弟ノードを示す。
        /// </remarks>
        public virtual Node 前のノード
        {
            get
            {
                if( this.親ノード is null )
                    throw new Exception( "親ノードが登録されていません。" );

                int index = this.親ノード.子ノードリスト.IndexOf( this );

                if( 0 > index )
                    throw new Exception( "自身が親ノードの子として登録されていません。" );

                index--;

                if( 0 > index )
                    index = this.親ノード.子ノードリスト.Count - 1;    // 先頭だったなら、末尾へ。

                return this.親ノード.子ノードリスト[ index ];
            }
        }

        /// <summary>
        ///		このノードの１つ後に位置する兄弟ノードを示す。
        /// </summary>
        /// <remarks>
        ///		このノードが末尾である（このノードの親ノードの子ノードリストの末尾である）場合は、先頭に位置する兄弟ノードを示す。
        /// </remarks>
        public virtual Node 次のノード
        {
            get
            {
                if( this.親ノード is null )
                    throw new Exception( "親ノードが登録されていません。" );

                int index = this.親ノード.子ノードリスト.IndexOf( this );

                if( 0 > index )
                    throw new Exception( "自身が親ノードの子として登録されていません。" );

                index++;

                if( this.親ノード.子ノードリスト.Count <= index )
                    index = 0;    // 末尾だったなら、先頭へ。

                return this.親ノード.子ノードリスト[ index ];
            }
        }



        // 生成と終了


        public Node()
        {
        }

        public virtual void Dispose()
        {
            this._サブタイトル文字列画像?.Dispose();
            this._タイトル文字列画像?.Dispose();
            this._ノード画像?.Dispose();
        }



        // ローカル


        protected 画像D2D? _ノード画像 = null;

        protected 文字列画像D2D? _タイトル文字列画像 = null;

        protected 文字列画像D2D? _サブタイトル文字列画像 = null;

        protected bool _現行化済み = false;

        /// <summary>
        ///     現行化処理との排他用。
        /// </summary>
        protected readonly object _排他 = new object();
    }
}
