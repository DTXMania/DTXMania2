using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SSTFEditor
{
    public partial class オプションダイアログ : Form
    {
        public オプションダイアログ()
        {
            InitializeComponent();
        }

        private void buttonViewerPath参照_Click( object sender, EventArgs e )
        {
            #region " ファイルを開くダイアログでファイルを選択する。"
            //-----------------
            var dialog = new OpenFileDialog() {
                Title = Properties.Resources.MSG_ファイル選択ダイアログのタイトル,
                Filter = Properties.Resources.MSG_ビュアー選択ダイアログのフィルタ,
                FilterIndex = 1,
                //InitialDirectory = "",
            };
            var result = dialog.ShowDialog( this );

            // メインフォームを再描画してダイアログを完全に消す。
            this.Refresh();

            // OKじゃないならここで中断。
            if( DialogResult.OK != result )
                return;
            //-----------------
            #endregion

            this.textBoxViewerPath.Text = dialog.FileName;
        }
    }
}