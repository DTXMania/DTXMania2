using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace DTXMania2
{
    class Utilities
    {
        /// <summary>
        ///		指定されたコマンド名が対象文字列内で使用されている場合に、パラメータ部分の文字列を返す。
        /// </summary>
        /// <remarks>
        ///		.dtx や box.def 等で使用されている "#＜コマンド名＞[:]＜パラメータ＞[;コメント]" 形式の文字列（対象文字列）について、
        ///		指定されたコマンドを使用する行であるかどうかを判別し、使用する行であるなら、そのパラメータ部分の文字列を引数に格納し、true を返す。
        ///		対象文字列のコマンド名が指定したコマンド名と異なる場合には、パラメータ文字列に null を格納して false を返す。
        ///		コマンド名は正しくてもパラメータが存在しない場合には、空文字列("") を格納して true を返す。
        /// </remarks>
        /// <param name="対象文字列">調べる対象の文字列。（例: "#TITLE: 曲名 ;コメント"）</param>
        /// <param name="コマンド名">調べるコマンドの名前（例:"TITLE"）。#は不要、大文字小文字は区別されない。</param>
        /// <returns>パラメータ文字列の取得に成功したら true、異なるコマンドだったなら false。</returns>
        public static bool コマンドのパラメータ文字列部分を返す( string 対象文字列, string コマンド名, out string パラメータ文字列 )
        {
            // コメント部分を除去し、両端をトリムする。なお、全角空白はトリムしない。
            対象文字列 = 対象文字列.Split( ';' )[ 0 ].Trim( ' ', '\t' );

            string 正規表現パターン = $@"^\s*#\s*{コマンド名}(:|\s)+(.*)\s*$";  // \s は空白文字。
            var m = Regex.Match( 対象文字列, 正規表現パターン, RegexOptions.IgnoreCase );

            if( m.Success && ( 3 <= m.Groups.Count ) )
            {
                パラメータ文字列 = m.Groups[ 2 ].Value;
                return true;
            }
            else
            {
                パラメータ文字列 = "";
                return false;
            }
        }
    }
}
