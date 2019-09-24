using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Reflection;

namespace MakeArchive
{
    /// <summary>
    ///     DTXMania2 の配布用 zip ファイルを作成します。
    ///     ・このプロジェクトは、zip アーカイブの対象となるプロジェクトへの依存関係を付与して、一番最後にビルドされるようにします。
    ///     ・配布用のファイルを作成するには、Release 版を実行します。
    /// </summary>
    class Program
    {
        static void Main( string[] args )
        {
            var exePath = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );
#if DEBUG
            Console.WriteLine( "！Debug 版を実行しています！\n継続する場合は Enter を、中断する場合は Ctrl-C を押下してください。" );
            Console.In.ReadLine();
#endif

            var ZIPファイル名 = $@"dtxmania2_{DTXMania.Version.Major.ToString( "000" )}.zip";    // 必要あれば変更する
            var solutionDir = Path.Combine( exePath, @"..\..\.." );                              // 必要あれば変更する
            var dtxmaniaBinDir = Path.Combine( solutionDir, @"DTXMania\bin\Release" );           // 必要あれば変更する
            var editorBinDir = Path.Combine( solutionDir, @"SSTFEditor\bin\Release" );           // 必要あれば変更する
            var 出力フォルダパス = Path.Combine( $@"{solutionDir}", "アーカイブ" );              // 必要あれば変更する

            Console.WriteLine( "開始します。" );

            if( !Directory.Exists( 出力フォルダパス ) )
                Directory.CreateDirectory( 出力フォルダパス );

            var 出力ファイルパス = Path.Combine( 出力フォルダパス, ZIPファイル名 );
            if( File.Exists( 出力ファイルパス ) )
                File.Delete( 出力ファイルパス );

            using( var archive = ZipFile.Open( 出力ファイルパス, ZipArchiveMode.Create, Encoding.GetEncoding( "shift_jis" ) ) )
            {
                var 出力対象ファイルリスト = new List<(string src, string dst)>();

                bool 重複がない( (string src, string dst) item )
                    => !( 出力対象ファイルリスト.Any( ( existingPair ) => ( existingPair.dst == item.dst ) ) );    // 同じ dst は除外する

                // 必要あれば変更する
                出力対象ファイルリスト.AddRange( GetPair( dtxmaniaBinDir, @"System\*.*" ).Where( ( pair ) => 重複がない( pair ) ) );
                出力対象ファイルリスト.AddRange( GetPair( dtxmaniaBinDir, @"x64\*.*" ).Where( ( pair ) => 重複がない( pair ) ) );
                出力対象ファイルリスト.AddRange( GetPair( dtxmaniaBinDir, @"x86\*.*" ).Where( ( pair ) => 重複がない( pair ) ) );
                出力対象ファイルリスト.AddRange( GetPair( dtxmaniaBinDir, @"*.dll" ).Where( ( pair ) => 重複がない( pair ) ) );
                出力対象ファイルリスト.AddRange( GetPair( dtxmaniaBinDir, @"*.exe" ).Where( ( pair ) => 重複がない( pair ) ) );
                出力対象ファイルリスト.AddRange( GetPair( dtxmaniaBinDir, @"*.exe.config" ).Where( ( pair ) => 重複がない( pair ) ) );
                出力対象ファイルリスト.AddRange( GetPair( editorBinDir, @"ja-JP\*.*" ).Where( ( pair ) => 重複がない( pair ) ) );
                出力対象ファイルリスト.AddRange( GetPair( editorBinDir, @"*.exe" ).Where( ( pair ) => 重複がない( pair ) ) );
                出力対象ファイルリスト.AddRange( GetPair( editorBinDir, @"*.exe.config" ).Where( ( pair ) => 重複がない( pair ) ) );

                foreach( var filepair in 出力対象ファイルリスト )
                {
                    Console.WriteLine( $"{filepair.src} -> {filepair.dst}" );
                    archive.CreateEntryFromFile( filepair.src, filepair.dst, CompressionLevel.Optimal );
                }
            }

            Console.WriteLine( "\n完了しました。Enter を押下してください。" );
            Console.In.ReadLine();
        }

        private static IEnumerable<(string src, string dst)> GetPair( string baseDir, string pattern )
        {
            var files = Directory.GetFiles( baseDir, pattern, SearchOption.AllDirectories );

            foreach( var file in files )
            {
                var fullfile = Path.GetFullPath( file );    // ".." をなくしたパスにする
                var destfile = FDK.Folder.絶対パスを相対パスに変換する( baseDir, fullfile );

                yield return (fullfile, destfile);
            }
        }
    }
}
