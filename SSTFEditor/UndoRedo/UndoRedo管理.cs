using System;
using System.Collections.Generic;
using System.Text;

namespace SSTFEditor.UndoRedo
{
    class UndoRedo管理
    {
        public static bool UndoRedoした直後である { get; set; } = false;

        public int Undo可能な回数 => this._ルート.Undo可能な回数;

        public int Redo可能な回数 => ( this.現在の総ノード数 - this.Undo可能な回数 );

        public int 現在の総ノード数 => this._ルート.現在の総セル数;


        public UndoRedo管理()
        {
            this._現在のセル = this._ルート;
        }

        public セルBase Redoするセルを取得して返す()
        {
            this._現在のセル = this._ルート;

            if( 0 >= this._ルート.Redo可能な回数 )
                return null;

            this._ルート.次にセルが追加される位置0to++;

            return this._ルート.セルs[ this._ルート.次にセルが追加される位置0to - 1 ];
        }

        public セルBase Undoするセルを取得して返す()
        {
            this._現在のセル = this._ルート;

            if( 0 >= this._ルート.Undo可能な回数 )
                return null;

            this._ルート.次にセルが追加される位置0to--;

            return this._ルート.セルs[ this._ルート.次にセルが追加される位置0to ];
        }

        public セルBase Undoするセルを取得して返す_見るだけ()
        {
            this._現在のセル = this._ルート;

            if( 0 >= this._ルート.Undo可能な回数 )
                return null;

            return this._ルート.セルs[ this._ルート.次にセルが追加される位置0to - 1 ];
        }

        public void セルを追加する( セルBase 単独セル )
        {
            // 追加するセルの後方にあるセルをすべて削除する。
            int index = this._現在のセル.次にセルが追加される位置0to;
            int count = this._現在のセル.現在の総セル数 - this._現在のセル.次にセルが追加される位置0to;
            if( 0 < count )
                this._現在のセル.セルs.RemoveRange( index, count );

            // セルを追加する。
            this._現在のセル.セルs.Add( 単独セル );
            this._現在のセル.次にセルが追加される位置0to++;
        }

        public void トランザクション記録を開始する()
        {
            // 追加するセルの後方にあるセルをすべて削除する。
            int index = this._現在のセル.次にセルが追加される位置0to;
            int count = this._現在のセル.現在の総セル数 - this._現在のセル.次にセルが追加される位置0to;
            if( 0 < count )
                this._現在のセル.セルs.RemoveRange( index, count );

            // リストセルを追加して開く。
            var セルリスト = new Cセルリスト( this._現在のセル );  // 現在のセルが親セル。
            this._現在のセル.セルs.Add( セルリスト );
            this._現在のセル.次にセルが追加される位置0to++;
            this._現在のセル = セルリスト;
        }

        public void トランザクション記録を終了する()
        {
            // リストセルを閉じる。
            if( null != this._現在のセル.親リスト )
            {
                var list = this._現在のセル;
                this._現在のセル = this._現在のセル.親リスト;

                if( 0 == list.セルs.Count )
                {
                    this._現在のセル.セルs.Remove( list );
                    this._現在のセル.次にセルが追加される位置0to--;
                }
            }
        }

        public void UndoRedoリストをすべて空にする()
        {
            this._ルート = new Cセルリスト( null );
            this._現在のセル = this._ルート;
        }


        private Cセルリスト _ルート = new Cセルリスト( null );

        private Cセルリスト _現在のセル = null;
    }
}
