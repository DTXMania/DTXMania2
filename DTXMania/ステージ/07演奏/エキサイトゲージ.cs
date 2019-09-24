using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Animation;
using FDK;

namespace DTXMania.演奏
{
    class エキサイトゲージ : IDisposable
    {

        // 生成と終了


        public エキサイトゲージ()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this._ゲージ枠通常 = new テクスチャ( @"$(System)images\演奏\エキサイトゲージ通常.png" );
                this._ゲージ枠DANGER = new テクスチャ( @"$(System)images\演奏\エキサイトゲージDANGER.png" );

                var dc = DXResources.Instance.既定のD2D1DeviceContext;

                this._通常ブラシ = new SolidColorBrush( dc, new Color4( 0xfff9b200 ) );      // ABGR
                this._DANGERブラシ = new SolidColorBrush( dc, new Color4( 0xff0000ff ) );
                this._MAXブラシ = new SolidColorBrush( dc, new Color4( 0xff00c9f4 ) );

                this._ゲージ量 = null;
                this._ゲージ量のストーリーボード = null;

                this._ゲージ量 = new Variable( DXResources.Instance.Animation.Manager, initialValue: 0 );
                this._ゲージ量のストーリーボード = null;
            }
        }

        public virtual void Dispose()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this._通常ブラシ?.Dispose();
                this._DANGERブラシ?.Dispose();
                this._MAXブラシ?.Dispose();
                this._ゲージ量のストーリーボード?.Dispose();
                this._ゲージ量?.Dispose();

                this._ゲージ枠通常?.Dispose();
                this._ゲージ枠DANGER?.Dispose();
            }
        }



        // 進行と描画


        /// <param name="ゲージ量">
        ///		0～1。 0.0で0%、1.0で100%。
        /// </param>
        public void 進行描画する( DeviceContext dc, double ゲージ量 )
        {
            ゲージ量 = Math.Max( Math.Min( ゲージ量, 1f ), 0f );

            var MAXゲージ領域 = new RectangleF( 557f, 971f, 628f, 26f );


            // 枠を描画。

            if( 0.25 > this._ゲージ量.Value )
            {
                this._ゲージ枠DANGER.描画する( 540f, 955f );
            }
            else
            {
                this._ゲージ枠通常.描画する( 540f, 955f );
            }


            // ゲージの進行。
            // ゲージ量のゴールが動くたび、アニメーションを開始して追従する。

            if( ゲージ量 != this._ゲージ量.FinalValue )
            {
                var animation = DXResources.Instance.Animation;

                this._ゲージ量のストーリーボード = new Storyboard( animation.Manager );

                using( var 移動遷移 = animation.TrasitionLibrary.Linear( duration: 0.4, finalValue: ゲージ量 ) )
                using( var 跳ね返り遷移1 = animation.TrasitionLibrary.Reversal( duration: 0.2 ) )
                using( var 跳ね返り遷移2 = animation.TrasitionLibrary.Reversal( duration: 0.2 ) )
                {
                    this._ゲージ量のストーリーボード.AddTransition( this._ゲージ量, 移動遷移 );
                    this._ゲージ量のストーリーボード.AddTransition( this._ゲージ量, 跳ね返り遷移1 );
                    this._ゲージ量のストーリーボード.AddTransition( this._ゲージ量, 跳ね返り遷移2 );
                }

                this._ゲージ量のストーリーボード.Schedule( animation.Timer.Time );
            }

            
            // ゲージの描画。

            DXResources.Instance.D2DBatchDraw( dc, () => {

                dc.PrimitiveBlend = PrimitiveBlend.SourceOver;

                var ゲージ領域 = MAXゲージ領域;
                ゲージ領域.Width *= Math.Min( (float) this._ゲージ量.Value, 1.0f );

                var ブラシ =
                    ( 0.25 > this._ゲージ量.Value ) ? this._DANGERブラシ :
                    ( 1.0 <= this._ゲージ量.Value ) ? this._MAXブラシ : this._通常ブラシ;

                dc.FillRectangle( ゲージ領域, ブラシ );

            } );
        }



        // private


        private テクスチャ _ゲージ枠通常 = null;

        private テクスチャ _ゲージ枠DANGER = null;

        private SolidColorBrush _通常ブラシ = null;    // 青

        private SolidColorBrush _DANGERブラシ = null;  // 赤

        private SolidColorBrush _MAXブラシ = null;     // 橙

        private Variable _ゲージ量 = null;

        private Storyboard _ゲージ量のストーリーボード = null;
    }
}
