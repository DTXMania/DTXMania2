using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Animation;
using FDK;

namespace DTXMania2.演奏
{
    class エキサイトゲージ : IDisposable
    {

        // 生成と終了


        public エキサイトゲージ()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._ゲージ枠通常 = new 画像( @"$(Images)\PlayStage\ExciteGauge.png" );
            this._ゲージ枠DANGER = new 画像( @"$(Images)\PlayStage\ExciteGauge_Danger.png" );

            var dc = Global.既定のD2D1DeviceContext;

            this._通常ブラシ = new SolidColorBrush( dc, new Color4( 0xfff9b200 ) );      // ABGR
            this._DANGERブラシ = new SolidColorBrush( dc, new Color4( 0xff0000ff ) );
            this._MAXブラシ = new SolidColorBrush( dc, new Color4( 0xff00c9f4 ) );

            this._ゲージ量 = new Variable( Global.Animation.Manager, initialValue: 0 );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._ゲージ量のストーリーボード?.Dispose();
            this._ゲージ量.Dispose();

            this._MAXブラシ.Dispose();
            this._DANGERブラシ.Dispose();
            this._通常ブラシ.Dispose();

            this._ゲージ枠DANGER.Dispose();
            this._ゲージ枠通常.Dispose();
        }



        // 進行と描画


        /// <param name="ゲージ量">
        ///		0～1。 0.0で0%、1.0で100%。
        /// </param>
        public void 進行描画する( DeviceContext dc, double ゲージ量 )
        {
            ゲージ量 = Math.Clamp( ゲージ量, min: 0f, max: 1f );

            var MAXゲージ領域 = new RectangleF( 557f, 971f, 628f, 26f );

            #region " 枠を描画。"
            //----------------
            if( 0.25 > this._ゲージ量.Value )
            {
                this._ゲージ枠DANGER.描画する( 540f, 955f );
            }
            else
            {
                this._ゲージ枠通常.描画する( 540f, 955f );
            }
            //----------------
            #endregion

            #region " ゲージの進行。ゲージ量のゴールが動くたび、アニメーションを開始して追従する。"
            //----------------
            if( ゲージ量 != this._ゲージ量.FinalValue )
            {
                this._ゲージ量のストーリーボード?.Dispose();
                this._ゲージ量のストーリーボード = new Storyboard( Global.Animation.Manager );

                using( var 移動遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 0.4, finalValue: ゲージ量 ) )
                using( var 跳ね返り遷移1 = Global.Animation.TrasitionLibrary.Reversal( duration: 0.2 ) )
                using( var 跳ね返り遷移2 = Global.Animation.TrasitionLibrary.Reversal( duration: 0.2 ) )
                {
                    this._ゲージ量のストーリーボード.AddTransition( this._ゲージ量, 移動遷移 );
                    this._ゲージ量のストーリーボード.AddTransition( this._ゲージ量, 跳ね返り遷移1 );
                    this._ゲージ量のストーリーボード.AddTransition( this._ゲージ量, 跳ね返り遷移2 );
                }

                this._ゲージ量のストーリーボード.Schedule( Global.Animation.Timer.Time );
            }
            //----------------
            #endregion

            #region " ゲージの描画。"
            //----------------
            D2DBatch.Draw( dc, () => {

                var ゲージ領域 = MAXゲージ領域;
                ゲージ領域.Width *= Math.Min( (float) this._ゲージ量.Value, 1.0f );
                var ブラシ =
                    ( 0.25 > this._ゲージ量.Value ) ? this._DANGERブラシ :
                    ( 1.0 <= this._ゲージ量.Value ) ? this._MAXブラシ : this._通常ブラシ;

                dc.FillRectangle( ゲージ領域, ブラシ );

            } );
            //----------------
            #endregion
        }



        // ローカル


        private readonly 画像 _ゲージ枠通常;

        private readonly 画像 _ゲージ枠DANGER;

        private readonly SolidColorBrush _通常ブラシ;    // 青

        private readonly SolidColorBrush _DANGERブラシ;  // 赤

        private readonly SolidColorBrush _MAXブラシ;     // 橙

        private readonly Variable _ゲージ量;

        private Storyboard _ゲージ量のストーリーボード = null!;
    }
}
