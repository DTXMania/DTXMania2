using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using YamlDotNet.Serialization;
using FDK;

namespace DTXMania2
{
    public class CommandLineOptions
    {
        [YamlIgnore]
        public bool ビュアーモードである => ( this.再生開始 || this.再生停止 );
        
        [YamlIgnore]
        public readonly string Usage =
            $"DTXMania2 Release {int.Parse( Application.ProductVersion.Split( '.' ).ElementAt( 0 ) ):000}\n" +
            "Usage: dtxmania2.exe [options] [filename]\n" +
            "-Vvvv,ppp,\"soundfilename\"    サウンドファイルの再生 vvv=volume, ppp=pan \n" +
            "-S                           DTXV再生停止 \n" +
            "-D(サウンドモード)(YかNが3文字続く) Viewerの設定 \n" +
            "                             (サウンドモード) WE=WASAPI Exclusive, WS=WASAPI Shared, A1=ASIO(数値はデバイス番号), D=DSound \n" +
            "                             YYY, YNYなど  1文字目=GRmode, 2文字目=TmeStretch, 3文字目=VSyncWait \n" +
            "-N[xxx]                      再生開始 xxx=再生開始小節番号 \n" +
            "-Etype,freq,bitrate,volBGM,volSE,volDrums,volGuitar,volBassmvolMaster,\"outfilename\",\"dtxfilename\" \n" +
            "                             DTX2WAVとして使用 type=\"WAV\"or\"MP3\"or\"OGG\", freq=48000など, bitrate=192 (kHzなど)" +
            "-B                           BGMのみ（ドラムサウンドを発声しない）" +
            "filename                     演奏するファイル。";

        // -V 系
        public bool サウンドファイルの再生 { get; set; } = false;
        public int Volume { get; set; }
        public int Pan { get; set; }
        public string サウンドファイル名 { get; set; } = null!;

        // -S 系
        public bool 再生停止 { get; set; } = false;

        // -D 系
        public bool ビュアーの設定 { get; set; } = false;
        public string サウンドモード { get; set; } = null!;
        public bool GRmode { get; set; }
        public bool TimeStretch { get; set; }
        public bool VSyncWait { get; set; }

        // -N 系
        public bool 再生開始 { get; set; } = false;
        public int 再生開始小節番号 { get; set; }

        // -B 系
        public bool ドラム音を発声する { get; set; } = true;

        // -E 系
        public bool DTX2WAV { get; set; } = false;
        public string OutputType { get; set; } = null!;
        public int Freq { get; set; }
        public int BitRate { get; set; }
        public int volBGM { get; set; }
        public int volSE { get; set; }
        public int volDrums { get; set; }
        public int volGuitar { get; set; }
        public int volBass { get; set; }
        public int volMaster { get; set; }
        public string OutFilename { get; set; } = null!;
        public string DTXFilename { get; set; } = null!;

        // ファイル名
        public string Filename { get; set; } = null!;



        // 生成と終了


        public CommandLineOptions()
        {
        }

        /// <summary>
        ///     インスタンスをYAMLに変換する。
        /// </summary>
        public string ToYaml()
        {
            var serializer = new SerializerBuilder()
                .WithTypeInspector( inner => new CommentGatheringTypeInspector( inner ) )
                .WithEmissionPhaseObjectGraphVisitor( args => new CommentsObjectGraphVisitor( args.InnerVisitor ) )
                .Build();

            return serializer.Serialize( this );
        }

        /// <summary>
        ///     YAMLからインスタンスを生成する。
        /// </summary>
        public static CommandLineOptions FromYaml( string yamlText )
        {
            var deserializer = new Deserializer();
            return deserializer.Deserialize<CommandLineOptions>( yamlText );
        }



        // 解析


        /// <summary>
        ///     指定されたコマンドライン引数を解析する。
        /// </summary>
        /// <param name="args">main()に渡されるものと同じ。</param>
        /// <returns>解析に成功すれば true、失敗すれば false。</returns>
        public bool 解析する( string[] args )
        {
            for( int i = 0; i < args.Length; i++ )
            {
                if( 1 <= args[ i ].Length && args[ i ][ 0 ] == '-' )
                {
                    if( args[ i ].StartsWith( "-V", StringComparison.OrdinalIgnoreCase ) )
                    {
                        // todo: サウンドファイルの再生
                    }
                    else if( args[ i ].StartsWith( "-S", StringComparison.OrdinalIgnoreCase ) )
                    {
                        #region " 再生停止 "
                        //----------------
                        this.再生停止 = true;
                        //----------------
                        #endregion
                    }
                    else if( args[ i ].StartsWith( "-D", StringComparison.OrdinalIgnoreCase ) )
                    {
                        // todo: ビュアーの設定
                    }
                    else if( args[ i ].StartsWith( "-N", StringComparison.OrdinalIgnoreCase ) )
                    {
                        #region " 再生開始 "
                        //----------------
                        int part = -1;  // 省略値は -1

                        if( 2 < args[ i ].Length )
                        {
                            if( !int.TryParse( args[ i ].Substring( 2 ), out part ) )   // 負数も OK
                                return false;
                        }

                        this.再生開始 = true;
                        this.再生開始小節番号 = part;
                        //----------------
                        #endregion
                    }
                    else if( args[ i ].StartsWith( "-E", StringComparison.OrdinalIgnoreCase ) )
                    {
                        // todo: DTX2WAV 実行
                    }
                    else if( args[ i ].StartsWith( "-C", StringComparison.OrdinalIgnoreCase ) )
                    {
                        // todo: DTX2WAV キャンセル？
                    }
                    else if( args[ i ].StartsWith( "-B", StringComparison.OrdinalIgnoreCase ) )
                    {
                        #region " BGMのみ（ドラムサウンドを発声しない）"
                        //----------------
                        this.ドラム音を発声する = false;
                        //----------------
                        #endregion
                    }
                    else
                    {
                        // 未知のオプション
                        return false;
                    }
                }
                else
                {
                    if( 1 < args[ i ].Length )
                    {
                        // オプション以外はすべてファイル名とみなす
                        this.Filename = args[ i ];
                    }
                }
            }


            // ファイル名の指定があるのに -N も -S もない場合には、-N-1 が指定されたものとみなす。
            if( null != this.Filename && ( !this.再生開始 && !this.再生停止 ) )
            {
                this.再生開始 = true;
                this.再生開始小節番号 = -1;
            }

            return true;
        }
    }
}
