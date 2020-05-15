using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SSTFormat.v004;
using FDK;

namespace SSTFEditor
{
    class 譜面 : IDisposable
    {
        public readonly Size チップサイズpx = new Size( 30, 8 );

        /// <summary>
        ///     <see cref="編集モード"/> のコンストラクタでも参照されるので、登録ルールに注意すること。
        ///     >登録ルール → 同一レーンについて、最初によく使うチップを、２番目にトグルで２番目によく使うチップを登録する。
        /// </summary>
        public readonly Dictionary<チップ種別, 編集レーン種別> dicチップ編集レーン対応表 = new Dictionary<チップ種別, 編集レーン種別>() {
            #region " *** "
            //-----------------
            { チップ種別.BPM,                編集レーン種別.BPM },
            { チップ種別.LeftCrash,          編集レーン種別.左シンバル },
            { チップ種別.HiHat_Close,        編集レーン種別.ハイハット },
            { チップ種別.HiHat_Open,         編集レーン種別.ハイハット },
            { チップ種別.HiHat_HalfOpen,     編集レーン種別.ハイハット },
            { チップ種別.HiHat_Foot,         編集レーン種別.ハイハット },
            { チップ種別.Snare,              編集レーン種別.スネア },
            { チップ種別.Snare_Ghost,        編集レーン種別.スネア },
            { チップ種別.Snare_ClosedRim,    編集レーン種別.スネア },
            { チップ種別.Snare_OpenRim,      編集レーン種別.スネア },
            { チップ種別.Tom1,               編集レーン種別.ハイタム },
            { チップ種別.Tom1_Rim,           編集レーン種別.ハイタム },
            { チップ種別.Bass,               編集レーン種別.バス },
            { チップ種別.Tom2,               編集レーン種別.ロータム },
            { チップ種別.Tom2_Rim,           編集レーン種別.ロータム },
            { チップ種別.Tom3,               編集レーン種別.フロアタム },
            { チップ種別.Tom3_Rim,           編集レーン種別.フロアタム },
            { チップ種別.RightCrash,         編集レーン種別.右シンバル },
            { チップ種別.Ride,               編集レーン種別.右シンバル },    // 右側で固定とする
			{ チップ種別.Ride_Cup,           編集レーン種別.右シンバル },    //
			{ チップ種別.China,              編集レーン種別.右シンバル },    //
			{ チップ種別.Splash,             編集レーン種別.右シンバル },    //
			{ チップ種別.LeftCymbal_Mute,    編集レーン種別.左シンバル },
            { チップ種別.RightCymbal_Mute,   編集レーン種別.右シンバル },
            { チップ種別.背景動画,           編集レーン種別.BGV },
            { チップ種別.BGM,                編集レーン種別.BGM },
            { チップ種別.小節線,             編集レーン種別.Unknown },
            { チップ種別.拍線,               編集レーン種別.Unknown },
            { チップ種別.小節の先頭,         編集レーン種別.Unknown },
            { チップ種別.小節メモ,           編集レーン種別.Unknown },
            { チップ種別.SE1,                編集レーン種別.Unknown },
            { チップ種別.SE2,                編集レーン種別.Unknown },
            { チップ種別.SE3,                編集レーン種別.Unknown },
            { チップ種別.SE4,                編集レーン種別.Unknown },
            { チップ種別.SE5,                編集レーン種別.Unknown },
            { チップ種別.SE6,                編集レーン種別.Unknown },
            { チップ種別.SE7,                編集レーン種別.Unknown },
            { チップ種別.SE8,                編集レーン種別.Unknown },
            { チップ種別.SE9,                編集レーン種別.Unknown },
            { チップ種別.SE10,               編集レーン種別.Unknown },
            { チップ種別.SE11,               編集レーン種別.Unknown },
            { チップ種別.SE12,               編集レーン種別.Unknown },
            { チップ種別.SE13,               編集レーン種別.Unknown },
            { チップ種別.SE14,               編集レーン種別.Unknown },
            { チップ種別.SE15,               編集レーン種別.Unknown },
            { チップ種別.SE16,               編集レーン種別.Unknown },
            { チップ種別.SE17,               編集レーン種別.Unknown },
            { チップ種別.SE18,               編集レーン種別.Unknown },
            { チップ種別.SE19,               編集レーン種別.Unknown },
            { チップ種別.SE20,               編集レーン種別.Unknown },
            { チップ種別.SE21,               編集レーン種別.Unknown },
            { チップ種別.SE22,               編集レーン種別.Unknown },
            { チップ種別.SE23,               編集レーン種別.Unknown },
            { チップ種別.SE24,               編集レーン種別.Unknown },
            { チップ種別.SE25,               編集レーン種別.Unknown },
            { チップ種別.SE26,               編集レーン種別.Unknown },
            { チップ種別.SE27,               編集レーン種別.Unknown },
            { チップ種別.SE28,               編集レーン種別.Unknown },
            { チップ種別.SE29,               編集レーン種別.Unknown },
            { チップ種別.SE30,               編集レーン種別.Unknown },
            { チップ種別.SE31,               編集レーン種別.Unknown },
            { チップ種別.SE32,               編集レーン種別.Unknown },
            { チップ種別.GuitarAuto,         編集レーン種別.Unknown },
            { チップ種別.BassAuto,           編集レーン種別.Unknown },
            { チップ種別.Unknown,            編集レーン種別.Unknown },
            //-----------------
            #endregion
        };

