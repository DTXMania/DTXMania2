using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania.選曲
{
    class BPMパネル : IDisposable
    {
        
        // 生成と終了


        public BPMパネル()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this._BPMパネル = new テクスチャ( @"$(System)images\選曲\BPMパネル.png" );
                this._パラメータ文字 = new 画像フォント( @"$(System)images\パラメータ文字_小.png", @"$(System)images\パラメータ文字_小.yaml", 文字幅補正dpx: 0f );
            }
        }

        public virtual void Dispose()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this._パラメータ文字?.Dispose();
                this._BPMパネル?.Dispose();
            }
        }



        // 進行と描画


        public void 描画する( DeviceContext dc )
        {
            var 領域 = new RectangleF( 78f, 455f, 357f, 55f );


            if( App進行描画.曲ツリー.フォーカス曲ノード != this._現在表示しているノード )
            {
                #region " フォーカスノードが変更されたので情報を更新する。"
                //----------------
                this._現在表示しているノード = App進行描画.曲ツリー.フォーカス曲ノード;

                this._最小BPM = this._現在表示しているノード?.最小BPM ?? 120.0;
                this._最大BPM = this._現在表示しているノード?.最大BPM ?? 120.0;
                //----------------
                #endregion
            }


            this._BPMパネル.描画する( 領域.X - 5f, 領域.Y - 4f );


            bool 表示可能ノードである = ( this._現在表示しているノード is MusicNode );   // 現状、BPMを表示できるノードは MusicNode のみ。

            if( 表示可能ノードである )
            {
                // BPM を表示する。

                if( 10.0 >= Math.Abs( this._最大BPM - this._最小BPM ) ) // 差が10以下なら同一値とみなす。
                {
                    // (A) 「最小値」だけ描画。
                    this._パラメータ文字.描画する( dc, 領域.X + 120f, 領域.Y, this._最小BPM.ToString( "0" ).PadLeft( 3 ) );
                }
                else
                {
                    // (B) 「最小～最大」を描画。
                    this._パラメータ文字.描画する( dc, 領域.X + 80f, 領域.Y, this._最小BPM.ToString( "0" ) + "~" + this._最大BPM.ToString( "0" ) );
                }
            }
        }



        // private


        private テクスチャ _BPMパネル = null;

        private 画像フォント _パラメータ文字 = null;

        private MusicNode _現在表示しているノード = null;

        private double _最小BPM;

        private double _最大BPM;
    }
}
