using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using FDK;
using SSTF=SSTFormat.v004;

namespace SSTFEditor
{
    partial class 検索条件入力ダイアログ : Form
    {
        public bool 小節範囲指定CheckBoxがチェックされている => this.checkBox小節範囲指定.Checked;

        public int 小節範囲開始番号
        {
            get
            {
                int 開始番号 = -1;

                if( this.textBox小節範囲開始.Text.Nullでも空でもない() )
                {
                    // (A) 開始番号に文字がある。
                    try
                    {
                        開始番号 = int.Parse( this.textBox小節範囲開始.Text );
                    }
                    catch
                    {
                        開始番号 = -1;
                    }
                }
                else if( this.textBox小節範囲終了.Text.Nullまたは空である() )
                {
                    // (B) 開始番号も終了番号も、空欄である。
                    開始番号 = -1;
                }
                else
                {
                    // (C) 終了番号にだけ文字がある。
                    try
                    {
                        開始番号 = int.Parse( this.textBox小節範囲終了.Text );
                    }
                    catch
                    {
                        開始番号 = -1;
                    }
                }

                return 開始番号;
            }
        }

        public int 小節範囲終了番号
        {
            get
            {
                int 終了番号 = -1;

                // (A) 終了番号に文字がある。
                if( this.textBox小節範囲終了.Text.Nullでも空でもない() )
                {
                    try
                    {
                        終了番号 = int.Parse( this.textBox小節範囲終了.Text );
                    }
                    catch
                    {
                        終了番号 = -1;
                    }
                }
                else if( this.textBox小節範囲開始.Text.Nullまたは空である() )
                {
                    // (B) 開始番号も終了番号も、空欄である。
                    終了番号 = -1;
                }
                else
                {
                    // (C) 開始場号にだけ文字がある。
                    try
                    {
                        終了番号 = int.Parse( this.textBox小節範囲開始.Text );
                    }
                    catch
                    {
                        終了番号 = -1;
                    }
                }

                return 終了番号;
            }
        }


        public 検索条件入力ダイアログ()
        {
            InitializeComponent();

            #region " レーンリスト（チェックリストボックス）とそのチェックボックスに、前回の検索条件値を反映する。"
            //----------------
            // 未初期化なら先に初期化する。
            if( null == 前回の設定値.レーンを検索対象にする )
            {
                前回の設定値.レーンを検索対象にする = new bool[ this.dic行と編集レーン対応表.Count ];
                for( int i = 0; i < 前回の設定値.レーンを検索対象にする.Length; i++ )
                    前回の設定値.レーンを検索対象にする[ i ] = false;    // 前回値をすべて false にする。
            }
            // 前回の検索条件値を反映する。
            foreach( var kvp in this.dic行と編集レーン対応表 )
                this.checkedListBoxレーン選択リスト.Items.Add( this.編集レーン名[ kvp.Key ], 前回の設定値.レーンを検索対象にする[ kvp.Key ] );
            //----------------
            #endregion

            #region " チップリストとチップリストチェックに前回値を指定する。未初期化なら初期化する。"
            //----------------
            // 未初期化なら先に初期化する。
            if( null == 前回の設定値.チップを検索対象にする )
            {
                前回の設定値.チップを検索対象にする = new bool[ this.dic行とチップ種別対応表.Count ];
                for( int i = 0; i < 前回の設定値.チップを検索対象にする.Length; i++ )
                    前回の設定値.チップを検索対象にする[ i ] = false;    // 前回値をすべて false にする。
            }
            // 前回の検索条件値を反映する。
            foreach( var kvp in this.dic行とチップ種別対応表 )
                this.checkedListBoxチップ選択リスト.Items.Add( this.チップ種別名[ kvp.Key ], 前回の設定値.チップを検索対象にする[ kvp.Key ] );
            //----------------
            #endregion

            this.checkBox小節範囲指定.CheckState = 前回の設定値.検索する小節範囲を指定する;

            this.チェックに連動して有効無効が決まるパーツについてEnabledを設定する();
        }

        public bool 選択されている( 編集レーン種別 laneType )
        {
            // First() で要素が見つからなかったらバグなので、そのまま System.InvalidOperationException を放出させる。
            var key = this.dic行と編集レーン対応表.First( ( kvp ) => ( kvp.Value == laneType ) ).Key;
            return ( this.checkedListBoxレーン選択リスト.GetItemCheckState( key ) == CheckState.Checked );
        }

        public bool 選択されている( SSTF.チップ種別 chipType )
        {
            // First() で要素が見つからなかったらバグなので、そのまま System.InvalidOperationException を放出させる。
            var key = this.dic行とチップ種別対応表.First( ( kvp ) => ( kvp.Value == chipType ) ).Key;
            return ( this.checkedListBoxチップ選択リスト.GetItemCheckState( key ) == CheckState.Checked );
        }


        protected static class 前回の設定値
        {
            public static CheckState 検索する小節範囲を指定する = CheckState.Unchecked;
            public static bool[] レーンを検索対象にする = null;
            public static bool[] チップを検索対象にする = null;
        };

