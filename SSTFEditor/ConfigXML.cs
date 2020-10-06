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
        /// <summary>
        ///     譜面コントロールのオートフォーカス。
        ///     これが true の場合、譜面エリアにマウスポインタが入ったら、譜面コントロールが自動的にフォーカスされる。
        /// </summary>
        public bool AutoFocus = true;

        /// <summary>
        ///     最近使用したファイルの一覧をファイルメニューに表示する場合は true。
        /// </summary>
        public bool ShowRecentUsedFiles = true;

        /// <summary>
        ///     最近使用したファイルの一覧に表示するファイル数の上限。
        /// </summary>
        public int MaxOfUsedRecentFiles = 10;

        /// <summary>
        ///     最近使用したファイルの絶対パスの一覧。
        /// </summary>
        public List<string> RecentUsedFiles = new List<string>();

        /// <summary>
        ///     ビュアーへのパス。
        /// </summary>
        public string ViewerPath = @".\DTXMania2.exe";

        /// <summary>
        ///     起動時のウィンドウ表示位置。
        /// </summary>
        public Point WindowLocation = new Point( 100, 100 );

        /// <summary>
        ///     起動時のウィンドウのクライアントサイズ。
        /// </summary>
        public Size ClientSize = new Size( 710, 512 );

        /// <summary>
        ///     起動時の譜面拡大率係数。1～13。
        ///     譜面の拡大率(x1～x4) = 1.0 + (ViewScale - 1) * 0.25 で算出。
        /// </summary>
        public int ViewScale = 1;

        /// <summary>
        ///     .sstf 以外のファイル（SSTFoverDTXを除く）を開く際に、SSTF形式に変換するかどうかを
        ///     確認するダイアログを表示するなら true。
        ///     false の場合は無条件で変換される。
        /// </summary>
        public bool DisplaysConfirmOfSSTFConversion = true;

        /// <summary>
        ///     ライドレーンの表示位置が左ならtrue, 右ならfalse。
        /// </summary>
        public bool RideLeft = false;

        /// <summary>
        ///     チャイナレーンの表示位置が左ならtrue, 右ならfalse。
        /// </summary>
        public bool ChinaLeft = false;

        /// <summary>
        ///     スプラッシュレーンの表示位置が左ならtrue, 右ならfalse。
        /// </summary>
        public bool SplashLeft = true;



        // メソッド


        public static Config 読み込む( string ファイル名 )
        {
            Config config;

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
            var ファイルパス = Path.GetFullPath( ファイル名 );

            // 一覧に同じ文字列があったら一覧から削除する。
            this.RecentUsedFiles.RemoveAll( ( path ) => { return path.Equals( ファイルパス ); } );

            // 一覧の先頭に登録する。
            this.RecentUsedFiles.Insert( 0, ファイルパス );

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
