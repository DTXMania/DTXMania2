using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DTXMania2
{
    /// <summary>
    ///		パフォーマンスカウンタを使用した高精度タイマ。
    /// </summary>
    /// <remarks>
    ///		以下の2種類の使い方を想定する。
    ///		(A) 正確に同一の時刻を複数の処理で共有できるように、現在時刻をキャプチャしてから取得する方法。
    ///		   1. 最初に「現在のカウントをキャプチャする()」を呼び出し、その時点での時刻を内部に保存する。
    ///		   2. キャプチャされたカウントを、「現在のキャプチャカウントを……取得する()」を呼び出して、希望する単位で取得する。
    ///		     （次に1.を行うまで、2.はずっと同じ時刻を返し続ける。）
    ///		(B) 常に現時刻（メソッド呼び出し時点の時刻）を取得する方法。
    ///		   a. 「現在のリアルタイムカウントを……取得する()」を呼び出して、希望する単位で取得する。
    ///		   または、
    ///		   b. 「生カウントを取得する()」を呼び出して、生カウンタを取得する。
    ///	
    ///		時刻の単位としては、[カウント], [秒], [100ナノ秒] を用意する。
    /// 
    ///		用語：
    ///		"カウント" …………………… タイマインスタンスの生成時（または前回のリセット時）から「前回キャプチャされた時点」までの、パフォーマンスカウンタの差分（相対値）。
    ///		"リアルタイムカウント" …… タイマインスタンスの生成時（または前回のリセット時）から「現時点」までの、パフォーマンスカウンタの差分（相対値）。
    ///		"生カウント" ………………… パフォーマンスカウンタの生の値。::QueryPerformanceCounter() で取得できる値に等しい。システム依存の絶対値。
    /// </remarks>
    class QPCTimer
    {
        /// <summary>
        ///		カウントが無効であることを示す定数。
        /// </summary>
        public const long 未使用 = -1;


        public static long 周波数
            => Stopwatch.Frequency;

        public static long 生カウント
            => Stopwatch.GetTimestamp();


        public static double 生カウント相対値を秒へ変換して返す( long 生カウント相対値 )
            => (double) 生カウント相対値 / QPCTimer.周波数;

        public static long 秒をカウントに変換して返す( double 秒 )
            => (long) ( 秒 * QPCTimer.周波数 );


        public long 現在のキャプチャカウント
        {
            get
            {
                lock( this._スレッド間同期 )
                {
                    if( 0 != this._一時停止回数 )
                    {
                        // 停止中。
                        return ( this._稼働中に一時停止した時点のキャプチャカウント - this._前回リセットした時点の生カウント );
                    }
                    else
                    {
                        // 稼働中。
                        return ( this._最後にキャプチャされたカウント - this._前回リセットした時点の生カウント );
                    }
                }
            }
        }

        public long 現在のキャプチャカウント100ns
            => (long) ( this.現在のキャプチャカウントsec * 10_000_000 + 0.5 );

        public double 現在のキャプチャカウントsec
            => (double) this.現在のキャプチャカウント / QPCTimer.周波数;

        public long 現在のリアルタイムカウント
        {
            get
            {
                lock( this._スレッド間同期 )
                {
                    if( 0 != this._一時停止回数 )
                    {
                        // 停止中。
                        return ( this._稼働中に一時停止した時点のキャプチャカウント - this._前回リセットした時点の生カウント );
                    }
                    else
                    {
                        // 稼働中。
                        return ( QPCTimer.生カウント - this._前回リセットした時点の生カウント );
                    }
                }
            }
        }

        public long 現在のリアルタイムカウント100ns
            => (long)( this.現在のリアルタイムカウントsec * 10_000_000 + 0.5 );

        public double 現在のリアルタイムカウントsec
            => (double) this.現在のリアルタイムカウント / QPCTimer.周波数;

        public long 前回リセットした時点の生カウント
        {
            get
            {
                lock( this._スレッド間同期 )
                {
                    return this._前回リセットした時点の生カウント;
                }
            }
        }

        public bool 停止中である
        {
            get
            {
                lock( this._スレッド間同期 )
                {
                    return ( 0 != this._一時停止回数 );
                }
            }
        }

        public bool 稼働中である
            => !( this.停止中である );


        public QPCTimer()
        {
            var 現在の生カウント = QPCTimer.生カウント;

            this._前回リセットした時点の生カウント = 現在の生カウント;
            this._最後にキャプチャされたカウント = 現在の生カウント;
            this._稼働中に一時停止した時点のキャプチャカウント = 現在の生カウント;

            this._一時停止回数 = 0;
        }

        public long 現在のカウントをキャプチャする()
        {
            lock( this._スレッド間同期 )
            {
                this._最後にキャプチャされたカウント = QPCTimer.生カウント;
                return this._最後にキャプチャされたカウント;
            }
        }

        public void リセットする( long 新しいカウント = 0 )
        {
            lock( this._スレッド間同期 )
            {
                this._前回リセットした時点の生カウント = QPCTimer.生カウント - 新しいカウント;
            }
        }

        public void 一時停止する()
        {
            lock( this._スレッド間同期 )
            {
                if( this.稼働中である )
                {
                    this._稼働中に一時停止した時点のキャプチャカウント = QPCTimer.生カウント;
                }

                this._一時停止回数++;
            }
        }

        public void 再開する()
        {
            lock( this._スレッド間同期 )
            {
                if( this.停止中である )
                {
                    this._一時停止回数--;

                    if( 0 >= this._一時停止回数 )
                    {
                        this._最後にキャプチャされたカウント = QPCTimer.生カウント;
                        this._前回リセットした時点の生カウント += this._最後にキャプチャされたカウント - this._稼働中に一時停止した時点のキャプチャカウント;
                    }
                }
            }
        }


        private long _前回リセットした時点の生カウント = 0;
        private long _最後にキャプチャされたカウント = 0;
        private long _稼働中に一時停止した時点のキャプチャカウント = 0;
        private int _一時停止回数 = 0;
        private readonly object _スレッド間同期 = new object();
    }
}
