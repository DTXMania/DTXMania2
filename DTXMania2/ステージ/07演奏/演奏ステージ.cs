using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct2D1;
using SSTFormat.v004;

namespace DTXMania2.演奏
{
    class 演奏ステージ : IStage
    {

        // プロパティ


        public const float ヒット判定位置Ydpx = 847f;

        public enum フェーズ
        {
            フェードイン,
            演奏開始,
            表示,
            クリア,
            失敗,
            キャンセル通知,
            キャンセル時フェードアウト,
            キャンセル完了,
            即時終了,
        }

        public フェーズ 現在のフェーズ { get; protected set; } = フェーズ.キャンセル完了;

        /// <summary>
        ///     フェードインアイキャッチの遷移元画面。
        ///     活性化前に、外部から設定される。
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

            this._背景画像 = new 画像( @"$(Images)\PlayStage\Background.png" );
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
            this._小節線色 = new SolidColorBrush( Global.既定のD2D1DeviceContext, Color.White );
            this._小節線影色 = new SolidColorBrush( Global.既定のD2D1DeviceContext, Color.Blue );
            this._拍線色 = new SolidColorBrush( Global.既定のD2D1DeviceContext, Color.Gray );

            this._フェードインカウンタ = new Counter( 0, 100, 10 );

            this.成績 = new 成績();

            #region " スコアに依存するデータを初期化する。"
            //----------------
            this._描画開始チップ番号 = -1;

            this._チップの演奏状態 = new Dictionary<チップ, チップの演奏状態>();
            foreach( var chip in Global.App.演奏スコア.チップリスト )
                this._チップの演奏状態.Add( chip, new チップの演奏状態( chip ) );

            this._スコア指定の背景画像 = string.IsNullOrEmpty( Global.App.演奏スコア.背景画像ファイル名 ) ?
                null! :
                new 画像( Path.Combine( Global.App.演奏スコア.PATH_WAV, Global.App.演奏スコア.背景画像ファイル名 ) );    // PATH_WAV は絶対パス
            //----------------
            #endregion

            #region " WAVを生成する。"
            //----------------
            Global.App.WAVキャッシュ.世代を進める();

            Global.App.WAV管理?.Dispose();
            Global.App.WAV管理 = new WAV管理();

            foreach( var kvp in Global.App.演奏スコア.WAVリスト )
            {
                var wavInfo = kvp.Value;

                var path = Path.Combine( Global.App.演奏スコア.PATH_WAV, wavInfo.ファイルパス );
                Global.App.WAV管理.登録する( kvp.Key, path, wavInfo.多重再生する, wavInfo.BGMである );
            }
            //----------------
            #endregion

            #region " AVIを生成する。"
            //----------------
            Global.App.AVI管理?.Dispose();
            Global.App.AVI管理 = new AVI管理();

            if( userConfig.演奏中に動画を表示する )
            {
                foreach( var kvp in Global.App.演奏スコア.AVIリスト )
                {
                    var path = Path.Combine( Global.App.演奏スコア.PATH_WAV, kvp.Value );
                    Global.App.AVI管理.登録する( kvp.Key, path, userConfig.再生速度 );
                }
            }
            //----------------
            #endregion

            this.現在のフェーズ = ( Global.Options.ビュアーモードである ) ? フェーズ.演奏開始 : フェーズ.フェードイン;
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            #region " 演奏状態を終了する。"
            //----------------
            this._描画開始チップ番号 = -1;

            //Global.App.WAV管理?.Dispose();	    --> ここではまだ解放しない。
            Global.App.AVI管理.Dispose();
            //----------------
            #endregion

            // ユーザDBに変更があれば保存する。
            Global.App.ログオン中のユーザ.保存する();

            this.キャプチャ画面?.Dispose();
            this._スコア指定の背景画像?.Dispose();

