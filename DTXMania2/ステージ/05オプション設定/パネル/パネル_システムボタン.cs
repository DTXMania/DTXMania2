using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;

namespace DTXMania2.オプション設定
{
    /// <summary>
    ///		システム制御用のボタン。「設定完了」「戻る」など。
    ///		描画が普通のパネルと違う。
    /// </summary>
    class パネル_システムボタン : パネル
    {

        // 生成と終了


        public パネル_システムボタン( string パネル名, Action<パネル>? 値の変更処理 = null )
            : base( パネル名, 値の変更処理 )
        {
            this._パネル名画像.前景色 = Color.Black;
        }

        public override void Dispose()
        {
            base.Dispose(); // 忘れずに
        }



        // 進行と描画


        public override void 進行描画する( DeviceContext dc, float left, float top, bool 選択中 )
        {
            float 拡大率Y = (float) this._パネルの高さ割合.Value;

            float テキストの上下マージン = 72f * ( 1f - 拡大率Y ) / 2f;
            var テキスト矩形 = new RectangleF( left + 32f, top + 12f + テキストの上下マージン, 294f, 72f * 拡大率Y );


            // (1) パネルの下地部分の描画。

            Global.D2DBatchDraw( dc, () => {
                using var テキスト背景色 = new SolidColorBrush( dc, Color.LightGray );
                dc.FillRectangle( テキスト矩形, テキスト背景色 );
            } );


            // (2) パネル名の描画。

            float 拡大率X = Math.Min( 1f, ( テキスト矩形.Width - 20f ) / this._パネル名画像.画像サイズdpx.Width );    // -20 は左右マージンの最低値[dpx]

            this._パネル名画像.描画する(
                dc,
                テキスト矩形.Left + ( テキスト矩形.Width - this._パネル名画像.画像サイズdpx.Width * 拡大率X ) / 2f,
                テキスト矩形.Top + ( テキスト矩形.Height - this._パネル名画像.画像サイズdpx.Height * 拡大率Y ) / 2f,
                X方向拡大率: 拡大率X,
                Y方向拡大率: 拡大率Y );
        }
    }
}
