using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Animation;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania
{
    class GO : アイキャッチ
    {

        // 生成と終了


        public GO()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this.現在のフェーズ = フェーズ.未定;

                this.文字画像 = new 画像[ 3 ] {
                    new 画像( @"$(System)images\アイキャッチ\G.png" ) { 加算合成 = true },
                    new 画像( @"$(System)images\アイキャッチ\O.png" ) { 加算合成 = true },
                    new 画像( @"$(System)images\アイキャッチ\!.png" ) { 加算合成 = true },
                };

                #region " Go! "
                //----------------
                this._文字アニメーション = new 文字[ 3 ];
                //----------------
                #endregion

                #region " ぐるぐる棒 "
                //----------------
                this._ぐるぐる棒アニメーション = new ぐるぐる棒[ 12 ];

                for( int i = 0; i < this._ぐるぐる棒アニメーション.Length; i++ )
                    this._ぐるぐる棒アニメーション[ i ] = new ぐるぐる棒();
                //----------------
                #endregion

                #region " フラッシュオーバー棒 "
                //----------------
                this._フラッシュオーバー棒アニメーション = new フラッシュオーバー棒[ 6 ];

                for( int i = 0; i < this._フラッシュオーバー棒アニメーション.Length; i++ )
                    this._フラッシュオーバー棒アニメーション[ i ] = new フラッシュオーバー棒();
                //----------------
                #endregion

                #region " フェードイン "
                //----------------
                this._フェードインアニメーション = new フェードイン();
                //----------------
                #endregion
            }
        }

        public override void Dispose()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                #region " Go! "
                //----------------
                if( null != this._文字アニメーション )
                {
                    foreach( var s in this._文字アニメーション )
                        s?.Dispose();   // 一度も実行されなければ null なので注意

                    this._文字アニメーション = null;
                }
                //----------------
                #endregion

                #region " ぐるぐる棒 "
                //----------------
                if( null != this._ぐるぐる棒アニメーション )
                {
                    foreach( var b in this._ぐるぐる棒アニメーション )
                        b.Dispose();

                    this._ぐるぐる棒アニメーション = null;
                }
                //----------------
                #endregion

                #region " フラッシュオーバー棒 "
                //----------------
                if( null != this._フラッシュオーバー棒アニメーション )
                {
                    foreach( var b in this._フラッシュオーバー棒アニメーション )
                        b.Dispose();

                    this._フラッシュオーバー棒アニメーション = null;
                }
                //----------------
                #endregion

                #region " フェードイン "
                //----------------
                this._フェードインアニメーション?.Dispose();
                this._フェードインアニメーション = null;
                //----------------
                #endregion

                foreach( var image in this.文字画像 )
                    image.Dispose();
                this.文字画像 = null;
            }

            base.Dispose();
        }



        // オープンとクローズ


        /// <summary>
        ///     アイキャッチのクローズアニメーションを開始する。
        /// </summary>
        public override void クローズする( float 速度倍率 = 1.0f )
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                double 秒( double v ) => ( v / 速度倍率 );

                var animation = DXResources.Instance.Animation;

                this.現在のフェーズ = フェーズ.クローズ;

                // Go!
                var basetime = animation.Timer.Time;
                var start = basetime;

                #region " 「G」のアニメーション構築 "
                //----------------
                {
                    var 文字 = this._文字アニメーション[ (int) 文字名.G ] = new 文字() {
                        画像 = this.文字画像[ (int) 文字名.G ],
                        中心位置X = new Variable( animation.Manager, 0.0 - 400.0 ),
                        中心位置Y = new Variable( animation.Manager, 1080.0 / 2.0 - 170.0 ),
                        拡大率 = new Variable( animation.Manager, 1.0 ),
                        ストーリーボード = new Storyboard( animation.Manager ),
                    };
                   
                    using( var 中心位置Xの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.23 ), finalValue: 1920.0 / 2.0 - 260.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                        文字.ストーリーボード.AddTransition( 文字.中心位置X, 中心位置Xの遷移 );

                    using( var 中心位置Xの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.07 ), finalValue: 1920.0 / 2.0 - 320.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                        文字.ストーリーボード.AddTransition( 文字.中心位置X, 中心位置Xの遷移 );

                    using( var 中心位置Xの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.07 ), finalValue: 1920.0 / 2.0 - 260.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                        文字.ストーリーボード.AddTransition( 文字.中心位置X, 中心位置Xの遷移 );

                    文字.ストーリーボード.Schedule( start );
                }
                //----------------
                #endregion
                #region " 「O」のアニメーション構築 "
                //----------------
                {
                    var 文字 = this._文字アニメーション[ (int) 文字名.O ] = new 文字() {
                        画像 = this.文字画像[ (int) 文字名.O ],
                        中心位置X = new Variable( animation.Manager, 1920.0 + 200.0 ),
                        中心位置Y = new Variable( animation.Manager, 1080.0 / 2.0 - 80.0 ),
                        拡大率 = new Variable( animation.Manager, 1.0 ),
                        ストーリーボード = new Storyboard( animation.Manager ),
                    };

                    using( var 中心位置Xの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.23 ), finalValue: 1920.0 / 2.0 - 20.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                        文字.ストーリーボード.AddTransition( 文字.中心位置X, 中心位置Xの遷移 );

                    using( var 中心位置Xの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.07 ), finalValue: 1920.0 / 2.0 + 20.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                        文字.ストーリーボード.AddTransition( 文字.中心位置X, 中心位置Xの遷移 );

                    using( var 中心位置Xの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.07 ), finalValue: 1920.0 / 2.0 - 20.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                        文字.ストーリーボード.AddTransition( 文字.中心位置X, 中心位置Xの遷移 );

                    文字.ストーリーボード.Schedule( start );
                }
                //----------------
                #endregion
                #region " 「!」のアニメーション構築 "
                //----------------
                {
                    var 文字 = this._文字アニメーション[ (int) 文字名.Exc ] = new 文字() {
                        画像 = this.文字画像[ (int) 文字名.Exc ],
                        中心位置X = new Variable( animation.Manager, 1920.0 / 2.0 + 140.0 ),
                        中心位置Y = new Variable( animation.Manager, 1080.0 / 2.0 + 100.0 ),
                        拡大率 = new Variable( animation.Manager, 0.1 ),
                        ストーリーボード = new Storyboard( animation.Manager ),
                    };

                    using( var 中心位置Yの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.14 ), finalValue: 1080.0 / 2.0 - 340.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    using( var 拡大率の遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.14 ), finalValue: 1.5, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        文字.ストーリーボード.AddTransition( 文字.中心位置Y, 中心位置Yの遷移 );
                        文字.ストーリーボード.AddTransition( 文字.拡大率, 拡大率の遷移 );
                    }

                    using( var 中心位置Yの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.1 ), finalValue: 1080.0 / 2.0 - 200.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    using( var 拡大率の遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.1 ), finalValue: 1.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        文字.ストーリーボード.AddTransition( 文字.中心位置Y, 中心位置Yの遷移 );
                        文字.ストーリーボード.AddTransition( 文字.拡大率, 拡大率の遷移 );
                    }

                    文字.ストーリーボード.Schedule( start + 秒( 0.16 ) );
                }
                //----------------
                #endregion

                // ぐるぐる棒
                start = basetime + 秒( 0.2 );

                #region " [0] 上側１番目の青 のアニメーション構築 "
                //----------------
                {
                    var bar = this._ぐるぐる棒アニメーション[ 0 ];
                    bar.Dispose();

                    bar.中心位置X = 1920.0 / 2.0;
                    bar.中心位置Y = 1080.0 / 2.0;
                    bar.太さ = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.棒の太さ = 50.0;
                    bar.回転角rad = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.辺の種類 = 辺の種類.上辺;
                    bar.ブラシ = new SolidColorBrush( DXResources.Instance.既定のD2D1DeviceContext, new Color4( 0.5f, 0.5f, 1f, 1f ) ); // 青
                    bar.ストーリーボード = new Storyboard( animation.Manager );

                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.1765 ), finalValue: 800.0, accelerationRatio: 0.0, decelerationRatio: 1.0 ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.088 ) ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.1765 ) ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.294 ), finalValue: Math.PI * 1.25, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.147 ), finalValue: 2200.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                    }

                    bar.ストーリーボード.Schedule( start );
                }
                //----------------
                #endregion
                #region " [1] 上側１番目の白 のアニメーション構築 "
                //----------------
                {
                    var bar = this._ぐるぐる棒アニメーション[ 1 ];
                    bar.Dispose();

                    bar.中心位置X = 1920.0 / 2.0;
                    bar.中心位置Y = 1080.0 / 2.0;
                    bar.太さ = new Variable( animation.Manager, initialValue: 200.0 );
                    bar.棒の太さ = 20.0;
                    bar.回転角rad = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.辺の種類 = 辺の種類.上辺;
                    bar.ブラシ = new SolidColorBrush( DXResources.Instance.既定のD2D1DeviceContext, new Color4( 1f, 1f, 1f, 1f ) ); // 白
                    bar.ストーリーボード = new Storyboard( animation.Manager );

                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.1765 ), finalValue: 1200.0, accelerationRatio: 0.0, decelerationRatio: 1.0 ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.8824 ) ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.1765 ) ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.294 ), finalValue: Math.PI * 1.25, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.147 ), finalValue: 2600.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                    }

                    bar.ストーリーボード.Schedule( start );
                }
                //----------------
                #endregion
                #region " [2] 上側２番目の青 のアニメーション構築 "
                //----------------
                {
                    var bar = this._ぐるぐる棒アニメーション[ 2 ];
                    bar.Dispose();

                    bar.中心位置X = 1920.0 / 2.0;
                    bar.中心位置Y = 1080.0 / 2.0;
                    bar.太さ = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.棒の太さ = 50.0;
                    bar.回転角rad = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.辺の種類 = 辺の種類.上辺;
                    bar.ブラシ = new SolidColorBrush( DXResources.Instance.既定のD2D1DeviceContext, new Color4( 0.5f, 0.5f, 1f, 1f ) ); // 青
                    bar.ストーリーボード = new Storyboard( animation.Manager );

                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.1765 ), finalValue: 800.0, accelerationRatio: 0.0, decelerationRatio: 1.0 ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.088 ) ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.1765 ) ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.294 ), finalValue: Math.PI * 1.25, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.147 ), finalValue: 2200.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                    }

                    bar.ストーリーボード.Schedule( start + 秒( 0.0294 ) );
                }
                //----------------
                #endregion
                #region " [3] 上側２番目の白 のアニメーション構築 "
                //----------------
                {
                    var bar = this._ぐるぐる棒アニメーション[ 3 ];
                    bar.Dispose();

                    bar.中心位置X = 1920.0 / 2.0;
                    bar.中心位置Y = 1080.0 / 2.0;
                    bar.太さ = new Variable( animation.Manager, initialValue: 200.0 );
                    bar.棒の太さ = 20.0;
                    bar.回転角rad = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.辺の種類 = 辺の種類.上辺;
                    bar.ブラシ = new SolidColorBrush( DXResources.Instance.既定のD2D1DeviceContext, new Color4( 1f, 1f, 1f, 1f ) ); // 白
                    bar.ストーリーボード = new Storyboard( animation.Manager );

                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.1765 ), finalValue: 1200.0, accelerationRatio: 0.0, decelerationRatio: 1.0 ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.088 ) ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.1765 ) ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.294 ), finalValue: Math.PI * 1.25, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.147 ), finalValue: 2600.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                    }

                    bar.ストーリーボード.Schedule( start + 秒( 0.0294 ) );
                }
                //----------------
                #endregion
                #region " [4] 上側３番目の青 のアニメーション構築 "
                //----------------
                {
                    var bar = this._ぐるぐる棒アニメーション[ 4 ];
                    bar.Dispose();

                    bar.中心位置X = 1920.0 / 2.0;
                    bar.中心位置Y = 1080.0 / 2.0;
                    bar.太さ = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.棒の太さ = 50.0;
                    bar.回転角rad = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.辺の種類 = 辺の種類.上辺;
                    bar.ブラシ = new SolidColorBrush( DXResources.Instance.既定のD2D1DeviceContext, new Color4( 0.1f, 0.1f, 0.5f, 0.5f ) ); // 青
                    bar.ストーリーボード = new Storyboard( animation.Manager );

                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.1765 ), finalValue: 800.0, accelerationRatio: 0.0, decelerationRatio: 1.0 ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.088 ) ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.1765 ) ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.294 ), finalValue: Math.PI * 1.25, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.147 ), finalValue: 2200.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                    }

                    bar.ストーリーボード.Schedule( start + 秒( 0.0471 ) );
                }
                //----------------
                #endregion
                #region " [5] 上側３番目の白 のアニメーション構築 "
                //----------------
                {
                    var bar = this._ぐるぐる棒アニメーション[ 5 ];
                    bar.Dispose();

                    bar.中心位置X = 1920.0 / 2.0;
                    bar.中心位置Y = 1080.0 / 2.0;
                    bar.太さ = new Variable( animation.Manager, initialValue: 200.0 );
                    bar.棒の太さ = 10.0;
                    bar.回転角rad = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.辺の種類 = 辺の種類.上辺;
                    bar.ブラシ = new SolidColorBrush( DXResources.Instance.既定のD2D1DeviceContext, new Color4( 1f, 1f, 1f, 0.5f ) ); // 白
                    bar.ストーリーボード = new Storyboard( animation.Manager );

                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.1765 ), finalValue: 1200.0, accelerationRatio: 0.0, decelerationRatio: 1.0 ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.088 ) ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.1765 ) ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.294 ), finalValue: Math.PI * 1.25, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.147 ), finalValue: 2600.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                    }

                    bar.ストーリーボード.Schedule( start + 秒( 0.0471 ) );
                }
                //----------------
                #endregion
                #region " [6] 下側１番目の青 のアニメーション構築 "
                //----------------
                {
                    var bar = this._ぐるぐる棒アニメーション[ 6 ];
                    bar.Dispose();

                    bar.中心位置X = 1920.0 / 2.0;
                    bar.中心位置Y = 1080.0 / 2.0;
                    bar.太さ = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.棒の太さ = 50.0;
                    bar.回転角rad = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.辺の種類 = 辺の種類.下辺;
                    bar.ブラシ = new SolidColorBrush( DXResources.Instance.既定のD2D1DeviceContext, new Color4( 0.5f, 0.5f, 1f, 1f ) ); // 青
                    bar.ストーリーボード = new Storyboard( animation.Manager );

                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.1765 ), finalValue: 800.0, accelerationRatio: 0.0, decelerationRatio: 1.0 ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.088 ) ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.1765 ) ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.294 ), finalValue: Math.PI * 1.25, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.147 ), finalValue: 2200.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                    }

                    bar.ストーリーボード.Schedule( start );
                }
                //----------------
                #endregion
                #region " [7] 下側１番目の白 のアニメーション構築 "
                //----------------
                {
                    var bar = this._ぐるぐる棒アニメーション[ 7 ];
                    bar.Dispose();

                    bar.中心位置X = 1920.0 / 2.0;
                    bar.中心位置Y = 1080.0 / 2.0;
                    bar.太さ = new Variable( animation.Manager, initialValue: 200.0 );
                    bar.棒の太さ = 20.0;
                    bar.回転角rad = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.辺の種類 = 辺の種類.下辺;
                    bar.ブラシ = new SolidColorBrush( DXResources.Instance.既定のD2D1DeviceContext, new Color4( 1f, 1f, 1f, 1f ) ); // 白
                    bar.ストーリーボード = new Storyboard( animation.Manager );

                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.1765 ), finalValue: 1200.0, accelerationRatio: 0.0, decelerationRatio: 1.0 ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.088 ) ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.1765 ) ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.294 ), finalValue: Math.PI * 1.25, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.147 ), finalValue: 2600.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                    }

                    bar.ストーリーボード.Schedule( start );
                }
                //----------------
                #endregion
                #region " [8] 下側２番目の青 のアニメーション構築 "
                //----------------
                {
                    var bar = this._ぐるぐる棒アニメーション[ 8 ];
                    bar.Dispose();

                    bar.中心位置X = 1920.0 / 2.0;
                    bar.中心位置Y = 1080.0 / 2.0;
                    bar.太さ = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.棒の太さ = 50.0;
                    bar.回転角rad = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.辺の種類 = 辺の種類.下辺;
                    bar.ブラシ = new SolidColorBrush( DXResources.Instance.既定のD2D1DeviceContext, new Color4( 0.5f, 0.5f, 1f, 1f ) ); // 青
                    bar.ストーリーボード = new Storyboard( animation.Manager );

                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.1765 ), finalValue: 800.0, accelerationRatio: 0.0, decelerationRatio: 1.0 ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.088 ) ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.1765 ) ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.294 ), finalValue: Math.PI * 1.25, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.147 ), finalValue: 2200.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                    }

                    bar.ストーリーボード.Schedule( start + 秒( 0.0294 ) );
                }
                //----------------
                #endregion
                #region " [9] 下側２番目の白 のアニメーション構築 "
                //----------------
                {
                    var bar = this._ぐるぐる棒アニメーション[ 9 ];
                    bar.Dispose();

                    bar.中心位置X = 1920.0 / 2.0;
                    bar.中心位置Y = 1080.0 / 2.0;
                    bar.太さ = new Variable( animation.Manager, initialValue: 200.0 );
                    bar.棒の太さ = 20.0;
                    bar.回転角rad = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.辺の種類 = 辺の種類.下辺;
                    bar.ブラシ = new SolidColorBrush( DXResources.Instance.既定のD2D1DeviceContext, new Color4( 1f, 1f, 1f, 1f ) ); // 白
                    bar.ストーリーボード = new Storyboard( animation.Manager );

                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.1765 ), finalValue: 1200.0, accelerationRatio: 0.0, decelerationRatio: 1.0 ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.088 ) ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.1765 ) ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.294 ), finalValue: Math.PI * 1.25, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.147 ), finalValue: 2600.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                    }

                    bar.ストーリーボード.Schedule( start + 秒( 0.0294 ) );
                }
                //----------------
                #endregion
                #region " [10] 下側３番目の青 のアニメーション構築 "
                //----------------
                {
                    var bar = this._ぐるぐる棒アニメーション[ 10 ];
                    bar.Dispose();

                    bar.中心位置X = 1920.0 / 2.0;
                    bar.中心位置Y = 1080.0 / 2.0;
                    bar.太さ = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.棒の太さ = 50.0;
                    bar.回転角rad = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.辺の種類 = 辺の種類.下辺;
                    bar.ブラシ = new SolidColorBrush( DXResources.Instance.既定のD2D1DeviceContext, new Color4( 0.1f, 0.1f, 0.5f, 0.5f ) ); // 青
                    bar.ストーリーボード = new Storyboard( animation.Manager );

                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.1765 ), finalValue: 800.0, accelerationRatio: 0.0, decelerationRatio: 1.0 ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.088 ) ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.1765 ) ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.294 ), finalValue: Math.PI * 1.25, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.147 ), finalValue: 2200.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                    }

                    bar.ストーリーボード.Schedule( start + 秒( 0.0471 ) );
                }
                //----------------
                #endregion
                #region " [11] 下側３番目の白 のアニメーション構築 "
                //----------------
                {
                    var bar = this._ぐるぐる棒アニメーション[ 11 ];
                    bar.Dispose();

                    bar.中心位置X = 1920.0 / 2.0;
                    bar.中心位置Y = 1080.0 / 2.0;
                    bar.太さ = new Variable( animation.Manager, initialValue: 200.0 );
                    bar.棒の太さ = 10.0;
                    bar.回転角rad = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.辺の種類 = 辺の種類.下辺;
                    bar.ブラシ = new SolidColorBrush( DXResources.Instance.既定のD2D1DeviceContext, new Color4( 1f, 1f, 1f, 0.5f ) ); // 白
                    bar.ストーリーボード = new Storyboard( animation.Manager );

                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.1765 ), finalValue: 1200.0, accelerationRatio: 0.0, decelerationRatio: 1.0 ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.088 ) ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 0.1765 ) ) )
                    using( var 回転の遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.294 ), finalValue: Math.PI * 1.25, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                        bar.ストーリーボード.AddTransition( bar.回転角rad, 回転の遷移 );
                    }
                    using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.147 ), finalValue: 2600.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    {
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );
                    }

                    bar.ストーリーボード.Schedule( start + 秒( 0.0471 ) );
                }
                //----------------
                #endregion

                // フラッシュオーバー棒
                start = basetime + 秒( 0.55 );

                #region " [0] 上側１番目の白 のアニメーション構築 "
                //----------------
                {
                    var bar = this._フラッシュオーバー棒アニメーション[ 0 ];
                    bar.Dispose();

                    bar.中心位置X = 1920.0 / 2.0;
                    bar.中心位置Y = 1080.0 / 2.0;
                    bar.太さ = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.棒の太さ = new Variable( animation.Manager, initialValue: 50.0 );
                    bar.回転角rad = Math.PI * 0.25;
                    bar.辺の種類 = 辺の種類.上辺;
                    bar.ブラシ = new SolidColorBrush( DXResources.Instance.既定のD2D1DeviceContext, new Color4( 1f, 1f, 1f, 1f ) ); // 白
                    bar.ストーリーボード = new Storyboard( animation.Manager );

                    using( var 太さの遷移 = animation.TrasitionLibrary.Linear( duration: 秒( 0.18 ), finalValue: 2200.0 ) )
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );

                    bar.ストーリーボード.Schedule( start );
                }
                //----------------
                #endregion
                #region " [1] 上側２番目の白 のアニメーション構築 "
                //----------------
                {
                    var bar = this._フラッシュオーバー棒アニメーション[ 1 ];
                    bar.Dispose();

                    bar.中心位置X = 1920.0 / 2.0;
                    bar.中心位置Y = 1080.0 / 2.0;
                    bar.太さ = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.棒の太さ = new Variable( animation.Manager, initialValue: 50.0 );
                    bar.回転角rad = Math.PI * 0.25;
                    bar.辺の種類 = 辺の種類.上辺;
                    bar.ブラシ = new SolidColorBrush( DXResources.Instance.既定のD2D1DeviceContext, new Color4( 1f, 1f, 1f, 1f ) ); // 白
                    bar.ストーリーボード = new Storyboard( animation.Manager );

                    using( var 太さの遷移 = animation.TrasitionLibrary.Linear( duration: 秒( 0.18 ), finalValue: 2200.0 ) )
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );

                    bar.ストーリーボード.Schedule( start + 秒( 0.02 ) );
                }
                //----------------
                #endregion
                #region " [2] 下側１番目の白 のアニメーション構築 "
                //----------------
                {
                    var bar = this._フラッシュオーバー棒アニメーション[ 2 ];
                    bar.Dispose();

                    bar.中心位置X = 1920.0 / 2.0;
                    bar.中心位置Y = 1080.0 / 2.0;
                    bar.太さ = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.棒の太さ = new Variable( animation.Manager, initialValue: 50.0 );
                    bar.回転角rad = Math.PI * 0.25;
                    bar.辺の種類 = 辺の種類.下辺;
                    bar.ブラシ = new SolidColorBrush( DXResources.Instance.既定のD2D1DeviceContext, new Color4( 1f, 1f, 1f, 1f ) ); // 白
                    bar.ストーリーボード = new Storyboard( animation.Manager );

                    using( var 太さの遷移 = animation.TrasitionLibrary.Linear( duration: 秒( 0.18 ), finalValue: 2200.0 ) )
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );

                    bar.ストーリーボード.Schedule( start );
                }
                //----------------
                #endregion
                #region " [3] 下側２番目の白 のアニメーション構築 "
                //----------------
                {
                    var bar = this._フラッシュオーバー棒アニメーション[ 3 ];
                    bar.Dispose();

                    bar.中心位置X = 1920.0 / 2.0;
                    bar.中心位置Y = 1080.0 / 2.0;
                    bar.太さ = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.棒の太さ = new Variable( animation.Manager, initialValue: 50.0 );
                    bar.回転角rad = Math.PI * 0.25;
                    bar.辺の種類 = 辺の種類.下辺;
                    bar.ブラシ = new SolidColorBrush( DXResources.Instance.既定のD2D1DeviceContext, new Color4( 1f, 1f, 1f, 1f ) ); // 白
                    bar.ストーリーボード = new Storyboard( animation.Manager );

                    using( var 太さの遷移 = animation.TrasitionLibrary.Linear( duration: 秒( 0.18 ), finalValue: 2200.0 ) )
                        bar.ストーリーボード.AddTransition( bar.太さ, 太さの遷移 );

                    bar.ストーリーボード.Schedule( start + 秒( 0.02 ) );
                }
                //----------------
                #endregion
                #region " [4] 真ん中の白 のアニメーション構築 "
                //----------------
                {
                    var bar = this._フラッシュオーバー棒アニメーション[ 4 ];
                    bar.Dispose();

                    bar.中心位置X = 1920.0 / 2.0;
                    bar.中心位置Y = 1080.0 / 2.0;
                    bar.太さ = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.棒の太さ = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.回転角rad = Math.PI * 0.25;
                    bar.辺の種類 = 辺の種類.上辺;
                    bar.ブラシ = new SolidColorBrush( DXResources.Instance.既定のD2D1DeviceContext, new Color4( 1f, 1f, 1f, 1f ) ); // 白
                    bar.ストーリーボード = new Storyboard( animation.Manager );

                    using( var 棒の太さの遷移 = animation.TrasitionLibrary.Linear( duration: 秒( 0.18 ), finalValue: 2200.0 ) )
                        bar.ストーリーボード.AddTransition( bar.棒の太さ, 棒の太さの遷移 );

                    bar.ストーリーボード.Schedule( start + 秒( 0.033 ) );
                }
                //----------------
                #endregion
                #region " [5] 真ん中の青 のアニメーション構築 "
                //----------------
                {
                    var bar = this._フラッシュオーバー棒アニメーション[ 5 ];
                    bar.Dispose();

                    bar.中心位置X = 1920.0 / 2.0;
                    bar.中心位置Y = 1080.0 / 2.0;
                    bar.太さ = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.棒の太さ = new Variable( animation.Manager, initialValue: 0.0 );
                    bar.回転角rad = Math.PI * 0.25;
                    bar.辺の種類 = 辺の種類.上辺;
                    bar.ブラシ = new SolidColorBrush( DXResources.Instance.既定のD2D1DeviceContext, new Color4( 0.5f, 0.5f, 1f, 1f ) ); // 青
                    bar.ストーリーボード = new Storyboard( animation.Manager );

                    using( var 棒の太さの遷移 = animation.TrasitionLibrary.Linear( duration: 秒( 0.18 ), finalValue: 2200.0 ) )
                        bar.ストーリーボード.AddTransition( bar.棒の太さ, 棒の太さの遷移 );

                    bar.ストーリーボード.Schedule( start + 秒( 0.1 ) );
                }
                //----------------
                #endregion

                // フェードイン → 使わない
                this._フェードインアニメーション?.Dispose();
                this._フェードインアニメーション = null;
            }
        }
        
        /// <summary>
        ///     アイキャッチのオープンアニメーションを開始する。
        /// </summary>
        public override void オープンする( float 速度倍率 = 1.0f )
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                double 秒( double v ) => ( v / 速度倍率 );

                var animation = DXResources.Instance.Animation;

                this.現在のフェーズ = フェーズ.オープン;
                var basetime = animation.Timer.Time;

                // Go! → 使わない
                foreach( var s in this._文字アニメーション )
                    s.Dispose();

                // ぐるぐる棒 → 使わない
                foreach( var b in this._ぐるぐる棒アニメーション )
                    b.Dispose();

                // フラッシュオーバー棒 → 使わない
                foreach( var b in this._フラッシュオーバー棒アニメーション )
                    b.Dispose();

                // フェードイン
                var start = basetime;
                this._フェードインアニメーション?.Dispose();
                this._フェードインアニメーション = new フェードイン() {
                    不透明度 = new Variable( animation.Manager, initialValue: 1.0 ),
                    ストーリーボード = new Storyboard( animation.Manager ),
                };
                using( var 不透明度の遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 0.8 ), finalValue: 0.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
                    this._フェードインアニメーション.ストーリーボード.AddTransition( this._フェードインアニメーション.不透明度, 不透明度の遷移 );

                this._フェードインアニメーション.ストーリーボード.Schedule( start );
            }
        }



        // 進行と描画


        /// <summary>
        ///     アイキャッチのアニメーションを進行し、アイキャッチ画像を描画する。
        /// </summary>
        protected override void 進行描画する( DeviceContext dc, StoryboardStatus 描画しないStatus )
        {
            bool すべて完了 = true;

            DXResources.Instance.D2DBatchDraw( dc, () => {

                var pretrans = dc.Transform;

                #region " ぐるぐる棒 "
                //----------------
                for( int i = 0; i < this._ぐるぐる棒アニメーション.Length; i++ )
                {
                    var context = this._ぐるぐる棒アニメーション[ i ];

                    if( null == context || null == context.ストーリーボード )
                        continue;

                    if( context.ストーリーボード.Status != StoryboardStatus.Ready )
                        すべて完了 = false;

                    if( context.ストーリーボード.Status == 描画しないStatus )
                        continue;

                    dc.Transform =
                        Matrix3x2.Rotation( (float) context.回転角rad.Value ) *
                        Matrix3x2.Translation( (float) context.中心位置X, (float) context.中心位置Y ) *
                        pretrans;

                    float contextの幅 = 2800.0f;
                    float contextの高さ = (float) context.太さ.Value;

                    var rc = ( context.辺の種類 == 辺の種類.上辺 ) ?
                        new RectangleF( -contextの幅 / 2f, -( contextの高さ + (float) context.棒の太さ ) / 2f, contextの幅, (float) context.棒の太さ ) :   // 上辺
                        new RectangleF( -contextの幅 / 2f, +( contextの高さ - (float) context.棒の太さ ) / 2f, contextの幅, (float) context.棒の太さ );    // 下辺

                    dc.FillRectangle( rc, context.ブラシ );
                }
                //----------------
                #endregion

                #region " フラッシュオーバー棒（[0～4]の5本）"
                //----------------
                for( int i = 0; i <= 4; i++ )
                {
                    var context = this._フラッシュオーバー棒アニメーション[ i ];

                    if( null == context || null == context.ストーリーボード )
                        continue;

                    if( context.ストーリーボード.Status != StoryboardStatus.Ready )
                        すべて完了 = false;

                    if( context.ストーリーボード.Status == 描画しないStatus )
                        continue;

                    dc.Transform =
                        Matrix3x2.Rotation( (float) context.回転角rad ) *
                        Matrix3x2.Translation( (float) context.中心位置X, (float) context.中心位置Y ) *
                        pretrans;

                    float contextの幅 = 2800.0f;
                    float contextの高さ = (float) context.太さ.Value;

                    var rc = ( context.辺の種類 == 辺の種類.上辺 ) ?
                        new RectangleF( -contextの幅 / 2f, -( contextの高さ + (float) context.棒の太さ.Value ) / 2f, contextの幅, (float) context.棒の太さ.Value ) :   // 上辺
                        new RectangleF( -contextの幅 / 2f, +( contextの高さ - (float) context.棒の太さ.Value ) / 2f, contextの幅, (float) context.棒の太さ.Value );    // 下辺

                    dc.FillRectangle( rc, context.ブラシ );
                }
                //----------------
                #endregion

                dc.Transform = pretrans;

            } );

            #region " Go! "
            //----------------
            foreach( var context in this._文字アニメーション )
            {
                if( null == context.ストーリーボード )
                    continue;

                if( context.ストーリーボード.Status != StoryboardStatus.Ready )
                    すべて完了 = false;

                if( context.ストーリーボード.Status == 描画しないStatus )
                    continue;

                var 変換行列2D =
                    Matrix3x2.Scaling( (float) context.拡大率.Value ) *
                    Matrix3x2.Translation( (float) context.中心位置X.Value, (float) context.中心位置Y.Value );

                context.画像.描画する( dc, 変換行列2D );
            }
            //----------------
            #endregion

            DXResources.Instance.D2DBatchDraw( dc, () => {

                var pretrans = dc.Transform;

                #region " フラッシュオーバー棒（[5]の1本）... Go! の上にかぶせる"
                //----------------
                {
                    var context = this._フラッシュオーバー棒アニメーション[ 5 ];

                    if( null != context.ストーリーボード && context.ストーリーボード.Status != 描画しないStatus )
                    {
                        if( context.ストーリーボード.Status != StoryboardStatus.Ready )
                            すべて完了 = false;

                        dc.Transform =
                            Matrix3x2.Rotation( (float) context.回転角rad ) *
                            Matrix3x2.Translation( (float) context.中心位置X, (float) context.中心位置Y ) *
                            pretrans;

                        float contextの幅 = 2800.0f;
                        float contextの高さ = (float) context.太さ.Value;

                        var rc = ( context.辺の種類 == 辺の種類.上辺 ) ?
                            new RectangleF( -contextの幅 / 2f, -( contextの高さ + (float) context.棒の太さ.Value ) / 2f, contextの幅, (float) context.棒の太さ.Value ) :   // 上辺
                            new RectangleF( -contextの幅 / 2f, +( contextの高さ - (float) context.棒の太さ.Value ) / 2f, contextの幅, (float) context.棒の太さ.Value );    // 下辺

                        dc.FillRectangle( rc, context.ブラシ );
                    }
                }
                //----------------
                #endregion

                #region " フェードイン "
                //----------------
                {
                    var context = this._フェードインアニメーション;

                    if( null != context && null != context.ストーリーボード && context.ストーリーボード.Status != 描画しないStatus )
                    {
                        if( context.ストーリーボード.Status != StoryboardStatus.Ready )
                            すべて完了 = false;

                        dc.Transform = pretrans;

                        using( var ブラシ = new SolidColorBrush( dc, new Color4( 0.5f, 0.5f, 1f, (float) context.不透明度.Value ) ) )
                            dc.FillRectangle( new RectangleF( 0f, 0f, 1920f, 1080f ), ブラシ );
                    }
                }
                //----------------
                #endregion

                dc.Transform = pretrans;

            } );

            if( すべて完了 )
            {
                if( this.現在のフェーズ == フェーズ.クローズ )
                {
                    this.現在のフェーズ = フェーズ.クローズ完了;
                }
                else if( this.現在のフェーズ == フェーズ.オープン )
                {
                    this.現在のフェーズ = フェーズ.オープン完了;
                }
            }
        }



        // protected, private

        private 画像[] 文字画像;

        /// <summary>
        ///     G, O, ! のアニメーション情報
        /// </summary>
        protected class 文字 : IDisposable
        {
            public 画像 画像 = null;
            public Variable 中心位置X = null;
            public Variable 中心位置Y = null;
            public Variable 拡大率 = null;
            public Storyboard ストーリーボード = null;

            public void Dispose()
            {
                this.画像 = null; // Disposeしない

                this.ストーリーボード?.Abandon();
                this.ストーリーボード?.Dispose();
                this.ストーリーボード = null;

                this.中心位置Y?.Dispose();
                this.中心位置Y = null;

                this.中心位置X?.Dispose();
                this.中心位置X = null;

                this.拡大率?.Dispose();
                this.拡大率 = null;
            }
        }
        private 文字[] _文字アニメーション = null;
        private enum 文字名 { G = 0, O = 1, Exc = 2 };

        /// <summary>
        ///     ぐるぐる棒のアニメーション情報
        /// </summary>
        private class ぐるぐる棒 : IDisposable
        {
            public double 中心位置X;
            public double 中心位置Y;
            public Variable 回転角rad = null;
            public Variable 太さ = null;
            public double 棒の太さ;
            public Storyboard ストーリーボード = null;
            public 辺の種類 辺の種類;
            public Brush ブラシ = null;

            public void Dispose()
            {
                this.ブラシ?.Dispose();
                this.ブラシ = null;

                this.ストーリーボード?.Abandon();
                this.ストーリーボード?.Dispose();
                this.ストーリーボード = null;

                this.太さ?.Dispose();
                this.太さ = null;

                this.回転角rad?.Dispose();
                this.回転角rad = null;
            }
        }
        private ぐるぐる棒[] _ぐるぐる棒アニメーション = null;
        private enum 辺の種類 { 上辺, 下辺 }

        /// <summary>
        ///     フラッシュオーバー棒のアニメーション情報
        /// </summary>
        private class フラッシュオーバー棒 : IDisposable
        {
            public double 中心位置X;
            public double 中心位置Y;
            public double 回転角rad;
            public Variable 太さ = null;
            public Variable 棒の太さ = null;
            public Storyboard ストーリーボード = null;
            public 辺の種類 辺の種類;
            public Brush ブラシ = null;

            public void Dispose()
            {
                this.ブラシ?.Dispose();
                this.ブラシ = null;

                this.ストーリーボード?.Abandon();
                this.ストーリーボード?.Dispose();
                this.ストーリーボード = null;

                this.棒の太さ?.Dispose();
                this.棒の太さ = null;

                this.太さ?.Dispose();
                this.太さ = null;
            }
        }
        private フラッシュオーバー棒[] _フラッシュオーバー棒アニメーション = null;

        /// <summary>
        ///     フェードインのアニメーション情報
        /// </summary>
        private class フェードイン : IDisposable
        {
            public Variable 不透明度 = null;
            public Storyboard ストーリーボード = null;

            public void Dispose()
            {
                this.ストーリーボード?.Abandon();
                this.ストーリーボード?.Dispose();
                this.ストーリーボード = null;

                this.不透明度?.Dispose();
                this.不透明度 = null;
            }
        }
        private フェードイン _フェードインアニメーション = null;
    }
}
