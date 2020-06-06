using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.MediaFoundation;

namespace FDK
{
    public class Video : IDisposable
    {

        // プロパティ


        public bool 加算合成 { get; set; } = false;

        public bool 再生中 { get; protected set; } = false;

        public double 再生速度 { get; protected set; } = 1.0;



        // 生成と終了


        protected Video()
        {
            this._再生タイマ = new QPCTimer();
        }

        public Video( DXGIDeviceManager deviceManager, DeviceContext d2dDeviceContext, VariablePath ファイルパス, double 再生速度 = 1.0 )
            : this()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this.再生速度 = 再生速度;
            try
            {
                this._VideoSource = new MediaFoundationFileVideoSource( deviceManager, d2dDeviceContext, ファイルパス, 再生速度 );
            }
            catch
            {
                Log.WARNING( $"動画のデコードに失敗しました。[{ファイルパス.変数付きパス}" );
                this._VideoSource = null;
                return;
            }

            this._ファイルから生成した = true;
        }

        public Video( IVideoSource videoSource, double 再生速度 = 1.0 )
            : this()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this.再生速度 = 再生速度;

            this._VideoSource = videoSource;
            this._ファイルから生成した = false;
        }

        public void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this.再生を終了する();

            if( this._ファイルから生成した )
                this._VideoSource?.Dispose();

            this._VideoSource = null;
        }



        // 再生、停止、再開


        public void 再生を開始する( double 再生開始時刻sec = 0.0 )
        {
            this._VideoSource?.Start( 再生開始時刻sec );

            this._再生タイマ.リセットする( QPCTimer.秒をカウントに変換して返す( 再生開始時刻sec ) );

            this.再生中 = true;
        }

        public void 再生を終了する()
        {
            this._VideoSource?.Stop();

            this._最後に描画したフレーム?.Dispose();
            this._最後に描画したフレーム = null;

            this.再生中 = false;
        }

        public void 一時停止する()
        {
            this._再生タイマ?.一時停止する();
            this._VideoSource?.Pause();
        }

        public void 再開する()
        {
            this._再生タイマ?.再開する();
            this._VideoSource?.Resume();
        }



        // 進行と描画


        public void 描画する( DeviceContext dc, RectangleF 描画先矩形, float 不透明度0to1 = 1.0f )
        {
            if( this._VideoSource is null )
                return;

            var 変換行列2D =
                Matrix3x2.Scaling( 描画先矩形.Width / this._VideoSource.フレームサイズ.Width, 描画先矩形.Height / this._VideoSource.フレームサイズ.Height ) *  // 拡大縮小
                Matrix3x2.Translation( 描画先矩形.Left, 描画先矩形.Top );  // 平行移動

            this.描画する( dc, 変換行列2D, 不透明度0to1 );
        }

        public void 描画する( DeviceContext dc, Matrix3x2 変換行列2D, float 不透明度0to1 = 1.0f )
        {
            if( this._VideoSource is null )
                return;

            long 次のフレームの表示予定時刻100ns = this._VideoSource.Peek(); // 次のフレームがなければ負数

            if( 0 <= 次のフレームの表示予定時刻100ns )
            {
                if( ( null != this._最後に描画したフレーム ) && ( 次のフレームの表示予定時刻100ns < this._最後に描画したフレーム.表示時刻100ns ) )
                {
                    // (A) 次のフレームが前のフレームより過去 → ループしたので、タイマをリセットしてから描画する。
                    this._再生タイマ.リセットする( QPCTimer.秒をカウントに変換して返す( (long)( 次のフレームの表示予定時刻100ns / 10_000_000.0 ) ) );
                    this._次のフレームを読み込んで描画する( dc, 変換行列2D, 不透明度0to1 );
                }
                else if( 次のフレームの表示予定時刻100ns <= this._再生タイマ.現在のリアルタイムカウント100ns )
                {
                    // (B) 次のフレームの表示時刻に達したので描画する。
                    this._次のフレームを読み込んで描画する( dc, 変換行列2D, 不透明度0to1 );
                }
                else
                {
                    // (C) 次のフレームの表示時刻にはまだ達していない → 最後に描画したフレームを再描画しておく
                    this.最後のフレームを再描画する( dc, 変換行列2D, 不透明度0to1 );
                }
            }
            else
            {
                // (D) デコードが追い付いてない、またはループせず再生が終わっている　→ 何も表示しない。デコードが追い付いてないなら点滅するだろう。
                this._最後に描画したフレーム?.Dispose();
                this._最後に描画したフレーム = null;
            }
        }

        public void 最後のフレームを再描画する( DeviceContext dc, RectangleF 描画先矩形, float 不透明度0to1 = 1.0f )
        {
            if( this._VideoSource is null )
                return;

            var 変換行列2D =
                Matrix3x2.Scaling( 描画先矩形.Width / this._VideoSource.フレームサイズ.Width, 描画先矩形.Height / this._VideoSource.フレームサイズ.Height ) *  // 拡大縮小
                Matrix3x2.Translation( 描画先矩形.Left, 描画先矩形.Top );  // 平行移動

            this.最後のフレームを再描画する( dc, 変換行列2D, 不透明度0to1 );
        }

        public void 最後のフレームを再描画する( DeviceContext dc, Matrix3x2 変換行列2D, float 不透明度0to1 = 1.0f )
        {
            if( this._最後に描画したフレーム is null )
                return;

            this._フレームを描画する( dc, 変換行列2D, 不透明度0to1, this._最後に描画したフレーム );
        }



        // ローカル


        private IVideoSource? _VideoSource = null;

        /// <summary>
        ///     <see cref="_VideoSource"/> をファイルから生成した場合は true、
        ///     参照を受け取った場合は false。
        /// </summary>
        private readonly bool _ファイルから生成した = false;

        private VideoFrame? _最後に描画したフレーム = null;

        private readonly QPCTimer _再生タイマ;


        private void _次のフレームを読み込んで描画する( DeviceContext dc, Matrix3x2 変換行列2D, float 不透明度0to1 = 1.0f )
        {
            if( this._VideoSource is null )
                return;

            VideoFrame? 次のフレーム;

            // フレームドロップ判定
            while( true )
            {
                // 次のフレームを取得：
                // デコードが間に合ってない場合にはブロックする。
                // ブロックされたくない場合は、事前に Peek() でチェックしておくこと。
                次のフレーム = this._VideoSource.Read();

                // フレームドロップ判定；
                // 次の次のフレームがすでに表示時刻を過ぎてるなら、次のフレームは破棄してループする。

                var 次の次のフレームの表示予定時刻100ns = this._VideoSource.Peek();

                if( 0 > 次の次のフレームの表示予定時刻100ns )
                {
                    // (A) 次の次のフレームがまだない場合　→　ドロップ判定ループを抜けて描画へ。
                    break;
                }
                else
                {
                    if( 次の次のフレームの表示予定時刻100ns <= this._再生タイマ.現在のキャプチャカウント100ns )
                    {
                        // (B) 次の次のフレームが存在し、かつ表示予定時刻を過ぎてる場合　→　次のフレームは破棄してさらに次へ。
                        次のフレーム?.Dispose();
                        continue;
                    }
                    else
                    {
                        // (C) 次の次のフレームが存在し、かつ表示予定時刻をまだすぎてない場合　→　ドロップ判定ループを抜けて描画へ。
                        break;
                    }
                }
            }

            // 描画。
            this._フレームを描画する( dc, 変換行列2D, 不透明度0to1, 次のフレーム! );

            // 更新。
            this._最後に描画したフレーム?.Dispose();
            this._最後に描画したフレーム = 次のフレーム;
        }

        private void _フレームを描画する( DeviceContext dc, Matrix3x2 変換行列2D, float 不透明度0to1, VideoFrame 描画するフレーム )
        {
            if( 描画するフレーム is null )
                return;

            D2DBatch.Draw( dc, () => {
                dc.Transform = ( 変換行列2D ) * dc.Transform;
                dc.PrimitiveBlend = ( this.加算合成 ) ? PrimitiveBlend.Add : PrimitiveBlend.SourceOver;
                dc.DrawBitmap( 描画するフレーム.Bitmap, 不透明度0to1, InterpolationMode.NearestNeighbor );
            } );
        }
    }
}
