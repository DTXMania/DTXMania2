using System;
using System.Collections.Generic;
using System.Text;

namespace SSTFEditor.UndoRedo
{
    public delegate void DGRedoを実行する<T>( T 変更対象, T 変更前の値, T 変更後の値, object 任意1, object 任意2 );

    public delegate void DGUndoを実行する<T>( T 変更対象, T 変更前の値, T 変更後の値, object 任意1, object 任意2 );


    class セル<T> : セルBase
    {
        public T 変更対象;

        public T 変更前の値;

        public T 変更後の値;

        public object 任意1;      // 任意に使えるパラメータ領域

        public object 任意2;      //


        public セル( object 所有者ID, DGUndoを実行する<T> Undoアクション, DGRedoを実行する<T> Redoアクション, T 変更対象, T 変更前の値, T 変更後の値, object 任意1 = null, object 任意2 = null )
        {
            base.所有者ID = 所有者ID;
            this.undoアクション = Undoアクション;
            this.redoアクション = Redoアクション;
            this.変更対象 = 変更対象;
            this.変更前の値 = 変更前の値;
            this.変更後の値 = 変更後の値;
            this.任意1 = 任意1;
            this.任意2 = 任意2;
        }

        public override void Redoを実行する()
        {
            base.所有者ID = null;
            this.redoアクション( this.変更対象, this.変更前の値, this.変更後の値, this.任意1, this.任意2 );
        }

        public override void Undoを実行する()
        {
            base.所有者ID = null;
            this.undoアクション( this.変更対象, this.変更前の値, this.変更後の値, this.任意1, this.任意2 );
        }


        protected DGRedoを実行する<T> redoアクション;

        protected DGUndoを実行する<T> undoアクション;
    }
}
