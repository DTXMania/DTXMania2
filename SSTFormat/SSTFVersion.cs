using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SSTFormat
{
    /// <summary>
    ///		SSTFormat のバージョンを表す。
    /// </summary>
    public class SSTFVersion : ICloneable, IComparable, IComparable<SSTFVersion>, IEquatable<SSTFVersion>
    {
        /// <summary>
        ///		メジャーバージョン番号。
        /// </summary>
        public int Major => this._version.Major;

        /// <summary>
        ///		マイナーバージョン番号。
        /// </summary>
        public int Minor => this._version.Minor;

        /// <summary>
        ///		ビルド番号。
        ///		未定義の場合は -1。
        /// </summary>
        public int Build => this._version.Build;

        /// <summary>
        ///		リビジョン番号。
        ///		未定義の場合は -1。
        /// </summary>
        public int Revision => this._version.Revision;

        public SSTFVersion()
        {
            this._version = new System.Version();
        }
        public SSTFVersion( string version )
        {
            this._version = new System.Version( version );
        }
        public SSTFVersion( int major, int minor, int build = -1, int revision = -1 )
        {
            // System.Version のコンストラクタの引数の数は ToString() に影響を与えるので、
            // 適切なコンストラクタを個別に呼び出すようにする。

            if( ( -1 == build ) && ( -1 == revision ) )
            {
                this._version = new System.Version( major, minor );
            }
            else if( -1 == revision )
            {
                this._version = new System.Version( major, minor, build );
            }
            else
            {
                this._version = new System.Version( major, minor, build, revision );
            }
        }

        /// <summary>
        ///		SSTF ファイルの SSTF バージョンを取得する。
        /// </summary>
        /// <param name="path">
        ///		SSTF ファイルのパス。
        /// </param>
        /// <returns>
        ///		取得された SSTF バージョン。
        ///		SSTF ファイルにバージョンの記載がない場合には、1.0.0.0 が返される。
        /// </returns>
        /// <remarks>
        ///		SSTFVersion の指定は、先頭行でのみ可能。
        ///		例: "# SSTFVersion 1.0.0.0"
        /// </remarks>
        public static SSTFVersion CreateVersionFromFile( string path )
        {
            if( false == File.Exists( path ) )
                throw new FileNotFoundException( $"指定されたファイルが存在しません。[{path}]" );

            using( var reader = new StreamReader( path, Encoding.UTF8 ) )
            {
                string バージョン文字列 = "1.2.0.0";    // 既定のバージョン
                string トークン = "# SSTFVersion";

                // ファイルの最初の行を読み込む。
                string 先頭行 = reader.ReadLine();

                // 最初の行にトークンがあればバージョン文字列を取得する。
                if( 先頭行.StartsWith( トークン ) )
                    バージョン文字列 = 先頭行[ ( トークン.Length ).. ].Trim();

                return new SSTFVersion( バージョン文字列 );
            }
        }

        /// <summary>
        ///		内部処理用コンストラクタ。
        /// </summary>
        /// <param name="version">
        ///		元になる System.Version インスタンス。
        /// </param>
        private SSTFVersion( System.Version version )
        {
            this._version = version;
        }

        /// <summary>
        ///		バージョン番号の文字列形式を、等価な Version オブジェクトに変換する。
        /// </summary>
        /// <param name="input">
        ///		変換するバージョン番号を含んだ文字列。
        ///	</param>
        /// <returns>
        ///		input パラメータで指定されているバージョン番号と等価のオブジェクト。
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        ///		input が null である。
        /// </exception>
        /// <exception cref="System.ArgumentException">
        ///		input のバージョン構成要素数が 2 未満であるか、4 を超えている。
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///		input 内の少なくとも 1 つの構成要素が 0 未満である。
        /// </exception>
        /// <exception cref="System.FormatException">
        ///		input 内の少なくとも 1 の構成要素が整数でない。
        /// </exception>
        /// <exception cref="System.OverflowException">
        ///		input の構成要素のうちの少なくとも 1 つが Int32.MaxValue を超えている。
        /// </exception>
        public static SSTFVersion Parse( string input )
        {
            return new SSTFVersion( System.Version.Parse( input ) );
        }

        /// <summary>
        ///		バージョン番号の文字列形式から、等価の Version オブジェクトへの変換を試行し、変換が成功したかどうかを示す値を返す。
        /// </summary>
        /// <param name="input">
        ///		変換するバージョン番号を含んだ文字列。
        ///	</param>
        /// <param name="result">
        ///		変換に成功した場合は、input が表す番号と等価の Version が格納される。
        ///		変換に失敗した場合は、メジャーバージョン番号とマイナーバージョン番号が 0 の Version オブジェクトが格納される。
        ///		input が null または System.String.Empty である場合、null が格納される。
        /// </param>
        /// <returns>
        ///		input パラメータが正常に変換された場合は true、それ以外の場合は false。
        /// </returns>
        public static bool TryParse( string input, out SSTFVersion result )
        {
            result = null;

            if( string.IsNullOrEmpty( input ) )
            {
                return false;
            }

            if( System.Version.TryParse( input, out System.Version sr ) )
            {
                result = new SSTFVersion( sr );
                return true;
            }

            result = new SSTFVersion( 0, 0 );
            return false;
        }

        /// <summary>
        ///		現在の Version オブジェクトと同じ値を持つ、新しい Version オブジェクトを返す。
        /// </summary>
        /// <returns>
        ///		現在の Version オブジェクトのコピーを値として持つ新しい System.Object。
        /// </returns>
        public object Clone()
        {
            return new SSTFVersion( this._version );
        }

        /// <summary>
        ///		指定したオブジェクトと現在の Version オブジェクトを比較し、これらの相対値を示す値を返す。
        /// </summary>
        /// <param name="version">
        ///		比較対象のオブジェクト、または null。
        ///	</param>
        /// <returns>
        ///		2 つのオブジェクトの相対的な値を示す符号付き整数。
        ///		0 より小さい値 …… 現在のオブジェクトは、比較対象より前のバージョンである。
        ///		0 …… 現在のオブジェクトは、比較対象と同じバージョンである。
        ///		0 を超える値 …… 現在のオブジェクトは、比較対象より後のバージョンであるか、または比較対象が null である。
        /// </returns>
        /// <exception cref="System.ArgumentException">
        ///		比較対象が Version 型ではない。
        /// </exception>
        public int CompareTo( object version )
        {
            if( null == version )
                return 1;

            var sstfVer = version as SSTFVersion;

            if( null == sstfVer )
                throw new ArgumentException( "version 引数が SSTFormat.Version 型ではありません。" );

            return this.CompareTo( sstfVer );
        }

        /// <summary>
        ///		指定した Version オブジェクトと現在の Version オブジェクトを比較し、これらの相対値を示す値を返す。
        /// </summary>
        /// <param name="value">
        ///		現在の Version オブジェクトと比較する Version オブジェクト、または null。
        ///	</param>
        /// <returns>
        ///		2 つのオブジェクトの相対的な値を示す符号付き整数。
        ///		0 より小さい値 …… 現在のオブジェクトは、value より前のバージョンである。
        ///		0 …… 現在のオブジェクトは、value と同じバージョンである。
        ///		0 を超える値 …… 現在のオブジェクトは、value より後のバージョンであるか、または value が null である。
        /// </returns>
        public int CompareTo( SSTFVersion value )
        {
            return this._version.CompareTo(
                new System.Version( value.Major, value.Minor, value.Build, value.Revision ) );
        }

        /// <summary>
        ///		指定されたオブジェクトと現在の Version オブジェクトが等しいかどうかを示す値を返す。
        /// </summary>
        /// <param name="obj">
        ///		現在の Version オブジェクトと比較するオブジェクト、または null。
        ///	</param>
        /// <returns>
        ///		現在の Version オブジェクトと obj がどちらも Version オブジェクトであり、
        ///		現在のオブジェクトのすべての構成要素が obj オブジェクトの対応する構成要素に一致する場合は true。
        ///		それ以外の場合は false。
        ///	</returns>
        /// <exception cref="System.ArgumentException">
        ///		比較対象が Version 型ではない。
        /// </exception>
        public override bool Equals( object obj )
        {
            if( null == obj )
                return false;

            var sstfVer = obj as SSTFVersion;

            if( null == sstfVer )
                throw new ArgumentException( "obj 引数が SSTFormat.Version 型ではありません。" );

            return this._version.Equals(
                new System.Version( sstfVer.Major, sstfVer.Minor, sstfVer.Build, sstfVer.Revision ) );
        }

        /// <summary>
        ///		現在の Version オブジェクトと指定した Version オブジェクトが同じ値を表しているかどうかを示す値を返す。
        /// </summary>
        /// <param name="obj">
        ///		現在の Version オブジェクトと比較する Version オブジェクト、または null。
        ///	</param>
        /// <returns>
        ///		現在の Version オブジェクトのすべての構成要素が obj パラメータの対応する構成要素に一致する場合は true、それ以外の場合は false。
        ///	</returns>
        public bool Equals( SSTFVersion obj )
        {
            if( null == obj )
                return false;

            return this._version.Equals(
                new System.Version( obj.Major, obj.Minor, obj.Build, obj.Revision ) );
        }

        /// <summary>
        ///		現在のオブジェクトのハッシュコードを返す。
        /// </summary>
        /// <returns>
        ///		32 ビット符号付き整数ハッシュコード。
        ///	</returns>
        public override int GetHashCode()
        {
            return this._version.GetHashCode();
        }

        /// <summary>
        ///		現在の Version オブジェクトの値を、それと等価な System.String 形式に変換する。
        /// </summary>
        /// <returns>
        ///		現在の Version オブジェクトの各構成要素 (メジャー、マイナー、ビルド、リビジョン) の値を、次に説明する書式で表した System.String 形式。
        ///		各構成要素はピリオド文字 (.) で区切られる。
        ///		角かっこ ("[" および "]") は、定義されていない場合は戻り値に現れない構成要素を示す。（major.minor[.build[.revision]]）
        ///		たとえば、コンストラクタ Version(1,1) を使用して Version オブジェクトを作成した場合は、文字列 "1.1" が返される。
        ///		コンストラクタ Version(1,3,4,2) を使用して Version オブジェクトを作成した場合は、文字列 "1.3.4.2" が返される。
        /// </returns>
        public override string ToString()
        {
            return this._version.ToString();
        }

        /// <summary>
        ///		現在の Version オブジェクトの値を、それと等価な System.String 形式に変換する。
        ///		指定された数は、返される構成要素の数を示す。
        /// </summary>
        /// <param name="fieldCount">
        ///     返される構成要素の数。0 ～ 4。
        /// </param>
        /// <returns>
        ///		現在の Version オブジェクトの各構成要素 (メジャー、マイナー、ビルド、リビジョン) の値をピリオド文字 (.) で区切って表した System.String 形式。
        ///		fieldCount パラメータは、返される構成要素の数を決定する。
        ///		fieldCount = 0 …… 空の文字列("")。
        ///		fieldCount = 1 …… メジャー
        ///		fieldCount = 2 …… メジャー.マイナー
        ///		fieldCount = 3 …… メジャー.マイナー.ビルド
        ///		fieldCount = 4 …… メジャー.マイナー.ビルド.リビジョン
        ///		たとえば、コンストラクタ Version(1,3,5) を使用して Version オブジェクトを作成した場合、
        ///		ToString(2) は "1.3" を返し、ToString(4) は例外をスローする。
        /// </returns>
        /// <exception cref="System.ArgumentException">
        ///		fieldCount が 0 未満であるか、4 を超えているか、または現在の Version オブジェクトに定義した構成要素の数を超えている。
        /// </exception>
        public string ToString( int fieldCount )
        {
            return this._version.ToString( fieldCount );
        }

        /// <summary>
        ///		指定した 2 つの Version オブジェクトが等しいかどうかを判断する。
        /// </summary>
        /// <param name="v1">
        ///		最初の Version オブジェクト。
        ///	</param>
        /// <param name="v2">
        ///		2 番目の Version オブジェクト。
        ///	</param>
        /// <returns>
        ///		v1 が v2 に等しい場合は true、それ以外の場合は false。
        /// </returns>
        public static bool operator ==( SSTFVersion v1, SSTFVersion v2 )
        {
            return ( v1._version == v2._version );
        }

        /// <summary>
        ///		指定した 2 つの Version オブジェクトが等しくないかどうかを判断する。
        /// </summary>
        /// <param name="v1">
        ///		最初の Version オブジェクト。
        ///	</param>
        /// <param name="v2">
        ///		2 番目の Version オブジェクト。
        ///	</param>
        /// <returns>
        ///		v1 が v2 に等しくない場合は true、それ以外の場合は false。
        /// </returns>
        public static bool operator !=( SSTFVersion v1, SSTFVersion v2 )
        {
            return ( v1._version != v2._version );
        }

        /// <summary>
        ///		最初に指定した Version オブジェクトが 2 番目に指定した Version オブジェクトより小さいかどうかを判断する。
        /// </summary>
        /// <param name="v1">
        ///		最初の Version オブジェクト。
        ///	</param>
        /// <param name="v2">
        ///		2 番目の Version オブジェクト。
        ///	</param>
        /// <returns>
        ///		v1 が v2 より小さい場合は true、それ以外の場合は false。
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        ///		v1 が null である。
        /// </exception>
        public static bool operator <( SSTFVersion v1, SSTFVersion v2 )
        {
            return ( v1._version < v2._version );
        }

        /// <summary>
        ///		最初に指定した Version オブジェクトが 2 番目に指定した Version オブジェクトより大きいかどうかを判断する。
        /// </summary>
        /// <param name="v1">
        ///		最初の Version オブジェクト。
        ///	</param>
        /// <param name="v2">
        ///		2 番目の Version オブジェクト。
        ///	</param>
        /// <returns>
        ///		v1 が v2 より大きい場合は true、それ以外の場合は false。
        /// </returns>
        public static bool operator >( SSTFVersion v1, SSTFVersion v2 )
        {
            return ( v1._version > v2._version );
        }

        /// <summary>
        ///		最初に指定した Version オブジェクトが 2 番目に指定した Version オブジェクト以下かどうかを判断する。
        /// </summary>
        /// <param name="v1">
        ///		最初の Version オブジェクト。
        ///	</param>
        /// <param name="v2">
        ///		2 番目の Version オブジェクト。
        ///	</param>
        /// <returns>
        ///		v1 が v2 以下の場合は true、それ以外の場合は false。
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        ///		v1 が null である。
        /// </exception>
        public static bool operator <=( SSTFVersion v1, SSTFVersion v2 )
        {
            return ( v1._version <= v2._version );
        }

        /// <summary>
        ///		最初に指定した Version オブジェクトが 2 番目に指定した Version オブジェクト以上であるかどうかを判断する。
        /// </summary>
        /// <param name="v1">
        ///		最初の Version オブジェクト。
        ///	</param>
        /// <param name="v2">
        ///		2 番目の Version オブジェクト。
        ///	</param>
        /// <returns>
        ///		v1 が v2 以上である場合は true、それ以外の場合は false。
        /// </returns>
        public static bool operator >=( SSTFVersion v1, SSTFVersion v2 )
        {
            return ( v1._version >= v2._version );
        }

        /// <summary>
        ///		実体として System.Version を利用する。
        /// </summary>
        private System.Version _version = null;
    }
}
