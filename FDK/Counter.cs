using System;
using System.Diagnostics;

namespace FDK
{
    /// <summary>
    ///		ある int 型整数を、開始値から終了値まで、一定間隔で単純増加させるカウンタ。
    ///		終了値に達したら、それ以降は終了値を維持する（不変）。
    /// </summary>
    public class Counter
    {
        /// <summary>
        ///		カウンタの開始値。
        ///		常に終了値以下。
        /// </summary>
        public int 開始値
        {
            get
            {
                lock( this._スレッド間同期 )
                {
                    return this._開始値;
                }
            }
        }

        /// <summary>
        ///		カウンタの終了値。
        ///		常に開始値以上。
        /// </summary>
        public int 終了値
        {
            get
            {
                lock( this._スレッド間同期 )
                {
                    return this._終了値;
                }
            }
        }

        /// <summary>
        ///		カウンタを進行し、現在の値を返す。
        /// </summary>
        public int 現在値
        {
            get
            {
                lock( this._スレッド間同期 )
                {
                    this.進行する();
                    return this._現在値;
                }
            }
        }

        /// <summary>
        ///		カウンタを進行し、現在の値を 0.0～1.0 の割合値に変換して返す。
        /// </summary>
        /// <return>
        ///		0.0 (開始値) ～ 1.0 (終了値)
        /// </return>
        public float 現在値の割合
        {
            get
            {
                lock( this._スレッド間同期 )
                {
                    if( this._開始値 != this._終了値 )
                    {
                        this.進行する();
                        return (float) ( this._現在値 - this._開始値 ) / (float) ( this._終了値 - this._開始値 );
                    }
                    else
                    {
                        return 1.0f;    // 開始値 ＝ 終了値 なら常に 1.0 とする。
                    }
                }
            }
        }

        /// <summary>
        ///		カウンタを進行し、その結果、カウンタの進行がまだ動作中なら true を返す。
        ///		（終了値に達しているかどうかは関係なく、開始前or一時停止中であるならfalseを返す。）
        /// </summary>
        public bool 動作中である
        {
            get
            {
                lock( this._スレッド間同期 )
                {
                    this.進行する();        // 終了してるかどうか判定する前に、溜まってる進行を全部消化する。
                    return this._動作中;
                }
            }
        }

        /// <summary>
        ///		カウンタの進行が一時停止されているなら true を返す。
        ///		（終了値に達しているかどうかは関係なく、開始前or一時停止中であるならtrueを返す。）
        /// </summary>
        public bool 停止中である => !this.動作中である;

        /// <summary>
        ///		カウンタを進行し、その結果、現在値が終了値に達していたら true を返す。
        /// </summary>
        public bool 終了値に達した
        {
            get
            {
                lock( this._スレッド間同期 )
                {
                    this.進行する();
                    return ( this._現在値 >= this._終了値 );
                }
            }
        }

        /// <summary>
        ///		カウンタを進行し、その結果、まだ現在値が終了値に達していないら true を返す。
        /// </summary>
        public bool 終了値に達していない => !this.終了値に達した;


        /// <summary>
        ///		コンストラクタ。
        ///		初期化のみ行い、カウンタは開始しない。
        /// </summary>
        public Counter()
        {
            this._間隔ms = QPCTimer.未使用;
            this._定間隔進行 = null!;
            this._開始値 = 0;
            this._終了値 = 0;
            this._現在値 = 0;
            this._動作中 = false;
        }

        /// <summary>
        ///		コンストラクタ。
        ///		初期化し、同時にカウンタを開始する。
        /// </summary>
        public Counter( int 最初の値, int 最後の値, long 値をひとつ増加させるのにかける時間ms = 1000 )
            : this()
        {
            this.開始する( 最初の値, 最後の値, 値をひとつ増加させるのにかける時間ms );
        }

        /// <summary>
        ///		カウンタの現在値を最初の値に設定し、カウンタの進行を開始する。
        /// </summary>
        public void 開始する( int 最初の値, int 最後の値, long 値をひとつ増加させるのにかける時間ms = 1000 )
        {
            lock( this._スレッド間同期 )
            {
                this._間隔ms = 値をひとつ増加させるのにかける時間ms;
                this._定間隔進行 = new 定間隔進行(); // 同時に開始する。
                this._開始値 = 最初の値;
                this._終了値 = Math.Max( 最初の値, 最後の値 );    // 逆転しないことを保証。
                this._現在値 = 最初の値;
                this._動作中 = true;
            }
        }

        /// <remarks>
        ///		一時停止中は、カウンタを進行させても、現在値が進まない。
        /// </remarks>
        public void 一時停止する()
        {
            lock( this._スレッド間同期 )
            {
                this._定間隔進行.経過時間の計測を一時停止する();
                this._動作中 = false;
            }
        }

        /// <summary>
        ///		一時停止しているカウンタを、再び進行できるようにする。
        /// </summary>
        public void 再開する()
        {
            lock( this._スレッド間同期 )
            {
                this._定間隔進行.経過時間の計測を再開する();
                this._動作中 = true;
            }
        }

        /// <summary>
        ///		前回のこのメソッドの呼び出しからの経過時間をもとに、必要なだけ現在値を増加させる。
        ///		カウント値が終了値に達している場合は、それ以上増加しない（終了値を維持する）。
        /// </summary>
        public void 進行する()
        {
            if( this._間隔ms == QPCTimer.未使用 )
                return; // 開始されていないなら無視。

            lock( this._スレッド間同期 )
            {
                this._定間隔進行?.経過時間の分だけ進行する( this._間隔ms, () => {

                    if( this._動作中 )
                    {
                        if( this._現在値 < this._終了値 )
                        {
                            this._現在値++;
                        }
                        else
                        {
                            // 終了値以降、現在値は不変。
                            this._現在値 = this._終了値;
                        }
                    }

                } );
            }
        }


        private int _開始値 = 0;
        
        private int _終了値 = 0;
        
        private int _現在値 = 0;
        
        private bool _動作中 = false;
        
        private long _間隔ms = QPCTimer.未使用;

        private 定間隔進行 _定間隔進行;
        
        private readonly object _スレッド間同期 = new object();
    }
}
