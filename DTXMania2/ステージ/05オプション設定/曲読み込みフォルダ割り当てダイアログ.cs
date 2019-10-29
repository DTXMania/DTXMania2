using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DTXMania2.オプション設定
{
    public partial class 曲読み込みフォルダ割り当てダイアログ : Form
    {
        // 生成と終了


        public 曲読み込みフォルダ割り当てダイアログ( IReadOnlyList<VariablePath> 現在の曲検索フォルダリスト )
            : base()
        {
            InitializeComponent();

            foreach( var path in 現在の曲検索フォルダリスト )
                this.listViewフォルダ一覧.Items.Add( new ListViewItem( $"{path.変数なしパス}" ) );	// ここでは変数なしでパスを表示する。
        }

        private void 曲読み込みフォルダ割り当てダイアログ_FormClosing( object sender, FormClosingEventArgs e )
        {
            if( this.DialogResult == DialogResult.Cancel )    // ウィンドウを閉じようとした時も Cancel になる。
            {
                if( this._リストに変更がある )
                {
                    if( MessageBox.Show( "変更を破棄していいですか？", "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2 ) == DialogResult.No )
                        e.Cancel = true;
                }
            }
        }

        /// <summary>
        ///     結果取得。変更があったら true を返す。
        /// </summary>
        public bool 新しい曲検索フォルダリストを取得する( out List<VariablePath> 新しいフォルダリスト )
        {
            新しいフォルダリスト = new List<VariablePath>();

            foreach( ListViewItem? item in this.listViewフォルダ一覧.Items )
            {
                if( null != item )
                    新しいフォルダリスト.Add( new VariablePath( item.SubItems[ 0 ].Text ) );
            }

            return this._リストに変更がある;
        }



        // その他GUIイベント


        private void listViewフォルダ一覧_SelectedIndexChanged( object sender, EventArgs e )
        {
            if( 0 < this.listViewフォルダ一覧.SelectedItems.Count )
            {
                // フォルダが選択されたので、削除ボタンを有効化。
                this.button削除.Enabled = true;
            }
            else
            {
                // フォルダの選択が解除されたので、削除ボタンを無効化。
                this.button削除.Enabled = false;
            }
        }

        private void button選択_Click( object sender, EventArgs e )
        {
            // フォルダ選択ダイアログを生成する。
            using( var folderBrowser = new FolderBrowserDialog() {
                Description = "追加するフォルダを選択してください。",
            } )
            {
                // フォルダ選択ダイアログを表示する。
                if( folderBrowser.ShowDialog( this ) == DialogResult.OK )
                {
                    // OK押下なら、選択されたフォルダを曲読み込みフォルダリストに追加して再表示する。
                    var vpath = new VariablePath( folderBrowser.SelectedPath );
                    this.listViewフォルダ一覧.Items.Add( new ListViewItem( $"{vpath.変数なしパス}" ) );
                    this.listViewフォルダ一覧.Refresh();

                    this._リストに変更がある = true;
                }
            }
        }

        private void button削除_Click( object sender, EventArgs e )
        {
            // 選択されたフォルダを曲読み込みフォルダリストから削除して再表示する。

            foreach( int? selectedIndex in this.listViewフォルダ一覧.SelectedIndices )
            {
                if( selectedIndex.HasValue )
                    this.listViewフォルダ一覧.Items.RemoveAt( selectedIndex.Value );
            }

            this.listViewフォルダ一覧.Refresh();

            this._リストに変更がある = true;
        }



        // ローカル


        private bool _リストに変更がある = false;
    }
}
