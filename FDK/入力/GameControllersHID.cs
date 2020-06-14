using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SharpDX.Multimedia;

namespace FDK
{
    public class GameControllersHID : IDisposable
    {

        // プロパティ


        /// <summary>
        ///     発生した入力イベントのリスト。
        ///     <see cref="ポーリングする()"/> を呼び出す度に更新される。
        /// </summary>
        public List<InputEvent> 入力イベントリスト { get; protected set; }

        /// <summary>
        ///     HIDデバイスリスト。
        ///     [key: HIDデバイスハンドル]
        /// </summary>
        /// <remarks>
        ///     ゲームコントローラデバイスを Raw Input で取得する場合、実際に WM_INPUT が流れてくるまで
        ///     どんなデバイスが接続されているかを確認しない。（リアルタイムで接続したデバイスも流れてくるので、
        ///     最初に列挙しても意味が薄いため。）
        ///     したがって、このデバイスリストも、WM_INPUTで新しいデバイスからの入力が届くたびに増やしていくことにする。
        /// </remarks>
        public Dictionary<IntPtr, GameControllerHIDProperty> Devices { get; protected set; }


        // 生成と終了


        /// <summary>
        ///     コンストラクタ。
        ///     ゲームコントローラデバイスをRawInputに登録する。
        /// </summary>
        /// <param name="hWindow">
        ///     対象とするウィンドウのハンドル。<see cref="IntPtr.Zero"/> にすると、キーボードフォーカスに追従する。
        /// </param>
        public GameControllersHID( IntPtr hWindow, SoundTimer soundTimer )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._SoundTimer = soundTimer;

            this.入力イベントリスト = new List<InputEvent>();
            this.Devices = new Dictionary<IntPtr, GameControllerHIDProperty>();

            // 登録したいデバイスの配列（ここでは２個）。
            var devs = new RawInput.RawInputDevice[] {
                new RawInput.RawInputDevice {
                    usUsagePage = UsagePage.Generic,    // Genericページの
                    usUsage = UsageId.GenericGamepad,   // Genericゲームパッドと、
                    Flags = RawInput.DeviceFlags.None,
                    hwndTarget = hWindow,
                },
                new RawInput.RawInputDevice {
                    usUsagePage = UsagePage.Generic,    // Genericページの
                    usUsage = UsageId.GenericJoystick,  // Genericジョイスティック。
                    Flags = RawInput.DeviceFlags.None,
                    hwndTarget = hWindow,
                }
            };

            // デバイスを登録。
            RawInput.RegisterRawInputDevices( devs, devs.Length, Marshal.SizeOf<RawInput.RawInputDevice>() );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            // デバイスプロパティを解放。
            foreach( var kvp in this.Devices )
                kvp.Value.Dispose();
            this.Devices.Clear();

            this._SoundTimer = null!;
        }



        // WM_INPUT


