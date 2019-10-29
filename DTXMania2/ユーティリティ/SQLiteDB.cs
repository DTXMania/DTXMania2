using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace DTXMania2
{
    /// <summary>
    ///		SQLiteのデータベースを操作するクラスの共通機能。
    /// </summary>
    class SQLiteDB : IDisposable
    {

        // プロパティ


        /// <summary>
        ///     データベースへの接続。
        /// </summary>
        public SqliteConnection Connection { get; protected set; } = null!;

        /// <summary>
        ///     データベースの user_version プロパティ。
        /// </summary>
        public long UserVersion
        {
            get
            {
                using( var cmd = new SqliteCommand( "PRAGMA user_version", this.Connection ) )
                {
                    return (long) ( cmd.ExecuteScalar() ??
                        throw new Exception( "SQLite DB からの user_version の取得に失敗しました。" ) );
                }
            }
            set
            {
                using( var cmd = new SqliteCommand( $"PRAGMA user_version = {value}", this.Connection ) )
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }



        // 生成と終了


        public SQLiteDB()
        {
        }
        
        public SQLiteDB( VariablePath DBファイルパス )
        {
            this.Open( DBファイルパス );
        }

        public virtual void Dispose()
        {
            this.Connection?.Close();
            this.Connection?.Dispose();

            // SQLite は接続を切断した後もロックを維持するので、GC でそれを解放する。
            // 参考: https://stackoverrun.com/ja/q/3363188
            GC.Collect();
        }

        public void Open( VariablePath DBファイルパス )
        {
            // DBへ接続し、開く。（DBファイルが存在しない場合は自動的に生成される。）

            var db接続文字列 = new SqliteConnectionStringBuilder() {
                DataSource = DBファイルパス.変数なしパス
            }.ToString();

            this.Connection = new SqliteConnection( db接続文字列 );
            this.Connection.Open();
        }
    }
}
