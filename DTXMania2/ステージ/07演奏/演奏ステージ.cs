﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct2D1;
using FDK;
using SSTF=SSTFormat.v004;

namespace DTXMania2.演奏
{
    class 演奏ステージ : IStage
    {

        // プロパティ


        public const float ヒット判定位置Ydpx = 847f;

        public enum フェーズ
        {
            演奏状態初期化,
            フェードイン,
            演奏開始,
            表示,
            クリア,
            失敗,
            キャンセル通知,
            キャンセル時フェードアウト,
            キャンセル完了,

            // 以下、ビュアーモード用。
            指示待機,
            曲読み込み開始,
            曲読み込み完了待ち,
        }

        public フェーズ 現在のフェーズ { get; protected set; } = フェーズ.キャンセル完了;

        /// <summary>
        ///     フェードインアイキャッチの遷移元画面。
        ///     利用前に、外部から設定される。
        /// </summary>
        public Bitmap キャプチャ画面 { get; set; } = null!;

        public 成績 成績 { get; protected set; } = null!;

        /// <summary>
        ///     ビュアーモード時、他プロセスから受け取ったオプションがここに格納される。
        /// </summary>
        public static ConcurrentQueue<CommandLineOptions> OptionsQueue { get; } = new ConcurrentQueue<CommandLineOptions>();

        /// <summary>
        ///     ビュアーモードで変更可能。通常モードでは -1、
        /// </summary>
        public static int 演奏開始小節番号 { get; set; } = -1;

        /// <summary>
        ///     ビュアーモードで変更可能。通常モードでは無効。
        /// </summary>
        public static bool ビュアーモードでドラム音を再生する { get; set; } = true;



        // 生成と終了


        public 演奏ステージ()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            var userConfig = Global.App.ログオン中のユーザ;

            this._背景画像 = new 画像D2D( @"$(Images)\PlayStage\Background.png" );
            this._レーンフレーム = new レーンフレーム();
            this._曲名パネル = new 曲名パネル();
            this._ドラムキットとヒットバー = new ドラムキットとヒットバー();
            this._レーンフラッシュ = new レーンフラッシュ();
            this._ドラムチップ = new ドラムチップ();
            this._判定文字列 = new 判定文字列();
            this._チップ光 = new チップ光();
            this._左サイドクリアパネル = new 左サイドクリアパネル();
            this._右サイドクリアパネル = new 右サイドクリアパネル();
            this._判定パラメータ表示 = new 判定パラメータ表示();
            this._フェーズパネル = new フェーズパネル();
            this._コンボ表示 = new コンボ表示();
            this._クリアメーター = new クリアメーター();
            this._スコア表示 = new スコア表示();
            this._プレイヤー名表示 = new プレイヤー名表示() { 名前 = userConfig.名前 };
            this._譜面スクロール速度 = new 譜面スクロール速度( userConfig.譜面スクロール速度 );
            this._達成率表示 = new 達成率表示();
            this._曲別SKILL = new 曲別SKILL();
            this._エキサイトゲージ = new エキサイトゲージ();
            this._システム情報 = new システム情報();
            this._数字フォント中グレー48x64 = new フォント画像D2D( @"$(Images)\NumberFont48x64White.png", @"$(Images)\NumberFont48x64.yaml", 文字幅補正dpx: -16f, 不透明度: 0.3f );
            this._小節線色 = new SolidColorBrush( Global.GraphicResources.既定のD2D1DeviceContext, Color.White );
            this._拍線色 = new SolidColorBrush( Global.GraphicResources.既定のD2D1DeviceContext, Color.Gray );
            this._スコア指定の背景画像 = null!;
            this._チップの演奏状態 = null!;
            this._フェードインカウンタ = new Counter();
            this._LoadingSpinner = new LoadingSpinner();
            this._早送りアイコン = new 早送りアイコン();
            this.成績 = new 成績();

            // 最初のフェーズへ。
            this.現在のフェーズ = ( Global.Options.ビュアーモードである ) ? フェーズ.指示待機 : フェーズ.演奏状態初期化;
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            #region " 演奏状態を終了する。"
            //----------------
            this._描画開始チップ番号 = -1;

            //Global.App.WAV管理?.Dispose();	    --> ここではまだ解放しない。
            Global.App.AVI管理?.Dispose();
            //----------------
            #endregion

            // ユーザDBに変更があれば保存する。
            Global.App.ログオン中のユーザ.保存する();

            this.キャプチャ画面?.Dispose();
            this._スコア指定の背景画像?.Dispose();

            this._早送りアイコン.Dispose();
            this._LoadingSpinner.Dispose();
            this._拍線色.Dispose();
            this._小節線色.Dispose();
            this._数字フォント中グレー48x64.Dispose();
            this._システム情報.Dispose();
            this._エキサイトゲージ.Dispose();
            this._曲別SKILL.Dispose();
            this._達成率表示.Dispose();
            this._譜面スクロール速度.Dispose();
            this._プレイヤー名表示.Dispose();
            this._スコア表示.Dispose();
            this._クリアメーター.Dispose();
            this._コンボ表示.Dispose();
            this._フェーズパネル.Dispose();
            this._判定パラメータ表示.Dispose();
            this._右サイドクリアパネル.Dispose();
            this._左サイドクリアパネル.Dispose();
            this._チップ光.Dispose();
            this._判定文字列.Dispose();
            this._ドラムチップ.Dispose();
            this._レーンフラッシュ.Dispose();
            this._ドラムキットとヒットバー.Dispose();
            this._曲名パネル.Dispose();
            this._レーンフレーム.Dispose();
            this._背景画像.Dispose();
        }



        // 進行と描画


