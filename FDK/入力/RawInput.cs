using System;
using System.Runtime.InteropServices;
using SharpDX.Multimedia;

namespace FDK
{
    /// <summary>
    ///     Raw Input 関連のクラス、構造体、Win32APIの定義。
    /// </summary>
    /// <remarks>
    ///     RawInput はすべて GUI スレッドで実行すること。
    /// </remarks>
    public class RawInput
    {
        [Flags]
        public enum DeviceFlags : uint
        {
            /// <summary>
            ///     既定のデバイス。
            /// </summary>
            None = 0x00000000,

            /// <summary>
            ///     アプリケーションキーを扱う。
            /// </summary>
            /// <remarks>
            ///     このフラグを設定すると、アプリケーションコマンドキーを扱うことができる。
            ///     このフラグは、キーボードデバイスに対して <see cref="NoLegacy"/> フラグが設定されている時のみ設定することができる。
            /// </remarks>
            AppKeys = 0x00000400,

            /// <summary>
            ///     マウスボタンをキャプチャする。
            /// </summary>
            /// <remarks>
            ///     このフラグを設定すると、マウスボタンをクリックしても、他のウィンドウをアクティブにしなくなる。
            /// </remarks>
            CaptureMouse = 0x00000200,

            /// <summary>
            ///     デバイスの接続・取り外しを通知する。
            /// </summary>
            /// <remarks>
            ///     このフラグを設定すると、デバイスが接続または取り外されたときに WM_INPUT_DEVICE_CHANGE メッセージが発行される。
            ///     このフラグは、Windows Vista 以降でのみサポートされる。
            /// </remarks>
            DeviceNotify = 0x00002000,

            /// <summary>
            ///     特定の TLC を除外する。
            /// </summary>
            /// <remarks>
            ///     このフラグを設定すると、特定の Usage Page に属する Top Level Collection を除外することを示す。
            ///     このフラグは、<see cref="PageOnly"/> が指定された Usage Page の TLC に対してのみ有効。
            /// </remarks>
            Exclude = 0x00000010,

            /// <summary>
            ///     バックグラウンドで入力を排他的に取得する。
            /// </summary>
            /// <remarks>
            ///     このフラグを設定すると、フォアグラウンドアプリケーションが処理しなかった入力を、バックグラウンドで受け取ることができる。
            ///     言い換えれば、フォアグラウンドアプリケーションが Raw Input に登録されておらず、バックグラウンドアプリケーションが登録されているなら、入力を受け取ることができる。
            ///     このフラグは、Windows Vista 以降でのみサポートされる。
            /// </remarks>
            ExclusiveInputSink = 0x00001000,

            /// <summary>
            ///     バックグラウンドで入力を取得する。
            /// </summary>
            /// <remarks>
            ///     このフラグを設定すると、フォアグラウンドにない状態でも入力を受け取ることができる。
            ///     <see cref="RawInputDevice.hwndTarget"/> が必ず設定されていること。
            /// </remarks>
            InputSink = 0x00000100,

            /// <summary>
            ///     アプリケーション定義のホットキーを無効化する。
            /// </summary>
            /// <remarks>
            ///     このフラグを設定すると、アプリケーション定義のキーボードデバイスホットキーが扱われなくなる。
            ///     ただし、システム定義のホットキー（例えば ALT+TAB や CTRL+ALT+DEL）は扱われる。
            ///     既定では、すべてのキーボードホットキーが扱われる。
            ///     このフラグは、<see cref="NoLegacy"/> フラグが未設定で <see cref="RawInputDevice.hwndTarget"/> が <see cref="IntPtr.Zero"/> である場合でも設定することができる。
            /// </remarks>
            NoHotKeys = 0x00000200,

            /// <summary>
            ///     レガシーメッセージを抑制する。
            /// </summary>
            /// <remarks>
            ///     このフラグを設定すると、<see cref="RawInputDevice.usUsagePage"/> または <see cref="RawInputDevice.usUsage"/> で指定されたすべてのデバイスに対して、
            ///     レガシーメッセージを生成しなくなる。
            ///     これは、マウスとキーボードに対してのみ有効。
            /// </remarks>
            NoLegacy = 0x00000030,

