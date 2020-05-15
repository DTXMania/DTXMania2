using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Animation;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania2.演奏
{
    class コンボ表示 : IDisposable
    {

        // 生成と終了


        public コンボ表示()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._コンボ文字画像 = new 画像D2D( @"$(Images)\PlayStage\ComboNumber.png" );
            this._コンボ文字の矩形リスト = new 矩形リスト( @"$(Images)\PlayStage\ComboNumber.yaml" );

            this._前回表示した値 = 0;
            this._前回表示した数字 = "    ";

            this._各桁のアニメ = new 各桁のアニメ[ 4 ];
            for( int i = 0; i < this._各桁のアニメ.Length; i++ )
                this._各桁のアニメ[ i ] = new 各桁のアニメ();

            this._百ごとのアニメ = new 百ごとのアニメ();
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._百ごとのアニメ.Dispose();

            for( int i = 0; i < this._各桁のアニメ.Length; i++ )
                this._各桁のアニメ[ i ].Dispose();

            this._コンボ文字画像.Dispose();
        }



        // 進行と描画


        /// <param name="全体の中央位置">
        ///		パネル(dc)の左上を原点とする座標。
        /// </param>
        public void 進行描画する( DeviceContext dc, Vector2 全体の中央位置, 成績 現在の成績 )
        {
            int Combo値 = Math.Clamp( 現在の成績.Combo, min: 0, max: 9999 );  // 表示は9999でカンスト。

            if( Combo値 < 10 )
                return; // 10未満は表示しない。

            if( ( this._前回表示した値 % 100 ) > ( Combo値 % 100 ) )
            {
                // 100を超えるたびアニメ開始。
                this._百ごとのアニメ.開始( Global.Animation );
            }


            var 数字 = Combo値.ToString().PadLeft( 4 ).Replace( ' ', 'o' ); // 右詰め4桁、余白は 'o'。
            var 画像矩形から表示矩形への拡大率 = new Vector2( 264f / ( 142f * 4f ), 140f / 188f );
            var 文字間隔補正 = -10f;
            var 全体の拡大率 = new Vector2( (float) ( this._百ごとのアニメ.拡大率?.Value ?? 1.0 ) );

            // 全体のサイズを算出。
            var 全体のサイズ = new Vector2( 0f, 0f );
            for( int i = 0; i < 数字.Length; i++ )
            {
                var 矩形 = this._コンボ文字の矩形リスト[ 数字[ i ].ToString() ]!;
                全体のサイズ.X += 矩形.Value.Width + 文字間隔補正;                // 合計
                全体のサイズ.Y = Math.Max( 全体のサイズ.Y, 矩形.Value.Height );   // 最大値
            }
            全体のサイズ *= 画像矩形から表示矩形への拡大率;

            // 全体の位置を修正。
            全体の中央位置.Y -= 全体のサイズ.Y / 2f;
            var 振動幅 = (float) ( this._百ごとのアニメ.振動幅?.Value ?? 0.0f );
            if( 0.0f < 振動幅 )
            {
                全体の中央位置.X += Global.App.乱数.NextFloat( -振動幅, +振動幅 );
                全体の中央位置.Y += Global.App.乱数.NextFloat( -振動幅, +振動幅 );
            }


            // １桁ずつ描画。

            D2DBatch.Draw( dc, () => {

                var pretrans = dc.Transform;

                #region " 数字を描画。"
                //----------------
                {
                    var 文字の位置 = new Vector2( -( 全体のサイズ.X / 2f ), 0f );

                    for( int i = 0; i < 数字.Length; i++ )
                    {
                        if( 数字[ i ] != this._前回表示した数字[ i ] )
                        {
                            // 桁アニメーション開始
                            this._各桁のアニメ[ i ].落下開始( Global.Animation );

                            // 1の位以外は、自分より上位の桁を順番に跳ねさせる。
                            if( 3 > i )
                            {
                                for( int p = ( i - 1 ); p >= 0; p-- )
                                    this._各桁のアニメ[ p ].跳ね開始( Global.Animation, 0.05 * ( ( i - 1 ) - p + 1 ) );
                            }
                        }

                        var 転送元矩形 = ( this._コンボ文字の矩形リスト[ 数字[ i ].ToString() ] )!;

                        dc.Transform =
                            Matrix3x2.Scaling( 画像矩形から表示矩形への拡大率 ) *
                            Matrix3x2.Translation( 文字の位置.X, 文字の位置.Y + (float) ( this._各桁のアニメ[ i ].Yオフセット?.Value ?? 0.0f ) ) *
                            Matrix3x2.Scaling( 全体の拡大率.X, 全体の拡大率.Y, center: new Vector2( 0f, 全体のサイズ.Y / 2f ) ) *
                            Matrix3x2.Translation( 全体の中央位置 ) *
                            pretrans;

                        dc.DrawBitmap( this._コンボ文字画像.Bitmap, (float) ( this._各桁のアニメ[ i ].不透明度?.Value ?? 1.0f ), BitmapInterpolationMode.Linear, 転送元矩形.Value );

                        文字の位置.X += ( 転送元矩形.Value.Width + 文字間隔補正 ) * 画像矩形から表示矩形への拡大率.X;
                    }
                }
                //----------------
                #endregion

                #region " Combo を描画。"
                //----------------
                {
                    var 転送元矩形 = this._コンボ文字の矩形リスト[ "Combo" ]!;
                    var 文字の位置 = new Vector2( 0f, 130f );

                    dc.Transform =
                        Matrix3x2.Scaling( 画像矩形から表示矩形への拡大率 ) *
                        Matrix3x2.Translation( 文字の位置 ) *
                        Matrix3x2.Scaling( 全体の拡大率 ) *
                        Matrix3x2.Translation( 全体の中央位置 ) *
                        pretrans;

                    dc.DrawBitmap( this._コンボ文字画像.Bitmap, 1.0f, BitmapInterpolationMode.Linear, 転送元矩形.Value );
                }
                //----------------
                #endregion

            } );

            // 保存
            this._前回表示した値 = 現在の成績.Combo;
            this._前回表示した数字 = 数字;
        }



        // ローカル


        private int _前回表示した値 = 0;

        private string _前回表示した数字 = "    ";

        private readonly 画像D2D _コンボ文字画像;

        private readonly 矩形リスト _コンボ文字の矩形リスト;


        // 桁ごとのアニメーション

        private class 各桁のアニメ : IDisposable
        {
            public Storyboard ストーリーボード = null!;
            public Variable Yオフセット = null!;
            public Variable 不透明度 = null!;

            public 各桁のアニメ()
            {
            }
            public void Dispose()
            {
                this.ストーリーボード?.Dispose();
                this.Yオフセット?.Dispose();
                this.不透明度?.Dispose();
            }

            public void 落下開始( Animation am )
            {
                this.Dispose();

                this.ストーリーボード = new Storyboard( am.Manager );
                this.Yオフセット = new Variable( am.Manager, initialValue: -70.0 );
                this.不透明度 = new Variable( am.Manager, initialValue: 1.0 );

                var Yオフセットの遷移 = new List<Transition>() {
                    am.TrasitionLibrary.Linear( 0.05, finalValue: 0.0 ),	// 落下して
					am.TrasitionLibrary.Reversal( 0.03 ),	                // 短時間でベクトル反転（下から上へ）して
					am.TrasitionLibrary.Reversal( 0.05 ),	                // 上にはねて戻る
				};
                for( int i = 0; i < Yオフセットの遷移.Count; i++ )
                {
                    this.ストーリーボード.AddTransition( this.Yオフセット, Yオフセットの遷移[ i ] );
                    Yオフセットの遷移[ i ].Dispose();
                }
                this.ストーリーボード.Schedule( am.Timer.Time );
            }
            public void 跳ね開始( Animation am, double 遅延sec )
            {
                this.Dispose();

                this.ストーリーボード = new Storyboard( am.Manager );
                this.Yオフセット = new Variable( am.Manager, initialValue: 0.0 );
                this.不透明度 = new Variable( am.Manager, initialValue: 1.0 );

                var Yオフセットの遷移 = new List<Transition>() {
                    am.TrasitionLibrary.Constant( 遅延sec ),
                    am.TrasitionLibrary.Linear( 0.05, finalValue: -20.0 ),	// 上へ移動して
					am.TrasitionLibrary.Linear( 0.05, finalValue: 0.0 ),	// 下へ戻る
				};
                for( int i = 0; i < Yオフセットの遷移.Count; i++ )
                {
                    this.ストーリーボード.AddTransition( this.Yオフセット, Yオフセットの遷移[ i ] );
                    Yオフセットの遷移[ i ].Dispose();
                }
                this.ストーリーボード.Schedule( am.Timer.Time );
            }
        };

        private readonly 各桁のアニメ[] _各桁のアニメ;


        // 百ごとのアニメーション

        private class 百ごとのアニメ : IDisposable
        {
            public Storyboard ストーリーボード = null!;
            public Variable 拡大率 = null!;
            public Variable 振動幅 = null!;

            public 百ごとのアニメ()
            {
            }
            public void Dispose()
            {
                this.ストーリーボード?.Dispose();
                this.拡大率?.Dispose();
                this.振動幅?.Dispose();
            }

            public void 開始( Animation am )
            {
                this.Dispose();

                this.ストーリーボード = new Storyboard( am.Manager );
                this.拡大率 = new Variable( am.Manager, initialValue: 1.0 );
                this.振動幅 = new Variable( am.Manager, initialValue: 0.0 );

                var 拡大率の遷移 = new List<Transition>() {
                    am.TrasitionLibrary.Linear( 0.08, finalValue: 1.25 ),
                    am.TrasitionLibrary.Constant( 0.3 ),
                    am.TrasitionLibrary.Linear( 0.08, finalValue: 1.0 ),
                };
                for( int i = 0; i < 拡大率の遷移.Count; i++ )
                {
                    this.ストーリーボード.AddTransition( this.拡大率, 拡大率の遷移[ i ] );
                    拡大率の遷移[ i ].Dispose();
                }

                var 振動幅の遷移 = new List<Transition>() {
                    am.TrasitionLibrary.Linear( 0.08, finalValue: 6.0 ),
                    am.TrasitionLibrary.Constant( 0.3 ),
                    am.TrasitionLibrary.Linear( 0.08, finalValue: 0.0 ),
                };
                for( int i = 0; i < 振動幅の遷移.Count; i++ )
                {
                    this.ストーリーボード.AddTransition( this.振動幅, 振動幅の遷移[ i ] );
                    振動幅の遷移[ i ].Dispose();
                }

                this.ストーリーボード.Schedule( am.Timer.Time );
            }
        };

        private readonly 百ごとのアニメ _百ごとのアニメ;
    }
}
