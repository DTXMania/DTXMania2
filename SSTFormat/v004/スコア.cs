using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SSTFormat.v004
{
    public partial class スコア : ISSTFScore
    {

        // 定数


        /// <summary>
        ///     このソースが実装するSSTFバージョン。
        /// </summary>
        public static readonly Version SSTFVERSION = new Version( 4, 1, 0, 0 );
        
        public const double 初期BPM = 120.0;
        
        public const double 初期小節解像度 = 480.0;



        // ヘッダ


        /// <summary>
        ///		SSTFバージョン。
        ///		ファイルから読み込んだ場合、ファイルにSSTFVersionの記述がなかった場合は v1.0.0.0 とみなす。
        /// </summary>
        public Version SSTFバージョン { get; set; }

        /// <summary>
        ///     このスコアの曲名。
        /// </summary>
        public string 曲名 { get; set; }

        /// <summary>
        ///     このスコアのアーティスト名。
        ///     作曲者名、団体名、作品名、スコア作者名など、内容は任意。
        /// </summary>
        public string アーティスト名 { get; set; }

        /// <summary>
        ///     この曲の説明文。内容は任意。
        /// </summary>
        public string 説明文 { get; set; }

        /// <summary>
        ///	    この曲の難易度。
        ///	    易:0.00～9.99:難
        /// </summary>
        public double 難易度 { get; set; }

        /// <summary>
        ///     このスコアが作成されたときのサウンドデバイスの遅延量[ミリ秒]。
        /// </summary>
        public float サウンドデバイス遅延ms { get; set; } = 0f;



        // ヘッダ：プレビュー


        /// <summary>
        ///		プレビュー画像ファイルの、
        ///    <see cref="譜面ファイルのあるフォルダ"/> からの相対パス。
        /// </summary>
        public string プレビュー画像ファイル名 { get; set; }

        /// <summary>
        ///		プレビュー音声ファイルの、
        ///    <see cref="譜面ファイルのあるフォルダ"/> からの相対パス。
        /// </summary>
        public string プレビュー音声ファイル名 { get; set; }

        /// <summary>
        ///		プレビュー動画ファイルの、
        ///    <see cref="譜面ファイルのあるフォルダ"/> からの相対パス。
        /// </summary>
        public string プレビュー動画ファイル名 { get; set; }



        // ヘッダ：ファイル・フォルダ情報


        /// <summary>
        ///    この曲の BGV として再生する動画ファイルの、
        ///    <see cref="譜面ファイルのあるフォルダ"/> からの相対パス。
        /// </summary>
        /// <remarks>
        ///     このファイルをソースとして、<see cref="チップ種別.背景動画"/> のチップで動画が再生される。
        ///     SSTFから変換された場合には、このフィールドに格納されたファイルは、#AVI01（固定）として <see cref="AVIリスト"/> にも登録される。
        ///     DTX他から変換された場合には、このフィールドは未使用（nullまたは空文字列）→ 動画は<see cref="AVIリスト"/>、音声は<see cref="WAVリスト"/>を使用する。
        /// </remarks>
        public string BGVファイル名 { get; set; }

        /// <summary>
        ///    この曲の BGM として再生する動画ファイルの、
        ///    <see cref="譜面ファイルのあるフォルダ"/> からの相対パス。
        /// </summary>
        /// <remarks>
        ///     このファイルをソースとして、<see cref="チップ種別.BGM"/> のチップで音声が再生される。
        ///     SSTFから変換された場合には、このフィールドに格納されたファイルは、#WAV01（固定）として <see cref="WAVリスト"/> にも登録される。
        ///     DTX他から変換された場合には、このフィールドは未使用（nullまたは空文字列）→ 動画は<see cref="AVIリスト"/>、音声は<see cref="WAVリスト"/>を使用する。
        /// </remarks>
        public string BGMファイル名 { get; set; }

        /// <summary>
        ///     この曲の背景画像として描画する画像ファイルの、<see cref="譜面ファイルのあるフォルダ"/> からの相対パス。
        /// </summary>
        public string 背景画像ファイル名 { get; set; }

        /// <summary>
        ///		譜面ファイルの絶対パス。
        /// </summary>
        public string 譜面ファイルの絶対パス { get; set; } = null;

        /// <summary>
        ///     譜面ファイルのあるフォルダの絶対パス。
        /// </summary>
        /// <remarks>
        ///     WAV, AVI ファイルへのパスには、このフィールドではなく <see cref="PATH_WAV"/> を使うこと。
        /// </remarks>
        public string 譜面ファイルのあるフォルダ
            => ( string.IsNullOrEmpty( this.譜面ファイルの絶対パス ) ) ? "" : Path.GetDirectoryName( this.譜面ファイルの絶対パス );

        /// <summary>
        ///		WAV と AVI ファイルの基点となるフォルダの絶対パス。
        /// </summary>
        /// <remarks>
        ///     譜面で指定された PATH_WAV が空文字列または相対パスの場合、譜面のあるフォルダからの相対パスとしてPATH_WAVを適用した絶対パスを返す。
        ///     　例：譜面ファイルが "D:\DTXData\DemoSong\score.sstf" であり、譜面内で PATH_WAV が未定義の場合、このプロパティは"D:\DTXData\DemoSong" を返す。
        ///     　例：譜面ファイルが "D:\DTXData\DemoSong\score.sstf" であり、譜面内で PATH_WAV=Sounds と指定されている場合、このプロパティは"D:\DTXData\DemoSong\Sounds" を返す。
        ///     譜面で指定された PATH_WAV が絶対パスの場合、その PATH_WAV をそのまま返す。
        ///     　例：譜面ファイルが "D:\DTXData\DemoSong\score.sstf" であり、譜面内で PATH_WAV=E:\Sounds と指定されている場合、このプロパティは"E:\Sounds" を返す。
        /// </remarks>
        public string PATH_WAV
        {
            get
            {
                if( string.IsNullOrEmpty( this._PATH_WAV ) )
                {
                    // (A) PATH_WAV が未指定の場合、譜面のあるフォルダの絶対パスを返す。
                    return this.譜面ファイルのあるフォルダ;
                }
                else if( !Path.IsPathRooted( this._PATH_WAV ) )
                {
                    // (B) 譜面で指定された PATH_WAV が空文字列または相対パスの場合、譜面のあるフォルダからの相対パスとしてPATH_WAVを適用した絶対パスを返す。
                    return Path.Combine( this.譜面ファイルのあるフォルダ, this._PATH_WAV );
                }
                else
                {
                    // (C) 譜面で指定された PATH_WAV が絶対パスの場合、その PATH_WAV をそのまま返す。
                    return this._PATH_WAV;
                }
            }
        }



        // チップリスト


        /// <summary>
        ///     このスコアに存在するすべてのチップのリスト。
        /// </summary>
        public List<チップ> チップリスト { get; protected set; }



        // 小節長倍率リスト


        /// <summary>
        ///     小節ごとの倍率。
        ///		インデックス番号が小節番号を表し、小節 0 から最大小節まで、すべての小節の倍率がこのリストに含まれる。
        /// </summary>
        public List<double> 小節長倍率リスト { get; protected set; }

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



        // 小節メモリスト


        /// <summary>
        ///     メモリスト。
        ///     [key: 小節番号, value:メモ]
        /// </summary>
        public Dictionary<int, string> 小節メモリスト { get; protected set; }



        // WAVリスト


        /// <summary>
        ///		#WAVzz で指定された、サウンドファイルへの相対パス他。
        ///		パスの基点は、#PATH_WAV があればそこ、なければ曲譜面ファイルと同じ場所。
        ///		[key: zz番号]
        /// </summary>
        public Dictionary<int, WAV情報> WAVリスト { get; protected set; }
        
        public class WAV情報
        {
            public string ファイルパス { get; set; }
            public bool 多重再生する { get; set; }
            public bool BGMである { get; set; }
        }



        // AVIリスト


        /// <summary>
        ///		#AVIzz で指定された、動画ファイルへの相対パス。
        ///		パスの基点は、#PATH_WAV があればそこ、なければ曲譜面ファイルと同じ場所。
        ///		[key: zz番号]
        /// </summary>
        public Dictionary<int, string> AVIリスト { get; protected set; }



        // 空うちチップマップ


        /// <summary>
        ///     レーンごとの空うちチップ番号。
        ///		空打ちチップが指定されている場合はそのWAVzzのzz番号を、指定されていないなら 0 を保持する。
        ///		[value: zz番号]
        /// </summary>
        public Dictionary<レーン種別, int> 空打ちチップマップ { get; protected set; }



        // メソッド


        public スコア()
        {
            this.リセットする();
        }

        public static スコア ファイルから生成する( string スコアファイルの絶対パス, bool ヘッダだけ = false )
        {
            ISSTFScore score = null;

            var 拡張子 = Path.GetExtension( スコアファイルの絶対パス ).ToLower();

            switch( 拡張子 )
            {
                case ".sstf":
                    score = SSTFScoreFactory.CreateFromFile( スコアファイルの絶対パス, ヘッダだけ );
                    break;

                default:    // dtx, gda, 他
                    score = DTX.ファイルから生成する( スコアファイルの絶対パス, DTX.データ種別.拡張子から判定, ヘッダだけ );
                    break;
            }

            //if( !( ヘッダだけ ) )
            //    _後処理を行う( score ); --> 生成メソッドの中で行っておくこと。

            return (スコア) score;
        }

        public static スコア SSTFファイルから生成する( string スコアファイルの絶対パス, bool ヘッダだけ = false )
        {
            return (スコア) SSTF.ファイルから生成する( スコアファイルの絶対パス, ヘッダだけ );
        }

        public スコア( v003.スコア v3score )
        {
            this.曲名 = v3score.曲名;
            this.アーティスト名 = v3score.アーティスト名;
            this.説明文 = v3score.説明文;
            this.難易度 = v3score.難易度;
            this.プレビュー画像ファイル名 = v3score.プレビュー画像ファイル名;
            this.プレビュー音声ファイル名 = v3score.プレビュー音声ファイル名;
            this.プレビュー動画ファイル名 = v3score.プレビュー動画ファイル名;
            this.サウンドデバイス遅延ms = v3score.サウンドデバイス遅延ms;

            #region " 曲ファイルと同じ場所にあるプレビュー画像の検索（v3仕様）"
            //----------------
            if( string.IsNullOrEmpty( this.プレビュー画像ファイル名 ) && !string.IsNullOrEmpty( v3score.譜面ファイルパス ) )
            {
                var _対応するサムネイル画像名 = new[] { "thumb.png", "thumb.bmp", "thumb.jpg", "thumb.jpeg" };

                var 基点フォルダ = Path.GetDirectoryName( v3score.譜面ファイルパス );

                var path =
                    ( from ファイル名 in Directory.GetFiles( 基点フォルダ )
                      where _対応するサムネイル画像名.Any( thumbファイル名 => ( Path.GetFileName( ファイル名 ).ToLower() == thumbファイル名 ) )
                      select ファイル名 ).FirstOrDefault();

                if( default != path )
                {
                    this.プレビュー画像ファイル名 = ( Path.IsPathRooted( path ) ) ? _絶対パスを相対パスに変換する( 基点フォルダ, path ) : path;
                }
            }
            //----------------
            #endregion

            this.BGVファイル名 = v3score.背景動画ID;    // v3では、BGV と
            this.BGMファイル名 = v3score.背景動画ID;    // BGM は同じファイル
            this.譜面ファイルの絶対パス = v3score.譜面ファイルパス;
            this._PATH_WAV = v3score.PATH_WAV;

            this.チップリスト = new List<チップ>();
            foreach( var v3chip in v3score.チップリスト )
                this.チップリスト.Add( new チップ( v3chip ) );

            this.小節長倍率リスト = v3score.小節長倍率リスト;
            this.小節メモリスト = v3score.小節メモリスト;

            this.空打ちチップマップ = new Dictionary<レーン種別, int>();
            foreach( var v3kara in v3score.空打ちチップマップ )
                this.空打ちチップマップ.Add( (レーン種別) v3kara.Key, v3kara.Value );

            this.WAVリスト = new Dictionary<int, WAV情報>();
            foreach( var v3wav in v3score.WAVリスト )
            {
                this.WAVリスト.Add( v3wav.Key, new WAV情報 {
                    ファイルパス = v3wav.Value.ファイルパス,
                    多重再生する = v3wav.Value.多重再生する,
                    BGMである = false,
                } );
            }

            this.AVIリスト = v3score.AVIリスト;

            if( !string.IsNullOrEmpty( this.BGVファイル名 ) )
            {
                #region " 背景動画が有効の場合に必要な追加処理。"
                //----------------
                // avi01 に登録。
                this.AVIリスト[ 1 ] = this.BGVファイル名;

                // wav01 にも登録。
                if( !( this.WAVリスト.ContainsKey( 1 ) ) )
                    this.WAVリスト.Add( 1, new WAV情報() );
                this.WAVリスト[ 1 ].ファイルパス = this.BGVファイル名;
                this.WAVリスト[ 1 ].多重再生する = false;
                this.WAVリスト[ 1 ].BGMである = true;

                // 背景動画チップと同じ場所にBGMチップを追加する。
                var 作業前リスト = this.チップリスト;
                this.チップリスト = new List<チップ>(); // Clear() はNG
                foreach( var chip in 作業前リスト )
                {
                    // コピーしながら、
                    this.チップリスト.Add( chip );

                    // 必要あれば追加する。
                    if( chip.チップ種別 == チップ種別.背景動画 )
                    {
                        chip.チップサブID = 1;   // zz = 01 で固定。

                        var bgmChip = (チップ) chip.Clone();
                        bgmChip.チップ種別 = チップ種別.BGM;  // チップ種別以外は共通

                        this.チップリスト.Add( bgmChip );
                    }
                }
                //----------------
                #endregion
            }
        }

        public void リセットする()
        {
            this.SSTFバージョン = SSTFVERSION;
            this.曲名 = "(no title)";
            this.アーティスト名 = "";
            this.説明文 = "";
            this.難易度 = 5.0;
            this.サウンドデバイス遅延ms = 0f;

            this.プレビュー画像ファイル名 = null;
            this.プレビュー音声ファイル名 = null;
            this.プレビュー動画ファイル名 = null;

            this.BGVファイル名 = null;
            this.BGMファイル名 = null;
            this.背景画像ファイル名 = null;
            this.譜面ファイルの絶対パス = null;
            this._PATH_WAV = "";

            this.チップリスト = new List<チップ>();
            this.小節長倍率リスト = new List<double>();
            this.小節メモリスト = new Dictionary<int, string>();
            this.WAVリスト = new Dictionary<int, WAV情報>();
            this.AVIリスト = new Dictionary<int, string>();
            this.空打ちチップマップ = new Dictionary<レーン種別, int>();
            foreach( レーン種別 lane in Enum.GetValues( typeof( レーン種別 ) ) )
                this.空打ちチップマップ.Add( lane, 0 );
        }

        /// <summary>
        ///     この譜面における最後（最大）の小節番号。
        /// </summary>
        public int 最大小節番号を返す()
        {
            if( 0 < this.チップリスト.Count )
                return this.チップリスト.Max( ( chip ) => chip.小節番号 );

            return -1;
        }



        // ローカル


        internal static void _スコア読み込み時の後処理を行う( スコア score )
        {
            #region " 小節の先頭チップを追加する。"
            //----------------
            {
                int 最大小節番号 = score.最大小節番号を返す();

                // 「小節の先頭」チップは、小節線と同じく、全小節の先頭位置に置かれる。
                // 小節線には今後譜面作者によって位置をアレンジできる可能性を残したいが、
                // ビュアーが小節の先頭位置を検索するためには、小節の先頭に置かれるチップが必要になる。
                // よって、譜面作者の影響を受けない（ビュアー用の）チップを機械的に配置する。

                for( int i = 0; i <= 最大小節番号; i++ )
                {
                    score.チップリスト.Add(
                        new チップ() {
                            小節番号 = i,
                            チップ種別 = チップ種別.小節の先頭,
                            小節内位置 = 0,
                            小節解像度 = 1,
                        } );
                }
            }
            //----------------
            #endregion

            #region " チップリストを並び替える。"
            //----------------
            score.チップリスト.Sort();
            //----------------
            #endregion

            #region " 全チップの発声/描画時刻と譜面内位置を計算する。"
            //-----------------
            {
                // 1. BPMチップを無視し(初期BPMで固定)、小節長倍率, 小節解像度, 小節内位置 から 発声/描画時刻を計算する。
                //    以下、チップリストが小節番号順にソートされているという前提で。

                double チップが存在する小節の先頭時刻ms = 0.0;
                int 現在の小節の番号 = 0;

                foreach( チップ chip in score.チップリスト )
                {
                    // チップの小節番号が現在の小節の番号よりも大きい場合、チップが存在する小節に至るまで、チップが存在する小節の先頭時刻ms を更新する。

                    while( 現在の小節の番号 < chip.小節番号 )   // 現在の小節番号 が chip.小節番号 に追いつくまでループする。
                    {
                        double 現在の小節の小節長倍率 = score.小節長倍率を取得する( 現在の小節の番号 );

                        チップが存在する小節の先頭時刻ms += _BPM初期値固定での1小節4拍の時間ms * 現在の小節の小節長倍率;

                        現在の小節の番号++;
                    }

                    // チップの発声/描画時刻を求める。

                    double チップが存在する小節の小節長倍率 = score.小節長倍率を取得する( 現在の小節の番号 );

                    double 時刻sec = ( チップが存在する小節の先頭時刻ms + ( _BPM初期値固定での1小節4拍の時間ms * チップが存在する小節の小節長倍率 * chip.小節内位置 ) / (double) chip.小節解像度 ) / 1000.0;

                    chip.発声時刻sec = 時刻sec;
                    chip.描画時刻sec = 時刻sec;
                }

                // 2. 次に、BPMチップを考慮しながら調整する。

                double 現在のBPM = スコア.初期BPM;
                int チップ数 = score.チップリスト.Count;

                for( int i = 0; i < チップ数; i++ )
                {
                    var BPMチップ = score.チップリスト[ i ];

                    if( BPMチップ.チップ種別 != チップ種別.BPM )
                        continue;   // BPM チップ以外は無視。

                    // BPMチップより後続の全チップの 発声/描画時刻ms を、新旧BPMの比率（加速率）で修正する。

                    double 加速率 = BPMチップ.BPM / 現在のBPM; // BPMチップ.dbBPM > 0.0 であることは読み込み時に保証済み。

                    for( int j = i + 1; j < チップ数; j++ )
                    {
                        double 時刻sec = BPMチップ.発声時刻sec + ( score.チップリスト[ j ].発声時刻sec - BPMチップ.発声時刻sec ) / 加速率;

                        score.チップリスト[ j ].発声時刻sec = 時刻sec;
                        score.チップリスト[ j ].描画時刻sec = 時刻sec;
                    }

                    現在のBPM = BPMチップ.BPM;
                }
            }
            //-----------------
            #endregion
        }


        /// <summary>
        ///		WAV と AVI ファイルの基点となるフォルダの相対または絶対パス。
        ///		譜面の PATH_WAV= の内容がそのまま入る。
        /// </summary>
        /// <remarks>
        ///     例: "Sounds"
        /// </remarks>
        private string _PATH_WAV = "";

        private const double _BPM初期値固定での1小節4拍の時間ms = ( 60.0 * 1000 ) / ( スコア.初期BPM / 4.0 );

        private const double _BPM初期値固定での1小節4拍の時間sec = 60.0 / ( スコア.初期BPM / 4.0 );

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
