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
    partial class 曲別SKILL
    {
        class アイコン : IDisposable
        {

            // プロパティ


            public bool アニメ完了 => ( null != this._ストーリーボード && this._ストーリーボード.Status == StoryboardStatus.Ready );



            // 生成と終了


            public アイコン()
            {
                this._アイコン画像 = new 画像( @"$(Images)\ResultStage\SkillIcon.png" );
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

                #region " 拡大角度rad "
                //----------------
                // 初期値 0.0
                this._拡大角度rad?.Dispose();
                this._拡大角度rad = new Variable( Global.Animation.Manager, initialValue: 0.0 );

                // 待つ
                using( var 遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 曲別SKILL.最初の待機時間sec ) )
                    this._ストーリーボード.AddTransition( this._拡大角度rad, 遷移 );

                // 2π へ
                using( var 遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 曲別SKILL.アニメ時間sec, finalValue: 2 * Math.PI ) )
                    this._ストーリーボード.AddTransition( this._拡大角度rad, 遷移 );
                //----------------
                #endregion

                #region " 半径倍率 "
                //----------------
                // 初期値 1.0
                this._半径倍率?.Dispose();
                this._半径倍率 = new Variable( Global.Animation.Manager, initialValue: 1.0 );

                // 待つ
                using( var 遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 曲別SKILL.最初の待機時間sec ) )
                    this._ストーリーボード.AddTransition( this._半径倍率, 遷移 );

                // 0.0 へ
                using( var 遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 曲別SKILL.アニメ時間sec, finalValue: 0.0 ) )
                    this._ストーリーボード.AddTransition( this._半径倍率, 遷移 );
                //----------------
                #endregion

                #region " 不透明度 "
                //----------------
                // 初期値 0.0
                this._不透明度?.Dispose();
                this._不透明度 = new Variable( Global.Animation.Manager, initialValue: 0.0 );

                // 待つ
                using( var 遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 曲別SKILL.最初の待機時間sec ) )
                    this._ストーリーボード.AddTransition( this._不透明度, 遷移 );

                // 1.0 へ
                using( var 遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 曲別SKILL.アニメ時間sec, finalValue: 1.0 ) )
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
                double 回転による拡大率 = Math.Abs( Math.Cos( this._拡大角度rad.Value ) );    // (0) 1 → 0 → 1（π) → 0 → 1 (2π)
                float 拡大率 = (float) ( 1.0 + 回転による拡大率 * this._半径倍率.Value );
                float 左位置dpx = left + ( ( 1.0f - 拡大率 ) * this._アイコン画像.サイズ.Width ) / 2.0f;
                float 上位置dpx = top + ( ( 1.0f - 拡大率 ) * this._アイコン画像.サイズ.Height ) / 2.0f;
                
                this._アイコン画像.描画する( 左位置dpx, 上位置dpx, 不透明度0to1: (float) this._不透明度.Value, X方向拡大率: 拡大率, Y方向拡大率: 拡大率 );
            }



            // ローカル


            private readonly 画像 _アイコン画像;

            private Storyboard _ストーリーボード = null!;

            private Variable _拡大角度rad = null!;

            private Variable _半径倍率 = null!;

            private Variable _不透明度 = null!;
        }
    }
}
