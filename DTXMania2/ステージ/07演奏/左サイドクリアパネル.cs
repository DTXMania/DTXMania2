using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania2.演奏
{
    class 左サイドクリアパネル : IDisposable
    {

        // プロパティ


        public 描画可能画像 クリアパネル { get; }

        public 画像D2D 背景 { get; }



        // 生成と終了


        public 左サイドクリアパネル()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this.背景 = new 画像D2D( @"$(Images)\PlayStage\LeftSideClearPanel.png" );
            this.クリアパネル = new 描画可能画像( this.背景.サイズ );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this.クリアパネル.Dispose();
            this.背景.Dispose();
        }



        // 進行と描画


        public void 進行描画する()
        {
            // テクスチャは画面中央が (0,0,0) で、Xは右がプラス方向, Yは上がプラス方向, Zは奥がプラス方向+。

            var 変換行列 =
                Matrix.RotationY( MathUtil.DegreesToRadians( -48f ) ) *
                Matrix.Translation( Global.画面左上dpx.X + 230f, Global.画面左上dpx.Y - 530f, 0f );

            this.クリアパネル.描画する( 変換行列 );
        }
    }
}
