﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FDK
{
    /// <summary>
    ///		一定間隔ごとの進行処理を実現するクラス。
    /// </summary>
    /// <remarks>
    ///		<para> 例えば、
    ///			<code>
    ///			  var cf = new C定間隔進行();
    ///			  cf.経過時間の分だけ進行する( 400, 定間隔処理 );
    ///			</code>
    ///			と記述した場合、400ms ごとに 定間隔処理() が実行されるようになる。
    ///		</para>
    ///		<para>
    ///			ただし、この動作は「経過時間の分だけ進行する()」メソッドを呼び出した時に、定間隔処理() を「必要な回数だけ反復実行」する仕様である。
    ///			例えば、先述の例において、メソッドの呼び出し時点で、前回の同メソッド（またはコンストラクタ）の呼び出しから 900ms が経過していたとすると、
    ///			定間隔処理() は 900÷400 = 2回実行され、残りの 100ms が経過時間として次回に繰り越される。
    ///			（一回の処理に時間がかかった場合にも定間隔処理が並列実行されるわけではない。）
    ///		</para>
    ///		<para>
    ///			なお、定間隔処理にラムダ式を使用する場合は、キャプチャ変数のライフサイクル（実行される時点でまだ存在しているか否か）に留意し、弱い参照 の利用も検討すること。
    ///		</para>
    /// </remarks>
    public class 定間隔進行
    {
        /// <summary>
        ///		コンストラクタ。
        ///		初期化し、同時に間隔の計測も開始する。
        /// </summary>
        public 定間隔進行()
        {
            this.経過時間の計測を開始する();
        }

        public void 経過時間の計測を開始する()
        {
            lock( this._スレッド間同期 )
            {
                this._タイマ.リセットする();
            }
        }

        public void 経過時間の計測を一時停止する()
        {
            lock( this._スレッド間同期 )
            {
                this._タイマ.一時停止する();
            }
        }

        public void 経過時間の計測を再開する()
        {
            lock( this._スレッド間同期 )
            {
                this._タイマ.再開する();
            }
        }

        public void 経過時間の分だけ進行する( long 間隔ms, Action 定間隔処理 )
        {
            lock( this._スレッド間同期 )
            {
                // 現在時刻を取得する。
                this._タイマ.現在のカウントをキャプチャする();
                long 現在時刻ms = this._タイマ.現在のキャプチャカウント100ns / 10_000;

                // 初めての進行の場合、前回時刻を初期化する。
                if( this._前回の進行時刻ms == QPCTimer.未使用 )
                    this._前回の進行時刻ms = 現在時刻ms;

                // （ないと思うが）タイマが一回りしてしまった時のための保護。正常動作を保証するものではない。
                if( this._前回の進行時刻ms > 現在時刻ms )
                    this._前回の進行時刻ms = 現在時刻ms;

                // 経過時間における回数だけ、処理を実行する。
                while( ( 現在時刻ms - this._前回の進行時刻ms ) >= 間隔ms )
                {
                    定間隔処理();
                    this._前回の進行時刻ms += 間隔ms;
                }
            }
        }


        private long _前回の進行時刻ms = QPCTimer.未使用;

        private readonly QPCTimer _タイマ = new QPCTimer();

        private readonly object _スレッド間同期 = new object();
    }
}
