using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;
using FDK;
using SSTF=SSTFormat.v004;

namespace SSTFEditor
{
    class 譜面 : IDisposable
    {

        // プロパティ


        /// <summary>
        ///     譜面に表示されるスコア。
        /// </summary>
        public SSTF.スコア スコア { get; protected set; }


        public readonly Size チップサイズpx = new Size( 30, 8 );  // チップの幅は、レーン幅の基準にもなる。

        public int 譜面表示下辺の譜面内絶対位置grid { get; set; }

        public int カレントラインの譜面内絶対位置grid
        {
            get => ( this.譜面表示下辺の譜面内絶対位置grid + ( 230 * this.Form.GRID_PER_PIXEL ) );    // 譜面拡大率によらず、大体下辺から -230 pixel くらいで。
        }

        /// <summary>
        ///     現在の全小節の高さを計算してグリッド単位で返す。
        /// </summary>
        public int 全小節の高さgrid
        {
            get
            {
                int 高さgrid = 0;
                int 最大小節番号 = this.スコア.最大小節番号を返す();

                for( int i = 0; i <= 最大小節番号; i++ )
                    高さgrid += this.小節長をグリッドで返す( i );

                return 高さgrid;
            }
        }

        public int レーンの合計幅px
        {
            get => this.レーン属性.Count * this.チップサイズpx.Width;
        }

        public int 譜面表示下辺に位置する小節番号
        {
            get => this.譜面内絶対位置gridに位置する小節の情報を返す( this.譜面表示下辺の譜面内絶対位置grid ).小節番号;
        }

        public int カレントラインに位置する小節番号
        {
            get => this.譜面内絶対位置gridに位置する小節の情報を返す( this.カレントラインの譜面内絶対位置grid ).小節番号;
        }


        /// <summary>
        ///     チップの属するレーンを定義する。
        /// </summary>
        /// <remarks>
        ///     <see cref="編集モード"/> のコンストラクタでも参照されるので、登録ルールに注意すること。
        ///     >登録ルール → 同一レーンについて、最初によく使うチップを、２番目にトグルで２番目によく使うチップを登録する。
        /// </remarks>
        public readonly Dictionary<SSTF.チップ種別, 編集レーン種別> チップ種別to編集レーン = new Dictionary<SSTF.チップ種別, 編集レーン種別>() {
            #region " *** "
            //-----------------
            { SSTF.チップ種別.BPM,                編集レーン種別.BPM },
            { SSTF.チップ種別.LeftCrash,          編集レーン種別.左シンバル },
            { SSTF.チップ種別.HiHat_Close,        編集レーン種別.ハイハット },
            { SSTF.チップ種別.HiHat_Open,         編集レーン種別.ハイハット },
            { SSTF.チップ種別.HiHat_HalfOpen,     編集レーン種別.ハイハット },
            { SSTF.チップ種別.HiHat_Foot,         編集レーン種別.ハイハット },
            { SSTF.チップ種別.Snare,              編集レーン種別.スネア },
            { SSTF.チップ種別.Snare_Ghost,        編集レーン種別.スネア },
            { SSTF.チップ種別.Snare_ClosedRim,    編集レーン種別.スネア },
            { SSTF.チップ種別.Snare_OpenRim,      編集レーン種別.スネア },
            { SSTF.チップ種別.Tom1,               編集レーン種別.ハイタム },
            { SSTF.チップ種別.Tom1_Rim,           編集レーン種別.ハイタム },
            { SSTF.チップ種別.Bass,               編集レーン種別.バス },
            { SSTF.チップ種別.Tom2,               編集レーン種別.ロータム },
            { SSTF.チップ種別.Tom2_Rim,           編集レーン種別.ロータム },
            { SSTF.チップ種別.Tom3,               編集レーン種別.フロアタム },
            { SSTF.チップ種別.Tom3_Rim,           編集レーン種別.フロアタム },
            { SSTF.チップ種別.RightCrash,         編集レーン種別.右シンバル },
            { SSTF.チップ種別.Ride,               編集レーン種別.右シンバル },   // Config で変更可
			{ SSTF.チップ種別.Ride_Cup,           編集レーン種別.右シンバル },   // Config で変更可
			{ SSTF.チップ種別.China,              編集レーン種別.右シンバル },   // Config で変更可
			{ SSTF.チップ種別.Splash,             編集レーン種別.右シンバル },   // Config で変更可
			{ SSTF.チップ種別.LeftCymbal_Mute,    編集レーン種別.左シンバル },
            { SSTF.チップ種別.RightCymbal_Mute,   編集レーン種別.右シンバル },
            { SSTF.チップ種別.背景動画,           編集レーン種別.BGV },
            { SSTF.チップ種別.BGM,                編集レーン種別.BGM },
            { SSTF.チップ種別.小節線,             編集レーン種別.Unknown },
            { SSTF.チップ種別.拍線,               編集レーン種別.Unknown },
            { SSTF.チップ種別.小節の先頭,         編集レーン種別.Unknown },
            { SSTF.チップ種別.小節メモ,           編集レーン種別.Unknown },
            { SSTF.チップ種別.SE1,                編集レーン種別.Unknown },
            { SSTF.チップ種別.SE2,                編集レーン種別.Unknown },
            { SSTF.チップ種別.SE3,                編集レーン種別.Unknown },
            { SSTF.チップ種別.SE4,                編集レーン種別.Unknown },
            { SSTF.チップ種別.SE5,                編集レーン種別.Unknown },
            { SSTF.チップ種別.SE6,                編集レーン種別.Unknown },
            { SSTF.チップ種別.SE7,                編集レーン種別.Unknown },
            { SSTF.チップ種別.SE8,                編集レーン種別.Unknown },
            { SSTF.チップ種別.SE9,                編集レーン種別.Unknown },
            { SSTF.チップ種別.SE10,               編集レーン種別.Unknown },
            { SSTF.チップ種別.SE11,               編集レーン種別.Unknown },
            { SSTF.チップ種別.SE12,               編集レーン種別.Unknown },
            { SSTF.チップ種別.SE13,               編集レーン種別.Unknown },
            { SSTF.チップ種別.SE14,               編集レーン種別.Unknown },
            { SSTF.チップ種別.SE15,               編集レーン種別.Unknown },
            { SSTF.チップ種別.SE16,               編集レーン種別.Unknown },
            { SSTF.チップ種別.SE17,               編集レーン種別.Unknown },
            { SSTF.チップ種別.SE18,               編集レーン種別.Unknown },
            { SSTF.チップ種別.SE19,               編集レーン種別.Unknown },
            { SSTF.チップ種別.SE20,               編集レーン種別.Unknown },
            { SSTF.チップ種別.SE21,               編集レーン種別.Unknown },
            { SSTF.チップ種別.SE22,               編集レーン種別.Unknown },
            { SSTF.チップ種別.SE23,               編集レーン種別.Unknown },
            { SSTF.チップ種別.SE24,               編集レーン種別.Unknown },
            { SSTF.チップ種別.SE25,               編集レーン種別.Unknown },
            { SSTF.チップ種別.SE26,               編集レーン種別.Unknown },
            { SSTF.チップ種別.SE27,               編集レーン種別.Unknown },
            { SSTF.チップ種別.SE28,               編集レーン種別.Unknown },
            { SSTF.チップ種別.SE29,               編集レーン種別.Unknown },
            { SSTF.チップ種別.SE30,               編集レーン種別.Unknown },
            { SSTF.チップ種別.SE31,               編集レーン種別.Unknown },
            { SSTF.チップ種別.SE32,               編集レーン種別.Unknown },
            { SSTF.チップ種別.GuitarAuto,         編集レーン種別.Unknown },
            { SSTF.チップ種別.BassAuto,           編集レーン種別.Unknown },
            { SSTF.チップ種別.Unknown,            編集レーン種別.Unknown },
            //-----------------
            #endregion
        };

