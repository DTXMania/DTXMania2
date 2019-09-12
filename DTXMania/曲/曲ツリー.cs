using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FDK;

namespace DTXMania
{
    /// <summary>
    ///		選曲画面で使用される、曲ツリーを管理する。
    ///		曲ツリーは、<see cref="ユーザ"/>ごとに１つずつ持つことができる。
    /// </summary>
    class 曲ツリー : IDisposable
    {

        // プロパティ


        /// <summary>
        ///		曲ツリーのルートを表すノード。
        ///		フォーカスリストやフォーカスノードも、このツリーの中に実態がある。
        /// </summary>
        public RootNode ルートノード { get; } = new RootNode();

        /// <summary>
        ///		現在選択されているノード。
        /// </summary>
        /// <remarks>
        ///		未選択またはフォーカスリストが空の場合は null 。
        ///		<see cref="フォーカスリスト"/>の<see cref="SelectableList{T}.SelectedIndex"/>で変更できる。
        ///	</remarks>
        public Node フォーカスノード
        {
            get
            {
                if( null == this.フォーカスリスト )
                    return null;    // 未設定。

                if( 0 > this.フォーカスリスト.SelectedIndex )
                    return null;    // リストが空。

                return this.フォーカスリスト[ this.フォーカスリスト.SelectedIndex ];
            }
        }

        /// <summary>
        ///     現在選択されているノードのインデックス番号（0～）を返す。
        /// </summary>
        public int フォーカスノードのインデックス
        {
            get
            {
                if( null == this.フォーカスリスト )
                    return 0;    // 未設定。

                if( 0 > this.フォーカスリスト.SelectedIndex )
                    return 0;    // リストが空。

                return this.フォーカスリスト.SelectedIndex;
            }
        }

        /// <summary>
        ///	    <see cref="フォーカスノード"/> が存在するノードリスト。
        ///	    変更するには、変更先のリスト内の任意のノードを選択すること。
        /// </summary>
        public SelectableList<Node> フォーカスリスト { get; protected set; } = null;

        /// <summary>
        ///		現在選択されているノードから曲ノードを取得して返す。
        /// </summary>
        /// <remarks>
        ///		<see cref="フォーカスノード"/>が<see cref="SetNode"/>型である場合は、それが保有する難易度の中で、
        ///		現在の <see cref="ユーザ希望難易度"/> に一番近い難易度の <see cref="MusicNode"/> が返される。
        ///		それ以外の場合は常に null が返される。
        /// </remarks>
        public MusicNode フォーカス曲ノード
        {
            get
            {
                var node = this.フォーカスノード;

                if( node is RandomSelectNode randomNode )
                    node = randomNode.選択曲;

                switch( node )
                {
                    case MusicNode musicNode:
                        return musicNode;

                    case SetNode setNode:
                        return setNode.MusicNodes[ this.フォーカス難易度 ];

                    default:
                        return null;
                }
            }
        }

        /// <summary>
        ///		現在選択されているノードが対応している、現在の <see cref="ユーザ希望難易度"/> に一番近い難易度（0:BASIC～4:ULTIMATE）を返す。
        /// </summary>
        public int フォーカス難易度
        {
            get
            {
                var node = this.フォーカスノード;

                if( node is RandomSelectNode randomNode )
                    node = randomNode.選択曲;

                switch( node )
                {
                    case MusicNode musicNode:
                        return 3;   // MASTER 相当で固定

                    case SetNode setNode:
                        return setNode.ユーザ希望難易度に最も近い難易度レベルを返す( this.ユーザ希望難易度 );

                    default:
                        return 0;   // BoxNode, BackNode, RandomSelectNode など
                }
            }
        }

        /// <summary>
        ///     ユーザが希望している難易度。
        /// </summary>
        public int ユーザ希望難易度 { get; protected set; } = 3;

