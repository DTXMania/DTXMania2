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
    ///		任意個の文字列から１つを選択できるパネル項目（コンボボックス）。
    ///		コンストラクタから活性化までの間に、<see cref="選択肢リスト"/> に文字列を設定すること。
    /// </summary>
    class パネル_文字列リスト : パネル
    {

        // プロパティ


        public int 現在選択されている選択肢の番号 { get; set; } = 0;

        public List<(string 文字列, Color4 色)> 選択肢リスト { get; protected set; } = new List<(string, Color4)>();



        // 生成と終了


        public パネル_文字列リスト( string パネル名, IEnumerable<string> 選択肢初期値リスト, int 初期選択肢番号 = 0, Action<パネル>? 値の変更処理 = null )
            : base( パネル名, 値の変更処理 )
        {
            var list = new List<(string 文字列, Color4 色)>();
            foreach( var item in 選択肢初期値リスト )
                list.Add( (item, Color4.White) );   // 既定の色は白

            this._初期化( list, 初期選択肢番号 );
        }

        // 色指定あり
        public パネル_文字列リスト( string パネル名, IEnumerable<(string 文字列, Color4 色)> 選択肢初期値リスト, int 初期選択肢番号 = 0, Action<パネル>? 値の変更処理 = null )
            : base( パネル名, 値の変更処理 )
        {
            this._初期化( 選択肢初期値リスト, 初期選択肢番号 );
        }

        private void _初期化( IEnumerable<(string 文字列, Color4 色)> 選択肢初期値リスト, int 初期選択肢番号 = 0 )
        {
            this.現在選択されている選択肢の番号 = 初期選択肢番号;

            // 初期値があるなら設定する。
            if( null != 選択肢初期値リスト )
                this.選択肢リスト.AddRange( 選択肢初期値リスト );

            this._選択肢文字列画像リスト = new Dictionary<string, 文字列画像D2D>();

            for( int i = 0; i < this.選択肢リスト.Count; i++ )
            {
                var image = new 文字列画像D2D() {
                    表示文字列 = this.選択肢リスト[ i ].文字列,
                    フォントサイズpt = 34f,
                    前景色 = this.選択肢リスト[ i ].色,
                };

                this._選択肢文字列画像リスト.Add( this.選択肢リスト[ i ].文字列, image );
            }
        }

        public override void Dispose()
        {
            foreach( var kvp in this._選択肢文字列画像リスト )
                kvp.Value.Dispose();

            base.Dispose(); // 忘れずに
        }

        public override string ToString()
            => $"{this.パネル名}, 選択肢: [{string.Join( ",", this.選択肢リスト )}]";



        // 入力


        public override void 左移動キーが入力された()
        {
            this.現在選択されている選択肢の番号 = ( this.現在選択されている選択肢の番号 - 1 + this.選択肢リスト.Count ) % this.選択肢リスト.Count;

            base.左移動キーが入力された();
        }

        public override void 右移動キーが入力された()
        {
            this.現在選択されている選択肢の番号 = ( this.現在選択されている選択肢の番号 + 1 ) % this.選択肢リスト.Count;

            base.右移動キーが入力された();
        }

        public override void 確定キーが入力された()
            => this.右移動キーが入力された();



        // 進行と描画


        public override void 進行描画する( DeviceContext dc, float left, float top, bool 選択中 )
        {
            // (1) パネルの下地と名前を描画。

            base.進行描画する( dc, left, top, 選択中 );


            // (2) 選択肢文字列画像の描画。

            float 拡大率Y = (float) this._パネルの高さ割合.Value;
            float 項目の上下マージン = this.項目領域.Height * ( 1f - 拡大率Y ) / 2f;

            var 項目矩形 = new RectangleF(
                x: this.項目領域.X + left,
                y: this.項目領域.Y + top + 項目の上下マージン,
                width: this.項目領域.Width,
                height: this.項目領域.Height * 拡大率Y );

            var 項目画像 = this._選択肢文字列画像リスト[ this.選択肢リスト[ this.現在選択されている選択肢の番号 ].文字列 ];

            項目画像.ビットマップを生成または更新する( dc );    // 先に画像を更新する。↓で画像サイズを取得するため。
            float 拡大率X = Math.Min( 1f, ( 項目矩形.Width - 20f ) / 項目画像.画像サイズdpx.Width );    // -20 は左右マージンの最低値[dpx]

            項目画像.描画する(
                dc,
                項目矩形.Left + ( 項目矩形.Width - 項目画像.画像サイズdpx.Width * 拡大率X ) / 2f,
                項目矩形.Top + ( 項目矩形.Height - 項目画像.画像サイズdpx.Height * 拡大率Y ) / 2f,
                X方向拡大率: 拡大率X,
                Y方向拡大率: 拡大率Y );
        }



        // ローカル


        // 各文字列は画像で保持。
        private Dictionary<string, 文字列画像D2D> _選択肢文字列画像リスト = null!;
    }
}
