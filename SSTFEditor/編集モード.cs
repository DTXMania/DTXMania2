using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SSTF=SSTFormat.v004;

namespace SSTFEditor
{
    class 編集モード
    {
        public Rectangle 現在のチップカーソル領域 { get; protected set; }


        public 編集モード( メインフォーム form )
        {
            this.Form = form;
            this.現在のチップカーソル領域 = new Rectangle( 0, 0, 0, 0 );

            // 現在のチップ種別番号の初期化。

            this.dicレーン別現在のチップ種別番号 = new Dictionary<編集レーン種別, int>();
            this.dicレーン別チップ種別バックアップ = new Dictionary<編集レーン種別, int>();

            foreach( 編集レーン種別 editLaneType in Enum.GetValues( typeof( 編集レーン種別 ) ) )
            {
                this.dicレーン別現在のチップ種別番号[ editLaneType ] = 0;
                this.dicレーン別チップ種別バックアップ[ editLaneType ] = 0;
            }

            this.レーン別チップ種別対応表を初期化する();
        }

        public void レーン別チップ種別対応表を初期化する()
        {
            // 譜面.チップ種別to編集レーン プロパティを元にするので、譜面を先に生成してから編集モードを生成すること。

            this.dicレーン別チップ種別対応表 = new Dictionary<編集レーン種別, List<SSTF.チップ種別>>();

            foreach( 編集レーン種別 editLaneType in Enum.GetValues( typeof( 編集レーン種別 ) ) )
                this.dicレーン別チップ種別対応表[ editLaneType ] = new List<SSTF.チップ種別>();

            foreach( var kvp in this.Form.譜面.チップ種別to編集レーン )
            {
                if( kvp.Key == SSTF.チップ種別.LeftBass )
                    continue;   // SSTFEditorではLeftBassを扱わない

                this.dicレーン別チップ種別対応表[ kvp.Value ].Add( kvp.Key );
            }
        }


        // イベント

        public void MouseClick( MouseEventArgs e )
        {
            this.チップを配置または削除する( e );
            this.Form.譜面をリフレッシュする();
        }

        public void MouseLeave( EventArgs e )
        {
            this.現在のチップカーソル領域 = new Rectangle( 0, 0, 0, 0 );
            this.Form.譜面をリフレッシュする();
        }

        public void MouseMove( MouseEventArgs e )
        {
            #region " チップカーソルの領域を、現在の位置に合わせて更新する。"
            //-----------------
            var 以前のチップカーソル領域 = this.現在のチップカーソル領域;

            this.現在チップカーソルがある編集レーン = this.Form.譜面.譜面パネル内X座標pxにある編集レーンを返す( e.X );
            this.現在のチップカーソルの譜面先頭からのガイド単位の位置grid = this.Form.譜面.譜面パネル内Y座標pxにおける譜面内絶対位置gridをガイド幅単位で返す( e.Y );

            // カーソルが譜面外にあるならここで終了。
            if( this.現在チップカーソルがある編集レーン == 編集レーン種別.Unknown ||
                ( 0 > this.現在のチップカーソルの譜面先頭からのガイド単位の位置grid ) )
            {
                this.現在チップカーソルがある編集レーン = 編集レーン種別.Unknown;
                this.Form.譜面をリフレッシュする();    // チップを消去する。
                return;
            }

            // 新しい現在の領域を取得。
            this.現在のチップカーソル領域 =
                new Rectangle(
                    this.Form.譜面.編集レーンのX座標pxを返す( this.現在チップカーソルがある編集レーン ),
                    this.Form.譜面.譜面内絶対位置gridにおける対象領域内のY座標pxを返す( this.現在のチップカーソルの譜面先頭からのガイド単位の位置grid, this.Form.譜面パネルサイズ ) - this.Form.譜面.チップサイズpx.Height,
                    this.Form.譜面.チップサイズpx.Width,
                    this.Form.譜面.チップサイズpx.Height );
            //-----------------
            #endregion

            this.チップカーソルのあるレーンに合わせて現在のチップ種別を変更する();

            #region " 領域が変わってたら譜面を再描画する。"
            //-----------------
            if( false == this.現在のチップカーソル領域.Equals( 以前のチップカーソル領域 ) )
                this.Form.譜面をリフレッシュする();
            //-----------------
            #endregion
        }