            /// <summary>
            ///     特定の Usage Page の全デバイスを使用する。
            /// </summary>
            /// <remarks>
            ///     このフラグを設定すると、指定された <see cref="RawInputDevice.usUsagePage"/> の Top Level Collection に属するすべてのデバイスが対象となる。
            ///     <see cref="RawInputDevice.usUsage"/> はゼロでなければならない。
            ///     特定の TLC を除外するには、<see cref="Exclude"/> フラグを使用する。
            /// </remarks>
            PageOnly = 0x00000020,

            /// <summary>
            ///     デバイスを対象外にする。
            /// </summary>
            /// <remarks>
            ///     このフラグを設定すると、Top Level Collection が対象から外される。
            ///     このフラグは、OS に対して、その TLC に適合するデバイスからの読み込みを停止するよう通知する。
            /// </remarks>
            Remove = 0x00000001,
        }

        public enum DataType : uint
        {
            /// <summary>
            ///     <see cref="RawInputData"/> からヘッダ情報を取得する。
            /// </summary>
            Header = 0x10000005,

            /// <summary>
            ///     <see cref="RawInputData"/> から生データを取得する。
            /// </summary>
            Input = 0x10000003,
        }

        public enum DeviceType : uint
        {
            /// <summary>
            ///     マウスからの Raw Input を扱う。
            /// </summary>
            Mouse = 0,

            /// <summary>
            ///     キーボードからの Raw Input を扱う。
            /// </summary>
            Keyboard = 1,

            /// <summary>
            ///     マウスとキーボード以外の任意のデバイスからの Raw Input を扱う。
            /// </summary>
            HumanInputDevice = 2,
        }

        [Flags]
        public enum ScanCodeFlags : short
        {
            /// <summary>
            ///     キーは押されている。
            /// </summary>
            Make = 0x0000,

            /// <summary>
            ///     キーは離されている。
            /// </summary>
            Break = 0x0001,

            /// <summary>
            ///     スキャンコードは、E0 エスケープを持っている。
            /// </summary>
            E0 = 0x0002,

            /// <summary>
            ///     スキャンコードは、E1 エスケープを持っている。
            /// </summary>
            E1 = 0x0004,
        }

        public enum DeviceInfoType : uint
        {
            /// <summary>
            ///     デバイス名を表す文字列。
            /// </summary>
            DeviceName = 0x20000007,

            /// <summary>
            ///     <see cref="DeviceInfo"/> 構造体。
            /// </summary>
            DeviceInfo = 0x2000000b,

            /// <summary>
            ///     事前解析されたデータ。
            /// </summary>
            PreparsedData = 0x20000005,
        }

        [Flags]
        public enum MouseButtonFlags : short
        {
            None = 0x0000,

            /// <summary>
            ///     左ボタンが押された。
            /// </summary>
            Button1Down = 0x0001,

            /// <summary>
            ///     左ボタンが離された。
            /// </summary>
            Button1Up = 0x0002,

            /// <summary>
            ///     右ボタンが押された。
            /// </summary>
            Button2Down = 0x0004,

            /// <summary>
            ///     右ボタンが離された。
            /// </summary>
            Button2Up = 0x0008,

            /// <summary>
            ///     中ボタンが押された。
            /// </summary>
            Button3Down = 0x0010,

            /// <summary>
            ///     中ボタンが離された。
            /// </summary>
            Button3Up = 0x0020,

            /// <summary>
            ///     XBUTTON1 が押された。
            /// </summary>
            Button4Down = 0x0040,

            /// <summary>
            ///     XBUTTON1 が離された。
            /// </summary>
            Button4Up = 0x0080,

            /// <summary>
            ///     XBUTTON2 が押された。
            /// </summary>
            Button5Down = 0x0100,

            /// <summary>
            ///     XBUTTON2 が離された。
            /// </summary>
            Button5Up = 0x0200,

            /// <summary>
            ///     マウスホイールが回転した。
            ///     <see cref="RawMouse.ButtonsData"/> にホイールの回転差分量が格納されている。
            /// </summary>
            MouseWheel = 0x0400,

            /// <summary>
            ///     マウスホイールが水平方向に移動された。
            ///     <see cref="RawMouse.ButtonsData"/> が正なら右、負なら左を示す。
            /// </summary>
            Hwheel = 0x0800,

            // 以下別名

            /// <summary>
            ///     左ボタンが押された。
            /// </summary>
            LeftButtonDown = 0x0001,

            /// <summary>
            ///     左ボタンが離された。
            /// </summary>
            LeftButtonUp = 0x0002,

