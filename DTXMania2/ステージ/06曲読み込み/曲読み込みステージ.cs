using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using FDK;
using SSTF=SSTFormat.v004;

namespace DTXMania2.曲読み込み
{
    /// <summary>
    ///     <see cref="App.演奏譜面"/> を読み込んで、<see cref="App.演奏スコア"/> を生成する。
    /// </summary>
    class 曲読み込みステージ : IStage
    {

        // プロパティ


        public enum フェーズ
        {
            フェードイン,
            表示,
            完了,
            キャンセル時フェードアウト,
            キャンセル,
        }

        public フェーズ 現在のフェーズ { get; protected set; } = フェーズ.完了;



        // 生成と終了


        public 曲読み込みステージ()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._舞台画像 = new 舞台画像();
            this._注意文 = new 画像D2D( @"$(Images)\LoadingStage\Caution.png" );
            this._曲名画像 = new 文字列画像D2D() {
                フォント名 = "HGMaruGothicMPRO",
                フォントサイズpt = 70f,
                フォントの太さ = FontWeight.Regular,
                フォントスタイル = FontStyle.Normal,
                描画効果 = 文字列画像D2D.効果.縁取り,
                縁のサイズdpx = 10f,
                前景色 = Color4.Black,
                背景色 = Color4.White,
                表示文字列 = Global.App.演奏譜面.譜面.Title,
            };
            this._サブタイトル画像 = new 文字列画像D2D() {
                フォント名 = "HGMaruGothicMPRO",
                フォントサイズpt = 45f,
                フォントの太さ = FontWeight.Regular,
                フォントスタイル = FontStyle.Normal,
                描画効果 = 文字列画像D2D.効果.縁取り,
                縁のサイズdpx = 7f,
                前景色 = Color4.Black,
                背景色 = Color4.White,
                表示文字列 = Global.App.演奏譜面.譜面.Artist,
            };
            this._プレビュー画像 = new プレビュー画像();
            this._難易度 = new 難易度();

            Global.App.システムサウンド.再生する( システムサウンド種別.曲読み込みステージ_開始音 );
            Global.App.システムサウンド.再生する( システムサウンド種別.曲読み込みステージ_ループBGM, ループ再生する: true );

            this._舞台画像.ぼかしと縮小を適用する( 0.0 );
            Global.App.アイキャッチ管理.現在のアイキャッチ.オープンする();

            // 最初のフェーズへ。
            this.現在のフェーズ = フェーズ.フェードイン;
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            //Global.App.システムサウンド.停止する( システムサウンド種別.曲読み込みステージ_開始音 ); --> 鳴りっぱなしでいい
            Global.App.システムサウンド.停止する( システムサウンド種別.曲読み込みステージ_ループBGM );

            this._難易度.Dispose();
            this._プレビュー画像.Dispose();
            this._サブタイトル画像.Dispose();
            this._曲名画像.Dispose();
            this._注意文.Dispose();
            this._舞台画像.Dispose();
        }



        // 進行と描画


