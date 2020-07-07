using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SSTFEditor
{
    static class Program
    {
        public readonly static string _ビュアー用パイプライン名 = "DTXMania2Viewer";

        [STAThread]
        static void Main()
        {
            Encoding.RegisterProvider( CodePagesEncodingProvider.Instance );    // .NET Core で Shift-JIS 他を利用可能にする

            Application.SetHighDpiMode( HighDpiMode.SystemAware );
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );

            try
            {
                Application.Run( new メインフォーム() );
            }
#if !DEBUG
            catch( Exception e )
            {
                //using( var dlg = new 未処理例外検出ダイアログ() )
                {
                    Trace.WriteLine( "" );
                    Trace.WriteLine( "====> 未処理の例外が検出されました。" );
                    Trace.WriteLine( "" );
                    Trace.WriteLine( e.ToString() );

                    //dlg.ShowDialog();
                }
            }
#else
            finally
            {
                // DEBUG 時には、未処理の例外が発出されてもcatchしない。（デバッガでキャッチすることを想定。）
            }
#endif
        }
    }
}