            /// <summary>
            ///     右ボタンが押された。
            /// </summary>
            RightButtonDown = 0x0004,

            /// <summary>
            ///     右ボタンが離された。
            /// </summary>
            RightButtonUp = 0x0008,

            /// <summary>
            ///     中ボタンが押された。
            /// </summary>
            MiddleButtonDown = 0x0010,

            /// <summary>
            ///     中ボタンが離された。
            /// </summary>
            MiddleButtonUp = 0x0020,
        }

        [Flags]
        public enum MouseMode : short
        {
            /// <summary>
            ///     マウスの移動データは、最後のマウス位置に対する相対位置である。
            /// </summary>
            MoveRelative = 0x0000,

            /// <summary>
            ///     マウスの移動データは、絶対位置である。
            /// </summary>
            MoveAbsolute = 0x0001,

            /// <summary>
            ///     マウス座標は、（マルチディスプレイシステム用の）仮想デスクトップにマップされる。
            /// </summary>
            VirtualDesktop = 0x0002,

            /// <summary>
            ///     マウスの属性が変化した。
            /// </summary>
            /// <remarks>
            ///     アプリケーションは、マウスの属性をクエリする必要がある。
            /// </remarks>
            AttributesChanged = 0x0004,

            /// <summary>
            ///     WM_MOUSEMOVE メッセージは合体されない。
            /// </summary>
            /// <remarks>
            ///     Windows Vista 以降でサポートされる。
            ///     既定では、WM_MOUSEMOVE メッセージは合体される。
            /// </remarks>
            MoveNoCoalesce = 0x0008,
        }


        [StructLayout( LayoutKind.Sequential )]
        public struct RawInputDevice
        {
            /// <summary>
            ///     Raw Input デバイスの Top Level Collection Usage Page。
            /// </summary>
            public UsagePage usUsagePage;

            /// <summary>
            ///     Raw Input デバイスの Top Level Collection Usage。
            /// </summary>
            public UsageId usUsage;

            /// <summary>
            ///     提供された <see cref="RawInputDevice.usUsagePage"/> と <see cref="RawInputDevice.usUsage"/> をどのように解釈するかを示すフラグ。
            /// </summary>
            /// <remarks>
            ///     このフラグはゼロにすることができる（既定値）。
            ///     既定では、OS は、Top Level Collection (TLC) で指定された Raw Input を、登録されたアプリケーションに対して、そのウィンドウがフォーカスを得ている間、送信する。
            /// </remarks>
            public DeviceFlags Flags;

            /// <summary>
            ///     ターゲットウィンドウのハンドル。
            /// </summary>
            /// <remarks>
            ///     <see cref="IntPtr.Zero"/> の場合は、キーボードのフォーカスに従う。
            /// </remarks>
            public IntPtr hwndTarget;
        };

        [StructLayout( LayoutKind.Sequential )]
        public struct RawInputData
        {
            /// <summary>
            ///     Raw Input ヘッダ情報。
            /// </summary>
            public RawInputHeader Header;

            /// <summary>
            ///     Raw Input 生データ共同体。
            /// </summary>
            public RawInputUnionData0 Data;
        }

        [StructLayout( LayoutKind.Sequential )]
        public struct RawInputHeader
        {
            /// <summary>
            ///     Raw Inpu デバイスの種別。
            /// </summary>
            public DeviceType Type;

            /// <summary>
            ///     データの入力パケット全体のサイズ（バイト単位）。
            /// </summary>
            /// <remarks>
            ///     これには、<see cref="RawInputData"/> に加えて、<see cref="RawHid"/> 可変長配列の中の拡張入力レポートも（あるなら）含まれる。
            /// </remarks>
            public int Size;

            /// <summary>
            ///     Raw Input データを生成するデバイスのハンドル。
            /// </summary>
            public IntPtr hDevice;

            /// <summary>
            ///     WM_INPUT メッセージの <see cref="System.Windows.Forms.Message.WParam"/> で渡される値。
            /// </summary>
            public IntPtr wParam;
        }

        [StructLayout( LayoutKind.Explicit )]
        public struct RawInputUnionData0
        {
            /// <summary>
            ///     マウスの Raw Input データ。
            /// </summary>
            [FieldOffset( 0 )]
            public RawMouse Mouse;

            /// <summary>
            ///     キーボードの Raw Input データ。
            /// </summary>
            [FieldOffset( 0 )]
            public RawKeyboard Keyboard;

