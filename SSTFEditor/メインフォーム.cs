using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using SSTFormat.v004;
using FDK;

namespace SSTFEditor
{
    partial class メインフォーム : Form
    {
        public int メジャーバージョン番号 => Assembly.GetExecutingAssembly().GetName().Version.Major;

        public int マイナーバージョン番号 => Assembly.GetExecutingAssembly().GetName().Version.Minor;

        public int リビジョン番号 => Assembly.GetExecutingAssembly().GetName().Version.Revision;

        public int ビルド番号 => Assembly.GetExecutingAssembly().GetName().Version.Build;

        public const int 最大音量 = 8;

        public const int 最小音量 = 1;

        public bool 未保存である
        {
            get
                => this._未保存である;

            set
            {
                // まず値を保存。
                this._未保存である = value;

                // ウィンドウのタイトルバーの文字列を修正する。
                string 表示するファイルの名前 = ( string.IsNullOrEmpty( this._編集中のファイル名 ) ) ? Properties.Resources.NEW_FILENAME : this._編集中のファイル名;
                if( this._未保存である )
                {
                    // 変更ありかつ未保存なら「*」を付ける。
                    this.Text = $"SSTFEditor {this.メジャーバージョン番号}.{this.マイナーバージョン番号}.{this.リビジョン番号}.{this.ビルド番号} *[{表示するファイルの名前}]";
                    this.toolStripMenuItem上書き保存.Enabled = true;
                    this.toolStripButton上書き保存.Enabled = true;
                }
                else
                {
                    // 保存後変更がないなら「*」は付けない。
                    this.Text = $"SSTFEditor {this.メジャーバージョン番号}.{this.マイナーバージョン番号}.{this.リビジョン番号}.{this.ビルド番号} [{表示するファイルの名前}]";
                    this.toolStripMenuItem上書き保存.Enabled = false;
                    this.toolStripButton上書き保存.Enabled = false;
                }
            }
        }

        public bool 選択モードである => ( CheckState.Checked == this.toolStripButton選択モード.CheckState );

        public bool 編集モードである => ( CheckState.Checked == this.toolStripButton編集モード.CheckState );

        /// <summary>
        ///		１小節あたりのグリッド数。
        ///		小節長倍率が 1.0 でない場合は、これを乗じることでグリッド数が変化する。
        /// </summary>
        public int GRID_PER_PART => int.Parse( Properties.Resources.GRID_PER_PART );

        /// <summary>
        ///		１ピクセルあたりのグリッド数。
        ///		現在の譜面拡大率によって変化する。
        /// </summary>
        public int GRID_PER_PIXEL => (int) ( int.Parse( Properties.Resources.GRID_PER_PIXEL ) / ( 1 + 0.25 * this.toolStripComboBox譜面拡大率.SelectedIndex ) );

        public Config Config { get; set; }

        public 選択モード 選択モード { get; set; }

        public 編集モード 編集モード { get; set; }

        public 譜面 譜面 { get; set; }

        public UndoRedo.UndoRedo管理 UndoRedo管理 { get; set; }

        public クリップボード クリップボード { get; set; }

        public Size 譜面パネルサイズ => this.pictureBox譜面パネル.ClientSize;

        public Rectangle 譜面パネル領域 => this.pictureBox譜面パネル.ClientRectangle;

        public チップ種別 現在のチップ種別
        {
            get
                => this._現在のチップ種別;

            set
            {
                this._現在のチップ種別 = value;
                this.label現在のチップ種別.Text = this._チップto名前[ value ];
            }
        }

        public int 現在のチップ音量 { get; set; } = メインフォーム.最大音量;

        public bool 初期化完了 { get; set; } = false;


        public メインフォーム()
        {
            InitializeComponent();

            this.Icon = Properties.Resources.Icon;

            this._アプリの起動処理を行う();
        }

        public void 選択モードに切替えて関連GUIを設定する()
        {
            this.toolStripButton選択モード.CheckState = CheckState.Checked;
            this.toolStripButton編集モード.CheckState = CheckState.Unchecked;

            this.toolStripMenuItem選択モード.CheckState = CheckState.Checked;
            this.toolStripMenuItem編集モード.CheckState = CheckState.Unchecked;

            this.label現在のチップ種別.Text = "----";

            this.譜面をリフレッシュする();
        }

        public void 編集モードに切替えて関連GUIを設定する()
        {
            this.選択モード.全チップの選択を解除する();
            this.譜面をリフレッシュする();

            this.toolStripButton選択モード.CheckState = CheckState.Unchecked;
            this.toolStripButton編集モード.CheckState = CheckState.Checked;

            this.toolStripMenuItem選択モード.CheckState = CheckState.Unchecked;
            this.toolStripMenuItem編集モード.CheckState = CheckState.Checked;
        }

        public void 選択チップの有無に応じて編集用GUIのEnabledを設定する()
        {
            bool 譜面上に選択チップがある = this._選択チップが１個以上ある;
            bool クリップボードに選択チップがある = ( null != this.クリップボード ) && ( 0 < this.クリップボード.アイテム数 );

            // 編集メニューの Enabled 設定
            this.toolStripMenuItemコピー.Enabled = 譜面上に選択チップがある;
            this.toolStripMenuItem切り取り.Enabled = 譜面上に選択チップがある;
            this.toolStripMenuItem貼り付け.Enabled = クリップボードに選択チップがある;
            this.toolStripMenuItem削除.Enabled = 譜面上に選択チップがある;

            // ツールバーの Enabled 設定
            this.toolStripButtonコピー.Enabled = 譜面上に選択チップがある;
            this.toolStripButton切り取り.Enabled = 譜面上に選択チップがある;
            this.toolStripButton貼り付け.Enabled = クリップボードに選択チップがある;
            this.toolStripButton削除.Enabled = 譜面上に選択チップがある;

            // 右メニューの Enabled 設定
            this.toolStripMenuItem選択チップのコピー.Enabled = 譜面上に選択チップがある;
            this.toolStripMenuItem選択チップの切り取り.Enabled = 譜面上に選択チップがある;
            this.toolStripMenuItem選択チップの貼り付け.Enabled = クリップボードに選択チップがある;
            this.toolStripMenuItem選択チップの削除.Enabled = 譜面上に選択チップがある;
        }

        public void 譜面をリフレッシュする()
        {
            this.pictureBox譜面パネル.Refresh();
        }

        public void UndoRedo用GUIのEnabledを設定する()
        {
            bool Undo可 = ( 0 < this.UndoRedo管理.Undo可能な回数 ) ? true : false;
            bool Redo可 = ( 0 < this.UndoRedo管理.Redo可能な回数 ) ? true : false;

            this.toolStripMenuItem元に戻す.Enabled = Undo可;
            this.toolStripMenuItemやり直す.Enabled = Redo可;
            this.toolStripButton元に戻す.Enabled = Undo可;
            this.toolStripButtonやり直す.Enabled = Redo可;
        }

        public void 選択モードのコンテクストメニューを表示する( int x, int y )
        {
            this.contextMenuStrip譜面右メニュー.Show( this.pictureBox譜面パネル, x, y );

            // メニューを表示した時のマウス座標を控えておく。
            this._選択モードでコンテクストメニューを開いたときのマウスの位置 = new Point( x, y );
        }

        public void 譜面を縦スクロールする( int スクロール量grid )
        {
            int 現在の位置grid = this.vScrollBar譜面用垂直スクロールバー.Value;
            int 最小値grid = this.vScrollBar譜面用垂直スクロールバー.Minimum;
            int 最大値grid = ( this.vScrollBar譜面用垂直スクロールバー.Maximum + 1 ) - this.vScrollBar譜面用垂直スクロールバー.LargeChange;
            int スクロール後の位置grid = 現在の位置grid + スクロール量grid;
            スクロール後の位置grid = Math.Clamp( スクロール後の位置grid, min: 最小値grid, max: 最大値grid );

            this.vScrollBar譜面用垂直スクロールバー.Value = スクロール後の位置grid;
        }


        private bool _未保存である = false;

        private readonly Dictionary<チップ種別, string> _チップto名前 = new Dictionary<チップ種別, string>() {
            #region " *** "
             //-----------------
            { チップ種別.Bass,               "BassDrum" },
            { チップ種別.BPM,                "BPM" },
            { チップ種別.China,              "China" },
            { チップ種別.HiHat_Close,        "HiHat(Close)" },
            { チップ種別.HiHat_Foot,         "FootPedal" },
            { チップ種別.HiHat_HalfOpen,     "HiHat(HalfOpen)" },
            { チップ種別.HiHat_Open,         "HiHat(Open)" },
            { チップ種別.LeftCrash,          "Crash" },
            { チップ種別.Ride,               "Ride" },
            { チップ種別.Ride_Cup,           "Ride(Cup)" },
            { チップ種別.RightCrash,         "Crash" },
            { チップ種別.Snare,              "Snare" },
            { チップ種別.Snare_ClosedRim,    "Snare(CloseRimShot)" },
            { チップ種別.Snare_Ghost,        "Snare(Ghost)" },
            { チップ種別.Snare_OpenRim,      "Snare(OpenRimShot)" },
            { チップ種別.Splash,             "Splash" },
            { チップ種別.Tom1,               "HighTom" },
            { チップ種別.Tom1_Rim,           "HighTom(RimShot)" },
            { チップ種別.Tom2,               "LowTom" },
            { チップ種別.Tom2_Rim,           "LowTom(RimShot)" },
            { チップ種別.Tom3,               "FloorTom" },
            { チップ種別.Tom3_Rim,           "FloorTom(RimShot)" },
            { チップ種別.LeftCymbal_Mute,    "Mute" },
            { チップ種別.RightCymbal_Mute,   "Mute" },
            { チップ種別.背景動画,           "" },
            { チップ種別.BGM,                "" },
            { チップ種別.Unknown,            "" },
            { チップ種別.小節線,             "" },
            { チップ種別.拍線,               "" },
            //-----------------
            #endregion
        };

        private Font _メモ用フォント = new Font( FontFamily.GenericSansSerif, 9.0f );

        private readonly Dictionary<int, string> _音量toラベル = new Dictionary<int, string>() {
            #region " *** "
            //----------------
            { 1, "-6 *-----+-" },
            { 2, "-5 **----+-" },
            { 3, "-4 ***---+-" },
            { 4, "-3 ****--+-" },
            { 5, "-2 *****-+-" },
            { 6, "-1 ******+-" },
            { 7, " 0 *******-" },
            { 8, "+1 ********" },
            //----------------
            #endregion
        };

