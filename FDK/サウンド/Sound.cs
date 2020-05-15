using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CSCore;

namespace FDK
{
    public class Sound : ISampleSource
    {

        // プロパティ


        public bool 再生中である
            => this._DeviceRef.TryGetTarget( out SoundDevice? device ) && device.Mixer.Contains( this );
        public bool 再生中ではない
            => !( this.再生中である );

        /// <summary>
        ///		シークが可能なら true。
        /// </summary>
        public bool CanSeek
            => this._BaseSampleSource?.CanSeek ?? false;

        /// <summary>
        ///		このサウンドのフォーマット。
        /// </summary>
        public WaveFormat WaveFormat
            => this._BaseSampleSource.WaveFormat;

        /// <remarks>
        ///		１つの <see cref="SampleSource"/>を複数の<see cref="Sound"/>インスタンスで共有できるように、
        ///		このプロパティは<see cref="Sound"/>インスタンスごとに独立して管理する。
        /// </remarks>
        public long Position
        {
            get => this._Position;
            set => this._Position = Math.Clamp( value, min: 0, max: this.Length );
        }

        public long Length
            => this._BaseSampleSource?.Length ?? 0;

        /// <summary>
        ///		音量。0.0(無音)～1.0(原音)～...上限なし
        /// </summary>
        /// <remarks>
        ///		このクラスではなく、<see cref="Mixer"/>クラスから参照して使用する。
        /// </remarks>
        public float Volume
        {
            get => this._Volume;
            set => this._Volume = Math.Max( value, 0 );
        }

        public bool IsLoop { get; set; } = false;

        /// <summary>
        ///     一時停止中なら true
        /// </summary>
        public bool IsPaused { get; set; } = false;



        // 生成と終了


        public Sound( SoundDevice device, ISampleSource sampleSource )
            : this( device )
        {
            this._BaseSampleSource = sampleSource;
        }

        protected Sound( SoundDevice device )
        {
            if( device is null )
                throw new ArgumentNullException();

            this._BaseSampleSource = null!;
            this._DeviceRef = new WeakReference<SoundDevice>( device );
        }

        public void Dispose()
        {
            this.Stop();
            //this._BaseSampleSource?.Dispose();	Dispose は行わない。（SampleSource は複数の Sound で共有されている可能性があるため。）
        }



        // 再生制御


        public void Play( long 再生開始位置frame = 0, bool ループ再生する = false )
        {
            if( this._BaseSampleSource is null )
                throw new InvalidOperationException( "サンプルソースが null です。" );

            if( this._DeviceRef.TryGetTarget( out SoundDevice? device ) )
            {
                // BaseSampleSource の位置を、再生開始位置へ移動。
                if( this._BaseSampleSource.CanSeek )
                {
                    this._Position = 再生開始位置frame * this.WaveFormat.Channels;
                    //this._BaseSampleSource.Position = this._Position;		--> ここではまだ設定しない。Read() で設定する。
                }
                else
                {
                    //Log.WARNING( $"このサンプルソースの再生位置を変更することができません。既定の位置から再生を開始します。" );
                    this._Position = 0;
                }

                this.IsLoop = ループ再生する;

                // ミキサーに追加（＝再生開始）。
                device.Mixer.AddSound( this );
            }
        }

        public void Play( double 再生開始位置sec, bool ループ再生する = false )
            => this.Play( this.秒ToFrame( 再生開始位置sec ), ループ再生する );

        public void Stop()
        {
            if( ( null != this._DeviceRef ) && this._DeviceRef.TryGetTarget( out SoundDevice? device ) )
            {
                device.Mixer.RemoveSound( this );
            }
        }

        public void Pause()
        {
            if( this.再生中ではない )
                return;

            // 停止。
            this.Stop();
            this.IsPaused = true;
        }

        public void Resume()
        {
            if( this.IsPaused )
            {
                // 停止位置から再生。
                this.Play(
                    this.Position / this.WaveFormat.Channels,   // position → frame
                    this.IsLoop );

                this.IsPaused = false;
            }
        }



        // 出力


        public int Read( float[] buffer, int offset, int count )
        {
            // ソースが未設定(null)なら即再生終了
            if( this._BaseSampleSource is null )
                return 0;

            if( this._BaseSampleSource.Length <= this._Position )
            {
                // (A) 最後まで再生済んだ、または次のストリームデータが届いていない場合

                if( this.IsLoop )
                {
                    // (A-a) ループする場合

                    this._Position = 0;                     // 再生位置を先頭に戻す。
                    Array.Clear( buffer, offset, count );   // 全部ゼロで埋めて返す。
                    return count;
                }
                else
                {
                    // (A-b) ループしない場合

                    return 0;   // 再生終了。
                }
            }
            else
            {
                // (B) 読み込みできるストリームデータがある場合

                // １つの BaseSampleSource を複数の Sound で共有するために、Position は Sound ごとに管理している。
                this._BaseSampleSource.Position = this._Position;
                var readCount = this._BaseSampleSource.Read( buffer, offset, count );   // 読み込み。
                this._Position = this._BaseSampleSource.Position;

                if( 0 == readCount && this.IsLoop )
                    this._Position = 0; // 再生をループ。

                return readCount;
            }
        }



        // その他


        public long 秒ToFrame( double 時間sec )
        {
            if( this._BaseSampleSource is null )
                return 0;

            return (long) ( 時間sec * this._BaseSampleSource.WaveFormat.SampleRate + 0.5 ); // +0.5 で四捨五入ができる
        }

        public double FrameTo秒( long 時間frame )
        {
            if( this._BaseSampleSource is null )
                return 0;

            return (double) 時間frame / this._BaseSampleSource.WaveFormat.SampleRate;
        }



        // ローカル


        private WeakReference<SoundDevice> _DeviceRef;

        private ISampleSource _BaseSampleSource;

        private long _Position = 0;

        private float _Volume = 1.0f;
    }
}