            /// <summary>
            ///     キーボードとマウス以外のデバイスの Raw Input データ。
            /// </summary>
            [FieldOffset( 0 )]
            public RawHid Hid;
        }

        [StructLayout( LayoutKind.Sequential )]
        public struct RawMouse
        {
            /// <summary>
            ///     マウスの状態。
            /// </summary>
            public MouseMode Flags;

            [StructLayout( LayoutKind.Explicit )]
            public struct RawMouseButtonsData
            {
                [FieldOffset( 0 )]
                public int Buttons;

                [FieldOffset( 0 )]
                public MouseButtonFlags ButtonFlags;

                /// <summary>
                ///     マウスホイールが水平または縦に移動すれば、ここに移動差分量が格納される。
                /// </summary>
                [FieldOffset( 2 )]
                public short ButtonData;
            }

            public RawMouseButtonsData ButtonsData;

            /// <summary>
            ///     生のボタンデータ。
            /// </summary>
            public int RawButtons;

            /// <summary>
            ///     X軸方向の移動量。
            /// </summary>
            /// <remarks>
            ///     この値は、符号付き相対移動量または絶対移動量であり、それは <see cref="Flags"/> に依存する。
            /// </remarks>
            public int LastX;

            /// <summary>
            ///     Y軸方向の移動量。
            /// </summary>
            /// <remarks>
            ///     この値は、符号付き相対移動量または絶対移動量であり、それは <see cref="Flags"/> に依存する。
            /// </remarks>
            public int LastY;

            /// <summary>
            ///     デバイス定義の追加情報。
            /// </summary>
            public int ExtraInformation;
        }

        [StructLayout( LayoutKind.Sequential, Pack = 1 )]
        public struct RawKeyboard
        {
            /// <summary>
            ///     スキャンコード。
            /// </summary>
            /// <remarks>
            ///     キーボードのオーバーランに対するスキャンコードは、KEYBOARD_OVERRUN_MAKE_CODE (0xFF) である。
            /// </remarks>
            public short MakeCode;

            /// <summary>
            ///     スキャンコード情報に関するフラグ。
            /// </summary>
            public ScanCodeFlags Flags;

            /// <summary>
            ///     予約済み；ゼロであること。
            /// </summary>
            public short Reserved;

            /// <summary>
            ///     Windows メッセージ互換の仮想キーコード。
            /// </summary>
            public short VKey;

            /// <summary>
            ///     対応するウィンドウメッセージ。
            ///     WM_KEYDOWN, WM_SYSKEYDOWN など。
            /// </summary>
            public uint Message;

            /// <summary>
            ///     デバイス定義の追加情報。
            /// </summary>
            public int ExtraInformation;
        }

        [StructLayout( LayoutKind.Sequential )]
        public struct RawHid
        {
            /// <summary>
            ///     <see cref="RawData"/> フィールド内のそれぞれの HID 入力のサイズ（バイト単位）。
            /// </summary>
            public int SizeHid;

            /// <summary>
            ///     <see cref="RawData"/> フィールド内の HID 入力の数.
            /// </summary>
            public int Count;

            /// <summary>
            ///     Type: BYTE[1]
            ///     Raw Input データ。バイトの配列。
            /// </summary>
            public byte RawData;
        }

        [StructLayout( LayoutKind.Sequential )]
        public struct RawInputDevicelist
        {
            /// <summary>	
            ///     Raw Input デバイスのハンドル。
            /// </summary>	
            public IntPtr Device;

            /// <summary>	
            ///     デバイスの種別。
            /// </summary>	
            public DeviceType Type;
        }

        [StructLayout( LayoutKind.Explicit )]
        public struct DeviceInfo
        {
            [FieldOffset( 0 )]
            public int Size;

            [FieldOffset( 4 )]
            public DeviceType Type;

            // 以下、共用体。

            [FieldOffset( 8 )]
            public DeviceInfoMouse Mouse;

            [FieldOffset( 8 )]
            public DeviceInfoKeyboard Keyboard;

            [FieldOffset( 8 )]
            public DeviceInfoHid Hid;
        }

        [StructLayout( LayoutKind.Sequential )]
        public struct DeviceInfoMouse
        {
            /// <summary>	
            ///     マウスデバイスの識別子。
            /// </summary>	
            public int Id;

            /// <summary>	
            ///     マウスのボタンの数。
            /// </summary>	
            public int NumberOfButtons;

