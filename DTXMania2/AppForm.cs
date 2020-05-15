using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DTXMania2.Properties;
using FDK;

namespace DTXMania2
{
    /// <summary>
    ///     アプリケーションのメインフォーム（とUIスレッド）。
    /// </summary>
    partial class AppForm : Form
    {

        // プロパティ


        /// <summary>
        ///     アプリケーション再起動指示フラグ。
        /// </summary>
        /// <remarks>
        ///     <see cref="AppForm"/> インスタンスの終了時にこのフラグが true になっている場合には、
        ///     このインスタンスの保持者（おそらくProgramクラス）は適切に再起動を行うこと。
        /// </remarks>
        public bool 再起動が必要 { get; protected set; } = false;

        /// <summary>
        ///     画面モード（ウィンドウ、全画面）。
        /// </summary>
        public ScreenMode ScreenMode { get; protected set; }

        /// <summary>
        ///     HIDキーボード入力。
        /// </summary>
        /// <remarks>
        ///     接続されているHIDキーボードのうち、既定の1デバイスだけを管理する。
        ///     RawInputを使うので、フォームのスレッドで管理する。（RawInputはUIスレッドでのみ動作する）
        /// </remarks>
        public KeyboardHID KeyboardHID { get; protected set; }

        /// <summary>
        ///     すべてのHIDゲームパッド、HIDジョイスティック入力。
        /// </summary>
        /// <remarks>
        ///     すべてのゲームパッド／ジョイスティックデバイスを管理する（<see cref="GameControllersHID.Devices"/>参照）。
        ///     RawInputを使うので、フォームのスレッドで管理する。（RawInputはUIスレッドでのみ動作する）
        /// </remarks>
        public GameControllersHID GameControllersHID { get; protected set; }

        /// <summary>
        ///     すべてのMIDI入力デバイス。
        /// </summary>
        public MidiIns MidiIns { get; protected set; }



        // 生成と終了


        /// <summary>
        ///     コンストラクタ。
        /// </summary>
        public AppForm()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            InitializeComponent();

            this.ScreenMode = new ScreenMode( this );
            this.KeyboardHID = new KeyboardHID();
            this.GameControllersHID = new GameControllersHID( this.Handle );
            this.MidiIns = new MidiIns();
            this.Icon = Resources.DTXMania2;
        }

        /// <summary>
        ///     アプリケーションの起動処理を行う。
        /// </summary>
        protected override void OnLoad( EventArgs e )
        {
            Log.Header( "アプリケーション起動" );
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this.Text = $"DTXMania2 Release {int.Parse( Application.ProductVersion.Split( '.' ).ElementAt( 0 ) ):000} {( ( Environment.Is64BitProcess ) ? "" : "(x86)" )}";
            this.ClientSize = new Size( 1024, 576 );

            // App を生成して進行描画タスクを起動する。
            Global.AppForm = this;
            Global.Handle = this.Handle;
            Global.App = new App();
            Global.App.進行描画タスクを開始する(
                設計画面サイズ: new SharpDX.Size2F( 1920f, 1080f ),
                物理画面サイズ: new SharpDX.Size2F( this.ClientSize.Width, this.ClientSize.Height ) );

            this._未初期化 = false; // Appの生成後に。

            // 全画面モードが設定されているならここで全画面に切り替える。
            if( Global.App.システム設定.全画面モードである )
                this.ScreenMode.ToFullscreenMode();

            base.OnLoad( e );
        }

        /// <summary>
        ///     アプリケーションの終了処理を行う。
        /// </summary>
        protected override void OnClosing( CancelEventArgs e )
        {
            Log.Header( "アプリケーション終了" );
            using var _ = new LogBlock( Log.現在のメソッド名 );

            Global.App.進行描画タスクを終了する();
            Global.App.Dispose();

            this.MidiIns.Dispose();
            this.GameControllersHID.Dispose();
            this.KeyboardHID.Dispose();

            this._未初期化 = true;

            base.OnClosing( e );
        }

