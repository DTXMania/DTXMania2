using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Data.Sqlite;

namespace DTXMania2
{
    partial class RecordDB : SQLiteDB
    {
        public static readonly VariablePath RecordDBPath = new VariablePath( @"$(AppData)\RecordDB.sqlite3" );


        public RecordDB()
            : base()
        {
            try
            {
                this.Open( RecordDBPath );
            }
            catch( Exception e )
            {
                Log.WARNING( $"エラーが発生したので、新しく作り直します。[{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( e.Message )}]" );

                #region " DBファイルを削除 "
                //----------------
                try
                {
                    File.Delete( RecordDBPath.変数なしパス );  // ファイルがない場合には例外は出ない
                }
                catch( Exception e2 )
                {
                    var msg = $"成績データベースファイルの削除に失敗しました。[{RecordDBPath.変数付きパス}][{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( e.Message )}]";
                    Log.ERROR( msg );
                    throw new Exception( msg, e2 );  // どうしようもないので例外発出
                }
                //----------------
                #endregion

                this.Open( RecordDBPath );
            }
        }
    }
}
