using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharpDX;
using FDK;
using SSTFormat.v004;
using DTXMania2.曲;

namespace DTXMania2
{
    /// <summary>
    ///     アプリケーションの進行描画（とそのタスク）。
    /// </summary>
    class App : IDisposable
    {

        // プロパティ


        public Random 乱数 { get; }

        public SystemConfig システム設定 { get; set; }

        public システムサウンド システムサウンド { get; }

        public ドラムサウンド ドラムサウンド { get; }

        public ドラム入力 ドラム入力 { get; protected set; } = null!;

        public SoundDevice サウンドデバイス { get; protected set; } = null!;

        public SoundTimer サウンドタイマ { get; protected set; } = null!;

        public アイキャッチ管理 アイキャッチ管理 { get; protected set; } = null!;

        public SelectableList<ユーザ設定> ユーザリスト { get; }

        public ユーザ設定 ログオン中のユーザ => this.ユーザリスト.SelectedItem!;

        public List<Score> 全譜面リスト { get; }

        public List<Song> 全曲リスト { get; }

        /// <summary>
        ///     曲ツリーのリスト。選曲画面の「表示方法選択パネル」で変更できる。<br/>
        ///     [0]全曲、[1]評価順 で固定。
        /// </summary>
        public SelectableList<曲ツリー> 曲ツリーリスト { get; }

        public 現行化 現行化 { get; }

        public CacheStore<CSCore.ISampleSource> WAVキャッシュ { get; protected set; } = null!;

        public IStage? ステージ { get; protected set; } = null;



        // 演奏ごとのプロパティ


        /// <summary>
        ///     演奏する譜面。<see cref="App.演奏スコア"/>の生成元。
        ///     選曲ステージで選曲確定後に更新される。
        /// </summary>
        /// <remarks>
        ///     <see cref="RandomSelectNode"/> がフォーカスされている場合は、ここには譜面がランダムに設定されるため
        ///     <see cref="曲ツリー.フォーカスノード"/> の譜面とは必ずしも一致しないので注意。
        /// </remarks>
        public Score 演奏譜面 { get; set; } = null!;

        /// <summary>
        ///     現在演奏中のスコア。
        ///     曲読み込みステージで<see cref="App.演奏譜面"/>を読み込んで生成される。
        /// </summary>
        public スコア 演奏スコア { get; set; } = null!;

        /// <summary>
        ///     <see cref="App.演奏スコア"/> に対応して生成されたWAVサウンドインスタンスの管理。
        /// </summary>
        public WAV管理 WAV管理 { get; set; } = null!;

        /// <summary>
        ///     <see cref="App.演奏スコア"/>  に対応して生成されたAVI動画インスタンスの管理。
        /// </summary>
        public AVI管理 AVI管理 { get; set; } = null!;



        // 生成と終了


        /// <summary>
        ///     コンストラクタ。一部のグローバルリソースを初期化する。
        /// </summary>
        /// <remarks>
        ///     アプリの起動直後の沈黙期間を短縮するために、コンストラクタでは必要最低限のグローバルリソースだけを生成し、
        ///     残りは <see cref="App.グローバルリソースを作成する()"/> メソッドで生成する。
        /// </remarks>
        public App()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            SystemConfig.最新版にバージョンアップする();
            UserConfig.最新版にバージョンアップする();

            this.乱数 = new Random( DateTime.Now.Millisecond );
            this.システム設定 = SystemConfig.読み込む();
            this.ユーザリスト = new SelectableList<ユーザ設定>();
            this.全譜面リスト = new List<Score>();
            this.全曲リスト = new List<Song>();
            this.曲ツリーリスト = new SelectableList<曲ツリー>();
            this.現行化 = new 現行化();

