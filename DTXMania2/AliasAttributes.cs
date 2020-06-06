using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DTXMania2
{
    /// <summary>
    ///     Alias 属性。フィールドに対する別名を定義する。
    /// </summary>
    [AttributeUsage( AttributeTargets.Field, Inherited = false, AllowMultiple = false )]
    sealed class AliasAttribute : Attribute
    {
        public string 別名 { get; private set; }
        public AliasAttribute( string 別名 )
        {
            this.別名 = 別名;
        }
    }

    /// <summary>
    ///     Alias 属性に関する拡張メソッド。
    /// </summary>
    static class AliasAttributeExtension
    {
        /// <summary>
        ///     指定された列挙子メンバに設定されている Alias 属性の値（文字列）を取得する。
        /// </summary>
        /// <param name="value">列挙子メンバ。</param>
        /// <returns>列挙子メンバに設定されている Alias 属性の値（文字列）。設定されていなければ、列挙子メンバの ToString() 値を返す。</returns>
        public static string GetAlias( this Enum value )
        {
            string 既定値 = value.ToString();
            var valueType = value.GetType();
            var member = valueType.GetMember( Enum.GetName( valueType, value )! )[ 0 ];  // 必ずある；なければ例外
            var attribute = Attribute.GetCustomAttribute( member, typeof( AliasAttribute ) ) as AliasAttribute;
            return attribute?.別名 ?? 既定値;
        }
    }
}
