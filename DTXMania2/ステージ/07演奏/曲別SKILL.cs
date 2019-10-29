using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;

namespace DTXMania2.演奏
{
    class 曲別SKILL : IDisposable
    {

        // 生成と終了


        public 曲別SKILL()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._数字画像 = new フォント画像D2D( @"$(Images)\ParameterFont_LargeBoldItalic.png", @"$(Images)\ParameterFont_LargeBoldItalic.yaml", 文字幅補正dpx: -6f );
            this._ロゴ画像 = new 画像D2D( @"$(Images)\SkillIcon.png" );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._ロゴ画像.Dispose();
            this._数字画像.Dispose();
        }



        // 進行と描画


        public void 進行描画する( DeviceContext dc, double? スキル値 )
        {
            if( スキル値 is null )
                return;

            var skill = (double) スキル値;
            var 描画領域 = new RectangleF( 108f, 780f, 275f, 98f );
            string スキル値文字列 = skill.ToString( "0.00" ).PadLeft( 6 ).Replace( ' ', 'o' );  // 右詰め、余白は'o'。

            // 曲別SKILLアイコンを描画する
            var 変換行列2D =
                Matrix3x2.Scaling( 0.375f, 0.5f ) *
                Matrix3x2.Translation( 描画領域.X, 描画領域.Y );

            this._ロゴ画像.描画する( dc, 変換行列2D );

            // 小数部を描画する
            this._数字画像.描画する( dc, 描画領域.X + 90f + 105f, 描画領域.Y + ( 描画領域.Height * 0.2f ), スキル値文字列[ 4.. ], new Size2F( 0.65f, 0.8f ) );

            // 整数部を描画する（'.'含む）
            this._数字画像.描画する( dc, 描画領域.X + 90f, 描画領域.Y, スキル値文字列[ 0..4 ], new Size2F( 0.65f, 1.0f ) );
        }



        // ローカル


        private readonly フォント画像D2D _数字画像;

        private readonly 画像D2D _ロゴ画像;
    }
}
