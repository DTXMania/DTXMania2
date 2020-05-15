using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DTXMania2
{
    /// <summary>
    ///     <see cref="Enum"/>にAliasという属性を設ける。
    /// </summary>
    static class AliasAttributeExtension
    {
        public static string GetAlias( this Enum value )
        {
            string 既定値 = value.ToString();
            var valueType = value.GetType();
            var member = valueType.GetMember( Enum.GetName( valueType, value )! )[ 0 ];  // 必ずある；なければ例外
            var attribute = Attribute.GetCustomAttribute( member, typeof( AliasAttribute ) ) as AliasAttribute;
            return attribute?.別名 ?? 既定値;
        }
    }

    [AttributeUsage( AttributeTargets.Field, Inherited = false, AllowMultiple = false )]
    sealed class AliasAttribute : Attribute
    {
        public string 別名 { get; private set; }
        public AliasAttribute( string 別名 )
        {
            this.別名 = 別名;
        }
    }
}