        public void Paint( PaintEventArgs e )
        {
            this.チップカーソルを描画する( e.Graphics, this.Form.現在のチップ種別 );
        }

        public void PreviewKeyDown( PreviewKeyDownEventArgs e )
        {
            // SPACE
            if( e.KeyCode == Keys.Space && !e.Shift )
            {
                #region " 現在のレーン別チップ種別を、登録順にローテーションする。"
                //-----------------
                // インデックスを１つ増やす。インデックスが範囲を超えたら 0 に戻す。
                int n = this.dicレーン別現在のチップ種別番号[ this.現在チップカーソルがある編集レーン ] + 1;
                if( n == this.dicレーン別チップ種別対応表[ this.現在チップカーソルがある編集レーン ].Count )
                    n = 0;
                this.dicレーン別現在のチップ種別番号[ this.現在チップカーソルがある編集レーン ] = n;

                // 画面へ反映する。
                this.チップカーソルのあるレーンに合わせて現在のチップ種別を変更する();
                this.Form.譜面をリフレッシュする();

                this.強制種別使用中 = false;
                //-----------------
                #endregion
            }
            // SHIFT + SPACE
            else if( e.KeyCode == Keys.Space && e.Shift )
            {
                #region " 現在のレーン別チップ種別を、特定の種別（２番目）とトグル切り替えする。"
                //-----------------
                if( this.dicレーン別チップ種別対応表[ this.現在チップカーソルがある編集レーン ].Count > 1 )  // 種別を２つ以上持たないレーンは関係ない。
                {
                    if( false == this.強制種別使用中 )
                    {
                        // (A) 特定の種別（２番目）に切替える。

                        // 現在のインデックスをバックアップしておく。
                        this.dicレーン別チップ種別バックアップ[ this.現在チップカーソルがある編集レーン ] =
                            this.dicレーン別現在のチップ種別番号[ this.現在チップカーソルがある編集レーン ];

                        // 現在のインデックスを強制的に「1」（２番目の要素）にする。
                        this.dicレーン別現在のチップ種別番号[ this.現在チップカーソルがある編集レーン ] = 1;

                        this.強制種別使用中 = true;
                    }
                    else
                    {
                        // (B) 元の種別に戻す。

                        // 現在のインデックスをバックアップ値に戻す。
                        this.dicレーン別現在のチップ種別番号[ this.現在チップカーソルがある編集レーン ] =
                            this.dicレーン別チップ種別バックアップ[ this.現在チップカーソルがある編集レーン ];

                        this.強制種別使用中 = false;
                    }

                    // 画面へ反映する。
                    this.チップカーソルのあるレーンに合わせて現在のチップ種別を変更する();
                    this.Form.譜面をリフレッシュする();
                }
                //-----------------
                #endregion
            }
        }


        protected メインフォーム Form;

        protected 編集レーン種別 現在チップカーソルがある編集レーン;

        protected int 現在のチップカーソルの譜面先頭からのガイド単位の位置grid;

        protected bool 強制種別使用中 = false;

        protected Dictionary<編集レーン種別, int> dicレーン別チップ種別バックアップ;

        protected Dictionary<編集レーン種別, int> dicレーン別現在のチップ種別番号;

        protected Dictionary<編集レーン種別, List<SSTF.チップ種別>> dicレーン別チップ種別対応表;


