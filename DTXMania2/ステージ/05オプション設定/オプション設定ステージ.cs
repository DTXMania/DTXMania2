using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using FDK;

namespace DTXMania2.オプション設定
{
    class オプション設定ステージ : IStage
    {

        // プロパティ


        public enum フェーズ
        {
            フェードイン,
            表示,
            入力割り当て,
            曲読み込みフォルダ割り当て,
            再起動,
            再起動待ち,
            フェードアウト,
            完了,
        }

        public フェーズ 現在のフェーズ { get; protected set; } = フェーズ.完了;



        // 生成と終了


        public オプション設定ステージ()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._舞台画像 = new 舞台画像();
            this._システム情報 = new システム情報();
            this._パネルリスト = new パネルリスト() {
                入力割り当てフェーズへ移行する = () => { this.現在のフェーズ = フェーズ.入力割り当て; },
                曲読み込みフォルダ割り当てフェーズへ移行する = () => { this.現在のフェーズ = フェーズ.曲読み込みフォルダ割り当て; },
                再起動フェーズへ移行する = () => { this.現在のフェーズ = フェーズ.再起動; },
                フェードアウトフェーズへ移行する = () => { this.現在のフェーズ = フェーズ.フェードアウト; },
            };
            this._再起動が必要 = false;
            Global.App.システムサウンド.再生する( システムサウンド種別.オプション設定ステージ_開始音 );
            this._舞台画像.ぼかしと縮小を適用する( 0.5 );

            this.現在のフェーズ = フェーズ.フェードイン;
            this._フェーズ完了 = false;
        }

