using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SSTFEditor
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
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
