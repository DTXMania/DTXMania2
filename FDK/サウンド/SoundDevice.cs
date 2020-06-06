using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using CSCore;
using CSCore.CoreAudioAPI;
using CSCore.SoundOut;
using CSCore.Win32;

namespace FDK
{
    /// <summary>
    ///     WASAPI サウンドデバイス。
    /// </summary>
    public class SoundDevice : IDisposable
    {

        // プロパティ


        public PlaybackState レンダリング状態 => this._レンダリング状態;

        public double 再生遅延sec { get; protected set; }

        /// <summary>
        ///		デバイスのレンダリングフォーマット。
        /// </summary>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        ///		レンダリングボリューム。
        ///		0.0 (0%) ～ 1.0 (100%) 。
        /// </summary>
        public float 音量
        {
            get
                => this.Mixer.Volume;
            set
            {
                if( ( 0.0f > value ) || ( 1.0f < value ) )
                    throw new ArgumentOutOfRangeException( $"音量の値が、範囲(0～1)を超えています。[{value}]" );

                this.Mixer.Volume = value;
            }
        }

        /// <summary>
        ///		ミキサー。
        /// </summary>
        internal Mixer Mixer { get; private set; }



        // 生成と終了


        /// <summary>
        ///		デバイスを初期化し、レンダリングを開始する。
        /// </summary>
        public SoundDevice( AudioClientShareMode 共有モード, double バッファサイズsec = 0.010, WaveFormat? 希望フォーマット = null )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._共有モード = 共有モード;
            this.再生遅延sec = バッファサイズsec;
            this._レンダリング状態 = PlaybackState.Stopped;

            lock( this._スレッド間同期 )
            {
                #region " MMDevice を生成し、AudioClient を取得する。"
                //----------------
                this._MMDevice = MMDeviceEnumerator.DefaultAudioEndpoint(
                    DataFlow.Render,    // 方向 ... 書き込み
                    Role.Console );     // 用途 ... ゲーム、システム通知音、音声命令

                Log.Info( $"Device: {this._MMDevice.FriendlyName}" );

                this._AudioClient = AudioClient.FromMMDevice( this._MMDevice );
                //----------------
                #endregion

                #region " フォーマットを決定する。"
                //----------------
                this.WaveFormat = this._適切なフォーマットを調べて返す( 希望フォーマット ) ??
                    throw new NotSupportedException( "サポート可能な WaveFormat が見つかりませんでした。" );

                Log.Info( $"WaveFormat: Channels={this.WaveFormat.Channels}, "+
                    $"SampleRate={this.WaveFormat.SampleRate}, " +
                    $"BytesPerSecond={this.WaveFormat.BytesPerSecond}, BlockAlign={this.WaveFormat.BlockAlign}, "+
                    $"BitsPerSample={this.WaveFormat.BitsPerSample}" );
                //----------------
                #endregion

                #region " AudioClient を初期化する。"
                //----------------
                try
                {
                    long 期間100ns = ( this._共有モード == AudioClientShareMode.Shared ) ?
                        0 :                                              // 共有モードの場合、0 を指定。
                        (long)( this.再生遅延sec * 10_000_000 + 0.5 );   // 排他モードの場合、コンストラクタで指定された値。

                    // イベント駆動で初期化。
                    this._AudioClient.Initialize( this._共有モード, AudioClientStreamFlags.StreamFlagsEventCallback, 期間100ns, 期間100ns, this.WaveFormat, Guid.Empty );
                }
                catch( CoreAudioAPIException e )
                {
                    if( e.ErrorCode == AUDCLNT_E_BUFFER_SIZE_NOT_ALIGNED )
                    {
                        // 排他モードかつイベント駆動 の場合、このエラーコードが返されることがある。
                        // この場合、バッファサイズを調整して再度初期化する。
                        int サイズframe = this._AudioClient.GetBufferSize();   // アライメント済みサイズが取得できる。
                        this.再生遅延sec = (double) サイズframe / this.WaveFormat.SampleRate;
                        long 期間100ns = (long) ( this.再生遅延sec * 10_000_000 + 0.5 );

                        // 再度初期化。
                        this._AudioClient.Initialize( this._共有モード, AudioClientStreamFlags.StreamFlagsEventCallback, 期間100ns, 期間100ns, this.WaveFormat, Guid.Empty );
                    }
                    else
                    {
                        throw;  // それでも例外なら知らん。
                    }
                }
                //----------------
                #endregion

                #region " イベント駆動に使うイベントを生成し、AudioClient へ登録する。"
                //----------------
                this._レンダリングイベント = new EventWaitHandle( false, EventResetMode.AutoReset );
                this._AudioClient.SetEventHandle( this._レンダリングイベント.SafeWaitHandle.DangerousGetHandle() );
                //----------------
                #endregion

                #region " その他のインターフェースを取得する。"
                //----------------
                this._AudioRenderClient = AudioRenderClient.FromAudioClient( this._AudioClient );
                this._AudioClock = AudioClock.FromAudioClient( this._AudioClient );
                //----------------
                #endregion

                Log.Info( $"サウンドデバイスを生成しました。" );

                #region " ミキサーを生成する。"
                //----------------
                this.Mixer = new Mixer( this.WaveFormat );
                //----------------
                #endregion
            }