            /// <summary>	
            ///     １秒あたりのデータ位置の数。
            /// </summary>	
            /// <remarks>
            ///     この情報は、必ずしもすべてのマウスデバイスに対して適用可能とは限らない。
            /// </remarks>
            public int SampleRate;

            /// <summary>
            ///     true なら、マウスは水平スクロール可能なホイールを持つ。そうでないなら false。
            /// </summary>
            /// <remarks>
            ///     このメンバは、Windows Vista 以降でのみサポートされる。
            /// </remarks>
            [MarshalAs( UnmanagedType.Bool )]
            public bool HasHorizontalWheel;
        }

        [StructLayout( LayoutKind.Sequential )]
        public struct DeviceInfoKeyboard
        {
            /// <summary>	
            ///     キーボードの種別。
            /// </summary>	
            public int Type;

            /// <summary>	
            ///     キーボードのサブタイプ。
            /// </summary>
            public int SubType;

            /// <summary>	
            ///     スキャンコードモード。
            /// </summary>	
            public int KeyboardMode;

            /// <summary>	
            ///     キーボード上のファンクションキーの数。
            /// </summary>	
            public int NumberOfFunctionKeys;

            /// <summary>
            ///     キーボード上の LED インジケータの数。
            /// </summary>	
            public int NumberOfIndicators;

            /// <summary>	
            ///     キーボード上のキーの総数。
            /// </summary>	
            public int NumberOfKeysTotal;
        }

        [StructLayout( LayoutKind.Sequential )]
        public struct DeviceInfoHid
        {
            /// <summary>
            ///     この HID の VendorID。
            /// </summary>	
            public int VendorId;

            /// <summary>
            ///     この HID の ProductID。
            /// </summary>	
            public int ProductId;

            /// <summary>
            ///     この HID のバージョン番号。
            /// </summary>	
            public int VersionNumber;

            /// <summary>
            ///     デバイスに対する Top Level Collection Usage Page。
            /// </summary>	
            public UsagePage UsagePage;

            /// <summary>
            ///     デバイスに対する Top Level Collection Usage。
            /// </summary>	
            public UsageId Usage;
        }


        /// <summary>
        ///     Raw Input デバイスを登録する。
        /// </summary>
        /// <param name="pRawInputDevices">Raw Input を供給するデバイスを表す <see cref="RawInputDevice"/> 構造体の配列。</param>
        /// <param name="uiNumDevices"><paramref name="pRawInputDevices"/> で示される <see cref="RawInputDevice"/> 構造体の数。</param>
        /// <param name="cbSize"><see cref="RawInputDevice"/> 構造体のサイズ（バイト単位）。</param>
        /// <returns>成功すれば true、失敗すれば false。</returns>
        [DllImport( "user32.dll", SetLastError = true )]
        [return: MarshalAs( UnmanagedType.Bool )]
        public static extern bool RegisterRawInputDevices( RawInputDevice[] pRawInputDevices, int uiNumDevices, int cbSize );

        /// <summary>
        ///     指定したデバイスから Raw Input を取得する。
        /// </summary>
        /// <param name="hDevice"><see cref="RawInputData"/> 構造体へのハンドル。WM_INPUT の lParam から取得される。</param>
        /// <param name="uiCommand">取得する内容を示すフラグ。</param>
        /// <param name="pData"><see cref="RawInputData"/> 構造体から得られるデータを示すポインタ。これは、<paramref name="uiCommand"/> の値に依存する。<see cref="IntPtr.Zero"/> を指定すると、必要なバッファのサイズが <paramref name="pcbSize"/> に格納される。</param>
        /// <param name="pcbSize"><paramref name="pData"/>メンバのサイズ（バイト単位）。</param>
        /// <param name="cbSizeHeader"><see cref="RawInputHeader"/> 構造体のサイズ（バイト単位）。</param>
        /// <returns>
        ///     <paramref name="pData"/> が <see cref="IntPtr.Zero"/> かつ成功した場合、0 が返される。
        ///     <paramref name="pData"/> が有効でかつ成功した場合、<paramref name="pData"/> にコピーされたバイト数を返す。
        ///     エラーが発生した場合は -1 が返される。
        /// </returns>
        [DllImport( "user32.dll", SetLastError = true )]
        public static extern int GetRawInputData( IntPtr hDevice, DataType uiCommand, out RawInputData pData, ref int pcbSize, int cbSizeHeader );

