using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SSTFormat.v001
{
    public class スコア : IDisposable, ISSTFScore
    {
        // ヘルパ

        /// <summary>
        ///		指定されたコマンド名が対象文字列内で使用されている場合に、パラメータ部分の文字列を返す。
        /// </summary>
        /// <remarks>
        ///		.dtx や box.def 等で使用されている "#＜コマンド名＞[:]＜パラメータ＞[;コメント]" 形式の文字列（対象文字列）について、
        ///		指定されたコマンドを使用する行であるかどうかを判別し、使用する行であるなら、そのパラメータ部分の文字列を返す。
        ///		対象文字列のコマンド名が指定したコマンド名と異なる場合には null を返す。
        ///		コマンド名は正しくてもパラメータが存在しない場合には、空文字列("")を返す。
        /// </remarks>
        /// <param name="対象文字列">
        ///		調べる対象の文字列。（例: "#TITLE: 曲名 ;コメント"）
        ///	</param>
        /// <param name="コマンド名">
        ///		調べるコマンドの名前（例:"TITLE"）。#は不要、大文字小文字は区別されない。
        ///	</param>
        /// <returns>
        ///		パラメータ文字列の取得に成功したらその文字列、異なるコマンドだったなら null。
        ///	</returns>
        public static bool コマンドのパラメータ文字列部分を返す( string 対象文字列, string コマンド名, out string パラメータ文字列 )
        {
            // コメント部分を除去し、両端をトリムする。なお、全角空白はトリムしない。
            対象文字列 = 対象文字列.Split( ';' )[ 0 ].Trim( ' ', '\t' );

            string 正規表現パターン = $@"^\s*#{コマンド名}(:|\s)+(.*)\s*$";  // \s は空白文字。
            var m = Regex.Match( 対象文字列, 正規表現パターン, RegexOptions.IgnoreCase );

            if( m.Success && ( 3 <= m.Groups.Count ) )
            {
                パラメータ文字列 = m.Groups[ 2 ].Value;
                return true;
            }
            else
            {
                パラメータ文字列 = null;
                return false;
            }
        }

        // 定数プロパティ

        public const double 初期BPM = 120.0;

        public const double 初期小節解像度 = 480.0;

        public const double BPM初期値固定での1小節4拍の時間ms = ( 60.0 * 1000 ) / ( スコア.初期BPM / 4.0 );

        public const double BPM初期値固定での1小節4拍の時間sec = 60.0 / ( スコア.初期BPM / 4.0 );

        /// <summary>
        ///		1ms あたりの設計ピクセル数 [dpx] 。
        /// </summary>
        /// <remarks>
        ///		BPM 150 のとき、1小節が 234 dpx になるように調整。
        ///		→ 60秒で150拍のとき、1小節(4拍)が 234 dpx。
        ///		→ 60秒の間に、150[拍]÷4[拍]＝37.5[小節]。
        ///		→ 60秒の間に、37.5[小節]×234[dpx/小節]＝ 8775[dpx]。
        ///		→ 1ms の間に、8775[dpx]÷60000[ms]＝0.14625[dpx/ms]。割り切れて良かった。
        /// </remarks>
        public const double 基準譜面速度dpxms = 0.14625 * 2.25;  // "* 2.25" は「x1.0はもう少し速くてもいいんではないか？」という感覚的な調整分。

        /// <summary>
        ///		1秒あたりの設計ピクセル数 [dpx] 。
        /// </summary>
        public const double 基準譜面速度dpxsec = 基準譜面速度dpxms * 1000.0;

        public static readonly Dictionary<レーン種別, List<チップ種別>> dicSSTFレーンチップ対応表
        #region " *** "
            //-----------------
            = new Dictionary<レーン種別, List<チップ種別>>() {
                { レーン種別.Bass, new List<チップ種別>() { チップ種別.Bass } },
                { レーン種別.BPM, new List<チップ種別>() { チップ種別.BPM } },
                { レーン種別.China, new List<チップ種別>() { チップ種別.China } },
                { レーン種別.HiHat, new List<チップ種別>() { チップ種別.HiHat_Close, チップ種別.HiHat_Foot, チップ種別.HiHat_HalfOpen, チップ種別.HiHat_Open } },
                { レーン種別.Ride, new List<チップ種別>() { チップ種別.Ride, チップ種別.Ride_Cup } },
                { レーン種別.Snare, new List<チップ種別>() { チップ種別.Snare, チップ種別.Snare_ClosedRim, チップ種別.Snare_Ghost, チップ種別.Snare_OpenRim } },
                { レーン種別.Song, new List<チップ種別>() { チップ種別.背景動画 } },
                { レーン種別.Splash, new List<チップ種別>() { チップ種別.Splash } },
                { レーン種別.Tom1, new List<チップ種別>() { チップ種別.Tom1, チップ種別.Tom1_Rim } },
                { レーン種別.Tom2, new List<チップ種別>() { チップ種別.Tom2, チップ種別.Tom2_Rim } },
                { レーン種別.Tom3, new List<チップ種別>() { チップ種別.Tom3, チップ種別.Tom3_Rim } },
            };
        //-----------------
        #endregion

        // 背景動画のデフォルト拡張子
        public static readonly List<string> 背景動画のデフォルト拡張子s = new List<string>() {
            ".avi", ".flv", ".mp4", ".wmv", ".mpg", ".mpeg"
        };

        // プロパティ；読み込み時または編集時に設定される

        /// <remarks>
        ///		背景動画ファイル名は、sstf ファイルには保存されず、必要時に sstf ファイルと同じフォルダを検索して取得する。
        /// </remarks>
        public string 背景動画ファイル名 = "";

        public class CHeader
        {
            public SSTFVersion SSTFバージョン = new SSTFVersion( 1, 0, 0, 0 );   // SSTFVersion 指定がない場合の既定値。
            public string 曲名 = "(no title)";
            public string 説明文 = "";
            public float サウンドデバイス遅延ms = 0f;
        }
        public CHeader Header = new CHeader();

        public List<チップ> チップリスト
        {
            get;
            protected set;
        }

        public List<double> 小節長倍率リスト
        {
            get;
            protected set;
        }

        public int 最大小節番号
        {
            get
            {
                int 最大小節番号 = 0;

                foreach( チップ chip in this.チップリスト )
                {
                    if( chip.小節番号 > 最大小節番号 )
                        最大小節番号 = chip.小節番号;
                }

                return 最大小節番号;
            }
        }

        public Dictionary<int, string> dicメモ = new Dictionary<int, string>();

        // メソッド

        public スコア()
        {
            this.Header.SSTFバージョン = new SSTFVersion( 1, 0, 0, 0 );  // このソースで対応するバージョン
            this.チップリスト = new List<チップ>();
            this.小節長倍率リスト = new List<double>();
        }

        public スコア( string 曲データファイル名, bool ヘッダだけ = false ) : this()
        {
            if( ヘッダだけ )
                this.曲データファイルを読み込む_ヘッダだけ( 曲データファイル名 );
            else
                this.曲データファイルを読み込む( 曲データファイル名 );
        }

        public void Dispose()
        {
        }

        /// <summary>
        ///		指定された曲データファイルを読み込む。
        ///		失敗すれば何らかの例外を発出する。
        /// </summary>
        public void 曲データファイルを読み込む( string 曲データファイル名 )
        {
            #region " SSTFバージョンチェック。"
            //----------------
            var version = SSTFVersion.CreateVersionFromFile( 曲データファイル名 );
            if( version.Major != 1 )
                throw new ArgumentException( "この曲データのバージョンは、1.x.x.x ではありません。" );
            //----------------
            #endregion

            #region " 初期化する。"
            //-----------------
            this.小節長倍率リスト = new List<double>();
            this.dicメモ = new Dictionary<int, string>();
            //-----------------
            #endregion
            #region " 背景動画ファイル名を更新する。"
            //----------------
            this.背景動画ファイル名 =
                ( from file in Directory.GetFiles( Path.GetDirectoryName( 曲データファイル名 ) )
                  where スコア.背景動画のデフォルト拡張子s.Any( 拡張子名 => ( Path.GetExtension( file ).ToLower() == 拡張子名 ) )
                  select file ).FirstOrDefault();
            //----------------
            #endregion
            #region " 曲データファイルを読み込む。"
            //-----------------
            using( var sr = new StreamReader( 曲データファイル名, Encoding.UTF8 ) )
            {
                int 行番号 = 0;
                int 現在の小節番号 = 0;
                int 現在の小節解像度 = 384;
                チップ種別 e現在のチップ = チップ種別.Unknown;

                while( false == sr.EndOfStream )
                {
                    // 1行ずつ読み込む。
                    行番号++;
                    string 行 = this._行を読み込む( sr );

                    if( string.IsNullOrEmpty( 行 ) )
                        continue;

                    // 前処理。

                    #region " 改行は ';' に、TABは空白文字にそれぞれ変換し、先頭末尾の空白を削除する。"
                    //-----------------
                    行 = 行.Replace( Environment.NewLine, ";" );
                    行 = 行.Replace( '\t', ' ' );
                    行 = 行.Trim();
                    //-----------------
                    #endregion
                    #region " 行中の '#' 以降はコメントとして除外する。また、コメントだけの行はスキップする。"
                    //-----------------
                    int 区切り位置 = 0;
                    for( 区切り位置 = 0; 区切り位置 < 行.Length; 区切り位置++ )
                    {
                        if( 行[ 区切り位置 ] == '#' )
                            break;
                    }
                    if( 0 == 区切り位置 )
                        continue;       // コメントだけの行はスキップ。
                    if( 区切り位置 < 行.Length )
                    {
                        行 = 行.Substring( 0, 区切り位置 - 1 );
                        行 = 行.Trim();
                    }
                    //-----------------
                    #endregion
                    #region " 空行ならこの行はスキップする。"
                    //-----------------
                    if( string.IsNullOrEmpty( 行 ) )
                        continue;
                    //-----------------
                    #endregion

                    // ヘッダコマンド処理。

                    #region " ヘッダコマンドの処理を行う。"
                    //-----------------
                    if( 行.StartsWith( "Title", StringComparison.OrdinalIgnoreCase ) )
                    {
                        #region " Title コマンド "
                        //-----------------
                        string[] items = 行.Split( '=' );

                        if( items.Length != 2 )
                        {
                            Trace.TraceError( $"Title の書式が不正です。スキップします。[{行番号}行目]" );
                            continue;
                        }

                        this.Header.曲名 = items[ 1 ].Trim();
                        //-----------------
                        #endregion

                        continue;
                    }
                    if( 行.StartsWith( "Description", StringComparison.OrdinalIgnoreCase ) )
                    {
                        #region " Description コマンド "
                        //-----------------
                        string[] items = 行.Split( '=' );

                        if( items.Length != 2 )
                        {
                            Trace.TraceError( $"Description の書式が不正です。スキップします。[{行番号}行目]" );
                            continue;
                        }

                        // ２文字のリテラル "\n" は改行に復号。
                        this.Header.説明文 = items[ 1 ].Trim().Replace( @"\n", Environment.NewLine );
                        //-----------------
                        #endregion

                        continue;
                    }
                    if( 行.StartsWith( "SoundDevice.Delay", StringComparison.OrdinalIgnoreCase ) )
                    {
                        #region " SoundDevice.Delay コマンド "
                        //-----------------
                        string[] items = 行.Split( '=' );

                        if( items.Length != 2 )
                        {
                            Trace.TraceError( $"SoundDevice.Delay の書式が不正です。スキップします。[{行番号}行目]" );
                            continue;
                        }

                        // ２文字のリテラル "\n" は改行に復号。
                        if( float.TryParse( items[ 1 ].Trim().Replace( @"\n", Environment.NewLine ), out float value ) )
                            this.Header.サウンドデバイス遅延ms = value;
                        //-----------------
                        #endregion

                        continue;
                    }
                    //-----------------
                    #endregion

                    // メモ（小節単位）処理。

                    #region " メモ（小節単位）の処理を行う。"
                    //-----------------
                    if( 行.StartsWith( "PartMemo", StringComparison.OrdinalIgnoreCase ) )
                    {
                        #region " '=' 以前を除去する。"
                        //-----------------
                        int 等号位置 = 行.IndexOf( '=' );
                        if( 0 >= 等号位置 )
                        {
                            Trace.TraceError( $"PartMemo の書式が不正です。スキップします。[{行番号}]行目]" );
                            continue;
                        }
                        行 = 行.Substring( 等号位置 + 1 ).Trim();
                        if( string.IsNullOrEmpty( 行 ) )
                        {
                            Trace.TraceError( $"PartMemo の書式が不正です。スキップします。[{行番号}]行目]" );
                            continue;
                        }
                        //-----------------
                        #endregion
                        #region " カンマ位置を取得する。"
                        //-----------------
                        int カンマ位置 = 行.IndexOf( ',' );
                        if( 0 >= カンマ位置 )
                        {
                            Trace.TraceError( $"PartMemo の書式が不正です。スキップします。[{行番号}]行目]" );
                            continue;
                        }
                        //-----------------
                        #endregion
                        #region " 小節番号を取得する。"
                        //-----------------
                        string 小節番号文字列 = 行.Substring( 0, カンマ位置 );
                        if( false == int.TryParse( 小節番号文字列, out int 小節番号 ) || ( 0 > 小節番号 ) )
                        {
                            Trace.TraceError( $"PartMemo の小節番号が不正です。スキップします。[{行番号}]行目]" );
                            continue;
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
                        #region " メモが空文字列でないなら dicメモ に登録すると同時に、チップとしても追加する。"
                        //-----------------
                        if( false == string.IsNullOrEmpty( メモ ) )
                        {
                            this.dicメモ.Add( 小節番号, メモ );

                            this.チップリスト.Add(
                                new チップ() {
                                    チップ種別 = チップ種別.小節メモ,
                                    小節番号 = 小節番号,
                                    小節内位置 = 0,
                                    小節解像度 = 1,
                                } );
                        }
                        //-----------------
                        #endregion

                        continue;
                    }
                    //-----------------
                    #endregion

                    // 上記行頭コマンド以外は、チップ記述行だと見なす。

                    #region " チップ記述コマンドの処理を行う。"
                    //-----------------

                    // 行を区切り文字でトークンに分割。
                    string[] tokens = 行.Split( new char[] { ';', ':' } );

                    // すべてのトークンについて……
                    foreach( string token in tokens )
                    {
                        // トークンを分割。

                        #region " トークンを区切り文字 '=' で strコマンド と strパラメータ に分割し、それぞれの先頭末尾の空白を削除する。"
                        //-----------------
                        string[] items = token.Split( '=' );

                        if( 2 != items.Length )
                        {
                            if( 0 == token.Trim().Length )  // 空文字列（行末など）は不正じゃない。
                                continue;

                            Trace.TraceError( $"コマンドとパラメータの記述書式が不正です。このコマンドをスキップします。[{行番号}行目]" );
                            continue;
                        }

                        string コマンド = items[ 0 ].Trim();
                        string パラメータ = items[ 1 ].Trim();
                        //-----------------
                        #endregion

                        // コマンド別に処理。

                        if( コマンド.Equals( "Part", StringComparison.OrdinalIgnoreCase ) )
                        {
                            #region " Part（小節番号指定）コマンド "
                            //-----------------

                            #region " 小節番号を取得・設定。"
                            //-----------------
                            string 小節番号文字列 = this._指定された文字列の先頭から数字文字列を取り出す( ref パラメータ );

                            if( string.IsNullOrEmpty( 小節番号文字列 ) )
                            {
                                Trace.TraceError( $"Part（小節番号）コマンドに小節番号の記述がありません。このコマンドをスキップします。[{行番号}行目]" );
                                continue;
                            }

                            if( false == int.TryParse( 小節番号文字列, out int 小節番号 ) )
                            {
                                Trace.TraceError( $"Part（小節番号）コマンドの小節番号が不正です。このコマンドをスキップします。[{行番号}行目]" );
                                continue;
                            }
                            if( 0 > 小節番号 )
                            {
                                Trace.TraceError( $"Part（小節番号）コマンドの小節番号が負数です。このコマンドをスキップします。[{行番号}行目]" );
                                continue;
                            }

                            現在の小節番号 = 小節番号;
                            //-----------------
                            #endregion
                            #region " Part 属性があれば取得する。"
                            //-----------------
                            while( 0 < パラメータ.Length )
                            {
                                // 属性ID を取得。
                                char 属性ID = char.ToLower( パラメータ[ 0 ] );

                                // Part 属性があれば取得する。
                                if( 属性ID == 's' )
                                {
                                    #region " 小節長倍率(>0) → list小節長倍率 "
                                    //-----------------
                                    パラメータ = パラメータ.Substring( 1 ).Trim();

                                    string 小節長倍率文字列 = this._指定された文字列の先頭から数字文字列を取り出す( ref パラメータ );
                                    if( string.IsNullOrEmpty( 小節長倍率文字列 ) )
                                    {
                                        Trace.TraceError( $"Part（小節番号）コマンドに小節長倍率の記述がありません。この属性をスキップします。[{行番号}行目]" );
                                        continue;
                                    }
                                    パラメータ = パラメータ.Trim();

                                    if( false == double.TryParse( 小節長倍率文字列, out double 小節長倍率 ) )
                                    {
                                        Trace.TraceError( $"Part（小節番号）コマンドの小節長倍率が不正です。この属性をスキップします。[{行番号}行目]" );
                                        continue;
                                    }
                                    if( 0.0 >= 小節長倍率 )
                                    {
                                        Trace.TraceError( $"Part（小節番号）コマンドの小節長倍率が 0.0 または負数です。この属性をスキップします。[{行番号}行目]" );
                                        continue;
                                    }

                                    // 小節長倍率辞書に追加 or 上書き更新。
                                    this.小節長倍率を設定する( 現在の小節番号, 小節長倍率 );
                                    //-----------------
                                    #endregion

                                    continue;
                                }
                            }
                            //-----------------
                            #endregion

                            //-----------------
                            #endregion

                            continue;
                        }
                        if( コマンド.Equals( "Lane", StringComparison.OrdinalIgnoreCase ) )
                        {
                            #region " Lane（レーン指定）コマンド（チップ種別の仮決め）"
                            //-----------------
                            if( パラメータ.Equals( "LeftCrash", StringComparison.OrdinalIgnoreCase ) )
                                e現在のチップ = チップ種別.LeftCrash;

                            else if( パラメータ.Equals( "Ride", StringComparison.OrdinalIgnoreCase ) )
                                e現在のチップ = チップ種別.Ride;

                            else if( パラメータ.Equals( "China", StringComparison.OrdinalIgnoreCase ) )
                                e現在のチップ = チップ種別.China;

                            else if( パラメータ.Equals( "Splash", StringComparison.OrdinalIgnoreCase ) )
                                e現在のチップ = チップ種別.Splash;

                            else if( パラメータ.Equals( "HiHat", StringComparison.OrdinalIgnoreCase ) )
                                e現在のチップ = チップ種別.HiHat_Close;

                            else if( パラメータ.Equals( "Snare", StringComparison.OrdinalIgnoreCase ) )
                                e現在のチップ = チップ種別.Snare;

                            else if( パラメータ.Equals( "Bass", StringComparison.OrdinalIgnoreCase ) )
                                e現在のチップ = チップ種別.Bass;

                            else if( パラメータ.Equals( "Tom1", StringComparison.OrdinalIgnoreCase ) )
                                e現在のチップ = チップ種別.Tom1;

                            else if( パラメータ.Equals( "Tom2", StringComparison.OrdinalIgnoreCase ) )
                                e現在のチップ = チップ種別.Tom2;

                            else if( パラメータ.Equals( "Tom3", StringComparison.OrdinalIgnoreCase ) )
                                e現在のチップ = チップ種別.Tom3;

                            else if( パラメータ.Equals( "RightCrash", StringComparison.OrdinalIgnoreCase ) )
                                e現在のチップ = チップ種別.RightCrash;

                            else if( パラメータ.Equals( "BPM", StringComparison.OrdinalIgnoreCase ) )
                                e現在のチップ = チップ種別.BPM;

                            else if( パラメータ.Equals( "Song", StringComparison.OrdinalIgnoreCase ) )
                                e現在のチップ = チップ種別.背景動画;
                            else
                                Trace.TraceError( $"Lane（レーン指定）コマンドのパラメータ記述 '{パラメータ}' が不正です。このコマンドをスキップします。[{行番号}行目]" );
                            //-----------------
                            #endregion

                            continue;
                        }
                        if( コマンド.Equals( "Resolution", StringComparison.OrdinalIgnoreCase ) )
                        {
                            #region " Resolution（小節解像度指定）コマンド "
                            //-----------------
                            if( false == int.TryParse( パラメータ, out int 解像度 ) )
                            {
                                Trace.TraceError( $"Resolution（小節解像度指定）コマンドの解像度が不正です。このコマンドをスキップします。[{行番号}行目]" );
                                continue;
                            }
                            if( 1 > 解像度 )
                            {
                                Trace.TraceError( $"Resolution（小節解像度指定）コマンドの解像度は 1 以上でなければなりません。このコマンドをスキップします。[{行番号}行目]" );
                                continue;
                            }
                            現在の小節解像度 = 解像度;
                            //-----------------
                            #endregion

                            continue;
                        }
                        if( コマンド.Equals( "Chips", StringComparison.OrdinalIgnoreCase ) )
                        {
                            #region " Chips（チップ指定）コマンド "
                            //-----------------

                            // パラメータを区切り文字 ',' でチップトークンに分割。
                            string[] chipTokens = パラメータ.Split( ',' );

                            // すべてのチップトークンについて……
                            for( int i = 0; i < chipTokens.Length; i++ )
                            {
                                chipTokens[ i ].Trim();

                                #region " 空文字はスキップ。"
                                //-----------------
                                if( 0 == chipTokens[ i ].Length )
                                    continue;
                                //-----------------
                                #endregion
                                #region " チップを生成する。"
                                //-----------------
                                var chip = new チップ() {
                                    小節番号 = 現在の小節番号,
                                    チップ種別 = e現在のチップ,
                                    小節解像度 = 現在の小節解像度,
                                    音量 = チップ.最大音量,
                                };
                                chip.可視 = chip.可視の初期値;
                                if( chip.チップ種別 == チップ種別.China ) chip.チップ内文字列 = "C N";
                                if( chip.チップ種別 == チップ種別.Splash ) chip.チップ内文字列 = "S P";
                                //-----------------
                                #endregion

                                #region " チップ位置を取得する。"
                                //-----------------
                                string 位置番号文字列 = this._指定された文字列の先頭から数字文字列を取り出す( ref chipTokens[ i ] );
                                chipTokens[ i ].Trim();

                                // 文法チェック。
                                if( string.IsNullOrEmpty( 位置番号文字列 ) )
                                {
                                    Trace.TraceError( $"チップの位置指定の記述がありません。このチップをスキップします。[{行番号}行目; {i + 1}個目のチップ]" );
                                    continue;
                                }

                                // 位置を取得。
                                if( false == int.TryParse( 位置番号文字列, out int チップ位置 ) )
                                {
                                    Trace.TraceError( $"チップの位置指定の記述が不正です。このチップをスキップします。[{行番号}行目; {i + 1}個目のチップ]" );
                                    continue;
                                }

                                // 値域チェック。
                                if( ( 0 > チップ位置 ) || ( チップ位置 >= 現在の小節解像度 ) )
                                {
                                    Trace.TraceError( $"チップの位置が負数であるか解像度(Resolution)以上の値になっています。このチップをスキップします。[{行番号}行目; {i + 1}個目のチップ]" );
                                    continue;
                                }

                                chip.小節内位置 = チップ位置;
                                //-----------------
                                #endregion
                                #region " 共通属性・レーン別属性があれば取得する。"
                                //-----------------
                                while( chipTokens[ i ].Length > 0 )
                                {
                                    // 属性ID を取得。
                                    char 属性ID = char.ToLower( chipTokens[ i ][ 0 ] );

                                    // 共通属性があれば取得。
                                    if( 属性ID == 'v' )
                                    {
                                        #region " 音量 "
                                        //-----------------
                                        chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                        string 音量文字列 = this._指定された文字列の先頭から数字文字列を取り出す( ref chipTokens[ i ] );
                                        chipTokens[ i ].Trim();

                                        // 文法チェック。
                                        if( string.IsNullOrEmpty( 音量文字列 ) )
                                        {
                                            Trace.TraceError( $"チップの音量指定の記述がありません。この属性をスキップします。[{行番号}行目; {i + 1}個目のチップ]" );
                                            continue;
                                        }

                                        // チップ音量の取得。
                                        if( false == int.TryParse( 音量文字列, out int チップ音量 ) )
                                        {
                                            Trace.TraceError( $"チップの音量指定の記述が不正です。この属性をスキップします。[{行番号}行目; {i + 1}個目のチップ]" );
                                            continue;
                                        }

                                        // 値域チェック。
                                        if( ( 1 > チップ音量 ) || ( チップ音量 > チップ.最大音量 ) )
                                        {
                                            Trace.TraceError( $"チップの音量が適正範囲（1～{チップ.最大音量}）を超えています。このチップをスキップします。[{行番号}行目; {i + 1}個目のチップ]" );
                                            continue;
                                        }

                                        chip.音量 = チップ音量;
                                        //-----------------
                                        #endregion

                                        continue;
                                    }

                                    // レーン別属性があれば取得。
                                    switch( e現在のチップ )
                                    {
                                        #region " case LeftCymbal "
                                        //-----------------
                                        case チップ種別.LeftCrash:
                                            #region " 未知の属性 "
                                            //-----------------
                                            chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                            Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{行番号}行目; {i + 1}個目のチップ]" );
                                            //-----------------
                                            #endregion
                                            continue;

                                        //-----------------
                                        #endregion
                                        #region " case Ride "
                                        //-----------------
                                        case チップ種別.Ride:
                                        case チップ種別.Ride_Cup:

                                            if( 属性ID == 'c' )
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
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        //-----------------
                                        #endregion
                                        #region " case China "
                                        //-----------------
                                        case チップ種別.China:

                                            #region " 未知の属性 "
                                            //-----------------
                                            chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                            Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{行番号}行目; {i + 1}個目のチップ]" );
                                            //-----------------
                                            #endregion

                                            continue;

                                        //-----------------
                                        #endregion
                                        #region " case Splash "
                                        //-----------------
                                        case チップ種別.Splash:

                                            #region " 未知の属性 "
                                            //-----------------
                                            chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                            Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{行番号}行目; {i + 1}個目のチップ]" );
                                            //-----------------
                                            #endregion

                                            continue;

                                        //-----------------
                                        #endregion
                                        #region " case HiHat "
                                        //-----------------
                                        case チップ種別.HiHat_Close:
                                        case チップ種別.HiHat_HalfOpen:
                                        case チップ種別.HiHat_Open:
                                        case チップ種別.HiHat_Foot:

                                            if( 属性ID == 'o' )
                                            {
                                                #region " HiHat.オープン "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                chip.チップ種別 = チップ種別.HiHat_Open;
                                                //-----------------
                                                #endregion
                                            }
                                            else if( 属性ID == 'h' )
                                            {
                                                #region " HiHat.ハーフオープン "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                chip.チップ種別 = チップ種別.HiHat_HalfOpen;
                                                //-----------------
                                                #endregion
                                            }
                                            else if( 属性ID == 'c' )
                                            {
                                                #region " HiHat.クローズ "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                chip.チップ種別 = チップ種別.HiHat_Close;
                                                //-----------------
                                                #endregion
                                            }
                                            else if( 属性ID == 'f' )
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
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        //-----------------
                                        #endregion
                                        #region " case Snare "
                                        //-----------------
                                        case チップ種別.Snare:
                                        case チップ種別.Snare_ClosedRim:
                                        case チップ種別.Snare_OpenRim:
                                        case チップ種別.Snare_Ghost:

                                            if( 属性ID == 'o' )
                                            {
                                                #region " Snare.オープンリム "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                chip.チップ種別 = チップ種別.Snare_OpenRim;
                                                //-----------------
                                                #endregion
                                            }
                                            else if( 属性ID == 'c' )
                                            {
                                                #region " Snare.クローズドリム "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                chip.チップ種別 = チップ種別.Snare_ClosedRim;
                                                //-----------------
                                                #endregion
                                            }
                                            else if( 属性ID == 'g' )
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
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        //-----------------
                                        #endregion
                                        #region " case Bass "
                                        //-----------------
                                        case チップ種別.Bass:

                                            #region " 未知の属性 "
                                            //-----------------
                                            chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                            Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{行番号}行目; {i + 1}個目のチップ]" );
                                            //-----------------
                                            #endregion

                                            continue;

                                        //-----------------
                                        #endregion
                                        #region " case Tom1 "
                                        //-----------------
                                        case チップ種別.Tom1:
                                        case チップ種別.Tom1_Rim:

                                            if( 属性ID == 'r' )
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
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        //-----------------
                                        #endregion
                                        #region " case Tom2 "
                                        //-----------------
                                        case チップ種別.Tom2:
                                        case チップ種別.Tom2_Rim:

                                            if( 属性ID == 'r' )
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
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        //-----------------
                                        #endregion
                                        #region " case Tom3 "
                                        //-----------------
                                        case チップ種別.Tom3:
                                        case チップ種別.Tom3_Rim:

                                            if( 属性ID == 'r' )
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
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        //-----------------
                                        #endregion
                                        #region " case RightCymbal "
                                        //-----------------
                                        case チップ種別.RightCrash:

                                            #region " 未知の属性 "
                                            //-----------------
                                            chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                            Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{行番号}行目; {i + 1}個目のチップ]" );
                                            //-----------------
                                            #endregion

                                            continue;

                                        //-----------------
                                        #endregion
                                        #region " case BPM "
                                        //-----------------
                                        case チップ種別.BPM:

                                            if( 属性ID == 'b' )
                                            {
                                                #region " BPM値 "
                                                //-----------------

                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();

                                                string BPM文字列 = this._指定された文字列の先頭から数字文字列を取り出す( ref chipTokens[ i ] );
                                                chipTokens[ i ].Trim();

                                                if( string.IsNullOrEmpty( BPM文字列 ) )
                                                {
                                                    Trace.TraceError( $"BPM数値の記述がありません。この属性をスキップします。[{行番号}行目; {i + 1}個目のチップ]" );
                                                    continue;
                                                }

                                                if( false == double.TryParse( BPM文字列, out double BPM ) || ( 0.0 >= BPM ) )
                                                {
                                                    Trace.TraceError( $"BPM数値の記述が不正です。この属性をスキップします。[{行番号}行目; {i + 1}個目のチップ]" );
                                                    continue;
                                                }

                                                chip.BPM = BPM;
                                                chip.チップ内文字列 = BPM.ToString( "###.##" );
                                                //-----------------
                                                #endregion
                                            }
                                            else
                                            {
                                                #region " 未知の属性 "
                                                //-----------------
                                                chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                                Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{行番号}行目; {i + 1}個目のチップ]" );
                                                //-----------------
                                                #endregion
                                            }
                                            continue;

                                        //-----------------
                                        #endregion
                                        #region " case Song "
                                        //-----------------
                                        case チップ種別.背景動画:

                                            #region " 未知の属性 "
                                            //-----------------
                                            chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                            Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{行番号}行目; {i + 1}個目のチップ]" );
                                            //-----------------
                                            #endregion

                                            continue;

                                            //-----------------
                                            #endregion
                                    }

                                    #region " 未知の属性 "
                                    //-----------------
                                    chipTokens[ i ] = chipTokens[ i ].Substring( 1 ).Trim();
                                    Trace.TraceError( $"未対応の属性「{属性ID}」が指定されています。この属性をスキップします。[{行番号}行目; {i + 1}個目のチップ]" );
                                    //-----------------
                                    #endregion
                                }
                                //-----------------
                                #endregion

                                this.チップリスト.Add( chip );
                            }
                            //-----------------
                            #endregion

                            continue;
                        }

                        Trace.TraceError( $"不正なコマンド「{コマンド}」が存在します。[{行番号}行目]" );
                    }
                    //-----------------
                    #endregion
                }

                sr.Close();
            }
            //-----------------
            #endregion
            #region " 拍線の追加。小節線を先に追加すると小節が１つ増えるので、先に拍線から追加する。"
            //-----------------
            int 最大小節番号 = this.最大小節番号;       // this.最大小節番号 プロパティはチップ数に依存して変化するので、for 文には組み込まないこと。

            for( int i = 0; i <= 最大小節番号; i++ )
            {
                double 小節長倍率 = this.小節長倍率を取得する( i );
                for( int n = 1; n * 0.25 < 小節長倍率; n++ )
                {
                    this.チップリスト.Add(
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
            #region " 小節線の追加。"
            //-----------------
            最大小節番号 = this.最大小節番号;

            for( int i = 0; i <= 最大小節番号 + 1; i++ )
            {
                this.チップリスト.Add(
                    new チップ() {
                        小節番号 = i,
                        チップ種別 = チップ種別.小節線,
                        小節内位置 = 0,
                        小節解像度 = 1,
                    } );
            }
            //-----------------
            #endregion

            this.チップリスト.Sort();

            #region " 全チップの発声/描画時刻と譜面内位置を計算する。"
            //-----------------

            // 1. BPMチップを無視し(初期BPMで固定)、dic小節長倍率, Cチップ.小節解像度, Cチップ.小節内位置 から両者を計算する。
            //    以下、listチップが小節番号順にソートされているという前提で。

            double チップが存在する小節の先頭時刻ms = 0.0;
            int 現在の小節の番号 = 0;

            foreach( チップ chip in this.チップリスト )
            {
                #region " チップの小節番号が現在の小節番号よりも大きい場合、チップが存在する小節に至るまで、「dbチップが存在する小節の先頭時刻ms」を更新する。"
                //-----------------
                while( 現在の小節の番号 < chip.小節番号 )
                {
                    double 現在の小節の小節長倍率 = this.小節長倍率を取得する( 現在の小節の番号 );
                    チップが存在する小節の先頭時刻ms += BPM初期値固定での1小節4拍の時間ms * 現在の小節の小節長倍率;

                    現在の小節の番号++;    // 現在の小節番号 が chip.小節番号 に追いつくまでループする。
                }
                //-----------------
                #endregion
                #region " チップの発声/描画時刻を求める。"
                //-----------------
                double チップが存在する小節の小節長倍率 = this.小節長倍率を取得する( 現在の小節の番号 );

                chip.発声時刻ms =
                    chip.描画時刻ms =
                        (long) ( チップが存在する小節の先頭時刻ms + ( BPM初期値固定での1小節4拍の時間ms * チップが存在する小節の小節長倍率 * chip.小節内位置 ) / chip.小節解像度 );
                //-----------------
                #endregion
            }

            // 2. BPMチップを考慮しながら調整する。（譜面内位置grid はBPMの影響を受けないので無視）

            double 現在のBPM = スコア.初期BPM;
            int チップ数 = this.チップリスト.Count;
            for( int i = 0; i < チップ数; i++ )
            {
                // BPM チップ以外は無視。
                var BPMチップ = this.チップリスト[ i ];
                if( BPMチップ.チップ種別 != チップ種別.BPM )
                    continue;

                // BPMチップより後続の全チップの n発声/描画時刻ms を、新旧BPMの比率（加速率）で修正する。
                double 加速率 = BPMチップ.BPM / 現在のBPM; // BPMチップ.dbBPM > 0.0 であることは読み込み時に保証済み。
                for( int j = i + 1; j < チップ数; j++ )
                {
                    long 時刻ms = (long) ( BPMチップ.発声時刻ms + ( ( this.チップリスト[ j ].発声時刻ms - BPMチップ.発声時刻ms ) / 加速率 ) );

                    this.チップリスト[ j ].発声時刻ms = 時刻ms;
                    this.チップリスト[ j ].描画時刻ms = 時刻ms;
                }

                現在のBPM = BPMチップ.BPM;
            }
            //-----------------
            #endregion
        }

        public void 曲データファイルを読み込む_ヘッダだけ( string 曲データファイル名 )
        {
            #region " SSTFバージョンチェック。"
            //----------------
            var version = SSTFVersion.CreateVersionFromFile( 曲データファイル名 );
            if( version.Major != 1 )
                throw new ArgumentException( "この曲データのバージョンは、1.x.x.x ではありません。" );
            //----------------
            #endregion

            this.小節長倍率リスト = new List<double>();

            // 曲データファイルのヘッダ部を読み込む。
            using( var sr = new StreamReader( 曲データファイル名, Encoding.UTF8 ) )
            {
                int 行番号 = 0;

                while( false == sr.EndOfStream )
                {
                    // 1行ずつ読み込む。

                    行番号++;
                    string 行 = this._行を読み込む( sr );

                    if( string.IsNullOrEmpty( 行 ) )
                        continue;

                    // ヘッダコマンド処理。

                    #region " ヘッダコマンドの処理を行う。"
                    //-----------------
                    if( 1 == 行番号 && // 先頭行に限る。
                        行.StartsWith( "SSTFVersion", StringComparison.OrdinalIgnoreCase ) )
                    {
                        #region " SSTFバージョン "
                        //----------------
                        string[] items = 行.Split( ' ' );    // SPACE 1文字 で統一するので注意

                        if( 2 != items.Length )
                        {
                            Trace.TraceError( $"SSTFVersion の書式が不正です。スキップします（バージョンは1.0.0.0と見なされます）。[{行番号}行目]" );
                            continue;
                        }
                        try
                        {
                            this.Header.SSTFバージョン = new SSTFVersion( items[ 1 ].Trim() );   // string から Version へ変換できる書式であること。（例: "1.2.3.4"）
                        }
                        catch
                        {
                            Trace.TraceError( $"SSTFVersion のバージョン書式が不正です。スキップします（バージョンは1.0.0.0と見なされます）。[{行番号}行目]" );
                            continue;
                        }
                        //----------------
                        #endregion

                        continue;
                    }
                    if( 行.StartsWith( "Title", StringComparison.OrdinalIgnoreCase ) )
                    {
                        #region " Title コマンド "
                        //-----------------
                        string[] items = 行.Split( '=' );

                        if( 2 != items.Length )
                        {
                            Trace.TraceError( $"Title の書式が不正です。スキップします。[{行番号}行目]" );
                            continue;
                        }

                        this.Header.曲名 = items[ 1 ].Trim();
                        //-----------------
                        #endregion

                        continue;
                    }
                    if( 行.StartsWith( "Description", StringComparison.OrdinalIgnoreCase ) )
                    {
                        #region " Description コマンド "
                        //-----------------
                        string[] items = 行.Split( '=' );

                        if( 2 != items.Length )
                        {
                            Trace.TraceError( $"Description の書式が不正です。スキップします。[{行番号}行目]" );
                            continue;
                        }

                        // ２文字のリテラル "\n" は改行に復号。
                        this.Header.説明文 = items[ 1 ].Trim().Replace( @"\n", Environment.NewLine );
                        //-----------------
                        #endregion

                        continue;
                    }
                    //-----------------
                    #endregion

                    // 上記行頭コマンド以外は無視。
                }
            }
        }

        /// <summary>
        ///		現在の スコア の内容をデータファイル（*.sstf）に書き出す。
        /// </summary>
        /// <remarks>
        ///		小節線、拍線、Unknown チップは出力しない。
        ///		失敗すれば何らかの例外を発出する。
        /// </remarks>
        public void 曲データファイルを書き出す( string 曲データファイル名, string ヘッダ行 )
        {
            using( var sw = new StreamWriter( 曲データファイル名, false, Encoding.UTF8 ) )
            {
                // SSTFバージョンの出力
                sw.WriteLine( $"# SSTFVersion {this.Header.SSTFバージョン.ToString()}" );

                // ヘッダ行の出力
                sw.WriteLine( $"{ヘッダ行}" );    // strヘッダ行に"{...}"が入ってても大丈夫なようにstring.Format()で囲む。
                sw.WriteLine( "" );

                // ヘッダコマンド行の出力
                sw.WriteLine( "Title=" + ( ( string.IsNullOrEmpty( this.Header.曲名 ) ) ? "(no title)" : this.Header.曲名 ) );
                if( false == string.IsNullOrEmpty( this.Header.説明文 ) )
                {
                    // 改行コードは、２文字のリテラル "\n" に置換。
                    sw.WriteLine( "Description=" + this.Header.説明文.Replace( Environment.NewLine, @"\n" ) );
                }
                sw.WriteLine( "SoundDevice.Delay={0}", this.Header.サウンドデバイス遅延ms );
                sw.WriteLine( "" );

                // 全チップの出力

                #region " 全チップの最終小節番号を取得する。"
                //-----------------
                int 最終小節番号 = 0;
                foreach( var cc in this.チップリスト )
                {
                    if( cc.小節番号 > 最終小節番号 )
                        最終小節番号 = cc.小節番号;
                }
                //-----------------
                #endregion

                for( int 小節番号 = 0; 小節番号 <= 最終小節番号; 小節番号++ )
                {
                    #region " dicレーン別チップリストの初期化。"
                    //-----------------
                    var dicレーン別チップリスト = new Dictionary<レーン種別, List<チップ>>();

                    foreach( レーン種別 laneType in Enum.GetValues( typeof( レーン種別 ) ) )
                        dicレーン別チップリスト[ laneType ] = new List<チップ>();
                    //-----------------
                    #endregion
                    #region " dicレーン別チップリストの構築； 小節番号 の小節に存在するチップのみをレーン別に振り分けて格納する。"
                    //-----------------
                    foreach( var cc in this.チップリスト )
                    {
                        #region " 出力しないチップ種別は無視。"
                        //----------------
                        if( cc.チップ種別 == チップ種別.小節線 ||
                            cc.チップ種別 == チップ種別.拍線 ||
                            cc.チップ種別 == チップ種別.小節メモ ||
                            cc.チップ種別 == チップ種別.Unknown )
                        {
                            continue;
                        }
                        //----------------
                        #endregion

                        if( cc.小節番号 > 小節番号 )
                        {
                            // チップリストは昇順に並んでいるので、これ以上検索しても無駄。
                            break;
                        }
                        else if( cc.小節番号 == 小節番号 )
                        {
                            var lane = レーン種別.Bass;   // 対応するレーンがなかったら Bass でも返しておく。

                            foreach( var kvp in dicSSTFレーンチップ対応表 )
                            {
                                if( kvp.Value.Contains( cc.チップ種別 ) )
                                {
                                    lane = kvp.Key;
                                    break;
                                }
                            }

                            dicレーン別チップリスト[ lane ].Add( cc );
                        }
                    }
                    //-----------------
                    #endregion

                    #region " Part行 出力。"
                    //-----------------
                    sw.Write( $"Part = {小節番号.ToString()}" );

                    if( this.小節長倍率リスト[ 小節番号 ] != 1.0 )
                        sw.Write( $"s{this.小節長倍率リスト[ 小節番号 ].ToString()}" );

                    sw.WriteLine( ";" );
                    //-----------------
                    #endregion
                    #region " Lane, Resolution, Chip 行 出力。"
                    //-----------------
                    foreach( レーン種別 laneType in Enum.GetValues( typeof( レーン種別 ) ) )
                    {
                        if( 0 < dicレーン別チップリスト[ laneType ].Count )
                        {
                            sw.Write( $"Lane={laneType.ToString()}; " );

                            #region " 新しい解像度を求める。"
                            //-----------------
                            int 新しい解像度 = 1;
                            foreach( var cc in dicレーン別チップリスト[ laneType ] )
                                新しい解像度 = this._最小公倍数を返す( 新しい解像度, cc.小節解像度 );
                            //-----------------
                            #endregion
                            #region " dicレーン別チップリスト[ lane ] 要素の 小節解像度 と 小節内位置 を 新しい解像度 に合わせて修正する。 "
                            //-----------------
                            foreach( var cc in dicレーン別チップリスト[ laneType ] )
                            {
                                int 倍率 = 新しい解像度 / cc.小節解像度;      // 新しい解像度 は 小節解像度 の最小公倍数なので常に割り切れる。

                                cc.小節解像度 *= 倍率;
                                cc.小節内位置 *= 倍率;
                            }
                            //-----------------
                            #endregion

                            sw.Write( $"Resolution = {新しい解像度}; " );
                            sw.Write( "Chips = " );

                            for( int i = 0; i < dicレーン別チップリスト[ laneType ].Count; i++ )
                            {
                                チップ cc = dicレーン別チップリスト[ laneType ][ i ];

                                // 位置を出力。
                                sw.Write( cc.小節内位置.ToString() );

                                // 属性を出力（あれば）。

                                #region " (1) 共通属性 "
                                //-----------------
                                if( cc.音量 < チップ.最大音量 )
                                    sw.Write( $"v{cc.音量.ToString()}" );
                                //-----------------
                                #endregion
                                #region " (2) 専用属性 "
                                //-----------------
                                switch( cc.チップ種別 )
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

                                    case チップ種別.BPM:
                                        sw.Write( $"b{cc.BPM.ToString()}" );
                                        break;
                                }
                                //-----------------
                                #endregion

                                // 区切り文字 または 終端文字 を出力
                                sw.Write( ( i == dicレーン別チップリスト[ laneType ].Count - 1 ) ? ";" : "," );
                            }

                            sw.WriteLine( "" ); // 改行
                        }
                    }
                    //-----------------
                    #endregion

                    sw.WriteLine( "" ); // 次の Part 前に１行あける。
                }

                // メモ（小節単位）の出力

                #region " dicメモ を小節番号で昇順に出力する。"
                //-----------------
                var dic昇順メモ = new Dictionary<int, string>();
                int 最大小節番号 = this.最大小節番号;

                for( int i = 0; i <= 最大小節番号; i++ )
                {
                    if( this.dicメモ.ContainsKey( i ) )
                        dic昇順メモ.Add( i, this.dicメモ[ i ] );
                }

                foreach( var kvp in dic昇順メモ )
                {
                    int 小節番号 = kvp.Key;

                    // 改行コードは、２文字のリテラル "\n" に置換。
                    string メモ = kvp.Value.Replace( Environment.NewLine, @"\n" );

                    sw.WriteLine( $"PartMemo={小節番号},{メモ}" );
                }

                sw.WriteLine( "" );
                //-----------------
                #endregion

                sw.Close();
            }
        }

        /// <summary>
        ///		指定された Config.Speed を考慮し、指定された時間[ms]の間に流れるピクセル数[dpx]を算出して返す。</para>
        /// </summary>
        [Obsolete( "指定時間がミリ秒単位ではなく秒単位であるメソッドを使用してください。" )]
        public int 指定された時間msに対応する符号付きピクセル数を返す( double speed, long 指定時間ms )
        {
            return (int) ( 指定時間ms * スコア.基準譜面速度dpxms * speed );
        }

        /// <summary>
        ///		指定された Config.Speed を考慮し、指定された時間[秒]の間に流れるピクセル数[dpx]を算出して返す。
        /// </summary>
        public double 指定された時間secに対応する符号付きピクセル数を返す( double speed, double 指定時間sec )
        {
            return ( 指定時間sec * スコア.基準譜面速度dpxsec * speed );
        }

        public double 小節長倍率を取得する( int 小節番号 )
        {
            // 小節長倍率リスト が短ければ増設する。
            if( 小節番号 >= this.小節長倍率リスト.Count )
            {
                int 不足数 = 小節番号 - this.小節長倍率リスト.Count + 1;
                for( int i = 0; i < 不足数; i++ )
                    this.小節長倍率リスト.Add( 1.0 );
            }

            // 小節番号に対応する倍率を返す。
            return this.小節長倍率リスト[ 小節番号 ];
        }

        public void 小節長倍率を設定する( int 小節番号, double 倍率 )
        {
            // 小節長倍率リスト が短ければ増設する。
            if( 小節番号 >= this.小節長倍率リスト.Count )
            {
                int 不足数 = 小節番号 - this.小節長倍率リスト.Count + 1;
                for( int i = 0; i < 不足数; i++ )
                    this.小節長倍率リスト.Add( 1.0 );
            }

            // 小節番号に対応付けて倍率を登録する。
            this.小節長倍率リスト[ 小節番号 ] = 倍率;
        }

        /// <summary>
        ///		取出文字列の先頭にある数字（小数点も有効）の連続した部分を取り出して、戻り値として返す。
        ///		また、取出文字列から取り出した数字文字列部分を除去した文字列を再度格納する。
        /// </summary>
        private string _指定された文字列の先頭から数字文字列を取り出す( ref string 取出文字列 )
        {
            int 桁数 = 0;
            while( ( 桁数 < 取出文字列.Length ) && ( char.IsDigit( 取出文字列[ 桁数 ] ) || 取出文字列[ 桁数 ] == '.' ) )
                桁数++;

            if( 0 == 桁数 )
                return "";

            string 数字文字列 = 取出文字列.Substring( 0, 桁数 );
            取出文字列 = ( 桁数 == 取出文字列.Length ) ? "" : 取出文字列.Substring( 桁数 );

            return 数字文字列;
        }

        /// <summary>
        ///		Stream から１行読み込み、コメントや改行等の前処理を行ってから返す。
        /// </summary>
        /// <param name="reader">
        ///		行の読み込み元。読み込んだ分、ポジションは進められる。
        /// </param>
        /// <returns>
        ///		読み込んで、コメントや改行等の前処理を適用したあとの行を返す。
        ///		reader が EOF だった場合には null を返す。
        /// </returns>
        private string _行を読み込む( StreamReader reader )
        {
            if( reader.EndOfStream )
                return null;

            // 1行読み込む。
            string 行 = reader.ReadLine();

            // (1) 改行とTABを空白文字に変換し、先頭末尾の空白を削除する。
            行 = 行.Replace( Environment.NewLine, " " );
            行 = 行.Replace( '\t', ' ' );
            行 = 行.Trim();

            // (2) 行中の '#' 以降はコメントとして除外する。
            int 区切り位置 = 行.IndexOf( '#' );
            if( 0 <= 区切り位置 )
            {
                行 = 行.Substring( 0, 区切り位置 );
                行 = 行.Trim();
            }

            return 行;
        }

        private int _最小公倍数を返す( int m, int n )
        {
            if( ( 0 >= m ) || ( 0 >= n ) )
                throw new ArgumentOutOfRangeException( "引数に0以下の数は指定できません。" );

            return ( m * n / this._最大公約数を返す( m, n ) );
        }

        private int _最大公約数を返す( int m, int n )
        {
            if( ( 0 >= m ) || ( 0 >= n ) )
                throw new ArgumentOutOfRangeException( "引数に0以下の数は指定できません。" );

            // ユーグリッドの互除法
            int r;
            while( ( r = m % n ) != 0 )
            {
                m = n;
                n = r;
            }

            return n;
        }
    }
}
