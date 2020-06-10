using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania2.結果
{
    class ランク : IDisposable
    {

        // 生成と終了


        public ランク()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._ランク画像 = new Dictionary<ランク種別, 画像D2D>() {
                { ランク種別.SS, new 画像D2D( @"$(Images)\ResultStage\RankSS.png" ) },
                { ランク種別.S, new 画像D2D( @"$(Images)\ResultStage\RankS.png" ) },
                { ランク種別.A, new 画像D2D( @"$(Images)\ResultStage\RankA.png" ) },
                { ランク種別.B, new 画像D2D( @"$(Images)\ResultStage\RankB.png" ) },
                { ランク種別.C, new 画像D2D( @"$(Images)\ResultStage\RankC.png" ) },
                { ランク種別.D, new 画像D2D( @"$(Images)\ResultStage\RankD.png" ) },
                { ランク種別.E, new 画像D2D( @"$(Images)\ResultStage\RankE.png" ) },
            };
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            foreach( var image in this._ランク画像.Values )
                image.Dispose();
        }



        // 進行と描画


        public void 進行描画する( DeviceContext dc, float left, float top, ランク種別 rank )
        {
            this._ランク画像[ rank ].描画する( dc, left, top, X方向拡大率: 3f, Y方向拡大率: 3f );
        }



        // ローカル


        private readonly Dictionary<ランク種別, 画像D2D> _ランク画像;
    }
}
