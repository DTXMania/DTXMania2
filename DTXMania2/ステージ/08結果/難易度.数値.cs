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
    partial class 難易度
    {
        class 数値 : IDisposable
        {

            // プロパティ


            public bool アニメ完了 => ( null != this._ストーリーボード && this._ストーリーボード.Status == StoryboardStatus.Ready );



            // 生成と終了


            public 数値()
            {
                this._数字画像 = new フォント画像( @"$(Images)\ParameterFont_Large.png", @"$(Images)\ParameterFont_Large.yaml", 文字幅補正dpx: 0f );
            }

            public void Dispose()
            {
                this._不透明度?.Dispose();
                this._左位置dpx?.Dispose();
                this._ストーリーボード?.Dispose();
                this._数字画像?.Dispose();
            }



            // 進行と描画


            public void 開始する( double 数値 )
            {
                string v = 数値.ToString( "0.00" ).PadLeft( 5 );    // 左余白は ' '。例:" 9.00", "99.99"
                this._難易度値文字列_整数部 = v.Substring( 0, 3 );  // '.' 含む
                this._難易度値文字列_小数部 = v.Substring( 3 );

                this._ストーリーボード?.Dispose();
                this._ストーリーボード = new Storyboard( Global.Animation.Manager );

                #region " 左位置dpx のアニメ構築 "
                //----------------
                // 初期値 +200
                this._左位置dpx?.Dispose();
                this._左位置dpx = new Variable( Global.Animation.Manager, initialValue: +200.0 );

                // 待つ
                using( var 遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 難易度.最初の待機時間sec ) )
                    this._ストーリーボード.AddTransition( this._左位置dpx, 遷移 );

                // 0.0 へ
                using( var 遷移 = Global.Animation.TrasitionLibrary.AccelerateDecelerate( duration: 難易度.アニメ時間sec / 2, finalValue: 0.0, accelerationRatio: 0.2, decelerationRatio: 0.8 ) )
                    this._ストーリーボード.AddTransition( this._左位置dpx, 遷移 );
                //----------------
                #endregion

                #region " 不透明度 のアニメ構築 "
                //----------------
                // 初期値 0.0
                this._不透明度?.Dispose();
                this._不透明度 = new Variable( Global.Animation.Manager, initialValue: 0.0 );

                // 待つ
                using( var 遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 難易度.最初の待機時間sec ) )
                    this._ストーリーボード.AddTransition( this._不透明度, 遷移 );

                // 1.0 へ
                using( var 遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 難易度.アニメ時間sec, finalValue: 1.0 ) )
                    this._ストーリーボード.AddTransition( this._不透明度, 遷移 );
                //----------------
                #endregion

                // アニメーション開始
                this._ストーリーボード.Schedule( Global.Animation.Timer.Time );
            }

            public void アニメを完了する()
            {
                this._ストーリーボード?.Finish( 0.1 );
            }

            public void 進行描画する( float left, float top )
            {
                this._数字画像.不透明度 = (float) this._不透明度.Value;

                float 左位置dpx = left + (float) this._左位置dpx.Value;

                // 整数部を描画する（'.'含む）
                this._数字画像.進行描画する( 左位置dpx, top, this._難易度値文字列_整数部, new Size2F( 1.0f, 1.2f ) );

                // 小数部を描画する
                this._数字画像.進行描画する( 左位置dpx + 127f, top + 17f, this._難易度値文字列_小数部, new Size2F( 1.0f, 1.0f ) );
            }



            // ローカル


            private readonly フォント画像 _数字画像;

            private string _難易度値文字列_小数部 = "";

            private string _難易度値文字列_整数部 = "";    // '.' 含む

            private Storyboard _ストーリーボード = null!;

            private Variable _左位置dpx = null!;

            private Variable _不透明度 = null!;
        }
    }
}
