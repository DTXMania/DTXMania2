using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Animation;
using FDK;

namespace DTXMania2.演奏
{
    class 判定文字列 : IDisposable
    {

        // 生成と終了


        public 判定文字列()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._判定文字列画像 = new 画像( @"$(Images)\PlayStage\JudgeLabel.png" );
            this._判定文字列の矩形リスト = new 矩形リスト( @"$(Images)\PlayStage\JudgeLabel.yaml" );

            this._FastSlow数字画像 = new 画像( @"$(Images)\PlayStage\FastSlowNumber.png" );
            this._FastSlow数字の矩形リスト = new 矩形リスト( @"$(Images)\PlayStage\FastSlowNumber.yaml" );

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

            this._判定文字列画像.Dispose();
            this._FastSlow数字画像.Dispose();
        }



        // 表示開始


        public void 表示を開始する( 表示レーン種別 lane, 判定種別 judge, double fastSlowSec )
        {
            var status = this._レーンtoステータス[ lane ];

            status.判定種別 = judge;
            status.FastSlow値sec = fastSlowSec;
            status.現在の状態 = 表示レーンステータス.状態.表示開始;
        }



        // 進行と描画


        public void 進行描画する()
        {
            foreach( 表示レーン種別? レーン in Enum.GetValues( typeof( 表示レーン種別 ) ) )
            {
                if( !レーン.HasValue )
                    continue;

                var status = this._レーンtoステータス[ レーン.Value ];

                switch( status.現在の状態 )
                {
                    case 表示レーンステータス.状態.表示開始:
                        #region " 開始処理 "
                        //----------------
                        {
                            status.アニメ用メンバを解放する();

                            #region " (1) 光 アニメーションを構築 "
                            //----------------
                            if( status.判定種別 == 判定種別.PERFECT )   // 今のところ、光はPERFECT時のみ表示。
                            {
                                // 初期状態
                                status.光の回転角 = new Variable( Global.Animation.Manager, initialValue: 0 );
                                status.光のX方向拡大率 = new Variable( Global.Animation.Manager, initialValue: 1.2 );
                                status.光のY方向拡大率 = new Variable( Global.Animation.Manager, initialValue: 0.25 );
                                status.光のストーリーボード = new Storyboard( Global.Animation.Manager );

                                double 期間sec;

                                // シーン1. 小さい状態からすばやく展開
                                期間sec = 0.03;
                                using( var 回転角の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 期間sec, finalValue: -100.0 ) )       // [degree]
                                using( var X方向拡大率の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 期間sec, finalValue: 1.0 ) )
                                using( var Y方向拡大率の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 期間sec, finalValue: 1.0 ) )
                                {
                                    status.光のストーリーボード.AddTransition( status.光の回転角, 回転角の遷移 );
                                    status.光のストーリーボード.AddTransition( status.光のX方向拡大率, X方向拡大率の遷移 );
                                    status.光のストーリーボード.AddTransition( status.光のY方向拡大率, Y方向拡大率の遷移 );
                                }

                                // シーン2. 大きい状態でゆっくり消える
                                期間sec = 0.29;
                                using( var 回転角の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 期間sec, finalValue: -140.0 ) )       // [degree]
                                using( var X方向拡大率の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 期間sec, finalValue: 0.0 ) )
                                using( var Y方向拡大率の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 期間sec ) )
                                {
                                    status.光のストーリーボード.AddTransition( status.光の回転角, 回転角の遷移 );
                                    status.光のストーリーボード.AddTransition( status.光のX方向拡大率, X方向拡大率の遷移 );
                                    status.光のストーリーボード.AddTransition( status.光のY方向拡大率, Y方向拡大率の遷移 );
                                }

                                // 開始
                                status.光のストーリーボード.Schedule( Global.Animation.Timer.Time );
                            }
                            else
                            {
                                status.光のストーリーボード = null;
                            }
                            //----------------
                            #endregion

                            #region " (2) 判定文字（影）アニメーションを構築 "
                            //----------------
                            {
                                // 初期状態
                                status.文字列影の相対Y位置dpx = new Variable( Global.Animation.Manager, initialValue: +40.0 );
                                status.文字列影の不透明度 = new Variable( Global.Animation.Manager, initialValue: 0.0 );
                                status.文字列影のストーリーボード = new Storyboard( Global.Animation.Manager );

                                double 期間sec;

                                // シーン1. 完全透明のまま下から上に移動。
                                期間sec = 0.05;
                                using( var 相対Y位置の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 期間sec, finalValue: -5.0 ) )
                                using( var 透明度の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 期間sec ) )
                                {
                                    status.文字列影のストーリーボード.AddTransition( status.文字列影の相対Y位置dpx, 相対Y位置の遷移 );
                                    status.文字列影のストーリーボード.AddTransition( status.文字列影の不透明度, 透明度の遷移 );
                                }

                                // シーン2. 透明になりつつ上に消える
                                期間sec = 0.15;
                                using( var 相対Y位置の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 期間sec, finalValue: -10.0 ) )
                                using( var 透明度の遷移1 = Global.Animation.TrasitionLibrary.Linear( duration: 0.0, finalValue: 0.5 ) )
                                using( var 透明度の遷移2 = Global.Animation.TrasitionLibrary.Linear( duration: 期間sec, finalValue: 0.0 ) )
                                {
                                    status.文字列影のストーリーボード.AddTransition( status.文字列影の相対Y位置dpx, 相対Y位置の遷移 );
                                    status.文字列影のストーリーボード.AddTransition( status.文字列影の不透明度, 透明度の遷移1 );
                                    status.文字列影のストーリーボード.AddTransition( status.文字列影の不透明度, 透明度の遷移2 );
                                }

                                // 開始
                                status.文字列影のストーリーボード.Schedule( Global.Animation.Timer.Time );
                            }
                            //----------------
                            #endregion

                            #region " (3) 判定文字（本体）アニメーションを構築 "
                            //----------------
                            {
                                // 初期状態
                                status.文字列本体の相対Y位置dpx = new Variable( Global.Animation.Manager, initialValue: +40.0 );
                                status.文字列本体のX方向拡大率 = new Variable( Global.Animation.Manager, initialValue: 1.0 );
                                status.文字列本体のY方向拡大率 = new Variable( Global.Animation.Manager, initialValue: 1.0 );
                                status.文字列本体の不透明度 = new Variable( Global.Animation.Manager, initialValue: 0.0 );
                                status.文字列本体のストーリーボード = new Storyboard( Global.Animation.Manager );

                                double 期間sec;

                                // シーン1. 透明から不透明になりつつ下から上に移動。
                                期間sec = 0.05;
                                using( var 相対Y位置の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 期間sec, finalValue: -5.0 ) )
                                using( var X方向拡大率の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 期間sec ) )
                                using( var Y方向拡大率の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 期間sec ) )
                                using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.AccelerateDecelerate( duration: 期間sec, finalValue: 1.0, accelerationRatio: 0.1, decelerationRatio: 0.9 ) )
                                {
                                    status.文字列本体のストーリーボード.AddTransition( status.文字列本体の相対Y位置dpx, 相対Y位置の遷移 );
                                    status.文字列本体のストーリーボード.AddTransition( status.文字列本体のX方向拡大率, X方向拡大率の遷移 );
                                    status.文字列本体のストーリーボード.AddTransition( status.文字列本体のY方向拡大率, Y方向拡大率の遷移 );
                                    status.文字列本体のストーリーボード.AddTransition( status.文字列本体の不透明度, 不透明度の遷移 );
                                }

                                // シーン2. ちょっと下に跳ね返る
                                期間sec = 0.05;
                                using( var 相対Y位置の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 期間sec, finalValue: +5.0 ) )
                                using( var X方向拡大率の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 期間sec ) )
                                using( var Y方向拡大率の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 期間sec ) )
                                using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 期間sec ) )
                                {
                                    status.文字列本体のストーリーボード.AddTransition( status.文字列本体の相対Y位置dpx, 相対Y位置の遷移 );
                                    status.文字列本体のストーリーボード.AddTransition( status.文字列本体のX方向拡大率, X方向拡大率の遷移 );
                                    status.文字列本体のストーリーボード.AddTransition( status.文字列本体のY方向拡大率, Y方向拡大率の遷移 );
                                    status.文字列本体のストーリーボード.AddTransition( status.文字列本体の不透明度, 不透明度の遷移 );
                                }

                                // シーン3. また上に戻る
                                期間sec = 0.05;
                                using( var 相対Y位置の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 期間sec, finalValue: +0.0 ) )
                                using( var X方向拡大率の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 期間sec ) )
                                using( var Y方向拡大率の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 期間sec ) )
                                using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 期間sec ) )
                                {
                                    status.文字列本体のストーリーボード.AddTransition( status.文字列本体の相対Y位置dpx, 相対Y位置の遷移 );
                                    status.文字列本体のストーリーボード.AddTransition( status.文字列本体のX方向拡大率, X方向拡大率の遷移 );
                                    status.文字列本体のストーリーボード.AddTransition( status.文字列本体のY方向拡大率, Y方向拡大率の遷移 );
                                    status.文字列本体のストーリーボード.AddTransition( status.文字列本体の不透明度, 不透明度の遷移 );
                                }

                                // シーン4. 静止
                                期間sec = 0.15;
                                using( var 相対Y位置の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 期間sec ) )
                                using( var X方向拡大率の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 期間sec ) )
                                using( var Y方向拡大率の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 期間sec ) )
                                using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 期間sec ) )
                                {
                                    status.文字列本体のストーリーボード.AddTransition( status.文字列本体の相対Y位置dpx, 相対Y位置の遷移 );
                                    status.文字列本体のストーリーボード.AddTransition( status.文字列本体のX方向拡大率, X方向拡大率の遷移 );
                                    status.文字列本体のストーリーボード.AddTransition( status.文字列本体のY方向拡大率, Y方向拡大率の遷移 );
                                    status.文字列本体のストーリーボード.AddTransition( status.文字列本体の不透明度, 不透明度の遷移 );
                                }

                                // シーン5. 横に広がり縦につぶれつつ消える
                                期間sec = 0.05;
                                using( var 相対Y位置の遷移 = Global.Animation.TrasitionLibrary.Constant( duration: 期間sec ) )
                                using( var X方向拡大率の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 期間sec, finalValue: 2.0 ) )
                                using( var Y方向拡大率の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 期間sec, finalValue: 0.0 ) )
                                using( var 不透明度の遷移 = Global.Animation.TrasitionLibrary.Linear( duration: 期間sec, finalValue: 0.0 ) )
                                {
                                    status.文字列本体のストーリーボード.AddTransition( status.文字列本体の相対Y位置dpx, 相対Y位置の遷移 );
                                    status.文字列本体のストーリーボード.AddTransition( status.文字列本体のX方向拡大率, X方向拡大率の遷移 );
                                    status.文字列本体のストーリーボード.AddTransition( status.文字列本体のY方向拡大率, Y方向拡大率の遷移 );
                                    status.文字列本体のストーリーボード.AddTransition( status.文字列本体の不透明度, 不透明度の遷移 );
                                }

                                // 開始
                                status.文字列本体のストーリーボード.Schedule( Global.Animation.Timer.Time );
                            }
                            //----------------
                            #endregion

                            status.現在の状態 = 表示レーンステータス.状態.表示中;
                        }
                        //----------------
                        #endregion
                        break;

                    case 表示レーンステータス.状態.表示中:
                        #region " 開始完了、表示中 "
                        //----------------
                        {
                            #region " (1) 光 の進行描画 "
                            //----------------
                            if( null != status.光のストーリーボード )
                            {
                                var 転送元矩形 = this._判定文字列の矩形リスト[ "PERFECT光" ];

                                var sx = (float) status.光のX方向拡大率.Value;
                                var sy = (float) status.光のY方向拡大率.Value;

                                var 変換行列 =
                                    Matrix.Scaling( sx, sy, 0f ) *
                                    Matrix.RotationZ(
                                        MathUtil.DegreesToRadians( (float) status.光の回転角.Value ) ) *
                                    Matrix.Translation(
                                        Global.画面左上dpx.X + ( status.表示中央位置dpx.X ),
                                        Global.画面左上dpx.Y - ( status.表示中央位置dpx.Y ),
                                        0f );

                                this._判定文字列画像.進行描画する( 変換行列, 転送元矩形: 転送元矩形 );
                            }
                            //----------------
                            #endregion

                            #region " (2) 判定文字列（影）の進行描画"
                            //----------------
                            if( null != status.文字列影のストーリーボード )
                            {
                                var 転送元矩形 = this._判定文字列の矩形リスト[ status.判定種別.ToString() ]!;

                                this._判定文字列画像.進行描画する(
                                    status.表示中央位置dpx.X - 転送元矩形.Value.Width / 2f,
                                    status.表示中央位置dpx.Y - 転送元矩形.Value.Height / 2f + (float) status.文字列影の相対Y位置dpx.Value,
                                    (float) status.文字列影の不透明度.Value,
                                    転送元矩形: 転送元矩形 );
                            }
                            //----------------
                            #endregion

                            #region " (3) 判定文字列（本体）の進行描画 "
                            //----------------
                            if( null != status.文字列本体のストーリーボード )
                            {
                                var 転送元矩形 = this._判定文字列の矩形リスト[ status.判定種別.ToString() ]!;

                                var sx = (float) status.文字列本体のX方向拡大率.Value;
                                var sy = (float) status.文字列本体のY方向拡大率.Value;

                                this._判定文字列画像.進行描画する(
                                    status.表示中央位置dpx.X - sx * 転送元矩形.Value.Width / 2f,
                                    status.表示中央位置dpx.Y - sy * 転送元矩形.Value.Height / 2f + (float) status.文字列本体の相対Y位置dpx.Value,
                                    X方向拡大率: sx,
                                    Y方向拡大率: sy,
                                    不透明度0to1: (float) status.文字列本体の不透明度.Value,
                                    転送元矩形: 転送元矩形 );
                            }
                            //----------------
                            #endregion

                            #region " (4) FAST/SLOW値の進行描画 "
                            //----------------
                            if( Global.App.ログオン中のユーザ.演奏中に判定FastSlowを表示する )
                            {
                                // FAST/SLOW値：
                                // ・チップより入力が早いと青文字、チップより入力が遅いと赤文字、
                                // 　チップと入力の時間差が 10ms 未満なら黄文字で表示する。
                                // ・MISS 時は表示しない。
                                // ・ミリ秒単位で表示する。

                                if( status.判定種別 != 判定種別.MISS )
                                {
                                    int FastSlow値 = (int) ( status.FastSlow値sec * 1000.0 ); // ミリ秒、小数切り捨て
                                    int FastSlow値の絶対値 = Math.Abs( FastSlow値 );
                                    var 接尾詞 =
                                        ( FastSlow値の絶対値 < 10 ) ? "_yellow" :
                                        ( 0 < FastSlow値 ) ? "_blue" : "_red";
                                    var 文字列 = FastSlow値.ToString( "D" );
                                    int 文字数 = 文字列.Length;
                                    float 拡大率 = 2f;

                                    var 拡大済み文字列サイズdpx = new Size2F( 0, 0 );
                                    for( int i = 0; i < 文字数; i++ )
                                    {
                                        var 文字矩形 = this._FastSlow数字の矩形リスト[ 文字列[ i ] + 接尾詞 ]!;

                                        拡大済み文字列サイズdpx.Width += 文字矩形.Value.Width * 拡大率;
                                        拡大済み文字列サイズdpx.Height = Math.Max( 拡大済み文字列サイズdpx.Height, 文字矩形.Value.Height * 拡大率 );
                                    }

                                    float x = status.表示中央位置dpx.X - 拡大済み文字列サイズdpx.Width / 2f;
                                    float y = status.表示中央位置dpx.Y - 拡大済み文字列サイズdpx.Height / 2f + 45f;
                                    for( int i = 0; i < 文字数; i++ )
                                    {
                                        var 文字矩形 = this._FastSlow数字の矩形リスト[ 文字列[ i ] + 接尾詞 ]!;
                                        this._FastSlow数字画像.進行描画する( x, y, X方向拡大率: 拡大率, Y方向拡大率: 拡大率, 転送元矩形: 文字矩形 );
                                        x += 文字矩形.Value.Width * 拡大率;
                                    }
                                }
                            }
                            //----------------
                            #endregion

                            // 全部終わったら非表示へ。
                            if( ( ( status.文字列影のストーリーボード is null ) || ( status.文字列影のストーリーボード.Status == StoryboardStatus.Ready ) ) &&
                                ( ( status.文字列本体のストーリーボード is null ) || ( status.文字列本体のストーリーボード.Status == StoryboardStatus.Ready ) ) &&
                                ( ( status.光のストーリーボード is null ) || ( status.光のストーリーボード.Status == StoryboardStatus.Ready ) ) )
                            {
                                status.現在の状態 = 表示レーンステータス.状態.非表示;
                            }
                        }
                        //----------------
                        #endregion
                        break;

                    default:
                        continue;   // 非表示
                }
            }
        }



        // ローカル


        private readonly 画像 _判定文字列画像;

        private readonly 矩形リスト _判定文字列の矩形リスト;

        private readonly 画像 _FastSlow数字画像;

        private readonly 矩形リスト _FastSlow数字の矩形リスト;

        /// <summary>
        ///		以下の画像のアニメ＆表示管理を行うクラス。
        ///		・判定文字列（本体）
        ///		・判定文字列（影）
        ///		・光（今のところPERFECTのみ）
        /// </summary>
        private class 表示レーンステータス : IDisposable
        {
            public enum 状態
            {
                非表示,
                表示開始,   // 高速進行スレッドが設定
                表示中,        // 描画スレッドが設定
            }
            public 状態 現在の状態 = 状態.非表示;

            public 判定種別 判定種別 = 判定種別.PERFECT;

            /// <summary>
            ///     チップと入力のズレ時間[秒]。
            ///     チップより入力が早ければ負数、遅ければ正数。
            /// </summary>
            public double FastSlow値sec = 0;

            public readonly Vector2 表示中央位置dpx;


            /// <summary>
            ///		判定文字列（本体）の表示されるY座標のオフセット。
            ///		表示中央位置dpx.Y からの相対値[dpx]。
            /// </summary>
            public Variable 文字列本体の相対Y位置dpx = null!;

            /// <summary>
            ///		判定文字列（本体）の不透明度。
            ///		0 で完全透明、1 で完全不透明。
            /// </summary>
            public Variable 文字列本体の不透明度 = null!;

            public Variable 文字列本体のX方向拡大率 = null!;

            public Variable 文字列本体のY方向拡大率 = null!;

            public Storyboard? 文字列本体のストーリーボード = null;


            /// <summary>
            ///		判定文字列（影）の表示されるY座標のオフセット。
            ///		表示中央位置dpx.Y からの相対値[dpx]。
            /// </summary>
            public Variable 文字列影の相対Y位置dpx = null!;

            /// <summary>
            ///		判定文字列（影）の不透明度。
            ///		0 で完全透明、1 で完全不透明。
            /// </summary>
            public Variable 文字列影の不透明度 = null!;

            public Storyboard? 文字列影のストーリーボード = null;


            /// <summary>
            ///		単位は度（degree）、時計回りを正とする。
            /// </summary>
            public Variable 光の回転角 = null!;

            public Variable 光のX方向拡大率 = null!;

            public Variable 光のY方向拡大率 = null!;

            public Storyboard? 光のストーリーボード = null;


            public 表示レーンステータス( 表示レーン種別 lane )
            {
                this.現在の状態 = 状態.非表示;

                float x = レーンフレーム.レーン中央位置X[ lane ];

                this.表示中央位置dpx = lane switch
                {
                    表示レーン種別.LeftCymbal => new Vector2( x, 530f ),
                    表示レーン種別.HiHat => new Vector2( x, 597f ),
                    表示レーン種別.Foot => new Vector2( x, 636f ),
                    表示レーン種別.Snare => new Vector2( x, 597f ),
                    表示レーン種別.Bass => new Vector2( x, 635f ),
                    表示レーン種別.Tom1 => new Vector2( x, 561f ),
                    表示レーン種別.Tom2 => new Vector2( x, 561f ),
                    表示レーン種別.Tom3 => new Vector2( x, 600f ),
                    表示レーン種別.RightCymbal => new Vector2( x, 533f ),
                    _ => new Vector2( x, -100f ),
                };
            }

            public void Dispose()
            {
                this.アニメ用メンバを解放する();
                this.現在の状態 = 状態.非表示;
            }

            public void アニメ用メンバを解放する()
            {
                this.文字列本体のストーリーボード?.Dispose();
                this.文字列本体の不透明度?.Dispose();
                this.文字列本体のY方向拡大率?.Dispose();
                this.文字列本体のX方向拡大率?.Dispose();
                this.文字列本体の相対Y位置dpx?.Dispose();

                this.文字列影のストーリーボード?.Dispose();
                this.文字列影の不透明度?.Dispose();
                this.文字列影の相対Y位置dpx?.Dispose();

                this.光のストーリーボード?.Dispose();
                this.光のY方向拡大率?.Dispose();
                this.光のX方向拡大率?.Dispose();
                this.光の回転角?.Dispose();
            }
        }

        private readonly Dictionary<表示レーン種別, 表示レーンステータス> _レーンtoステータス;
    }
}
