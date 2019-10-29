using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using DTXMania2.演奏;

namespace DTXMania2.オプション設定
{
    /// <summary>
    ///     <see cref="パネル_フォルダ"/> の選択と表示。
    /// </summary>
    class パネルリスト : IDisposable
    {

        // 外部依存アクション


        public Action 入力割り当てフェーズへ移行する = () => throw new NotImplementedException();

        public Action 曲読み込みフォルダ割り当てフェーズへ移行する = () => throw new NotImplementedException();

        public Action 再起動フェーズへ移行する = () => throw new NotImplementedException();

        public Action フェードアウトフェーズへ移行する = () => throw new NotImplementedException();



        // プロパティ


        public パネル_フォルダ パネルツリーのルートノード { get; }

        public パネル_フォルダ 現在のパネルフォルダ { get; private set; }

        public パネル? 現在選択中のパネル => this.現在のパネルフォルダ.子パネルリスト.SelectedItem;



        // 生成と終了


        public パネルリスト()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._青い線 = new 青い線();
            this._パッド矢印 = new パッド矢印();

            this.パネルツリーのルートノード = new パネル_フォルダ( "root", 親パネル: null );
            this.現在のパネルフォルダ = this.パネルツリーのルートノード;
            this._パネル_自動演奏_ONOFFトグルリスト = new List<パネル_ONOFFトグル>();

            // ツリー構築・オプションパート（ユーザ別）

            var systemConfig = Global.App.システム設定;
            var userConfig = Global.App.ログオン中のユーザ;
            if( userConfig is null )
                throw new Exception( "ユーザが選択されていません。" );

            #region "「自動演奏」フォルダ"
            //----------------
            {
                var 自動演奏フォルダ = new パネル_フォルダ(

                    パネル名:
                        "自動演奏",

                    親パネル:
                        this.パネルツリーのルートノード,

                    値の変更処理:
                        ( panel ) => {
                            this.子のパネルを選択する();
                            this.フェードインを開始する();
                        } );

                this.パネルツリーのルートノード.子パネルリスト.Add( 自動演奏フォルダ );

                // 子フォルダツリーの構築

                #region "「すべてON/OFF」パネル "
                //----------------
                自動演奏フォルダ.子パネルリスト.Add(

                    new パネル(

                        パネル名:
                            "すべてON/OFF",

                        値の変更処理:
                            ( panel ) => {
                                bool 設定値 = !( this._パネル_自動演奏_ONOFFトグルリスト[ 0 ].ONである );  // 最初の項目値の反対にそろえる
                                foreach( var typePanel in this._パネル_自動演奏_ONOFFトグルリスト )
                                {
                                    if( typePanel.ONである != 設定値 )    // 設定値と異なるなら
                                        typePanel.確定キーが入力された(); // ON/OFF反転
                                }
                            }
                    ) );
                //----------------
                #endregion

                #region " 各パッドのON/OFFパネル "
                //----------------
                foreach( AutoPlay種別? autoPlayType in Enum.GetValues( typeof( AutoPlay種別 ) ) )
                {
                    if( !autoPlayType.HasValue || autoPlayType.Value == AutoPlay種別.Unknown )
                        continue;

                    var typePanel = new パネル_ONOFFトグル(

                        パネル名:
                            autoPlayType.Value.ToString(),

                        初期状態はON:
                            ( userConfig.AutoPlay[ autoPlayType.Value ] ),

                        値の変更処理:
                            ( panel ) => {
                                userConfig.AutoPlay[ autoPlayType.Value ] = ( (パネル_ONOFFトグル) panel ).ONである;
                            }
                    );

                    自動演奏フォルダ.子パネルリスト.Add( typePanel );
                    this._パネル_自動演奏_ONOFFトグルリスト.Add( typePanel );
                }
                //----------------
                #endregion

                #region "「設定完了（戻る）」システムボタン
                //----------------
                自動演奏フォルダ.子パネルリスト.Add(

                    new パネル_システムボタン(

                        パネル名:
                            "設定完了（戻る）",

                        値の変更処理:
                            ( panel ) => {
                                this.親のパネルを選択する();
                                this.フェードインを開始する();
                            }
                    ) );
                //----------------
                #endregion

                自動演奏フォルダ.子パネルリスト.SelectFirst();
            }
            //----------------
            #endregion

