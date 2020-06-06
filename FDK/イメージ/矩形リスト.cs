using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using SharpDX;

namespace FDK
{
    /// <summary>
    ///		任意の文字列から任意の矩形を引き当てるための辞書。
    ///		辞書の内容は、yamlファイルから読み込むことができる。
    /// </summary>
    public class 矩形リスト
    {

        // プロパティ


        /// <summary>
        ///     文字列から矩形へのマッピング。
        /// </summary>
        public Dictionary<string, RectangleF> 文字列to矩形 { get; }

        /// <summary>
        ///		文字列に対応する矩形を返す。
        /// </summary>
        /// <param name="文字列">文字列。</param>
        /// <returns>文字列に対応する矩形。文字列がマッピングできなければ null を返す。</returns>
        public RectangleF? this[ string 文字列 ]
            => this.文字列to矩形.TryGetValue( 文字列, out var 矩形 ) ? 矩形 : (RectangleF?) null;



        // 生成と終了


        public 矩形リスト()
        {
            this.文字列to矩形 = new Dictionary<string, RectangleF>();
        }

        public 矩形リスト( VariablePath yamlファイルパス )
            : this()
        {
            this.矩形リストをyamlファイルから読み込む( yamlファイルパス );
        }

        public void 矩形リストをyamlファイルから読み込む( VariablePath yamlファイルパス )
        {
            // yaml ファイルを読み込む。
            var yamlText = File.ReadAllText( yamlファイルパス.変数なしパス );
            var deserializer = new YamlDotNet.Serialization.Deserializer();
            var yaml = deserializer.Deserialize<YAMLマップ>( yamlText );

            // 内容から矩形リストを作成。
            foreach( var kvp in yaml.RectangleList )
            {
                if( 4 == kvp.Value.Length )
                    this.文字列to矩形[ kvp.Key ] = new RectangleF( kvp.Value[ 0 ], kvp.Value[ 1 ], kvp.Value[ 2 ], kvp.Value[ 3 ] );
                else
                    Log.ERROR( $"矩形リストの書式が不正です。[{yamlファイルパス.変数付きパス}]" );
            }
        }



        // ローカル


        private class YAMLマップ
        {
            public Dictionary<string, float[]> RectangleList { get; set; } = new Dictionary<string, float[]>();
        }
    }
}
