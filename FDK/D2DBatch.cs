using System;
using System.Collections.Generic;
using SharpDX.Direct2D1;

namespace FDK
{
    public class D2DBatch
    {
        /// <summary>
        ///		指定したレンダーターゲットに対して、D2D描画処理をバッチ実行する。
        /// </summary>
        /// <remarks>
        ///		このメソッドを使うと、D2D描画処理がレンダーターゲットの BeginDraw() と EndDraw() の間で行われることが保証される。
        ///		また、D2D描画処理中に例外が発生しても EndDraw() の呼び出しが確実に保証される。
        ///     この処理中に D3Dの描画を実行すると、そちらが先に描画されてしまうので注意！！
        /// </remarks>
        /// <param name="renderTarget">レンダリングターゲット。</param>
        /// <param name="D2D描画処理">BeginDraw() と EndDraw() の間で行う処理。</param>
        public static void Draw( RenderTarget renderTarget, Action D2D描画処理 )
        {
            // BatchDraw中のレンダーターゲットリストになかったら、この RenderTarget を使うのは初めてなので、BeginDraw/EndDraw() の呼び出しを行う。
            // もしリストに登録されていたら、この RenderTarget は他の誰かが BeginDraw して EndDraw してない状態（D2DBatcDraw() の最中に
            // D2DBatchDraw() が呼び出されている状態）なので、これらを呼び出してはならない。
            bool BeginとEndを行う = !( _BatchDraw中のレンダーターゲットリスト.Contains( renderTarget ) );

            var pretrans = renderTarget.Transform;
            var preblend = ( renderTarget is DeviceContext dc ) ? dc.PrimitiveBlend : PrimitiveBlend.SourceOver;

            try
            {
                if( BeginとEndを行う )
                {
                    _BatchDraw中のレンダーターゲットリスト.Add( renderTarget );     // Begin したらリストに追加。
                    renderTarget.BeginDraw();
                }

                D2D描画処理();
            }
            finally
            {
                renderTarget.Transform = pretrans;
                if( renderTarget is DeviceContext dc2 )
                    dc2.PrimitiveBlend = preblend;

                if( BeginとEndを行う )
                {
                    renderTarget.EndDraw();
                    _BatchDraw中のレンダーターゲットリスト.Remove( renderTarget );  // End したらリストから削除。
                }
            }
        }

        private static readonly List<RenderTarget> _BatchDraw中のレンダーターゲットリスト = new List<RenderTarget>();
    }
}
