using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania2
{
    /// <summary>
    ///		枠を形成する青い「線」を一本描画する。
    /// </summary>
    class 青い線 : IDisposable
    {

        // プロパティ


        public float 太さdpx { get; protected set; } = 26f;



        // 生成と終了


        public 青い線()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            int 青色 = 0x61200a;
            int 水色 = 0xf95925;
            int 白色 = 0xffffff;

            this._線グラ頂点集合 = new GradientStopCollection(
                Global.既定のD2D1DeviceContext,
                new GradientStop[] {
                    new GradientStop() { Position = 0.00f, Color = new Color4( new Color3( 青色 ), 0f ) },		// 完全透明
					new GradientStop() { Position = 0.35f, Color = new Color4( new Color3( 水色 ), 1f ) },
                    new GradientStop() { Position = 0.42f, Color = new Color4( new Color3( 青色 ), 0.7f ) },

                    new GradientStop() { Position = 0.48f, Color = new Color4( new Color3( 白色 ), 1f ) },
                    new GradientStop() { Position = 0.52f, Color = new Color4( new Color3( 白色 ), 1f ) },

                    new GradientStop() { Position = 0.58f, Color = new Color4( new Color3( 青色 ), 0.7f ) },
                    new GradientStop() { Position = 0.65f, Color = new Color4( new Color3( 水色 ), 1f ) },
                    new GradientStop() { Position = 1.00f, Color = new Color4( new Color3( 青色 ), 0f ) },        // 完全透明
                } );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._線グラブラシ?.Dispose();
            this._線グラ頂点集合.Dispose();
        }



        // 進行と描画


        /// <summary>
        /// 	よこ線（左→右）か、たて線（上→下）のいずれかを描画できる。
        /// 	よこ線を描画したい場合は<paramref name="幅dpx"/>を指定し、
        /// 	たて線を描画したい場合は<paramref name="高さdpx"/>を指定する。
        /// 	<paramref name="幅dpx"/> と <paramref name="高さdpx"/> を同時に指定することはできない。
        /// </summary>
        /// <param name="幅dpx">横方向（左→右）の長さ。<paramref name="高さdpx"/>と同時に指定してはならない。</param>
        /// <param name="高さdpx">縦方向（上→下）の長さ。<paramref name="幅dpx"/>と同時に指定してはならない。</param>
        public void 描画する( DeviceContext dc, Vector2 開始位置dpx, float 幅dpx = -1f, float 高さdpx = -1f )
        {
            var check = ( 幅dpx * 高さdpx );
            Debug.Assert( 0f >= check, "幅か高さが両方指定されていないか、両方指定されています。どちらか一方だけを指定してください。" );
            if( 0f == check )
                return; // 面積ゼロ

            D2DBatch.Draw( dc, () => {

                dc.PrimitiveBlend = PrimitiveBlend.Add; // 加算合成

                if( 0f < 幅dpx )
                {
                    // (A) 横方向（左→右）の枠
                    var 矩形 = new RectangleF(
                        開始位置dpx.X,
                        開始位置dpx.Y - this.太さdpx / 2f,
                        幅dpx,
                        this.太さdpx );

                    this._線グラブラシ?.Dispose();
                    this._線グラブラシ = new LinearGradientBrush(
                        dc,
                        new LinearGradientBrushProperties() {
                            StartPoint = new Vector2( 矩形.Left, 矩形.Top ),
                            EndPoint = new Vector2( 矩形.Left, 矩形.Bottom ),
                        },
                        this._線グラ頂点集合 );

                    dc.FillRectangle( 矩形, this._線グラブラシ );
                }
                else
                {
                    // (B) 縦方向（上→下）の枠
                    var 矩形 = new RectangleF(
                        開始位置dpx.X - this.太さdpx / 2f,
                        開始位置dpx.Y,
                        this.太さdpx,
                        高さdpx );

                    this._線グラブラシ?.Dispose();
                    this._線グラブラシ = new LinearGradientBrush(
                        dc,
                        new LinearGradientBrushProperties() {
                            StartPoint = new Vector2( 矩形.Left, 矩形.Top ),
                            EndPoint = new Vector2( 矩形.Right, 矩形.Top ),
                        },
                        this._線グラ頂点集合 );

                    dc.FillRectangle( 矩形, this._線グラブラシ );
                }

            } );
        }



        // ローカル


        private LinearGradientBrush? _線グラブラシ = null;

        private readonly GradientStopCollection _線グラ頂点集合;
    }
}
