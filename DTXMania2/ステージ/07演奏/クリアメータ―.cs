using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania2.演奏
{
    /// <summary>
    ///     1曲を曲の長さによらず64等分し、それぞれの区間での成績を大雑把に示すメーター。
    ///     区間内で Ok, Miss 判定を出さなかったら黄色、出したら青色で示される。
    ///     この64等分された配列データをカウントマップと称する。
    /// </summary>
    class クリアメーター : IDisposable
    {
        /// <summary>
        ///		カウント値の配列。
        /// </summary>
        /// <remarks>
        ///		インデックスが小さいほど曲の前方に位置する。
        ///		カウント値の値域は 0～12。今のところは 0 で非表示、1 で水色、2～12で黄色表示。
        /// </remarks>
        public int[] カウントマップ { get; }

        /// <summary>
        ///     カウント値の配列の最大要素数（定数）。
        /// </summary>
        /// <remarks>
        ///		全曲の最大カウントを 768 とするので、
        ///		カウントマップリストの要素数は 768÷12 = 64 個が最大となる。
        /// </remarks>
        public const int カウントマップの最大要素数 = 768 / 12;



        // 生成と終了


        public クリアメーター()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this.カウントマップ = new int[ カウントマップの最大要素数 ];
            for( int i = 0; i < this.カウントマップ.Length; i++ )
                this.カウントマップ[ i ] = 0;

            this._前回の設定位置 = 0f;
            this._前回設定したときの成績 = new Dictionary<判定種別, int>();
            foreach( 判定種別? judge in Enum.GetValues( typeof( 判定種別 ) ) )
            {
                if( judge.HasValue )
                    this._前回設定したときの成績.Add( judge.Value, 0 );
            }
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );
        }



        // 設定登録


        /// <summary>
        ///		初期化。
        /// </summary>
        public void 最高成績のカウントマップを登録する( int[] カウントマップ )
        {
            Debug.Assert( カウントマップの最大要素数 == カウントマップ.Length, "カウントマップの要素数が不正です。" );

            this._最高成績のカウントマップ = new int[ カウントマップ.Length ];
            カウントマップ.CopyTo( this._最高成績のカウントマップ, 0 );    // コピー
        }

        /// <summary>
        ///		指定された位置における現在の成績から、対応するカウント値を算出し、反映する。
        /// </summary>
        /// <param name="現在位置">現在の位置を 開始点:0～1:終了点 で示す。</param>
        /// <param name="判定toヒット数">現在の位置における成績。</param>
        /// <returns>現在のカウントマップを文字列化したものを返す。</returns>
        public string カウント値を設定する( float 現在位置, IReadOnlyDictionary<判定種別, int> 判定toヒット数 )
        {
            // 判定種別ごとに、前回からの成績の増加分を得る。

            var 増加値 = new Dictionary<判定種別, int>();

            foreach( 判定種別? judge in Enum.GetValues( typeof( 判定種別 ) ) )
            {
                if( judge.HasValue )
                    増加値.Add( judge.Value, 判定toヒット数[ judge.Value ] - this._前回設定したときの成績[ judge.Value ] );
            }


            // カウント値を算出する。

            int カウント値 = ( 0 < 増加値[ 判定種別.OK ] || 0 < 増加値[ 判定種別.MISS ] ) ? 1 : 12;    // 今のところ 1 or 12 の二段階のみ


            // 前回の設定位置 から 現在位置 までの期間に対応するすべてのカウント値に反映する。

            int 前回の位置 = (int) ( this._前回の設定位置 * カウントマップの最大要素数 );
            int 今回の位置 = (int) ( 現在位置 * カウントマップの最大要素数 );

            if( 1.0f > 現在位置 )
            {
                for( int i = 前回の位置; i <= 今回の位置; i++ )
                {
                    // 同一区間では、成績の悪いほう（カウント値の小さいほう）を優先する。
                    this.カウントマップ[ i ] = ( 0 < this.カウントマップ[ i ] ) ? Math.Min( カウント値, this.カウントマップ[ i ] ) : カウント値;
                }
            }


            // 位置と成績を保存。

            this._前回の設定位置 = 現在位置;

            foreach( 判定種別? judge in Enum.GetValues( typeof( 判定種別 ) ) )
            {
                if( judge.HasValue )
                    this._前回設定したときの成績[ judge.Value ] = 判定toヒット数[ judge.Value ];
            }


            // 現在のカウントマップを生成して返す。

            var sb = new StringBuilder( カウントマップ.Length );
            for( int i = 0; i < this.カウントマップ.Length; i++ )
                sb.Append( this._カウントマップ文字列[ this.カウントマップ[ i ] ] );

            return sb.ToString();
        }



        // 進行と描画


        public void 進行描画する( DeviceContext dc )
        {
            D2DBatch.Draw( dc, () => {

                using var 水色ブラシ = new SolidColorBrush( dc, new Color4( 0xffdd8e69 ) );
                using var 黄色ブラシ = new SolidColorBrush( dc, new Color4( 0xff17fffe ) );

                const float 単位幅 = 12f;
                var 今回のライン全体の矩形 = new RectangleF( 1357f, 108f, 10f, 768f );
                var 過去最高のライン全体の矩形 = new RectangleF( 1371f, 108f, 6f, 768f );


                // (1) 今回のクリアメータ―を描画する。

                for( int i = 0; i < this.カウントマップ.Length; i++ )
                {
                    if( 0 == this.カウントマップ[ i ] )
                        continue;

                    dc.FillRectangle(
                        new RectangleF( 今回のライン全体の矩形.Left, 今回のライン全体の矩形.Bottom - 単位幅 * ( i + 1 ), 今回のライン全体の矩形.Width, 単位幅 ),
                        ( 2 <= this.カウントマップ[ i ] ) ? 黄色ブラシ : 水色ブラシ );
                }


                // (2) 過去の最高成績のクリアメータ―を描画する。

                if( null != this._最高成績のカウントマップ )
                {
                    for( int i = 0; i < this._最高成績のカウントマップ.Length; i++ )
                    {
                        if( 0 == this._最高成績のカウントマップ[ i ] )
                            continue;

                        dc.FillRectangle(
                            new RectangleF( 過去最高のライン全体の矩形.Left, 過去最高のライン全体の矩形.Bottom - 単位幅 * ( i + 1 ), 過去最高のライン全体の矩形.Width, 単位幅 ),
                            ( 2 <= this._最高成績のカウントマップ[ i ] ) ? 黄色ブラシ : 水色ブラシ );
                    }
                }

            } );
        }



        // ローカル


        private int[]? _最高成績のカウントマップ = null;

        private float _前回の設定位置 = 0f;

        private readonly Dictionary<判定種別, int> _前回設定したときの成績;

        private readonly char[] _カウントマップ文字列 = new char[] { '0','1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C' };
    }
}