        /// <summary>
        ///     ゲームコントローラHID からの WM_INPUT のコールバック。
        /// </summary>
        /// <remarks>
        ///     UIフォームのウィンドウメッセージループで WM_INPUT を受信した場合は、このコールバックを呼び出すこと。
        /// </remarks>
        public void OnInput( in RawInput.RawInputData rawInputData )
        {
            if( rawInputData.Header.Type != RawInput.DeviceType.HumanInputDevice )
                return; // Keyboard, Mouse は無視


            GameControllerHIDProperty deviceProperty;

            #region " デバイスプロパティを取得する。"
            //----------------
            if( this.Devices.ContainsKey( rawInputData.Header.hDevice ) )
            {
                // (A) すでに登録済みならデバイスリストから取得。

                deviceProperty = this.Devices[ rawInputData.Header.hDevice ];
            }
            else
            {
                // (B) データの発信元デバイスがデバイスリストにないなら、登録する。

                string deviceName;

                #region " デバイス名を取得する。"
                //----------------
                {
                    int nameSize = 0;

                    if( 0 > RawInput.GetRawInputDeviceInfo( rawInputData.Header.hDevice, RawInput.DeviceInfoType.DeviceName, IntPtr.Zero, ref nameSize ) )
                    {
                        Log.ERROR( $"GetRawInputDeviceInfo(): error = { Marshal.GetLastWin32Error()}" );
                        return;
                    }

                    IntPtr deviceNamePtr = Marshal.AllocHGlobal( nameSize * sizeof( char ) );

                    if( 0 > RawInput.GetRawInputDeviceInfo( rawInputData.Header.hDevice, RawInput.DeviceInfoType.DeviceName, deviceNamePtr, ref nameSize ) )
                    {
                        Marshal.FreeHGlobal( deviceNamePtr );
                        Log.ERROR( $"GetRawInputDeviceInfo(): error = { Marshal.GetLastWin32Error()}" );
                        return;
                    }

                    deviceName = Marshal.PtrToStringAuto( deviceNamePtr, nameSize ) ?? "Unknown";

                    Marshal.FreeHGlobal( deviceNamePtr );
                }
                //----------------
                #endregion


                IntPtr preparseData;

                #region " デバイスの事前解析データを取得する。"
                //----------------
                {
                    int pcbSize = 0;

                    if( 0 != RawInput.GetRawInputDeviceInfo( rawInputData.Header.hDevice, RawInput.DeviceInfoType.PreparsedData, IntPtr.Zero, ref pcbSize ) )
                    {
                        Log.ERROR( $"GetRawInputDeviceInfo(): error = { Marshal.GetLastWin32Error()}" );
                        return;
                    }

                    preparseData = Marshal.AllocHGlobal( pcbSize );

                    if( pcbSize != RawInput.GetRawInputDeviceInfo( rawInputData.Header.hDevice, RawInput.DeviceInfoType.PreparsedData, preparseData, ref pcbSize ) )
                    {
                        Marshal.FreeHGlobal( preparseData );
                        Log.ERROR( $"GetRawInputDeviceInfo(): error = { Marshal.GetLastWin32Error()}" );
                        return;
                    }
                }
                //----------------
                #endregion


                HID.Caps caps;

                #region " デバイスの CAPS を取得する。"
                //----------------
                {
                    if( HID.Status.Success != HID.HidP_GetCaps( preparseData, out caps ) )
                    {
                        Marshal.FreeHGlobal( preparseData );
                        Log.ERROR( $"HidP_GetCaps() error" );
                        return;
                    }
                }
                //----------------
                #endregion


                HID.ButtonCaps[] buttonCaps;
                bool[][] buttonState;

                #region " デバイスの ButtonCaps を取得し、ButtonState を作成する。"
                //----------------
                {
                    ushort buttonCapsLength = caps.NumberInputButtonCaps;
                    buttonCaps = new HID.ButtonCaps[ buttonCapsLength ];

                    if( HID.Status.Success != HID.HidP_GetButtonCaps( HID.ReportType.Input, buttonCaps, ref buttonCapsLength, preparseData ) )
                    {
                        Marshal.FreeHGlobal( preparseData );
                        Log.ERROR( $"HidP_GetButtonCaps() error" );
                        return;
                    }

                    buttonState = new bool[ buttonCaps.Length ][];

                    for( int b = 0; b < buttonCaps.Length; b++ )
                    {
                        if( buttonCaps[ b ].IsRange )
                        {
                            buttonState[ b ] = new bool[ buttonCaps[ b ].Range.UsageMax - buttonCaps[ b ].Range.UsageMin + 1 ];
                            for( int i = 0; i < buttonState[ b ].Length; i++ )
                                buttonState[ b ][ i ] = false;

                        }
                        else
                        {
                            buttonState[ b ] = new bool[ 1 ] { false };
                        }
                    }
                }
                //----------------
                #endregion


                HID.ValueCaps[] valueCaps;

                #region " デバイスの ValueCaps を取得する。"
                //----------------
                {
                    uint valueCapsLength = caps.NumberInputValueCaps;
                    valueCaps = new HID.ValueCaps[ valueCapsLength ];

                    if( HID.Status.Success != HID.HidP_GetValueCaps( HID.ReportType.Input, valueCaps, ref valueCapsLength, preparseData ) )
                    {
                        Marshal.FreeHGlobal( preparseData );
                        Log.ERROR( $"HidP_GetValueCaps() error" );
                        return;
                    }
                }
                //----------------
                #endregion


                HID.LinkCollectionNode[] collectionNodes;

                #region " デバイスのコレクションノードリストを取得する。"
                //----------------
                {
                    uint collectionNodesLength = caps.NumberLinkCollectionNodes;
                    collectionNodes = new HID.LinkCollectionNode[ collectionNodesLength ];

                    if( HID.Status.Success != HID.HidP_GetLinkCollectionNodes( collectionNodes, ref collectionNodesLength, preparseData ) )
                    {
                        Marshal.FreeHGlobal( preparseData );
                        Log.ERROR( $"HidP_GetCollectionNodes() error" );
                        return;
                    }
                }
                //----------------
                #endregion


                #region " プロパティインスタンスを生成して、デバイスリストに登録する。 "
                //----------------
                deviceProperty = new GameControllerHIDProperty() {
                    DeviceID = this.Devices.Count,  // n 番目の DeviceID は n (n = 0...)
                    Name = deviceName,
                    PreparseData = preparseData,
                    Caps = caps,
                    ButtonCaps = buttonCaps,
                    ButtonState = buttonState,
                    ValueCaps = valueCaps,
                    CollectionNodes = collectionNodes,
                };
                this.Devices.Add( rawInputData.Header.hDevice, deviceProperty );
                //----------------
                #endregion

                Log.Info( $"新しいゲームコントローラ {deviceProperty.DeviceID} を認識しました。" );
            }
            //----------------
            #endregion


            byte[][] rawHidReports;

            #region " RawInput データからHIDレポートを取得する。"
            //----------------
            {
                rawHidReports = new byte[ rawInputData.Data.Hid.Count ][];

                // インライン配列からバイト配列を取得。
                unsafe
                {
                    fixed( RawInput.RawInputData* pData = &rawInputData )
                    {
                        byte* pRawHidData = ( (byte*) ( &pData->Data.Hid ) ) +
                            sizeof( int ) + // sizeof( RawHid.SizeHid )
                            sizeof( int );  // sizeof( RawHid.Count )

                        for( int c = 0; c < rawInputData.Data.Hid.Count; c++ )
                        {
                            rawHidReports[ c ] = new byte[ rawInputData.Data.Hid.SizeHid ];

                            for( int p = 0; p < rawInputData.Data.Hid.SizeHid; p++ )
                                rawHidReports[ c ][ p ] = *pRawHidData++;
                        }
                    }
                }
            }
            //----------------
            #endregion


            // すべての生HIDレポートについて、内容を解析し、入力として処理する。

            for( int nReport = 0; nReport < rawHidReports.GetLength( 0 ); nReport++ )
            {
                #region " (1) Button 入力を確認する。"
                //----------------
                for( int nButtonCap = 0; nButtonCap < deviceProperty.Caps.NumberInputButtonCaps; nButtonCap++ )
                {
                    var buttonCap = deviceProperty.ButtonCaps[ nButtonCap ];
                    uint usageLength = HID.HidP_MaxUsageListLength( HID.ReportType.Input, buttonCap.UsagePage, deviceProperty.PreparseData ?? IntPtr.Zero );
                    var usageList = new ushort[ usageLength ];

                    var status = HID.HidP_GetButtons(
                        HID.ReportType.Input,
                        buttonCap.UsagePage,
                        buttonCap.LinkCollection,
                        usageList,
                        ref usageLength,
                        deviceProperty.PreparseData ?? IntPtr.Zero,
                        rawHidReports[ nReport ],
                        (uint) rawHidReports[ nReport ].Length );

                    if( status == HID.Status.Success )
                    {
                        // current を prev にコピーしつつ current を false で初期化。
                        var currentButtonState = deviceProperty.ButtonState[ nButtonCap ];
                        var prevButtonState = new bool[ currentButtonState.Length ];
                        for( int usageIndex = 0; usageIndex < currentButtonState.Length; usageIndex++ )
                        {
                            prevButtonState[ usageIndex ] = currentButtonState[ usageIndex ];
                            currentButtonState[ usageIndex ] = false;
                        }

                        // ON 通知のあった current を true にする。
                        for( int usageIndex = 0; usageIndex < usageLength; usageIndex++ )
                            currentButtonState[ usageList[ usageIndex ] - buttonCap.UsageMin ] = true;

                        // prev から current で変化のあった usage に対して InputEvent を作成。
                        for( int usageIndex = 0; usageIndex < currentButtonState.Length; usageIndex++ )
                        {
                            if( prevButtonState[ usageIndex ] != currentButtonState[ usageIndex ] )
                            {
                                ushort usagePage = buttonCap.UsagePage;
                                ushort usage = (ushort) ( usageIndex + buttonCap.UsageMin );

                                var inputEvent = new InputEvent() {
                                    DeviceID = deviceProperty.DeviceID,
                                    Key = usagePage << 16 | usage,  // ExtendUsage = 上位16bit:UsagePage, 下位16bit:Usage
                                    押された = currentButtonState[ usageIndex ],
                                    Velocity = 255,       // 固定
                                    TimeStamp = this._SoundTimer.現在時刻sec,
                                    Extra = $"{buttonCap.UsagePageName} / {HID.GetUsageName( usagePage, usage )}",
                                };

                                lock( this._一時入力イベントリスト )
                                {
                                    // 一時リストに追加。
                                    this._一時入力イベントリスト.Add( inputEvent );

                                    // キーの状態を更新。
                                    this._現在のキーの押下状態[ inputEvent.Key ] = inputEvent.押された;
                                }
                            }
                        }
                    }
                }
                //----------------
                #endregion

                // hack: (2) Value 入力を確認する。--> 現状、軸入力やアナログ入力には未対応。
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
        /// <param name="deviceID"></param>
        /// <param name="extendedUsage">上位16bit:UsagePage, 下位16bit:Usage</param>
        public bool キーが押された( int deviceID, int extendedUsage )
            => this.キーが押された( deviceID, extendedUsage, out _ );

        /// <summary>
        ///     最後のポーリングでキーが押されたならtrueを返す。
        /// </summary>
        /// <param name="deviceID"></param>
        /// <param name="extendedUsage">上位16bit:UsagePage, 下位16bit:Usage</param>
        /// <param name="ev">対応する入力イベント。押されていないなら null 。</param>
        public bool キーが押された( int deviceID, int extendedUsage, out InputEvent? ev )
        {
            lock( this._一時入力イベントリスト )
                ev = this.入力イベントリスト.Find( ( item ) => ( item.Key == extendedUsage && item.押された ) );

            return ( null != ev );
        }

        /// <summary>
        ///     現在キーが押下状態ならtrueを返す。
        /// </summary>
        /// <param name="deviceID"></param>
        /// <param name="extendedUsage">上位16bit:UsagePage, 下位16bit:Usage</param>
        public bool キーが押されている( int deviceID, int extendedUsage )
        {
            lock( this._一時入力イベントリスト )
            {
                return ( this._現在のキーの押下状態.TryGetValue( extendedUsage, out bool 押されている ) ) ? 押されている : false;
            }
        }

        /// <summary>
        ///     最後のポーリングでキーが離されたならtrueを返す。
        /// </summary>
        /// <param name="deviceID"></param>
        /// <param name="extendedUsage">上位16bit:UsagePage, 下位16bit:Usage</param>
        public bool キーが離された( int deviceID, int extendedUsage )
            => this.キーが離された( deviceID, extendedUsage, out _ );

        /// <summary>
        ///     最後のポーリングでキーが離されたならtrueを返す。
        /// </summary>
        /// <param name="deviceID"></param>
        /// <param name="extendedUsage">上位16bit:UsagePage, 下位16bit:Usage</param>
        /// <param name="ev">対応する入力イベント。押されていないなら null 。</param>
        public bool キーが離された( int deviceID, int extendedUsage, out InputEvent? ev )
        {
            lock( this._一時入力イベントリスト )
            {
                ev = this.入力イベントリスト.Find( ( item ) => ( item.Key == extendedUsage && item.離された ) );
            }

            return ( null != ev );
        }

        /// <summary>
        ///     現在キーが非押下状態ならtrueを返す。
        /// </summary>
        /// <param name="deviceID"></param>
        /// <param name="extendedUsage">上位16bit:UsagePage, 下位16bit:Usage</param>
        public bool キーが離されている( int deviceID, int extendedUsage )
        {
            lock( this._一時入力イベントリスト )
            {
                return ( this._現在のキーの押下状態.TryGetValue( extendedUsage, out bool 押されている ) ) ? !( 押されている ) : true;
            }
        }



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

        private SoundTimer _SoundTimer;
    }
}