            #region " リソース関連のフォルダ変数を更新する。"
            //----------------
            {
                // DrumSounds
                var drumSounds = this.システム設定.DrumSoundsFolder;
                Folder.フォルダ変数を追加または更新する( "DrumSounds", ( Path.IsPathRooted( drumSounds.変数なしパス ) ) ? 
                    drumSounds.変数なしパス :
                    new VariablePath( @$"$(ResourcesRoot)\{drumSounds}" ).変数なしパス );
                Log.Info( $"DrumSounds folder: {new VariablePath( Folder.フォルダ変数の内容を返す( "DrumSounds" ) ).変数付きパス}" );

                // SystemSounds
                var systemSounds = this.システム設定.SystemSoundsFolder;
                Folder.フォルダ変数を追加または更新する( "SystemSounds", ( Path.IsPathRooted( systemSounds.変数なしパス ) ) ?
                    systemSounds.変数なしパス :
                    new VariablePath( @$"$(ResourcesRoot)\{systemSounds}" ).変数なしパス );
                Log.Info( $"SystemSounds folder: {new VariablePath( Folder.フォルダ変数の内容を返す( "SystemSounds" ) ).変数付きパス}" );

                // Images
                var images = this.システム設定.ImagesFolder;
                Folder.フォルダ変数を追加または更新する( "Images", ( Path.IsPathRooted( images.変数なしパス ) ) ?
                    images.変数なしパス :
                    new VariablePath( @$"$(ResourcesRoot)\{images}" ).変数なしパス );
                Log.Info( $"Images folder: {new VariablePath( Folder.フォルダ変数の内容を返す( "Images" ) ).変数付きパス}" );
            }
            //----------------
            #endregion

            this.サウンドデバイス = new SoundDevice( CSCore.CoreAudioAPI.AudioClientShareMode.Shared );
            // マスタ音量（小:0～1:大）... 0.5を超えるとだいたいWASAPI共有モードのリミッターに抑制されるようになる
            // ※サウンドデバイスの音量プロパティはコンストラクタの実行後でないと set できないので、初期化子にはしないこと。（した場合の挙動は不安定）
            this.サウンドデバイス.音量 = 0.5f;
            this.サウンドタイマ = new SoundTimer( this.サウンドデバイス );
            this.システムサウンド = new システムサウンド( this.サウンドデバイス );  // 個々のサウンドの生成は後工程で。
            this.ドラムサウンド = new ドラムサウンド( this.サウンドデバイス );      // 　　　　　　〃
        }

        /// <summary>
        ///     残りのグローバルリソースを初期化する。
        /// </summary>
        /// <remarks>
        ///     アプリの起動直後の沈黙期間を短縮するために、コンストラクタでは必要最低限のグローバルリソースだけを生成し、
        ///     残りはこのメソッドで生成する。
        /// </remarks>
        public void グローバルリソースを作成する()
        {
            this.ドラム入力 = new ドラム入力( Global.AppForm.KeyboardHID, Global.AppForm.GameControllersHID, Global.AppForm.MidiIns );
            this.WAVキャッシュ = new CacheStore<CSCore.ISampleSource>() {
                ファイルからデータを生成する = ( path ) => SampleSourceFactory.Create( Global.App.サウンドデバイス, path, Global.App.ログオン中のユーザ.再生速度 ),
            };

            #region " ユーザリストにユーザを登録する。"
            //----------------
            {
                var userYamls = Directory.EnumerateFiles( Folder.フォルダ変数の内容を返す( "AppData" ), @"User_*.yaml", SearchOption.TopDirectoryOnly );
                int headLength = "User_".Length;
                int tailLength = ".yaml".Length;

                foreach( var userYaml in userYamls )
                {
                    // ファイル名からユーザIDを抽出。
                    var fname = Path.GetFileName( userYaml );
                    var userId = fname[ headLength..(fname.Length - tailLength) ];

                    var userConfig = ユーザ設定.読み込む( userId );
                    this.ユーザリスト.Add( userConfig );
                }
            }
            //----------------
            #endregion

            // 各DBを最新版にバージョンアップする。
            ScoreDB.最新版にバージョンアップする();
            RecordDB.最新版にバージョンアップする();
            ScorePropertiesDB.最新版にバージョンアップする();
        }

        /// <summary>
        ///     グローバルリソースを破棄する。
        /// </summary>
        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            //this.現行化.終了する();  --> Globalが破棄されるより前に実行する必要があるので、進行描画のメインループの終了箇所（Globalが破棄されるところ）に移動。

            this.ステージ?.Dispose();  // 進行描画メインループ終了時にDispose済みだが念のため
            this.ステージ = null;      // 

            foreach( var song in this.全曲リスト )
                song.Dispose(); // 全譜面リストもここで解放される。

            foreach( var tree in this.曲ツリーリスト )
                tree.Dispose();