            #region "「画面モード」リスト "
            //----------------
            this.パネルツリーのルートノード.子パネルリスト.Add(

                new パネル_文字列リスト(

                    パネル名:
                        "画面モード",

                    選択肢初期値リスト:
                        new[] { "ウィンドウ", "全画面" },

                    初期選択肢番号:
                        ( systemConfig.全画面モードである ) ? 1 : 0,

                    値の変更処理:
                        ( panel ) => {
                            bool 全画面モードにする = ( 1 == ( (パネル_文字列リスト) panel ).現在選択されている選択肢の番号 );

                            if( 全画面モードにする )
                                Global.AppForm.ScreenMode.ToFullscreenMode();
                            else
                                Global.AppForm.ScreenMode.ToWindowMode();

                            systemConfig.全画面モードである = 全画面モードにする;
                        }
                ) );
            //----------------
            #endregion
            #region "「譜面スピード」リスト "
            //----------------
            this.パネルツリーのルートノード.子パネルリスト.Add(

                new パネル_倍率(

                    パネル名:
                        "譜面スピード",

                    初期値:
                        userConfig.譜面スクロール速度,

                    最小倍率:
                        0.5,

                    最大倍率:
                        8.0,

                    増減量:
                        0.5,

                    値の変更処理:
                        ( panel ) => {
                            userConfig.譜面スクロール速度 = ( (パネル_倍率) panel ).現在値;
                        }
                    ) );
            //----------------
            #endregion
            #region "「演奏中の壁紙表示」ON/OFFトグル "
            //----------------
            this.パネルツリーのルートノード.子パネルリスト.Add(

                new パネル_ONOFFトグル(

                    パネル名:
                        "演奏中の壁紙表示",

                    初期状態はON:
                        userConfig.スコア指定の背景画像を表示する,

                    値の変更処理:
                        ( panel ) => {
                            userConfig.スコア指定の背景画像を表示する = ( (パネル_ONOFFトグル) panel ).ONである;
                        }
                ) );
            //----------------
            #endregion
            #region "「演奏中の動画表示」ON/OFFトグル "
            //----------------
            this.パネルツリーのルートノード.子パネルリスト.Add(

                new パネル_ONOFFトグル(

                    パネル名:
                        "演奏中の動画表示",

                    初期状態はON:
                        userConfig.演奏中に動画を表示する,

                    値の変更処理:
                        ( panel ) => {
                            userConfig.演奏中に動画を表示する = ( (パネル_ONOFFトグル) panel ).ONである;
                        }
                ) );
            //----------------
            #endregion
            #region "「演奏中の動画サイズ」リスト "
            //----------------
            this.パネルツリーのルートノード.子パネルリスト.Add(

                new パネル_文字列リスト(

                    パネル名:
                        "演奏中の動画サイズ",

                    選択肢初期値リスト:
                        new[] { "全画面", "中央寄せ" },

                    初期選択肢番号:
                        (int) userConfig.動画の表示サイズ,

                    値の変更処理:
                        ( panel ) => {
                            userConfig.動画の表示サイズ = (動画の表示サイズ) ( (パネル_文字列リスト) panel ).現在選択されている選択肢の番号;
                        }
                ) );
            //----------------
            #endregion
            #region "「シンバルフリー」ON/OFFトグル "
            //----------------
            this.パネルツリーのルートノード.子パネルリスト.Add(

                new パネル_ONOFFトグル(

                    パネル名:
                        "シンバルフリー",

                    初期状態はON:
                        userConfig.シンバルフリーモードである,

                    値の変更処理:
                        ( panel ) => {
                            userConfig.シンバルフリーモードである = ( (パネル_ONOFFトグル) panel ).ONである;
                            userConfig.ドラムチッププロパティリスト.反映する( ( userConfig.シンバルフリーモードである ) ?
                                入力グループプリセット種別.シンバルフリー :
                                入力グループプリセット種別.基本形 );
                        }
                ) );
            //----------------
            #endregion
            #region "「Rideの表示位置」リスト "
            //----------------
            this.パネルツリーのルートノード.子パネルリスト.Add(

                new パネル_文字列リスト(

                    パネル名:
                        "Rideの表示位置",

                    選択肢初期値リスト:
                        new[] { "左", "右" },

                    初期選択肢番号:
                        ( userConfig.表示レーンの左右.Rideは左 ) ? 0 : 1,

                    値の変更処理:
                        ( panel ) => {
                            userConfig.表示レーンの左右 = new 表示レーンの左右() {
                                Rideは左 = ( ( (パネル_文字列リスト) panel ).現在選択されている選択肢の番号 == 0 ),
                                Chinaは左 = userConfig.表示レーンの左右.Chinaは左,
                                Splashは左 = userConfig.表示レーンの左右.Splashは左,
                            };
                        }
                ) );
            //----------------
            #endregion
            #region "「Chinaの表示位置」リスト "
            //----------------
            this.パネルツリーのルートノード.子パネルリスト.Add(

                new パネル_文字列リスト(

                    パネル名:
                        "Chinaの表示位置",

                    選択肢初期値リスト:
                        new[] { "左", "右" },

                    初期選択肢番号:
                        ( userConfig.表示レーンの左右.Chinaは左 ) ? 0 : 1,

                    値の変更処理:
                        ( panel ) => {
                            userConfig.表示レーンの左右 = new 表示レーンの左右() {
                                Rideは左 = userConfig.表示レーンの左右.Rideは左,
                                Chinaは左 = ( ( (パネル_文字列リスト) panel ).現在選択されている選択肢の番号 == 0 ),
                                Splashは左 = userConfig.表示レーンの左右.Splashは左,
                            };
                        }
                ) );
            //----------------
            #endregion
            #region "「Splashの表示位置」リスト "
            //----------------
            this.パネルツリーのルートノード.子パネルリスト.Add(

                new パネル_文字列リスト(

                    パネル名:
                        "Splashの表示位置",

                    選択肢初期値リスト:
                        new[] { "左", "右" },

                    初期選択肢番号:
                        ( userConfig.表示レーンの左右.Splashは左 ) ? 0 : 1,

                    値の変更処理:
                        ( panel ) => {
                            userConfig.表示レーンの左右 = new 表示レーンの左右() {
                                Rideは左 = userConfig.表示レーンの左右.Rideは左,
                                Chinaは左 = userConfig.表示レーンの左右.Splashは左,
                                Splashは左 = ( ( (パネル_文字列リスト) panel ).現在選択されている選択肢の番号 == 0 ),
                            };
                        }
                ) );
            //----------------
            #endregion
            #region "「ドラムサウンド」ON/OFFトグル "
            //----------------
            this.パネルツリーのルートノード.子パネルリスト.Add(

                new パネル_ONOFFトグル(

                    パネル名:
                        "ドラムサウンド",

                    初期状態はON:
                        userConfig.ドラムの音を発声する,

                    値の変更処理:
                        ( panel ) => {
                            userConfig.ドラムの音を発声する = ( (パネル_ONOFFトグル) panel ).ONである;
                        }
                ) );
            //----------------
            #endregion
            #region "「レーンの透明度」数値ボックス "
            //----------------
            this.パネルツリーのルートノード.子パネルリスト.Add(

                new パネル_整数(

                    パネル名:
                        "レーンの透明度",

                    最小値:
                        0,

                    最大値:
                        100,

                    初期値:
                        userConfig.レーンの透明度,

                    増加減単位値:
                        5,

                    単位:
                        "%",

                    値の変更処理:
                        ( panel ) => {
                            userConfig.レーンの透明度 = ( (パネル_整数) panel ).現在の値;
                        }
                ) );
            //----------------
            #endregion
            #region "「演奏スピード」リスト "
            //----------------
            this.パネルツリーのルートノード.子パネルリスト.Add(

                new パネル_倍率(

                    パネル名:
                        "演奏スピード",

                    初期値:
                        userConfig.再生速度,

                    最小倍率:
                        0.1,

                    最大倍率:
                        2.0,

                    増減量:
                        0.1,

                    値の変更処理:
                        ( panel ) => {
                            userConfig.再生速度 = ( (パネル_倍率) panel ).現在値;

                            // WAVキャッシュをすべて削除するために、世代を２つ進める。
                            Global.App.WAVキャッシュ.世代を進める();
                            Global.App.WAVキャッシュ.世代を進める();
                        }
                    ) );
            //----------------
            #endregion
            #region "「小節線と拍線の表示」ON/OFFトグル "
            //----------------
            this.パネルツリーのルートノード.子パネルリスト.Add(

                new パネル_ONOFFトグル(

                    パネル名:
                        "小節線と拍線の表示",

                    初期状態はON:
                        userConfig.演奏中に小節線と拍線を表示する,

                    値の変更処理:
                        ( panel ) => {
                            userConfig.演奏中に小節線と拍線を表示する = ( (パネル_ONOFFトグル) panel ).ONである;
                        }
                ) );
            //----------------
            #endregion
            #region "「小節番号の表示」ON/OFFトグル "
            //----------------
            this.パネルツリーのルートノード.子パネルリスト.Add(

                new パネル_ONOFFトグル(

                    パネル名:
                        "小節番号の表示",

                    初期状態はON:
                        userConfig.演奏中に小節番号を表示する,

                    値の変更処理:
                        ( panel ) => {
                            userConfig.演奏中に小節番号を表示する = ( (パネル_ONOFFトグル) panel ).ONである;
                        }
                ) );
            //----------------
            #endregion
            #region "「判定FAST/SLOW値の表示」ON/OFFトグル "
            //----------------
            this.パネルツリーのルートノード.子パネルリスト.Add(

                new パネル_ONOFFトグル(

                    パネル名:
                        "判定FAST/SLOWの表示",

                    初期状態はON:
                        userConfig.演奏中に判定FastSlowを表示する,

                    値の変更処理:
                        ( panel ) => {
                            userConfig.演奏中に判定FastSlowを表示する = ( (パネル_ONOFFトグル) panel ).ONである;
                        }
                ) );
            //----------------
            #endregion
            #region "「音量によるサイズ変化」ON/OFFトグル "
            //----------------
            this.パネルツリーのルートノード.子パネルリスト.Add(

                new パネル_ONOFFトグル(

                    パネル名:
                        "音量によるサイズ変化",

                    初期状態はON:
                        userConfig.音量に応じてチップサイズを変更する,

                    値の変更処理:
                        ( panel ) => {
                            userConfig.音量に応じてチップサイズを変更する = ( (パネル_ONOFFトグル) panel ).ONである;
                        }
                ) );
            //----------------
            #endregion
            #region "「ダークモード」リスト "
            //----------------
            this.パネルツリーのルートノード.子パネルリスト.Add(

                new パネル_文字列リスト(

                    パネル名:
                        "ダークモード",

                    選択肢初期値リスト:
                        new[] { ("OFF", Color.Red), ("HALF", Color4.White), ("FULL", Color4.White) },

                    初期選択肢番号:
                        (int) userConfig.ダーク,

                    値の変更処理:
                        ( panel ) => {
                            userConfig.ダーク = (ダーク種別) ( (パネル_文字列リスト) panel ).現在選択されている選択肢の番号;
                        }
                ) );
            //----------------
            #endregion

