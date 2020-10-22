﻿using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania2.結果
{
    partial class 曲別SKILL : IDisposable
    {

        // プロパティ


        /// <summary>
        ///     アニメがすべて終わったら true。
        /// </summary>
        public bool アニメ完了 => this._アイコン.アニメ完了 && this._下線.アニメ完了 && this._数値.アニメ完了;



        // 生成と終了


        public 曲別SKILL()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._アイコン = new アイコン();
            this._下線 = new 下線();
            this._数値 = new 数値();

            this._初めての進行描画 = true;
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._数値.Dispose();
            this._下線.Dispose();
            this._アイコン.Dispose();
        }



        // 進行と描画


        public void アニメを完了する()
        {
            this._アイコン.アニメを完了する();
            this._下線.アニメを完了する();
            this._数値.アニメを完了する();
        }

        public void 進行描画する( DeviceContext d2ddc, float x, float y, double スキル値 )
        {
            if( this._初めての進行描画 )
            {
                // アニメーション開始
                this._アイコン.開始する();
                this._下線.開始する();
                this._数値.開始する( スキル値 );

                this._初めての進行描画 = false;
            }

            this._アイコン.進行描画する( d2ddc, x, y );
            this._数値.進行描画する( d2ddc, x + 180f, y + 3f );
            this._下線.進行描画する( d2ddc, x + 33f, y + 113f );
        }



        // ローカル


        private const double _最初の待機時間sec = 0.75;

        private const double _アニメ時間sec = 0.25;

        private bool _初めての進行描画;

        private readonly 曲別SKILL.アイコン _アイコン;

        private readonly 曲別SKILL.下線 _下線;

        private readonly 曲別SKILL.数値 _数値;
    }
}
