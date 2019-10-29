using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace DTXMania2.演奏
{
    class 曲名パネル : IDisposable
    {


        // 生成と終了


        public 曲名パネル()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._パネル = new 画像( @"$(Images)\PlayStage\ScoreTitlePanel.png" );
            this._既定のノード画像 = new 画像( @"$(Images)\DefaultPreviewImage.png" );
            this._現行化前のノード画像 = new 画像( @"$(Images)\PreviewImageWaitForActivation.png" );

            this._曲名画像 = new 文字列画像D2D() {
                フォント名 = "HGMaruGothicMPRO",
                フォントサイズpt = 26f,
                フォントの太さ = FontWeight.Regular,
                フォントスタイル = FontStyle.Normal,
                描画効果 = 文字列画像D2D.効果.縁取り,
                縁のサイズdpx = 4f,
                前景色 = Color4.Black,
                背景色 = Color4.White,
                表示文字列 = Global.App.演奏スコア?.曲名 ?? "",
            };

            this._サブタイトル画像 = new 文字列画像D2D() {
                フォント名 = "HGMaruGothicMPRO",
                フォントサイズpt = 18f,
                フォントの太さ = FontWeight.Regular,
                フォントスタイル = FontStyle.Normal,
                描画効果 = 文字列画像D2D.効果.縁取り,
                縁のサイズdpx = 3f,
                前景色 = Color4.Black,
                背景色 = Color4.White,
                表示文字列 = Global.App.演奏スコア?.アーティスト名 ?? "",
            };
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._サブタイトル画像.Dispose();
            this._曲名画像.Dispose();
            this._現行化前のノード画像.Dispose();
            this._既定のノード画像.Dispose();
            this._パネル.Dispose();
        }



        // 進行と描画


        public void 描画する( DeviceContext dc )
        {
            this._パネル.描画する( 1458f, 3f );
            this._サムネイルを描画する();
            this._曲名を描画する( dc );
            this._サブタイトルを描画する( dc );
        }



        // ローカル


        private readonly 画像 _パネル;

        private readonly 画像 _既定のノード画像;

        private readonly 画像 _現行化前のノード画像;

        private readonly 文字列画像D2D _曲名画像;

        private readonly 文字列画像D2D _サブタイトル画像;

        private readonly Vector3 _サムネイル画像表示位置dpx = new Vector3( 1477f, 19f, 0f );

        private readonly Vector3 _サムネイル画像表示サイズdpx = new Vector3( 91f, 91f, 0f );

        private readonly Vector2 _曲名表示位置dpx = new Vector2( 1576f + 4f, 43f + 0f );

        private readonly Vector2 _曲名表示サイズdpx = new Vector2( 331f - 8f - 4f, 70f - 10f );

        private void _サムネイルを描画する()
        {
            var サムネイル画像 = Global.App.演奏譜面.プレビュー画像 ?? this._既定のノード画像;

            var 変換行列 =
                Matrix.Scaling(
                    this._サムネイル画像表示サイズdpx.X / サムネイル画像.サイズ.Width,
                    this._サムネイル画像表示サイズdpx.Y / サムネイル画像.サイズ.Height,
                    0f ) *
                Matrix.Translation( // テクスチャは画面中央が (0,0,0) で、Xは右がプラス方向, Yは上がプラス方向, Zは奥がプラス方向+。
                    Global.画面左上dpx.X + this._サムネイル画像表示位置dpx.X + this._サムネイル画像表示サイズdpx.X / 2f,
                    Global.画面左上dpx.Y - this._サムネイル画像表示位置dpx.Y - this._サムネイル画像表示サイズdpx.Y / 2f,
                    0f );

            サムネイル画像.描画する( 変換行列 );
        }

        private void _曲名を描画する( DeviceContext dc )
        {
            // 拡大率を計算して描画する。

            this._曲名画像.描画する(
                dc,
                this._曲名表示位置dpx.X,
                this._曲名表示位置dpx.Y,
                X方向拡大率: ( this._曲名画像.画像サイズdpx.Width <= this._曲名表示サイズdpx.X ) ? 1f : this._曲名表示サイズdpx.X / this._曲名画像.画像サイズdpx.Width );
        }

        private void _サブタイトルを描画する( DeviceContext dc )
        {
            // 拡大率を計算して描画する。

            this._サブタイトル画像.描画する(
                dc,
                this._曲名表示位置dpx.X,
                this._曲名表示位置dpx.Y + 30f,
                X方向拡大率: ( this._サブタイトル画像.画像サイズdpx.Width <= this._曲名表示サイズdpx.X ) ? 1f : this._曲名表示サイズdpx.X / this._サブタイトル画像.画像サイズdpx.Width );
        }
    }
}
