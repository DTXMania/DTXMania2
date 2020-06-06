using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.DirectWrite;
using Microsoft.Data.Sqlite;
using FDK;

namespace DTXMania2.曲
{
    /// <summary>
    ///     <see cref="SongNode"/> を現行化する。
    /// </summary>
    /// <remarks>
    ///     現行化とは、仮の初期状態から、最新の完全な状態に更新する処理である。
    ///     具体的には、DB から作成された <see cref="SongNode"/> の情報を実際のファイルが持つ
    ///     最新の情報と比較して DB を更新したり、他の DB からデータを読み込んだり、
    ///     <see cref="SongNode"/> が持つ画像を生成したりする。
    /// </remarks>
    class 現行化
    {

        // プロパティ


        /// <summary>
        ///     現在、現行化処理中なら true, 停止中なら false。
        /// </summary>
        public bool 現行化中 => this._現行化中.IsSet;



        // 開始と終了


        /// <summary>
        ///     指定された曲ツリーの現行化を開始する。
        /// </summary>
        /// <param name="root">曲ツリーのルートノードのリスト。</param>
        public void 開始する( IEnumerable<RootNode> roots, ユーザ設定 userConfig )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            // 全ノードをスタックに投入。
            this._現行化待ちスタック.Clear();
            foreach( var root in roots )
                foreach( var node in root.Traverse() )
                    this._現行化待ちスタック.Push( node );

            this._現行化を開始する( userConfig );
        }

        /// <summary>
        ///     現行化待ちスタックにノードリストを追加する。
        /// </summary>
        /// <remarks>
        ///     現行化待ちスタックは LIFO なので、投入されたノードリストは優先的に現行化される。
        /// </remarks>
        /// <param name="nodeList">追加するノードリスト。</param>
        public async Task 追加するAsync( ICollection<Node> nodeList )
        {
            //using var _ = new LogBlock( Log.現在のメソッド名 );
            
            await Task.Run( () => {

                if( nodeList is Node[] nodeArray )
                {
                    this._現行化待ちスタック.PushRange( nodeArray );
                }
                else
                {
                    var _nodeArray = new Node[ nodeList.Count ];
                    nodeList.CopyTo( _nodeArray, 0 );

                    this._現行化待ちスタック.PushRange( _nodeArray );
                }
            } );
        }