        /// <summary>
        ///     レーンの表示情報。
        /// </summary>
        /// <remarks>
        ///     <see cref="編集レーン種別.Unknown"/> の指定は不可。
        /// </remarks>
        public readonly Dictionary<編集レーン種別, (int 位置, string 名前, Color 背景色, bool 区分線が太線)> レーン属性 = new Dictionary<編集レーン種別, (int 位置, string 名前, Color 背景色, bool 区分線が太線)>() {
            { 編集レーン種別.BPM,         (位置:  0, 名前: "BPM", 背景色: Color.FromArgb( レーン背景色透明度, Color.SkyBlue ),   区分線が太線: true ) },
            { 編集レーン種別.左シンバル,  (位置:  1, 名前: "LC",  背景色: Color.FromArgb( レーン背景色透明度, Color.WhiteSmoke), 区分線が太線: false) },
            { 編集レーン種別.ハイハット,  (位置:  2, 名前: "HH",  背景色: Color.FromArgb( レーン背景色透明度, Color.SkyBlue),    区分線が太線: false) },
            { 編集レーン種別.スネア,      (位置:  3, 名前: "SD",  背景色: Color.FromArgb( レーン背景色透明度, Color.Orange),     区分線が太線: false) },
            { 編集レーン種別.ハイタム,    (位置:  4, 名前: "HT",  背景色: Color.FromArgb( レーン背景色透明度, Color.Lime),       区分線が太線: false) },
            { 編集レーン種別.バス,        (位置:  5, 名前: "BD",  背景色: Color.FromArgb( レーン背景色透明度, Color.Gainsboro),  区分線が太線: false) },
            { 編集レーン種別.ロータム,    (位置:  6, 名前: "LT",  背景色: Color.FromArgb( レーン背景色透明度, Color.Red),        区分線が太線: false) },
            { 編集レーン種別.フロアタム,  (位置:  7, 名前: "FT",  背景色: Color.FromArgb( レーン背景色透明度, Color.Magenta),    区分線が太線: false) },
            { 編集レーン種別.右シンバル,  (位置:  8, 名前: "RC",  背景色: Color.FromArgb( レーン背景色透明度, Color.WhiteSmoke), 区分線が太線: true ) },
            { 編集レーン種別.BGV,         (位置:  9, 名前: "BGV", 背景色: Color.FromArgb( レーン背景色透明度, Color.SkyBlue),    区分線が太線: false) },
            { 編集レーン種別.BGM,         (位置: 10, 名前: "BGM", 背景色: Color.FromArgb( レーン背景色透明度, Color.SkyBlue),    区分線が太線: true ) },
        };

        /// <summary>
        ///     チップの色。
        /// </summary>
        protected readonly Dictionary<SSTF.チップ種別, Color> チップto色 = new Dictionary<SSTF.チップ種別, Color>() {
            #region " *** "
            //-----------------
            { SSTF.チップ種別.BPM,                Color.FromArgb( チップ背景色透明度, Color.SkyBlue ) },
            { SSTF.チップ種別.LeftCrash,          Color.FromArgb( チップ背景色透明度, Color.WhiteSmoke ) },
            { SSTF.チップ種別.LeftCymbal_Mute,    Color.FromArgb( チップ背景色透明度, Color.Gray ) },
            { SSTF.チップ種別.HiHat_Close,        Color.FromArgb( チップ背景色透明度, Color.SkyBlue ) },
            { SSTF.チップ種別.HiHat_Foot,         Color.FromArgb( チップ背景色透明度, Color.SkyBlue ) },
            { SSTF.チップ種別.HiHat_HalfOpen,     Color.FromArgb( チップ背景色透明度, Color.SkyBlue ) },
            { SSTF.チップ種別.HiHat_Open,         Color.FromArgb( チップ背景色透明度, Color.SkyBlue ) },
            { SSTF.チップ種別.Snare,              Color.FromArgb( チップ背景色透明度, Color.Orange ) },
            { SSTF.チップ種別.Snare_ClosedRim,    Color.FromArgb( チップ背景色透明度, Color.OrangeRed ) },
            { SSTF.チップ種別.Snare_Ghost,        Color.FromArgb( チップ背景色透明度, Color.DeepPink ) },
            { SSTF.チップ種別.Snare_OpenRim,      Color.FromArgb( チップ背景色透明度, Color.Orange ) },
            { SSTF.チップ種別.Tom1,               Color.FromArgb( チップ背景色透明度, Color.Lime ) },
            { SSTF.チップ種別.Tom1_Rim,           Color.FromArgb( チップ背景色透明度, Color.Lime ) },
            { SSTF.チップ種別.Bass,               Color.FromArgb( チップ背景色透明度, Color.Gainsboro ) },
            { SSTF.チップ種別.Tom2,               Color.FromArgb( チップ背景色透明度, Color.Red ) },
            { SSTF.チップ種別.Tom2_Rim,           Color.FromArgb( チップ背景色透明度, Color.Red ) },
            { SSTF.チップ種別.Tom3,               Color.FromArgb( チップ背景色透明度, Color.Magenta ) },
            { SSTF.チップ種別.Tom3_Rim,           Color.FromArgb( チップ背景色透明度, Color.Magenta ) },
            { SSTF.チップ種別.RightCrash,         Color.FromArgb( チップ背景色透明度, Color.WhiteSmoke ) },
            { SSTF.チップ種別.RightCymbal_Mute,   Color.FromArgb( チップ背景色透明度, Color.Gray ) },
            { SSTF.チップ種別.Ride,               Color.FromArgb( チップ背景色透明度, Color.WhiteSmoke ) },
            { SSTF.チップ種別.Ride_Cup,           Color.FromArgb( チップ背景色透明度, Color.WhiteSmoke ) },
            { SSTF.チップ種別.China,              Color.FromArgb( チップ背景色透明度, Color.Tan ) },
            { SSTF.チップ種別.Splash,             Color.FromArgb( チップ背景色透明度, Color.LightGray ) },
            { SSTF.チップ種別.背景動画,           Color.FromArgb( チップ背景色透明度, Color.SkyBlue ) },
            { SSTF.チップ種別.BGM,                Color.FromArgb( チップ背景色透明度, Color.SkyBlue ) },
            //-----------------
            #endregion
        };



        // 生成と終了


        public 譜面( メインフォーム form )
        {
            this.Form = form;
            this.スコア = new SSTF.スコア();
            this.譜面表示下辺の譜面内絶対位置grid = 0;

            // 最初は10小節ほど用意しておく → 10小節目の先頭に Unknown チップを置くことで実現
            this.スコア.チップリスト.Add(
                new 描画用チップ() {
                    チップ種別 = SSTF.チップ種別.Unknown,
                    小節番号 = 9,   // 0から数えて10番目の小節 = 009
                    小節解像度 = 1,
                    小節内位置 = 0,
                    譜面内絶対位置grid = 9 * this.Form.GRID_PER_PART,      // 小節009の先頭位置
                } );
        }

