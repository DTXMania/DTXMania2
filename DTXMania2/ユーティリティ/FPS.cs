﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania2
{
    /// <summary>
    ///		FPS（１秒間の進行処理回数）と VPS（１秒間の描画処理回数）を計測する。
    /// </summary>
    /// <remarks>
    ///		FPSをカウントする() を呼び出さないと、VPS も更新されないので注意。
    /// </remarks>
    class FPS : FDK.FPS, IDisposable
    {

        // 生成と終了


        public FPS()
        {
            this._FPSパラメータ = new 文字列画像D2D( Global.DWriteFactory, Global.D2D1Factory1, Global.既定のD2D1DeviceContext, Global.設計画面サイズ );
        }

        public virtual void Dispose()
        {
            this._FPSパラメータ.Dispose();
        }



        // 描画


        public void 描画する( DeviceContext dc, float x = 0f, float y = 0f )
        {
            int fps = this.現在のFPS;
            int vps = this.現在のVPS;
            double FPSの周期ms = ( 0 < fps ) ? ( 1000.0 / fps ) : -1.0;

            this._FPSパラメータ.表示文字列 = $"VPS: {vps.ToString()} / FPS: {fps.ToString()} (" + FPSの周期ms.ToString( "0.000" ) + "ms)";
            this._FPSパラメータ.描画する( dc, x, y );
        }



        // ローカル


        private 文字列画像D2D _FPSパラメータ;
    }
}
