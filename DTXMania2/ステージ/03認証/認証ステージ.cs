using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using FDK;

namespace DTXMania2.認証
{
    /// <summary>
    ///		ユーザ選択画面。
    /// </summary>
    class 認証ステージ : IStage
    {

        // プロパティ


        public enum フェーズ
        {
            フェードイン,
            ユーザ選択,
            フェードアウト,
            完了,
            キャンセル,
        }

        public フェーズ 現在のフェーズ { get; protected set; } = フェーズ.完了;



        // 生成と終了


        public 認証ステージ()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._舞台画像 = new 舞台画像();
            this._ウィンドウ画像 = new 画像( @"$(Images)\AuthStage\UserSelectFrame.png" );
            this._プレイヤーを選択してください = new 文字列画像D2D() {
                表示文字列 = "プレイヤーを選択してください。",
                フォントサイズpt = 30f,
                描画効果 = 文字列画像D2D.効果.ドロップシャドウ,
            };
            this._ユーザリスト = new ユーザリスト();
            this._システム情報 = new システム情報();

            Global.App.アイキャッチ管理.現在のアイキャッチ.オープンする();

            Global.App.システムサウンド.再生する( システムサウンド種別.認証ステージ_開始音 );
            Global.App.システムサウンド.再生する( システムサウンド種別.認証ステージ_ループBGM, ループ再生する: true );

            this.現在のフェーズ = フェーズ.フェードイン;
        }

        public void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._システム情報.Dispose();
            this._ユーザリスト.Dispose();
            this._プレイヤーを選択してください.Dispose();
            this._ウィンドウ画像.Dispose();
            this._舞台画像.Dispose();

            Global.App.システムサウンド.停止する( システムサウンド種別.認証ステージ_開始音 );
            Global.App.システムサウンド.停止する( システムサウンド種別.認証ステージ_ループBGM );
            //Global.App.システムサウンド.停止する( システムサウンド種別.認証ステージ_ログイン音 );    // --> なりっぱなしでいい
        }



        // 進行と描画


        public void 進行する()
        {
            this._システム情報.FPSをカウントしプロパティを更新する();

            Global.App.ドラム入力.すべての入力デバイスをポーリングする();

            switch( this.現在のフェーズ )
            {
                case フェーズ.ユーザ選択:

                    if( Global.App.ドラム入力.確定キーが入力された() )
                    {
                        #region " 確定 → フェードアウトへ"
                        //----------------
                        Global.App.システムサウンド.停止する( システムサウンド種別.認証ステージ_ループBGM );
                        Global.App.システムサウンド.再生する( システムサウンド種別.認証ステージ_ログイン音 );

                        Global.App.アイキャッチ管理.アイキャッチを選択しクローズする( nameof( 回転幕 ) );   // アイキャッチを開始して次のフェーズへ。
                        this.現在のフェーズ = フェーズ.フェードアウト;
                        //----------------
                        #endregion
                    }
                    else if( Global.App.ドラム入力.キャンセルキーが入力された() )
                    {
                        #region " キャンセル "
                        //----------------
                        Global.App.システムサウンド.再生する( システムサウンド種別.取消音 );
                        this.現在のフェーズ = フェーズ.キャンセル;
                        //----------------
                        #endregion
                    }
                    else if( Global.App.ドラム入力.上移動キーが入力された() )
                    {
                        #region " 上移動 → 前のユーザを選択 "
                        //----------------
                        Global.App.システムサウンド.再生する( システムサウンド種別.カーソル移動音 );
                        this._ユーザリスト.前のユーザを選択する();
                        //----------------
                        #endregion
                    }
                    else if( Global.App.ドラム入力.下移動キーが入力された() )
                    {
                        #region " 下移動 → 次のユーザを選択 "
                        //----------------
                        Global.App.システムサウンド.再生する( システムサウンド種別.カーソル移動音 );
                        this._ユーザリスト.次のユーザを選択する();
                        //----------------
                        #endregion
                    }
                    break;
            }
        }

        public void 描画する()
        {
            this._システム情報.VPSをカウントする();

            var 描画領域 = new RectangleF( 566f, 60f, 784f, 943f );

            var dc = Global.既定のD2D1DeviceContext;
            dc.Transform = Global.拡大行列DPXtoPX;

            switch( this.現在のフェーズ )
            {
                case フェーズ.フェードイン:
                    #region " アイキャッチを使って認証ステージをフェードインする。"
                    //----------------
                    this._舞台画像.進行描画する( dc, 黒幕付き: true );
                    this._ウィンドウ画像.描画する(  描画領域.X, 描画領域.Y );
                    this._プレイヤーを選択してください.描画する( dc, 描画領域.X + 28f, 描画領域.Y + 45f );
                    this._ユーザリスト.進行描画する( dc );

                    Global.App.アイキャッチ管理.現在のアイキャッチ.進行描画する( dc );
                    if( Global.App.アイキャッチ管理.現在のアイキャッチ.現在のフェーズ == アイキャッチ.フェーズ.オープン完了 )
                        this.現在のフェーズ = フェーズ.ユーザ選択;  // アイキャッチが終了したら次のフェーズへ。

                    this._システム情報.描画する( dc );
                    //----------------
                    #endregion
                    break;

                case フェーズ.ユーザ選択:
                    #region " ユーザ選択画面を表示する。"
                    //----------------
                    this._舞台画像.進行描画する( dc, 黒幕付き: true );
                    this._ウィンドウ画像.描画する( 描画領域.X, 描画領域.Y );
                    this._プレイヤーを選択してください.描画する( dc, 描画領域.X + 28f, 描画領域.Y + 45f );
                    this._ユーザリスト.進行描画する( dc );

                    this._システム情報.描画する( dc );
                    //----------------
                    #endregion
                    break;

                case フェーズ.フェードアウト:
                    #region " アイキャッチを使って認証ステージをフェードアウトする。"
                    //----------------
                    this._舞台画像.進行描画する( dc, true );

                    Global.App.アイキャッチ管理.現在のアイキャッチ.進行描画する( dc );

                    // アイキャッチが完了したらログインする。
                    if( Global.App.アイキャッチ管理.現在のアイキャッチ.現在のフェーズ == アイキャッチ.フェーズ.クローズ完了 )
                    {
                        // 現在ログイン中のユーザがいればログオフする。
                        if( null != Global.App.ログオン中のユーザ )
                        {
                            // ログオフ処理は特にない
                            Log.Info( $"ユーザ「{Global.App.ログオン中のユーザ.名前}」をログオフしました。" );
                        }

                        // 選択中のユーザでログインする。
                        Global.App.ユーザリスト.SelectItem( this._ユーザリスト.選択中のユーザ );
                        Log.Info( $"ユーザ「{Global.App.ログオン中のユーザ!.名前}」でログインしました。" );

                        this.現在のフェーズ = フェーズ.完了;
                    }

                    this._システム情報.描画する( dc );
                    //----------------
                    #endregion
                    break;
            }
        }



        // ローカル


        private readonly 舞台画像 _舞台画像;

        private readonly 画像 _ウィンドウ画像;

        private readonly 文字列画像D2D _プレイヤーを選択してください;

        private readonly ユーザリスト _ユーザリスト;

        private readonly システム情報 _システム情報;
    }
}
