using System;
using System.Collections.Generic;
using System.Text;

namespace SSTFEditor.UndoRedo
{
    class セルBase
    {
        public bool 所有権がある( object 所有者候補 )
            => ( this.所有者ID == 所有者候補 );

        public void 所有権を放棄する( object 現所有者 )
        {
            if( this.所有者ID == 現所有者 )
                this.所有者ID = null;
        }

        public virtual void Redoを実行する()
        {
        }

        public virtual void Undoを実行する()
        {
        }


        protected object 所有者ID = null;
    }
}
