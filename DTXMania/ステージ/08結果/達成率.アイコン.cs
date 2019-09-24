using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Animation;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania.結果
{
    partial class 達成率
    {
        class アイコン : IDisposable
        {

            // 生成と終了


            public アイコン( Animation animation )
            {
                this._refAnimation = new WeakReference<Animation>( animation );
                this._アイコン画像 = new テクスチャ( @"$(System)Images\結果\達成率アイコン.png" );
                this._ストーリーボード = null;
                this._拡大角度rad = null;
                this._半径倍率 = null;
                this._不透明度 = null;
            }

            public void Dispose()
            {
                this._不透明度?.Dispose();
                this._半径倍率?.Dispose();
                this._拡大角度rad?.Dispose();
                this._ストーリーボード?.Dispose();
                this._アイコン画像?.Dispose();
                this._refAnimation = null;
            }



            // 進行と描画


            public void 開始する()
            {
                if( this._refAnimation.TryGetTarget( out var animation ) )
                {
                    this._ストーリーボード?.Dispose();
                    this._ストーリーボード = new Storyboard( animation.Manager );


                    // 初期値 0.0
                    this._拡大角度rad?.Dispose();
                    this._拡大角度rad = new Variable( animation.Manager, initialValue: 0.0 );
                 
                    // 待つ
                    using( var 遷移 = animation.TrasitionLibrary.Constant( duration: 達成率.最初の待機時間sec ) )
                        this._ストーリーボード.AddTransition( this._拡大角度rad, 遷移 );

                    // 2π へ
                    using( var 遷移 = animation.TrasitionLibrary.Linear( duration: 達成率.アニメ時間sec, finalValue: 2 * Math.PI ) )
                        this._ストーリーボード.AddTransition( this._拡大角度rad, 遷移 );


                    // 初期値 1.0
                    this._半径倍率?.Dispose();
                    this._半径倍率 = new Variable( animation.Manager, initialValue: 1.0 );

                    // 待つ
                    using( var 遷移 = animation.TrasitionLibrary.Constant( duration: 達成率.最初の待機時間sec ) )
                        this._ストーリーボード.AddTransition( this._半径倍率, 遷移 );

                    // 0.0 へ
                    using( var 遷移 = animation.TrasitionLibrary.Linear( duration: 達成率.アニメ時間sec, finalValue: 0.0 ) )
                        this._ストーリーボード.AddTransition( this._半径倍率, 遷移 );


                    // 初期値 0.0
                    this._不透明度?.Dispose();
                    this._不透明度 = new Variable( animation.Manager, initialValue: 0.0 );

                    // 待つ
                    using( var 遷移 = animation.TrasitionLibrary.Constant( duration: 達成率.最初の待機時間sec ) )
                        this._ストーリーボード.AddTransition( this._不透明度, 遷移 );

                    // 1.0 へ
                    using( var 遷移 = animation.TrasitionLibrary.Linear( duration: 達成率.アニメ時間sec, finalValue: 1.0 ) )
                        this._ストーリーボード.AddTransition( this._不透明度, 遷移 );


                    // アニメーション開始
                    this._ストーリーボード.Schedule( animation.Timer.Time );
                }
            }

            public void 進行描画する( DeviceContext dc, float left, float top )
            {
                double 回転による拡大率 = Math.Abs( Math.Cos( this._拡大角度rad.Value ) );    // (0) 1 → 0 → 1（π) → 0 → 1 (2π)
                float 拡大率 = (float) ( 1.0 + 回転による拡大率 * this._半径倍率.Value );
                float 左位置dpx = left + ( ( 1.0f - 拡大率 ) * this._アイコン画像.サイズ.Width ) / 2.0f;
                float 上位置dpx = top + ( ( 1.0f - 拡大率 ) * this._アイコン画像.サイズ.Height ) / 2.0f;
                this._アイコン画像.描画する( 左位置dpx, 上位置dpx, 不透明度0to1: (float) this._不透明度.Value, X方向拡大率: 拡大率, Y方向拡大率: 拡大率 );
            }



            // ローカル


            private WeakReference<Animation> _refAnimation;

            private テクスチャ _アイコン画像;

            private Storyboard _ストーリーボード;

            private Variable _拡大角度rad;

            private Variable _半径倍率;

            private Variable _不透明度;
        }
    }
}
