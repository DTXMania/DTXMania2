using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania2
{
    class システム情報 : IDisposable
    {

        // プロパティ


        public int 現在のFPS => this._FPS.現在のFPS;

        public int 現在のVPS => this._FPS.現在のVPS;



        // 生成と終了


        public システム情報()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._FPS = new FPS();
            this._文字列画像 = new 文字列画像D2D();
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._文字列画像.Dispose();
            this._FPS.Dispose();
        }



        // カウント


        public bool FPSをカウントしプロパティを更新する()
            => this._FPS.FPSをカウントしプロパティを更新する();

        public void VPSをカウントする()
            => this._FPS.VPSをカウントする();



        // 進行と描画


        public void 描画する( DeviceContext dc, string 追加文字列 = "" )
        {
            double FPSの周期ms = ( 0 < this._FPS.現在のFPS ) ? ( 1000.0 / this._FPS.現在のFPS ) : -1.0;

            this._文字列画像.表示文字列 =
                $"VPS: {this._FPS.現在のVPS.ToString()} / FPS: {this._FPS.現在のFPS.ToString()} (" + FPSの周期ms.ToString( "0.000" ) + "ms)" + Environment.NewLine +
                $"GameMode: {ONOFF[ GameMode.ゲームモードである ]}" + Environment.NewLine +
                追加文字列;

            this._文字列画像.描画する( dc, 0f, 0f );
        }



        // プライベート


        private FPS _FPS;

        private 文字列画像D2D _文字列画像;

        private readonly Dictionary<bool, string> ONOFF = new Dictionary<bool, string> { { false, "OFF" }, { true, "ON" } };
    }
}
