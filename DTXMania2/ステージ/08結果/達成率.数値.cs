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
    partial class 達成率
    {
        class 数値 : IDisposable
        {

            // プロパティ


            public bool アニメ完了 => ( null != this._ストーリーボード && this._ストーリーボード.Status == StoryboardStatus.Ready );



            // 生成と終了


            public 数値()
            {
                this._数字画像 = new フォント画像( @"$(Images)\ParameterFont_LargeBoldItalic.png", @"$(Images)\ParameterFont_LargeBoldItalic.yaml", 文字幅補正dpx: -2f );
                this._MAX = new 画像( @"$(Images)\ResultStage\MAX.png" );
                this._MAXである = false;
            }

            public void Dispose()
            {
                this._MAX用半径倍率?.Dispose();
                this._MAX用拡大角度rad?.Dispose();
                this._不透明度?.Dispose();
                this._左位置dpx?.Dispose();
                this._ストーリーボード?.Dispose();
                this._MAX.Dispose();
                this._数字画像.Dispose();
            }



            // 進行と描画


            public void 開始する( double 数値0to100 )
            {
                if( 数値0to100 < 100.0 )
                {
                    this._MAXである = false;
                    string v = 数値0to100.ToString( "0.00" ).PadLeft( 6 ) + '%';    // 左余白は ' '。例:" 19.00%", "100.00%"
                    this._達成率文字列_整数部 = v[ 0..4 ];  // '.' 含む
                    this._達成率文字列_小数部 = v[ 4.. ];   // '%' 含む
                }
                else
                {
                    this._MAXである = true;
                    this._達成率文字列_整数部 = "";
                    this._達成率文字列_小数部 = "";
                }

                this._ストーリーボード?.Dispose();
                this._ストーリーボード = new Storyboard( Global.Animation.Manager );

                #region " 左位置dpx のアニメ構築 "
                //----------------
                // 初期値 +200
                this._左位置dpx?.Dispose();
                this._左位置dpx = new Variable( Global.Animation.Manager, initialValue: +200.0 );

                // 待つ
                using( var 遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 達成率.最初の待機時間sec ) )
                    this._ストーリーボード.AddTransition( this._左位置dpx, 遷移 );

                // 0.0 へ
                using( var 遷移 = Global.Animation.TrasitionLibrary.AccelerateDecelerate( duration: 達成率.アニメ時間sec / 2, finalValue: 0.0, accelerationRatio: 0.2, decelerationRatio: 0.8 ) )
                    this._ストーリーボード.AddTransition( this._左位置dpx, 遷移 );
                //----------------
                #endregion

                #region " 不透明度 のアニメ構築 "
                //----------------
                // 初期値 0.0
                this._不透明度?.Dispose();
                this._不透明度 = new Variable( Global.Animation.Manager, initialValue: 0.0 );

                // 待つ
                using( var 遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 達成率.最初の待機時間sec ) )
                    this._ストーリーボード.AddTransition( this._不透明度, 遷移 );

                // 1.0 へ
                using( var 遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 達成率.アニメ時間sec, finalValue: 1.0 ) )
                    this._ストーリーボード.AddTransition( this._不透明度, 遷移 );
                //----------------
                #endregion

                #region " MAX用拡大角度rad のアニメ構築 "
                //----------------
                // 初期値 0.0
                this._MAX用拡大角度rad?.Dispose();
                this._MAX用拡大角度rad = new Variable( Global.Animation.Manager, initialValue: 0.0 );

                // 待つ
                using( var 遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 達成率.最初の待機時間sec ) )
                    this._ストーリーボード.AddTransition( this._MAX用拡大角度rad, 遷移 );

                // 2π へ
                using( var 遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 達成率.アニメ時間sec, finalValue: 2 * Math.PI ) )
                    this._ストーリーボード.AddTransition( this._MAX用拡大角度rad, 遷移 );
                //----------------
                #endregion

                #region " MAX用半径倍率 のアニメ構築 "
                //----------------
                // 初期値 1.0
                this._MAX用半径倍率?.Dispose();
                this._MAX用半径倍率 = new Variable( Global.Animation.Manager, initialValue: 1.0 );

                // 待つ
                using( var 遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 達成率.最初の待機時間sec ) )
                    this._ストーリーボード.AddTransition( this._MAX用半径倍率, 遷移 );

                // 0.0 へ
                using( var 遷移 = Global.Animation.TrasitionLibrary.AccelerateDecelerate( duration: 達成率.アニメ時間sec, finalValue: 0.0, accelerationRatio: 0.9, decelerationRatio: 0.1 ) )
                    this._ストーリーボード.AddTransition( this._MAX用半径倍率, 遷移 );
                //----------------
                #endregion

                // アニメーション開始
                this._ストーリーボード.Schedule( Global.Animation.Timer.Time );
            }

            public void アニメを完了する()
            {
                this._ストーリーボード?.Finish( 0.1 );
            }

            public void 進行描画する( DeviceContext dc, float left, float top )
            {
                if( this._MAXである )
                {
                    // MAX
                    double 回転による拡大率 = Math.Abs( Math.Cos( this._MAX用拡大角度rad.Value ) );    // (0) 1 → 0 → 1（π) → 0 → 1 (2π)
                    float 拡大率 = (float) ( 1.0 + 回転による拡大率 * this._MAX用半径倍率.Value );
                    float 左位置dpx = left + 124f + ( ( 1.0f - 拡大率 ) * this._MAX.サイズ.Width ) / 2.0f;
                    float 上位置dpx = top + 66f + ( ( 1.0f - 拡大率 ) * this._MAX.サイズ.Height ) / 2.0f;

                    this._MAX.描画する( 左位置dpx, 上位置dpx, 不透明度0to1: (float) this._不透明度.Value, X方向拡大率: 拡大率, Y方向拡大率: 拡大率 );
                }
                else
                {
                    this._数字画像.不透明度 = (float) this._不透明度.Value;

                    float 左位置dpx = left + (float) this._左位置dpx.Value;

                    // 整数部を描画する（'.'含む）
                    this._数字画像.描画する( 左位置dpx, top, this._達成率文字列_整数部, new Size2F( 1.4f, 1.6f ) );

                    // 小数部を描画する
                    this._数字画像.描画する( 左位置dpx + 246f, top + 50f, this._達成率文字列_小数部, new Size2F( 1.0f, 1.0f ) );
                }
            }



            // ローカル


            private readonly フォント画像 _数字画像;

            private readonly 画像 _MAX;

            private bool _MAXである;

            private string _達成率文字列_小数部 = "";

            private string _達成率文字列_整数部 = "";    // '.' 含む

            private Storyboard _ストーリーボード = null!;

            private Variable _左位置dpx = null!;

            private Variable _不透明度 = null!;

            private Variable _MAX用拡大角度rad = null!;

            private Variable _MAX用半径倍率 = null!;
        }
    }
}
