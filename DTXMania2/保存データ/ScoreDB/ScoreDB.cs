using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DTXMania2
{
    partial class ScoreDB : SQLiteDB
    {
        public static readonly VariablePath ScoreDBPath = new VariablePath( @"$(AppData)\ScoreDB.sqlite3" );


        public ScoreDB( VariablePath? path = null )
            : base()
        {
            path ??= ScoreDBPath;

            try
            {
                this.Open( path );
            }
            catch( Exception e )
            {
                Log.WARNING( $"エラーが発生したので、新しく作り直します。[{path.変数付きパス}][{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( e.Message )}]" );

                #region " DBファイルを削除 "
                //----------------
                try
                {
                    File.Delete( path.変数なしパス );  // ファイルがない場合には例外は出ない
                }
                catch( Exception e2 )
                {
                    var msg = $"曲データベースファイルの削除に失敗しました。[{path.変数付きパス}][{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( e.Message )}]";
                    Log.ERROR( msg );
                    throw new Exception( msg, e2 );  // どうしようもないので例外発出
                }
                //----------------
                #endregion

                this.Open( path );
            }
        }
    }
}
