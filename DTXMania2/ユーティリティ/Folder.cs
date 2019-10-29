using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DTXMania2
{
    /// <summary>
    ///		フォルダ変数の変換機能を提供する。
    /// </summary>
    /// <remarks>
    ///		(1) システムから自動的に取得できるフォルダパス、(2) ユーザ名などが含まれていてログに出力するのがためらわれるフォルダパス などで
    ///		「フォルダ変数」を使うことにより、これらを隠蔽可能にする。
    ///		フォルダ変数は、"$("＋名前＋")" で自由に定義できる。
    /// </remarks>
    class Folder
    {
        
        // フォルダ変数関連


        public static void フォルダ変数を追加または更新する( string 変数名, string 置換するパス文字列 )
        {
            Folder._フォルダ変数toパス[ 変数名 ] = 置換するパス文字列;
        }

        public static void フォルダ変数を削除する( string 変数名 )
        {
            if( Folder._フォルダ変数toパス.ContainsKey( 変数名 ) )
            {
                Folder._フォルダ変数toパス.Remove( 変数名 );
            }
            else
            {
                throw new Exception( $"指定されたフォルダ変数「{変数名}」は存在しません。" );
            }
        }

        public static string フォルダ変数の内容を返す( string 変数名 )
        {
            return ( Folder._フォルダ変数toパス.ContainsKey( 変数名 ) ) ? 
                Folder._フォルダ変数toパス[ 変数名 ] :
                "";
        }

        public static string 絶対パスに含まれるフォルダ変数を展開して返す( string 絶対パス )
        {
            if( string.IsNullOrEmpty( 絶対パス ) )
                return "";

            foreach( var kvp in Folder._フォルダ変数toパス )
            {
                if( !string.IsNullOrEmpty( kvp.Value ) )
                    絶対パス = 絶対パス.Replace( "$(" + kvp.Key + ")", kvp.Value );
            }

            return 絶対パス;
        }

        public static string 絶対パスをフォルダ変数付き絶対パスに変換して返す( string 絶対パス )
        {
            if( string.IsNullOrEmpty( 絶対パス ) )
                return "";

            foreach( var kvp in Folder._フォルダ変数toパス )
            {
                if( !string.IsNullOrEmpty( kvp.Value ) )
                    絶対パス = 絶対パス.Replace( kvp.Value, "$(" + kvp.Key + ")" );
            }

            return 絶対パス;
        }



        // ユーティリティ


        public static string 絶対パスを相対パスに変換する( string 基点フォルダの絶対パス, string 変換したいフォルダの絶対パス )
        {
            if( string.IsNullOrEmpty( 変換したいフォルダの絶対パス ) )
                return "";

            if( !( Path.IsPathRooted( 基点フォルダの絶対パス ) ) )
                throw new Exception( $"基点フォルダは絶対パスで指定してください。[{基点フォルダの絶対パス}]" );

            if( !( Path.IsPathRooted( 変換したいフォルダの絶対パス ) ) )
                throw new Exception( $"変換対象フォルダは絶対パスで指定してください。[{変換したいフォルダの絶対パス}]" );

            // 末尾は \ にしておく（"+"でパスを連結する事態を想定。Path.Combine() を使う分には、末尾に \ があってもなくてもどっちでもいい。）
            if( '\\' != 基点フォルダの絶対パス[ 基点フォルダの絶対パス.Length - 1 ] )
                基点フォルダの絶対パス += @"\";

            // 絶対-相対パス変換は、System.IO.Path クラスではなく System.IO.Uri クラスでしか行えない。
            var 基点uri = new Uri( 基点フォルダの絶対パス );
            var 変換前uri = new Uri( 変換したいフォルダの絶対パス );
            var 変換後uri = 基点uri.MakeRelativeUri( 変換前uri );

            // URI形式になっているので、パス形式に戻す。（具体的には、エスケープ文字を復元し、さらに '/' を '\' に置換する。）
            return Uri.UnescapeDataString( 変換後uri.ToString() ).Replace( oldChar: '/', newChar: '\\' );
        }



        // ローカル


        private static readonly Dictionary<string, string> _フォルダ変数toパス = new Dictionary<string, string>();
    }
}