            // ツリー構築・システム設定パート（全ユーザ共通）

            #region "「判定位置調整」数値ボックス "
            //----------------
            this.パネルツリーのルートノード.子パネルリスト.Add(

                new パネル_整数(

                    パネル名:
                        "判定位置調整",

                    最小値:
                        -99,

                    最大値:
                        +99,

                    初期値:
                        systemConfig.判定位置調整ms,

                    増加減単位値:
                        1,

                    単位:
                        "ms",

                    値の変更処理:
                        ( panel ) => {
                            systemConfig.判定位置調整ms = ( (パネル_整数) panel ).現在の値;
                        }
                ) );
            //----------------
            #endregion
            #region "「入力割り当て」パネル "
            //----------------
            this.パネルツリーのルートノード.子パネルリスト.Add(

                new パネル(

                    パネル名:
                        "入力割り当て",

                    値の変更処理:
                        ( panel ) => {
                            this.入力割り当てフェーズへ移行する();
                        },

                    ヘッダ色:
                        パネル.ヘッダ色種別.赤
                ) );
            //----------------
            #endregion
            #region "「曲読み込みフォルダ」パネル "
            //----------------
            this.パネルツリーのルートノード.子パネルリスト.Add(

                new パネル(

                    パネル名:
                        "曲読み込みフォルダ",

                    値の変更処理:
                        ( panel ) => {
                            this.曲読み込みフォルダ割り当てフェーズへ移行する();
                        },

                    ヘッダ色:
                        パネル.ヘッダ色種別.赤
                ) );
            //----------------
            #endregion

