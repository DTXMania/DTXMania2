using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using SharpDX;
using SSTFormat.v4;
using FDK;

namespace DTXMania
{
    class App進行描画 : App進行描画Base
    {

        // グローバルリソース(static)

        public static App進行描画 Instance { get; protected set; }

        public static Random 乱数 { get; protected set; }

        public static システム設定 システム設定 { get; set; }

        public static SoundDevice サウンドデバイス { get; protected set; }

        public static SoundTimer サウンドタイマ { get; protected set; }

        public static システムサウンド システムサウンド { get; protected set; }

        public static ユーザ管理 ユーザ管理 { get; protected set; }

        public static キャッシュデータレンタル<CSCore.ISampleSource> WAVキャッシュレンタル { get; protected set; }

        public static 入力管理 入力管理 { get; set; }

        public static ドラムサウンド ドラムサウンド { get; protected set; }

        public static 曲ツリー 曲ツリー { get; set; }   // ビュアーモード時は未使用。

        public static アイキャッチ管理 アイキャッチ管理 { get; protected set; }

        public static EffekseerManager EffekseerManager { get; protected set; }



        // 演奏ごとのプロパティ(static)


        public static MusicNode ビュアー用曲ノード { get; set; } // ビュアーモード時のみ使用。

        public static MusicNode 演奏曲ノード
            => DTXMania.AppForm.ビュアーモードである ? App進行描画.ビュアー用曲ノード : App進行描画.曲ツリー.フォーカス曲ノード; // MusicNode 以外は null が返される

        /// <summary>
        ///     現在演奏中のスコア。
        ///     選曲画面で選曲するたびに更新される。
        /// </summary>
        public static スコア 演奏スコア { get; set; }

        /// <summary>
        ///     <see cref="演奏スコア"/> に対応して生成されたWAVサウンドインスタンスの管理。
        /// </summary>
        public static WAV管理 WAV管理 { get; set; }

        /// <summary>
        ///     <see cref="演奏スコア"/>  に対応して生成されたAVI動画インスタンスの管理。
        /// </summary>
        public static AVI管理 AVI管理 { get; set; }



        // 生成と終了


        public App進行描画()
        {
            App進行描画.Instance = this;
        }

        protected override void On開始()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                // グローバルリソースを生成。（最低限。残りは起動ステージから グローバルリソースを生成する() が呼び出されたときに行われる。）

                App進行描画.乱数 = new Random( DateTime.Now.Millisecond );
                //App進行描画.システム設定 = システム設定.読み込む();   --> App() で初期化する。
                App進行描画.WAVキャッシュレンタル = new キャッシュデータレンタル<CSCore.ISampleSource>() {
                    ファイルからデータを生成する = ( path ) => SampleSourceFactory.Create( App進行描画.サウンドデバイス, path, App進行描画.ユーザ管理.ログオン中のユーザ.再生速度 ),
                };
                App進行描画.サウンドデバイス = new SoundDevice( CSCore.CoreAudioAPI.AudioClientShareMode.Shared );
                App進行描画.サウンドデバイス.音量 = 0.5f; // マスタ音量（小:0～1:大）... 0.5を超えるとだいたいWASAPI共有モードのリミッターに抑制されるようになる
                                            // ※↑「音量」はコンストラクタの実行後でないと set できないので、初期化子にはしないこと。（挙動は不明）

                App進行描画.サウンドタイマ = new SoundTimer( App進行描画.サウンドデバイス );
                App進行描画.ドラムサウンド = new ドラムサウンド();
                App進行描画.システムサウンド = new システムサウンド();
                //App進行描画.システムサウンド.読み込む();  --> 起動ステージで行う。
                App進行描画.入力管理 = new 入力管理( App進行描画.システム設定, this.AppForm.キーボード, this.AppForm.ゲームコントローラ );
                App進行描画.ユーザ管理 = new ユーザ管理();
                App進行描画.ユーザ管理.ユーザリスト.SelectItem( ( user ) => ( user.ユーザID == "AutoPlayer" ) );  // ひとまずAutoPlayerを選択。


                // ステージを生成。（残りは起動ステージから グローバルリソースを生成する() が呼び出されたときにおこなれる。）

                this.起動ステージ = new 起動.起動ステージ();
                this.演奏ステージ_ビュアーモード = new 演奏.演奏ステージ_ビュアーモード();


                // 最初のステージを設定し、活性化する。

