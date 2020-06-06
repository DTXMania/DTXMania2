using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using FDK;
using DTXMania2.曲;

namespace DTXMania2.選曲
{
    class 難易度と成績 : IDisposable
    {

        // 外部接続アクション


        public Func<青い線> 青い線を取得する = () => throw new NotImplementedException();



        // 生成と終了


        public 難易度と成績()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._数字画像 = new フォント画像D2D( @"$(Images)\ParameterFont_Large.png", @"$(Images)\ParameterFont_Large.yaml" );
            this._見出し用TextFormat = new TextFormat( Global.DWriteFactory, "Century Gothic", 16f ) { TextAlignment = TextAlignment.Trailing };
            this._説明文用TextFormat = new TextFormat( Global.DWriteFactory, "Century Gothic", 16f ) { TextAlignment = TextAlignment.Center };
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._説明文用TextFormat.Dispose();
            this._見出し用TextFormat.Dispose();
            this._数字画像.Dispose();
        }



        // 進行と描画


        /// <param name="選択している難易度レベル">
        ///		0:BASIC～4:ULTIMATE
        ///	</param>
        public void 進行描画する( DeviceContext dc, int 選択している難易度レベル, Node フォーカスノード )
        {
            var 背景領域dpx = new RectangleF( 642f, 529f, 338f, 508f );

            #region " 背景を描画する。"
            //----------------
            D2DBatch.Draw( dc, () => {
                using var _黒透過ブラシ = new SolidColorBrush( dc, new Color4( Color3.Black, 0.5f ) );
                dc.PrimitiveBlend = PrimitiveBlend.SourceOver;
                dc.FillRectangle( 背景領域dpx, _黒透過ブラシ );
            } );
            //----------------
            #endregion

            var パネル位置リスト = new (float X, float Y)[ 5 ] {
                ( 背景領域dpx.X + 156f, 背景領域dpx.Y + 417f ),
                ( 背景領域dpx.X + 156f, 背景領域dpx.Y + 316f ),
                ( 背景領域dpx.X + 156f, 背景領域dpx.Y + 215f ),
                ( 背景領域dpx.X + 156f, 背景領域dpx.Y + 114f ),
                ( 背景領域dpx.X + 156f, 背景領域dpx.Y +  13f ),
            };

            #region " 難易度パネル（背景）を描画する。"
            //----------------
            using var 黒ブラシ = new SolidColorBrush( dc, Color4.Black );
            using var 白ブラシ = new SolidColorBrush( dc, Color4.White );
            using var ULTIMATE色ブラシ = new SolidColorBrush( dc, Song.難易度色リスト[ 4 ] );
            using var MASTER色ブラシ = new SolidColorBrush( dc, Song.難易度色リスト[ 3 ] );
            using var EXTREME色ブラシ = new SolidColorBrush( dc, Song.難易度色リスト[ 2 ] );
            using var ADVANCED色ブラシ = new SolidColorBrush( dc, Song.難易度色リスト[ 1 ] );
            using var BASIC色ブラシ = new SolidColorBrush( dc, Song.難易度色リスト[ 0 ] );

            var 難易度パネル色 = new Brush[ 5 ] {
                BASIC色ブラシ,
                ADVANCED色ブラシ,
                EXTREME色ブラシ,
                MASTER色ブラシ,
                ULTIMATE色ブラシ,
            };

            D2DBatch.Draw( dc, () => {
                for( int i = 0; i < 5; i++ )
                    this._難易度パネルの背景を１つ描画する( dc, パネル位置リスト[ i ].X, パネル位置リスト[ i ].Y, 難易度パネル色[ i ], 黒ブラシ );
            } );
            //----------------
            #endregion

            #region " フォーカスノードが変更されていれば更新する。"
            //----------------
            if( フォーカスノード != this._現在表示しているノード )
            {
                this._現在表示しているノード = フォーカスノード;
            }
            //----------------
            #endregion

            if( !( フォーカスノード is SongNode ) && !( フォーカスノード is RandomSelectNode ) )
                return; // 上記２つ以外はここまで。

            var 難易度ラベルリスト = new string[ 5 ];
            var 難易度リスト = new double[ 5 ];

            #region " 難易度ラベルリストと難易度リストを作成する。"
            //----------------
            if( フォーカスノード is SongNode snode )
            {
                for( int i = 0; i < 5; i++ )
                {
                    難易度ラベルリスト[ i ] = snode.曲.譜面リスト[ i ]?.難易度ラベル ?? "";
                    難易度リスト[ i ] = snode.曲.譜面リスト[ i ]?.譜面.Level ?? 5.0;
                }
            }
            else if( フォーカスノード is RandomSelectNode rnode )
            {
                for( int i = 0; i < 5; i++ )
                {
                    難易度ラベルリスト[ i ] = SetDef.デフォルトのラベル[ i ];
                    難易度リスト[ i ] = 0.0;
                }
            }
            //----------------
            #endregion

            #region " 難易度パネル（テキスト、数値）を描画する。"
            //----------------
            D2DBatch.Draw( dc, () => {
                for( int i = 0; i < 5; i++ )
                    this._難易度パネルのテキストを１つ描画する( dc, フォーカスノード, パネル位置リスト[ i ].X, パネル位置リスト[ i ].Y,難易度ラベルリスト[i], 難易度リスト[i], 白ブラシ );
            } );
            //----------------
            #endregion

            #region " 選択枠を描画する。"
            //----------------
            var 青い線 = this.青い線を取得する();

            if( null != 青い線 )
            {
                var 青領域dpx = new RectangleF( 642f + 10f, 529f + 5f + ( 4 - 選択している難易度レベル ) * 101f, 338f - 20f, 100f );
                var 太さdpx = 青い線.太さdpx;

                青い線.描画する( dc, new Vector2( 青領域dpx.Left - 太さdpx / 4f, 青領域dpx.Top ), 幅dpx: 青領域dpx.Width + 太さdpx / 2f );      // 上辺
                青い線.描画する( dc, new Vector2( 青領域dpx.Left, 青領域dpx.Top - 太さdpx / 4f ), 高さdpx: 青領域dpx.Height + 太さdpx / 2f );   // 左辺
                青い線.描画する( dc, new Vector2( 青領域dpx.Left - 太さdpx / 4f, 青領域dpx.Bottom ), 幅dpx: 青領域dpx.Width + 太さdpx / 2f );   // 下辺
                青い線.描画する( dc, new Vector2( 青領域dpx.Right, 青領域dpx.Top - 太さdpx / 4f ), 高さdpx: 青領域dpx.Height + 太さdpx / 2f );  // 右辺
            }
            //----------------
            #endregion
        }

