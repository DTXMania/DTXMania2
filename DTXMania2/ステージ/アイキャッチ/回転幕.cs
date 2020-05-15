using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.Animation;
using SharpDX.Direct2D1;
using FDK;

/* 回転幕の仕様（推定）
 * 
 *	※ 画面A ... 切り替え元画面
 *	　 画面B ... アイキャッチ遷移画面1（回転中）
 *	　 画面C ... アイキャッチ画面
 *	　 画面D ... アイキャッチ遷移画面2（逆回転中）
 *	　 画面E ... 切り替え先画面
 *	　 
 * ＜クローズ＞
 * 1. 画面A切り替え元画面が表示されている。
 * 2. 画面Aの上に、黒帯が上下から１本ずつ現れる。
 * 3. 黒帯がそれぞれ画面中央にY方向移動する。
 *		→ このとき、上下の黒帯間には画面Aが、黒帯の外側には画面Bが描画される。
 * 4. 黒帯が、画面中央付近で回転を始める。
 *		→ 回転開始直前に、画面Aの表示は終わり、すべて画面Bに置き換わる。
 * 5. 黒帯が、270°回転したところで左右にX方向移動する。
 *		→ このとき、左右の黒帯間には画面Cが、黒帯の外側には画面Bが描画される。
 * 6. 黒帯が停止。
 *		→ 停止直後、画面Bの表示は終わり、すべて画面Cに置き換わる。
 *		
 *	＜オープン＞
 * 1. 画面Cの上に、黒帯が２つ左右に表示されている。
 * 2. 黒帯がそれぞれ画面中央にX方向移動する。
 *		→ このとき、左右の黒帯間には画面Cが、黒帯の外側には画面Dが描画される。
 * 3. 黒帯が、画面中央付近で逆回転を始める。
 *		→ 回転開始直前に、画面Cの表示は終わり、すべて画面Dに置き換わる。
 * 4. 黒帯が、-270°回転したところで上下にY方向移動する。
 *		→ このとき、上下の黒帯間には画面Eが、黒帯の外側には画面Dが描画される。
 * 5. 黒帯が画面外へ消失。
 *		→ 消失後、画面Dの表示は終わり、すべて画面Eに置き換わる。
 */

namespace DTXMania2
{
    class 回転幕 : アイキャッチ
    {

        // 生成と終了


        public 回転幕()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._ロゴ = new 画像D2D( @"$(Images)\TitleLogo.png" );
            this._画面BC_アイキャッチ遷移画面1_回転中 = new 舞台画像();
            this._画面D_アイキャッチ遷移画面2_逆回転中 = new 舞台画像();

            this._斜めジオメトリマスク = new PathGeometry( Global.D2D1Factory1 );
            using( var sink = this._斜めジオメトリマスク.Open() )
            {
                // 長方形。これを、縮小＆45°回転してマスクさせる。
                const float w = 1920f;
                const float h = 1080f * 2.0f;   // 斜めになるのでこのくらいいる。
                sink.SetFillMode( FillMode.Winding );
                sink.BeginFigure( new Vector2( -w / 2f, -h / 2f ), FigureBegin.Filled );    // (0,0) を長方形の中心とする。（スケーリング＆回転させるのに都合がいい）
                sink.AddLine( new Vector2( -w / 2f, +h / 2f ) );
                sink.AddLine( new Vector2( +w / 2f, +h / 2f ) );
                sink.AddLine( new Vector2( +w / 2f, -h / 2f ) );
                sink.EndFigure( FigureEnd.Closed );
                sink.Close();
            }

            this._斜めレイヤーパラメータ = new LayerParameters1 {
                ContentBounds = RectangleF.Infinite,
                GeometricMask = this._斜めジオメトリマスク,
                MaskAntialiasMode = AntialiasMode.PerPrimitive,
                MaskTransform = Matrix3x2.Identity,
                Opacity = 1.0f,
                OpacityBrush = null,
                LayerOptions = LayerOptions1.None,
            };

            this.現在のフェーズ = フェーズ.未定;
        }

        public override void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._斜めレイヤーパラメータ.GeometricMask = null; // 参照してるので先に手放す
            this._斜めジオメトリマスク.Dispose();

            if( null != this._黒幕アニメーション )
            {
                foreach( var b in this._黒幕アニメーション )
                    b.Dispose();
            }