        public readonly Dictionary<編集レーン種別, int> dicレーン番号 = new Dictionary<編集レーン種別, int>() {
            #region " *** "
            //-----------------
            { 編集レーン種別.BPM, 0 },
            { 編集レーン種別.左シンバル, 1 },
            { 編集レーン種別.ハイハット, 2 },
            { 編集レーン種別.スネア, 3 },
            { 編集レーン種別.ハイタム, 4 },
            { 編集レーン種別.バス, 5 },
            { 編集レーン種別.ロータム, 6 },
            { 編集レーン種別.フロアタム, 7 },
            { 編集レーン種別.右シンバル, 8 },
            { 編集レーン種別.BGV, 9 },
            { 編集レーン種別.BGM, 10 },
            { 編集レーン種別.Unknown, -1 },
            //-----------------
            #endregion
        };

        public readonly Dictionary<int, 編集レーン種別> dicレーン番号逆引き = new Dictionary<int, 編集レーン種別>();   // 初期化はコンストラクタ内で。


        public スコア SSTFormatScore;

        public int 譜面表示下辺の譜面内絶対位置grid { get; set; }

        public int カレントラインの譜面内絶対位置grid
            => ( this.譜面表示下辺の譜面内絶対位置grid + ( 230 * this.Form.GRID_PER_PIXEL ) ); // 譜面拡大率によらず、大体下辺から -230 pixel くらいで。

        public int 全小節の高さgrid
        {
            get
            {
                int 高さgrid = 0;
                int 最大小節番号 = this.SSTFormatScore.最大小節番号を返す();

                for( int i = 0; i <= 最大小節番号; i++ )
                    高さgrid += this.小節長をグリッドで返す( i );

                return 高さgrid;
            }
        }

        public int レーンの合計幅px
            => ( Enum.GetValues( typeof( 編集レーン種別 ) ).Length - 1 ) * this.チップサイズpx.Width;    // -1 は Unknown の分

        public int 譜面表示下辺に位置する小節番号
            => this.譜面内絶対位置gridに位置する小節の情報を返す( this.譜面表示下辺の譜面内絶対位置grid ).小節番号;

        public int カレントラインに位置する小節番号
            => this.譜面内絶対位置gridに位置する小節の情報を返す( this.カレントラインの譜面内絶対位置grid ).小節番号;


        public 譜面( メインフォーム form )
        {
            this.Form = form;

            // 初期化

            this.SSTFormatScore = new スコア();
            this.譜面表示下辺の譜面内絶対位置grid = 0;
            foreach( var kvp in this.dicレーン番号 )
                this.dicレーン番号逆引き.Add( kvp.Value, kvp.Key );

            #region " 最初は10小節ほど用意しておく → 10小節目の先頭に Unknown チップを置くことで実現。"
            //-----------------
            this.SSTFormatScore.チップリスト.Add(
                new 描画用チップ() {
                    チップ種別 = チップ種別.Unknown,
                    小節番号 = 9,   // 0から数えて10番目の小節 = 009
                    小節解像度 = 1,
                    小節内位置 = 0,
                    譜面内絶対位置grid = 9 * this.Form.GRID_PER_PART,      // 小節009の先頭位置
                } );
            //-----------------
            #endregion
        }