                this.現在のステージ = this.起動ステージ;
                this.現在のステージ.活性化する();
            }
        }

        // 起動ステージから呼び出される。
        public void グローバルリソースを生成する()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                テクスチャ.全インスタンスで共有するリソースを作成する();

                App進行描画.アイキャッチ管理 = new アイキャッチ管理();
                App進行描画.EffekseerManager = new EffekseerManager();


                // 起動ステージ以外のステージを生成。

                this.タイトルステージ = new タイトル.タイトルステージ();
                this.認証ステージ = new 認証.認証ステージ();
                this.選曲ステージ = new 選曲.選曲ステージ();
                this.オプション設定ステージ = new オプション設定.オプション設定ステージ();
                this.曲読み込みステージ = new 曲読み込み.曲読み込みステージ();
                this.演奏ステージ = new 演奏.演奏ステージ();
                //this.演奏ステージ_ビュアーモード = new 演奏.演奏ステージ_ビュアーモード();    --> On開始する() で生成
                this.結果ステージ = new 結果.結果ステージ() {
                    BGMを停止する = () => 演奏ステージ.BGMを停止する(),
                    結果を取得する = () => 演奏ステージ.成績,
                };
                this.終了ステージ = new 終了.終了ステージ();
            }
        }

        protected override void On終了()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this.現在のステージ = null;


                // ステージを解放。

                this.起動ステージ?.Dispose();
                this.タイトルステージ?.Dispose();
                this.認証ステージ?.Dispose();
                this.選曲ステージ?.Dispose();
                this.オプション設定ステージ?.Dispose();
                this.曲読み込みステージ?.Dispose();
                this.演奏ステージ?.Dispose();
                this.結果ステージ?.Dispose();
                this.終了ステージ?.Dispose();


                // グローバルリソースを解放。

                App進行描画.EffekseerManager?.Dispose();
                App進行描画.アイキャッチ管理?.Dispose();
                App進行描画.曲ツリー?.Dispose();
                App進行描画.ドラムサウンド?.Dispose();
                App進行描画.入力管理?.Dispose();
                App進行描画.ユーザ管理?.Dispose();
                App進行描画.システムサウンド?.Dispose();
                App進行描画.サウンドタイマ?.Dispose();
                App進行描画.サウンドデバイス?.Dispose();
                App進行描画.WAVキャッシュレンタル?.Dispose();    // サウンドデバイスより後

                テクスチャ.全インスタンスで共有するリソースを解放する();

                App進行描画.Instance = null;
            }
        }

        private void _アプリを終了する()
        {
            this.AppForm.BeginInvoke( new Action( () => {   // UIスレッドで実行する
                this.AppForm.Close();
            } ) );
        }

        internal static void ユーザ管理を再構築する()
        {
            ユーザ管理?.Dispose();
            ユーザ管理 = new ユーザ管理();
        }



        // 進行と描画


        protected ステージ 現在のステージ;

        protected override void On進行()
        {
            // ステージを進行する。

            this.現在のステージ?.進行する();

            
            // 進行結果により処理分岐。

            switch( this.現在のステージ )
            {
                case 起動.起動ステージ stage:
                    #region " キャンセル → アプリ終了 "
                    //----------------
                    if( stage.現在のフェーズ == 起動.起動ステージ.フェーズ.キャンセル )
                    {
                        this._アプリを終了する();
                    }
                    //----------------
                    #endregion
                    #region " 完了 → 通常時はタイトルステージ、ビュアーモード時は演奏ステージ_ビュアーモードへ "
                    //----------------
                    if( stage.現在のフェーズ == 起動.起動ステージ.フェーズ.完了 )
                    {
                        stage.非活性化する();

                        if( DTXMania.AppForm.ビュアーモードである )
                        {
                            // (A) ビュアーモードなら 演奏ステージ_ビュアーモード へ
                            this.現在のステージ = this.演奏ステージ_ビュアーモード;
                        }
                        else
                        {
                            // (B) 通常時はタイトルステージへ
                            this.現在のステージ = this.タイトルステージ;
                        }

                        this.現在のステージ.活性化する();
                    }
                    //----------------
                    #endregion
                    break;

                case タイトル.タイトルステージ stage:
                    #region " キャンセル → 終了ステージへ "
                    //----------------
                    if( stage.現在のフェーズ == タイトル.タイトルステージ.フェーズ.キャンセル )
                    {
                        stage.非活性化する();
                        this.現在のステージ = this.終了ステージ;
                        this.現在のステージ.活性化する();
                    }
                    //----------------
                    #endregion
                    #region " 完了 → 認証ステージへ "
                    //----------------
                    if( stage.現在のフェーズ == タイトル.タイトルステージ.フェーズ.完了 )
                    {
                        stage.非活性化する();
                        this.現在のステージ = this.認証ステージ;
                        this.現在のステージ.活性化する();
                    }
                    //----------------
                    #endregion
                    break;

                case 認証.認証ステージ stage:
                    #region " キャンセル → タイトルステージへ "
                    //----------------
                    if( stage.現在のフェーズ == 認証.認証ステージ.フェーズ.キャンセル )
                    {
                        stage.非活性化する();
                        this.現在のステージ = this.タイトルステージ;
                        this.現在のステージ.活性化する();
                    }
                    //----------------
                    #endregion
                    #region " 完了 → 選曲ステージへ "
                    //----------------
                    if( stage.現在のフェーズ == 認証.認証ステージ.フェーズ.完了 )
                    {
                        stage.非活性化する();
                        this.現在のステージ = this.選曲ステージ;
                        this.現在のステージ.活性化する();
                    }
                    //----------------
                    #endregion
                    break;

                case 選曲.選曲ステージ stage:
                    #region " キャンセル → タイトルステージへ "
                    //----------------
                    if( stage.現在のフェーズ == 選曲.選曲ステージ.フェーズ.キャンセル )
                    {
                        stage.非活性化する();
                        this.現在のステージ = this.タイトルステージ;
                        this.現在のステージ.活性化する();
                    }
                    //----------------
                    #endregion
                    #region " 確定_選曲 → 曲読み込みステージへ "
                    //----------------
                    if( stage.現在のフェーズ == 選曲.選曲ステージ.フェーズ.確定_選曲 )
                    {
                        // 曲ツリーの現行化タスクが動いていれば、一時停止する。
                        曲ツリー.現行化タスクの一時停止.現在の状態 = TriStateEvent.状態種別.ON;

                        stage.非活性化する();
                        this.現在のステージ = this.曲読み込みステージ;
                        this.現在のステージ.活性化する();
                    }
                    //----------------
                    #endregion
                    #region " 確定_設定 → 設定ステージへ "
                    //----------------
                    if( stage.現在のフェーズ == 選曲.選曲ステージ.フェーズ.確定_設定 )
                    {
                        stage.非活性化する();
                        this.現在のステージ = this.オプション設定ステージ;
                        this.現在のステージ.活性化する();
                    }
                    //----------------
                    #endregion
                    break;

                case オプション設定.オプション設定ステージ stage:
                    #region " キャンセル/完了 → 選曲ステージへ "
                    //----------------
                    if( stage.現在のフェーズ == オプション設定.オプション設定ステージ.フェーズ.キャンセル ||
                        stage.現在のフェーズ == オプション設定.オプション設定ステージ.フェーズ.完了 )
                    {
                        stage.非活性化する();
                        this.現在のステージ = this.選曲ステージ;
                        this.現在のステージ.活性化する();
                    }
                    //----------------
                    #endregion
                    break;

                case 曲読み込み.曲読み込みステージ stage:
                    #region " 確定 → 演奏ステージへ "
                    //----------------
                    if( stage.現在のフェーズ == 曲読み込み.曲読み込みステージ.フェーズ.完了 )
                    {
                        stage.非活性化する();
                        this.現在のステージ = this.演奏ステージ;
                        this.演奏ステージ.活性化する();

                        // 曲読み込みステージ画面をキャプチャする（演奏ステージのクロスフェードで使う）
                        this.演奏ステージ.キャプチャ画面 = 画面キャプチャ.取得する();
                    }
                    //----------------
                    #endregion
                    break;

                case 演奏.演奏ステージ stage:
                    #region " キャンセル → 選曲ステージへ "
                    //----------------
                    if( stage.現在のフェーズ == 演奏.演奏ステージ.フェーズ.キャンセル完了 )
                    {
                        stage.非活性化する();
                        this.現在のステージ = this.選曲ステージ;
                        this.現在のステージ.活性化する();

                        // 曲ツリーの現行化タスクを再開する。
                        曲ツリー.現行化タスクの一時停止.現在の状態 = TriStateEvent.状態種別.OFF;
                    }
                    //----------------
                    #endregion
                    #region " クリア → 結果ステージへ "
                    //----------------
                    if( stage.現在のフェーズ == 演奏.演奏ステージ.フェーズ.クリア )
                    {
                        if( DTXMania.AppForm.ビュアーモードである )
                        {
                            // ビュアーモードならクリアフェーズを維持。（サービスメッセージ待ち。）
                        }
                        else
                        {
                            stage.非活性化する();
                            this.現在のステージ = this.結果ステージ;
                            this.現在のステージ.活性化する();
                        }
                    }
                    //----------------
                    #endregion
                    break;

                case 結果.結果ステージ stage:
                    #region " 確定 → 選曲ステージへ "
                    //----------------
                    if( stage.現在のフェーズ == 結果.結果ステージ.フェーズ.完了 )
                    {
                        stage.非活性化する();
                        this.現在のステージ = this.選曲ステージ;
                        this.現在のステージ.活性化する();

                        // 曲ツリーの現行化タスクを再開する。
                        曲ツリー.現行化タスクの一時停止.現在の状態 = TriStateEvent.状態種別.OFF;
                    }
                    //----------------
                    #endregion
                    break;

                case 終了.終了ステージ stage:
                    #region " 完了 → アプリ終了 "
                    //----------------
                    if( stage.現在のフェーズ == 終了.終了ステージ.フェーズ.完了 )
                    {
                        stage.非活性化する();
                        this.現在のステージ = null;
                        this._アプリを終了する();
                    }
                    //----------------
                    #endregion
                    break;
            }
        }

        protected override void On描画()
        {
            #region " 画面クリア "
            //----------------
            {
                var d3ddc = DXResources.Instance.D3D11Device1.ImmediateContext;

                // 既定のD3Dレンダーターゲットビューを黒でクリアする。
                d3ddc.ClearRenderTargetView( DXResources.Instance.既定のD3D11RenderTargetView, Color4.Black );

                // 深度バッファを 1.0f でクリアする。
                d3ddc.ClearDepthStencilView(
                    DXResources.Instance.既定のD3D11DepthStencilView,
                    SharpDX.Direct3D11.DepthStencilClearFlags.Depth | SharpDX.Direct3D11.DepthStencilClearFlags.Stencil,
                    depth: 1.0f,
                    stencil: 0 );
            }
            //----------------
            #endregion

            this.現在のステージ?.描画する();

            App進行描画.EffekseerManager?.進行描画する();
        }

        protected override void メッセージを処理する( 通知 msg )
        {
            switch( msg )
            {
                case ViewerPlayメッセージ msg2:
                    if( DTXMania.AppForm.ビュアーモードである )
                        this.演奏ステージ_ビュアーモード.ViewerPlay( msg2 );
                    break;

                case ViewerStopメッセージ msg2:
                    if( DTXMania.AppForm.ビュアーモードである )
                        this.演奏ステージ_ビュアーモード.ViewerStop( msg2 );
                    break;
            }

            base.メッセージを処理する( msg ); // 忘れずに
        }



        // ステージ


        protected 起動.起動ステージ 起動ステージ;

        protected タイトル.タイトルステージ タイトルステージ;

        protected 認証.認証ステージ 認証ステージ;

        protected 選曲.選曲ステージ 選曲ステージ;

        protected オプション設定.オプション設定ステージ オプション設定ステージ;

        protected 曲読み込み.曲読み込みステージ 曲読み込みステージ;

        protected 演奏.演奏ステージ 演奏ステージ;

        protected 演奏.演奏ステージ_ビュアーモード 演奏ステージ_ビュアーモード;

        protected 結果.結果ステージ 結果ステージ;

        protected 終了.終了ステージ 終了ステージ;



        // サイズ変更


        protected override void Onスワップチェーンに依存するグラフィックリソースの作成()
        {
            this.現在のステージ?.スワップチェーンに依存するリソースを復元する();
        }

        protected override void Onスワップチェーンに依存するグラフィックリソースの解放()
        {
            this.現在のステージ?.スワップチェーンに依存するリソースを解放する();
        }



        // IDTXManiaService の実装


        #region " IDTXManiaService.ViewerPlay "
        //----------------
        public AutoResetEvent ViewerPlay( string path, int startPart = 0, bool drumsSound = true )
        {
            var msg = new ViewerPlayメッセージ {
                path = path,
                startPart = startPart,
                drumSound = drumsSound,
            };
            this.メッセージキュー.Enqueue( msg );
            return msg.完了通知;
        }

        public class ViewerPlayメッセージ : 通知
        {
            public string path = "";
            public int startPart = 0;
            public bool drumSound = true;
        }
        //----------------
        #endregion

        #region " IDTXManiaService.ViewerStop "
        //----------------
        public AutoResetEvent ViewerStop()
        {
            var msg = new ViewerStopメッセージ();
            this.メッセージキュー.Enqueue( msg );
            return msg.完了通知;
        }

        public class ViewerStopメッセージ : 通知
        {
        }
        //----------------
        #endregion

        #region " IDTXManiaService.GetSoundDelay "
        //----------------
        public float GetSoundDelay()    // 常に同期
        {
            return (float) App進行描画.サウンドデバイス.再生遅延sec;
        }
        //----------------
        #endregion
    }
}
