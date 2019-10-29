using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SharpDX.Multimedia;

namespace DTXMania2
{
    class KeyboardHID : IDisposable
    {

        // プロパティ


        /// <summary>
        ///     発生した入力イベントのリスト。
        ///     <see cref="ポーリングする()"/> を呼び出す度に更新される。
        /// </summary>
        public List<InputEvent> 入力イベントリスト { get; protected set; } = new List<InputEvent>();



        // 生成と終了


        public KeyboardHID()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            // 登録したいデバイスの配列（ここでは１個）。
            var devs = new RawInput.RawInputDevice[] {
                new RawInput.RawInputDevice {
                    usUsagePage = UsagePage.Generic,    // Genericページの
                    usUsage = UsageId.GenericKeyboard,  // Genericキーボード。
                    Flags = RawInput.DeviceFlags.None,
                    hwndTarget = IntPtr.Zero,
                },
            };

            // デバイスを登録。
            RawInput.RegisterRawInputDevices( devs, 1, Marshal.SizeOf<RawInput.RawInputDevice>() );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );
        }



        // WM_INPUT


        /// <summary>
        ///     HIDキーボードからの WM_INPUT のコールバック。
        /// </summary>
        /// <remarks>
        ///     UIフォームのウィンドウメッセージループで WM_INPUT を受信した場合は、このコールバックを呼び出すこと。
        /// </remarks>
        public void OnInput( in RawInput.RawInputData rawInput )
        {
            if( rawInput.Header.Type != RawInput.DeviceType.Keyboard )
                return; // Keyboard 以外は無視。

            var keyboard = rawInput.Data.Keyboard;

            // InputEvent 作成。
            var inputEvent = new InputEvent() {
                DeviceID = 0,         // 固定
                Key = keyboard.VKey,  // 仮想キーコード(VK_*)
                押された = ( RawInput.ScanCodeFlags.Make == ( keyboard.Flags & RawInput.ScanCodeFlags.Break ) ),
                Velocity = 255,       // 固定
                TimeStamp = Stopwatch.GetTimestamp(),
                Extra = keyboard.ExtraInformation.ToString( "X8" ),
            };

            lock( this._一時入力イベントリスト )
            {
                // 一時リストに追加。
                this._一時入力イベントリスト.Add( inputEvent );

                // キーの状態を更新。
                this._現在のキーの押下状態[ inputEvent.Key ] = inputEvent.押された;
            }
        }



        // 入力


        public void ポーリングする()
        {
            this.入力イベントリスト.Clear();

            lock( this._一時入力イベントリスト )
            {
                this.入力イベントリスト = this._一時入力イベントリスト; // 一時リストへの参照を直接渡して、
                this._一時入力イベントリスト = new List<InputEvent>();  // 一時リストは新しく確保。
            }
        }

        /// <summary>
        ///     最後のポーリングでキーが押されたならtrueを返す。
        /// </summary>
        /// <param name="deviceID">無効。常に 0 を指定。</param>
        /// <param name="key">仮想キーコード(VK_*)。</param>
        public bool キーが押された( int deviceID, int key )
            => this.キーが押された( deviceID, key, out _ );

        /// <summary>
        ///     最後のポーリングでキーが押されたならtrueを返す。
        /// </summary>
        /// <param name="deviceID">無効。常に 0 を指定。</param>
        /// <param name="key">仮想キーコード(VK_*)。</param>
        /// <param name="ev">対応する入力イベント。押されていないなら null 。</param>
        public bool キーが押された( int deviceID, int key, out InputEvent? ev )
        {
            lock( this._一時入力イベントリスト )
            {
                ev = this.入力イベントリスト.Find( ( item ) => ( item.Key == key && item.押された ) );
            }

            return ( null != ev );
        }

        /// <summary>
        ///     最後のポーリングでキーが押されたならtrueを返す。
        /// </summary>
        /// <param name="deviceID">無効。常に 0 を指定。</param>
        /// <param name="key">キーコード。</param>
        public bool キーが押された( int deviceID, System.Windows.Forms.Keys key )
            => this.キーが押された( deviceID, (int) key, out _ );

        /// <summary>
        ///     最後のポーリングでキーが押されたならtrueを返す。
        /// </summary>
        /// <param name="deviceID">無効。常に 0 を指定。</param>
        /// <param name="key">キーコード。</param>
        /// <param name="ev">対応する入力イベント。押されていないなら null 。</param>
        public bool キーが押された( int deviceID, System.Windows.Forms.Keys key, out InputEvent? ev )
            => this.キーが押された( deviceID, (int) key, out ev );

        /// <summary>
        ///     現在キーが押下状態ならtrueを返す。
        /// </summary>
        /// <param name="deviceID">無効。常に 0 を指定。</param>
        /// <param name="key">仮想キーコード(VK_*)。</param>
        public bool キーが押されている( int deviceID, int key )
        {
            lock( this._一時入力イベントリスト )
            {
                return ( this._現在のキーの押下状態.TryGetValue( key, out bool 押されている ) ) ? 押されている : false;
            }
        }

        /// <summary>
        ///     現在キーが押下状態ならtrueを返す。
        /// </summary>
        /// <param name="deviceID">無効。常に 0 を指定。</param>
        /// <param name="key">キーコード。</param>
        public bool キーが押されている( int deviceID, System.Windows.Forms.Keys key )
            => this.キーが押されている( deviceID, (int) key );

        /// <summary>
        ///     最後のポーリングでキーが離されたならtrueを返す。
        /// </summary>
        /// <param name="deviceID">無効。常に 0 を指定。</param>
        /// <param name="key">仮想キーコード(VK_*)。</param>
        public bool キーが離された( int deviceID, int key )
            => this.キーが離された( deviceID, key, out _ );

        /// <summary>
        ///     最後のポーリングでキーが離されたならtrueを返す。
        /// </summary>
        /// <param name="deviceID">無効。常に 0 を指定。</param>
        /// <param name="key">仮想キーコード(VK_*)。</param>
        /// <param name="ev">対応する入力イベント。押されていないなら null 。</param>
        public bool キーが離された( int deviceID, int key, out InputEvent? ev )
        {
            lock( this._一時入力イベントリスト )
            {
                ev = this.入力イベントリスト.Find( ( item ) => ( item.Key == key && item.離された ) );
            }

            return ( null != ev );
        }

        /// <summary>
        ///     最後のポーリングでキーが離されたならtrueを返す。
        /// </summary>
        /// <param name="deviceID">無効。常に 0 を指定。</param>
        /// <param name="key">キーコード。</param>
        public bool キーが離された( int deviceID, System.Windows.Forms.Keys key )
            => this.キーが離された( deviceID, (int) key, out _ );

        /// <summary>
        ///     最後のポーリングでキーが離されたならtrueを返す。
        /// </summary>
        /// <param name="deviceID">無効。常に 0 を指定。</param>
        /// <param name="key">キーコード。</param>
        /// <param name="ev">対応する入力イベント。押されていないなら null 。</param>
        public bool キーが離された( int deviceID, System.Windows.Forms.Keys key, out InputEvent? ev )
            => this.キーが離された( deviceID, (int) key, out ev );

        /// <summary>
        ///     現在キーが非押下状態ならtrueを返す。
        /// </summary>
        /// <param name="deviceID">無効。常に 0 を指定。</param>
        /// <param name="key">仮想キーコード(VK_*)。</param>
        public bool キーが離されている( int deviceID, int key )
        {
            lock( this._一時入力イベントリスト )
            {
                return ( this._現在のキーの押下状態.TryGetValue( key, out bool 押されている ) ) ? !( 押されている ) : true;
            }
        }

        /// <summary>
        ///     現在キーが非押下状態ならtrueを返す。
        /// </summary>
        /// <param name="deviceID">無効。常に 0 を指定。</param>
        /// <param name="key">キーコード。</param>
        public bool キーが離されている( int deviceID, System.Windows.Forms.Keys key )
            => this.キーが離されている( deviceID, (int) key );



        // ローカル


        /// <summary>
        ///     <see cref="OnInput(in RawInput.RawInputData)"/> で受け取ったイベントを一時的に蓄えておくリスト。
        /// </summary>
        /// <remarks>
        ///     <see cref="ポーリングする()"/> の実行で、内容を <see cref="入力イベントリスト"/> にコピーしたのち、クリアされる。
        ///     アクセス時には必ずこのインスタンス自身を lock すること。
        /// </remarks>
        private List<InputEvent> _一時入力イベントリスト = new List<InputEvent>();

        /// <summary>
        ///	    現在のキーの押下状態。
        ///	    [key: 仮想キーコードをintにしたもの]
        ///	    true なら押されている状態、false なら離されている状態。
        /// </summary>
        private readonly Dictionary<int, bool> _現在のキーの押下状態 = new Dictionary<int, bool>();
    }
}