        public void Dispose()
        {
            this.スコア = null;

            this.譜面パネル背景?.Dispose();
            this.譜面パネル背景 = null;

            this.小節番号文字フォント?.Dispose();
            this.小節番号文字フォント = null;

            this.小節番号文字ブラシ?.Dispose();
            this.小節番号文字ブラシ = null;

            this.小節番号文字フォーマット?.Dispose();
            this.小節番号文字フォーマット = null;

            this.ガイド線ペン?.Dispose();
            this.ガイド線ペン = null;

            this.小節線ペン?.Dispose();
            this.小節線ペン = null;

            this.拍線ペン?.Dispose();
            this.拍線ペン = null;

            this.レーン区分線ペン?.Dispose();
            this.レーン区分線ペン = null;

            this.レーン区分線太ペン?.Dispose();
            this.レーン区分線太ペン = null;

            this.カレントラインペン?.Dispose();
            this.カレントラインペン = null;

            this.レーン名文字フォント?.Dispose();
            this.レーン名文字フォント = null;

            this.レーン名文字ブラシ?.Dispose();
            this.レーン名文字ブラシ = null;

            this.レーン名文字影ブラシ?.Dispose();
            this.レーン名文字影ブラシ = null;

            this.レーン名文字フォーマット?.Dispose();
            this.レーン名文字フォーマット = null;

            this.チップの太枠ペン?.Dispose();
            this.チップの太枠ペン = null;

            this.チップ内文字列フォーマット?.Dispose();
            this.チップ内文字列フォーマット = null;

            this.チップ内文字列フォント?.Dispose();
            this.チップ内文字列フォント = null;

            this.白丸白バツペン?.Dispose();
            this.白丸白バツペン = null;

            this.Form = null;
        }

        public void コンフィグを譜面に反映する( Config config )
        {
            // RideLeft, ChinaLeft, SplashLeft に従ってレーンの位置を決定する。
            this.チップ種別to編集レーン[ SSTF.チップ種別.Ride ] = ( config.RideLeft ) ? 編集レーン種別.左シンバル : 編集レーン種別.右シンバル;
            this.チップ種別to編集レーン[ SSTF.チップ種別.Ride_Cup ] = ( config.RideLeft ) ? 編集レーン種別.左シンバル : 編集レーン種別.右シンバル;
            this.チップ種別to編集レーン[ SSTF.チップ種別.China ] = ( config.ChinaLeft ) ? 編集レーン種別.左シンバル : 編集レーン種別.右シンバル;
            this.チップ種別to編集レーン[ SSTF.チップ種別.Splash ] = ( config.SplashLeft ) ? 編集レーン種別.左シンバル : 編集レーン種別.右シンバル;
        }



        // ファイル入出力


        public void 曲データファイルを読み込む( string ファイル名 )
        {
            // 解放
            this.スコア = null;

            // 読み込み
            this.スコア = SSTF.スコア.ファイルから生成する( ファイル名 );

            // 後処理

            #region " 小節線・拍線・Unknown チップをすべて削除する。"
            //-----------------
            this.スコア.チップリスト.RemoveAll( ( chip ) => (
                chip.チップ種別 == SSTF.チップ種別.小節線 || 
                chip.チップ種別 == SSTF.チップ種別.拍線 || 
                chip.チップ種別 == SSTF.チップ種別.Unknown ) );
            //-----------------
            #endregion

            #region " チップリストのすべてのチップを、描画用チップに変換する。"
            //----------------
            {
                // バックアップを取って、
                var 元のチップリスト = new SSTF.チップ[ this.スコア.チップリスト.Count ];
                for( int i = 0; i < this.スコア.チップリスト.Count; i++ )
                    元のチップリスト[ i ] = this.スコア.チップリスト[ i ];

                // クリアして、
                this.スコア.チップリスト.Clear();

                // 再構築。
                for( int i = 0; i < 元のチップリスト.Length; i++ )
                    this.スコア.チップリスト.Add( new 描画用チップ( 元のチップリスト[ i ] ) );
            }
            //----------------
            #endregion

            #region " 全チップに対して「譜面内絶対位置grid」を設定する。"
            //-----------------
            {
                int チップが存在する小節の先頭grid = 0;
                int 現在の小節番号 = 0;

                foreach( 描画用チップ chip in this.スコア.チップリスト )
                {
                    // チップの小節番号が現在の小節番号よりも大きい場合、チップが存在する小節に至るまで、「チップが存在する小節の先頭grid」を更新する。
                    while( 現在の小節番号 < chip.小節番号 )
                    {
                        double 現在の小節の小節長倍率 = this.スコア.小節長倍率を取得する( 現在の小節番号 );
                        チップが存在する小節の先頭grid += (int) ( this.Form.GRID_PER_PART * 現在の小節の小節長倍率 );

                        現在の小節番号++;      // 現在の小節番号 が chip.小節番号 に追いつくまでループする。
                    }

                    chip.譜面内絶対位置grid =
                        チップが存在する小節の先頭grid + 
                        ( chip.小節内位置 * this.小節長をグリッドで返す( chip.小節番号 ) ) / chip.小節解像度;
                }
            }
            //-----------------
            #endregion
        }

        public void SSTFファイルを書き出す( string ファイル名, string ヘッダ行 )
        {
            using( var fs = new FileStream( ファイル名, FileMode.Create, FileAccess.Write ) )
                SSTF.スコア.SSTF.出力する( this.スコア, fs, $"{ヘッダ行}{Environment.NewLine}" );
        }

        public void SSTFoverDTXファイルを書き出す( string ファイル名, string ヘッダ行 )
        {
            using( var fs = new FileStream( ファイル名, FileMode.Create, FileAccess.Write ) )
                SSTF.スコア.SSTFoverDTX.出力する( this.スコア, fs, $"{ヘッダ行}{Environment.NewLine}" );
        }



        // 描画


