using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.WIC;

namespace FDK
{
    /// <summary>
    ///     文字盤（１枚のビットマップ）の一部の矩形を文字として、文字列を表示する。
    /// </summary>
    public class フォント画像D2D : IImage, IDisposable
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


        public フォント画像D2D( ImagingFactory2 imagingFactory2, DeviceContext d2dDeviceContext, VariablePath 文字盤の画像ファイルパス, VariablePath 文字盤設定ファイルパス, float 文字幅補正dpx = 0f, float 不透明度 = 1f )
        {
            this.文字幅補正dpx = 文字幅補正dpx;
            this.不透明度 = 不透明度;

            this._文字盤 = new 画像D2D( imagingFactory2, d2dDeviceContext, 文字盤の画像ファイルパス );
            this._矩形リスト = new 矩形リスト( 文字盤設定ファイルパス );
        }

        public virtual void Dispose()
        {
            this._文字盤.Dispose();
        }



        // 進行と描画


        /// <param name="基点のX位置">左揃えなら左端位置、右揃えなら右端位置のX座標。</param>
        /// <param name="右揃え">trueなら右揃え、falseなら左揃え。</param>
        public void 描画する( DeviceContext dc, float 基点のX位置, float 上位置, string 表示文字列, Size2F? 拡大率 = null, bool 右揃え = false )
        {
            if( string.IsNullOrEmpty( 表示文字列 ) )
                return;

            拡大率 ??= new Size2F( 1, 1 );


            // 有効文字（矩形リストに登録されている文字）の矩形、文字数を抽出し、文字列全体のサイズを計算する。

            var 文字列全体のサイズ = Size2F.Empty;
            var 有効文字矩形リスト =
                from ch in 表示文字列
                where ( this._矩形リスト.文字列to矩形.ContainsKey( ch.ToString() ) )
                select ( this._矩形リスト[ ch.ToString() ] );

            int 有効文字数 = 有効文字矩形リスト.Count();
            if( 0 == 有効文字数 )
                return;

            foreach( var 文字矩形 in 有効文字矩形リスト )
            {
                文字列全体のサイズ.Width += ( 文字矩形!.Value.Width * 拡大率.Value.Width + this.文字幅補正dpx );

                if( 文字列全体のサイズ.Height < 文字矩形!.Value.Height * 拡大率.Value.Height )
                    文字列全体のサイズ.Height = 文字矩形!.Value.Height * 拡大率.Value.Height;  // 文字列全体の高さは、最大の文字高に一致。
            }


            // 描画する。

            if( 右揃え )
                基点のX位置 -= 文字列全体のサイズ.Width;

            for( int i = 0; i < 有効文字数; i++ )
            {
                var 文字矩形 = 有効文字矩形リスト.ElementAt( i );

                this._文字盤.描画する(
                    dc,
                    基点のX位置,
                    上位置 + ( 文字列全体のサイズ.Height - 文字矩形!.Value.Height * 拡大率.Value.Height ),
                    転送元矩形: 文字矩形,
                    X方向拡大率: 拡大率.Value.Width,
                    Y方向拡大率: 拡大率.Value.Height,
                    不透明度0to1: this.不透明度 );

                基点のX位置 += ( 文字矩形!.Value.Width * 拡大率.Value.Width + this.文字幅補正dpx );
            }
        }



        // ローカル


        private 画像D2D _文字盤;

        private 矩形リスト _矩形リスト;
    }
}
