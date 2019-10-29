using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace DTXMania2
{
    partial class SystemConfig
    {
        /// <summary>
        ///		入力コードのマッピング用 Dictionary のキーとなる型。
        /// </summary>
        /// <remarks>
        ///		入力は、デバイスID（入力デバイスの内部識別用ID; FDKのIInputEvent.DeviceIDと同じ）と、
        ///		キー（キーコード、ノート番号などデバイスから得られる入力値）の組で定義される。
        /// </remarks>
        public struct IdKey : IYamlConvertible
        {
            public int deviceId { get; set; }

            public int key { get; set; }


            public IdKey( int deviceId, int key )
            {
                this.deviceId = deviceId;
                this.key = key;
            }
            
            public IdKey( InputEvent ie )
            {
                this.deviceId = ie.DeviceID;
                this.key = ie.Key;
            }
            
            public IdKey( string 文字列 )
            {
                // 変なの食わせたらそのまま例外発出する。
                string[] v = 文字列.Split( new char[] { ',' } );

                this.deviceId = int.Parse( v[ 0 ] );
                this.key = int.Parse( v[ 1 ] );
            }

            void IYamlConvertible.Read( IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer )
            {
                var devkey = (string?) nestedObjectDeserializer( typeof( string ) ) ?? "";

                string 正規表現パターン = $@"^(\d+),(\d+)$";  // \d は10進数数字
                var m = Regex.Match( devkey, 正規表現パターン, RegexOptions.IgnoreCase );

                if( m.Success && ( 3 <= m.Groups.Count ) )
                {
                    this.deviceId = int.Parse( m.Groups[ 1 ].Value );
                    this.key = int.Parse( m.Groups[ 2 ].Value );
                }
            }
            
            void IYamlConvertible.Write( IEmitter emitter, ObjectSerializer nestedObjectSerializer )
            {
                nestedObjectSerializer( $"{this.deviceId},{this.key}" );
            }

            public override string ToString()
                => $"{this.deviceId},{this.key}";
        }
    }
}
