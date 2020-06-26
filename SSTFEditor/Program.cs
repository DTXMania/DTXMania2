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
        public readonly static string _�r���A�[�p�p�C�v���C���� = "DTXMania2Viewer";

        [STAThread]
        static void Main()
        {
            Encoding.RegisterProvider( CodePagesEncodingProvider.Instance );    // .NET Core �� Shift-JIS ���𗘗p�\�ɂ���

            Application.SetHighDpiMode( HighDpiMode.SystemAware );
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );

            try
            {
                Application.Run( new ���C���t�H�[��() );
            }
#if !DEBUG
            catch( Exception e )
            {
                //using( var dlg = new ��������O���o�_�C�A���O() )
                {
                    Trace.WriteLine( "" );
                    Trace.WriteLine( "====> �������̗�O�����o����܂����B" );
                    Trace.WriteLine( "" );
                    Trace.WriteLine( e.ToString() );

                    //dlg.ShowDialog();
                }
            }
#else
            finally
            {
                // DEBUG ���ɂ́A�������̗�O�����o����Ă�catch���Ȃ��B�i�f�o�b�K�ŃL���b�`���邱�Ƃ�z��B�j
            }
#endif
        }
    }
}