        private void _難易度パネルの背景を１つ描画する( DeviceContext dc, float 基点X, float 基点Y, Brush 見出し背景ブラシ, Brush 数値背景ブラシ )
        {
            D2DBatch.Draw( dc, () => {

                dc.FillRectangle( new RectangleF( 基点X, 基点Y, 157f, 20f ), 見出し背景ブラシ );
                dc.FillRectangle( new RectangleF( 基点X, 基点Y + 20f, 157f, 66f ), 数値背景ブラシ );

            } );
        }

        private void _難易度パネルのテキストを１つ描画する( DeviceContext dc, Node node, float 基点X, float 基点Y, string 難易度ラベル, double 難易度値, Brush 文字ブラシ )
        {
            D2DBatch.Draw( dc, () => {

                #region " 難易度ラベル "
                //----------------
                dc.DrawText( 難易度ラベル, this._見出し用TextFormat, new RectangleF( 基点X + 4f, 基点Y, 157f - 8f, 18f ), 文字ブラシ );
                //----------------
                #endregion

                if( node is RandomSelectNode )
                {
                    #region " RandomNode 用説明文 "
                    //----------------
                    dc.DrawText( 難易度ラベル + " 付近を\nランダムに選択", this._説明文用TextFormat, new RectangleF( 基点X + 4f, 基点Y + 30f, 157f - 8f, 40f ), 文字ブラシ );
                    //----------------
                    #endregion
                }
                else if( !string.IsNullOrEmpty( 難易度ラベル ) && 0.00 != 難易度値 )
                {
                    #region " 難易度値 "
                    //----------------
                    var 難易度値文字列 = 難易度値.ToString( "0.00" ).PadLeft( 1 ); // 整数部は２桁を保証（１桁なら十の位は空白文字）

                    // 小数部を描画する
                    this._数字画像.描画する( dc, 基点X + 84f, 基点Y + 38f, 難易度値文字列[ 2.. ], new Size2F( 0.5f, 0.5f ) );

                    // 整数部を描画する（'.'含む）
                    this._数字画像.描画する( dc, 基点X + 20f, 基点Y + 20f, 難易度値文字列[ 0..2 ], new Size2F( 0.7f, 0.7f ) );
                    //----------------
                    #endregion
                }
            
            } );
        }



        // ローカル


        private readonly フォント画像D2D _数字画像;

        private readonly TextFormat _見出し用TextFormat;

        private readonly TextFormat _説明文用TextFormat;

        private Node? _現在表示しているノード = null;
    }
}
