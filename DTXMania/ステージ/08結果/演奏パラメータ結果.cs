using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Animation;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania.結果
{
    class 演奏パラメータ結果 : 演奏.判定パラメータ表示
    {

        // 生成と終了


        public 演奏パラメータ結果()
            : base()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this._フルコンボ再生済み = false;

                var animation = DXResources.Instance.Animation;

                this._パラメータアニメ = new パラメータアニメ( animation.Manager );
                this._パラメータアニメ.X位置オフセット = new Variable[ 6 ];
                this._パラメータアニメ.不透明度 = new Variable[ 6 ];

                for( int i = 0; i < 6; i++ )
                {
                    const float 開始Xオフセット = +50f;

                    this._パラメータアニメ.X位置オフセット[ i ] = new Variable( animation.Manager, initialValue: 開始Xオフセット );
                    this._パラメータアニメ.不透明度[ i ] = new Variable( animation.Manager, initialValue: 0.0 );

                    // 不透明度のストーリーボード 
                    using( var 遅延 = animation.TrasitionLibrary.Constant( duration: i * 0.05 ) )
                    using( var 不透明度の遷移 = animation.TrasitionLibrary.Linear( duration: 0.8, finalValue: 1.0 ) )
                    {
                        this._パラメータアニメ.ストーリーボード.AddTransition( this._パラメータアニメ.不透明度[ i ], 遅延 );
                        this._パラメータアニメ.ストーリーボード.AddTransition( this._パラメータアニメ.不透明度[ i ], 不透明度の遷移 );
                    }

                    // X位置オフセットのストーリーボード
                    using( var 遅延 = animation.TrasitionLibrary.Constant( duration: i * 0.05 ) )
                    using( var 遷移1 = animation.TrasitionLibrary.Cubic( duration: 0.2, finalValue: +0.0, finalVelocity: -200.0 ) )    // 左へスライド
                    using( var 遷移2 = animation.TrasitionLibrary.Reversal( duration: 0.2 ) )      // 方向転換
                    {
                        this._パラメータアニメ.ストーリーボード.AddTransition( this._パラメータアニメ.X位置オフセット[ i ], 遅延 );
                        this._パラメータアニメ.ストーリーボード.AddTransition( this._パラメータアニメ.X位置オフセット[ i ], 遷移1 );
                        this._パラメータアニメ.ストーリーボード.AddTransition( this._パラメータアニメ.X位置オフセット[ i ], 遷移2 );
                    }
                }

                // 開始。
                this._パラメータアニメ.ストーリーボード.Schedule( animation.Timer.Time );
            }
        }

        public override void Dispose()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this._パラメータアニメ?.Dispose();

                base.Dispose();
            }
        }



        // 進行と描画


        public override void 描画する( DeviceContext dc, float x, float y, 成績 現在の成績 )
        {
            // パラメータアニメが完了してからフルコンボチェック。

            if( this._パラメータアニメ.ストーリーボード.Status == StoryboardStatus.Ready && !(this._フルコンボ再生済み ) )
            {
                if( 現在の成績.MaxCombo == 現在の成績.総ノーツ数 )
                    App進行描画.システムサウンド.再生する( システムサウンド種別.フルコンボ );

                this._フルコンボ再生済み = true; // 再生してようがしてまいが
            }

            DXResources.Instance.D2DBatchDraw( dc, () => {

                var 拡大率 = new Size2F( 1.4f, 1.3f ); // 画像が小さいので少々拡大。

                var 割合表 = 現在の成績.判定toヒット割合;
                int 合計 = 0;

                float 基点X = x;

                x = 基点X + (float) this._パラメータアニメ.X位置オフセット[ 0 ].Value;
                this.パラメータを一行描画する( dc, x, y, 拡大率, 判定種別.PERFECT, 現在の成績.判定toヒット数[ 判定種別.PERFECT ], 割合表[ 判定種別.PERFECT ], (float) this._パラメータアニメ.不透明度[ 0 ].Value );
                合計 += 現在の成績.判定toヒット数[ 判定種別.PERFECT ];
                y += _改行幅dpx;

                x = 基点X + (float) this._パラメータアニメ.X位置オフセット[ 1 ].Value;
                this.パラメータを一行描画する( dc, x, y, 拡大率, 判定種別.GREAT, 現在の成績.判定toヒット数[ 判定種別.GREAT ], 割合表[ 判定種別.GREAT ], (float) this._パラメータアニメ.不透明度[ 1 ].Value );
                合計 += 現在の成績.判定toヒット数[ 判定種別.GREAT ];
                y += _改行幅dpx;

                x = 基点X + (float) this._パラメータアニメ.X位置オフセット[ 2 ].Value;
                this.パラメータを一行描画する( dc, x, y, 拡大率, 判定種別.GOOD, 現在の成績.判定toヒット数[ 判定種別.GOOD ], 割合表[ 判定種別.GOOD ], (float) this._パラメータアニメ.不透明度[ 2 ].Value );
                合計 += 現在の成績.判定toヒット数[ 判定種別.GOOD ];
                y += _改行幅dpx;

                x = 基点X + (float) this._パラメータアニメ.X位置オフセット[ 3 ].Value;
                this.パラメータを一行描画する( dc, x, y, 拡大率, 判定種別.OK, 現在の成績.判定toヒット数[ 判定種別.OK ], 割合表[ 判定種別.OK ], (float) this._パラメータアニメ.不透明度[ 3 ].Value );
                合計 += 現在の成績.判定toヒット数[ 判定種別.OK ];
                y += _改行幅dpx;

                x = 基点X + (float) this._パラメータアニメ.X位置オフセット[ 4 ].Value;
                this.パラメータを一行描画する( dc, x, y, 拡大率, 判定種別.MISS, 現在の成績.判定toヒット数[ 判定種別.MISS ], 割合表[ 判定種別.MISS ], (float) this._パラメータアニメ.不透明度[ 4 ].Value );
                合計 += 現在の成績.判定toヒット数[ 判定種別.MISS ];
                y += _改行幅dpx;

                x = 基点X + (float) this._パラメータアニメ.X位置オフセット[ 5 ].Value;
                var 矩形 = this._判定種別文字の矩形リスト[ "MaxCombo" ];
                this._判定種別文字.描画する( dc, x, y, 転送元矩形: 矩形, 不透明度0to1: (float) this._パラメータアニメ.不透明度[ 5 ].Value );

                x += 矩形.Width + 16f;
                this.数値を描画する( dc, x, y, 拡大率, 現在の成績.MaxCombo, 4, (float) this._パラメータアニメ.不透明度[ 5 ].Value );
                this.数値を描画する( dc, x + _dr * 拡大率.Width, y, 拡大率, (int) Math.Floor( 100.0 * 現在の成績.MaxCombo / 合計 ), 3, (float) this._パラメータアニメ.不透明度[ 5 ].Value );    // 切り捨てでいいやもう
                this._パラメータ文字.不透明度 = (float) this._パラメータアニメ.不透明度[ 5 ].Value;
                this._パラメータ文字.描画する( dc, x + _dp * 拡大率.Width, y, "%", 拡大率 );

            } );
        }



        // private


        private class パラメータアニメ : IDisposable
        {
            public Variable[] X位置オフセット = null;
            public Variable[] 不透明度 = null;
            public Storyboard ストーリーボード = null;

            public パラメータアニメ( Manager am )
            {
                this.ストーリーボード = new Storyboard( am );
            }
            public void Dispose()
            {
                this.ストーリーボード?.Abandon();
                this.ストーリーボード?.Dispose();

                for( int i = 0; i < 6; i++ )
                {
                    this.X位置オフセット[i]?.Dispose();
                    this.不透明度[i]?.Dispose();
                }

                this.X位置オフセット = null;
                this.不透明度 = null;
            }
        };

        private パラメータアニメ _パラメータアニメ = null;

        protected new const float _改行幅dpx = 36f;


        private bool _フルコンボ再生済み = false;
    }
}
