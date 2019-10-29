using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SSTFormat.v004;
using DTXMania2.曲;

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
            キャンセル,
        }

        public フェーズ 現在のフェーズ { get; protected set; } = フェーズ.完了;



        // 生成と終了


        public 曲読み込みステージ()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._舞台画像 = new 舞台画像();
            this._注意文 = new 画像( @"$(Images)\LoadingStage\Caution.png" );
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

            this.現在のフェーズ = フェーズ.フェードイン;
        }

        public void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            //Global.App.システムサウンド.停止する( システムサウンド種別.曲読み込みステージ_開始音 ); --> なりっぱなしでいい
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
        }

        public void 描画する()
        {
            var dc = Global.既定のD2D1DeviceContext;
            dc.Transform = Global.拡大行列DPXtoPX;

            this._舞台画像.進行描画する( dc );
            this._注意文.描画する( 0f, 760f );
            this._プレビュー画像.描画する();
            this._難易度.描画する( dc );

            this._曲名を描画する( dc );
            this._サブタイトルを描画する( dc );

            switch( this.現在のフェーズ )
            {
                case フェーズ.フェードイン:
                    Global.App.アイキャッチ管理.現在のアイキャッチ.進行描画する( dc );
                    if( Global.App.アイキャッチ管理.現在のアイキャッチ.現在のフェーズ == アイキャッチ.フェーズ.オープン完了 )
                        this.現在のフェーズ = フェーズ.表示;
                    break;

                case フェーズ.表示:
                    スコアを読み込む();
                    Global.App.ドラム入力.すべての入力デバイスをポーリングする();  // 先行入力があったらここでキャンセル
                    this.現在のフェーズ = フェーズ.完了;
                    break;
            }
        }



        // スコアの読み込み


        public static void スコアを読み込む()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );


            // 曲ファイルを読み込む。

            var 選択曲ファイルの絶対パス = Global.App.演奏譜面.譜面.ScorePath;

            Global.App.演奏スコア = スコア.ファイルから生成する( 選択曲ファイルの絶対パス );


            // 全チップの発声時刻を修正する。

            foreach( var chip in Global.App.演奏スコア.チップリスト )
            {
                chip.発声時刻sec /= Global.App.ログオン中のユーザ.再生速度;
                chip.描画時刻sec /= Global.App.ログオン中のユーザ.再生速度;

                chip.発声時刻sec -= Global.App.サウンドデバイス.再生遅延sec;
            }

            // 完了。

            Log.Info( $"曲ファイルを読み込みました。[{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( 選択曲ファイルの絶対パス )}]" );
        }



        // ローカル


        private readonly 舞台画像 _舞台画像;

        private readonly 画像 _注意文;

        private readonly 文字列画像D2D _曲名画像;

        private readonly 文字列画像D2D _サブタイトル画像;

        private readonly プレビュー画像 _プレビュー画像;

        private readonly 難易度 _難易度;

        private void _曲名を描画する( DeviceContext dc )
        {
            var 表示位置dpx = new Vector2( 782f, 409f );

            // 拡大率を計算して描画する。            
            float 最大幅dpx = Global.設計画面サイズ.Width - 表示位置dpx.X - 20f;  // -20f はマージン
            float X方向拡大率 = ( this._曲名画像.画像サイズdpx.Width <= 最大幅dpx ) ? 1f : 最大幅dpx / this._曲名画像.画像サイズdpx.Width;
            this._曲名画像.描画する( dc, 表示位置dpx.X, 表示位置dpx.Y, X方向拡大率: X方向拡大率 );
        }

        private void _サブタイトルを描画する( DeviceContext dc )
        {
            var 表示位置dpx = new Vector2( 782f, 520f );

            // 拡大率を計算して描画する。
            float 最大幅dpx = Global.設計画面サイズ.Width - 表示位置dpx.X;
            float X方向拡大率 = ( this._サブタイトル画像.画像サイズdpx.Width <= 最大幅dpx ) ? 1f : 最大幅dpx / this._サブタイトル画像.画像サイズdpx.Width;
            this._サブタイトル画像.描画する( dc, 表示位置dpx.X, 表示位置dpx.Y, X方向拡大率: X方向拡大率 );
        }
    }
}
