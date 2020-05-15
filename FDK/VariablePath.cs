using System;
using System.Collections.Generic;
using System.Diagnostics;
using YamlDotNet.Serialization;

namespace FDK
{
    /// <summary>
    ///		フォルダ変数（<see cref="Folder"/>参照）の使えるパスを管理する。
    /// </summary>
    /// <remarks>
    ///		string の代わりにこの型を使えば、任意のパスを扱う際に、そのパスのフォルダ変数の有無を考慮することなく
    ///		好きなほうのメンバ（変数なし・変数付き）を使うことができるようになる。
    ///     <see cref="VariablePath"/> 型と string は、暗黙的に相互変換できる。
    /// </remarks>
    public class VariablePath : IYamlConvertible
    {

        // プロパティ


        /// <summary>
        ///		管理しているパスにフォルダ変数が含まれている場合、それを展開して返す。
        /// </summary>
        public string 変数なしパス { get; protected set; } = "";

        /// <summary>
        ///		管理しているパスにフォルダ変数に置き換えられる場所があるなら、それを置き換えて返す。
        /// </summary>
        public string 変数付きパス { get; protected set; } = "";



        // 生成と終了


        /// <summary>
        ///     デフォルトコンストラクタ。
        ///     Yamlデシリアライズのために必要。
        /// </summary>
        public VariablePath()
        {
        }

        /// <summary>
        ///     コンストラクタ。
        /// </summary>
        /// <param name="パス">管理したいパス文字列。変数付き・変数なしのどちらを指定してもいい。</param>
        public VariablePath( string パス )
        {
            this._初期化( パス );
        }

        private void _初期化( string パス )
        {
            this.変数なしパス = Folder.絶対パスに含まれるフォルダ変数を展開して返す( パス );
            this.変数付きパス = Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( this.変数なしパス );
        }



        // string との相互変換


        /// <summary>
        ///     string から <see cref="VariablePath"/> への暗黙的変換。
        /// </summary>
        public static implicit operator VariablePath( string パス )
            => new VariablePath( パス );

        /// <summary>
        ///     <see cref="VariablePath"/> から string への暗黙的変換その１。
        /// </summary>
        /// <param name="パス"></param>
        //public static implicit operator string( VariablePath パス )     --> なんかすごく間違いやすくなるので廃止。
        //    => パス?.変数付きパス ?? null;

        /// <summary>
        ///     <see cref="VariablePath"/> から string への暗黙的変換その２。
        /// </summary>
        public override string ToString()
            => this.変数付きパス;



        // ローカル（Yaml関連）


        #region " IYamlConvertible 実装 "
        //----------------
        // Yaml から変換する
        void IYamlConvertible.Read( YamlDotNet.Core.IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer )
        {
            var vpath = ( (string?) nestedObjectDeserializer( typeof( string ) ) ) ?? "";

            this._初期化( vpath );
        }

        // Yaml に変換する
        void IYamlConvertible.Write( YamlDotNet.Core.IEmitter emitter, ObjectSerializer nestedObjectSerializer )
        {
            nestedObjectSerializer( this.変数付きパス );
        }
        //----------------
        #endregion
    }
}
