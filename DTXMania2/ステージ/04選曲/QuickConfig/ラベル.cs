using System;
using System.Collections.Generic;
using System.Text;
using SharpDX;
using SharpDX.Direct2D1;

namespace DTXMania2.選曲.QuickConfig
{
    class ラベル : IDisposable
    {

        // プロパティ


        public string 名前 { get; protected set; } = "";



        // 生成と終了


        public ラベル( string 名前 )
        {
            this.名前 = 名前;
            this._項目名画像 = new 文字列画像D2D() {
                表示文字列 = this.名前,
                フォントサイズpt = 34f,
                前景色 = Color4.White
            };
        }

        public virtual void Dispose()
        {
            this._項目名画像.Dispose();
        }



        // 進行と描画


        public virtual void 進行描画する( DeviceContext dc, float 左位置, float 上位置 )
        {
            this._項目名画像.描画する( dc, 左位置, 上位置 );
        }



        // ローカル


        protected readonly 文字列画像D2D _項目名画像;
    }
}