            this._拍線色.Dispose();
            this._小節線影色.Dispose();
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
            try
            {
                this._システム情報.FPSをカウントしプロパティを更新する();

                #region " オプションが届いていれば、取り出して処理する。"
                //----------------
                if( Global.Options.ビュアーモードである && OptionsQueue.TryDequeue( out var options ) )
                {
                    if( options.再生停止 )
                    {
                        Log.Info( "演奏を即時終了します。" );
                        Global.App.WAV管理.すべての発声を停止する();    // DTXでのBGMサウンドはこっちに含まれる。
                        this.現在のフェーズ = フェーズ.即時終了;
                    }
                }
                //----------------
                #endregion

                var userConfig = Global.App.ログオン中のユーザ;

                switch( this.現在のフェーズ )
                {
                    case フェーズ.フェードイン:
                        {
                            if( this._フェードインカウンタ.終了値に達した )
                            {
                                Log.Info( "演奏を開始します。" );
                                this.現在のフェーズ = フェーズ.演奏開始;
                            }
                        }
                        break;

                    case フェーズ.演奏開始:
                        {
                            this._描画開始チップ番号 = -1; // -1 以外になれば演奏開始。

                            if( Global.Options.ビュアーモードである && 0 <= 演奏ステージ.演奏開始小節番号 )
                            {
                                // ビュアーモードの場合、演奏開始小節番号から演奏を開始する。
                                var 演奏開始時刻sec = this._指定小節へ移動する( 演奏ステージ.演奏開始小節番号 );
                                Global.App.サウンドタイマ.リセットする( 演奏開始時刻sec );
                            }
                            else
                            {
                                // 通常モードの場合、最初から。
                                this._描画開始チップ番号 = 0;
                                Global.App.サウンドタイマ.リセットする();
                            }

                            this.現在のフェーズ = フェーズ.表示;

                            // ここで break すると、次の表示フェーズまで１フレーム分の時間（数ミリ秒）が空いてしまう。
                            // なので、フレームが空かないように、ここですぐさま最初の表示フェーズを実行する。
                            this.進行する();
                        }
                        break;

                    case フェーズ.表示:
                        {
                            // ※注:クリアや失敗の判定は、ここではなく描画側で行っている。

                            double 現在の演奏時刻sec = this._演奏開始からの経過時間secを返す();
                            long 現在の演奏時刻qpc = QPCTimer.生カウント;

                            #region " 自動ヒット処理。"
                            //----------------
                            this._描画範囲内のすべてのチップに対して( 現在の演奏時刻sec, ( chip, index, ヒット判定バーと描画との時間sec, ヒット判定バーと発声との時間sec, ヒット判定バーとの距離dpx ) => {

                                var ドラムチッププロパティ = userConfig.ドラムチッププロパティリスト[ chip.チップ種別 ];

                                bool AutoPlayである = userConfig.AutoPlay[ ドラムチッププロパティ.AutoPlay種別 ];
                                bool チップはヒット済みである = this._チップの演奏状態[ chip ].ヒット済みである;
                                bool チップはまだヒットされていない = !( チップはヒット済みである );
                                bool チップはMISSエリアに達している = ( ヒット判定バーと描画との時間sec > userConfig.最大ヒット距離sec[ 判定種別.OK ] );
                                bool チップは描画についてヒット判定バーを通過した = ( 0 <= ヒット判定バーと描画との時間sec );
                                bool チップは発声についてヒット判定バーを通過した = ( 0 <= ヒット判定バーと発声との時間sec );

                                if( チップはまだヒットされていない && チップはMISSエリアに達している )
                                {
                                    #region " MISS判定。"
                                    //----------------
                                    if( AutoPlayである && ドラムチッププロパティ.AutoPlayON_Miss判定 )
                                    {
                                        // AutoPlay時
                                        this._チップのヒット処理を行う(
                                            chip,
                                            判定種別.MISS,
                                            ドラムチッププロパティ.AutoPlayON_自動ヒット_再生,
                                            ドラムチッププロパティ.AutoPlayON_自動ヒット_判定,
                                            ドラムチッププロパティ.AutoPlayON_自動ヒット_非表示,
                                            ヒット判定バーと発声との時間sec,
                                            ヒット判定バーと描画との時間sec );
                                        return;
                                    }
                                    else if( !AutoPlayである && ドラムチッププロパティ.AutoPlayOFF_Miss判定 )
                                    {
                                        // 手動演奏時
                                        this._チップのヒット処理を行う(
                                            chip,
                                            判定種別.MISS,
                                            ドラムチッププロパティ.AutoPlayOFF_ユーザヒット_再生,
                                            ドラムチッププロパティ.AutoPlayOFF_ユーザヒット_判定,
                                            ドラムチッププロパティ.AutoPlayOFF_ユーザヒット_非表示,
                                            ヒット判定バーと発声との時間sec,
                                            ヒット判定バーと描画との時間sec );

                                        // 手動演奏なら MISS はエキサイトゲージに反映。
                                        this.成績.エキサイトゲージを更新する( 判定種別.MISS ); 
                                        return;
                                    }
                                    else
                                    {
                                        // 通過。
                                    }
                                    //----------------
                                    #endregion
                                }

                                // ヒット処理(1) 発声時刻
                                if( チップは発声についてヒット判定バーを通過した )    // ヒット済みかどうかには関係ない
                                {
                                    #region " 自動ヒット判定（再生）"
                                    //----------------
                                    if( ( AutoPlayである && ドラムチッププロパティ.AutoPlayON_自動ヒット_再生 ) ||
                                        ( !AutoPlayである && ドラムチッププロパティ.AutoPlayOFF_自動ヒット_再生 ) )
                                    {
                                        // チップの発声がまだなら発声を行う。
                                        if( !( this._チップの演奏状態[ chip ].発声済みである ) )
                                        {
                                            this._チップの発声を行う( chip, userConfig.ドラムの音を発声する );
                                            this._チップの演奏状態[ chip ].発声済みである = true;
                                        }
                                    }
                                    //----------------
                                    #endregion
                                }

                                // ヒット処理(2) 描画時刻
                                if( チップはまだヒットされていない && チップは描画についてヒット判定バーを通過した )
                                {
                                    #region " 自動ヒット判定（判定）"
                                    //----------------
                                    if( AutoPlayである && ドラムチッププロパティ.AutoPlayON_自動ヒット )
                                    {
                                        // 自動演奏時
                                        this._チップのヒット処理を行う(
                                            chip,
                                            判定種別.PERFECT,   // AutoPlay 時は Perfect 扱い。
                                            ドラムチッププロパティ.AutoPlayON_自動ヒット_再生,
                                            ドラムチッププロパティ.AutoPlayON_自動ヒット_判定,
                                            ドラムチッププロパティ.AutoPlayON_自動ヒット_非表示,
                                            ヒット判定バーと発声との時間sec,
                                            ヒット判定バーと描画との時間sec );

                                        //this.成績.エキサイトゲージを加算する( 判定種別.PERFECT ); -> エキサイトゲージには反映しない。
                                        this._ドラムキットとヒットバー.ヒットアニメ開始( ドラムチッププロパティ.表示レーン種別 );

                                        return; // ここで終了。
                                    }
                                    else if( !AutoPlayである && ドラムチッププロパティ.AutoPlayOFF_自動ヒット )
                                    {
                                        // 手動演奏時
                                        this._チップのヒット処理を行う(
                                            chip,
                                            判定種別.PERFECT,   // AutoPlay OFF でも自動ヒットする場合は Perfect 扱い。
                                            ドラムチッププロパティ.AutoPlayOFF_自動ヒット_再生,
                                            ドラムチッププロパティ.AutoPlayOFF_自動ヒット_判定,
                                            ドラムチッププロパティ.AutoPlayOFF_自動ヒット_非表示,
                                            ヒット判定バーと発声との時間sec,
                                            ヒット判定バーと描画との時間sec );

                                        //this.成績.エキサイトゲージを加算する( 判定種別.PERFECT ); -> エキサイトゲージには反映しない。
                                        this._ドラムキットとヒットバー.ヒットアニメ開始( ドラムチッププロパティ.表示レーン種別 );

                                        return; // ここで終了。
                                    }
                                    else
                                    {
                                        // 通過。
                                    }
                                    //----------------
                                    #endregion
                                }

                            } );
                            //----------------
                            #endregion

                            Global.App.ドラム入力.すべての入力デバイスをポーリングする( 入力履歴を記録する: false );

                            #region " 手動ヒット処理。"
                            //----------------
                            {
                                var ヒット処理済み入力 = new List<ドラム入力イベント>(); // ヒット処理が終わった入力は、二重処理しないよう、この中に追加しておく。

                                this._描画範囲内のすべてのチップに対して( 現在の演奏時刻sec, ( chip, index, ヒット判定バーと描画との時間sec, ヒット判定バーと発声との時間sec, ヒット判定バーとの距離 ) => {

                                    var ドラムチッププロパティ = userConfig.ドラムチッププロパティリスト[ chip.チップ種別 ];

                                    #region " チップがヒット対象外であれば無視する。"
                                    //----------------
                                    bool チップはAutoPlayONである = userConfig.AutoPlay[ ドラムチッププロパティ.AutoPlay種別 ];
                                    bool チップはMISSエリアに達している = ( ヒット判定バーと描画との時間sec > userConfig.最大ヒット距離sec[ 判定種別.OK ] );
                                    bool チップはOKエリアに達していない = ヒット判定バーと描画との時間sec < -( userConfig.最大ヒット距離sec[ 判定種別.OK ] );
                                    bool チップはすでにヒット済みである = this._チップの演奏状態[ chip ].ヒット済みである;
                                    bool チップはAutoPlayOFFのときゆユーザヒットの対象にならない = !( ドラムチッププロパティ.AutoPlayOFF_ユーザヒット );

                                    if( チップはAutoPlayONである ||
                                        チップはすでにヒット済みである ||
                                        チップはAutoPlayOFFのときゆユーザヒットの対象にならない ||
                                        チップはMISSエリアに達している ||
                                        チップはOKエリアに達していない )
                                        return;
                                    //----------------
                                    #endregion

                                    ドラム入力イベント? チップのヒットエリア内にある入力 = null;

                                    #region " チップのヒットエリア内にある入力を探す。"
                                    //----------------
                                    チップのヒットエリア内にある入力 = Global.App.ドラム入力.ポーリング結果.FirstOrDefault( ( 入力 ) => {

                                        if( !入力.InputEvent.押された ||                   // 押下入力じゃないなら無視。
                                            ヒット処理済み入力.Contains( 入力 ) ||         // すでに今回のターンで処理済み（＝処理済み入力リストに追加済み）なら無視。
                                            入力.Type == ドラム入力種別.HiHat_Control )    // HiHat_Control 入力はここでは無視。
                                            return false;

                                        var チップの入力グループ = ドラムチッププロパティ.入力グループ種別;
                                        if( チップの入力グループ == 入力グループ種別.Unknown )
                                        {
                                            // (A) 入力グループ種別 が Unknown の場合 → ドラム入力種別で比較
                                            return ( ドラムチッププロパティ.ドラム入力種別 == 入力.Type );          // 一致すればヒット
                                        }
                                        else
                                        {
                                            // (B) 入力グループ種別が Unknown ではない場合　→　入力グループ種別で比較
                                            var 入力の入力グループ種別リスト =
                                                from kvp in userConfig.ドラムチッププロパティリスト.チップtoプロパティ
                                                where ( kvp.Value.ドラム入力種別 == 入力.Type )
                                                select kvp.Value.入力グループ種別;

                                            return 入力の入力グループ種別リスト.Any( ( groupType ) => ( groupType == チップの入力グループ ) );  // どれかが一致すればヒット
                                        }

                                    } );
                                    //----------------
                                    #endregion


                                    if( null != チップのヒットエリア内にある入力 ) // チップにヒットした入力があった場合
                                    {
                                        #region " チップの手動ヒット処理。"
                                        //----------------
                                        // この入力はこのチップでヒット処理した。
                                        ヒット処理済み入力.Add( チップのヒットエリア内にある入力 );

                                        // 入力とチップの時間差を算出する。
                                        double 入力とチップの時間差sec = 0.0;  // チップより入力が早ければ負数、遅ければ正数。

                                        #region " 入力とチップとの時間差を算出。"
                                        //----------------
                                        {
                                            // 入力時刻とはヒット判定バーの位置における時刻であり、理想値は 0 である。
                                            // しかし実際には、入力遅延（MIDI入力のポーリング時刻と現在時刻のズレなど）が発生することがあるので、0 または正数となる。
                                            double 入力時刻sec = QPCTimer.生カウント相対値を秒へ変換して返す( 現在の演奏時刻qpc - チップのヒットエリア内にある入力.InputEvent.TimeStamp );

                                            // そこで、システム設定の「判定位置調整」量を加えることで、この入力遅延を相殺する。
                                            // 具体的には、ヒット判定バーの位置、すなわち入力時刻を、判定位置調整の値で調整する。
                                            // 例えば、判定位置調整が -50ms である場合、各入力はそれぞれ 50ms 早く発生したものとみなされる。
                                            // 注意：この機能を、タイミングが常に早い方向か遅い方向にずれまくる人のための「人為的な入力遅延」として使うことは推奨されない。
                                            入力時刻sec += Global.App.システム設定.判定位置調整ms * 0.001;

                                            入力とチップの時間差sec = 入力時刻sec - ヒット判定バーと描画との時間sec;
                                        }
                                        //----------------
                                        #endregion

                                        // 時刻差から判定を算出。
                                        double 入力とチップの時間差の絶対値sec = Math.Abs( 入力とチップの時間差sec );
                                        var 判定 =
                                            ( 入力とチップの時間差の絶対値sec <= userConfig.最大ヒット距離sec[ 判定種別.PERFECT ] ) ? 判定種別.PERFECT :
                                            ( 入力とチップの時間差の絶対値sec <= userConfig.最大ヒット距離sec[ 判定種別.GREAT ] ) ? 判定種別.GREAT :
                                            ( 入力とチップの時間差の絶対値sec <= userConfig.最大ヒット距離sec[ 判定種別.GOOD ] ) ? 判定種別.GOOD : 判定種別.OK;

                                        // ヒット処理。
                                        this._チップのヒット処理を行う(
                                            chip,
                                            判定,
                                            userConfig.ドラムの音を発声する ? ドラムチッププロパティ.AutoPlayOFF_ユーザヒット_再生 : false,   // ヒットすれば再生する？
                                            ドラムチッププロパティ.AutoPlayOFF_ユーザヒット_判定,                                             // ヒットすれば判定する？
                                            ドラムチッププロパティ.AutoPlayOFF_ユーザヒット_非表示,                                           // ヒットすれば非表示にする？
                                            ヒット判定バーと発声との時間sec,
                                            入力とチップの時間差sec );

                                        // エキサイトゲージに反映する。
                                        this.成績.エキサイトゲージを更新する( 判定 );
                                        //----------------
                                        #endregion
                                    }

                                } );

                                #region " ヒットしてようがしてまいが起こすアクションを実行。"
                                //----------------
                                foreach( var 入力 in Global.App.ドラム入力.ポーリング結果 )
                                {
                                    // 押下入力じゃないなら無視。
                                    if( 入力.InputEvent.離された )
                                        continue;

                                    var プロパティs = userConfig.ドラムチッププロパティリスト.チップtoプロパティ.Where( ( kvp ) => ( kvp.Value.ドラム入力種別 == 入力.Type ) );
                                    
                                    if( 0 < プロパティs.Count() )    // 1つだけのはずだが念のため。
                                    {
                                        var laneType = プロパティs.First().Value.表示レーン種別;

                                        // ヒットしてようがしてまいが起こすアクション。
                                        this._ドラムキットとヒットバー.ヒットアニメ開始( laneType );
                                        this._レーンフラッシュ.開始する( laneType );
                                    }
                                }
                                //----------------
                                #endregion

                                #region " どのチップにもヒットしなかった入力は空打ちとみなし、空打ち音を再生する。"
                                //----------------
                                if( userConfig.ドラムの音を発声する )
                                {
                                    foreach( var 入力 in Global.App.ドラム入力.ポーリング結果 )
                                    {
                                        if( ヒット処理済み入力.Contains( 入力 ) ||  // ヒット済みなら無視。
                                            入力.InputEvent.離された )              // 押下じゃないなら無視。
                                            continue;

                                        var プロパティs = userConfig.ドラムチッププロパティリスト.チップtoプロパティ.Where( ( kvp ) => ( kvp.Value.ドラム入力種別 == 入力.Type ) );

                                        for( int i = 0; i < プロパティs.Count(); i++ )
                                        {
                                            var prop = プロパティs.ElementAt( i ).Value;

                                            if( 0 < Global.App.演奏スコア.空打ちチップマップ.Count )
                                            {
                                                #region " DTX他の場合（空うちチップマップ使用）"
                                                //----------------
                                                int zz = Global.App.演奏スコア.空打ちチップマップ[ prop.レーン種別 ];

                                                // (A) 空打ちチップの指定があるなら、それを発声する。
                                                if( 0 != zz )
                                                    Global.App.WAV管理.発声する( zz, prop.チップ種別, prop.発声前消音, prop.消音グループ種別, true );

                                                // (B) 空打ちチップの指定がないなら、一番近いチップを検索し、それを発声する。
                                                else
                                                {
                                                    var chip = this._指定された時刻に一番近いチップを返す( 現在の演奏時刻sec, 入力.Type );

                                                    if( null != chip )
                                                    {
                                                        this._チップの発声を行う( chip, true );
                                                        break;  // 複数のチップが該当する場合でも、最初のチップの発声のみ行う。
                                                    }
                                                }
                                                //----------------
                                                #endregion
                                            }
                                            else
                                            {
                                                #region " SSTFの場合（空うちチップマップ未使用）"
                                                //----------------
                                                Global.App.ドラムサウンド.再生する( prop.チップ種別, 0, prop.発声前消音, prop.消音グループ種別 );
                                                //----------------
                                                #endregion
                                            }
                                        }
                                    }
                                }
                                //----------------
                                #endregion
                            }
                            //----------------
                            #endregion

                            #region " その他の手動入力の処理。"
                            //----------------
                            if( Global.App.ドラム入力.Keybaord.TryGetTarget( out var keyboard ) )
                            {
                                if( !Global.Options.ビュアーモードである && keyboard.キーが押された( 0, Keys.Escape ) )
                                {
                                    #region " ESC → 演奏中断（ビュアーモード時は無効）"
                                    //----------------
                                    Log.Info( "ESC キーが押されました。演奏を中断します。" );

                                    Global.App.WAV管理.すべての発声を停止する();    // DTXでのBGMサウンドはこっちに含まれる。

                                    // 後の処理は描画処理で。
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
                                        Global.App.WAV管理.すべてのBGMの再生位置を移動する( +10.0 / 1000.0 );  // sec
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
                                        Global.App.WAV管理.すべてのBGMの再生位置を移動する( -10.0 / 1000.0 );  // sec
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

                            #region " ハイハットの開閉 "
                            //----------------
                            if( Global.App.ドラム入力.MidiIn.TryGetTarget( out var midiIns ) )
                            {
                                foreach( var ev in midiIns.入力イベントリスト.Where( ( ie ) => ( 255 == ie.Key ) ) )
                                {
                                    this._ドラムキットとヒットバー.ハイハットの開度を設定する( ev.Velocity );
                                }
                            }
                            //----------------
                            #endregion

                            if( Global.App.ドラム入力.ドラムが入力された( ドラム入力種別.Pause_Resume ) )
                            {
                                #region " Pause/Resumu パッド → 演奏の一時停止または再開 "
                                //----------------
                                this._演奏を一時停止または再開する();
                                //----------------
                                #endregion
                            }
                            //----------------
                            #endregion
                        }
                        break;
                }
            }
            catch( Exception e )
            {
                Log.ERROR( $"!!! 進行スレッドで例外が発生しました !!! [{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( e.Message )}]" );
            }
        }

        public void 描画する()
        {
            var dc = Global.既定のD2D1DeviceContext;
            dc.Transform = Global.拡大行列DPXtoPX;

            var userConfig = Global.App.ログオン中のユーザ;

            switch( this.現在のフェーズ )
            {
                case フェーズ.フェードイン:
                case フェーズ.キャンセル完了:
                    #region " *** "
                    //----------------
                    {
                        #region " スコア指定の壁紙 "
                        //----------------
                        if( userConfig.スコア指定の背景画像を表示する )
                        {
                            this._スコア指定の背景画像?.描画する( 0f, 0f,
                                X方向拡大率: Global.設計画面サイズ.Width / this._スコア指定の背景画像.サイズ.Width,
                                Y方向拡大率: Global.設計画面サイズ.Height / this._スコア指定の背景画像.サイズ.Height );
                        }
                        //----------------
                        #endregion

                        #region " 左サイドパネルへの描画と、左サイドパネルの表示 "
                        //----------------
                        this._左サイドクリアパネル.クリアする();
                        this._左サイドクリアパネル.クリアパネル.画像へ描画する( ( dcp ) => {

                            // プレイヤー名
                            this._プレイヤー名表示.進行描画する( dcp );

                            // スコア
                            if( userConfig.ダーク == ダーク種別.OFF )
                                this._スコア表示.進行描画する( dcp, Global.Animation, new Vector2( +280f, +120f ), this.成績 );

                            // 達成率
                            this._達成率表示.描画する( dcp, (float) this.成績.Achievement );

                            // 判定パラメータ
                            this._判定パラメータ表示.描画する( dcp, +118f, +372f, this.成績 );

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
                            this._エキサイトゲージ.進行描画する( dc, this.成績.エキサイトゲージ量 );
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

                        #region " キャプチャ画像 "
                        //----------------
                        this._キャプチャ画面を描画する( dc, ( 1.0f - this._フェードインカウンタ.現在値の割合 ) );
                        //----------------
                        #endregion
                    }
                    //----------------
                    #endregion
                    break;

                case フェーズ.表示:
                case フェーズ.キャンセル時フェードアウト:
                    #region " *** "
                    //----------------
                    {
                        double 演奏時刻sec = this._演奏開始からの経過時間secを返す() + Global.次のDComp表示までの残り時間sec;

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
                            this._スコア指定の背景画像?.描画する( 0f, 0f,
                                X方向拡大率: Global.設計画面サイズ.Width / this._スコア指定の背景画像.サイズ.Width,
                                Y方向拡大率: Global.設計画面サイズ.Height / this._スコア指定の背景画像.サイズ.Height );
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
                                            #region " *** "
                                            //----------------
                                            {
                                                // 100%全体表示
                                                float w = Global.設計画面サイズ.Width;
                                                float h = Global.設計画面サイズ.Height;
                                                video.描画する( dc, new RectangleF( 0f, 0f, w, h ) );
                                            }
                                            //----------------
                                            #endregion
                                            break;

                                        case 動画の表示サイズ.中央寄せ:
                                            #region " *** "
                                            //----------------
                                            {
                                                // 75%縮小表示
                                                float w = Global.設計画面サイズ.Width;
                                                float h = Global.設計画面サイズ.Height;

                                                // (1) 画面いっぱいに描画。
                                                video.描画する( dc, new RectangleF( 0f, 0f, w, h ), 0.2f );    // 不透明度は 0.2 で暗くする。

                                                // (2) ちょっと縮小して描画。
                                                float 拡大縮小率 = 0.75f;
                                                float 上移動 = 100.0f;
                                                video.最後のフレームを再描画する( dc, new RectangleF(   // 直前に取得したフレームをそのまま描画。
                                                    w * ( 1f - 拡大縮小率 ) / 2f,
                                                    h * ( 1f - 拡大縮小率 ) / 2f - 上移動,
                                                    w * 拡大縮小率,
                                                    h * 拡大縮小率 ) );
                                            }
                                            //----------------
                                            #endregion
                                            break;
                                    }
                                }
                            }
                        }
                        //----------------
                        #endregion

                        #region " 左サイドパネルへの描画と、左サイドパネルの表示 "
                        //----------------
                        this._左サイドクリアパネル.クリアする();
                        this._左サイドクリアパネル.クリアパネル.画像へ描画する( ( dcp ) => {

                            // プレイヤー名
                            this._プレイヤー名表示.進行描画する( dcp );

                            // スコア
                            if( userConfig.ダーク == ダーク種別.OFF )
                                this._スコア表示.進行描画する( dcp, Global.Animation, new Vector2( +280f, +120f ), this.成績 );

                            // 達成率
                            this._達成率表示.描画する( dcp, (float) this.成績.Achievement );

                            // 判定パラメータ
                            this._判定パラメータ表示.描画する( dcp, +118f, +372f, this.成績 );

                            // スキル
                            this._曲別SKILL.進行描画する( dcp, this.成績.スキル );

                        } );
                        this._左サイドクリアパネル.描画する();
                        //----------------
                        #endregion

                        #region " 右サイドパネルへの描画と、右サイドパネルの表示 "
                        //----------------
                        this._右サイドクリアパネル.クリアする();
                        this._右サイドクリアパネル.クリアパネル.画像へ描画する( ( dcp ) => {

                            // コンボ
                            this._コンボ表示.進行描画する( dcp, new Vector2( +228f + 264f / 2f, +234f ), this.成績 );

                        } );
                        this._右サイドクリアパネル.描画する();
                        //----------------
                        #endregion

                        #region " レーンフレーム "
                        //----------------
                        this._レーンフレーム.描画する( dc, userConfig.レーンの透明度, レーンラインを描画する: ( userConfig.ダーク == ダーク種別.OFF ) ? true : false );
                        //----------------
                        #endregion

                        #region " レーンフラッシュ  "
                        //----------------
                        this._レーンフラッシュ.進行描画する();
                        //----------------
                        #endregion

                        #region " 小節線拍線 "
                        //----------------
                        if( userConfig.ダーク != ダーク種別.FULL )
                            this._小節線拍線を描画する( dc, 演奏時刻sec );
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
                            this._エキサイトゲージ.進行描画する( dc, this.成績.エキサイトゲージ量 );
                        //----------------
                        #endregion

                        #region " クリアメーター, フェーズパネル "
                        //----------------
                        double 曲の長さsec = Global.App.演奏スコア.チップリスト[ Global.App.演奏スコア.チップリスト.Count - 1 ].描画時刻sec;
                        float 現在位置 = (float) ( 1.0 - ( 曲の長さsec - 演奏時刻sec ) / 曲の長さsec );

                        // クリアメーター
                        this.成績.CountMap = this._クリアメーター.カウント値を設定する( 現在位置, this.成績.判定別ヒット数 );
                        if( userConfig.ダーク == ダーク種別.OFF )
                            this._クリアメーター.進行描画する( dc );

                        // フェーズパネル
                        this._フェーズパネル.現在位置 = 現在位置;
                        if( userConfig.ダーク == ダーク種別.OFF )
                            this._フェーズパネル.進行描画する();
                        //----------------
                        #endregion

                        #region " 曲名パネル "
                        //----------------
                        if( userConfig.ダーク == ダーク種別.OFF )
                            this._曲名パネル.描画する( dc );
                        //----------------
                        #endregion

                        #region " ヒットバー "
                        //----------------
                        // ヒットバー
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

                        #region " ドラムチップ "
                        //----------------
                        this._描画範囲内のすべてのチップに対して( 演奏時刻sec, ( チップ chip, int index, double ヒット判定バーと描画との時間sec, double ヒット判定バーと発声との時間sec, double ヒット判定バーとの距離dpx ) => {

                            // クリア判定はこの中。
                            if( this._ドラムチップ.進行描画する( ref this._描画開始チップ番号, this._チップの演奏状態[ chip ], chip, index, ヒット判定バーとの距離dpx ) )
                            {
                                this.現在のフェーズ = フェーズ.クリア;
                            }

                        } );
                        //----------------
                        #endregion

                        #region " チップ光 "
                        //----------------
                        this._チップ光.進行描画する( dc );
                        //----------------
                        #endregion

                        #region " 判定文字列 "
                        //----------------
                        this._判定文字列.進行描画する();
                        //----------------
                        #endregion

                        #region " システム情報 "
                        //----------------
                        this._システム情報.VPSをカウントする();

                        if( userConfig.ダーク == ダーク種別.OFF )
                            this._システム情報.描画する( dc, $"BGMAdjust: {Global.App.演奏譜面.譜面.BGMAdjust}" );
                        //----------------
                        #endregion

                        #region " アイキャッチ "
                        //----------------
                        if( this.現在のフェーズ == フェーズ.キャンセル時フェードアウト )
                        {
                            Global.App.アイキャッチ管理.現在のアイキャッチ.進行描画する( dc );

                            if( Global.App.アイキャッチ管理.現在のアイキャッチ.現在のフェーズ == アイキャッチ.フェーズ.クローズ完了 )
                            {
                                this.現在のフェーズ = フェーズ.キャンセル完了;
                            }
                        }
                        //----------------
                        #endregion
                    }
                    //----------------
                    #endregion
                    break;

                case フェーズ.クリア:
                    #region " *** "
                    //----------------
                    {
                        #region " 背景画像 "
                        //----------------
                        this._背景画像.描画する( 0f, 0f );
                        //----------------
                        #endregion
                    }
                    //----------------
                    #endregion
                    break;

                case フェーズ.キャンセル通知:
                    #region " *** "
                    //----------------
                    {
                        #region " フェードアウト開始 "
                        //----------------
                        Global.App.アイキャッチ管理.アイキャッチを選択しクローズする( nameof( 半回転黒フェード ) );
                        this.現在のフェーズ = フェーズ.キャンセル時フェードアウト;
                        //----------------
                        #endregion
                    }
                    //----------------
                    #endregion
                    break;

                case フェーズ.即時終了:
                    break;
            }
        }



