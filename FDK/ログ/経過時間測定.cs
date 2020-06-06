using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace FDK
{
    public class 経過時間測定
    {
        public ConcurrentDictionary<string, double> スタック { get; }

        public double 現在のリアルタムカウントsec => this._Timer.現在のリアルタイムカウントsec;


        public 経過時間測定()
        {
            this.スタック = new ConcurrentDictionary<string, double>();
            this.リセット();
        }

        public void リセット()
        {
            lock( this._lock )
            {
                this.スタック.Clear();
                this._Timer.リセットする();
                this.経過ポイント( "開始" );
            }
        }
        
        public void 経過ポイント( string ポイント名 )
        {
            lock( this._lock )
            {
                this.スタック.TryAdd( ポイント名, (float) ( this._Timer.現在のリアルタイムカウントsec ) );
            }
        }

        public void 表示()
        {
            lock( this._lock )
            {
                double 直前の時刻 = 0.0;
                var sortedDic = this.スタック.OrderBy( ( kvp ) => ( kvp.Value ) );
                
                for( int i = 0; i < sortedDic.Count(); i++ )
                {
                    var kvp = sortedDic.ElementAt( i );
                    Debug.Write( $"{kvp.Key}," );
                }
                Debug.WriteLine( "区間計(ms)" );

                for( int i = 0; i < sortedDic.Count(); i++ )
                {
                    var kvp = sortedDic.ElementAt( i );
                    Debug.Write( $"+{1000 * ( kvp.Value - 直前の時刻 ):0.00000}, " );

                    直前の時刻 = kvp.Value;
                }
                Debug.WriteLine( $"{1000 * sortedDic.Last().Value:0.00000}" );
            }
        }


        private QPCTimer _Timer = new QPCTimer();
        
        private readonly object _lock = new object();
    }
}
