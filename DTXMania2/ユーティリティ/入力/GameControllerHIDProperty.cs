using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DTXMania2
{
    /// <summary>
    ///     HIDゲームコントローラのプロパティ。
    /// </summary>
    class GameControllerHIDProperty : IDisposable
    {
        public int DeviceID = 0;
        public string Name = "";
        public IntPtr? PreparseData = null;

        public HID.Caps Caps;
        public HID.ButtonCaps[] ButtonCaps;
        public HID.ValueCaps[] ValueCaps;
        public HID.LinkCollectionNode[] CollectionNodes;

        /// <summary>
        ///     ボタンの押下状態
        ///     [Capインデックス][ボタン番号(=UsageMixからの相対値)]
        /// </summary>
        public bool[][] ButtonState;



        // 生成と終了


        public GameControllerHIDProperty()
        {
            this.Caps = new HID.Caps();
            this.ButtonCaps = new HID.ButtonCaps[ 0 ];
            this.ValueCaps = new HID.ValueCaps[ 0 ];
            this.CollectionNodes = new HID.LinkCollectionNode[ 0 ];
            this.ButtonState = new bool[ 0 ][];
        }

        public virtual void Dispose()
        {
            if( this.PreparseData.HasValue )
            {
                //HID.HidD_FreePreparsedData( this.PreparseData.Value );  --> RawInput 経由で取得したものなので不要
                Marshal.FreeHGlobal( this.PreparseData.Value );
                this.PreparseData = null;
            }
        }
    }
}