        // 画面を構成するもの


        private readonly 画像 _背景画像;

        private readonly 画像 _スコア指定の背景画像;

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

        private void _キャプチャ画面を描画する( DeviceContext dc, float 不透明度 = 1.0f )
        {
            Debug.Assert( null != this.キャプチャ画面, "キャプチャ画面が設定されていません。" );

            Global.D2DBatchDraw( dc, () => {

                dc.DrawBitmap(
                    this.キャプチャ画面,
                    new RectangleF( 0f, 0f, Global.設計画面サイズ.Width, Global.設計画面サイズ.Height ),
                    不透明度,
                    BitmapInterpolationMode.Linear );

            } );
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

        private readonly SolidColorBrush _小節線影色;

        private readonly SolidColorBrush _拍線色;

        private void _小節線拍線を描画する( DeviceContext dc, double 現在の演奏時刻sec )
        {
            // 小節線・拍線 と チップ は描画階層（奥行き）が異なるので、別々のメソッドに分ける。

            Global.D2DBatchDraw( dc, () => {

                var userConfig = Global.App.ログオン中のユーザ;

                this._描画範囲内のすべてのチップに対して( 現在の演奏時刻sec, ( chip, index, ヒット判定バーと描画との時間sec, ヒット判定バーと発声との時間sec, ヒット判定バーとの距離dpx ) => {

                    if( chip.チップ種別 == チップ種別.小節線 )
                    {
                        float 上位置dpx = (float) ( ヒット判定位置Ydpx + ヒット判定バーとの距離dpx - 1f );   // -1f は小節線の厚みの半分。

                        // 小節線
                        if( userConfig.演奏中に小節線と拍線を表示する )
                        {
                            float x = 441f;
                            float w = 780f;
                            dc.DrawLine( new Vector2( x, 上位置dpx + 0f ), new Vector2( x + w, 上位置dpx + 0f ), this._小節線色 );
                            dc.DrawLine( new Vector2( x, 上位置dpx + 1f ), new Vector2( x + w, 上位置dpx + 1f ), this._小節線影色 );
                        }

                        // 小節番号
                        if( userConfig.演奏中に小節番号を表示する )
                        {
                            float 右位置dpx = 441f + 780f - 24f;   // -24f は適当なマージン。
                            this._数字フォント中グレー48x64.描画する( dc, 右位置dpx, 上位置dpx - 84f, chip.小節番号.ToString(), 右揃え: true );    // -84f は適当なマージン。
                        }
                    }
                    else if( chip.チップ種別 == チップ種別.拍線 )
                    {
                        // 拍線
                        if( userConfig.演奏中に小節線と拍線を表示する )
                        {
                            float 上位置dpx = (float) ( ヒット判定位置Ydpx + ヒット判定バーとの距離dpx - 1f );   // -1f は拍線の厚みの半分。
                            dc.DrawLine( new Vector2( 441f, 上位置dpx ), new Vector2( 441f + 780f, 上位置dpx ), this._拍線色, strokeWidth: 1f );
                        }
                    }

                } );

            } );
        }



        // 演奏状態


        /// <summary>
        ///		<see cref="スコア.チップリスト"/> のうち、描画を始めるチップのインデックス番号。
        ///		未演奏時・演奏終了時は -1 。
        /// </summary>
        /// <remarks>
        ///		演奏開始直後は 0 で始まり、対象番号のチップが描画範囲を流れ去るたびに +1 される。
        ///		このメンバの更新は、高頻度進行タスクではなく、進行描画メソッドで行う。（低精度で構わないので）
        /// </remarks>
        private int _描画開始チップ番号 = -1;

        private readonly Dictionary<チップ, チップの演奏状態> _チップの演奏状態;

        private double _演奏開始からの経過時間secを返す()
            => Global.App.サウンドタイマ.現在時刻sec;

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
            using( var scoredb = new ScoreDB() )
                譜面レコード.ReplaceTo( scoredb );
        }