        /// <summary>
        ///     コントロールに譜面を描画する。
        /// </summary>
        /// <param name="g">描画に使用するグラフィックス</param>
        /// <param name="panel">描画先のコントロール</param>
        public void 描画する( Graphics g, Control panel )
        {
            #region " レーンの背景画像が未作成なら作成する。"
            //-----------------
            if( null == this.譜面パネル背景 )
            {
                this.譜面パネル背景 = new Bitmap( this.レーンの合計幅px, 1 );
                using var graphics = Graphics.FromImage( this.譜面パネル背景 );

                foreach( var laneProp in this.レーン属性.Values )
                {
                    using var brush = new SolidBrush( laneProp.背景色 );
                    graphics.FillRectangle(
                        brush,
                        laneProp.位置 * this.チップサイズpx.Width, 0,
                        this.チップサイズpx.Width, 1 );
                }

                panel.Width = this.レーンの合計幅px;
                panel.BackgroundImage = this.譜面パネル背景;
                panel.BackgroundImageLayout = ImageLayout.Tile;
            }
            //-----------------
            #endregion

            int 小節先頭の譜面内絶対位置grid = 0;
            int パネル下辺の譜面内絶対位置grid = this.譜面表示下辺の譜面内絶対位置grid;
            int パネル上辺の譜面内絶対位置grid = パネル下辺の譜面内絶対位置grid + ( panel.ClientSize.Height * this.Form.GRID_PER_PIXEL );

            #region " 小節番号・ガイド線・拍線・レーン区分線・小節線を描画。"
            //-----------------
            {
                int 最大小節番号 = this.スコア.最大小節番号を返す();

                // すべての小節について……
                for( int 小節番号 = 0; 小節番号 <= 最大小節番号; 小節番号++ )
                {
                    int 小節長grid = this.小節長をグリッドで返す( 小節番号 );
                    int 次の小節の先頭位置grid = 小節先頭の譜面内絶対位置grid + 小節長grid;

                    #region " (A) 小節の描画領域すべてが、パネルの領域外（下）にある場合。→ この小節は描画しない。次の小節へ。"
                    //-----------------
                    if( 次の小節の先頭位置grid < パネル下辺の譜面内絶対位置grid )
                    {
                        小節先頭の譜面内絶対位置grid = 次の小節の先頭位置grid;
                        continue;
                    }
                    //-----------------
                    #endregion
                    #region " (B) 小節の描画領域すべてが、パネルの領域外（上）にある場合。→ これ以降の小節は描画しない。ここでループを抜ける。"
                    //-----------------
                    else if( 小節先頭の譜面内絶対位置grid >= パネル上辺の譜面内絶対位置grid )
                    {
                        break;
                    }
                    //-----------------
                    #endregion

                    Rectangle 小節の描画領域px;

                    #region " 小節の描画領域作成(A) 小節の描画領域が、パネル内にすべて収まっている場合。"
                    //-----------------
                    if( ( 小節先頭の譜面内絶対位置grid >= パネル下辺の譜面内絶対位置grid ) && ( 次の小節の先頭位置grid < パネル上辺の譜面内絶対位置grid ) )
                    {
                        小節の描画領域px = new Rectangle() {
                            X = 0,
                            Y = ( パネル上辺の譜面内絶対位置grid - 次の小節の先頭位置grid ) / this.Form.GRID_PER_PIXEL,
                            Width = panel.ClientSize.Width,
                            Height = ( 次の小節の先頭位置grid - 小節先頭の譜面内絶対位置grid ) / this.Form.GRID_PER_PIXEL,
                        };
                    }
                    //-----------------
                    #endregion
                    #region " 小節の描画領域作成(B) 小節の描画領域が、パネルをすべて包み込んでいる場合。"
                    //-----------------
                    else if( ( 小節先頭の譜面内絶対位置grid < パネル下辺の譜面内絶対位置grid ) && ( 次の小節の先頭位置grid >= パネル上辺の譜面内絶対位置grid ) )
                    {
                        小節の描画領域px = new Rectangle() {
                            X = 0,
                            Y = ( パネル上辺の譜面内絶対位置grid - 次の小節の先頭位置grid ) / this.Form.GRID_PER_PIXEL,
                            Width = panel.ClientSize.Width,
                            Height = ( 次の小節の先頭位置grid - 小節先頭の譜面内絶対位置grid ) / this.Form.GRID_PER_PIXEL,
                        };
                    }
                    //-----------------
                    #endregion
                    #region " 小節の描画領域作成(C) 小節の描画領域が、パネルの下側にはみだしている場合。"
                    //-----------------
                    else if( 小節先頭の譜面内絶対位置grid < パネル下辺の譜面内絶対位置grid )
                    {
                        小節の描画領域px = new Rectangle() {
                            X = 0,
                            Y = ( パネル上辺の譜面内絶対位置grid - 次の小節の先頭位置grid ) / this.Form.GRID_PER_PIXEL,
                            Width = panel.ClientSize.Width,
                            Height = ( 次の小節の先頭位置grid - 小節先頭の譜面内絶対位置grid ) / this.Form.GRID_PER_PIXEL,
                        };
                    }
                    //-----------------
                    #endregion
                    #region " 小節の描画領域作成(D) 小節の描画領域が、パネルの上側にはみだしている場合。"
                    //-----------------
                    else
                    {
                        小節の描画領域px = new Rectangle() {
                            X = 0,
                            Y = ( パネル上辺の譜面内絶対位置grid - 次の小節の先頭位置grid ) / this.Form.GRID_PER_PIXEL,
                            Width = panel.ClientSize.Width,
                            Height = ( 次の小節の先頭位置grid - 小節先頭の譜面内絶対位置grid ) / this.Form.GRID_PER_PIXEL,
                        };
                    }
                    //-----------------
                    #endregion

                    // 描画。

                    #region " 小節番号を描画。"
                    //-----------------
                    g.DrawString(
                        小節番号.ToString( "000" ),
                        this.小節番号文字フォント,
                        this.小節番号文字ブラシ,
                        小節の描画領域px,
                        this.小節番号文字フォーマット );
                    //-----------------
                    #endregion
                    #region " ガイド線を描画。"
                    //-----------------
                    this.譜面に定間隔で横線を描画する( g, 小節の描画領域px, this.ガイド間隔grid, this.ガイド線ペン );
                    //-----------------
                    #endregion
                    #region " 拍線を描画。"
                    //-----------------
                    this.譜面に定間隔で横線を描画する( g, 小節の描画領域px, this.Form.GRID_PER_PART / 4, this.拍線ペン );
                    //-----------------
                    #endregion
                    #region " レーン区分線（縦線）を描画。"
                    //-----------------
                    foreach( var laneProp in this.レーン属性.Values )
                    {
                        int x = Math.Min( ( ( laneProp.位置 + 1 ) * this.チップサイズpx.Width ), ( 小節の描画領域px.Width - 1 ) );

                        g.DrawLine(
                            ( laneProp.区分線が太線 ) ? this.レーン区分線太ペン : this.レーン区分線ペン,
                            x, 小節の描画領域px.Top,
                            x, 小節の描画領域px.Bottom );
                    }
                    //-----------------
                    #endregion
                    #region " 小節線を描画。"
                    //-----------------
                    this.譜面に定間隔で横線を描画する( g, 小節の描画領域px, 小節長grid, this.小節線ペン );
                    //-----------------
                    #endregion

                    小節先頭の譜面内絶対位置grid = 次の小節の先頭位置grid;
                }
            }
            //-----------------
            #endregion

            #region " チップを描画。"
            //-----------------
            {
                // すべてのチップについて……
                foreach( 描画用チップ chip in this.スコア.チップリスト )
                {
                    #region " クリッピングと終了判定。"
                    //-----------------
                    if( chip.チップ種別 == SSTF.チップ種別.Unknown )
                        continue;   // 描画対象外

                    if( 0 != chip.枠外レーン数 )
                        continue;   // 描画範囲外

                    if( chip.譜面内絶対位置grid < パネル下辺の譜面内絶対位置grid )
                        continue;   // 描画範囲外（次のチップへ）

                    if( chip.譜面内絶対位置grid >= パネル上辺の譜面内絶対位置grid )
                        break;      // 描画範囲外（ここで終了）
                    //-----------------
                    #endregion

                    var チップのレーン種別 = this.チップ種別to編集レーン[ chip.チップ種別 ];
                    int レーン位置 = this.レーン属性[ チップのレーン種別 ].位置;

                    var チップ描画領域 = new Rectangle(
                        x: レーン位置 * this.チップサイズpx.Width,
                        y: panel.ClientSize.Height - ( chip.譜面内絶対位置grid - this.譜面表示下辺の譜面内絶対位置grid ) / this.Form.GRID_PER_PIXEL - this.チップサイズpx.Height,
                        width: this.チップサイズpx.Width,
                        height: this.チップサイズpx.Height );

                    this.チップを指定領域へ描画する( g, chip, チップ描画領域 );

                    // 選択中なら太枠を付与。
                    if( chip.ドラッグ操作により選択中である || chip.選択が確定している )
                        this.チップの太枠を指定領域へ描画する( g, チップ描画領域 );
                }
            }
            //-----------------
            #endregion

            #region " レーン名を描画。"
            //-----------------
            var レーン名描画領域上側 = new Rectangle( 0, 0, panel.Width, 10 );
            var レーン名描画領域下側 = new Rectangle( 0, 10, panel.Width, 譜面.レーン名表示高さpx );

           
            // 背景のグラデーションを描画。
            
            using( var brush = new LinearGradientBrush( レーン名描画領域下側, Color.FromArgb( 255, 50, 155, 50 ), Color.FromArgb( 0, 0, 255, 0 ), LinearGradientMode.Vertical ) )
                g.FillRectangle( brush, レーン名描画領域下側 );
            using( var brush = new LinearGradientBrush( レーン名描画領域上側, Color.FromArgb( 255, 0, 100, 0 ), Color.FromArgb( 255, 50, 155, 50 ), LinearGradientMode.Vertical ) )
                g.FillRectangle( brush, レーン名描画領域上側 );


            // 各レーン名を描画。

            foreach( var laneProp in this.レーン属性.Values )
            {
                var レーン名描画領域 = new Rectangle(
                    x: レーン名描画領域下側.X + ( laneProp.位置 * this.チップサイズpx.Width ) + 2,
                    y: レーン名描画領域下側.Y + 2,
                    width: this.チップサイズpx.Width,
                    height: 24 );

                g.DrawString(
                    laneProp.名前,
                    this.レーン名文字フォント,
                    this.レーン名文字影ブラシ,    // 文字の影
                    レーン名描画領域,
                    this.レーン名文字フォーマット );

                レーン名描画領域.X -= 2;
                レーン名描画領域.Y -= 2;

                g.DrawString(
                    laneProp.名前,
                    this.レーン名文字フォント,
                    this.レーン名文字ブラシ,     // 文字本体
                    レーン名描画領域,
                    this.レーン名文字フォーマット );
            }
            //-----------------
            #endregion

            #region " カレントラインを描画。"
            //-----------------
            float y = panel.Size.Height - ( (float) ( this.カレントラインの譜面内絶対位置grid - this.譜面表示下辺の譜面内絶対位置grid ) / (float) this.Form.GRID_PER_PIXEL );

            g.DrawLine(
                this.カレントラインペン,
                0.0f, y,
                (float)( panel.Size.Width - 1 ), y );
            //-----------------
            #endregion
        }

