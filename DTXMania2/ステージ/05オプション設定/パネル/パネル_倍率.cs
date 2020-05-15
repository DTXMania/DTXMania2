using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania2.オプション設定
{
    /// <summary>
    ///		倍率専用の数値ボックス。
    /// </summary>
    class パネル_倍率 : パネル
    {

        // プロパティ


        public double 最小倍率 { get; } = 0.5;

        public double 最大倍率 { get; } = 8.0;

        public double 増減量 { get; } = 0.5;

        public double 現在値 { get; protected set; } = 0.0;



        // 生成と終了


        public パネル_倍率( string パネル名, double 初期値, double 最小倍率, double 最大倍率, double 増減量, Action<パネル>? 値の変更処理 = null )
            : base( パネル名, 値の変更処理 )
        {
            this.現在値 = 初期値;
            this.最小倍率 = 最小倍率;
            this.最大倍率 = 最大倍率;
            this.増減量 = 増減量;
            this._項目画像 = new 文字列画像D2D() {
                表示文字列 = "",
                フォントサイズpt = 34f,
                前景色 = Color4.White
            };
        }

        public override void Dispose()
        {
            this._項目画像.Dispose();

            base.Dispose(); // 忘れずに
        }



        // 入力


        public override void 左移動キーが入力された()
        {
            // 値を減らす。
            this.現在値 = Math.Max( this.現在値 - this.増減量, 最小倍率 );

            base.左移動キーが入力された();
        }

        public override void 右移動キーが入力された()
        {
            // 値を増やす。
            this.現在値 = Math.Min( this.現在値 + this.増減量, 最大倍率 );

            base.右移動キーが入力された();
        }

        public override void 確定キーが入力された()
        {
            // 値を増やす。
            this.現在値 += this.増減量;

            // 最大値を超えたら最小値に戻る。
            if( 最大倍率 < this.現在値 )
                this.現在値 = 最小倍率;

            base.確定キーが入力された();
        }



        // 進行と描画


        public override void 進行描画する( DeviceContext dc, float left, float top, bool 選択中 )
        {
            // (1) パネルの下地と名前を描画。

            base.進行描画する( dc, left, top, 選択中 );


            // (2) 値を描画。

            this._項目画像.表示文字列 = "×　" + this.現在値.ToString( "0.0#" );  // 小数第2位は、ゼロなら非表示。
            this._項目画像.ビットマップを生成または更新する( dc );      // このあと画像のサイズが必要になるので、先に生成/更新する。

            float 拡大率Y = (float) this._パネルの高さ割合.Value;
            float 項目の上下マージン = this.項目領域.Height * ( 1f - 拡大率Y ) / 2f;

            var 項目矩形 = new RectangleF(
                x: this.項目領域.X + left,
                y: this.項目領域.Y + top + 項目の上下マージン,
                width: this.項目領域.Width,
                height: this.項目領域.Height * 拡大率Y );

            float 拡大率X = Math.Min( 1f, ( 項目矩形.Width - 20f ) / this._項目画像.画像サイズdpx.Width );    // -20 は左右マージンの最低値[dpx]

            this._項目画像.描画する(
                dc,
                項目矩形.Left + ( 項目矩形.Width - this._項目画像.画像サイズdpx.Width * 拡大率X ) / 2f,
                項目矩形.Top + ( 項目矩形.Height - this._項目画像.画像サイズdpx.Height * 拡大率Y ) / 2f,
                X方向拡大率: 拡大率X,
                Y方向拡大率: 拡大率Y );
        }



        // ローカル


        private readonly 文字列画像D2D _項目画像;
    }
}