        private double _指定小節へ移動する( int 演奏開始小節番号 )
        {
            this._描画開始チップ番号 = -1;

            if( 0 > 演奏開始小節番号 )
            {
                this._描画開始チップ番号 = 0;
                return 0.0; // 最初から。
            }


            var score = Global.App.演奏スコア;

            var 最初のチップ番号 = -1;

            #region " 演奏開始小節番号に位置する最初のチップを検索する。"
            //----------------
            for( int i = 0; i < score.チップリスト.Count; i++ )
            {
                if( score.チップリスト[ i ].小節番号 >= 演奏開始小節番号 )
                {
                    最初のチップ番号 = i;
                    break;
                }
            }
            //----------------
            #endregion

            if( -1 == 最初のチップ番号 )
            {
                // すべて演奏終了している
                this._描画開始チップ番号 = score.チップリスト.Count - 1;
                return score.チップリスト.Last().発声時刻sec;
            }


            // 演奏開始時刻を、最初のチップの描画より少し手前に設定。

            double 演奏開始時刻sec = Math.Max( 0.0, score.チップリスト[ 最初のチップ番号 ].描画時刻sec - 0.5 );   // 0.5 秒早く

            #region " 演奏開始時刻をもとに、描画開始チップ番号を確定する。"
            //----------------
            for( int i = 0; i < score.チップリスト.Count; i++ )
            {
                var chip = score.チップリスト[ i ];

                if( chip.描画時刻sec >= 演奏開始時刻sec )
                {
                    this._描画開始チップ番号 = i;
                    break;
                }

                // 同時に、開始時刻より前のチップはヒット済みとする。

                this._チップの演奏状態[ chip ].ヒット済みの状態にする();
            }
            //----------------
            #endregion


            // 必要なら、AVI動画, WAV音声の途中再生を行う。

            #region " AVI動画の途中再生 "
            //----------------
            if( Global.App.ログオン中のユーザ.演奏中に動画を表示する )
            {
                var ヒット済みの動画チップリスト = score.チップリスト.Where( ( chip ) => (
                    this._チップの演奏状態[ chip ].ヒット済みである &&
                    chip.チップ種別 == チップ種別.背景動画 ) );

                foreach( var aviChip in ヒット済みの動画チップリスト )
                {
                    if( Global.App.AVI管理.動画リスト.TryGetValue( aviChip.チップサブID, out Video? video ) )
                    {
                        video?.再生を開始する( 演奏開始時刻sec - aviChip.発声時刻sec );
                    }
                }
            }
            //----------------
            #endregion

            #region " WAV音声の途中再生 "
            //----------------
            {
                var ヒット済みのBGMチップリスト = score.チップリスト.Where( ( chip ) => (
                    this._チップの演奏状態[ chip ].ヒット済みである &&
                    chip.チップ種別 == チップ種別.BGM ) );

                foreach( var wavChip in ヒット済みのBGMチップリスト )
                {
                    var prop = Global.App.ログオン中のユーザ.ドラムチッププロパティリスト.チップtoプロパティ[ wavChip.チップ種別 ];

                    Global.App.WAV管理.発声する(
                        wavChip.チップサブID,
                        wavChip.チップ種別,
                        prop.発声前消音,
                        prop.消音グループ種別,
                        BGM以外も再生する: Global.App.ログオン中のユーザ.ドラムの音を発声する && ( Global.Options.ビュアーモードである && ビュアーモードでドラム音を再生する ),
                        音量: wavChip.音量 / (float) チップ.最大音量, 演奏開始時刻sec - wavChip.発声時刻sec );
                }
            }
            //----------------
            #endregion

            return 演奏開始時刻sec;
        }