        public void チップを指定領域へ描画する( Graphics g, 描画用チップ chip, Rectangle チップ描画領域 )
        {
            this.チップを指定領域へ描画する( g, chip.チップ種別, chip.音量, チップ描画領域, chip.チップ内文字列 );
        }

        public void チップを指定領域へ描画する( Graphics g, SSTF.チップ種別 チップ種別, int 音量, Rectangle チップ描画領域, string チップ内文字列 )
        {
            switch( チップ種別 )
            {
                case SSTF.チップ種別.BPM:
                case SSTF.チップ種別.LeftCrash:
                case SSTF.チップ種別.HiHat_Close:
                case SSTF.チップ種別.Snare:
                case SSTF.チップ種別.Tom1:
                case SSTF.チップ種別.Bass:
                case SSTF.チップ種別.Tom2:
                case SSTF.チップ種別.Tom3:
                case SSTF.チップ種別.RightCrash:
                case SSTF.チップ種別.China:
                case SSTF.チップ種別.Splash:
                case SSTF.チップ種別.背景動画:
                case SSTF.チップ種別.BGM:
                    this.チップを描画する_通常( g, チップ種別, 音量, チップ描画領域, チップ内文字列 );
                    break;

                case SSTF.チップ種別.Snare_Ghost:
                    this.チップを描画する_小丸( g, チップ種別, 音量, チップ描画領域, チップ内文字列 );
                    break;

                case SSTF.チップ種別.Ride:
                    this.チップを描画する_幅狭( g, チップ種別, 音量, チップ描画領域, チップ内文字列 );
                    break;

                case SSTF.チップ種別.Snare_OpenRim:
                case SSTF.チップ種別.HiHat_Open:
                    this.チップを描画する_幅狭白丸( g, チップ種別, 音量, チップ描画領域, チップ内文字列 );
                    break;

                case SSTF.チップ種別.HiHat_HalfOpen:
                case SSTF.チップ種別.Ride_Cup:
                    this.チップを描画する_幅狭白狭丸( g, チップ種別, 音量, チップ描画領域, チップ内文字列 );
                    break;

                case SSTF.チップ種別.HiHat_Foot:
                case SSTF.チップ種別.Snare_ClosedRim:
                case SSTF.チップ種別.Tom1_Rim:
                case SSTF.チップ種別.Tom2_Rim:
                case SSTF.チップ種別.Tom3_Rim:
                case SSTF.チップ種別.LeftCymbal_Mute:
                case SSTF.チップ種別.RightCymbal_Mute:
                    this.チップを描画する_幅狭白バツ( g, チップ種別, 音量, チップ描画領域, チップ内文字列 );
                    break;
            }
        }

        public void チップの太枠を指定領域へ描画する( Graphics g, Rectangle チップ描画領域 )
        {
            g.DrawRectangle( this.チップの太枠ペン, チップ描画領域 );
        }



        // 譜面操作


        public void 現在のガイド間隔を変更する( int n分 )
        {
            this.ガイド間隔grid = ( n分 == 0 ) ? 1 : ( this.Form.GRID_PER_PART / n分 );
        }

        public void チップを配置または置換する( 編集レーン種別 e編集レーン, SSTF.チップ種別 eチップ, int 譜面内絶対位置grid, string チップ文字列, int 音量, double BPM, bool 選択確定中 )
        {
            this.Form.UndoRedo管理.トランザクション記録を開始する();
            try
            {
                // 配置先に既にチップがあれば削除する（チップがなければ何もしない）。
                this.チップを削除する( e編集レーン, 譜面内絶対位置grid );

                // 新しいチップを作成し配置する。
                var 小節情報 = this.譜面内絶対位置gridに位置する小節の情報を返す( 譜面内絶対位置grid );
                int 小節の長さgrid = this.小節長をグリッドで返す( 小節情報.小節番号 );

                var chip = new 描画用チップ() {
                    選択が確定している = 選択確定中,
                    BPM = BPM,
                    発声時刻sec = 0,     // SSTFEditorでは使わない
                    チップ種別 = eチップ,
                    音量 = 音量,
                    小節解像度 = 小節の長さgrid,
                    小節内位置 = 譜面内絶対位置grid - 小節情報.小節の先頭位置grid,
                    小節番号 = 小節情報.小節番号,
                    譜面内絶対位置grid = 譜面内絶対位置grid,
                    チップ内文字列 = チップ文字列,
                };

                // チップを譜面に追加。
                var 変更前チップ = new 描画用チップ( chip );
                var cell = new UndoRedo.セル<描画用チップ>(
                    所有者ID: null,
                    Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                        this.スコア.チップリスト.Remove( 変更対象 );
                        this.Form.未保存である = true;
                    },
                    Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                        変更対象.CopyFrom( 変更前 );
                        this.スコア.チップリスト.Add( 変更対象 );
                        this.スコア.チップリスト.Sort();
                        this.Form.未保存である = true;
                    },
                    変更対象: chip,
                    変更前の値: 変更前チップ,
                    変更後の値: null );

                this.Form.UndoRedo管理.セルを追加する( cell );
                cell.Redoを実行する();

