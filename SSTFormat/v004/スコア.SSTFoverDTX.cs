using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SSTFormat.v004
{
    public partial class スコア
    {
        public static class SSTFoverDTX
        {
            /// <summary>
            ///     指定されたファイルが SSTF over DTX 形式のファイルであれば true を返す。
            /// </summary>
            public static bool ファイルがSSTFoverDTXである( string ファイルの絶対パス )
            {
                string 行;
                using( var sr = new StreamReader( ファイルの絶対パス, Encoding.GetEncoding( "shift-jis" ) ) )  // SSTFoverDTX は Shift-JIS
                    行 = sr.ReadLine().Trim();

                int コメント識別子の位置 = 行.IndexOf( '#' );    // 見つからなければ -1
                if( 0 > コメント識別子の位置 )
                    return false;   // 先頭行がコメント行ではない

                var コメント文 = 行[ ( コメント識別子の位置 + 1 ).. ].Trim();
                return コメント文.ToLower().StartsWith( "sstf over dtx" );   // コメント文が sstf over dtx で始まっていれば true
            }

            /// <summary>
            ///		ファイルをSSTFoverDTXデータであるとみなして読み込み、スコアを生成して返す。
            ///		読み込みに失敗した場合は、何らかの例外を発出する。
            /// </summary>
            public static スコア ファイルから生成する( string SSTFoverDTXファイルの絶対パス, bool ヘッダだけ = false )
            {
                // DTX ファイルとして読み込み、スコアを生成する。
                var score = DTX.ファイルから生成する( SSTFoverDTXファイルの絶対パス, DTX.データ種別.DTX, ヘッダだけ );

                // スコアを SSTFoverDTX 仕様に基づいて復元する。
                _SSTFoverDTXからSSTFに復元する( score );

                return score;
            }

            /// <summary>
            ///     文字列がSSTFoverDTXフォーマットのテキストデータを含むとみなして読み込み、スコアを生成して返す。
            ///		読み込みに失敗した場合は、何らかの例外を発出する。
            /// </summary>
            public static スコア 文字列から生成する( string 全入力文字列, bool ヘッダだけ = false )
            {
                // DTX ファイルとして読み込み、スコアを生成する。
                var score = DTX.文字列から生成する( 全入力文字列, DTX.データ種別.DTX, ヘッダだけ );

                // スコアを SSTFoverDTX 仕様に基づいて復元する。
                _SSTFoverDTXからSSTFに復元する( score );

                return score;
            }

            /// <summary>
            ///		現在の スコア の内容をDTX互換形式ファイル（*.dtx）に書き出す。
            ///		小節線、拍線、Unknown チップは出力しない。
            ///		失敗時は何らかの例外を発出する。
            /// </summary>
            public static void 出力する( スコア score, Stream 出力先, string 追加ヘッダ文 = null )
            {
                using var sw = new StreamWriter( 出力先, Encoding.GetEncoding( "shift_jis" ) );

                // バージョンの出力
                sw.WriteLine( $"# SSTF over DTX, SSTFVersion {SSTFVERSION}" );

                // 追加ヘッダの出力（あれば）
                if( !( string.IsNullOrEmpty( 追加ヘッダ文 ) ) )
                {
                    sw.WriteLine( $"{追加ヘッダ文}" );    // ヘッダ文に "{...}" が入ってても大丈夫なように、$"{...}" で囲む。
                    sw.WriteLine( "" );
                }

                _ヘッダ行を出力する( score, sw );

                _BPMを出力する( score, sw );

                _小節長倍率を出力する( score, sw );

                _WAVとVOLUMEを出力する( score, sw );

                _オブジェクト記述を出力する( score, sw );

                sw.Close();
            }



            // 入力


            /// <summary>
            ///     通常のDTXファイルとして読み込まれたスコアを、SSTFoverDTX の仕様に基づいて復元する。
            /// </summary>
            private static void _SSTFoverDTXからSSTFに復元する( スコア score )
            {
                foreach( var chip in score.チップリスト )
                {
                    switch( chip.チップ種別 )
                    {
                        case チップ種別.SE1:
                        {
                            #region " SE1,18～1F → LeftCymbal_Mute "
                            //----------------
                            int zz = chip.チップサブID - _zz( "18" );

                            if( 0 <= zz && zz < 8 )
                            {
                                chip.チップ種別 = チップ種別.LeftCymbal_Mute;
                                chip.音量 = zz + 1;
                            }
                            //----------------
                            #endregion

                            break;
                        }
                        case チップ種別.HiHat_Open:
                        {
                            #region " HiHat_Open,2G～2N → HiHat_HalfOpen "
                            //----------------
                            int zz = chip.チップサブID - _zz( "2G" );
                            
                            if( 0 <= zz && zz < 8)
                            {
                                chip.チップ種別 = チップ種別.HiHat_HalfOpen;
                                chip.音量 = zz + 1;
                            }
                            //----------------
                            #endregion

                            break;
                        }
                        case チップ種別.Snare:
                        {
                            #region " Snare,38～3F → Snare_OpenRim "
                            //----------------
                            int zz = chip.チップサブID - _zz( "38" );

                            if( 0 <= zz && zz < 8 )
                            {
                                chip.チップ種別 = チップ種別.Snare_OpenRim;
                                chip.音量 = zz + 1;
                            }
                            //----------------
                            #endregion

                            #region " Snare,3G～3N → Snare_ClosedRim "
                            //----------------
                            else if( 8 <= zz && zz < 16 )
                            {
                                chip.チップ種別 = チップ種別.Snare_ClosedRim;
                                chip.音量 = zz - 7;
                            }
                            //----------------
                            #endregion

                            #region " Snare,3O～3V → Snare_Ghost "
                            //----------------
                            if( 16 <= zz && zz < 24 )
                            {
                                chip.チップ種別 = チップ種別.Snare_Ghost;
                                chip.音量 = zz - 15;
                            }
                            //----------------
                            #endregion

                            break;
                        }
                        case チップ種別.Tom1:
                        {
                            #region " Tom1,58～5F → Tom1_Rim "
                            //----------------
                            int zz = chip.チップサブID - _zz( "58" );

                            if( 0 <= zz && zz < 8 )
                            {
                                chip.チップ種別 = チップ種別.Tom1_Rim;
                                chip.音量 = zz + 1;
                            }
                            //----------------
                            #endregion

                            break;
                        }
                        case チップ種別.Tom2:
                        {
                            #region " Tom2,68～6F → Tom2_Rim "
                            //----------------
                            int zz = chip.チップサブID - _zz( "68" );

                            if( 0 <= zz && zz < 8 )
                            {
                                chip.チップ種別 = チップ種別.Tom2_Rim;
                                chip.音量 = zz + 1;
                            }
                            //----------------
                            #endregion

                            break;
                        }
                        case チップ種別.Tom3:
                        {
                            #region " Tom3,78～7F → Tom3_Rim "
                            //----------------
                            int zz = chip.チップサブID - _zz( "78" );

                            if( 0 <= zz && zz < 8 )
                            {
                                chip.チップ種別 = チップ種別.Tom3_Rim;
                                chip.音量 = zz + 1;
                            }
                            //----------------
                            #endregion

                            break;
                        }
                        case チップ種別.SE2:
                        {
                            #region " SE2,88～8F → RightCymbal_Mute "
                            //----------------
                            int zz = chip.チップサブID - _zz( "88" );

                            if( 0 <= zz && zz < 8 )
                            {
                                chip.チップ種別 = チップ種別.RightCymbal_Mute;
                                chip.音量 = zz + 1;
                            }
                            //----------------
                            #endregion

                            break;
                        }
                        case チップ種別.Ride:
                        {
                            #region " Ride,98～9F → Ride_Cup "
                            //----------------
                            int zz = chip.チップサブID - _zz( "98" );

                            if( 0 <= zz && zz < 8 )
                            {
                                chip.チップ種別 = チップ種別.Ride_Cup;
                                chip.音量 = zz + 1;
                            }
                            //----------------
                            #endregion

                            break;
                        }
                        case チップ種別.RightCrash:
                        {
                            #region " RightCrash,A0～A7 → China "
                            //----------------
                            int zz = chip.チップサブID - _zz( "A0" );

                            if( 0 <= zz && zz < 8 )
                            {
                                chip.チップ種別 = チップ種別.China;
                                chip.音量 = zz + 1;
                            }
                            //----------------
                            #endregion

                            break;
                        }
                        case チップ種別.LeftCrash:
                        {
                            #region " LeftCrash,B0～B7 → Splash "
                            //----------------
                            int zz = chip.チップサブID - _zz( "B0" );

                            if( 0 <= zz && zz < 8 )
                            {
                                chip.チップ種別 = チップ種別.Splash;
                                chip.音量 = zz + 1;
                            }
                            //----------------
                            #endregion

                            break;
                        }
                    }
                }
            }



            // 出力


            private static void _ヘッダ行を出力する( スコア score, StreamWriter sw )
            {
                #region " 曲名 → #TITLE "
                //----------------
                if( !string.IsNullOrEmpty( score.曲名 ) )
                {
                    sw.WriteLine( $"#TITLE: {score.曲名}" );
                }
                else
                {
                    sw.WriteLine( $"#TITLE: (no title)" );
                }
                //----------------
                #endregion

                #region " アーティスト名 → #ARTIST "
                //----------------
                if( !string.IsNullOrEmpty( score.アーティスト名 ) )
                {
                    sw.WriteLine( $"#ARTIST: {score.アーティスト名}" );
                }
                else
                {
                    // 省略可。
                }
                //----------------
                #endregion

                #region " 説明文 → #COMMENT "
                //----------------
                if( !string.IsNullOrEmpty( score.説明文 ) )
                {
                    sw.WriteLine( $"#COMMENT: {score.説明文}" );
                }
                else
                {
                    // 省略可。
                }
                //----------------
                #endregion

                #region " 難易度(0.00～9.99) → #DLEVEL(1～100) "
                //----------------
                if( 0.0 < score.難易度 )
                {
                    sw.WriteLine( $"#DLEVEL: {Math.Clamp( (int) ( score.難易度 * 10 ), min: 1, max: 100 ) }" );
                }
                else
                {
                    sw.WriteLine( $"#DLEVEL: 1" );  // 0 や省略は不可。
                }
                //----------------
                #endregion

                #region " プレビュー音声 → #PREVIEW "
                //----------------
                if( !string.IsNullOrEmpty( score.プレビュー音声ファイル名 ) )
                {
                    sw.WriteLine( $"#PREVIEW: {score.プレビュー音声ファイル名}" );
                }
                else
                {
                    // 省略可。
                }
                //----------------
                #endregion

                #region " プレビュー画像 → #PREIMAGE "
                //----------------
                if( !string.IsNullOrEmpty( score.プレビュー画像ファイル名 ) )
                {
                    sw.WriteLine( $"#PREIMAGE: {score.プレビュー画像ファイル名}" );
                }
                else
                {
                    // 省略可。
                }
                //----------------
                #endregion

                #region " BGV → #VIDEO01 "
                //----------------
                if( !string.IsNullOrEmpty( score.BGVファイル名 ) )
                {
                    sw.WriteLine( $"#VIDEO01: {score.BGVファイル名}" );
                }
                else
                {
                    // 省略可。
                }
                //----------------
                #endregion

                #region " BGM → #WAVC0 "
                //----------------
                if( !string.IsNullOrEmpty( score.BGMファイル名 ) )
                {
                    sw.WriteLine( $"#WAVC0: {score.BGMファイル名}" );
                    sw.WriteLine( $"#BGMWAV: C0" );
                }
                else
                {
                    // 省略可。
                }
                //----------------
                #endregion

                sw.WriteLine( "" );
            }

            private static void _WAVとVOLUMEを出力する( スコア score, StreamWriter sw )
            {
                if( チップ.最大音量 != 8 )
                    throw new Exception( "チップの最大音量が 8 ではありません。" );

                foreach( var kvp in _チップ種別マップ )
                {
                    for( int vol = 0; vol < チップ.最大音量; vol++ )
                    {
                        var zz = _zz( kvp.Value.先頭wav番号 + vol );
                        sw.WriteLine( $"#WAV{zz}: {kvp.Value.ファイル名}" );
                        sw.WriteLine( $"#VOLUME{zz}: {(int) ( ( vol + 1 ) * 100.0 / チップ.最大音量 )}" );
                    }
                }

                sw.WriteLine( "" );
            }

            private static void _BPMを出力する( スコア score, StreamWriter sw )
            {
                int zz = 1;

                _BPMリスト = new List<double>();

                foreach( var chip in score.チップリスト )
                {
                    if( chip.チップ種別 == チップ種別.BPM && !_BPMリスト.Contains( chip.BPM ) )
                    {
                        sw.WriteLine( $"#BPM{_zz( zz )}: {chip.BPM}" );
                        zz++;
                        _BPMリスト.Add( chip.BPM );
                    }
                }

                sw.WriteLine( "" );
            }

            private static void _小節長倍率を出力する( スコア score, StreamWriter sw )
            {
                int 最終小節番号 = score.最大小節番号を返す();
                double 直前の小節の倍率 = 1.0;

                for( int 小節番号 = 0; 小節番号 <= 最終小節番号; 小節番号++ )
                {
                    // SSTF では、すべての小節番号に対して小節倍率が登録されている。
                    double 倍率 = score.小節長倍率リスト[ 小節番号 ];

                    // DTX では一度指定された倍率はそれ以降も有効だが、SSTF ではその小節限りとなる。
                    if( 倍率 != 直前の小節の倍率 )
                    {
                        sw.WriteLine( $"#{_zxx( 小節番号 )}02: {倍率}" );
                    }

                    直前の小節の倍率 = 倍率;
                }

                sw.WriteLine( "" );
            }

            private static void _オブジェクト記述を出力する( スコア score, StreamWriter sw )
            {
                int 最終小節番号 = score.最大小節番号を返す();

                // 小節単位で出力。
                for( int 小節番号 = 0; 小節番号 <= 最終小節番号; 小節番号++ )
                {
                    var この小節に存在するチップのリスト = score.チップリスト.Where( ( c ) => c.小節番号 == 小節番号 );

                    // チップの複製をとりつつ、チップ種別ごとに分類する。
                    // 複製をとるのは、このあとメンバをいじるため。

                    var 種別ごとのチップリスト = new Dictionary<チップ種別, List<チップ>>();
                    foreach( var chip in この小節に存在するチップのリスト )
                    {
                        if( 種別ごとのチップリスト.ContainsKey( chip.チップ種別 ) )
                            種別ごとのチップリスト[ chip.チップ種別 ].Add( (チップ) chip.Clone() );
                        else
                            種別ごとのチップリスト.Add( chip.チップ種別, new List<チップ>() { (チップ) chip.Clone() } );
                    }

                    // チップ種別ごとに処理し、出力する。

                    foreach( var chipType in 種別ごとのチップリスト.Keys )
                    {
                        if( chipType != チップ種別.背景動画 &&
                            chipType != チップ種別.BGM &&
                            chipType != チップ種別.BPM &&
                            !_チップ種別マップ.ContainsKey( chipType ) )
                            continue;   // オブジェクト記述の出力対象外

                        var chips = 種別ごとのチップリスト[ chipType ];

                        #region " 各チップの小節解像度を統一し、新しい小節内位置を算出する。"
                        //----------------
                        {
                            // 全チップの小節解像度の最小公倍数を計算する。
                            int 小節解像度の最小公倍数 = chips[ 0 ].小節解像度;
                            for( int i = 1; i < chips.Count; i++ )
                                小節解像度の最小公倍数 = _最小公倍数を返す( 小節解像度の最小公倍数, chips[ i ].小節解像度 );

                            // 小節解像度を最小公倍数で統一する。
                            for( int i = 0; i < chips.Count; i++ )
                            {
                                chips[ i ].小節内位置 *= 小節解像度の最小公倍数 / chips[ i ].小節解像度; // 必ず割り切れる
                                chips[ i ].小節解像度 = 小節解像度の最小公倍数;
                            }

                            // オブジェクト記述を短くするべく、ある程度までの素数で約分する。

                            for( int i = 0; i < _素数リスト.Length; i++ )
                            {
                                while( true )
                                {
                                    bool すべて割り切れる = true;

                                    foreach( var chip in chips )
                                    {
                                        if( 0 != ( chip.小節内位置 % _素数リスト[ i ] ) ||
                                            0 != ( chip.小節解像度 % _素数リスト[ i ] ) )
                                        {
                                            すべて割り切れる = false;
                                            break;
                                        }
                                    }

                                    if( !すべて割り切れる )
                                        break;

                                    foreach( var chip in chips )
                                    {
                                        chip.小節内位置 /= _素数リスト[ i ];
                                        chip.小節解像度 /= _素数リスト[ i ];
                                    }
                                }
                            }
                        }
                        //----------------
                        #endregion


                        // オブジェクト記述を作成する。

                        string DTXチャンネル番号 = "00";
                        var オブジェクト記述 = new int[ chips[ 0 ].小節解像度 ];

                        for( int i = 0; i < オブジェクト記述.Length; i++ )
                            オブジェクト記述[ i ] = 0;

                        foreach( var chip in chips )
                        {
                            switch( chipType )
                            {
                                case チップ種別.背景動画:
                                    オブジェクト記述[ chip.小節内位置 ] = 1;  // 背景動画は #VIDEO01 固定
                                    DTXチャンネル番号 = "5A";
                                    break;

                                case チップ種別.BGM:
                                    オブジェクト記述[ chip.小節内位置 ] = 12 * 36 + 0;  // BGM は #WAVC0 固定
                                    DTXチャンネル番号 = "01";
                                    break;

                                case チップ種別.BPM:
                                    オブジェクト記述[ chip.小節内位置 ] = _BPMリスト.IndexOf( chip.BPM ) + 1;   // #BPMzz: に対応する zz
                                    DTXチャンネル番号 = "08";
                                    break;

                                default:
                                    オブジェクト記述[ chip.小節内位置 ] = _チップ種別マップ[ chipType ].先頭wav番号 + ( chip.音量 - 1 );
                                    DTXチャンネル番号 = _チップ種別マップ[ chipType ].DTXチャンネル番号.ToString( "X2" );
                                    break;
                            }
                        }

                        sw.Write( $"#{_zxx( 小節番号 )}{DTXチャンネル番号}: " );
                        for( int i = 0; i < オブジェクト記述.Length; i++ )
                            sw.Write( $"{_zz( オブジェクト記述[ i ] )}" );
                        sw.WriteLine();
                    }

                }

                sw.WriteLine( "" );
            }



            // ローカル


            private static List<double> _BPMリスト = new List<double>();

            private static readonly Dictionary<チップ種別, (string ファイル名, int 先頭wav番号, int DTXチャンネル番号)> _チップ種別マップ;

            private static string _xx( int 値 )
            {
                if( 0 > 値 || 16 * 16 <= 値 )
                    throw new ArgumentOutOfRangeException( $"値が 0～{16 * 16 - 1} の範囲を越えています。" );

                return 値.ToString( "X2" );
            }

            private static string _zxx( int 値 )
            {
                if( 0 > 値 || 36 * 100 <= 値 )
                    throw new ArgumentOutOfRangeException( $"値が 0～{36 * 100 - 1} の範囲を越えています。" );

                int d3 = 値 / 100;
                int d21 = 値 % 100;
                return ( _36進数変換表[ d3 ] + d21.ToString( "D2" ) );
            }

            private static string _zz( int 値 )
            {
                if( 0 > 値 || 36 * 36 <= 値 )
                    throw new ArgumentOutOfRangeException( $"値が 0～{36 * 36 - 1} の範囲を越えています。" );

                char ch2 = _36進数変換表[ 値 / 36 ];
                char ch1 = _36進数変換表[ 値 % 36 ];
                return ( ch2.ToString() + ch1.ToString() );
            }

            private static int _zz( string zz )
            {
                if( zz.Length < 2 )
                    return -1;

                int d2 = _36進数変換表.IndexOf( zz[ 0 ] );
                if( d2 < 0 )
                    return -1;

                if( d2 >= 36 )
                    d2 -= ( 36 - 10 );      // 小文字の場合

                int d1 = _36進数変換表.IndexOf( zz[ 1 ] );
                if( d1 < 0 )
                    return -1;

                if( d1 >= 36 )
                    d1 -= ( 36 - 10 );      // 小文字の場合

                return d2 * 36 + d1;
            }

            private static readonly string _36進数変換表;


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

            private static readonly int[] _素数リスト;



            // 静的コンストラクタ


            static SSTFoverDTX()
            {
                // 生成順序に依存関係がある（36進数変換表はチップ種別マップより先に初期化されている必要がある）ので、
                // メンバ初期化子ではなく、静的コンストラクタで順序づけて生成する。

                _36進数変換表 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

                _チップ種別マップ = new Dictionary<チップ種別, (string ファイル名, int 先頭wav番号, int DTXチャンネル番号)>() {
                    { チップ種別.LeftCrash,          ( @"DrumSounds\LeftCrash.wav",          _zz("10"), 0x1A ) },
                    { チップ種別.LeftCymbal_Mute,    ( @"DrumSounds\LeftCymbalMute.wav",     _zz("18"), 0x61 ) },
                    { チップ種別.HiHat_Close,        ( @"DrumSounds\HiHatClose.wav",         _zz("20"), 0x11 ) },
                    { チップ種別.HiHat_Open,         ( @"DrumSounds\HiHatOpen.wav",          _zz("28"), 0x18 ) },
                    { チップ種別.HiHat_HalfOpen,     ( @"DrumSounds\HiHatHalfOpen.wav",      _zz("2G"), 0x18 ) },
                    { チップ種別.HiHat_Foot,         ( @"DrumSounds\HiHatFoot.wav",          _zz("2O"), 0x1B ) },
                    { チップ種別.Snare,              ( @"DrumSounds\Snare.wav",              _zz("30"), 0x12 ) },
                    { チップ種別.Snare_OpenRim,      ( @"DrumSounds\SnareOpenRim.wav",       _zz("38"), 0x12 ) },
                    { チップ種別.Snare_ClosedRim,    ( @"DrumSounds\SnareClosedRim.wav",     _zz("3G"), 0x12 ) },
                    { チップ種別.Snare_Ghost,        ( @"DrumSounds\SnareGhost.wav",         _zz("3O"), 0x12 ) },
                    { チップ種別.Bass,               ( @"DrumSounds\Bass.wav",               _zz("40"), 0x13 ) },
                    { チップ種別.LeftBass,           ( @"DrumSounds\Bass.wav",               _zz("48"), 0x1C ) },
                    { チップ種別.Tom1,               ( @"DrumSounds\Tom1.wav",               _zz("50"), 0x14 ) },
                    { チップ種別.Tom1_Rim,           ( @"DrumSounds\Tom1Rim.wav",            _zz("58"), 0x14 ) },
                    { チップ種別.Tom2,               ( @"DrumSounds\Tom2.wav",               _zz("60"), 0x15 ) },
                    { チップ種別.Tom2_Rim,           ( @"DrumSounds\Tom2Rim.wav",            _zz("68"), 0x15 ) },
                    { チップ種別.Tom3,               ( @"DrumSounds\Tom3.wav",               _zz("70"), 0x17 ) },
                    { チップ種別.Tom3_Rim,           ( @"DrumSounds\Tom3Rim.wav",            _zz("78"), 0x17 ) },
                    { チップ種別.RightCrash,         ( @"DrumSounds\RightCrash.wav",         _zz("80"), 0x16 ) },
                    { チップ種別.RightCymbal_Mute,   ( @"DrumSounds\RightCymbalMute.wav",    _zz("88"), 0x62 ) },
                    { チップ種別.Ride,               ( @"DrumSounds\Ride.wav",               _zz("90"), 0x19 ) },
                    { チップ種別.Ride_Cup,           ( @"DrumSounds\RideCup.wav",            _zz("98"), 0x19 ) },
                    { チップ種別.China,              ( @"DrumSounds\China.wav",              _zz("A0"), 0x16 ) },
                    { チップ種別.Splash,             ( @"DrumSounds\Splash.wav",             _zz("B0"), 0x1A ) },
                };

                _素数リスト = new int[] {
                    2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71,
                    73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173,
                    179, 181, 191, 193, 197, 199, 211, 223, 227, 229, 233, 239, 241, 251, 257, 263, 269, 271, 277, 281,
                };
            }
        }
    }
}