            this.レンダリングを開始する();
        }

        public void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this.レンダリングを停止する();
            if( !this._レンダリング終了完了通知.WaitOne( 5000 ) )
                throw new InvalidOperationException( "レンダリングの終了完了待ちでタイムアウトしました。" );

            this.Mixer.Dispose();
            this._AudioClock.Dispose();
            this._AudioRenderClient.Dispose();
            this._AudioClient.Dispose();
            this._レンダリングイベント.Dispose();
            this._MMDevice.Dispose();
        }



        // レンダリング操作


        /// <summary>
        ///		レンダリングスレッドを生成し、ミキサーの出力のレンダリングを開始する。
        /// </summary>
        public void レンダリングを開始する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            var 現在の状態 = PlaybackState.Stopped;
            lock( this._スレッド間同期 )
                現在の状態 = this._レンダリング状態;

            switch( 現在の状態 )
            {
                case PlaybackState.Paused:
                    this.レンダリングを再開する();     // 再開する。
                    break;

                case PlaybackState.Stopped:
                    this._レンダリング起動完了通知 = new ManualResetEvent( false );
                    this._レンダリング終了完了通知 = new ManualResetEvent( false );

                    // レンダリングスレッドを生成する。
                    if( null != this._レンダリングスレッド )  // すでに起動してたら例外発出。
                        throw new InvalidOperationException( "レンダリングスレッドがすでに起動しています。" );

                    this._レンダリングスレッド = new Thread( this._レンダリングスレッドエントリ ) {
                        Name = "WASAPI Playback",
                        Priority = ThreadPriority.AboveNormal, // 標準よりやや上
                    };
                    this._レンダリングスレッド.SetApartmentState( ApartmentState.MTA );   // マルチスレッドアパートメント
                    this._レンダリングスレッド.Start();

                    // 起動完了待ち。
                    if( !this._レンダリング起動完了通知.WaitOne( 5000 ) )
                        throw new InvalidOperationException( "レンダリングスレッドの起動完了待ちがタイムアウトしました。" );

                    Log.Info( "レンダリングスレッドを起動しました。" );
                    break;

                case PlaybackState.Playing:
                    break;
            }
        }

        /// <summary>
        ///		ミキサーの出力のレンダリングを一時停止する。
        ///		ミキサーに登録されているすべてのサウンドの再生が一時停止する。
        /// </summary>
        public void レンダリングを一時停止する()
        {
            lock( this._スレッド間同期 )
            {
                using var _ = new LogBlock( Log.現在のメソッド名 );

                if( this._レンダリング状態 == PlaybackState.Playing )
                    this._レンダリング状態 = PlaybackState.Paused;
            }
        }

        /// <summary>
        ///		ミキサーの出力のレンダリングを再開する。
        ///		一時停止状態にあるときのみ有効。
        /// </summary>
        public void レンダリングを再開する()
        {
            lock( this._スレッド間同期 )
            {
                using var _ = new LogBlock( Log.現在のメソッド名 );
             
                if( this._レンダリング状態 == PlaybackState.Paused )
                    this._レンダリング状態 = PlaybackState.Playing;
            }
        }

        /// <summary>
        ///		ミキサーの出力のレンダリングを停止する。
        ///		ミキサーに登録されているすべてのサウンドの再生が停止する。
        /// </summary>
        public void レンダリングを停止する()
        {
            lock( this._スレッド間同期 )
            {
                using var _ = new LogBlock( Log.現在のメソッド名 );

                if( this._レンダリング状態 != PlaybackState.Stopped )
                    this._レンダリング状態 = PlaybackState.Stopped;
            }
        }



        // レンダリングスレッド


        /// <summary>
        ///		WASAPIイベント駆動スレッドのエントリ。
        /// </summary>
        private void _レンダリングスレッドエントリ()
        {
            var 元のMMCSS特性 = IntPtr.Zero;
            var encoding = AudioSubTypes.EncodingFromSubType( WaveFormatExtensible.SubTypeFromWaveFormat( this.WaveFormat ) );

            try
            {
                #region " 初期化。"
                //----------------
                int バッファサイズframe = this._AudioClient.BufferSize;
                var バッファ = new float[ バッファサイズframe * this.WaveFormat.Channels ];    // this._レンダリング先（ミキサー）の出力は 32bit-float で固定。
                var gchバッファ = GCHandle.Alloc( バッファ, GCHandleType.Pinned );

                // このスレッドの MMCSS 型を登録する。
                string mmcssType;
                switch( this.再生遅延sec )
                {
                    case double 遅延 when( 0.0105 > 遅延 ):
                        mmcssType = "Pro Audio";
                        break;

                    case double 遅延 when( 0.0150 > 遅延 ):
                        mmcssType = "Games";
                        break;

                    default:
                        mmcssType = "Audio";
                        break;
                }
                
                元のMMCSS特性 = AvSetMmThreadCharacteristics( mmcssType, out int taskIndex );

                this._AudioClient.Start();

                this._レンダリング状態 = PlaybackState.Playing;
                this._レンダリング起動完了通知.Set();
                //----------------
                #endregion

                Log.Info( "レンダリングループを開始します。" );

                #region " レンダリングループ。"
                //----------------
                var イベントs = new WaitHandle[] { this._レンダリングイベント };

                while( true )
                {
                    // イベントs[] のいずれかのイベントが発火する（またはタイムアウトする）まで待つ。

                    if( WaitHandle.WaitAny(
                        waitHandles: イベントs,
                        millisecondsTimeout: (int) ( 3000.0 * this.再生遅延sec ), // 適正値は レイテンシ×3 [ms] (MSDNより)
                        exitContext: false ) == WaitHandle.WaitTimeout )
                    {
                        continue;   // タイムアウトした＝まだどのイベントもきてない。
                    }


                    // １ターン分をレンダリング。

                    lock( this._スレッド間同期 )
                    {
                        // 状態チェック。

                        if( this._レンダリング状態 == PlaybackState.Stopped )
                            break;      // ループを抜ける。

                        if( this._レンダリング状態 == PlaybackState.Paused )
                            continue;   // 何もしない。


                        // バッファの空き容量を計算する。

                        int 未再生数frame = ( this._共有モード == AudioClientShareMode.Exclusive ) ? 0 : this._AudioClient.GetCurrentPadding();
                        int 空きframe = バッファサイズframe - 未再生数frame;
                        if( 5 >= 空きframe )
                            continue;   // あまりに空きが小さいなら、何もせずスキップする。


                        // 今回の読み込みサイズ（サンプル単位）を計算する。

                        int 読み込むサイズsample = (int) this._位置をブロック境界単位にそろえて返す(
                            空きframe * this.WaveFormat.Channels,       // 前提・レンダリング先.WaveFormat と this.WaveFormat は同一。
                            this.WaveFormat.BlockAlign / this.WaveFormat.BytesPerSample );

                        if( 0 >= 読み込むサイズsample )
                            continue;   // 今回は読み込まない。スキップする。


                        // ミキサーからの出力（32bit-float）をバッファに取得する。

                        int 読み込んだサイズsample = this.Mixer.Read( バッファ, 0, 読み込むサイズsample );


                        // バッファのデータをレンダリングフォーマットに変換しつつ、AudioRenderClient へ出力する。

                        IntPtr bufferPtr = this._AudioRenderClient.GetBuffer( 空きframe );
                        try
                        {
                            switch( encoding )
                            {
                                case AudioEncoding.IeeeFloat:
                                    #region " FLOAT32 → FLOAT32 "
                                    //----------------
                                    Marshal.Copy( バッファ, 0, bufferPtr, 読み込んだサイズsample );
                                    //----------------
                                    #endregion
                                    break;

                                case AudioEncoding.Pcm:
                                    switch( this.WaveFormat.BitsPerSample )
                                    {
                                        case 24:
                                            #region " FLOAT32 → PCM24 "
                                            //----------------
                                            {
                                                // ※ 以下のコードでは、まだ、まともに再生できない。おそらくザーッという大きいノイズだらけの音になる。
                                                unsafe
                                                {
                                                    byte* ptr = (byte*) bufferPtr.ToPointer();  // AudioRenderClient のバッファは GC 対象外なのでピン止め不要。

                                                    for( int i = 0; i < 読み込んだサイズsample; i++ )
                                                    {
                                                        float data = バッファ[ i ];
                                                        if( -1.0f > data ) data = -1.0f;
                                                        if( +1.0f < data ) data = +1.0f;

                                                        uint sample32 = (uint) ( data * 8388608f - 1f );    // 24bit PCM の値域は -8388608～+8388607
                                                        byte* psample32 = (byte*) &sample32;
                                                        *ptr++ = *psample32++;
                                                        *ptr++ = *psample32++;
                                                        *ptr++ = *psample32++;
                                                    }
                                                }
                                            }
                                            //----------------
                                            #endregion
                                            break;

                                        case 16:
                                            #region " FLOAT32 → PCM16 "
                                            //----------------
                                            unsafe
                                            {
                                                byte* ptr = (byte*) bufferPtr.ToPointer();  // AudioRenderClient のバッファは GC 対象外なのでピン止め不要。

                                                for( int i = 0; i < 読み込んだサイズsample; i++ )
                                                {
                                                    float data = バッファ[ i ];
                                                    if( -1.0f > data ) data = -1.0f;
                                                    if( +1.0f < data ) data = +1.0f;

                                                    short sample16 = (short) ( data * short.MaxValue );
                                                    byte* psample16 = (byte*) &sample16;
                                                    *ptr++ = *psample16++;
                                                    *ptr++ = *psample16++;
                                                }
                                            }
                                            //----------------
                                            #endregion
                                            break;

                                        case 8:
                                            #region " FLOAT32 → PCM8 "
                                            //----------------
                                            unsafe
                                            {
                                                byte* ptr = (byte*) bufferPtr.ToPointer();  // AudioRenderClient のバッファは GC 対象外なのでピン止め不要。

                                                for( int i = 0; i < 読み込んだサイズsample; i++ )
                                                {
                                                    float data = バッファ[ i ];
                                                    if( -1.0f > data ) data = -1.0f;
                                                    if( +1.0f < data ) data = +1.0f;

                                                    byte value = (byte) ( ( data + 1 ) * 128f );
                                                    *ptr++ = unchecked(value);
                                                }
                                            }
                                            //----------------
                                            #endregion
                                            break;
                                    }
                                    break;
                            }
                        }
                        finally
                        {
                            int 出力したフレーム数 = 読み込んだサイズsample / this.WaveFormat.Channels;

                            this._AudioRenderClient.ReleaseBuffer(
                                出力したフレーム数,
                                ( 0 < 出力したフレーム数 ) ? AudioClientBufferFlags.None : AudioClientBufferFlags.Silent );
                        }

                        // ミキサーからの出力がなくなったらレンダリングを停止する。

                        if( 0 == 読み込んだサイズsample )
                            this._レンダリング状態 = PlaybackState.Stopped;
                    }
                }
                //----------------
                #endregion

                #region " 終了。"
                //----------------
                this._AudioClient.Stop();
                this._AudioClient.Reset();

                // ハードウェアの再生が終わるくらいまで、少し待つ。
                Thread.Sleep( (int) ( this.再生遅延sec * 1000 / 2 ) );

                gchバッファ.Free();
                //----------------
                #endregion

                Log.Info( "レンダリングスレッドを終了します。" );
                this._レンダリング終了完了通知.Set();
            }
            //catch( Exception e )  ---> 例外をcatchするとスレッドが終了して無音になるだけなのでエラーに気づかない。例外はそのままスローする。
            //{
            //	Log.ERROR( $"例外が発生しました。レンダリングスレッドを中断します。[{e.Message}]" );
            //}
            finally
            {
                #region " 完了。"
                //----------------
                if( 元のMMCSS特性 != IntPtr.Zero )
                    AvRevertMmThreadCharacteristics( 元のMMCSS特性 );

                this._レンダリング起動完了通知.Set();
                this._レンダリング終了完了通知.Set();
                //----------------
                #endregion
            }
        }



        // その他


        /// <summary>
        ///		現在のデバイス位置を返す[秒]。
        /// </summary>
        public double GetDevicePosition()
        {
            // AudioClock から現在のデバイス位置を取得する。
            this.GetClock( out long position, out long qpcPosition, out long frequency );

            // position ...... 現在のデバイス位置（デバイスからの報告）
            // frequency ..... 現在のデバイス周波数（デバイスからの報告）
            // qpcPosition ... デバイス位置を取得した時刻 [100ns単位; パフォーマンスカウンタの生値ではないので注意]

            // デバイス位置÷デバイス周波数 で、秒単位に換算できる。
            double デバイス位置sec = (double) position / frequency;

            // デバイス位置の精度が荒い（階段状のとびとびの値になる）場合には、パフォーマンスカウンタで補間する。
            if( position == this._最後のデバイス位置.position )
            {
                // (A) デバイス位置が前回と同じである場合：
                // → 最後のデバイス位置における qpcPosition と今回の qpcPosition の差をデバイス位置secに加算する。
                デバイス位置sec += ( qpcPosition - this._最後のデバイス位置.qpcPosition ) / 10_000_000.0;
            }
            else
            {
                // (B) デバイス位置が前回と異なる場合：
                // → 最後のデバイス位置を現在の値に更新する。今回のデバイス位置secは補間しない。
                this._最後のデバイス位置 = (position, qpcPosition);
            }

            return デバイス位置sec;

            // ボツ１：
            // ↓サウンドデバイス固有のpositionを使う場合。なんかの拍子で、音飛びと同時に不連続になる？
            //return ( (double) position / frequency );

            // ボツ２：
            // ↓パフォーマンスカウンタを使う場合。こちらで、音飛び＆不連続現象が消えるか否かを検証中。
            // なお、qpcPosition には、生カウンタではなく、100ns単位に修正された値が格納されているので注意。（GetClockの仕様）
            //return FDKUtilities.変換_100ns単位からsec単位へ( qpcPosition );
        }



        // ローカル


        private volatile PlaybackState _レンダリング状態 = PlaybackState.Stopped;

        private AudioClientShareMode _共有モード;

        private AudioClock _AudioClock;

        private AudioRenderClient _AudioRenderClient;

        private AudioClient _AudioClient;

        private MMDevice _MMDevice;

        private (long position, long qpcPosition) _最後のデバイス位置 = (0, 0);

        private Thread _レンダリングスレッド = null!;

        private EventWaitHandle _レンダリングイベント;

        private readonly object _スレッド間同期 = new object();

        private ManualResetEvent _レンダリング起動完了通知 = null!;

        private ManualResetEvent _レンダリング終了完了通知 = null!;


        /// <summary>
        ///		希望したフォーマットをもとに、適切なフォーマットを調べて返す。
        /// </summary>
        /// <param name="waveFormat">希望するフォーマット</param>
        /// <param name="audioClient">AudioClient インスタンス。Initialize 前でも可。</param>
        /// <returns>適切なフォーマット。見つからなかったら null。</returns>
        private WaveFormat? _適切なフォーマットを調べて返す( WaveFormat? waveFormat )
        {
            var 最も近いフォーマット = (WaveFormat?) null;
            var 最終的に決定されたフォーマット = (WaveFormat?) null;

            if( ( null != waveFormat ) && this._AudioClient!.IsFormatSupported( this._共有モード, waveFormat, out 最も近いフォーマット ) )
            {
                // (A) そのまま使える。
                最終的に決定されたフォーマット = waveFormat;
            }
            else if( null != 最も近いフォーマット )
            {
                // (B) AudioClient が推奨フォーマットを返してきたので、それを採択する。
                最終的に決定されたフォーマット = 最も近いフォーマット;
            }
            else
            {
                // (C) AudioClient からの提案がなかったので、共有モードのフォーマットを採択してみる。

                var 共有モードのフォーマット = this._AudioClient.GetMixFormat();

                if( ( null != 共有モードのフォーマット ) && this._AudioClient.IsFormatSupported( this._共有モード, 共有モードのフォーマット ) )
                {
                    最終的に決定されたフォーマット = 共有モードのフォーマット;
                }
                else
                {
                    // (D) 共有モードのフォーマットも NG である場合は、以下から探す。

                    bool found = this._AudioClient.IsFormatSupported( AudioClientShareMode.Exclusive,
                        new WaveFormat( 48000, 24, 2, AudioEncoding.Pcm ),
                        out WaveFormat closest );

                    最終的に決定されたフォーマット = new[] {
                        new WaveFormat( 48000, 32, 2, AudioEncoding.IeeeFloat ),
                        new WaveFormat( 44100, 32, 2, AudioEncoding.IeeeFloat ),
						/*
						 * 24bit PCM には対応しない。
						 * 
						 * https://msdn.microsoft.com/ja-jp/library/cc371566.aspx
						 * > wFormatTag が WAVE_FORMAT_PCM の場合、wBitsPerSample は 8 または 16 でなければならない。
						 * > wFormatTag が WAVE_FORMAT_EXTENSIBLE の場合、この値は、任意の 8 の倍数を指定できる。
						 * 
						 * また、Realtek HD Audio の場合、IAudioClient.IsSupportedFormat() は 24bit PCM でも true を返してくるが、
						 * 単純に 1sample = 3byte で書き込んでも正常に再生できない。
						 * おそらく 32bit で包む必要があると思われるが、その方法は不明。
						 */
						//new WaveFormat( 48000, 24, 2, AudioEncoding.Pcm ),
						//new WaveFormat( 44100, 24, 2, AudioEncoding.Pcm ),
						new WaveFormat( 48000, 16, 2, AudioEncoding.Pcm ),
                        new WaveFormat( 44100, 16, 2, AudioEncoding.Pcm ),
                        new WaveFormat( 48000,  8, 2, AudioEncoding.Pcm ),
                        new WaveFormat( 44100,  8, 2, AudioEncoding.Pcm ),
                        new WaveFormat( 48000, 32, 1, AudioEncoding.IeeeFloat ),
                        new WaveFormat( 44100, 32, 1, AudioEncoding.IeeeFloat ),
						//new WaveFormat( 48000, 24, 1, AudioEncoding.Pcm ),
						//new WaveFormat( 44100, 24, 1, AudioEncoding.Pcm ),
						new WaveFormat( 48000, 16, 1, AudioEncoding.Pcm ),
                        new WaveFormat( 44100, 16, 1, AudioEncoding.Pcm ),
                        new WaveFormat( 48000,  8, 1, AudioEncoding.Pcm ),
                        new WaveFormat( 44100,  8, 1, AudioEncoding.Pcm ),
                    }
                    .FirstOrDefault( ( format ) => ( this._AudioClient.IsFormatSupported( this._共有モード, format ) ) );

                    // (E) それでも見つからなかったら null のまま。
                }
            }

            return 最終的に決定されたフォーマット;
        }

        /// <summary>
        ///		現在のデバイス位置を取得する。
        /// </summary>
        private void GetClock( out long Pu64Position, out long QPCPosition, out long Pu64Frequency )
        {
            lock( this._スレッド間同期 )
            {
                this._AudioClock.GetFrequencyNative( out Pu64Frequency );

                int hr = 0;
                long pos = 0;
                long qpcPos = 0;

                for( int リトライ回数 = 0; リトライ回数 < 10; リトライ回数++ )    // 最大10回までリトライ。
                {
                    hr = this._AudioClock.GetPositionNative( out pos, out qpcPos );

                    // ※IAudioClock::GetPosition() は、S_FALSE を返すことがある。
                    // 　これは、WASAPI排他モードにおいて、GetPosition 時に優先度の高いイベントが発生しており
                    // 　規定時間内にデバイス位置を取得できなかった場合に返される。(MSDNより)

                    if( ( (int) HResult.S_OK ) == hr )
                    {
                        break;      // OK
                    }
                    else if( ( (int) HResult.S_FALSE ) == hr )
                    {
                        continue;   // リトライ
                    }
                    else
                    {
                        throw new Win32ComException( hr, "IAudioClock", "GetPosition" );
                    }
                }

                Pu64Position = pos;
                QPCPosition = qpcPos;
            }
        }

        private long _位置をブロック境界単位にそろえて返す( long position, long blockAlign )
        {
            return ( position - ( position % blockAlign ) );
        }



        // Win32


        private const int AUDCLNT_E_BUFFER_SIZE_NOT_ALIGNED = unchecked((int) 0x88890019);
        
        private const int AUDCLNT_E_INVALID_DEVICE_PERIOD = unchecked((int) 0x88890020);
        
        private const int AUDCLNT_E_NOT_INITIALIZED = unchecked((int) 0x88890001);


        [DllImport( "Avrt.dll", CharSet = CharSet.Unicode )]
        private static extern IntPtr AvSetMmThreadCharacteristics( [MarshalAs( UnmanagedType.LPWStr )] string proAudio, out int taskIndex );

        [DllImport( "Avrt.dll" )]
        private static extern bool AvRevertMmThreadCharacteristics( IntPtr avrtHandle );
    }
}
