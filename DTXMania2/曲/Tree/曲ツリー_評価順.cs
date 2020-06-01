using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using FDK;

namespace DTXMania2.曲
{
    class 曲ツリー_評価順 : 曲ツリー
    {

        // 生成と終了


        /// <summary>
        ///     <see cref="App.全曲リスト"/> を元に、評価順曲ツリーを構築する。
        /// </summary>
        public 曲ツリー_評価順()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this.初期化する();
        }

        public override void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            // 全ノードを解放する。
            foreach( var node in this.ルートノード.Traverse() )
                node.Dispose();

            base.Dispose();
        }


        /// <summary>
        ///     ツリーを初期状態に戻す。
        /// </summary>
        public void 初期化する()
        {
            // 全ノードを解放する。
            foreach( var node in this.ルートノード.Traverse() )
                node.Dispose();
            
            this.ルートノード.子ノードリスト.Clear();

            // RANDOM SELECT を追加する。
            this.ルートノード.子ノードリスト.Add( new RandomSelectNode() { 親ノード = this.ルートノード } );

            // 評価BOX を追加する。
            var ratingBoxLabel = new[] {
                "評価 なし",
                "評価 ☆",
                "評価 ☆☆",
                "評価 ☆☆☆",
                "評価 ☆☆☆☆",
                "評価 ☆☆☆☆☆",
            };
            this._評価BOX = new BoxNode[ 6 ];   // 0～5
            for( int i = 0; i < this._評価BOX.Length; i++ )
            {
                this._評価BOX[ i ] = new BoxNode( ratingBoxLabel[ i ] ) { 親ノード = this.ルートノード };
                this._評価BOX[ i ].子ノードリスト.Add( new BackNode() { 親ノード = this._評価BOX[ i ] } );
                this._評価BOX[ i ].子ノードリスト.Add( new RandomSelectNode() { 親ノード = this._評価BOX[ i ] } );
            }
            for( int i = 0; i < this._評価BOX.Length; i++ )
                this.ルートノード.子ノードリスト.Add( this._評価BOX[ this._評価BOX.Length - 1 - i ] );

            // ここまで追加したすべてのノードの画像を生成する。（現行化待ち中に表示されるため）
            foreach( var node in this.ルートノード.Traverse() )
            {
                node.タイトル文字列画像 = 現行化.タイトル文字列画像を生成する( node.タイトル );
                node.サブタイトル文字列画像 = 現行化.サブタイトル文字列画像を生成する( node.サブタイトル );
                node.ノード画像 = 現行化.ノード画像を生成する( node.ノード画像ファイルの絶対パス );
            }

            // 最初のノードを選択する。
            this.フォーカスリスト.SelectFirst();
        }

        /// <summary>
        ///     現状の <see cref="App.全曲リスト"/> の内容に従って、評価順曲ツリーを再構築する。
        /// </summary>
        public void 再構築する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this.初期化する();

            foreach( var song in Global.App.全曲リスト )
            {
                var score = song.譜面リスト?.FirstOrDefault( ( s ) => ( s != null && s.譜面の属性 != null ) );

                int rating = score?.譜面の属性?.Rating ?? 0;

                this._評価BOX[ rating ].子ノードリスト.Add( new SongNode( song ) { 親ノード = this._評価BOX[ rating ] } );
            }
        }


        protected BoxNode[] _評価BOX = null!; // [0～5]
    }
}