        private bool _選択チップが１個以上ある
        {
            get
            {
                if( ( null != this.譜面.SSTFormatScore ) &&
                    ( null != this.譜面.SSTFormatScore.チップリスト ) &&
                    ( 0 < this.譜面.SSTFormatScore.チップリスト.Count ) )
                {
                    foreach( 描画用チップ chip in this.譜面.SSTFormatScore.チップリスト )
                    {
                        if( chip.選択が確定している )
                            return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        ///		単位: [n分の1] （例: 間隔=16 なら、"1/16" を意味する。）
        /// </summary>
        private int _現在のガイド間隔 = 0;

        private Point _選択モードでコンテクストメニューを開いたときのマウスの位置;

        private チップ種別 _現在のチップ種別 = チップ種別.Unknown;

        #region " フォルダ、ファイルパス "
        //----------------
        /// <summary>
        ///		SSTFEditor.exe が格納されているフォルダへのパス。
        /// </summary>
        private string _システムフォルダパス => ( Path.GetDirectoryName( Application.ExecutablePath ) );

        /// <summary>
        ///		Windowsログインユーザのアプリデータフォルダ。末尾は '\'。
        /// </summary>
        /// <remarks>
        ///		例: "C:\Users\ログインユーザ名\AppData\Roaming\SSTFEditor\"
        /// </remarks>
        private string _ユーザフォルダパス
        {
            get
            {
                var path = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create ), @"SSTFEditor\" );

                if( false == Directory.Exists( path ) )
                    Directory.CreateDirectory( path );  // なければ作成する。

                return path;
            }
        }

        private string _作業フォルダパス = null;

        private string _編集中のファイル名 = null;

        private string _最後にプレイヤーに渡した一時ファイル名 = null;
        //----------------
        #endregion

        #region " Viewer 用パイプライン "
        //----------------
        private NamedPipeClientStream _Viewerに接続する()
        {
            var pipeToViewer = new NamedPipeClientStream( ".", Program._ビュアー用パイプライン名, PipeDirection.Out );
            try
            {
                // パイプラインサーバへの接続を試みる。
                pipeToViewer.Connect( 100 );
            }
            catch( TimeoutException )
            {
                // サーバが立ち上がっていないなら null を返す。
                pipeToViewer?.Dispose();
                pipeToViewer = null;
            }

            return pipeToViewer;    // null じゃない場合は Dispose を忘れないこと。
        }
        //----------------
        #endregion


        // アクションメソッド

        #region " アクションメソッド（最上位の機能エントリ。メニューやコマンドバーなどから呼び出される。）"
        //----------------
        private void _アプリの起動処理を行う()
        {
            // 作業フォルダの初期値は、Windowsユーザのマイドキュメントフォルダ。

            this._作業フォルダパス = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );


            // Config.xml を読み込む。

            this.Config = Config.読み込む( Path.Combine( this._ユーザフォルダパス, Properties.Resources.CONFIG_FILE_NAME ) );
            if( this.Config.ViewerPath.Nullまたは空である() )
            {
                // 既定のプレイヤーは、exe と同じ場所にあるものとする。
                this.Config.ViewerPath = Path.Combine( this._システムフォルダパス, Properties.Resources.PLAYER_NAME );

                if( false == File.Exists( this.Config.ViewerPath ) )
                    this.Config.ViewerPath = "";    // ビュアーが存在してない。
            }


            // ウィンドウの位置とサイズ。

            this.StartPosition = FormStartPosition.Manual;
            this.Location = this.Config.WindowLocation;
            this.ClientSize = this.Config.ClientSize;
           

            // デザイナでは追加できないイベントを手動で追加する。

            this.splitContainer分割パネルコンテナ.MouseWheel += new MouseEventHandler( splitContainer分割パネルコンテナ_MouseWheel );


            // 最近使ったファイル一覧を更新する。

            this._ConfigのRecentUsedFilesをファイルメニューへ追加する();


            // その他の初期化。

            this._新規作成する();
            this._ガイド間隔を変更する( 16 ); // 初期は 1/16 間隔。


            // 完了。

            this.初期化完了 = true;


            // コマンドライン引数に、存在するファイルの指定があれば開く。

            foreach( var arg in Environment.GetCommandLineArgs().Skip( 1 ) )    // 先頭は exe 名なのでスキップ。
            {
                if( File.Exists( arg ) )
                {
                    this._指定されたファイルを開く( arg );
                    break;  // 最初の１つだけ。
                }
            }
        }
        private void _アプリの終了処理を行う()
        {
            // 一時ファイルが残っていれば、削除する。

            if( File.Exists( this._最後にプレイヤーに渡した一時ファイル名 ) )
                File.Delete( this._最後にプレイヤーに渡した一時ファイル名 );

            
            // Config.xml を保存する。

            this.Config.保存する( Path.Combine( this._ユーザフォルダパス, Properties.Resources.CONFIG_FILE_NAME ) );


            // その他の終了処理。

            this.譜面?.Dispose();
            this.譜面 = null;

            this.選択モード?.Dispose();
            this.選択モード = null;
        }

        private void _新規作成する()
        {
            if( DialogResult.Cancel == this._未保存なら保存する() )
                return; // 保存がキャンセルされた場合はここで中断。

            this._エディタを初期化する();
        }
        private void _開く()
        {
            if( DialogResult.Cancel == this._未保存なら保存する() )
                return; // 保存がキャンセルされた場合はここで中断。

            #region " ファイルを開くダイアログでファイルを選択する。"
            //-----------------
            var dialog = new OpenFileDialog() {
                Title = Properties.Resources.MSG_ファイル選択ダイアログのタイトル,
                Filter = Properties.Resources.MSG_曲ファイル選択ダイアログのフィルタ,
                FilterIndex = 1,
                InitialDirectory = this._作業フォルダパス,
            };
            var result = dialog.ShowDialog( this );

            // メインフォームを再描画してダイアログを完全に消す。
            this.Refresh();

            // OKじゃないならここで中断。
            if( DialogResult.OK != result )
                return;
            //-----------------
            #endregion

            this._ファイルを読み込む( dialog.FileName );
        }
        private void _指定されたファイルを開く( string ファイルパス )
        {
            if( DialogResult.Cancel == this._未保存なら保存する() )
                return; // 保存がキャンセルされた場合はここで中断。

            this._ファイルを読み込む( ファイルパス );
        }

        private void _上書き保存する()
        {
            #region " ファイル名が未設定なら、初めての保存と見なし、ファイル保存ダイアログで保存ファイル名を指定させる。"
            //-----------------
            if( this._編集中のファイル名.Nullまたは空である() )
            {
                string 絶対パスファイル名 = this._ファイル保存ダイアログを開いてファイル名を取得する();

                if( string.IsNullOrEmpty( 絶対パスファイル名 ) )
                    return; // ファイル保存ダイアログがキャンセルされたのならここで打ち切り。

                this._作業フォルダパス = Path.GetDirectoryName( 絶対パスファイル名 );    // ダイアログでディレクトリを変更した場合、カレントディレクトリも変更されている。
                this._編集中のファイル名 = Path.GetFileName( 絶対パスファイル名 );
            }
            //-----------------
            #endregion

            this._上書き保存する(
                Path.Combine( this._作業フォルダパス, this._編集中のファイル名 ),
                一時ファイルである: false );
        }
        private void _上書き保存する( string ファイルの絶対パス, bool 一時ファイルである )
        {
            #region " [保存中です] ポップアップを表示する。"
            //-----------------
            var msg = new Popupメッセージ(
                Properties.Resources.MSG_保存中です + Environment.NewLine +
                Properties.Resources.MSG_しばらくお待ち下さい );
            msg.Owner = this;
            msg.Show();
            msg.Refresh();
            //-----------------
            #endregion

            try
            {
                // 選択モードだったら、全チップの選択を解除する。
                if( this.選択モードである )
                    this.選択モード.全チップの選択を解除する();

                // ファイルを出力する。
                string ヘッダ行 = $"# Created by SSTFEditor {this.メジャーバージョン番号}.{this.マイナーバージョン番号}.{this.リビジョン番号}.{this.ビルド番号}";
                switch( Path.GetExtension( ファイルの絶対パス ).ToLower() )
                {
                    case ".dtx":
                        this.譜面.SSTFoverDTXファイルを書き出す( ファイルの絶対パス, ヘッダ行 );
                        break;

                    case ".sstf":
                        this.譜面.SSTFファイルを書き出す( ファイルの絶対パス, ヘッダ行 );
                        break;
                }

                // 出力したファイルのパスを、[ファイル]メニューの最近使ったファイル一覧に追加する。
                if( false == 一時ファイルである )
                {
                    this.Config.ファイルを最近使ったファイルの一覧に追加する( ファイルの絶対パス );
                    this._ConfigのRecentUsedFilesをファイルメニューへ追加する();
                }
            }
            finally
            {
                #region " [保存中です] ポップアップを閉じる。"
                //-----------------
                msg.Close();
                //-----------------
                #endregion

                // 最後に、ダイアログのゴミなどを消すために再描画。
                this.Refresh();

                // 一時ファイルじゃないなら、保存完了。
                if( false == 一時ファイルである )
                    this.未保存である = false;
            }
        }
        private void _名前を付けて保存する()
        {
            #region " ユーザに保存ファイル名を入力させる。"
            //-----------------
            string 絶対パスファイル名 = this._ファイル保存ダイアログを開いてファイル名を取得する();

            if( 絶対パスファイル名.Nullまたは空である() )
                return; // キャンセルされたらここで中断。

            this._作業フォルダパス = Path.GetDirectoryName( 絶対パスファイル名 );
            this._編集中のファイル名 = Path.GetFileName( 絶対パスファイル名 );
            //-----------------
            #endregion

            this._上書き保存する();

            this.未保存である = true; // ウィンドウタイトルに表示されているファイル名を変更するため、一度わざと true にする。
            this.未保存である = false;
        }

        private void _終了する()
        {
            this.Close();
        }

        private void _元に戻す()
        {
            // Undo する対象を UndoRedoリストから取得する。
            var cell = this.UndoRedo管理.Undoするセルを取得して返す();
            if( null == cell )
                return;     // なければ何もしない。

            // Undo を実行する。
            cell.Undoを実行する();

            // GUIを再描画する。
            this.UndoRedo用GUIのEnabledを設定する();
            this.選択チップの有無に応じて編集用GUIのEnabledを設定する();
            this.譜面をリフレッシュする();
        }
        private void _やり直す()
        {
            // Redo する対象を UndoRedoリストから取得する。
            var cell = this.UndoRedo管理.Redoするセルを取得して返す();
            if( null == cell )
                return; // なければ何もしない。

            // Redo を実行する。
            cell.Redoを実行する();

            // GUI を再描画する。
            this.UndoRedo用GUIのEnabledを設定する();
            this.選択チップの有無に応じて編集用GUIのEnabledを設定する();
            this.譜面をリフレッシュする();
        }

        private void _切り取る()
        {
            // 譜面にフォーカスがないなら、何もしない。
            if( false == this.pictureBox譜面パネル.Focused )
                return;

            // 切り取り ＝ コピー ＋ 削除
            this._コピーする();
            this._削除する();
        }
        private void _コピーする()
        {
            // 譜面にフォーカスがないなら何もしない。
            if( false == this.pictureBox譜面パネル.Focused )
                return;

            // コピーする。
            this.クリップボード.現在選択されているチップをボードにコピーする();

            // 画面を再描画する。
            this.選択チップの有無に応じて編集用GUIのEnabledを設定する();
            this.譜面をリフレッシュする();
        }
        private void _貼り付ける( int 貼り付けを開始する譜面内絶対位置grid )
        {
            // 譜面にフォーカスがないなら何もしない。
            if( false == this.pictureBox譜面パネル.Focused )
                return;

            this.クリップボード.チップを指定位置から貼り付ける( 貼り付けを開始する譜面内絶対位置grid );
        }
        private void _削除する()
        {
            // 譜面にフォーカスがないなら何もしない。
            if( false == this.pictureBox譜面パネル.Focused )
                return;

            try
            {
                this.UndoRedo管理.トランザクション記録を開始する();

                #region " 譜面が持つすべてのチップについて、選択されているチップがあれば削除する。"
                //----------------
                for( int i = this.譜面.SSTFormatScore.チップリスト.Count - 1; 0 <= i; i-- )
                {
                    var chip = (描画用チップ) this.譜面.SSTFormatScore.チップリスト[ i ];

                    if( chip.選択が確定していない )
                        continue;

                    var chip変更前 = new 描画用チップ( chip );

                    var cell = new UndoRedo.セル<描画用チップ>(
                        所有者ID: null,
                        Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                            変更対象.CopyFrom( 変更前 );
                            this.譜面.SSTFormatScore.チップリスト.Add( 変更対象 );
                            this.譜面.SSTFormatScore.チップリスト.Sort();
                        },
                        Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                            this.譜面.SSTFormatScore.チップリスト.Remove( 変更対象 );
                            this.未保存である = true;
                        },
                        変更対象: chip,
                        変更前の値: chip変更前,
                        変更後の値: null,
                        任意1: null,
                        任意2: null );

                    this.UndoRedo管理.セルを追加する( cell );
                    cell.Redoを実行する();
                }
                //----------------
                #endregion
            }
            finally
            {
                this.UndoRedo管理.トランザクション記録を終了する();

                #region " GUI を再描画する。"
                //----------------
                this.UndoRedo用GUIのEnabledを設定する();
                this.選択チップの有無に応じて編集用GUIのEnabledを設定する();
                this.譜面をリフレッシュする();
                //----------------
                #endregion
            }
        }

        private void _すべて選択する()
        {
            // 編集モードなら強制的に選択モードにする。
            if( this.編集モードである )
                this.選択モードに切替えて関連GUIを設定する();

            this.選択モード.全チップを選択する();
        }

        private void _選択モードにする()
        {
            this.選択モードに切替えて関連GUIを設定する();
        }
        private void _編集モードにする()
        {
            this.編集モードに切替えて関連GUIを設定する();
        }
        private void _モードを切替える()
        {
            if( this.選択モードである )
            {
                this.編集モードに切替えて関連GUIを設定する();
            }
            else
            {
                this.選択モードに切替えて関連GUIを設定する();
            }
        }

        private void _検索する()
        {
            this.選択モード.検索する();  // 現在のモードに関わらず、検索はすべて選択モードオブジェクトが行う。	
        }

        private void _ガイド間隔を変更する( int n分 )
        {
            // 引数チェック。
            if( !( new[] { 4, 6, 8, 12, 16, 24, 32, 36, 48, 64, 128, 0 }.Contains( n分 ) ) )
                throw new ArgumentException( $"不正なガイド間隔({n分})が指定されました。" );

            // 新しいガイド間隔を設定する。
            this._現在のガイド間隔 = n分;
            this.譜面.現在のガイド間隔を変更する( n分 );    // 譜面オブジェクトとも共有する。

            #region " ガイド間隔関連GUI（メニュー、コンボボックス）を更新する。"
            //-----------------

            // 一度、すべてのガイド間隔メニューのチェックをはずす。
            this.toolStripMenuItemガイド間隔4分.CheckState = CheckState.Unchecked;
            this.toolStripMenuItemガイド間隔6分.CheckState = CheckState.Unchecked;
            this.toolStripMenuItemガイド間隔8分.CheckState = CheckState.Unchecked;
            this.toolStripMenuItemガイド間隔12分.CheckState = CheckState.Unchecked;
            this.toolStripMenuItemガイド間隔16分.CheckState = CheckState.Unchecked;
            this.toolStripMenuItemガイド間隔24分.CheckState = CheckState.Unchecked;
            this.toolStripMenuItemガイド間隔32分.CheckState = CheckState.Unchecked;
            this.toolStripMenuItemガイド間隔36分.CheckState = CheckState.Unchecked;
            this.toolStripMenuItemガイド間隔48分.CheckState = CheckState.Unchecked;
            this.toolStripMenuItemガイド間隔64分.CheckState = CheckState.Unchecked;
            this.toolStripMenuItemガイド間隔128分.CheckState = CheckState.Unchecked;
            this.toolStripMenuItemガイド間隔フリー.CheckState = CheckState.Unchecked;

            // で、制定された間隔に対応するメニューだけをチェックする。 
            switch( n分 )
            {
                // Menu と ComboBox の２つを変更することでイベントが２つ発生し、最終的に
                // _ガイド間隔を変更する() を立て続けに２回呼び出してしまうことになるが……、まぁよしとする。
                case 4:
                    this.toolStripMenuItemガイド間隔4分.CheckState = CheckState.Checked;
                    this.toolStripComboBoxガイド間隔.SelectedIndex = 0;
                    break;

                case 6:
                    this.toolStripMenuItemガイド間隔6分.CheckState = CheckState.Checked;
                    this.toolStripComboBoxガイド間隔.SelectedIndex = 1;
                    break;

                case 8:
                    this.toolStripMenuItemガイド間隔8分.CheckState = CheckState.Checked;
                    this.toolStripComboBoxガイド間隔.SelectedIndex = 2;
                    break;

                case 12:
                    this.toolStripMenuItemガイド間隔12分.CheckState = CheckState.Checked;
                    this.toolStripComboBoxガイド間隔.SelectedIndex = 3;
                    break;

                case 16:
                    this.toolStripMenuItemガイド間隔16分.CheckState = CheckState.Checked;
                    this.toolStripComboBoxガイド間隔.SelectedIndex = 4;
                    break;

                case 24:
                    this.toolStripMenuItemガイド間隔24分.CheckState = CheckState.Checked;
                    this.toolStripComboBoxガイド間隔.SelectedIndex = 5;
                    break;

                case 32:
                    this.toolStripMenuItemガイド間隔32分.CheckState = CheckState.Checked;
                    this.toolStripComboBoxガイド間隔.SelectedIndex = 6;
                    break;

                case 36:
                    this.toolStripMenuItemガイド間隔36分.CheckState = CheckState.Checked;
                    this.toolStripComboBoxガイド間隔.SelectedIndex = 7;
                    break;

                case 48:
                    this.toolStripMenuItemガイド間隔48分.CheckState = CheckState.Checked;
                    this.toolStripComboBoxガイド間隔.SelectedIndex = 8;
                    break;

                case 64:
                    this.toolStripMenuItemガイド間隔64分.CheckState = CheckState.Checked;
                    this.toolStripComboBoxガイド間隔.SelectedIndex = 9;
                    break;

                case 128:
                    this.toolStripMenuItemガイド間隔128分.CheckState = CheckState.Checked;
                    this.toolStripComboBoxガイド間隔.SelectedIndex = 10;
                    break;

                case 0:
                    this.toolStripMenuItemガイド間隔フリー.CheckState = CheckState.Checked;
                    this.toolStripComboBoxガイド間隔.SelectedIndex = 11;
                    break;
            }
            //-----------------
            #endregion

            // 画面を再描画する。
            this.pictureBox譜面パネル.Invalidate();
        }
        private void _ガイド間隔を拡大する()
        {
            switch( this._現在のガイド間隔 )
            {
                case 4: break;
                case 6: this._ガイド間隔を変更する( 4 ); break;
                case 8: this._ガイド間隔を変更する( 6 ); break;
                case 12: this._ガイド間隔を変更する( 8 ); break;
                case 16: this._ガイド間隔を変更する( 12 ); break;
                case 24: this._ガイド間隔を変更する( 16 ); break;
                case 32: this._ガイド間隔を変更する( 24 ); break;
                case 36: this._ガイド間隔を変更する( 32 ); break;
                case 48: this._ガイド間隔を変更する( 36 ); break;
                case 64: this._ガイド間隔を変更する( 48 ); break;
                case 128: this._ガイド間隔を変更する( 64 ); break;
                case 0: this._ガイド間隔を変更する( 128 ); break;
            }
        }
        private void _ガイド間隔を縮小する()
        {
            switch( this._現在のガイド間隔 )
            {
                case 4: this._ガイド間隔を変更する( 6 ); break;
                case 6: this._ガイド間隔を変更する( 8 ); break;
                case 8: this._ガイド間隔を変更する( 12 ); break;
                case 12: this._ガイド間隔を変更する( 16 ); break;
                case 16: this._ガイド間隔を変更する( 24 ); break;
                case 24: this._ガイド間隔を変更する( 32 ); break;
                case 32: this._ガイド間隔を変更する( 36 ); break;
                case 36: this._ガイド間隔を変更する( 48 ); break;
                case 48: this._ガイド間隔を変更する( 64 ); break;
                case 64: this._ガイド間隔を変更する( 128 ); break;
                case 128: this._ガイド間隔を変更する( 0 ); break;
                case 0: break;
            }
        }

        private void _譜面拡大率を変更する( int n倍 )
        {
            if( ( 1 > n倍 ) || ( this.toolStripComboBox譜面拡大率.Items.Count < n倍 ) )
                throw new ArgumentException( $"不正な譜面拡大率({n倍})が指定されました。" );

            this.toolStripComboBox譜面拡大率.SelectedIndex = ( n倍 - 1 );

            this.譜面をリフレッシュする();

            this.Config.ViewScale = n倍;
        }