            #region "「初期化」フォルダ "
            //----------------
            {
                var 初期化フォルダ = new パネル_フォルダ(

                    パネル名:
                        "初期化",

                    親パネル:
                        this.パネルツリーのルートノード,

                    値の変更処理:
                        ( panel ) => {
                            this.子のパネルを選択する();
                            this.フェードインを開始する();
                        } );

                this.パネルツリーのルートノード.子パネルリスト.Add( 初期化フォルダ );

                // 子フォルダツリーの構築

                #region "「戻る」システムボタン "
                //----------------
                初期化フォルダ.子パネルリスト.Add(

                    new パネル_システムボタン(

                        パネル名:
                            "戻る",

                        値の変更処理:
                            ( panel ) => {
                                this.親のパネルを選択する();
                                this.フェードインを開始する();
                            }
                    ) );
                //----------------
                #endregion
                #region "「設定を初期化」パネル"
                //----------------
                初期化フォルダ.子パネルリスト.Add(

                    new パネル(

                        パネル名:
                            "設定を初期化",

                        値の変更処理:
                            ( panel ) => {
                                this._システム設定を初期化する();
                                this.再起動フェーズへ移行する();
                            }
                    ) );
                //----------------
                #endregion
                #region "「曲DBを初期化」パネル"
                //----------------
                初期化フォルダ.子パネルリスト.Add(

                    new パネル(

                        パネル名:
                            "曲DBを初期化",

                        値の変更処理:
                            ( panel ) => {
                                this._曲データベースを初期化する();
                                this.再起動フェーズへ移行する();
                            }
                    ) );
                //----------------
                #endregion
                #region "「ユーザDBを初期化」パネル"
                //----------------
                初期化フォルダ.子パネルリスト.Add(

                    new パネル(

                        パネル名:
                            "ユーザDBを初期化",

                        値の変更処理:
                            ( panel ) => {
                                this._ユーザデータベースを初期化する();
                                this.再起動フェーズへ移行する();
                            }
                    ) );
                //----------------
                #endregion
                #region "「成績DBを初期化」パネル "
                //----------------
                初期化フォルダ.子パネルリスト.Add(

                    new パネル(

                        パネル名:
                            "成績DBを初期化",

                        値の変更処理:
                            new Action<パネル>( ( panel ) => {
                                this._成績データベースを初期化する();
                                this.再起動フェーズへ移行する();
                            } )
                    ) );
                //----------------
                #endregion

                初期化フォルダ.子パネルリスト.SelectFirst();
            }
            //----------------
            #endregion

