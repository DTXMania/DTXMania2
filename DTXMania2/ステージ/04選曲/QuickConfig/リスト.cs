using System;
using System.Collections.Generic;
using System.Text;
using SharpDX;
using SharpDX.Direct2D1;

namespace DTXMania2.選曲.QuickConfig
{
    class リスト : ラベル, IDisposable
    {

        // プロパティ


        public int 現在の選択肢番号 { get; set; } = 0;

        public List<string> 選択肢リスト { get; protected set; } = new List<string>();



        // 生成と終了


        public リスト( string 名前, IEnumerable<string> 選択肢初期値リスト, int 初期選択肢番号 = 0, Action<リスト>? 値が変更された = null )
            : base( 名前 )
        {
            this.現在の選択肢番号 = 初期選択肢番号;
            if( null != 選択肢初期値リスト )
                this.選択肢リスト.AddRange( 選択肢初期値リスト );
            this._値が変更された = 値が変更された;

            this._選択肢文字列画像リスト = new Dictionary<string, 文字列画像D2D>();
            for( int i = 0; i < this.選択肢リスト.Count; i++ )
            {
                var image = new 文字列画像D2D() {
                    表示文字列 = this.選択肢リスト[ i ],
                    フォントサイズpt = 34f,
                    前景色 = Color.White,
                };

                this._選択肢文字列画像リスト.Add( this.選択肢リスト[ i ], image );
            }
        }

        public override void Dispose()
        {
            foreach( var kvp in this._選択肢文字列画像リスト )
                kvp.Value.Dispose();

            base.Dispose();
        }



        // 進行と描画


        public virtual void 前を選択する( bool Loop = true )
        {
            if( Loop )
            {
                // 前がなければ末尾に戻る。
                this.現在の選択肢番号 = ( this.現在の選択肢番号 - 1 + this.選択肢リスト.Count ) % this.選択肢リスト.Count;
                this._値が変更された?.Invoke( this );
            }
            else
            {
                if( this.現在の選択肢番号 > 0 )
                {
                    this.現在の選択肢番号--;
                    this._値が変更された?.Invoke( this );
                }
            }
        }

        public virtual void 次を選択する( bool Loop = true )
        {
            if( Loop )
            {
                // 次がなければ先頭に戻る。
                this.現在の選択肢番号 = ( this.現在の選択肢番号 + 1 ) % this.選択肢リスト.Count;
                this._値が変更された?.Invoke( this );
            }
            else
            {
                if( this.現在の選択肢番号 < this.選択肢リスト.Count - 1 )
                {
                    this.現在の選択肢番号++;
                    this._値が変更された?.Invoke( this );
                }
            }
        }

        public override void 進行描画する( DeviceContext dc, float 左位置, float 上位置 )
        {
            // ラベル

            base.進行描画する( dc, 左位置, 上位置 );

            // 項目

            int 項目番号 = this.現在の選択肢番号;
            var 項目名 = this.選択肢リスト[ 項目番号 ];
            var 項目画像 = this._選択肢文字列画像リスト[ 項目名 ];

            項目画像.描画する( dc, 左位置 + 400f, 上位置 );
        }



        // ローカル


        private readonly Dictionary<string, 文字列画像D2D> _選択肢文字列画像リスト;

        private Action<リスト>? _値が変更された;
    }
}
