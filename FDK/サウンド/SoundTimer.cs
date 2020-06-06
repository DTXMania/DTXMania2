using SharpDX.Direct2D1;
using SharpDX.WIC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDK
{
    /// <summary>
    ///     サウンドデバイスを基準にしたタイマ。
    /// </summary>
    public class SoundTimer : IDisposable
    {

        // プロパティ


        /// <summary>
        ///		コンストラクタまたはリセットの時点からの相対経過時間[sec]。
        /// </summary>
        public double 現在時刻sec
        {
            get
            {
                lock( this._スレッド間同期 )
                {
                    if( 0 < this._停止回数 )
                        return ( this._停止位置sec - this._開始位置sec );   // 一時停止中。

                    if( this._SoundDevice.TryGetTarget( out SoundDevice? device ) )
                        return ( device.GetDevicePosition() - this._開始位置sec );  // 稼働中。

                    throw new InvalidOperationException( "サウンドデバイスが無効です。" );
                }
            }
        }



        // 生成と終了


        public SoundTimer( SoundDevice device )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._SoundDevice = new WeakReference<SoundDevice>( device );
            this.リセットする();
        }

        public void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );
        }



        // 操作


        public void リセットする( double 新しい現在時刻sec = 0.0 )
        {
            lock( this._スレッド間同期 )
            {
                if( this._SoundDevice.TryGetTarget( out SoundDevice? device ) )
                {
                    this._開始位置sec = device.GetDevicePosition() - 新しい現在時刻sec;
                    this._停止回数 = 0;
                    this._停止位置sec = 0;
                }
            }
        }

        public void 一時停止する()
        {
            lock( this._スレッド間同期 )
            {
                if( this._SoundDevice.TryGetTarget( out SoundDevice? device ) )
                {
                    if( 0 == this._停止回数 )
                        this._停止位置sec = device.GetDevicePosition();

                    this._停止回数++;
                }
            }
        }

        public void 再開する()
        {
            lock( this._スレッド間同期 )
            {
                this._停止回数--;

                if( 0 == this._停止回数 )
                    this.リセットする( this._停止位置sec - this._開始位置sec );
            }
        }



        // ローカル


        private WeakReference<SoundDevice> _SoundDevice;

        private int _停止回数 = 0;

        private double _開始位置sec = 0.0;

        private double _停止位置sec = 0.0;

        private readonly object _スレッド間同期 = new object();
    }
}