        // フェードイン（キャプチャ画像とステージ画像とのA-B変換）
        // → 既定のアイキャッチを使わないので独自管理する。


        /// <summary>
        ///		読み込み画面: 0 ～ 1: 演奏画面
        /// </summary>
        private readonly Counter _フェードインカウンタ;



        // ドラムチップ関連


        /// <summary>
        ///		描画範囲内にイチするすべてのチップに対して、指定された処理を実行する。
        /// </summary>
        /// <remarks>
        ///		「描画範囲内のチップ」とは、<see cref="_描画開始チップ番号"/> のチップを画面最下位のチップとし、画面上端にはみ出すまでの間のすべてのチップを示す。
        /// </remarks>
        /// <param name="適用する処理">
        ///		引数は、順に、対象のチップ, チップ番号, ヒット判定バーと描画との時間sec, ヒット判定バーと発声との時間sec, ヒット判定バーとの距離dpx。
        ///		時間と距離はいずれも、負数ならバー未達、0でバー直上、正数でバー通過。
        ///	</param>
        private void _描画範囲内のすべてのチップに対して( double 現在の演奏時刻sec, Action<チップ, int, double, double, double> 適用する処理 )
        {
            var score = Global.App.演奏スコア;

            // 描画開始チップから後方（画面最下位のチップから画面上方）のチップに向かって……
            for( int i = this._描画開始チップ番号; ( 0 <= i ) && ( i < score.チップリスト.Count ); i++ )
            {
                var chip = score.チップリスト[ i ];

                // チップに対する、ヒット判定バーとの時間と距離を算出する。
                // いずれも、負数ならバー未達、0でバー直上、正数でバー通過。
                const double _1秒あたりのピクセル数 = 0.14625 * 2.25 * 1000.0;    // これを変えると、speed あたりの速度が変わる。
                double ヒット判定バーと描画との時間sec = 現在の演奏時刻sec - chip.描画時刻sec;
                double ヒット判定バーと発声との時間sec = 現在の演奏時刻sec - chip.発声時刻sec;
                double ヒット判定バーとの距離dpx = ヒット判定バーと描画との時間sec * _1秒あたりのピクセル数 * this._譜面スクロール速度.補間付き速度;

                // チップが画面上方に隠れるならここでループを抜ける。
                bool チップは画面上端より上に出ている = ( ( ヒット判定位置Ydpx + ヒット判定バーとの距離dpx ) < -40.0 );   // -40 はチップが隠れるであろう適当なマージン。
                if( チップは画面上端より上に出ている )
                    break;

                // 適用する処理を呼び出す。開始判定（描画開始チップ番号の更新）もこの中で。
                適用する処理( chip, i, ヒット判定バーと描画との時間sec, ヒット判定バーと発声との時間sec, ヒット判定バーとの距離dpx );
            }
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
        private void _チップのヒット処理を行う( チップ chip, 判定種別 judge, bool 再生, bool 判定, bool 非表示, double ヒット判定バーと発声との時間sec, double 入力とチップとの間隔sec )
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
                    this._チップの発声を行う( chip, true );
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

                this.成績.成績を更新する( judge, userConfig.AutoPlay[ 対応表.AutoPlay種別 ] );
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

        private void _チップの発声を行う( チップ chip, bool ドラムサウンドを再生する )
        {
            var userConfig = Global.App.ログオン中のユーザ;

            if( chip.チップ種別 == チップ種別.背景動画 )
            {
                #region " (A) 背景動画チップ → AVI動画を再生する。"
                //----------------
                if( userConfig.演奏中に動画を表示する )
                {
                    int AVI番号 = chip.チップサブID;

                    if( Global.App.AVI管理.動画リスト.TryGetValue( AVI番号, out Video? video ) )
                    {
                        Global.App.サウンドタイマ.一時停止する();       // 止めても止めなくてもカクつくだろうが、止めておけば譜面は再開時にワープしない。
                        video.再生を開始する();
                        Global.App.サウンドタイマ.再開する();
                    }
                }
                //----------------
                #endregion
            }
            else
            {
                if( Global.Options.ビュアーモードである && !演奏ステージ.ビュアーモードでドラム音を再生する )
                    return;

                var drumChipProperty = userConfig.ドラムチッププロパティリスト.チップtoプロパティ[ chip.チップ種別 ];

                if( 0 == chip.チップサブID && ドラムサウンドを再生する )
                {
                    #region " (B) チップサブIDがゼロ → SSTF準拠のドラムサウンドを再生する。"
                    //----------------
                    // ドラムサウンドを持つチップなら発声する。（持つかどうかはこのメソッド↓内で判定される。）
                    Global.App.ドラムサウンド.再生する(
                        chip.チップ種別, 
                        0,
                        drumChipProperty.発声前消音,
                        drumChipProperty.消音グループ種別, 
                        ( chip.音量 / (float) チップ.最大音量 ) );
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
                        chip.チップ種別,
                        drumChipProperty.発声前消音, 
                        drumChipProperty.消音グループ種別,
                        ドラムサウンドを再生する,
                         ( chip.音量 / (float) チップ.最大音量 ) );
                    //----------------
                    #endregion
                }
            }
        }

