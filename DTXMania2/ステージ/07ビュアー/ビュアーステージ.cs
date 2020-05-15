using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct2D1;
using FDK;
using DTXMania2.演奏;

namespace DTXMania2.ビュアー
{
    class ビュアーステージ : IStage
    {

        // プロパティ


        public enum フェーズ
        {
            指示待機,
            曲読み込み開始,
            曲読み込み完了待ち,
            曲読み込み完了,
        }

        public フェーズ 現在のフェーズ { get; protected set; }

        /// <summary>
        ///     他プロセスから受け取ったオプションがここに格納される。
        /// </summary>
        public static ConcurrentQueue<CommandLineOptions> OptionsQueue { get; } = new ConcurrentQueue<CommandLineOptions>();



        // 生成と終了


        public ビュアーステージ()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            var userConfig = Global.App.ログオン中のユーザ;

            Global.App.演奏譜面 = new 曲.Score();
            Global.App.演奏スコア = null!;
            this._成績 = new 成績();
            this._曲読み込み完了通知 = new ManualResetEventSlim( true );

            this._背景画像 = new 画像( @"$(Images)\PlayStage\Background.png" );
            this._レーンフレーム = new レーンフレーム();
            this._曲名パネル = new 曲名パネル();
            this._ドラムキットとヒットバー = new ドラムキットとヒットバー();
            this._左サイドクリアパネル = new 左サイドクリアパネル();
            this._右サイドクリアパネル = new 右サイドクリアパネル();
            this._判定パラメータ表示 = new 判定パラメータ表示();
            this._フェーズパネル = new フェーズパネル();
            this._クリアメーター = new クリアメーター();
            this._スコア表示 = new スコア表示();
            this._プレイヤー名表示 = new プレイヤー名表示() { 名前 = userConfig.名前 };
            this._譜面スクロール速度 = new 譜面スクロール速度( userConfig.譜面スクロール速度 );
            this._達成率表示 = new 達成率表示();
            this._曲別SKILL = new 曲別SKILL();
            this._エキサイトゲージ = new エキサイトゲージ();

            this.現在のフェーズ = フェーズ.指示待機;
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._エキサイトゲージ.Dispose();
            this._曲別SKILL.Dispose();
            this._達成率表示.Dispose();
            this._譜面スクロール速度.Dispose();
            this._プレイヤー名表示.Dispose();
            this._スコア表示.Dispose();
            this._クリアメーター.Dispose();
            this._フェーズパネル.Dispose();
            this._判定パラメータ表示.Dispose();
            this._右サイドクリアパネル.Dispose();
            this._左サイドクリアパネル.Dispose();
            this._ドラムキットとヒットバー.Dispose();
            this._曲名パネル.Dispose();
            this._レーンフレーム.Dispose();
            this._背景画像.Dispose();
        }



        // 進行と描画


        public void 進行する()
        {
            switch( this.現在のフェーズ )
            {
                case フェーズ.指示待機:

                    #region " オプションが届いていれば、取り出して処理する。"
                    //----------------
                    if( OptionsQueue.TryDequeue( out var options ) )
                    {
                        if( options.再生開始 )
                        {
                            if( File.Exists( options.Filename ) )
                            {
                                Global.App.演奏譜面 = new 曲.Score() {   // 読み込みに必要な最低減の譜面情報で生成。
                                    譜面 = new ScoreDBRecord() {
                                        ScorePath = options.Filename,
                                    },
                                };
                                演奏ステージ.演奏開始小節番号 = options.再生開始小節番号;
                                演奏ステージ.ビュアーモードでドラム音を再生する = options.ドラム音を発声する;
                                this.現在のフェーズ = フェーズ.曲読み込み開始;
                            }
                            else
                            {
                                Log.ERROR( $"ファイルが存在しません。[{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( options.Filename )}]" );
                            }
                        }
                    }
                    //----------------
                    #endregion

                    break;

                case フェーズ.曲読み込み開始:

                    #region " 完了通知イベントをリセットして、読み込みタスクを起動する。"
                    //----------------
                    this._曲読み込み完了通知.Reset();
                    
                    Task.Run( () => {

                        曲読み込み.曲読み込みステージ.スコアを読み込む();
                        this._曲読み込み完了通知.Set();

                    } );

                    this.現在のフェーズ = フェーズ.曲読み込み完了待ち;
                    //----------------
                    #endregion
                    
                    break;

                case フェーズ.曲読み込み完了待ち:

                    #region " 完了通知が来るまで待ち、通知が来たら完了フェーズへ。"
                    //----------------
                    if( this._曲読み込み完了通知.IsSet )
                        this.現在のフェーズ = フェーズ.曲読み込み完了;
                    //----------------
                    #endregion
                    
                    break;

                case フェーズ.曲読み込み完了:
                    break;
            }
        }