        protected readonly Dictionary<int, 編集レーン種別> dic行と編集レーン対応表 = new Dictionary<int, 編集レーン種別>() {
            #region " *** "
            //-----------------
            {  0, 編集レーン種別.BPM },
            {  1, 編集レーン種別.左シンバル },
            {  2, 編集レーン種別.ハイハット },
            {  3, 編集レーン種別.スネア },
            {  4, 編集レーン種別.ハイタム },
            {  5, 編集レーン種別.バス },
            {  6, 編集レーン種別.ロータム },
            {  7, 編集レーン種別.フロアタム },
            {  8, 編集レーン種別.右シンバル },
            {  9, 編集レーン種別.BGV },
            { 10, 編集レーン種別.BGM },
            //-----------------
            #endregion
        };

        protected readonly string[] 編集レーン名 = new string[] {
            #region " *** "
            //-----------------
            "BPM",
            "Left Cymbal",
            "HiHat",
            "Snare",
            "High Tom",
            "Bass Drum",
            "Low Tom",
            "Floor Tom",
            "Right Cymbal",
            "BGA",
            //-----------------
            #endregion
        };

        protected readonly Dictionary<int, SSTF.チップ種別> dic行とチップ種別対応表 = new Dictionary<int, SSTF.チップ種別>() {
            #region " *** "
            //----------------
            {  0, SSTF.チップ種別.BPM },
            {  1, SSTF.チップ種別.LeftCrash },
            {  2, SSTF.チップ種別.HiHat_Close },
            {  3, SSTF.チップ種別.HiHat_HalfOpen },
            {  4, SSTF.チップ種別.HiHat_Open },
            {  5, SSTF.チップ種別.HiHat_Foot },
            {  6, SSTF.チップ種別.Snare },
            {  7, SSTF.チップ種別.Snare_Ghost },
            {  8, SSTF.チップ種別.Snare_ClosedRim },
            {  9, SSTF.チップ種別.Snare_OpenRim },
            { 10, SSTF.チップ種別.Tom1 },
            { 11, SSTF.チップ種別.Tom1_Rim },
            { 12, SSTF.チップ種別.Bass },
            { 13, SSTF.チップ種別.Tom2 },
            { 14, SSTF.チップ種別.Tom2_Rim },
            { 15, SSTF.チップ種別.Tom3 },
            { 16, SSTF.チップ種別.Tom3_Rim },
            { 17, SSTF.チップ種別.RightCrash },
            { 18, SSTF.チップ種別.Ride },
            { 19, SSTF.チップ種別.Ride_Cup },
            { 20, SSTF.チップ種別.China },
            { 21, SSTF.チップ種別.Splash },
            { 22, SSTF.チップ種別.背景動画 },
            //----------------
            #endregion
        };

        protected readonly string[] チップ種別名 = new string[] {
            #region " *** "
            //-----------------
            "BPM",
            "Left Crash",
            "HiHat Close",
            "HiHat HalfOpen",
            "HiHat Open",
            "Foot Pedal",
            "Snare",
            "Snare Ghost",
            "Snare Closed RimShot",
            "Snare Open RimShot",
            "High Tom",
            "High Tom RimShoft",
            "Bass Drum",
            "Low Tom",
            "Low Tom RimShot",
            "Floor Tom",
            "Floor Tom RimShoft",
            "Right Crash",
            "Ride",
            "Cup",
            "China Cymbal",
            "Splash Cymbal",
            "BGA",
            //-----------------
            #endregion
        };

        protected void チェックに連動して有効無効が決まるパーツについてEnabledを設定する()
        {
            bool flag = this.checkBox小節範囲指定.Checked;
            this.textBox小節範囲開始.Enabled = flag;
            this.textBox小節範囲終了.Enabled = flag;
        }


        // イベント

        protected void OnFormClosing( object sender, FormClosingEventArgs e )
        {
            if( this.DialogResult == DialogResult.OK )
            {
                // 入力値の妥当性を確認する。

                #region " 小節範囲開始 "
                //----------------
                {
                    var text = this.textBox小節範囲開始.Text;
                    if( text.Nullでも空でもない() && ( false == int.TryParse( text, out int num ) || ( 0 > num ) ) )
                    {
                        MessageBox.Show(
                            $"{Properties.Resources.MSG_小節番号に誤りがあります}{Environment.NewLine}'{text}'",
                            Properties.Resources.MSG_エラーダイアログのタイトル,
                            MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1 );
                        this.textBox小節範囲開始.Focus();
                        this.textBox小節範囲開始.SelectAll();
                        e.Cancel = true;
                        return;
                    }
                }
                //----------------
                #endregion
                #region " 小節範囲終了 "
                //----------------
                {
                    var text = this.textBox小節範囲終了.Text;
                    if( text.Nullでも空でもない() && ( false == int.TryParse( text, out int num ) || ( 0 > num ) ) )
                    {
                        MessageBox.Show(
                            $"{Properties.Resources.MSG_小節番号に誤りがあります}{Environment.NewLine}'{text}'",
                            Properties.Resources.MSG_エラーダイアログのタイトル,
                            MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1 );
                        this.textBox小節範囲終了.Focus();
                        this.textBox小節範囲終了.SelectAll();
                        e.Cancel = true;
                        return;
                    }
                }
                //----------------
                #endregion

                // 入力値を前回値として保存する。

                #region " 小節範囲指定 "
                //----------------
                前回の設定値.検索する小節範囲を指定する = this.checkBox小節範囲指定.CheckState;
                //----------------
                #endregion
                #region " レーンを検索対象にする[] "
                //----------------
                for( int i = 0; i < this.checkedListBoxレーン選択リスト.Items.Count; i++ )
                    前回の設定値.レーンを検索対象にする[ i ] = ( this.checkedListBoxレーン選択リスト.GetItemCheckState( i ) == CheckState.Checked );
                //----------------
                #endregion
                #region " チップを選択対象にする[] "
                //----------------
                for( int i = 0; i < this.checkedListBoxチップ選択リスト.Items.Count; i++ )
                    前回の設定値.チップを検索対象にする[ i ] = this.checkedListBoxチップ選択リスト.GetItemCheckState( i ) == CheckState.Checked;
                //----------------
                #endregion
            }
        }

