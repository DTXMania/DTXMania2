using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using DTXMania2.曲;

namespace DTXMania2.曲読み込み
{
    class プレビュー画像 : IDisposable
    {

        // 生成と終了


        public プレビュー画像()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._既定のノード画像 = new 画像( @"$(Images)\DefaultPreviewImage.png" );
            this._現行化前のノード画像 = new 画像( @"$(Images)\PreviewImageWaitForActivation.png" );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._現行化前のノード画像.Dispose();
            this._既定のノード画像.Dispose();
        }



        // 進行と描画


        public void 描画する()
        {
            var ノード画像 = Global.App.演奏譜面.譜面と画像を現行化済み ? ( Global.App.演奏譜面.プレビュー画像 ?? this._既定のノード画像 ) : this._現行化前のノード画像;

            var 変換行列 =
                Matrix.Scaling(
                    this._プレビュー画像表示サイズdpx.X / ノード画像.サイズ.Width,
                    this._プレビュー画像表示サイズdpx.Y / ノード画像.サイズ.Height,
                    0f ) *
                Matrix.Translation( // テクスチャは画面中央が (0,0,0) で、Xは右がプラス方向, Yは上がプラス方向, Zは奥がプラス方向+。
                    Global.画面左上dpx.X + this._プレビュー画像表示位置dpx.X + this._プレビュー画像表示サイズdpx.X / 2f,
                    Global.画面左上dpx.Y - this._プレビュー画像表示位置dpx.Y - this._プレビュー画像表示サイズdpx.Y / 2f,
                    0f );

            ノード画像.描画する( 変換行列 );
        }



        // ローカル


        private readonly Vector3 _プレビュー画像表示位置dpx = new Vector3( 150f, 117f, 0f );

        private readonly Vector3 _プレビュー画像表示サイズdpx = new Vector3( 576f, 576f, 0f );

        private readonly 画像 _既定のノード画像;

        private readonly 画像 _現行化前のノード画像;
    }
}