        [DllImport( "user32.dll", SetLastError = true )]
        public static extern int GetRawInputData( IntPtr hDevice, DataType uiCommand, ref IntPtr pData, ref int pcbSize, int cbSizeHeader );

        [DllImport( "user32.dll", SetLastError = true )]
        public static extern int GetRawInputData( IntPtr hDevice, DataType uiCommand, [In, Out] byte[]? pData, ref int pcbSize, int cbSizeHeader );

        [DllImport( "user32.dll", SetLastError = true )]
        public static unsafe extern int GetRawInputData( IntPtr hDevice, DataType uiCommand, byte* pData, int* pcbSize, int cbSizeHeader );

        /// <summary>
        ///     システムに接続されている Raw Input デバイスを列挙する。
        /// </summary>
        /// <param name="pRawInputDeviceList">システムに接続されあtデバイスの <see cref="RawInputDevicelist"/> 構造体の配列。null を指定すると、デバイスの数が <paramref name="uiNumDevices"/> に返される。</param>
        /// <param name="uiNumDevices">
        ///     <paramref name="pRawInputDeviceList"/> が null である場合、システムに接続されているデバイスの数がこの引数に格納される。
        ///     そうでない場合、<paramref name="pRawInputDeviceList"/> が示すバッファに含まれている <see cref="RawInputDevicelist"/> 構造体の数を指定する。
        ///     この値がシステムに接続されているデバイスの数よりも小さい場合、この引数には実際のデバイス数が返され、メソッドは ERROR_INSUFFICIENT_BUFFER エラーで失敗する。
        /// </param>
        /// <param name="cbSize"><see cref="RawInputDevicelist"/> 構造体のサイズ（バイト単位）。</param>
        /// <returns>成功した場合は、<paramref name="pRawInputDeviceList"/> に格納されたデバイスの数が返される。エラーが発生した場合は -1 が返される。</returns>
        [DllImport( "user32.dll", SetLastError = true )]
        public static extern int GetRawInputDeviceList( [In, Out] RawInputDevicelist[] pRawInputDeviceList, ref int uiNumDevices, int cbSize );

        /// <summary>
        ///     Raw Input デバイスの情報を取得する。
        /// </summary>
        /// <param name="hDevice">Raw Input デバイスのハンドル。この値は、<see cref="RawInputHeader.hDevice"/> または <see cref="GetRawInputDeviceList(RawInputDevicelist[], ref int, int)"/> から取得される。</param>
        /// <param name="uiCommand"><paramref name="pData"/> に何のデータが返されるかを示す。</param>
        /// <param name="pData">
        ///     <paramref name="uiCommand"/> で指定される情報を格納するバッファへのポインタ。
        ///     <paramref name="uiCommand"/> が <see cref="DeviceInfoType.DeviceInfo"/> である場合、このメソッドを呼び出す前に、<paramref name="pcbSize"/> に <see cref="DeviceInfo"/> 構造体のサイズ（バイト単位）を格納すること。
        /// </param>
        /// <param name="pcbSize"><paramref name="pData"/> に含まれるデータのサイズ（バイト単位）。</param>
        /// <returns>
        ///     成功した場合、このメソッドは 0 以上の値を返す。これは、<paramref name="pData"/> にコピーされたバイト数を示している。
        ///     <paramref name="pData"/> がデータに対して十分に大きくない場合、-1 が返される。
        ///     <paramref name="pData"/> が <see cref="IntPtr.Zero"/> である場合、0 が返される。
        ///     いずれの場合も、<paramref name="pcbSize"/> には <paramref name="pData"/> バッファに必要となる最小のサイズが設定される。
        /// </returns>
        [DllImport( "user32.dll", SetLastError = true, CharSet = CharSet.Auto )]
        public static extern uint GetRawInputDeviceInfo( IntPtr hDevice, DeviceInfoType uiCommand, [In, Out] IntPtr pData, ref int pcbSize );

        [DllImport( "user32.dll", SetLastError = true )]
        public static extern uint GetRawInputBuffer( [In, Out] RawInputData[] pData, ref uint cbSize, uint csSizeHeader );

        [DllImport( "user32.dll", SetLastError = true, EntryPoint = "GetRawInputBuffer" )]
        public static extern uint GetRawInputBufferB( ref IntPtr pData, ref uint cbSize, uint csSizeHeader );
    }
}