        /// <summary>
        ///     現行化処理を一時停止する。
        /// </summary>
        public void 一時停止する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._タスク再開通知.Reset();      // 順番注意
            this._タスク一時停止通知.Set();    //
        }

        /// <summary>
        ///     一時停止している現行化処理を再開する。
        /// </summary>
        public void 再開する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._タスク一時停止通知.Reset();    // 順番注意
            this._タスク再開通知.Set();          //
        }

        /// <summary>
        ///     現行化タスクを終了する。
        /// </summary>
        public void 終了する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            if( this._現行化タスク is null )
                return;

            if( this._現行化タスク.IsCompleted )
            {
                this._現行化タスク.Dispose();
                this._現行化タスク = null;
                return;
            }

            this._タスク再開通知.Set();    // 終了通知より先に再開を通知する。
            this._タスク終了通知.Set();

            if( !this._現行化タスク.Wait( 5000 ) )
                throw new Exception( "現行化タスク終了待ちがタイムアウトしました。" );

            this._現行化タスク.Dispose();
            this._現行化タスク = null;
        }

        /// <summary>
        ///     指定されたツリーの全譜面のユーザ依存の現行化フラグをリセットする。
        /// </summary>
        public void リセットする( IEnumerable<RootNode> roots, ユーザ設定 userConfig )
        {
            foreach( var root in roots )
            {
                foreach( var node in root.Traverse() )
                {
                    if( node is SongNode snode )
                    {
                        snode.現行化済み = false;

                        foreach( var score in snode.曲.譜面リスト )
                        {
                            if( score is null )
                                continue;
                            //score.譜面と画像を現行化済み = false;    --> ユーザに依存しないので現状維持
                            score.最高記録 = null;
                            score.最高記録を現行化済み = false;
                            score.譜面の属性 = null;
                            score.譜面の属性を現行化済み = false;
                        }
                    }
                }
            }
        }

        public void すべての譜面について属性を現行化する( string userID )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            int 属性取得数 = 0;
            using var scorepropdb = new ScorePropertiesDB();
            using var cmd = new SqliteCommand( "SELECT * FROM ScoreProperties WHERE UserId = @UserId", scorepropdb.Connection );
            cmd.Parameters.AddRange( new[] {
                new SqliteParameter( "@UserId", userID ),
            } );
            var result = cmd.ExecuteReader();
            while( result.Read() )
            {
                var prop = new ScorePropertiesDBRecord( result );
                var scores = Global.App.全譜面リスト.Where( ( s ) => s.譜面.ScorePath == prop.ScorePath );
                foreach( var score in scores )
                {
                    score.譜面の属性 = prop;
                    score.譜面の属性を現行化済み = true;
                    属性取得数++;
                }
            }
            Log.Info( $"{属性取得数} 件の属性を更新しました。" );
        }


        // 生成(static)


        public static 文字列画像D2D? タイトル文字列画像を生成する( string タイトル文字列 )
        {
            try
            {
                var image = new 文字列画像D2D() {
                    表示文字列 = タイトル文字列,
                    フォント名 = "HGMaruGothicMPRO", // "メイリオ",
                    フォントの太さ = FontWeight.Regular,
                    フォントスタイル = FontStyle.Normal,
                    フォントサイズpt = 40f,
                    描画効果 = 文字列画像D2D.効果.縁取り,
                    縁のサイズdpx = 6f,
                    前景色 = Color4.Black,
                    背景色 = Color4.White,
                };

                //Log.Info( $"タイトル画像を生成しました。[{タイトル文字列}]" );
                return image;
            }
            catch( Exception e )
            {
                Log.ERROR( $"タイトル画像の生成に失敗しました。[{タイトル文字列}][{e.Message}]" );
                return null;
            }
        }

        public static 文字列画像D2D? サブタイトル文字列画像を生成する( string? サブタイトル文字列 )
        {
            if( string.IsNullOrEmpty( サブタイトル文字列 ) )
                return null;

            try
            {
                var image = new 文字列画像D2D() {
                    表示文字列 = サブタイトル文字列,
                    フォント名 = "HGMaruGothicMPRO", // "メイリオ",
                    フォントの太さ = FontWeight.Regular,
                    フォントスタイル = FontStyle.Normal,
                    フォントサイズpt = 20f,
                    描画効果 = 文字列画像D2D.効果.縁取り,
                    縁のサイズdpx = 4f,
                    前景色 = Color4.Black,
                    背景色 = Color4.White,
                };

                //Log.Info( $"サブタイトル画像を生成しました。[{サブタイトル文字列}]" );
                return image;
            }
            catch( Exception e )
            {
                Log.ERROR( $"サブタイトル画像の生成に失敗しました。[{サブタイトル文字列}][{e.Message}]" );
                return null;
            }
        }

        public static 画像? ノード画像を生成する( VariablePath? ノード画像ファイルの絶対パス )
        {
            if( ノード画像ファイルの絶対パス is null )
                return null;

            try
            {
                var image = new 画像( ノード画像ファイルの絶対パス.変数なしパス );

                //Log.Info( $"ノード画像を生成しました。[{ノード画像ファイルの絶対パス.変数付きパス}]" );
                return image;
            }
            catch( Exception e )
            {
                Log.ERROR( $"ノード画像の生成に失敗しました。[{ノード画像ファイルの絶対パス.変数付きパス}][{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( e.Message )}]" );
                return null;
            }
        }



        // ローカル


        private ConcurrentStack<Node> _現行化待ちスタック = new ConcurrentStack<Node>();

        private Task? _現行化タスク = null;

        private ManualResetEventSlim _タスク一時停止通知 = new ManualResetEventSlim();

        private ManualResetEventSlim _タスク再開通知 = new ManualResetEventSlim();

        private ManualResetEventSlim _タスク終了通知 = new ManualResetEventSlim();

        private ManualResetEventSlim _現行化中 = new ManualResetEventSlim( false );

        private void _現行化を開始する( ユーザ設定 userConfig )
        {
            this._タスク終了通知.Reset();

            this._現行化タスク = Task.Run( () => {

                //Log.現在のスレッドに名前をつける( "現行化" );  --> await 後にワーカスレッドが変わることがある
                Log.Info( "現行化タスクを開始しました。" );

                while( !this._タスク終了通知.IsSet )
                {
                    if( this._タスク一時停止通知.IsSet )
                        this._タスク再開通知.Wait();

                    if( this._現行化待ちスタック.TryPop( out var node ) )
                    {
                        this._現行化中.Set();
                        this._ノードを現行化する( node, userConfig );
                    }
                    else
                    {
                        // スタックが空だったら少し待機。
                        this._現行化中.Reset();
                        Thread.Sleep( 100 );
                    }
                }

                Log.Info( "現行化タスクを終了しました。" );

            } );
        }

        private void _ノードを現行化する( Node node, ユーザ設定 userConfig )
        {
            #region " 1. ノードが持つ譜面の現行化 "
            //----------------
            if( node is SongNode snode )
            {
                using var scoredb = new ScoreDB();

                using( var cmd = new SqliteCommand( "BEGIN", scoredb.Connection ) )
                    cmd.ExecuteNonQuery();

                // すべての譜面について……

                for( int i = 0; i < 5; i++ )
                {
                    var score = snode.曲.譜面リスト[ i ];
                    if( score is null )
                        continue;

                    if( !File.Exists( score.譜面.ScorePath ) )
                    {
                        Log.ERROR( $"ファイルが存在しません。[{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( score.譜面.ScorePath )}]" );
                        snode.曲.譜面リスト[ i ]!.Dispose();
                        snode.曲.譜面リスト[ i ] = null;
                        continue;
                    }

                    // 1.1. 譜面と画像 の現行化

                    if( !score.譜面と画像を現行化済み )
                    {
                        #region " ファイルの更新を確認し、ScoreDB と score を現行化する。"
                        //----------------
                        try
                        {
                            using var query = new SqliteCommand( "SELECT * FROM Scores WHERE ScorePath = @ScorePath", scoredb.Connection );
                            query.Parameters.Add( new SqliteParameter( "@ScorePath", score.譜面.ScorePath ) );
                            var result = query.ExecuteReader();
                            if( !result.Read() )
                            {
                                // (A) ScoreDB に既存のレコードがない場合

                                #region " ScoreDBの レコードを新規追加し score を更新する。"
                                //----------------
                                var レコード = new ScoreDBRecord( score.譜面.ScorePath, userConfig );

                                // レコードを ScoreDB に新規追加する。
                                レコード.ReplaceTo( scoredb );

                                // score にも反映する。
                                score.譜面.UpdateFrom( レコード );

                                // 完了。
                                Log.Info( $"ScoreDBに曲を追加しました。{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( score.譜面.ScorePath )}" );
                                //----------------
                                #endregion
                            }
                            else
                            {
                                // (B) ScoreDB に既存のレコードがある場合

                                var レコード = new ScoreDBRecord( result );
                                string 譜面ファイルの最終更新日時 = File.GetLastWriteTime( score.譜面.ScorePath ).ToString( "G" );

                                if( レコード.LastWriteTime != 譜面ファイルの最終更新日時 )
                                {
                                    #region " (B-a) 譜面ファイルの最終更新日時が更新されている → ScoreDB のレコードと score を更新する。"
                                    //----------------
                                    レコード = new ScoreDBRecord( score.譜面.ScorePath, userConfig );

                                    // ScoreDB のレコードを置換する。
                                    レコード.ReplaceTo( scoredb );

                                    // score にも反映する。
                                    score.譜面.UpdateFrom( レコード );

                                    // 完了。
                                    Log.Info( $"最終更新日時が変更されているため、曲の情報を更新しました。{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( score.譜面.ScorePath )}" );
                                    //----------------
                                    #endregion
                                }
                                else
                                {
                                    #region " (B-b) それ以外 → 何もしない "
                                    //----------------
                                    //----------------
                                    #endregion
                                }
                            }
                        }
                        catch( Exception e )
                        {
                            Log.ERROR( $"譜面の現行化に失敗しました。[{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( e.Message )}][{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( score.譜面.ScorePath )}]" );
                            // 続行
                        }
                        //----------------
                        #endregion

                        #region " 画像を現行化する。"
                        //----------------
                        try
                        {
                            if( score.タイトル文字列画像 is null || score.タイトル文字列画像.表示文字列 != score.譜面.Title )
                            {
                                score.タイトル文字列画像?.Dispose();
                                score.タイトル文字列画像 = タイトル文字列画像を生成する( score.譜面.Title );
                            }
                            if( score.サブタイトル文字列画像 is null || score.サブタイトル文字列画像.表示文字列 != score.譜面.Artist )
                            {
                                score.サブタイトル文字列画像?.Dispose();
                                score.サブタイトル文字列画像 = サブタイトル文字列画像を生成する( string.IsNullOrEmpty( score.譜面.Artist ) ? null : score.譜面.Artist );
                            }
                            score.プレビュー画像?.Dispose();
                            score.プレビュー画像 = ノード画像を生成する(
                                string.IsNullOrEmpty( score.譜面.PreImage ) ?
                                null :
                                new VariablePath( Path.Combine( Path.GetDirectoryName( score.譜面.ScorePath ) ?? @"\", score.譜面.PreImage ) ) );
                        }
                        catch( Exception e )
                        {
                            Log.ERROR( $"譜面画像の現行化に失敗しました。[{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( e.Message )}][{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す(score.譜面.ScorePath)}]" );
                            // 続行
                        }
                        //----------------
                        #endregion

                        // 現行化の成否によらず完了。
                        score.譜面と画像を現行化済み = true;
                    }

                    // 1.2. 属性 の現行化

                    if( !score.譜面の属性を現行化済み )
                    {
                        #region " 譜面の属性を現行化する。"
                        //----------------
                        try
                        {
                            using var scorepropdb = new ScorePropertiesDB();
                            using var cmd = new SqliteCommand( "SELECT * FROM ScoreProperties WHERE ScorePath = @ScorePath AND UserId = @UserId", scorepropdb.Connection );
                            cmd.Parameters.AddRange( new[] {
                                new SqliteParameter( "@ScorePath", score.譜面.ScorePath ),
                                new SqliteParameter( "@UserId", userConfig.ID ),
                            } );
                            var result = cmd.ExecuteReader();
                            if( result.Read() )
                            {
                                score.譜面の属性 = new ScorePropertiesDBRecord( result );

                                Log.Info( $"譜面の属性を現行化しました。[{score.譜面.ScorePath}]" );
                            }
                        }
                        catch( Exception e )
                        {
                            Log.ERROR( $"譜面の属性の現行化に失敗しました。[{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( e.Message )}][{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( score.譜面.ScorePath )}]" );
                            // 続行
                        }

                        // 現行化の成否によらず完了。
                        score.譜面の属性を現行化済み = true;
                        //----------------
                        #endregion
                    }

                    // 1.3. 最高記録 の現行化

                    if( !score.最高記録を現行化済み )
                    {
                        #region " 最高記録を現行化する。"
                        //----------------
                        try
                        {
                            using var recorddb = new RecordDB();
                            using var cmd = new SqliteCommand( "SELECT * FROM Records WHERE ScorePath = @ScorePath AND UserId = @UserId", recorddb.Connection );
                            cmd.Parameters.AddRange( new[] {
                                new SqliteParameter( "@ScorePath", score.譜面.ScorePath ),
                                new SqliteParameter( "@UserId", userConfig.ID ),
                            } );
                            var result = cmd.ExecuteReader();
                            if( result.Read() )
                            {
                                score.最高記録 = new RecordDBRecord( result );

                                Log.Info( $"最高記録を現行化しました。[{score.譜面.ScorePath}]" );
                            }
                        }
                        catch( Exception e )
                        {
                            Log.ERROR( $"最高記録の現行化に失敗しました。[{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( e.Message )}][{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( score.譜面.ScorePath )}]" );
                            // 続行
                        }

                        // 現行化の成否によらず完了。
                        score.最高記録を現行化済み = true;
                        //----------------
                        #endregion
                    }
                }

                using( var cmd = new SqliteCommand( "END", scoredb.Connection ) )
                    cmd.ExecuteNonQuery();
            }
            //----------------
            #endregion

            #region " 2. ノードの現行化 "
            //----------------
            if( !node.現行化済み )
            {
                if( node is SongNode )
                {
                    // SongNode は生成不要。
                    // → App.全譜面リスト の構築時に、タイトル文字列画像とサブタイトル文字列画像だけ先に生成済み。
                }
                else
                {
                    // SongNode 以外は生成する。
                    node.タイトル文字列画像 = タイトル文字列画像を生成する( node.タイトル );
                    node.サブタイトル文字列画像 = サブタイトル文字列画像を生成する( node.サブタイトル );
                    node.ノード画像 = ノード画像を生成する( node.ノード画像ファイルの絶対パス );
                }

                // 生成の成否によらず完了。
                node.現行化済み = true;
            }
            //----------------
            #endregion
        }
    }
}
