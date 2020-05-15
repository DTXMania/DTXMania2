using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SSTFEditor.UndoRedo
{
    class Cセルリスト : セルBase
    {
        public List<セルBase> セルs { get; set; } = null;

        public int 次にセルが追加される位置0to { get; set; } = 0;

        public Cセルリスト 親リスト { get; set; } = null;

        public int Undo可能な回数 => ( this.次にセルが追加される位置0to );

        public int Redo可能な回数 => ( this.現在の総セル数 - this.Undo可能な回数 );

        public int 現在の総セル数 => this.セルs.Count;


        public Cセルリスト( Cセルリスト 親リスト )
        {
            this.親リスト = 親リスト;
            this.セルs = new List<セルBase>();
            this.次にセルが追加される位置0to = 0;
        }

        public override void Redoを実行する()
        {
            // 前から順に実行する。
            for( int i = 0; i < this.セルs.Count; i++ )
                this.セルs[ i ].Redoを実行する();
        }

        public override void Undoを実行する()
        {
            // 後ろから順に実行する。
            for( int i = this.セルs.Count - 1; i >= 0; i-- )
                this.セルs[ i ].Undoを実行する();
        }
    }
}