        protected void OnKeyDown( object sender, KeyEventArgs e )
        {
            // ENTER → OK
            if( e.KeyCode == Keys.Return )
                this.buttonOK.PerformClick();

            // ESC → キャンセル
            else if( e.KeyCode == Keys.Escape )
                this.buttonキャンセル.PerformClick();
        }

        protected void textBox小節範囲開始_KeyDown( object sender, KeyEventArgs e )
        {
            // ENTER → OK
            if( e.KeyCode == Keys.Return )
                this.buttonOK.PerformClick();

            // ESC → キャンセル
            else if( e.KeyCode == Keys.Escape )
                this.buttonキャンセル.PerformClick();
        }

        protected void textBox小節範囲終了_KeyDown( object sender, KeyEventArgs e )
        {
            // ENTER → OK
            if( e.KeyCode == Keys.Return )
                this.buttonOK.PerformClick();

            // ESC → キャンセル
            else if( e.KeyCode == Keys.Escape )
                this.buttonキャンセル.PerformClick();
        }

        protected void checkBox小節範囲指定_CheckStateChanged( object sender, EventArgs e )
        {
            this.チェックに連動して有効無効が決まるパーツについてEnabledを設定する();
        }

        protected void checkBox小節範囲指定_KeyDown( object sender, KeyEventArgs e )
        {
            // ENTER → OK
            if( e.KeyCode == Keys.Return )
                this.buttonOK.PerformClick();

            // ESC → キャンセル
            else if( e.KeyCode == Keys.Escape )
                this.buttonキャンセル.PerformClick();
        }

        protected void checkedListBoxレーン選択リスト_KeyDown( object sender, KeyEventArgs e )
        {
            // ENTER → OK
            if( e.KeyCode == Keys.Return )
                this.buttonOK.PerformClick();

            // ESC → キャンセル
            else if( e.KeyCode == Keys.Escape )
                this.buttonキャンセル.PerformClick();
        }

        protected void buttonAllレーン_Click( object sender, EventArgs e )
        {
            for( int i = 0; i < this.checkedListBoxレーン選択リスト.Items.Count; i++ )
                this.checkedListBoxレーン選択リスト.SetItemChecked( i, true );
        }

        protected void buttonAllレーン_KeyDown( object sender, KeyEventArgs e )
        {
            // ESC → キャンセル
            if( e.KeyCode == Keys.Escape )
                this.buttonキャンセル.PerformClick();
        }

        protected void buttonClearレーン_Click( object sender, EventArgs e )
        {
            for( int i = 0; i < this.checkedListBoxレーン選択リスト.Items.Count; i++ )
                this.checkedListBoxレーン選択リスト.SetItemCheckState( i, CheckState.Unchecked );
        }

        protected void buttonClearレーン_KeyDown( object sender, KeyEventArgs e )
        {
            // ESC → キャンセル
            if( e.KeyCode == Keys.Escape )
                this.buttonキャンセル.PerformClick();
        }

        protected void buttonAllチップ_Click( object sender, EventArgs e )
        {
            for( int i = 0; i < this.checkedListBoxチップ選択リスト.Items.Count; i++ )
                this.checkedListBoxチップ選択リスト.SetItemChecked( i, true );
        }

        protected void buttonAllチップ_KeyDown( object sender, KeyEventArgs e )
        {
            // ESC → キャンセル
            if( e.KeyCode == Keys.Escape )
                this.buttonキャンセル.PerformClick();
        }

        protected void buttonClearチップ_Click( object sender, EventArgs e )
        {
            for( int i = 0; i < this.checkedListBoxチップ選択リスト.Items.Count; i++ )
                this.checkedListBoxチップ選択リスト.SetItemCheckState( i, CheckState.Unchecked );
        }

        protected void buttonClearチップ_KeyDown( object sender, KeyEventArgs e )
        {
            // ESC → キャンセル
            if( e.KeyCode == Keys.Escape )
                this.buttonキャンセル.PerformClick();
        }
    }
}
