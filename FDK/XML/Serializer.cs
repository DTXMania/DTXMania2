using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace FDK
{
    /// <summary>
    ///		XmlSerializer を使った XML 入出力機能を提供する。
    /// </summary>
    /// <remarks>
    ///		C# のインスタンスを丸ごと XML に変換・復号できる……　が、制限がいろいろあるので注意。
    ///		インスタンスの構造が複雑なら、FDK.XML.ReaderWriter 名前空間を使うほうが楽かも。
    /// </remarks>
    public class Serializer
    {
        public static void インスタンスをシリアライズしてファイルに保存する<T>( VariablePath XMLファイルパス, T target )
        {
            using( var sw = new StreamWriter(
                new FileStream( XMLファイルパス.変数なしパス, FileMode.Create, FileAccess.Write, FileShare.ReadWrite ),
                Encoding.UTF8 ) )
            {
                new XmlSerializer( typeof( T ) ).Serialize( sw, target );
            }
        }

        public static T ファイルをデシリアライズしてインスタンスを生成する<T>( VariablePath XMLファイル名 )
        {
            using( var sr = new StreamReader(
                new FileStream( XMLファイル名.変数なしパス, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ),
                Encoding.UTF8 ) )
            {
                return (T) new XmlSerializer( typeof( T ) ).Deserialize( sr );
            }
        }
    }
}
