﻿using System;
using System.Collections.Generic;
using System.Text;
using SharpDX;
using FDK;

namespace DTXMania2
{
    /// <summary>
    ///     ローディングスピナー（回転画像アニメーション）。
    /// </summary>
    class LoadingSpinner : IDisposable
    {
        const int _回転段数 = 9;


        // 生成と終了

        public LoadingSpinner()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._Spinner画像 = new 画像( @"$(Images)\LoadingSpinner.png" );
            this._回転カウンタ = new LoopCounter( 0, _回転段数, 100 );
        }

        public void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._Spinner画像.Dispose();
        }



        // 進行と描画


        public void 描画する()
        {
            var count = this._回転カウンタ.現在値;   // 0～_回転段数-1
            var 変換行列 = Matrix.RotationZ( (float) ( -2.0 * Math.PI * ( (double) count / _回転段数 ) ) );

            this._Spinner画像.描画する( 変換行列 );
        }



        // ローカル


        private readonly 画像 _Spinner画像;

        private readonly LoopCounter _回転カウンタ;
    }
}
