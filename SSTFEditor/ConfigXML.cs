using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Xml.Serialization;
using System.Windows.Forms;

namespace SSTFEditor
{
    public class Config
    {
        public bool AutoFocus = true;

        public bool ShowRecentUsedFiles = true;

        public int MaxOfUsedRecentFiles = 10;

        public List<string> RecentUsedFiles = new List<string>();

        public string ViewerPath = @".\DTXMania2.exe";

        public Point WindowLocation = new Point( 100, 100 );

        public Size ClientSize = new Size( 710, 512 );

        public int ViewScale = 1;

        public bool DisplaysConfirmOfSSTFConversion = true;


        public static Config 読み込む( string ファイル名 )
        {
            Config config = null;

            try
            {
                config = FDK.Serializer.ファイルをデシリアライズしてインスタンスを生成する<Config>( ファイル名 );
            }
            catch( Exception )
            {
                config = new Config();  // 読み込めなかったら新規作成する。
            }

            return config;
        }

        public void 保存する( string ファイル名 )
        {
            try
            {
                FDK.Serializer.インスタンスをシリアライズしてファイルに保存する( ファイル名, this );
            }
            catch( Exception e )
            {
                MessageBox.Show( $"ファイルの保存に失敗しました。[{ファイル名}]\n--------\n{e.ToString()}" );
            }
        }

        public void ファイルを最近使ったファイルの一覧に追加する( string ファイル名 )
        {
            // 絶対パスを取得する。
            ファイル名 = Path.GetFullPath( ファイル名 );

            // 一覧に同じ文字列があったら一覧から削除する。
            this.RecentUsedFiles.RemoveAll( ( path ) => { return path.Equals( ファイル名 ); } );

            // 一覧の先頭に登録する。
            this.RecentUsedFiles.Insert( 0, ファイル名 );

            // 一定以上は記録しない。
            if( this.RecentUsedFiles.Count > this.MaxOfUsedRecentFiles )
            {
                int 超えてる数 = this.RecentUsedFiles.Count - this.MaxOfUsedRecentFiles;

                for( int i = 超えてる数; i > 0; i-- )
                    this.RecentUsedFiles.RemoveAt( this.MaxOfUsedRecentFiles + i - 1 );
            }
        }
    }
}
