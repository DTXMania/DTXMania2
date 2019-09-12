using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using FDK;

namespace DTXMania.選曲
{
    class 難易度と成績 : IDisposable
    {
        // 外部接続アクション
        public Func<青い線> 青い線を取得する = null;



        // 生成と終了


        public 難易度と成績()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this._数字画像 = new 画像フォント( @"$(System)images\パラメータ文字_大.png", @"$(System)images\パラメータ文字_大.yaml", 文字幅補正dpx: 0f );
                this._見出し用TextFormat = new TextFormat( DXResources.Instance.DWriteFactory, "Century Gothic", 16f );

                var dc = DXResources.Instance.既定のD2D1DeviceContext;

                this._黒ブラシ = new SolidColorBrush( dc, Color4.Black );
                this._黒透過ブラシ = new SolidColorBrush( dc, new Color4( Color3.Black, 0.5f ) );
                this._白ブラシ = new SolidColorBrush( dc, Color4.White );
                this._ULTIMATE色ブラシ = new SolidColorBrush( dc, Node.LevelColor[ 4 ] );
                this._MASTER色ブラシ = new SolidColorBrush( dc, Node.LevelColor[ 3 ] );
                this._EXTREME色ブラシ = new SolidColorBrush( dc, Node.LevelColor[ 2 ] );
                this._ADVANCED色ブラシ = new SolidColorBrush( dc, Node.LevelColor[ 1 ] );
                this._BASIC色ブラシ = new SolidColorBrush( dc, Node.LevelColor[ 0 ] );
            }
        }

        public virtual void Dispose()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this._黒ブラシ?.Dispose();
                this._黒透過ブラシ?.Dispose();
                this._白ブラシ?.Dispose();
                this._ULTIMATE色ブラシ?.Dispose();
                this._MASTER色ブラシ?.Dispose();
                this._EXTREME色ブラシ?.Dispose();
                this._ADVANCED色ブラシ?.Dispose();
                this._BASIC色ブラシ?.Dispose();

                this._見出し用TextFormat?.Dispose();
                this._数字画像?.Dispose();
            }
        }



        // 進行と描画


        /// <param name="選択している難易度">
        ///		0:BASIC～4:ULTIMATE
        ///	</param>
        public void 描画する( DeviceContext dc, int 選択している難易度 )
        {
            if( App進行描画.曲ツリー.フォーカスノード != this._現在表示しているノード )
            {
                #region " フォーカスノードが変更されたので情報を更新する。"
                //----------------
                // フォーカス曲ノードではない → このクラスではMusicNode以外も表示できる　というかSetNodeな。
                this._現在表示しているノード = App進行描画.曲ツリー.フォーカスノード;
                //----------------
                #endregion
            }


            var node = this._現在表示しているノード;

            bool 表示可能ノードである = ( node is MusicNode ) || ( node is SetNode );


            DXResources.Instance.D2DBatchDraw( dc, () => {

                dc.PrimitiveBlend = PrimitiveBlend.SourceOver;

                #region " 難易度パネルを描画する。"
                //----------------
                var 領域dpx = new RectangleF( 642f, 529f, 338f, 508f );

                    // 背景
                    dc.FillRectangle( 領域dpx, this._黒透過ブラシ );

                if( 表示可能ノードである )
                {
                    var 難易度リスト = new (string ラベル, double 値)[ 5 ];

                    if( node is SetNode setNode )
                    {
                        for( int i = 0; i < 5; i++ )
                            難易度リスト[ i ] = (setNode.MusicNodes[ i ]?.難易度ラベル ?? "", setNode.MusicNodes[ i ]?.難易度 ?? 0.0);
                    }
                    else
                    {
                        for( int i = 0; i < 5; i++ )
                            難易度リスト[ i ] = ("", 0f);

                        難易度リスト[ 3 ] = (node.難易度ラベル, node.難易度);
                    }

                    // ULTIMATE 相当
                    this._難易度パネルを１つ描画する( dc, 領域dpx.X + 156f, 領域dpx.Y + 13f, 難易度リスト[ 4 ].ラベル, 難易度リスト[ 4 ].値, this._白ブラシ, this._ULTIMATE色ブラシ, this._黒ブラシ );

                    // MASTER 相当
                    this._難易度パネルを１つ描画する( dc, 領域dpx.X + 156f, 領域dpx.Y + 114f, 難易度リスト[ 3 ].ラベル, 難易度リスト[ 3 ].値, this._白ブラシ, this._MASTER色ブラシ, this._黒ブラシ );

                    // EXTREME 相当
                    this._難易度パネルを１つ描画する( dc, 領域dpx.X + 156f, 領域dpx.Y + 215f, 難易度リスト[ 2 ].ラベル, 難易度リスト[ 2 ].値, this._白ブラシ, this._EXTREME色ブラシ, this._黒ブラシ );

                    // ADVANCED 相当
                    this._難易度パネルを１つ描画する( dc, 領域dpx.X + 156f, 領域dpx.Y + 316f, 難易度リスト[ 1 ].ラベル, 難易度リスト[ 1 ].値, this._白ブラシ, this._ADVANCED色ブラシ, this._黒ブラシ );

                    // BASIC 相当
                    this._難易度パネルを１つ描画する( dc, 領域dpx.X + 156f, 領域dpx.Y + 417f, 難易度リスト[ 0 ].ラベル, 難易度リスト[ 0 ].値, this._白ブラシ, this._BASIC色ブラシ, this._黒ブラシ );
                }
                //----------------
                #endregion

            } );


            if( 表示可能ノードである )
            {
                #region " 選択枠を描画する。"
                //----------------
                var 青い線 = this.青い線を取得する();

                if( null != 青い線 )
                {
                    var 領域dpx = new RectangleF( 642f + 10f, 529f + 5f + ( 4 - 選択している難易度 ) * 101f, 338f - 20f, 100f );
                    var 太さdpx = 青い線.太さdpx;

                    青い線.描画する( dc, new Vector2( 領域dpx.Left - 太さdpx / 4f, 領域dpx.Top ), 幅dpx: 領域dpx.Width + 太さdpx / 2f );      // 上辺
                    青い線.描画する( dc, new Vector2( 領域dpx.Left, 領域dpx.Top - 太さdpx / 4f ), 高さdpx: 領域dpx.Height + 太さdpx / 2f );   // 左辺
                    青い線.描画する( dc, new Vector2( 領域dpx.Left - 太さdpx / 4f, 領域dpx.Bottom ), 幅dpx: 領域dpx.Width + 太さdpx / 2f );   // 下辺
                    青い線.描画する( dc, new Vector2( 領域dpx.Right, 領域dpx.Top - 太さdpx / 4f ), 高さdpx: 領域dpx.Height + 太さdpx / 2f );  // 右辺
                }
                //----------------
                #endregion
            }
        }

        private void _難易度パネルを１つ描画する( DeviceContext dc, float 基点X, float 基点Y, string 難易度ラベル, double 難易度値, Brush 文字ブラシ, Brush 見出し背景ブラシ, Brush 数値背景ブラシ )
        {
            dc.FillRectangle( new RectangleF( 基点X, 基点Y, 157f, 20f ), 見出し背景ブラシ );
            dc.FillRectangle( new RectangleF( 基点X, 基点Y + 20f, 157f, 66f ), 数値背景ブラシ );

            this._見出し用TextFormat.TextAlignment = TextAlignment.Trailing;

            dc.DrawText( 難易度ラベル, this._見出し用TextFormat, new RectangleF( 基点X + 4f, 基点Y, 157f - 8f, 18f ), 文字ブラシ );


            if( 難易度ラベル.Nullでも空でもない() && 0.00 != 難易度値 )
            {
                var 難易度値文字列 = 難易度値.ToString( "0.00" ).PadLeft( 1 ); // 整数部は２桁を保証（１桁なら十の位は空白文字）

                // 小数部を描画する
                this._数字画像.描画する( dc, 基点X + 84f, 基点Y + 38f, 難易度値文字列.Substring( 2 ), new Size2F( 0.5f, 0.5f ) );

                // 整数部を描画する（'.'含む）
                this._数字画像.描画する( dc, 基点X + 20f, 基点Y + 20f, 難易度値文字列.Substring( 0, 2 ), new Size2F( 0.7f, 0.7f ) );
            }
        }



        // private


        private 画像フォント _数字画像 = null;

        private Node _現在表示しているノード = null;

        private TextFormat _見出し用TextFormat = null;

        private SolidColorBrush _黒ブラシ;

        private SolidColorBrush _黒透過ブラシ;

        private SolidColorBrush _白ブラシ;

        private SolidColorBrush _ULTIMATE色ブラシ;

        private SolidColorBrush _MASTER色ブラシ;

        private SolidColorBrush _EXTREME色ブラシ;

        private SolidColorBrush _ADVANCED色ブラシ;

        private SolidColorBrush _BASIC色ブラシ;
    }
}
