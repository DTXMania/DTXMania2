using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Animation;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania2.オプション設定
{
    /// <summary>
    ///		すべてのパネルのベースとなるクラス。
    ///		名前だけのパネルとしても使う。
    /// </summary>
    class パネル : IDisposable
    {

        // プロパティ


        public string パネル名 { get; protected set; } = "";

        /// <summary>
        ///		パネル全体のサイズ。static。
        /// </summary>
        public static Size2F サイズ => new Size2F( 642f, 96f );

        public class ヘッダ色種別
        {
            public static readonly Color4 青 = new Color4( 0xff725031 );   // ABGR
            public static readonly Color4 赤 = new Color4( 0xff315072 );
        }

        public Color4 ヘッダ色 { get; set; } = ヘッダ色種別.青;



        // 生成と終了


        public パネル( string パネル名, Action<パネル>? 値の変更処理 = null, Color4? ヘッダ色 = null )
        {
            this.パネル名 = パネル名;
            this.ヘッダ色 = ( ヘッダ色.HasValue ) ? ヘッダ色.Value : ヘッダ色種別.青;  // 既定値は青
            this._値の変更処理 = 値の変更処理;
            this._パネル名画像 = new 文字列画像D2D() {
                表示文字列 = this.パネル名,
                フォントサイズpt = 34f,
                前景色 = Color4.White
            };
            this._パネルの高さ割合 = new Variable( Global.Animation.Manager, initialValue: 1.0 );
            this._パネルのストーリーボード = null;
        }

        // ※派生クラスから呼び出すのを忘れないこと。
        public virtual void Dispose()
        {
            this._パネルのストーリーボード?.Dispose();
            this._パネルの高さ割合.Dispose();
            this._パネル名画像.Dispose();
        }

        public override string ToString() => $"{this.パネル名}";



        // フェードイン・アウト


        public void フェードインを開始する( double 遅延sec, double 速度倍率 = 1.0 )
        {
            double 秒( double v ) => ( v / 速度倍率 );

            var animation = Global.Animation;

            this._パネルの高さ割合.Dispose();
            this._パネルの高さ割合 = new Variable( animation.Manager, initialValue: 1.0 );

            this._パネルのストーリーボード?.Dispose();
            this._パネルのストーリーボード = new Storyboard( animation.Manager );

            using( var 遅延遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 遅延sec ) ) )
            using( var 縮む遷移 = animation.TrasitionLibrary.Linear( duration: 秒( 0.1 ), finalValue: 0.0 ) )
            using( var 膨らむ遷移 = animation.TrasitionLibrary.Linear( duration: 秒( 0.1 ), finalValue: 1.0 ) )
            {
                this._パネルのストーリーボード.AddTransition( this._パネルの高さ割合, 遅延遷移 );
                this._パネルのストーリーボード.AddTransition( this._パネルの高さ割合, 縮む遷移 );
                this._パネルのストーリーボード.AddTransition( this._パネルの高さ割合, 膨らむ遷移 );
            }
            this._パネルのストーリーボード.Schedule( animation.Timer.Time );
        }

        public void フェードアウトを開始する( double 遅延sec, double 速度倍率 = 1.0 )
        {
            double 秒( double v ) => ( v / 速度倍率 );

            var animation = Global.Animation;

            if( null == this._パネルの高さ割合 )    // 未生成のときだけ生成。生成済みなら、その現状を引き継ぐ。
                this._パネルの高さ割合 = new Variable( animation.Manager, initialValue: 1.0 );

            this._パネルのストーリーボード?.Dispose();
            this._パネルのストーリーボード = new Storyboard( animation.Manager );

            using( var 遅延遷移 = animation.TrasitionLibrary.Constant( duration: 秒( 遅延sec ) ) )
            using( var 縮む遷移 = animation.TrasitionLibrary.Linear( duration: 秒( 0.1 ), finalValue: 0.0 ) )
            {
                this._パネルのストーリーボード.AddTransition( this._パネルの高さ割合, 遅延遷移 );
                this._パネルのストーリーボード.AddTransition( this._パネルの高さ割合, 縮む遷移 );
            }
            this._パネルのストーリーボード.Schedule( animation.Timer.Time );
        }



        // 進行と描画


        public virtual void 進行描画する( DeviceContext dc, float left, float top, bool 選択中 )
        {
            float 拡大率Y = (float) this._パネルの高さ割合.Value;
            float パネルとヘッダの上下マージン = サイズ.Height * ( 1f - 拡大率Y ) / 2f;
            float テキストの上下マージン = 76f * ( 1f - 拡大率Y ) / 2f;
            var パネル矩形 = new RectangleF( left, top + パネルとヘッダの上下マージン, サイズ.Width, サイズ.Height * 拡大率Y );
            var ヘッダ矩形 = new RectangleF( left, top + パネルとヘッダの上下マージン, 40f, サイズ.Height * 拡大率Y );
            var テキスト矩形 = new RectangleF( left + 20f, top + 10f + テキストの上下マージン, 280f, 76f * 拡大率Y );

            if( 選択中 )
            {
                // 選択されているパネルは、パネル矩形を左右にちょっと大きくする。
                パネル矩形.Left -= 38f;
                パネル矩形.Width += 38f * 2f;
            }


            // (1) パネルの下地部分の描画。

            D2DBatch.Draw( dc, () => {

                using( var パネル背景色 = new SolidColorBrush( dc, new Color4( Color3.Black, 0.5f ) ) )
                using( var ヘッダ背景色 = new SolidColorBrush( dc, this.ヘッダ色 ) )
                using( var テキスト背景色 = new SolidColorBrush( dc, Color4.Black ) )
                {
                    dc.FillRectangle( パネル矩形, パネル背景色 );
                    dc.FillRectangle( ヘッダ矩形, ヘッダ背景色 );
                    dc.FillRectangle( テキスト矩形, テキスト背景色 );
                }

            } );


            // (2) パネル名の描画。

            this._パネル名画像.ビットマップを生成または更新する( dc );    // 先に画像を更新する。↓で画像サイズを取得するため。
            float 拡大率X = Math.Min( 1f, ( テキスト矩形.Width - 20f ) / this._パネル名画像.画像サイズdpx.Width );    // -20 は左右マージンの最低値[dpx]

            this._パネル名画像.描画する(
                dc,
                テキスト矩形.Left + ( テキスト矩形.Width - this._パネル名画像.画像サイズdpx.Width * 拡大率X ) / 2f,
                テキスト矩形.Top + ( テキスト矩形.Height - this._パネル名画像.画像サイズdpx.Height * 拡大率Y ) / 2f,
                X方向拡大率: 拡大率X,
                Y方向拡大率: 拡大率Y );
        }



        // 入力


        public virtual void 確定キーが入力された()
        {
            // 必要あれば、派生クラスで実装すること。

            this._値の変更処理?.Invoke( this );
        }

        public virtual void 左移動キーが入力された()
        {
            // 必要あれば、派生クラスで実装すること。

            this._値の変更処理?.Invoke( this );
        }

        public virtual void 右移動キーが入力された()
        {
            // 必要あれば、派生クラスで実装すること。

            this._値の変更処理?.Invoke( this );
        }



        // その他


        /// <summary>
        ///     子孫を直列に列挙する。
        /// </summary>
        public IEnumerable<パネル> Traverse()
        {
            // (1) 自分
            yield return this;

            // (2) 子
            if( this is パネル_フォルダ folder )
            {
                foreach( var child in folder.子パネルリスト )
                    foreach( var coc in child.Traverse() )
                        yield return coc;
            }
        }



        // ローカル


        // パネル名は画像で保持。
        protected readonly 文字列画像D2D _パネル名画像;

        protected Action<パネル>? _値の変更処理 = null;

        /// <summary>
        ///		項目部分のサイズ。
        ///		left と top は、パネルほ left,top からの相対値。
        /// </summary>
        protected RectangleF 項目領域 = new RectangleF( +322f, +0f, 342f, サイズ.Height );

        /// <summary>
        ///		0.0:ゼロ ～ 1.0:原寸
        /// </summary>
        protected Variable _パネルの高さ割合;

        /// <summary>
        ///     フェードイン・アウトアニメーション用
        /// </summary>
        protected Storyboard? _パネルのストーリーボード = null;
    }
}