        protected void チップカーソルを描画する( Graphics g, SSTF.チップ種別 チップ種別 )
        {
            #region " 事前チェック。"
            //-----------------
            if( ( 0 >= this.現在のチップカーソル領域.Width ) ||
                ( 0 >= this.現在のチップカーソル領域.Height ) ||
                ( this.現在チップカーソルがある編集レーン == 編集レーン種別.Unknown ) ||
                ( チップ種別 == SSTF.チップ種別.Unknown ) ||
                ( チップ種別 == SSTF.チップ種別.小節線 ) ||
                ( チップ種別 == SSTF.チップ種別.拍線 ) )
            {
                return;     // 描画しない。
            }
            //-----------------
            #endregion

            // チップを描いて、
            this.Form.譜面.チップを指定領域へ描画する( g, チップ種別, this.Form.現在のチップ音量, this.現在のチップカーソル領域, null );

            // チップをカーソル枠で囲む。
            this.Form.譜面.チップの太枠を指定領域へ描画する( g, this.現在のチップカーソル領域 );
        }

        protected void チップカーソルのあるレーンに合わせて現在のチップ種別を変更する()
        {
            int index = this.dicレーン別現在のチップ種別番号[ this.現在チップカーソルがある編集レーン ];
            this.Form.現在のチップ種別 = this.dicレーン別チップ種別対応表[ this.現在チップカーソルがある編集レーン ][ index ];
        }

        protected void チップを配置または削除する( MouseEventArgs eClick )
        {
            if( ( 0 >= this.現在のチップカーソル領域.Width ) || ( 0 >= this.現在のチップカーソル領域.Height ) )
                return;

            // 左クリック
            if( eClick.Button == MouseButtons.Left )
            {
                #region " チップを配置する。"
                //-----------------
                bool CTRL押下 = ( Control.ModifierKeys & Keys.Control ) == Keys.Control;

                if( this.現在チップカーソルがある編集レーン != 編集レーン種別.BPM )
                {
                    #region " (A) BPM レーン以外の場合 "
                    //-----------------
                    string チップ内文字列 = null;

                    if( this.Form.現在のチップ種別 == SSTF.チップ種別.China )
                        チップ内文字列 = "C N";

                    if( this.Form.現在のチップ種別 == SSTF.チップ種別.Splash )
                        チップ内文字列 = "S P";

                    this.Form.譜面.チップを配置または置換する(
                        e編集レーン: this.現在チップカーソルがある編集レーン,
                        eチップ: this.Form.現在のチップ種別,
                        譜面内絶対位置grid: this.現在のチップカーソルの譜面先頭からのガイド単位の位置grid,
                        チップ文字列: チップ内文字列,
                        音量: this.Form.現在のチップ音量, // 音量変化は未実装
                        BPM: 0.0,
                        選択確定中: false );
                    //-----------------
                    #endregion
                }
                else
                {
                    #region " (B) BPM レーンの場合 "
                    //-----------------
                    using var dialog = new 数値入力ダイアログ(
                        (decimal) this.Form.譜面.譜面内絶対位置gridにおけるBPMを返す( this.現在のチップカーソルの譜面先頭からのガイド単位の位置grid ),
                        0.0001M,
                        1000M,
                        Properties.Resources.MSG_BPM選択ダイアログの説明文 );

                    if( dialog.ShowDialog( this.Form ) != DialogResult.OK )
                        return;

                    double bpm = (double) dialog.数値;

                    this.Form.譜面.チップを配置または置換する(
                        e編集レーン: 編集レーン種別.BPM,
                        eチップ: SSTF.チップ種別.BPM,
                        譜面内絶対位置grid: this.現在のチップカーソルの譜面先頭からのガイド単位の位置grid,
                        チップ文字列: bpm.ToString( "###.##" ),
                        音量: メインフォーム.最大音量,       // BPM チップは常に最大音量枠
                        BPM: bpm,
                        選択確定中: false );
                    //-----------------
                    #endregion
                }
                //-----------------
                #endregion
            }
            else if( eClick.Button == MouseButtons.Right )
            {
                #region " チップを削除する。"
                //-----------------
                this.Form.譜面.チップを削除する(
                    this.現在チップカーソルがある編集レーン,
                    this.現在のチップカーソルの譜面先頭からのガイド単位の位置grid );
                //-----------------
                #endregion
            }
        }
    }
}