        public void ユーザ希望難易度をひとつ増やす()
        {
            for( int i = 0; i < 5; i++ )   // 最大でも5回まで
            {
                this.ユーザ希望難易度 = ( this.ユーザ希望難易度 + 1 ) % 5;

                if( this.フォーカスノード is SetNode setnode )
                {
                    if( null != setnode.MusicNodes[ this.ユーザ希望難易度 ] )
                        return; // その難易度に対応する曲ノードがあればOK。
                }

                // なければ次のアンカへ。
            }
        }



        // フォーカス


        /// <summary>
        ///		指定されたノードをフォーカスする。
        ///		<see cref="フォーカスリスト"/>もそのノードのあるリストへ変更される。
        ///		現在活性化中である場合、移動前のフォーカスリストは非活性化され、新しいフォーカスリストが活性化される。
        /// </summary>
        public void フォーカスする( Node ノード )
        {
            //Debug.Assert( this.活性化している );	--> どちらの状態で呼び出してもよい。

            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                var 親ノード = ノード?.親ノード ?? this.ルートノード;
                Trace.Assert( null != 親ノード?.子ノードリスト );


                // 必要あればフォーカスリストを変更。

                var 旧フォーカスリスト = this.フォーカスリスト;    // 初回は null 。
                this.フォーカスリスト = 親ノード.子ノードリスト;   // 常に非 null。（先のAssertで保証されている。）

                if( 旧フォーカスリスト == this.フォーカスリスト )
                {
                    // (A) フォーカスリストが変わらない場合 → 必要あればフォーカスノードを変更する。

                    if( null != ノード )
                    {
                        // (A-a) ノードの指定がある（非null）なら、それを選択する。
                        this.フォーカスリスト.SelectItem( ノード );
                    }
                    else
                    {
                        // (A-b) ノードの指定がない（null）なら、フォーカスノードは現状のまま維持する。
                    }
                }
                else
                {
                    // (B) フォーカスリストが変更される場合

                    Log.Info( "フォーカスリストが変更されました。" );

                    if( null != 旧フォーカスリスト ) // 初回は null 。
                    {
                        旧フォーカスリスト.SelectionChanged -= this.フォーカスリスト_SelectionChanged;   // ハンドラ削除
                    }

                    if( null != ノード )
                        this.フォーカスリスト.SelectItem( ノード );    // イベントハンドラ登録前

                    this.フォーカスリスト.SelectionChanged += this.フォーカスリスト_SelectionChanged;   // ハンドラ登録

                    // 手動でイベントを発火。
                    this.フォーカスノードが変更された?.Invoke( this.フォーカスリスト, (this.フォーカスリスト?.SelectedItem, 旧フォーカスリスト?.SelectedItem) );
                }
            }
        }

        /// <remarks>
        ///		末尾なら先頭に戻る。
        /// </remarks>
        public void 次のノードをフォーカスする()
        {
            var index = this.フォーカスリスト.SelectedIndex;

            if( 0 > index )
                return; // 現在フォーカスされているノードがない。

            index = ( index + 1 ) % this.フォーカスリスト.Count;

            this.フォーカスリスト.SelectItem( index );
        }

        /// <remarks>
        ///		先頭なら末尾に戻る。
        /// </remarks>
        public void 前のノードをフォーカスする()
        {
            var index = this.フォーカスリスト.SelectedIndex;

            if( 0 > index )
                return; // 現在フォーカスされているノードがない。

            index = ( index - 1 + this.フォーカスリスト.Count ) % this.フォーカスリスト.Count;

            this.フォーカスリスト.SelectItem( index );
        }

        /// <summary>
        ///     フォーカスノードが変更された場合に発生するイベント。
        /// </summary>
        /// <remarks>
        ///     選択されたノードと、選択が解除されたノードは、必ずしも同じNodeリストに存在するとは限らない。
        ///     例えば、BOXを移動する場合、選択されるNodeは移動後のNodeリストに、選択が解除されたNodeは移動前のNodeリストに、それぞれ存在する。
        ///     この場合、後者はすでに非活性化されているので注意すること。
        /// </remarks>
        public event EventHandler<(Node 選択されたNode, Node 選択が解除されたNode)> フォーカスノードが変更された;

