﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using FDK;

namespace DTXMania2.終了
{
    class 終了ステージ : IStage
    {

        // プロパティ


        public enum フェーズ
        {
            開始,
            表示中,
            開始音終了待ち,
            完了,
        }

        public フェーズ 現在のフェーズ { get; protected set; } = フェーズ.完了;



        // 生成と終了


        public 終了ステージ()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._背景画像 = new 画像( @"$(Images)\ExitStage\Background.jpg" );
            
            // 最初のフェーズへ。
            this.現在のフェーズ = フェーズ.開始;
        }

        public void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._背景画像?.Dispose();
        }




        // 進行と描画


        public void 進行描画する()
        {
            // 進行

            switch( this.現在のフェーズ )
            {
                case フェーズ.開始:
                {
                    #region " 終了ステージ開始音を再生し、表示中フェーズへ。"
                    //----------------
                    Global.App.システムサウンド.再生する( システムサウンド種別.終了ステージ_開始音 );
                    this._カウンタ = new Counter( 0, 1, 値をひとつ増加させるのにかける時間ms: 1000 );
                    this.現在のフェーズ = フェーズ.表示中;
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.表示中:
                {
                    #region " 一定時間が経過したら開始音終了待ちフェーズへ。"
                    //----------------
                    if( this._カウンタ.終了値に達した )
                        this.現在のフェーズ = フェーズ.開始音終了待ち;
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.開始音終了待ち:
                {
                    #region " 開始音の再生が終わったら完了フェーズへ。 "
                    //----------------
                    if( !Global.App.システムサウンド.再生中( システムサウンド種別.終了ステージ_開始音 ) )
                        this.現在のフェーズ = フェーズ.完了;
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.完了:
                {
                    #region " 遷移終了。Appによるステージ遷移待ち。"
                    //----------------
                    break;
                    //----------------
                    #endregion
                }
            }


            // 描画

            var dc = Global.既定のD2D1DeviceContext;
            dc.Transform = Global.拡大行列DPXtoPX;

            switch( this.現在のフェーズ )
            {
                case フェーズ.開始:
                case フェーズ.表示中:
                case フェーズ.開始音終了待ち:
                {
                    #region " 背景画像 "
                    //----------------
                    this._背景画像.描画する( 0f, 0f );
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


        private readonly 画像 _背景画像;

        private Counter _カウンタ = null!;

    }
}
