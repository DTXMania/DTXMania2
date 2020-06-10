using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Animation;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania2.演奏
{
    class チップ光 : IDisposable
    {

        // 生成と終了


        public チップ光()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._放射光 = new 画像D2D( @"$(Images)\PlayStage\ChipFlush.png" ) { 加算合成する = true };
            this._光輪 = new 画像D2D( @"$(Images)\PlayStage\ChipFlushRing.png" ) { 加算合成する = true };
            this._放射光の矩形リスト = new 矩形リスト( @"$(Images)\PlayStage\ChipFlush.yaml" );

            this._レーンtoステータス = new Dictionary<表示レーン種別, 表示レーンステータス>() {
                { 表示レーン種別.Unknown,      new 表示レーンステータス( 表示レーン種別.Unknown ) },
                { 表示レーン種別.LeftCymbal,   new 表示レーンステータス( 表示レーン種別.LeftCymbal ) },
                { 表示レーン種別.HiHat,        new 表示レーンステータス( 表示レーン種別.HiHat ) },
                { 表示レーン種別.Foot,         new 表示レーンステータス( 表示レーン種別.Foot ) },
                { 表示レーン種別.Snare,        new 表示レーンステータス( 表示レーン種別.Snare ) },
                { 表示レーン種別.Bass,         new 表示レーンステータス( 表示レーン種別.Bass ) },
                { 表示レーン種別.Tom1,         new 表示レーンステータス( 表示レーン種別.Tom1 ) },
                { 表示レーン種別.Tom2,         new 表示レーンステータス( 表示レーン種別.Tom2 ) },
                { 表示レーン種別.Tom3,         new 表示レーンステータス( 表示レーン種別.Tom3 ) },
                { 表示レーン種別.RightCymbal,  new 表示レーンステータス( 表示レーン種別.RightCymbal ) },
            };
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            foreach( var kvp in this._レーンtoステータス )
                kvp.Value.Dispose();

            this._光輪.Dispose();
            this._放射光.Dispose();
        }



        // 表示開始


        public void 表示を開始する( 表示レーン種別 lane )
        {
            var status = this._レーンtoステータス[ lane ];

            status.現在の状態 = 表示レーンステータス.状態.表示開始;  // 描画スレッドへ通知。
        }



        // 進行と描画


        public void 進行描画する( DeviceContext dc )
        {
            foreach( 表示レーン種別? レーン in Enum.GetValues( typeof( 表示レーン種別 ) ) )
            {
                if( !レーン.HasValue )
                    continue;

                var status = this._レーンtoステータス[ レーン.Value ];

                switch( status.現在の状態 )
                {
                    case 表示レーンステータス.状態.表示開始:
                    {
                        #region " 表示開始 "
                        //----------------
                        status.アニメ用メンバを解放する();

                        // 初期状態
                        status.放射光の回転角 = new Variable( Global.Animation.Manager, initialValue: 0.0 );
                        status.放射光の拡大率 = new Variable( Global.Animation.Manager, initialValue: 1.0 );
                        status.光輪の拡大率 = new Variable( Global.Animation.Manager, initialValue: 0.0 );
                        status.光輪の不透明度 = new Variable( Global.Animation.Manager, initialValue: 1.0 );
                        status.ストーリーボード = new Storyboard( Global.Animation.Manager );

                        double 期間sec;

                        #region " (1) 放射光 アニメーションの構築 "
                        //----------------
                        {
                            // シーン1. 回転しつつ拡大縮小
                            期間sec = 0.1;
                            using( var 回転角の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 期間sec, finalValue: 45.0 ) )
                            using( var 拡大率の遷移1 = Global.Animation.TrasitionLibrary.Linear( duration: 期間sec / 2.0, finalValue: 1.5 ) )
                            using( var 拡大率の遷移2 = Global.Animation.TrasitionLibrary.Linear( duration: 期間sec / 2.0, finalValue: 0.7 ) )
                            {
                                status.ストーリーボード.AddTransition( status.放射光の回転角, 回転角の遷移 );
                                status.ストーリーボード.AddTransition( status.放射光の拡大率, 拡大率の遷移1 );
                                status.ストーリーボード.AddTransition( status.放射光の拡大率, 拡大率の遷移2 );
                            }

                            // シーン2. 縮小して消滅
                            期間sec = 0.1;
                            using( var 拡大率の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 期間sec / 2.0, finalValue: 0.0 ) )
                            {
                                status.ストーリーボード.AddTransition( status.放射光の拡大率, 拡大率の遷移 );
                            }
                        }
                        //----------------
                        #endregion

                        #region " (2) 光輪 アニメーションの構築 "
                        //----------------
                        {
                            // シーン1. ある程度まで拡大
                            期間sec = 0.05;
                            using( var 光輪の拡大率の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 期間sec, finalValue: 0.6 ) )
                            using( var 光輪の不透明度の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 期間sec ) )
                            {
                                status.ストーリーボード.AddTransition( status.光輪の拡大率, 光輪の拡大率の遷移 );
                                status.ストーリーボード.AddTransition( status.光輪の不透明度, 光輪の不透明度の遷移 );
                            }

                            // シーン2. ゆっくり拡大しつつ消える
                            期間sec = 0.15;
                            using( var 光輪の拡大率の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 期間sec, finalValue: 0.8 ) )
                            using( var 光輪の不透明度の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 期間sec, finalValue: 0.0 ) )
                            {
                                status.ストーリーボード.AddTransition( status.光輪の拡大率, 光輪の拡大率の遷移 );
                                status.ストーリーボード.AddTransition( status.光輪の不透明度, 光輪の不透明度の遷移 );
                            }
                        }
                        //----------------
                        #endregion

                        // アニメーション 開始。
                        status.ストーリーボード.Schedule( Global.Animation.Timer.Time );
                        status.現在の状態 = 表示レーンステータス.状態.表示中;
                        //----------------
                        #endregion

                        break;
                    }
                    case 表示レーンステータス.状態.表示中:
                    {
                        #region " (1) 放射光 の進行描画。"
                        //----------------
                        {
                            var 転送元矩形dpx = this._放射光の矩形リスト[ レーン.Value.ToString() ]!;

                            var w = 転送元矩形dpx!.Value.Width;
                            var h = 転送元矩形dpx!.Value.Height;

                            var sx = (float) status.放射光の拡大率.Value;
                            var sy = (float) status.放射光の拡大率.Value;

                            var 変換行列2D =
                                Matrix3x2.Rotation(
                                    angle: MathUtil.DegreesToRadians( (float) status.放射光の回転角.Value ),
                                    center: new Vector2( w / 2f, h / 2f ) ) *
                                Matrix3x2.Scaling( sx, sy ) *
                                Matrix3x2.Translation(
                                    status.表示中央位置dpx.X - w * sx / 2f,
                                    status.表示中央位置dpx.Y - h * sy / 2f );

                            const float 不透明度 = 0.5f;    // 眩しいので減光

                            this._放射光.描画する( dc, 変換行列2D, 転送元矩形: 転送元矩形dpx, 不透明度0to1: 不透明度 );
                        }
                        //----------------
                        #endregion

                        #region " (2) 光輪 の進行描画。"
                        //----------------
                        {
                            var 転送元矩形dpx = this._放射光の矩形リスト[ レーン.Value.ToString() ]!;

                            var w = 転送元矩形dpx!.Value.Width;
                            var h = 転送元矩形dpx!.Value.Height;

                            var sx = (float) status.光輪の拡大率.Value;
                            var sy = (float) status.光輪の拡大率.Value;

                            var 変換行列2D =
                                Matrix3x2.Scaling( sx, sy ) *
                                Matrix3x2.Translation(
                                    status.表示中央位置dpx.X - w * sx / 2f,
                                    status.表示中央位置dpx.Y - h * sy / 2f );

                        this._光輪.描画する( dc, 変換行列2D, 転送元矩形: 転送元矩形dpx, 不透明度0to1: (float) status.光輪の不透明度.Value );
                        }
                        //----------------
                        #endregion

                        #region " 全部終わったら非表示へ。"
                        //----------------
                        if( status.ストーリーボード is null || status.ストーリーボード.Status == StoryboardStatus.Ready )
                            status.現在の状態 = 表示レーンステータス.状態.非表示;
                        //----------------
                        #endregion

                        break;
                    }
                }
            }
        }



        // ローカル


        private readonly 画像D2D _放射光;

        private readonly 画像D2D _光輪;

        private readonly 矩形リスト _放射光の矩形リスト;

        /// <summary>
        ///		以下の画像のアニメ＆表示管理を行うクラス。
        ///		・放射光
        ///		・フレア（輪）
        /// </summary>
        private class 表示レーンステータス : IDisposable
        {
            public enum 状態
            {
                非表示,
                表示開始,   // 高速進行スレッドが設定
                表示中,     // 描画スレッドが設定
            }
            public 状態 現在の状態 = 状態.非表示;

            public readonly Vector2 表示中央位置dpx;
            public Variable 放射光の回転角 = null!;
            public Variable 放射光の拡大率 = null!;
            public Variable 光輪の拡大率 = null!;
            public Variable 光輪の不透明度 = null!;
            public Storyboard ストーリーボード = null!;

            public 表示レーンステータス( 表示レーン種別 lane )
            {
                this.現在の状態 = 状態.非表示;

                // 表示中央位置は、レーンごとに固定。
                this.表示中央位置dpx = new Vector2( レーンフレーム.レーン中央位置X[ lane ], 演奏ステージ.ヒット判定位置Ydpx );
            }
            public void Dispose()
            {
                this.アニメ用メンバを解放する();
                this.現在の状態 = 状態.非表示;
            }
            public void アニメ用メンバを解放する()
            {
                this.ストーリーボード?.Dispose();
                this.放射光の回転角?.Dispose();
                this.放射光の拡大率?.Dispose();
                this.光輪の拡大率?.Dispose();
                this.光輪の不透明度?.Dispose();
            }
        }

        private readonly Dictionary<表示レーン種別, 表示レーンステータス> _レーンtoステータス;
    }
}
