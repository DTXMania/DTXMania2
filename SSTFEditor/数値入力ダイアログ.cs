using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace SSTFEditor
{
    public partial class 数値入力ダイアログ : Form
    {
        public decimal 数値 => this.numericUpDown数値.Value;


        public 数値入力ダイアログ()
        {
            InitializeComponent();
        }

        public 数値入力ダイアログ( decimal 開始値, decimal 最小値, decimal 最大値, string 表示するメッセージ )
        {
            this.InitializeComponent();

            this.labelメッセージ.Text = 表示するメッセージ;
            this.numericUpDown数値.Value = 開始値;
            this.numericUpDown数値.Minimum = 最小値;
            this.numericUpDown数値.Maximum = 最大値;
        }


        // イベント

        protected void numericUpDown数値_KeyDown( object sender, KeyEventArgs e )
        {
            // ENTER → OK
            if( e.KeyCode == Keys.Return )
                this.buttonOK.PerformClick();

            // ESC → キャンセル
            if( e.KeyCode == Keys.Escape )
                this.buttonキャンセル.PerformClick();
        }
    }
}