        public void 進行する()
        {
            this._システム情報.FPSをカウントしプロパティを更新する();

            CommandLineOptions? options = null!;
            var userConfig = Global.App.ログオン中のユーザ;

            #region " ビュアーモード時、オプションが届いていれば処理する。"
            //----------------
            if( Global.Options.ビュアーモードである && OptionsQueue.TryDequeue( out options ) )
            {
                if( options.再生停止 )
                {
                    Log.Info( "演奏を即時終了します。" );
                    Global.App.WAV管理?.すべての発声を停止する();    // DTXでのBGMサウンドはこっちに含まれる。

                    this.現在のフェーズ = フェーズ.指示待機;
                }
            }
            //----------------
            #endregion

            switch( this.現在のフェーズ )
            {
                case フェーズ.演奏状態初期化:
                {
                    #region " 演奏状態を初期化し、演奏開始またはフェードインフェーズへ。"
                    //----------------
                    this._演奏状態を初期化する();
                    this._フェードインカウンタ = new Counter( 0, 100, 10 );

                    // 次のフェーズへ。
                    this.現在のフェーズ = ( Global.Options.ビュアーモードである ) ? フェーズ.演奏開始 : フェーズ.フェードイン;
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.フェードイン:
                {
                    #region " フェードインが完了したら演奏開始フェーズへ。"
                    //----------------
                    if( this._フェードインカウンタ.終了値に達した )
                    {
                        Log.Info( "演奏を開始します。" );
                        this.現在のフェーズ = フェーズ.演奏開始;
                    }
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.演奏開始:
                {
                    #region " 演奏を開始し、表示フェーズへ。"
                    //----------------
                    // 演奏を開始する。
                    this._描画開始チップ番号 = -1; // -1 以外になれば演奏開始。

                    if( Global.Options.ビュアーモードである && 0 <= 演奏ステージ.演奏開始小節番号 )
                    {
                        #region " (A) 演奏開始小節番号から演奏を開始する。"
                        //----------------
                        this._指定小節へ移動する( 演奏ステージ.演奏開始小節番号, out this._描画開始チップ番号, out double 演奏開始時刻sec );

                        Global.App.サウンドタイマ.リセットする( 演奏開始時刻sec );
                        //----------------
                        #endregion
                    }
                    else
                    {
                        #region " (B) 最初から再生する。"
                        //----------------
                        this._描画開始チップ番号 = 0;

                        Global.App.サウンドタイマ.リセットする();
                        //----------------
                        #endregion
                    }

                    // 次のフェーズへ。
                    this._表示フェーズの結果 = 演奏結果.演奏中;
                    this.現在のフェーズ = フェーズ.表示;
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.表示:
                {
                    if( this._表示フェーズの結果 == 演奏結果.クリア )
                    {
                        #region " 演奏をクリアしたら、クリア or 指示待機フェーズへ。"
                        //----------------
                        this.現在のフェーズ = ( Global.Options.ビュアーモードである ) ? フェーズ.指示待機 : フェーズ.クリア;
                        //----------------
                        #endregion
                    }
                    else if( this._表示フェーズの結果 == 演奏結果.失敗 )
                    {
                        // todo: StageFailed したら失敗フェーズへ
                    }
                    else
                    {
                        this._入力とヒット処理を行う();
                    }
                    break;
                }
                case フェーズ.キャンセル通知:
                {
                    #region " フェードアウトを開始してキャセル時フェードアウトフェーズへ。"
                    //----------------
                    Global.App.アイキャッチ管理.アイキャッチを選択しクローズする( nameof( 半回転黒フェード ) );

                    this.現在のフェーズ = フェーズ.キャンセル時フェードアウト;
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.キャンセル時フェードアウト:
                {
                    #region " フェードアウトが完了したらキャンセル完了フェーズへ。"
                    //----------------
                    if( Global.App.アイキャッチ管理.現在のアイキャッチ.現在のフェーズ == アイキャッチ.フェーズ.クローズ完了 )
                        this.現在のフェーズ = フェーズ.キャンセル完了;
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.クリア:
                {
                    break;
                }
                case フェーズ.失敗:
                {
                    // todo: 失敗フェーズの実装
                    break;
                }
                case フェーズ.キャンセル完了:
                {
                    break;
                }

                // 以下、ビュアーモード用。

                case フェーズ.指示待機:
                {
                    #region " オプションが届いていれば、取り出して処理する。 "
                    //----------------
                    if( options?.再生開始 ?? false )
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

                            // 次のフェーズへ。
                            this.現在のフェーズ = フェーズ.曲読み込み開始;
                        }
                        else
                        {
                            Log.ERROR( $"ファイルが存在しません。[{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( options.Filename )}]" );
                        }
                    }
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.曲読み込み開始:
                {
                    #region " 曲読み込みタスクを起動し、曲読み込み完了待ちフェーズへ。"
                    //----------------
                    this._曲読み込みタスク = Task.Run( () => {
                        曲読み込み.曲読み込みステージ.スコアを読み込む();
                    } );

                    // 次のフェーズへ。
                    this.現在のフェーズ = フェーズ.曲読み込み完了待ち;
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.曲読み込み完了待ち:
                {
                    #region " 曲読み込みタスクが完了すれば演奏状態初期化フェーズへ。 "
                    //----------------
                    if( this._曲読み込みタスク.IsCompleted )
                        this.現在のフェーズ = フェーズ.演奏状態初期化;
                    //----------------
                    #endregion

                    break;
                }
            }
        }

        public void 描画する()
        {
            this._システム情報.VPSをカウントする();

            var d2ddc = Global.GraphicResources.既定のD2D1DeviceContext;
            d2ddc.Transform = Matrix3x2.Identity;

            switch( this.現在のフェーズ )
            {
                case フェーズ.演奏状態初期化:
                    break;

                case フェーズ.フェードイン:
                {
                    #region " 演奏パーツなし画面＆フェードインを描画する。"
                    //----------------
                    Global.App.画面をクリアする();

                    d2ddc.BeginDraw();

                    this._演奏パーツなし背景画面を描画する( d2ddc );
                    this._キャプチャ画面を描画する( d2ddc, ( 1.0f - this._フェードインカウンタ.現在値の割合 ) );

                    d2ddc.EndDraw();
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.演奏開始:
                {
                    #region " 演奏パーツなし画面を描画する。"
                    //----------------
                    Global.App.画面をクリアする();

                    d2ddc.BeginDraw();

                    this._演奏パーツなし背景画面を描画する( d2ddc );

                    d2ddc.EndDraw();
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.表示:
                {
                    #region " 演奏パーツあり画面を描画する。"
                    //----------------
                    Global.App.画面をクリアする();

                    // D2DとD3Dの混在描画について：
                    // 　Direct2D の BeginDraw()～EndDraw() の負荷はかなり高い。（～30組/描画 程度が許容限界）
                    // 　そこで、描画メソッド全体を1つの BeginDraw() と EndDraw() で囲むことにする。
                    // 　途中で Direct3D の Draw を行いたい場合には、いったん EndDraw()し、D3Dで描画し、そして再びBeginDraw()するようにする。

                    d2ddc.BeginDraw();

                    this._表示フェーズの結果 = this._演奏パーツあり背景画面を描画する( d2ddc );

                    d2ddc.EndDraw();
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.キャンセル通知:
                    break;

                case フェーズ.キャンセル時フェードアウト:
                {
                    #region " 演奏パーツあり画面＆フェードアウトを描画する。"
                    //----------------
                    Global.App.画面をクリアする();

                    d2ddc.BeginDraw();

                    this._演奏パーツあり背景画面を描画する( d2ddc );
                    Global.App.アイキャッチ管理.現在のアイキャッチ.進行描画する( d2ddc );

                    d2ddc.EndDraw();
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.クリア:
                {
                    #region " 演奏パーツなし画面を描画する。"
                    //----------------
                    Global.App.画面をクリアする();

                    d2ddc.BeginDraw();

                    this._演奏パーツなし背景画面を描画する( d2ddc );

                    d2ddc.EndDraw();
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.失敗:
                    break;  // todo: 失敗フェーズの実装

                case フェーズ.キャンセル完了:
                {
                    #region " フェードアウトを描画する。"
                    //----------------
                    d2ddc.BeginDraw();

                    Global.App.アイキャッチ管理.現在のアイキャッチ.進行描画する( d2ddc );

                    d2ddc.EndDraw();
                    //----------------
                    #endregion

                    break;
                }

                // 以下、ビュアーモード用。

                case フェーズ.指示待機:
                case フェーズ.曲読み込み開始:
                {
                    #region " 待機画面を描画する。"
                    //----------------
                    Global.App.画面をクリアする();

                    d2ddc.BeginDraw();

                    this._ビュアーモードの待機時背景を描画する( d2ddc );

                    d2ddc.EndDraw();
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.曲読み込み完了待ち:
                {
                    #region " 待機画面＆ローディング画像を描画する。"
                    //----------------
                    Global.App.画面をクリアする();

                    d2ddc.BeginDraw();

                    this._ビュアーモードの待機時背景を描画する( d2ddc );
                    this._LoadingSpinner.進行描画する( d2ddc );

                    d2ddc.EndDraw();
                    //----------------
                    #endregion

                    break;
                }
            }
        }

        private void _入力とヒット処理を行う()
        {
            var userConfig = Global.App.ログオン中のユーザ;
            double 現在の演奏時刻sec = Global.App.サウンドタイマ.現在時刻sec;

            #region " (1) 自動ヒット処理; ユーザの入力がなくても、判定バーに達したら自動的にヒット扱いされるチップの処理。"
            //----------------
            // 描画開始チップ（画面最下位のチップ）から後方（画面上方）のチップに向かって……
            for( int i = this._描画開始チップ番号; ( 0 <= i ) && ( i < Global.App.演奏スコア.チップリスト.Count ); i++ )
            {
                var chip = Global.App.演奏スコア.チップリスト[ i ];
                var chipProperty = userConfig.ドラムチッププロパティリスト[ chip.チップ種別 ];

                // チップの状態を確認。

                this._チップと判定との距離と時間を計算する(
                    現在の演奏時刻sec,
                    chip,
                    out double ヒット判定バーと描画との時間sec,   // 負数ならバー未達、0でバー直上、正数でバー通過。 
                    out double ヒット判定バーと発声との時間sec,   //
                    out double ヒット判定バーとの距離dpx );       //

                bool チップはAutoPlayである = userConfig.AutoPlay[ chipProperty.AutoPlay種別 ];
                bool チップはAutoPlayではない = !チップはAutoPlayである;
                bool チップはヒット済みである = this._チップの演奏状態[ chip ].ヒット済みである;
                bool チップはヒットされていない = !チップはヒット済みである;
                bool チップは発声済みである = this._チップの演奏状態[ chip ].発声済みである;
                bool チップは発声されていない = !チップは発声済みである;
                bool チップはMISSエリアに達している = ( ヒット判定バーと描画との時間sec - Global.App.システム設定.判定位置調整ms * 0.001 ) > userConfig.最大ヒット距離sec[ 判定種別.OK ];
                bool チップは判定エリアに達していない = ( ヒット判定バーと描画との時間sec - Global.App.システム設定.判定位置調整ms * 0.001 ) < -userConfig.最大ヒット距離sec[ 判定種別.OK ];
                bool チップの描画位置がヒット判定バーを通過した = ( 0 <= ヒット判定バーと描画との時間sec );
                bool チップの発声位置がヒット判定バーを通過した = ( 0 <= ヒット判定バーと発声との時間sec );

                // ループ終了判定。

                if( チップは判定エリアに達していない )
                    break;  // これ以降のチップはヒット判定する必要がないので、ここでループを抜ける。


                // チップの状態に応じて、必要があれば自動ヒット処理を行う。

                // (1-1) MISS 判定

                if( チップはヒットされていない && チップはMISSエリアに達している )
                {
                    if( チップはAutoPlayである && chipProperty.AutoPlayON_Miss判定 )
                    {
                        #region " MISS(A) 自動演奏(AutoPlay)チップ "
                        //----------------
                        this._チップのヒット処理を行う(
                            chip,
                            判定種別.MISS,
                            chipProperty.AutoPlayON_自動ヒット_再生,
                            chipProperty.AutoPlayON_自動ヒット_判定,
                            chipProperty.AutoPlayON_自動ヒット_非表示,
                            ヒット判定バーと発声との時間sec,
                            ヒット判定バーと描画との時間sec );
                        //----------------
                        #endregion
                    }
                    else if( チップはAutoPlayではない && chipProperty.AutoPlayOFF_Miss判定 )
                    {
                        #region " MISS(B) 手動演奏チップ "
                        //----------------
                        this._チップのヒット処理を行う(
                            chip,
                            判定種別.MISS,
                            chipProperty.AutoPlayOFF_ユーザヒット_再生,
                            chipProperty.AutoPlayOFF_ユーザヒット_判定,
                            chipProperty.AutoPlayOFF_ユーザヒット_非表示,
                            ヒット判定バーと発声との時間sec,
                            ヒット判定バーと描画との時間sec );
                        //----------------
                        #endregion
                    }
                }
                else
                {
                    // (1-2-1) 自動ヒット判定（再生）

                    if( チップの発声位置がヒット判定バーを通過した && チップは発声されていない )    // ヒット済みかどうかには関係ない
                    {
                        if( チップはAutoPlayである && chipProperty.AutoPlayON_自動ヒット_再生 )
                        {
                            #region " 自動発声(A) 自動演奏(AutoPlay)チップ "
                            //----------------
                            this._チップの再生を行う( chip, userConfig.ドラムの音を発声する );
                            this._チップの演奏状態[ chip ].発声済みである = true;
                            //----------------
                            #endregion
                        }
                        else if( チップはAutoPlayではない && chipProperty.AutoPlayOFF_自動ヒット_再生 )
                        {
                            #region " 自動発声(B) 手動演奏チップ "
                            //----------------
                            this._チップの再生を行う( chip, userConfig.ドラムの音を発声する );
                            this._チップの演奏状態[ chip ].発声済みである = true;
                            //----------------
                            #endregion
                        }
                    }

                    // (1-2-2) 自動ヒット判定

                    if( チップの描画位置がヒット判定バーを通過した && チップはヒットされていない )    // else if ではない
                    {
                        if( チップはAutoPlayである && chipProperty.AutoPlayON_自動ヒット )
                        {
                            #region " 自動ヒット(A) 自動演奏(AutoPlay)チップ "
                            //----------------
                            this._チップのヒット処理を行う(
                                chip,
                                判定種別.PERFECT,   // AutoPlay 時は Perfect 扱い。
                                chipProperty.AutoPlayON_自動ヒット_再生,
                                chipProperty.AutoPlayON_自動ヒット_判定,
                                chipProperty.AutoPlayON_自動ヒット_非表示,
                                ヒット判定バーと発声との時間sec,
                                ヒット判定バーと描画との時間sec );

                            this._ドラムキットとヒットバー.ヒットアニメ開始( chipProperty.表示レーン種別 );
                            //----------------
                            #endregion
                        }
                        else if( チップはAutoPlayではない && chipProperty.AutoPlayOFF_自動ヒット )
                        {
                            #region " 自動ヒット(B) 手動演奏チップだがAutoPlayOFF時に自動ヒットするよう指定されているチップ "
                            //----------------
                            this._チップのヒット処理を行う(
                                chip,
                                判定種別.PERFECT,   // AutoPlay OFF でも自動ヒットする場合は Perfect 扱い。
                                chipProperty.AutoPlayOFF_自動ヒット_再生,
                                chipProperty.AutoPlayOFF_自動ヒット_判定,
                                chipProperty.AutoPlayOFF_自動ヒット_非表示,
                                ヒット判定バーと発声との時間sec,
                                ヒット判定バーと描画との時間sec );

                            this._ドラムキットとヒットバー.ヒットアニメ開始( chipProperty.表示レーン種別 );
                            //----------------
                            #endregion
                        }
                    }
                }
            }
            //----------------
            #endregion

            Global.App.ドラム入力.すべての入力デバイスをポーリングする( 入力履歴を記録する: false );

            if( 0 < Global.App.ドラム入力.ポーリング結果.Count )
            {
                // 各入力に処理済みフラグを付与した、新しいリストを作成する。（要素はタプルであり値型なので注意。）
                (ドラム入力イベント 入力, bool 処理済み)[] 入力リスト =
                    Global.App.ドラム入力.ポーリング結果.Select( ( ie ) => (ie, false) ).ToArray();

                // 入力時刻を補正する。
                foreach( var (入力, 処理済み) in 入力リスト )
                    入力.InputEvent.TimeStamp -= Global.App.システム設定.判定位置調整ms * 0.001;

                #region " (2) ユーザ入力に対するチップのヒット判定とヒット処理。"
                //----------------
                // 入力集合の判定は２回に分けて行う。１回目は入力グループを無効とし、２回目は有効とする。
                // これにより、各入力は、入力グループに属するチップよりも自身が対応するチップを優先してヒット判定される。
                // 例：
                //      HH入力とRD入力は同一の入力グループに属しており、HH入力でRDチップをヒットしたり、RD入力でHHチップをヒットしたりすることができる。
                //      ここで、HHチップとRDチップが同時刻に配置されていて、そこへHH入力とRD入力が同時に行われた場合を考える。
                //      このような場合、HH入力はHHチップと、そしてRD入力はRDチップと、チップを取り違えることなくそれぞれ正しくヒット判定されなければならない。
                //      すなわち、自身が対応するチップがヒット判定可能である場合には、入力グループに属する他のチップよりも優先して判定されなければならない。
                for( int i = 1; i <= 2; i++ )
                {
                    bool 入力グループが有効 = ( i != 1 );  // １回目はfalse、２回目はtrue

                    // すべての入力について……
                    for( int n = 0; n < 入力リスト.Length; n++ )
                    {
                        if( 入力リスト[ n ].処理済み )
                            continue;

                        var 入力 = 入力リスト[ n ].入力;

                        if( this._ユーザヒット対象外である( 入力 ) )
                            continue;   // ヒット判定対象外の入力は無視。

                        #region " (2-1) チップにヒットしてようがしてまいが、入力に対して起こすアクションを実行。"
                        //----------------
                        {
                            var dispLane = _入力に対応する表示レーン種別を返す( 入力 );

                            if( dispLane != 表示レーン種別.Unknown )
                            {
                                this._ドラムキットとヒットバー.ヒットアニメ開始( dispLane );
                                this._レーンフラッシュ.開始する( dispLane );
                            }
                        }
                        //----------------
                        #endregion

                        #region " (2-2) 手動ヒット処理; ユーザの入力に対してヒット可能なチップを検索し、あればヒット処理する。"
                        //----------------
                        {
                            // 入力に対応する一番近いチップを検索する。

                            var chip = this._指定された時刻に一番近いチップを返す(
                                入力.InputEvent.TimeStamp,
                                this._描画開始チップ番号,
                                追加の検索条件: ( c ) => {

                                    #region " チップ c が入力にヒットしているなら true を返す。"
                                    //----------------
                                    // ヒット済みチップは無視。
                                    if( this._チップの演奏状態[ c ].ヒット済みである )
                                        return false;

                                    // ドラムチッププロパティが無効のチップは無視。
                                    var chipProperty = userConfig.ドラムチッププロパティリスト.チップtoプロパティ[ c.チップ種別 ];
                                    if( chipProperty.ドラム入力種別 == ドラム入力種別.Unknown )
                                        return false;

                                    // AutoPlay ON のチップは無視。
                                    if( userConfig.AutoPlay[ chipProperty.AutoPlay種別 ] )
                                        return false;

                                    // AutoPlay OFF のときユーザヒットの対象にならないチップは無視。
                                    if( !chipProperty.AutoPlayOFF_ユーザヒット )
                                        return false;

                                    // 距離と時刻を計算。
                                    this._チップと判定との距離と時間を計算する(
                                        現在の演奏時刻sec,
                                        c,
                                        out double ヒット判定バーと描画との時間sec,   // 負数ならバー未達、0でバー直上、正数でバー通過。 
                                        out double ヒット判定バーと発声との時間sec,   //
                                        out double ヒット判定バーとの距離dpx );       //

                                    // 入力時刻の補正とは別に、判定エリア／MISS判定のためのチップの時刻の補正も必要。
                                    ヒット判定バーと描画との時間sec -= Global.App.システム設定.判定位置調整ms * 0.001;

                                    // 判定エリアに達していないチップは無視。
                                    if( ヒット判定バーと描画との時間sec < -userConfig.最大ヒット距離sec[ 判定種別.OK ] )
                                        return false;

                                    // MISSエリアに達しているチップは無視。
                                    if( ヒット判定バーと描画との時間sec > userConfig.最大ヒット距離sec[ 判定種別.OK ] )
                                        return false;

                                    // 入力に対応しないチップは無視……の前に入力グループ判定。
                                    if( chipProperty.ドラム入力種別 != 入力.Type )
                                    {
                                        if( 入力グループが有効 )
                                        {
                                            // 入力グループ判定：
                                            // 1つの入力に対して、種類の異なる複数のチップがヒット判定対象になることができる。
                                            // 例えば、Ride入力は、RideチップとRide_Cupチップのどちらにもヒットすることができる。

                                            // 入力が属する入力グループ種別（任意個）を取得。
                                            var 入力のヒット判定対象となる入力グループ種別集合 =
                                                from kvp in userConfig.ドラムチッププロパティリスト.チップtoプロパティ
                                                where kvp.Value.ドラム入力種別 == 入力.Type
                                                select kvp.Value.入力グループ種別;

                                            // チップの入力グループ種別が入力の入力グループ種別集合に含まれていないなら無視。
                                            if( !入力のヒット判定対象となる入力グループ種別集合.Any( ( type ) => ( type == chipProperty.入力グループ種別 ) ) )
                                                return false;
                                        }
                                        else
                                        {
                                            return false;   // 無視。
                                        }
                                    }

                                    // ここまで到達できれば、チップは入力にヒットしている。
                                    return true;
                                    //----------------
                                    #endregion

                                } );

                            if( null != chip )   // あった
                            {
                                #region " チップの手動ヒット処理。"
                                //----------------
                                this._チップと判定との距離と時間を計算する(
                                    現在の演奏時刻sec,
                                    chip,
                                    out double ヒット判定バーと描画との時間sec,   // 負数ならバー未達、0でバー直上、正数でバー通過。 
                                    out double ヒット判定バーと発声との時間sec,   //
                                    out double ヒット判定バーとの距離dpx );       //

                                var chipProperty = userConfig.ドラムチッププロパティリスト.チップtoプロパティ[ chip.チップ種別 ];
                                double 入力とチップの時間差sec = 入力.InputEvent.TimeStamp - chip.描画時刻sec;
                                double 入力とチップの時間差の絶対値sec = Math.Abs( 入力とチップの時間差sec );
                                var ヒット判定 =
                                    ( 入力とチップの時間差の絶対値sec <= userConfig.最大ヒット距離sec[ 判定種別.PERFECT ] ) ? 判定種別.PERFECT :
                                    ( 入力とチップの時間差の絶対値sec <= userConfig.最大ヒット距離sec[ 判定種別.GREAT ] ) ? 判定種別.GREAT :
                                    ( 入力とチップの時間差の絶対値sec <= userConfig.最大ヒット距離sec[ 判定種別.GOOD ] ) ? 判定種別.GOOD :
                                    判定種別.OK;

                                this._チップのヒット処理を行う(
                                    chip,
                                    ヒット判定,
                                    chipProperty.AutoPlayOFF_ユーザヒット_再生       // ヒットすれば再生する？
                                        && userConfig.ドラムの音を発声する,          // 　自動演奏チップとは異なり、オプション設定の影響を受ける。
                                    chipProperty.AutoPlayOFF_ユーザヒット_判定,      // ヒットすれば判定する？
                                    chipProperty.AutoPlayOFF_ユーザヒット_非表示,    // ヒットすれば非表示にする？
                                    ヒット判定バーと発声との時間sec,
                                    入力とチップの時間差sec );

                                this.成績.エキサイトゲージを更新する( ヒット判定 );
                                //----------------
                                #endregion

                                入力リスト[ n ].処理済み = true;
                            }
                        }
                        //----------------
                        #endregion
                    }
                }

                if( userConfig.ドラムの音を発声する )
                {
                    foreach( var (入力, 処理済み) in 入力リスト )
                    {
                        if( !処理済み && !this._ユーザヒット対象外である( 入力 ) )
                        {
                            #region " ヒットしなかった入力については空打ちと見なし、空打ち音を再生する。"
                            //----------------
                            // 入力に一番近いチップ（ヒット・未ヒット問わず、入力グループは有効）を検索する。
                            var chip = this._指定された時刻に一番近いチップを返す(
                                入力.InputEvent.TimeStamp,
                                検索開始チップ番号: 0,   // 常に先頭から
                                追加の検索条件: ( chip ) => {

                                    var prop = Global.App.ログオン中のユーザ.ドラムチッププロパティリスト.チップtoプロパティ[ chip.チップ種別 ];
                                    if( prop.ドラム入力種別 != 入力.Type )
                                    {
                                        // 入力グループ判定：
                                        // 1つの入力に対して、種類の異なる複数のチップがヒット判定対象になることができる。
                                        // 例えば、Ride入力は、RideチップとRide_Cupチップのどちらにもヒットすることができる。
                                        var 入力のヒット判定対象となる入力グループ種別集合 =
                                                from kvp in userConfig.ドラムチッププロパティリスト.チップtoプロパティ
                                                where kvp.Value.ドラム入力種別 == 入力.Type
                                                select kvp.Value.入力グループ種別;

                                        // チップの入力グループ種別が入力の入力グループ種別集合に含まれていないなら無視。
                                        if( !入力のヒット判定対象となる入力グループ種別集合.Any( ( type ) => ( type == prop.入力グループ種別 ) ) )
                                            return false;
                                    }
                                    return true;

                                } );

                            if( null != chip )  // あった
                            {
                                var prop = Global.App.ログオン中のユーザ.ドラムチッププロパティリスト[ chip.チップ種別 ];

                                if( 0 == chip.チップサブID )
                                {
                                    // (A) SSTF の場合 → プリセットドラムを鳴らす。
                                    Global.App.ドラムサウンド.再生する( prop.チップ種別, 0, prop.発声前消音, prop.消音グループ種別 );
                                }
                                else
                                {
                                    // (B) DTX他の場合 → チップのWAVを再生する。
                                    Global.App.WAV管理.発声する(
                                        chip.チップサブID,   // zz 番号を示す
                                        prop.発声前消音,
                                        prop.消音グループ種別,
                                        BGM以外も再生する: true,
                                        音量: chip.音量 / (float)SSTF.チップ.最大音量 );
                                }
                            }
                            else
                            {
                                // SSTF, DTX他とも、該当するチップがなかった場合には無音となる。
                            }
                            //----------------
                            #endregion
                        }
                    }
                }
                //----------------
                #endregion

                #region " (3) 入力に応じたハイハットの開閉 "
                //----------------
                foreach( var ev in Global.App.ドラム入力.MidiIns.入力イベントリスト.Where( ( ie ) => ( 255 == ie.Key ) ) )
                    this._ドラムキットとヒットバー.ハイハットの開度を設定する( ev.Velocity );
                //----------------
                #endregion
            }

            #region " (4) その他の操作。"
            //----------------
            {
                var keyboard = Global.App.ドラム入力.Keyboard;

                if( !Global.Options.ビュアーモードである && keyboard.キーが押された( 0, Keys.Escape ) )
                {
                    #region " ESC → キャンセル通知フェーズへ（ビュアーモード時は無効）"
                    //----------------
                    Log.Info( "ESC キーが押されました。演奏を中断します。" );

                    Global.App.WAV管理?.すべての発声を停止する();    // DTXでのBGMサウンドはこっちに含まれる。

                    this.現在のフェーズ = フェーズ.キャンセル通知;
                    //----------------
                    #endregion
                }
                if( keyboard.キーが押された( 0, Keys.Up ) )
                {
                    if( keyboard.キーが押されている( 0, Keys.ShiftKey ) )
                    {
                        #region " Shift+上 → BGMAdjust 増加 "
                        //----------------
                        Global.App.演奏譜面.譜面.BGMAdjust += 10; // ms
                        Global.App.WAV管理?.すべてのBGMの再生位置を移動する( +10.0 / 1000.0 );  // sec
                        this._BGMAdjustをデータベースに保存する( Global.App.演奏譜面.譜面 );
                        //----------------
                        #endregion
                    }
                    else
                    {
                        #region " 上 → 譜面スクロールを加速 "
                        //----------------
                        const double 最大倍率 = 8.0;
                        userConfig.譜面スクロール速度 = Math.Min( userConfig.譜面スクロール速度 + 0.5, 最大倍率 );
                        //----------------
                        #endregion
                    }
                }
                if( keyboard.キーが押された( 0, Keys.Down ) )
                {
                    if( keyboard.キーが押されている( 0, Keys.ShiftKey ) )
                    {
                        #region " Shift+下 → BGMAdjust 減少 "
                        //----------------
                        Global.App.演奏譜面.譜面.BGMAdjust -= 10; // ms
                        Global.App.WAV管理?.すべてのBGMの再生位置を移動する( -10.0 / 1000.0 );  // sec
                        this._BGMAdjustをデータベースに保存する( Global.App.演奏譜面.譜面 );
                        //----------------
                        #endregion
                    }
                    else
                    {
                        #region " 下 → 譜面スクロールを減速 "
                        //----------------
                        const double 最小倍率 = 0.5;
                        userConfig.譜面スクロール速度 = Math.Max( userConfig.譜面スクロール速度 - 0.5, 最小倍率 );
                        //----------------
                        #endregion
                    }
                }
            }
            if( Global.App.ドラム入力.ドラムが入力された( ドラム入力種別.Pause_Resume ) )
            {
                #region " Pause/Resume → 演奏の一時停止・再開 "
                //----------------
                this._演奏を一時停止または再開する();
                //----------------
                #endregion
            }
            if( Global.App.ドラム入力.ドラムが入力された( ドラム入力種別.PlaySpeed_Up ) && 0 <= this._描画開始チップ番号 )
            {
                #region " PageUp → 早送り；1小節先に進む "
                //----------------
                // 現在時刻における小節番号を取得する。
                int 現在の小節番号 = Global.App.演奏スコア.チップリスト[ this._描画開始チップ番号 ].小節番号;
                double 現在時刻sec = Global.App.サウンドタイマ.現在時刻sec;
                for( int i = this._描画開始チップ番号; i < Global.App.演奏スコア.チップリスト.Count; i++ )
                {
                    var chip = Global.App.演奏スコア.チップリスト[ i ];
                    if( chip.チップ種別 == SSTF.チップ種別.小節線 && 現在時刻sec <= chip.描画時刻sec )
                        break;
                    現在の小節番号 = chip.小節番号;
                }

                // 取得した小節の1つ次の小節へ移動する。
                this._指定小節へ移動する( 現在の小節番号 + 1, out this._描画開始チップ番号, out double 演奏開始時刻sec );
                Global.App.サウンドタイマ.リセットする( 演奏開始時刻sec );
                this._クリアメーター.カウントマップをクリアする();

                this._早送りアイコン.早送りする();

                this.成績.無効 = true;
                //----------------
                #endregion
            }
            if( Global.App.ドラム入力.ドラムが入力された( ドラム入力種別.PlaySpeed_Down ) && 0 <= this._描画開始チップ番号 )
            {
                #region " PageDown → 早戻し；1小節前に戻る "
                //----------------
                // こちらは描画開始チップの小節番号を基点とする。
                int 現在の小節番号 = Global.App.演奏スコア.チップリスト[ this._描画開始チップ番号 ].小節番号;

                // 取得した小節の1つ前の小節へ移動する。
                this._指定小節へ移動する( 現在の小節番号 - 1, out this._描画開始チップ番号, out double 演奏開始時刻sec );
                Global.App.サウンドタイマ.リセットする( 演奏開始時刻sec );

                this._クリアメーター.カウントマップをクリアする();
                this._早送りアイコン.早戻しする();

                this.成績.無効 = true;
                //----------------
                #endregion
            }
            //----------------
            #endregion
        }



        // ローカル


        演奏結果 _表示フェーズの結果 = 演奏結果.演奏中;



        // 画面を構成するもの


        private readonly 画像D2D _背景画像;

        private 画像D2D _スコア指定の背景画像;

        private readonly 曲名パネル _曲名パネル;

        private readonly システム情報 _システム情報;

        private readonly レーンフレーム _レーンフレーム;

        private readonly ドラムキットとヒットバー _ドラムキットとヒットバー;

        private readonly ドラムチップ _ドラムチップ;

        private readonly 譜面スクロール速度 _譜面スクロール速度;

        private readonly エキサイトゲージ _エキサイトゲージ;

        private readonly フェーズパネル _フェーズパネル;

        private readonly クリアメーター _クリアメーター;

        private readonly 左サイドクリアパネル _左サイドクリアパネル;

        private readonly 右サイドクリアパネル _右サイドクリアパネル;

        private readonly 早送りアイコン _早送りアイコン;

        private void _演奏パーツなし背景画面を描画する( DeviceContext d2ddc )
        {
            var userConfig = Global.App.ログオン中のユーザ;

            #region " スコア指定の壁紙 "
            //----------------
            if( userConfig.スコア指定の背景画像を表示する )
            {
                this._スコア指定の背景画像?.描画する( d2ddc, 0f, 0f,
                    X方向拡大率: Global.GraphicResources.設計画面サイズ.Width / this._スコア指定の背景画像.サイズ.Width,
                    Y方向拡大率: Global.GraphicResources.設計画面サイズ.Height / this._スコア指定の背景画像.サイズ.Height );
            }
            //----------------
            #endregion

            #region " 左サイドパネルへの描画と、左サイドパネルの表示 "
            //----------------
            {
                var preTarget = d2ddc.Target;
                var preTrans = d2ddc.Transform;
                var preBlend = d2ddc.PrimitiveBlend;

                d2ddc.Target = this._左サイドクリアパネル.クリアパネル.Bitmap;
                d2ddc.Transform = Matrix3x2.Identity;  // 等倍描画(DPXtoDPX)
                d2ddc.PrimitiveBlend = PrimitiveBlend.SourceOver;

                // 背景画像
                d2ddc.Clear( new Color4( Color3.Black, 0f ) );
                d2ddc.DrawBitmap( this._左サイドクリアパネル.背景.Bitmap, opacity: 1f, interpolationMode: InterpolationMode.Linear );

                // プレイヤー名
                this._プレイヤー名表示.進行描画する( d2ddc );

                // スコア
                if( userConfig.ダーク == ダーク種別.OFF )
                    this._スコア表示.進行描画する( d2ddc, Global.Animation, new Vector2( +280f, +120f ), this.成績 );

                // 達成率
                this._達成率表示.進行描画する( d2ddc, (float)this.成績.Achievement );

                // 判定パラメータ
                this._判定パラメータ表示.進行描画する( d2ddc, +118f, +372f, this.成績 );

                // スキル
                this._曲別SKILL.進行描画する( d2ddc, 0f );

                d2ddc.Flush(); // いったんここまで描画

                d2ddc.Target = preTarget;
                d2ddc.Transform = preTrans;
                d2ddc.PrimitiveBlend = preBlend;

                ( (IUnknown)preTarget ).Release(); // 要Release
            }

            this._左サイドクリアパネル.進行描画する();
            //----------------
            #endregion

            #region " 右サイドパネルへの描画と、右サイトパネルの表示 "
            //----------------
            {
                var preTarget = d2ddc.Target;
                var preTrans = d2ddc.Transform;
                var preBlend = d2ddc.PrimitiveBlend;

                d2ddc.Target = this._右サイドクリアパネル.クリアパネル.Bitmap;
                d2ddc.Transform = Matrix3x2.Identity;  // 等倍描画(DPXtoDPX)
                d2ddc.PrimitiveBlend = PrimitiveBlend.SourceOver;

                // 背景画像
                d2ddc.Clear( new Color4( Color3.Black, 0f ) );
                d2ddc.DrawBitmap( this._右サイドクリアパネル.背景.Bitmap, opacity: 1f, interpolationMode: InterpolationMode.Linear );

                d2ddc.Flush(); // いったんここまで描画

                d2ddc.Target = preTarget;
                d2ddc.Transform = preTrans;
                d2ddc.PrimitiveBlend = preBlend;

                ( (IUnknown)preTarget ).Release(); // 要Release
            }

            this._右サイドクリアパネル.進行描画する();
            //----------------
            #endregion

            #region " レーンフレーム "
            //----------------
            this._レーンフレーム.進行描画する( d2ddc, userConfig.レーンの透明度, レーンラインを描画する: ( userConfig.ダーク == ダーク種別.OFF ) );
            //----------------
            #endregion

            #region " 背景画像 "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._背景画像.描画する( d2ddc, 0f, 0f );
            //----------------
            #endregion

            #region " 譜面スクロール速度 "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._譜面スクロール速度.描画する( d2ddc, userConfig.譜面スクロール速度 );
            //----------------
            #endregion

            #region " エキサイトゲージ "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._エキサイトゲージ.進行描画する( d2ddc, this.成績.エキサイトゲージ量 );
            //----------------
            #endregion

            #region " クリアメーター "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._クリアメーター.進行描画する( d2ddc );
            //----------------
            #endregion

            #region " フェーズパネル "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._フェーズパネル.進行描画する( d2ddc );
            //----------------
            #endregion

            #region " 曲目パネル "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._曲名パネル.進行描画する( d2ddc );
            //----------------
            #endregion

            #region " ヒットバー "
            //----------------
            if( userConfig.ダーク != ダーク種別.FULL )
                this._ドラムキットとヒットバー.ヒットバーを進行描画する( d2ddc );
            //----------------
            #endregion

            #region " ドラムキット "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._ドラムキットとヒットバー.ドラムキットを進行描画する( d2ddc );
            //----------------
            #endregion
        }

        private 演奏結果 _演奏パーツあり背景画面を描画する( DeviceContext d2ddc )
        {
            var result = 演奏結果.演奏中;
            var userConfig = Global.App.ログオン中のユーザ;

            double 演奏時刻sec = Global.App.サウンドタイマ.現在時刻sec;
            if( Global.Options.VSyncWait )
                演奏時刻sec += Global.GraphicResources.次のDComp表示までの残り時間sec;

            #region " 譜面スクロールの進行 "
            //----------------
            // チップの表示より前に進行だけ行う。
            this._譜面スクロール速度.進行する( userConfig.譜面スクロール速度 );
            //----------------
            #endregion

            #region " スコア指定の壁紙 "
            //----------------
            if( userConfig.スコア指定の背景画像を表示する )
            {
                this._スコア指定の背景画像?.描画する( d2ddc, 0f, 0f,
                    X方向拡大率: Global.GraphicResources.設計画面サイズ.Width / this._スコア指定の背景画像.サイズ.Width,
                    Y方向拡大率: Global.GraphicResources.設計画面サイズ.Height / this._スコア指定の背景画像.サイズ.Height );
            }
            //----------------
            #endregion

            #region " 動画 "
            //----------------
            if( userConfig.演奏中に動画を表示する )
            {
                foreach( var kvp in Global.App.AVI管理.動画リスト )
                {
                    int zz = kvp.Key;       // zz 番号
                    var video = kvp.Value;  // Video インスタンス

                    if( video.再生中 )
                    {
                        switch( userConfig.動画の表示サイズ )
                        {
                            case 動画の表示サイズ.全画面:
                            {
                                #region " 100%全体表示 "
                                //----------------
                                float w = Global.GraphicResources.設計画面サイズ.Width;
                                float h = Global.GraphicResources.設計画面サイズ.Height;
                                video.描画する( d2ddc, new RectangleF( 0f, 0f, w, h ) );
                                //----------------
                                #endregion

                                break;
                            }

                            case 動画の表示サイズ.中央寄せ:
                            {
                                #region " 75%縮小表示 "
                                //----------------
                                float w = Global.GraphicResources.設計画面サイズ.Width;
                                float h = Global.GraphicResources.設計画面サイズ.Height;

                                // (1) 画面いっぱいに描画。
                                video.描画する( d2ddc, new RectangleF( 0f, 0f, w, h ), 0.2f );    // 不透明度は 0.2 で暗くする。

                                // (2) ちょっと縮小して描画。
                                float 拡大縮小率 = 0.75f;
                                float 上移動 = 100.0f;
                                video.最後のフレームを再描画する( d2ddc, new RectangleF(   // 直前に取得したフレームをそのまま描画。
                                    w * ( 1f - 拡大縮小率 ) / 2f,
                                    h * ( 1f - 拡大縮小率 ) / 2f - 上移動,
                                    w * 拡大縮小率,
                                    h * 拡大縮小率 ) );
                                //----------------
                                #endregion

                                break;
                            }
                        }
                    }
                }
            }
            //----------------
            #endregion

            #region " 左サイドパネルへの描画と、左サイドパネルの表示 "
            //----------------
            {
                var preTarget = d2ddc.Target;
                var preTrans = d2ddc.Transform;
                var preBlend = d2ddc.PrimitiveBlend;

                d2ddc.Target = this._左サイドクリアパネル.クリアパネル.Bitmap;
                d2ddc.Transform = Matrix3x2.Identity;  // 等倍描画(DPXtoDPX)
                d2ddc.PrimitiveBlend = PrimitiveBlend.SourceOver;

                // 背景画像
                d2ddc.Clear( new Color4( Color3.Black, 0f ) );
                d2ddc.DrawBitmap( this._左サイドクリアパネル.背景.Bitmap, opacity: 1f, interpolationMode: InterpolationMode.Linear );

                // プレイヤー名
                this._プレイヤー名表示.進行描画する( d2ddc );

                // スコア
                if( userConfig.ダーク == ダーク種別.OFF )
                    this._スコア表示.進行描画する( d2ddc, Global.Animation, new Vector2( +280f, +120f ), this.成績 );

                // 達成率
                this._達成率表示.進行描画する( d2ddc, (float)this.成績.Achievement );

                // 判定パラメータ
                this._判定パラメータ表示.進行描画する( d2ddc, +118f, +372f, this.成績 );

                // スキル
                this._曲別SKILL.進行描画する( d2ddc, this.成績.スキル );

                d2ddc.Flush(); // いったんここまで描画。

                d2ddc.Target = preTarget;
                d2ddc.Transform = preTrans;
                d2ddc.PrimitiveBlend = preBlend;

                ( (IUnknown)preTarget ).Release(); // 要Release
            }

            this._左サイドクリアパネル.進行描画する();
            //----------------
            #endregion

            #region " 右サイドパネルへの描画と、右サイドパネルの表示 "
            //----------------
            {
                var preTarget = d2ddc.Target;
                var preTrans = d2ddc.Transform;
                var preBlend = d2ddc.PrimitiveBlend;

                d2ddc.Target = this._右サイドクリアパネル.クリアパネル.Bitmap;
                d2ddc.Transform = Matrix3x2.Identity;  // 等倍描画(DPXtoDPX)
                d2ddc.PrimitiveBlend = PrimitiveBlend.SourceOver;

                // 背景画像
                d2ddc.Clear( new Color4( Color3.Black, 0f ) );
                d2ddc.DrawBitmap( this._右サイドクリアパネル.背景.Bitmap, opacity: 1f, interpolationMode: InterpolationMode.Linear );

                // コンボ
                this._コンボ表示.進行描画する( d2ddc, new Vector2( +228f + 264f / 2f, +234f ), this.成績 );

                d2ddc.Flush(); // いったんここまで描画。

                d2ddc.Target = preTarget;
                d2ddc.Transform = preTrans;
                d2ddc.PrimitiveBlend = preBlend;

                ( (IUnknown)preTarget ).Release(); // 要Release
            }

            this._右サイドクリアパネル.進行描画する();
            //----------------
            #endregion

            #region " レーンフレーム "
            //----------------
            this._レーンフレーム.進行描画する( d2ddc, userConfig.レーンの透明度, レーンラインを描画する: ( userConfig.ダーク == ダーク種別.OFF ) );
            //----------------
            #endregion

            #region " レーンフラッシュ  "
            //----------------
            this._レーンフラッシュ.進行描画する( d2ddc );
            //----------------
            #endregion

            #region " 小節線拍線 "
            //----------------
            if( userConfig.ダーク != ダーク種別.FULL )
                this._小節線拍線を描画する( d2ddc, 演奏時刻sec );
            //----------------
            #endregion

            #region " 背景画像 "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._背景画像.描画する( d2ddc, 0f, 0f );
            //----------------
            #endregion

            #region " 譜面スクロール速度 "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._譜面スクロール速度.描画する( d2ddc, userConfig.譜面スクロール速度 );
            //----------------
            #endregion

            #region " エキサイトゲージ "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._エキサイトゲージ.進行描画する( d2ddc, this.成績.エキサイトゲージ量 );
            //----------------
            #endregion

            #region " クリアメーター, フェーズパネル "
            //----------------
            double 曲の長さsec = Global.App.演奏スコア.チップリスト[ Global.App.演奏スコア.チップリスト.Count - 1 ].描画時刻sec;
            float 現在位置 = (float)( 1.0 - ( 曲の長さsec - 演奏時刻sec ) / 曲の長さsec );

            // クリアメーター
            this.成績.CountMap = this._クリアメーター.カウント値を設定する( 現在位置, this.成績.判定別ヒット数 );
            if( userConfig.ダーク == ダーク種別.OFF )
                this._クリアメーター.進行描画する( d2ddc );

            // フェーズパネル
            this._フェーズパネル.現在位置 = 現在位置;
            if( userConfig.ダーク == ダーク種別.OFF )
                this._フェーズパネル.進行描画する( d2ddc );
            //----------------
            #endregion

            #region " 曲名パネル "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._曲名パネル.進行描画する( d2ddc );
            //----------------
            #endregion

            #region " ヒットバー "
            //----------------
            if( userConfig.ダーク != ダーク種別.FULL )
                this._ドラムキットとヒットバー.ヒットバーを進行描画する( d2ddc );
            //----------------
            #endregion

            #region " ドラムキット "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._ドラムキットとヒットバー.ドラムキットを進行描画する( d2ddc );
            //----------------
            #endregion

            // ↓クリア判定はこの中。
            #region " ドラムチップ "
            //----------------
            this._描画範囲内のすべてのチップに対して( 演奏時刻sec, ( SSTF.チップ chip, int index, double ヒット判定バーと描画との時間sec, double ヒット判定バーと発声との時間sec, double ヒット判定バーとの距離dpx ) => {

                result = this._ドラムチップ.進行描画する( d2ddc, ref this._描画開始チップ番号, this._チップの演奏状態[ chip ], chip, index, ヒット判定バーとの距離dpx );

            } );
            //----------------
            #endregion

            #region " チップ光 "
            //----------------
            this._チップ光.進行描画する( d2ddc );
            //----------------
            #endregion

            #region " 判定文字列 "
            //----------------
            this._判定文字列.進行描画する( d2ddc );
            //----------------
            #endregion

            #region " 早送りアイコン "
            //----------------
            this._早送りアイコン.進行描画する( d2ddc );
            //----------------
            #endregion

            #region " システム情報 "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._システム情報.描画する( d2ddc, $"BGMAdjust: {Global.App.演奏譜面.譜面.BGMAdjust}" );
            //----------------
            #endregion

            return result;
        }

        private void _キャプチャ画面を描画する( DeviceContext d2ddc, float 不透明度 = 1.0f )
        {
            Debug.Assert( null != this.キャプチャ画面, "キャプチャ画面が設定されていません。" );

            d2ddc.DrawBitmap(
                this.キャプチャ画面,
                new RectangleF( 0f, 0f, Global.GraphicResources.設計画面サイズ.Width, Global.GraphicResources.設計画面サイズ.Height ),
                不透明度,
                BitmapInterpolationMode.Linear );
        }



        // 左サイドクリアパネル内に表示されるもの


        private readonly スコア表示 _スコア表示;

        private readonly プレイヤー名表示 _プレイヤー名表示;

        private readonly 判定パラメータ表示 _判定パラメータ表示;

        private readonly 達成率表示 _達成率表示;

        private readonly 曲別SKILL _曲別SKILL;



        // 右サイドクリアパネル内に表示されるもの


        private readonly コンボ表示 _コンボ表示;



        // 譜面上に表示されるもの


        private readonly レーンフラッシュ _レーンフラッシュ;

        private readonly 判定文字列 _判定文字列;

        private readonly チップ光 _チップ光;

        private readonly フォント画像D2D _数字フォント中グレー48x64;

        private readonly SolidColorBrush _小節線色;

        private readonly SolidColorBrush _拍線色;

        private void _小節線拍線を描画する( DeviceContext d2ddc, double 現在の演奏時刻sec )
        {
            // 小節線・拍線 と チップ は描画階層（奥行き）が異なるので、別々のメソッドに分ける。

            var userConfig = Global.App.ログオン中のユーザ;

            this._描画範囲内のすべてのチップに対して( 現在の演奏時刻sec, ( chip, index, ヒット判定バーと描画との時間sec, ヒット判定バーと発声との時間sec, ヒット判定バーとの距離dpx ) => {

                if( chip.チップ種別 == SSTF.チップ種別.小節線 )
                {
                    const float 小節線の厚みの半分 = 1f;
                    float 上位置dpx = MathF.Round( (float)( ヒット判定位置Ydpx + ヒット判定バーとの距離dpx - 小節線の厚みの半分 ) );

                    // 小節線
                    if( userConfig.演奏中に小節線と拍線を表示する )
                    {
                        const float x = 441f;
                        const float w = 780f;
                        d2ddc.DrawLine( new Vector2( x, 上位置dpx + 0f ), new Vector2( x + w, 上位置dpx + 0f ), this._小節線色 );
                        d2ddc.DrawLine( new Vector2( x, 上位置dpx + 1f ), new Vector2( x + w, 上位置dpx + 1f ), this._小節線色 );
                    }

                    // 小節番号
                    if( userConfig.演奏中に小節番号を表示する )
                    {
                        const float 右位置dpx = 441f + 780f - 24f;   // -24f は適当なマージン。
                        this._数字フォント中グレー48x64.描画する( d2ddc, 右位置dpx, 上位置dpx - 84f, chip.小節番号.ToString(), 右揃え: true );    // -84f は適当なマージン。
                    }
                }
                else if( chip.チップ種別 == SSTF.チップ種別.拍線 )
                {
                    // 拍線
                    if( userConfig.演奏中に小節線と拍線を表示する )
                    {
                        const float 拍線の厚みの半分 = 1f;
                        float 上位置dpx = MathF.Round( (float)( ヒット判定位置Ydpx + ヒット判定バーとの距離dpx - 拍線の厚みの半分 ) );
                        d2ddc.DrawLine( new Vector2( 441f, 上位置dpx ), new Vector2( 441f + 780f, 上位置dpx ), this._拍線色 );
                    }
                }

            } );
        }



        // 演奏状態、操作


        /// <summary>
        ///		<see cref="スコア.チップリスト"/> のうち、描画を始めるチップのインデックス番号。
        ///		未演奏時・演奏終了時は -1 。
        /// </summary>
        /// <remarks>
        ///		演奏開始直後は 0 で始まり、対象番号のチップが描画範囲を流れ去るたびに +1 される。
        ///		このメンバの更新は、高頻度進行タスクではなく、進行描画メソッドで行う。（低精度で構わないので）
        /// </remarks>
        private int _描画開始チップ番号 = -1;

        private Dictionary<SSTF.チップ, チップの演奏状態> _チップの演奏状態;

        private bool _一時停止中 = false;


        private void _演奏を一時停止または再開する()
        {
            if( !this._一時停止中 )
            {
                this._一時停止中 = true;

                Global.App.サウンドタイマ.一時停止する();

                Global.App.AVI管理.再生中の動画をすべて一時停止する();
                Global.App.WAV管理.再生中の音声をすべて一時停止する();
            }
            else
            {
                this._一時停止中 = false;

                Global.App.AVI管理.一時停止中の動画をすべて再開する();
                Global.App.WAV管理.一時停止中の音声をすべて再開する();

                Global.App.サウンドタイマ.再開する();
            }
        }

        private void _BGMAdjustをデータベースに保存する( ScoreDBRecord 譜面レコード )
        {
            Task.Run( () => {
                using( var scoredb = new ScoreDB() )
                    譜面レコード.ReplaceTo( scoredb );
            } );
        }

        /// <summary>
        ///     指定された小節に演奏箇所を移動する。
        /// </summary>
        /// <param name="移動先小節番号">移動先の小節番号。</param>
        /// <param name="描画開始チップ番号">移動先に位置する先頭のチップが返される。なければ 0 。</param>
        /// <param name="演奏開始時刻sec">移動先に移動した場合の演奏開始時刻[秒]。なければ 0.0 。</param>
        /// <remarks>
        ///     指定された小節に演奏箇所を移動し、新しい描画開始チップ番号と、新しい演奏開始時刻secを返す。
        ///     また、新しい演奏開始時刻sec より前のAVI/WAVチップについては、途中からの再生を行う。
        /// </remarks>
        private void _指定小節へ移動する( int 移動先小節番号, out int 描画開始チップ番号, out double 演奏開始時刻sec )
        {
            var score = Global.App.演奏スコア;

            // AVI動画、WAV音声を停止する。
            Global.App.AVI管理.再生中の動画をすべて一時停止する();
            Global.App.WAV管理.再生中の音声をすべて一時停止する();

            // 一度、すべてのチップを未ヒット状態に戻す。
            foreach( var chip in score.チップリスト )
                this._チップの演奏状態[ chip ].ヒット前の状態にする();

            // 移動先の小節番号が負数なら、一番最初へ移動。
            if( 0 > 移動先小節番号 )
            {
                描画開始チップ番号 = 0;
                演奏開始時刻sec = 0.0;
                return;
            }

            // 移動先小節番号に位置している最初のチップを検索する。
            int 最初のチップ番号 = score.チップリスト.FindIndex( ( c ) => c.小節番号 >= 移動先小節番号 );    // 見つからなければ -1。

            if( -1 == 最初のチップ番号 )
            {
                // すべて演奏終了しているので、最後のチップを指定する。
                描画開始チップ番号 = score.チップリスト.Count - 1;
                演奏開始時刻sec = score.チップリスト.Last().発声時刻sec;
                return;
            }

            // 演奏開始時刻を、最初のチップの描画より少し手前に設定。
            演奏開始時刻sec = Math.Max( 0.0, score.チップリスト[ 最初のチップ番号 ].描画時刻sec - 0.5 );   // 0.5 秒早く

            // 演奏開始時刻をもとに描画開始チップ番号を確定するとともに、全チップのヒット状態をリセットする。
            描画開始チップ番号 = -1;
            for( int i = 0; i < score.チップリスト.Count; i++ )
            {
                var chip = score.チップリスト[ i ];

                if( chip.描画時刻sec >= 演奏開始時刻sec )
                {
                    // (A) 演奏開始時刻以降のチップ
                    if( -1 == 描画開始チップ番号 )
                        描画開始チップ番号 = i;  // 先頭チップ
                    break;
                }
                else
                {
                    // (B) 演奏開始時刻より前のチップ → ヒット済みとする。
                    this._チップの演奏状態[ chip ].ヒット済みの状態にする();
                }
            }

            // 必要なら、AVI動画, WAV音声の途中再生を行う。

            #region " AVI動画の途中再生 "
            //----------------
            if( Global.App.ログオン中のユーザ.演奏中に動画を表示する )
            {
                var ヒット済みの動画チップリスト =
                    from chip in score.チップリスト
                    where chip.チップ種別 == SSTF.チップ種別.背景動画 //&& this._チップの演奏状態[ chip ].ヒット済みである    --> 未ヒットも再構築する
                    select chip;

                foreach( var aviChip in ヒット済みの動画チップリスト )
                {
                    int avi番号 = aviChip.チップサブID;

                    if( Global.App.AVI管理.動画リスト.ContainsKey( avi番号 ) )
                    {
                        Global.App.AVI管理.再構築する( avi番号 );

                        double 再生開始時刻sec = 演奏開始時刻sec - aviChip.発声時刻sec;

                        if( 0 <= 再生開始時刻sec )    // 念のため。
                            Global.App.AVI管理.動画リスト[ avi番号 ]?.再生を開始する( 再生開始時刻sec );
                    }
                }
            }
            //----------------
            #endregion

            #region " WAV音声の途中再生 "
            //----------------
            {
                var ヒット済みのWAVチップリスト =
                    from chip in score.チップリスト
                    where ( chip.チップ種別 == SSTF.チップ種別.BGM ) && ( this._チップの演奏状態[ chip ].ヒット済みである )
                    select chip;

                foreach( var wavChip in ヒット済みのWAVチップリスト )
                {
                    var prop = Global.App.ログオン中のユーザ.ドラムチッププロパティリスト.チップtoプロパティ[ wavChip.チップ種別 ];

                    Global.App.WAV管理.発声する(
                        wavChip.チップサブID,
                        prop.発声前消音,
                        prop.消音グループ種別,
                        BGM以外も再生する:
                            Global.App.ログオン中のユーザ.ドラムの音を発声する &&
                            Global.Options.ビュアーモードである &&
                            ビュアーモードでドラム音を再生する,
                        音量: wavChip.音量 / (float)SSTF.チップ.最大音量,
                        再生開始時刻sec: 演奏開始時刻sec - wavChip.発声時刻sec );
                }
            }
            //----------------
            #endregion
        }

        private void _演奏状態を初期化する()
        {
            this._描画開始チップ番号 = -1;

            // スコアに依存するデータを初期化する。

            this.成績 = new 成績();

            this._チップの演奏状態 = new Dictionary<SSTF.チップ, チップの演奏状態>();
            foreach( var chip in Global.App.演奏スコア.チップリスト )
                this._チップの演奏状態.Add( chip, new チップの演奏状態( chip ) );

            this._スコア指定の背景画像 = string.IsNullOrEmpty( Global.App.演奏スコア.背景画像ファイル名 ) ?
                null! :
                new 画像D2D( Path.Combine( Global.App.演奏スコア.PATH_WAV, Global.App.演奏スコア.背景画像ファイル名 ) );    // PATH_WAV は絶対パス
        }



        // フェードイン（キャプチャ画像とステージ画像とのA-B変換）→ 既定のアイキャッチを使わないので独自管理する。


        /// <summary>
        ///		読み込み画面: 0 ～ 1: 演奏画面
        /// </summary>
        private Counter _フェードインカウンタ;



        // ドラムチップ関連


        /// <summary>
        ///		現在の描画範囲内のすべてのチップに対して、指定された処理を実行する。
        /// </summary>
        /// <remarks>
        ///		「描画範囲内のチップ」とは、<see cref="_描画開始チップ番号"/> のチップを画面最下位のチップとし、画面上端にはみ出すまでの間のすべてのチップを示す。
        /// </remarks>
        /// <param name="適用する処理">
        ///		引数は、順に、対象のチップ, チップ番号, ヒット判定バーと描画との時間sec, ヒット判定バーと発声との時間sec, ヒット判定バーとの距離dpx。
        ///		時間と距離はいずれも、負数ならバー未達、0でバー直上、正数でバー通過。
        ///	</param>
        private void _描画範囲内のすべてのチップに対して( double 現在の演奏時刻sec, Action<SSTF.チップ, int, double, double, double> 適用する処理 )
        {
            var score = Global.App.演奏スコア;

            // 描画開始チップから後方（画面最下位のチップから画面上方）のチップに向かって……
            for( int i = this._描画開始チップ番号; ( 0 <= i ) && ( i < score.チップリスト.Count ); i++ )
            {
                var chip = score.チップリスト[ i ];

                this._チップと判定との距離と時間を計算する(
                    現在の演奏時刻sec,
                    chip,
                    out double ヒット判定バーと描画との時間sec,   // 負数ならバー未達、0でバー直上、正数でバー通過。 
                    out double ヒット判定バーと発声との時間sec,   //
                    out double ヒット判定バーとの距離dpx );       //

                // チップが画面外に出たならここで終了。
                if( this._チップは画面上端より外に出ている( ヒット判定バーとの距離dpx ) )
                    break;

                // 適用する処理を呼び出す。開始判定（描画開始チップ番号の更新）もこの中で。
                適用する処理( chip, i, ヒット判定バーと描画との時間sec, ヒット判定バーと発声との時間sec, ヒット判定バーとの距離dpx );
            }
        }

        /// <summary>
        ///     指定された時刻における、チップとヒット判定バーとの間の距離と時間を算出する。
        /// </summary>
        /// <param name="現在の演奏時刻sec"></param>
        /// <param name="chip"></param>
        /// <param name="ヒット判定バーと描画との時間sec">負数ならバー未達、0でバー直上、正数でバー通過。 </param>
        /// <param name="ヒット判定バーと発声との時間sec">負数ならバー未達、0でバー直上、正数でバー通過。 </param>
        /// <param name="ヒット判定バーとの距離dpx">負数ならバー未達、0でバー直上、正数でバー通過。 </param>
        private void _チップと判定との距離と時間を計算する( double 現在の演奏時刻sec, SSTF.チップ chip, out double ヒット判定バーと描画との時間sec, out double ヒット判定バーと発声との時間sec, out double ヒット判定バーとの距離dpx )
        {
            const double _1秒あたりのピクセル数 = 0.14625 * 2.25 * 1000.0;    // これを変えると、speed あたりの速度が変わる。

            ヒット判定バーと描画との時間sec = 現在の演奏時刻sec - chip.描画時刻sec;
            ヒット判定バーと発声との時間sec = 現在の演奏時刻sec - chip.発声時刻sec;
            ヒット判定バーとの距離dpx = ヒット判定バーと描画との時間sec * _1秒あたりのピクセル数 * this._譜面スクロール速度.補間付き速度;
        }

        /// <summary>
        ///     チップとヒット判定バーとの距離をもとに、チップが画面上端よりも上に（画面外に）出ているかを確認する。
        /// </summary>
        /// <returns>チップが画面上端よりも上に（画面外に）でていれば true を返す。</returns>
        private bool _チップは画面上端より外に出ている( double ヒット判定バーとの距離dpx )
        {
            const double チップが隠れるであろう適当なマージン = -40.0;
            return ( 演奏ステージ.ヒット判定位置Ydpx + ヒット判定バーとの距離dpx ) < チップが隠れるであろう適当なマージン;
        }

        /// <summary>
        ///     入力にヒットしたチップの処理（再生、判定、非表示化）を行う。
        /// </summary>
        /// <param name="chip"></param>
        /// <param name="judge">ヒットしたチップの<see cref="判定種別"/>。</param>
        /// <param name="再生">チップを再生するならtrue。</param>
        /// <param name="判定">チップを判定するならtrue。</param>
        /// <param name="非表示">チップを非表示化するならtrue。</param>
        /// <param name="ヒット判定バーと発声との時間sec">負数でバー未達、0で直上、正数でバー通過。</param>
        /// <param name="入力とチップとの間隔sec">チップより入力が早ければ負数、遅ければ正数。</param>
        private void _チップのヒット処理を行う( SSTF.チップ chip, 判定種別 judge, bool 再生, bool 判定, bool 非表示, double ヒット判定バーと発声との時間sec, double 入力とチップとの間隔sec )
        {
            this._チップの演奏状態[ chip ].ヒット済みである = true;

            if( 再生 && ( judge != 判定種別.MISS ) )
            {
                #region " チップの発声がまだなら行う。"
                //----------------
                // チップの発声時刻は、描画時刻と同じかそれより過去に位置するので、ここに来た時点で未発声なら発声していい。
                // というか発声時刻が過去なのに未発声というならここが最後のチャンスなので、必ず発声しないといけない。
                if( !this._チップの演奏状態[ chip ].発声済みである )
                {
                    this._チップの再生を行う( chip, Global.App.ログオン中のユーザ.ドラムの音を発声する );
                    this._チップの演奏状態[ chip ].発声済みである = true;
                }
                //----------------
                #endregion
            }
            if( 判定 )
            {
                #region " チップの判定処理を行う。"
                //----------------
                var userConfig = Global.App.ログオン中のユーザ;
                var 対応表 = userConfig.ドラムチッププロパティリスト[ chip.チップ種別 ];

                if( judge != 判定種別.MISS )
                {
                    this._チップ光.表示を開始する( 対応表.表示レーン種別 );
                    this._レーンフラッシュ.開始する( 対応表.表示レーン種別 );
                }
                this._判定文字列.表示を開始する( 対応表.表示レーン種別, judge, 入力とチップとの間隔sec );

                this.成績.成績を更新する( judge );
                //----------------
                #endregion
            }
            if( 非表示 )
            {
                #region " チップを非表示にする。"
                //----------------
                if( judge == 判定種別.MISS )
                {
                    // (A) MISSチップ → 最後まで表示し続ける。
                }
                else
                {
                    // (B) MISS以外 → 非表示。
                    this._チップの演奏状態[ chip ].可視 = false;
                }
                //----------------
                #endregion
            }
        }

        private void _チップの再生を行う( SSTF.チップ chip, bool ドラムサウンドを再生する )
        {
            var userConfig = Global.App.ログオン中のユーザ;

            if( chip.チップ種別 == SSTF.チップ種別.背景動画 )
            {
                #region " (A) 背景動画チップ → AVI動画を再生する。"
                //----------------
                if( userConfig.演奏中に動画を表示する )
                {
                    int AVI番号 = chip.チップサブID;

                    if( Global.App.AVI管理.動画リスト.TryGetValue( AVI番号, out Video? video ) )
                    {
                        // 止めても止めなくてもカクつくだろうが、止めておけば譜面は再開時にワープしない。
                        // ---> 2020/10/22: やっぱりコメントアウト
                        //      BGM 開始後に動画を開始する場合、ここでタイマを止めると、BGMと譜面で無視できない大きさの誤差が発生する。
                        //      なので、ここでタイマを止めてはならない（ワープも致し方ない）。
                        //Global.App.サウンドタイマ.一時停止する();
                        video.再生を開始する();
                        //Global.App.サウンドタイマ.再開する();
                    }
                }
                //----------------
                #endregion
            }
            else
            {
                if( Global.Options.ビュアーモードである &&
                    !演奏ステージ.ビュアーモードでドラム音を再生する &&
                    chip.チップ種別 != SSTF.チップ種別.BGM )   // BGM はドラムサウンドではない
                    return;

                var chipProperty = userConfig.ドラムチッププロパティリスト.チップtoプロパティ[ chip.チップ種別 ];

                if( 0 == chip.チップサブID && ドラムサウンドを再生する )
                {
                    #region " (B) チップサブIDがゼロ → SSTF準拠のドラムサウンドを再生する。"
                    //----------------
                    // ドラムサウンドを持つチップなら発声する。（持つかどうかはこのメソッド↓内で判定される。）
                    Global.App.ドラムサウンド.再生する(
                        chip.チップ種別,
                        0,
                        chipProperty.発声前消音,
                        chipProperty.消音グループ種別,
                        ( chip.音量 / (float)SSTF.チップ.最大音量 ) );
                    //----------------
                    #endregion
                }
                else
                {
                    #region " (C) チップサブIDがある → DTX準拠のWAVサウンドを再生する。"
                    //----------------
                    // WAVを持つチップなら発声する。（WAVを持つかどうかはこのメソッド↓内で判定される。）
                    Global.App.WAV管理.発声する(
                        chip.
                        チップサブID,
                        chipProperty.発声前消音,
                        chipProperty.消音グループ種別,
                        ドラムサウンドを再生する,
                        音量: ( chip.音量 / (float)SSTF.チップ.最大音量 ) );
                    //----------------
                    #endregion
                }
            }
        }

        /// <summary>
        ///     該当するチップが1つもなかったら null を返す。
        /// </summary>
        private SSTF.チップ? _指定された時刻に一番近いチップを返す( double 時刻sec, int 検索開始チップ番号, Func<SSTF.チップ, bool> 追加の検索条件 )
        {
            if( 0 > 検索開始チップ番号 )
                return null;    // 演奏が完全に終わっていたらチップも返さない。

            var 一番近いチップ = (SSTF.チップ?)null;
            var 一番近いチップの時刻差の絶対値sec = (double)0.0;

            // すべてのチップについて、描画時刻の早い順に調べていく。
            for( int i = 検索開始チップ番号; i < Global.App.演奏スコア.チップリスト.Count; i++ )
            {
                var chip = Global.App.演奏スコア.チップリスト[ i ];

                if( !追加の検索条件( chip ) )
                    continue;   // 検索条件を満たさないチップは無視

                var 今回の時刻差の絶対値sec = Math.Abs( chip.描画時刻sec - 時刻sec );

                if( null != 一番近いチップ &&
                    一番近いチップの時刻差の絶対値sec < 今回の時刻差の絶対値sec )
                {
                    // 時刻差の絶対値が前回より増えた → 前回のチップが指定時刻への再接近だった
                    break;
                }

                // 更新して次へ
                一番近いチップ = chip;
                一番近いチップの時刻差の絶対値sec = 今回の時刻差の絶対値sec;
            }

            return 一番近いチップ;
        }

        private 表示レーン種別 _入力に対応する表示レーン種別を返す( ドラム入力イベント 入力 )
        {
            var 表示レーン種別集合 =
                from kvp in Global.App.ログオン中のユーザ.ドラムチッププロパティリスト.チップtoプロパティ
                where kvp.Value.ドラム入力種別 == 入力.Type
                select kvp.Value.表示レーン種別;

            return ( 0 < 表示レーン種別集合.Count() ) ? 表示レーン種別集合.First() : 表示レーン種別.Unknown;
        }

        private bool _ユーザヒット対象外である( ドラム入力イベント 入力 )
        {
            return
                !入力.InputEvent.押された ||                  // 押下以外は対象外
                入力.InputEvent.Control != 0 ||               // コントロールチェンジは対象外
                入力.Type == ドラム入力種別.HiHat_Control ||  // ハイハットコントロールは対象外
                入力.Type == ドラム入力種別.Unknown;          // 未知の入力は対象外
        }



        // ビュアーモード


        private readonly LoadingSpinner _LoadingSpinner;

        private Task _曲読み込みタスク = null!;

        private void _ビュアーモードの待機時背景を描画する( DeviceContext d2ddc )
        {
            var userConfig = Global.App.ログオン中のユーザ;

            #region " 左サイドパネルへの描画と、左サイドパネルの表示 "
            //----------------
            {
                var preTarget = d2ddc.Target;
                var preTrans = d2ddc.Transform;
                var preBlend = d2ddc.PrimitiveBlend;

                d2ddc.Target = this._左サイドクリアパネル.クリアパネル.Bitmap;
                d2ddc.Transform = Matrix3x2.Identity;  // 等倍描画(DPXtoDPX)
                d2ddc.PrimitiveBlend = PrimitiveBlend.SourceOver;

                // 背景画像
                d2ddc.Clear( new Color4( Color3.Black, 0f ) );
                d2ddc.DrawBitmap( this._左サイドクリアパネル.背景.Bitmap, opacity: 1f, interpolationMode: InterpolationMode.Linear );

                // プレイヤー名
                this._プレイヤー名表示.進行描画する( d2ddc );

                // スコア
                if( userConfig.ダーク == ダーク種別.OFF )
                    this._スコア表示.進行描画する( d2ddc, Global.Animation, new Vector2( +280f, +120f ), this.成績 );

                // 達成率
                this._達成率表示.進行描画する( d2ddc, (float)this.成績.Achievement );

                // 判定パラメータ
                this._判定パラメータ表示.進行描画する( d2ddc, +118f, +372f, this.成績 );

                // スキル
                this._曲別SKILL.進行描画する( d2ddc, 0f );

                d2ddc.Flush(); // いったんここまで描画。

                d2ddc.Target = preTarget;
                d2ddc.Transform = preTrans;
                d2ddc.PrimitiveBlend = preBlend;

                ( (IUnknown)preTarget ).Release(); // 要Release
            }

            this._左サイドクリアパネル.進行描画する();
            //----------------
            #endregion

            #region " 右サイドパネルへの描画と、右サイトパネルの表示 "
            //----------------
            {
                var preTarget = d2ddc.Target;
                var preTrans = d2ddc.Transform;
                var preBlend = d2ddc.PrimitiveBlend;

                d2ddc.Target = this._右サイドクリアパネル.クリアパネル.Bitmap;
                d2ddc.Transform = Matrix3x2.Identity;  // 等倍描画(DPXtoDPX)
                d2ddc.PrimitiveBlend = PrimitiveBlend.SourceOver;

                // 背景画像
                d2ddc.Clear( new Color4( Color3.Black, 0f ) );
                d2ddc.DrawBitmap( this._右サイドクリアパネル.背景.Bitmap, opacity: 1f, interpolationMode: InterpolationMode.Linear );

                d2ddc.Flush(); // いったんここまで描画。

                d2ddc.Target = preTarget;
                d2ddc.Transform = preTrans;
                d2ddc.PrimitiveBlend = preBlend;

                ( (IUnknown)preTarget ).Release(); // 要Release
            }

            this._右サイドクリアパネル.進行描画する();
            //----------------
            #endregion

            #region " レーンフレーム "
            //----------------
            this._レーンフレーム.進行描画する( d2ddc, userConfig.レーンの透明度, レーンラインを描画する: ( userConfig.ダーク == ダーク種別.OFF ) );
            //----------------
            #endregion

            #region " 背景画像 "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._背景画像.描画する( d2ddc, 0f, 0f );
            //----------------
            #endregion

            #region " 譜面スクロール速度 "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._譜面スクロール速度.描画する( d2ddc, userConfig.譜面スクロール速度 );
            //----------------
            #endregion

            #region " エキサイトゲージ "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._エキサイトゲージ.進行描画する( d2ddc, this.成績.エキサイトゲージ量 );
            //----------------
            #endregion

            #region " クリアメーター "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._クリアメーター.進行描画する( d2ddc );
            //----------------
            #endregion

            #region " フェーズパネル "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._フェーズパネル.進行描画する( d2ddc );
            //----------------
            #endregion

            #region " 曲目パネル "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._曲名パネル.進行描画する( d2ddc );
            //----------------
            #endregion

            #region " ヒットバー "
            //----------------
            if( userConfig.ダーク != ダーク種別.FULL )
                this._ドラムキットとヒットバー.ヒットバーを進行描画する( d2ddc );
            //----------------
            #endregion

            #region " ドラムキット "
            //----------------
            if( userConfig.ダーク == ダーク種別.OFF )
                this._ドラムキットとヒットバー.ドラムキットを進行描画する( d2ddc );
            //----------------
            #endregion
        }
    }
}
