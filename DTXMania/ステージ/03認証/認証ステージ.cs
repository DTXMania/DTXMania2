﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania.認証
{
    /// <summary>
    ///		ユーザ選択画面。
    /// </summary>
    class 認証ステージ : ステージ
    {
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
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
            }
        }

        public override void OnDispose()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                base.OnDispose();
            }
        }



        // 活性化と非活性化


        public override void On活性化()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this._舞台画像 = new 舞台画像();
                this._ウィンドウ画像 = new 画像( @"$(System)images\認証\ユーザ選択ウィンドウ.png" );
                this._プレイヤーを選択してください = new 文字列画像() {
                    表示文字列 = "プレイヤーを選択してください。",
                    フォントサイズpt = 30f,
                    描画効果 = 文字列画像.効果.ドロップシャドウ,
                };
                this._ユーザリスト = new ユーザリスト();
                this._システム情報 = new システム情報();

                App進行描画.アイキャッチ管理.現在のアイキャッチ.オープンする();

                App進行描画.システムサウンド.再生する( システムサウンド種別.認証ステージ_開始音 );
                App進行描画.システムサウンド.再生する( システムサウンド種別.認証ステージ_ループBGM, ループ再生する: true );

                this.現在のフェーズ = フェーズ.フェードイン;

                base.On活性化();
            }
        }

        public override void On非活性化()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this._システム情報?.Dispose();
                this._ユーザリスト?.Dispose();
                this._プレイヤーを選択してください?.Dispose();
                this._ウィンドウ画像?.Dispose();
                this._舞台画像?.Dispose();

                App進行描画.システムサウンド.停止する( システムサウンド種別.認証ステージ_開始音 );
                App進行描画.システムサウンド.停止する( システムサウンド種別.認証ステージ_ループBGM );
                //App進行描画.システムサウンド.停止する( システムサウンド種別.認証ステージ_ログイン音 );    // --> なりっぱなしでいい

                base.On非活性化();
            }
        }



        // 進行と描画


        public override void 進行する()
        {
            this._システム情報.FPSをカウントしプロパティを更新する();

            App進行描画.入力管理.すべての入力デバイスをポーリングする();

            switch( this.現在のフェーズ )
            {
                case フェーズ.ユーザ選択:
                    #region " ユーザを選択する。"
                    //----------------
                    if( App進行描画.入力管理.確定キーが入力された() )
                    {
                        #region " 確定 → フェードアウトへ"
                        //----------------
                        App進行描画.システムサウンド.停止する( システムサウンド種別.認証ステージ_ループBGM );
                        App進行描画.システムサウンド.再生する( システムサウンド種別.認証ステージ_ログイン音 );

                        App進行描画.アイキャッチ管理.アイキャッチを選択しクローズする( nameof( 回転幕 ) );   // アイキャッチを開始して次のフェーズへ。

                        this.現在のフェーズ = フェーズ.フェードアウト;
                        //----------------
                        #endregion
                    }
                    else if( App進行描画.入力管理.キャンセルキーが入力された() )
                    {
                        #region " キャンセル "
                        //----------------
                        App進行描画.システムサウンド.再生する( システムサウンド種別.取消音 );

                        this.現在のフェーズ = フェーズ.キャンセル;
                        //----------------
                        #endregion
                    }
                    else if( App進行描画.入力管理.上移動キーが入力された() )
                    {
                        #region " 上移動 → 前のユーザを選択 "
                        //----------------
                        App進行描画.システムサウンド.再生する( システムサウンド種別.カーソル移動音 );

                        this._ユーザリスト.前のユーザを選択する();
                        //----------------
                        #endregion
                    }
                    else if( App進行描画.入力管理.下移動キーが入力された() )
                    {
                        #region " 下移動 → 次のユーザを選択 "
                        //----------------
                        App進行描画.システムサウンド.再生する( システムサウンド種別.カーソル移動音 );

                        this._ユーザリスト.次のユーザを選択する();
                        //----------------
                        #endregion
                    }
                    //----------------
                    #endregion
                    break;
            }
        }

        public override void 描画する()
        {
            this._システム情報.VPSをカウントする();

            var 描画領域 = new RectangleF( 566f, 60f, 784f, 943f );

            var dc = DXResources.Instance.既定のD2D1DeviceContext;
            dc.Transform = DXResources.Instance.拡大行列DPXtoPX;

            switch( this.現在のフェーズ )
            {
                case フェーズ.フェードイン:
                    #region " アイキャッチを使って認証ステージをフェードインする。"
                    //----------------
                    // 認証画面を描画する。

                    this._舞台画像.進行描画する( dc, 黒幕付き: true );
                    this._ウィンドウ画像.描画する( dc, 描画領域.X, 描画領域.Y );
                    this._プレイヤーを選択してください.描画する( dc, 描画領域.X + 28f, 描画領域.Y + 45f );
                    this._ユーザリスト.進行描画する( dc );

                    // アイキャッチを描画する。

                    App進行描画.アイキャッチ管理.現在のアイキャッチ.進行描画する( dc );

                    if( App進行描画.アイキャッチ管理.現在のアイキャッチ.現在のフェーズ == アイキャッチ.フェーズ.オープン完了 )
                        this.現在のフェーズ = フェーズ.ユーザ選択;  // アイキャッチが終了したら次のフェーズへ。

                    // システム情報を描画する。

                    this._システム情報.描画する( dc );
                    //----------------
                    #endregion
                    break;

                case フェーズ.ユーザ選択:
                    #region " ユーザ選択画面を表示する。"
                    //----------------
                    // 認証画面を描画する。

                    this._舞台画像.進行描画する( dc, 黒幕付き: true );
                    this._ウィンドウ画像.描画する( dc, 描画領域.X, 描画領域.Y );
                    this._プレイヤーを選択してください.描画する( dc, 描画領域.X + 28f, 描画領域.Y + 45f );
                    this._ユーザリスト.進行描画する( dc );

                    // システム情報を描画する。

                    this._システム情報.描画する( dc );
                    //----------------
                    #endregion
                    break;

                case フェーズ.フェードアウト:
                    #region " アイキャッチを使って認証ステージをフェードアウトする。"
                    //----------------
                    // 認証画面を描画する。

                    this._舞台画像.進行描画する( dc, true );

                    
                    // アイキャッチを描画する。

                    App進行描画.アイキャッチ管理.現在のアイキャッチ.進行描画する( dc );


                    // アイキャッチが完了したらログインする。

                    if( App進行描画.アイキャッチ管理.現在のアイキャッチ.現在のフェーズ == アイキャッチ.フェーズ.クローズ完了 )
                    {
                        // 現在ログイン中のユーザがいればログオフする。
                        if( null != App進行描画.ユーザ管理.ログオン中のユーザ )
                        {
                            // ログオフ処理は特にない
                            Log.Info( $"ユーザ「{App進行描画.ユーザ管理.ログオン中のユーザ.ユーザ名}」をログオフしました。" );
                        }

                        // 選択中のユーザでログインする。
                        App進行描画.ユーザ管理.ユーザリスト.SelectItem( this._ユーザリスト.選択中のユーザ );
                        Log.Info( $"ユーザ「{App進行描画.ユーザ管理.ログオン中のユーザ.ユーザ名}」でログインしました。" );

                        // 曲ツリーの現行化を開始する。
                        App進行描画.曲ツリー.曲ツリーを現行化するAsync( new Action<Exception>( ( e ) => App進行描画.Instance.AppForm.例外を通知する( e ) ) ); // 非同期

                        this.現在のフェーズ = フェーズ.完了;
                    }

                    // システム情報を描画する。

                    this._システム情報.描画する( dc );
                    //----------------
                    #endregion
                    break;
            }
        }



        // private


        private 舞台画像 _舞台画像 = null;

        private 画像 _ウィンドウ画像 = null;

        private 文字列画像 _プレイヤーを選択してください = null;

        private ユーザリスト _ユーザリスト = null;

        private システム情報 _システム情報 = null;
    }
}