        /// <summary>
        ///     該当するチップが1つもなかったら null を返す。
        /// </summary>
        private チップ? _指定された時刻に一番近いチップを返す( double 時刻sec, ドラム入力種別 drumType )
        {
            var チップtoプロパティ = Global.App.ログオン中のユーザ.ドラムチッププロパティリスト.チップtoプロパティ;

            var 一番近いチップ = (チップ?) null;
            var 一番近いチップの時刻差の絶対値sec = (double) 0.0;

            // すべてのチップについて、時刻の若い順に調べていく。
            for( int i = 0; i < Global.App.演奏スコア.チップリスト.Count; i++ )
            {
                var chip = Global.App.演奏スコア.チップリスト[ i ];

                // 指定されたドラム入力種別ではないチップは無視。
                if( チップtoプロパティ[ chip.チップ種別 ].ドラム入力種別 != drumType )
                    continue;

                // ループ抜ける？
                if( null != 一番近いチップ )
                {
                    var 今回の時刻差の絶対値sec = Math.Abs( chip.描画時刻sec - 時刻sec );

                    if( 一番近いチップの時刻差の絶対値sec < 今回の時刻差の絶対値sec )
                    {
                        // 時刻差の絶対値が前回より増えた → 前回のチップが指定時刻への再接近だった
                        break;
                    }
                }

                // 更新して次へ
                一番近いチップ = chip;
                一番近いチップの時刻差の絶対値sec = Math.Abs( 一番近いチップ.描画時刻sec - 時刻sec );
            }

            return 一番近いチップ;
        }
    }
}
