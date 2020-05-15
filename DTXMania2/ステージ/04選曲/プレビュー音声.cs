using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSCore;
using FDK;

namespace DTXMania2.選曲
{
    /// <summary>
    ///    プレビュー音声の再生とライフサイクル管理。
    /// </summary>
    /// <remarks>
    ///     指定された音声ファイルを、一定時間（500ミリ秒）後に生成して再生する機能を提供する。
    ///     生成された音声データは、一定数までキャッシングされる。
    /// </remarks>
    class プレビュー音声 : IDisposable
    {

        // 生成と終了


        public プレビュー音声()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );
        }



        // 再生と停止


        /// <summary>
        ///     サウンドファイルの再生を予約する。
        /// </summary>
        /// <remarks>
        ///     予約されたサウンドは、500ミリ秒後に生成され、再生される。
        /// </remarks>
        public void 再生を予約する( VariablePath 音声ファイルの絶対パス )
        {
            CancellationTokenSource cancelTokenSource;
            CancellationToken cancelToken;

            lock( this._CancelTokenSources排他 )
            {
                // 既存のすべてのタスクをキャンセルする。
                foreach( var source in this._CanscellationTokenSources )
                    source.Cancel();

                // 新しいタスク用のキャンセルトークンソースを生成する。
                cancelTokenSource = new CancellationTokenSource();
                cancelToken = cancelTokenSource.Token;
                this._CanscellationTokenSources.Add( cancelTokenSource );
            }

            // 新しいタスクを開始する。

            Task.Run( () => {

                // 500ミリ秒待つ。
                Task.Delay( 500 ).Wait();
                if( cancelToken.IsCancellationRequested )
                    return; // キャンセルされた

                // ファイルからサウンドを生成する。
                var sound = this._サウンドを生成する( 音声ファイルの絶対パス );
                if( sound is null )
                {
                    Log.ERROR( $"サウンドの生成に失敗しました。[{音声ファイルの絶対パス}]" );
                    return; // 失敗した
                }
                if( cancelToken.IsCancellationRequested )
                {
                    sound.Dispose();
                    return; // キャンセルされた
                }

                // サウンドを再生する。
                sound.Play( 0, ループ再生する: true );

                // キャンセルされるまでブロック。
                cancelToken.WaitHandle.WaitOne();

                // サウンドを解放する。
                sound.Dispose();

            }, cancelToken )

                .ContinueWith( ( t ) => {

                    // タスクが終了したらトークンソースを解放。
                    lock( this._CancelTokenSources排他 )
                    {
                        this._CanscellationTokenSources.Remove( cancelTokenSource );
                        cancelTokenSource.Dispose();
                    }

                } );
        }

        /// <summary>
        ///     現在再生されているサウンドを停止し、既存の予約があればキャンセルする。
        /// </summary>
        public void 停止する()
        {
            lock( this._CancelTokenSources排他 )
            {
                // 既存のすべてのタスクをキャンセルする。
                foreach( var source in this._CanscellationTokenSources )
                    source.Cancel();
            }
        }



        // ローカル


        private List<CancellationTokenSource> _CanscellationTokenSources = new List<CancellationTokenSource>();

        private readonly object _CancelTokenSources排他 = new object();

        private Sound? _サウンドを生成する( VariablePath 音声ファイルの絶対パス )
        {
            // ファイルから ISampleSource を取得する。
            var sampleSource = this._SampleSourceを生成する( 音声ファイルの絶対パス );
            if( sampleSource is null )
            {
                Log.ERROR( $"ISampleSourceの生成に失敗しました。[{音声ファイルの絶対パス}]" );
                return null;
            }

            // ISampleSource からサウンドを生成して返す。
            return new Sound( Global.App.サウンドデバイス, sampleSource );
        }

        private ISampleSource? _SampleSourceを生成する( VariablePath 音声ファイルの絶対パス )
        {
            ISampleSource? sampleSource = null;

            // キャッシュにある？
            lock( this._キャッシュ用排他 )
            {
                if( this._サンプルソースキャッシュ.ContainsKey( 音声ファイルの絶対パス.変数なしパス ) )
                {
                    // あるなら、対応する ISampleSource を取得。
                    sampleSource = this._サンプルソースキャッシュ[ 音声ファイルの絶対パス.変数なしパス ];
                }
            }

            if( sampleSource is null )
            {
                // ファイルから ISampleSource を生成する。
                sampleSource = SampleSourceFactory.Create( Global.App.サウンドデバイス, 音声ファイルの絶対パス );
                if( sampleSource is null )
                    return null;    // 失敗した

                // ISampleSource をキャッシュに登録する。
                lock( this._キャッシュ用排他 )
                {
                    this._サンプルソースキャッシュ.Add( 音声ファイルの絶対パス.変数なしパス, sampleSource );
                    this._キャッシュ世代リスト.Add( 音声ファイルの絶対パス.変数なしパス );
                }

                // キャッシュが一定数を超えたら、一番古いものから削除する。
                if( キャッシュする個数 < this._キャッシュ世代リスト.Count )
                {
                    lock( this._キャッシュ用排他 )
                    {
                        var path = this._キャッシュ世代リスト.Last();       // リストの末尾の最後の
                        this._サンプルソースキャッシュ[ path ].Dispose();   // ISampleSource を解放して
                        this._キャッシュ世代リスト.Remove( path );          // キャッシュからも
                        this._サンプルソースキャッシュ.Remove( path );      // 削除。
                    }
                }
            }
            else
            {
                // キャッシュの世代リストで、今取得したソースが一番若くなるように操作する。
                lock( this._キャッシュ用排他 )
                {
                    this._キャッシュ世代リスト.Remove( 音声ファイルの絶対パス.変数なしパス );  // 冤罪の位置から除去して
                    this._キャッシュ世代リスト.Add( 音声ファイルの絶対パス.変数なしパス );     // 一番最新の位置へ。
                }
            }

            // ISampleSource を返す。
            return sampleSource;
        }



        // キャッシュ


        private const int キャッシュする個数 = 20;

        private readonly object _キャッシュ用排他 = new object();

        private Dictionary<string, ISampleSource> _サンプルソースキャッシュ = new Dictionary<string, ISampleSource>();

        private List<string> _キャッシュ世代リスト = new List<string>();
    }
}
