using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DTXMania2
{
    static class GameMode
    {
        /// <summary>
        ///     ゲームモードが有効である場合はtrueを、そうでなければfalseを返す。
        /// </summary>
        public static bool ゲームモードである
        {
            get
            {
                HasExpandedResources( out bool bHas );
                return bHas;
            }
        }


        [DllImport( "gamemode.dll" )]
        public static extern int HasExpandedResources( out bool bHas );
    }
}
