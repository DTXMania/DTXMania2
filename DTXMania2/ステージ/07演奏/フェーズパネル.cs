using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SharpDX;
using FDK;

namespace DTXMania2.演奏
{
    class フェーズパネル : IDisposable
    {

        // プロパティ


        /// <summary>
        ///		現在の位置を 開始点:0～1:終了点 で示す。
        /// </summary>
        public float 現在位置
        {
            get => this._現在位置;
            set => this._現在位置 = Math.Clamp( value, min: 0.0f, max: 1.0f );
        }



        // 生成と終了


        public フェーズパネル()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._演奏位置カーソル画像 = new 画像( @"$(Images)\PlayStage\PlayPositionCursor.png" );
            this._演奏位置カーソルの矩形リスト = new 矩形リスト( @"$(Images)\PlayStage\PlayPositionCursor.yaml" );
            this._現在位置 = 0.0f;
            this._左右三角アニメ用カウンタ = new LoopCounter( 0, 100, 5 );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._演奏位置カーソル画像.Dispose();
        }



        // 進行と描画


        public void 進行描画する()
        {

            var 中央位置dpx = new Vector2( 1308f, 876f - this._現在位置 * 767f );

            var バー矩形 = this._演奏位置カーソルの矩形リスト[ "Bar" ]!;
            this._演奏位置カーソル画像.進行描画する(
                中央位置dpx.X - バー矩形.Value.Width / 2f,
                中央位置dpx.Y - バー矩形.Value.Height / 2f,
                転送元矩形: バー矩形 );

            var 左三角矩形 = this._演奏位置カーソルの矩形リスト[ "Left" ]!;
            this._演奏位置カーソル画像.進行描画する(
                中央位置dpx.X - 左三角矩形.Value.Width / 2f - this._左右三角アニメ用カウンタ.現在値の割合 * 40f,
                中央位置dpx.Y - 左三角矩形.Value.Height / 2f,
                転送元矩形: 左三角矩形 );

            var 右三角矩形 = this._演奏位置カーソルの矩形リスト[ "Right" ]!;
            this._演奏位置カーソル画像.進行描画する(
                中央位置dpx.X - 右三角矩形.Value.Width / 2f + this._左右三角アニメ用カウンタ.現在値の割合 * 40f,
                中央位置dpx.Y - 右三角矩形.Value.Height / 2f,
                転送元矩形: 右三角矩形 );
        }



        // ローカル


        private float _現在位置 = 0.0f;

        private readonly 画像 _演奏位置カーソル画像;

        private readonly 矩形リスト _演奏位置カーソルの矩形リスト;

        private readonly LoopCounter _左右三角アニメ用カウンタ;
    }
}
