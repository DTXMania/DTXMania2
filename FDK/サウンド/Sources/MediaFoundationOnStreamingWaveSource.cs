using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FDK
{
    /// <summary>
    ///		指定されたメディアファイル（動画, 音楽）をバックグラウンドで読み込む <see cref="CSCore.IWaveSource"/> オブジェクトを生成する。
    /// </summary>
    public class MediaFoundationOnStreamingWaveSource : CSCore.IWaveSource
    {

        // プロパティ


        public virtual string Name { get; set; }

        /// <summary>
        ///		シーク能力があるなら true 。
        /// </summary>
        public virtual bool CanSeek => true;

        /// <summary>
        ///		デコード後のオーディオデータのフォーマット。
        /// </summary>
        public virtual CSCore.WaveFormat WaveFormat { get; protected set; }

        /// <summary>
        ///		デコード後のオーディオデータのすべての長さ[byte]。
        /// </summary>
        public virtual long Length
            => this._Disposed ? 0 : this._DecodedWaveDataQueue.書き込み位置;

        /// <summary>
        ///		現在の再生位置[byte]。
        /// </summary>
        public virtual long Position
        {
            get
                => this._Disposed ? 0 : this._DecodedWaveDataQueue.読み出し位置;
            set
            {
                if( !( this._Disposed ) )
                    this._DecodedWaveDataQueue.読み出し位置 = this._位置をブロック境界単位にそろえて返す( value, this.WaveFormat.BlockAlign );
            }
        }

        /// <summary>
        ///     デコードタスクがデコードを完了したらセットされるイベント。
        /// </summary>
        public ManualResetEventSlim DecodeComplete { get; } = new ManualResetEventSlim( false );



        // 生成と終了


        /// <summary>
        ///		コンストラクタ。
        ///		指定されたファイルを指定されたフォーマットでデコードし、内部にオンメモリで保管する。
        /// </summary>
        public MediaFoundationOnStreamingWaveSource( VariablePath ファイルパス, CSCore.WaveFormat deviceFormat )
        {
            this.Name = ファイルパス.変数付きパス;

            this.WaveFormat = new CSCore.WaveFormat(
                deviceFormat.SampleRate,
                32,
                deviceFormat.Channels,
                CSCore.AudioEncoding.IeeeFloat );

            // ファイルから SourceReaderEx を生成する。
            //
            //using( var sourceReader = new MFSourceReader( path ) )  // SourceReader は、SharpDX ではなく CSCore のものを使う。（WaveFormat から MediaType に一発で変換できるので。）
            //	→ CSCore.Win32.Comobject のファイナライザに不具合があるので、SourceReader には CSCore ではなく SharpDX のものを使う。
            //	  _MFMediaType, WaveFormat フィールドは CSCore のものなので注意。
            //
            using( var sourceReader = new SharpDX.MediaFoundation.SourceReader( ファイルパス.変数なしパス ) )      // パスは URI 扱い
                this._SourceReaderEx = sourceReader.QueryInterface<SharpDX.MediaFoundation.SourceReaderEx>();

            this._SourceReaderの取得後の初期化();
        }

        /// <summary>
        ///		コンストラクタ。
        ///		指定されたソースリーダーを指定されたフォーマットで、内部にオンメモリで保管する。
        /// </summary>
        public MediaFoundationOnStreamingWaveSource( SharpDX.MediaFoundation.SourceReaderEx sourceReader, CSCore.WaveFormat deviceFormat )
        {
            this.Name = "";

            this.WaveFormat = new CSCore.WaveFormat(
                deviceFormat.SampleRate,
                32,
                deviceFormat.Channels,
                CSCore.AudioEncoding.IeeeFloat );

            this._SourceReaderEx = sourceReader;

            this._SourceReaderの取得後の初期化();
        }
        
        private void _SourceReaderの取得後の初期化()
        {
            // 最初のオーディオストリームを選択する。
            //this._SourceReaderEx.SetStreamSelection( SharpDX.MediaFoundation.SourceReaderIndex.AllStreams, false );   --> その他についてはいじらない。
            this._SourceReaderEx.SetStreamSelection( SharpDX.MediaFoundation.SourceReaderIndex.FirstAudioStream, true );

            // 部分メディアタイプを作成。
            //
            // ↓CSCore の場合。WaveFormatEx にも対応。
            //using( var partialMediaType = MFMediaType.FromWaveFormat( this.WaveFormat ) )
            // 
            // ↓SharpDX の場合。
            var wf = SharpDX.Multimedia.WaveFormat.CreateIeeeFloatWaveFormat( this.WaveFormat.SampleRate, this.WaveFormat.Channels );
            SharpDX.MediaFoundation.MediaFactory.CreateAudioMediaType( ref wf, out SharpDX.MediaFoundation.AudioMediaType partialMediaType );

            using( partialMediaType )
            {
                // 作成した部分メディアタイプを SourceReader にセットする。必要なデコーダが見つからなかったら、ここで例外が発生する。
                this._SourceReaderEx.SetCurrentMediaType( SharpDX.MediaFoundation.SourceReaderIndex.FirstAudioStream, partialMediaType );

                // 完成されたメディアタイプを取得する。
                this._AudioMediaType = this._SourceReaderEx.GetCurrentMediaType( SharpDX.MediaFoundation.SourceReaderIndex.FirstAudioStream );
                this._MFAudioMediaType = new CSCore.MediaFoundation.MFMediaType( this._AudioMediaType.NativePointer ); // ネイティブポインタを使って、SharpDX → CSCore へ変換。SharpDX側オブジェクトは解放したらダメ。
            }

            // メディアタイプから WaveFormat を取得する。
            this.WaveFormat = this._MFAudioMediaType.ToWaveFormat( CSCore.MediaFoundation.MFWaveFormatExConvertFlags.Normal );

            // デコード開始。
            this._DecodedWaveDataQueue = new QueueMemoryStream();
            this._デコードキャンセル = new CancellationTokenSource();
            this._デコードタスク = Task.Factory.StartNew( this._デコードタスクエントリ, this._デコードキャンセル.Token );
        }

        public void Dispose()
        {
            if( !this._Disposed )
            {
                #region " デコードタスクが稼働しているなら終了する。"
                //----------------
                if( ( null != this._デコードタスク ) && !( this._デコードタスク.IsCompleted ) )
                {
                    this._デコードキャンセル.Cancel();
                    this._DecodedWaveDataQueue.Cancel();
                    this._デコードタスク.Wait( 5000 ); // 最大5秒までは待つ
                }
                //----------------
                #endregion

                this._デコードキャンセル.Dispose();
                this._DecodedWaveDataQueue.Dispose();
                this._SourceReaderEx.Dispose();
                //this._AudioMediaType.Dispose();   // _MFAudioMediaType と同じネイティブポインタを共有している。
                this._MFAudioMediaType.Dispose();   //

                this._Disposed = true;
            }
        }



        // 出力


        /// <summary>
        ///		連続したデータを読み込む。
        ///		データが不足していれば、その分は 0 で埋めて返す。
        /// </summary>
        /// <param name="buffer">読み込んだデータを格納するための配列。</param>
        /// <param name="offset"><paramref name="buffer"/> に格納を始める位置。</param>
        /// <param name="count">読み込む最大のデータ数。</param>
        /// <returns><paramref name="buffer"/> に読み込んだデータの総数（常に <paramref name="count"/> に一致）。</returns>
        public virtual int Read( byte[] buffer, int offset, int count )
        {
            // 前提条件チェック。音がめちゃくちゃになるとうざいので、このメソッド内では例外を出さないこと。
            if( this._Disposed || ( null == this._SourceReaderEx ) || ( null == this._DecodedWaveDataQueue ) || ( null == buffer ) )
                return 0;

            // ストリームから読み出す。データが不足していてもブロックせずすぐ戻る。
            int readCount = this._DecodedWaveDataQueue.Read( buffer, offset, count, データが足りないならブロックする: false );

            // データが不足しているなら、不足分を 0 で埋める。
            if( readCount < count )
                Array.Clear( buffer, ( offset + readCount ), ( count - readCount ) );

            return count;
        }



        // ローカル


        protected SharpDX.MediaFoundation.SourceReaderEx _SourceReaderEx = null!;

        protected SharpDX.MediaFoundation.MediaType _AudioMediaType = null!;

        protected CSCore.MediaFoundation.MFMediaType _MFAudioMediaType = null!;

        protected QueueMemoryStream _DecodedWaveDataQueue = null!;

        protected Task _デコードタスク = null!;

        protected CancellationTokenSource _デコードキャンセル = null!;

        private bool _Disposed = false;


        private void _デコードタスクエントリ()
        {
            Log.現在のスレッドに名前をつける( "オーディオデコード" );
            Log.Info( $"オーディオデコードタスクを起動しました。[{this.Name}]" );

            while( true )
            {
                // キャンセル通知が来てればループを抜ける。
                if( this._デコードキャンセル.IsCancellationRequested )
                {
                    Log.Info( $"キャンセル通知を受信しました。[{this.Name}]" );
                    break;
                }

                // 次のサンプルをひとつデコードする。
                using( var サンプル = this._SourceReaderEx.ReadSample(
                    SharpDX.MediaFoundation.SourceReaderIndex.FirstAudioStream,
                    SharpDX.MediaFoundation.SourceReaderControlFlags.None,
                    out _,
                    out var ストリームフラグ,
                    out long サンプルの表示時刻100ns ) )
                {

                    // デコード結果を確認する。
                    if( ストリームフラグ.HasFlag( SharpDX.MediaFoundation.SourceReaderFlags.Endofstream ) )
                    {
                        Log.Info( $"ストリームが終了しました。[{this.Name}]" );
                        break;
                    }
                    if( ストリームフラグ.HasFlag( SharpDX.MediaFoundation.SourceReaderFlags.Error ) ||
                        ストリームフラグ.HasFlag( SharpDX.MediaFoundation.SourceReaderFlags.AllEffectsremoved ) ||
                        ストリームフラグ.HasFlag( SharpDX.MediaFoundation.SourceReaderFlags.Currentmediatypechanged ) ||
                        ストリームフラグ.HasFlag( SharpDX.MediaFoundation.SourceReaderFlags.Nativemediatypechanged ) ||
                        ストリームフラグ.HasFlag( SharpDX.MediaFoundation.SourceReaderFlags.Newstream ) ||
                        ストリームフラグ.HasFlag( SharpDX.MediaFoundation.SourceReaderFlags.StreamTick ) )
                    {
                        Log.ERROR( $"デコード中にエラーが発生、または未対応の状態変化が発生しました。[{this.Name}]" );
                        break;
                    }

                    // サンプルをロックし、オーディオデータへのポインタを取得して、オーディオデータをメモリストリームに書き込む。
                    using( var mediaBuffer = サンプル.ConvertToContiguousBuffer() )
                    {
                        var audioData = mediaBuffer.Lock( out _, out int cbCurrentLength );
                        byte[] dstData = new byte[ cbCurrentLength ];
                        Marshal.Copy( audioData, dstData, 0, cbCurrentLength );

                        this._DecodedWaveDataQueue.Write( dstData, 0, cbCurrentLength );

                        mediaBuffer.Unlock();
                    }
                }
            }

            this.DecodeComplete.Set();
            Log.Info( $"デコードタスクを終了しました。[{this.Name}]" );
        }

        private long _位置をブロック境界単位にそろえて返す( long position, long blockAlign )
        {
            return ( position - ( position % blockAlign ) );
        }
    }
}
