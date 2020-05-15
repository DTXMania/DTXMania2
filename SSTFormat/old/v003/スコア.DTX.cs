using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SSTFormat.v003
{
    public partial class スコア
    {
        /// <summary>
        ///     DTXフォーマットのファイルまたはテキストから <see cref="スコア"/> インスタンスを生成するためのクラス。
        /// </summary>
        /// <remarks>
        ///     テストプロジェクトに対しては InternalsVisibleTo 属性(AssemblyInfo.cs参照))により internal メソッドを可視としているため、
        ///     テスト対象のメソッドは、本来 private でも internal として宣言している。
        /// </remarks>
        public static class DTX
        {
            public enum データ種別 { DTX, GDA, G2D, BMS, BME, 拡張子から判定 };

            /// <summary>
            ///		ファイルから指定された種別のデータを読み込み、スコアを生成して返す。
            ///		読み込みに失敗した場合は、何らかの例外を発出する。
            /// </summary>
            public static スコア ファイルから生成する( string ファイルパス, データ種別 データ種別 = データ種別.拡張子から判定, bool ヘッダだけ = false )
            {
                if( データ種別 == データ種別.拡張子から判定 )
                {
                    #region " ファイルパスの拡張子からデータ種別を自動判別する。"
                    //----------------
                    var 拡張子toデータ種別マップ = new Dictionary<string, データ種別>() {
                        { ".dtx", データ種別.DTX },
                        { ".gda", データ種別.GDA },
                        { ".g2d", データ種別.G2D },
                        { ".bms", データ種別.BMS },
                        { ".bme", データ種別.BME },
                    };

                    string ファイルの拡張子 = Path.GetExtension( ファイルパス ).ToLower();

                    // マップから、拡張子に対応するデータ種別を取得する。
                    if( !( 拡張子toデータ種別マップ.TryGetValue( ファイルの拡張子, out データ種別 確定したデータ種別 ) ) )
                    {
                        // マップにない拡張子はすべて DTX とみなす。
                        確定したデータ種別 = データ種別.DTX;
                    }

                    データ種別 = 確定したデータ種別;
                    //----------------
                    #endregion
                }

                string 全入力文字列 = null;

                // ファイルの内容を一気読み。
                using( var sr = new StreamReader( ファイルパス, Encoding.GetEncoding( 932/*Shift-JIS*/ ) ) )
                    全入力文字列 = sr.ReadToEnd();

                // 読み込んだ内容でスコアを生成する。
                var score = 文字列から生成する( 全入力文字列, データ種別, ヘッダだけ );

                // ファイルから読み込んだ場合のみ、このメンバが有効。
                score.譜面ファイルパス = ファイルパス;

                return score;
            }

            /// <summary>
            ///     指定されたデータ種別のテキストデータを含んだ１つの文字列から、スコアを生成して返す。
            ///		読み込みに失敗した場合は、何らかの例外を発出する。
            /// </summary>
            public static スコア 文字列から生成する( string 全入力文字列, データ種別 データ種別 = データ種別.DTX, bool ヘッダだけ = false )
            {
                if( データ種別 == データ種別.拡張子から判定 )
                    throw new Exception( "文字列から生成する場合には、拡張子からの自動判定は行えません。" );

                現在の.状態をリセットする();
                現在の.スコア = new スコア();
                現在の.スコア.譜面ファイルパス = null;    // ファイルから読み込んだ場合のみ、このメンバが有効。
                現在の.データ種別 = データ種別;

                全入力文字列 = 全入力文字列.Replace( '\t', ' ' );   // TAB は空白に置換しておく。


                // 読み込み

                using( var sr = new StringReader( 全入力文字列 ) )
                {
                    #region " すべての行について解析する。"
                    //----------------
                    string 行;

                    for( 現在の.行番号 = 1; ( 行 = sr.ReadLine() ) != null; 現在の.行番号++ )
                    {
                        // 行を分割する。

                        if( !_行をコマンドとパラメータとコメントに分解する( 行, out 現在の.コマンド, out 現在の.コマンドzzなし, out 現在の.zz16進数, out 現在の.zz36進数, out 現在の.パラメータ, out 現在の.コメント ) )
                        {
                            //Trace.TraceWarning( $"書式が不正です。無視します。[{現在の.行番号}行]" );
                            continue;
                        }

                        if( string.IsNullOrEmpty( 現在の.コマンド ) )
                            continue;


                        // コマンド別に解析する。

                        if( _コマンドtoアクションマップ.TryGetValue( 現在の.コマンドzzなし.ToLower(), out var アクション ) ||    // zz付きコマンドとzzなしコマンドが重複する場合は、zzなしのほうが優先
                            _コマンドtoアクションマップ.TryGetValue( 現在の.コマンド.ToLower(), out アクション ) )
                        {
                            if( !ヘッダだけ || アクション.ヘッダである )
                                アクション.解析アクション();
                        }
                        else
                        {
                            // マップにあるコマンドに該当がなければ、オブジェクト配置として解析する。
                            if( !ヘッダだけ )
                                _コマンド_オブジェクト記述();
                        }
                    }
                    //----------------
                    #endregion
                }


                // 後処理

                if( !ヘッダだけ )
                {
                    #region " チップリストの先頭に #BPM チップを追加する。"
                    //----------------
                    {
                        if( !( 現在の.BPM定義マップ.TryGetValue( 0, out double BPM値 ) ) )
                            BPM値 = スコア.初期BPM;   // '#BPM:' が定義されてないなら初期BPMを使う

                        現在の.スコア.チップリスト.Add(
                            new チップ() {
                                BPM = BPM値,
                                チップ種別 = チップ種別.BPM,
                                小節番号 = 0,
                                小節解像度 = 現在の.小節解像度,
                                小節内位置 = 0,
                                音量 = チップ.最大音量,
                                可視 = false,
                            } );
                    }
                    //----------------
                    #endregion

                    #region " 拍線を追加する。"
                    //-----------------
                    {
                        // 小節線を先に追加すると小節が１つ増えてしまうので、拍線から先に追加する。

                        int 最大小節番号 = 現在の.スコア.最大小節番号を返す();      // 最大小節番号はチップ数に依存して変化するので、次の for 文には組み込まないこと。

                        for( int i = 0; i <= 最大小節番号; i++ )
                        {
                            double 小節長倍率 = 現在の.スコア.小節長倍率を取得する( i );

                            for( int n = 1; n * 0.25 < 小節長倍率; n++ )
                            {
                                現在の.スコア.チップリスト.Add(
                                    new チップ() {
                                        小節番号 = i,
                                        チップ種別 = チップ種別.拍線,
                                        小節内位置 = (int) ( ( n * 0.25 ) * 100 ),
                                        小節解像度 = (int) ( 小節長倍率 * 100 ),
                                    } );
                            }
                        }
                    }
                    //-----------------
                    #endregion

                    #region " 小節線を追加する。"
                    //-----------------
                    {
                        int 最大小節番号 = 現在の.スコア.最大小節番号を返す();

                        for( int i = 0; i <= 最大小節番号 + 1; i++ )
                        {
                            現在の.スコア.チップリスト.Add(
                                new チップ() {
                                    小節番号 = i,
                                    チップ種別 = チップ種別.小節線,
                                    小節内位置 = 0,
                                    小節解像度 = 1,
                                } );
                        }
                    }
                    //-----------------
                    #endregion

                    #region " 小節長倍率マップをもとに、スコアの小節長倍率リストを構築する。"
                    //----------------
                    {
                        double 現在の倍率 = 1.0;
                        int 最大小節番号 = 現在の.スコア.最大小節番号を返す();

                        for( int i = 0; i <= 最大小節番号; i++ )      // すべての小節に対して設定。（SST仕様）
                        {
                            if( 現在の.小節長倍率マップ.ContainsKey( i ) )
                                現在の倍率 = 現在の.小節長倍率マップ[ i ]; // 指定された倍率は、それが指定された小節以降の小節にも適用する。（DTX仕様）

                            現在の.スコア.小節長倍率を設定する( i, 現在の倍率 );
                        }
                    }
                    //----------------
                    #endregion

                    #region " BPMチップの値を引き当てる。"
                    //----------------
                    foreach( var kvp in 現在の.BPM参照マップ )
                    {
                        // BPMチャンネルから作成された BPMチップには、BASEBPM を加算する。
                        // #BASEBPM が複数宣言されていた場合は、最後の値が使用される。
                        kvp.Key.BPM = 現在の.BASEBPM + 現在の.BPM定義マップ[ kvp.Value ];
                    }

                    // 引き当てが終わったら、マップが持つチップへの参照を解放する。
                    現在の.BPM参照マップ.Clear();
                    //----------------
                    #endregion

                    #region " WAVチップのPAN値, VOLUME値を引き当てる。"
                    //----------------
                    foreach( var chip in 現在の.スコア.チップリスト )
                    {
                        // このクラスで実装しているチャンネルで、
                        var query = _DTXチャンネルプロパティマップ.Where( ( kvp ) => ( kvp.Value.チップ種別 == chip.チップ種別 ) );

                        if( 1 == query.Count() )
                        {
                            var kvp = query.Single();   // タプル型なので SingleOrDefault() は使えない。

                            // WAV を使うチャンネルで、
                            if( kvp.Value.WAVを使う )
                            {
                                // PAN の指定があるなら、
                                if( 現在の.PAN定義マップ.ContainsKey( chip.チップサブID ) )
                                {
                                    // それをチップに設定する。
                                    chip.左右位置 = 現在の.PAN定義マップ[ chip.チップサブID ];
                                }

                                // VOLUME の指定があるなら、
                                if( 現在の.VOLUME定義マップ.ContainsKey( chip.チップサブID ) )
                                {
                                    // それをチップに設定する。
                                    var DTX音量 = Math.Clamp( 現在の.VOLUME定義マップ[ chip.チップサブID ], min: 0, max: 100 );    // 無音:0 ～ 100:原音

                                    chip.音量 =
                                        ( 100 == DTX音量 ) ? チップ.最大音量 :
                                        (int) ( DTX音量 * チップ.最大音量 / 100.0 ) + 1;
                                }
                            }
                        }
                    }
                    //----------------
                    #endregion

                    スコア._後処理を行う( 現在の.スコア );
                }

                return 現在の.スコア;
            }


            /// <summary>
            ///     解析時の状態を保持するクラス。
            ///     static クラスなので、利用前には <see cref="状態をリセットする"/> を呼び出して前回の解析状態をクリアすること。
            /// </summary>
            internal static class 現在の
            {
                public static スコア スコア;
                public static データ種別 データ種別;
                public static Random Random;

                public static int 行番号;

                public static string コマンド;
                public static string コマンドzzなし;  // コマンドの末尾に、16進数2桁または36進数2桁と解釈できる文字列があるなら、それを除外したコマンド文。ないならコマンドと同じ。
                public static int zz16進数;           // コマンドの末尾に16進数2桁と解釈できる文字列があるならそれを10進数に変換した値。なければ -1。
                public static int zz36進数;           // コマンドの末尾に36進数2桁と解釈できる文字列があるならそれを10進数に変換した値。なければ -1。
                public static string パラメータ;
                public static string コメント;

                public static int 小節番号;
                public static int チャンネル番号;

                public static チップ種別 チップ種別;
                public static int 小節解像度;
                public static int オブジェクト総数;
                public static int オブジェクト番号;

                /// <summary>
                ///		[key: zz番号, value: PAN値(左:-100～中央:0～+100:右)]
                /// </summary>
                public static Dictionary<int, int> PAN定義マップ;

                /// <summary>
                ///		[key: zz番号, key: VOLUME値(0:無音 ～ 100:原音)]
                /// </summary>
                public static Dictionary<int, int> VOLUME定義マップ;


                /* #BPM, #BPMzz の処理方法：
                 * ・#BPMzz は、BPM定義マップ[1～3599] に登録する。
                 * ・#BPM は、#BPM00 とみなして BPM定義マップ[0] に登録する。
                 * ・ch.03 (整数BPM) はそのまま SSTチップ(BPM) としてチップリストに登録する。
                 * ・ch.08（拡張BPM）は、BPM値が 0 の SSTチップ(BPM) をチップリストに登録する。同時に、BPM参照マップ[チップ] にも登録する。
                 * ・DTXファイル読み込み後の後処理として、BPM参照マップ に掲載されているすべての SSTチップ(BPM) の BPM値を、BPM定義マップ から引き当てて設定する。
                 */

                public static double BASEBPM = 0.0;

                /// <summary>
                ///		[key: zz番号, value: BPM]
                /// </summary>
                public static Dictionary<int, double> BPM定義マップ;

                /// <summary>
                /// 	[key: BPMチップ, value: BPM定義のzz番号]
                /// </summary>
                public static Dictionary<チップ, int> BPM参照マップ;


                /* 小節長倍率の処理方法：
                 * ・DTXでは指定した小節以降の小節長がすべて変わってしまうが、SSTでは指定した小節の小節長のみ変わる。
                 * ・そのため、解析時は 小節長倍率マップ に指定があった小節の情報のみ残しておき、
                 *   後処理でまとめて スコア.小節長倍率リスト に登録するものとする。
                 * ・なお、スコア.小節長倍率リスト には存在するすべての小節が登録されていなければならない。（スコアクラスの仕様）
                 */

                /// <summary>
                ///		[key: 小節番号, value: 倍率]
                /// </summary>
                public static SortedDictionary<int, double> 小節長倍率マップ;


                // 初期化。
                public static void 状態をリセットする()
                {
                    スコア = null;
                    データ種別 = データ種別.DTX;
                    Random = new Random( (int) ( Stopwatch.GetTimestamp() % int.MaxValue ) );

                    行番号 = 0;

                    コマンド = "";
                    コマンドzzなし = "";
                    zz16進数 = -1;
                    zz36進数 = -1;
                    パラメータ = "";
                    コメント = "";

                    小節番号 = 0;
                    チャンネル番号 = 0;

                    チップ種別 = チップ種別.Unknown;
                    小節解像度 = 384;    // DTX の小節解像度は 384 固定
                    オブジェクト総数 = 0;
                    オブジェクト番号 = 0;

                    PAN定義マップ = new Dictionary<int, int>();
                    VOLUME定義マップ = new Dictionary<int, int>();

                    BASEBPM = 0.0;
                    BPM定義マップ = new Dictionary<int, double>();
                    BPM参照マップ = new Dictionary<チップ, int>();

                    小節長倍率マップ = new SortedDictionary<int, double>();
                }
            }


            internal static bool _行をコマンドとパラメータとコメントに分解する( string 行, out string コマンド, out string コマンドzzなし, out int zz16進数, out int zz36進数, out string パラメータ, out string コメント )
            {
                コマンド = null;
                コマンドzzなし = null;
                zz16進数 = -1;
                zz36進数 = -1;
                パラメータ = null;
                コメント = null;

                // コマンド, パラメータ, コメント の３つに分割する。
#if DEBUG
                string 行の正規表現 = @"^\s*(?:#\s*([^:;\s]*)[:\s]*([^;]*)?)?[;\s]*(.*)$";    // 仕様の詳細については、テストクラスを参照のこと。
                var m = Regex.Match( 行, 行の正規表現 );
                if( !m.Success || ( 4 != m.Groups.Count ) )
                    return false;
                コマンド = m.Groups[ 1 ].Value?.Trim();
                パラメータ = m.Groups[ 2 ].Value?.Trim();
                コメント = m.Groups[ 3 ].Value?.Trim();

#else
                // todo: Release 版はいずれ高速化したい……

                string 行の正規表現 = @"^\s*(?:#\s*([^:;\s]*)[:\s]*([^;]*)?)?[;\s]*(.*)$";    // 仕様の詳細については、テストクラスを参照のこと。
                var m = Regex.Match( 行, 行の正規表現 );
                if( !m.Success || ( 4 != m.Groups.Count ) )
                    return false;
                コマンド = m.Groups[ 1 ].Value?.Trim();
                パラメータ = m.Groups[ 2 ].Value?.Trim();
                コメント = m.Groups[ 3 ].Value?.Trim();
#endif

                // 残りを決定する。

                コマンドzzなし = コマンド;

                int len = コマンド.Length;

                if( 2 < len )
                {
                    if( !_16進数2桁の文字列を数値に変換して返す( コマンド.Substring( len - 2 ), out zz16進数 ) )
                        zz16進数 = -1;

                    if( !_36進数2桁の文字列を数値に変換して返す( コマンド.Substring( len - 2 ), out zz36進数 ) )
                        zz36進数 = -1;

                    if( -1 != zz16進数 || -1 != zz36進数 )  // どっちか取得できた
                        コマンドzzなし = コマンド.Substring( 0, len - 2 );
                }

                return true;
            }

            internal static void _コマンド_TITLE()
            {
                現在の.スコア.曲名 = 現在の.パラメータ;
            }
            internal static void _コマンド_ARTIST()
            {
                現在の.スコア.アーティスト名 = 現在の.パラメータ;
            }
            internal static void _コマンド_COMMENT()
            {
                現在の.スコア.説明文 = 現在の.パラメータ;
            }
            internal static void _コマンド_DLEVEL_PLAYLEVEL()
            {
                if( !int.TryParse( 現在の.パラメータ, out int level ) )
                {
                    Trace.TraceError( $"#DLEVEL(PLAYLEVEL) の値の取得に失敗しました。[{現在の.行番号}行]" );
                    return;
                }

                現在の.スコア.難易度 = Math.Clamp( level, min: 0, max: 99 ) / 10.0;     // 0～99 → 0.0～9.90
            }
            internal static void _コマンド_PREVIEW()
            {
                現在の.スコア.プレビュー音声ファイル名 = 現在の.パラメータ;
            }
            internal static void _コマンド_PREIMAGE()
            {
                現在の.スコア.プレビュー画像ファイル名 = 現在の.パラメータ;
            }
            internal static void _コマンド_PREMOVIE()
            {
                現在の.スコア.プレビュー動画ファイル名 = 現在の.パラメータ;
            }
            internal static void _コマンド_PATH_WAV()
            {
                現在の.スコア.PATH_WAV = 現在の.パラメータ;
            }
            internal static void _コマンド_WAVzz()
            {
                if( -1 == 現在の.zz36進数 )
                {
                    Trace.TraceError( $"#WAV のWAV番号の取得に失敗しました。[{現在の.行番号}行]" );
                    return;
                }
                if( 1 > 現在の.zz36進数 || 36 * 36 <= 現在の.zz36進数 )
                {
                    Trace.TraceError( $"#WAV のWAV番号に 01～ZZ 以外の値が指定されています。[{現在の.行番号}行]" );
                    return;
                }

                // ここでは、PATH_WAV はまだ反映しない。
                現在の.スコア.WAVリスト[ 現在の.zz36進数 ] = (現在の.パラメータ, true);   // あれば上書き、なければ追加
            }
            internal static void _コマンド_PANzz_WAVPANzz()
            {
                if( -1 == 現在の.zz36進数 )
                {
                    Trace.TraceError( $"#PAN(WAVPAN) のWAV番号の取得に失敗しました。[{現在の.行番号}行]" );
                    return;
                }
                if( 1 > 現在の.zz36進数 || 36 * 36 <= 現在の.zz36進数 )
                {
                    Trace.TraceError( $"#PAN(WAVPAN) のWAV番号に 01～ZZ 以外の値が指定されています。[{現在の.行番号}行]" );
                    return;
                }

                if( !int.TryParse( 現在の.パラメータ, out int PAN値 ) )
                {
                    Trace.TraceError( $"#PAN(WAVPAN) のPAN値の取得に失敗しました。[{現在の.行番号}行]" );
                    return;
                }

                現在の.PAN定義マップ[ 現在の.zz36進数 ] = Math.Clamp( PAN値, min: -100, max: +100 );  // あれば上書き、なければ追加
            }
            internal static void _コマンド_VOLUMEzz_WAVVOLzz()
            {
                if( -1 == 現在の.zz36進数 )
                {
                    Trace.TraceError( $"#VOLUME(WAVVOL) のWAV番号の取得に失敗しました。[{現在の.行番号}行]" );
                    return;
                }
                if( 1 > 現在の.zz36進数 || 36 * 36 <= 現在の.zz36進数 )
                {
                    Trace.TraceError( $"#VOLUME(WAVVOL) のWAV番号に 01～ZZ 以外の値が指定されています。[{現在の.行番号}行]" );
                    return;
                }

                if( !int.TryParse( 現在の.パラメータ, out int VOLUME値 ) )
                {
                    Trace.TraceError( $"#VOLUME(WAVVOL) の値の取得に失敗しました。[{現在の.行番号}行]" );
                    return;
                }

                現在の.VOLUME定義マップ[ 現在の.zz16進数 ] = Math.Clamp( VOLUME値, min: 0, max: 100 );  // あれば上書き、なければ追加
            }
            internal static void _コマンド_BASEBPM()
            {
                if( !_DTX仕様の実数を取得する( 現在の.パラメータ, out double BPM値 ) )
                {
                    Trace.TraceWarning( $"#BASEBPM の値の取得に失敗しました。[{現在の.行番号}行]" );
                    return;
                }
                if( 0.0 > BPM値 )
                {
                    Trace.TraceWarning( $"#BASEBPM の値に負数が指定されています。[{現在の.行番号}行]" );
                    return;
                }

                現在の.BASEBPM = BPM値; // 上書き可
            }
            internal static void _コマンド_BPM_BPMzz()
            {
                if( "bpm" == 現在の.コマンド.ToLower() )
                {
                    現在の.zz36進数 = 0;
                }
                if( 0 > 現在の.zz36進数 || 36 * 36 <= 現在の.zz36進数 )
                {
                    Trace.TraceError( $"#BPM のWAV番号に 00～ZZ 以外の値が指定されています。[{現在の.行番号}行]" );
                    return;
                }

                if( !_DTX仕様の実数を取得する( 現在の.パラメータ, out double BPM値 ) || ( 0.0 >= BPM値 ) || ( 1000.0 <= BPM値 ) ) // 値域制限(<1000)はDTX仕様
                {
                    Trace.TraceError( $"#BPM のBPM値が不正です。[{現在の.行番号}行]" );
                    return;
                }

                現在の.BPM定義マップ[ 現在の.zz36進数 ] = BPM値;  // あれば上書き、なければ追加
            }
            internal static void _コマンド_AVIzz()
            {
                if( -1 == 現在の.zz36進数 )
                {
                    Trace.TraceError( $"#AVI のAVI番号の取得に失敗しました。[{現在の.行番号}行]" );
                    return;
                }
                if( 1 > 現在の.zz36進数 || 36 * 36 <= 現在の.zz36進数 )
                {
                    Trace.TraceError( $"#AVI のAVI番号に 01～ZZ 以外の値が指定されています。[{現在の.行番号}行]" );
                    return;
                }

                // ここでは、PATH_WAV はまだ反映しない。
                現在の.スコア.AVIリスト[ 現在の.zz36進数 ] = 現在の.パラメータ;   // あれば上書き、なければ追加
            }
            internal static void _コマンド_オブジェクト記述()
            {
                #region " 小節番号とチャンネル番号を取得する。"
                //----------------
                if( !_小節番号とチャンネル番号を取得する( 現在の.コマンド, out 現在の.小節番号, out 現在の.チャンネル番号 ) )
                {
                    //Trace.TraceWarning( $"小節番号またはチャンネル番号の取得に失敗しました。[{現在の.行番号}行]" );
                    return;
                }

                // 番号をずらして、先頭に空の小節を１つ設ける。（いきなり演奏が始まらないように。）
                現在の.小節番号++;
                //----------------
                #endregion

                if( 0x02 == 現在の.チャンネル番号 )
                {
                    #region " ch02 小節長倍率 はオブジェクト記述ではないので別途処理する。"
                    //----------------
                    if( !_DTX仕様の実数を取得する( 現在の.パラメータ, out double 小節長倍率 ) )
                    {
                        Trace.TraceWarning( $"ch02 のパラメータ（小節長倍率）の値の取得に失敗しました。[{現在の.行番号}行目]" );
                        return;
                    }
                    if( 0.0 >= 小節長倍率 )
                    {
                        Debug.WriteLine( $"ch02 のパラメータ（小数長倍率）に 0 または負数を指定することはできません。[{現在の.行番号}行目]" );
                        return;
                    }

                    現在の.小節長倍率マップ[ 現在の.小節番号 ] = 小節長倍率;   // あれば上書き、なければ追加
                    //----------------
                    #endregion

                    return;
                }

                現在の.パラメータ = 現在の.パラメータ.Replace( "_", "" );  // 見やすさのために '_' を区切り文字として使用できる（DTX仕様）
                現在の.パラメータ = 現在の.パラメータ.ToLower();           // すべて小文字化

                現在の.オブジェクト総数 = 現在の.パラメータ.Length / 2;    // 1オブジェクトは2文字；余った末尾は切り捨てる。（DTX仕様）


                // すべてのオブジェクトについて...

                for( 現在の.オブジェクト番号 = 0; 現在の.オブジェクト番号 < 現在の.オブジェクト総数; 現在の.オブジェクト番号++ )
                {
                    string オブジェクト記述 = 現在の.パラメータ.Substring( 現在の.オブジェクト番号 * 2, 2 );

                    if( "00" == オブジェクト記述 )
                        continue;   // 00 はスキップ。


                    int オブジェクト値 = 0;

                    #region " オブジェクト値を取得。"
                    //----------------
                    if( 0x03 == 現在の.チャンネル番号 )
                    {
                        // (A) ch03 (BPM) のみ 16進数2桁表記
                        if( !_16進数2桁の文字列を数値に変換して返す( オブジェクト記述, out オブジェクト値 ) )
                        {
                            Debug.WriteLine( $"オブジェクトの記述に不正があります。[{現在の.行番号}行目]" );
                            return;
                        }
                    }
                    else
                    {
                        // (B) その他は 36進数2桁表記
                        if( !_36進数2桁の文字列を数値に変換して返す( オブジェクト記述, out オブジェクト値 ) )
                        {
                            Debug.WriteLine( $"オブジェクトの記述に不正があります。[{現在の.行番号}行目]" );
                            return;
                        }
                    }
                    //----------------
                    #endregion


                    チップ chip;

                    #region " チップを作成する。"
                    //----------------
                    {
                        // チャンネルに対応するチャンネルプロパティを取得。

                        if( !_DTXチャンネルプロパティマップ.TryGetValue( 現在の.チャンネル番号, out var チャンネルプロパティ ) )
                            continue;   // ないなら無視。


                        // チップを生成。

                        chip = new チップ() {
                            チップ種別 = チャンネルプロパティ.チップ種別,
                            チップサブID = オブジェクト値,
                            小節番号 = 現在の.小節番号,
                            小節解像度 = 現在の.小節解像度,
                            小節内位置 = (int) ( 現在の.小節解像度 * 現在の.オブジェクト番号 / 現在の.オブジェクト総数 ),
                            音量 = チップ.最大音量,  // DTX の音量は既定で最大
                            可視 = チャンネルプロパティ.可視,
                        };


                        // オプション処理があれば行う。

                        switch( 現在の.チャンネル番号 )
                        {
                            // BPM
                            case 0x03:
                                chip.BPM = オブジェクト値 + 現在の.BASEBPM;  // 引き当てないので、ここでBASEBPMを加算する。
                                break;

                            // 拡張BPM
                            case 0x08:
                                chip.BPM = 0.0; // あとで引き当てる。BASEBPMは引き当て時に加算する。
                                現在の.BPM参照マップ.Add( chip, オブジェクト値 );  // 引き当てを予約。
                                break;

                            // SE1～5
                            case int ch when( 0x61 <= ch && ch <= 0x65 ):
                                _WAVの多重再生を無効にする( オブジェクト値 );
                                break;

                            // チップ配置（ギター）
                            case int ch when( 0x20 <= ch && ch <= 0x27 ):
                                _WAVの多重再生を無効にする( オブジェクト値 );
                                break;

                            // チップ配置（ベース）
                            case int ch when( 0xA0 <= ch && ch <= 0xA7 ):
                                _WAVの多重再生を無効にする( オブジェクト値 );
                                break;

                            // 空打ち（ドラム）
                            case 0xB1: 現在の.スコア.空打ちチップマップ[ レーン種別.HiHat ] = オブジェクト値; break;      // HiHat Open と共有
                            case 0xB2: 現在の.スコア.空打ちチップマップ[ レーン種別.Snare ] = オブジェクト値; break;
                            case 0xB3: 現在の.スコア.空打ちチップマップ[ レーン種別.Bass ] = オブジェクト値; break;
                            case 0xB4: 現在の.スコア.空打ちチップマップ[ レーン種別.Tom1 ] = オブジェクト値; break;
                            case 0xB5: 現在の.スコア.空打ちチップマップ[ レーン種別.Tom2 ] = オブジェクト値; break;
                            case 0xB6:
                                現在の.スコア.空打ちチップマップ[ レーン種別.RightCrash ] = オブジェクト値;               // ReftCrash と China は共有
                                現在の.スコア.空打ちチップマップ[ レーン種別.China ] = オブジェクト値;
                                break;
                            case 0xB7: 現在の.スコア.空打ちチップマップ[ レーン種別.Tom3 ] = オブジェクト値; break;
                            case 0xB8: 現在の.スコア.空打ちチップマップ[ レーン種別.HiHat ] = オブジェクト値; break;      // HiHat Close と共有
                            case 0xB9: 現在の.スコア.空打ちチップマップ[ レーン種別.Ride ] = オブジェクト値; break;
                            case 0xBC:
                                現在の.スコア.空打ちチップマップ[ レーン種別.LeftCrash ] = オブジェクト値;                // LeftCrash と Splash は共有
                                現在の.スコア.空打ちチップマップ[ レーン種別.Splash ] = オブジェクト値;
                                break;
                        }

                        // ローカル関数
                        void _WAVの多重再生を無効にする( int WAV番号 )
                        {
                            var dicWAV = 現在の.スコア.WAVリスト;

                            if( dicWAV.ContainsKey( WAV番号 ) )
                                dicWAV[ WAV番号 ] = (dicWAV[ WAV番号 ].ファイルパス, 多重再生する: false);
                        }
                    }
                    //----------------
                    #endregion


                    現在の.スコア.チップリスト.Add( chip );
                }
            }

            internal static bool _DTX仕様の実数を取得する( string 文字列, out double 数値 )
            {
                // DTX仕様の実数の定義（カルチャ非依存）
                // --> 仕様の詳細はテストメソッドを参照のこと。

                const string 小数点文字s = ".,";                 // '.' の他に ',' も使える。
                const string 任意の桁区切り文字 = @"[\.,' ]";    // 小数点文字と被ってる文字もあるので注意。

                int 小数点の位置 = 文字列.LastIndexOfAny( 小数点文字s.ToCharArray() );    // 小数点文字s のうち、一番最後に現れた箇所。（DTX仕様）

                string 整数部;
                string 小数部;

                // 整数部と小数部に分けて取得し、それぞれから桁区切り文字を除去する。
                if( -1 == 小数点の位置 )
                {
                    // (A) 小数点がない場合
                    整数部 = Regex.Replace( 文字列, 任意の桁区切り文字, "" );
                    小数部 = "";
                }
                else
                {
                    // (B) 小数点がある場合
                    整数部 = Regex.Replace( 文字列.Substring( 0, 小数点の位置 ), 任意の桁区切り文字, "" );
                    小数部 = Regex.Replace( 文字列.Substring( 小数点の位置 + 1 ), 任意の桁区切り文字, "" );
                }

                // 整数部＋小数点＋小数部 で CurrentCulture な実数文字列を作成し、double へ変換する。
                return double.TryParse( $"{整数部}{CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator}{小数部}", out 数値 );
            }
            internal static bool _16進数2桁の文字列を数値に変換して返す( string 文字列, out int 値 )
            {
                値 = 0;

                if( 2 > 文字列.Length )
                    return false;

                文字列 = 文字列.ToLower();

                int _10の位 = _16進数変換表.IndexOf( 文字列[ 0 ] );
                int _1の位 = _16進数変換表.IndexOf( 文字列[ 1 ] );

                if( -1 == _10の位 || -1 == _1の位 )
                    return false;

                値 = ( _10の位 * 16 ) + _1の位;

                return true;
            }
            internal static bool _36進数2桁の文字列を数値に変換して返す( string 文字列, out int 値 )
            {
                値 = 0;

                if( 2 > 文字列.Length )
                    return false;

                文字列 = 文字列.ToLower();

                int _10の位 = _36進数変換表.IndexOf( 文字列[ 0 ] );
                int _1の位 = _36進数変換表.IndexOf( 文字列[ 1 ] );

                if( -1 == _10の位 || -1 == _1の位 )
                    return false;

                値 = ( _10の位 * 36 ) + _1の位;

                return true;
            }
            internal static bool _小節番号とチャンネル番号を取得する( string コマンド, out int 小節番号, out int チャンネル番号 )
            {
                小節番号 = 0;
                チャンネル番号 = 0;

                if( 5 != コマンド.Length )
                    return false;   // 必ず5文字ちょうどであること。

                #region " 小節番号を取得する。"
                //----------------
                {
                    var 小節番号文字列 = コマンド.Substring( 0, 3 ).ToLower();   // 先頭 3 文字

                    int 百の位 = _36進数変換表.IndexOf( 小節番号文字列[ 0 ] );   // 0～Z（36進数1桁）
                    int 十の位 = _16進数変換表.IndexOf( 小節番号文字列[ 1 ] );   // 0～9（10進数1桁）
                    int 一の位 = _16進数変換表.IndexOf( 小節番号文字列[ 2 ] );   // 0～9（10進数1桁）

                    if( -1 == 百の位 || -1 == 十の位 || -1 == 一の位 )
                    {
                        //Trace.TraceWarning( $"小節番号が不正です。[{現在の.行番号}行]" );
                        return false;
                    }

                    小節番号 = ( 百の位 * 100 ) + ( 十の位 * 10 ) + 一の位;
                }
                //----------------
                #endregion

                #region " チャンネル番号を取得する。"
                //----------------
                {
                    var チャンネル文字列 = コマンド.Substring( 3, 2 ).ToLower();  // 後ろ 2 文字

                    switch( 現在の.データ種別 )
                    {
                        case データ種別.GDA:
                        case データ種別.G2D:
                            // 英数字2桁
                            if( !_GDAtoDTXチャンネルマップ.TryGetValue( チャンネル文字列.ToUpper(), out チャンネル番号 ) )
                            {
                                //Trace.TraceWarning( $"チャンネル番号が不正です。[{現在の.行番号}行]" );
                                return false;
                            }
                            break;

                        default:

                            // 16進数2桁
                            if( !_16進数2桁の文字列を数値に変換して返す( チャンネル文字列, out チャンネル番号 ) )
                            {
                                //Trace.TraceWarning( $"チャンネル番号が不正です。[{現在の.行番号}行]" );
                                return false;
                            }
                            break;
                    }
                }
                //----------------
                #endregion

                return true;
            }

            private static readonly string _16進数変換表 = "0123456789abcdef";  // めんどいので大文字は考慮しない。利用先で小文字に変換のこと。
            private static readonly string _36進数変換表 = "0123456789abcdefghijklmnopqrstuvwxyz";  // 同上。

            private static readonly Dictionary<string, (bool ヘッダである, Action 解析アクション)> _コマンドtoアクションマップ = new Dictionary<string, (bool, Action)> {
                #region " *** "
                //----------------
                // コマンド名,   ヘッダである,  解析アクション
                { "title",      ( true,   _コマンド_TITLE ) },
                { "artist",     ( true,   _コマンド_ARTIST ) },
                { "comment",    ( true,   _コマンド_COMMENT ) },
                { "path_wav",   ( true,   _コマンド_PATH_WAV ) },
                { "wav",        ( false,  _コマンド_WAVzz ) },
                { "pan",        ( false,  _コマンド_PANzz_WAVPANzz ) },
                { "wavpan",     ( false,  _コマンド_PANzz_WAVPANzz ) },
                { "volume",     ( false,  _コマンド_VOLUMEzz_WAVVOLzz ) },
                { "wavvol",     ( false,  _コマンド_VOLUMEzz_WAVVOLzz ) },
                { "basebpm",    ( true,   _コマンド_BASEBPM ) },
                { "bpm",        ( true,   _コマンド_BPM_BPMzz ) },
                { "dlevel",     ( true,   _コマンド_DLEVEL_PLAYLEVEL ) },
                { "playlevel",  ( true,   _コマンド_DLEVEL_PLAYLEVEL ) },
                { "preview",    ( true,   _コマンド_PREVIEW ) },
                { "preimage",   ( true,   _コマンド_PREIMAGE ) },
                { "premovie",   ( true,   _コマンド_PREMOVIE ) },
                { "avi",        ( true,   _コマンド_AVIzz ) },
                //----------------
                #endregion
            };

            // ここにないGDA/G2Dチャンネルは未対応。
            private static readonly Dictionary<string, int> _GDAtoDTXチャンネルマップ = new Dictionary<string, int> {
                #region " *** "
                //----------------
                //GDAチャンネル, DTXチャンネル
                { "TC", 0x03 }, // BPM
                { "BL", 0x02 }, // BarLength; 小節長
                { "GS", 0x29 }, // ギター譜面スピード
                { "DS", 0x30 }, // ドラム譜面スピード
                { "FI", 0x53 }, // フィルイン
                { "HH", 0x11 }, // ハイハットクローズ
                { "SD", 0x12 }, // スネア
                { "BD", 0x13 }, // バスドラム
                { "HT", 0x14 }, // ハイタム
                { "LT", 0x15 }, // ロータム
                { "CY", 0x16 }, // 右シンバル
                { "G0", 0x20 }, // ギター OPEN
                { "G1", 0x21 }, // ギター --B
                { "G2", 0x22 }, // ギター -G-
                { "G3", 0x23 }, // ギター -GB
                { "G4", 0x24 }, // ギター R--
                { "G5", 0x25 }, // ギター R-B
                { "G6", 0x26 }, // ギター RG-
                { "G7", 0x27 }, // ギター RGB
                { "GW", 0x28 }, // ギター Wailing
                { "01", 0x01 }, // BGM
                { "02", 0x62 }, // SE 02
                { "03", 0x63 }, // SE 03
                { "04", 0x64 }, // SE 04
                { "05", 0x65 }, // SE 05
                { "06", 0x66 }, // SE 06
                { "07", 0x67 }, // SE 07
                { "08", 0x68 }, // SE 08
                { "09", 0x69 }, // SE 09
                { "0A", 0x70 }, // SE 10
                { "0B", 0x71 }, // SE 11
                { "0C", 0x72 }, // SE 12
                { "0D", 0x73 }, // SE 13
                { "0E", 0x74 }, // SE 14
                { "0F", 0x75 }, // SE 15
                { "10", 0x76 }, // SE 16
                { "11", 0x77 }, // SE 17
                { "12", 0x78 }, // SE 18
                { "13", 0x79 }, // SE 19
                { "14", 0x80 }, // SE 20
                { "15", 0x81 }, // SE 21
                { "16", 0x82 }, // SE 22
                { "17", 0x83 }, // SE 23
                { "18", 0x84 }, // SE 24
                { "19", 0x85 }, // SE 25
                { "1A", 0x86 }, // SE 26
                { "1B", 0x87 }, // SE 27
                { "1C", 0x88 }, // SE 28
                { "1D", 0x89 }, // SE 29
                { "1E", 0x90 }, // SE 30
                { "1F", 0x91 }, // SE 31
                { "20", 0x92 }, // SE 32
                { "B0", 0xA0 }, // ベース OPEN
                { "B1", 0xA1 }, // ベース --B
                { "B2", 0xA2 }, // ベース -G-
                { "B3", 0xA3 }, // ベース -GB
                { "B4", 0xA4 }, // ベース R--
                { "B5", 0xA5 }, // ベース R-B
                { "B6", 0xA6 }, // ベース RG-
                { "B7", 0xA7 }, // ベース RGB
                { "BW", 0xA8 }, // ベース Wailing
                //----------------
                #endregion
            };

            // ここにないDTXチャンネルは未対応。
            private static readonly Dictionary<int, (チップ種別 チップ種別, bool 可視, bool WAVを使う)> _DTXチャンネルプロパティマップ = new Dictionary<int, (チップ種別 チップ種別, bool 可視, bool WAVを使う)> {
                #region " *** "
                //----------------
                { 0x01, ( チップ種別.BGM,        false,  true  ) },  // バックコーラス(BGM)
                { 0x02, ( チップ種別.Unknown,    false,  false ) },	 // 小節長倍率 ... SSTFではチップじゃない。
                { 0x03, ( チップ種別.BPM,        false,  false ) },  // BPM
                { 0x08, ( チップ種別.BPM,        false,  false ) },  // 拡張BPM
                { 0x11, ( チップ種別.HiHat_Close,true,   true  ) },  // チップ配置（ドラム）・ハイハットクローズ
                { 0x12, ( チップ種別.Snare,      true,   true  ) },  // チップ配置（ドラム）・スネア
                { 0x13, ( チップ種別.Bass,       true,   true  ) },  // チップ配置（ドラム）・バス
                { 0x14, ( チップ種別.Tom1,       true,   true  ) },  // チップ配置（ドラム）・ハイタム
                { 0x15, ( チップ種別.Tom2,       true,   true  ) },  // チップ配置（ドラム）・ロータム
                { 0x16, ( チップ種別.RightCrash, true,   true  ) },  // チップ配置（ドラム）・右シンバル
                { 0x17, ( チップ種別.Tom3,       true,   true  ) },  // チップ配置（ドラム）・フロアタム
                { 0x18, ( チップ種別.HiHat_Open, true,   true  ) },  // チップ配置（ドラム）・ハイハットオープン
                { 0x19, ( チップ種別.Ride,       true,   true  ) },  // チップ配置（ドラム）・ライドシンバル
                { 0x1A, ( チップ種別.LeftCrash,  true,   true  ) },  // チップ配置（ドラム）・左シンバル
                { 0x1B, ( チップ種別.HiHat_Foot, true,   true  ) },  // チップ配置（ドラム）・左ペダル
                { 0x1C, ( チップ種別.LeftBass,   true,   true  ) },  // チップ配置（ドラム）・左バス
                { 0x20, ( チップ種別.GuitarAuto, false,  true  ) },  // チップ配置（ギター）・OPEN
                { 0x21, ( チップ種別.GuitarAuto, false,  true  ) },  // チップ配置（ギター）・xxB
                { 0x22, ( チップ種別.GuitarAuto, false,  true  ) },  // チップ配置（ギター）・xGx
                { 0x23, ( チップ種別.GuitarAuto, false,  true  ) },  // チップ配置（ギター）・xGB
                { 0x24, ( チップ種別.GuitarAuto, false,  true  ) },  // チップ配置（ギター）・Rxx
                { 0x25, ( チップ種別.GuitarAuto, false,  true  ) },  // チップ配置（ギター）・RxB
                { 0x26, ( チップ種別.GuitarAuto, false,  true  ) },  // チップ配置（ギター）・RGx
                { 0x27, ( チップ種別.GuitarAuto, false,  true  ) },  // チップ配置（ギター）・RGB
                { 0x50, ( チップ種別.小節線,     true,   false ) },  // 小節線
                { 0x51, ( チップ種別.拍線,       true,   false ) },  // 拍線
                { 0x54, ( チップ種別.背景動画,   false,  false ) },  // 動画
                { 0x5A, ( チップ種別.背景動画,   false,  false ) },  // 動画（全画面）
                { 0x61, ( チップ種別.SE1,        false,  true  ) },  // SE1
                { 0x62, ( チップ種別.SE2,        false,  true  ) },  // SE2
                { 0x63, ( チップ種別.SE3,        false,  true  ) },  // SE3
                { 0x64, ( チップ種別.SE4,        false,  true  ) },  // SE4
                { 0x65, ( チップ種別.SE5,        false,  true  ) },  // SE5
                { 0xA0, ( チップ種別.BassAuto,   false,  true  ) },  // チップ配置（ベース）・OPEN
                { 0xA1, ( チップ種別.BassAuto,   false,  true  ) },  // チップ配置（ベース）・xxB
                { 0xA2, ( チップ種別.BassAuto,   false,  true  ) },  // チップ配置（ベース）・xGx
                { 0xA3, ( チップ種別.BassAuto,   false,  true  ) },  // チップ配置（ベース）・xGB
                { 0xA4, ( チップ種別.BassAuto,   false,  true  ) },  // チップ配置（ベース）・Rxx
                { 0xA5, ( チップ種別.BassAuto,   false,  true  ) },  // チップ配置（ベース）・RxB
                { 0xA6, ( チップ種別.BassAuto,   false,  true  ) },  // チップ配置（ベース）・RGx
                { 0xA7, ( チップ種別.BassAuto,   false,  true  ) },  // チップ配置（ベース）・RGB
                { 0xC2, ( チップ種別.Unknown,    false,  false ) },	 // 拍線・小節線表示指定 ... SSTFではチップじゃないが、後処理で使用する。
                //----------------
                #endregion
            };
        }
    }
}
