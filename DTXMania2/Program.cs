using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
                    using( var console = new Console() )
                        console.Out?.WriteLine( Global.Options.Usage );  // 標準出力の両方へ
                    return;
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

                        // (A) サービスが立ち上がっている
                        if( Global.Options.ビュアーモードである )
                        {
                            #region " (A-a) オプション内容をサーバへ送信して正常終了。"
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
                            #region " (A-b) 二重起動としてエラー終了。"
                            //----------------
                            var ss = new StreamStringForNamedPipe( pipeToViewer );
                            ss.WriteString( "ping" );

                            var msg = "二重起動はできません。";
                            Trace.WriteLine( msg );                     // Traceと
                            MessageBox.Show( msg, "DTXMania2 error" );  // ダイアログ表示。
                            return;
                            //----------------
                            #endregion
                        }
                    }
                    catch( TimeoutException )
                    {
                        // (B) サービスが立ち上がっていない → そのまま起動
                    }
                }
                //----------------
                #endregion

                #region " AppData/DTXMania2 フォルダがなければ作成する。"
                //----------------
                //var AppDataフォルダ名 = Application.UserAppDataPath;  // %USERPROFILE%/AppData/<会社名>/DTXMania2/
                var AppDataフォルダ名 = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create ), "DTXMania2" ); // %USERPROFILE%/AppData/DTXMania2/

                if( !( Directory.Exists( AppDataフォルダ名 ) ) )
                    Directory.CreateDirectory( AppDataフォルダ名 );
                //----------------
                #endregion

                #region " ログファイルへのログの複製出力開始。"
                //----------------
                {
                    const int ログファイルの最大保存日数 = 30;
                    Trace.AutoFlush = true;

                    var ログファイル名 = Log.ログファイル名を生成する( Path.Combine( AppDataフォルダ名, "Logs" ), "Log.", TimeSpan.FromDays( ログファイルの最大保存日数 ) );

                    // ログファイルをTraceリスナとして追加。
                    // 以降、Trace（ならびにLogクラス）による出力は、このリスナ（＝ログファイル）にも出力される。
                    Trace.Listeners.Add( new TraceLogListener( new StreamWriter( ログファイル名, false, Encoding.GetEncoding( "utf-8" ) ) ) );

                    Log.現在のスレッドに名前をつける( "Form" );
                }
                //----------------
                #endregion

                #region " タイトル、著作権、システム情報をログ出力する。"
                //----------------
                Log.WriteLine( $"{Application.ProductName} Release {int.Parse( Application.ProductVersion.Split( '.' ).ElementAt( 0 ) ):000}" );

                var copyrights = (AssemblyCopyrightAttribute[]) Assembly.GetExecutingAssembly().GetCustomAttributes( typeof( AssemblyCopyrightAttribute ), false );
                Log.WriteLine( $"{copyrights[ 0 ].Copyright}" );
                Log.WriteLine( "" );

                Log.システム情報をログ出力する();
                Log.WriteLine( "" );
                //----------------
                #endregion

                #region " フォルダ変数を設定する。"
                //----------------
                {
                    var exePath = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) ?? "";

                    Folder.フォルダ変数を追加または更新する( "Exe", exePath );
                    Folder.フォルダ変数を追加または更新する( "ResourcesRoot", Path.Combine( exePath, "Resources" ) );
                    Folder.フォルダ変数を追加または更新する( "DrumSounds", Path.Combine( exePath, @"Resources\Default\DrumSounds" ) );      // Skin.yaml により変更される
                    Folder.フォルダ変数を追加または更新する( "SystemSounds", Path.Combine( exePath, @"Resources\Default\SystemSounds" ) );  // Skin.yaml により変更される
                    Folder.フォルダ変数を追加または更新する( "Images", Path.Combine( exePath, @"Resources\Default\Images" ) );              // Skin.yaml により変更される
                    Folder.フォルダ変数を追加または更新する( "AppData", AppDataフォルダ名 );
                    Folder.フォルダ変数を追加または更新する( "UserProfile", Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ) );
                }
                //----------------
                #endregion


                // アプリ起動

                Application.SetHighDpiMode( HighDpiMode.SystemAware );
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault( false );
                AppForm appForm;
                do
                {
                    appForm = new AppForm();
                    Application.Run( appForm );
                    appForm.Dispose();
                } while( appForm.再起動が必要 );  // 戻ってきた際、再起動フラグが立っていたらここでアプリを再起動する。

                #region " 備考: 再起動について "
                //----------------
                // .NET Core 3 で Application.Restart() すると、「起動したプロセスじゃないので却下」と言われる。
                // おそらく起動プロセスが dotnet であるため？
                // 　
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
            catch( Exception e )
            {
                MessageBox.Show(
                    $"未処理の例外が発生しました。\n\n" +
                    $"{e.Message}\n" +
                    $"{e.StackTrace}",
                    "Exception" );
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