        public void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._パネルリスト.Dispose();
            this._システム情報.Dispose();
            this._舞台画像.Dispose();
        }



        // 進行と描画


        public void 進行する()
        {
            this._システム情報.FPSをカウントしプロパティを更新する();

            var 入力 = Global.App.ドラム入力;
            入力.すべての入力デバイスをポーリングする();

            switch( this.現在のフェーズ )
            {
                case フェーズ.フェードイン:
                {
                    #region " フェードイン描画が完了したら表示フェーズへ。"
                    //----------------
                    if( this._フェーズ完了 )
                    {
                        this.現在のフェーズ = フェーズ.表示;
                        this._フェーズ完了 = false;
                    }
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.表示:
                {
                    #region " 入力処理。"
                    //----------------
                    if( 入力.キャンセルキーが入力された() )
                    {
                        // キャンセル

                        if( this._パネルリスト.現在のパネルフォルダ.親パネル is null )
                        {
                            #region " (A) 親ツリーがない → ステージをフェードアウトフェーズへ。"
                            //----------------
                            Global.App.システムサウンド.再生する( システムサウンド種別.取消音 );

                            this._パネルリスト.フェードアウトを開始する();
                            Global.App.アイキャッチ管理.アイキャッチを選択しクローズする( nameof( 半回転黒フェード ) );
                            this.現在のフェーズ = フェーズ.フェードアウト;
                            //----------------
                            #endregion
                        }
                        else
                        {
                            #region " (B) 親ツリーがある → 親ツリーへ戻る。"
                            //----------------
                            Global.App.システムサウンド.再生する( システムサウンド種別.取消音 );

                            this._パネルリスト.親のパネルを選択する();
                            this._パネルリスト.フェードインを開始する();
                            //----------------
                            #endregion
                        }
                    }
                    else if( 入力.上移動キーが入力された() )
                    {
                        #region " 上移動 "
                        //----------------
                        Global.App.システムサウンド.再生する( システムサウンド種別.カーソル移動音 );
                        this._パネルリスト.前のパネルを選択する();
                        //----------------
                        #endregion
                    }
                    else if( 入力.下移動キーが入力された() )
                    {
                        #region " 下移動 "
                        //----------------
                        Global.App.システムサウンド.再生する( システムサウンド種別.カーソル移動音 );
                        this._パネルリスト.次のパネルを選択する();
                        //----------------
                        #endregion
                    }
                    else if( 入力.左移動キーが入力された() )
                    {
                        #region " 左移動 "
                        //----------------
                        Global.App.システムサウンド.再生する( システムサウンド種別.変更音 );
                        this._パネルリスト.現在選択中のパネル!.左移動キーが入力された();
                        //----------------
                        #endregion
                    }
                    else if( 入力.右移動キーが入力された() )
                    {
                        #region " 右移動 "
                        //----------------
                        Global.App.システムサウンド.再生する( システムサウンド種別.変更音 );
                        this._パネルリスト.現在選択中のパネル!.右移動キーが入力された();
                        //----------------
                        #endregion
                    }
                    else if( 入力.確定キーが入力された() )
                    {
                        #region " 確定 "
                        //----------------
                        Global.App.システムサウンド.再生する( システムサウンド種別.変更音 );
                        this._パネルリスト.現在選択中のパネル!.確定キーが入力された();
                        //----------------
                        #endregion
                    }
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.入力割り当て:
                {
                    #region " 入力割り当てが完了したら表示フェーズへ。"
                    //----------------
                    if( this._フェーズ完了 )
                    {
                        this.現在のフェーズ = フェーズ.表示;
                        this._フェーズ完了 = false;
                    }
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.曲読み込みフォルダ割り当て:
                {
                    #region " 割り当てが完了したら次のフェーズへ。"
                    //----------------
                    if( this._フェーズ完了 )
                    {
                        this.現在のフェーズ = ( this._再起動が必要 ) ? フェーズ.再起動 : フェーズ.表示;
                        this._フェーズ完了 = false;
                    }
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.再起動:
                {
                    #region " 再起動の仕込みが完了すれば次のフェーズへ。 "
                    //----------------
                    if( this._フェーズ完了 )
                    {
                        this.現在のフェーズ = フェーズ.再起動待ち;
                        this._フェーズ完了 = false;
                    }
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.再起動待ち:
                {
                    #region " 遷移終了。Appによる再起動待ち。"
                    //----------------
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.フェードアウト:
                {
                    #region " フェードアウト描画が完了したら次のフェーズへ。"
                    //----------------
                    if( this._フェーズ完了 )
                    {
                        this.現在のフェーズ = フェーズ.完了;
                        this._フェーズ完了 = false;
                    }
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.完了:
                {
                    #region " 遷移終了。Appによるステージ遷移を待つ。"
                    //----------------
                    break;
                    //----------------
                    #endregion
                }
            }

            #region " 画面モードが外部（F11キーなど）で変更されている場合には、それを「画面モード」パネルにも反映する。"
            //----------------
            {
                var 画面モードパネル = this._パネルリスト.パネルツリーのルートノード.子パネルリスト.Find( ( p ) => ( p.パネル名 == "画面モード" ) ) as パネル_文字列リスト;

                if( 画面モードパネル is null )  // 念のため
                    throw new Exception( "「画面モード」パネルが存在していません。" );

                int システム設定上の現在の画面モード = ( Global.App.システム設定.全画面モードである ) ? 1 : 0; // 0:ウィンドウ, 1:全画面

                if( 画面モードパネル.現在選択されている選択肢の番号 != システム設定上の現在の画面モード )
                    画面モードパネル.現在選択されている選択肢の番号 = システム設定上の現在の画面モード;
            }
            //----------------
            #endregion
        }

        public void 描画する()
        {
            this._システム情報.VPSをカウントする();

            var dc = Global.既定のD2D1DeviceContext;
            dc.Transform = Global.拡大行列DPXtoPX;

            switch( this.現在のフェーズ )
            {
                case フェーズ.フェードイン:
                {
                    #region " 背景画面＆フェードイン "
                    //----------------
                    this._舞台画像.進行描画する( dc );
                    this._パネルリスト.進行描画する( dc, 613f, 0f );
                    this._システム情報.描画する( dc );

                    if( !this._フェーズ完了 )
                    {
                        this._パネルリスト.フェードインを開始する();
                        this._フェーズ完了 = true;    // 完了
                    }
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.表示:
                case フェーズ.再起動待ち:
                {
                    #region " 背景画面 "
                    //----------------
                    this._舞台画像.進行描画する( dc );
                    this._パネルリスト.進行描画する( dc, 613f, 0f );
                    this._システム情報.描画する( dc );
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.入力割り当て:
                {
                    #region " 入力割り当てダイアログ "
                    //----------------
                    this._舞台画像.進行描画する( dc );
                    this._パネルリスト.進行描画する( dc, 613f, 0f );
                    this._システム情報.描画する( dc );

                    if( !this._フェーズ完了 )
                    {
                        var 完了通知 = new ManualResetEvent( false );

                        // 入力割り当てダイアログを表示。
                        Global.AppForm.BeginInvoke( new Action( () => {

                            using var dlg = new 入力割り当てダイアログ();

                            Cursor.Show();  // いったんマウスカーソル表示

                            dlg.表示する();

                            if( Global.AppForm.ScreenMode.IsFullscreenMode )
                                Cursor.Hide();  // 全画面ならマウスカーソルを消す。

                            完了通知.Set();

                        } ) );

                        完了通知.WaitOne();

                        this._パネルリスト.フェードインを開始する();
                        this._フェーズ完了 = true;    // 完了
                    }
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.曲読み込みフォルダ割り当て:
                {
                    #region " 曲読み込みフォルダ割り当てダイアログ "
                    //----------------
                    this._舞台画像.進行描画する( dc );
                    this._パネルリスト.進行描画する( dc, 613f, 0f );
                    this._システム情報.描画する( dc );

                    if( !this._フェーズ完了 )
                    {
                        var 完了通知 = new ManualResetEvent( false );

                        Global.AppForm.BeginInvoke( new Action( () => {

                            using var dlg = new 曲読み込みフォルダ割り当てダイアログ( Global.App.システム設定.曲検索フォルダ );

                            Cursor.Show();  // いったんマウスカーソル表示

                            if( dlg.ShowDialog() == DialogResult.OK &&
                                dlg.新しい曲検索フォルダリストを取得する( out List<VariablePath> 新フォルダリスト ) )
                            {
                                // 曲検索フォルダを新しいリストに更新。
                                Global.App.システム設定.曲検索フォルダ.Clear();
                                Global.App.システム設定.曲検索フォルダ.AddRange( 新フォルダリスト );
                                Global.App.システム設定.保存する();

                                // 再起動へ。
                                this._再起動が必要 = true;
                            }
                            else
                            {
                                this._再起動が必要 = false;
                            }

                            if( Global.AppForm.ScreenMode.IsFullscreenMode )
                                Cursor.Hide();  // 全画面ならマウスカーソルを消す。

                            完了通知.Set();

                        } ) );

                        完了通知.WaitOne();

                        this._フェーズ完了 = true;    // 完了
                    }
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.再起動:
                {
                    #region " UIスレッドで再起動を実行 "
                    //----------------
                    this._舞台画像.進行描画する( dc );
                    this._パネルリスト.進行描画する( dc, 613f, 0f );
                    this._システム情報.描画する( dc );

                    if( !this._フェーズ完了 )
                    {
                        Global.AppForm.BeginInvoke( new Action( () => {
                            Global.AppForm.再起動する();     // UIスレッドで実行
                        } ) );
                        this._フェーズ完了 = true;    // 完了
                    }
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.フェードアウト:
                {
                    #region " 背景画面＆フェードアウト "
                    //----------------
                    this._舞台画像.進行描画する( dc );
                    this._パネルリスト.進行描画する( dc, 613f, 0f );
                    
                    if( Global.App.アイキャッチ管理.現在のアイキャッチ.進行描画する( dc ) == アイキャッチ.フェーズ.クローズ完了 )
                        this._フェーズ完了 = true;    // 完了

                    this._システム情報.描画する( dc );
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.完了:
                {
                    #region " 最後の画面を維持。"
                    //----------------
                    break;
                    //----------------
                    #endregion
                }
            }
        }



        // ローカル


        private readonly 舞台画像 _舞台画像;

        private readonly システム情報 _システム情報;

        private readonly パネルリスト _パネルリスト;

        private bool _フェーズ完了;

        private bool _再起動が必要;
    }
}
