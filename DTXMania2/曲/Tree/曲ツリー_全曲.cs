using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using FDK;

namespace DTXMania2.曲
{
    class 曲ツリー_全曲 : 曲ツリー
    {

        // プロパティ


        public long 進捗カウンタ
            => Interlocked.Read( ref this._進捗カウンタ );



        // 生成と終了


        public 曲ツリー_全曲()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._進捗カウンタ = 0;
        }

        public override void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            base.Dispose();
        }



        // ツリーと全譜面リストの構築


        /// <summary>
        ///     標準の曲ツリーを構築する。
        /// </summary>
        /// <remarks>
        ///     曲ツリーは、曲検索フォルダを検索した結果に、現状の（現行前の）ScoreDB の内容を反映したものとなる。
        ///     曲ツリーを構築すると同時に、全曲リスト/全譜面リストも構築する。
        /// </remarks>
        public async Task 構築するAsync( IEnumerable<VariablePath> 曲検索フォルダパスリスト )
        {
            //using var _ = new LogBlock( Log.現在のメソッド名 );

            await Task.Run( () => {

                // 曲検索フォルダパスをスキャンして、曲ツリー（と全譜面リスト）を構築する。

                // ルートノードの子ノードリストの先頭に「ランダムセレクト」を追加する。
                this.ルートノード.子ノードリスト.Add( new RandomSelectNode() { 親ノード = this.ルートノード } );

                // 曲検索パスに従って、ルートノード以降を構築する。
                // 同時に、この中で、全曲リストと全譜面リストも構築する。
                foreach( var path in 曲検索フォルダパスリスト )
                    this._構築する( path, this.ルートノード );

                //  最初の子ノードをフォーカスする。
                this.フォーカスリスト.SelectFirst();
            
            } );
        }

        /// <summary>
        ///     現状の ScoreDB と ScorePropertiesDB（ともに現行化前）を読み込んで反映する。
        /// </summary>
        public async Task ノードにDBを反映するAsync()
        {
            //using var _ = new LogBlock( Log.現在のメソッド名 );

            this._進捗カウンタ = 0;

            await Task.Run( () => {

                using var scoredb = new ScoreDB();
                using var scorePropertiesdb = new ScorePropertiesDB();
                using var query = new SqliteCommand( "SELECT * FROM Scores", scoredb.Connection );  // 全レコード抽出
                var result = query.ExecuteReader();
                while( result.Read() )
                {
                    Interlocked.Increment( ref this._進捗カウンタ );

                    var record = new ScoreDBRecord( result );

                    // レコードに記載されているパスが全譜面リストに存在していれば、レコードの内容で更新する。
                    var scores = Global.App.全譜面リスト.Where( ( s ) => s.譜面.ScorePath == record.ScorePath );
                    foreach( var score in scores )
                        score.譜面.UpdateFrom( record );
                }

            } );
        }

        /// <summary>
        ///     文字列画像のみ生成する。（現行化待ち中に表示されるため）
        /// </summary>
        public async Task 文字列画像を生成するAsync()
        {
            //using var _ = new LogBlock( Log.現在のメソッド名 );

            this._進捗カウンタ = 0;

            await Task.Run( () => {

                foreach( var score in Global.App.全譜面リスト )
                {
                    Interlocked.Increment( ref this._進捗カウンタ );

                    score.タイトル文字列画像 = 現行化.タイトル文字列画像を生成する( score.譜面.Title );
                    score.サブタイトル文字列画像 = 現行化.サブタイトル文字列画像を生成する( score.譜面.Artist );
                }

            } );
        }



        // ローカル


        private long _進捗カウンタ;   // Interlocked でアクセスすること


        /// <summary>
        ///     1つのフォルダに対して検索と構築を行う。
        ///     同時に、全譜面リストと全曲リストも構築する。
        /// </summary>
        /// <param name="基点フォルダパス">検索フォルダの絶対パス。</param>
        /// <param name="親ノード">ノードは、このノードの子ノードとして構築される。</param>
        /// <param name="boxdefが有効">true にすると、フォルダ内の box.def が有効になる。false にすると box.def を無視する。</param>
        private void _構築する( VariablePath 基点フォルダパス, BoxNode 親ノード, bool boxdefが有効 = true )
        {
            #region " 基点フォルダが存在しない場合やアクセスできない場合は無視。"
            //----------------
            if( !( Directory.Exists( 基点フォルダパス.変数なしパス ) ) )
            {
                Log.WARNING( $"指定されたフォルダが存在しません。無視します。[{基点フォルダパス.変数付きパス}]" );
                return;
            }

            try
            {
                Directory.GetFiles( 基点フォルダパス.変数なしパス );
            }
            catch( UnauthorizedAccessException )
            {
                Log.ERROR( $"アクセスできないフォルダです。無視します。[{基点フォルダパス.変数付きパス}]" );
                return;
            }
            //----------------
            #endregion

            // 一時リスト。作成したノードをいったんこのノードリストに格納し、あとでまとめて曲ツリーに登録する。
            var 追加ノードリスト = new List<Node>();

            // set.def/box.def ファイルの有無により処理分岐。
            var dirInfo = new DirectoryInfo( 基点フォルダパス.変数なしパス );
            var boxDefPath = new VariablePath( Path.Combine( 基点フォルダパス.変数なしパス, @"box.def" ) );
            var setDefPath = new VariablePath( Path.Combine( 基点フォルダパス.変数なしパス, @"set.def" ) );
            bool サブフォルダを検索する = true;

            if( boxdefが有効 && File.Exists( boxDefPath.変数なしパス ) )
            {
                #region " (A) このフォルダに box.def がある → BOXノードを作成し、子ノードリストを再帰的に構築する。"
                //----------------
                // box.defを読み込んでBOXノードを作成する。
                var boxDef = new BoxDef( boxDefPath );
                var boxNode = new BoxNode( boxDef );
                追加ノードリスト.Add( boxNode );

                // BOXノードの子ノードリストの先頭に「戻る」と「ランダムセレクト」を追加する。
                boxNode.子ノードリスト.Add( new BackNode() { 親ノード = boxNode } );
                boxNode.子ノードリスト.Add( new RandomSelectNode() { 親ノード = boxNode } );

                // このフォルダを対象として再帰的に構築する。ただし box.def は無効とする。
                // 親ノードとしてBOXノードを指定しているので、構築結果のノードはBOXノードの子として付与される。
                this._構築する( 基点フォルダパス, boxNode, boxdefが有効: false );

                // box.def があった場合、サブフォルダは検索しない。
                サブフォルダを検索する = false;
                //----------------
                #endregion
            }
            else if( File.Exists( setDefPath.変数なしパス ) )
            {
                #region " (B) このフォルダに set.def がある → その内容で任意個のノードを作成する。"
                //----------------
                Interlocked.Increment( ref this._進捗カウンタ );

                // set.def を読み込む。
                var setDef = new SetDef( setDefPath );

                // set.def 内のすべてのブロックについて、Song と SongNode を作成する。
                var list = new List<Node>( 5 );
                foreach( var block in setDef.Blocks )
                {
                    // set.def のブロックから Song を生成する。
                    var song = new Song( block, dirInfo.FullName );

                    // L1～L5が1つ以上有効なら、このブロックに対応する SongNode を生成する。
                    if( song.譜面リスト.Any( ( score ) => null != score ) )
                        list.Add( new SongNode( song ) );
                }
                // 1つ以上の SongNode がある（1つ以上の有効なブロックがある）場合のみ登録する。
                if( 0 < list.Count )
                    追加ノードリスト.AddRange( list );

                // set.def があった場合、サブフォルダは検索しない。
                サブフォルダを検索する = false;
                //----------------
                #endregion
            }
            else
            {
                #region " (C) box.def も set.def もない → このフォルダにあるすべての譜面ファイルを検索してノードを作成する。"
                //----------------
                var 対応する拡張子 = new[] { ".sstf", ".dtx", ".gda", ".g2d", "bms", "bme" };

                // 譜面ファイルを検索する。曲ファイルは、対応する拡張子を持つファイルである。
                var fileInfos = dirInfo.GetFiles( "*.*", SearchOption.TopDirectoryOnly )
                    .Where( ( fileInfo ) => 対応する拡張子.Any( 拡張子名 => ( Path.GetExtension( fileInfo.Name ).ToLower() == 拡張子名 ) ) );

                // 列挙されたすべての譜面ファイルについて……
                foreach( var fileInfo in fileInfos )
                {
                    Interlocked.Increment( ref this._進捗カウンタ );

                    var path = new VariablePath( fileInfo.FullName );
                    try
                    {
                        // 1つの譜面を持つ Song を生成する。
                        var song = new Song( path );
                        追加ノードリスト.Add( new SongNode( song ) );
                    }
                    catch( Exception e )
                    {
                        Log.ERROR( $"SongNode の生成に失敗しました。[{path.変数付きパス}][{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( e.Message )}]" );
                    }

                    // 続けて、サブフォルダも検索する。
                    サブフォルダを検索する = true;
                }
                //----------------
                #endregion
            }


            #region " 作成した一時リストを親ノードの子として正式に追加する。"
            //----------------
            foreach( var node in 追加ノードリスト )
            {
                // 親ノードの子として登録する。
                親ノード.子ノードリスト.Add( node );
                node.親ノード = 親ノード;

                // SongNode なら、全曲リストと全譜面リストにも追加する。
                if( node is SongNode snode )
                {
                    Global.App.全曲リスト.Add( snode.曲 );

                    for( int i = 0; i < 5; i++ )
                    {
                        if( null != snode.曲.譜面リスト[ i ] )
                            Global.App.全譜面リスト.Add( snode.曲.譜面リスト[ i ]! );
                    }
                }
            }
            //----------------
            #endregion


            if( サブフォルダを検索する )
            {
                // すべてのサブフォルダについて……
                foreach( var di in dirInfo.GetDirectories() )
                {
                    if( di.Name.StartsWith( "DTXFiles.", StringComparison.OrdinalIgnoreCase ) )
                    {
                        #region " (A) サブフォルダが DTXFiles. で始まるBOXである → BOXノードを追加し、サブフォルダを再帰的に検索する。"
                        //----------------
                        // BOXノードを作成し、ツリーに登録する。
                        var boxNode = new BoxNode( di.Name[ ( "DTXFiles.".Length ).. ] ) { 親ノード = 親ノード };
                        親ノード.子ノードリスト.Add( boxNode );

                        // BOXノードの先頭に「戻る」と「ランダムセレクト」を追加する。
                        boxNode.子ノードリスト.Add( new BackNode() { 親ノード = boxNode } );         // 戻る
                        boxNode.子ノードリスト.Add( new RandomSelectNode() { 親ノード = boxNode } ); // ランタムセレクト

                        // BOXノードを親として、サブフォルダを検索する。
                        this._構築する( di.FullName, boxNode );
                        //----------------
                        #endregion
                    }
                    else
                    {
                        #region " (B) それ以外 → サブフォルダを再帰検索し、その内容を同じ親ノードに追加する。"
                        //----------------
                        this._構築する( di.FullName, 親ノード );
                        //----------------
                        #endregion
                    }
                }
            }
        }
    }
}