            #region "「設定完了」システムボタン "
            //----------------
            this.パネルツリーのルートノード.子パネルリスト.Add(

                new パネル_システムボタン(

                    パネル名:
                        "設定完了",

                    値の変更処理:
                        ( panel ) => {
                            this.フェードアウトを開始する();
                            Global.App.アイキャッチ管理.アイキャッチを選択しクローズする( nameof( シャッター ) );
                            this.フェードアウトフェーズへ移行する();
                        }
                ) );
            //----------------
            #endregion


            // 最後のパネルを選択。
            this.パネルツリーのルートノード.子パネルリスト.SelectLast();
            this.フェードインを開始する();

        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            Global.App.システム設定.保存する();
            Global.App.ログオン中のユーザ.保存する();

            foreach( var panel in this.パネルツリーのルートノード.Traverse() )
                panel.Dispose();
            this.パネルツリーのルートノード.Dispose();
            this._パネル_自動演奏_ONOFFトグルリスト.Clear(); // 要素はツリーに含まれるのでDispose不要

            this._パッド矢印.Dispose();
            this._青い線.Dispose();
        }



        // フェードイン・アウト


        public void フェードインを開始する( double 速度倍率 = 1.0 )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            for( int i = 0; i < this.現在のパネルフォルダ.子パネルリスト.Count; i++ )
            {
                this.現在のパネルフォルダ.子パネルリスト[ i ].フェードインを開始する( 0.02, 速度倍率 );
            }
        }

        public void フェードアウトを開始する( double 速度倍率 = 1.0 )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            for( int i = 0; i < this.現在のパネルフォルダ.子パネルリスト.Count; i++ )
            {
                this.現在のパネルフォルダ.子パネルリスト[ i ].フェードアウトを開始する( 0.02, 速度倍率 );
            }
        }



        // 選択


        public void 前のパネルを選択する()
        {
            this.現在のパネルフォルダ.子パネルリスト.SelectPrev( Loop: true );
        }

        public void 次のパネルを選択する()
        {
            this.現在のパネルフォルダ.子パネルリスト.SelectNext( Loop: true );
        }

        public void 親のパネルを選択する()
        {
            var 親パネル = this.現在のパネルフォルダ.親パネル;

            if( null != 親パネル )
                this.現在のパネルフォルダ = 親パネル;
        }

        public void 子のパネルを選択する()
        {
            var 子パネル = this.現在のパネルフォルダ.子パネルリスト.SelectedItem as パネル_フォルダ;

            if( null != 子パネル )
                this.現在のパネルフォルダ = 子パネル;
        }



        // 進行と描画


        public void 進行描画する( DeviceContext dc, float left, float top )
        {
            const float パネルの下マージン = 4f;
            float パネルの高さ = パネル.サイズ.Height + パネルの下マージン;


            // (1) フレーム１（たて線）を描画。

            this._青い線.描画する( dc, new Vector2( left, 0f ), 高さdpx: Global.設計画面サイズ.Height );


            // (2) パネルを描画。（選択中のパネルの3つ上から7つ下までの計11枚。）

            var panels = this.現在のパネルフォルダ.子パネルリスト;

            if( panels.SelectedItem is null )
                return;

            for( int i = 0; i < 11; i++ )
            {
                int 描画パネル番号 = ( ( panels.SelectedIndex - 3 + i ) + panels.Count * 3 ) % panels.Count;       // panels の末尾に達したら先頭に戻る。
                var 描画パネル = panels[ 描画パネル番号 ];

                描画パネル.進行描画する(
                    dc,
                    left + 22f,
                    top + i * パネルの高さ,
                    選択中: ( i == 3 ) );
            }


            // (3) フレーム２（選択パネル周囲）を描画。

            float 幅 = パネル.サイズ.Width + 22f * 2f;

            this._青い線.描画する( dc, new Vector2( left, パネルの高さ * 3f ), 幅dpx: 幅 );
            this._青い線.描画する( dc, new Vector2( left, パネルの高さ * 4f ), 幅dpx: 幅 );
            this._青い線.描画する( dc, new Vector2( left + 幅, パネルの高さ * 3f ), 高さdpx: パネルの高さ );


            // (4) パッド矢印（上＆下）を描画。

            this._パッド矢印.描画する( パッド矢印.種類.上_Tom1, new Vector2( left, パネルの高さ * 3f ) );
            this._パッド矢印.描画する( パッド矢印.種類.下_Tom2, new Vector2( left, パネルの高さ * 4f ) );
        }



        // ローカル


        // コード内で参照が必要になるパネルのホルダ。
        private readonly List<パネル_ONOFFトグル> _パネル_自動演奏_ONOFFトグルリスト;

        private readonly 青い線 _青い線;

        private readonly パッド矢印 _パッド矢印;

        private void _システム設定を初期化する()
        {
            // 現行化中なら終了する。

            Global.App.現行化.終了する();


            // ファイルを削除する。

            var path = SystemConfig.ConfigYamlPath;

            try
            {
                File.Delete( path.変数なしパス );  // ファイルがない場合には例外は出ない
            }
            catch( Exception e )
            {
                Log.ERROR( $"システム設定ファイルの削除に失敗しました。[{path.変数付きパス}][{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( e.Message )}]" );
            }
        }

        private void _曲データベースを初期化する()
        {
            // 現行化中なら終了する。

            Global.App.現行化.終了する();


            // ファイルを削除する。

            var path = ScoreDB.ScoreDBPath;

            try
            {
                File.Delete( path.変数なしパス );  // ファイルがない場合には例外は出ない
            }
            catch( Exception e )
            {
                Log.ERROR( $"曲データベースファイルの削除に失敗しました。[{path.変数付きパス}][{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( e.Message )}]" );
            }
        }

        private void _ユーザデータベースを初期化する()
        {
            // 現行化中なら終了する。

            Global.App.現行化.終了する();


            // ファイルを削除する。

            var path = new VariablePath( @$"$(AppData)\User_{Global.App.ログオン中のユーザ.ID}.yaml" );
            
            try
            {
                File.Delete( path.変数なしパス );  // ファイルがない場合には例外は出ない
            }
            catch( Exception e )
            {
                Log.ERROR( $"ユーザデータベースファイルの削除に失敗しました。[{path.変数付きパス}][{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( e.Message )}]" );
            }
        }

        private void _成績データベースを初期化する()
        {
            // 現行化中なら終了する。

            Global.App.現行化.終了する();


            // ファイルを削除する。

            var path = RecordDB.RecordDBPath;

            try
            {
                File.Delete( path.変数なしパス );  // ファイルがない場合には例外は出ない
            }
            catch( Exception e )
            {
                Log.ERROR( $"成績データベースファイルの削除に失敗しました。[{path.変数付きパス}][{Folder.絶対パスをフォルダ変数付き絶対パスに変換して返す( e.Message )}]" );
            }
        }
    }
}
