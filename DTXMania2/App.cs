using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FDK;
using SSTFormat.v004;
using DTXMania2.曲;

namespace DTXMania2
{
    [DebuggerDisplay("Form")]   // Form.ToString() の評価タイムアウト回避用。
    public partial class App : Form
    {

        // プロパティ


        internal ScreenMode ScreenMode { get; private protected set; } = null!;

        internal KeyboardHID KeyboardHID { get; private protected set; } = null!;

        internal GameControllersHID GameControllersHID { get; private protected set; } = null!;

        internal MidiIns MidiIns { get; private protected set; } = null!;

        internal Random 乱数 { get; } = new Random( DateTime.Now.Millisecond );

        internal SystemConfig システム設定 { get; set; } = null!;

        internal システムサウンド システムサウンド { get; private protected set; } = null!;

        internal ドラムサウンド ドラムサウンド { get; private protected set; } = null!;

        internal ドラム入力 ドラム入力 { get; private protected set; } = null!;

        internal SoundDevice サウンドデバイス { get; private protected set; } = null!;

        internal SoundTimer サウンドタイマ { get; private protected set; } = null!;

        internal アイキャッチ管理 アイキャッチ管理 { get; private protected set; } = null!;

        internal SelectableList<ユーザ設定> ユーザリスト { get; } = new SelectableList<ユーザ設定>();

        internal ユーザ設定 ログオン中のユーザ => this.ユーザリスト.SelectedItem!;

        internal List<Score> 全譜面リスト { get; } = new List<Score>();

        internal List<Song> 全曲リスト { get; } = new List<Song>();

        /// <summary>
        ///     曲ツリーのリスト。選曲画面の「表示方法選択パネル」で変更できる。<br/>
        ///     [0]全曲、[1]評価順 で固定。
        /// </summary>
        internal SelectableList<曲ツリー> 曲ツリーリスト { get; } = new SelectableList<曲ツリー>();

        internal 現行化 現行化 { get; } = new 現行化();

        internal CacheStore<CSCore.ISampleSource> WAVキャッシュ { get; private protected set; } = null!;

        internal IStage? ステージ { get; private protected set; } = null;

        /// <summary>
        ///     アプリケーション再起動指示フラグ。
        /// </summary>
        /// <remarks>
        ///     <see cref="App"/> インスタンスの終了時にこのフラグが true になっている場合には、
        ///     このインスタンスの保持者（おそらくProgramクラス）は適切に再起動を行うこと。
        /// </remarks>
        internal bool 再起動が必要 { get; private protected set; } = false;



        // 演奏ごとに更新されるプロパティ


        /// <summary>
        ///     演奏する譜面。<see cref="App.演奏スコア"/>の生成元。
        ///     選曲ステージで選曲確定後に更新される。
        /// </summary>
        /// <remarks>
        ///     <see cref="RandomSelectNode"/> がフォーカスされている場合は、ここには譜面がランダムに設定されるため
        ///     <see cref="曲ツリー.フォーカスノード"/> の譜面とは必ずしも一致しないので注意。
        /// </remarks>
        internal Score 演奏譜面 { get; set; } = null!;

        /// <summary>
        ///     現在演奏中のスコア。
        ///     曲読み込みステージで<see cref="App.演奏譜面"/>を読み込んで生成される。
        /// </summary>
        internal スコア 演奏スコア { get; set; } = null!;

        /// <summary>
        ///     <see cref="App.演奏スコア"/> に対応して生成されたWAVサウンドインスタンスの管理。
        /// </summary>
        internal WAV管理 WAV管理 { get; set; } = null!;

        /// <summary>
        ///     <see cref="App.演奏スコア"/>  に対応して生成されたAVI動画インスタンスの管理。
        /// </summary>
        internal AVI管理 AVI管理 { get; set; } = null!;



        // 生成と終了


        /// <summary>
        ///     コンストラクタ。
        /// </summary>
        internal App()
        {
            InitializeComponent();
        }

