using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SSTFormat.v004
{
    public partial class スコア
    {
        /// <summary>
        ///     SSTFフォーマットのファイルまたはテキストから <see cref="スコア"/> インスタンスを生成するためのクラス。
        /// </summary>
        /// <remarks>
        ///     テストプロジェクトに対しては InternalsVisibleTo 属性(AssemblyInfo.cs参照))により internal メソッドを可視としているため、
        ///     テスト対象のメソッドは、本来 private でも internal として宣言している。
        /// </remarks>
        public static class SSTF
        {
            /// <summary>
            ///		ファイルからSSTFデータを読み込み、スコアを生成して返す。
            ///		読み込みに失敗した場合は、何らかの例外を発出する。
            /// </summary>
            public static スコア ファイルから生成する( string SSTFファイルの絶対パス, bool ヘッダだけ = false )
            {
                // ファイルのSSTFバージョンを確認。

                string 先頭行;
                using( var sr = new StreamReader( SSTFファイルの絶対パス, Encoding.UTF8 ) )  // SSTF は UTF-8
                    先頭行 = sr.ReadLine();

                var SSTFバージョン = _行にSSTFVersionがあるなら解析して返す( 先頭行 ) ?? new Version( 1, 0, 0, 0 );  // 既定値


                // ファイルのSSTFバージョンに応じた方法でスコアを生成する。

                スコア score = null;

                // ファイルの内容を一気読み。
                string 全入力文字列 = null;
                using( var sr = new StreamReader( SSTFファイルの絶対パス ) )
                    全入力文字列 = sr.ReadToEnd();

                // 読み込んだ内容でスコアを生成する。
                score = _全行解析する( ref 全入力文字列, ヘッダだけ );

                // ファイルから読み込んだ場合のみ、このメンバが有効。
                score.譜面ファイルの絶対パス = SSTFファイルの絶対パス;


                // 後処理。

                if( !( ヘッダだけ ) )
                {
                    スコア._スコア読み込み時の後処理を行う( score );
                }

                // SSTFの場合、空うちマップは空とする。
                score.空打ちチップマップ.Clear();

                return score;
            }

            /// <summary>
            ///     SSTFフォーマットのテキストデータを含んだ１つの文字列から、スコアを生成して返す。
            ///		読み込みに失敗した場合は、何らかの例外を発出する。
            /// </summary>
            public static スコア 文字列から生成する( string 全入力文字列, bool ヘッダだけ = false )
            {
                // データのSSTFバージョンを確認。

                string 先頭行;
                using( var sr = new StringReader( 全入力文字列 ) )
                    先頭行 = sr.ReadLine();

                var SSTFバージョン = _行にSSTFVersionがあるなら解析して返す( 先頭行 ) ?? new Version( 1, 0, 0, 0 );  // 既定値


                // データのSSTFバージョンに応じた方法でスコアを生成する。
                スコア score = null;
                score = _全行解析する( ref 全入力文字列, ヘッダだけ );
                score.譜面ファイルの絶対パス = null;   // ファイルから読み込んだ場合のみ、このメンバが有効。

                if( !( ヘッダだけ ) )
                {
                    スコア._スコア読み込み時の後処理を行う( score );
                }

                // SSTFの場合、空うちマップは空とする。
                score.空打ちチップマップ.Clear();

                return score;
            }

            /// <summary>
            ///		現在の スコア の内容をデータファイル（*.sstf）に書き出す。
            ///		小節線、拍線、Unknown チップは出力しない。
            ///		失敗時は何らかの例外を発出する。
            /// </summary>
            public static void 出力する( スコア score, Stream 出力先, string 追加ヘッダ文 = null )
            {
                using( var sw = new StreamWriter( 出力先, Encoding.UTF8 ) )
                {
                    // SSTFバージョンの出力
                    sw.WriteLine( $"# SSTFVersion {SSTFVERSION.ToString()}" );

                    // 追加ヘッダの出力（あれば）
                    if( !( string.IsNullOrEmpty( 追加ヘッダ文 ) ) )
                    {
                        sw.WriteLine( $"{追加ヘッダ文}" );    // ヘッダ文に "{...}" が入ってても大丈夫なように、$"{...}" で囲む。
                        sw.WriteLine( "" );
                    }

                    _ヘッダ行を出力する( score, sw );

                    _チップ記述行を出力する( score, sw );

                    _小節メモ行を出力する( score, sw );

                    sw.Close();
                }
            }



            // 行解析


            #region " 解析に使う状態変数。(static) "
            //----------------
            private static class 現在の
            {
                public static スコア スコア;
                public static int 行番号;
                public static int 小節番号;
                public static int 小節解像度;
                public static チップ種別 チップ種別;
                public static bool 可視;

                public static void 状態をリセットする()
                {
                    スコア = null;
                    行番号 = 0;
                    小節番号 = 0;
                    小節解像度 = 384;
                    チップ種別 = チップ種別.Unknown;
                    可視 = true;
                }
            }
            //----------------
            #endregion

            internal static スコア _全行解析する( ref string 全入力文字列, bool ヘッダだけ = false )
            {
                現在の.状態をリセットする();
                現在の.スコア = new スコア();

                using( var sr = new StringReader( 全入力文字列 ) )
                {
                    // すべての行について……
                    string 行;
                    while( ( 行 = sr.ReadLine() ) != null )  // EOF なら null
                    {
                        現在の.行番号++;

                        // 行の前処理

                        #region " 改行とTABを空白文字に変換し、先頭末尾の空白を削除する。"
                        //----------------
                        行 = 行.Replace( Environment.NewLine, " " );
                        行 = 行.Replace( '\t', ' ' );
                        行 = 行.Trim();
                        //----------------
                        #endregion

                        #region " 行中の '#' 以降はコメントとして除外する。"
                        //----------------
                        {
                            int 区切り位置 = 行.IndexOf( '#' );
                            if( 0 <= 区切り位置 )
                            {
                                行 = 行[ ..区切り位置 ];
                                行 = 行.Trim();
                            }
                        }
                        //----------------
                        #endregion

                        if( string.IsNullOrEmpty( 行 ) )
                            continue;   // 空行

                        // 行の解析

                        if( _行をヘッダ行と想定して解析する( ref 行 ) )
                            continue;

                        if( !( ヘッダだけ ) )
                        {
                            if( _行を小節メモ行として解析する( ref 行 ) )
                                continue;

                            if( _行をチップ記述行として解析する( ref 行 ) )
                                continue;
                        }
                    }
                }

                if( !( ヘッダだけ ) )
                {
                    #region " 拍線を追加する。"
                    //-----------------
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
                    //-----------------
                    #endregion

                    #region " 小節線を追加する。"
                    //-----------------
                    最大小節番号 = 現在の.スコア.最大小節番号を返す();

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
                    //-----------------
                    #endregion
                }

                var score = 現在の.スコア;
                現在の.状態をリセットする();
                return score;
            }

            /// <returns>
            ///		行をヘッダとして処理したなら true 、該当しないまたはエラーが発生したときは false を返す。
            ///	</returns>
            private static bool _行をヘッダ行と想定して解析する( ref string 行 )
            {
                #region " Title "
                //-----------------
                if( 行.StartsWith( "title", StringComparison.OrdinalIgnoreCase ) )
                {
                    string[] items = 行.Split( '=' );

                    if( 2 != items.Length )
                    {
                        Trace.TraceError( $"Title の書式が不正です。スキップします。[{現在の.行番号}行目]" );
                        return false;
                    }

                    現在の.スコア.曲名 = items[ 1 ].Trim();

                    return true;
                }
                //-----------------
                #endregion
                #region " Artist "
                //----------------
                if( 行.StartsWith( "artist", StringComparison.OrdinalIgnoreCase ) )
                {
                    string[] items = 行.Split( '=' );

                    if( 2 != items.Length )
                    {
                        Trace.TraceError( $"Artist の書式が不正です。スキップします。[{現在の.行番号}行目]" );
                        return false;
                    }

                    現在の.スコア.アーティスト名 = items[ 1 ].Trim();

                    return true;
                }
                //----------------
                #endregion
                #region " Description "
                //-----------------
                if( 行.StartsWith( "description", StringComparison.OrdinalIgnoreCase ) )
                {
                    string[] items = 行.Split( '=' );

                    if( 2 != items.Length )
                    {
                        Trace.TraceError( $"Description の書式が不正です。スキップします。[{現在の.行番号}行目]" );
                        return false;
                    }

                    // ２文字のリテラル "\n" は改行に復号。
                    現在の.スコア.説明文 = items[ 1 ].Trim().Replace( @"\n", Environment.NewLine );

                    return true;
                }
                //-----------------
                #endregion
                #region " SoundDevice.Delay "
                //-----------------
                if( 行.StartsWith( "sounddevice.delay", StringComparison.OrdinalIgnoreCase ) )
                {
                    string[] items = 行.Split( '=' );

                    if( 2 != items.Length )
                    {
                        Trace.TraceError( $"SoundDevice.Delay の書式が不正です。スキップします。[{現在の.行番号}行目]" );
                        return false;
                    }

                    // ２文字のリテラル "\n" は改行に復号。
                    if( float.TryParse( items[ 1 ].Trim().Replace( @"\n", Environment.NewLine ), out float value ) )
                        現在の.スコア.サウンドデバイス遅延ms = value;

                    return true;
                }
                //-----------------
                #endregion
                #region " Level "
                //----------------
                if( 行.StartsWith( "level", StringComparison.OrdinalIgnoreCase ) )
                {
                    string[] items = 行.Split( '=' );

                    if( 2 != items.Length )
                    {
                        Trace.TraceError( $"Level の書式が不正です。スキップします。[{現在の.行番号}行目]" );
                        return false;
                    }

                    try
                    {
                        現在の.スコア.難易度 = Math.Clamp( double.Parse( items[ 1 ].Trim() ), min: 0.00, max: 9.99 );
                    }
                    catch
                    {
                        Trace.TraceError( $"Level の右辺が不正です。スキップします。[{現在の.行番号}行目]" );
                        return false;
                    }

                    return true;
                }
                //----------------
                #endregion
                #region " BGV "
                //----------------
                if( 行.StartsWith( "BGV", StringComparison.OrdinalIgnoreCase ) )
                {
                    string[] items = 行.Split( '=' );

                    if( 2 != items.Length )
                    {
                        Trace.TraceError( $"BGV の書式が不正です。スキップします。[{現在の.行番号}行目]" );
                        return false;
                    }

                    現在の.スコア.BGVファイル名 = items[ 1 ].Trim();
                    現在の.スコア.AVIリスト[ 1 ] = 現在の.スコア.BGVファイル名;            // #AVI01 固定。あれば上書き、なければ追加

                    return true;
                }
                //----------------
                #endregion
                #region " BGM "
                //----------------
                if( 行.StartsWith( "BGM", StringComparison.OrdinalIgnoreCase ) )
                {
                    string[] items = 行.Split( '=' );

                    if( 2 != items.Length )
                    {
                        Trace.TraceError( $"BGM の書式が不正です。スキップします。[{現在の.行番号}行目]" );
                        return false;
                    }

                    現在の.スコア.BGMファイル名 = items[ 1 ].Trim();

                    現在の.スコア.WAVリスト[ 1 ] = new WAV情報 {      // #WAV01 固定。あれば上書き、なければ追加
                        ファイルパス = 現在の.スコア.BGMファイル名,
                        多重再生する = false,
                        BGMである = true,
                    };

                    return true;
                }
                //----------------
                #endregion
                #region " Preview.Sound "
                //----------------
                if( 行.StartsWith( "preview.sound", StringComparison.OrdinalIgnoreCase ) )
                {
                    string[] items = 行.Split( '=' );

                    if( 2 != items.Length )
                    {
                        Trace.TraceError( $"Preview.Sound の書式が不正です。スキップします。[{現在の.行番号}行目]" );
                        return false;
                    }

                    現在の.スコア.プレビュー音声ファイル名 = items[ 1 ].Trim();

                    return true;
                }
                //----------------
                #endregion
                #region " Preview.Image "
                //----------------
                if( 行.StartsWith( "preview.image", StringComparison.OrdinalIgnoreCase ) )
                {
                    string[] items = 行.Split( '=' );

                    if( 2 != items.Length )
                    {
                        Trace.TraceError( $"Preview.Image の書式が不正です。スキップします。[{現在の.行番号}行目]" );
                        return false;
                    }

                    現在の.スコア.プレビュー画像ファイル名 = items[ 1 ].Trim();

                    return true;
                }
                //----------------
                #endregion
                #region " Preview.Movie "
                //----------------
                if( 行.StartsWith( "preview.movie", StringComparison.OrdinalIgnoreCase ) )
                {
                    string[] items = 行.Split( '=' );

                    if( 2 != items.Length )
                    {
                        Trace.TraceError( $"Preview.Movie の書式が不正です。スキップします。[{現在の.行番号}行目]" );
                        return false;
                    }

                    現在の.スコア.プレビュー動画ファイル名 = items[ 1 ].Trim();

                    return true;
                }
                //----------------
                #endregion

                return false;   // 該当なし
            }

            /// <returns>
            ///		行を小節メモ行として処理したなら true 、該当しないまたはエラーが発生したときは false を返す。
            ///	</returns>
            private static bool _行を小節メモ行として解析する( ref string 行 )
            {
                if( 行.StartsWith( "partmemo", StringComparison.OrdinalIgnoreCase ) )
                {
                    #region " '=' 以前を除去する。"
                    //-----------------
                    int 等号位置 = 行.IndexOf( '=' );

                    if( 0 >= 等号位置 ) // 0 or -1
                    {
                        Trace.TraceError( $"PartMemo の書式が不正です。スキップします。[{現在の.行番号}]行目]" );
                        return false;
                    }

                    行 = 行[ ( 等号位置 + 1 ).. ].Trim();

                    if( string.IsNullOrEmpty( 行 ) )
                    {
                        Trace.TraceError( $"PartMemo の書式が不正です。スキップします。[{現在の.行番号}]行目]" );
                        return false;
                    }
                    //-----------------
                    #endregion
                    #region " カンマ位置を取得する。"
                    //-----------------
                    int カンマ位置 = 行.IndexOf( ',' );

                    if( 0 >= カンマ位置 )
                    {
                        Trace.TraceError( $"PartMemo の書式が不正です。スキップします。[{現在の.行番号}]行目]" );
                        return false;
                    }
                    //-----------------
                    #endregion
                    #region " 小節番号を取得する。"
                    //-----------------
                    string 小節番号文字列 = 行[ 0..カンマ位置 ];

                    if( !( int.TryParse( 小節番号文字列, out int 小節番号 ) || ( 0 > 小節番号 ) ) )
                    {
                        Trace.TraceError( $"PartMemo の小節番号が不正です。スキップします。[{現在の.行番号}]行目]" );
                        return false;
                    }
                    //-----------------
                    #endregion
                    #region " メモを取得する。"
                    //-----------------
                    string メモ = 行[ ( カンマ位置 + 1 ).. ];

                    // ２文字のリテラル文字列 "\n" は改行に復号。
                    メモ = メモ.Replace( @"\n", Environment.NewLine );
                    //-----------------
                    #endregion
                    #region " メモが空文字列でないなら メモリスト に登録する。"
                    //-----------------
                    if( !( string.IsNullOrEmpty( メモ ) ) )
                    {
                        現在の.スコア.小節メモリスト.Add( 小節番号, メモ );

                        //現在の.スコア.チップリスト.Add(
                        //    new チップ() {
                        //        チップ種別 = チップ種別.小節メモ,
                        //        小節番号 = 小節番号,
                        //        小節内位置 = 0,
                        //        小節解像度 = 1,
                        //    } );
                        // --> チップは廃止。（不要）
                    }
                    //-----------------
                    #endregion

                    return true;
                }

                return false;   // 該当なし
            }

            /// <returns>
            ///		常に true を返す。
            ///	</returns>
            private static bool _行をチップ記述行として解析する( ref string 行 )
            {
                // 行を区切り文字でトークンに分割。
                string[] tokens = 行.Split( new char[] { ';', ':' } );

                // すべてのトークンについて……
                foreach( string token in tokens )
                {
                    string コマンド;
                    string パラメータ;

                    #region " トークンを区切り文字 '=' で コマンド と パラメータ に分割し、それぞれの先頭末尾の空白を削除する。"
                    //-----------------
                    string[] items = token.Split( '=' );

                    if( 2 != items.Length )
                    {
                        if( 0 == token.Trim().Length )  // 空文字列（行末など）は不正じゃない。
                            continue;

                        Trace.TraceError( $"コマンドとパラメータの記述書式が不正です。このコマンドをスキップします。[{現在の.行番号}行目]" );
                        continue;
                    }

                    コマンド = items[ 0 ].Trim();
                    パラメータ = items[ 1 ].Trim();
                    //-----------------
                    #endregion

                    switch( コマンド.ToLower() )
                    {
                        case "part":
                            #region " Part（小節番号指定）コマンド "
                            //-----------------
                            {
                                // 小節番号を取得・設定。

                                string 小節番号文字列 = _指定された文字列の先頭から数字文字列を取り出す( ref パラメータ );

                                if( string.IsNullOrEmpty( 小節番号文字列 ) )
                                {
                                    Trace.TraceError( $"Part（小節番号）コマンドに小節番号の記述がありません。このコマンドをスキップします。[{現在の.行番号}行目]" );
                                    continue;
                                }
                                if( !( int.TryParse( 小節番号文字列, out int 小節番号 ) ) )
                                {
                                    Trace.TraceError( $"Part（小節番号）コマンドの小節番号が不正です。このコマンドをスキップします。[{現在の.行番号}行目]" );
                                    continue;
                                }
                                if( 0 > 小節番号 )
                                {
                                    Trace.TraceError( $"Part（小節番号）コマンドの小節番号が負数です。このコマンドをスキップします。[{現在の.行番号}行目]" );
                                    continue;
                                }

                                現在の.小節番号 = 小節番号;


                                // Part の属性があれば取得する。

                                while( 0 < パラメータ.Length )
                                {
                                    char 属性ID = char.ToLower( パラメータ[ 0 ] );

                                    if( 属性ID == 's' )
                                    {
                                        #region " 小節長倍率(>0) → list小節長倍率 "
                                        //-----------------
                                        パラメータ = パラメータ[ 1.. ].Trim();

                                        string 小節長倍率文字列 = _指定された文字列の先頭から数字文字列を取り出す( ref パラメータ );

                                        if( string.IsNullOrEmpty( 小節長倍率文字列 ) )
                                        {
                                            Trace.TraceError( $"Part（小節番号）コマンドに小節長倍率の記述がありません。この属性をスキップします。[{現在の.行番号}行目]" );
                                            continue;
                                        }
                                        パラメータ = パラメータ.Trim();

                                        if( !( double.TryParse( 小節長倍率文字列, out double 小節長倍率 ) ) )
                                        {
                                            Trace.TraceError( $"Part（小節番号）コマンドの小節長倍率が不正です。この属性をスキップします。[{現在の.行番号}行目]" );
                                            continue;
                                        }
                                        if( 0.0 >= 小節長倍率 )
                                        {
                                            Trace.TraceError( $"Part（小節番号）コマンドの小節長倍率が 0.0 または負数です。この属性をスキップします。[{現在の.行番号}行目]" );
                                            continue;
                                        }

                                        // 小節長倍率辞書に追加 or 上書き更新。
                                        現在の.スコア.小節長倍率を設定する( 現在の.小節番号, 小節長倍率 );

                                        continue;
                                        //-----------------
                                        #endregion
                                    }
                                }
                            }
                            //-----------------
                            #endregion
                            break;

                        case "lane":
                            #region " Lane（レーン指定）コマンド（チップ種別の仮決め）"
                            //-----------------
                            {
                                var lane = パラメータ.ToLower();

                                if( _レーンプロパティ.TryGetValue( lane, out var プロパティ ) )
                                {
                                    現在の.チップ種別 = プロパティ.チップ種別;
                                    現在の.可視 = プロパティ.可視;
                                }
                                else
                                {
                                    Trace.TraceError( $"Lane（レーン指定）コマンドのパラメータ記述 '{パラメータ}' が不正です。このコマンドをスキップします。[{現在の.行番号}行目]" );
                                }
                            }
                            //-----------------
                            #endregion
                            break;

                        case "resolution":
                            #region " Resolution（小節解像度指定）コマンド "
                            //-----------------
                            {
                                if( !( int.TryParse( パラメータ, out int 解像度 ) ) )
                                {
                                    Trace.TraceError( $"Resolution（小節解像度指定）コマンドの解像度が不正です。このコマンドをスキップします。[{現在の.行番号}行目]" );
                                    continue;
                                }
                                if( 1 > 解像度 )
                                {
                                    Trace.TraceError( $"Resolution（小節解像度指定）コマンドの解像度は 1 以上でなければなりません。このコマンドをスキップします。[{現在の.行番号}行目]" );
                                    continue;
                                }

                                現在の.小節解像度 = 解像度;
                            }
                            //-----------------
                            #endregion
                            break;

                        case "chips":
                            #region " Chips（チップ指定）コマンド "
                            //-----------------
                            // パラメータを区切り文字 ',' でチップトークンに分割。
                            string[] chipTokens = パラメータ.Split( ',' );

                            // すべてのチップトークンについて……
                            for( int i = 0; i < chipTokens.Length; i++ )
                            {
                                chipTokens[ i ].Trim();

                                if( 0 == chipTokens[ i ].Length )
                                    continue;

                                #region " チップを生成する。"
                                //-----------------
                                var chip = new チップ() {
                                    小節番号 = 現在の.小節番号,
                                    チップ種別 = 現在の.チップ種別,
                                    チップサブID = ( 現在の.チップ種別 == チップ種別.背景動画 || 現在の.チップ種別 == チップ種別.BGM ) ? 1 : 0,    // AVI, WAV なら 01 固定。
                                    小節解像度 = 現在の.小節解像度,
                                    音量 = チップ.既定音量,
                                    可視 = 現在の.可視,
                                };
                                //-----------------
                                #endregion

                                #region " チップ位置を取得する。"
                                //-----------------
                                {
                                    string 位置番号文字列 = _指定された文字列の先頭から数字文字列を取り出す( ref chipTokens[ i ] );
                                    chipTokens[ i ].Trim();

                                    // 文法チェック。
                                    if( string.IsNullOrEmpty( 位置番号文字列 ) )
                                    {
                                        Trace.TraceError( $"チップの位置指定の記述がありません。このチップをスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                        continue;
                                    }

                                    // 位置を取得。
                                    if( false == int.TryParse( 位置番号文字列, out int チップ位置 ) )
                                    {
                                        Trace.TraceError( $"チップの位置指定の記述が不正です。このチップをスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                        continue;
                                    }

                                    // 値域チェック。
                                    if( ( 0 > チップ位置 ) || ( チップ位置 >= 現在の.小節解像度 ) )
                                    {
                                        Trace.TraceError( $"チップの位置が負数であるか解像度(Resolution)以上の値になっています。このチップをスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                        continue;
                                    }

                                    chip.小節内位置 = チップ位置;
                                }
                                //-----------------
                                #endregion

                                #region " 共通属性・レーン別属性があれば取得する。"
                                //-----------------
                                while( 0 < chipTokens[ i ].Length )
                                {
                                    var 属性ID = char.ToLower( chipTokens[ i ][ 0 ] );

                                    // 共通属性

                                    if( 'v' == 属性ID )
                                    {
                                        #region " 音量 "
                                        //----------------
                                        chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                        string 音量文字列 = _指定された文字列の先頭から数字文字列を取り出す( ref chipTokens[ i ] );
                                        chipTokens[ i ].Trim();

                                        // 文法チェック。
                                        if( string.IsNullOrEmpty( 音量文字列 ) )
                                        {
                                            Trace.TraceError( $"チップの音量指定の記述がありません。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                            continue;
                                        }

                                        // チップ音量の取得。
                                        if( !( int.TryParse( 音量文字列, out int チップ音量 ) ) )
                                        {
                                            Trace.TraceError( $"チップの音量指定の記述が不正です。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                            continue;
                                        }

                                        // 値域チェック。
                                        if( ( 1 > チップ音量 ) || ( チップ音量 > チップ.最大音量 ) )
                                        {
                                            Trace.TraceError( $"チップの音量が適正範囲（1～{チップ.最大音量}）を超えています。このチップをスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                            continue;
                                        }

                                        chip.音量 = チップ音量;

                                        continue;
                                        //----------------
                                        #endregion
                                    }

                                    // レーン別属性

                                    switch( 現在の.チップ種別 )
                                    {
                                        case チップ種別.LeftCrash:
                                            if( 'm' == 属性ID )
                                            {
                                                #region " ミュート "
                                                //----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                chip.チップ種別 = チップ種別.LeftCymbal_Mute;
                                                //----------------
                                                #endregion
                                            }
                                            else
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        case チップ種別.Ride:
                                        case チップ種別.Ride_Cup:
                                            if( 'c' == 属性ID )
                                            {
                                                #region " Ride.カップ "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                chip.チップ種別 = チップ種別.Ride_Cup;
                                                //-----------------
                                                #endregion
                                            }
                                            else
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        case チップ種別.China:
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        case チップ種別.Splash:
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        case チップ種別.HiHat_Close:
                                        case チップ種別.HiHat_HalfOpen:
                                        case チップ種別.HiHat_Open:
                                        case チップ種別.HiHat_Foot:
                                            if( 'o' == 属性ID )
                                            {
                                                #region " HiHat.オープン "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                chip.チップ種別 = チップ種別.HiHat_Open;
                                                //-----------------
                                                #endregion
                                            }
                                            else if( 'h' == 属性ID )
                                            {
                                                #region " HiHat.ハーフオープン "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                chip.チップ種別 = チップ種別.HiHat_HalfOpen;
                                                //-----------------
                                                #endregion
                                            }
                                            else if( 'c' == 属性ID )
                                            {
                                                #region " HiHat.クローズ "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                chip.チップ種別 = チップ種別.HiHat_Close;
                                                //-----------------
                                                #endregion
                                            }
                                            else if( 'f' == 属性ID )
                                            {
                                                #region " HiHat.フットスプラッシュ "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                chip.チップ種別 = チップ種別.HiHat_Foot;
                                                //-----------------
                                                #endregion
                                            }
                                            else
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        case チップ種別.Snare:
                                        case チップ種別.Snare_ClosedRim:
                                        case チップ種別.Snare_OpenRim:
                                        case チップ種別.Snare_Ghost:
                                            if( 'o' == 属性ID )
                                            {
                                                #region " Snare.オープンリム "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                chip.チップ種別 = チップ種別.Snare_OpenRim;
                                                //-----------------
                                                #endregion
                                            }
                                            else if( 'c' == 属性ID )
                                            {
                                                #region " Snare.クローズドリム "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                chip.チップ種別 = チップ種別.Snare_ClosedRim;
                                                //-----------------
                                                #endregion
                                            }
                                            else if( 'g' == 属性ID )
                                            {
                                                #region " Snare.ゴースト "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                chip.チップ種別 = チップ種別.Snare_Ghost;
                                                //-----------------
                                                #endregion
                                            }
                                            else
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        case チップ種別.Bass:
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        case チップ種別.Tom1:
                                        case チップ種別.Tom1_Rim:
                                            if( 'r' == 属性ID )
                                            {
                                                #region " Tom1.リム "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                chip.チップ種別 = チップ種別.Tom1_Rim;
                                                //-----------------
                                                #endregion
                                            }
                                            else
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        case チップ種別.Tom2:
                                        case チップ種別.Tom2_Rim:
                                            if( 'r' == 属性ID )
                                            {
                                                #region " Tom2.リム "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                chip.チップ種別 = チップ種別.Tom2_Rim;
                                                //-----------------
                                                #endregion
                                            }
                                            else
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        case チップ種別.Tom3:
                                        case チップ種別.Tom3_Rim:
                                            if( 'r' == 属性ID )
                                            {
                                                #region " Tom3.リム "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                chip.チップ種別 = チップ種別.Tom3_Rim;
                                                //-----------------
                                                #endregion
                                            }
                                            else
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        case チップ種別.RightCrash:
                                            if( 'm' == 属性ID )
                                            {
                                                #region " ミュート "
                                                //----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                chip.チップ種別 = チップ種別.RightCymbal_Mute;
                                                //----------------
                                                #endregion
                                            }
                                            else
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        case チップ種別.BPM:
                                            if( 'b' == 属性ID )
                                            {
                                                #region " BPM値 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                string BPM文字列 = _指定された文字列の先頭から数字文字列を取り出す( ref chipTokens[ i ] );
                                                chipTokens[ i ].Trim();

                                                if( string.IsNullOrEmpty( BPM文字列 ) )
                                                {
                                                    Trace.TraceError( $"BPM数値の記述がありません。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                                    continue;
                                                }
                                                if( false == double.TryParse( BPM文字列, out double BPM ) || ( 0.0 >= BPM ) )
                                                {
                                                    Trace.TraceError( $"BPM数値の記述が不正です。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                                    continue;
                                                }

                                                chip.BPM = BPM;
                                                //-----------------
                                                #endregion
                                            }
                                            else
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        case チップ種別.背景動画:
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;
                                    }

                                    #region " 未知の属性 "
                                    //-----------------
                                    chipTokens[ i ] = chipTokens[ i ][ 1.. ].Trim();
                                    Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                    //-----------------
                                    #endregion
                                }
                                //-----------------
                                #endregion

                                現在の.スコア.チップリスト.Add( chip );
                            }
                            //-----------------
                            #endregion
                            break;
                    }
                }

                return true;
            }

            /// <remarks>
            ///		取出文字列の先頭にある数字（小数点も有効）の連続した部分を取り出して、戻り値として返す。
            ///		また、取出文字列から取り出した数字文字列部分を除去した文字列を再度格納する。
            /// </remarks>
            private static string _指定された文字列の先頭から数字文字列を取り出す( ref string 取出文字列 )
            {
                // 数字が何桁続くか数える。
                int 桁数 = 0;
                while( ( 桁数 < 取出文字列.Length ) && ( char.IsDigit( 取出文字列[ 桁数 ] ) || 取出文字列[ 桁数 ] == '.' ) )
                    桁数++;

                if( 0 == 桁数 )
                    return "";

                // その桁数分を取り出して返す。
                string 数字文字列 = 取出文字列[ ..桁数 ];
                取出文字列 = ( 桁数 == 取出文字列.Length ) ? "" : 取出文字列[ 桁数.. ];

                return 数字文字列;
            }



            // 行出力


            private static void _ヘッダ行を出力する( スコア score, StreamWriter sw )
            {
                #region " Title "
                //----------------
                sw.WriteLine( "Title=" + ( ( string.IsNullOrEmpty( score.曲名 ) ) ? "(no title)" : score.曲名 ) );  // Title は必須
                //----------------
                #endregion
                #region " Artist "
                //----------------
                if( !string.IsNullOrEmpty( score.アーティスト名 ) )    // Artist は任意
                    sw.WriteLine( $"Artist=" + score.アーティスト名 );
                //----------------
                #endregion
                #region " Description "
                //----------------
                if( !string.IsNullOrEmpty( score.説明文 ) )    // Description は任意
                {
                    // 改行コードは、２文字のリテラル "\n" に置換。
                    sw.WriteLine( $"Description=" + score.説明文.Replace( Environment.NewLine, @"\n" ) );
                }
                //----------------
                #endregion
                #region " SoundDevice.Delay "
                //----------------
                sw.WriteLine( $"SoundDevice.Delay={score.サウンドデバイス遅延ms}" );
                //----------------
                #endregion
                #region " Level "
                //----------------
                sw.WriteLine( $"Level={score.難易度.ToString( "0.00" )}" );
                //----------------
                #endregion
                #region " BGV "
                //----------------
                if( !string.IsNullOrEmpty( score.BGVファイル名 ) )
                    sw.WriteLine( $"BGV=" + score.BGVファイル名 );
                //----------------
                #endregion
                #region " BGM "
                //----------------
                if( !string.IsNullOrEmpty( score.BGMファイル名 ) )
                    sw.WriteLine( $"BGM=" + score.BGMファイル名 );
                //----------------
                #endregion
                #region " Preview.Sound "
                //----------------
                if( !string.IsNullOrEmpty( score.プレビュー音声ファイル名 ) )
                    sw.WriteLine( $"Preview.Sound=" + score.プレビュー音声ファイル名 );
                //----------------
                #endregion
                #region " Preview.Image "
                //----------------
                if( !string.IsNullOrEmpty( score.プレビュー画像ファイル名 ) )
                    sw.WriteLine( $"Preview.Image=" + score.プレビュー画像ファイル名 );
                //----------------
                #endregion
                #region " Preview.Movie "
                //----------------
                if( !string.IsNullOrEmpty( score.プレビュー動画ファイル名 ) )
                    sw.WriteLine( $"Preview.Movie=" + score.プレビュー動画ファイル名 );
                //----------------
                #endregion

                sw.WriteLine( "" );
            }

            private static void _小節メモ行を出力する( スコア score, StreamWriter sw )
            {
                int 最大小節番号 = score.最大小節番号を返す();

                // 小節番号昇順に出力。

                for( int i = 0; i <= 最大小節番号; i++ )
                {
                    if( score.小節メモリスト.TryGetValue( i, out string メモ ) )
                    {
                        メモ = メモ.Replace( Environment.NewLine, @"\n" );  // 改行コードは、２文字のリテラル "\n" に置換。
                        sw.WriteLine( $"PartMemo = {i},{メモ}" );
                    }
                }

                sw.WriteLine( "" );
            }

            private static void _チップ記述行を出力する( スコア score, StreamWriter sw )
            {
                if( 0 == score.チップリスト.Count )
                    return;

                int 最終小節番号 = score.最大小節番号を返す();

                // すべての小節番号について……
                for( int 小節番号 = 0; 小節番号 <= 最終小節番号; 小節番号++ )
                {
                    var 現在の小節に存在するチップのレーン別リスト = new Dictionary<レーン種別, チップ[]>();

                    #region " 現在の小節に存在するチップのレーン別リストを作成する。"
                    //----------------
                    foreach( レーン種別 laneType in Enum.GetValues( typeof( レーン種別 ) ) )
                    {
                        if( laneType == レーン種別.Unknown ) // チップ記述対象外レーン
                            continue;

                        var chips = score.チップリスト.Where( ( chip ) => (
                            chip.小節番号 == 小節番号 &&
                            SSTFプロパティ.チップtoレーンマップ[ chip.チップ種別 ] == laneType &&
                            // チップ記述対象外チップ
                            chip.チップ種別 != チップ種別.SE1 && chip.チップ種別 != チップ種別.SE2 && chip.チップ種別 != チップ種別.SE3 &&
                            chip.チップ種別 != チップ種別.SE4 && chip.チップ種別 != チップ種別.SE5 && chip.チップ種別 != チップ種別.SE6 &&
                            chip.チップ種別 != チップ種別.SE7 && chip.チップ種別 != チップ種別.SE8 && chip.チップ種別 != チップ種別.SE9 &&
                            chip.チップ種別 != チップ種別.SE10 && chip.チップ種別 != チップ種別.SE11 && chip.チップ種別 != チップ種別.SE12 &&
                            chip.チップ種別 != チップ種別.SE13 && chip.チップ種別 != チップ種別.SE14 && chip.チップ種別 != チップ種別.SE15 &&
                            chip.チップ種別 != チップ種別.SE16 && chip.チップ種別 != チップ種別.SE17 && chip.チップ種別 != チップ種別.SE18 &&
                            chip.チップ種別 != チップ種別.SE19 && chip.チップ種別 != チップ種別.SE20 && chip.チップ種別 != チップ種別.SE21 &&
                            chip.チップ種別 != チップ種別.SE22 && chip.チップ種別 != チップ種別.SE23 && chip.チップ種別 != チップ種別.SE24 &&
                            chip.チップ種別 != チップ種別.SE25 && chip.チップ種別 != チップ種別.SE26 && chip.チップ種別 != チップ種別.SE27 &&
                            chip.チップ種別 != チップ種別.SE28 && chip.チップ種別 != チップ種別.SE29 && chip.チップ種別 != チップ種別.SE30 &&
                            chip.チップ種別 != チップ種別.SE31 && chip.チップ種別 != チップ種別.SE32 &&
                            chip.チップ種別 != チップ種別.GuitarAuto &&
                            chip.チップ種別 != チップ種別.BassAuto ) );

                        現在の小節に存在するチップのレーン別リスト[ laneType ] = chips.ToArray();
                    }
                    //----------------
                    #endregion


                    #region " Part を出力する。"
                    //-----------------
                    {
                        var options = ( score.小節長倍率リスト[ 小節番号 ] == 1.0 ) ? "" : $"s{score.小節長倍率リスト[ 小節番号 ].ToString()}";     // 小節長倍率指定

                        sw.WriteLine( $"Part = {小節番号.ToString()}{options};" );
                    }
                    //-----------------
                    #endregion

                    foreach( レーン種別 laneType in Enum.GetValues( typeof( レーン種別 ) ) )
                    {
                        if( !( 現在の小節に存在するチップのレーン別リスト.ContainsKey( laneType ) ) ||
                            0 == 現在の小節に存在するチップのレーン別リスト[ laneType ].Length )
                            continue;

                        #region " Lane を出力する。"
                        //----------------
                        sw.Write( $"Lane={laneType.ToString()}; " );
                        //----------------
                        #endregion

                        #region " Resolution を出力する。"
                        //----------------
                        {
                            // チップの 位置 と 解像度 を約分する。

                            foreach( var chip in 現在の小節に存在するチップのレーン別リスト[ laneType ] )
                            {
                                var 最大公約数 = _最大公約数を返す( chip.小節内位置, chip.小節解像度 );

                                chip.小節内位置 /= 最大公約数;
                                chip.小節解像度 /= 最大公約数;
                            }


                            // この小節の解像度を、チップの解像度の最小公倍数から算出する。

                            int この小節の解像度 = 1;
                            foreach( var chip in 現在の小節に存在するチップのレーン別リスト[ laneType ] )
                                この小節の解像度 = _最小公倍数を返す( この小節の解像度, chip.小節解像度 );


                            // 算出した解像度を、Resolution として出力する。

                            sw.Write( $"Resolution={この小節の解像度}; " );


                            // Resolution にあわせて、チップの位置と解像度を修正する。

                            foreach( var chip in 現在の小節に存在するチップのレーン別リスト[ laneType ] )
                            {
                                int 倍率 = この小節の解像度 / chip.小節解像度;      // 必ず割り切れる。（この小節の解像度はチップの小節解像度の最小公倍数なので）

                                chip.小節内位置 *= 倍率;
                                chip.小節解像度 *= 倍率;
                            }
                        }
                        //----------------
                        #endregion

                        #region " Chips を出力する。"
                        //----------------
                        sw.Write( "Chips=" );

                        for( int i = 0; i < 現在の小節に存在するチップのレーン別リスト[ laneType ].Length; i++ )
                        {
                            var chip = 現在の小節に存在するチップのレーン別リスト[ laneType ][ i ];


                            // 位置を出力。

                            sw.Write( chip.小節内位置.ToString() );


                            // 属性を出力（あれば）。

                            #region " (1) 共通属性 "
                            //-----------------
                            if( chip.音量 < チップ.最大音量 )
                                sw.Write( $"v{chip.音量.ToString()}" );
                            //-----------------
                            #endregion

                            #region " (2) 専用属性 "
                            //-----------------
                            switch( chip.チップ種別 )
                            {
                                case チップ種別.Ride_Cup:
                                    sw.Write( 'c' );
                                    break;

                                case チップ種別.HiHat_Open:
                                    sw.Write( 'o' );
                                    break;

                                case チップ種別.HiHat_HalfOpen:
                                    sw.Write( 'h' );
                                    break;

                                case チップ種別.HiHat_Foot:
                                    sw.Write( 'f' );
                                    break;

                                case チップ種別.Snare_OpenRim:
                                    sw.Write( 'o' );
                                    break;

                                case チップ種別.Snare_ClosedRim:
                                    sw.Write( 'c' );
                                    break;

                                case チップ種別.Snare_Ghost:
                                    sw.Write( 'g' );
                                    break;

                                case チップ種別.Tom1_Rim:
                                    sw.Write( 'r' );
                                    break;

                                case チップ種別.Tom2_Rim:
                                    sw.Write( 'r' );
                                    break;

                                case チップ種別.Tom3_Rim:
                                    sw.Write( 'r' );
                                    break;

                                case チップ種別.LeftCymbal_Mute:
                                    sw.Write( 'm' );
                                    break;

                                case チップ種別.RightCymbal_Mute:
                                    sw.Write( 'm' );
                                    break;

                                case チップ種別.BPM:
                                    sw.Write( $"b{chip.BPM.ToString()}" );
                                    break;
                            }
                            //-----------------
                            #endregion


                            // 区切り文字(,) または 終端文字(;) を出力。

                            bool 最後のチップである = ( i == 現在の小節に存在するチップのレーン別リスト[ laneType ].Length - 1 );
                            sw.Write( 最後のチップである ? ";" : "," );
                        }
                        //----------------
                        #endregion

                        sw.WriteLine( "" ); // ここまでが１行。
                    }

                    sw.WriteLine( "" ); // 次の Part 前に１行あける。
                }

            }

            private static Dictionary<string, (チップ種別 チップ種別, bool 可視)> _レーンプロパティ = new Dictionary<string, (チップ種別 チップ種別, bool 可視)> {
                #region " *** "
                //----------------
                [ "leftcrash" ] = (チップ種別.LeftCrash, true),
                [ "ride" ] = (チップ種別.Ride, true),
                [ "china" ] = (チップ種別.China, true),
                [ "splash" ] = (チップ種別.Splash, true),
                [ "hihat" ] = (チップ種別.HiHat_Close, true),
                [ "foot" ] = (チップ種別.HiHat_Foot, true),
                [ "snare" ] = (チップ種別.Snare, true),
                [ "bass" ] = (チップ種別.Bass, true),
                [ "tom1" ] = (チップ種別.Tom1, true),
                [ "tom2" ] = (チップ種別.Tom2, true),
                [ "tom3" ] = (チップ種別.Tom3, true),
                [ "rightcrash" ] = (チップ種別.RightCrash, true),
                [ "bpm" ] = (チップ種別.BPM, false),
                [ "bgv" ] = (チップ種別.背景動画, false),
                [ "bgm" ] = (チップ種別.BGM, false),
                //----------------
                #endregion
            };



            // その他


            private static int _最大公約数を返す( int m, int n )
            {
                if( ( 0 > m ) || ( 0 > n ) )
                    throw new Exception( "引数に負数は指定できません。" );

                if( 0 == m )
                    return n;

                if( 0 == n )
                    return m;

                // ユーグリッドの互除法
                int r;
                while( ( r = m % n ) != 0 )
                {
                    m = n;
                    n = r;
                }

                return n;
            }

            private static int _最小公倍数を返す( int m, int n )
            {
                if( ( 0 >= m ) || ( 0 >= n ) )
                    throw new Exception( "引数に0以下の数は指定できません。" );

                return ( m * n / _最大公約数を返す( m, n ) );
            }

            /// <summary>
            ///     指定された行が SSTFVersion コメントであるかを判定し、そうであるならそのバージョンを解析して返す。
            ///     それ以外は null を返す。
            /// </summary>
            internal static Version _行にSSTFVersionがあるなら解析して返す( string 行 )
            {
                try
                {
                    行 = 行.Trim();

                    int コメント識別子の位置 = 行.IndexOf( '#' );    // 見つからなければ -1

                    if( 0 <= コメント識別子の位置 )
                    {
                        var コメント文 = 行[ ( コメント識別子の位置 + 1 ).. ].Trim();

                        if( コメント文.ToLower().StartsWith( "sstfversion" ) )
                        {
                            return new Version( コメント文[ ( "sstfversion".Length ).. ] );  // 生成失敗なら例外発生
                        }
                    }
                }
                catch
                {
                }
                return null;
            }

            private static string _絶対パスを相対パスに変換する( string 基点フォルダの絶対パス, string 変換したいフォルダの絶対パス )
            {
                if( null == 変換したいフォルダの絶対パス )
                    return null;

                if( !( Path.IsPathRooted( 基点フォルダの絶対パス ) ) )
                    throw new Exception( $"基点フォルダは絶対パスで指定してください。[{基点フォルダの絶対パス}]" );

                if( !( Path.IsPathRooted( 変換したいフォルダの絶対パス ) ) )
                    throw new Exception( $"変換対象フォルダは絶対パスで指定してください。[{変換したいフォルダの絶対パス}]" );

                // 末尾は \ にしておく（"+"でパスを連結する事態を想定。Path.Combine() を使う分には、末尾に \ があってもなくてもどっちでもいい。）
                if( '\\' != 基点フォルダの絶対パス[ 基点フォルダの絶対パス.Length - 1 ] )
                    基点フォルダの絶対パス += @"\";

                // 絶対-相対パス変換は、System.IO.Path クラスではなく System.IO.Uri クラスでしか行えない。
                var 基点uri = new Uri( 基点フォルダの絶対パス );
                var 変換前uri = new Uri( 変換したいフォルダの絶対パス );
                var 変換後uri = 基点uri.MakeRelativeUri( 変換前uri );

                // URI形式になっているので、パス形式に戻す。（具体的には、エスケープ文字を復元し、さらに '/' を '\' に置換する。）
                return Uri.UnescapeDataString( 変換後uri.ToString() ).Replace( oldChar: '/', newChar: '\\' );
            }
        }
    }
}
