using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SharpDX;
using FDK;

namespace DTXMania2.演奏
{
    class レーンフラッシュ : IDisposable
    {


        // 生成と終了


        public レーンフラッシュ()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._レーンフラッシュ画像 = new 画像( @"$(Images)\PlayStage\LaneFlush.png" ) { 加算合成する = true };
            this._レーンフラッシュの矩形リスト = new 矩形リスト( @"$(Images)\PlayStage\LaneFlush.yaml" );

            this._フラッシュ情報 = new Dictionary<表示レーン種別, Counter>() {
                { 表示レーン種別.LeftCymbal,  new Counter() },
                { 表示レーン種別.HiHat,       new Counter() },
                { 表示レーン種別.Foot,        new Counter() },
                { 表示レーン種別.Snare,       new Counter() },
                { 表示レーン種別.Tom1,        new Counter() },
                { 表示レーン種別.Bass,        new Counter() },
                { 表示レーン種別.Tom2,        new Counter() },
                { 表示レーン種別.Tom3,        new Counter() },
                { 表示レーン種別.RightCymbal, new Counter() },
            };
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._レーンフラッシュ画像.Dispose();
        }



        // フラッシュ開始


        public void 開始する( 表示レーン種別 laneType )
        {
            this._フラッシュ情報[ laneType ].開始する( 0, 10, 15 );
        }



        // 進行と描画


        public void 進行描画する()
        {
            foreach( var laneType in this._フラッシュ情報.Keys )
            {
                if( this._フラッシュ情報[ laneType ].終了値に達した )
                    continue;

                var フラッシュ１枚のサイズ = new Size2F( this._レーンフラッシュの矩形リスト[ laneType.ToString() ]!.Value.Width, this._レーンフラッシュの矩形リスト[ laneType.ToString() ]!.Value.Height );

                float 割合 = this._フラッシュ情報[ laneType ].現在値の割合;   // 0 → 1
                float 横拡大率 = 0.2f + 0.8f * 割合;                          // 0.2 → 1.0
                割合 = (float) Math.Cos( 割合 * Math.PI / 2f );               // 1 → 0（加速しながら）

                for( float y = ( レーンフレーム.領域.Bottom - フラッシュ１枚のサイズ.Height ); y > ( レーンフレーム.領域.Top - フラッシュ１枚のサイズ.Height ); y -= フラッシュ１枚のサイズ.Height - 0.5f )
                {
                    this._レーンフラッシュ画像.進行描画する(
                        レーンフレーム.レーン中央位置X[ laneType ] - フラッシュ１枚のサイズ.Width * 横拡大率 / 2f,
                        y,
                        不透明度0to1: 割合 * 0.75f,   // ちょっと暗めに。
                        転送元矩形: this._レーンフラッシュの矩形リスト[ laneType.ToString() ]!.Value,
                        X方向拡大率: 横拡大率 );
                }
            }
        }



        // ローカル


        private readonly 画像 _レーンフラッシュ画像;

        private readonly 矩形リスト _レーンフラッシュの矩形リスト;

        private readonly Dictionary<表示レーン種別, Counter> _フラッシュ情報;
    }
}