        /// <summary>
        ///     アプリケーションの起動処理を行う。
        /// </summary>
        protected override void OnLoad( EventArgs e )
        {
            // ※ このメソッドは GUI スレッドで実行されるので、後回しにできる初期化処理は進行描画タスクに回して、
            // 　 なるべく早くこのメソッドを抜けること。

            Log.Header( "アプリケーション起動" );
            using var _ = new LogBlock( Log.現在のメソッド名 );

            
            // フォームを設定する。
            
            this.Text = $"DTXMania2 Release {int.Parse( Application.ProductVersion.Split( '.' ).ElementAt( 0 ) ):000}";
            this.ClientSize = new Size( 1024, 576 );
            this.Icon = Properties.Resources.DTXMania2;
            
            this.ScreenMode = new ScreenMode( this );
            
            Global.App = this;
            Global.Handle = this.Handle;


            // サウンドデバイスとサウンドタイマを初期化する。これらは入力デバイスで使用されるので先に初期化する。

            this.サウンドデバイス = new SoundDevice( CSCore.CoreAudioAPI.AudioClientShareMode.Shared );
            // マスタ音量（小:0～1:大）... 0.5を超えるとだいたいWASAPI共有モードのリミッターに抑制されるようになる
            // ※サウンドデバイスの音量プロパティはコンストラクタの実行後でないと set できないので、初期化子にはしないこと。（した場合の挙動は不安定）
            this.サウンドデバイス.音量 = 0.5f;
            this.サウンドタイマ = new SoundTimer( this.サウンドデバイス );


            // 入力デバイスを初期化する。これらは GUI スレッドで行う必要がある。

            this.KeyboardHID = new KeyboardHID( this.サウンドタイマ );
            this.GameControllersHID = new GameControllersHID( this.Handle, this.サウンドタイマ );
            this.MidiIns = new MidiIns( this.サウンドタイマ );


            // システム設定ファイルを読み込む。

            SystemConfig.最新版にバージョンアップする();
            this.システム設定 = SystemConfig.読み込む();


            // メインループを別スレッドで開始する。

            if( !this._進行描画タスクを起動する().WaitOne( 5000 ) )
                throw new TimeoutException( "進行描画タスクの起動処理がタイムアウトしました。" );

            
            // 初期化完了。（進行描画タスクの起動後に）
            
            this._未初期化 = false;

            
            // 全画面モードが設定されているならここで全画面に切り替える。
            
            if( this.システム設定.全画面モードである )
                this.ScreenMode.ToFullscreenMode();

            base.OnLoad( e );
        }

        /// <summary>
        ///     アプリケーションの終了処理を行う。
        /// </summary>
        protected override void OnClosing( CancelEventArgs e )
        {
            Log.Header( "アプリケーション終了" );
            using var _ = new LogBlock( Log.現在のメソッド名 );


            // 進行描画タスクのメインループに終了指示を送り、終了するのを待つ。

            this._進行描画タスクを終了する();


            // システム設定ファイルを保存する。

            this.システム設定.保存する();


            // 入力デバイスを破棄する。

            this.MidiIns.Dispose();
            this.GameControllersHID.Dispose();
            this.KeyboardHID.Dispose();


            // サウンドデバイスとサウンドタイマを破棄する。

            this.サウンドタイマ.Dispose();
            this.サウンドデバイス.Dispose();
            this.WAVキャッシュ?.Dispose();   // WAVキャッシュの破棄は最後に。


            // 未初期化状態へ。

            this._未初期化 = true;

            base.OnClosing( e );
        }

        /// <summary>
        ///     再起動フラグをセットして、アプリケーションを終了する。
        /// </summary>
        internal void 再起動する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this.再起動が必要 = true;
            this.Close();
        }



        // 進行と描画


        private ManualResetEvent _進行描画タスクを起動する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            var 起動完了通知 = new ManualResetEvent( false );

