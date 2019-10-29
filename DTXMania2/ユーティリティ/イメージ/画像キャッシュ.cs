using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace DTXMania2
{
    /// <summary>
    ///     画像をキャッシュする辞書。
    /// </summary>
    class 画像キャッシュ : ConcurrentDictionary<object, IImage>, IDisposable
    {

        // プロパティ


        /// <summary>
        ///     指定されたキーに対する画像。
        /// </summary>
        /// <remarks>
        ///     キーが存在しなかった場合は null が返される。
        ///     キーに対する新しい値として null を set すると、そのキーと既存の値は削除される。
        /// </remarks>
        public new IImage? this[ object key ]
        {
            get
            {
                // キーが存在しないなら null を返す。
                return ( this.ContainsKey( key ) && this.TryGetValue( key, out var value ) ) ? value : null;
            }
            set
            {
                if( value is null )
                {
                    // キーに対する新しい値として null を set すると、そのキーと既存の値は削除される。
                    if( this.ContainsKey( key ) && this.TryRemove( key, out var image ) )
                        image.Dispose();
                }
                else
                {
                    // null 以外の場合、追加または更新する。
                    this.AddOrUpdate( key, value!, ( key, old_image ) => {
                        old_image.Dispose();    // 更新の場合は古い画像を破棄する。
                        return value!;
                    } );
                }
            }
        }



        // 生成と終了


        public 画像キャッシュ()
        {
        }

        public virtual void Dispose()
        {
            foreach( var kvp in this )
                kvp.Value.Dispose();

            this.Clear();
        }
    }
}
