﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct2D1;
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

            this._Spinner画像 = new 画像D2D( @"$(Images)\LoadingSpinner.png" );
            this._回転カウンタ = new LoopCounter( 0, _回転段数, 100 );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._Spinner画像.Dispose();
        }



        // 進行と描画


        public void 進行描画する( DeviceContext d2ddc )
        {
            var count = this._回転カウンタ.現在値;   // 0～_回転段数-1
            var 変換行列2D =
                Matrix3x2.Rotation(
                    angle: (float)( 2.0 * Math.PI * ( (double)count / _回転段数 ) ),
                    center: new Vector2( this._Spinner画像.サイズ.Width / 2f, this._Spinner画像.サイズ.Height / 2f ) ) *
                Matrix3x2.Translation(
                    ( Global.GraphicResources.設計画面サイズ.Width - this._Spinner画像.サイズ.Width ) / 2f,
                    ( Global.GraphicResources.設計画面サイズ.Height - this._Spinner画像.サイズ.Height ) / 2f );

            this._Spinner画像.描画する( d2ddc, 変換行列2D );
        }



        // ローカル


        private readonly 画像D2D _Spinner画像;

        private readonly LoopCounter _回転カウンタ;
    }
}
