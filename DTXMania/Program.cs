using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FDK;

namespace DTXMania
{
    static class Program
    {
        const int ログファイルの最大保存日数 = 30;
        public static string ログファイル名 = "";

        [STAThread]
        static void Main( string[] args )
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault( false );

                #region " SharpDX のデバッグパラメータを設定する。"
                //----------------
#if DEBUG
                SharpDX.Configuration.EnableReleaseOnFinalizer = true;          // ファイナライザの実行中、未解放のCOMを見つけたら解放を試みる。
                SharpDX.Configuration.EnableTrackingReleaseOnFinalizer = true;  // その際には Trace にメッセージを出力する。
#endif
                //----------------
                #endregion

                #region " フォルダ変数を設定する。"
                //----------------
                var exePath = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );
                VariablePath.フォルダ変数を追加または更新する( "Exe", $@"{exePath}\" );
                VariablePath.フォルダ変数を追加または更新する( "System", Path.Combine( exePath, @"System\" ) );
                VariablePath.フォルダ変数を追加または更新する( "AppData", Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create ), @"DTXMania2\" ) );
                VariablePath.フォルダ変数を追加または更新する( "UserProfile", Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ) + @"\" );
                //----------------
                #endregion

                #region " %USERPROFILE%/AppData/DTXMania2 フォルダがなければ作成する。"
                //----------------
                var AppDataフォルダ名 = new VariablePath( "$(AppData)" );

                if( !( Directory.Exists( AppDataフォルダ名.変数なしパス ) ) )
                    Directory.CreateDirectory( AppDataフォルダ名.変数なしパス );
                //----------------
                #endregion

                #region " ログファイルへのログの複製出力開始。"
                //----------------
                Trace.AutoFlush = true;

                ログファイル名 = Log.ログファイル名を生成する( Path.Combine( AppDataフォルダ名.変数なしパス, "Logs" ), "Log.", TimeSpan.FromDays( ログファイルの最大保存日数 ) );

                // ログファイルをTraceリスナとして追加。
                // 以降、Trace（ならびにFDK.Logクラス）による出力は、このリスナ（＝ログファイル）にも出力される。
                Trace.Listeners.Add( new TraceLogListener( new StreamWriter( ログファイル名, false, Encoding.GetEncoding( "utf-8" ) ) ) );

                // 最初の出力。
                Log.現在のスレッドに名前をつける( "UI" );
                Log.WriteLine( Application.ProductName + " " + AppForm.リリース番号.ToString( "000" ) );  // アプリ名とバージョン
                Log.システム情報をログ出力する();
                Log.WriteLine( "" );
                //----------------
                #endregion

                #region " コマンドライン引数を解析する。"
                //----------------
                var options = new CommandLineOptions();

                if( !options.解析する( args ) ) // 解析に失敗すればfalse
                {
                    // 利用法を表示して終了。

                    Log.WriteLine( options.Usage );               // ログと

                    using( var console = new FDK.Console() )
                        console.Out?.WriteLine( options.Usage );  // 標準出力の両方へ
                    return;
                }
                //----------------
                #endregion

                #region " アプリ起動。"
                //----------------
                // Appインスタンスを生成、初期化。
                using( var app = new AppForm( options ) )
                {
                    if( app.WCFサービスをチェックする( options ) ) // true ならWCFサービスが存在していない。
                    {
                        // アプリを起動。
                        // アプリが終了するまで、このメソッドからは戻ってこない。

                        Application.Run( app );

                        // 戻ってきた際、再起動フラグが立っていたらここでアプリを再起動する。

                        if( app.再起動が必要 )
                        {
                            // 注意：Visual Sutdio のデバッグ＞例外設定で Common Language Runtime Exceptions にチェックを入れていると、
                            // ここで InvalidDeploymentException が発生してデバッガが一時停止するが、これは「ファーストチャンス例外」なので、
                            // 単に無視すること。
                            Application.Restart();
                        }
                    }
                    else
                    {
                        // false ならアプリを起動しない。
                    }
                }
                //----------------
                #endregion
            }

            // Release 時には、未処理の例外をキャッチしたらダイアログを表示する。
#if !DEBUG
            catch( Exception e )
            {
                using( var dlg = new 未処理例外検出ダイアログ() )
                {
                    Trace.WriteLine( "" );
                    Trace.WriteLine( "====> 未処理の例外が検出されました。" );
                    Trace.WriteLine( "" );
                    Trace.WriteLine( e.ToString() );

                    dlg.ShowDialog();
                }
            }
#else
            // DEBUG 時には、未処理の例外が発出されても無視。（デバッガでキャッチすることを想定。）
            finally
            {
            }
#endif

            Task.Delay( 1000 ).Wait();

            Log.WriteLine( "" );
            Log.WriteLine( "遊んでくれてありがとう！" );
        }
    }
}
