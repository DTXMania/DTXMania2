using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SSTFEditor
{
    public partial class 小節長倍率入力ダイアログ : Form
    {
        public bool 後続も全部変更する
        {
            get => this.checkBox後続設定.Checked;
            set => this.checkBox後続設定.CheckState = value ? CheckState.Checked : CheckState.Unchecked;
        }

        public float 倍率
        {
            get => (float) this.numericUpDown小節長の倍率.Value;
            set => this.numericUpDown小節長の倍率.Value = (decimal) value;
        }


        public 小節長倍率入力ダイアログ( int 小節番号 )
        {
            InitializeComponent();

            this.textBox小節番号.Text = 小節番号.ToString( "000" );
        }


        protected void numericUpDown小節長の倍率_KeyDown( object sender, KeyEventArgs e )
        {
            // ENTER → OK
            if( e.KeyCode == Keys.Return )
                this.buttonOK.PerformClick();

            // ESC → Cancel
            else if( e.KeyCode == Keys.Escape )
                this.buttonキャンセル.PerformClick();
        }

        protected void checkBox後続設定_KeyDown( object sender, KeyEventArgs e )
        {
            // ENTER → OK
            if( e.KeyCode == Keys.Return )
                this.buttonOK.PerformClick();

            // ESC → Cancel
            else if( e.KeyCode == Keys.Escape )
                this.buttonキャンセル.PerformClick();
        }
    }
}
