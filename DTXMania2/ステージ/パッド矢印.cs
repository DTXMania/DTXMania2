﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania2
{
    class パッド矢印 : IDisposable
    {

        // プロパティ


        public enum 種類 { 
            上_Tom1, 
            下_Tom2,
            左_Snare,
            右_Tom3,
        };



        // 生成と終了


        public パッド矢印()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._矢印画像 = new 画像D2D( @"$(Images)\PadArrow.png" );
            this._矢印の矩形リスト = new 矩形リスト( @"$(Images)\PadArrow.yaml" );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._矢印画像.Dispose();
        }



        // 進行と描画


        public void 描画する( DeviceContext d2ddc, 種類 type, Vector2 中央位置dpx, float 拡大率 = 1f )
        {
            var 矩形 = type switch
            {
                種類.上_Tom1 => this._矢印の矩形リスト[ "Up" ],
                種類.下_Tom2 => this._矢印の矩形リスト[ "Down" ],
                種類.左_Snare => this._矢印の矩形リスト[ "Left" ],
                種類.右_Tom3 => this._矢印の矩形リスト[ "Right" ],
                _ => throw new Exception( "未対応の種類です。" ),
            };

            if( 矩形 is null )
                return;

            var 左上位置dpx = new Vector3(
                中央位置dpx.X - 矩形.Value.Width * 拡大率 / 2f,
                中央位置dpx.Y - 矩形.Value.Height * 拡大率 / 2f,
                0f );

            var 変換行列 =
                Matrix.Scaling( 拡大率 ) *
                Matrix.Translation( 左上位置dpx );

            this._矢印画像.描画する( d2ddc, 変換行列, 転送元矩形: 矩形 );
        }



        // ローカル


        private readonly 画像D2D _矢印画像;

        private readonly 矩形リスト _矢印の矩形リスト;
    }
}
