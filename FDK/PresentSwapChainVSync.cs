using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FDK
{
    /// <summary>
    ///     垂直帰線に合わせてスワップチェーンを表示するタスク。
    /// </summary>
    public class PresentSwapChainVSync
    {
        /// <summary>
        ///     スワップチェーンの表示待機中なら true。
        /// </summary>
        public bool 表示待機中
        {
            get => ( 0 != Interlocked.Read( ref this._ただいま表示中 ) );
            protected set => Interlocked.Exchange( ref this._ただいま表示中, ( value ) ? 1 : 0 );
        }

        /// <summary>
        ///     表示用の専用タスクを生成して、そこで垂直帰線同期とスワップチェーンの表示を行う。
        /// </summary>
        public void 表示する( SharpDX.DXGI.Output1 dxgiOutput1, SharpDX.DXGI.SwapChain1 swapChain1, int syncInterval )
        {
            this.表示待機中 = true;    // 表示開始

            // HACK: 毎回 Task.Run を実行しないような実装に変える。
            Task.Run( () => {

                // SwapChain.Present での垂直帰線待ちは広範囲のリソースを巻き込んで処理を停滞させるため、DXGIで行うようにする。
                dxgiOutput1.WaitForVerticalBlank();

                if( !swapChain1.IsDisposed )    // Wait中にDisposeされる場合がある
                    swapChain1.Present( syncInterval, SharpDX.DXGI.PresentFlags.None );

                this.表示待機中 = false;   // 表示完了

            } );
        }


        /// <summary>
        ///     0 なら描画処理が可能、非 0 なら描画処理は不可（スワップチェーンの表示待機中のため）。
        ///     Interlocked クラスを使ってアクセスすること。
        /// </summary>
        private long _ただいま表示中 = 0;
    }
}
