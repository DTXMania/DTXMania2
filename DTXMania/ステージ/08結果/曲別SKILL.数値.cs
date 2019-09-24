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
    partial class 曲別SKILL
    {
        class 数値 : IDisposable
        {

            // 生成と終了


            public 数値( Animation animation )
            {
                this._refAnimation = new WeakReference<Animation>( animation );
                this._数字画像 = new 画像フォント( @"$(System)images\パラメータ文字_大.png", @"$(System)images\パラメータ文字_大.yaml", 文字幅補正dpx: 0f );
                this._ストーリーボード = null;
                this._左位置dpx = null;
                this._不透明度 = null;
            }

            public void Dispose()
            {
                this._不透明度?.Dispose();
                this._左位置dpx?.Dispose();
                this._ストーリーボード?.Dispose();
                this._数字画像?.Dispose();
                this._refAnimation = null;
            }



            // 進行と描画


            public void 開始する( double 数値 )
            {
                string v = 数値.ToString( "0.00" ).PadLeft( 6 );    // 左余白は ' '。例:" 19.00", "199.99"
                this._スキル値文字列_整数部 = v.Substring( 0, 4 );  // '.' 含む
                this._スキル値文字列_小数部 = v.Substring( 4 );

                if( this._refAnimation.TryGetTarget( out var animation ) )
                {
                    this._ストーリーボード?.Dispose();
                    this._ストーリーボード = new Storyboard( animation.Manager );


                    // 初期値 +200
                    this._左位置dpx?.Dispose();
                    this._左位置dpx = new Variable( animation.Manager, initialValue: +200.0 );

                    // 待つ
                    using( var 遷移 = animation.TrasitionLibrary.Constant( duration:曲別SKILL.最初の待機時間sec ) )
                        this._ストーリーボード.AddTransition( this._左位置dpx, 遷移 );

                    // 0.0 へ
                    using( var 遷移 = animation.TrasitionLibrary.AccelerateDecelerate( duration: 曲別SKILL.アニメ時間sec / 2, finalValue: 0.0, accelerationRatio: 0.2, decelerationRatio: 0.8 ) )
                        this._ストーリーボード.AddTransition( this._左位置dpx, 遷移 );


                    // 初期値 0.0
                    this._不透明度?.Dispose();
                    this._不透明度 = new Variable( animation.Manager, initialValue: 0.0 );

                    // 待つ
                    using( var 遷移 = animation.TrasitionLibrary.Constant( duration: 曲別SKILL.最初の待機時間sec ) )
                        this._ストーリーボード.AddTransition( this._不透明度, 遷移 );

                    // 1.0 へ
                    using( var 遷移 = animation.TrasitionLibrary.Linear( duration: 曲別SKILL.アニメ時間sec, finalValue: 1.0 ) )
                        this._ストーリーボード.AddTransition( this._不透明度, 遷移 );


                    // アニメーション開始
                    this._ストーリーボード.Schedule( animation.Timer.Time );
                }
            }

            public void 進行描画する( DeviceContext dc, float left, float top )
            {
                this._数字画像.不透明度 = (float) this._不透明度.Value;

                float 左位置dpx = left + (float) this._左位置dpx.Value;

                // 整数部を描画する（'.'含む）
                this._数字画像.描画する( dc, 左位置dpx, top, this._スキル値文字列_整数部, new Size2F( 1.0f, 1.2f ) );

                // 小数部を描画する
                this._数字画像.描画する( dc, 左位置dpx + 180f, top + 17f, this._スキル値文字列_小数部, new Size2F( 1.0f, 1.0f ) );
            }



            // ローカル


            private WeakReference<Animation> _refAnimation;

            private 画像フォント _数字画像;

            private string _スキル値文字列_小数部;

            private string _スキル値文字列_整数部;    // '.' 含む

            private Storyboard _ストーリーボード;

            private Variable _左位置dpx;

            private Variable _不透明度;
        }
    }
}
