using System;
using System.Collections.Generic;
using System.Text;

namespace SSTFEditor
{
    class クリップボード
    {
        public int アイテム数 => this.アイテムリスト.Count;


        public クリップボード( メインフォーム form )
        {
            this.Form = form;
        }

        public void 現在選択されているチップをボードにコピーする()
        {
            this.アイテムリスト.Clear();

            foreach( 描画用チップ chip in this.Form.譜面.SSTFormatScore.チップリスト )
            {
                if( chip.選択が確定している )
                {
                    this.アイテムリスト.Add(
                        new アイテム() { チップ = new 描画用チップ( chip ) } );
                }
            }
        }

        public void チップを指定位置から貼り付ける( int 貼り付け先頭の譜面内絶対位置grid )
        {
            if( 0 == this.アイテム数 )
                return;

            try
            {
                this.Form.UndoRedo管理.トランザクション記録を開始する();

                // すべてのセルについて、チップ位置を、ボード内でもっとも位置が前にあるセルを 0grid とした相対値に変換する。
                int 最小値grid = this.アイテムリスト[ 0 ].チップ.譜面内絶対位置grid;
                foreach( var cell in this.アイテムリスト )
                {
                    if( cell.チップ.譜面内絶対位置grid < 最小値grid )
                        最小値grid = cell.チップ.譜面内絶対位置grid;
                }
                foreach( var cell in this.アイテムリスト )
                    cell.チップ.譜面内絶対位置grid -= 最小値grid;

                // すべてのセルについて、チップ位置を、実際に貼り付ける位置に変換する。
                foreach( var cell in this.アイテムリスト )
                    cell.チップ.譜面内絶対位置grid += 貼り付け先頭の譜面内絶対位置grid;

                // チップを譜面に貼り付ける。
                foreach( var cell in this.アイテムリスト )
                {
                    this.Form.譜面.チップを配置または置換する(
                        this.Form.譜面.dicチップ編集レーン対応表[ cell.チップ.チップ種別 ],
                        cell.チップ.チップ種別,
                        cell.チップ.譜面内絶対位置grid,
                        cell.チップ.チップ内文字列,
                        cell.チップ.音量,
                        cell.チップ.BPM,
                        選択確定中: true );
                }
            }
            finally
            {
                this.Form.UndoRedo管理.トランザクション記録を終了する();

                this.Form.UndoRedo用GUIのEnabledを設定する();
                this.Form.選択チップの有無に応じて編集用GUIのEnabledを設定する();
                this.Form.譜面をリフレッシュする();
                this.Form.未保存である = true;
            }
        }


        protected class アイテム
        {
            public bool 貼り付け済み = false;
            public int グループID = 0;
            public 描画用チップ チップ = null;
        }

        protected メインフォーム Form;

        protected readonly List<アイテム> アイテムリスト = new List<アイテム>();
    }
}
