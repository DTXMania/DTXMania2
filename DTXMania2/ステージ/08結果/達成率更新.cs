using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Animation;
using FDK;

namespace DTXMania2.結果
{
    /// <summary>
    ///     最高達成率を更新した場合の達成率表示。
    /// </summary>
    partial class 達成率更新 : 達成率Base
    {

        // プロパティ


        public override bool アニメ完了
        {
            get
            {
                // 黒帯終了？
                foreach( var anim in this._黒帯アニメーション )
                {
                    if( anim.ストーリーボード.Status != StoryboardStatus.Ready )
                        return false;   // まだ
                }

                // その他アニメ終了？
                return this._数値.アニメ完了 && this._アイコン.アニメ完了 && this._下線.アニメ完了;
            }
        }



        // 生成と終了


        public 達成率更新( float 速度倍率 = 1.0f )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            double 秒( double v ) => ( v / 速度倍率 );

            this._アイコン = new アイコン();
            this._下線 = new 下線();
            this._数値 = new 数値();


            // ストーリーボードの構築・黒帯

            this._黒帯アニメーション = new 黒帯[ 3 ];

            #region " ストーリーボードの構築(1) 左→右へ移動する帯 "
            //----------------
            {
                // 初期状態
                var 黒帯 = this._黒帯アニメーション[ 0 ] = new 黒帯() {
                    中心位置X = new Variable( Global.Animation.Manager, initialValue: 1206.0 ),
                    中心位置Y = new Variable( Global.Animation.Manager, initialValue: 540.0 ),
                    回転角rad = new Variable( Global.Animation.Manager, initialValue: MathUtil.DegreesToRadians( 20.0f ) ),
                    太さ = new Variable( Global.Animation.Manager, initialValue: 198.0 ),
                    不透明度 = new Variable( Global.Animation.Manager, initialValue: 0.0 ),
                    ストーリーボード = new Storyboard( Global.Animation.Manager ),
                };

                // シーン1. 待つ
                {
                    double シーン期間 = 秒( 達成率更新._最初の待機時間sec / 2 );
                    using( var 中心位置Xの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 中心位置Yの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 回転角radの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 太さの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    {
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置X, 中心位置Xの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置Y, 中心位置Yの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.回転角rad, 回転角radの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.太さ, 太さの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.不透明度, 不透明度の遷移 );
                    }
                }

                // シーン2. 左から真ん中へ、細く不透明になりつつ移動。
                {
                    double シーン期間 = 秒( 0.15 );
                    using( var 中心位置Xの遷移 = Global.Animation.TrasitionLibrary.Linear( duration: シーン期間, finalValue: 1564.0 ) )
                    using( var 中心位置Yの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 回転角radの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 太さの遷移 = Global.Animation.TrasitionLibrary.AccelerateDecelerate( duration: シーン期間, finalValue: 14.0, accelerationRatio: 0.9, decelerationRatio: 0.1 ) )
                    using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: シーン期間, finalValue: 0.75 ) )
                    {
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置X, 中心位置Xの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置Y, 中心位置Yの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.回転角rad, 回転角radの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.太さ, 太さの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.不透明度, 不透明度の遷移 );
                    }
                }

                // シーン3. 真ん中から右へ移動。
                {
                    double シーン期間 = 秒( 0.1 );
                    using( var 中心位置Xの遷移 = Global.Animation.TrasitionLibrary.Linear( duration: シーン期間, finalValue: 1922.0 ) )
                    using( var 中心位置Yの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 回転角radの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 太さの遷移 = Global.Animation.TrasitionLibrary.Linear( duration: シーン期間, finalValue: 20.0 ) )
                    using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    {
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置X, 中心位置Xの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置Y, 中心位置Yの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.回転角rad, 回転角radの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.太さ, 太さの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.不透明度, 不透明度の遷移 );
                    }
                }

                // シーン4. 待つ
                {
                    double シーン期間 = 秒( 達成率更新._登場後の待機時間sec );
                    using( var 中心位置Xの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 中心位置Yの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 回転角radの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 太さの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    {
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置X, 中心位置Xの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置Y, 中心位置Yの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.回転角rad, 回転角radの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.太さ, 太さの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.不透明度, 不透明度の遷移 );
                    }
                }

                // シーン5. 右へ消えていく。
                {
                    double シーン期間 = 秒( 達成率更新._退場アニメ時間sec );
                    using( var 中心位置Xの遷移 = Global.Animation.TrasitionLibrary.Linear( duration: シーン期間, finalValue: 1922.0 + 66.0 ) )
                    using( var 中心位置Yの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 回転角radの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 太さの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: シーン期間, finalValue: 0.0 ) )
                    {
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置X, 中心位置Xの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置Y, 中心位置Yの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.回転角rad, 回転角radの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.太さ, 太さの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.不透明度, 不透明度の遷移 );
                    }
                }
            }
            //----------------
            #endregion

            #region " ストーリーボードの構築(2) 右→左へ移動する帯 "
            //----------------
            {
                // 初期状態
                var 黒帯 = this._黒帯アニメーション[ 1 ] = new 黒帯() {
                    中心位置X = new Variable( Global.Animation.Manager, initialValue: 1922.0 ),
                    中心位置Y = new Variable( Global.Animation.Manager, initialValue: 540.0 ),
                    回転角rad = new Variable( Global.Animation.Manager, initialValue: MathUtil.DegreesToRadians( 20.0f ) ),
                    太さ = new Variable( Global.Animation.Manager, initialValue: 198.0 ),
                    不透明度 = new Variable( Global.Animation.Manager, initialValue: 0.0 ),
                    ストーリーボード = new Storyboard( Global.Animation.Manager ),
                };

                // シーン1. 待つ
                {
                    double シーン期間 = 秒( 達成率更新._最初の待機時間sec /2 );
                    using( var 中心位置Xの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 中心位置Yの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 回転角radの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 太さの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    {
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置X, 中心位置Xの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置Y, 中心位置Yの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.回転角rad, 回転角radの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.太さ, 太さの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.不透明度, 不透明度の遷移 );
                    }
                }

                // シーン2. 右から真ん中へ、細く不透明になりつつ移動。
                {
                    double シーン期間 = 秒( 0.15 );
                    using( var 中心位置Xの遷移 = Global.Animation.TrasitionLibrary.Linear( duration: シーン期間, finalValue: 1564.0 ) )
                    using( var 中心位置Yの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 回転角radの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 太さの遷移 = Global.Animation.TrasitionLibrary.AccelerateDecelerate( duration: シーン期間, finalValue: 14.0, accelerationRatio: 0.9, decelerationRatio: 0.1 ) )
                    using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: シーン期間, finalValue: 0.75 ) )
                    {
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置X, 中心位置Xの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置Y, 中心位置Yの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.回転角rad, 回転角radの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.太さ, 太さの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.不透明度, 不透明度の遷移 );
                    }
                }

                // シーン3. 真ん中から左へ移動。
                {
                    double シーン期間 = 秒( 0.1 );
                    using( var 中心位置Xの遷移 = Global.Animation.TrasitionLibrary.Linear( duration: シーン期間, finalValue: 1214.0 ) )
                    using( var 中心位置Yの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 回転角radの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 太さの遷移 = Global.Animation.TrasitionLibrary.Linear( duration: シーン期間, finalValue: 20.0 ) )
                    using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    {
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置X, 中心位置Xの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置Y, 中心位置Yの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.回転角rad, 回転角radの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.太さ, 太さの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.不透明度, 不透明度の遷移 );
                    }
                }

                // シーン4. 待つ
                {
                    double シーン期間 = 秒( 達成率更新._登場後の待機時間sec );
                    using( var 中心位置Xの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 中心位置Yの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 回転角radの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 太さの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    {
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置X, 中心位置Xの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置Y, 中心位置Yの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.回転角rad, 回転角radの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.太さ, 太さの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.不透明度, 不透明度の遷移 );
                    }
                }

                // シーン5. 左へ消えていく。
                {
                    double シーン期間 = 秒( 達成率更新._退場アニメ時間sec );
                    using( var 中心位置Xの遷移 = Global.Animation.TrasitionLibrary.Linear( duration: シーン期間, finalValue: 1214.0 - 66.0 ) )
                    using( var 中心位置Yの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 回転角radの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 太さの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: シーン期間, finalValue: 0.0 ) )
                    {
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置X, 中心位置Xの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置Y, 中心位置Yの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.回転角rad, 回転角radの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.太さ, 太さの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.不透明度, 不透明度の遷移 );
                    }
                }
            }
            //----------------
            #endregion

            #region " ストーリーボードの構築(3) 真ん中の帯 "
            //----------------
            {
                // 初期状態
                var 黒帯 = this._黒帯アニメーション[ 2 ] = new 黒帯() {
                    中心位置X = new Variable( Global.Animation.Manager, initialValue: 1564.0 ),
                    中心位置Y = new Variable( Global.Animation.Manager, initialValue: 540.0 ),
                    回転角rad = new Variable( Global.Animation.Manager, initialValue: MathUtil.DegreesToRadians( 20.0f ) ),
                    太さ = new Variable( Global.Animation.Manager, initialValue: 0.0 ),
                    不透明度 = new Variable( Global.Animation.Manager, initialValue: 0.0 ),
                    ストーリーボード = new Storyboard( Global.Animation.Manager ),
                };

                // シーン1. 待つ
                {
                    double シーン期間 = 秒( 達成率更新._最初の待機時間sec / 2 );
                    using( var 中心位置Xの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 中心位置Yの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 回転角radの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 太さの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    {
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置X, 中心位置Xの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置Y, 中心位置Yの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.回転角rad, 回転角radの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.太さ, 太さの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.不透明度, 不透明度の遷移 );
                    }
                }

                // シーン2. 何もしない
                {
                    double シーン期間 = 秒( 0.15 );
                    using( var 中心位置Xの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 中心位置Yの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 回転角radの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 太さの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    {
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置X, 中心位置Xの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置Y, 中心位置Yの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.回転角rad, 回転角radの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.太さ, 太さの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.不透明度, 不透明度の遷移 );
                    }
                }

                // シーン3. 真ん中で太くなる。
                {
                    double シーン期間 = 秒( 0.1 );
                    using( var 中心位置Xの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 中心位置Yの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 回転角radの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 太さの遷移 = Global.Animation.TrasitionLibrary.AccelerateDecelerate( duration: シーン期間, finalValue: 600.0, accelerationRatio: 0.9, decelerationRatio: 0.1 ) )
                    using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: シーン期間, finalValue: 0.75 ) )
                    {
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置X, 中心位置Xの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置Y, 中心位置Yの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.回転角rad, 回転角radの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.太さ, 太さの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.不透明度, 不透明度の遷移 );
                    }
                }

                // シーン4. 待つ
                {
                    double シーン期間 = 秒( 達成率更新._登場後の待機時間sec );
                    using( var 中心位置Xの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 中心位置Yの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 回転角radの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 太さの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    {
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置X, 中心位置Xの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置Y, 中心位置Yの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.回転角rad, 回転角radの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.太さ, 太さの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.不透明度, 不透明度の遷移 );
                    }
                }

                // シーン5. 真ん中で細くなって消えていく。
                {
                    double シーン期間 = 秒( 達成率更新._退場アニメ時間sec );
                    using( var 中心位置Xの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 中心位置Yの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 回転角radの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 太さの遷移 = Global.Animation.TrasitionLibrary.Linear( duration: シーン期間, finalValue: 0.0 ) )
                    using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: シーン期間, finalValue: 0.0 ) )
                    {
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置X, 中心位置Xの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.中心位置Y, 中心位置Yの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.回転角rad, 回転角radの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.太さ, 太さの遷移 );
                        黒帯.ストーリーボード.AddTransition( 黒帯.不透明度, 不透明度の遷移 );
                    }
                }
            }
            //----------------
            #endregion
        }

        public override void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._アイコン.Dispose();
            this._下線.Dispose();
            this._数値.Dispose();

            foreach( var anim in this._黒帯アニメーション )
                anim.Dispose();
        }



        // 進行と描画


        public override void アニメを完了する()
        {
            foreach( var anim in this._黒帯アニメーション )
                anim.ストーリーボード?.Finish( 0.0 );

            this._アイコン.アニメを完了する();
            this._下線.アニメを完了する();
            this._数値.アニメを完了する();
        }

        public override void 進行描画する( DeviceContext dc, float left, float top, double 達成率0to100 )
        {
            if( this._初めての進行描画 )
            {
                this._初めての進行描画 = false;
                this._アイコン.開始する();
                this._下線.開始する();
                this._数値.開始する( 達成率0to100 );
                var start = Global.Animation.Timer.Time;
                foreach( var anim in this._黒帯アニメーション )
                    anim.ストーリーボード?.Schedule( start );
            }

            var preTrans = dc.Transform;

            // 黒帯
            foreach( var 黒帯 in this._黒帯アニメーション )
            {
                dc.Transform =
                    Matrix3x2.Rotation( (float) 黒帯.回転角rad.Value ) *
                    Matrix3x2.Translation( (float) 黒帯.中心位置X.Value, (float) 黒帯.中心位置Y.Value ) *
                    preTrans;

                using var brush = new SolidColorBrush( dc, new Color4( 0f, 0f, 0f, (float) 黒帯.不透明度.Value ) );
                float w = (float) 黒帯.太さ.Value;
                float h = 1600.0f;
                var rc = new RectangleF( -w / 2f, -h / 2f, w, h );
                dc.FillRectangle( rc, brush );
            }

            dc.Transform = preTrans;

            this._アイコン.進行描画する( dc, left, top );
            this._数値.進行描画する( dc, left + 150f, top + 48f );
            this._下線.進行描画する( dc, left + 33f, top + 198f );
        }



        // ローカル


        private bool _初めての進行描画 = true;

        private const double _最初の待機時間sec = 1.0;

        private const double _アニメ時間sec = 0.25;

        private const double _登場後の待機時間sec = 3.0;

        private const double _退場アニメ時間sec = 0.1;

        private class 黒帯 : IDisposable
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
        private readonly 黒帯[] _黒帯アニメーション;

        private readonly 達成率更新.アイコン _アイコン;

        private readonly 達成率更新.下線 _下線;

        private readonly 達成率更新.数値 _数値;
    }
}