        /// <summary>
        ///     アプリケーションを終了し、再起動する。
        /// </summary>
        public void 再起動する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this.再起動が必要 = true;
            this.Close();
        }



        // ウィンドウサイズの変更


        /* 次の２通りがある。
         * 
         * A.ユーザのドラッグによるサイズ変更。
         *      → ResizeBegin ～ ResizeEnd の範囲内で Resize が発生するがいったん無視し、ResizeEnd のタイミングでサイズの変更を行う。
         * 
         * B.最大化、最小化など。
         *      → ResizeBegin ～ ResizeEnd の範囲外で Resize が発生するので、そのタイミングでサイズの変更を行う。
         */

        protected override void OnResizeBegin( EventArgs e )
        {
            this._リサイズ中 = true; // リサイズ開始

            base.OnResizeBegin( e );
        }

        protected override void OnResizeEnd( EventArgs e )
        {
            this._リサイズ中 = false;    // リサイズ終了（先に設定）

            if( this.WindowState == FormWindowState.Minimized )
            {
                // (A) 最小化された → 何もしない
            }
            else if( this.ClientSize.IsEmpty )
            {
                // (B) クライアントサイズが空 → たまに起きるらしい。スキップする。
            }
            else
            {
                // (C) それ以外は Resize イベントハンドラへ委譲。
                this.OnResize( e );
            }

            base.OnResizeEnd( e );
        }

        protected override void OnResize( EventArgs e )
        {
            if( this._未初期化 || this._リサイズ中 )
            {
                //Log.Info( "未初期化、またはリサイズ中なので無視します。" );
                return;
            }
            else
            {
                using var _ = new LogBlock( Log.現在のメソッド名 );

                // スワップチェーンとその依存リソースを解放し、改めて作成しなおすように進行描画タスクへ指示する。
                var msg = new TaskMessage(
                    宛先: TaskMessage.タスク名.進行描画,
                    内容: TaskMessage.内容名.サイズ変更,
                    引数: new object[] { this.ClientSize } );

                // 進行描画タスクからの完了通知を待つ。
                if( !Global.TaskMessageQueue.Post( msg ).Wait( 5000 ) )
                    throw new TimeoutException( "サイズ変更タスクメッセージがタイムアウトしました。" );
            }

            base.OnResize( e );
        }

        private bool _リサイズ中 = false;



        // その他


        /// <summary>
        ///     いずれかのスレッドで例外が発生したことを通知する。
        /// </summary>
        /// <remarks>
        ///     通常、メインスレッド（UIスレッド）以外のスレッドで例外が発生した場合、
        ///     デバッグ環境ではデバッガで catch することもできるが、リリース環境では放置される。
        ///     そのため、メインスレッド以外のスレッドで致命的な例外が発生した場合にこのメソッドを呼び出すと、
        ///     他スレッドで致命的な例外が発生したことをメインスレッドに通知することができる。
        /// </remarks>
        public void 例外を通知する( Exception e )
        {
            this.BeginInvoke( new Action( () => {   // UIスレッドで実行する
                throw e;
            } ) );
        }



        // ローカル


        /// <summary>
        ///     起動直後は true, OnLoad で false, OnClosing で true になる。
        /// </summary>
        private bool _未初期化 = true;

        /// <summary>
        ///     このフォームのウィンドウメッセージ処理。
        /// </summary>
        protected override void WndProc( ref Message msg )
        {
            const int WM_INPUT = 0x00FF;

            switch( msg.Msg )
            {
                case WM_INPUT:
                    this.OnInput( msg );
                    break;
            }

            base.WndProc( ref msg );
        }

        /// <summary>
        ///     キー押下。
        /// </summary>
        protected override void OnKeyDown( KeyEventArgs e )
        {
            #region " F11 → 全画面／ウィンドウモードを切り替える。"
            //----------------
            if( e.KeyCode == Keys.F11 )
            {
                // ScreenMode は非同期処理なので、すぐに値が反映されるとは限らない。
                // なので、ログオン中のユーザへの設定は、その変更より先に行なっておく。
                Global.App.システム設定.全画面モードである = this.ScreenMode.IsWindowMode; // 先に設定するので Mode が逆になっていることに注意。

                if( this.ScreenMode.IsWindowMode )
                    this.ScreenMode.ToFullscreenMode();
                else
                    this.ScreenMode.ToWindowMode();
            }
            //----------------
            #endregion

            base.OnKeyDown( e );
        }

        /// <summary>
        ///     WM_INPUT 処理。
        /// </summary>
        protected virtual void OnInput( in Message msg )
        {
            RawInput.RawInputData rawInputData;

            #region " RawInput データを取得する。"
            //----------------
            unsafe 
            {
                // RawInputData 構造体（可変長）の実サイズを取得する。
                int dataSize = 0;
                if( 0 > RawInput.GetRawInputData( msg.LParam, RawInput.DataType.Input, null, ref dataSize, Marshal.SizeOf<RawInput.RawInputHeader>() ) )
                {
                    Log.ERROR( $"GetRawInputData(): error = { Marshal.GetLastWin32Error()}" );
                    return;
                }

                // RawInputData 構造体の実データを取得する。
                var dataBytes = stackalloc byte[ dataSize ];
                if( 0 > RawInput.GetRawInputData( msg.LParam, RawInput.DataType.Input, dataBytes, &dataSize, Marshal.SizeOf<RawInput.RawInputHeader>() ) )
                {
                    Log.ERROR( $"GetRawInputData(): error = { Marshal.GetLastWin32Error()}" );
                    return;
                }

                // 取得された実データは byte[] なので、これを RawInputData 構造体に変換する。
                rawInputData = Marshal.PtrToStructure<RawInput.RawInputData>( new IntPtr( dataBytes ) );
            }
            //----------------
            #endregion

            this.KeyboardHID.OnInput( rawInputData );
            this.GameControllersHID.OnInput( rawInputData );
        }
    }
}
