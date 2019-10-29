using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace DTXMania2
{
    class TaskMessageQueue
    {

        // メッセージの送信・取得


        /// <summary>
        ///     タスクメッセージを投稿し、完了通知待ち用のイベントを返す。
        /// </summary>
        /// <returns>
        ///     返されるイベントは、<see cref="TaskMessage.完了通知"/> と同一のインスタンスである。
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
        /// <returns>取得できたメッセージの列挙。1つも無ければ空。</returns>
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


        private readonly List<TaskMessage> _TaskMessageList = new List<TaskMessage>();
    }
}
