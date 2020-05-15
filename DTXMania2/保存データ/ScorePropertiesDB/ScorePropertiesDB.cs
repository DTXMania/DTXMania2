using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using FDK;

namespace DTXMania2
{
    partial class ScorePropertiesDB : SQLiteDB
    {
        public static readonly VariablePath ScorePropertiesDBPath = new VariablePath( @"$(AppData)\ScorePropertiesDB.sqlite3" );


        public ScorePropertiesDB()
            : base()
        {
            try
            {
                this.Open( ScorePropertiesDBPath );
            }
            catch( Exception e )
            {
                Log.WARNING( $"エラーが発生したので、新しく作り直します。[{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( e.Message )}]" );

                #region " DBファイルを削除 "
                //----------------
                try
                {
                    File.Delete( ScorePropertiesDBPath.変数なしパス );  // ファイルがない場合には例外は出ない
                }
                catch( Exception e2 )
                {
                    var msg = $"曲属性データベースファイルの削除に失敗しました。[{ScorePropertiesDBPath.変数付きパス}][{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( e.Message )}]";
                    Log.ERROR( msg );
                    throw new Exception( msg, e2 );  // どうしようもないので例外発出
                }
                //----------------
                #endregion

                this.Open( ScorePropertiesDBPath );
            }
        }
    }
}
