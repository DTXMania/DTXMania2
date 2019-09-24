using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Animation;
using FDK;

namespace DTXMania.結果
{
    partial class 難易度
    {
        class 下線 : IDisposable
        {

            // 生成と終了


            public 下線( Animation animation )
            {
                this._refAnimation = new WeakReference<Animation>( animation );
                this._ストーリーボード = null;
                this._長さdpx = null;
            }

            public void Dispose()
            {
                this._長さdpx?.Dispose();
                this._ストーリーボード?.Dispose();
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
                    this._長さdpx?.Dispose();
                    this._長さdpx = new Variable( animation.Manager, initialValue: 0.0 );

                    // 待つ
                    using( var 遷移 = animation.TrasitionLibrary.Constant( duration: 難易度.最初の待機時間sec ) )
                        this._ストーリーボード.AddTransition( this._長さdpx, 遷移 );

                    // 全長dpx へ
                    using( var 遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 難易度.アニメ時間sec / 3, finalValue: _全長dpx, accelerationRatio: 0.8, decelerationRatio: 0.2 ) )
                        this._ストーリーボード.AddTransition( this._長さdpx, 遷移 );

                    // アニメーション開始
                    this._ストーリーボード.Schedule( animation.Timer.Time );
                }
            }

            public void 進行描画する( DeviceContext dc, float left, float top )
            {
                if( this._refAnimation.TryGetTarget( out var animation ) )
                {
                    DXResources.Instance.D2DBatchDraw( dc, () => {
                        float 長さdpx = (float) this._長さdpx.Value;
                        using( var brush = new SolidColorBrush( dc, Color4.White ) )
                            dc.FillRectangle( new RectangleF( left + ( _全長dpx - 長さdpx ) / 2f, top, 長さdpx, 3f ), brush );
                    } );
                }
            }



            // ローカル

            private const float _全長dpx = 513f;

            private WeakReference<Animation> _refAnimation;

            private Storyboard _ストーリーボード;

            private Variable _長さdpx;
        }
    }
}
