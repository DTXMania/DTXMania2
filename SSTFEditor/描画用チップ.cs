using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SSTF=SSTFormat.v004;

namespace SSTFEditor
{
    /// <summary>
    ///		SSTFormat.チップ の機能拡張。
    /// </summary>
    class 描画用チップ : SSTF.チップ
    {
        public int 譜面内絶対位置grid { get; set; } = 0;

        public bool ドラッグ操作により選択中である { get; set; } = false;

        public bool 選択が確定している { get; set; } = false;
        public bool 選択が確定していない
        {
            get => !this.選択が確定している;
            set => this.選択が確定している = !value;
        }

        public bool 移動済みである { get; set; } = true;
        public bool 移動されていない
        {
            get => !this.移動済みである;
            set => this.移動済みである = !value;
        }

        public string チップ内文字列 { get; set; } = null;

        public int 枠外レーン数 { get; set; } = 0;


        public 描画用チップ( SSTF.チップ sstfChip = null )
        {
            if( null != sstfChip )
                this.CopyFrom( sstfChip );

            this._特別なチップ内文字列を設定する();
        }
        public 描画用チップ( 描画用チップ sstfChip )
        {
            描画用チップ.Copy( sstfChip, this );

            this._特別なチップ内文字列を設定する();
        }

        public static void Copy( 描画用チップ src, 描画用チップ dst )
        {
            ( (SSTF.チップ) dst ).CopyFrom( src );

            dst.譜面内絶対位置grid = src.譜面内絶対位置grid;
            dst.ドラッグ操作により選択中である = src.ドラッグ操作により選択中である;
            dst.選択が確定している = src.選択が確定している;
            dst.移動済みである = src.移動済みである;
            dst.チップ内文字列 = src.チップ内文字列;
            dst.枠外レーン数 = src.枠外レーン数;
        }
        public void CopyFrom( 描画用チップ コピー元チップ )
        {
            描画用チップ.Copy( コピー元チップ, this );
        }


        private void _特別なチップ内文字列を設定する()
        {
            if( this.チップ種別 == SSTF.チップ種別.China ) this.チップ内文字列 = "C N";
            if( this.チップ種別 == SSTF.チップ種別.Splash ) this.チップ内文字列 = "S P";
            if( this.チップ種別 == SSTF.チップ種別.BPM ) this.チップ内文字列 = this.BPM.ToString( "###.##" );
        }
    }
}