            Task.Run( () => {

                Log.現在のスレッドに名前をつける( "進行描画" );
                Log.Info( "進行描画タスクを起動しました。" );
                
                this._進行描画タスクのNETスレッドID = Thread.CurrentThread.ManagedThreadId;
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

                起動完了通知.Set();

                this._進行描画のメインループを実行する();

            } );

            return 起動完了通知;
        }

        private void _進行描画タスクを終了する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            if( Thread.CurrentThread.ManagedThreadId != this._進行描画タスクのNETスレッドID )
            {
                // 進行描画タスクに終了を指示し、完了を待つ。
                var msg = new TaskMessage(
                    宛先: TaskMessage.タスク名.進行描画,
                    内容: TaskMessage.内容名.終了指示 );

                if( !Global.TaskMessageQueue.Post( msg ).Wait( 5000 ) )     // 最大5秒待つ
                    throw new TimeoutException( "進行描画タスクの終了がタイムアウトしました。" );
            }
            else
            {
                // ハングアップ回避; 念のため。
                Log.WARNING( "進行描画タスクから呼び出されました。完了通知待ちをスキップします。" );
            }
        }

        private void _進行描画のメインループを実行する()
        {
            try
            {
                #region " 初期化 "
                //----------------
                this._パイプラインサーバを起動する();

                UserConfig.最新版にバージョンアップする();
                ScoreDB.最新版にバージョンアップする();
                RecordDB.最新版にバージョンアップする();
                ScorePropertiesDB.最新版にバージョンアップする();

                Global.生成する(
                    設計画面サイズ: new SharpDX.Size2F( 1920f, 1080f ),
                    物理画面サイズ: new SharpDX.Size2F( this.ClientSize.Width, this.ClientSize.Height ) );

                画像.全インスタンスで共有するリソースを作成する( Global.D3D11Device1, @"$(Images)\TextureVS.cso", @"$(Images)\TexturePS.cso" );

                this._システム設定をもとにリソース関連のフォルダ変数を更新する();

                this.システムサウンド = new システムサウンド( this.サウンドデバイス );  // 個々のサウンドの生成は後工程で。

                this.ドラムサウンド = new ドラムサウンド( this.サウンドデバイス );      // 　　　　　　〃

                this.WAVキャッシュ = new CacheStore<CSCore.ISampleSource>() {
                    ファイルからデータを生成する = ( path ) => SampleSourceFactory.Create( this.サウンドデバイス, path, this.ログオン中のユーザ.再生速度 ),
                };

                this._ユーザリストにユーザを登録する();

                this.アイキャッチ管理 = new アイキャッチ管理();

                this.ドラム入力 = new ドラム入力( this.KeyboardHID, this.GameControllersHID, this.MidiIns );


                // 最初のステージを生成する。

                Log.Header( "起動ステージ" );
                this.ステージ = new 起動.起動ステージ();
                //----------------
                #endregion

                TaskMessage? 終了指示メッセージ = null;

                while( true )
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
                                this._リソースを再構築する( msg );
                                break;
                        }
                    }

                    // 終了指示が来てたらループを抜ける。
                    if( null != 終了指示メッセージ )
                        break;
                    //----------------
                    #endregion

                    #region " 進行し、描画し、表示する。"
                    //----------------
                    Global.Animation.進行する();

                    this.ステージ?.進行描画する();

                    Global.DXGISwapChain1.Present( 1, SharpDX.DXGI.PresentFlags.None );
                    //----------------
                    #endregion

                    #region " ステージの現在のフェーズにより処理分岐。"
                    //----------------
                    switch( this.ステージ )
                    {
                        case 起動.起動ステージ stage:
                        {
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
                        }
                        case タイトル.タイトルステージ stage:
                        {
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
                        }
                        case 認証.認証ステージ stage:
                        {
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
                                this.ログオンする( this.ユーザリスト[ stage.現在選択中のユーザ ].ID ?? "AutoPlayer" );

                                this.ステージ.Dispose();

                                Log.Header( "選曲ステージ" );
                                this.ステージ = new 選曲.選曲ステージ();
                            }
                            //----------------
                            #endregion

                            break;
                        }
                        case 選曲.選曲ステージ stage:
                        {
                            #region " キャンセル → タイトルステージへ "
                            //----------------
                            if( stage.現在のフェーズ == 選曲.選曲ステージ.フェーズ.キャンセル )
                            {
                                // 曲ツリーの現行化タスクが動いていれば、一時停止する。
                                this.現行化.一時停止する();

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
                        }
                        case オプション設定.オプション設定ステージ stage:
                        {
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
                        }
                        case 曲読み込み.曲読み込みステージ stage:
                        {
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
                        }
                        case 演奏.演奏ステージ stage:
                        {
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
                        }
                        case 結果.結果ステージ stage:
                        {
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
                        }
                        case 終了.終了ステージ stage:
                        {
                            #region " 完了 → アプリ終了 "
                            //----------------
                            if( stage.現在のフェーズ == 終了.終了ステージ.フェーズ.完了 )
                            {
                                this._アプリを終了する();
                            }
                            //----------------
                            #endregion
                            
                            break;
                        }
                    }
                    //----------------
                    #endregion
                }

                #region " 終了 "
                //----------------
                this._パイプラインサーバを終了する(); // これ以上受信しないよう真っ先に終了。

                this.現行化.終了する();

                this.ステージ?.Dispose();

                this.アイキャッチ管理.Dispose();

                foreach( var song in this.全曲リスト )
                    song.Dispose(); // 全譜面リストもここで解放される。

                foreach( var tree in this.曲ツリーリスト )
                    tree.Dispose();

                this.ドラムサウンド.Dispose();

                this.システムサウンド.Dispose();

                画像.全インスタンスで共有するリソースを解放する();

                Global.解放する();
                //----------------
                #endregion

                終了指示メッセージ.完了通知.Set();
            }
