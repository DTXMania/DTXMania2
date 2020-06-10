using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using FDK;
using DTXMania2.曲;
using SharpDX.Direct2D1;

namespace DTXMania2.曲読み込み
{
    class プレビュー画像 : IDisposable
    {

        // 生成と終了


        public プレビュー画像()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._既定のノード画像 = new 画像D2D( @"$(Images)\DefaultPreviewImage.png" );
            this._現行化前のノード画像 = new 画像D2D( @"$(Images)\PreviewImageWaitForActivation.png" );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._現行化前のノード画像.Dispose();
            this._既定のノード画像.Dispose();
        }



        // 進行と描画


        public void 進行描画する( DeviceContext dc )
        {
            var ノード画像 = Global.App.演奏譜面.譜面と画像を現行化済み ? ( Global.App.演奏譜面.プレビュー画像 ?? this._既定のノード画像 ) : this._現行化前のノード画像;

            var 変換行列2D =
                Matrix3x2.Scaling(
                    this._プレビュー画像表示サイズdpx.X / ノード画像.サイズ.Width,
                    this._プレビュー画像表示サイズdpx.Y / ノード画像.サイズ.Height )*
                Matrix3x2.Translation(
                    this._プレビュー画像表示位置dpx.X,
                    this._プレビュー画像表示位置dpx.Y );

            ノード画像.描画する( dc, 変換行列2D );
        }



        // ローカル


        private readonly Vector3 _プレビュー画像表示位置dpx = new Vector3( 150f, 117f, 0f );

        private readonly Vector3 _プレビュー画像表示サイズdpx = new Vector3( 576f, 576f, 0f );

        private readonly 画像D2D _既定のノード画像;

        private readonly 画像D2D _現行化前のノード画像;
    }
}
