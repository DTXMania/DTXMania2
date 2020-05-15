using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania2.演奏
{
    class 達成率表示 : IDisposable
    {

        // 生成と終了


        public 達成率表示()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._数字画像 = new フォント画像D2D( @"$(Images)\ParameterFont_Large.png", @"$(Images)\ParameterFont_Large.yaml", 文字幅補正dpx: 0f );
            this._達成率ロゴ画像 = new 画像D2D( @"$(Images)\AchivementLogo.png" );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._達成率ロゴ画像.Dispose();
            this._数字画像.Dispose();
        }



        // 進行と描画


        public void 描画する( DeviceContext dc, float 達成率0to100 )
        {
            var 描画領域 = new RectangleF( 200f, 650f, 165f, 80f );

            string 達成率文字列 = MathUtil.NearEqual( 100f, 達成率0to100 ) ?
                "100.00%" :
                ' ' + ( 達成率0to100.ToString( "0.00" ) + '%' ).PadLeft( 6 ).Replace( ' ', 'o' );  // 右詰め、余白は'o'。例:"99.00%", "o8.12%", "o0.00%"


            // 達成率ロゴを描画する

            var 変換行列2D =
                Matrix3x2.Scaling( 0.4f, 0.5f ) *
                Matrix3x2.Translation( 描画領域.X - 46f, 描画領域.Y - 60f );

            this._達成率ロゴ画像.描画する( dc, 変換行列2D );


            // 小数部を描画する（'%'含む）

            this._数字画像.描画する( dc, 描画領域.X + 86f, 描画領域.Y + ( 描画領域.Height * 0.2f ), 達成率文字列[ 4.. ], new Size2F( 0.5f, 0.8f ) );


            // 整数部を描画する（'.'含む）

            this._数字画像.描画する( dc, 描画領域.X, 描画領域.Y, 達成率文字列[ 0..4 ], new Size2F( 0.5f, 1.0f ) );
        }



        // ローカル


        private readonly フォント画像D2D _数字画像;

        private readonly 画像D2D _達成率ロゴ画像;
    }
}
