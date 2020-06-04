﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct2D1;
using FDK;
using System.Threading;
using Windows.ApplicationModel.Background;

namespace DTXMania2.タイトル
{
    class タイトルステージ : IStage
    {

        // プロパティ


        public enum フェーズ
        {
            表示,
            フェードアウト,
            完了,
            キャンセル,
        }

        public フェーズ 現在のフェーズ { get; protected set; } = フェーズ.完了;



        // 生成と終了


        public タイトルステージ()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._舞台画像 = new 舞台画像();
            this._システム情報 = new システム情報();
            this._タイトルロゴ = new 画像( @"$(Images)\TitleLogo.png" );
            this._帯ブラシ = new SolidColorBrush( Global.既定のD2D1DeviceContext, new Color4( 0f, 0f, 0f, 0.8f ) );
            this._パッドを叩いてください = new 文字列画像D2D() {
                表示文字列 = "パッドを叩いてください",
                フォントサイズpt = 40f,
                描画効果 = 文字列画像D2D.効果.縁取り,
            };

            Global.App.システムサウンド.再生する( システムサウンド種別.タイトルステージ_開始音 );
            Global.App.システムサウンド.再生する( システムサウンド種別.タイトルステージ_ループBGM, ループ再生する: true );

            Global.App.ログオフする();

            // 最初のフェーズへ。
            this.現在のフェーズ = フェーズ.表示;
            this._フェーズ完了 = false;
        }

        public void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            Global.App.システムサウンド.停止する( システムサウンド種別.タイトルステージ_開始音 );
            Global.App.システムサウンド.停止する( システムサウンド種別.タイトルステージ_ループBGM );
            //Global.App.システムサウンド.停止する( システムサウンド種別.タイトルステージ_確定音 );  --> ならしっぱなしでいい

            this._パッドを叩いてください.Dispose();
            this._帯ブラシ.Dispose();
            this._タイトルロゴ.Dispose();
            this._システム情報.Dispose();
            this._舞台画像.Dispose();
        }



        // 進行と描画


        public void 進行描画する()
        {
            // 進行

            this._システム情報.FPSをカウントしプロパティを更新する();
            Global.App.ドラム入力.すべての入力デバイスをポーリングする();

            switch( this.現在のフェーズ )
            {
                case フェーズ.表示:
                {
                    #region " 入力処理。"
                    //----------------
                    if( Global.App.ドラム入力.確定キーが入力された() )
                    {
                        #region " 確定 → クローズアイキャッチを開始してフェードアウトへ "
                        //----------------
                        Global.App.システムサウンド.再生する( システムサウンド種別.タイトルステージ_確定音 );
                        Global.App.アイキャッチ管理.アイキャッチを選択しクローズする( nameof( シャッター ) );

                        this.現在のフェーズ = フェーズ.フェードアウト;
                        //----------------
                        #endregion
                    }
                    else if( Global.App.ドラム入力.キャンセルキーが入力された() )
                    {
                        #region " キャンセル → キャンセルフェーズへ "
                        //----------------
                        this.現在のフェーズ = フェーズ.キャンセル;
                        //----------------
                        #endregion
                    }
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.フェードアウト:
                {
                    #region " フェードアウト描画が完了したら完了フェーズへ。"
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
                case フェーズ.キャンセル:
                case フェーズ.完了:
                {
                    #region " 遷移終了。Appによるステージ遷移を待つ。"
                    //----------------
                    break;
                    //----------------
                    #endregion
                }
            }


            // 描画

            this._システム情報.VPSをカウントする();

            var dc = Global.既定のD2D1DeviceContext;
            dc.Transform = Global.拡大行列DPXtoPX;

            switch( this.現在のフェーズ )
            {
                case フェーズ.表示:
                {
                    #region " タイトル画面。"
                    //----------------
                    this._舞台画像.進行描画する( dc );

                    this._タイトルロゴ.描画する(
                        ( Global.設計画面サイズ.Width - this._タイトルロゴ.サイズ.Width ) / 2f,
                        ( Global.設計画面サイズ.Height - this._タイトルロゴ.サイズ.Height ) / 2f - 100f );

                    this._帯メッセージを描画する( dc );

                    this._システム情報.描画する( dc );
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.フェードアウト:
                {
                    #region " タイトル画面＆フェードアウト。"
                    //----------------
                    this._舞台画像.進行描画する( dc );

                    this._タイトルロゴ.描画する(
                        ( Global.設計画面サイズ.Width - this._タイトルロゴ.サイズ.Width ) / 2f,
                        ( Global.設計画面サイズ.Height - this._タイトルロゴ.サイズ.Height ) / 2f - 100f );

                    this._帯メッセージを描画する( dc );

                    if( Global.App.アイキャッチ管理.現在のアイキャッチ.進行描画する( dc ) == アイキャッチ.フェーズ.クローズ完了 )
                        this._フェーズ完了 = true;    // 完了

                    this._システム情報.描画する( dc );
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.キャンセル:
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

        private readonly 画像 _タイトルロゴ;

        private readonly Brush _帯ブラシ;

        private readonly 文字列画像D2D _パッドを叩いてください;

        private bool _フェーズ完了;


        private void _帯メッセージを描画する( DeviceContext dc )
        {
            var 領域 = new RectangleF( 0f, 800f, Global.設計画面サイズ.Width, 80f );

            D2DBatch.Draw( dc, () => {
                dc.FillRectangle( 領域, this._帯ブラシ );
            } );

            this._パッドを叩いてください.描画する( dc, 720f, 810f );
        }
    }
}