        public void Dispose()
        {
            this.SSTFormatScore = null;

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

        public void 曲データファイルを読み込む( string ファイル名 )
        {
            // 解放
            this.SSTFormatScore = null;

            // 読み込み
            this.SSTFormatScore = スコア.ファイルから生成する( ファイル名 );

            // 後処理

            #region " 小節線・拍線・Unknown チップをすべて削除する。"
            //-----------------
            this.SSTFormatScore.チップリスト.RemoveAll( ( chip ) => (
                chip.チップ種別 == チップ種別.小節線 || 
                chip.チップ種別 == チップ種別.拍線 || 
                chip.チップ種別 == チップ種別.Unknown ) );
            //-----------------
            #endregion

            #region " チップリストのすべてのチップを、描画用チップに変換する。"
            //----------------
            {
                // バックアップを取って、
                var 元のチップリスト = new チップ[ this.SSTFormatScore.チップリスト.Count ];
                for( int i = 0; i < this.SSTFormatScore.チップリスト.Count; i++ )
                    元のチップリスト[ i ] = this.SSTFormatScore.チップリスト[ i ];

                // クリアして、
                this.SSTFormatScore.チップリスト.Clear();

                // 再構築。
                for( int i = 0; i < 元のチップリスト.Length; i++ )
                    this.SSTFormatScore.チップリスト.Add( new 描画用チップ( 元のチップリスト[ i ] ) );
            }
            //----------------
            #endregion

            #region " 全チップに対して「譜面内絶対位置grid」を設定する。"
            //-----------------
            {
                int チップが存在する小節の先頭grid = 0;
                int 現在の小節番号 = 0;

                foreach( 描画用チップ chip in this.SSTFormatScore.チップリスト )
                {
                    // チップの小節番号が現在の小節番号よりも大きい場合、チップが存在する小節に至るまで、「チップが存在する小節の先頭grid」を更新する。
                    while( 現在の小節番号 < chip.小節番号 )
                    {
                        double 現在の小節の小節長倍率 = this.SSTFormatScore.小節長倍率を取得する( 現在の小節番号 );
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
            using var fs = new FileStream( ファイル名, FileMode.Create, FileAccess.Write );
            スコア.SSTF.出力する( this.SSTFormatScore, fs, $"{ヘッダ行}{Environment.NewLine}" );
        }

        public void 描画する( Graphics g, Control panel )
        {
            #region " panel のレーン背景画像が未作成なら作成する。"
            //-----------------
            if( null == panel.BackgroundImage )
            {
                this.譜面パネル背景 = new Bitmap( this.レーンの合計幅px, 1 );
                using( var graphics = Graphics.FromImage( this.譜面パネル背景 ) )
                {
                    int x = 0;
                    foreach( var kvp in this.レーンto背景色 )
                    {
                        using( var brush = new SolidBrush( kvp.Value ) )
                            graphics.FillRectangle( brush, x, 0, this.チップサイズpx.Width, 1 );

                        x += this.チップサイズpx.Width;
                    }
                }

                panel.Width = レーンの合計幅px;
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
                int 最大小節番号 = this.SSTFormatScore.最大小節番号を返す();

                for( int 小節番号 = 0; 小節番号 <= 最大小節番号; 小節番号++ )
                {
                    int 小節長grid = this.小節長をグリッドで返す( 小節番号 );
                    int 次の小節の先頭位置grid = 小節先頭の譜面内絶対位置grid + 小節長grid;
                    Rectangle 小節の描画領域px;

                    // クリッピングと小節の描画領域の取得。小節が描画領域上端を超えたら終了。

                    #region " (A) 小節の描画領域が、パネルの領域外（下）にある場合。→ この小節は無視して次の小節へ。"
                    //-----------------
                    if( 次の小節の先頭位置grid < パネル下辺の譜面内絶対位置grid )
                    {
                        小節先頭の譜面内絶対位置grid = 次の小節の先頭位置grid;
                        continue;
                    }
                    //-----------------
                    #endregion
                    #region " (B) 小節の描画領域が、パネルの領域外（上）にある場合。→ ここで描画終了。"
                    //-----------------
                    else if( 小節先頭の譜面内絶対位置grid >= パネル上辺の譜面内絶対位置grid )
                    {
                        break;
                    }
                    //-----------------
                    #endregion
                    #region " (C) 小節の描画領域が、パネル内にすべて収まっている場合。"
                    //-----------------
                    else if( ( 小節先頭の譜面内絶対位置grid >= パネル下辺の譜面内絶対位置grid ) && ( 次の小節の先頭位置grid < パネル上辺の譜面内絶対位置grid ) )
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
                    #region " (D) 小節の描画領域が、パネルをすべて包み込んでいる場合。"
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
                    #region " (E) 小節の描画領域が、パネルの下側にはみだしている場合。"
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
                    #region " (F) 小節の描画領域が、パネルの上側にはみだしている場合。"
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
                    this.譜面に定間隔で線を描画する( g, 小節番号, 小節の描画領域px, this.ガイド間隔grid, this.ガイド線ペン );
                    //-----------------
                    #endregion
                    #region " 拍線を描画。"
                    //-----------------
                    this.譜面に定間隔で線を描画する( g, 小節番号, 小節の描画領域px, this.Form.GRID_PER_PART / 4, this.拍線ペン );
                    //-----------------
                    #endregion
                    #region " レーン区分線を描画。"
                    //-----------------
                    {
                        int x = 0;
                        int num = Enum.GetValues( typeof( 編集レーン種別 ) ).Length - 1;   // -1 は Unknown の分
                        for( int i = 0; i < num; i++ )
                        {
                            x += this.チップサイズpx.Width;

                            if( x >= 小節の描画領域px.Width )
                                x = 小節の描画領域px.Width - 1;

                            g.DrawLine(
                                ( i == 0 || i == num - 3 ) ? this.レーン区分線太ペン : this.レーン区分線ペン,
                                x,
                                小節の描画領域px.Top,
                                x,
                                小節の描画領域px.Bottom );
                        }
                    }
                    //-----------------
                    #endregion
                    #region " 小節線を描画。"
                    //-----------------
                    this.譜面に定間隔で線を描画する( g, 小節番号, 小節の描画領域px, 小節長grid, this.小節線ペン );
                    //-----------------
                    #endregion

                    // 次の小節へ。
                    小節先頭の譜面内絶対位置grid = 次の小節の先頭位置grid;
                }
            }
            //-----------------
            #endregion

            #region " チップを描画。"
            //-----------------
            var チップ描画領域 = new Rectangle();
            foreach( 描画用チップ chip in this.SSTFormatScore.チップリスト )
            {
                #region " クリッピング。"
                //-----------------
                if( chip.チップ種別 == チップ種別.Unknown )
                    continue;   // 描画対象外

                if( 0 != chip.枠外レーン数 )
                    continue;   // 描画範囲外

                if( chip.譜面内絶対位置grid < パネル下辺の譜面内絶対位置grid )
                    continue;   // 描画範囲外（次のチップへ）

                if( chip.譜面内絶対位置grid >= パネル上辺の譜面内絶対位置grid )
                    break;      // 描画範囲外（ここで終了）
                //-----------------
                #endregion

                int レーン番号 = this.dicレーン番号[ this.dicチップ編集レーン対応表[ chip.チップ種別 ] ];

                チップ描画領域.X = レーン番号 * this.チップサイズpx.Width;
                チップ描画領域.Y = panel.ClientSize.Height - ( chip.譜面内絶対位置grid - this.譜面表示下辺の譜面内絶対位置grid ) / this.Form.GRID_PER_PIXEL - this.チップサイズpx.Height;
                チップ描画領域.Width = this.チップサイズpx.Width;
                チップ描画領域.Height = this.チップサイズpx.Height;

                this.チップを指定領域へ描画する( g, chip.チップ種別, chip.音量, チップ描画領域, chip.チップ内文字列 );

                // 選択中なら太枠を付与。
                if( chip.ドラッグ操作により選択中である || chip.選択が確定している )
                    this.チップの太枠を指定領域へ描画する( g, チップ描画領域 );
            }
            //-----------------
            #endregion

            #region " レーン名を描画。"
            //-----------------
            var レーン名描画領域下側 = new Rectangle( 0, 10, panel.Width, 譜面.レーン番号表示高さpx );
            var レーン名描画領域上側 = new Rectangle( 0, 0, panel.Width, 10 );

            // グラデーション描画。
            using( var brush = new LinearGradientBrush( レーン名描画領域下側, Color.FromArgb( 255, 50, 155, 50 ), Color.FromArgb( 0, 0, 255, 0 ), LinearGradientMode.Vertical ) )
                g.FillRectangle( brush, レーン名描画領域下側 );

            using( var brush = new LinearGradientBrush( レーン名描画領域上側, Color.FromArgb( 255, 0, 100, 0 ), Color.FromArgb( 255, 50, 155, 50 ), LinearGradientMode.Vertical ) )
                g.FillRectangle( brush, レーン名描画領域上側 );

            // レーン名を描画。
            var レーン名描画領域 = new Rectangle( 0, 0, 0, 0 );

            foreach( 編集レーン種別 editLaneType in Enum.GetValues( typeof( 編集レーン種別 ) ) )
            {
                if( editLaneType == 編集レーン種別.Unknown )
                    break;

                レーン名描画領域.X = レーン名描画領域下側.X + ( this.dicレーン番号[ editLaneType ] * this.チップサイズpx.Width ) + 2;
                レーン名描画領域.Y = レーン名描画領域下側.Y + 2;
                レーン名描画領域.Width = this.チップサイズpx.Width;
                レーン名描画領域.Height = 24;

                g.DrawString(
                    this.レーンto名前[ editLaneType ],
                    this.レーン名文字フォント,
                    this.レーン名文字影ブラシ,
                    レーン名描画領域,
                    this.レーン名文字フォーマット );

                レーン名描画領域.X -= 2;
                レーン名描画領域.Y -= 2;

                g.DrawString(
                    this.レーンto名前[ editLaneType ],
                    this.レーン名文字フォント,
                    this.レーン名文字ブラシ,
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
                0.0f,
                y,
                (float) ( panel.Size.Width - 1 ),
                y );
            //-----------------
            #endregion
        }

        public void チップを指定領域へ描画する( Graphics g, チップ種別 eチップ, int 音量, Rectangle チップ描画領域, string チップ内文字列 )
        {
            // ※SSTFormat.チップ の描画以外の目的でも呼ばれるため、本メソッドの引数には SSTFormat.チップ を入れていない。

            switch( eチップ )
            {
                case チップ種別.BPM:
                case チップ種別.LeftCrash:
                case チップ種別.HiHat_Close:
                case チップ種別.Snare:
                case チップ種別.Tom1:
                case チップ種別.Bass:
                case チップ種別.Tom2:
                case チップ種別.Tom3:
                case チップ種別.RightCrash:
                case チップ種別.China:
                case チップ種別.Splash:
                case チップ種別.背景動画:
                case チップ種別.BGM:
                    this.チップを描画する_通常( g, eチップ, 音量, チップ描画領域, チップ内文字列 );
                    break;

                case チップ種別.Snare_Ghost:
                    this.チップを描画する_小丸( g, eチップ, 音量, チップ描画領域, チップ内文字列 );
                    break;

                case チップ種別.Ride:
                    this.チップを描画する_幅狭( g, eチップ, 音量, チップ描画領域, チップ内文字列 );
                    break;

                case チップ種別.Snare_OpenRim:
                case チップ種別.HiHat_Open:
                    this.チップを描画する_幅狭白丸( g, eチップ, 音量, チップ描画領域, チップ内文字列 );
                    break;

                case チップ種別.HiHat_HalfOpen:
                case チップ種別.Ride_Cup:
                    this.チップを描画する_幅狭白狭丸( g, eチップ, 音量, チップ描画領域, チップ内文字列 );
                    break;

                case チップ種別.HiHat_Foot:
                case チップ種別.Snare_ClosedRim:
                case チップ種別.Tom1_Rim:
                case チップ種別.Tom2_Rim:
                case チップ種別.Tom3_Rim:
                case チップ種別.LeftCymbal_Mute:
                case チップ種別.RightCymbal_Mute:
                    this.チップを描画する_幅狭白バツ( g, eチップ, 音量, チップ描画領域, チップ内文字列 );
                    break;
            }
        }

        public void チップの太枠を指定領域へ描画する( Graphics g, Rectangle チップ描画領域 )
        {
            g.DrawRectangle( this.チップの太枠ペン, チップ描画領域 );
        }

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
                throw new ArgumentOutOfRangeException( "譜面内絶対位置grid が負数です。" );

            var result = (小節番号: 0, 小節の先頭位置grid: -1);

            int n = 0;
            int back = 0;
            int i = 0;
            while( true )       // 最大譜面番号を超えてどこまでもチェック。
            {
                back = n;
                n += this.小節長をグリッドで返す( i );

                if( 譜面内絶対位置grid < n )
                {
                    result.小節の先頭位置grid = back;
                    result.小節番号 = i;
                    break;
                }

                i++;
            }

            return result;
        }

        // Xpx → 編集レーン種別
        public 編集レーン種別 譜面パネル内X座標pxにある編集レーンを返す( int 譜面パネル内X座標px )
        {
            int レーン番号 = 譜面パネル内X座標px / this.チップサイズpx.Width;

            foreach( var kvp in this.dicレーン番号 )
            {
                if( kvp.Value == レーン番号 )
                    return kvp.Key;
            }

            return 編集レーン種別.Unknown;
        }

        // 編集レーン → Xpx
        public int 編集レーンのX座標pxを返す( 編集レーン種別 lane )
        {
            if( lane == 編集レーン種別.Unknown )
                return -1;

            return this.dicレーン番号[ lane ] * this.チップサイズpx.Width;
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
            {
                return (小節番号: -1, 小節の譜面内絶対位置grid: -1);
            }

            int 現在の小節の先頭までの長さgrid = 0;
            int 次の小節の先頭までの長さgrid = 0;

            int i = 0;
            while( true )   // 最大小節番号を超えてどこまでもチェック。
            {
                double 小節長倍率 = this.SSTFormatScore.小節長倍率を取得する( i );

                現在の小節の先頭までの長さgrid = 次の小節の先頭までの長さgrid;
                次の小節の先頭までの長さgrid += (int) ( this.Form.GRID_PER_PART * 小節長倍率 );

                if( 譜面パネル内Y座標に対応する譜面内絶対位置grid < 次の小節の先頭までの長さgrid )
                {
                    return (小節番号: i, 小節の譜面内絶対位置grid: 現在の小節の先頭までの長さgrid);
                }

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
            double bpm = スコア.初期BPM;

            foreach( 描画用チップ chip in this.SSTFormatScore.チップリスト )
            {
                if( chip.譜面内絶対位置grid > 譜面内絶対位置grid )
                    break;

                if( chip.チップ種別 == チップ種別.BPM )
                    bpm = chip.BPM;
            }

            return bpm;
        }

        // 小節長 → grid
        public int 小節長をグリッドで返す( int 小節番号 )
        {
            double この小節の倍率 = this.SSTFormatScore.小節長倍率を取得する( 小節番号 );
            return (int) ( this.Form.GRID_PER_PART * この小節の倍率 );
        }

        public 描画用チップ 譜面パネル内座標pxに存在するチップがあれば返す( int x, int y )
        {
            var 座標の編集レーン = this.譜面パネル内X座標pxにある編集レーンを返す( x );
            if( 座標の編集レーン == 編集レーン種別.Unknown )
                return null;
            int 座標の譜面内絶対位置grid = this.譜面パネル内Y座標pxにおける譜面内絶対位置gridを返す( y );
            int チップの厚さgrid = this.チップサイズpx.Height * this.Form.GRID_PER_PIXEL;

            foreach( 描画用チップ chip in this.SSTFormatScore.チップリスト )
            {
                if( ( this.dicチップ編集レーン対応表[ chip.チップ種別 ] == 座標の編集レーン ) &&
                    ( 座標の譜面内絶対位置grid >= chip.譜面内絶対位置grid ) &&
                    ( 座標の譜面内絶対位置grid < chip.譜面内絶対位置grid + チップの厚さgrid ) )
                {
                    return chip;
                }
            }

            return null;
        }

        public void 現在のガイド間隔を変更する( int n分 )
        {
            this.ガイド間隔grid = ( n分 == 0 ) ? 1 : ( this.Form.GRID_PER_PART / n分 );
        }

        public void チップを配置または置換する( 編集レーン種別 e編集レーン, チップ種別 eチップ, int 譜面内絶対位置grid, string チップ文字列, int 音量, double BPM, bool 選択確定中 )
        {
            try
            {
                this.Form.UndoRedo管理.トランザクション記録を開始する();

                // 配置位置にチップがあれば削除する。
                this.チップを削除する( e編集レーン, 譜面内絶対位置grid );   // そこにチップがなければ何もしない。

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
                        this.SSTFormatScore.チップリスト.Remove( 変更対象 );
                        this.Form.未保存である = true;
                    },
                    Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                        変更対象.CopyFrom( 変更前 );
                        this.SSTFormatScore.チップリスト.Add( 変更対象 );
                        this.SSTFormatScore.チップリスト.Sort();
                        this.Form.未保存である = true;
                    },
                    変更対象: chip,
                    変更前の値: 変更前チップ,
                    変更後の値: null );

                this.Form.UndoRedo管理.セルを追加する( cell );
                cell.Redoを実行する();

                // 配置した小節が現状最後の小節だったら、後ろに小節を４つ追加する。
                if( chip.小節番号 == this.SSTFormatScore.最大小節番号を返す() )
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
                ( from chip in this.SSTFormatScore.チップリスト
                  where ( ( this.dicチップ編集レーン対応表[ chip.チップ種別 ] == e編集レーン ) && ( ( (描画用チップ) chip ).譜面内絶対位置grid == 譜面内絶対位置grid ) )
                  select chip ).FirstOrDefault();   // チップが重なってたとしても、削除するのはひとつだけ。

            if( null != 削除チップ )
            {
                // UndoRedo セルを登録。
                var 変更前チップ = new 描画用チップ( 削除チップ );
                var cell = new UndoRedo.セル<描画用チップ>(
                    所有者ID: null,
                    Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                        変更対象.CopyFrom( 変更前 );
                        this.SSTFormatScore.チップリスト.Add( 変更対象 );
                        this.SSTFormatScore.チップリスト.Sort();
                        this.Form.未保存である = true;
                    },
                    Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                        this.SSTFormatScore.チップリスト.Remove( 変更対象 );
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
            int 最大小節番号 = this.SSTFormatScore.最大小節番号を返す();

            // 最終小節の小節先頭位置grid と 小節長倍率 を取得する。
            int 小節先頭位置grid = this.小節先頭の譜面内絶対位置gridを返す( 最大小節番号 );
            int 小節の長さgrid = this.小節長をグリッドで返す( 最大小節番号 );
            double 最終小節の小節長倍率 = this.SSTFormatScore.小節長倍率を取得する( 最大小節番号 );

            // ダミーで置いた Unknown チップがあれば削除する。
            this.チップを削除する( 編集レーン種別.Unknown, 小節先頭位置grid );

            // 新しくダミーの Unknown チップを、最終小節番号の控え＋４の小節の先頭に置く。
            var dummyChip = new 描画用チップ() {
                チップ種別 = チップ種別.Unknown,
                小節番号 = 最大小節番号 + 4,
                小節解像度 = 1,
                小節内位置 = 0,
                譜面内絶対位置grid = 小節先頭位置grid + 小節の長さgrid + ( this.Form.GRID_PER_PART * 3 ),
            };

            var 変更後チップ = new 描画用チップ( dummyChip );
            var cell = new UndoRedo.セル<描画用チップ>(
                所有者ID: null,
                Undoアクション: ( 変更対象, 変更前, 変更後, 小節長倍率, 任意2 ) => {
                    this.SSTFormatScore.チップリスト.Remove( 変更対象 );
                    for( int i = 0; i < 4; i++ )
                        this.SSTFormatScore.小節長倍率リスト.RemoveAt( 変更後.小節番号 - 3 );
                },
                Redoアクション: ( 変更対象, 変更前, 変更後, 小節長倍率, 任意2 ) => {
                    変更対象.CopyFrom( 変更後 );
                    this.SSTFormatScore.チップリスト.Add( 変更対象 );
                    this.SSTFormatScore.チップリスト.Sort();
                    if( (double) 小節長倍率 != 1.0 ) // 増設した４つの小節の小節長倍率を、最終小節の小節長倍率と同じにする。1.0 の場合は何もしない。
                    {
                        for( int i = 0; i < 4; i++ )
                            this.SSTFormatScore.小節長倍率を設定する( 変更後.小節番号 - i, (double) 小節長倍率 );
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


        protected メインフォーム Form;

        protected int ガイド間隔grid = 0;

        protected const int レーン番号表示高さpx = 32;

        protected const int チップ背景色透明度 = 192;

        protected const int チップ明影透明度 = 255;

        protected const int チップ暗影透明度 = 64;

        protected const int レーン背景色透明度 = 25;

        protected readonly Dictionary<編集レーン種別, Color> レーンto背景色 = new Dictionary<編集レーン種別, Color>() {
            #region " *** "
            //-----------------
            { 編集レーン種別.BPM,          Color.FromArgb( レーン背景色透明度, Color.SkyBlue ) },
            { 編集レーン種別.左シンバル,   Color.FromArgb( レーン背景色透明度, Color.WhiteSmoke ) },
            { 編集レーン種別.ハイハット,   Color.FromArgb( レーン背景色透明度, Color.SkyBlue ) },
            { 編集レーン種別.スネア,       Color.FromArgb( レーン背景色透明度, Color.Orange ) },
            { 編集レーン種別.ハイタム,     Color.FromArgb( レーン背景色透明度, Color.Lime ) },
            { 編集レーン種別.バス,         Color.FromArgb( レーン背景色透明度, Color.Gainsboro) },
            { 編集レーン種別.ロータム,     Color.FromArgb( レーン背景色透明度, Color.Red ) },
            { 編集レーン種別.フロアタム,   Color.FromArgb( レーン背景色透明度, Color.Magenta ) },
            { 編集レーン種別.右シンバル,   Color.FromArgb( レーン背景色透明度, Color.WhiteSmoke ) },
            { 編集レーン種別.BGV,          Color.FromArgb( レーン背景色透明度, Color.SkyBlue ) },
            { 編集レーン種別.BGM,          Color.FromArgb( レーン背景色透明度, Color.SkyBlue ) },
            { 編集レーン種別.Unknown,      Color.FromArgb( レーン背景色透明度, Color.White ) },
            //-----------------
            #endregion
        };

        protected readonly Dictionary<チップ種別, Color> チップto色 = new Dictionary<チップ種別, Color>() {
            #region " *** "
            //-----------------
            { チップ種別.BPM,                Color.FromArgb( チップ背景色透明度, Color.SkyBlue ) },
            { チップ種別.LeftCrash,          Color.FromArgb( チップ背景色透明度, Color.WhiteSmoke ) },
            { チップ種別.LeftCymbal_Mute,    Color.FromArgb( チップ背景色透明度, Color.Gray ) },
            { チップ種別.HiHat_Close,        Color.FromArgb( チップ背景色透明度, Color.SkyBlue ) },
            { チップ種別.HiHat_Foot,         Color.FromArgb( チップ背景色透明度, Color.SkyBlue ) },
            { チップ種別.HiHat_HalfOpen,     Color.FromArgb( チップ背景色透明度, Color.SkyBlue ) },
            { チップ種別.HiHat_Open,         Color.FromArgb( チップ背景色透明度, Color.SkyBlue ) },
            { チップ種別.Snare,              Color.FromArgb( チップ背景色透明度, Color.Orange ) },
            { チップ種別.Snare_ClosedRim,    Color.FromArgb( チップ背景色透明度, Color.OrangeRed ) },
            { チップ種別.Snare_Ghost,        Color.FromArgb( チップ背景色透明度, Color.DeepPink ) },
            { チップ種別.Snare_OpenRim,      Color.FromArgb( チップ背景色透明度, Color.Orange ) },
            { チップ種別.Tom1,               Color.FromArgb( チップ背景色透明度, Color.Lime ) },
            { チップ種別.Tom1_Rim,           Color.FromArgb( チップ背景色透明度, Color.Lime ) },
            { チップ種別.Bass,               Color.FromArgb( チップ背景色透明度, Color.Gainsboro ) },
            { チップ種別.Tom2,               Color.FromArgb( チップ背景色透明度, Color.Red ) },
            { チップ種別.Tom2_Rim,           Color.FromArgb( チップ背景色透明度, Color.Red ) },
            { チップ種別.Tom3,               Color.FromArgb( チップ背景色透明度, Color.Magenta ) },
            { チップ種別.Tom3_Rim,           Color.FromArgb( チップ背景色透明度, Color.Magenta ) },
            { チップ種別.RightCrash,         Color.FromArgb( チップ背景色透明度, Color.WhiteSmoke ) },
            { チップ種別.RightCymbal_Mute,   Color.FromArgb( チップ背景色透明度, Color.Gray ) },
            { チップ種別.Ride,               Color.FromArgb( チップ背景色透明度, Color.WhiteSmoke ) },
            { チップ種別.Ride_Cup,           Color.FromArgb( チップ背景色透明度, Color.WhiteSmoke ) },
            { チップ種別.China,              Color.FromArgb( チップ背景色透明度, Color.WhiteSmoke ) },
            { チップ種別.Splash,             Color.FromArgb( チップ背景色透明度, Color.WhiteSmoke ) },
            { チップ種別.背景動画,           Color.FromArgb( チップ背景色透明度, Color.SkyBlue ) },
            { チップ種別.BGM,                Color.FromArgb( チップ背景色透明度, Color.SkyBlue ) },
            //-----------------
            #endregion
        };

        protected readonly Dictionary<編集レーン種別, string> レーンto名前 = new Dictionary<編集レーン種別, string>() {
            #region " *** "
            //-----------------
            { 編集レーン種別.BPM,          "BPM" },
            { 編集レーン種別.左シンバル,   "LC" },
            { 編集レーン種別.ハイハット,   "HH" },
            { 編集レーン種別.スネア,       "SD" },
            { 編集レーン種別.ハイタム,     "HT" },
            { 編集レーン種別.バス,         "BD" },
            { 編集レーン種別.ロータム,     "LT" },
            { 編集レーン種別.フロアタム,   "FT" },
            { 編集レーン種別.右シンバル,   "RC" },
            { 編集レーン種別.BGV,          "BGV" },
            { 編集レーン種別.BGM,          "BGM" },
            { 編集レーン種別.Unknown,      "NG" },
            //-----------------
            #endregion
        };

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


        protected void 譜面に定間隔で線を描画する( Graphics g, int 小節番号, Rectangle 小節の描画領域, int 間隔grid, Pen 描画ペン )
        {
            if( 描画ペン == this.ガイド線ペン )
                Debug.Assert( 間隔grid != 0 );    // ガイド線なら間隔 0 はダメ。

            if( 間隔grid >= this.Form.GRID_PER_PIXEL * 2 )    // 間隔 1px 以下は描画しない。最低2pxから。
            {
                for( int i = 0; true; i++ )
                {
                    int y = 小節の描画領域.Bottom - ( ( i * 間隔grid ) / this.Form.GRID_PER_PIXEL );

                    if( y < 小節の描画領域.Top )
                        break;

                    g.DrawLine(
                        描画ペン,
                        小節の描画領域.Left,
                        y,
                        小節の描画領域.Right,
                        y );
                }
            }
        }

        protected void チップを描画する_通常( Graphics g, チップ種別 eチップ, int 音量, Rectangle チップ描画領域, string チップ内文字列, Color 描画色 )
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

        protected void チップを描画する_通常( Graphics g, チップ種別 eチップ, int 音量, Rectangle チップ描画領域, string チップ内文字列 )
        {
            this.チップを描画する_通常( g, eチップ, 音量, チップ描画領域, チップ内文字列, this.チップto色[ eチップ ] );
        }

        protected void チップを描画する_幅狭( Graphics g, チップ種別 eチップ, int 音量, Rectangle チップ描画領域, string チップ内文字列, Color 描画色 )
        {
            // チップの幅を半分にする。
            int w = チップ描画領域.Width;
            チップ描画領域.Width = w / 2;
            チップ描画領域.X += w / 4;

            this.チップを描画する_通常( g, eチップ, 音量, チップ描画領域, チップ内文字列, 描画色 );
        }

        protected void チップを描画する_幅狭( Graphics g, チップ種別 eチップ, int 音量, Rectangle チップ描画領域, string チップ内文字列 )
        {
            this.チップを描画する_幅狭( g, eチップ, 音量, チップ描画領域, チップ内文字列, this.チップto色[ eチップ ] );
        }

        protected void チップを描画する_幅狭白丸( Graphics g, チップ種別 eチップ, int 音量, Rectangle チップ描画領域, string チップ内文字列 )
        {
            // 幅狭チップを描画。
            this.チップを描画する_幅狭( g, eチップ, 音量, チップ描画領域, チップ内文字列 );

            // その上に丸を描く。
            this.チップ音量に合わせてチップ描画領域を縮小する( 音量, ref チップ描画領域 );
            g.DrawEllipse( this.白丸白バツペン, チップ描画領域 );
        }

        protected void チップを描画する_幅狭白狭丸( Graphics g, チップ種別 eチップ, int 音量, Rectangle チップ描画領域, string チップ内文字列 )
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

        protected void チップを描画する_幅狭白バツ( Graphics g, チップ種別 eチップ, int 音量, Rectangle チップ描画領域, string チップ内文字列 )
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

        protected void チップを描画する_小丸( Graphics g, チップ種別 eチップ, int 音量, Rectangle チップ描画領域, string チップ内文字列 )
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
