using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace FDK
{
    /// <summary>
    ///     ファイルから生成されるデータをキャッシュし、貸与する。
    /// </summary>
    /// <remarks>
    ///     世代番号について：
    ///         ・このクラスでは、「世代」という時間を管理する。
    ///         　世代は、0以上の整数であり、上限はない。
    ///         ・利用者は、任意のタイミングで、このクラスの世代番号を１つ進める（加算する）ことができる。
    ///           これ以外に、世代を変更する方法はない。
    ///           
    ///     生成データについて：
    ///         ・ファイルから <see cref="ファイルからデータを生成する"/> を通じて生成したものが、生成データである。
    ///         ・生成データには、作成時点における世代番号が付与され、貸し出しのたびに現行化される。
    ///         ・利用者は、渡された生成データが IDisposable であったとしても、それを破棄してはならない。（レンタル品・共用物品なので）
    ///         
    ///     古いデータの削除について：
    ///         ・このクラスの世代番号が加算されたときには、指定された世代数（今は1）だけ残して、
    ///         　それより古い世代のインスタンスがすべて破棄（Dispose）される。
    ///         　そのため、世代番号を加算する前には、利用者がもつすべてのインスタンスへの参照を断っておくことが望ましい。
    ///         　
    ///     利用イメージ：
    ///         (0) このクラスのインスタンスの生成後、外部依存アクションを接続すること。
    ///         (1) 利用者は、データを含んだファイルAを用意する。
    ///         (2) このクラスは、ファイルAからデータSを生成し、利用者に貸与する。
    ///             このとき、データSには、その時点での世代番号が付与される。
    ///         (3) 利用者は、データSを活用する。
    ///         (4) 利用者は、利用の終わったデータSへの参照を解放する。（これが返却にあたる。Disposeはしないこと！）
    ///         (5) 利用者は、このクラスの世代を１つ進める。
    ///         (6) このクラスは、生成データのうち、世代が２つ以上離れているデータを破棄（Dispose）する。
    ///         (7) 利用者は、再び、データを含んだファイルAを用意する。
    ///         (8) このクラスは、ファイルAから生成済みのデータSを持っているので、それを利用者に貸与する。
    ///             このとき、データSに割り当てられている世代番号は、その時点での世代番号に更新される。
    /// </remarks>
    public class CacheStore<T> : IDisposable where T : class
    {
        /// <summary>
        ///     指定したファイルからデータを生成して返す、外部依存アクション。
        /// </summary>
        /// <remarks>
        ///     このメソッドは内部利用用。
        /// </remarks>
        public Func<VariablePath, T?>? ファイルからデータを生成する = null;

        /// <summary>
        ///     現在の世代番号。0 以上の整数、上限なし。
        /// </summary>
        public int 現世代 { get; protected set; } = 0;


        public CacheStore()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._キャッシュデータリスト = new Dictionary<string, キャッシュ情報<T>>();
        }

        public void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            foreach( var kvp in this._キャッシュデータリスト )
                ( kvp.Value as IDisposable )?.Dispose();
        }

        /// <summary>
        ///     世代を１つ加算する。
        /// </summary>
        public void 世代を進める()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            // 今回の世代では貸与されなかった（＝最終貸与世代が現時点の世代ではない）キャッシュデータをすべて破棄する。

            var 削除対象リスト = this._キャッシュデータリスト
                .Where( ( kvp ) => kvp.Value.最終貸与世代 < this.現世代 )
                .Select( ( kvp ) => kvp.Key )
                .ToArray();

            foreach( var key in 削除対象リスト )
            {
                this._キャッシュデータリスト[ key ].Dispose();
                this._キャッシュデータリスト.Remove( key );
            }

            Log.Info( $"{this._キャッシュデータリスト.Count} 個のサウンドのうち、{削除対象リスト.Length} 個を削除しました。" );

            // 世代番号を１つ加算する。

            this.現世代++;
        }

        /// <summary>
        ///     指定されたファイルに対応するデータを（未生成なら）生成し、返す。
        ///     生成に失敗したら null。
        /// </summary>
        public T? 作成する( VariablePath ファイルパス )
        {
            try
            {
                if( !File.Exists( ファイルパス.変数なしパス ) )
                {
                    Log.ERROR( $"ファイルが存在しません。[{ファイルパス.変数付きパス}]" );
                    return null;    // 失敗
                }

                var fileInfo = new FileInfo( ファイルパス.変数なしパス );

                if( this._キャッシュデータリスト.TryGetValue( ファイルパス.変数なしパス, out var キャッシュ情報 ) )   // キャッシュにある
                {
                    if( キャッシュ情報.ファイルの最終更新日時 == fileInfo.LastWriteTime )  // ファイルは更新されていない
                    {
                        #region " (A) データがキャッシュに存在していて、ファイルも更新されていない場合　→　それを貸与する "
                        //----------------
                        キャッシュ情報.最終貸与世代 = this.現世代;    // 更新
                        return キャッシュ情報.生成データ;
                        //----------------
                        #endregion
                    }
                    else
                    {
                        #region " (B) データがキャッシュに存在しているが、ファイルが更新されている場合　→　再作成して貸与する "
                        //----------------
                        ( キャッシュ情報.生成データ as IDisposable )?.Dispose();
                        キャッシュ情報.生成データ = this.ファイルからデータを生成する?.Invoke( ファイルパス );
                        if( キャッシュ情報.生成データ is null )
                        {
                            this._キャッシュデータリスト.Remove( ファイルパス.変数なしパス );
                            return null;    // 失敗
                        }

                        キャッシュ情報.ファイルの最終更新日時 = fileInfo.LastWriteTime;
                        キャッシュ情報.最終貸与世代 = 現世代;

                        return キャッシュ情報.生成データ;
                        //----------------
                        #endregion
                    }
                }
                else
                {
                    #region " (C) データがキャッシュに存在しない場合　→　新規作成して貸与する "
                    //----------------
                    var 生成データ = this.ファイルからデータを生成する?.Invoke( ファイルパス );
                    if( 生成データ is null )
                        return null;    // 失敗

                    this._キャッシュデータリスト.Add(  // キャッシュに追加
                        ファイルパス.変数なしパス,
                        new キャッシュ情報<T>() {
                            ファイルパス = ファイルパス,
                            ファイルの最終更新日時 = fileInfo.LastWriteTime,
                            生成データ = 生成データ,
                            最終貸与世代 = this.現世代,
                        } );

                    return 生成データ;
                    //----------------
                    #endregion
                }
            }
            catch
            {
                return null;    // 例外発生
            }
        }


        /// <summary>
        ///     キャッシュされる生成データとその関連情報。
        /// </summary>
        /// <typeparam name="TS">データ型</typeparam>
        internal protected class キャッシュ情報<TS> : IDisposable where TS : class
        {
            /// <summary>
            ///     ファイルから生成されたデータ。
            /// </summary>
            public TS? 生成データ { get; set; } = null;

            /// <summary>
            ///     <see cref="生成データ"/> のもとになったファイルのパス。
            /// </summary>
            public VariablePath ファイルパス { get; set; } = "";

            /// <summary>
            ///     データがファイルから生成されたときの、そのファイルの最終更新日時。
            /// </summary>
            public DateTime ファイルの最終更新日時 { get; set; }

            /// <summary>
            ///     データが生成または最後に貸与されたときの世代番号。
            ///     小さいほど古い。
            /// </summary>
            public int 最終貸与世代 { get; set; }


            /// <summary>
            ///     生成データを破棄する。
            /// </summary>
            public void Dispose()
            {
                ( this.生成データ as IDisposable )?.Dispose();
                this.生成データ = null;
            }
        }

        /// <summary>
        ///     キャッシュデータのリスト。
        ///     [key: 生成元ファイルパス]
        /// </summary>
        internal protected Dictionary<string, キャッシュ情報<T>> _キャッシュデータリスト;
    }
}
