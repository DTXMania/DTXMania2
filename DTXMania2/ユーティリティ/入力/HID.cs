using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DTXMania2
{
    /// <summary>
    ///     HID (Human Interface Devices) 関連の API
    /// </summary>
    public class HID
    {
        public enum ReportType : int
        {
            Input = 0,
            Output = 1,
            Feature = 2,
        }

        public enum Status : int
        {
            Success = ( 0x0 << 28 ) | ( 0x11 << 16 ) | 0,
            Null = ( 0x8 << 28 ) | ( 0x11 << 16 ) | 1,
            InvalidPreparsedData = ( 0xC << 28 ) | ( 0x11 << 16 ) | 1,
            InvalidReportType = ( 0xC << 28 ) | ( 0x11 << 16 ) | 2,
            InvalidReportLength = ( 0xC << 28 ) | ( 0x11 << 16 ) | 3,
            UsageNotFound = ( 0xC << 28 ) | ( 0x11 << 16 ) | 4,
            ValueOutOfRange = ( 0xC << 28 ) | ( 0x11 << 16 ) | 5,
            BadLogPhyValues = ( 0xC << 28 ) | ( 0x11 << 16 ) | 6,
            BufferTooSmall = ( 0xC << 28 ) | ( 0x11 << 16 ) | 7,
            InternalError = ( 0xC << 28 ) | ( 0x11 << 16 ) | 8,
            I8042TransUnknown = ( 0xC << 28 ) | ( 0x11 << 16 ) | 9,
            IncompatibleReportID = ( 0xC << 28 ) | ( 0x11 << 16 ) | 0xA,
            NotVablueArray = ( 0xC << 28 ) | ( 0x11 << 16 ) | 0xB,
            IsValueArray = ( 0xC << 28 ) | ( 0x11 << 16 ) | 0xC,
            DataIndexNotFound = ( 0xC << 28 ) | ( 0x11 << 16 ) | 0xD,
            DataIndexOutOfRange = ( 0xC << 28 ) | ( 0x11 << 16 ) | 0xE,
            ButtonNotPressed = ( 0xC << 28 ) | ( 0x11 << 16 ) | 0xF,
            ReportDoesNotExist = ( 0xC << 28 ) | ( 0x11 << 16 ) | 0x10,
            NotImplemented = ( 0xC << 28 ) | ( 0x11 << 16 ) | 0x20,
        }


        [StructLayout( LayoutKind.Sequential )]
        public struct Caps
        {
            public ushort Usage;
            public ushort UsagePage;
            public string UsageName => GetUsageName( this.UsagePage, this.Usage );
            public ushort InputReportByteLength;
            public ushort OutputReportByteLength;
            public ushort FeatureReportByteLength;
            [MarshalAs( UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U2, SizeConst = 17 )]
            public ushort[] Reserved;

            public ushort NumberLinkCollectionNodes;

            public ushort NumberInputButtonCaps;
            public ushort NumberInputValueCaps;
            public ushort NumberInputDataIndices;

            public ushort NumberOutputButtonCaps;
            public ushort NumberOutputValueCaps;
            public ushort NumberOutputDataIndices;

            public ushort NumberFeatureButtonCaps;
            public ushort NumberFeatureValueCaps;
            public ushort NumberFeatureDataIndices;
        }

        [StructLayout( LayoutKind.Explicit )]
        public struct Data
        {
            [FieldOffset( 0 )]
            public ushort DataIndex;
            [FieldOffset( 2 )]
            public ushort Reserved;
            [FieldOffset( 4 )]
            public uint RawValue; // for values
            [FieldOffset( 4 ), MarshalAs( UnmanagedType.U1 )]
            public bool On; // for buttons MUST BE TRUE for buttons.
        };


        [StructLayout( LayoutKind.Explicit )]
        public struct ButtonCaps
        {
            [FieldOffset( 0 )]
            public ushort UsagePage;
            public string UsagePageName => GetUsagePageName( this.UsagePage );
            [FieldOffset( 2 )]
            public byte ReportID;
            [FieldOffset( 3 ), MarshalAs( UnmanagedType.U1 )]
            public bool IsAlias;

            [FieldOffset( 4 )]
            public ushort BitField;
            [FieldOffset( 6 )]
            public ushort LinkCollection;

            [FieldOffset( 8 )]
            public ushort LinkUsage;
            [FieldOffset( 10 )]
            public ushort LinkUsagePage;
            public string LinkUsageName => GetUsageName( this.LinkUsagePage, this.LinkUsage );

            [FieldOffset( 12 ), MarshalAs( UnmanagedType.U1 )]
            public bool IsRange;
            [FieldOffset( 13 ), MarshalAs( UnmanagedType.U1 )]
            public bool IsStringRange;
            [FieldOffset( 14 ), MarshalAs( UnmanagedType.U1 )]
            public bool IsDesignatorRange;
            [FieldOffset( 15 ), MarshalAs( UnmanagedType.U1 )]
            public bool IsAbsolute;

            [FieldOffset( 16 ), MarshalAs( UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U4, SizeConst = 10 )]
            public uint[] Reserved;

            [FieldOffset( 56 )]
            public ButtonCapsRange Range;

            [FieldOffset( 56 )]
            public ButtonCapsNotRange NotRange;
            public string? NotRangeUsageName => this.IsRange ? null : GetUsageName( this.UsagePage, this.NotRange.Usage );

            public ushort UsageMin => this.IsRange ? this.Range.UsageMin : this.NotRange.Usage;
        }

        [StructLayout( LayoutKind.Sequential )]
        public struct ButtonCapsRange
        {
            public ushort UsageMin;
            public ushort UsageMax;
            public ushort StringMin;
            public ushort StringMax;
            public ushort DesignatorMin;
            public ushort DesignatorMax;
            public ushort DataIndexMin;
            public ushort DataIndexMax;
        }

        [StructLayout( LayoutKind.Sequential )]
        public struct ButtonCapsNotRange
        {
            public ushort Usage;
            public ushort Reserved1;
            public ushort StringIndex;
            public ushort Reserved2;
            public ushort DesignatorIndex;
            public ushort Reserved3;
            public ushort DataIndex;
            public ushort Reserved4;
        }


        [StructLayout( LayoutKind.Explicit )]
        public struct ValueCaps
        {
            [FieldOffset( 0 )]
            public ushort UsagePage;
            public string UsagePageName => GetUsagePageName( this.UsagePage );
            [FieldOffset( 2 )]
            public byte ReportID;
            [FieldOffset( 3 ), MarshalAs( UnmanagedType.U1 )]
            public bool IsAlias;

            [FieldOffset( 4 )]
            public ushort BitField;
            [FieldOffset( 6 )]
            public ushort LinkCollection;

            [FieldOffset( 8 )]
            public ushort LinkUsage;
            [FieldOffset( 10 )]
            public ushort LinkUsagePage;
            public string LinkUsageName => GetUsageName( this.LinkUsagePage, this.LinkUsage );

            [FieldOffset( 12 ), MarshalAs( UnmanagedType.U1 )]
            public bool IsRange;
            [FieldOffset( 13 ), MarshalAs( UnmanagedType.U1 )]
            public bool IsStringRange;
            [FieldOffset( 14 ), MarshalAs( UnmanagedType.U1 )]
            public bool IsDesignatorRange;
            [FieldOffset( 15 ), MarshalAs( UnmanagedType.U1 )]
            public bool IsAbsolute;

            [FieldOffset( 16 ), MarshalAs( UnmanagedType.U1 )]
            public bool HasNull;
            [FieldOffset( 17 )]
            public byte Reserved;
            [FieldOffset( 18 )]
            public ushort BitSize;

            [FieldOffset( 20 )]
            public ushort ReportCount;
            [FieldOffset( 22 )]
            public ushort Reserved2_1;
            [FieldOffset( 24 )]
            public ushort Reserved2_2;
            [FieldOffset( 26 )]
            public ushort Reserved2_3;
            [FieldOffset( 28 )]
            public ushort Reserved2_4;
            [FieldOffset( 30 )]
            public ushort Reserved2_5;

            [FieldOffset( 32 )]
            public uint UnitsExp;
            [FieldOffset( 36 )]
            public uint Units;

            [FieldOffset( 40 )]
            public int LogicalMin;
            [FieldOffset( 44 )]
            public int LogicalMax;
            [FieldOffset( 48 )]
            public int PhysicalMin;
            [FieldOffset( 52 )]
            public int PhysicalMax;

            [FieldOffset( 56 )]
            public ValueCapsRange Range;

            [FieldOffset( 56 )]
            public ValueCapsNotRange NotRange;
            public string? NotRangeUsageName => this.IsRange ? null : GetUsageName( this.UsagePage, this.NotRange.Usage );
        }

        [StructLayout( LayoutKind.Sequential )]
        public struct ValueCapsRange
        {
            public ushort UsageMin;
            public ushort UsageMax;
            public ushort StringMin;
            public ushort StringMax;
            public ushort DesignatorMin;
            public ushort DesignatorMax;
            public ushort DataIndexMin;
            public ushort DataIndexMax;
        }

        [StructLayout( LayoutKind.Sequential )]
        public struct ValueCapsNotRange
        {
            public ushort Usage;
            public ushort Reserved1;
            public ushort StringIndex;
            public ushort Reserved2;
            public ushort DesignatorIndex;
            public ushort Reserved3;
            public ushort DataIndex;
            public ushort Reserved4;
        }


        [StructLayout( LayoutKind.Sequential )]
        public struct LinkCollectionNode
        {
            public ushort LinkUsage;
            public ushort LinkUsagePage;
            public string LinkUsageName => GetUsageName( this.LinkUsagePage, this.LinkUsage );
            public ushort Parent;
            public ushort NumberOfChildren;
            public ushort NextSibling;
            public ushort FirstChild;
            public byte CollectionType;
            public byte IsAlias;
            public byte Reserved_1;
            public byte Reserved_2;
            public IntPtr UserContext;
        }

        [StructLayout( LayoutKind.Sequential )]
        public struct UsageAndPage
        {
            public ushort Usage;
            public ushort UsagePage;
        }


        /// <summary>
        ///     事前解析データによって指定されるHIDデバイスの能力のリストを返す。
        /// </summary>
        /// <param name="preparsedData">
        ///     HIDCLASSから返された事前解析データ。
        ///     </param>
        /// <param name="capabilities">
        ///     <see cref="Caps"/>構造体。
        ///     </param>
        /// <returns>
        ///     HIDP_STATUS_SUCCESS
        ///     HIDP_STATUS_INVALID_PREPARSED_DATA
        /// </returns>
        [DllImport( "hid.dll" )]
        public static extern Status HidP_GetCaps(
            IntPtr preparsedData,
            out Caps capabilities );

        /// <summary>
        ///     指定されたこのHIDデバイスのリンクコレクションツリーを記述する、<see cref="LinkCollectionNode"/> のリストを返す。
        /// </summary>
        /// <param name="linkCollectionNodes">
        ///     呼び出し元が割り当てた配列。このメソッドは、情報をそこに格納する。
        ///     </param>
        /// <param name="LinkCollectionNodesLength">
        ///     呼び出し元は、この値に、配列の要素数を設定すること。このメソッドは、この値を、実際の要素数に設定して返す。
        ///     このHIDデバイスを記述するために必要とされるノードの総数は、<see cref="Caps.NumberLinkCollectionNodes"/> で確認することができる。
        ///     </param>
        /// <param name="preparsedData">
        ///     HIDCLASSから返された事前解析データ。
        ///     </param>
        /// <returns></returns>
        [DllImport( "hid.dll" )]
        public static extern Status HidP_GetLinkCollectionNodes(
            [In, Out] LinkCollectionNode[] linkCollectionNodes,
            ref uint LinkCollectionNodesLength,
            IntPtr preparsedData );

        /// <summary>
        ///     指定された制限に該当するすべてのボタン（のバイナリ値）を返す。
        /// </summary>
        /// <param name="reportType">
        ///     <see cref="ReportType"/>のいずれか。
        ///     </param>
        /// <param name="usagePage">
        ///     返されるボタン能力は、このUsagePageに制限される。
        ///     0 を指定した場合、このパラメータは無視される。
        ///     </param>
        /// <param name="linkCollection">
        ///     返されるボタン能力は、このリンクコレクション配列のインデックスに制限される。
        ///     0 を指定した場合、このパラメータは無視される。
        ///     </param>
        /// <param name="usage">
        ///     返されるボタン能力は、このUsageに制限される。
        ///     0 を指定した場合、このパラメータは無視される。
        ///     </param>
        /// <param name="buttonCaps">
        ///     指定されたレポート内のすべてのバイナリ値に関する情報を含んだ<see cref="ButtonCaps"/>の配列。
        ///     この配列は、呼び出し元によって割り当てられる。
        ///     </param>
        /// <param name="buttonCapsLength">
        ///     入力としては、<paramref name="buttonCaps"/>（配列）の長さを要素単位で指定する。
        ///     出力としては、実際に埋められた要素数が返される。
        ///     最大数は、<see cref="Caps"/>で調べることができる。
        ///     <see cref="HID.Status.BufferTooSmall"/>が返された場合、このパラメータには必要とされる要素数が格納されている。
        ///     </param>
        /// <param name="preparseData">
        ///     HIDCLASSから返された事前解析データ。
        ///     </param>
        /// <returns>
        ///     HIDP_STATUS_SUCCESS
        ///     HIDP_STATUS_INVALID_REPORT_TYPE
        ///     HIDP_STATUS_INVALID_PREPARSED_DATA
        ///     HIDP_STATUS_BUFFER_TOO_SMALL( all given entries however have been filled in)
        ///     HIDP_STATUS_USAGE_NOT_FOUND
        /// </returns>
        [DllImport( "hid.dll" )]
        public static extern Status HidP_GetSpecificButtonCaps(
            ReportType reportType,
            ushort usagePage,
            ushort linkCollection,
            ushort usage,
            [In, Out] ButtonCaps[] buttonCaps,
            ref ushort buttonCapsLength,
            IntPtr preparseData );

        /// <summary>
        ///     すべてのボタン（のバイナリ値）を返す。
        /// </summary>
        /// <param name="reportType">
        ///     <see cref="ReportType"/>のいずれか。
        ///     </param>
        /// <param name="buttonCaps">
        ///     指定されたレポート内のすべてのバイナリ値に関する情報を含んだ<see cref="ButtonCaps"/>の配列。
        ///     この配列は、呼び出し元によって割り当てられる。
        ///     </param>
        /// <param name="buttonCapsLength">
        ///     入力としては、<paramref name="buttonCaps"/>（配列）の長さを要素単位で指定する。
        ///     出力としては、実際に埋められた要素数が返される。
        ///     最大数は、<see cref="Caps"/>で調べることができる。
        ///     <see cref="HID.Status.BufferTooSmall"/>が返された場合、このパラメータには必要とされる要素数が格納されている。
        ///     </param>
        /// <param name="preparseData">
        ///     HIDCLASSから返された事前解析データ。
        ///     </param>
        /// <returns>
        ///     HIDP_STATUS_SUCCESS
        ///     HIDP_STATUS_INVALID_REPORT_TYPE
        ///     HIDP_STATUS_INVALID_PREPARSED_DATA
        ///     HIDP_STATUS_BUFFER_TOO_SMALL( all given entries however have been filled in)
        ///     HIDP_STATUS_USAGE_NOT_FOUND
        /// </returns>
        [DllImport( "hid.dll" )]
        public static extern Status HidP_GetButtonCaps(
            ReportType reportType,
            [In, Out] ButtonCaps[] buttonCaps,
            ref ushort buttonCapsLength,
            IntPtr preparseData );

        /// <summary>
        ///     指定された制限に該当するすべての値（非バイナリ）を返す。
        /// </summary>
        /// <param name="reportType">
        ///     <see cref="ReportType"/>のいずれか。
        ///     </param>
        /// <param name="usagePage">
        ///     返される値能力は、このUsagePageに制限される。
        ///     0 を指定した場合、このパラメータは無視される。
        ///     </param>
        /// <param name="linkCollection">
        ///     返される値能力は、このリンクコレクション配列のインデックスに制限される。
        ///     0 を指定した場合、このパラメータは無視される。
        ///     </param>
        /// <param name="usage">
        ///     返される値能力は、このUsageに制限される。
        ///     0 を指定した場合、このパラメータは無視される。
        ///     </param>
        /// <param name="valueCaps">
        ///     指定されたレポート内のすべての非バイナリ値に関する情報を含んだ<see cref="ValueCaps"/>の配列。
        ///     この配列は、呼び出し元によって割り当てられる。
        ///     </param>
        /// <param name="valueCapsLength">
        ///     入力としては、<paramref name="valueCaps"/>（配列）の長さを要素単位で指定する。
        ///     出力としては、実際に埋められた要素数が返される。
        ///     最大数は、<see cref="Caps"/>で調べることができる。
        ///     <see cref="HID.Status.BufferTooSmall"/>が返された場合、このパラメータには必要とされる要素数が格納されている。
        ///     </param>
        /// <param name="preparsedData">
        ///     HIDCLASSから返された事前解析データ。
        ///     </param>
        /// <returns>
        ///     HIDP_STATUS_SUCCESS
        ///     HIDP_STATUS_INVALID_REPORT_TYPE
        ///     HIDP_STATUS_INVALID_PREPARSED_DATA
        ///     HIDP_STATUS_BUFFER_TOO_SMALL( all given entries however have been filled in)
        ///     HIDP_STATUS_USAGE_NOT_FOUND
        /// </returns>
        [DllImport( "hid.dll" )]
        public static extern Status HidP_GetSpecificValueCaps(
            ReportType reportType,
            ushort usagePage,
            ushort linkCollection,
            ushort usage,
            [In, Out] ValueCaps[] valueCaps,
            ref uint valueCapsLength,
            IntPtr preparsedData );

        /// <summary>
        ///     すべての値（非バイナリ）を返す。
        /// </summary>
        /// <param name="reportType">
        ///     <see cref="ReportType"/>のいずれか。
        ///     </param>
        /// <param name="valueCaps">
        ///     指定されたレポート内のすべての非バイナリ値に関する情報を含んだ<see cref="ValueCaps"/>の配列。
        ///     この配列は、呼び出し元によって割り当てられる。
        ///     </param>
        /// <param name="valueCapsLength">
        ///     入力としては、<paramref name="valueCaps"/>（配列）の長さを要素単位で指定する。
        ///     出力としては、実際に埋められた要素数が返される。
        ///     最大数は、<see cref="Caps"/>で調べることができる。
        ///     <see cref="HID.Status.BufferTooSmall"/>が返された場合、このパラメータには必要とされる要素数が格納されている。
        ///     </param>
        /// <param name="preparsedData">
        ///     HIDCLASSから返された事前解析データ。
        ///     </param>
        /// <returns>
        ///     HIDP_STATUS_SUCCESS
        ///     HIDP_STATUS_INVALID_REPORT_TYPE
        ///     HIDP_STATUS_INVALID_PREPARSED_DATA
        ///     HIDP_STATUS_BUFFER_TOO_SMALL( all given entries however have been filled in)
        ///     HIDP_STATUS_USAGE_NOT_FOUND
        /// </returns>
        [DllImport( "hid.dll" )]
        public static extern Status HidP_GetValueCaps(
            ReportType reportType,
            [In, Out] ValueCaps[] valueCaps,
            ref uint valueCapsLength,
            IntPtr preparsedData );

        /// <summary>
        ///     注意：明確な理由のために、このメソッドは UsageValueArrays にはアクセスしません。
        /// </summary>
        /// <param name="reportType">
        ///     <see cref="ReportType"/>のいずれか。
        ///     </param>
        /// <param name="dataList">
        ///     <see cref="Data"/>構造体の配列。指定されたレポートのデータ値がここに格納される。
        ///     </param>
        /// <param name="dataLength">
        ///     入力としては、<paramref name="dataList"/>の要素数を指定する。
        ///     出力としては、取得できた要素数が格納される。
        ///     要素数の最大値は、<see cref="HidP_MaxDataListLength"/>で取得することができる。
        ///     </param>
        /// <param name="preparsedData">
        ///     HIDCLASSから得られた事前解析データ。
        ///     </param>
        /// <param name="report">
        ///     データが格納されているバッファ。
        ///     </param>
        /// <param name="reportLength">
        ///     レポートの長さ。
        ///     </param>
        /// <returns>
        ///     HIDP_STATUS_SUCCESS
        ///     HIDP_STATUS_INVALID_REPORT_TYPE
        ///     HIDP_STATUS_INVALID_PREPARSED_DATA
        ///     HIDP_STATUS_INVALID_REPORT_LENGTH
        ///     HIDP_STATUS_REPORT_DOES_NOT_EXIST
        ///     HIDP_STATUS_BUFFER_TOO_SMALL
        /// </returns>
        [DllImport( "hid.dll" )]
        public static extern Status HidP_GetData(
            ReportType reportType,
            [In, Out] Data[] dataList,
            ref uint dataLength,
            IntPtr preparsedData,
            [MarshalAs( UnmanagedType.LPArray, SizeParamIndex = 5 )]
            [In, Out] byte[] report,
            uint reportLength );

        /// <summary>
        ///     <see cref="HidP_GetData(ReportType, Data[], ref uint, IntPtr, ref byte[], uint)"/> で返される<see cref="Data"/>要素の最大の長さを返す。
        /// </summary>
        /// <param name="reportType"></param>
        /// <param name="preparsedData"></param>
        /// <returns></returns>
        [DllImport( "hid.dll" )]
        public static extern uint HidP_MaxDataListLength(
            ReportType reportType,
            IntPtr preparsedData );

        /// <summary>
        ///     HIDレポート内に設定されているバイナリ値（ボタン）を返す。
        /// </summary>
        /// <param name="reportType">
        ///     <see cref="ReportType"/>のいずれか。
        ///     </param>
        /// <param name="usagePage">
        ///     返されるバイナリ値のUsagePage。
        ///     複数のUsagePageを指定したい場合には、この関数を複数回呼び出すこと。
        ///     </param>
        /// <param name="linkCollection">
        ///     <paramref name="usageList"/>に返される値は、このリンクコレクション配列インデックス内に存在する値に制限される。
        ///     0 を指定した場合、このパラメータは無視される。
        ///     </param>
        /// <param name="usageList">
        ///     レポート内に見つかったすべてのUsageを含む配列。
        ///     </param>
        /// <param name="usageLength">
        ///     入力としては、<paramref name="usageList"/>の要素数を指定する。
        ///     出力としては、見つかったUsageの数が格納される。
        ///     最大の要素数は、<see cref="HidP_MaxUsageListLength(ReportType, ushort, IntPtr)"/>で取得することができる。
        ///     </param>
        /// <param name="preparsedData">
        ///     HIDCLASSから得られた事前解析データ。
        ///     </param>
        /// <param name="report">
        ///     レポートパケット。
        ///     </param>
        /// <param name="reportLength">
        ///     レポートパケットの長さ（バイト単位）。
        ///     </param>
        /// <returns>
        ///     HIDP_STATUS_SUCCESS
        ///     HIDP_STATUS_INVALID_REPORT_TYPE
        ///     HIDP_STATUS_INVALID_PREPARSED_DATA
        ///     HIDP_STATUS_INVALID_REPORT_LENGTH
        ///     HIDP_STATUS_REPORT_DOES_NOT_EXIST
        ///     HIDP_STATUS_BUFFER_TOO_SMALL
        ///     HIDP_STATUS_INCOMPATIBLE_REPORT_ID
        ///     HIDP_STATUS_USAGE_NOT_FOUND
        /// </returns>
        [DllImport( "hid.dll" )]
        public static extern Status HidP_GetUsages(
            ReportType reportType,
            ushort usagePage,
            ushort linkCollection,
            [In, Out] ushort[] usageList,
            ref uint usageLength,
            IntPtr preparsedData,
            [MarshalAs( UnmanagedType.LPArray, SizeParamIndex = 7 )]
            [In, Out] byte[] report,
            uint reportLength );

        public static Status HidP_GetButtons(
            ReportType reportType,
            ushort usagePage,
            ushort linkCollection,
            ushort[] usageList,
            ref uint usageLength,
            IntPtr preparsedData,
            byte[] report,
            uint reportLength )
            => HidP_GetUsages( reportType, usagePage, linkCollection, usageList, ref usageLength, preparsedData, report, reportLength );

        /// <summary>
        ///     HIDレポート内に設定されているバイナリ値（ボタン）を返す。
        /// </summary>
        /// <param name="reportType"></param>
        /// <param name="linkCollection"></param>
        /// <param name="buttonList"></param>
        /// <param name="usageLength"></param>
        /// <param name="preparsedData"></param>
        /// <param name="report"></param>
        /// <param name="reportLength"></param>
        /// <returns></returns>
        [DllImport( "hid.dll" )]
        public static extern Status HidP_GetUsagesEx(
            ReportType reportType,
            ushort linkCollection,
            [In, Out] UsageAndPage[] buttonList,
            ref uint usageLength,
            IntPtr preparsedData,
            [MarshalAs( UnmanagedType.LPArray, SizeParamIndex = 6 )]
            [In] byte[] report,
            uint reportLength );

        public static Status HidP_GetButtonsEx(
            ReportType reportType,
            ushort linkCollection,
            [In, Out] UsageAndPage[] buttonList,
            ref uint usageLength,
            IntPtr preparsedData,
            byte[] report,
            uint reportLength )
            => HidP_GetUsagesEx( reportType, linkCollection, buttonList, ref usageLength, preparsedData, report, reportLength );

        /// <summary>
        ///     指定されたタイプのHIDレポートおよび指定された最上位コレクションに対して、HidP_GetUsages 関数が返すことができるHID Usageの最大数を返します。
        /// </summary>
        /// <param name="reportType">
        ///     </param>
        /// <param name="usagePage">
        ///     オプション。0 を指定すると、コレクション内のUsageの数が返される。
        ///     </param>
        /// <param name="preparsedData">
        ///     </param>
        /// <returns></returns>
        [DllImport( "hid.dll" )]
        public static extern uint HidP_MaxUsageListLength(
            ReportType reportType,
            ushort usagePage,
            IntPtr preparsedData );

        /// <summary>
        ///     HIDレポートの選択基準に一致するHID制御値を抽出します。
        /// </summary>
        /// <param name="reportType"></param>
        /// <param name="usagePage"></param>
        /// <param name="linkCollection"></param>
        /// <param name="usage"></param>
        /// <param name="usageValue"></param>
        /// <param name="preparsedData"></param>
        /// <param name="report"></param>
        /// <param name="reportLength"></param>
        /// <returns></returns>
        [DllImport( "hid.dll" )]
        public static extern Status HidP_GetUsageValue(
            ReportType reportType,
            ushort usagePage,
            ushort linkCollection,
            ushort usage,
            out uint usageValue,
            IntPtr preparsedData,
            [MarshalAs( UnmanagedType.LPArray, SizeParamIndex = 7 )]
            [In] byte[] report,
            uint reportLength );

        /// <summary>
        ///     HIDレポートから抽出された、スケーリング済みの符号付きHID制御値を返します。
        /// </summary>
        /// <param name="reportType">
        ///     HIDのUsage値を含むHIDレポートのタイプを示す<see cref="ReportType"/>値を指定します。
        ///     </param>
        /// <param name="usagePage">
        ///     抽出する値のUsagePageを指定します。
        ///     </param>
        /// <param name="linkCollection">
        ///     抽出する値のリンクコレクション識別子を指定します。 
        ///     0の場合は、最上位コレクションを示します。
        ///     </param>
        /// <param name="usage">
        ///     抽出する値のUsageを指定します。
        ///     </param>
        /// <param name="usageValue">
        ///     スケーリング済みの符号付き値が格納されます。
        ///     </param>
        /// <param name="preparsedData">
        ///     レポートを生成した最上位コレクションの事前解析済みデータを指定します。
        ///     </param>
        /// <param name="report">
        ///     Usageを含むレポートを指定します。
        ///     </param>
        /// <param name="reportLength">
        ///     <paramref name="report"/>の長さをバイト単位で指定します。
        ///     </param>
        /// <returns></returns>
        /// <remarks>
        ///     <paramref name="preparsedData"/>, <paramref name="usageValue"/>, および <paramref name="report/> のバッファは、非ページプールから割り当てる必要があります。 
        ///     ユーザーモードアプリケーションとカーネルモードドライバは、HidP_GetUsageValueArray を使用してUsage配列データを抽出する必要があります。 
        ///     戻り値が <see cref="HID.Status.BadLogPhyValues"/> であった場合、アプリケーションまたはドライバは HidP_GetUsageValue を呼び出して生のUsageデータを抽出できます。
        ///     詳しくは <see cref="https://technet.microsoft.com/ru-ru/ff539861(v=vs.110)">HIDコレクション</see>を参照してください。
        /// </remarks>
        [DllImport( "hid.dll" )]
        public static extern Status HidP_GetScaledUsageValue(
            ReportType reportType,
            ushort usagePage,
            ushort linkCollection,
            ushort usage,
            out int usageValue,
            IntPtr preparsedData,
            [MarshalAs( UnmanagedType.LPArray, SizeParamIndex = 7 )]
            [In] byte[] report,
            uint reportLength );

        [DllImport( "hid.dll" )]
        public static extern Status HidP_GetUsageValueArray(
            ReportType reportType,
            ushort usagePage,
            ushort linkCollection,
            ushort usage,
            [MarshalAs( UnmanagedType.LPArray, SizeParamIndex = 5 )]
            [In, Out] byte[] usageValue,
            ushort usageValueByteLength,
            IntPtr preparsedData,
            [MarshalAs( UnmanagedType.LPArray, SizeParamIndex = 8 )]
            [In] byte[] report,
            uint reportLength );

        /// <summary>
        ///     2つの HID Usage 配列を比較し、その差異を返す。
        /// </summary>
        /// <param name="previousUsageList">前回のUsage配列。</param>
        /// <param name="currentUsageList">今回のUsage配列。</param>
        /// <param name="breakUsageList">前回にはあるけど今回にはないUsageの配列が格納される。</param>
        /// <param name="makeUsageList">今回にはあるけど前回にはないUsageの配列が格納される。</param>
        /// <param name="UsageListLength">配列の要素数。前回と今回で要素数が異なるなら、大きいほうを指定すること。</param>
        /// <returns></returns>
        /// <remarks>
        ///     配列中に 0 の Usage があると、それ以降は解釈されない。この場合、未解釈の出力 Usage は 0 に設定される。
        /// </remarks>
        [DllImport( "hid.dll" )]
        public static extern Status HidP_UsageListDifference(
            [In] ushort[] previousUsageList,
            [In] ushort[] currentUsageList,
            [In, Out] ushort[] breakUsageList,
            [In, Out] ushort[] makeUsageList,
            uint UsageListLength );

        [DllImport( "hid.dll" )]
        public static extern Status HidP_UsageAndPageListDifference(
            [In] UsageAndPage[] previousUsageList,
            [In] UsageAndPage[] currentUsageList,
            [In, Out] UsageAndPage[] breakUsageList,
            [In, Out] UsageAndPage[] makeUsageList,
            uint usageListLength );


        public static string GetUsagePageName( ushort usagePage )
        {
            return ( _UsagePageName.ContainsKey( usagePage ) ) ?
                _UsagePageName[ usagePage ] :
                $"{usagePage:X4}";
        }

        public static string GetUsageName( uint extendedUsage )
            => GetUsageName( (ushort) ( ( extendedUsage >> 16 ) & 0xFFFF ), (ushort) ( extendedUsage & 0xFFFF ) );

        public static string GetUsageName( ushort usagePage, ushort usageId )
        {
            uint extendedUsage = (uint) ( usagePage << 16 ) | (ushort) usageId;

            return ( _UsageName.ContainsKey( extendedUsage ) ) ?
                _UsageName[ extendedUsage ] :
                $"{usagePage:X4} {usageId:X4}";
        }

        private static readonly Dictionary<ushort, string> _UsagePageName = new Dictionary<ushort, string>() {
            #region " *** "
            //----------------
            { 0x0001, "Generic Desktop Page" },
            { 0x0002, "Simulation Controls Page" },
            { 0x0003, "VR Controls Page" },
            { 0x0004, "Sport Control Page" },
            { 0x0005, "Game Control Page" },
            { 0x0006, "Generic Device Controls Page" },
            { 0x0007, "Keyboard/Keypad Page" },
            { 0x0008, "LED Page" },
            { 0x0009, "Button Page" },
            { 0x000A, "Ordinal Page" },
            { 0x000B, "Telephony Device Page" },
            { 0x000C, "Consumer Page" },
            { 0x000D, "Digitizers Page" },
            { 0x0014, "Alphanumeric Display Page" },
            { 0x0040, "Medical Instrument Page" },
            //----------------
            #endregion
        };

        /// <summary>
        ///     Extended Usage (上位2バイトがUsagePage, 下位2バイトがUsageID）と名前との対応表。
        /// </summary>
        private static readonly Dictionary<uint, string> _UsageName = new Dictionary<uint, string>() {
            #region " 0x01: Generic Desktop Page "
            //----------------
            { 0x0001_0000, "Undefined" },
            { 0x0001_0001, "Pointer" },
            { 0x0001_0002, "Mouse" },
            { 0x0001_0004, "Joystick" },
            { 0x0001_0005, "Game Pad" },
            { 0x0001_0006, "Keyboard" },
            { 0x0001_0007, "Keypad" },
            { 0x0001_0008, "Multi-axis Controller" },
            { 0x0001_0009, "Tablet PC System Controls" },
            { 0x0001_0030, "X" },
            { 0x0001_0031, "Y" },
            { 0x0001_0032, "Z" },
            { 0x0001_0033, "Rx" },
            { 0x0001_0034, "Ry" },
            { 0x0001_0035, "Rz" },
            { 0x0001_0036, "Slider" },
            { 0x0001_0037, "Dial" },
            { 0x0001_0038, "Wheel" },
            { 0x0001_0039, "Hat switch" },
            { 0x0001_003A, "Counted Buffer" },
            { 0x0001_003B, "Byte Count" },
            { 0x0001_003C, "Motion Wakeup" },
            { 0x0001_003D, "Start" },
            { 0x0001_003E, "Select" },
            { 0x0001_0040, "Vx" },
            { 0x0001_0041, "Vy" },
            { 0x0001_0042, "Vz" },
            { 0x0001_0043, "Vbrx" },
            { 0x0001_0044, "Vbry" },
            { 0x0001_0045, "Vbrz" },
            { 0x0001_0046, "Vno" },
            { 0x0001_0047, "Feature Notification" },
            { 0x0001_0048, "Resolution Multiplier" },
            { 0x0001_0080, "System Control" },
            { 0x0001_0081, "System Power Down" },
            { 0x0001_0082, "System Sleep" },
            { 0x0001_0083, "System Wake Up" },
            { 0x0001_0084, "System Context Menu" },
            { 0x0001_0085, "System Main Menu" },
            { 0x0001_0086, "System App Menu" },
            { 0x0001_0087, "System Menu Help" },
            { 0x0001_0088, "System Menu Exit" },
            { 0x0001_0089, "System Menu Select" },
            { 0x0001_008A, "System Menu Right" },
            { 0x0001_008B, "System Menu Left" },
            { 0x0001_008C, "System Menu Up" },
            { 0x0001_008D, "System Menu Down" },
            { 0x0001_008E, "System Cold Restart" },
            { 0x0001_008F, "System Warm Restart" },
            { 0x0001_0090, "D-pad Up" },
            { 0x0001_0091, "D-pad Down" },
            { 0x0001_0092, "D-pad Right" },
            { 0x0001_0093, "D-pad Left" },
            { 0x0001_00A0, "System Dock" },
            { 0x0001_00A1, "System Undock" },
            { 0x0001_00A2, "System Setup" },
            { 0x0001_00A3, "System Break" },
            { 0x0001_00A4, "System Debugger Break" },
            { 0x0001_00A5, "Application Break" },
            { 0x0001_00A6, "Application Debugger Break" },
            { 0x0001_00A7, "System Speaker Mute" },
            { 0x0001_00A8, "System Hibernate" },
            { 0x0001_00B0, "System Display Invert" },
            { 0x0001_00B1, "System Display Internal" },
            { 0x0001_00B2, "System Display External" },
            { 0x0001_00B3, "System Display Both" },
            { 0x0001_00B4, "System Display Dual" },
            { 0x0001_00B5, "System Display Toggle Int/Ext" },
            { 0x0001_00B6, "System Display Swap Primary/Secondary" },
            { 0x0001_00B7, "System Display LCD Autoscale" },
            //----------------
            #endregion
            #region " 0x02: Simulation Controls Page "
            //----------------
            { 0x0002_0000, "Undefined" },
            { 0x0002_0001, "Flight Simulation Device" },
            { 0x0002_0002, "Automobile Simulation Device" },
            { 0x0002_0003, "Tank Simulation Device" },
            { 0x0002_0004, "Spaceship Simulation Device" },
            { 0x0002_0005, "Submarine Simulation Device" },
            { 0x0002_0006, "Salling Simulation Device" },
            { 0x0002_0007, "Motorcycle Simulation Device" },
            { 0x0002_0008, "Sports Simulation Device" },
            { 0x0002_0009, "Airplane Simulation Device" },
            { 0x0002_000A, "Helicopter Simulation Device" },
            { 0x0002_000B, "Magic Carpet Simulation Device" },
            { 0x0002_000C, "Bicycle Simulation Device" },
            { 0x0002_0020, "Flight Control Stick" },
            { 0x0002_0021, "Flight Stick" },
            { 0x0002_0022, "Cyclic Control" },
            { 0x0002_0023, "Cyclic Trim" },
            { 0x0002_0024, "Flight Yoke" },
            { 0x0002_0025, "Track Control" },
            { 0x0002_00B0, "Aileron" },
            { 0x0002_00B1, "Aileron Trim" },
            { 0x0002_00B2, "Anti-Torque Control" },
            { 0x0002_00B3, "Autopilot Enable" },
            { 0x0002_00B4, "Chaff Release" },
            { 0x0002_00B5, "Collective Control" },
            { 0x0002_00B6, "Dive Brake" },
            { 0x0002_00B7, "Electronic Countermeasures" },
            { 0x0002_00B8, "Elevator" },
            { 0x0002_00B9, "Elevator Trim" },
            { 0x0002_00BA, "Rudder" },
            { 0x0002_00BB, "Throttle" },
            { 0x0002_00BC, "Flight Communications" },
            { 0x0002_00BD, "Flare Release" },
            { 0x0002_00BE, "Landing Gear" },
            { 0x0002_00BF, "Toe Break" },
            { 0x0002_00C0, "Trigger" },
            { 0x0002_00C1, "Weapons Arm" },
            { 0x0002_00C2, "Weapons Select" },
            { 0x0002_00C3, "Wing Flaps" },
            { 0x0002_00C4, "Accelerator" },
            { 0x0002_00C5, "Brake" },
            { 0x0002_00C6, "Clutch" },
            { 0x0002_00C7, "Shifter" },
            { 0x0002_00C8, "Steering" },
            { 0x0002_00C9, "Turret Direction" },
            { 0x0002_00CA, "Barrel Elevation" },
            { 0x0002_00CB, "Dive Plane" },
            { 0x0002_00CC, "Ballast" },
            { 0x0002_00CD, "Bicycle Crank" },
            { 0x0002_00CE, "Handle Bars" },
            { 0x0002_00CF, "Front Brake" },
            { 0x0002_00D0, "Rear Brake" },
            //----------------
            #endregion
            #region " 0x03: VR Controls Page "
            //----------------
            { 0x0003_0000, "Undefined" },
            { 0x0003_0001, "Belt" },
            { 0x0003_0002, "Body Suit" },
            { 0x0003_0003, "Flexor" },
            { 0x0003_0004, "Glove" },
            { 0x0003_0005, "Head Tracker" },
            { 0x0003_0006, "Head Mounted Display" },
            { 0x0003_0007, "Hand Tracker" },
            { 0x0003_0008, "Occulometer" },
            { 0x0003_0009, "Vest" },
            { 0x0003_000A, "Animatronic Device" },
            { 0x0003_0020, "Stereo Enable" },
            { 0x0003_0021, "Display Enable" },
            //----------------
            #endregion
            #region " 0x04: Sport Controls Page "
            //----------------
            { 0x0004_0000, "Undefined" },
            { 0x0004_0001, "Baseball Bat" },
            { 0x0004_0002, "Golf Club" },
            { 0x0004_0003, "Rowing Machine" },
            { 0x0004_0004, "Treadmill" },
            { 0x0004_0030, "Oar" },
            { 0x0004_0031, "Slope" },
            { 0x0004_0032, "Rate" },
            { 0x0004_0033, "Stick Speed" },
            { 0x0004_0034, "Stick Face Angle" },
            { 0x0004_0035, "Stick Heel/Toe" },
            { 0x0004_0036, "Stick Follow Through" },
            { 0x0004_0037, "Stick Tempo" },
            { 0x0004_0038, "Stick Type" },
            { 0x0004_0039, "Stick Height" },
            { 0x0004_0050, "Putter" },
            { 0x0004_0051, "1 Iron" },
            { 0x0004_0052, "2 Iron" },
            { 0x0004_0053, "3 Iron" },
            { 0x0004_0054, "4 Iron" },
            { 0x0004_0055, "5 Iron" },
            { 0x0004_0056, "6 Iron" },
            { 0x0004_0057, "7 Iron" },
            { 0x0004_0058, "8 Iron" },
            { 0x0004_0059, "9 Iron" },
            { 0x0004_005A, "10 Iron" },
            { 0x0004_005B, "11 Iron" },
            { 0x0004_005C, "Sand Wedge" },
            { 0x0004_005D, "Loft Wedge" },
            { 0x0004_005E, "Power Wedge" },
            { 0x0004_005F, "1 Wood" },
            { 0x0004_0060, "3 Wood" },
            { 0x0004_0061, "5 Wood" },
            { 0x0004_0062, "7 Wood" },
            { 0x0004_0063, "9 Wood" },
            //----------------
            #endregion
            #region " 0x05: Game Control Page "
            //----------------
            { 0x0005_0000, "Undefined" },
            { 0x0005_0001, "3D Game Controller" },
            { 0x0005_0002, "Pinball Device" },
            { 0x0005_0003, "Gun Device" },
            { 0x0005_0020, "Point of View" },
            { 0x0005_0021, "Turn Right/Left" },
            { 0x0005_0022, "Pitch Forward/Backward" },
            { 0x0005_0023, "Roll Right/Left" },
            { 0x0005_0024, "Move Right/Left" },
            { 0x0005_0025, "Move Forward/Backward" },
            { 0x0005_0026, "Move Up/Down" },
            { 0x0005_0027, "Lean Right/Left" },
            { 0x0005_0028, "Lean Forward/Backward" },
            { 0x0005_0029, "Height of POV" },
            { 0x0005_002A, "Flipper" },
            { 0x0005_002B, "Secondary Flipper" },
            { 0x0005_002C, "Bump" },
            { 0x0005_002D, "New Game" },
            { 0x0005_002E, "Shoot Ball" },
            { 0x0005_002F, "Player" },
            { 0x0005_0030, "Gun Bolt" },
            { 0x0005_0031, "Gun Clip" },
            { 0x0005_0032, "Gun Selector" },
            { 0x0005_0033, "Gun Single Shot" },
            { 0x0005_0034, "Gun Burst" },
            { 0x0005_0035, "Gun Automatic" },
            { 0x0005_0036, "Gun Safety" },
            { 0x0005_0037, "Gamepad Fire/Jump" },
            { 0x0005_0039, "Gamepad Trigger" },
            //----------------
            #endregion
            #region " 0x06: Generic Device Controls Page "
            //----------------
            { 0x0006_0000, "Undefined" },
            { 0x0006_0020, "Battery Strength" },
            { 0x0006_0021, "Wireless Channel" },
            { 0x0006_0022, "Wireless ID" },
            { 0x0006_0023, "Discover Wireless Control" },
            { 0x0006_0024, "Security Code Character Entered" },
            { 0x0006_0025, "Security Code Character Erased" },
            { 0x0006_0026, "Security Code Cleared" },
            //----------------
            #endregion
            #region " 0x07: Keyboard/Keypad Page "
            //----------------
            { 0x0007_0000, "Reserved (no event indicated)" },
            { 0x0007_0001, "Keyboard ErrorRollOver" },
            { 0x0007_0002, "Keyboard POSTFail" },
            { 0x0007_0003, "Keyboard ErrorUndefined" },
            { 0x0007_0004, "Keyboard a and A" },
            { 0x0007_0005, "Keyboard b and B" },
            { 0x0007_0006, "Keyboard c and C" },
            { 0x0007_0007, "Keyboard d and D" },
            { 0x0007_0008, "Keyboard e and E" },
            { 0x0007_0009, "Keyboard f and F" },
            { 0x0007_000A, "Keyboard g and G" },
            { 0x0007_000B, "Keyboard h and H" },
            { 0x0007_000C, "Keyboard i and I" },
            { 0x0007_000D, "Keyboard j and J" },
            { 0x0007_000E, "Keyboard k and K" },
            { 0x0007_000F, "Keyboard l and L" },
            { 0x0007_0010, "Keyboard m and M" },
            { 0x0007_0011, "Keyboard n and N" },
            { 0x0007_0012, "Keyboard o and O" },
            { 0x0007_0013, "Keyboard p and P" },
            { 0x0007_0014, "Keyboard q and Q" },
            { 0x0007_0015, "Keyboard r and R" },
            { 0x0007_0016, "Keyboard s and S" },
            { 0x0007_0017, "Keyboard t and T" },
            { 0x0007_0018, "Keyboard u and U" },
            { 0x0007_0019, "Keyboard v and V" },
            { 0x0007_001A, "Keyboard w and W" },
            { 0x0007_001B, "Keyboard x and X" },
            { 0x0007_001C, "Keyboard y and Y" },
            { 0x0007_001D, "Keyboard z and Z" },
            { 0x0007_001E, "Keyboard 1 and !" },
            { 0x0007_001F, "Keyboard 2 and @" },
            { 0x0007_0020, "Keyboard 3 and #" },
            { 0x0007_0021, "Keyboard 4 and $" },
            { 0x0007_0022, "Keyboard 5 and %" },
            { 0x0007_0023, "Keyboard 6 and ^" },
            { 0x0007_0024, "Keyboard 7 and &" },
            { 0x0007_0025, "Keyboard 8 and *" },
            { 0x0007_0026, "Keyboard 9 and (" },
            { 0x0007_0027, "Keyboard 0 and )" },
            { 0x0007_0028, "Keyboard Return (ENTER)" },
            { 0x0007_0029, "Keyboard ESCAPE" },
            { 0x0007_002A, "Keyboard DELETE (Backspace)" },
            { 0x0007_002B, "Keyboard Tab" },
            { 0x0007_002C, "Keyboard Spacebar" },
            { 0x0007_002D, "Keyboard - and (underscore)" },
            { 0x0007_002E, "Keyboard = and +" },
            { 0x0007_002F, "Keyboard [ and {" },
            { 0x0007_0030, "Keyboard ] and }" },
            { 0x0007_0031, "Keyboard \\ and |" },
            { 0x0007_0032, "Keyboard Non-US # and ~" },
            { 0x0007_0033, "Keyboard ; and :" },
            { 0x0007_0034, "Keyboard ` and \"" },
            { 0x0007_0035, "Keyboard Grave Accent and Tilde" },
            { 0x0007_0036, "Keyboard , and <" },
            { 0x0007_0037, "Keyboard . and >" },
            { 0x0007_0038, "Keyboard / and ?" },
            { 0x0007_0039, "Keyboard Caps Lock" },
            { 0x0007_003A, "Keyboard F1" },
            { 0x0007_003B, "Keyboard F2" },
            { 0x0007_003C, "Keyboard F3" },
            { 0x0007_003D, "Keyboard F4" },
            { 0x0007_003E, "Keyboard F5" },
            { 0x0007_003F, "Keyboard F6" },
            { 0x0007_0040, "Keyboard F7" },
            { 0x0007_0041, "Keyboard F8" },
            { 0x0007_0042, "Keyboard F9" },
            { 0x0007_0043, "Keyboard F10" },
            { 0x0007_0044, "Keyboard F11" },
            { 0x0007_0045, "Keyboard F12" },
            { 0x0007_0046, "Keyboard PrintScreen" },
            { 0x0007_0047, "Keyboard Scroll Lock" },
            { 0x0007_0048, "Keyboard Pause" },
            { 0x0007_0049, "Keyboard Insert" },
            { 0x0007_004A, "Keyboard Home" },
            { 0x0007_004B, "Keyboard PageUp" },
            { 0x0007_004C, "Keyboard Delete Forward" },
            { 0x0007_004D, "Keyboard End" },
            { 0x0007_004E, "Keyboard PageDown" },
            { 0x0007_004F, "Keyboard RightArrow" },
            { 0x0007_0050, "Keyboard LeftArrow" },
            { 0x0007_0051, "Keyboard DownArrow" },
            { 0x0007_0052, "Keyboard UpArrow" },
            { 0x0007_0053, "Keypad Num Lock and Clear" },
            { 0x0007_0054, "Keypad /" },
            { 0x0007_0055, "Keypad *" },
            { 0x0007_0056, "Keypad -" },
            { 0x0007_0057, "Keypad +" },
            { 0x0007_0058, "Keypad ENTER" },
            { 0x0007_0059, "Keypad 1 and End" },
            { 0x0007_005A, "Keypad 2 and Down Arrow" },
            { 0x0007_005B, "Keypad 3 and PageDn" },
            { 0x0007_005C, "Keypad 4 and Left Arrow" },
            { 0x0007_005D, "Keypad 5" },
            { 0x0007_005E, "Keypad 6 and Right Arrow" },
            { 0x0007_005F, "Keypad 7 and Home" },
            { 0x0007_0060, "Keypad 8 and Up Arrow" },
            { 0x0007_0061, "Keypad 9 and PageUp" },
            { 0x0007_0062, "Keypad 0 and Insert" },
            { 0x0007_0063, "Keypad . and Delete" },
            { 0x0007_0064, "Keyboard Non-US \\ and |" },
            { 0x0007_0065, "Keyboard Application" },
            { 0x0007_0066, "Keyboard Power" },
            { 0x0007_0067, "Keypad =" },
            { 0x0007_0068, "Keyboard F13" },
            { 0x0007_0069, "Keyboard F14" },
            { 0x0007_006A, "Keyboard F15" },
            { 0x0007_006B, "Keyboard F16" },
            { 0x0007_006C, "Keyboard F17" },
            { 0x0007_006D, "Keyboard F18" },
            { 0x0007_006E, "Keyboard F19" },
            { 0x0007_006F, "Keyboard F20" },
            { 0x0007_0070, "Keyboard F21" },
            { 0x0007_0071, "Keyboard F22" },
            { 0x0007_0072, "Keyboard F23" },
            { 0x0007_0073, "Keyboard F24" },
            { 0x0007_0074, "Keyboard Execute" },
            { 0x0007_0075, "Keyboard Help" },
            { 0x0007_0076, "Keyboard Menu" },
            { 0x0007_0077, "Keyboard Select" },
            { 0x0007_0078, "Keyboard Stop" },
            { 0x0007_0079, "Keyboard Again" },
            { 0x0007_007A, "Keyboard Undo" },
            { 0x0007_007B, "Keyboard Cut" },
            { 0x0007_007C, "Keyboard Copy" },
            { 0x0007_007D, "Keyboard Paste" },
            { 0x0007_007E, "Keyboard Find" },
            { 0x0007_007F, "Keyboard Mute" },
            { 0x0007_0080, "Keyboard Volume Up" },
            { 0x0007_0081, "Keyboard Volume Down" },
            { 0x0007_0082, "Keyboard Locking Caps Lock" },
            { 0x0007_0083, "Keyboard Locking Num Lock" },
            { 0x0007_0084, "Keyboard Locking Scroll Lock" },
            { 0x0007_0085, "Keyboard Comma" },
            { 0x0007_0086, "Keyboard Equal Sign" },
            { 0x0007_0087, "Keyboard International1" },
            { 0x0007_0088, "Keyboard International2" },
            { 0x0007_0089, "Keyboard International3" },
            { 0x0007_008A, "Keyboard International4" },
            { 0x0007_008B, "Keyboard International5" },
            { 0x0007_008C, "Keyboard International6" },
            { 0x0007_008D, "Keyboard International7" },
            { 0x0007_008E, "Keyboard International8" },
            { 0x0007_008F, "Keyboard International9" },
            { 0x0007_0090, "Keyboard LANG1" },
            { 0x0007_0091, "Keyboard LANG2" },
            { 0x0007_0092, "Keyboard LANG3" },
            { 0x0007_0093, "Keyboard LANG4" },
            { 0x0007_0094, "Keyboard LANG5" },
            { 0x0007_0095, "Keyboard LANG6" },
            { 0x0007_0096, "Keyboard LANG7" },
            { 0x0007_0097, "Keyboard LANG8" },
            { 0x0007_0098, "Keyboard LANG9" },
            { 0x0007_0099, "Keyboard Alternate Erase" },
            { 0x0007_009A, "Keyboard SysReq/Attention" },
            { 0x0007_009B, "Keyboard Cancel" },
            { 0x0007_009C, "Keyboard Clear" },
            { 0x0007_009D, "Keyboard Prior" },
            { 0x0007_009E, "Keyboard Return" },
            { 0x0007_009F, "Keyboard Separator" },
            { 0x0007_00A0, "Keyboard Out" },
            { 0x0007_00A1, "Keyboard Oper" },
            { 0x0007_00A2, "Keyboard Clear/Again" },
            { 0x0007_00A3, "Keyboard CrSel/Props" },
            { 0x0007_00A4, "Keyboard ExSel" },
            { 0x0007_00B0, "Keypad 00" },
            { 0x0007_00B1, "Keypad 000" },
            { 0x0007_00B2, "Thousands Separator" },
            { 0x0007_00B3, "Decimal Separator" },
            { 0x0007_00B4, "Currency Unit" },
            { 0x0007_00B5, "Currency Sub-unit" },
            { 0x0007_00B6, "Keypad (" },
            { 0x0007_00B7, "Keypad )" },
            { 0x0007_00B8, "Keypad {" },
            { 0x0007_00B9, "Keypad }" },
            { 0x0007_00BA, "Keypad Tab" },
            { 0x0007_00BB, "Keypad Backspace" },
            { 0x0007_00BC, "Keypad A" },
            { 0x0007_00BD, "Keypad B" },
            { 0x0007_00BE, "Keypad C" },
            { 0x0007_00BF, "Keypad D" },
            { 0x0007_00C0, "Keypad E" },
            { 0x0007_00C1, "Keypad F" },
            { 0x0007_00C2, "Keypad XOR" },
            { 0x0007_00C3, "Keypad ^" },
            { 0x0007_00C4, "Keypad %" },
            { 0x0007_00C5, "Keypad <" },
            { 0x0007_00C6, "Keypad >" },
            { 0x0007_00C7, "Keypad &" },
            { 0x0007_00C8, "Keypad &&" },
            { 0x0007_00C9, "Keypad |" },
            { 0x0007_00CA, "Keypad ||" },
            { 0x0007_00CB, "Keypad :" },
            { 0x0007_00CC, "Keypad #" },
            { 0x0007_00CD, "Keypad Space" },
            { 0x0007_00CE, "Keypad @" },
            { 0x0007_00CF, "Keypad !" },
            { 0x0007_00D0, "Keypad Memory Store" },
            { 0x0007_00D1, "Keypad Memory Recall" },
            { 0x0007_00D2, "Keypad Memory Clear" },
            { 0x0007_00D3, "Keypad Memory Add" },
            { 0x0007_00D4, "Keypad Memory Subtract" },
            { 0x0007_00D5, "Keypad Memory Multiply" },
            { 0x0007_00D6, "Keypad Memory Divide" },
            { 0x0007_00D7, "Keypad +/-" },
            { 0x0007_00D8, "Keypad Clear" },
            { 0x0007_00D9, "Keypad Clear Entry" },
            { 0x0007_00DA, "Keypad Binary" },
            { 0x0007_00DB, "Keypad Octal" },
            { 0x0007_00DC, "Keypad Decimal" },
            { 0x0007_00DD, "Keypad Hexadecimal" },
            { 0x0007_00E0, "Keyboard LeftControl" },
            { 0x0007_00E1, "Keyboard LeftShift" },
            { 0x0007_00E2, "Keyboard LeftAlt" },
            { 0x0007_00E3, "Keyboard Left GUI" },
            { 0x0007_00E4, "Keyboard RightControl" },
            { 0x0007_00E5, "Keyboard RightShift" },
            { 0x0007_00E6, "Keyboard RightAlt" },
            { 0x0007_00E7, "Keyboard Right GUI" },
            //----------------
            #endregion
            #region " 0x08: LED Page "
            //----------------
            { 0x0008_0000, "Undefined" },
            { 0x0008_0001, "Num Lock" },
            { 0x0008_0002, "Caps Lock" },
            { 0x0008_0003, "Scroll Lock" },
            { 0x0008_0004, "Compose" },
            { 0x0008_0005, "Kana" },
            { 0x0008_0006, "Power" },
            { 0x0008_0007, "Shift" },
            { 0x0008_0008, "Do Not Disturb" },
            { 0x0008_0009, "Mute" },
            { 0x0008_000A, "Tone Enable" },
            { 0x0008_000B, "High Cut Filter" },
            { 0x0008_000C, "Low Cut Filter" },
            { 0x0008_000D, "Equalizer Enable" },
            { 0x0008_000E, "Sound Field On" },
            { 0x0008_000F, "Surround On" },
            { 0x0008_0010, "Repear" },
            { 0x0008_0011, "Stereo" },
            { 0x0008_0012, "Sampling Rate Detect" },
            { 0x0008_0013, "Spinning" },
            { 0x0008_0014, "CAV" },
            { 0x0008_0015, "CLV" },
            { 0x0008_0016, "Recording Format Detect" },
            { 0x0008_0017, "Off-Hook" },
            { 0x0008_0018, "Ring" },
            { 0x0008_0019, "Message Waiting" },
            { 0x0008_001A, "Data Mode" },
            { 0x0008_001B, "Battery Operation" },
            { 0x0008_001C, "Battery OK" },
            { 0x0008_001D, "Battery Low" },
            { 0x0008_001E, "Speaker" },
            { 0x0008_001F, "Head Set" },
            { 0x0008_0020, "Hold" },
            { 0x0008_0021, "Microphone" },
            { 0x0008_0022, "Coverage" },
            { 0x0008_0023, "Night Mode" },
            { 0x0008_0024, "Send Calls" },
            { 0x0008_0025, "Call Pickup" },
            { 0x0008_0026, "Conference" },
            { 0x0008_0027, "Stand-by" },
            { 0x0008_0028, "Camera On" },
            { 0x0008_0029, "Camera Off" },
            { 0x0008_002A, "On-Line" },
            { 0x0008_002B, "Off-Line" },
            { 0x0008_002C, "Busy" },
            { 0x0008_002D, "Ready" },
            { 0x0008_002E, "Paper-Out" },
            { 0x0008_002F, "Paper-Jam" },
            { 0x0008_0030, "Remote" },
            { 0x0008_0031, "Forward" },
            { 0x0008_0032, "Reverse" },
            { 0x0008_0033, "Stop" },
            { 0x0008_0034, "Rewind" },
            { 0x0008_0035, "Fast Forward" },
            { 0x0008_0036, "Play" },
            { 0x0008_0037, "Pause" },
            { 0x0008_0038, "Record" },
            { 0x0008_0039, "Error" },
            { 0x0008_003A, "Usage Selected Indicator" },
            { 0x0008_003B, "Usage In Use Indicator" },
            { 0x0008_003C, "Usage Multi Mode Indicator" },
            { 0x0008_003D, "Indicator On" },
            { 0x0008_003E, "Indicator Flash" },
            { 0x0008_003F, "Indicator Slow Blink" },
            { 0x0008_0040, "Indicator Fast Blink" },
            { 0x0008_0041, "Indicator Off" },
            { 0x0008_0042, "Flash On Time" },
            { 0x0008_0043, "Slow Blink On Time" },
            { 0x0008_0044, "Slow Blink Off Time" },
            { 0x0008_0045, "Fast Blink On Time" },
            { 0x0008_0046, "Fast Blink Off Time" },
            { 0x0008_0047, "Usage Indicator Color" },
            { 0x0008_0048, "Indicator Red" },
            { 0x0008_0049, "Indicator Green" },
            { 0x0008_004A, "Indicator Amber" },
            { 0x0008_004B, "Generic Indicator" },
            { 0x0008_004C, "System Suspend" },
            { 0x0008_004D, "External Power Connected" },
            //----------------
            #endregion
            #region " 0x09: Button Page "
            //----------------
            { 0x0009_0000, "No Button pressed" },
            { 0x0009_0001, "Button 1 (primary/trigger)" },
            { 0x0009_0002, "Button 2 (secondary)" },
            { 0x0009_0003, "Button 3 (tertiary)" },
            { 0x0009_0004, "Button 4" },
            { 0x0009_0005, "Button 5" },
            { 0x0009_0006, "Button 6" },
            { 0x0009_0007, "Button 7" },
            { 0x0009_0008, "Button 8" },
            { 0x0009_0009, "Button 9" },
            { 0x0009_000A, "Button 10" },
            { 0x0009_000B, "Button 11" },
            { 0x0009_000C, "Button 12" },
            { 0x0009_000D, "Button 13" },
            { 0x0009_000E, "Button 14" },
            { 0x0009_000F, "Button 15" },
            { 0x0009_0010, "Button 16" },
            { 0x0009_0011, "Button 17" },
            { 0x0009_0012, "Button 18" },
            { 0x0009_0013, "Button 19" },
            { 0x0009_0014, "Button 20" },
            { 0x0009_0015, "Button 21" },
            { 0x0009_0016, "Button 22" },
            { 0x0009_0017, "Button 23" },
            { 0x0009_0018, "Button 24" },
            { 0x0009_0019, "Button 25" },
            { 0x0009_001A, "Button 26" },
            { 0x0009_001B, "Button 27" },
            { 0x0009_001C, "Button 28" },
            { 0x0009_001D, "Button 29" },
            { 0x0009_001E, "Button 30" },
            { 0x0009_001F, "Button 31" },
            { 0x0009_0020, "Button 32" },
            //----------------
            #endregion
            #region " 0x0A: Ordinal Page "
            //----------------
            { 0x000A_0000, "Reserved" },
            { 0x000A_0001, "Instance 1" },
            { 0x000A_0002, "Instance 2" },
            { 0x000A_0003, "Instance 3" },
            { 0x000A_0004, "Instance 4" },
            { 0x000A_0005, "Instance 5" },
            { 0x000A_0006, "Instance 6" },
            { 0x000A_0007, "Instance 7" },
            { 0x000A_0008, "Instance 8" },
            { 0x000A_0009, "Instance 9" },
            { 0x000A_000A, "Instance 10" },
            { 0x000A_000B, "Instance 11" },
            { 0x000A_000C, "Instance 12" },
            { 0x000A_000D, "Instance 13" },
            { 0x000A_000E, "Instance 14" },
            { 0x000A_000F, "Instance 15" },
            { 0x000A_0010, "Instance 16" },
            { 0x000A_0011, "Instance 17" },
            { 0x000A_0012, "Instance 18" },
            { 0x000A_0013, "Instance 19" },
            { 0x000A_0014, "Instance 20" },
            { 0x000A_0015, "Instance 21" },
            { 0x000A_0016, "Instance 22" },
            { 0x000A_0017, "Instance 23" },
            { 0x000A_0018, "Instance 24" },
            { 0x000A_0019, "Instance 25" },
            { 0x000A_001A, "Instance 26" },
            { 0x000A_001B, "Instance 27" },
            { 0x000A_001C, "Instance 28" },
            { 0x000A_001D, "Instance 29" },
            { 0x000A_001E, "Instance 30" },
            { 0x000A_001F, "Instance 31" },
            { 0x000A_0020, "Instance 32" },
            //----------------
            #endregion
            #region " 0x0B: Telephony Device Page "
            //----------------
            { 0x000B_0000, "Unassigned" },
            { 0x000B_0001, "Phone" },
            { 0x000B_0002, "Answering Machine" },
            { 0x000B_0003, "Message Controls" },
            { 0x000B_0004, "Handset" },
            { 0x000B_0005, "Headset" },
            { 0x000B_0006, "Telephony Key Pad" },
            { 0x000B_0007, "Programmable Button" },
            { 0x000B_0020, "Hook Switch" },
            { 0x000B_0021, "Flash" },
            { 0x000B_0022, "Feature" },
            { 0x000B_0023, "Hold" },
            { 0x000B_0024, "Redial" },
            { 0x000B_0025, "Transfer" },
            { 0x000B_0026, "Drop" },
            { 0x000B_0027, "Park" },
            { 0x000B_0028, "Forward Calls" },
            { 0x000B_0029, "Alternate Function" },
            { 0x000B_002A, "Line" },
            { 0x000B_002B, "Speaker Phone" },
            { 0x000B_002C, "Conference" },
            { 0x000B_002D, "Ring Enable" },
            { 0x000B_002E, "Ring Select" },
            { 0x000B_002F, "Phone Mute" },
            { 0x000B_0030, "Caller ID" },
            { 0x000B_0031, "Send" },
            { 0x000B_0050, "Speed Dial" },
            { 0x000B_0051, "Store Number" },
            { 0x000B_0052, "Recall Number" },
            { 0x000B_0053, "Phone Directory" },
            { 0x000B_0070, "Voice Mail" },
            { 0x000B_0071, "Screen Calls" },
            { 0x000B_0072, "Do Not Disturb" },
            { 0x000B_0073, "Message" },
            { 0x000B_0074, "Answer On/Off" },
            { 0x000B_0090, "Inside Dial Tone" },
            { 0x000B_0091, "Outside Dial Tone" },
            { 0x000B_0092, "Inside Ring Tone" },
            { 0x000B_0093, "Outside Ring Tone" },
            { 0x000B_0094, "Priority Ring Tone" },
            { 0x000B_0095, "Inside Ringback" },
            { 0x000B_0096, "Priority Ringback" },
            { 0x000B_0097, "Line Busy Tone" },
            { 0x000B_0098, "Recorder Tone" },
            { 0x000B_0099, "Call Waiting Tone" },
            { 0x000B_009A, "Confirmation Tone 1" },
            { 0x000B_009B, "Confirmation Tone 2" },
            { 0x000B_009C, "Tones Off" },
            { 0x000B_009D, "Outside Ringback" },
            { 0x000B_009E, "Ringer" },
            { 0x000B_00B0, "Phone Key 0" },
            { 0x000B_00B1, "Phone Key 1" },
            { 0x000B_00B2, "Phone Key 2" },
            { 0x000B_00B3, "Phone Key 3" },
            { 0x000B_00B4, "Phone Key 4" },
            { 0x000B_00B5, "Phone Key 5" },
            { 0x000B_00B6, "Phone Key 6" },
            { 0x000B_00B7, "Phone Key 7" },
            { 0x000B_00B8, "Phone Key 8" },
            { 0x000B_00B9, "Phone Key 9" },
            { 0x000B_00BA, "Phone Key Star" },
            { 0x000B_00BB, "Phone Key Pound" },
            { 0x000B_00BC, "Phone Key A" },
            { 0x000B_00BD, "Phone Key B" },
            { 0x000B_00BE, "Phone Key C" },
            { 0x000B_00BF, "Phone Key D" },
            //----------------
            #endregion
            #region " 0x0C: Consumer Page "
            //----------------
            { 0x000C_0000, "Unassigned" },
            { 0x000C_0001, "Consumer Control" },
            { 0x000C_0002, "Numeric Key Pad" },
            { 0x000C_0003, "Programmable Buttons" },
            { 0x000C_0004, "Microphone" },
            { 0x000C_0005, "Headphone" },
            { 0x000C_0006, "Graphic Equalizer" },
            { 0x000C_0020, "+10" },
            { 0x000C_0021, "+100" },
            { 0x000C_0022, "AM/PM" },
            { 0x000C_0030, "Power" },
            { 0x000C_0031, "Reset" },
            { 0x000C_0032, "Slepp" },
            { 0x000C_0033, "Sleep After" },
            { 0x000C_0034, "Sleep Mode" },
            { 0x000C_0035, "Illumication" },
            { 0x000C_0036, "Function Buttons" },
            { 0x000C_0040, "Menu" },
            { 0x000C_0041, "Menu Pick" },
            { 0x000C_0042, "Menu Up" },
            { 0x000C_0043, "Menu Down" },
            { 0x000C_0044, "Menu Left" },
            { 0x000C_0045, "Menu Right" },
            { 0x000C_0046, "Menu Escape" },
            { 0x000C_0047, "Menu Value Increase" },
            { 0x000C_0048, "Menu Value Decrease" },
            { 0x000C_0060, "Data On Screen" },
            { 0x000C_0061, "Closed Caption" },
            { 0x000C_0062, "Closed Caption Select" },
            { 0x000C_0063, "VCR/TV" },
            { 0x000C_0064, "Broadcast Mode" },
            { 0x000C_0065, "Snapshot" },
            { 0x000C_0066, "Still" },
            { 0x000C_0080, "Selection" },
            { 0x000C_0081, "Assign Selection" },
            { 0x000C_0082, "Mode Step" },
            { 0x000C_0083, "Recall Last" },
            { 0x000C_0084, "Enter Channel" },
            { 0x000C_0085, "Order Movie" },
            { 0x000C_0086, "Channel" },
            { 0x000C_0087, "Media Selection" },
            { 0x000C_0088, "Media Select Computer" },
            { 0x000C_0089, "Media Select TV" },
            { 0x000C_008A, "Media Select WWW" },
            { 0x000C_008B, "Media Select DVD" },
            { 0x000C_008C, "Media Select Telephone" },
            { 0x000C_008D, "Media Select Program Guide" },
            { 0x000C_008E, "Media Select Video Phone" },
            { 0x000C_008F, "Media Select Games" },
            { 0x000C_0090, "Media Select Messages" },
            { 0x000C_0091, "Media Select CD" },
            { 0x000C_0092, "Media Select VCR" },
            { 0x000C_0093, "Media Select Tuner" },
            { 0x000C_0094, "Quit" },
            { 0x000C_0095, "Help" },
            { 0x000C_0096, "Media Select Tape" },
            { 0x000C_0097, "Media Select Cable" },
            { 0x000C_0098, "Media Select Satellite" },
            { 0x000C_0099, "Media Select Security" },
            { 0x000C_009A, "Media Select Home" },
            { 0x000C_009B, "Media Select Call" },
            { 0x000C_009C, "Channel Increment" },
            { 0x000C_009D, "Channel Decrement" },
            { 0x000C_009E, "Media Select SAP" },
            { 0x000C_00A0, "VCR Plus" },
            { 0x000C_00A1, "Once" },
            { 0x000C_00A2, "Daily" },
            { 0x000C_00A3, "Weekly" },
            { 0x000C_00A4, "Monthly" },
            { 0x000C_00B0, "Play" },
            { 0x000C_00B1, "Pause" },
            { 0x000C_00B2, "Record" },
            { 0x000C_00B3, "Fast Forward" },
            { 0x000C_00B4, "Rewind" },
            { 0x000C_00B5, "Scan Nect Track" },
            { 0x000C_00B6, "Scan Previous Track" },
            { 0x000C_00B7, "Stop" },
            { 0x000C_00B8, "Eject" },
            { 0x000C_00B9, "Random Play" },
            { 0x000C_00BA, "Select Disc" },
            { 0x000C_00BB, "Enter Disc" },
            { 0x000C_00BC, "Repeat" },
            { 0x000C_00BD, "Tracking" },
            { 0x000C_00BE, "Track Normal" },
            { 0x000C_00BF, "Slow Tracking" },
            { 0x000C_00C0, "Frame Forward" },
            { 0x000C_00C1, "Frame Back" },
            { 0x000C_00C2, "Mark" },
            { 0x000C_00C3, "Clear Mark" },
            { 0x000C_00C4, "Repeat From Mark" },
            { 0x000C_00C5, "Return To Mark" },
            { 0x000C_00C6, "Search Mark Forward" },
            { 0x000C_00C7, "Search Mark Backwards" },
            { 0x000C_00C8, "Counter Reset" },
            { 0x000C_00C9, "Show Counter" },
            { 0x000C_00CA, "Tracking Increment" },
            { 0x000C_00CB, "Tracking Decrement" },
            { 0x000C_00CC, "Stop/Eject" },
            { 0x000C_00CD, "Play/Pause" },
            { 0x000C_00E0, "Volume" },
            { 0x000C_00E1, "Balance" },
            { 0x000C_00E2, "Mute" },
            { 0x000C_00E3, "Bass" },
            { 0x000C_00E4, "Treble" },
            { 0x000C_00E5, "Bass boost" },
            { 0x000C_00E6, "Surround Mode" },
            { 0x000C_00E7, "Loudness" },
            { 0x000C_00E8, "MPX" },
            { 0x000C_00E9, "Volume Increment" },
            { 0x000C_00EA, "Volume Decrement" },
            { 0x000C_00F0, "Speed Select" },
            { 0x000C_00F1, "Playback Speed" },
            { 0x000C_00F2, "Standard Play" },
            { 0x000C_00F3, "Long Play" },
            { 0x000C_00F4, "Extended Play" },
            { 0x000C_00F5, "Slow" },
            { 0x000C_0100, "Fan Enable" },
            { 0x000C_0101, "Fan Speed" },
            { 0x000C_0102, "Light Enable" },
            { 0x000C_0103, "Light Illumication Level" },
            { 0x000C_0104, "Climate Control Enable" },
            { 0x000C_0105, "Room Temperature" },
            { 0x000C_0106, "Security Enable" },
            { 0x000C_0107, "Fire Alarm" },
            { 0x000C_0108, "Police Alarm" },
            { 0x000C_0109, "Proximity" },
            { 0x000C_010A, "Motion" },
            { 0x000C_010B, "Duress Alarm" },
            { 0x000C_010C, "Holdup Alarm" },
            { 0x000C_010D, "Medical Alarm" },
            { 0x000C_0150, "Balance Right" },
            { 0x000C_0151, "Balance Left" },
            { 0x000C_0152, "Bass Increment" },
            { 0x000C_0153, "Bass Decrement" },
            { 0x000C_0154, "Treble Increment" },
            { 0x000C_0155, "Treble Decrement" },
            { 0x000C_0160, "Speaker System" },
            { 0x000C_0161, "Channel Left" },
            { 0x000C_0162, "Channel Right" },
            { 0x000C_0163, "Channel Center" },
            { 0x000C_0164, "Channel Front" },
            { 0x000C_0165, "Channel Center Front" },
            { 0x000C_0166, "Channel Side" },
            { 0x000C_0167, "Channel Surround" },
            { 0x000C_0168, "Channel Low Frequency Enhancement" },
            { 0x000C_0169, "Channel Top" },
            { 0x000C_016A, "Channel Unknown" },
            { 0x000C_0170, "Sub-channel" },
            { 0x000C_0171, "Sub-channel Increment" },
            { 0x000C_0172, "Sub-channel Decrement" },
            { 0x000C_0173, "Alternate Audio Increment" },
            { 0x000C_0174, "Alternate Audio Decrement" },
            { 0x000C_0180, "Application Launch Buttons" },
            { 0x000C_0181, "AL Launch Button Configuration Tool" },
            { 0x000C_0182, "AL Programmable Button Configuratin" },
            { 0x000C_0183, "AL Consumer Control Configuration" },
            { 0x000C_0184, "AL Word Processor" },
            { 0x000C_0185, "AL Text Editor" },
            { 0x000C_0186, "AL Spreadsheet" },
            { 0x000C_0187, "AL Graphics Editor" },
            { 0x000C_0188, "AL Presentation App" },
            { 0x000C_0189, "AL Database App" },
            { 0x000C_018A, "AL Email Reader" },
            { 0x000C_018B, "AL Newsreader" },
            { 0x000C_018C, "AL Voicemail" },
            { 0x000C_018D, "AL Contacts/Address Book" },
            { 0x000C_018E, "AL Calendar/Schedule" },
            { 0x000C_018F, "AL Task/Project Manager" },
            { 0x000C_0190, "AL Log/Journal/Timecard" },
            { 0x000C_0191, "AL Checkbook/Finance" },
            { 0x000C_0192, "AL Calculator" },
            { 0x000C_0193, "AL A/V Capture/Playback" },
            { 0x000C_0194, "AL Local Machine Browser" },
            { 0x000C_0195, "AL LAN/WAN Browser" },
            { 0x000C_0196, "AL Internet Browser" },
            { 0x000C_0197, "AL Remote Networking/ISP Connect" },
            { 0x000C_0198, "AL Network Conference" },
            { 0x000C_0199, "AL Network Chat" },
            { 0x000C_019A, "AL Telephony/Dialer" },
            { 0x000C_019B, "AL Logon" },
            { 0x000C_019C, "AL Logoff" },
            { 0x000C_019D, "AL Logon/Logoff" },
            { 0x000C_019E, "AL Terminal Lock/Screensaver" },
            { 0x000C_019F, "AL Control Panel" },
            { 0x000C_01A0, "AL Command Line Processor/Run" },
            { 0x000C_01A1, "AL Process/Task Manager" },
            { 0x000C_01A2, "AL Select Task/Application" },
            { 0x000C_01A3, "AL Next Task/Application" },
            { 0x000C_01A4, "AL Previous Task/Application" },
            { 0x000C_01A5, "AL Preemptive Halt Task/Application" },
            { 0x000C_01A6, "AL Integrated Help Center" },
            { 0x000C_01A7, "AL Documents" },
            { 0x000C_01A8, "AL Thesaurus" },
            { 0x000C_01A9, "AL Dictionary" },
            { 0x000C_01AA, "AL Desktop" },
            { 0x000C_01AB, "AL Spell Check" },
            { 0x000C_01AC, "AL Grammar Check" },
            { 0x000C_01AD, "AL Wireless Status" },
            { 0x000C_01AE, "AL Keybaord Layout" },
            { 0x000C_01AF, "AL Virus Protection" },
            { 0x000C_01B0, "AL Encryption" },
            { 0x000C_01B1, "AL Screen Saver" },
            { 0x000C_01B2, "AL Alarms" },
            { 0x000C_01B3, "AL Cock" },
            { 0x000C_01B4, "AL Fire Browser" },
            { 0x000C_01B5, "AL Power Status" },
            { 0x000C_01B6, "AL Image Browser" },
            { 0x000C_01B7, "AL Audio Browser" },
            { 0x000C_01B8, "AL Movie Browser" },
            { 0x000C_01B9, "AL Digital rights Manager" },
            { 0x000C_01BA, "AL Digital Wallet" },
            { 0x000C_01BC, "AL Instant Messaging" },
            { 0x000C_01BD, "AL OEM Features/Tips/Tutorial Browser" },
            { 0x000C_01BE, "AL OEM Help" },
            { 0x000C_01BF, "AL Online Community" },
            { 0x000C_01C0, "AL Entertainment Content Browser" },
            { 0x000C_01C1, "AL Online Shopping Browser" },
            { 0x000C_01C2, "AL SmartCard Information/Help" },
            { 0x000C_01C3, "AL Market Monitor/Finance Browser" },
            { 0x000C_01C4, "AL Customized Corporate News Browser" },
            { 0x000C_01C5, "AL Online Activity Browser" },
            { 0x000C_01C6, "AL Research/Search Browser" },
            { 0x000C_01C7, "AL Audio Player" },
            { 0x000C_0200, "Generic GUI Application Controls" },
            { 0x000C_0201, "AC New" },
            { 0x000C_0202, "AC Open" },
            { 0x000C_0203, "AC Close" },
            { 0x000C_0204, "AC Exit" },
            { 0x000C_0205, "AC Maximize" },
            { 0x000C_0206, "AC Minimize" },
            { 0x000C_0207, "AC Save" },
            { 0x000C_0208, "AC Print" },
            { 0x000C_0209, "AC Properties" },
            { 0x000C_021A, "AC Undo" },
            { 0x000C_021B, "AC Copy" },
            { 0x000C_021C, "AC Cut" },
            { 0x000C_021D, "AC Paster" },
            { 0x000C_021E, "AC Select All" },
            { 0x000C_021F, "AC Find" },
            { 0x000C_0220, "AC Find and Replace" },
            { 0x000C_0221, "AC Search" },
            { 0x000C_0222, "AC Go To" },
            { 0x000C_0223, "AC Home" },
            { 0x000C_0224, "AC Back" },
            { 0x000C_0225, "AC Forward" },
            { 0x000C_0226, "AC Stop" },
            { 0x000C_0227, "AC Refresh" },
            { 0x000C_0228, "AC Previous Link" },
            { 0x000C_0229, "AC Next Link" },
            { 0x000C_022A, "AC Bookmarks" },
            { 0x000C_022B, "AC History" },
            { 0x000C_022C, "AC Subscriptions" },
            { 0x000C_022D, "AC Zoom In" },
            { 0x000C_022E, "AC Zoom Out" },
            { 0x000C_022F, "AC Zoom" },
            { 0x000C_0230, "AC Full Screen View" },
            { 0x000C_0231, "AC Normal View" },
            { 0x000C_0232, "AC View Toggle" },
            { 0x000C_0233, "AC Scroll Up" },
            { 0x000C_0234, "AC Scroll Down" },
            { 0x000C_0235, "AC Scroll" },
            { 0x000C_0236, "AC Pan Left" },
            { 0x000C_0237, "AC Pan Right" },
            { 0x000C_0238, "AC Pan" },
            { 0x000C_0239, "AC New Window" },
            { 0x000C_023A, "AC Tile Horizontally" },
            { 0x000C_023B, "AC Tile Vertically" },
            { 0x000C_023C, "AC Format" },
            { 0x000C_023D, "AC Edit" },
            { 0x000C_023E, "AC Bold" },
            { 0x000C_023F, "AC Italics" },
            { 0x000C_0240, "AC Underline" },
            { 0x000C_0241, "AC Strikethrough" },
            { 0x000C_0242, "AC Subscript" },
            { 0x000C_0243, "AC Superscript" },
            { 0x000C_0244, "AC All caps" },
            { 0x000C_0245, "AC Rotate" },
            { 0x000C_0246, "AC Resize" },
            { 0x000C_0247, "AC Flip Horizontal" },
            { 0x000C_0248, "AC Flip Vertical" },
            { 0x000C_0249, "AC Mirror Horizontal" },
            { 0x000C_024A, "AC Mirror Vertical" },
            { 0x000C_024B, "AC Font select" },
            { 0x000C_024C, "AC Font Color" },
            { 0x000C_024D, "AC Font Size" },
            { 0x000C_024E, "AC Justify Left" },
            { 0x000C_024F, "AC Justify Center H" },
            { 0x000C_0250, "AC Justify Right" },
            { 0x000C_0251, "AC Justify Block H" },
            { 0x000C_0252, "AC Justify Top" },
            { 0x000C_0253, "AC Justify Center V" },
            { 0x000C_0254, "AC Justify Bottom" },
            { 0x000C_0255, "AC Justify Block V" },
            { 0x000C_0256, "AC Indent Decrease" },
            { 0x000C_0257, "AC Indent Increase" },
            { 0x000C_0258, "AC Numbered List" },
            { 0x000C_0259, "AC Restart Numbering" },
            { 0x000C_025A, "AC Bulleted List" },
            { 0x000C_025B, "AC Promote" },
            { 0x000C_025C, "AC Demote" },
            { 0x000C_025D, "AC Yes" },
            { 0x000C_025E, "AC No" },
            { 0x000C_025F, "AC Cancel" },
            { 0x000C_0260, "AC Catalog" },
            { 0x000C_0261, "AC Buy/Checkout" },
            { 0x000C_0262, "AC Add to Card" },
            { 0x000C_0263, "AC Expand" },
            { 0x000C_0264, "AC Expand All" },
            { 0x000C_0265, "AC Collapse" },
            { 0x000C_0266, "AC Collapse All" },
            { 0x000C_0267, "AC Print Preview" },
            { 0x000C_0268, "AC Paste Special" },
            { 0x000C_0269, "AC Insert Mode" },
            { 0x000C_026A, "AC Delete" },
            { 0x000C_026B, "AC Lock" },
            { 0x000C_026C, "AC Unlock" },
            { 0x000C_026D, "AC Protect" },
            { 0x000C_026E, "AC Unprotect" },
            { 0x000C_026F, "AC Attach Comment" },
            { 0x000C_0270, "AC Delete Comment" },
            { 0x000C_0271, "AC View Comment" },
            { 0x000C_0272, "AC Select Word" },
            { 0x000C_0273, "AC Select Sentence" },
            { 0x000C_0274, "AC Select Paragraph" },
            { 0x000C_0275, "AC Select Column" },
            { 0x000C_0276, "AC Select Row" },
            { 0x000C_0277, "AC Select Table" },
            { 0x000C_0278, "AC Select Object" },
            { 0x000C_0279, "AC Redo/Repeat" },
            { 0x000C_027A, "AC Sort" },
            { 0x000C_027B, "AC Sort Ascending" },
            { 0x000C_027C, "AC Sort Descending" },
            { 0x000C_027D, "AC Filter" },
            { 0x000C_027E, "AC Set Clock" },
            { 0x000C_027F, "AC View Clock" },
            { 0x000C_0280, "AC Select Time Zone" },
            { 0x000C_0281, "AC Edit Time Zone" },
            { 0x000C_0282, "AC Set Alarm" },
            { 0x000C_0283, "AC Clear Alarm" },
            { 0x000C_0284, "AC Snooze Alarm" },
            { 0x000C_0285, "AC Reset Alarm" },
            { 0x000C_0286, "AC Synchronize" },
            { 0x000C_0287, "AC Send/Receive" },
            { 0x000C_0288, "AC Send To" },
            { 0x000C_0289, "AC Reply" },
            { 0x000C_028A, "AC Reply All" },
            { 0x000C_028B, "AC Forward Msg" },
            { 0x000C_028C, "AC Send" },
            { 0x000C_028D, "AC Attach File" },
            { 0x000C_028E, "AC Upload" },
            { 0x000C_028F, "AC Download (Save Target As)" },
            { 0x000C_0290, "AC Set Borders" },
            { 0x000C_0291, "AC Insert Row" },
            { 0x000C_0292, "AC Insert Column" },
            { 0x000C_0293, "AC Insert File" },
            { 0x000C_0294, "AC Insert Picture" },
            { 0x000C_0295, "AC Insert Object" },
            { 0x000C_0296, "AC Insert Symbol" },
            { 0x000C_0297, "AC Save and Close" },
            { 0x000C_0298, "AC Rename" },
            { 0x000C_0299, "AC Merge" },
            { 0x000C_029A, "AC Split" },
            { 0x000C_029B, "AC Distribute Horizontally" },
            { 0x000C_029C, "AC Distribute Vertically" },
            //----------------
            #endregion
            #region " 0x0D: Digitizers Page "
            //----------------
            { 0x000D_0000, "Undefined" },
            { 0x000D_0001, "Digitizer" },
            { 0x000D_0002, "Pen" },
            { 0x000D_0003, "Light Pen" },
            { 0x000D_0004, "Touch Screen" },
            { 0x000D_0005, "Touch Pad" },
            { 0x000D_0006, "White Board" },
            { 0x000D_0007, "Coordinate Measuring Machine" },
            { 0x000D_0008, "3D Digitizer" },
            { 0x000D_0009, "Stereo Plotter" },
            { 0x000D_000A, "Articulated Arm" },
            { 0x000D_000B, "Armature" },
            { 0x000D_000C, "Multiple Point Digitizer" },
            { 0x000D_000D, "Free Space Wand" },
            { 0x000D_0020, "Sylus" },
            { 0x000D_0021, "Puck" },
            { 0x000D_0022, "Finger" },
            { 0x000D_0030, "Tip Pressure" },
            { 0x000D_0031, "Barrel Pressure" },
            { 0x000D_0032, "In Range" },
            { 0x000D_0033, "Touch" },
            { 0x000D_0034, "Untouch" },
            { 0x000D_0035, "Tap" },
            { 0x000D_0036, "Quality" },
            { 0x000D_0037, "Data Valid" },
            { 0x000D_0038, "Transducer Index" },
            { 0x000D_0039, "Tablet Function Keys" },
            { 0x000D_003A, "Program Change Keys" },
            { 0x000D_003B, "Battery Strength" },
            { 0x000D_003C, "Invert" },
            { 0x000D_003D, "X Tilt" },
            { 0x000D_003E, "Y tilt" },
            { 0x000D_003F, "Azimuth" },
            { 0x000D_0040, "Altitude" },
            { 0x000D_0041, "Twist" },
            { 0x000D_0042, "Tip Switch" },
            { 0x000D_0043, "Secondary Tip Switch" },
            { 0x000D_0044, "Barrel Switch" },
            { 0x000D_0045, "Eraser" },
            { 0x000D_0046, "Tablet Pick" },
            //----------------
            #endregion
            #region " 0x14: Alphanumeric Display Page "
            //----------------
            { 0x0014_0000, "Undefined" },
            { 0x0014_0001, "Alphanumeric Display" },
            { 0x0014_0002, "Bitmapped Display" },
            { 0x0014_0020, "Display Attributes Report" },
            { 0x0014_0021, "ASCII Character Set" },
            { 0x0014_0022, "Data Read Back" },
            { 0x0014_0023, "Font Read Back" },
            { 0x0014_0024, "Display Control Report" },
            { 0x0014_0025, "Clear Display" },
            { 0x0014_0026, "Display Enable" },
            { 0x0014_0027, "Screen Saver Delay" },
            { 0x0014_0028, "Screen Saver Enable" },
            { 0x0014_0029, "Vertical Scroll" },
            { 0x0014_002A, "Horizontal Scroll" },
            { 0x0014_002B, "Character Report" },
            { 0x0014_002C, "Display Data" },
            { 0x0014_002D, "Display Status" },
            { 0x0014_002E, "Stat Not Ready" },
            { 0x0014_002F, "Stat Ready" },
            { 0x0014_0030, "Err Not a loadable character" },
            { 0x0014_0031, "Err Font data cannot be read" },
            { 0x0014_0032, "Cursor Position Report" },
            { 0x0014_0033, "Row" },
            { 0x0014_0034, "Column" },
            { 0x0014_0035, "Rows" },
            { 0x0014_0036, "Columns" },
            { 0x0014_0037, "Cursor Pixel Positioning" },
            { 0x0014_0038, "Cursor Mode" },
            { 0x0014_0039, "Cursor Enable" },
            { 0x0014_003A, "Cursor Blink" },
            { 0x0014_003B, "Font Report" },
            { 0x0014_003C, "Font Data" },
            { 0x0014_003D, "Character Width" },
            { 0x0014_003E, "Character Height" },
            { 0x0014_003F, "Character Spacing Horizontal" },
            { 0x0014_0040, "Character Spacing Vertical" },
            { 0x0014_0041, "Unicode Character Set" },
            { 0x0014_0042, "Font 7-Segment" },
            { 0x0014_0043, "7-Segment Direct Map" },
            { 0x0014_0044, "Font 14-Segment" },
            { 0x0014_0045, "14-Segment Direct Map" },
            { 0x0014_0046, "Display Brightness" },
            { 0x0014_0047, "Display Contrast" },
            { 0x0014_0048, "Character Attribute" },
            { 0x0014_0049, "Attribute Readback" },
            { 0x0014_004A, "Attribute data" },
            { 0x0014_004B, "Char Attr Enhance" },
            { 0x0014_004C, "Char Attr Underline" },
            { 0x0014_004D, "Char Attr Blink" },
            { 0x0014_0080, "Bitmap Size X" },
            { 0x0014_0081, "Bitmap Size Y" },
            { 0x0014_0083, "Bit Depth Format" },
            { 0x0014_0084, "Display Orientation" },
            { 0x0014_0085, "Palette Report" },
            { 0x0014_0086, "Palette Data Size" },
            { 0x0014_0087, "Palette Data Offset" },
            { 0x0014_0088, "Palette Data" },
            { 0x0014_008A, "Blit Report" },
            { 0x0014_008B, "Blit Rectangle X1" },
            { 0x0014_008C, "Blit Rectangle Y1" },
            { 0x0014_008D, "Blit Rectangle X2" },
            { 0x0014_008E, "Blit Rectangle Y2" },
            { 0x0014_008F, "Blit Data" },
            { 0x0014_0090, "Soft Button" },
            { 0x0014_0091, "Soft Button ID" },
            { 0x0014_0092, "Soft Button Side" },
            { 0x0014_0093, "Soft Button Offset 1" },
            { 0x0014_0094, "Soft Button Offset 2" },
            { 0x0014_0095, "Soft Button Report" },
            //----------------
            #endregion
            #region " 0x40: Medical Instrument Page "
            //----------------
            { 0x0040_0000, "Undefined" },
            { 0x0040_0001, "Medical Ultrasound" },
            { 0x0040_0020, "VCR/Acquisition" },
            { 0x0040_0021, "Freeze/Thaw" },
            { 0x0040_0022, "Clip Store" },
            { 0x0040_0023, "Update" },
            { 0x0040_0024, "Next" },
            { 0x0040_0025, "Save" },
            { 0x0040_0026, "Print" },
            { 0x0040_0027, "Microphone Enable" },
            { 0x0040_0040, "Cine" },
            { 0x0040_0041, "Transmit Power" },
            { 0x0040_0042, "Volume" },
            { 0x0040_0043, "Focus" },
            { 0x0040_0044, "Depth" },
            { 0x0040_0060, "Soft Step - Primary" },
            { 0x0040_0061, "Soft Step - Secondary" },
            { 0x0040_0070, "Depth Gain Compensation" },
            { 0x0040_0080, "Zoom Select" },
            { 0x0040_0081, "Zoom Adjust" },
            { 0x0040_0082, "Spectral Doppler Mode Select" },
            { 0x0040_0083, "Spectral Doppler Adjust" },
            { 0x0040_0084, "Color Doppler Mode Select" },
            { 0x0040_0085, "Color Doppler Adjust" },
            { 0x0040_0086, "Motion Mode Select" },
            { 0x0040_0087, "Motion Mode Adjust" },
            { 0x0040_0088, "2-D Mode Select" },
            { 0x0040_0089, "2-D Mode Adjust" },
            { 0x0040_00A0, "Soft Control Select" },
            { 0x0040_00A1, "Soft Control Adjust" },
            //----------------
            #endregion
        };
    }
}
