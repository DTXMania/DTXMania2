using System;
using System.Collections.Generic;
using System.Text;

namespace FDK
{
    /// <summary>
    ///     System.String の拡張メソッド
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        ///		文字列が Null でも空でもないなら true を返す。
        /// </summary>
        public static bool Nullでも空でもない( this string 検査対象 )
            => !( string.IsNullOrEmpty( 検査対象 ) );

        /// <summary>
        ///		文字列が Null または空なら true を返す。
        /// </summary>
        public static bool Nullまたは空である( this string 検査対象 )
            => string.IsNullOrEmpty( 検査対象 );
    }
}
