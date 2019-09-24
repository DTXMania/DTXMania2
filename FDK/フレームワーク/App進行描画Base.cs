using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FDK
{
    /// <summary>
    ///     アプリのメインループ（進行描画）を受け持つ。
    /// </summary>
    public class App進行描画Base
    {

        public AppFormBase AppForm { get; protected set; }



        // スレッド間メッセージ


        protected 通知キュー メッセージキュー = new 通知キュー();

        protected virtual void メッセージを処理する( 通知 msg )
        {
            // ※ 派生クラスでオーバーライドできるように、独立したメソッドにしておく。

            switch( msg )
            {
                case 終了通知 _:
                    break;  // 特別にメインループ内で処理するのでここでは何もしない。

                case サイズ変更通知 msg2:
                    this._サイズを変更する( msg2 );
                    break;

                default:
                    //Log.WARNING( "未定義のメッセージが指定されました。" );
                    break;
            }
        }



        // 開始とメインループ


        public App進行描画Base()
        {
        }

        /// <summary>
        ///     進行描画ループを実行するタスクを生成し、開始する。
        /// </summary>
        public void 開始する( AppFormBase form, Size 物理画面サイズ, Size 設計画面サイズ, IntPtr hWindow )
        {
            this.AppForm = form;

            // 進行描画タスクを生成する。
            this._タスク = Task.Run( () => {   // MTAThread

                try
                {
                    #region " 初期化 "
                    //----------------
                    Thread.CurrentThread.Name = "進行描画";
                    Debug.Assert( Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA, "MTAThread で実行してください。" );

                    DXResources.CreateInstance( hWindow, 物理画面サイズ, 設計画面サイズ );

                    this.On開始();

                    this._Tick通知 = new AutoResetEvent( false );
                    this._タイマ = new QueueTimer( 1, 1, () => this._Tick通知.Set() );   // 1ms ごとに Tick通知を set する

                    通知 終了通知 = null;
                    //----------------
                    #endregion

                    #region " メインループ "
                    //----------------
                    var 表示タスク = new 表示タスク();

                    while( this._Tick通知.WaitOne() )     // Tick 通知が来るまで待機。
                    {
                        // (1) メッセージがあれば全部処理する。

                        bool メインループを抜ける = false;

                        while( this.メッセージキュー.TryDequeue( out 通知 msg ) )
                        {
                            if( msg is 終了通知 )
                            {
                                終了通知 = msg;
                                メインループを抜ける = true;
                                break;
                            }
                            else
                            {
                                this.メッセージを処理する( msg );
                            }
                        }
                        if( メインループを抜ける )
                            break;


                        // (2) 進行する。

                        DXResources.Instance.Animation.進行する();
                        this.On進行();


                        // (3) 描画する。

                        if( 表示タスク.表示待機中 )
                        {
                            // 表示タスクがすでに起動されていて表示待ち状態になっているなら、今回は描画しない。
                        }
                        else
                        {
                            this.On描画();
                            表示タスク.表示を開始する();
                        }
                    }
                    //----------------
                    #endregion

                    #region " 終了 "
                    //----------------
                    this._タイマ?.Dispose();

                    this.On終了();

                    DXResources.ReleaseInstance();

                    this.AppForm = null;

                    終了通知?.完了通知.Set();
                    //----------------
                    #endregion
                }
                catch( Exception e )
                {
                    form.例外を通知する( e );
                }

            } );
        }

        protected virtual void On開始()
        {
            // 追加処理があれば、派生クラスで実装する。
        }


        /// <summary>
        ///     進行描画スレッドのインスタンス。
        /// </summary>
        private Task _タスク;

        /// <summary>
        ///     進行描画用タイマ。
        /// </summary>
        private QueueTimer _タイマ;

        /// <summary>
        ///     一定時間ごとに set されることを想定したイベント。
        /// </summary>
        /// <remarks>
        ///     このイベントが Set されるごとに、進行または描画が１回行われる。
        /// </remarks>
        private AutoResetEvent _Tick通知;



        // 終了


        /// <summary>
        ///     進行描画ループに終了通知を送る。
        /// </summary>
        /// <returns>通知が受信されれば set されるイベント。</returns>
        public AutoResetEvent 終了を通知する()
        {
            var msg = new 終了通知();
            this.メッセージキュー.Enqueue( msg );

            return msg.完了通知;
        }

        protected virtual void On終了()
        {
            // 追加処理があれば、派生クラスで実装する。
        }



        // 進行、描画


        protected virtual void On進行()
        {
            // 派生クラスで実装する。
        }

        protected virtual void On描画()
        {
            // 派生クラスで実装する。
        }


        
        // 物理画面（スワップチェーン）サイズの変更


        /// <summary>
        ///		物理画面サイズの変更通知を進行描画ループに贈る。
        /// </summary>
        /// <returns>通知が受信されれば set されるイベント。</returns>
        public AutoResetEvent サイズ変更を通知する( Size 新物理画面サイズ )
        {
            var msg = new サイズ変更通知 {
                新物理画面サイズ = 新物理画面サイズ,
            };
            this.メッセージキュー.Enqueue( msg );

            return msg.完了通知;
        }

        protected void _サイズを変更する( サイズ変更通知 msg )
        {
            // リソースを解放して、
            this.Onスワップチェーンに依存するグラフィックリソースの解放();

            // スワップチェーンを再構築して、
            DXResources.Instance.物理画面サイズを変更する( msg.新物理画面サイズ );

            // リソースを再作成する。
            this.Onスワップチェーンに依存するグラフィックリソースの作成();

            // 完了。
            msg.完了通知.Set();
        }

        protected virtual void Onスワップチェーンに依存するグラフィックリソースの作成()
        {
            // 派生クラスで実装すること。
        }

        protected virtual void Onスワップチェーンに依存するグラフィックリソースの解放()
        {
            // 派生クラスで実装すること。
        }
    }
}