        private void _最初から再生する()
        {
            this._指定された小節の先頭から再生する( 小節番号: 0 );
        }
        private void _現在位置から再生する()
        {
            int 小節番号 = this.譜面.譜面内絶対位置gridに位置する小節の情報を返す( this.譜面.カレントラインの譜面内絶対位置grid ).小節番号;
            this._指定された小節の先頭から再生する( 小節番号 );
        }
        private void _現在位置からBGMのみ再生する()
        {
            int 小節番号 = this.譜面.譜面内絶対位置gridに位置する小節の情報を返す( this.譜面.カレントラインの譜面内絶対位置grid ).小節番号;
            this._指定された小節の先頭から再生する( 小節番号, ドラム音を発声する: false );
        }
        private void _指定された小節の先頭から再生する( int 小節番号, bool ドラム音を発声する = true )
        {
            if( ( this.Config.ViewerPath.Nullまたは空である() ) ||
                ( false == File.Exists( this.Config.ViewerPath ) ) )
                return;

            // 前回の一時ファイルが存在していれば削除する。
            if( File.Exists( this._最後にプレイヤーに渡した一時ファイル名 ) )
                File.Delete( this._最後にプレイヤーに渡した一時ファイル名 );

            // 新しい一時ファイルの名前をランダムに決める。
            do
            {
                this._最後にプレイヤーに渡した一時ファイル名 = Path.Combine( this._作業フォルダパス, Path.GetRandomFileName() + @".sstf" );
            }
            while( File.Exists( this._最後にプレイヤーに渡した一時ファイル名 ) );    // 同一名のファイルが存在してたらもう一度。（まずないだろうが）

            // 譜面を一時ファイルとして出力する。
            this._上書き保存する(
                this._最後にプレイヤーに渡した一時ファイル名,
                一時ファイルである: true );    // 一時ファイルである場合、「最近使ったファイル一覧」には残されない。

            // ビュアーに演奏開始を指示する。
            NamedPipeClientStream pipeStream = null;
            try
            {
                // ビュアーに接続を試みる。
                pipeStream = this._Viewerに接続する();
                if( pipeStream is null )
                {
                    #region " 接続に失敗したのでビュアーを起動する。"
                    //----------------
                    if( this.Config.ViewerPath.Nullまたは空である() || !File.Exists( this.Config.ViewerPath ) )
                        return; // ビュアーの設定がないか、ビュアーファイルが存在していない。

                    // ビュアーオプション(-s)を付けてプロセスを起動。
                    Process.Start( this.Config.ViewerPath, "-s" );

                    // 再度ビュアーに接続を試みる。
                    for( int リトライ回数 = 0; リトライ回数 < 50; リトライ回数++ )    // 5秒相当
                    {
                        pipeStream = this._Viewerに接続する();
                        if( null != pipeStream )
                            break;  // つながった

                        Thread.Sleep( 100 );
                    }
                    if( pipeStream is null )
                    {
                        // すべて接続に失敗した。諦める。
                        this._Viewer再生関連GUIのEnabledを設定する();
                        return;
                    }
                    //----------------
                    #endregion
                }

                // 演奏開始を指示する。
                var options = new DTXMania2.CommandLineOptions() {
                    Filename = this._最後にプレイヤーに渡した一時ファイル名,
                    再生開始 = true,
                    再生開始小節番号 = 小節番号,
                    ドラム音を発声する = ドラム音を発声する,
                };
                var ss = new StreamStringForNamedPipe( pipeStream );
                var yamlText = options.ToYaml(); // YAML化
                ss.WriteString( yamlText );
            }
            catch
            {
                // 例外は無視。
            }
            finally
            {
                pipeStream?.Dispose();
            }
        }
        private void _再生を停止する()
        {
            if( ( this.Config.ViewerPath.Nullまたは空である() ) ||
                ( false == File.Exists( this.Config.ViewerPath ) ) )
                return;

            // プレイヤーの演奏を停止する。
            NamedPipeClientStream pipeStream = null;
            try
            {
                // ビュアーに接続を試みる。
                pipeStream = this._Viewerに接続する();
                if( pipeStream is null )
                    return; // 失敗したら何もしない。

                // 演奏停止を指示する。
                var options = new DTXMania2.CommandLineOptions() {
                    再生停止 = true,
                };
                var ss = new StreamStringForNamedPipe( pipeStream );
                var yamlText = options.ToYaml(); // YAML化
                ss.WriteString( yamlText );
            }
            catch
            {
                // 例外は無視。
            }
            finally
            {
                pipeStream?.Dispose();
            }
        }

        private void _オプションを設定する()
        {
            using var dialog = new オプションダイアログ();

            // Config の現在の値をダイアログへ設定する。
            dialog.checkBoxオートフォーカス.CheckState = ( this.Config.AutoFocus ) ? CheckState.Checked : CheckState.Unchecked;
            dialog.checkBox最近使用したファイル.CheckState = ( this.Config.ShowRecentUsedFiles ) ? CheckState.Checked : CheckState.Unchecked;
            dialog.numericUpDown最近使用したファイルの最大表示個数.Value = this.Config.MaxOfUsedRecentFiles;
            dialog.textBoxViewerPath.Text = this.Config.ViewerPath;
            dialog.checkBoxSSTF変換通知ダイアログ.CheckState = ( this.Config.DisplaysConfirmOfSSTFConversion ) ? CheckState.Checked : CheckState.Unchecked;

            if( DialogResult.OK == dialog.ShowDialog( this ) )
            {
                // 決定された値をダイアログから Config に反映する。
                this.Config.AutoFocus = dialog.checkBoxオートフォーカス.Checked;
                this.Config.ShowRecentUsedFiles = dialog.checkBox最近使用したファイル.Checked;
                this.Config.MaxOfUsedRecentFiles = (int) dialog.numericUpDown最近使用したファイルの最大表示個数.Value;
                this.Config.ViewerPath = dialog.textBoxViewerPath.Text;
                this.Config.DisplaysConfirmOfSSTFConversion = dialog.checkBoxSSTF変換通知ダイアログ.Checked;

                this._Viewer再生関連GUIのEnabledを設定する();

                // [ファイル] メニューを修正。
                this._ConfigのRecentUsedFilesをファイルメニューへ追加する();

                // Config.xml を保存する。
                this.Config.保存する( Path.Combine( this._ユーザフォルダパス, Properties.Resources.CONFIG_FILE_NAME ) );
            }

            // 画面を再描画してダイアログのゴミを消す。
            this.Refresh();
        }

        private void _バージョンを表示する()
        {
            using var dialog = new バージョン表示ダイアログ();
            dialog.ShowDialog( this );
        }

        private void _小節長倍率を変更する( int 小節番号 )
        {
            double 変更後倍率 = 1.0;
            int 変更開始小節番号 = 小節番号;
            int 変更終了小節番号 = 小節番号;

            #region " 変更後の小節長倍率をユーザに入力させる。"
            //-----------------
            double 現在の小節長倍率 = this.譜面.SSTFormatScore.小節長倍率を取得する( 小節番号 );

            using( var dialog = new 小節長倍率入力ダイアログ( 小節番号 ) )
            {
                dialog.倍率 = (float) 現在の小節長倍率;
                dialog.後続も全部変更する = false;

                if( DialogResult.OK != dialog.ShowDialog( this ) )  // キャンセルされたらここで中断。
                    return;

                変更後倍率 = (double) dialog.倍率;
                変更終了小節番号 = ( dialog.後続も全部変更する ) ? this.譜面.SSTFormatScore.最大小節番号を返す() : 小節番号;
            }
            //-----------------
            #endregion

            try
            {
                this.UndoRedo管理.トランザクション記録を開始する();

                for( int i = 変更開始小節番号; i <= 変更終了小節番号; i++ )
                {
                    var 変更前倍率 = this.譜面.SSTFormatScore.小節長倍率を取得する( i );

                    #region " 新しい小節長倍率を設定する。"
                    //-----------------
                    var cell = new UndoRedo.セル<double>(
                        所有者ID: null,
                        Undoアクション: ( 変更対象, 変更前, 変更後, 対象小節番号, 任意2 ) => {
                            this.譜面.SSTFormatScore.小節長倍率を設定する( (int) 対象小節番号, 変更前 );
                            this.未保存である = true;
                        },
                        Redoアクション: ( 変更対象, 変更前, 変更後, 対象小節番号, 任意2 ) => {
                            this.譜面.SSTFormatScore.小節長倍率を設定する( (int) 対象小節番号, 変更後 );
                            this.未保存である = true;
                        },
                        変更対象: 0.0,
                        変更前の値: 変更前倍率,
                        変更後の値: 変更後倍率,
                        任意1: i,
                        任意2: null );

                    this.UndoRedo管理.セルを追加する( cell );
                    cell.Redoを実行する();
                    //-----------------
                    #endregion

                    #region " チップを移動または削除する。"
                    //-----------------
                    int 変化量grid = (int) ( ( 変更後倍率 - 変更前倍率 ) * this.GRID_PER_PART );

                    for( int j = this.譜面.SSTFormatScore.チップリスト.Count - 1; j >= 0; j-- )    // 削除する場合があるので後ろからカウントする。
                    {
                        var chip = (描画用チップ) this.譜面.SSTFormatScore.チップリスト[ j ];

                        // (A) 変更対象の小節内のチップ　→　移動なし。カウント変更あり。小節はみ出しチェックあり。

                        if( chip.小節番号 == i )
                        {
                            #region " (A-a) 小節からはみ出したチップは、削除する。"
                            //-----------------
                            int 次小節の先頭位置grid = this.譜面.小節先頭の譜面内絶対位置gridを返す( i ) + (int) ( 変更後倍率 * this.GRID_PER_PART );

                            if( 次小節の先頭位置grid <= chip.譜面内絶対位置grid )
                            {
                                var chip変更前 = new 描画用チップ( chip );

                                var cc = new UndoRedo.セル<描画用チップ>(
                                    所有者ID: null,
                                    Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                                        変更対象.CopyFrom( 変更前 );
                                        this.譜面.SSTFormatScore.チップリスト.Add( 変更対象 );
                                        this.譜面.SSTFormatScore.チップリスト.Sort();
                                        this.未保存である = true;
                                    },
                                    Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                                        this.譜面.SSTFormatScore.チップリスト.Remove( 変更対象 );
                                        this.未保存である = true;
                                    },
                                    変更対象: chip,
                                    変更前の値: chip変更前,
                                    変更後の値: null,
                                    任意1: null,
                                    任意2: null );

                                this.UndoRedo管理.セルを追加する( cc );
                                cc.Redoを実行する();
                            }
                            //-----------------
                            #endregion

                            #region " (A-b) 小節からはみ出さなかったチップは、パラメータを変更する。"
                            //-----------------
                            else
                            {
                                // 小節解像度を更新する。（小節内位置, 譜面内絶対位置grid は更新しない。）
                                var cc = new UndoRedo.セル<描画用チップ>(
                                    所有者ID: null,
                                    Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                                        変更対象.小節解像度 = (int) ( (double) 任意1 * this.GRID_PER_PART );
                                        this.未保存である = true;
                                    },
                                    Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                                        変更対象.小節解像度 = (int) ( (double) 任意2 * this.GRID_PER_PART );
                                        this.未保存である = true;
                                    },
                                    変更対象: chip,
                                    変更前の値: null,
                                    変更後の値: null,
                                    任意1: 変更前倍率,
                                    任意2: 変更後倍率 );

                                this.UndoRedo管理.セルを追加する( cc );
                                cc.Redoを実行する();
                            }
                            //-----------------
                            #endregion
                        }

                        // (B) 変更対象より先の小節内のチップ　→　移動あり。カウントなし。小節はみ出しチェックなし。

