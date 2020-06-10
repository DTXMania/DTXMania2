using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Animation;
using SharpDX.Direct2D1;

namespace DTXMania2.結果
{
    partial class 達成率更新
    {
        class アイコン : IDisposable
        {

            // プロパティ


            public bool アニメ完了 => ( null != this._ストーリーボード && this._ストーリーボード.Status == StoryboardStatus.Ready );



            // 生成と終了


            public アイコン()
            {
                this._アイコン画像 = new 画像D2D( @"$(Images)\ResultStage\AchivementIcon.png" );
            }

            public void Dispose()
            {
                this._不透明度?.Dispose();
                this._半径倍率?.Dispose();
                this._拡大角度rad?.Dispose();
                this._ストーリーボード?.Dispose();
                this._アイコン画像.Dispose();
            }



            // 進行と描画


            public void 開始する()
            {
                this._ストーリーボード?.Dispose();
                this._ストーリーボード = new Storyboard( Global.Animation.Manager );

                #region " ストーリーボードの構築 "
                //----------------
                // 初期状態
                this._拡大角度rad?.Dispose();
                this._半径倍率?.Dispose();
                this._不透明度?.Dispose();
                this._拡大角度rad = new Variable( Global.Animation.Manager, initialValue: 0.0 );
                this._半径倍率 = new Variable( Global.Animation.Manager, initialValue: 1.5 );
                this._不透明度 = new Variable( Global.Animation.Manager, initialValue: 0.0 );

                // シーン1. 待つ
                {
                    double シーン期間 = 達成率更新._最初の待機時間sec;
                    using( var 拡大角度radの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 半径倍率の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    {
                        this._ストーリーボード.AddTransition( this._拡大角度rad, 拡大角度radの遷移 );
                        this._ストーリーボード.AddTransition( this._半径倍率, 半径倍率の遷移 );
                        this._ストーリーボード.AddTransition( this._不透明度, 不透明度の遷移 );
                    }
                }

                // シーン2. アニメする
                {
                    double シーン期間 = 達成率更新._アニメ時間sec;
                    using( var 拡大角度radの遷移 = Global.Animation.TrasitionLibrary.Linear( duration: シーン期間, finalValue: 2 * Math.PI ) )
                    using( var 半径倍率の遷移 = Global.Animation.TrasitionLibrary.AccelerateDecelerate( duration: シーン期間, finalValue: 0.5, accelerationRatio: 0.1, decelerationRatio: 0.9 ) )
                    using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: シーン期間, finalValue: 1.0 ) )
                    {
                        this._ストーリーボード.AddTransition( this._拡大角度rad, 拡大角度radの遷移 );
                        this._ストーリーボード.AddTransition( this._半径倍率, 半径倍率の遷移 );
                        this._ストーリーボード.AddTransition( this._不透明度, 不透明度の遷移 );
                    }
                }

                // シーン3. 待つ
                {
                    double シーン期間 = 達成率更新._登場後の待機時間sec;
                    using( var 拡大角度radの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 半径倍率の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    {
                        this._ストーリーボード.AddTransition( this._拡大角度rad, 拡大角度radの遷移 );
                        this._ストーリーボード.AddTransition( this._半径倍率, 半径倍率の遷移 );
                        this._ストーリーボード.AddTransition( this._不透明度, 不透明度の遷移 );
                    }
                }

                // シーン4. 等倍に戻る
                {
                    double シーン期間 = 達成率更新._退場アニメ時間sec;
                    using( var 拡大角度radの遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    using( var 半径倍率の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: シーン期間, finalValue: 0.0 ) )
                    using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: シーン期間 ) )
                    {
                        this._ストーリーボード.AddTransition( this._拡大角度rad, 拡大角度radの遷移 );
                        this._ストーリーボード.AddTransition( this._半径倍率, 半径倍率の遷移 );
                        this._ストーリーボード.AddTransition( this._不透明度, 不透明度の遷移 );
                    }
                }
                //----------------
                #endregion

                // アニメーション開始
                this._ストーリーボード.Schedule( Global.Animation.Timer.Time );
            }

            public void アニメを完了する()
            {
                this._ストーリーボード?.Finish( 0.0 );
            }

            public void 進行描画する( DeviceContext dc, float left, float top )
            {
                double 回転による拡大率 = Math.Abs( Math.Cos( this._拡大角度rad.Value ) );    // (0) 1 → 0 → 1（π) → 0 → 1 (2π)
                float 拡大率 = (float) ( 1.0 + 回転による拡大率 * this._半径倍率.Value );
                float 左位置dpx = left + ( ( 1.0f - 拡大率 ) * this._アイコン画像.サイズ.Width ) / 2.0f;
                float 上位置dpx = top + ( ( 1.0f - 拡大率 ) * this._アイコン画像.サイズ.Height ) / 2.0f;
                
                this._アイコン画像.描画する( dc, 左位置dpx, 上位置dpx, 不透明度0to1: (float) this._不透明度.Value, X方向拡大率: 拡大率, Y方向拡大率: 拡大率 );
            }



            // ローカル


            private readonly 画像D2D _アイコン画像;

            private Storyboard _ストーリーボード = null!;

            private Variable _拡大角度rad = null!;

            private Variable _半径倍率 = null!;

            private Variable _不透明度 = null!;
        }
    }
}
