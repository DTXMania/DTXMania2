using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using DTXMania2.曲;

namespace DTXMania2.曲読み込み
{
    class 難易度 : IDisposable
    {

        // 生成と終了


        public 難易度()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._数字画像 = new フォント画像D2D( @"$(Images)\ParameterFont_Large.png", @"$(Images)\ParameterFont_Large.yaml", 文字幅補正dpx: 0f );
            this._見出し用TextFormat = new TextFormat( Global.DWriteFactory, "Century Gothic", 50f );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._見出し用TextFormat.Dispose();
            this._数字画像.Dispose();
        }



        // 進行と描画


        public void 描画する( DeviceContext dc )
        {
            var 見出し描画領域 = new RectangleF( 783f, 117f, 414f, 63f );
            var 数値描画領域 = new RectangleF( 783f, 180f, 414f, 213f );


            // 難易度のラベルと値を描画する。

            Global.D2DBatchDraw( dc, () => {

                using var 見出し背景ブラシ = new SolidColorBrush( dc, Song.難易度色リスト[ Global.App.曲ツリーリスト.SelectedItem!.フォーカス難易度レベル ] );
                using var 黒ブラシ = new SolidColorBrush( dc, Color4.Black );
                using var 黒透過ブラシ = new SolidColorBrush( dc, new Color4( Color3.Black, 0.5f ) );
                using var 白ブラシ = new SolidColorBrush( dc, Color4.White );

                // 背景領域を塗りつぶす。
                dc.FillRectangle( 見出し描画領域, 見出し背景ブラシ );
                dc.FillRectangle( 数値描画領域, 黒ブラシ );

                // 見出し文字列を描画する。
                this._見出し用TextFormat.TextAlignment = TextAlignment.Trailing;
                var 見出し文字領域 = 見出し描画領域;
                見出し文字領域.Width -= 8f;    // 右マージン
                dc.DrawText( Global.App.演奏譜面.難易度ラベル, this._見出し用TextFormat, 見出し文字領域, 白ブラシ );

                // 小数部を描画する。
                var 数値文字列 = Global.App.演奏譜面.譜面.Level.ToString( "0.00" ).PadLeft( 1 );
                this._数字画像.描画する( dc, 数値描画領域.X + 175f, 数値描画領域.Y, 数値文字列.Substring( 2 ), new Size2F( 2.2f, 2.2f ) );

                // 整数部と小数点を描画する。
                this._数字画像.描画する( dc, 数値描画領域.X + 15f, 数値描画領域.Y, 数値文字列.Substring( 0, 2 ), new Size2F( 2.2f, 2.2f ) );

            } );
        }



        // ローカル


        private readonly フォント画像D2D _数字画像;

        private readonly TextFormat _見出し用TextFormat;
    }
}
