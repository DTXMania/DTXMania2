using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using FDK;

namespace DTXMania2
{
    static class Program
    {
        public readonly static string _ビュアー用パイプライン名 = "DTXMania2Viewer";

        [STAThread]
        static void Main( string[] args )
        {
            try
            {
                // 初期化

                timeBeginPeriod( 1 );
                Encoding.RegisterProvider( CodePagesEncodingProvider.Instance );    // .NET Core で Shift-JIS 他を利用可能にする

                #region " コマンドライン引数を解析する。"
                //----------------
                Global.Options = new CommandLineOptions();

                if( !Global.Options.解析する( args ) ) // 解析に失敗すればfalse
                {
                    // 利用法を表示して終了。

                    Trace.WriteLine( Global.Options.Usage );             // Traceと
                    using( var console = new FDK.Console() )
                        console.Out?.WriteLine( Global.Options.Usage );  // 標準出力の両方へ
                    return;
                }
                //----------------
                #endregion

                #region " AppData に DTXMania2 フォルダがなければ作成する。"
                //----------------
                var AppDataフォルダ名 = Path.Combine( 
                    Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create ),
                    "DTXMania2" ); // %USERPROFILE%/AppData/local/DTXMania2/

                if( !( Directory.Exists( AppDataフォルダ名 ) ) )
                    Directory.CreateDirectory( AppDataフォルダ名 );
                //----------------
                #endregion

                #region " ログファイルへのログの複製出力開始。"
                //----------------
                {
                    const int ログファイルの最大保存日数 = 30;
                    Trace.AutoFlush = true;

                    var ログファイル名 = Log.ログファイル名を生成する(
                        ログフォルダパス: Path.Combine( AppDataフォルダ名, "Logs" ),
                        ログファイルの接頭辞: "Log.",
                        最大保存期間: TimeSpan.FromDays( ログファイルの最大保存日数 ) );

                    // ログファイルをTraceリスナとして追加。
                    // 以降、Trace（ならびにLogクラス）による出力は、このリスナ（＝ログファイル）にも出力される。
                    Trace.Listeners.Add( new TraceLogListener( new StreamWriter( ログファイル名, false, Encoding.GetEncoding( "utf-8" ) ) ) );

                    Log.現在のスレッドに名前をつける( "Form" );
                }
                //----------------
                #endregion

                #region " 二重起動チェックまたはオプション送信。"
                //----------------
                using( var pipeToViewer = new NamedPipeClientStream( ".", _ビュアー用パイプライン名, PipeDirection.Out ) )
                {
                    try
                    {
                        // パイプラインサーバへの接続を試みる。
                        pipeToViewer.Connect( 100 );

                        // (A) サービスが立ち上がっている場合 → ビュアーモードかどうかでさらに分岐

                        if( Global.Options.ビュアーモードである )
                        {
                            #region " (A-a) ビュアーモードである → オプション内容をパイプラインサーバへ送信して正常終了。"
                            //----------------
                            var ss = new StreamStringForNamedPipe( pipeToViewer );
                            var yamlText = Global.Options.ToYaml(); // YAML化
                            ss.WriteString( yamlText );
                            return;
                            //----------------
                            #endregion
                        }
                        else
                        {
                            #region " (A-b) 通常モードである → 二重起動としてエラー終了。"
                            //----------------
                            var ss = new StreamStringForNamedPipe( pipeToViewer );
                            ss.WriteString( "ping" );

                            var msg = "二重起動はできません。";
                            Trace.WriteLine( msg );                     // Traceと
                            MessageBox.Show( msg, "DTXMania2 error" );  // ダイアログに表示。
                            return;
                            //----------------
                            #endregion
                        }
                    }
                    catch( TimeoutException )
                    {
                        // (B) サービスが立ち上がっていない場合 → そのまま起動
                    }
                }
                //----------------
                #endregion

                #region " タイトル、著作権、システム情報をログ出力する。"
                //----------------
                Log.WriteLine( $"{Application.ProductName} Release {int.Parse( Application.ProductVersion.Split( '.' ).ElementAt( 0 ) ):000}" );

                var copyrights = (AssemblyCopyrightAttribute[]) Assembly.GetExecutingAssembly().GetCustomAttributes( typeof( AssemblyCopyrightAttribute ), false );
                Log.WriteLine( $"{copyrights[ 0 ].Copyright}" );
                Log.WriteLine( "" );

                #region " Windows 情報 "
                //----------------
                using( var hklmKey = Microsoft.Win32.Registry.LocalMachine )
                using( var subKey = hklmKey.OpenSubKey( @"SOFTWARE\Microsoft\Windows NT\CurrentVersion" ) ) // キーがなかったら null が返される
                {
                    if( null != subKey )
                    {
                        var os_product = subKey.GetValue( "ProductName" ).ToString() ?? "Unknown OS";
                        var os_release = subKey.GetValue( "ReleaseId" ).ToString() ?? "Unknown Release";
                        var os_build = subKey.GetValue( "CurrentBuild" ).ToString() ?? "Unknown Build";
                        var os_bit = Environment.Is64BitOperatingSystem ? "64bit" : "32bit";
                        var process_bit = Environment.Is64BitProcess ? "64bit" : "32bit";

                        Log.WriteLine( $"{os_product} {os_release}.{os_build} ({os_bit} OS, {process_bit} process)" );
                    }
                }
                //----------------
                #endregion

                #region " メモリ情報 "
                //----------------
                {
                    var output = "";
                    var info = new ProcessStartInfo();
                    info.FileName = "wmic";
                    info.Arguments = "OS get FreePhysicalMemory,TotalVisibleMemorySize /Value";
                    info.RedirectStandardOutput = true;
                    using( var process = Process.Start( info ) )
                        output = process.StandardOutput.ReadToEnd();
                    var lines = output.Trim().Split( "\n" );
                    var freeMemoryParts = lines[ 0 ].Split( "=", StringSplitOptions.RemoveEmptyEntries );
                    var totalMemoryParts = lines[ 1 ].Split( "=", StringSplitOptions.RemoveEmptyEntries );
                    var Total = Math.Round( double.Parse( totalMemoryParts[ 1 ] ) / 1024 / 1024, 0 );
                    var Free = Math.Round( double.Parse( freeMemoryParts[ 1 ] ) / 1024 / 1024, 0 );

                    Log.WriteLine( $"{Total}GB Total physical memory, {Free}GB Free" );
                }
                //----------------
                #endregion

                #region " .NET ランタイム情報 "
                //----------------
                Log.WriteLine( $".NET version {Environment.Version}" );
                //----------------
                #endregion

                Log.WriteLine( "" );
                //----------------
                #endregion

                #region " フォルダ変数を設定する。"
                //----------------
                {
                    var exePath = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) ?? "";

                    Folder.フォルダ変数を追加または更新する( "Exe", exePath );
                    Folder.フォルダ変数を追加または更新する( "ResourcesRoot", Path.Combine( exePath, "Resources" ) );
                    Folder.フォルダ変数を追加または更新する( "DrumSounds", Path.Combine( exePath, @"Resources\Default\DrumSounds" ) );
                    Folder.フォルダ変数を追加または更新する( "SystemSounds", Path.Combine( exePath, @"Resources\Default\SystemSounds" ) );
                    Folder.フォルダ変数を追加または更新する( "Images", Path.Combine( exePath, @"Resources\Default\Images" ) );
                    Folder.フォルダ変数を追加または更新する( "AppData", AppDataフォルダ名 );
                    Folder.フォルダ変数を追加または更新する( "UserProfile", Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ) );
                }
                //----------------
                #endregion

