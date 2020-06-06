using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace DTXMania2
{
    /// <summary>
    ///     <see cref="TaskMessage"/> の送受信に使用するキュー。
    /// </summary>
    class TaskMessageQueue
    {

        // 生成と終了


        public TaskMessageQueue()
        {
            this._TaskMessageList = new List<TaskMessage>();
        }



        // メッセージの送信・取得


        /// <summary>
        ///     タスクメッセージをキューに格納し、完了通知待ち用のイベントを返す。
        /// </summary>
        /// <returns>
        ///     返されるイベントは、引数で受け取ったメッセージの <see cref="TaskMessage.完了通知"/> と同一である。
        /// </returns>
        public ManualResetEventSlim Post( TaskMessage msg )
        {
            lock( this._TaskMessageList )
            {
                this._TaskMessageList.Add( msg );
                return msg.完了通知;
            }
        }

        /// <summary>
        ///     宛先に対するメッセージを1つ取得して返す。
        /// </summary>
        /// <param name="宛先">取得するメッセージの宛先。</param>
        /// <returns>取得できたメッセージの列挙。1つも無ければ空の列挙を返す。</returns>
        public IEnumerable<TaskMessage> Get( TaskMessage.タスク名 宛先 )
        {
            lock( this._TaskMessageList )
            {
                var result = new List<TaskMessage>();
                var resultIndexes = new List<int>();

                // 指定された宛先のメッセージをリストから抽出。
                for( int i = 0; i < this._TaskMessageList.Count; i++ )
                {
                    var msg = this._TaskMessageList[ i ];
                    if( msg.宛先 == 宛先 )
                    {
                        result.Add( msg );
                        resultIndexes.Add( i );
                    }
                }

                // 抽出されたメッセージをリストから削除。
                foreach( var index in resultIndexes )
                    this._TaskMessageList.RemoveAt( index );

                // 結果を返す。
                return result;
            }
        }



        // ローカル


        private readonly List<TaskMessage> _TaskMessageList;
    }
}
