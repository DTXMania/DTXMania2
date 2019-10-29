using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;

namespace DTXMania2.演奏
{
    /// <summary>
    ///     譜面スクロール速度の遷移と表示を行う。
    /// </summary>
    /// <remarks>
    ///     譜面スクロール速度には、
    ///     　「ユーザ設定速度」
    ///     　「補間付き速度」
    ///     ２種類がある。
    ///     
    ///     本クラスが画面に表示する速度は常に「ユーザ設定速度」である。
    ///     これは外部から供給される値であり、本クラスでは参照のみ行う。
    ///     
    ///     「ユーザ設定速度」は通常 0.5 単位で増減するが、
    ///     「補間付き速度」はその増減が滑らかになるよう、一定時間をかけて補完する値である。
    ///     これは本クラスで提供する。
    ///     
    ///     譜面上を流れるチップの表示位置計算には、本クラスで定義する <see cref="補間付き速度"/> プロパティの値を使うこと。
    /// </remarks>
    class 譜面スクロール速度 : IDisposable
    {

        // プロパティ


        public double 補間付き速度 { get; protected set; }



        // 生成と終了


        public 譜面スクロール速度( double ユーザ設定速度 )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._文字画像 = new フォント画像( @"$(Images)\ParameterFont_Small.png", @"$(Images)\ParameterFont_Small.yaml", 文字幅補正dpx: -3f );
            
            this.補間付き速度 = ユーザ設定速度;
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._文字画像.Dispose();
        }



        // 進行と描画


        public void 進行する( double ユーザ設定速度 )
        {
            // 現在の 補間付き速度 が ユーザ設定速度 と異なっているなら、近づける。

            if( this.補間付き速度 < ユーザ設定速度 )
            {
                #region " (A) 速度が上がった "
                //----------------
                if( 0 > this._スクロール倍率追い付き用_最後の値 )
                {
                    this._スクロール倍率追い付き用カウンタ = new LoopCounter( 0, 1000, 10 );    // 0→100; 全部で10×1000 = 10000ms = 10sec あれば十分だろう
                    this._スクロール倍率追い付き用_最後の値 = 0;
                }
                else
                {
                    while( this._スクロール倍率追い付き用_最後の値 < this._スクロール倍率追い付き用カウンタ.現在値 )
                    {
                        this.補間付き速度 += 0.025;
                        this._スクロール倍率追い付き用_最後の値++;
                    }

                    this.補間付き速度 = Math.Min( this.補間付き速度, ユーザ設定速度 );
                }
                //----------------
                #endregion
            }
            else if( this.補間付き速度 > ユーザ設定速度 )
            {
                #region " (B) 速度が下がった "
                //----------------
                if( 0 > this._スクロール倍率追い付き用_最後の値 )
                {
                    this._スクロール倍率追い付き用カウンタ = new LoopCounter( 0, 1000, 10 );    // 0→100; 全部で10×1000 = 10000ms = 10sec あれば十分だろう
                    this._スクロール倍率追い付き用_最後の値 = 0;
                }
                else
                {
                    while( this._スクロール倍率追い付き用_最後の値 < this._スクロール倍率追い付き用カウンタ.現在値 )
                    {
                        this.補間付き速度 -= 0.025;
                        this._スクロール倍率追い付き用_最後の値++;
                    }

                    this.補間付き速度 = Math.Max( this.補間付き速度, ユーザ設定速度 );
                }
                //----------------
                #endregion
            }
            else
            {
                #region " (C) 速度は変わってない "
                //----------------
                this._スクロール倍率追い付き用_最後の値 = -1;
                //----------------
                #endregion
            }
        }

        public void 描画する( DeviceContext dc, double ユーザ設定速度 )
        {
            var 表示領域 = new RectangleF( 482, 985f, 48f, 24f );

            this._文字画像.描画する( 表示領域.X, 表示領域.Y, ユーザ設定速度.ToString( "0.0" ) ); // 表示は 補間付き速度 ではない
        }



        // ローカル


        private readonly フォント画像 _文字画像;

        private LoopCounter _スクロール倍率追い付き用カウンタ = null!;

        private int _スクロール倍率追い付き用_最後の値 = -1;
    }
}