            base.Dispose();
        }



        // オープンとクローズ


        /// <summary>
        ///     アイキャッチのクローズアニメーションを開始する。
        /// </summary>
        public override void クローズする( float 速度倍率 = 1.0f )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            double 秒( double v ) => ( v / 速度倍率 );

            var animation = Global.Animation;

            this.現在のフェーズ = フェーズ.クローズ;

            if( null != this._黒幕アニメーション )
            {
                foreach( var b in this._黒幕アニメーション )
                    b.Dispose();
            }

            this._黒幕アニメーション = new 黒幕[ 2 ] {
				    // 上＆左
				    new 黒幕() {
                        中心位置X = new Variable( animation.Manager, initialValue: 1920.0/2.0 ),	// クローズ初期位置、以下同
					    中心位置Y = new Variable( animation.Manager, initialValue: 0.0-500.0 ),
                        回転角rad = new Variable( animation.Manager, initialValue: 0.0 ),
                        太さ = new Variable( animation.Manager, initialValue: 1000.0 ),
                        不透明度 = new Variable( animation.Manager, initialValue: 1.0 ),
                        ストーリーボード = new Storyboard( animation.Manager ),
                    },
				    // 下＆右
				    new 黒幕() {
                        中心位置X = new Variable( animation.Manager, initialValue: 1920.0/2.0 ),
                        中心位置Y = new Variable( animation.Manager, initialValue: 1080.0+500.0 ),
                        回転角rad = new Variable( animation.Manager, initialValue: 0.0 ),
                        太さ = new Variable( animation.Manager, initialValue: 1000.0 ),
                        不透明度 = new Variable( animation.Manager, initialValue: 1.0 ),
                        ストーリーボード = new Storyboard( animation.Manager ),
                    },
                };

            this._クローズ割合?.Dispose();
            this._クローズ割合 = new Variable( animation.Manager, initialValue: 0.0 );     // 0.0 からスタート

            #region " ストーリーボードの構築(1) 上→左の黒幕, クローズ割合(便乗) "
            //----------------
            var 幕 = this._黒幕アニメーション[ 0 ];

            // シーン1 細くなりつつ画面中央へ移動。
            using( var 中心位置Xの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( _シーン1期間 ) ) )
            using( var 中心位置Yの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン1期間 ), finalValue: 1080.0 / 2.0, accelerationRatio: 0.1, decelerationRatio: 0.9 ) )
            using( var 回転radの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( _シーン1期間 - 0.1 ) ) )
            using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン1期間 ), finalValue: 100.0, accelerationRatio: 0.1, decelerationRatio: 0.9 ) )
            using( var 不透明度の遷移1 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン1期間 * 0.75 ), finalValue: 0.9, accelerationRatio: 0.1, decelerationRatio: 0.9 ) )
            using( var 不透明度の遷移2 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン1期間 * 0.25 ), finalValue: 0.5, accelerationRatio: 0.1, decelerationRatio: 0.9 ) )
            {
                幕.ストーリーボード.AddTransition( 幕.中心位置X, 中心位置Xの遷移 );
                幕.ストーリーボード.AddTransition( 幕.中心位置Y, 中心位置Yの遷移 );
                幕.ストーリーボード.AddTransition( 幕.回転角rad, 回転radの遷移 );
                幕.ストーリーボード.AddTransition( 幕.太さ, 太さの遷移 );
                幕.ストーリーボード.AddTransition( 幕.不透明度, 不透明度の遷移1 );
                幕.ストーリーボード.AddTransition( 幕.不透明度, 不透明度の遷移2 );

                // 便乗
                using( var クローズ割合の遷移0to1 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン1期間 - 0.07/*他より短め*/), finalValue: 1.0, accelerationRatio: 0.9, decelerationRatio: 0.1 ) )
                {
                    幕.ストーリーボード.AddTransition( this._クローズ割合, クローズ割合の遷移0to1 );
                }
            }

            // シーン2 270°回転。
            using( var 中心位置Xの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( _シーン2期間 - 0.18 ) ) )
            using( var 中心位置Yの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( _シーン2期間 - 0.18 ) ) )
            using( var 回転radの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン2期間 ), finalValue: Math.PI * 1.75, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
            using( var 太さの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( _シーン2期間 - 0.18 ) ) )
            using( var 不透明度の遷移 = animation.TrasitionLibrary.Constant( duration: 秒( _シーン2期間 - 0.18 ) ) )
            {
                幕.ストーリーボード.AddTransition( 幕.中心位置X, 中心位置Xの遷移 );
                幕.ストーリーボード.AddTransition( 幕.中心位置Y, 中心位置Yの遷移 );
                幕.ストーリーボード.AddTransition( 幕.回転角rad, 回転radの遷移 );
                幕.ストーリーボード.AddTransition( 幕.太さ, 太さの遷移 );
                幕.ストーリーボード.AddTransition( 幕.不透明度, 不透明度の遷移 );

                // 便乗
                using( var クローズ割合の遷移1to2 = animation.TrasitionLibrary.Linear( duration: 秒( _シーン2期間 - 0.18 + 0.07/*他より長め*/), finalValue: 2.0 ) )
                {
                    幕.ストーリーボード.AddTransition( this._クローズ割合, クローズ割合の遷移1to2 );
                }
            }

            // シーン3 太くなりつつ画面左へ移動。
            using( var 中心位置Xの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン3期間 ), finalValue: 0.0 - 200.0, accelerationRatio: 0.1, decelerationRatio: 0.9 ) )
            using( var 中心位置Yの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( _シーン3期間 ) ) )
            using( var 回転radの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( _シーン3期間 ) ) )
            using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン3期間 + 0.05 ), finalValue: 800.0, accelerationRatio: 0.1, decelerationRatio: 0.9 ) )
            using( var 不透明度の遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン3期間 * 0.25 ), finalValue: 1.0, accelerationRatio: 0.1, decelerationRatio: 0.9 ) )
            {
                幕.ストーリーボード.AddTransition( 幕.中心位置X, 中心位置Xの遷移 );
                幕.ストーリーボード.AddTransition( 幕.中心位置Y, 中心位置Yの遷移 );
                幕.ストーリーボード.AddTransition( 幕.回転角rad, 回転radの遷移 );
                幕.ストーリーボード.AddTransition( 幕.太さ, 太さの遷移 );
                幕.ストーリーボード.AddTransition( 幕.不透明度, 不透明度の遷移 );

                // 便乗
                using( var クローズ割合の遷移2to3 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン3期間 ), finalValue: 3.0, accelerationRatio: 0.1, decelerationRatio: 0.9 ) )
                {
                    幕.ストーリーボード.AddTransition( this._クローズ割合, クローズ割合の遷移2to3 );
                }
            }
            //----------------
            #endregion

            #region " ストーリーボードの構築(2) 下→右の黒幕 "
            //----------------
            幕 = this._黒幕アニメーション[ 1 ];

            double ずれ = 0.03;

            // シーン1 細くなりつつ画面中央へ移動。
            double 期間 = _シーン1期間 - ずれ;
            using( var 中心位置Xの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 期間 ) ) )
            using( var 中心位置Yの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 期間 ), finalValue: 1080.0 / 2.0, accelerationRatio: 0.1, decelerationRatio: 0.9 ) )
            using( var 回転radの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 期間 - 0.1 ) ) )
            using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 期間 ), finalValue: 100.0, accelerationRatio: 0.1, decelerationRatio: 0.9 ) )
            using( var 不透明度の遷移1 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 期間 * 0.75 ), finalValue: 0.9, accelerationRatio: 0.1, decelerationRatio: 0.9 ) )
            using( var 不透明度の遷移2 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 期間 * 0.25 ), finalValue: 0.5, accelerationRatio: 0.1, decelerationRatio: 0.9 ) )
            {
                幕.ストーリーボード.AddTransition( 幕.中心位置X, 中心位置Xの遷移 );
                幕.ストーリーボード.AddTransition( 幕.中心位置Y, 中心位置Yの遷移 );
                幕.ストーリーボード.AddTransition( 幕.回転角rad, 回転radの遷移 );
                幕.ストーリーボード.AddTransition( 幕.太さ, 太さの遷移 );
                幕.ストーリーボード.AddTransition( 幕.不透明度, 不透明度の遷移1 );
                幕.ストーリーボード.AddTransition( 幕.不透明度, 不透明度の遷移2 );
            }

            // シーン2 270°回転。
            期間 = _シーン2期間 + ずれ;
            using( var 中心位置Xの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 期間 - 0.18 ) ) )
            using( var 中心位置Yの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 期間 - 0.18 ) ) )
            using( var 回転radの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 期間 ), finalValue: Math.PI * 1.75, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
            using( var 太さの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 期間 - 0.18 ) ) )
            using( var 不透明度の遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 期間 - 0.18 ) ) )
            {
                幕.ストーリーボード.AddTransition( 幕.中心位置X, 中心位置Xの遷移 );
                幕.ストーリーボード.AddTransition( 幕.中心位置Y, 中心位置Yの遷移 );
                幕.ストーリーボード.AddTransition( 幕.回転角rad, 回転radの遷移 );
                幕.ストーリーボード.AddTransition( 幕.太さ, 太さの遷移 );
                幕.ストーリーボード.AddTransition( 幕.不透明度, 不透明度の遷移 );
            }

            // シーン3 太くなりつつ画面右へ移動。
            using( var 中心位置Xの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン3期間 ), finalValue: 1920.0 + 200.0, accelerationRatio: 0.1, decelerationRatio: 0.9 ) )
            using( var 中心位置Yの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( _シーン3期間 ) ) )
            using( var 回転radの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( _シーン3期間 ) ) )
            using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン3期間 + 0.05 ), finalValue: 800.0, accelerationRatio: 0.1, decelerationRatio: 0.9 ) )
            using( var 不透明度の遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン3期間 * 0.25 ), finalValue: 1.0, accelerationRatio: 0.1, decelerationRatio: 0.9 ) )
            {
                幕.ストーリーボード.AddTransition( 幕.中心位置X, 中心位置Xの遷移 );
                幕.ストーリーボード.AddTransition( 幕.中心位置Y, 中心位置Yの遷移 );
                幕.ストーリーボード.AddTransition( 幕.回転角rad, 回転radの遷移 );
                幕.ストーリーボード.AddTransition( 幕.太さ, 太さの遷移 );
                幕.ストーリーボード.AddTransition( 幕.不透明度, 不透明度の遷移 );
            }
            //----------------
            #endregion

            // 今すぐ開始。
            var start = animation.Timer.Time;
            foreach( var bs in this._黒幕アニメーション )
                bs.ストーリーボード.Schedule( start );

            this._初めての進行描画 = true;
        }

        /// <summary>
        ///     アイキャッチのオープンアニメーションを開始する。
        /// </summary>
        public override void オープンする( float 速度倍率 = 1.0f )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            double 秒( double v ) => ( v / 速度倍率 );

            var animation = Global.Animation;

            this.現在のフェーズ = フェーズ.オープン;

            if( null != this._黒幕アニメーション )
            {
                foreach( var b in this._黒幕アニメーション )
                    b.Dispose();
            }

            this._黒幕アニメーション = new 黒幕[ 2 ] {
				    // 上＆左
				    new 黒幕() {
                        中心位置X = new Variable( animation.Manager, initialValue: 0.0 - 200.0 ),	// オープン初期位置、以下同
					    中心位置Y = new Variable( animation.Manager, initialValue: 1080.0 / 2.0 ),
                        回転角rad = new Variable( animation.Manager, initialValue: Math.PI * 1.75 ),
                        太さ = new Variable( animation.Manager, initialValue: 800.0 ),
                        不透明度 = new Variable( animation.Manager, initialValue: 1.0 ),
                        ストーリーボード = new Storyboard( animation.Manager ),
                    },
				    // 下＆右
				    new 黒幕() {
                        中心位置X = new Variable( animation.Manager, initialValue: 1920.0 + 200.0 ),
                        中心位置Y = new Variable( animation.Manager, initialValue: 1080.0 / 2.0 ),
                        回転角rad = new Variable( animation.Manager, initialValue: Math.PI * 1.75 ),
                        太さ = new Variable( animation.Manager, initialValue: 800.0 ),
                        不透明度 = new Variable( animation.Manager, initialValue: 1.0 ),
                        ストーリーボード = new Storyboard( animation.Manager ),
                    },
                };

            this._クローズ割合?.Dispose();
            this._クローズ割合 = new Variable( animation.Manager, initialValue: 3.0 );     // 3.0 からスタート

            #region " ストーリーボードの構築(1) 上→左の黒幕, クローズ割合(便乗) "
            //----------------
            var 幕 = this._黒幕アニメーション[ 0 ];

            // シーン3 細くなりつつ画面中央へ移動。
            using( var 中心位置Xの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン3期間 ), finalValue: 1920.0 / 2.0, accelerationRatio: 0.9, decelerationRatio: 0.1 ) )
            using( var 中心位置Yの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( _シーン3期間 ) ) )
            using( var 回転radの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( _シーン3期間 - 0.08 ) ) )
            using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン3期間 ), finalValue: 100.0, accelerationRatio: 0.9, decelerationRatio: 0.1 ) )
            using( var 不透明度の遷移1 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン3期間 * 0.75 ), finalValue: 0.9, accelerationRatio: 0.9, decelerationRatio: 0.1 ) )
            using( var 不透明度の遷移2 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン3期間 * 0.25 ), finalValue: 0.5, accelerationRatio: 0.9, decelerationRatio: 0.1 ) )
            {
                幕.ストーリーボード.AddTransition( 幕.中心位置X, 中心位置Xの遷移 );
                幕.ストーリーボード.AddTransition( 幕.中心位置Y, 中心位置Yの遷移 );
                幕.ストーリーボード.AddTransition( 幕.回転角rad, 回転radの遷移 );
                幕.ストーリーボード.AddTransition( 幕.太さ, 太さの遷移 );
                幕.ストーリーボード.AddTransition( 幕.不透明度, 不透明度の遷移1 );
                幕.ストーリーボード.AddTransition( 幕.不透明度, 不透明度の遷移2 );

                // 便乗
                using( var クローズ割合の遷移3to2 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン3期間 ), finalValue: 2.0, accelerationRatio: 0.9, decelerationRatio: 0.1 ) )
                {
                    幕.ストーリーボード.AddTransition( this._クローズ割合, クローズ割合の遷移3to2 );
                }
            }

            // シーン2 -270°回転。
            using( var 中心位置Xの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( _シーン2期間 ) ) )
            using( var 中心位置Yの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( _シーン2期間 - 0.18 ) ) )
            using( var 回転radの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン2期間 ), finalValue: 0.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
            using( var 太さの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( _シーン2期間 - 0.18 ) ) )
            using( var 不透明度の遷移 = animation.TrasitionLibrary.Constant( duration: 秒( _シーン2期間 - 0.18 ) ) )
            {
                幕.ストーリーボード.AddTransition( 幕.中心位置X, 中心位置Xの遷移 );
                幕.ストーリーボード.AddTransition( 幕.中心位置Y, 中心位置Yの遷移 );
                幕.ストーリーボード.AddTransition( 幕.回転角rad, 回転radの遷移 );
                幕.ストーリーボード.AddTransition( 幕.太さ, 太さの遷移 );
                幕.ストーリーボード.AddTransition( 幕.不透明度, 不透明度の遷移 );

                // 便乗
                using( var クローズ割合の遷移2to1 = animation.TrasitionLibrary.Linear( duration: 秒( _シーン2期間 - 0.18 + 0.07/*他より長め*/), finalValue: 1.0 ) )
                {
                    幕.ストーリーボード.AddTransition( this._クローズ割合, クローズ割合の遷移2to1 );
                }
            }

            // シーン1 太くなりつつ画面上方へ移動。
            using( var 中心位置Xの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( _シーン1期間 ) ) )
            using( var 中心位置Yの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン1期間 ), finalValue: 0.0 - 500.0, accelerationRatio: 0.9, decelerationRatio: 0.1 ) )
            using( var 回転radの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( _シーン1期間 ) ) )
            using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン1期間 ), finalValue: 1000.0, accelerationRatio: 0.9, decelerationRatio: 0.1 ) )
            using( var 不透明度の遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン1期間 * 0.25 ), finalValue: 1.0, accelerationRatio: 0.9, decelerationRatio: 0.1 ) )
            using( var ロゴの不透明度の遷移 = animation.TrasitionLibrary.Discrete( delay: 秒( _シーン3期間 * ( 1.0 - 0.24 ) ), finalValue: 0.0, hold: ( _シーン3期間 ) / 速度倍率 ) )
            {
                幕.ストーリーボード.AddTransition( 幕.中心位置X, 中心位置Xの遷移 );
                幕.ストーリーボード.AddTransition( 幕.中心位置Y, 中心位置Yの遷移 );
                幕.ストーリーボード.AddTransition( 幕.回転角rad, 回転radの遷移 );
                幕.ストーリーボード.AddTransition( 幕.太さ, 太さの遷移 );
                幕.ストーリーボード.AddTransition( 幕.不透明度, 不透明度の遷移 );

                // 便乗
                using( var クローズ割合の遷移1to0 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン1期間 - 0.07/*他より短め*/), finalValue: 0.0, accelerationRatio: 0.9, decelerationRatio: 0.1 ) )
                {
                    幕.ストーリーボード.AddTransition( this._クローズ割合, クローズ割合の遷移1to0 );
                }
            }
            //----------------
            #endregion

            #region " ストーリーボードの構築(2) 下＆右の黒幕 "
            //----------------
            幕 = this._黒幕アニメーション[ 1 ];

            double ずれ = 0.03;

            // シーン3 細くなりつつ画面中央へ移動。
            using( var 中心位置Xの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン3期間 ), finalValue: 1920.0 / 2.0, accelerationRatio: 0.9, decelerationRatio: 0.1 ) )
            using( var 中心位置Yの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( _シーン3期間 ) ) )
            using( var 回転radの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( _シーン3期間 - 0.08 ) ) )
            using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン3期間 ), finalValue: 100.0, accelerationRatio: 0.9, decelerationRatio: 0.1 ) )
            using( var 不透明度の遷移1 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン3期間 * 0.75 ), finalValue: 0.9, accelerationRatio: 0.9, decelerationRatio: 0.1 ) )
            using( var 不透明度の遷移2 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( _シーン3期間 * 0.25 ), finalValue: 0.5, accelerationRatio: 0.9, decelerationRatio: 0.1 ) )
            {
                幕.ストーリーボード.AddTransition( 幕.中心位置X, 中心位置Xの遷移 );
                幕.ストーリーボード.AddTransition( 幕.中心位置Y, 中心位置Yの遷移 );
                幕.ストーリーボード.AddTransition( 幕.回転角rad, 回転radの遷移 );
                幕.ストーリーボード.AddTransition( 幕.太さ, 太さの遷移 );
                幕.ストーリーボード.AddTransition( 幕.不透明度, 不透明度の遷移1 );
                幕.ストーリーボード.AddTransition( 幕.不透明度, 不透明度の遷移2 );
            }
            // シーン2 -270°回転。
            double 期間 = _シーン2期間 + ずれ;
            using( var 中心位置Xの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 期間 ) ) )
            using( var 中心位置Yの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 期間 - 0.18 ) ) )
            using( var 回転radの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 期間 ), finalValue: 0.0, accelerationRatio: 0.5, decelerationRatio: 0.5 ) )
            using( var 太さの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 期間 - 0.18 ) ) )
            using( var 不透明度の遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 期間 - 0.18 ) ) )
            {
                幕.ストーリーボード.AddTransition( 幕.中心位置X, 中心位置Xの遷移 );
                幕.ストーリーボード.AddTransition( 幕.中心位置Y, 中心位置Yの遷移 );
                幕.ストーリーボード.AddTransition( 幕.回転角rad, 回転radの遷移 );
                幕.ストーリーボード.AddTransition( 幕.太さ, 太さの遷移 );
                幕.ストーリーボード.AddTransition( 幕.不透明度, 不透明度の遷移 );
            }

            // シーン1 太くなりつつ画面下方へ移動。
            期間 = _シーン1期間 - ずれ;
            using( var 中心位置Xの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 期間 ) ) )
            using( var 中心位置Yの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 期間 ), finalValue: 1080.0 + 500.0, accelerationRatio: 0.9, decelerationRatio: 0.1 ) )
            using( var 回転radの遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 期間 ) ) )
            using( var 太さの遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 期間 ), finalValue: 1000.0, accelerationRatio: 0.9, decelerationRatio: 0.1 ) )
            using( var 不透明度の遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 秒( 期間 * 0.25 ), finalValue: 1.0, accelerationRatio: 0.9, decelerationRatio: 0.1 ) )
            {
                幕.ストーリーボード.AddTransition( 幕.中心位置X, 中心位置Xの遷移 );
                幕.ストーリーボード.AddTransition( 幕.中心位置Y, 中心位置Yの遷移 );
                幕.ストーリーボード.AddTransition( 幕.回転角rad, 回転radの遷移 );
                幕.ストーリーボード.AddTransition( 幕.太さ, 太さの遷移 );
                幕.ストーリーボード.AddTransition( 幕.不透明度, 不透明度の遷移 );
            }
            //----------------
            #endregion

            // 今すぐ開始。
            var start = animation.Timer.Time;
            foreach( var bs in this._黒幕アニメーション )
                bs.ストーリーボード.Schedule( start );

            this._初めての進行描画 = true;
        }



        // 進行と描画


        /// <summary>
        ///     アイキャッチのアニメーションを進行し、アイキャッチ画像を描画する。
        /// </summary>
        protected override void 進行描画する( DeviceContext dc, StoryboardStatus 描画しないStatus )
        {
            bool すべて完了 = true;

            #region " 背景の画像 "
            //----------------
            switch( this.現在のフェーズ )
            {
                case フェーズ.クローズ:
                    {
                        if( this._初めての進行描画 )
                        {
                            this._画面BC_アイキャッチ遷移画面1_回転中.ぼかしと縮小を解除する( 0.0 );  // 全部解除してから
                            this._画面BC_アイキャッチ遷移画面1_回転中.ぼかしと縮小を適用する();       // ゆっくり適用開始。

                            this._初めての進行描画 = false;
                        }

                        switch( this._クローズ割合.Value )    // 0 → 3.0
                        {
                            // 画面A（切り替え元画面）
                            // 画面B（アイキャッチ遷移画面1（回転中））
                            // 画面C（アイキャッチ画面）
                            // ※ このメソッドの呼び出し前に、画面Aが全面描画済みであるものと想定する。

                            case double 割合 when( 1.0 > 割合 ):
                                #region " シーン1. 画面Aを下絵に、上下端から画面Bの描画領域が増えていく。（上下の黒帯の移動に伴って）"
                                //----------------
                                Size2F 画面Bサイズ = this._画面BC_アイキャッチ遷移画面1_回転中.サイズ;
                                float 画面B表示縦幅 = (float) ( 画面Bサイズ.Height * 割合 / 2.0 );    // 0 → height/2

                                // 上から
                                this._画面BC_アイキャッチ遷移画面1_回転中.進行描画する( dc, false, new Vector4( 0f, 0f, 画面Bサイズ.Width, 画面B表示縦幅 ) );

                                // 下から
                                this._画面BC_アイキャッチ遷移画面1_回転中.進行描画する( dc, false, new Vector4( 0f, 画面Bサイズ.Height - 画面B表示縦幅, 画面Bサイズ.Width, 画面Bサイズ.Height ) );
                                //----------------
                                #endregion
                                break;

                            case double 割合 when( 2.0 > 割合 ):
                                #region " シーン2. 画面Bを全表示。（黒帯は回転中）"
                                //----------------
                                this._画面BC_アイキャッチ遷移画面1_回転中.進行描画する( dc );
                                //----------------
                                #endregion
                                break;

                            case double 割合: // default
                                #region " シーン3. 画面Bを下絵に、中央から左右に向かって（黒帯の移動に従って）、画面Cの描画領域が広くなっていく。"
                                //----------------
                                this._画面BC_アイキャッチ遷移画面1_回転中.進行描画する( dc );    // 下絵の画面B、全表示。

                                // 以下、画面Cを上に重ねて描画。

                                割合 = 割合 - 2.0;  // 0 → 1.0

                                this._斜めレイヤーパラメータ.MaskTransform =
                                    Matrix3x2.Scaling( (float) ( 割合 * 0.5 ), 1.0f ) *    // x:0 → 0.5
                                    ( ( 割合 < 0.5 ) ?
                                        Matrix3x2.Rotation( (float) ( Math.PI / ( 5.85 - 1.85 * ( 割合 * 2 ) ) ) ) :
                                        Matrix3x2.Rotation( (float) ( Math.PI / 4.0 ) ) // 45°
                                    ) *
                                    Matrix3x2.Translation( Global.設計画面サイズ.Width / 2.0f, Global.設計画面サイズ.Height / 2.0f ); // 画面中央固定。

                                this._画面BC_アイキャッチ遷移画面1_回転中.進行描画する( dc, layerParameters1: this._斜めレイヤーパラメータ );
                                this._ロゴを描画する( dc );
                                //----------------
                                #endregion
                                break;
                        }
                    }
                    break;

                case フェーズ.クローズ完了:
                    {
                        // 画面C（アイキャッチ画面（背景＋ロゴ））
                        this._画面BC_アイキャッチ遷移画面1_回転中.進行描画する( dc );
                        this._ロゴを描画する( dc );
                    }
                    break;

                case フェーズ.オープン:
                    {
                        if( this._初めての進行描画 )
                        {
                            this._画面BC_アイキャッチ遷移画面1_回転中.ぼかしと縮小を適用する( 0.0 );      // 0.0秒以内 → 最初から全部適用状態。

                            this._画面D_アイキャッチ遷移画面2_逆回転中.ぼかしと縮小を適用する( 0.0 );     // 全部適用してから
                            this._画面D_アイキャッチ遷移画面2_逆回転中.ぼかしと縮小を解除する();          // ゆっくり解除開始。

                            this._初めての進行描画 = false;
                        }

                        switch( this._クローズ割合.Value )    // 3.0 → 0
                        {
                            // 画面C（アイキャッチ画面）
                            // 画面D（アイキャッチ遷移画面2（逆回転中））
                            // 画面E（切り替え先画面）
                            // ※ このメソッドの呼び出し前に、画面Eが全面描画済みであるものと想定する。

                            case double 割合 when( 2.0 < 割合 ):
                                #region " シーン3. 画面Cを下絵に、左右から中央に向かって（黒帯の移動に従って）、画面Dの描画領域が広くなっていく。"
                                //----------------
                                this._画面D_アイキャッチ遷移画面2_逆回転中.進行描画する( dc );    // 画面D、全表示。（画面Cじゃないので注意）

                                // 以下、画面C（画面Dじゃないので注意）を左右の黒帯の間に描画。

                                割合 = 割合 - 2.0;  // 1.0 → 0

                                this._斜めレイヤーパラメータ.MaskTransform =
                                    Matrix3x2.Scaling( (float) ( 割合 * 0.5 ), 1.0f ) *    // x:0.5 → 0
                                    ( ( 割合 < 0.5 ) ?
                                        Matrix3x2.Rotation( (float) ( Math.PI / ( 5.85 - 1.85 * ( 割合 * 2 ) ) ) ) :
                                        Matrix3x2.Rotation( (float) ( Math.PI / 4.0 ) ) // 45°
                                    ) *
                                    Matrix3x2.Translation( Global.設計画面サイズ.Width / 2.0f, Global.設計画面サイズ.Height / 2.0f ); // 画面中央固定。

                                this._画面BC_アイキャッチ遷移画面1_回転中.進行描画する( dc, layerParameters1: this._斜めレイヤーパラメータ );
                                this._ロゴを描画する( dc );
                                //----------------
                                #endregion
                                break;

                            case double 割合 when( 1.0 < 割合 ):
                                #region " シーン2. 画面Dを全表示。（黒帯は逆回転中）"
                                //----------------
                                this._画面D_アイキャッチ遷移画面2_逆回転中.進行描画する( dc );
                                //----------------
                                #endregion
                                break;

                            case double 割合: // default
                                #region " シーン1. 画面Dを下絵に、中央から上下端に向かって（黒帯の移動に従って）、画面Eの描画領域が減っていく。"
                                //----------------
                                Size2F 画面Dサイズ = this._画面D_アイキャッチ遷移画面2_逆回転中.サイズ;
                                float 画面D表示縦幅 = (float) ( 画面Dサイズ.Height * 割合 / 2.0 );    // height/2 → 0

                                // 上から
                                this._画面D_アイキャッチ遷移画面2_逆回転中.進行描画する( dc, false, new Vector4( 0f, 0f, 画面Dサイズ.Width, 画面D表示縦幅 ) );

                                // 下から
                                this._画面D_アイキャッチ遷移画面2_逆回転中.進行描画する( dc, false, new Vector4( 0f, 画面Dサイズ.Height - 画面D表示縦幅, 画面Dサイズ.Width, 画面Dサイズ.Height ) );
                                //----------------
                                #endregion
                                break;
                        }
                    }
                    break;

                case フェーズ.オープン完了:
                    {
                        // 画面E（切り替え先画面、すでに描画済みと想定）
                    }
                    break;
            }
            //----------------
            #endregion

            #region " 黒帯（全シーンで共通）"
            //----------------
            D2DBatch.Draw( dc, () => {

                var pretrans = dc.Transform;

                for( int i = 0; i < 2; i++ )
                {
                    var context = this._黒幕アニメーション[ i ];

                    if( context.ストーリーボード.Status != StoryboardStatus.Ready )
                        すべて完了 = false;

                    if( context.ストーリーボード.Status == 描画しないStatus )
                        continue;

                    dc.Transform =
                        Matrix3x2.Rotation( (float) context.回転角rad.Value ) *
                        Matrix3x2.Translation( (float) context.中心位置X.Value, (float) context.中心位置Y.Value ) *
                        pretrans;

                    using( var brush = new SolidColorBrush( dc, new Color4( 0f, 0f, 0f, (float) context.不透明度.Value ) ) )
                    {
                        float w = 2800.0f;
                        float h = (float) context.太さ.Value;
                        var rc = new RectangleF( -w / 2f, -h / 2f, w, h );
                        dc.FillRectangle( rc, brush );
                    }
                }

            } );
            //----------------
            #endregion

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



        // private


        private bool _初めての進行描画 = false;

        private class 黒幕 : IDisposable
        {
            public Variable 中心位置X = null!;
            public Variable 中心位置Y = null!;
            public Variable 回転角rad = null!;
            public Variable 太さ = null!;
            public Variable 不透明度 = null!;
            public Storyboard ストーリーボード = null!;

            public void Dispose()
            {
                this.ストーリーボード?.Dispose();
                this.不透明度?.Dispose();
                this.太さ?.Dispose();
                this.回転角rad?.Dispose();
                this.中心位置Y?.Dispose();
                this.中心位置X?.Dispose();
            }
        }
        private 黒幕[] _黒幕アニメーション = null!;

        private readonly 画像D2D _ロゴ;
        private readonly RectangleF _ロゴ表示領域 = new RectangleF( ( 1920f - 730f ) / 2f, ( 1080f - 300f ) / 2f, 730f, 300f );

        /// <summary>
        ///		1:全表示 ... 0:非表示
        /// </summary>
        private Variable _クローズ割合 = null!;
        private readonly 舞台画像 _画面BC_アイキャッチ遷移画面1_回転中;        // 画面BとCで共通。
        private readonly 舞台画像 _画面D_アイキャッチ遷移画面2_逆回転中;

        private readonly PathGeometry _斜めジオメトリマスク;
        private LayerParameters1 _斜めレイヤーパラメータ;

        private const double _シーン1期間 = 0.3;
        private const double _シーン2期間 = 0.4;
        private const double _シーン3期間 = 0.2;

        private void _ロゴを描画する( DeviceContext dc )
        {
            this._ロゴ.描画する(
                dc,
                this._ロゴ表示領域.Left,
                this._ロゴ表示領域.Top,
                1.0f,
                X方向拡大率: ( this._ロゴ表示領域.Width / this._ロゴ.サイズ.Width ),
                Y方向拡大率: ( this._ロゴ表示領域.Height / this._ロゴ.サイズ.Height ),
                レイヤーパラメータ: this._斜めレイヤーパラメータ );
        }
    }
}
