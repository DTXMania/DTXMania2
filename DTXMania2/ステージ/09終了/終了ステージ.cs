using System;
using System.Collections.Generic;
using System.Diagnostics;

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


        public void 進行する()
        {
            switch( this.現在のフェーズ )
            {
                case フェーズ.開始:
                    Global.App.システムサウンド.再生する( システムサウンド種別.終了ステージ_開始音 );
                    this._カウンタ = new Counter( 0, 1, 値をひとつ増加させるのにかける時間ms: 1000 );
                    this.現在のフェーズ = フェーズ.表示中;
                    break;

                case フェーズ.表示中:
                    if( this._カウンタ.終了値に達した )
                        this.現在のフェーズ = フェーズ.開始音終了待ち;
                    break;

                case フェーズ.開始音終了待ち:
                    if( !Global.App.システムサウンド.再生中( システムサウンド種別.終了ステージ_開始音 ) )
                        this.現在のフェーズ = フェーズ.完了; // 再生が終わったらフェーズ遷移。
                    break;
            }
        }

        public void 描画する()
        {
            var dc = Global.既定のD2D1DeviceContext;
            dc.Transform = Global.拡大行列DPXtoPX;

            switch( this.現在のフェーズ )
            {
                case フェーズ.表示中:
                case フェーズ.開始音終了待ち:
                    this._背景画像.描画する( 0f, 0f );
                    break;
            }
        }



        // ローカル


        private readonly 画像 _背景画像;

        private Counter _カウンタ = null!;

    }
}