                #region " スレッドプールのオンラインスレッド数を変更する。"
                //----------------
                {
                    const int 希望オンラインスレッド数 = 32;

                    // 既定の数はCPUコア数。
                    // .NET の仕様により、Taskの同時利用数が最小値を超えると、それ以降の Task.Run での起動には最大2回/秒もの制限がかかる。
                    // https://docs.microsoft.com/ja-jp/dotnet/api/system.threading.threadpool.getminthreads
                    ThreadPool.GetMaxThreads( out int workMax, out int compMax );
                    ThreadPool.GetMinThreads( out int workMin, out int compMin );

                    ThreadPool.SetMinThreads(
                        Math.Clamp( 希望オンラインスレッド数, min: workMin, max: workMax ),     // workMin ～ workMax の範囲を越えない
                        Math.Clamp( 希望オンラインスレッド数, min: compMin, max: compMax ) );   // compMin ～ compMax の範囲を越えない
                }
                //----------------
                #endregion


                // メインループ

                Application.SetHighDpiMode( HighDpiMode.SystemAware );
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault( false );
                App appForm;
                do
                {
                    appForm = new App();

                    // アプリのメインループを実行する。
                    // アプリが終了するまでこのメソッドからは戻ってこない。
                    Application.Run( appForm );

                    appForm.Dispose();

                } while( appForm.再起動が必要 );  // 戻ってきた際、再起動フラグが立っていたらここでアプリを再起動する。

                #region " 備考: 再起動について "
                //----------------
                // .NET Core 3 で Application.Restart() すると、「起動したプロセスじゃないので却下」と言われる。
                // おそらく起動プロセスが dotnet であるため？
                // 　
                // ↓没コード
                // if( appForm.再起動が必要 )
                // {
                //     // 注意：Visual Sutdio のデバッグ＞例外設定で Common Language Runtime Exceptions にチェックを入れていると、
                //     // ここで InvalidDeploymentException が発生してデバッガが一時停止するが、これは「ファーストチャンス例外」なので、
                //     // 単に無視すること。
                //     Application.Restart();
                // }
                //----------------
                #endregion


                // 終了

                timeEndPeriod( 1 );

                Log.WriteLine( "" );
                Log.WriteLine( "遊んでくれてありがとう！" );
            }
#if !DEBUG
            // Release 時には、未処理の例外をキャッチしたらダイアログを表示する。
            catch( Exception )
            {
            }
#else
            // Debug 時には、未処理の例外が発出されても無視。（デバッガでキャッチすることを想定。）
            finally
            {
            }
#endif
        }

        #region " Win32 "
        //----------------
        [System.Runtime.InteropServices.DllImport( "winmm.dll" )]
        static extern void timeBeginPeriod( uint x );

        [System.Runtime.InteropServices.DllImport( "winmm.dll" )]
        static extern void timeEndPeriod( uint x );
        //----------------
        #endregion
    }
}