        public void 描画する()
        {
            var dc = Global.既定のD2D1DeviceContext;
            dc.Transform = Global.拡大行列DPXtoPX;

            this._ベース画像を描画する( dc );

            switch( this.現在のフェーズ )
            {
                case フェーズ.指示待機:
                    break;

                case フェーズ.曲読み込み開始:
                    break;

                case フェーズ.曲読み込み完了待ち:
                    break;

                case フェーズ.曲読み込み完了:
                    break;
            }
        }



        // ローカル


        private 成績 _成績;

        private ManualResetEventSlim _曲読み込み完了通知;

        private void _ベース画像を描画する( DeviceContext dc )
        {
            var userConfig = Global.App.ログオン中のユーザ;

            #region " 左サイドパネルへの描画と、左サイドパネルの表示 "
            //----------------
            this._左サイドクリアパネル.クリアする();
            this._左サイドクリアパネル.クリアパネル.画像へ描画する( ( dcp ) => {

                // プレイヤー名
                this._プレイヤー名表示.進行描画する( dcp );

                // スコア
                if( userConfig.ダーク == ダーク種別.OFF )
                    this._スコア表示.進行描画する( dcp, Global.Animation, new Vector2( +280f, +120f ), this._成績 );

                // 達成率
                this._達成率表示.描画する( dcp, (float) this._成績.Achievement );

                // 判定パラメータ
                this._判定パラメータ表示.描画する( dcp, +118f, +372f, this._成績 );

                // スキル
                this._曲別SKILL.進行描画する( dcp, 0f );

            } );
            this._左サイドクリアパネル.描画する();
            //----------------
            #endregion

            #region " 右サイドパネルへの描画と、右サイトパネルの表示 "
            //----------------
            this._右サイドクリアパネル.クリアする();
            this._右サイドクリアパネル.クリアパネル.画像へ描画する( ( dcp ) => {
                // 特になし
            } );
            this._右サイドクリアパネル.描画する();
            //----------------
            #endregion

            #region " レーンフレーム "
            //----------------
            this._レーンフレーム.描画する( dc, userConfig.レーンの透明度, レーンラインを描画する: ( userConfig.ダーク == ダーク種別.OFF ) ? true : false );
            //----------------
            #endregion

            #region " 背景画像 "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._背景画像.描画する( 0f, 0f );
            //----------------
            #endregion

            #region " 譜面スクロール速度 "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._譜面スクロール速度.描画する( dc, userConfig.譜面スクロール速度 );
            //----------------
            #endregion

            #region " エキサイトゲージ "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._エキサイトゲージ.進行描画する( dc, this._成績.エキサイトゲージ量 );
            //----------------
            #endregion

            #region " クリアメーター "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._クリアメーター.進行描画する( dc );
            //----------------
            #endregion

            #region " フェーズパネル "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._フェーズパネル.進行描画する();
            //----------------
            #endregion

            #region " 曲目パネル "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._曲名パネル.描画する( dc );
            //----------------
            #endregion

            #region " ヒットバー "
            //----------------
            if( userConfig.ダーク != ダーク種別.FULL )
                this._ドラムキットとヒットバー.ヒットバーを進行描画する();
            //----------------
            #endregion

            #region " ドラムキット "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._ドラムキットとヒットバー.ドラムキットを進行描画する();
            //----------------
            #endregion
        }



        // 画面を構成するもの


        private readonly 画像 _背景画像;

        private readonly 曲名パネル _曲名パネル;

        private readonly レーンフレーム _レーンフレーム;

        private readonly ドラムキットとヒットバー _ドラムキットとヒットバー;

        private readonly 譜面スクロール速度 _譜面スクロール速度;

        private readonly エキサイトゲージ _エキサイトゲージ;

        private readonly フェーズパネル _フェーズパネル;

        private readonly クリアメーター _クリアメーター;

        private readonly 左サイドクリアパネル _左サイドクリアパネル;

        private readonly 右サイドクリアパネル _右サイドクリアパネル;



        // 左サイドクリアパネル内に表示されるもの


        private readonly スコア表示 _スコア表示;

        private readonly プレイヤー名表示 _プレイヤー名表示;

        private readonly 判定パラメータ表示 _判定パラメータ表示;

        private readonly 達成率表示 _達成率表示;

        private readonly 曲別SKILL _曲別SKILL;
    }
}