                // 配置した小節が現状最後の小節だったら、後ろに小節を４つ追加する。
                if( chip.小節番号 == this.スコア.最大小節番号を返す() )
                    this.最後の小節の後ろに小節を４つ追加する();
            }
            finally
            {
                this.Form.UndoRedo管理.トランザクション記録を終了する();
                this.Form.UndoRedo用GUIのEnabledを設定する();
                this.Form.未保存である = true;
            }
        }

        public void チップを削除する( 編集レーン種別 e編集レーン, int 譜面内絶対位置grid )
        {
            var 削除チップ = (描画用チップ)
                ( from chip in this.スコア.チップリスト
                  where ( ( this.チップ種別to編集レーン[ chip.チップ種別 ] == e編集レーン ) && ( ( (描画用チップ)chip ).譜面内絶対位置grid == 譜面内絶対位置grid ) )
                  select chip ).FirstOrDefault();   // チップが重なってたとしても、削除するのはひとつだけ。

            if( null != 削除チップ )
            {
                // UndoRedo セルを登録。
                var 変更前チップ = new 描画用チップ( 削除チップ );
                var cell = new UndoRedo.セル<描画用チップ>(
                    所有者ID: null,
                    Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                        変更対象.CopyFrom( 変更前 );
                        this.スコア.チップリスト.Add( 変更対象 );
                        this.スコア.チップリスト.Sort();
                        this.Form.未保存である = true;
                    },
                    Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                        this.スコア.チップリスト.Remove( 変更対象 );
                        this.Form.未保存である = true;
                    },
                    変更対象: 削除チップ,
                    変更前の値: 変更前チップ,
                    変更後の値: null );

                this.Form.UndoRedo管理.セルを追加する( cell );

                // 削除する。
                cell.Redoを実行する();

                // 削除完了。
                this.Form.UndoRedo用GUIのEnabledを設定する();
            }
        }

        public void 最後の小節の後ろに小節を４つ追加する()
        {
            int 最大小節番号 = this.スコア.最大小節番号を返す();

            // 最終小節の小節先頭位置grid と 小節長倍率 を取得する。
            int 小節先頭位置grid = this.小節先頭の譜面内絶対位置gridを返す( 最大小節番号 );
            int 小節の長さgrid = this.小節長をグリッドで返す( 最大小節番号 );
            double 最終小節の小節長倍率 = this.スコア.小節長倍率を取得する( 最大小節番号 );

            // ダミーで置いた Unknown チップがあれば削除する。
            this.チップを削除する( 編集レーン種別.Unknown, 小節先頭位置grid );

            // 新しくダミーの Unknown チップを、最終小節番号の控え＋４の小節の先頭に置く。
            var dummyChip = new 描画用チップ() {
                チップ種別 = SSTF.チップ種別.Unknown,
                小節番号 = 最大小節番号 + 4,
                小節解像度 = 1,
                小節内位置 = 0,
                譜面内絶対位置grid = 小節先頭位置grid + 小節の長さgrid + ( this.Form.GRID_PER_PART * 3 ),
            };

            var 変更後チップ = new 描画用チップ( dummyChip );
            var cell = new UndoRedo.セル<描画用チップ>(
                所有者ID: null,
                Undoアクション: ( 変更対象, 変更前, 変更後, 小節長倍率, 任意2 ) => {
                    this.スコア.チップリスト.Remove( 変更対象 );
                    for( int i = 0; i < 4; i++ )
                        this.スコア.小節長倍率リスト.RemoveAt( 変更後.小節番号 - 3 );
                },
                Redoアクション: ( 変更対象, 変更前, 変更後, 小節長倍率, 任意2 ) => {
                    変更対象.CopyFrom( 変更後 );
                    this.スコア.チップリスト.Add( 変更対象 );
                    this.スコア.チップリスト.Sort();
                    if( (double)小節長倍率 != 1.0 ) // 増設した４つの小節の小節長倍率を、最終小節の小節長倍率と同じにする。1.0 の場合は何もしない。
                    {
                        for( int i = 0; i < 4; i++ )
                            this.スコア.小節長倍率を設定する( 変更後.小節番号 - i, (double)小節長倍率 );
                    }
                    this.Form.未保存である = true;
                },
                変更対象: dummyChip,
                変更前の値: null,
                変更後の値: 変更後チップ,
                任意1: 最終小節の小節長倍率,
                任意2: null );

            this.Form.UndoRedo管理.セルを追加する( cell );
            cell.Redoを実行する();
        }


        // 各種変換


        // 小節番号 → grid
        public int 小節先頭の譜面内絶対位置gridを返す( int 小節番号 )
        {
            if( 0 > 小節番号 )
                throw new ArgumentOutOfRangeException( "小節番号に負数が指定されました。" );

            int 高さgrid = 0;

            for( int i = 0; i < 小節番号; i++ )
                高さgrid += this.小節長をグリッドで返す( i );

            return 高さgrid;
        }

        // grid → 小節番号, grid
        public (int 小節番号, int 小節の先頭位置grid) 譜面内絶対位置gridに位置する小節の情報を返す( int 譜面内絶対位置grid )
        {
            if( 0 > 譜面内絶対位置grid )
                throw new ArgumentOutOfRangeException( "位置に負数が指定されました。" );

            var result = (小節番号: 0, 小節の先頭位置grid: -1);

            int 現在の小節長合計grid = 0;
            int 現在の小節番号 = 0;
        
            while( true )       // 最大小節番号を超えてどこまでもチェック。
            {
                int 以前の値 = 現在の小節長合計grid;

                現在の小節長合計grid += this.小節長をグリッドで返す( 現在の小節番号 );

                if( 譜面内絶対位置grid < 現在の小節長合計grid )
                {
                    result.小節の先頭位置grid = 以前の値;
                    result.小節番号 = 現在の小節番号;
                    break;
                }

                現在の小節番号++;
            }

            return result;
        }

        // Xpt → レーン位置
        public int 譜面パネル内X座標pxにある表示レーン位置を返す( int 譜面パネル内X座標px )
        {
            return 譜面パネル内X座標px / this.チップサイズpx.Width;
        }

        // レーン位置 → Xpx
        public int 編集レーンのX座標pxを返す( int レーン位置 )
        {
            return this.チップサイズpx.Width * レーン位置;
        }

        // レーン位置 → 編集レーン
        public 編集レーン種別 レーン位置にある編集レーンを返す( int レーン位置 )
        {
            var kvp = this.レーン属性.FirstOrDefault( ( kvp ) => ( kvp.Value.位置 == レーン位置 ) );
            return ( kvp.Value.名前 != null ) ? kvp.Key : 編集レーン種別.Unknown;
        }

        // Xpx → 編集レーン種別
        public 編集レーン種別 譜面パネル内X座標pxにある編集レーンを返す( int 譜面パネル内X座標px )
        {
            int レーン位置 = 譜面パネル内X座標pxにある表示レーン位置を返す( 譜面パネル内X座標px );
            return レーン位置にある編集レーンを返す( レーン位置 );
        }

        // 編集レーン → Xpx
        public int 編集レーンのX座標pxを返す( 編集レーン種別 lane )
        {
            int レーン位置 = this.レーン属性[ lane ].位置;

            if( 0 > レーン位置 )
                throw new Exception( $"指定された編集レーン「{lane}」のレーン位置が設定されていません。" );

            return this.編集レーンのX座標pxを返す( レーン位置 );
        }

        // Ypx → 小節番号
        public int 譜面パネル内Y座標pxにおける小節番号を返す( int 譜面パネル内Y座標px )
        {
            return this.譜面パネル内Y座標pxにおける小節番号とその小節の譜面内絶対位置gridを返す( 譜面パネル内Y座標px ).小節番号;
        }

        // Ypx → grid
        public int 譜面パネル内Y座標pxにおける小節の譜面内絶対位置gridを返す( int 譜面パネル内Y座標px )
        {
            return this.譜面パネル内Y座標pxにおける小節番号とその小節の譜面内絶対位置gridを返す( 譜面パネル内Y座標px ).小節の譜面内絶対位置grid;
        }

        // Ypx → 小節番号, grid
        public (int 小節番号, int 小節の譜面内絶対位置grid) 譜面パネル内Y座標pxにおける小節番号とその小節の譜面内絶対位置gridを返す( int 譜面パネル内Y座標px )
        {
            int 譜面パネル内Y座標に対応する譜面内絶対位置grid =
                this.譜面表示下辺の譜面内絶対位置grid + ( this.Form.譜面パネルサイズ.Height - 譜面パネル内Y座標px ) * this.Form.GRID_PER_PIXEL;

            if( 譜面パネル内Y座標に対応する譜面内絶対位置grid < 0 )
                return (小節番号: -1, 小節の譜面内絶対位置grid: -1);

            int 次の小節の先頭までの長さgrid = 0;

            int i = 0;
            while( true )   // 最大小節番号を超えてどこまでもチェック。
            {
                double 小節長倍率 = this.スコア.小節長倍率を取得する( i );

                int 現在の小節の先頭までの長さgrid = 次の小節の先頭までの長さgrid;
                次の小節の先頭までの長さgrid += (int) ( this.Form.GRID_PER_PART * 小節長倍率 );

                if( 譜面パネル内Y座標に対応する譜面内絶対位置grid < 次の小節の先頭までの長さgrid )
                    return (小節番号: i, 小節の譜面内絶対位置grid: 現在の小節の先頭までの長さgrid);

                i++;
            }
        }

        // Ypx → grid
        public int 譜面パネル内Y座標pxにおける譜面内絶対位置gridを返す( int 譜面パネル内Y座標px )
        {
            int 譜面パネル底辺からの高さpx = this.Form.譜面パネルサイズ.Height - 譜面パネル内Y座標px;
            return this.譜面表示下辺の譜面内絶対位置grid + ( 譜面パネル底辺からの高さpx * this.Form.GRID_PER_PIXEL );
        }

        // Ypx → grid
        public int 譜面パネル内Y座標pxにおける譜面内絶対位置gridをガイド幅単位で返す( int 譜面パネル内Y座標px )
        {
            int 最高解像度での譜面内絶対位置grid = this.譜面パネル内Y座標pxにおける譜面内絶対位置gridを返す( 譜面パネル内Y座標px );
            int 対応する小節の譜面内絶対位置grid = this.譜面パネル内Y座標pxにおける小節の譜面内絶対位置gridを返す( 譜面パネル内Y座標px );
            int 対応する小節の小節先頭からの相対位置grid = ( ( 最高解像度での譜面内絶対位置grid - 対応する小節の譜面内絶対位置grid ) / this.ガイド間隔grid ) * this.ガイド間隔grid;
            return 対応する小節の譜面内絶対位置grid + 対応する小節の小節先頭からの相対位置grid;
        }

        // grid → Ypx
        public int 譜面内絶対位置gridにおける対象領域内のY座標pxを返す( int 譜面内絶対位置grid, Size 対象領域サイズpx )
        {
            int 対象領域内の高さgrid = Math.Abs( 譜面内絶対位置grid - this.譜面表示下辺の譜面内絶対位置grid );
            return ( 対象領域サイズpx.Height - ( 対象領域内の高さgrid / this.Form.GRID_PER_PIXEL ) );
        }
        
        // grid → BPM
        public double 譜面内絶対位置gridにおけるBPMを返す( int 譜面内絶対位置grid )
        {
            double bpm = SSTF.スコア.初期BPM;

            foreach( 描画用チップ chip in this.スコア.チップリスト )
            {
                if( chip.譜面内絶対位置grid > 譜面内絶対位置grid )
                    break;

                if( chip.チップ種別 == SSTF.チップ種別.BPM )
                    bpm = chip.BPM;
            }

            return bpm;
        }

        // 小節長 → grid
        public int 小節長をグリッドで返す( int 小節番号 )
        {
            double この小節の倍率 = this.スコア.小節長倍率を取得する( 小節番号 );
            return (int) ( this.Form.GRID_PER_PART * この小節の倍率 );
        }

        // x, y → チップ
        public 描画用チップ 譜面パネル内座標pxに存在するチップがあれば返す( int x, int y )
        {
            var 座標の編集レーン = this.譜面パネル内X座標pxにある編集レーンを返す( x );
            if( 座標の編集レーン == 編集レーン種別.Unknown )
                return null;

            int 座標の譜面内絶対位置grid = this.譜面パネル内Y座標pxにおける譜面内絶対位置gridを返す( y );
            int チップの厚さgrid = this.チップサイズpx.Height * this.Form.GRID_PER_PIXEL;

            foreach( 描画用チップ chip in this.スコア.チップリスト )
            {
                if( ( this.チップ種別to編集レーン[ chip.チップ種別 ] == 座標の編集レーン ) &&
                    ( 座標の譜面内絶対位置grid >= chip.譜面内絶対位置grid ) &&
                    ( 座標の譜面内絶対位置grid < chip.譜面内絶対位置grid + チップの厚さgrid ) )
                {
                    return chip;
                }
            }

            return null;
        }



        // ローカル


        protected メインフォーム Form;

        protected int ガイド間隔grid = 0;

        protected const int レーン名表示高さpx = 32;

        protected const int チップ背景色透明度 = 192;

        protected const int チップ明影透明度 = 255;

        protected const int チップ暗影透明度 = 64;

        protected const int レーン背景色透明度 = 25;

        protected Bitmap 譜面パネル背景 = null;

        protected Font 小節番号文字フォント = new Font( "MS UI Gothic", 50f, FontStyle.Regular );

        protected Brush 小節番号文字ブラシ = new SolidBrush( Color.FromArgb( 80, Color.White ) );

        protected StringFormat 小節番号文字フォーマット = new StringFormat() { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };

        protected Pen ガイド線ペン = new Pen( Color.FromArgb( 50, 50, 50 ) );

        protected Pen 小節線ペン = new Pen( Color.White, 2.0f );

        protected Pen 拍線ペン = new Pen( Color.Gray );

        protected Pen レーン区分線ペン = new Pen( Color.Gray );

        protected Pen レーン区分線太ペン = new Pen( Color.Gray, 3.0f );

        protected Pen カレントラインペン = new Pen( Color.Red );

        protected Font レーン名文字フォント = new Font( "MS US Gothic", 8.0f, FontStyle.Regular );

        protected Brush レーン名文字ブラシ = new SolidBrush( Color.FromArgb( 0xff, 220, 220, 220 ) );

        protected Brush レーン名文字影ブラシ = new SolidBrush( Color.Black );

        protected StringFormat レーン名文字フォーマット = new StringFormat() { LineAlignment = StringAlignment.Near, Alignment = StringAlignment.Center };

        protected Pen チップの太枠ペン = new Pen( Color.White, 2.0f );

        protected StringFormat チップ内文字列フォーマット = new StringFormat() { LineAlignment = StringAlignment.Near, Alignment = StringAlignment.Center };

        protected Font チップ内文字列フォント = new Font( "MS Gothic", 8f, FontStyle.Bold );

        protected Pen 白丸白バツペン = new Pen( Color.White );


        protected void 譜面に定間隔で横線を描画する( Graphics g, Rectangle 小節の描画領域px, int 間隔grid, Pen 描画ペン )
        {
            if( 描画ペン == this.ガイド線ペン )
                Debug.Assert( 間隔grid != 0 );    // ガイド線なら間隔 0 はダメ。

            if( 間隔grid >= this.Form.GRID_PER_PIXEL * 2 )    // 間隔 1px 以下は描画しない。最低2pxから。
            {
                for( int i = 0; true; i++ )
                {
                    int y = 小節の描画領域px.Bottom - ( ( i * 間隔grid ) / this.Form.GRID_PER_PIXEL );

                    if( y < 小節の描画領域px.Top )
                        break;

                    g.DrawLine( 描画ペン, 小節の描画領域px.Left, y, 小節の描画領域px.Right, y );
                }
            }
        }

        protected void チップを描画する_通常( Graphics g, SSTF.チップ種別 eチップ, int 音量, Rectangle チップ描画領域, string チップ内文字列, Color 描画色 )
        {
            using var 背景ブラシ = new SolidBrush( 描画色 );
            using var 明るいペン = new Pen( Color.FromArgb( チップ明影透明度, 描画色 ) );
            using var 暗いペン = new Pen( Color.FromArgb( チップ暗影透明度, 描画色 ) );

            this.チップ音量に合わせてチップ描画領域を縮小する( 音量, ref チップ描画領域 );

            // チップ本体
            g.FillRectangle( 背景ブラシ, チップ描画領域 );
            g.DrawLine( 明るいペン, チップ描画領域.X, チップ描画領域.Y, チップ描画領域.Right, チップ描画領域.Y );
            g.DrawLine( 明るいペン, チップ描画領域.X, チップ描画領域.Y, チップ描画領域.X, チップ描画領域.Bottom );
            g.DrawLine( 暗いペン, チップ描画領域.X, チップ描画領域.Bottom, チップ描画領域.Right, チップ描画領域.Bottom );
            g.DrawLine( 暗いペン, チップ描画領域.Right, チップ描画領域.Bottom, チップ描画領域.Right, チップ描画領域.Y );

            // チップ内文字列
            if( チップ内文字列.Nullでも空でもない() )
            {
                var layout = new RectangleF() {
                    X = チップ描画領域.X,
                    Y = チップ描画領域.Y,
                    Width = チップ描画領域.Width,
                    Height = チップ描画領域.Height,
                };
                g.DrawString( チップ内文字列, this.チップ内文字列フォント, Brushes.Black, layout, this.チップ内文字列フォーマット );
                layout.X--;
                layout.Y--;
                g.DrawString( チップ内文字列, チップ内文字列フォント, Brushes.White, layout, this.チップ内文字列フォーマット );
            }
        }

        protected void チップを描画する_通常( Graphics g, SSTF.チップ種別 eチップ, int 音量, Rectangle チップ描画領域, string チップ内文字列 )
        {
            this.チップを描画する_通常( g, eチップ, 音量, チップ描画領域, チップ内文字列, this.チップto色[ eチップ ] );
        }

        protected void チップを描画する_幅狭( Graphics g, SSTF.チップ種別 eチップ, int 音量, Rectangle チップ描画領域, string チップ内文字列, Color 描画色 )
        {
            // チップの幅を半分にする。
            int w = チップ描画領域.Width;
            チップ描画領域.Width = w / 2;
            チップ描画領域.X += w / 4;

            this.チップを描画する_通常( g, eチップ, 音量, チップ描画領域, チップ内文字列, 描画色 );
        }

        protected void チップを描画する_幅狭( Graphics g, SSTF.チップ種別 eチップ, int 音量, Rectangle チップ描画領域, string チップ内文字列 )
        {
            this.チップを描画する_幅狭( g, eチップ, 音量, チップ描画領域, チップ内文字列, this.チップto色[ eチップ ] );
        }

        protected void チップを描画する_幅狭白丸( Graphics g, SSTF.チップ種別 eチップ, int 音量, Rectangle チップ描画領域, string チップ内文字列 )
        {
            // 幅狭チップを描画。
            this.チップを描画する_幅狭( g, eチップ, 音量, チップ描画領域, チップ内文字列 );

            // その上に丸を描く。
            this.チップ音量に合わせてチップ描画領域を縮小する( 音量, ref チップ描画領域 );
            g.DrawEllipse( this.白丸白バツペン, チップ描画領域 );
        }

        protected void チップを描画する_幅狭白狭丸( Graphics g, SSTF.チップ種別 eチップ, int 音量, Rectangle チップ描画領域, string チップ内文字列 )
        {
            // 幅狭チップを描画。
            this.チップを描画する_幅狭( g, eチップ, 音量, チップ描画領域, チップ内文字列 );

            // その上に狭い丸を描く。
            this.チップ音量に合わせてチップ描画領域を縮小する( 音量, ref チップ描画領域 );
            int w = チップ描画領域.Width;
            チップ描画領域.Width = w / 3;
            チップ描画領域.X += w / 3 - 1; // -1 は見た目のバランス（直感）
            g.DrawEllipse( this.白丸白バツペン, チップ描画領域 );
        }

        protected void チップを描画する_幅狭白バツ( Graphics g, SSTF.チップ種別 eチップ, int 音量, Rectangle チップ描画領域, string チップ内文字列 )
        {
            // 幅狭チップを描画。
            this.チップを描画する_幅狭( g, eチップ, 音量, チップ描画領域, チップ内文字列 );

            // その上にバツを描く。
            this.チップ音量に合わせてチップ描画領域を縮小する( 音量, ref チップ描画領域 );
            int w = チップ描画領域.Width;
            チップ描画領域.Width = w / 3;
            チップ描画領域.X += w / 3;
            g.DrawLine( this.白丸白バツペン, new Point( チップ描画領域.Left, チップ描画領域.Top ), new Point( チップ描画領域.Right, チップ描画領域.Bottom ) );
            g.DrawLine( this.白丸白バツペン, new Point( チップ描画領域.Left, チップ描画領域.Bottom ), new Point( チップ描画領域.Right, チップ描画領域.Top ) );
        }

        protected void チップを描画する_小丸( Graphics g, SSTF.チップ種別 eチップ, int 音量, Rectangle チップ描画領域, string チップ内文字列 )
        {
            this.チップ音量に合わせてチップ描画領域を縮小する( 音量, ref チップ描画領域 );

            Color 描画色 = this.チップto色[ eチップ ];

            int w = チップ描画領域.Width;
            チップ描画領域.Width = w / 3;
            チップ描画領域.X += w / 3;

            using var 背景ブラシ = new SolidBrush( 描画色 );
            using var 枠ペン = new Pen( Color.Orange );

            g.FillEllipse( 背景ブラシ, チップ描画領域 );
            g.DrawEllipse( 枠ペン, チップ描画領域 );
        }

        protected void チップ音量に合わせてチップ描画領域を縮小する( int チップ音量, ref Rectangle 描画領域 )
        {
            double 縮小率 = (double) チップ音量 * ( 1.0 / ( メインフォーム.最大音量 - メインフォーム.最小音量 + 1 ) );

            描画領域.Y += (int) ( 描画領域.Height * ( 1.0 - 縮小率 ) );
            描画領域.Height = (int) ( 描画領域.Height * 縮小率 );
        }
    }
}