#if !DEBUG
                // GUIスレッド以外のスレッドで発生した例外は、Debug 版だとデバッガがキャッチするが、
                // Release 版だと何も表示されずスルーされるので、念のためログ出力しておく。
                catch( Exception e )
                {
                    Log.ERROR( $"例外が発生しました。\n{e}" );
                }
#endif
            finally
            {
                Log.Info( "進行描画タスクを終了しました。" );
            }
        }

        internal void 画面をクリアする()
        {
            var d3ddc = Global.D3D11Device1.ImmediateContext;

            // 既定のD3Dレンダーターゲットビューを黒でクリアする。
            d3ddc.ClearRenderTargetView( Global.既定のD3D11RenderTargetView, SharpDX.Color4.Black );

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
        internal bool ログオンする( string ユーザID )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );


            // 現在ログオン中のユーザがあれば、先にログオフする。

            this.ログオフする();

            
            // 新しいユーザを選択する。

            if( !this.ユーザリスト.SelectItem( ( user ) => user.ID == ユーザID ) )
            {
                Log.ERROR( $"ユーザ「{ユーザID}」のログオンに失敗しました。ユーザが存在しません。" );
                return false;
            }
            var userConfig = this.ログオン中のユーザ;
            var roots = this.曲ツリーリスト.Select( ( t ) => t.ルートノード );


            // すべての曲ツリーのユーザ依存情報をリセットし、属性のみ今ここで現行化する。

            this.現行化.リセットする( roots, userConfig );
            this.現行化.すべての譜面について属性を現行化する( userConfig.ID! );


            // 評価順曲ツリーを新しい属性にあわせて再構築する。

            var ratingTree = (曲ツリー_評価順) this.曲ツリーリスト[ 1 ];  // [1]評価順
            ratingTree.再構築する();

            
            // すべての曲ツリーの現行化を開始する。

            this.現行化.開始する( roots, this.ログオン中のユーザ );


            // 選択する曲ツリーリストを初期化。

            foreach( var tree in this.曲ツリーリスト )
                tree.ルートノード.子ノードリスト.SelectFirst();
            this.曲ツリーリスト.SelectFirst();


            // 完了。

            Log.Info( $"{ユーザID} でログオンしました。" );
            return true;
        }

        /// <summary>
        ///     現在ログオン中のユーザをログオフする。
        /// </summary>
        internal void ログオフする()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            var userConfig = this.ユーザリスト.SelectedItem;
            if( null != userConfig )
            {
                this.現行化.終了する();

                Log.Info( $"{userConfig.ID} をログオフしました。" );
            }

            // ユーザを未選択状態へ。
            this.ユーザリスト.SelectItem( -1 );
        }



        // ウィンドウサイズの変更


        /* 次の２通りがある。
         * 
         * A.ユーザのドラッグによるサイズ変更。
         *      → ResizeBegin ～ ResizeEnd の範囲内で Resize が発生するがいったん無視し、ResizeEnd のタイミングでサイズの変更を行う。
         * 
         * B.最大化、最小化など。
         *      → ResizeBegin ～ ResizeEnd の範囲外で Resize が発生するので、そのタイミングでサイズの変更を行う。
         */

        protected override void OnResizeBegin( EventArgs e )
        {
            this._リサイズ中 = true; // リサイズ開始

            base.OnResizeBegin( e );
        }

        protected override void OnResizeEnd( EventArgs e )
        {
            this._リサイズ中 = false;    // リサイズ終了（先に設定）

            if( this.WindowState == FormWindowState.Minimized )
            {
                // (A) 最小化された → 何もしない
            }
            else if( this.ClientSize.IsEmpty )
            {
                // (B) クライアントサイズが空 → たまに起きるらしい。スキップする。
            }
            else
            {
                // (C) それ以外は Resize イベントハンドラへ委譲。
                this.OnResize( e );
            }

            base.OnResizeEnd( e );
        }

        protected override void OnResize( EventArgs e )
        {
            if( this._未初期化 || this._リサイズ中 )
            {
                //Log.Info( "未初期化、またはリサイズ中なので無視します。" );
                return;
            }
            else
            {
                using var _ = new LogBlock( Log.現在のメソッド名 );

                Log.Info( $"新画面サイズ: {this.ClientSize}" );

                // スワップチェーンとその依存リソースを解放し、改めて作成しなおすように進行描画タスクへ指示する。
                var msg = new TaskMessage(
                    宛先: TaskMessage.タスク名.進行描画,
                    内容: TaskMessage.内容名.サイズ変更,
                    引数: new object[] { this.ClientSize } );

                // 進行描画タスクからの完了通知を待つ。
                if( !Global.TaskMessageQueue.Post( msg ).Wait( 5000 ) )
                    throw new TimeoutException( "サイズ変更タスクメッセージがタイムアウトしました。" );
            }

            base.OnResize( e );
        }

        private bool _リサイズ中 = false;

        private void _リソースを再構築する( TaskMessage msg )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            // リソースを解放して、
            //this.Onスワップチェーンに依存するグラフィックリソースの解放();

            // スワップチェーンを再構築して、
            var size = (System.Drawing.Size) msg.引数![ 0 ];
            Global.物理画面サイズを変更する( new SharpDX.Size2F( size.Width, size.Height ) );

            // リソースを再作成する。
            //this.Onスワップチェーンに依存するグラフィックリソースの作成();

            // 完了。
            msg.完了通知.Set();
        }



        // パイプラインサーバ


        private CancellationTokenSource _PipeServerCancellationTokenSource = null!;

        private async void _パイプラインサーバを起動する()
        {
            //using var _ = new LogBlock( Log.現在のメソッド名 );

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

            //this._PipeServerCancellationTokenSource.Dispose();
        }



        // ウィンドウメッセージ


        protected override void WndProc( ref Message m )
        {
            const int WM_INPUT = 0x00FF;

            switch( m.Msg )
            {
                case WM_INPUT:
                    this.OnInput( m );
                    break;
            }

            base.WndProc( ref m );
        }

        protected override void OnKeyDown( KeyEventArgs e )
        {
            #region " F11 → 全画面／ウィンドウモードを切り替える。"
            //----------------
            if( e.KeyCode == Keys.F11 )
            {
                // ScreenMode は非同期処理なので、すぐに値が反映されるとは限らない。
                // なので、ログオン中のユーザへの設定は、その変更より先に行なっておく。
                this.システム設定.全画面モードである = this.ScreenMode.IsWindowMode; // 先に設定するので Mode が逆になっていることに注意。

                if( this.ScreenMode.IsWindowMode )
                    this.ScreenMode.ToFullscreenMode();
                else
                    this.ScreenMode.ToWindowMode();
            }
            //----------------
            #endregion

            base.OnKeyDown( e );
        }

        protected virtual void OnInput( in Message m )
        {
            RawInput.RawInputData rawInputData;

            #region " RawInput データを取得する。"
            //----------------
            unsafe
            {
                // RawInputData 構造体（可変長）の実サイズを取得する。
                int dataSize = 0;
                if( 0 > RawInput.GetRawInputData( m.LParam, RawInput.DataType.Input, null, ref dataSize, Marshal.SizeOf<RawInput.RawInputHeader>() ) )
                {
                    Log.ERROR( $"GetRawInputData(): error = { Marshal.GetLastWin32Error()}" );
                    return;
                }

                // RawInputData 構造体の実データを取得する。
                var dataBytes = stackalloc byte[ dataSize ];
                if( 0 > RawInput.GetRawInputData( m.LParam, RawInput.DataType.Input, dataBytes, &dataSize, Marshal.SizeOf<RawInput.RawInputHeader>() ) )
                {
                    Log.ERROR( $"GetRawInputData(): error = { Marshal.GetLastWin32Error()}" );
                    return;
                }

                // 取得された実データは byte[] なので、これを RawInputData 構造体に変換する。
                rawInputData = Marshal.PtrToStructure<RawInput.RawInputData>( new IntPtr( dataBytes ) );
            }
            //----------------
            #endregion

            this.KeyboardHID.OnInput( rawInputData );
            this.GameControllersHID.OnInput( rawInputData );
        }



        // ローカル


        /// <summary>
        ///     アプリの初期化が完了していなければ true。
        ///     起動直後は true, OnLoad() で false, OnClosing() で true になる。
        /// </summary>
        /// <remarks>
        ///     アプリの OnLoad() より前に OnResize() が呼び出されることがあるので、その対策用。
        /// </remarks>
        private bool _未初期化 = true;

        private int _進行描画タスクのNETスレッドID;

        private void _システム設定をもとにリソース関連のフォルダ変数を更新する()
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

        private void _ユーザリストにユーザを登録する()
        {
            // パターンに該当するファイルをすべて列挙する。
            var userYamls = Directory.EnumerateFiles( Folder.フォルダ変数の内容を返す( "AppData" ), @"User_*.yaml", SearchOption.TopDirectoryOnly );
            int headLength = "User_".Length;
            int tailLength = ".yaml".Length;

            foreach( var userYaml in userYamls )
            {
                // ファイル名からユーザIDを抽出。
                var fname = Path.GetFileName( userYaml );
                var userId = fname[ headLength..( fname.Length - tailLength ) ];

                var userConfig = ユーザ設定.読み込む( userId );
                this.ユーザリスト.Add( userConfig );
            }
        }

        private void _アプリを終了する()
        {
            this.BeginInvoke( new Action( () => {   // UIスレッドで実行する
                this.Close();
            } ) );
        }
    }
}
