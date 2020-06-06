using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania2.演奏
{
    /// <summary>
    ///		チップの背景であり、レーン全体を示すフレーム画像。
    /// </summary>
    class レーンフレーム : IDisposable
    {

        // static


        /// <summary>
        ///		画面全体に対する、レーンフレームの表示位置と範囲。
        /// </summary>
        public static RectangleF 領域 => new RectangleF( 445f, 0f, 778f, 1080f );

        public static Dictionary<表示レーン種別, float> レーン中央位置X = null!;

        public static Dictionary<表示レーン種別, Color4> レーン色 = null!;



        // 生成と終了


        static レーンフレーム()
        {
            レーン中央位置X = new Dictionary<表示レーン種別, float>() {
                { 表示レーン種別.Unknown, 0f },
                { 表示レーン種別.LeftCymbal, 489f },
                { 表示レーン種別.HiHat, 570f },
                { 表示レーン種別.Foot, 570f },
                { 表示レーン種別.Snare, 699f },
                { 表示レーン種別.Tom1, 812f },
                { 表示レーン種別.Bass, 896f },
                { 表示レーン種別.Tom2, 976f },
                { 表示レーン種別.Tom3, 1088f },
                { 表示レーン種別.RightCymbal, 1193f },
            };

            レーン色 = new Dictionary<表示レーン種別, Color4>() {
                { 表示レーン種別.Unknown, new Color4( 0x00000000 ) },  // ABGR
                { 表示レーン種別.LeftCymbal, new Color4(0xff5a5a5a) },
                { 表示レーン種別.HiHat, new Color4( 0xff7d5235 ) },
                { 表示レーン種別.Foot, new Color4( 0xff492d1f ) },
                { 表示レーン種別.Snare, new Color4( 0xff406283 ) },
                { 表示レーン種別.Tom1, new Color4( 0xff2e5730 ) },
                { 表示レーン種別.Bass, new Color4( 0xff424141 ) },
                { 表示レーン種別.Tom2, new Color4( 0xff323267 ) },
                { 表示レーン種別.Tom3, new Color4( 0xff70565c ) },
                { 表示レーン種別.RightCymbal, new Color4( 0xff606060 ) },
            };
        }

        public レーンフレーム()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );
        }



        // 進行と描画


        public void 進行描画する( DeviceContext dc, int BGAの透明度, bool レーンラインを描画する = true )
        {
            D2DBatch.Draw( dc, () => {

                // レーンエリアを描画する。
                {
                    var color = Color4.Black;
                    color.Alpha *= ( 100 - BGAの透明度 ) / 100.0f;   // BGAの透明度0→100 のとき Alpha×1→×0
                    using( var laneBrush = new SolidColorBrush( dc, color ) )
                    {
                        dc.FillRectangle( レーンフレーム.領域, laneBrush );
                    }
                }

                // レーンラインを描画する。

                if( レーンラインを描画する )
                {
                    foreach( 表示レーン種別? displayLaneType in Enum.GetValues( typeof( 表示レーン種別 ) ) )
                    {
                        if( !displayLaneType.HasValue || displayLaneType.Value == 表示レーン種別.Unknown )
                            continue;

                        var レーンライン色 = レーン色[ displayLaneType.Value ];
                        レーンライン色.Alpha *= ( 100 - BGAの透明度 ) / 100.0f;   // BGAの透明度0→100 のとき Alpha×1→×0

                        using( var laneLineBrush = new SolidColorBrush( dc, レーンライン色 ) )
                        {
                            var rc = new RectangleF( レーン中央位置X[ displayLaneType.Value ] - 1, 0f, 3f, 領域.Height );
                            dc.FillRectangle( rc, laneLineBrush );
                        }
                    }
                }

            } );
        }
    }
}
