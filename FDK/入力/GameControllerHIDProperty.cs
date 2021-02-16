using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FDK
{
    /// <summary>
    ///     HIDゲームコントローラのプロパティ。
    /// </summary>
    public class GameControllerHIDProperty : IDisposable
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
            this.ButtonCaps = Array.Empty<HID.ButtonCaps>();
            this.ValueCaps = Array.Empty<HID.ValueCaps>();
            this.CollectionNodes = Array.Empty<HID.LinkCollectionNode>();
            this.ButtonState = Array.Empty<bool[]>();
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