                        else if( i < chip.小節番号 )
                        {
                            #region " チップを 変化量grid ぶん移動する。"
                            //-----------------
                            var cc = new UndoRedo.セル<描画用チップ>(
                                所有者ID: null,
                                Undoアクション: ( 変更対象, 変更前, 変更後, _変化量grid, 任意2 ) => {
                                    変更対象.譜面内絶対位置grid -= (int) _変化量grid;
                                    this.未保存である = true;
                                },
                                Redoアクション: ( 変更対象, 変更前, 変更後, _変化量grid, 任意2 ) => {
                                    変更対象.譜面内絶対位置grid += (int) _変化量grid;
                                    this.未保存である = true;
                                },
                                変更対象: chip,
                                変更前の値: null,
                                変更後の値: null,
                                任意1: 変化量grid,
                                任意2: null );

                            this.UndoRedo管理.セルを追加する( cc );
                            cc.Redoを実行する();
                            //-----------------
                            #endregion
                        }
                    }
                    //-----------------
                    #endregion
                }
            }
            finally
            {
                this.UndoRedo管理.トランザクション記録を終了する();

                // 画面を再描画する。
                this.UndoRedo用GUIのEnabledを設定する();
                this.譜面をリフレッシュする();

                this.未保存である = true;
            }
        }

        private void _小節を挿入する( int 挿入前小節番号 )
        {
            // 挿入する新しい小節の小節長は、直前の（挿入前小節番号-1 の小節）と同じサイズとする。
            double 小節長倍率 = ( 0 < 挿入前小節番号 ) ? this.譜面.SSTFormatScore.小節長倍率を取得する( 挿入前小節番号 - 1 ) : 1.0;

            try
            {
                this.UndoRedo管理.トランザクション記録を開始する();

                #region " 後方のチップを移動する。"
                //-----------------
                int 挿入に伴う増加量grid = (int) ( this.GRID_PER_PART * 小節長倍率 );

                foreach( 描画用チップ chip in this.譜面.SSTFormatScore.チップリスト )
                {
                    if( 挿入前小節番号 <= chip.小節番号 )
                    {
                        var cell = new UndoRedo.セル<描画用チップ>(
                            所有者ID: null,
                            Undoアクション: ( 変更対象, 変更前, 変更後, _挿入に伴う増加量grid, 任意2 ) => {
                                変更対象.小節番号--;
                                変更対象.譜面内絶対位置grid -= (int) _挿入に伴う増加量grid;
                            },
                            Redoアクション: ( 変更対象, 変更前, 変更後, _挿入に伴う増加量grid, 任意2 ) => {
                                変更対象.小節番号++;
                                変更対象.譜面内絶対位置grid += (int) _挿入に伴う増加量grid;
                            },
                            変更対象: chip,
                            変更前の値: null,
                            変更後の値: null,
                            任意1: 挿入に伴う増加量grid,
                            任意2: null );

                        this.UndoRedo管理.セルを追加する( cell );
                        cell.Redoを実行する();
                    }
                }
                //-----------------
                #endregion

                #region " 後方の小節長倍率を移動する。"
                //-----------------
                var cc = new UndoRedo.セル<double>(
                    所有者ID: null,
                    Undoアクション: ( 変更対象, 変更前, 変更後, _挿入前小節番号, 任意2 ) => {
                        this.譜面.SSTFormatScore.小節長倍率リスト.RemoveAt( (int) _挿入前小節番号 );
                        this.未保存である = true;
                    },
                    Redoアクション: ( 変更対象, 変更前, 変更後, _挿入前小節番号, 任意2 ) => {
                        this.譜面.SSTFormatScore.小節長倍率リスト.Insert( (int) _挿入前小節番号, 小節長倍率 );
                        this.未保存である = true;
                    },
                    変更対象: 0.0,
                    変更前の値: 0.0,
                    変更後の値: 小節長倍率,
                    任意1: 挿入前小節番号,
                    任意2: null );

                this.UndoRedo管理.セルを追加する( cc );
                cc.Redoを実行する();
                //-----------------
                #endregion
            }
            finally
            {
                this.UndoRedo管理.トランザクション記録を終了する();

                // 画面を再描画する。
                this.UndoRedo用GUIのEnabledを設定する();
                this.譜面をリフレッシュする();

                this.未保存である = true;
            }
        }
        private void _小節を削除する( int 削除する小節番号 )
        {
            double 削除する小節の小節長倍率 = this.譜面.SSTFormatScore.小節長倍率を取得する( 削除する小節番号 );

            // 削除する。
            try
            {
                this.UndoRedo管理.トランザクション記録を開始する();

                #region " 削除する小節内のチップをすべて削除する。"
                //-----------------
                for( int i = this.譜面.SSTFormatScore.チップリスト.Count - 1; i >= 0; i-- )
                {
                    var chip = (描画用チップ) this.譜面.SSTFormatScore.チップリスト[ i ];

                    if( 削除する小節番号 == chip.小節番号 )
                    {
                        var chip変更前 = new 描画用チップ( chip );

                        var cell = new UndoRedo.セル<描画用チップ>(
                            所有者ID: null,
                            Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                                変更対象.CopyFrom( 変更前 );
                                this.譜面.SSTFormatScore.チップリスト.Add( 変更対象 );
                                this.譜面.SSTFormatScore.チップリスト.Sort();
                            },
                            Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                                this.譜面.SSTFormatScore.チップリスト.Remove( 変更対象 );
                                this.未保存である = true;
                            },
                            変更対象: chip,
                            変更前の値: chip変更前,
                            変更後の値: null,
                            任意1: null,
                            任意2: null );

                        this.UndoRedo管理.セルを追加する( cell );
                        cell.Redoを実行する();
                    }
                }
                //-----------------
                #endregion

                #region " 削除した小節より後方のチップを、その小節分だけ前に移動する。"
                //-----------------
                int 削除に伴う減少量grid = (int) ( this.GRID_PER_PART * 削除する小節の小節長倍率 );

                foreach( 描画用チップ chip in this.譜面.SSTFormatScore.チップリスト )
                {
                    if( 削除する小節番号 < chip.小節番号 )
                    {
                        var cell = new UndoRedo.セル<描画用チップ>(
                            所有者ID: null,
                            Undoアクション: ( 変更対象, 変更前, 変更後, _削除に伴う減少量grid, 任意2 ) => {
                                変更対象.小節番号++;
                                変更対象.譜面内絶対位置grid += (int) _削除に伴う減少量grid;
                            },
                            Redoアクション: ( 変更対象, 変更前, 変更後, _削除に伴う減少量grid, 任意2 ) => {
                                変更対象.小節番号--;
                                変更対象.譜面内絶対位置grid -= (int) _削除に伴う減少量grid;
                            },
                            変更対象: chip,
                            変更前の値: null,
                            変更後の値: null,
                            任意1: 削除に伴う減少量grid,
                            任意2: null );

                        this.UndoRedo管理.セルを追加する( cell );
                        cell.Redoを実行する();
                    }
                }
                //-----------------
                #endregion

                #region " 削除した小節を、小節長倍率リストからも削除する。"
                //-----------------
                var cc = new UndoRedo.セル<double>(
                    所有者ID: null,
                    Undoアクション: ( 変更対象, 変更前, 変更後, _削除する小節番号, 任意2 ) => {
                        this.譜面.SSTFormatScore.小節長倍率リスト.Insert( (int) _削除する小節番号, 変更前 );
                        this.未保存である = true;
                    },
                    Redoアクション: ( 変更対象, 変更前, 変更後, _削除する小節番号, 任意2 ) => {
                        this.譜面.SSTFormatScore.小節長倍率リスト.RemoveAt( (int) _削除する小節番号 );
                        this.未保存である = true;
                    },
                    変更対象: 0.0,
                    変更前の値: 削除する小節の小節長倍率,
                    変更後の値: 0.0,
                    任意1: 削除する小節番号,
                    任意2: null );

                this.UndoRedo管理.セルを追加する( cc );
                cc.Redoを実行する();
                //-----------------
                #endregion
            }
            finally
            {
                this.UndoRedo管理.トランザクション記録を終了する();

                // 画面を再描画する。
                this.UndoRedo用GUIのEnabledを設定する();
                this.譜面をリフレッシュする();

                this.未保存である = true;
            }
        }

        private void _小節の先頭へ移動する( int 小節番号 )
        {
            // 小節番号をクリッピングする。
            小節番号 = Math.Clamp( 小節番号, min: 0, max: this.譜面.SSTFormatScore.最大小節番号を返す() );

            // 垂直スクロールバーを移動させると、画面も自動的に移動する。
            var bar = this.vScrollBar譜面用垂直スクロールバー;
            bar.Value = ( ( bar.Maximum + 1 ) - bar.LargeChange ) - this.譜面.小節先頭の譜面内絶対位置gridを返す( 小節番号 );
        }
        //----------------
        #endregion

        #region " アクション補佐メソッド（複数のアクションで共通する処理。） "
        //----------------
        private void _エディタを初期化する()
        {
            this._編集中のファイル名 = null;

            #region " 各種オブジェクトを生成する。"
            //-----------------
            this.譜面?.Dispose();
            this.譜面 = new 譜面( this );  // 譜面は、選択・編集モードよりも先に生成すること。

            this.UndoRedo管理 = new UndoRedo.UndoRedo管理();
            this.選択モード = new 選択モード( this );
            this.編集モード = new 編集モード( this );
            this.クリップボード = new クリップボード( this );
            //-----------------
            #endregion

            // GUI の初期値を設定する。

            #region " 基本情報タブ "
            //-----------------
            this._次のプロパティ変更がUndoRedoリストに載らないようにする();
            this.textBox曲名.Clear();
            this.textBoxアーティスト名.Clear();
            this.textBoxLevel.Text = "5.00";
            this.trackBarLevel.Value = 500;
            this.textBox説明.Clear();
            this.textBoxBGV.Clear();
            this.textBoxBGM.Clear();
            this.numericUpDownメモ用小節番号.Value = 0;
            this.textBoxメモ.Clear();
            this.textBoxプレビュー音声.Clear();
            this.textBoxプレビュー画像.Clear();
            this.pictureBoxプレビュー画像.Image = Properties.Resources.既定のプレビュー画像;
            //-----------------
            #endregion
            #region " Viewer 再生 "
            //-----------------
            this._Viewer再生関連GUIのEnabledを設定する();
            //-----------------
            #endregion
            #region " ガイド間隔 "
            //-----------------
            this._ガイド間隔を変更する( 16 ); // 初期値は 1/16。
            //-----------------
            #endregion
            #region " 譜面拡大率 "
            //-----------------
            this._譜面拡大率を変更する( this.Config.ViewScale );
            //-----------------
            #endregion
            #region " Undo/Redo "
            //-----------------
            this._次のプロパティ変更がUndoRedoリストに載るようにする();
            this.UndoRedo用GUIのEnabledを設定する();
            //-----------------
            #endregion
            #region " 垂直スクロールバー "
            //-----------------
            this.vScrollBar譜面用垂直スクロールバー.Minimum = 0;
            this.vScrollBar譜面用垂直スクロールバー.Maximum = ( this.譜面.全小節の高さgrid - 1 );
            this.vScrollBar譜面用垂直スクロールバー.SmallChange = ( this.GRID_PER_PART / 16 );
            this.vScrollBar譜面用垂直スクロールバー.LargeChange = this.GRID_PER_PART;
            this.vScrollBar譜面用垂直スクロールバー.Value = this.vScrollBar譜面用垂直スクロールバー.Maximum - this.vScrollBar譜面用垂直スクロールバー.LargeChange;
            //-----------------
            #endregion

            // 最初は編集モードで始める。
            this.編集モードに切替えて関連GUIを設定する();

            this.未保存である = false;
        }

        private DialogResult _未保存なら保存する()
        {
            // 既に保存済みなら何もしない。
            if( false == this.未保存である )
                return DialogResult.OK;

            // [編集中のデータを保存しますか？] ダイアログを表示。
            var result = MessageBox.Show(
                Properties.Resources.MSG_編集中のデータを保存しますか,
                Properties.Resources.MSG_確認ダイアログのタイトル,
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1 );

            // Yes なら上書き保存する。
            if( DialogResult.Yes == result )
                this._上書き保存する();

            // 画面を再描画してダイアログを消去する。
            this.Refresh();

            return result;  // ダイアログの結果を返す。
        }

        private string _ファイル保存ダイアログを開いてファイル名を取得する()
        {
            DialogResult result;
            string ファイル名;
            int filterIndex;

            // ダイアログでファイル名を取得する。
            using( var dialog = new SaveFileDialog() {
                Title = "名前をつけて保存",
                Filter = "SSTFoverDTXファイル(*.dtx)|*.dtx|SSTFファイル(*.sstf)|*.sstf",
                FilterIndex = 1,
                InitialDirectory = this._作業フォルダパス,
            } )
            {
                result = dialog.ShowDialog( this );
                ファイル名 = dialog.FileName;
                filterIndex = dialog.FilterIndex;
            }

            // 画面を再描画してダイアログのゴミを消去する。
            this.Refresh();

            // キャンセルされたら ""（空文字列）を返す。
            if( DialogResult.OK != result )
                return "";

            // ファイルの拡張子がなければ付与する。
            if( 0 == Path.GetExtension( ファイル名 ).Length )
            {
                string 既定の拡張子 = filterIndex switch
                {
                    1 => ".dtx",
                    2 => ".sstf",
                    _ => throw new NotImplementedException(),
                };

                ファイル名 = Path.ChangeExtension( ファイル名, 既定の拡張子 );
            }

            return ファイル名;
        }

        private void _ファイルを読み込む( string ファイル名 )
        {
            bool SSTFoverDTXである = スコア.SSTFoverDTX.ファイルがSSTFoverDTXである( ファイル名 );

            #region " .sstf 以外のファイルの場合（SSTFoverDTXを除く）、SSTF形式でインポートする旨を表示する。"
            //----------------
            if( this.Config.DisplaysConfirmOfSSTFConversion )
            {
                var 拡張子 = Path.GetExtension( ファイル名 ).ToLower();

                if( 拡張子 != ".sstf" && !SSTFoverDTXである )   // SSTFoverDTX なら表示しない
                {
                    // [SSTF形式に変換しますか？] ダイアログを表示。
                    var result = MessageBox.Show(
                        Properties.Resources.MSG_SSTF形式に変換します,
                        Properties.Resources.MSG_確認ダイアログのタイトル,
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button1 );

                    // No なら何もしない。
                    if( DialogResult.No == result )
                        return;
                }
            }
            //----------------
            #endregion

            this._エディタを初期化する();

            #region " [読み込み中です] ポップアップを表示する。"
            //-----------------
            var msg = new Popupメッセージ(
                Properties.Resources.MSG_読み込み中です + Environment.NewLine +
                Properties.Resources.MSG_しばらくお待ち下さい );
            msg.Owner = this;
            msg.Show();
            msg.Refresh();
            //-----------------
            #endregion

            try
            {
                // 読み込む。
                this.譜面.曲データファイルを読み込む( ファイル名 );

                // 最低でも 10 小節は存在させる。
                int 最大小節番号 = this.譜面.SSTFormatScore.最大小節番号を返す();
                for( int n = 最大小節番号 + 1; n < 9; n++ )
                    ;

                string 読み込み時の拡張子 = Path.GetExtension( ファイル名 ).ToLower();
                if( !SSTFoverDTXである )
                {
                    // 読み込んだファイルの拡張子を .sstf に変換。（ファイルはすでに読み込み済み）
                    this._編集中のファイル名 = Path.ChangeExtension( Path.GetFileName( ファイル名 ), ".sstf" );
                }
                else
                    this._編集中のファイル名 = Path.GetFileName( ファイル名 );
                this._作業フォルダパス = Path.GetDirectoryName( ファイル名 ) + @"\";

                // 読み込んだファイルを [ファイル]メニューの最近使ったファイル一覧に追加する。
                this.Config.ファイルを最近使ったファイルの一覧に追加する( Path.Combine( this._作業フォルダパス, this._編集中のファイル名 ) );
                this._ConfigのRecentUsedFilesをファイルメニューへ追加する();

                // 基本情報タブを設定する。

                this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                this.textBox曲名.Text = 譜面.SSTFormatScore.曲名;

                this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                this.textBoxアーティスト名.Text = 譜面.SSTFormatScore.アーティスト名;

                this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                this.textBoxLevel.Text = 譜面.SSTFormatScore.難易度.ToString( "0.00" );

                this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                this.trackBarLevel.Value = Math.Clamp( (int) ( 譜面.SSTFormatScore.難易度 * 100 ), min: 0, max: 999 );

                this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                this.textBox説明.Text = 譜面.SSTFormatScore.説明文;

                this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                this.textBoxBGV.Text = 譜面.SSTFormatScore.BGVファイル名;

                this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                this.textBoxBGM.Text = 譜面.SSTFormatScore.BGMファイル名;

                this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                this.textBoxメモ.Text = ( this.譜面.SSTFormatScore.小節メモリスト.ContainsKey( 0 ) ) ? this.譜面.SSTFormatScore.AVIリスト[ 0 ] : "";

                this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                this.textBoxプレビュー音声.Text = 譜面.SSTFormatScore.プレビュー音声ファイル名;

                this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                this.textBoxプレビュー画像.Text = 譜面.SSTFormatScore.プレビュー画像ファイル名;

                this._プレビュー画像を更新する();

                // ウィンドウのタイトルバーの表示変更（str編集中のファイル名 が確定した後に）
                this.未保存である = true;     // 以前の状態によらず、確実に更新するようにする。

                // 読み込み時と拡張子が同一である（が読み込み時はSSTFoverDTXではない）場合は、保存済み状態としておく。
                if( 読み込み時の拡張子.ToLower() == Path.GetExtension( this._編集中のファイル名 ).ToLower() &&
                    ! SSTFoverDTXである )
                    this.未保存である = false;
            }
            catch( InvalidDataException )
            {
                MessageBox.Show(
                    Properties.Resources.MSG_対応していないファイルです,
                    Properties.Resources.MSG_確認ダイアログのタイトル,
                    MessageBoxButtons.OK );
            }

            #region "「読み込み中です」ポップアップを閉じる。 "
            //-----------------
            msg.Close();
            //-----------------
            #endregion

            // 最後に、ダイアログのゴミなどを消すために画面を再描画する。
            this.Refresh();
        }

        private enum タブ種別 : int { 基本情報 = 0 }

        private void _タブを選択する( タブ種別 eタブ種別 )
        {
            this.tabControl情報タブコンテナ.SelectedIndex = (int) eタブ種別;
        }

        private void _次のプロパティ変更がUndoRedoリストに載らないようにする()
        {
            UndoRedo.UndoRedo管理.UndoRedoした直後である = true;
        }

        private void _次のプロパティ変更がUndoRedoリストに載るようにする()
        {
            UndoRedo.UndoRedo管理.UndoRedoした直後である = false;
        }

        private void _垂直スクロールバーと譜面の上下位置を調整する()
        {
            var bar = this.vScrollBar譜面用垂直スクロールバー;
            var box = this.pictureBox譜面パネル;
            var panel2 = this.splitContainer分割パネルコンテナ.Panel2;

            // 譜面パネルの長さをパネルに合わせて調整する。
            box.ClientSize = new Size(
                box.ClientSize.Width,
                panel2.ClientSize.Height - box.Location.Y );

            // 現在のバーの位置を割合で記憶する。
            var bar率 = (double) bar.Value / (double) ( bar.Maximum - bar.Minimum );

            // 新しい値域を設定した後、バーの位置を記憶しておいた割合で設定。
            bar.Minimum = 0;
            bar.Maximum = this.譜面.全小節の高さgrid - 1;
            bar.Value = (int) ( ( bar.Maximum - bar.Minimum ) * bar率 );

            // 譜面長さが画面長さより短いなら、スクロールバーを表示しない。
            bar.Enabled = ( bar.Maximum > bar.LargeChange ) ? true : false;
        }

        private void _Viewer再生関連GUIのEnabledを設定する()
        {
            bool 各GUIは有効;

            using( var pipeStream = this._Viewerに接続する() )
            {
                if( null != pipeStream )
                {
                    // (A) パイプラインサーバが起動しているなら、各GUIは有効。
                    各GUIは有効 = true;
                }
                else if( File.Exists( this.Config.ViewerPath ) )
                {
                    // (B) パイプラインサーバが起動していなくても、設定されている Viewer ファイルが存在するなら、各GUIは有効。
                    各GUIは有効 = true;
                }
                else
                {
                    // (C) パイプラインサーバが起動しておらず、かつ Viewer ファイルも存在しないなら、各GUIは無効。
                    各GUIは有効 = false;
                }
            }

            this.toolStripButton先頭から再生.Enabled = 各GUIは有効;
            this.toolStripButton現在位置から再生.Enabled = 各GUIは有効;
            this.toolStripButton現在位置からBGMのみ再生.Enabled = 各GUIは有効;
            this.toolStripButton再生停止.Enabled = 各GUIは有効;

            this.toolStripMenuItem先頭から再生.Enabled = 各GUIは有効;
            this.toolStripMenuItem現在位置から再生.Enabled = 各GUIは有効;
            this.toolStripMenuItem現在位置からBGMのみ再生.Enabled = 各GUIは有効;
            this.toolStripMenuItem再生停止.Enabled = 各GUIは有効;
        }

        private void _ConfigのRecentUsedFilesをファイルメニューへ追加する()
        {
            #region " [ファイル] メニューから、[最近使ったファイルの一覧] をクリアする。"
            //-----------------
            for( int i = 0; i < this.toolStripMenuItemファイル.DropDownItems.Count; i++ )
            {
                var item = this.toolStripMenuItemファイル.DropDownItems[ i ];

                // ↓削除したくないサブメニューの一覧。これ以外のサブメニュー項目はすべて削除する。
                if( item != this.toolStripMenuItem新規作成 &&
                    item != this.toolStripMenuItem開く &&
                    item != this.toolStripMenuItem上書き保存 &&
                    item != this.toolStripMenuItem名前を付けて保存 &&
                    item != this.toolStripSeparator1 &&
                    item != this.toolStripMenuItem終了 )
                {
                    this.toolStripMenuItemファイル.DropDownItems.Remove( item );
                    i = -1; // 要素数が変わったので列挙しなおし。RemoveAll() はないのか。
                }
            }
            //-----------------
            #endregion

            if( ( false == this.Config.ShowRecentUsedFiles ) || // 表示しない or
                ( 0 == this.Config.RecentUsedFiles.Count ) )    // 履歴が 0 件
                return;

            #region " Config が持つ履歴にそって、[ファイル] メニューにサブメニュー項目リストを追加する（ただし最大表示数まで）。"
            //-----------------
            // [File] のサブメニューリストに項目が１つでもある場合は、履歴サブメニュー項目を追加する前に、[終了] の下にセパレータを入れる。手動で。
            bool セパレータの追加がまだ = true;

            // すべての [最近使ったファイル] について...
            for( int i = 0; i < this.Config.RecentUsedFiles.Count; i++ )
            {
                // 最大表示数を越えたら中断。
                if( this.Config.MaxOfUsedRecentFiles <= i )
                    return;

                // ファイルパスを、サブメニュー項目として [ファイル] メニューに追加する。
                string ファイルパス = this.Config.RecentUsedFiles[ i ];
                if( string.IsNullOrEmpty( ファイルパス ) )
                    continue;

                // セパレータの追加がまだなら追加する。
                if( セパレータの追加がまだ )
                {
                    this.toolStripMenuItemファイル.DropDownItems.Add( new ToolStripSeparator() { Size = this.toolStripSeparator1.Size } );
                    セパレータの追加がまだ = false;
                }

                // ToolStripMenuItem を手動で作って [ファイル] のサブメニューリストに追加する。
                var item = new ToolStripMenuItem() {
                    Name = $"最近使ったファイル{i}",
                    Size = this.toolStripMenuItem終了.Size,
                    Text = $"&{i} {ファイルパス}",
                    ToolTipText = ファイルパス,
                };
                item.Click += new EventHandler( this.toolStripMenuItem最近使ったファイル_Click );
                this.toolStripMenuItemファイル.DropDownItems.Add( item );

                // 追加したファイルが既に存在していないなら項目を無効化（グレー表示）する。
                if( false == File.Exists( ファイルパス ) )
                    item.Enabled = false;
            }
            //-----------------
            #endregion
        }

        private Point _現在のマウス位置を譜面パネル内座標pxに変換して返す()
        {
            return this.pictureBox譜面パネル.PointToClient( new Point( Cursor.Position.X, Cursor.Position.Y ) );
        }

        private void _現在のチップ音量をツールバーに表示する()
        {
            this.toolStripLabel音量.Text = ( this._音量toラベル.ContainsKey( this.現在のチップ音量 ) ) ? this._音量toラベル[ this.現在のチップ音量 ] : @"???";
        }

        private void _プレビュー画像を更新する()
        {
            try
            {
                string path = this.textBoxプレビュー画像.Text;

                if( !Path.IsPathRooted( path ) )
                    path = Path.Combine( this._作業フォルダパス, path );

                this.pictureBoxプレビュー画像.Image = System.Drawing.Image.FromFile( path );
            }
            catch
            {
                this.pictureBoxプレビュー画像.Image = Properties.Resources.既定のプレビュー画像;
            }
        }
        //----------------
        #endregion


        // GUIイベントメソッド

        #region " メインフォーム イベント "
        //-----------------
        protected void メインフォーム_DragEnter( object sender, DragEventArgs e )
        {
            if( e.Data.GetDataPresent( DataFormats.FileDrop ) )
            {
                e.Effect = DragDropEffects.Copy;    // ファイルならコピーと見なす（カーソルがコピー型になる）
            }
            else
            {
                e.Effect = DragDropEffects.None;    // ファイルじゃないなら無視（カーソル変化なし）
            }
        }

        protected void メインフォーム_DragDrop( object sender, DragEventArgs e )
        {
            string[] data = (string[]) e.Data.GetData( DataFormats.FileDrop );

            if( 1 <= data.Length )
                this._指定されたファイルを開く( data[ 0 ] );            // Dropされたファイルが複数個あっても、先頭のファイルだけを有効とする。
        }

        protected void メインフォーム_FormClosing( object sender, FormClosingEventArgs e )
        {
            if( DialogResult.Cancel == this._未保存なら保存する() )
            {
                e.Cancel = true;
            }
            else
            {
                this._アプリの終了処理を行う();
            }
        }

        protected void メインフォーム_ResizeEnd( object sender, EventArgs e )
        {
            this.Config.WindowLocation = this.Location;
            this.Config.ClientSize = this.ClientSize;
        }
        //-----------------
        #endregion

        #region " メニューバー イベント [File] "
        //-----------------
        protected void toolStripMenuItem新規作成_Click( object sender, EventArgs e )
        {
            this._新規作成する();
        }

        protected void toolStripMenuItem開く_Click( object sender, EventArgs e )
        {
            this._開く();
        }

        protected void toolStripMenuItem上書き保存_Click( object sender, EventArgs e )
        {
            this._上書き保存する();
        }

        protected void toolStripMenuItem名前を付けて保存_Click( object sender, EventArgs e )
        {
            this._名前を付けて保存する();
        }

        protected void toolStripMenuItem終了_Click( object sender, EventArgs e )
        {
            this._終了する();
        }

        protected void toolStripMenuItem最近使ったファイル_Click( object sender, EventArgs e )
        {
            // ※このイベントハンドラに対応する「toolStripMenuItem最近使ったファイル」というアイテムはデザイナにはないので注意。
            //   最近使ったファイルをFileメニューへ追加する際に、手動で作って追加したアイテムに対するハンドラである。

            this._指定されたファイルを開く( ( (ToolStripMenuItem) sender ).ToolTipText );
        }
        //-----------------
        #endregion

        #region " メニューバー イベント [Edit] "
        //----------------
        protected void toolStripMenuItem元に戻す_Click( object sender, EventArgs e )
        {
            this._元に戻す();
        }

        protected void toolStripMenuItemやり直す_Click( object sender, EventArgs e )
        {
            this._やり直す();
        }

        protected void toolStripMenuItem切り取り_Click( object sender, EventArgs e )
        {
            this._切り取る();
        }

        protected void toolStripMenuItemコピー_Click( object sender, EventArgs e )
        {
            this._コピーする();
        }

        protected void toolStripMenuItem貼り付け_Click( object sender, EventArgs e )
        {
            var マウスの位置 = this._現在のマウス位置を譜面パネル内座標pxに変換して返す();

            // (A) マウスが譜面上になかった → 表示領域下辺から貼り付ける。
            if( ( ( 0 > マウスの位置.X ) || ( 0 > マウスの位置.Y ) ) ||
                ( ( マウスの位置.X > this.譜面パネルサイズ.Width ) || ( マウスの位置.Y > this.譜面パネルサイズ.Height ) ) )
            {
                this._貼り付ける( this.譜面.譜面表示下辺の譜面内絶対位置grid );
            }
            // (B) マウスが譜面上にある → そこから貼り付ける。
            else
            {
                this._貼り付ける( this.譜面.譜面パネル内Y座標pxにおける譜面内絶対位置gridをガイド幅単位で返す( マウスの位置.Y ) );
            }
        }

        protected void toolStripMenuItem削除_Click( object sender, EventArgs e )
        {
            this._削除する();
        }

        protected void toolStripMenuItemすべて選択_Click( object sender, EventArgs e )
        {
            this._すべて選択する();
        }

        protected void toolStripMenuItem選択モード_Click( object sender, EventArgs e )
        {
            this.選択モードに切替えて関連GUIを設定する();
        }

        protected void toolStripMenuItem編集モード_Click( object sender, EventArgs e )
        {
            this.編集モードに切替えて関連GUIを設定する();
        }

        protected void toolStripMenuItemモード切替え_Click( object sender, EventArgs e )
        {
            if( this.選択モードである )
            {
                this.編集モードに切替えて関連GUIを設定する();
            }
            else
            {
                this.選択モードに切替えて関連GUIを設定する();
            }
        }

        protected void toolStripMenuItem検索_Click( object sender, EventArgs e )
        {
            this._検索する();
        }
        //----------------
        #endregion

        #region " メニューバー イベント [View] "
        //----------------
        protected void toolStripMenuItemガイド間隔4分_Click( object sender, EventArgs e )
        {
            this._ガイド間隔を変更する( 4 );
        }

        protected void toolStripMenuItemガイド間隔6分_Click( object sender, EventArgs e )
        {
            this._ガイド間隔を変更する( 6 );
        }

        protected void toolStripMenuItemガイド間隔8分_Click( object sender, EventArgs e )
        {
            this._ガイド間隔を変更する( 8 );
        }

        protected void toolStripMenuItemガイド間隔12分_Click( object sender, EventArgs e )
        {
            this._ガイド間隔を変更する( 12 );
        }

        protected void toolStripMenuItemガイド間隔16分_Click( object sender, EventArgs e )
        {
            this._ガイド間隔を変更する( 16 );
        }

        protected void toolStripMenuItemガイド間隔24分_Click( object sender, EventArgs e )
        {
            this._ガイド間隔を変更する( 24 );
        }

        protected void toolStripMenuItemガイド間隔32分_Click( object sender, EventArgs e )
        {
            this._ガイド間隔を変更する( 32 );
        }

        protected void toolStripMenuItemガイド間隔36分_Click( object sender, EventArgs e )
        {
            this._ガイド間隔を変更する( 36 );
        }

        protected void toolStripMenuItemガイド間隔48分_Click( object sender, EventArgs e )
        {
            this._ガイド間隔を変更する( 48 );
        }

        protected void toolStripMenuItemガイド間隔64分_Click( object sender, EventArgs e )
        {
            this._ガイド間隔を変更する( 64 );
        }

        protected void toolStripMenuItemガイド間隔128分_Click( object sender, EventArgs e )
        {
            this._ガイド間隔を変更する( 128 );
        }

        protected void toolStripMenuItemガイド間隔フリー_Click( object sender, EventArgs e )
        {
            this._ガイド間隔を変更する( 0 );
        }

        protected void toolStripMenuItemガイド間隔拡大_Click( object sender, EventArgs e )
        {
            this._ガイド間隔を拡大する();
        }

        protected void toolStripMenuItemガイド間隔縮小_Click( object sender, EventArgs e )
        {
            this._ガイド間隔を縮小する();
        }
        //----------------
        #endregion

        #region " メニューバー イベント [Play] "
        //----------------
        protected void toolStripMenuItem先頭から再生_Click( object sender, EventArgs e )
        {
            this._最初から再生する();
        }

        protected void toolStripMenuItem現在位置から再生_Click( object sender, EventArgs e )
        {
            this._現在位置から再生する();
        }

        protected void toolStripMenuItem現在位置からBGMのみ再生_Click( object sender, EventArgs e )
        {
            this._現在位置からBGMのみ再生する();
        }

        protected void toolStripMenuItem再生停止_Click( object sender, EventArgs e )
        {
            this._再生を停止する();
        }
        //----------------
        #endregion

        #region " メニューバー イベント [Tool] "
        //----------------
        protected void toolStripMenuItemオプション_Click( object sender, EventArgs e )
        {
            this._オプションを設定する();
        }
        //----------------
        #endregion

        #region " メニューバー イベント [Help] "
        //----------------
        protected void toolStripMenuItemバージョン_Click( object sender, EventArgs e )
        {
            this._バージョンを表示する();
        }
        //----------------
        #endregion

        #region " ツールバー イベント "
        //-----------------
        protected void toolStripButton新規作成_Click( object sender, EventArgs e )
        {
            this._新規作成する();
        }

        protected void toolStripButton開く_Click( object sender, EventArgs e )
        {
            this._開く();
        }

        protected void toolStripButton上書き保存_Click( object sender, EventArgs e )
        {
            this._上書き保存する();
        }

        protected void toolStripButton切り取り_Click( object sender, EventArgs e )
        {
            this._切り取る();
        }

        protected void toolStripButtonコピー_Click( object sender, EventArgs e )
        {
            this._コピーする();
        }

        protected void toolStripButton貼り付け_Click( object sender, EventArgs e )
        {
            var マウスの位置 = this._現在のマウス位置を譜面パネル内座標pxに変換して返す();

            // (A) マウスが譜面上になかった → 表示領域下辺から貼り付ける。
            if( ( ( マウスの位置.X < 0 ) || ( マウスの位置.Y < 0 ) ) ||
                ( ( マウスの位置.X > this.譜面パネルサイズ.Width ) || ( マウスの位置.Y > this.譜面パネルサイズ.Height ) ) )
            {
                this._貼り付ける( this.譜面.譜面表示下辺の譜面内絶対位置grid );
            }
            // (B) マウスが譜面上にある → そこから貼り付ける。
            else
            {
                this._貼り付ける( this.譜面.譜面パネル内Y座標pxにおける譜面内絶対位置gridをガイド幅単位で返す( マウスの位置.Y ) );
            }
        }

        protected void toolStripButton削除_Click( object sender, EventArgs e )
        {
            this._削除する();
        }

        protected void toolStripButton元に戻す_Click( object sender, EventArgs e )
        {
            this._元に戻す();
        }

        protected void toolStripButtonやり直す_Click( object sender, EventArgs e )
        {
            this._やり直す();
        }

        protected void toolStripComboBox譜面拡大率_SelectedIndexChanged( object sender, EventArgs e )
        {
            this._譜面拡大率を変更する( this.toolStripComboBox譜面拡大率.SelectedIndex + 1 );
        }

        protected void toolStripComboBoxガイド間隔_SelectedIndexChanged( object sender, EventArgs e )
        {
            switch( this.toolStripComboBoxガイド間隔.SelectedIndex )
            {
                case 0:
                    this._ガイド間隔を変更する( 4 );
                    return;

                case 1:
                    this._ガイド間隔を変更する( 6 );
                    return;

                case 2:
                    this._ガイド間隔を変更する( 8 );
                    return;

                case 3:
                    this._ガイド間隔を変更する( 12 );
                    return;

                case 4:
                    this._ガイド間隔を変更する( 16 );
                    return;

                case 5:
                    this._ガイド間隔を変更する( 24 );
                    return;

                case 6:
                    this._ガイド間隔を変更する( 32 );
                    return;

                case 7:
                    this._ガイド間隔を変更する( 36 );
                    return;

                case 8:
                    this._ガイド間隔を変更する( 48 );
                    return;

                case 9:
                    this._ガイド間隔を変更する( 64 );
                    return;

                case 10:
                    this._ガイド間隔を変更する( 128 );
                    return;

                case 11:
                    this._ガイド間隔を変更する( 0 );
                    return;
            }

        }

        protected void toolStripButton選択モード_Click( object sender, EventArgs e )
        {
            this._選択モードにする();
        }

        protected void toolStripButton編集モード_Click( object sender, EventArgs e )
        {
            this._編集モードにする();
        }

        protected void toolStripButton先頭から再生_Click( object sender, EventArgs e )
        {
            this._最初から再生する();
        }

        protected void toolStripButton現在位置から再生_Click( object sender, EventArgs e )
        {
            this._現在位置から再生する();
        }

        protected void toolStripButton現在位置からBGMのみ再生_Click( object sender, EventArgs e )
        {
            this._現在位置からBGMのみ再生する();
        }

        protected void toolStripButton再生停止_Click( object sender, EventArgs e )
        {
            this._再生を停止する();
        }

        protected void toolStripButton音量Down_Click( object sender, EventArgs e )
        {
            int 新音量 = this.現在のチップ音量 - 1;
            this.現在のチップ音量 = ( 新音量 < メインフォーム.最小音量 ) ? メインフォーム.最小音量 : 新音量;

            this._現在のチップ音量をツールバーに表示する();
        }

        protected void toolStripButton音量UP_Click( object sender, EventArgs e )
        {
            int 新音量 = this.現在のチップ音量 + 1;
            this.現在のチップ音量 = ( 新音量 > メインフォーム.最大音量 ) ? メインフォーム.最大音量 : 新音量;

            this._現在のチップ音量をツールバーに表示する();
        }
        //-----------------
        #endregion

        #region " 分割パネルコンテナ、譜面パネル、スクロールバー イベント "
        //-----------------
        protected void pictureBox譜面パネル_MouseClick( object sender, MouseEventArgs e )
        {
            // フォーカスを得る。
            this.pictureBox譜面パネル.Focus();

            // 選択・編集モードオブジェクトのいずれかへ処理を引き継ぐ。
            if( this.選択モードである )
            {
                this.選択モード.MouseClick( e );
            }
            else
            {
                this.編集モード.MouseClick( e );
            }
        }

        protected void pictureBox譜面パネル_MouseDown( object sender, MouseEventArgs e )
        {
            // 選択モードオブジェクトへ処理を引き継ぐ。
            if( this.選択モードである )
                this.選択モード.MouseDown( e );
        }

        protected void pictureBox譜面パネル_MouseEnter( object sender, EventArgs e )
        {
            // オートフォーカスが有効の場合、譜面にマウスが入ったら譜面がフォーカスを得る。"
            if( this.Config.AutoFocus )
                this.pictureBox譜面パネル.Focus();
        }

        protected void pictureBox譜面パネル_MouseLeave( object sender, EventArgs e )
        {
            // 編集モードオブジェクトへ処理を引き継ぐ。
            if( this.編集モードである )
                this.編集モード.MouseLeave( e );
        }

        protected void pictureBox譜面パネル_MouseMove( object sender, MouseEventArgs e )
        {
            // 選択・編集モードオブジェクトのいずれかへ処理を引き継ぐ。
            if( this.選択モードである )
            {
                this.選択モード.MouseMove( e );
            }
            else
            {
                this.編集モード.MouseMove( e );
            }
        }

        protected void pictureBox譜面パネル_Paint( object sender, PaintEventArgs e )
        {
            if( false == this.初期化完了 )
                return;     // 初期化が終わってないのに呼び出されることがあるので、その場合は無視。

            #region " 小節数が変わってたら、スクロールバーの値域を調整する。"
            //-----------------
            int 全譜面の高さgrid = this.譜面.全小節の高さgrid;

            if( this.vScrollBar譜面用垂直スクロールバー.Maximum != 全譜面の高さgrid - 1 ) // 小節数が変わっている
            {
                // 譜面の高さ(grid)がどれだけ変わったか？
                int 増加分grid = ( 全譜面の高さgrid - 1 ) - this.vScrollBar譜面用垂直スクロールバー.Maximum;

                #region " スクロールバーの状態を新しい譜面の高さに合わせる。"
                //-----------------
                {
                    int value = this.vScrollBar譜面用垂直スクロールバー.Value;      // 次の式で Maximum が Value より小さくなると例外が発生するので、
                    this.vScrollBar譜面用垂直スクロールバー.Value = 0;              // Value のバックアップを取っておいて、ひとまず 0 にする。
                    this.vScrollBar譜面用垂直スクロールバー.Maximum = 全譜面の高さgrid - 1;

                    int newValue = value + 増加分grid;

                    // オーバーフローしないようクリッピングする。
                    if( 0 > newValue )
                    {
                        this.vScrollBar譜面用垂直スクロールバー.Value = 0;
                    }
                    else if( ( this.vScrollBar譜面用垂直スクロールバー.Maximum - this.vScrollBar譜面用垂直スクロールバー.LargeChange ) <= newValue )
                    {
                        this.vScrollBar譜面用垂直スクロールバー.Value = this.vScrollBar譜面用垂直スクロールバー.Maximum - this.vScrollBar譜面用垂直スクロールバー.LargeChange;
                    }
                    else
                    {
                        this.vScrollBar譜面用垂直スクロールバー.Value = newValue;
                    }
                }
                //-----------------
                #endregion

                #region " 譜面表示下辺の位置を更新する。"
                //-----------------
                this.譜面.譜面表示下辺の譜面内絶対位置grid =
                    ( ( this.vScrollBar譜面用垂直スクロールバー.Maximum - this.vScrollBar譜面用垂直スクロールバー.LargeChange ) + 1 ) - this.vScrollBar譜面用垂直スクロールバー.Value;
                //-----------------
                #endregion
            }
            //-----------------
            #endregion
            
            #region " 譜面を描画する。"
            //-----------------
            this.譜面.描画する( e.Graphics, this.pictureBox譜面パネル );
            //-----------------
            #endregion

            // 選択・編集モードオブジェクトのいずれかへ処理を引き継ぐ。
            if( this.選択モードである )
            {
                this.選択モード.Paint( e );
            }
            else
            {
                this.編集モード.Paint( e );
            }
        }

        protected void pictureBox譜面パネル_PreviewKeyDown( object sender, PreviewKeyDownEventArgs e )
        {
            if( Keys.Prior == e.KeyCode )
            {
                #region " PageUp → 垂直つまみを移動させる。あとはこの移動で生じる ChangedValue イベントで処理。"
                //-----------------
                int 移動すべき数grid = -this.GRID_PER_PART;
                int 新しい位置 = this.vScrollBar譜面用垂直スクロールバー.Value + 移動すべき数grid;
                int 最小値 = this.vScrollBar譜面用垂直スクロールバー.Minimum;
                int 最大値 = ( this.vScrollBar譜面用垂直スクロールバー.Maximum + 1 ) - this.vScrollBar譜面用垂直スクロールバー.LargeChange;

                if( 新しい位置 < 最小値 )
                {
                    新しい位置 = 最小値;
                }
                else if( 新しい位置 > 最大値 )
                {
                    新しい位置 = 最大値;
                }
                this.vScrollBar譜面用垂直スクロールバー.Value = 新しい位置;
                //-----------------
                #endregion
            }
            else if( Keys.Next == e.KeyCode )
            {
                #region " PageDown → 垂直つまみを移動させる。あとはこの移動で生じる ChangedValue イベントで処理。"
                //-----------------
                int 移動すべき数grid = this.GRID_PER_PART;
                int 新しい位置 = this.vScrollBar譜面用垂直スクロールバー.Value + 移動すべき数grid;
                int 最小値 = this.vScrollBar譜面用垂直スクロールバー.Minimum;
                int 最大値 = ( this.vScrollBar譜面用垂直スクロールバー.Maximum + 1 ) - this.vScrollBar譜面用垂直スクロールバー.LargeChange;

                if( 新しい位置 < 最小値 )
                {
                    新しい位置 = 最小値;
                }
                else if( 新しい位置 > 最大値 )
                {
                    新しい位置 = 最大値;
                }
                this.vScrollBar譜面用垂直スクロールバー.Value = 新しい位置;
                //-----------------
                #endregion
            }
            else
            {
                // 編集モードオブジェクトへ処理を引き継ぐ。
                if( this.編集モードである )
                    this.編集モード.PreviewKeyDown( e );
            }
        }

        protected void splitContainer分割パネルコンテナ_MouseWheel( object sender, MouseEventArgs e )
        {
            if( false == this.初期化完了 )
                return;     // 初期化が終わってないのに呼び出されることがあるので、その場合は無視。

            #region " 移動量に対応する grid だけ垂直つまみを移動させる。あとはこの移動で生じる ChangedValue イベントで処理する。"
            //-----------------
            if( 0 == e.Delta )
                return;     // 移動量なし

            // e.Delta は、スクロールバーを下へ動かしたいときに負、上へ動かしたいときに正となる。
            int 移動すべき行数 = ( -e.Delta * SystemInformation.MouseWheelScrollLines ) / 120;

            // 1行＝0.125拍とする。
            int 移動すべき数grid = 移動すべき行数 * ( this.GRID_PER_PART / 32 );

            // スクロールバーのつまみを移動する。
            int 新しい位置 = this.vScrollBar譜面用垂直スクロールバー.Value + 移動すべき数grid;
            int 最小値 = this.vScrollBar譜面用垂直スクロールバー.Minimum;
            int 最大値 = ( this.vScrollBar譜面用垂直スクロールバー.Maximum + 1 ) - this.vScrollBar譜面用垂直スクロールバー.LargeChange;

            if( 新しい位置 < 最小値 )
            {
                新しい位置 = 最小値;
            }
            else if( 新しい位置 > 最大値 )
            {
                新しい位置 = 最大値;
            }
            this.vScrollBar譜面用垂直スクロールバー.Value = 新しい位置;
            //-----------------
            #endregion
        }

        protected void splitContainer分割パネルコンテナ_Panel2_SizeChanged( object sender, EventArgs e )
        {
            if( false == this.初期化完了 )
                return;     // 初期化が終わってないのに呼び出されることがあるので、その場合は無視。

            this._垂直スクロールバーと譜面の上下位置を調整する();
        }

        protected void splitContainer分割パネルコンテナ_Panel2_Paint( object sender, PaintEventArgs e )
        {
            if( false == this.初期化完了 )
                return;     // 初期化が終わってないのに呼び出されることがあるので、その場合は無視。

            var g = e.Graphics;
            var メモ領域左上隅の位置 = new PointF() {
                X = this.譜面.レーンの合計幅px,
                Y = this.pictureBox譜面パネル.Location.Y,
            };

            #region " [小節メモ] を描画する。"
            //-----------------
            g.DrawString( Properties.Resources.MSG_小節メモ, this._メモ用フォント, Brushes.White, PointF.Add( メモ領域左上隅の位置, new Size( 24, -24 )/*マージン*/ ) );
            //-----------------
            #endregion

            #region " 小節メモの内容を描画する。"
            //-----------------

            // グリッド値は 上辺＞下辺 なので注意。
            int パネル下辺grid = this.譜面.譜面表示下辺の譜面内絶対位置grid;
            int パネル上辺grid = パネル下辺grid + ( this.pictureBox譜面パネル.ClientSize.Height * this.GRID_PER_PIXEL );
            int 開始小節番号 = this.譜面.譜面表示下辺に位置する小節番号;

            int 最大小節番号 = this.譜面.SSTFormatScore.最大小節番号を返す();
            for( int 小節番号 = 開始小節番号; 小節番号 <= 最大小節番号; 小節番号++ )
            {
                int 小節の下辺grid = this.譜面.小節先頭の譜面内絶対位置gridを返す( 小節番号 );
                int 小節の上辺grid = 小節の下辺grid + this.譜面.小節長をグリッドで返す( 小節番号 );

                if( 小節の下辺grid > パネル上辺grid )
                    break;  // 小節が画面上方にはみ出し切ってしまったらそこで終了。

                if( this.譜面.SSTFormatScore.小節メモリスト.ContainsKey( 小節番号 ) )
                {
                    string メモ = this.譜面.SSTFormatScore.小節メモリスト[ 小節番号 ];

                    string[] lines = メモ.Split( new string[] { Environment.NewLine }, StringSplitOptions.None );
                    int 行数 = lines.Length;

                    var メモの位置 = new PointF() {
                        X = メモ領域左上隅の位置.X + 4,   // + 4 はマージン
                        Y = メモ領域左上隅の位置.Y + ( パネル上辺grid - 小節の下辺grid ) / this.GRID_PER_PIXEL - ( 行数 * 16 ),       // 9pt = だいたい16px 
                    };
                    g.DrawString( メモ, this._メモ用フォント, Brushes.White, メモの位置 );
                }
            }
            //-----------------
            #endregion
        }

        protected void vScrollBar譜面用垂直スクロールバー_ValueChanged( object sender, EventArgs e )
        {
            if( false == this.初期化完了 )
                return;     // 初期化が終わってないのに呼び出されることがあるので、その場合は無視。

            var bar = vScrollBar譜面用垂直スクロールバー;

            if( bar.Enabled )
            {
                // 下辺の位置を再計算。
                this.譜面.譜面表示下辺の譜面内絶対位置grid = ( ( bar.Maximum + 1 ) - bar.LargeChange ) - bar.Value;

                // 編集モードの場合、カーソルのgrid位置を再計算。
                if( this.編集モードである )
                {
                    var cp = this.pictureBox譜面パネル.PointToClient( Cursor.Position );
                    this.編集モード.MouseMove( new MouseEventArgs( MouseButtons.None, 0, cp.X, cp.Y, 0 ) );
                }

                // メモ用小節番号を再計算。
                this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                this.numericUpDownメモ用小節番号.Value = this.譜面.カレントラインに位置する小節番号;
                this._次のプロパティ変更がUndoRedoリストに載るようにする();

                // 小節メモを再描画する。
                this.splitContainer分割パネルコンテナ.Panel2.Refresh();
            }
        }
        //-----------------
        #endregion

        #region " 譜面右メニュー イベント "
        //-----------------
        protected void toolStripMenuItem選択チップの切り取り_Click( object sender, EventArgs e )
        {
            this._切り取る();
        }

        protected void toolStripMenuItem選択チップのコピー_Click( object sender, EventArgs e )
        {
            this._コピーする();
        }

        protected void toolStripMenuItem選択チップの貼り付け_Click( object sender, EventArgs e )
        {
            // メニューが開かれたときのマウスの座標を取得。
            // ※メニューは必ずマウス位置を左上にして表示されるとは限らないため、メニューの表示位置からは取得しないこと。
            var マウスの位置 = this._選択モードでコンテクストメニューを開いたときのマウスの位置;

            if( this.譜面.譜面パネル内X座標pxにある編集レーンを返す( マウスの位置.X ) == 編集レーン種別.Unknown )
                return;     // クリックされた場所にレーンがないなら無視。

            // アクションを実行。
            this._貼り付ける( this.譜面.譜面パネル内Y座標pxにおける譜面内絶対位置gridをガイド幅単位で返す( マウスの位置.Y ) );
        }

        protected void toolStripMenuItem選択チップの削除_Click( object sender, EventArgs e )
        {
            this._削除する();
        }

        protected void toolStripMenuItemすべてのチップの選択_Click( object sender, EventArgs e )
        {
            // 編集モードなら強制的に選択モードにする。
            if( this.編集モードである )
                this.選択モードに切替えて関連GUIを設定する();

            // 全チップを選択。
            this.選択モード.全チップを選択する();
        }

        protected void toolStripMenuItem小節長変更_Click( object sender, EventArgs e )
        {
            // メニューが開かれたときのマウスの座標を取得。
            // ※メニューは必ずマウス位置を左上にして表示されるとは限らないため、メニューの表示位置からは取得しないこと。
            var マウスの位置 = this._選択モードでコンテクストメニューを開いたときのマウスの位置;

            if( this.譜面.譜面パネル内X座標pxにある編集レーンを返す( マウスの位置.X ) == 編集レーン種別.Unknown )
                return;     // クリックされた場所にレーンがないなら無視。

            // アクションを実行。
            this._小節長倍率を変更する( this.譜面.譜面パネル内Y座標pxにおける小節番号を返す( マウスの位置.Y ) );
        }

        protected void toolStripMenuItem小節の挿入_Click( object sender, EventArgs e )
        {
            // メニューが開かれたときのマウスの座標を取得。
            // ※メニューは必ずマウス位置を左上にして表示されるとは限らないため、メニューの表示位置からは取得しないこと。
            var マウスの位置 = this._選択モードでコンテクストメニューを開いたときのマウスの位置;

            if( this.譜面.譜面パネル内X座標pxにある編集レーンを返す( マウスの位置.X ) == 編集レーン種別.Unknown )
                return;     // クリックされた場所にレーンがないなら無視。

            // アクションを実行。
            this._小節を挿入する( this.譜面.譜面パネル内Y座標pxにおける小節番号を返す( マウスの位置.Y ) );
        }

        protected void toolStripMenuItem小節の削除_Click( object sender, EventArgs e )
        {
            // メニューが開かれたときのマウスの座標を取得。
            // ※メニューは必ずマウス位置を左上にして表示されるとは限らないため、メニューの表示位置からは取得しないこと。
            var マウスの位置 = this._選択モードでコンテクストメニューを開いたときのマウスの位置;

            if( this.譜面.譜面パネル内X座標pxにある編集レーンを返す( マウスの位置.X ) == 編集レーン種別.Unknown )
                return;     // クリックされた場所にレーンがないなら無視。

            // アクションを実行。
            this._小節を削除する( this.譜面.譜面パネル内Y座標pxにおける小節番号を返す( マウスの位置.Y ) );
        }
        //-----------------
        #endregion

        #region " 基本情報タブ イベント "
        //-----------------
        protected void textBox曲名_TextChanged( object sender, EventArgs e )
        {
            #region " この変更が Undo/Redo したことによるものではない場合、UndoRedoセルを追加 or 修正する。"
            //-----------------
            if( false == UndoRedo.UndoRedo管理.UndoRedoした直後である )
            {
                // 最新のセルの所有者が自分？
                var cell = this.UndoRedo管理.Undoするセルを取得して返す_見るだけ();

                if( ( null != cell ) && cell.所有権がある( this.textBox曲名 ) )
                {
                    // (A) 所有者である → 最新のセルの "変更後の値" を現在のコントロールの値に更新する。
                    ( (UndoRedo.セル<string>) cell ).変更後の値 = this.textBox曲名.Text;
                }
                else
                {
                    // (B) 所有者ではない → 以下のようにセルを新規追加する。
                    //    "変更前の値" ← 以前の値
                    //    "変更後の値" ← 現在の値
                    //    "所有者ID" ← 対象となるコンポーネントオブジェクト
                    var cc = new UndoRedo.セル<string>(
                        所有者ID: this.textBox曲名,
                        Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                            this._タブを選択する( タブ種別.基本情報 );
                            this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                            this.textBox曲名.Text = 変更前;
                            this.textBox曲名.Focus();
                        },
                        Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                            this._タブを選択する( タブ種別.基本情報 );
                            this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                            this.textBox曲名.Text = 変更後;
                            this.textBox曲名.Focus();
                        },
                        変更対象: null,
                        変更前の値: this.textBox曲名_以前の値,
                        変更後の値: this.textBox曲名.Text,
                        任意1: null,
                        任意2: null );

                    this.UndoRedo管理.セルを追加する( cc );

                    // Undo ボタンを有効にする。
                    this.UndoRedo用GUIのEnabledを設定する();
                }
            }
            //-----------------
            #endregion

            this.textBox曲名_以前の値 = this.textBox曲名.Text;      // 以前の値 ← 現在の値
            UndoRedo.UndoRedo管理.UndoRedoした直後である = false;
            this.未保存である = true;

            // スコアには随時保存する。
            譜面.SSTFormatScore.曲名 = this.textBox曲名.Text;
        }
        protected void textBox曲名_Validated( object sender, EventArgs e )
        {
            // 最新の UndoRedoセル の所有権を放棄する。
            this.UndoRedo管理.Undoするセルを取得して返す_見るだけ()?.所有権を放棄する( this.textBox曲名 );
        }
        private string textBox曲名_以前の値 = "";

        protected void textBoxアーティスト名_TextChanged( object sender, EventArgs e )
        {
            #region " この変更が Undo/Redo したことによるものではない場合、UndoRedoセルを追加 or 修正する。"
            //-----------------
            if( false == UndoRedo.UndoRedo管理.UndoRedoした直後である )
            {
                // 最新のセルの所有者が自分？
                var cell = this.UndoRedo管理.Undoするセルを取得して返す_見るだけ();

                if( ( null != cell ) && cell.所有権がある( this.textBoxアーティスト名 ) )
                {
                    // (A) 所有者である → 最新のセルの "変更後の値" を現在のコントロールの値に更新する。
                    ( (UndoRedo.セル<string>) cell ).変更後の値 = this.textBoxアーティスト名.Text;
                }
                else
                {
                    // (B) 所有者ではない → 以下のようにセルを新規追加する。
                    //    "変更前の値" ← 以前の値
                    //    "変更後の値" ← 現在の値
                    //    "所有者ID" ← 対象となるコンポーネントオブジェクト
                    var cc = new UndoRedo.セル<string>(
                        所有者ID: this.textBoxアーティスト名,
                        Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                            this._タブを選択する( タブ種別.基本情報 );
                            this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                            this.textBoxアーティスト名.Text = 変更前;
                            this.textBoxアーティスト名.Focus();
                        },
                        Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                            this._タブを選択する( タブ種別.基本情報 );
                            this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                            this.textBoxアーティスト名.Text = 変更後;
                            this.textBoxアーティスト名.Focus();
                        },
                        変更対象: null,
                        変更前の値: this.textBoxアーティスト名_以前の値,
                        変更後の値: this.textBoxアーティスト名.Text,
                        任意1: null,
                        任意2: null );

                    this.UndoRedo管理.セルを追加する( cc );

                    // Undo ボタンを有効にする。
                    this.UndoRedo用GUIのEnabledを設定する();
                }
            }
            //-----------------
            #endregion

            this.textBoxアーティスト名_以前の値 = this.textBoxアーティスト名.Text;      // 以前の値 ← 現在の値
            UndoRedo.UndoRedo管理.UndoRedoした直後である = false;
            this.未保存である = true;

            // スコアには随時保存する。
            譜面.SSTFormatScore.アーティスト名 = this.textBoxアーティスト名.Text;
        }
        protected void textBoxアーティスト名_Validated( object sender, EventArgs e )
        {
            // 最新の UndoRedoセル の所有権を放棄する。
            this.UndoRedo管理.Undoするセルを取得して返す_見るだけ()?.所有権を放棄する( this.textBoxアーティスト名 );
        }
        private string textBoxアーティスト名_以前の値 = "";

        protected void textBox説明_TextChanged( object sender, EventArgs e )
        {
            #region " この変更が Undo/Redo したことによるものではない場合、UndoRedoセルを追加 or 修正する。"
            //-----------------
            if( false == UndoRedo.UndoRedo管理.UndoRedoした直後である )
            {
                // 最新のセルの所有者が自分？
                var cell = this.UndoRedo管理.Undoするセルを取得して返す_見るだけ();
                if( ( null != cell ) && cell.所有権がある( this.textBox説明 ) )
                {
                    // (A) 所有者である → 最新のセルの "変更後の値" を現在のコントロールの値に更新。

                    ( (UndoRedo.セル<string>) cell ).変更後の値 = this.textBox説明.Text;
                }
                else
                {
                    // (B) 所有者ではない → 以下のようにセルを新規追加する。
                    //    "変更前の値" ← 以前の値
                    //    "変更後の値" ← 現在の値
                    //    "所有者ID" ← 対象となるコンポーネントオブジェクト
                    var cc = new UndoRedo.セル<string>(
                        所有者ID: this.textBox説明,
                        Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                            this._タブを選択する( タブ種別.基本情報 );
                            this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                            this.textBox説明.Text = 変更前;
                            this.textBox説明.Focus();
                        },
                        Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                            this._タブを選択する( タブ種別.基本情報 );
                            this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                            this.textBox説明.Text = 変更後;
                            this.textBox説明.Focus();
                        },
                        変更対象: null,
                        変更前の値: this.textBox説明_以前の値,
                        変更後の値: this.textBox説明.Text,
                        任意1: null,
                        任意2: null );

                    this.UndoRedo管理.セルを追加する( cc );

                    // Undo ボタンを有効にする。
                    this.UndoRedo用GUIのEnabledを設定する();
                }
            }
            //-----------------
            #endregion

            this.textBox説明_以前の値 = this.textBox説明.Text;  // 以前の値 ← 現在の値
            UndoRedo.UndoRedo管理.UndoRedoした直後である = false;
            this.未保存である = true;

            // スコアには随時保存する。
            譜面.SSTFormatScore.説明文 = this.textBox説明.Text;
        }
        protected void textBox説明_Validated( object sender, EventArgs e )
        {
            // 最新 UndoRedoセル の所有権を放棄する。
            this.UndoRedo管理.Undoするセルを取得して返す_見るだけ()?.所有権を放棄する( this.textBox説明 );
        }
        private string textBox説明_以前の値 = "";

        protected void textBoxメモ_TextChanged( object sender, EventArgs e )
        {
            #region " この変更が Undo/Redo したことによるものではない場合、UndoRedoセルを追加or修正する。"
            //-----------------
            if( !UndoRedo.UndoRedo管理.UndoRedoした直後である )
            {
                // 最新のセルの所有者が自分？

                UndoRedo.セルBase cell = this.UndoRedo管理.Undoするセルを取得して返す_見るだけ();

                if( ( cell != null ) && cell.所有権がある( this.textBoxメモ ) )
                {
                    // (Yes) 最新のセルの "変更後の値" を <現在の値> に更新。

                    ( (UndoRedo.セル<string>) cell ).変更後の値 = this.textBoxメモ.Text;
                }
                else
                {
                    // (No) セルを新規追加：
                    //      "変更前の値" = <以前の値>
                    //      "変更後の値" = <現在の値>
                    //      "所有者ID" = 対象となるコンポーネントオブジェクト

                    var cc = new UndoRedo.セル<string>(
                        所有者ID: this.textBoxメモ,
                        Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                            this._タブを選択する( タブ種別.基本情報 );
                            this.numericUpDownメモ用小節番号.Value = (decimal) 任意1;
                            this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                            this.textBoxメモ.Text = 変更前;
                            this.textBoxメモ.Focus();

                            int 小節番号 = (int) ( (decimal) 任意1 );

                            #region " dicメモ の更新 "
                            //-----------------
                            if( this.譜面.SSTFormatScore.小節メモリスト.ContainsKey( 小節番号 ) )
                            {
                                if( string.IsNullOrEmpty( 変更前 ) )
                                    this.譜面.SSTFormatScore.小節メモリスト.Remove( 小節番号 );
                                else
                                    this.譜面.SSTFormatScore.小節メモリスト[ 小節番号 ] = 変更前;
                            }
                            else
                            {
                                if( !string.IsNullOrEmpty( 変更前 ) )
                                    this.譜面.SSTFormatScore.小節メモリスト.Add( 小節番号, 変更前 );
                            }
                            //-----------------
                            #endregion

                            this._小節の先頭へ移動する( 小節番号 );
                            this.splitContainer分割パネルコンテナ.Panel2.Refresh();  // 小節メモをリフレッシュ。
                        },
                        Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                            this._タブを選択する( タブ種別.基本情報 );
                            this.numericUpDownメモ用小節番号.Value = (decimal) 任意1;
                            this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                            this.textBoxメモ.Text = 変更後;
                            this.textBoxメモ.Focus();

                            int 小節番号 = (int) ( (decimal) 任意1 );

                            #region " dicメモの更新 "
                            //-----------------
                            if( this.譜面.SSTFormatScore.小節メモリスト.ContainsKey( 小節番号 ) )
                            {
                                if( string.IsNullOrEmpty( 変更後 ) )
                                    this.譜面.SSTFormatScore.小節メモリスト.Remove( 小節番号 );
                                else
                                    this.譜面.SSTFormatScore.小節メモリスト[ 小節番号 ] = 変更後;
                            }
                            else
                            {
                                if( !string.IsNullOrEmpty( 変更後 ) )
                                    this.譜面.SSTFormatScore.小節メモリスト.Add( 小節番号, 変更後 );
                            }
                            //-----------------
                            #endregion

                            this._小節の先頭へ移動する( 小節番号 );
                            this.splitContainer分割パネルコンテナ.Panel2.Refresh();  // 小節メモをリフレッシュ。
                        },
                        変更対象: null,
                        変更前の値: this.textBoxメモ_以前の値,
                        変更後の値: this.textBoxメモ.Text,
                        任意1: (object) this.numericUpDownメモ用小節番号.Value,
                        任意2: null );

                    this.UndoRedo管理.セルを追加する( cc );

                    // Undo ボタンを有効にする。
                    this.UndoRedo用GUIのEnabledを設定する();
                }
            }
            //-----------------
            #endregion

            this.textBoxメモ_以前の値 = this.textBoxメモ.Text;  // <以前の値> = <現在の値>

            if( false == UndoRedo.UndoRedo管理.UndoRedoした直後である )
                this.未保存である = true;

            UndoRedo.UndoRedo管理.UndoRedoした直後である = false;

            #region " 小節番号に対応するメモを dicメモ に登録する。"
            //-----------------
            {
                int 小節番号 = (int) this.numericUpDownメモ用小節番号.Value;

                if( string.IsNullOrEmpty( this.textBoxメモ.Text ) )
                {
                    // (A) 空文字列の場合
                    if( this.譜面.SSTFormatScore.小節メモリスト.ContainsKey( 小節番号 ) )
                        this.譜面.SSTFormatScore.小節メモリスト.Remove( 小節番号 );        // 存在してたら削除。
                                                                            // 存在してなかったら何もしない。
                }
                else
                {
                    // (B) その他の場合
                    if( this.譜面.SSTFormatScore.小節メモリスト.ContainsKey( 小節番号 ) )
                        this.譜面.SSTFormatScore.小節メモリスト[ 小節番号 ] = this.textBoxメモ.Text;     // 存在してたら更新。
                    else
                        this.譜面.SSTFormatScore.小節メモリスト.Add( 小節番号, this.textBoxメモ.Text );      // 存在してなかったら追加。
                }
            }
            //-----------------
            #endregion
            #region " もし最終小節だったなら、後ろに４つ小節を加える。"
            //-----------------
            {
                int 小節番号 = (int) this.numericUpDownメモ用小節番号.Value;
                if( 小節番号 == this.譜面.SSTFormatScore.最大小節番号を返す() )
                {
                    this.譜面.最後の小節の後ろに小節を４つ追加する();
                }
            }
            //-----------------
            #endregion
        }
        protected void textBoxメモ_Validated( object sender, EventArgs e )
        {
            // 最新 UndoRedoセル の所有権を放棄する。
            this.UndoRedo管理.Undoするセルを取得して返す_見るだけ()?.所有権を放棄する( this.textBoxメモ );

            // 小節メモをリフレッシュ。
            this.splitContainer分割パネルコンテナ.Panel2.Refresh();
        }
        private string textBoxメモ_以前の値 = "";

        protected void numericUpDownメモ用小節番号_ValueChanged( object sender, EventArgs e )
        {
            // 小節番号にあわせて、textBoxメモにメモを表示する。
            int 小節番号 = (int) this.numericUpDownメモ用小節番号.Value;
            this._次のプロパティ変更がUndoRedoリストに載らないようにする();
            if( this.譜面.SSTFormatScore.小節メモリスト.ContainsKey( 小節番号 ) )
                this.textBoxメモ.Text = this.譜面.SSTFormatScore.小節メモリスト[ 小節番号 ];
            else
                this.textBoxメモ.Text = "";
            this._次のプロパティ変更がUndoRedoリストに載るようにする();
        }

        protected void trackBarLevel_Scroll( object sender, EventArgs e )
        {
            // テキストボックスに数値を反映。（0～999 → 0.00～9.99 に変換）
            this.textBoxLevel.Text = ( this.trackBarLevel.Value / 100.0 ).ToString( "0.00" );

            // テキストボックスに Validation を起こさせる。
            this.textBoxLevel.Focus();
            this.trackBarLevel.Focus();
        }

        protected void textBoxLevel_Validating( object sender, System.ComponentModel.CancelEventArgs e )
        {
            // 入力値が 0.00 ～ 9.99 の小数であるか確認する。
            if( float.TryParse( this.textBoxLevel.Text, out float val ) )
            {
                // 値を丸める
                if( val < 0.0f )
                {
                    this.textBoxLevel.Text = "0.00";
                    val = 0.0f;
                }
                else if( val > 9.99f )
                {
                    this.textBoxLevel.Text = "9.99";
                    val = 9.99f;
                }

                #region " この変更が Undo/Redo したことによるものではない場合、UndoRedoセルを追加 or 修正する。"
                //-----------------
                if( false == UndoRedo.UndoRedo管理.UndoRedoした直後である )
                {
                    // 最新のセルの所有者が自分？
                    var cell = this.UndoRedo管理.Undoするセルを取得して返す_見るだけ();

                    if( ( null != cell ) && cell.所有権がある( this.textBoxLevel ) )
                    {
                        // (A) 所有者である → 最新のセルの "変更後の値" を現在のコントロールの値に更新する。
                        ( (UndoRedo.セル<string>) cell ).変更後の値 = this.textBoxLevel.Text;
                    }
                    else
                    {
                        // (B) 所有者ではない → 以下のようにセルを新規追加する。
                        //    "変更前の値" ← 以前の値
                        //    "変更後の値" ← 現在の値
                        //    "所有者ID" ← 対象となるコンポーネントオブジェクト
                        var cc = new UndoRedo.セル<string>(
                            所有者ID: this.textBoxLevel,
                            Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                                this._タブを選択する( タブ種別.基本情報 );
                                this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                                this.textBoxLevel.Text = 変更前;
                                this.textBoxLevel.Focus();
                                this.trackBarLevel.Value = ( string.IsNullOrEmpty( 変更前 ) ) ? 0 : (int) ( float.Parse( 変更前 ) * 100 );
                            },
                            Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                                this._タブを選択する( タブ種別.基本情報 );
                                this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                                this.textBoxLevel.Text = 変更後;
                                this.textBoxLevel.Focus();
                                this.trackBarLevel.Value = ( string.IsNullOrEmpty( 変更後 ) ) ? 0 : (int) ( float.Parse( 変更後 ) * 100 );
                            },
                            変更対象: null,
                            変更前の値: this.textBoxLevel_以前の値,
                            変更後の値: this.textBoxLevel.Text,
                            任意1: null,
                            任意2: null );

                        this.UndoRedo管理.セルを追加する( cc );

                        // Undo ボタンを有効にする。
                        this.UndoRedo用GUIのEnabledを設定する();
                    }
                }
                //-----------------
                #endregion

                this.textBoxLevel_以前の値 = this.textBoxLevel.Text;      // 以前の値 ← 現在の値
                UndoRedo.UndoRedo管理.UndoRedoした直後である = false;
                this.未保存である = true;

                // トラックバーに反映する。
                this.trackBarLevel.Value = (int) ( val * 100 );

                // スコアに反映する。
                譜面.SSTFormatScore.難易度 = val;
            }
            else
            {
                e.Cancel = true;
                this.textBoxLevel.Text = ( this.trackBarLevel.Value / 100 ).ToString( "0.00" );
                this.textBoxLevel.Select();
            }
        }
        protected void textBoxLevel_Validated( object sender, EventArgs e )
        {
            // 最新の UndoRedoセル の所有権を放棄する。
            this.UndoRedo管理.Undoするセルを取得して返す_見るだけ()?.所有権を放棄する( this.textBoxLevel );
        }
        private string textBoxLevel_以前の値 = "5.00";

        protected void textBoxBGV_TextChanged( object sender, EventArgs e )
        {
            #region " この変更が Undo/Redo したことによるものではない場合、UndoRedoセルを追加 or 修正する。"
            //-----------------
            if( false == UndoRedo.UndoRedo管理.UndoRedoした直後である )
            {
                // 最新のセルの所有者が自分？
                var cell = this.UndoRedo管理.Undoするセルを取得して返す_見るだけ();

                if( ( null != cell ) && cell.所有権がある( this.textBoxBGV ) )
                {
                    // (A) 所有者である → 最新のセルの "変更後の値" を現在のコントロールの値に更新する。
                    ( (UndoRedo.セル<string>) cell ).変更後の値 = this.textBoxBGV.Text;
                }
                else
                {
                    // (B) 所有者ではない → 以下のようにセルを新規追加する。
                    //    "変更前の値" ← 以前の値
                    //    "変更後の値" ← 現在の値
                    //    "所有者ID" ← 対象となるコンポーネントオブジェクト
                    var cc = new UndoRedo.セル<string>(
                        所有者ID: this.textBoxBGV,
                        Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                            this._タブを選択する( タブ種別.基本情報 );
                            this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                            this.textBoxBGV.Text = 変更前;
                            this.textBoxBGV.Focus();
                        },
                        Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                            this._タブを選択する( タブ種別.基本情報 );
                            this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                            this.textBoxBGV.Text = 変更後;
                            this.textBoxBGV.Focus();
                        },
                        変更対象: null,
                        変更前の値: this.textBoxBGV_以前の値,
                        変更後の値: this.textBoxBGV.Text,
                        任意1: null,
                        任意2: null );

                    this.UndoRedo管理.セルを追加する( cc );

                    // Undo ボタンを有効にする。
                    this.UndoRedo用GUIのEnabledを設定する();
                }
            }
            //-----------------
            #endregion

            this.textBoxBGV_以前の値 = this.textBoxBGV.Text;      // 以前の値 ← 現在の値
            UndoRedo.UndoRedo管理.UndoRedoした直後である = false;
            this.未保存である = true;

            // スコアには随時保存する。
            譜面.SSTFormatScore.BGVファイル名 = this.textBoxBGV.Text;
        }
        protected void textBoxBGV_Validated( object sender, EventArgs e )
        {
            // 最新の UndoRedoセル の所有権を放棄する。
            this.UndoRedo管理.Undoするセルを取得して返す_見るだけ()?.所有権を放棄する( this.textBoxBGV );
        }
        private string textBoxBGV_以前の値 = "";

        protected void buttonBGV参照_Click( object sender, EventArgs e )
        {
            #region " ファイルを開くダイアログでファイルを選択する。"
            //-----------------
            using var dialog = new OpenFileDialog() {
                Title = Properties.Resources.MSG_ファイル選択ダイアログのタイトル,
                Filter = Properties.Resources.MSG_背景動画ファイル選択ダイアログのフィルタ,
                FilterIndex = 1,
                InitialDirectory = this._作業フォルダパス,
            };
            var result = dialog.ShowDialog( this );

            // メインフォームを再描画してダイアログを完全に消す。
            this.Refresh();

            // OKじゃないならここで中断。
            if( DialogResult.OK != result )
                return;
            //-----------------
            #endregion

            this.textBoxBGV.Text = FDK.Folder.絶対パスを相対パスに変換する( this._作業フォルダパス, dialog.FileName );
        }

        protected void textBoxBGM_TextChanged( object sender, EventArgs e )
        {
            #region " この変更が Undo/Redo したことによるものではない場合、UndoRedoセルを追加 or 修正する。"
            //-----------------
            if( false == UndoRedo.UndoRedo管理.UndoRedoした直後である )
            {
                // 最新のセルの所有者が自分？
                var cell = this.UndoRedo管理.Undoするセルを取得して返す_見るだけ();

                if( ( null != cell ) && cell.所有権がある( this.textBoxBGM ) )
                {
                    // (A) 所有者である → 最新のセルの "変更後の値" を現在のコントロールの値に更新する。
                    ( (UndoRedo.セル<string>) cell ).変更後の値 = this.textBoxBGM.Text;
                }
                else
                {
                    // (B) 所有者ではない → 以下のようにセルを新規追加する。
                    //    "変更前の値" ← 以前の値
                    //    "変更後の値" ← 現在の値
                    //    "所有者ID" ← 対象となるコンポーネントオブジェクト
                    var cc = new UndoRedo.セル<string>(
                        所有者ID: this.textBoxBGM,
                        Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                            this._タブを選択する( タブ種別.基本情報 );
                            this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                            this.textBoxBGM.Text = 変更前;
                            this.textBoxBGM.Focus();
                        },
                        Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                            this._タブを選択する( タブ種別.基本情報 );
                            this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                            this.textBoxBGM.Text = 変更後;
                            this.textBoxBGM.Focus();
                        },
                        変更対象: null,
                        変更前の値: this.textBoxBGM_以前の値,
                        変更後の値: this.textBoxBGM.Text,
                        任意1: null,
                        任意2: null );

                    this.UndoRedo管理.セルを追加する( cc );

                    // Undo ボタンを有効にする。
                    this.UndoRedo用GUIのEnabledを設定する();
                }
            }
            //-----------------
            #endregion

            this.textBoxBGM_以前の値 = this.textBoxBGM.Text;      // 以前の値 ← 現在の値
            UndoRedo.UndoRedo管理.UndoRedoした直後である = false;
            this.未保存である = true;

            // スコアには随時保存する。
            譜面.SSTFormatScore.BGMファイル名 = this.textBoxBGM.Text;
        }
        private void textBoxBGM_Validated( object sender, EventArgs e )
        {
            // 最新の UndoRedoセル の所有権を放棄する。
            this.UndoRedo管理.Undoするセルを取得して返す_見るだけ()?.所有権を放棄する( this.textBoxBGM );
        }
        private string textBoxBGM_以前の値 = "";

        protected void buttonBGM参照_Click( object sender, EventArgs e )
        {
            #region " ファイルを開くダイアログでファイルを選択する。"
            //-----------------
            using var dialog = new OpenFileDialog() {
                Title = Properties.Resources.MSG_ファイル選択ダイアログのタイトル,
                Filter = Properties.Resources.MSG_背景動画ファイル選択ダイアログのフィルタ,
                FilterIndex = 1,
                InitialDirectory = this._作業フォルダパス,
            };
            var result = dialog.ShowDialog( this );

            // メインフォームを再描画してダイアログを完全に消す。
            this.Refresh();

            // OKじゃないならここで中断。
            if( DialogResult.OK != result )
                return;
            //-----------------
            #endregion

            this.textBoxBGM.Text = FDK.Folder.絶対パスを相対パスに変換する( this._作業フォルダパス, dialog.FileName );
        }

        protected void textBoxプレビュー音声_TextChanged( object sender, EventArgs e )
        {
            #region " この変更が Undo/Redo したことによるものではない場合、UndoRedoセルを追加 or 修正する。"
            //-----------------
            if( false == UndoRedo.UndoRedo管理.UndoRedoした直後である )
            {
                // 最新のセルの所有者が自分？
                var cell = this.UndoRedo管理.Undoするセルを取得して返す_見るだけ();

                if( ( null != cell ) && cell.所有権がある( this.textBoxプレビュー音声 ) )
                {
                    // (A) 所有者である → 最新のセルの "変更後の値" を現在のコントロールの値に更新する。
                    ( (UndoRedo.セル<string>) cell ).変更後の値 = this.textBoxプレビュー音声.Text;
                }
                else
                {
                    // (B) 所有者ではない → 以下のようにセルを新規追加する。
                    //    "変更前の値" ← 以前の値
                    //    "変更後の値" ← 現在の値
                    //    "所有者ID" ← 対象となるコンポーネントオブジェクト
                    var cc = new UndoRedo.セル<string>(
                        所有者ID: this.textBoxプレビュー音声,
                        Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                            this._タブを選択する( タブ種別.基本情報 );
                            this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                            this.textBoxプレビュー音声.Text = 変更前;
                            this.textBoxプレビュー音声.Focus();
                        },
                        Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                            this._タブを選択する( タブ種別.基本情報 );
                            this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                            this.textBoxプレビュー音声.Text = 変更後;
                            this.textBoxプレビュー音声.Focus();
                        },
                        変更対象: null,
                        変更前の値: this.textBoxプレビュー音声_以前の値,
                        変更後の値: this.textBoxプレビュー音声.Text,
                        任意1: null,
                        任意2: null );

                    this.UndoRedo管理.セルを追加する( cc );

                    // Undo ボタンを有効にする。
                    this.UndoRedo用GUIのEnabledを設定する();
                }
            }
            //-----------------
            #endregion

            this.textBoxプレビュー音声_以前の値 = this.textBoxプレビュー音声.Text;      // 以前の値 ← 現在の値
            UndoRedo.UndoRedo管理.UndoRedoした直後である = false;
            this.未保存である = true;

            // スコアには随時保存する。
            譜面.SSTFormatScore.プレビュー音声ファイル名 = this.textBoxプレビュー音声.Text;
        }
        protected void textBoxプレビュー音声_Validated( object sender, EventArgs e )
        {
            // 最新の UndoRedoセル の所有権を放棄する。
            this.UndoRedo管理.Undoするセルを取得して返す_見るだけ()?.所有権を放棄する( this.textBoxプレビュー音声 );
        }
        private string textBoxプレビュー音声_以前の値 = "";

        private void buttonプレビュー音声_Click( object sender, EventArgs e )
        {
            #region " ファイルを開くダイアログでファイルを選択する。"
            //-----------------
            using var dialog = new OpenFileDialog() {
                Title = Properties.Resources.MSG_ファイル選択ダイアログのタイトル,
                Filter = Properties.Resources.MSG_背景動画ファイル選択ダイアログのフィルタ,
                FilterIndex = 1,
                InitialDirectory = this._作業フォルダパス,
            };
            var result = dialog.ShowDialog( this );

            // メインフォームを再描画してダイアログを完全に消す。
            this.Refresh();

            // OKじゃないならここで中断。
            if( DialogResult.OK != result )
                return;
            //-----------------
            #endregion

            this.textBoxプレビュー音声.Text = FDK.Folder.絶対パスを相対パスに変換する( this._作業フォルダパス, dialog.FileName );
        }

        private void textBoxプレビュー画像_TextChanged( object sender, EventArgs e )
        {
            #region " この変更が Undo/Redo したことによるものではない場合、UndoRedoセルを追加 or 修正する。"
            //-----------------
            if( false == UndoRedo.UndoRedo管理.UndoRedoした直後である )
            {
                // 最新のセルの所有者が自分？
                var cell = this.UndoRedo管理.Undoするセルを取得して返す_見るだけ();

                if( ( null != cell ) && cell.所有権がある( this.textBoxプレビュー画像 ) )
                {
                    // (A) 所有者である → 最新のセルの "変更後の値" を現在のコントロールの値に更新する。
                    ( (UndoRedo.セル<string>) cell ).変更後の値 = this.textBoxプレビュー画像.Text;
                }
                else
                {
                    // (B) 所有者ではない → 以下のようにセルを新規追加する。
                    //    "変更前の値" ← 以前の値
                    //    "変更後の値" ← 現在の値
                    //    "所有者ID" ← 対象となるコンポーネントオブジェクト
                    var cc = new UndoRedo.セル<string>(
                        所有者ID: this.textBoxプレビュー画像,
                        Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                            this._タブを選択する( タブ種別.基本情報 );
                            this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                            this.textBoxプレビュー画像.Text = 変更前;
                            this.textBoxプレビュー画像.Focus();
                        },
                        Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                            this._タブを選択する( タブ種別.基本情報 );
                            this._次のプロパティ変更がUndoRedoリストに載らないようにする();
                            this.textBoxプレビュー画像.Text = 変更後;
                            this.textBoxプレビュー画像.Focus();
                        },
                        変更対象: null,
                        変更前の値: this.textBoxプレビュー画像_以前の値,
                        変更後の値: this.textBoxプレビュー画像.Text,
                        任意1: null,
                        任意2: null );

                    this.UndoRedo管理.セルを追加する( cc );

                    // Undo ボタンを有効にする。
                    this.UndoRedo用GUIのEnabledを設定する();
                }
            }
            //-----------------
            #endregion

            this.textBoxプレビュー画像_以前の値 = this.textBoxプレビュー画像.Text;      // 以前の値 ← 現在の値
            UndoRedo.UndoRedo管理.UndoRedoした直後である = false;
            this.未保存である = true;

            // スコアには随時保存する。
            譜面.SSTFormatScore.プレビュー画像ファイル名 = this.textBoxプレビュー画像.Text;
        }
        private void textBoxプレビュー画像_Validated( object sender, EventArgs e )
        {
            this._プレビュー画像を更新する();

            // 最新の UndoRedoセル の所有権を放棄する。
            this.UndoRedo管理.Undoするセルを取得して返す_見るだけ()?.所有権を放棄する( this.textBoxプレビュー画像 );
        }
        private string textBoxプレビュー画像_以前の値 = "";

        private void buttonプレビュー画像参照_Click( object sender, EventArgs e )
        {
            #region " ファイルを開くダイアログでファイルを選択する。"
            //-----------------
            using var dialog = new OpenFileDialog() {
                Title = Properties.Resources.MSG_ファイル選択ダイアログのタイトル,
                Filter = Properties.Resources.MSG_画像ファイル選択ダイアログのフィルタ,
                FilterIndex = 1,
                InitialDirectory = this._作業フォルダパス,
            };
            var result = dialog.ShowDialog( this );

            // メインフォームを再描画してダイアログを完全に消す。
            this.Refresh();

            // OKじゃないならここで中断。
            if( DialogResult.OK != result )
                return;
            //-----------------
            #endregion

            this.textBoxプレビュー画像.Text = FDK.Folder.絶対パスを相対パスに変換する( this._作業フォルダパス, dialog.FileName );

            this._プレビュー画像を更新する();
        }
        //-----------------
        #endregion
    }
}
