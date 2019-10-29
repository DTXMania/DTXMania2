using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;

namespace DTXMania2
{
    class フォント画像 : IImage, IDisposable
    {

        // プロパティ


        /// <summary>
        ///		それぞれの文字矩形の幅に加算する補正値。
        /// </summary>
        public float 文字幅補正dpx { get; set; } = 0f;

        /// <summary>
        ///		透明: 0 ～ 1 :不透明
        /// </summary>
        public float 不透明度 { get; set; } = 1f;



        // 生成と終了


        /// <summary>
        ///		コンストラクタ。
        ///		指定された画像ファイルと矩形リストyamlファイルを使って、フォント画像を生成する。
        /// </summary>
        public フォント画像( VariablePath 文字盤の画像ファイルパス, VariablePath 文字盤設定ファイルパス, float 文字幅補正dpx = 0f, float 不透明度 = 1f )
        {
            this._文字盤 = new 画像( 文字盤の画像ファイルパス );
            this._矩形リスト = new 矩形リスト( 文字盤設定ファイルパス );
            this.文字幅補正dpx = 文字幅補正dpx;
            this.不透明度 = 不透明度;
        }

        public virtual void Dispose()
        {
            this._文字盤.Dispose();
        }



        // 進行と描画


        /// <param name="基点のX位置">左揃えなら左端位置、右揃えなら右端位置のX座標。</param>
        /// <param name="右揃え">trueなら右揃え、falseなら左揃え。</param>
        public void 描画する( float 基点のX位置, float 上位置, string 表示文字列, Size2F? 拡大率 = null, bool 右揃え = false )
        {
            if( string.IsNullOrEmpty( 表示文字列 ) )
                return;

            拡大率 ??= new Size2F( 1, 1 );

            if( !this._有効文字の矩形と文字数を抽出し文字列全体のサイズを返す( 表示文字列, 拡大率.Value, out Size2F 文字列全体のサイズ, out int 有効文字数, out var 有効文字矩形リスト ) )
                return;


            if( 右揃え )
                基点のX位置 -= 文字列全体のサイズ.Width;

            for( int i = 0; i < 有効文字数; i++ )
            {
                var 文字矩形 = 有効文字矩形リスト.ElementAt( i );

                if( !文字矩形.HasValue )
                    continue;

                this._文字盤.描画する(
                    基点のX位置,
                    上位置 + ( 文字列全体のサイズ.Height - 文字矩形.Value.Height * 拡大率.Value.Height ),
                    this.不透明度,
                    拡大率.Value.Width,
                    拡大率.Value.Height,
                    文字矩形 );

                基点のX位置 += ( 文字矩形!.Value.Width * 拡大率.Value.Width + this.文字幅補正dpx );
            }


        }



        // ローカル


        private 画像 _文字盤;

        private 矩形リスト _矩形リスト;


        private bool _有効文字の矩形と文字数を抽出し文字列全体のサイズを返す( string 表示文字列, Size2F 拡大率, out Size2F 文字列全体のサイズ, out int 有効文字数, out IEnumerable<RectangleF?> 有効文字矩形リスト )
        {
            文字列全体のサイズ = Size2F.Empty;
            
            有効文字矩形リスト =
                from 文字 in 表示文字列
                where ( null != this._矩形リスト[ 文字.ToString() ] )
                select this._矩形リスト[ 文字.ToString() ];

            有効文字数 = 有効文字矩形リスト.Count();
            if( 0 == 有効文字数 )
                return false;

            foreach( var 文字矩形 in 有効文字矩形リスト )
            {
                文字列全体のサイズ.Width += 文字矩形!.Value.Width * 拡大率.Width + this.文字幅補正dpx;

                if( 文字列全体のサイズ.Height < 文字矩形!.Value.Height * 拡大率.Height )    // 文字列全体の高さは、最大の文字高に一致。
                    文字列全体のサイズ.Height = 文字矩形!.Value.Height * 拡大率.Height;
            }

            return true;
        }
    }
}