        public void 進行する()
        {
            switch( this.現在のフェーズ )
            {
                case フェーズ.フェードイン:
                {
                    #region " フェードインが完了したら表示フェーズへ。"
                    //----------------
                    if( Global.App.アイキャッチ管理.現在のアイキャッチ.現在のフェーズ == アイキャッチ.フェーズ.オープン完了 )
                        this.現在のフェーズ = フェーズ.表示;
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.表示:
                {
                    #region " スコアを読み込んで次のフェーズへ。"
                    //----------------
                    if( スコアを読み込む() )
                    {
                        // 成功
                        this.現在のフェーズ = フェーズ.完了;
                    }
                    else
                    {
                        // 失敗またはキャンセル
                        Log.Info( "スコアの読み込みをキャンセルしました。" );

                        Global.App.アイキャッチ管理.アイキャッチを選択しクローズする( nameof( 半回転黒フェード ) );
                        this.現在のフェーズ = フェーズ.キャンセル時フェードアウト;
                    }
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.完了:
                case フェーズ.キャンセル:
                {
                    #region " 遷移終了。Appによるステージ遷移待ち。"
                //----------------
                //----------------
                #endregion

                    break;
                }
                case フェーズ.キャンセル時フェードアウト:
                {
                    #region " フェードアウトが完了したらキャンセルフェーズへ。"
                    //----------------
                    if( Global.App.アイキャッチ管理.現在のアイキャッチ.現在のフェーズ == アイキャッチ.フェーズ.クローズ完了 )
                        this.現在のフェーズ = フェーズ.キャンセル;
                    //----------------
                    #endregion
        
                    break;
                }
            }
        }

        public void 描画する()
        {
            var d2ddc = Global.GraphicResources.既定のD2D1DeviceContext;
            d2ddc.Transform = Matrix3x2.Identity;

            switch( this.現在のフェーズ )
            {
                case フェーズ.フェードイン:
                {
                    #region " 背景画面＆アイキャッチフェードインを描画する。"
                    //----------------
                    d2ddc.BeginDraw();

                    this._背景画面を描画する( d2ddc );
                    Global.App.アイキャッチ管理.現在のアイキャッチ.進行描画する( d2ddc );

                    d2ddc.EndDraw();
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.表示:
                {
                    #region " スコアを読み込んで完了フェーズへ。"
                    //----------------
                    d2ddc.BeginDraw();

                    this._背景画面を描画する( d2ddc );

                    d2ddc.EndDraw();
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.完了:
                {
                    #region " 遷移終了。Appによるステージ遷移待ち。"
                    //----------------
                    d2ddc.BeginDraw();

                    this._背景画面を描画する( d2ddc );

                    d2ddc.EndDraw();
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.キャンセル時フェードアウト:
                {
                    #region " 背景画面＆アイキャッチフェードアウトを描画する。"
                    //----------------
                    d2ddc.BeginDraw();

                    this._背景画面を描画する( d2ddc );
                    Global.App.アイキャッチ管理.現在のアイキャッチ.進行描画する( d2ddc );

                    d2ddc.EndDraw();
                    //----------------
                    #endregion

                    break;
                }
            }
        }



        /// <summary>
        ///     スコアを読み込む。
        /// </summary>
        /// <returns>読み込みに成功すれば true, 失敗またはキャンセルされれば false。</returns>
        public static bool スコアを読み込む()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            var keyboard = Global.App.ドラム入力.Keyboard;

            #region " キャンセルチェック "
            //----------------
            keyboard.ポーリングする();
            if( keyboard.キーが押された( 0, Keys.Escape ) )
                return false;
            //----------------
            #endregion


            // 曲ファイルを読み込む。

            var 選択曲ファイルの絶対パス = Global.App.演奏譜面.譜面.ScorePath;
            Global.App.演奏スコア = SSTF.スコア.ファイルから生成する( 選択曲ファイルの絶対パス );

            #region " キャンセルチェック "
            //----------------
            keyboard.ポーリングする();
            if( keyboard.キーが押された( 0, Keys.Escape ) )
                return false;
            //----------------
            #endregion


            // 全チップの発声時刻を修正する。

            foreach( var chip in Global.App.演奏スコア.チップリスト )
            {
                // Viewerでの再生速度は、ビュアーモード時のみ反映する。
                double 再生速度 = Global.Options.ビュアーモードである ?
                    Global.App.ログオン中のユーザ.再生速度 * Global.App.演奏スコア.Viewerでの再生速度 :
                    Global.App.ログオン中のユーザ.再生速度;

                chip.発声時刻sec /= 再生速度;
                chip.描画時刻sec /= 再生速度;

                chip.発声時刻sec -= Global.App.サウンドデバイス.再生遅延sec;
            }

            #region " キャンセルチェック "
            //----------------
            keyboard.ポーリングする();
            if( keyboard.キーが押された( 0, Keys.Escape ) )
                return false;
            //----------------
            #endregion


            // WAVを生成する。

            Global.App.WAVキャッシュ.世代を進める();

            // ビュアーモードで再生速度が異なっている場合は、さらに世代を進めてキャッシュを無効にする。
            if( _ビュアーモードでの前回の再生速度 != Global.App.演奏スコア.Viewerでの再生速度 )
            {
                Global.App.WAVキャッシュ.世代を進める();
                _ビュアーモードでの前回の再生速度 = Global.App.演奏スコア.Viewerでの再生速度;
            }

            Global.App.WAV管理?.Dispose();
            Global.App.WAV管理 = new WAV管理();

            foreach( var kvp in Global.App.演奏スコア.WAVリスト )
            {
                #region " キャンセルチェック "
                //----------------
                keyboard.ポーリングする();
                if( keyboard.キーが押された( 0, Keys.Escape ) )
                    return false;
                //----------------
                #endregion

                var wavInfo = kvp.Value;

                var path = Path.Combine( Global.App.演奏スコア.PATH_WAV, wavInfo.ファイルパス );
                Global.App.WAV管理.登録する( kvp.Key, path, wavInfo.多重再生する, wavInfo.BGMである );
            }


            // AVIを生成する。

            Global.App.AVI管理?.Dispose();
            Global.App.AVI管理 = new AVI管理();

            if( Global.App.ログオン中のユーザ.演奏中に動画を表示する )
            {
                foreach( var kvp in Global.App.演奏スコア.AVIリスト )
                {
                    #region " キャンセルチェック "
                    //----------------
                    keyboard.ポーリングする();
                    if( keyboard.キーが押された( 0, Keys.Escape ) )
                        return false;
                    //----------------
                    #endregion

                    var path = Path.Combine( Global.App.演奏スコア.PATH_WAV, kvp.Value );

                    // Viewerでの再生速度は、ビュアーモード時のみ反映する。
                    double 再生速度 = Global.Options.ビュアーモードである ?
                        Global.App.ログオン中のユーザ.再生速度 * Global.App.演奏スコア.Viewerでの再生速度 :
                        Global.App.ログオン中のユーザ.再生速度;

                    Global.App.AVI管理.登録する( kvp.Key, path, 再生速度 );
                }
            }

            #region " キャンセルチェック "
            //----------------
            keyboard.ポーリングする();
            if( keyboard.キーが押された( 0, Keys.Escape ) )
                return false;
            //----------------
            #endregion


            // 完了。

            Log.Info( $"曲ファイルを読み込みました。[{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( 選択曲ファイルの絶対パス )}]" );

            return true;
        }



        // ローカル


        private readonly 舞台画像 _舞台画像;

        private readonly 画像D2D _注意文;

        private readonly 文字列画像D2D _曲名画像;

        private readonly 文字列画像D2D _サブタイトル画像;

        private readonly プレビュー画像 _プレビュー画像;

        private readonly 難易度 _難易度;

        private static double _ビュアーモードでの前回の再生速度 = 1.0;


        private void _曲名を描画する( DeviceContext d2ddc )
        {
            var 表示位置dpx = new Vector2( 782f, 409f );

            // 拡大率を計算して描画する。            
            float 最大幅dpx = Global.GraphicResources.設計画面サイズ.Width - 表示位置dpx.X - 20f;  // -20f はマージン
            float X方向拡大率 = ( this._曲名画像.画像サイズdpx.Width <= 最大幅dpx ) ? 1f : 最大幅dpx / this._曲名画像.画像サイズdpx.Width;
            this._曲名画像.描画する( d2ddc, 表示位置dpx.X, 表示位置dpx.Y, X方向拡大率: X方向拡大率 );
        }

        private void _サブタイトルを描画する( DeviceContext d2ddc )
        {
            var 表示位置dpx = new Vector2( 782f, 520f );

            // 拡大率を計算して描画する。
            float 最大幅dpx = Global.GraphicResources.設計画面サイズ.Width - 表示位置dpx.X;
            float X方向拡大率 = ( this._サブタイトル画像.画像サイズdpx.Width <= 最大幅dpx ) ? 1f : 最大幅dpx / this._サブタイトル画像.画像サイズdpx.Width;
            this._サブタイトル画像.描画する( d2ddc, 表示位置dpx.X, 表示位置dpx.Y, X方向拡大率: X方向拡大率 );
        }

        private void _背景画面を描画する( DeviceContext d2ddc )
        {
            this._舞台画像.進行描画する( d2ddc );
            this._注意文.描画する( d2ddc, 0f, 760f );
            this._プレビュー画像.進行描画する( d2ddc );
            this._難易度.進行描画する( d2ddc );
            this._曲名を描画する( d2ddc );
            this._サブタイトルを描画する( d2ddc );
        }
    }
}
