using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX.Direct2D1;

namespace DTXMania2.選曲
{
    /// <summary>
    ///     一定時間ごとに、選択曲を囲む枠の上下辺を右から左へすーっと走る光。
    /// </summary>
    class 選択曲枠ランナー : IDisposable
    {

        // 生成と終了


        public 選択曲枠ランナー()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );
            
            this._ランナー画像 = new 画像( @"$(Images)\SelectStage\FrameRunner.png" );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._ランナー画像.Dispose();
        }



        // 進行と描画


        public void リセットする()
        {
            // 0～2000ms: 非表示、2000～2300ms: 表示 のループ
            this._カウンタ = new LoopCounter( 0, 2300, 1 );
        }

        public void 進行描画する()
        {
            if( this._カウンタ is null )
                return;

            if( 2000 <= this._カウンタ.現在値 )
            {
                float 割合 = ( this._カウンタ.現在値 - 2000 ) / 300f;    // 0→1

                // 上
                this._ランナー画像.描画する(
                    1920f - 割合 * ( 1920f - 1044f ),
                    485f - this._ランナー画像.サイズ.Height / 2f );

                // 下
                this._ランナー画像.描画する(
                    1920f - 割合 * ( 1920f - 1044f ),
                    598f - this._ランナー画像.サイズ.Height / 2f );
            }
        }



        // ローカル


        private readonly 画像 _ランナー画像;

        private LoopCounter? _カウンタ = null;
    }
}
