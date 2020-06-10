using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania2.選曲
{
    class 表示方法選択パネル : IDisposable
    {

        // 生成と終了


        public 表示方法選択パネル()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._論理パネル番号 = new TraceValue( Global.Animation, 初期値: 0.0, 切替時間sec: 0.1 );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._論理パネル番号.Dispose();

            foreach( var p in this._パネルs )
                p.Dispose();
        }



        // 進行と描画


        public void 次のパネルを選択する()
        {
            this._論理パネル番号.目標値++;

            Global.App.曲ツリーリスト.SelectNext( Loop: true );
        }

        public void 前のパネルを選択する()
        {
            this._論理パネル番号.目標値--;

            Global.App.曲ツリーリスト.SelectPrev( Loop: true );
        }

        public void 進行描画する( DeviceContext dc )
        {
            // パネルを合計８枚表示する。（左隠れ１枚 ＋ 表示６枚 ＋ 右隠れ１枚）

            int 論理パネル番号 = (int) Math.Truncate( this._論理パネル番号.現在値 );
            double 差分 = this._論理パネル番号.現在値 - (double) 論理パネル番号;   // -1.0 < 差分 < 1.0

            for( int i = 0; i < 8; i++ )
            {
                int 実パネル番号 = this._論理パネル番号を実パネル番号に変換して返す( 論理パネル番号 + i - 3 );    // 現在のパネルの3つ前から表示開始。
                var 画像 = this._パネルs[ 実パネル番号 ].画像;

                const float パネル幅 = 144f;
                画像.描画する(
                    dc, 
                    左位置: (float) ( 768f + パネル幅 * ( i - 差分 ) ),
                    上位置: ( 3 == i ) ? 90f : 54f,            // i==3 が現在の選択パネル。他より下に描画。
                    不透明度0to1: ( 3 == i ) ? 1f : 0.5f );    //          〃　　　　　　　他より明るく描画。
            }
        }



        // ローカル


        private class Panel : IDisposable
        {
            public VariablePath 画像の絶対パス;
            public 画像D2D 画像;

            public Panel( VariablePath path )
            {
                this.画像の絶対パス = path;
                this.画像 = new 画像D2D( path );

            }
            public void Dispose()
            {
                this.画像.Dispose();
            }
        };

        /// <summary>
        ///     <see cref="App.曲ツリーリスト"/> と同じ並びであること。
        /// </summary>
        private readonly List<Panel> _パネルs = new List<Panel>() {
            new Panel( @"$(Images)\SelectStage\Sorting_All.png" ),
            new Panel( @"$(Images)\SelectStage\Sorting_Evaluation.png" ),
        };

        private readonly TraceValue _論理パネル番号;

        private int _論理パネル番号を実パネル番号に変換して返す( int 論理パネル番号 )
        {
            return ( 0 <= 論理パネル番号 ) ?

                // 例:パネル数 3 で論理パネル番号が正の時: 0,1,2,3,4,5,... → 実パネル番号 = 0,1,2,0,1,2,...
                論理パネル番号 % this._パネルs.Count :

                // 例:パネル数 3 で論理パネル番号が負の時: -1,-2,-3,-4,-5,... → 実パネル番号 = 2,1,0,2,1,0,...
                ( this._パネルs.Count - ( ( -論理パネル番号 ) % this._パネルs.Count )) % this._パネルs.Count;
        }
    }
}
