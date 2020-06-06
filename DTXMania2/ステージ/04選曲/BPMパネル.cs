using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using FDK;
using DTXMania2.曲;

namespace DTXMania2.選曲
{
    class BPMパネル : IDisposable
    {

        // 生成と終了


        public BPMパネル()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._BPMパネル = new 画像( @"$(Images)\SelectStage\BpmPanel.png" );
            this._パラメータ文字 = new フォント画像( @"$(Images)\ParameterFont_Small.png", @"$(Images)\ParameterFont_Small.yaml", 文字幅補正dpx: 0f );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._パラメータ文字.Dispose();
            this._BPMパネル.Dispose();
        }



        // 進行と描画


        public void 進行描画する( DeviceContext dc, Node フォーカスノード )
        {
            var 領域 = new RectangleF( 78f, 455f, 357f, 55f );

            #region " パネルを描画する。"
            //----------------
            this._BPMパネル.進行描画する( 領域.X - 5f, 領域.Y - 4f );
            //----------------
            #endregion

            if( !( フォーカスノード is SongNode snode ) )
            {
                // 現状、BPMを表示できるノードは SongNode のみ。
                // SongNode 以外はパネルの表示だけで終わり。
                return;
            }

            #region " フォーカスノードが変更されていれば情報を更新する。"
            //----------------
            if( フォーカスノード != this._現在表示しているノード )
            {
                this._現在表示しているノード = フォーカスノード;

                if( snode.曲.フォーカス譜面?.譜面と画像を現行化済み ?? false )
                {
                    this._最小BPM = snode.曲.フォーカス譜面!.譜面.MinBPM;
                    this._最大BPM = snode.曲.フォーカス譜面!.譜面.MaxBPM;
                }
                else
                {
                    this._最小BPM = null;
                    this._最大BPM = null;
                }
            }
            //----------------
            #endregion

            #region " BPM を描画する。"
            //----------------
            if( this._最小BPM.HasValue && this._最大BPM.HasValue )
            {
                if( 10.0 >= Math.Abs( this._最大BPM.Value - this._最小BPM.Value ) ) // 差が10以下なら同一値(A)とみなす。
                {
                    // (A) 「最小値」だけ描画。
                    this._パラメータ文字.進行描画する( 領域.X + 120f, 領域.Y, this._最小BPM.Value.ToString( "0" ).PadLeft( 3 ) );
                }
                else
                {
                    // (B) 「最小～最大」を描画。
                    this._パラメータ文字.進行描画する( 領域.X + 120f, 領域.Y, this._最小BPM.Value.ToString( "0" ) + "~" + this._最大BPM.Value.ToString( "0" ) );
                }
            }
            //----------------
            #endregion
        }



        // ローカル


        private readonly 画像 _BPMパネル;

        private readonly フォント画像 _パラメータ文字;

        private Node? _現在表示しているノード = null;

        private double? _最小BPM = null;

        private double? _最大BPM = null;
    }
}