            this.ドラムサウンド.Dispose();
            this.システムサウンド.Dispose();
            this.サウンドタイマ.Dispose();
            this.サウンドデバイス.Dispose();
            this.WAVキャッシュ?.Dispose();    // WAVキャッシュの破棄は最後に。

            this.システム設定.保存する();
        }

        /// <summary>
        ///     進行描画タスクを生成し、進行描画処理ループを開始する。
        /// </summary>
        public void 進行描画タスクを開始する( Size2F 設計画面サイズ, Size2F 物理画面サイズ )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            Task.Run( () => {

                Log.現在のスレッドに名前をつける( "進行描画" );
                this._進行描画タスクのNETスレッドID = Thread.CurrentThread.ManagedThreadId;

                this._進行描画のメインループを実行する( 設計画面サイズ, 物理画面サイズ );

            } );
        }

        /// <summary>
        ///     進行描画処理を終了し、タスクを終了する。
        /// </summary>
        public void 進行描画タスクを終了する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            // 進行描画タスクに終了を指示する。
            var msg = new TaskMessage(
                宛先: TaskMessage.タスク名.進行描画,
                内容: TaskMessage.内容名.終了指示 );

            // 進行描画タスクからの完了通知を待つ。
            if( Thread.CurrentThread.ManagedThreadId != this._進行描画タスクのNETスレッドID )   // ハングアップ回避; 念のため
            {
                if( !Global.TaskMessageQueue.Post( msg ).Wait( 5000 ) )
                    throw new Exception( "進行描画タスクの終了がタイムアウトしました。" );
            }
            else
            {
                Log.WARNING( "進行描画タスクから呼び出されました。完了通知待ちをスキップします。" );
            }
        }



        // 進行と描画


        /// <summary>
        ///     進行描画処理の初期化、メインループ、終了処理を行う。
        ///     <see cref="TaskMessage"/>で終了が指示されるまで、このメソッドからは戻らない。
        /// </summary>
        private void _進行描画のメインループを実行する( Size2F 設計画面サイズ, Size2F 物理画面サイズ )
        {
            try
            {
                #region " 初期化する。"
                //----------------
                this._パイプラインサーバを起動する();

                #region " スレッドプールのオンラインスレッド数を変更する。"
                //----------------
                const int 希望オンラインスレッド数 = 32;

                // 既定の数はCPUコア数に同じ。.NET の仕様により、Taskの同時利用数がこの数を超えると、それ以降の Task.Run での起動には最大2回/秒もの制限がかかる。
                ThreadPool.GetMaxThreads( out int workMax, out int compMax );
                ThreadPool.GetMinThreads( out int workMin, out int compMin );

                ThreadPool.SetMinThreads(
                    Math.Clamp( 希望オンラインスレッド数, min: workMin, max: workMax ),     // workMin ～ workMax の範囲を越えない
                    Math.Clamp( 希望オンラインスレッド数, min: compMin, max: compMax ) );   // compMin ～ compMax の範囲を越えない
                //----------------
                #endregion

                QueueTimer timer;
                AutoResetEvent tick通知;

                using( new LogBlock( "進行描画タスクの開始" ) )
                {
                    // グローバルリソースの大半は、進行描画タスクの中で生成する。
                    Global.生成する( 設計画面サイズ, 物理画面サイズ );
                    画像.全インスタンスで共有するリソースを作成する( Global.D3D11Device1 );

                    this.アイキャッチ管理 = new アイキャッチ管理();

                    // 1ms ごとに進行描画ループを行うよう仕込む。
                    tick通知 = new AutoResetEvent( false );
                    timer = new QueueTimer( 1, 1, () => tick通知.Set() );   // 1ms ごとに tick通知を set する
                }

                var スワップチェーン表示タスク = new PresentSwapChainVSync();
                TaskMessage? 終了指示メッセージ = null;

                // 最初のステージを生成する。
                Log.Header( "起動ステージ" );
                this.ステージ = new 起動.起動ステージ();
                //----------------
                #endregion

                Log.Info( "進行描画ループを開始します。" );

                while( tick通知.WaitOne() )     // Tick 通知が来るまで待機。
                {
                    #region " 自分宛のメッセージが届いていたら、すべて処理する。"
                    //----------------
                    foreach( var msg in Global.TaskMessageQueue.Get( TaskMessage.タスク名.進行描画 ) )
                    {
                        switch( msg.内容 )
                        {
                            case TaskMessage.内容名.終了指示:
                                終了指示メッセージ = msg;
                                break;

                            case TaskMessage.内容名.サイズ変更:
                                this.Onサイズ変更( msg );
                                break;
                        }
                    }

                    // 終了指示が来てたらループを抜ける。
                    if( null != 終了指示メッセージ )
                        break;
                    //----------------
                    #endregion

                    #region " 進行・描画する。"
                    //----------------
                    this._進行する();

                    if( スワップチェーン表示タスク.表示待機中 )
                    {
                        // 表示タスクがすでに起動されているなら、今回は描画も表示も行わない。
                    }
                    else
                    {
                        // 表示タスクが起動していないなら、描画して、表示タスクを起動する。
                        this._描画する();
                        スワップチェーン表示タスク.表示する( Global.DXGIOutput1, Global.DXGISwapChain1! );
                    }
                    //----------------
                    #endregion
                }

                #region " 終了する。"
                //----------------
                using( new LogBlock( "進行描画タスクの終了" ) )
                {
                    this._パイプラインサーバを終了する();

                    this.現行化.終了する();

                    this.ステージ?.Dispose();
                    this.ステージ = null;

                    timer.Dispose();
                    tick通知.Dispose();

                    this.アイキャッチ管理.Dispose();

                    画像.全インスタンスで共有するリソースを解放する();
                    Global.解放する();

                    // 終了指示を受け取っていた場合は完了を通知する。
                    終了指示メッセージ?.完了通知.Set();
                }
                //----------------
                #endregion
            }
            catch( Exception e )
            {
                Log.ERROR( $"進行描画タスクを異常終了します。[{e.Message}]" );
                Global.AppForm.例外を通知する( e );
            }
        }

        /// <summary>
        ///     アプリケーションの進行処理を行う。
        /// </summary>
        private void _進行する()
        {
            // ステージを進行する。

            this.ステージ?.進行する();
            Global.Animation.進行する();

            // ステージの現在のフェーズにより処理分岐。

            if( this.ステージ is null )
                return;

            switch( this.ステージ )
            {
                case 起動.起動ステージ stage:

                    #region " 完了 → タイトルステージまたは演奏ステージへ "
                    //----------------
                    if( stage.現在のフェーズ == 起動.起動ステージ.フェーズ.完了 )
                    {
                        this.ステージ.Dispose();

                        if( Global.Options.ビュアーモードである )
                        {
                            #region " (A) ビュアーモードなら演奏ステージへ "
                            //----------------
                            Log.Header( "ビュアーステージ" );

                            // AutoPlayer でログイン。
                            if( !this.ログオンする( "AutoPlay" ) )
                            {
                                System.Windows.Forms.MessageBox.Show( "AutoPlayerでのログオンに失敗しました。", "DTXMania2 error" );
                                this.ステージ = null;
                                this._アプリを終了する();
                            }
                            else
                            {
                                Log.Info( "AutoPlayer でログオンしました。" );
                            }

                            this.ステージ = new 演奏.演奏ステージ();
                            //----------------
                            #endregion
                        }
                        else
                        {
                            #region " (B) 通常時はタイトルステージへ "
                            //----------------
                            Log.Header( "タイトルステージ" );
                            this.ステージ = new タイトル.タイトルステージ();
                            //----------------
                            #endregion
                        }
                    }
                    //----------------
                    #endregion

                    break;


                case タイトル.タイトルステージ stage:

                    #region " キャンセル → 終了ステージへ "
                    //----------------
                    if( stage.現在のフェーズ == タイトル.タイトルステージ.フェーズ.キャンセル )
                    {
                        this.ステージ.Dispose();
                        
                        Log.Header( "終了ステージ" );
                        this.ステージ = new 終了.終了ステージ();
                    }
                    //----------------
                    #endregion

                    #region " 完了 → 認証ステージへ "
                    //----------------
                    else if( stage.現在のフェーズ == タイトル.タイトルステージ.フェーズ.完了 )
                    {
                        this.ステージ.Dispose();

                        Log.Header( "認証ステージ" );
                        this.ステージ = new 認証.認証ステージ();
                    }
                    //----------------
                    #endregion

                    break;


                case 認証.認証ステージ stage:

                    #region " キャンセル → タイトルステージへ "
                    //----------------
                    if( stage.現在のフェーズ == 認証.認証ステージ.フェーズ.キャンセル )
                    {
                        this.ステージ.Dispose();

                        Log.Header( "タイトルステージ" );
                        this.ステージ = new タイトル.タイトルステージ();
                    }
                    //----------------
                    #endregion

                    #region " 完了 → 選曲ステージへ "
                    //----------------
                    else if( stage.現在のフェーズ == 認証.認証ステージ.フェーズ.完了 )
                    {
                        // 選択中のユーザでログインする。ログオン中のユーザがあれば先にログオフされる。
                        Global.App.ログオンする( Global.App.ユーザリスト[ stage.現在選択中のユーザ ].ID ?? "AutoPlayer" );

                        this.ステージ.Dispose();

                        Log.Header( "選曲ステージ" );
                        this.ステージ = new 選曲.選曲ステージ();
                    }
                    //----------------
                    #endregion

                    break;


                case 選曲.選曲ステージ stage:

                    #region " キャンセル → タイトルステージへ "
                    //----------------
                    if( stage.現在のフェーズ == 選曲.選曲ステージ.フェーズ.キャンセル )
                    {
                        // 曲ツリーの現行化タスクが動いていれば、一時停止する。
                        Global.App.現行化.一時停止する();

                        this.ステージ.Dispose();

                        Log.Header( "タイトルステージ" );
                        this.ステージ = new タイトル.タイトルステージ();
                    }
                    //----------------
                    #endregion

                    #region " 確定_選曲 → 曲読み込みステージへ "
                    //----------------
                    else if( stage.現在のフェーズ == 選曲.選曲ステージ.フェーズ.確定_選曲 )
                    {
                        this.ステージ.Dispose();

                        Log.Header( "曲読み込みステージ" );
                        this.ステージ = new 曲読み込み.曲読み込みステージ();
                    }
                    //----------------
                    #endregion

                    #region " 確定_設定 → 設定ステージへ "
                    //----------------
                    else if( stage.現在のフェーズ == 選曲.選曲ステージ.フェーズ.確定_設定 )
                    {
                        this.ステージ.Dispose();

                        Log.Header( "オプション設定ステージ" );
                        this.ステージ = new オプション設定.オプション設定ステージ();
                    }
                    //----------------
                    #endregion

                    break;

                case オプション設定.オプション設定ステージ stage:

                    #region " 完了 → 選曲ステージへ "
                    //----------------
                    if( stage.現在のフェーズ == オプション設定.オプション設定ステージ.フェーズ.完了 )
                    {
                        this.ステージ.Dispose();

                        Log.Header( "選曲ステージ" );
                        this.ステージ = new 選曲.選曲ステージ();
                    }
                    //----------------
                    #endregion

                    break;

                case 曲読み込み.曲読み込みステージ stage:

                    #region " 完了 → 演奏ステージへ "
                    //----------------
                    if( stage.現在のフェーズ == 曲読み込み.曲読み込みステージ.フェーズ.完了 )
                    {
                        this.ステージ.Dispose();

                        Log.Header( "演奏ステージ" );
                        this.ステージ = new 演奏.演奏ステージ();

                        // 曲読み込みステージ画面をキャプチャする（演奏ステージのクロスフェードで使う）
                        ( (演奏.演奏ステージ) this.ステージ ).キャプチャ画面 = 画面キャプチャ.取得する(
                            Global.D3D11Device1,
                            Global.DXGISwapChain1,
                            Global.既定のD3D11RenderTargetView,
                            Global.既定のD2D1DeviceContext );
                    }
                    //----------------
                    #endregion

                    break;

                case 演奏.演奏ステージ stage:

                    #region " キャンセル完了 → 選曲ステージへ "
                    //----------------
                    if( stage.現在のフェーズ == 演奏.演奏ステージ.フェーズ.キャンセル完了 )   // ビュアーモードではこのフェーズにはならない。
                    {
                        this.ステージ.Dispose();

                        Log.Header( "選曲ステージ" );
                        this.ステージ = new 選曲.選曲ステージ();
                    }
                    //----------------
                    #endregion

                    #region " クリア → 結果ステージへ "
                    //----------------
                    else if( stage.現在のフェーズ == 演奏.演奏ステージ.フェーズ.クリア )
                    {
                        this.ステージ.Dispose();

                        Log.Header( "結果ステージ" );
                        this.ステージ = new 結果.結果ステージ( stage.成績 );
                    }
                    //----------------
                    #endregion

                    #region " 失敗 → 現在未対応 "
                    //----------------
                    else if( stage.現在のフェーズ == 演奏.演奏ステージ.フェーズ.失敗 )
                    {
                        // todo: 演奏失敗処理の実装
                        throw new NotImplementedException();
                    }
                    //----------------
                    #endregion

                    break;

                case 結果.結果ステージ stage:

                    #region " 完了 → 選曲ステージへ "
                    //----------------
                    if( stage.現在のフェーズ == 結果.結果ステージ.フェーズ.完了 )
                    {
                        this.ステージ.Dispose();

                        Log.Header( "選曲ステージ" );
                        this.ステージ = new 選曲.選曲ステージ();
                    }
                    //----------------
                    #endregion

                    break;

                case 終了.終了ステージ stage:

                    #region " 完了 → アプリ終了 "
                    //----------------
                    if( stage.現在のフェーズ == 終了.終了ステージ.フェーズ.完了 )
                    {
                        this.ステージ?.Dispose();
                        this.ステージ = null;

                        this._アプリを終了する();
                    }
                    //----------------
                    #endregion

                    break;
            }
        }

        /// <summary>
        ///     アプリケーションの描画処理を行う。
        /// </summary>
        private void _描画する()
        {
            this.ステージ?.描画する();
        }

        internal void 画面をクリアする()
        {
            var d3ddc = Global.D3D11Device1.ImmediateContext;

            // 既定のD3Dレンダーターゲットビューを黒でクリアする。
            d3ddc.ClearRenderTargetView( Global.既定のD3D11RenderTargetView, Color4.Black );

            // 既定の深度/ステンシルバッファをクリアする。
            d3ddc.ClearDepthStencilView(
                Global.既定のD3D11DepthStencilView,
                SharpDX.Direct3D11.DepthStencilClearFlags.Depth | SharpDX.Direct3D11.DepthStencilClearFlags.Stencil,
                depth: 1.0f,
                stencil: 0 );
        }



        // ログオンとログオフ


        /// <summary>
        ///     指定されたユーザでログオンする。
        ///     現在ログオン中のユーザがあれば、先にログオフする。
        /// </summary>
        /// <param name="ユーザID"></param>
        /// <returns>ログオンに成功したらtrue。</returns>
        public bool ログオンする( string ユーザID )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this.ログオフする();

            // 新しいユーザを選択する。
            if( !Global.App.ユーザリスト.SelectItem( ( user ) => user.ID == ユーザID ) )
            {
                Log.ERROR( $"ユーザ「{ユーザID}」のログオンに失敗しました。ユーザが存在しません。" );
                return false;
            }

            var userConfig = Global.App.ログオン中のユーザ;
            var roots = Global.App.曲ツリーリスト.Select( ( t ) => t.ルートノード );

            // すべての曲ツリーのユーザ依存情報をリセットし、属性のみ今ここで現行化する。
            Global.App.現行化.リセットする( roots, userConfig );
            Global.App.現行化.すべての譜面について属性を現行化する( userConfig.ID! );

            // 評価順曲ツリーを新しい属性にあわせて再構築する。
            var ratingTree = (曲ツリー_評価順) Global.App.曲ツリーリスト[ 1 ];  // [1]評価順
            ratingTree.再構築する();

            // すべての曲ツリーの現行化を開始する。
            Global.App.現行化.開始する( roots, Global.App.ログオン中のユーザ );

            // 選択する曲ツリーリストを初期化。
            foreach( var tree in Global.App.曲ツリーリスト )
                tree.ルートノード.子ノードリスト.SelectFirst();
            Global.App.曲ツリーリスト.SelectFirst();


            // 完了。
            Log.Info( $"{ユーザID} でログオンしました。" );
            return true;
        }

        /// <summary>
        ///     現在ログオン中のユーザをログオフする。
        /// </summary>
        public void ログオフする()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            var userConfig = this.ユーザリスト.SelectedItem;
            if( null != userConfig )
            {
                Global.App.現行化.終了する();

                Log.Info( $"{userConfig.ID} をログオフしました。" );
            }

            // ユーザを未選択状態へ。
            Global.App.ユーザリスト.SelectItem( -1 );
        }



        // ウィンドウサイズの変更への対応


        protected void Onサイズ変更( TaskMessage msg )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            // リソースを解放して、
            this.Onスワップチェーンに依存するグラフィックリソースの解放();

            // スワップチェーンを再構築して、
            var size = (System.Drawing.Size) msg.引数![ 0 ];
            Global.物理画面サイズを変更する( new Size2F( size.Width, size.Height ) );

            // リソースを再作成する。
            this.Onスワップチェーンに依存するグラフィックリソースの作成();

            // 完了。
            msg.完了通知.Set();
        }

        protected void Onスワップチェーンに依存するグラフィックリソースの作成()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );
        }

        protected void Onスワップチェーンに依存するグラフィックリソースの解放()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );
        }



        // パイプラインサービス


        private CancellationTokenSource _PipeServerCancellationTokenSource = null!;


        private async void _パイプラインサーバを起動する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._PipeServerCancellationTokenSource?.Dispose();
            this._PipeServerCancellationTokenSource = new CancellationTokenSource();

            var cancelToken = this._PipeServerCancellationTokenSource.Token;

            await Task.Run( async () => {

                Log.Info( "パイプラインサーバを起動しました。" );

                int 例外発生回数 = 0;
                bool ビュアーモードではない = !Global.Options.ビュアーモードである;

                while( !cancelToken.IsCancellationRequested )
                {
                    try
                    {
                        // パイプラインサーバを起動する。
                        using var pipeServer = new NamedPipeServerStream(
                            pipeName: Program._ビュアー用パイプライン名,
                            direction: PipeDirection.In,
                            maxNumberOfServerInstances: 2 ); // 再起動の際に一時的に 2 個開く瞬間がある

                        // クライアントの接続を待つ。
                        await pipeServer.WaitForConnectionAsync( cancelToken );

                        // キャンセルされたならループを抜ける。
                        if( cancelToken.IsCancellationRequested )
                            break;

                        Log.Info( "パイプラインクライアントと接続しました。" );

                        // パイプラインからYAMLを受取る。
                        var ss = new StreamStringForNamedPipe( pipeServer );
                        var yamlText = ss.ReadString();
                        Trace.WriteLine( "受信文字列\n--- ここから ---" );
                        Trace.WriteLine( $"{yamlText}" );
                        Trace.WriteLine( "--- ここまで ---" );

                        if( yamlText.Nullまたは空である() || yamlText == "ping" )
                            continue;   // 空送信またはテスト送信

                        // 受け取ったオプションは、ビュアーモードでなければ実行されない。
                        if( ビュアーモードではない )
                            continue;

                        // YAMLからコマンドラインオプションを復元する。
                        var options = CommandLineOptions.FromYaml( yamlText );

                        // オプションを担当ステージに送る。
                        if( options.再生停止 )
                        {
                            if( this.ステージ is 演奏.演奏ステージ )
                            {
                                // 停止
                                演奏.演奏ステージ.OptionsQueue.Enqueue( options );
                            }
                        }
                        else if( options.再生開始 )
                        {
                            if( this.ステージ is 演奏.演奏ステージ )
                            {
                                // 停止と
                                演奏.演奏ステージ.OptionsQueue.Enqueue( new CommandLineOptions() { 再生停止 = true } );

                                // 開始
                                演奏.演奏ステージ.OptionsQueue.Enqueue( options );
                            }
                        }
                        else
                        {
                            // HACK: その他のコマンドラインオプションへの対応
                        }
                    }
                    catch( Exception e )
                    {
                        Log.ERROR( $"パイプラインサーバで例外が発生しました。[{e.Message}]" );

                        if( 10 < ++例外発生回数 )
                        {
                            Log.ERROR( "例外発生回数が 10 を越えたので、異常と見なして終了します。" );
                            break;
                        }
                    }
                }

                Log.Info( "パイプラインサーバを終了しました。" );

            } );
        }

        private void _パイプラインサーバを終了する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            if( !this._PipeServerCancellationTokenSource.IsCancellationRequested )
                this._PipeServerCancellationTokenSource.Cancel();

            this._PipeServerCancellationTokenSource.Dispose();
        }



        // ローカル


        private int _進行描画タスクのNETスレッドID;

        private void _アプリを終了する()
        {
            Global.AppForm.BeginInvoke( new Action( () => {   // UIスレッドで実行する
                Global.AppForm.Close();
            } ) );
        }
    }
}
