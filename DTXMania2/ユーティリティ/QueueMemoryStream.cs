using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace DTXMania2
{
    /// <summary>
    ///     読み込みと書き込みを別々のスレッドから行えるキュー。
    /// </summary>
    public class QueueMemoryStream : MemoryTributary
    {
        public long 読み出し位置
        {
            get
            {
                lock( this._Stream排他 )
                    return this._読み出し位置;
            }
            set
            {
                lock( this._Stream排他 )
                    this._読み出し位置 = Math.Min( Math.Max( value, 0 ), this._読み出し位置 + this.読み出し可能サイズ );
            }
        }
        public long 書き込み位置
        {
            get
            {
                lock( this._Stream排他 )
                    return this._書き込み位置;
            }
            protected set
            {
                lock( this._Stream排他 )
                    this._書き込み位置 = value;
            }
        }
        public long 読み出し可能サイズ
        {
            get
            {
                lock( this._Stream排他 )
                    return ( this._書き込み位置 - this._読み出し位置 );
            }
        }

        public QueueMemoryStream()
        {
        }

        /// <summary>
        ///     ストリームの現在の読み込み位置からデータを読み出す。
        ///     データが足りないなら、そろうまでブロックする。
        /// </summary>
        /// <param name="buffer">読み込んだデータを格納する配列。</param>
        /// <param name="offset">格納を開始する <paramref name="buffer"/> の位置。0 から始まるインデックス値。</param>
        /// <param name="count">読み込むバイト数。</param>
        /// <returns>格納したバイト数。</returns>
        public override int Read( byte[] buffer, int offset, int count )
        {
            lock( this._Stream排他 )
            {
                // キャンセル済みなら何もしない。
                if( this._Canceled )
                    return default;

                // データが足りないなら、そろうまでブロックする。
                while( this.読み出し可能サイズ < count )
                {
                    // lock を解放してウェイトに入る。戻ってきたときには lock が再取得されている。
                    Monitor.Wait( this._Stream排他 );

                    // ブロックが解除されたとき、既にキャンセル済みだったら何もせず戻る。
                    if( this._Canceled )
                        return default;
                }

                this.Position = this.読み出し位置;

                int num = base.Read( buffer, offset, count );

                this.読み出し位置 = this.Position;

                // キューの中身が変化したことを、Monitor.Wait してるスレッドへ通知する。
                Monitor.PulseAll( this._Stream排他 );

                return num;
            }
        }

        /// <summary>
        ///     ストリームの現在の読み込み位置からデータを読み出す。
        ///     データが足りないときにブロックするか否かは引数で指定できる。
        /// </summary>
        /// <param name="buffer">読み込んだデータを格納する配列。</param>
        /// <param name="offset">格納を開始する <paramref name="buffer"/> の位置。0 から始まるインデックス値。</param>
        /// <param name="count">読み込むバイト数。</param>
        /// <param name="データが足りないならブロックする">読みだせるデータが足りない場合の挙動。true ならデータがそろうまでブロックし、false ならあるだけ全部読みだしてすぐに戻る。</param>
        /// <returns>格納したバイト数。</returns>
        public int Read( byte[] buffer, int offset, int count, bool データが足りないならブロックする )
        {
            if( データが足りないならブロックする )
                return this.Read( buffer, offset, count );

            lock( this._Stream排他 )
            {
                // キャンセル済みなら何もしない。
                if( this._Canceled )
                    return default;

                if( this.読み出し可能サイズ < count )
                {
                    // データが足りないなら、あるだけ全部読みだす。
                    count = (int) this.読み出し可能サイズ;
                }

                this.Position = this.読み出し位置;

                int num = base.Read( buffer, offset, count );

                this.読み出し位置 = this.Position;

                // キューの中身が変化したことを、Monitor.Wait してるスレッドへ通知する。
                Monitor.PulseAll( this._Stream排他 );

                return num;
            }
        }

        /// <summary>
        ///     ストリームの現在の書き込み位置からデータを書き込む。
        /// </summary>
        /// <param name="buffer">ストリームに書き込むデータを持つ配列。</param>
        /// <param name="offset">書き込みを開始する <paramref name="buffer"/> の位置。0 から始まるインデックス値。</param>
        /// <param name="count">書き込むバイト数。</param>
        public override void Write( byte[] buffer, int offset, int count )
        {
            lock( this._Stream排他 )
            {
                // キャンセル済みなら何もしない。
                if( this._Canceled )
                    return;

                this.Position = this.書き込み位置;

                base.Write( buffer, offset, count );

                this.書き込み位置 = this.Position;

                // キューの中身が変化したことを、Monitor.Wait してるスレッドへ通知する。
                Monitor.PulseAll( this._Stream排他 );
            }
        }

        /// <summary>
        ///     <see cref="Read"/> あるいは <see cref="Write(byte[], int, int)"/> でブロックしているスレッドにキャンセルを通知してブロックを解除する。
        ///     その後、Read も Write も無効とする。
        /// </summary>
        public void Cancel()
        {
            this._Canceled = true;

            lock( this._Stream排他 )
            {
                // Monitor.Wait してるスレッドへ通知する。
                Monitor.PulseAll( this._Stream排他 );
            }
        }


        private long _読み出し位置 = 0;

        private long _書き込み位置 = 0;

        private object _Stream排他 = new object();

        private bool _Canceled = false;
    }
}
