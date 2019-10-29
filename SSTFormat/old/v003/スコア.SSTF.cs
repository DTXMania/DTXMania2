using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SSTFormat.v003
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
            public static スコア ファイルから生成する( string SSTFファイルパス, bool ヘッダだけ = false )
            {
                // ファイルのSSTFバージョンを確認。

                string 先頭行;
                using( var sr = new StreamReader( SSTFファイルパス, Encoding.UTF8 ) )
                    先頭行 = sr.ReadLine();

                var SSTFバージョン = _行にSSTFVersionがあるなら解析して返す( 先頭行 ) ?? new Version( 1, 0, 0, 0 );  // 既定値


                // ファイルのSSTFバージョンに応じた方法でスコアを生成する。

                スコア score = null;

                if( 3 == SSTFバージョン.Major )
                {
                    #region " (A) 現行版 "
                    //----------------
                    string 全入力文字列 = null;

                    // ファイルの内容を一気読み。
                    using( var sr = new StreamReader( SSTFファイルパス ) )
                        全入力文字列 = sr.ReadToEnd();

                    // 読み込んだ内容でスコアを生成する。
                    score = _全行解析する( ref 全入力文字列, ヘッダだけ );

                    // ファイルから読み込んだ場合のみ、このメンバが有効。
                    score.譜面ファイルパス = SSTFファイルパス;
                    //----------------
                    #endregion
                }
                else if( 3 > SSTFバージョン.Major )
                {
                    #region " (B) 前方互換 "
                    //----------------
                    using( var v2score = new SSTFormat.v002.スコア( SSTFファイルパス ) )
                    {
                        score = _v2から移行する( v2score );

                        // ファイルから読み込んだ場合のみ、このメンバが有効。
                        score.譜面ファイルパス = SSTFファイルパス;
                    }
                    //----------------
                    #endregion
                }
                else
                {
                    #region " (C) 後方互換はなし "
                    //----------------
                    throw new ArgumentException( $"この曲データファイルのSSTFバージョン({SSTFバージョン})には未対応です。" );
                    //----------------
                    #endregion
                }

                if( null == score )
                    new Exception( "スコアが生成されませんでした。" );

                if( !( ヘッダだけ ) )
                {
                    // ファイル以外から情報を取得する。

                    if( string.IsNullOrEmpty( score.背景動画ID ) )
                    {
                        #region " 背景動画ファイルを検索する。"
                        //----------------
                        score.背景動画ID =
                            ( from file in Directory.GetFiles( Path.GetDirectoryName( score.譜面ファイルパス ) )
                              where スコア.背景動画のデフォルト拡張子リスト.Any( 拡張子名 => ( Path.GetExtension( file ).ToLower() == 拡張子名 ) )
                              select file ).FirstOrDefault();
                        //----------------
                        #endregion
                    }

                    スコア._後処理を行う( score );
                }

                return score;
            }

            /// <summary>
            ///     SSTFフォーマットのテキストデータを含んだ１つの文字列から、スコアを生成して返す。
            ///		読み込みに失敗した場合は、何らかの例外を発出する。
            /// </summary>
            public static スコア 文字列から生成する( string 全入力文字列, bool ヘッダだけ = false )
            {
                // データのバージョンを確認。

                string 先頭行;
                using( var sr = new StringReader( 全入力文字列 ) )
                    先頭行 = sr.ReadLine();

                var SSTFバージョン = _行にSSTFVersionがあるなら解析して返す( 先頭行 ) ?? new Version( 1, 0, 0, 0 );  // 既定値


                // データのSSTFバージョンに応じた方法でスコアを生成する。

                スコア score = null;

                if( 3 == SSTFバージョン.Major )
                {
                    #region " (A) 現行版 "
                    //----------------
                    score = _全行解析する( ref 全入力文字列, ヘッダだけ );

                    // ファイルから読み込んだ場合のみ、このメンバが有効。
                    score.譜面ファイルパス = null;
                    //----------------
                    #endregion
                }
                else if( 3 > SSTFバージョン.Major )
                {
                    #region " (B) 前方互換 "
                    //----------------
                    var 一時ファイルパス = Path.GetTempFileName();

                    try
                    {
                        // v2 にはファイルからの生成メソッドしかないので、全入力文字列を一時ファイルに出力し、
                        using( var sw = new StreamWriter( 一時ファイルパス ) )
                            sw.Write( 全入力文字列 );

                        // それを再び読み込んで生成する。
                        using( var v2score = new SSTFormat.v002.スコア( 一時ファイルパス ) )
                            score = _v2から移行する( v2score );

                        // ファイルから読み込んだ場合のみ、このメンバが有効。
                        // ここでは便宜上ファイルを介しただけなので、このメンバは無効とする。
                        score.譜面ファイルパス = null;
                    }
                    finally
                    {
                        File.Delete( 一時ファイルパス );
                    }
                    //----------------
                    #endregion
                }
                else
                {
                    #region " (C) 後方互換はなし "
                    //----------------
                    throw new ArgumentException( $"この曲データファイルのSSTFバージョン({SSTFバージョン})には未対応です。" );
                    //----------------
                    #endregion
                }

                if( null == score )
                    new Exception( "スコアが生成されませんでした。" );

                if( !( ヘッダだけ ) )
                {
                    // ファイル以外から情報を取得する。
                    //score.背景動画ファイル名 = ...     --> 文字列から生成した場合は設定しない。

                    スコア._後処理を行う( score );
                }

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


            // private

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
                        var コメント文 = 行.Substring( コメント識別子の位置 + 1 ).Trim();

                        if( コメント文.ToLower().StartsWith( "sstfversion" ) )
                        {
                            return new Version( コメント文.Substring( "sstfversion".Length ) );  // 生成失敗なら例外発生
                        }
                    }
                }
                catch
                {
                }
                return null;
            }

            /// <summary>
            ///     v2のスコアをもとに、現行版のスコアを新規生成して返す。
            /// </summary>
            private static スコア _v2から移行する( SSTFormat.v002.スコア v2score )
            {
                var score = new スコア();

                score.曲名 = v2score.Header.曲名;
                score.アーティスト名 = "";               // v3で新規追加
                score.説明文 = v2score.Header.説明文;
                score.難易度 = 5.0;                      // v3で新規追加
                score.背景動画ID = v2score.背景動画ファイル名;                 // v3で新規追加
                score.プレビュー画像ファイル名 = null;        // v3で新規追加
                score.サウンドデバイス遅延ms = v2score.Header.サウンドデバイス遅延ms;
                //score.譜面ファイルパス = SSTFファイルパス;  --> 呼び出し元で設定すること。

                score.チップリスト = new List<チップ>();
                foreach( var v2chip in v2score.チップリスト )
                    score.チップリスト.Add( new チップ( v2chip ) );

                score.小節長倍率リスト = new List<double>();
                foreach( var v2scale in v2score.小節長倍率リスト )
                    score.小節長倍率リスト.Add( v2scale );

                score.小節メモリスト = new Dictionary<int, string>();
                foreach( var kvp in v2score.dicメモ )
                    score.小節メモリスト.Add( kvp.Key, kvp.Value );

                score.空打ちチップマップ = new Dictionary<レーン種別, int>();    // v3で新規追加
                score.WAVリスト = new Dictionary<int, (string ファイルパス, bool 多重再生する)>(); // v3で新規追加
                score.AVIリスト = new Dictionary<int, string>();   // v3で新規追加
                score.PATH_WAV = @"\";  // v3で新規追加

                return score;
            }

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


            // 行解析

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
                                行 = 行.Substring( 0, 区切り位置 );
                                行 = 行.Trim();
                            }
                        }
                        //----------------
                        #endregion

                        if( string.IsNullOrEmpty( 行 ) )
                            continue;   // 空行


                        // ヘッダの解析

                        if( _行をヘッダ行と想定して解析する( ref 行 ) )
                            continue;

                        if( ヘッダだけ )
                            continue;   // ヘッダだけならここまで。


                        // ヘッダ以外の解析

                        if( _行を小節メモ行として解析する( ref 行 ) )
                            continue;

                        if( _行をチップ記述行として解析する( ref 行 ) )
                            continue;
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

                    if( items.Length != 2 )
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
                        現在の.スコア.難易度 = Math.Max( Math.Min( double.Parse( items[ 1 ].Trim() ), 9.99 ), 0.00 );
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
                #region " Video "
                //----------------
                if( 行.StartsWith( "video", StringComparison.OrdinalIgnoreCase ) )
                {
                    string[] items = 行.Split( '=' );

                    if( 2 != items.Length )
                    {
                        Trace.TraceError( $"Video の書式が不正です。スキップします。[{現在の.行番号}行目]" );
                        return false;
                    }

                    現在の.スコア.背景動画ID = items[ 1 ].Trim();

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

                    行 = 行.Substring( 等号位置 + 1 ).Trim();

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
                    string 小節番号文字列 = 行.Substring( 0, カンマ位置 );

                    if( !( int.TryParse( 小節番号文字列, out int 小節番号 ) || ( 0 > 小節番号 ) ) )
                    {
                        Trace.TraceError( $"PartMemo の小節番号が不正です。スキップします。[{現在の.行番号}]行目]" );
                        return false;
                    }
                    //-----------------
                    #endregion
                    #region " メモを取得する。"
                    //-----------------
                    string メモ = 行.Substring( カンマ位置 + 1 );

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
                        //        チップ種別 = チップ種別.小節メモ,       --> チップは廃止。（不要）
                        //        小節番号 = 小節番号,
                        //        小節内位置 = 0,
                        //        小節解像度 = 1,
                        //    } );
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
                                        パラメータ = パラメータ.Substring( 1 ).Trim();

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
                                        chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
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
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                chip.チップ種別 = チップ種別.LeftCymbal_Mute;
                                                //----------------
                                                #endregion
                                            }
                                            else
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
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
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                chip.チップ種別 = チップ種別.Ride_Cup;
                                                //-----------------
                                                #endregion
                                            }
                                            else
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        case チップ種別.China:
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        case チップ種別.Splash:
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
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
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                chip.チップ種別 = チップ種別.HiHat_Open;
                                                //-----------------
                                                #endregion
                                            }
                                            else if( 'h' == 属性ID )
                                            {
                                                #region " HiHat.ハーフオープン "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                chip.チップ種別 = チップ種別.HiHat_HalfOpen;
                                                //-----------------
                                                #endregion
                                            }
                                            else if( 'c' == 属性ID )
                                            {
                                                #region " HiHat.クローズ "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                chip.チップ種別 = チップ種別.HiHat_Close;
                                                //-----------------
                                                #endregion
                                            }
                                            else if( 'f' == 属性ID )
                                            {
                                                #region " HiHat.フットスプラッシュ "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                chip.チップ種別 = チップ種別.HiHat_Foot;
                                                //-----------------
                                                #endregion
                                            }
                                            else
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
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
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                chip.チップ種別 = チップ種別.Snare_OpenRim;
                                                //-----------------
                                                #endregion
                                            }
                                            else if( 'c' == 属性ID )
                                            {
                                                #region " Snare.クローズドリム "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                chip.チップ種別 = チップ種別.Snare_ClosedRim;
                                                //-----------------
                                                #endregion
                                            }
                                            else if( 'g' == 属性ID )
                                            {
                                                #region " Snare.ゴースト "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                chip.チップ種別 = チップ種別.Snare_Ghost;
                                                //-----------------
                                                #endregion
                                            }
                                            else
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        case チップ種別.Bass:
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
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
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                chip.チップ種別 = チップ種別.Tom1_Rim;
                                                //-----------------
                                                #endregion
                                            }
                                            else
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
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
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                chip.チップ種別 = チップ種別.Tom2_Rim;
                                                //-----------------
                                                #endregion
                                            }
                                            else
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
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
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                chip.チップ種別 = チップ種別.Tom3_Rim;
                                                //-----------------
                                                #endregion
                                            }
                                            else
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
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
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                chip.チップ種別 = チップ種別.RightCymbal_Mute;
                                                //----------------
                                                #endregion
                                            }
                                            else
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
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
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
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
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        case チップ種別.背景動画:
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{現在の.行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;
                                    }

                                    #region " 未知の属性 "
                                    //-----------------
                                    chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
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

            /// <summary>
            ///		取出文字列の先頭にある数字（小数点も有効）の連続した部分を取り出して、戻り値として返す。
            ///		また、取出文字列から取り出した数字文字列部分を除去した文字列を再度格納する。
            /// </summary>
            private static string _指定された文字列の先頭から数字文字列を取り出す( ref string 取出文字列 )
            {
                // 数字が何桁続くか数える。
                int 桁数 = 0;
                while( ( 桁数 < 取出文字列.Length ) && ( char.IsDigit( 取出文字列[ 桁数 ] ) || 取出文字列[ 桁数 ] == '.' ) )
                    桁数++;

                if( 0 == 桁数 )
                    return "";

                // その桁数分を取り出して返す。
                string 数字文字列 = 取出文字列.Substring( 0, 桁数 );
                取出文字列 = ( 桁数 == 取出文字列.Length ) ? "" : 取出文字列.Substring( 桁数 );

                return 数字文字列;
            }


            // 行出力

            private static void _ヘッダ行を出力する( スコア score, StreamWriter sw )
            {
                sw.WriteLine( "Title=" + ( ( string.IsNullOrEmpty( score.曲名 ) ) ? "(no title)" : score.曲名 ) );

                if( !string.IsNullOrEmpty( score.説明文 ) )
                    sw.WriteLine( $"Description=" + score.説明文.Replace( Environment.NewLine, @"\n" ) );   // 改行コードは、２文字のリテラル "\n" に置換。

                sw.WriteLine( $"SoundDevice.Delay={score.サウンドデバイス遅延ms}" );

                sw.WriteLine( $"Level={score.難易度.ToString( "0.00" )}" );

                if( !string.IsNullOrEmpty( score.背景動画ID ) )
                    sw.WriteLine( $"Video={score.背景動画ID}" );

                sw.WriteLine( "" );
            }

            private static void _小節メモ行を出力する( スコア score, StreamWriter sw )
            {
                int 最大小節番号 = score.最大小節番号を返す();

                // 小節番号について昇順に出力。
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
                            スコア.チップtoレーンマップ[ chip.チップ種別 ] == laneType ) );

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
                { "leftcrash",  ( チップ種別.LeftCrash,   true  ) },
                { "ride",       ( チップ種別.Ride,        true  ) },
                { "china",      ( チップ種別.China,       true  ) },
                { "splash",     ( チップ種別.Splash,      true  ) },
                { "hihat",      ( チップ種別.HiHat_Close, true  ) },
                { "foot",       ( チップ種別.HiHat_Foot,  true  ) },
                { "snare",      ( チップ種別.Snare,       true  ) },
                { "bass",       ( チップ種別.Bass,        true  ) },
                { "tom1",       ( チップ種別.Tom1,        true  ) },
                { "tom2",       ( チップ種別.Tom2,        true  ) },
                { "tom3",       ( チップ種別.Tom3,        true  ) },
                { "rightcrash", ( チップ種別.RightCrash,  true  ) },
                { "bpm",        ( チップ種別.BPM,         false ) },
                { "song",       ( チップ種別.背景動画,    false ) },
                //----------------
                #endregion
            };
        }
    }
}