        private void フォーカスリスト_SelectionChanged( object sender, (Node 選択されたItem, Node 選択が解除されたItem) e )
        {
            // 間接呼び出し；
            // フォーカスリストの SelectedChanged イベントハンドラ　→　このクラス内で変更されうる
            // 外部に対するイベントハンドラ　→　このクラス内では変更されない
            this.フォーカスノードが変更された?.Invoke( sender, e );
        }



        // 生成と終了


        public 曲ツリー()
        {
            //this.ユーザ希望難易度 = 3;	-> 初期化せず、前回の値を継承する。

            this._ランダムセレクトノードを追加する( this.ルートノード );   // 先頭はRandomSelect
        }

        public virtual void Dispose()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this._ノードを削除する( this.ルートノード );
            }
        }

        private void _ノードを削除する( Node node )
        {
            this.フォーカスリスト = null;

            foreach( var child in node.子ノードリスト )
                this._ノードを削除する( child );

            node.子ノードリスト.Clear();
            node.Dispose();
        }



        // 曲ツリーの構築


        public void 曲ツリーを構築する( IEnumerable<VariablePath> 曲検索フォルダパスリスト )
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                // (1) 曲ツリーの構造を確定する。
                foreach( var path in 曲検索フォルダパスリスト )
                    this._曲ツリーを構築する( path, this.ルートノード );

                // (2) SongDB にレコードがあれば、MusicNode に反映する。
                using( var songdb = new SongDB() )
                {
                    foreach( var node in this.ルートノード.Traverse() )
                    {
                        if( node is MusicNode mnode )
                        {
                            var r = songdb.Songs.Where( ( song ) => ( song.Path == mnode.曲ファイルの絶対パス.変数なしパス ) ).SingleOrDefault();
                            if( null != r )
                                mnode.レコードを反映する( r );
                        }
                    }
                }
            }
        }

        private void _曲ツリーを構築する( VariablePath 基点フォルダパス, Node 親ノード, bool BoxDefが有効 = true )
        {
            if( !( Directory.Exists( 基点フォルダパス.変数なしパス ) ) )
            {
                Log.WARNING( $"指定されたフォルダが存在しません。無視します。[{基点フォルダパス.変数付きパス}]" );
                return;
            }

            // 作成したノードはいったんこのノードリストに格納し、あとでまとめて曲ツリーに登録する。
            var 追加ノードリスト = new List<Node>();

            var dirInfo = new DirectoryInfo( 基点フォルダパス.変数なしパス );
            var boxDefPath = new VariablePath( Path.Combine( 基点フォルダパス.変数なしパス, @"box.def" ) );
            var setDefPath = new VariablePath( Path.Combine( 基点フォルダパス.変数なしパス, @"set.def" ) );
            bool サブフォルダを検索する = true;

            if( BoxDefが有効 && File.Exists( boxDefPath.変数なしパス ) )
            {
                #region " (A) このフォルダに box.def がある → BOXノードを作成し、子ノードリストを作成する。"
                //----------------
                try
                {
                    // box.defを読み込んでBOXノードを作成する。
                    var boxNode = new BoxNode( boxDefPath );
                    boxNode.子ノードリスト.Add( new BackNode( boxNode ) );         // 戻る
                    boxNode.子ノードリスト.Add( new RandomSelectNode( boxNode ) ); // RandomSelect
                    追加ノードリスト.Add( boxNode );

                    // box.defを無効にして、このフォルダを対象として、再度構築する。
                    // 構築結果のノードリストは、BOXノードの子として付与される。
                    this._曲ツリーを構築する( 基点フォルダパス, boxNode, BoxDefが有効: false );

                    // box.def があった場合、サブフォルダは検索しない。
                    サブフォルダを検索する = false;
                }
                catch
                {
                    Log.ERROR( $"box.def に対応するノードの生成に失敗しました。[{setDefPath.変数付きパス}]" );
                }
                //----------------
                #endregion
            }
            else if( File.Exists( setDefPath.変数なしパス ) )
            {
                #region " (B) このフォルダに set.def がある → その内容でSetノード（任意個）を作成する。"
                //----------------
                try
                {
                    // set.def を読み込む。
                    var setDef = SetDef.復元する( setDefPath );

                    // set.def 内のすべてのブロックについて、SetNodeを作成する。
                    foreach( var block in setDef.Blocks )
                    {
                        // １つのブロックにつき１つの SetNode を作成する。
                        var setNode = new SetNode( block, 基点フォルダパス );

                        if( 0 < setNode.子ノードリスト.Count ) // L1～L5のいずれかが有効であるときのみ登録する。
                            追加ノードリスト.Add( setNode );
                        else
                            setNode?.Dispose();
                    }

                    // set.def があった場合、サブフォルダは検索しない。
                    サブフォルダを検索する = false;
                }
                catch
                {
                    Log.ERROR( $"set.def からのノードの生成に失敗しました。[{setDefPath.変数付きパス}]" );
                }
                //----------------
                #endregion
            }
            else
            {
                #region " (C) このフォルダにあるすべての曲ファイルを検索して、曲ノードを作成する。"
                //----------------
                // 対応する拡張子を持つファイルを列挙する。
                var fileInfos = dirInfo.GetFiles( "*.*", SearchOption.TopDirectoryOnly )
                    .Where( ( fileInfo ) => _対応する拡張子.Any( 拡張子名 => ( Path.GetExtension( fileInfo.Name ).ToLower() == 拡張子名 ) ) );

                // 列挙されたそれぞれのファイルについて……
                foreach( var fileInfo in fileInfos )
                {
                    var vpath = new VariablePath( fileInfo.FullName );
                    try
                    {
                        // MusicNodeを作成し、追加する。
                        var music = new MusicNode( vpath );
                        追加ノードリスト.Add( music );
                    }
                    catch
                    {
                        Log.ERROR( $"MusicNode の生成に失敗しました。[{vpath.変数付きパス}]" );
                    }

                    // 続けて、サブフォルダも検索する。
                    サブフォルダを検索する = true;
                }
                //----------------
                #endregion
            }

            #region " (D) 作成したノードリストを親ノードの子として追加する。"
            //----------------
            foreach( var item in 追加ノードリスト )
            {
                item.親ノード = 親ノード;
                親ノード.子ノードリスト.Add( item );
            }
            //----------------
            #endregion

            if( サブフォルダを検索する )
            {
                #region " (E) サブフォルダを検索する。"
                //----------------
                foreach( var dir in dirInfo.GetDirectories() )
                {
                    if( dir.Name.StartsWith( "DTXFiles.", StringComparison.OrdinalIgnoreCase ) )
                    {
                        #region " (E-a) サブフォルダがBOXである → BOXノードを追加し、サブフォルダを再帰検索する。"
                        //----------------
                        // BOXノードを作成し、ツリーに登録する。
                        var boxNode = new BoxNode( dir.Name.Substring( 9 ), 親ノード );
                        boxNode.子ノードリスト.Add( new BackNode( boxNode ) );         // 戻る
                        boxNode.子ノードリスト.Add( new RandomSelectNode( boxNode ) ); // RandomSelect
                        親ノード.子ノードリスト.Add( boxNode );

                        // BOXノードを親として、サブフォルダを検索する。
                        this._曲ツリーを構築する( dir.FullName, boxNode );
                        //----------------
                        #endregion
                    }
                    else
                    {
                        #region " (E-c) それ以外 → サブフォルダの内容を同じ親ノードに追加する。"
                        //----------------
                        this._曲ツリーを構築する( dir.FullName, 親ノード );
                        //----------------
                        #endregion
                    }
                }
                //----------------
                #endregion
            }
        }

        /// <summary>
        ///     指定された親ノードの末尾に、RandomSelct ノードを追加する。
        /// </summary>
        private void _ランダムセレクトノードを追加する( Node 親ノード )
        {
            親ノード.子ノードリスト.Add( new RandomSelectNode( 親ノード ) );
        }

        private string[] _対応する拡張子 = { ".sstf", ".dtx", ".gda", ".g2d", "bms", "bme" };


        // 曲ツリーの現行化


        public Task 現行化タスク { get; protected set; } = null;

        public CancellationTokenSource 現行化タスクキャンセル通知 { get; protected set; } = new CancellationTokenSource();

        /// <summary>
        ///     ON の間は現行化タスクを一時停止する。
        ///     OFF にすると再開する。
        /// </summary>
        public TriStateEvent 現行化タスクの一時停止 { get; protected set; } = new TriStateEvent( TriStateEvent.状態種別.OFF );


        public async void 曲ツリーを現行化するAsync( Action<Exception> 例外通知 )
        {
            this.現行化タスク = Task.Run( () => {

                try
                {
                    Log.現在のスレッドに名前をつける( "現行化" );
                    Log.Info( "曲ツリーの現行化を開始します。" );

                    // すべての MusicNode の現行化フラグと成績をリセットする。
                    foreach( var node in this.ルートノード.Traverse() )   // SetNode.MusicNodes[] も展開される。
                    {
                        if( node is MusicNode mnode )
                        {
                            mnode.現行化未実施 = true;
                            mnode.達成率 = null;
                        }
                    }

                    // すべてのMusicNodeを現行化する。
                    int musicNode数 = 0;
                    var timer = Stopwatch.StartNew();
                    using( var recorddb = new RecordDB() )
                    using( var songdb = new SongDB() )
                    using( var songdb_trans = songdb.Connection.BeginTransaction() )
                    {
                        foreach( var node in this.ルートノード.Traverse() )   // SetNode.MusicNodes[] も展開される。
                        {
                            if( node is MusicNode music )
                            {
                                // DataContext は軽量なので、使い捨てにする。
                                // DataContext.SubmitChanges() の回数が増えるとどんどん重くなるので注意。
                                songdb.ResetDataContext();

                                music.現行化する( songdb, recorddb );
                                musicNode数++;

                                songdb.DataContext.SubmitChanges();
                            }

                            // キャンセル？
                            if( this.現行化タスクキャンセル通知.IsCancellationRequested )
                            {
                                Log.Info( "曲ツリーの現行化タスクのキャンセルが要請されました。" );
                                break;
                            }

                            // 一時停止？
                            this.現行化タスクの一時停止.OFFになるまでブロックする();
                        }

                        songdb_trans.Commit();
                    }

                    int 曲数 = this.ルートノード.Traverse().Count( ( n ) => ( n is SetNode || ( n is MusicNode && !( n.親ノード is SetNode ) ) ) ); // MusicNode と SetNode（MusicNodes[]含まない）の数
                    Log.Info( $"曲ツリーの現行化を終了します。({曲数}曲, {musicNode数}譜面, {timer.ElapsedMilliseconds / 1000.0}秒)" );
                }
                catch( Exception e )
                {
                    例外通知( e );
                }

            }, this.現行化タスクキャンセル通知.Token );

            await this.現行化タスク;
        }

        public void 曲ツリーの現行化をキャンセルする()
        {
            if( null != this.現行化タスク && !this.現行化タスク.IsCompleted )
            {
                Log.Info( "曲ツリーの現行化タスクをキャンセルします。" );

                this.現行化タスクキャンセル通知.Cancel();

                if( !this.現行化タスク.Wait( 5000 ) )
                    Log.ERROR( "曲ツリーの現行化タスクのキャンセルがタイムアウトしました。" );
                else
                    Log.Info( "曲ツリーの現行化タスクをキャンセルしました。" );
            }
        }
    }
}
