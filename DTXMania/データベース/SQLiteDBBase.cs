using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using FDK;

namespace DTXMania
{
    /// <summary>
    ///		SQLiteのデータベースを操作するクラスの共通機能。
    /// </summary>
    abstract class SQLiteDBBase : IDisposable
    {
        /// <summary>
        ///     データベースの user_version プロパティ。
        /// </summary>
        public long UserVersion
        {
            get => this.DataContext.ExecuteQuery<long>( @"PRAGMA user_version" ).FirstOrDefault();
            set => this.DataContext.ExecuteCommand( $"PRAGMA user_version = {value}" );
        }

        public SQLiteConnection Connection { get; protected set; } = null;

        public DataContext DataContext { get; protected set; } = null;


        public SQLiteDBBase()
        {
        }

        public virtual void Open( VariablePath DBファイルパス, long Version )
        {
            if( 0 == Version )
                throw new Exception( "Version = 0 は予約されています。" );


            // DBへ接続し、開く。

            this._DBファイルパス = DBファイルパス;
            this._DB接続文字列 = new SQLiteConnectionStringBuilder() { DataSource = this._DBファイルパス.変数なしパス }.ToString();

            this.Connection = new SQLiteConnection( this._DB接続文字列 );
            this.Connection.Open();

            this.ResetDataContext();


            // マイグレーションが必要？

            var 実DBのバージョン = this.UserVersion;   // DBが存在しない場合は 0 。

            if( 実DBのバージョン == Version )
            {
                // (A) マイグレーション不要。
            }
            else if( 実DBのバージョン == 0 )
            {
                #region " (B) 実DBが存在していない　→　作成する。"
                //----------------
                this.テーブルがなければ作成する();
                this.UserVersion = Version;
                //----------------
                #endregion
            }
            else if( 実DBのバージョン < Version )
            {
                #region " (C) 実DBが下位バージョンである　→　アップグレードする。"
                //----------------
                try
                {
                    while( 実DBのバージョン < Version )
                    {
                        // 1バージョンずつアップグレード。
                        this.データベースのアップグレードマイグレーションを行う( 実DBのバージョン );
                        実DBのバージョン++;
                    }

                    // すべて成功した場合にのみ UserVersion を更新する。
                    this.UserVersion = Version;
                }
                catch( Exception ex )
                {
                    Log.ERROR( ex.Message );
                }
                //----------------
                #endregion
            }
            else
            {
                // (D) 実DBが上位バージョンである　→　例外発出。上位互換はなし。
                throw new Exception( $"データベースが未知のバージョン({実DBのバージョン})です。" );
            }
        }

        public void Dispose()
        {
            // DB接続を閉じる。

            //this.DataContext?.SubmitChanges();	--> Submit していいとは限らない。
            this.DataContext?.Dispose();

            this.Connection?.Close();
            this.Connection?.Dispose();

            // SQLite は接続を切断した後もロックを維持するので、GC でそれを解放する。
            // 参考: https://stackoverrun.com/ja/q/3363188
            GC.Collect();
        }

        public void ResetDataContext()
        {
            this.DataContext?.Dispose();
            this.DataContext = new DataContext( this.Connection );
        }


        protected VariablePath _DBファイルパス;

        protected string _DB接続文字列;


        // 以下、派生クラスで実装する。

        protected abstract void テーブルがなければ作成する();

        /// <summary>
        ///		{移行元DBバージョン} から {移行元DBバージョン+1} へ、１つだけマイグレーションする。
        /// </summary>
        protected abstract void データベースのアップグレードマイグレーションを行う( long 移行元DBバージョン );
    }
}
