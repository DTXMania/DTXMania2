﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX.Direct2D1;
using FDK;
using DTXMania.設定;

namespace DTXMania.ステージ.オプション設定
{
    class オプション設定ステージ : ステージ
    {
        public enum フェーズ
        {
            フェードイン,
            表示,
            入力割り当て,
			曲読み込みフォルダ割り当て,
			曲読み込みフォルダ変更済み,
			フェードアウト,
            確定,
            キャンセル,
        }
        public フェーズ 現在のフェーズ { get; protected set; }


        public オプション設定ステージ()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this.子を追加する( this._舞台画像 = new 舞台画像() );
                this.子を追加する( this._パネルリスト = new パネルリスト() );
                //this.子を追加する( this._ルートパネルフォルダ = new パネル_フォルダ( "Root", null, null ) ); --> 活性化のたびに、子パネルとまとめて動的に追加する。
            }
        }

        protected override void On活性化()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this.現在のフェーズ = フェーズ.フェードイン;
                this._初めての進行描画 = true;

                #region " パネルフォルダツリーを構築する。"
                //----------------
                var user = App.ユーザ管理.ログオン中のユーザ;

                this._ルートパネルフォルダ = new パネル_フォルダ( "Root", null, null ) {
                    子パネルリスト = new SelectableList<パネル>(),
                };

                #region "「画面モード」リスト "
                //----------------
                this._ルートパネルフォルダ.子パネルリスト.Add(
                    
                    new パネル_文字列リスト(

                        パネル名:
                            "画面モード",
                        
                        選択肢初期値リスト:
                            new[] { "ウィンドウ", "全画面" },

                        初期選択肢番号:
                            ( user.全画面モードである ) ? 1 : 0,

                        値の変更処理: 
                            new Action<パネル>( ( panel ) => {
                                user.全画面モードである = ( 1 == ( (パネル_文字列リスト) panel ).現在選択されている選択肢の番号 );
                                App.全画面モード = user.全画面モードである;
                            } )
                    ) );
                //----------------
                #endregion
                #region "「演奏モード」リスト "
                //----------------
                this._ルートパネルフォルダ.子パネルリスト.Add(

                    new パネル_文字列リスト(

                        パネル名:
                            "演奏モード",

                        選択肢初期値リスト:
                            new[] { "BASIC", "EXPERT" },

                        初期選択肢番号:
                            (int) user.演奏モード,

                        値の変更処理:
                            new Action<パネル>( ( panel ) => {
                                user.演奏モード = (PlayMode) ( (パネル_文字列リスト) panel ).現在選択されている選択肢の番号;
                                user.ドラムチッププロパティ管理.反映する( user.演奏モード );
                            } )
                    ) );
                //----------------
                #endregion
                #region "「譜面スピード」リスト "
                //----------------
                this._ルートパネルフォルダ.子パネルリスト.Add(
                    new パネル_譜面スピード( "譜面スピード" ) );
                //----------------
                #endregion
                #region "「シンバルフリー」ON/OFFトグル "
                //----------------
                this._ルートパネルフォルダ.子パネルリスト.Add(

                    new パネル_ONOFFトグル(

                        パネル名:
                            "シンバルフリー",

                        初期状態はON:
                            user.シンバルフリーモードである,

                        値の変更処理:
                            new Action<パネル>( ( panel ) => {
                                user.シンバルフリーモードである = ( (パネル_ONOFFトグル) panel ).ONである;
                                user.ドラムチッププロパティ管理.反映する( ( user.シンバルフリーモードである ) ? 入力グループプリセット種別.シンバルフリー : 入力グループプリセット種別.基本形 );
                            } )
                    ) );
                //----------------
                #endregion
                #region "「Rideの表示位置」リスト "
                //----------------
                this._ルートパネルフォルダ.子パネルリスト.Add(

                    new パネル_文字列リスト(

                        パネル名:
                            "Rideの表示位置",

                        選択肢初期値リスト:
                            new[] { "左", "右" },

                        初期選択肢番号:
                            ( user.表示レーンの左右.Rideは左 ) ? 0 : 1,

                        値の変更処理:
                            new Action<パネル>( ( panel ) => {
                                user.表示レーンの左右 = new 表示レーンの左右() {
                                    Rideは左 = ( ( (パネル_文字列リスト) panel ).現在選択されている選択肢の番号 == 0 ),
                                    Chinaは左 = user.表示レーンの左右.Chinaは左,
                                    Splashは左 = user.表示レーンの左右.Splashは左,
                                };
                            } )
                    ) );
                //----------------
                #endregion
                #region "「Chinaの表示位置」リスト "
                //----------------
                this._ルートパネルフォルダ.子パネルリスト.Add(

                    new パネル_文字列リスト(

                        パネル名:
                            "Chinaの表示位置",

                        選択肢初期値リスト:
                            new[] { "左", "右" },

                        初期選択肢番号:
                            ( user.表示レーンの左右.Chinaは左 ) ? 0 : 1,

                        値の変更処理:
                            new Action<パネル>( ( panel ) => {
                                user.表示レーンの左右 = new 表示レーンの左右() {
                                    Rideは左 = user.表示レーンの左右.Rideは左,
                                    Chinaは左 = ( ( (パネル_文字列リスト) panel ).現在選択されている選択肢の番号 == 0 ),
                                    Splashは左 = user.表示レーンの左右.Splashは左,
                                };
                            } )
                    ) );
                //----------------
                #endregion
                #region "「Splashの表示位置」リスト "
                //----------------
                this._ルートパネルフォルダ.子パネルリスト.Add(

                    new パネル_文字列リスト(

                        パネル名:
                            "Splashの表示位置",

                        選択肢初期値リスト:
                            new[] { "左", "右" },

                        初期選択肢番号:
                            ( user.表示レーンの左右.Splashは左 ) ? 0 : 1,

                        値の変更処理:
                            new Action<パネル>( ( panel ) => {
                                user.表示レーンの左右 = new 表示レーンの左右() {
                                    Rideは左 = user.表示レーンの左右.Rideは左,
                                    Chinaは左 = user.表示レーンの左右.Splashは左,
                                    Splashは左 = ( ( (パネル_文字列リスト) panel ).現在選択されている選択肢の番号 == 0 ),
                                };
                            } )
                    ) );
                //----------------
                #endregion
                #region "「ドラムサウンド」ON/OFFトグル "
                //----------------
                this._ルートパネルフォルダ.子パネルリスト.Add(

                    new パネル_ONOFFトグル(

                        パネル名:
                            "ドラムサウンド",

                        初期状態はON:
                            user.ドラムの音を発声する,

                        値の変更処理:
                            new Action<パネル>( ( panel ) => {
                                user.ドラムの音を発声する = ( (パネル_ONOFFトグル) panel ).ONである;
                            } )
                    ) );
                //----------------
                #endregion
                #region "「レーン配置」リスト "
                //----------------
                {
                    var 選択肢リスト = 演奏.レーンフレーム.レーン配置リスト.Keys.ToList();

                    this._ルートパネルフォルダ.子パネルリスト.Add(

                        new パネル_文字列リスト(

                            パネル名:
                                "レーン配置",

                            選択肢初期値リスト:
                                選択肢リスト,

                            初期選択肢番号:
                                ( 選択肢リスト.Contains( user.レーン配置 ) ) ? 選択肢リスト.IndexOf( user.レーン配置 ) : 0,

                            値の変更処理:
                                new Action<パネル>( ( panel ) => {
                                    user.レーン配置 = 選択肢リスト[ ( (パネル_文字列リスト) panel ).現在選択されている選択肢の番号 ];
                                } )
                        ) );
                }
                //----------------
                #endregion

                #region "「入力発声スレッドのスリープ量」リスト "
                //----------------
                this._ルートパネルフォルダ.子パネルリスト.Add(

                    new パネル_文字列リスト(

                        パネル名: 
                            "入力発声スレッドのスリープ量",

                        選択肢初期値リスト:
                            new[] { "1 ms", "2 ms", "3 ms", "4 ms", "5 ms", "6 ms", "7 ms", "8 ms", "9 ms", "10 ms" },

                        初期選択肢番号:
                            ( App.システム設定.入力発声スレッドのスリープ量ms - 1 ),   // 1～10 → 0～9

                        値の変更処理:
                            new Action<パネル>( ( panel ) => {
                                App.システム設定.入力発声スレッドのスリープ量ms = ( (パネル_文字列リスト) panel ).現在選択されている選択肢の番号 + 1;  // 0～9 → 1～10
                            } )
                    ) );
                //----------------
                #endregion
                #region "「入力割り当て」パネル "
                //----------------
                this._ルートパネルフォルダ.子パネルリスト.Add(
                    this._パネル_入力割り当て = new パネル( "入力割り当て" ) {
                        ヘッダ色 = パネル.ヘッダ色種別.赤,
                    } );
				//----------------
				#endregion
				#region "「曲読み込みフォルダ」パネル "
				//----------------
				this._ルートパネルフォルダ.子パネルリスト.Add(
                    this._パネル_曲読み込みフォルダ = new パネル( "曲読み込みフォルダ" ) {
						ヘッダ色 = パネル.ヘッダ色種別.赤,
					} );
                //----------------
                #endregion

                #region "「自動演奏」フォルダ"
                //----------------
                {
                    var 自動演奏フォルダ = new パネル_フォルダ( "自動演奏", this._ルートパネルフォルダ ) {
                        ヘッダ色 = パネル.ヘッダ色種別.赤,
                    };
                    this._ルートパネルフォルダ.子パネルリスト.Add( 自動演奏フォルダ );

                    #region " 子フォルダツリーの構築 "
                    //----------------
                    自動演奏フォルダ.子パネルリスト = new SelectableList<パネル>();

                    自動演奏フォルダ.子パネルリスト.Add(
                        this._パネル_自動演奏_すべてONOFF = new パネル( "すべてON/OFF" ) );

                    this._パネル_自動演奏_ONOFFトグルリスト = new List<パネル_ONOFFトグル>();

                    foreach( AutoPlay種別 apType in Enum.GetValues( typeof( AutoPlay種別 ) ) )
                    {
                        if( apType == AutoPlay種別.Unknown )
                            continue;

                        var typePanel = new パネル_ONOFFトグル(

                            パネル名:
                                apType.ToString(),

                            初期状態はON:
                                ( user.AutoPlay[ apType ] ),

                            値の変更処理:
                                new Action<パネル>( ( panel ) => {
                                    user.AutoPlay[ apType ] = ( (パネル_ONOFFトグル) panel ).ONである;
                                } )
                        );

                        自動演奏フォルダ.子パネルリスト.Add( typePanel );
                        this._パネル_自動演奏_ONOFFトグルリスト.Add( typePanel );
                    }

                    自動演奏フォルダ.子パネルリスト.Add(
                        this._パネル_自動演奏_設定完了_戻る = new パネル_システムボタン( "設定完了（戻る）" ) );

                    自動演奏フォルダ.子パネルリスト.SelectFirst();
                    //----------------
                    #endregion
                }
                //----------------
                #endregion

                #region "「設定完了」システムボタン "
                //----------------
                this._ルートパネルフォルダ.子パネルリスト.Add(
                    this._パネル_設定完了 = new パネル_システムボタン( "設定完了" ) );
                //----------------
                #endregion

                //----------------
                #endregion

                // 最後のパネルを選択。
                this._ルートパネルフォルダ.子パネルリスト.SelectLast();

                // ルートパネルフォルダを最初のツリーとして表示する。
                this._パネルリスト.パネルリストを登録する( this._ルートパネルフォルダ );

                // ルートパネルフォルダを活性化する。
                this._ルートパネルフォルダ.活性化する();
            }
        }

        protected override void On非活性化()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this._ルートパネルフォルダ.非活性化する();
                this._ルートパネルフォルダ = null;
            }
        }

        public override void 進行描画する( DeviceContext1 dc )
        {
            // (1) 全フェーズ共通の進行描画。

            if( this._初めての進行描画 )
            {
                this._舞台画像.ぼかしと縮小を適用する( 0.5 );
                this._初めての進行描画 = false;
            }

            this._舞台画像.進行描画する( dc );
            this._パネルリスト.進行描画する( dc, 613f, 0f );


            // (2) フェーズ別の進行描画。

            switch( this.現在のフェーズ )
            {
                case フェーズ.フェードイン:
                    this._パネルリスト.フェードインを開始する();
                    this.現在のフェーズ = フェーズ.表示;
                    break;

                case フェーズ.表示:
                    break;

                case フェーズ.入力割り当て:
					using( Log.Block( FDKUtilities.現在のメソッド名 ) )
					{
						using( var dlg = new 入力割り当てダイアログ() )
							dlg.表示する();
					}
					this._パネルリスト.フェードインを開始する();
                    this.現在のフェーズ = フェーズ.表示;
                    break;

				case フェーズ.曲読み込みフォルダ割り当て:
					{
						bool 変更された = false;
						using( Log.Block( FDKUtilities.現在のメソッド名 ) )
						{
							using( var dlg = new 曲読み込みフォルダ割り当てダイアログ() )
								変更された = dlg.表示する();
						}
						this._パネルリスト.フェードインを開始する();
						this.現在のフェーズ = ( 変更された ) ? フェーズ.曲読み込みフォルダ変更済み : フェーズ.表示;
					}
					break;

                case フェーズ.曲読み込みフォルダ変更済み:
                    break;

                case フェーズ.フェードアウト:
                    App.ステージ管理.現在のアイキャッチ.進行描画する( dc );
                    if( App.ステージ管理.現在のアイキャッチ.現在のフェーズ == アイキャッチ.アイキャッチ.フェーズ.クローズ完了 )
                        this.現在のフェーズ = フェーズ.確定;
                    break;

                case フェーズ.確定:
                case フェーズ.キャンセル:
                    break;
            }

            
            // (3) フェーズ別の入力。

            App.入力管理.すべての入力デバイスをポーリングする();

            switch( this.現在のフェーズ )
            {
                case フェーズ.表示:

                    if( App.入力管理.キャンセルキーが入力された() )
                    {
                        this._パネルリスト.フェードアウトを開始する();
                        App.ステージ管理.アイキャッチを選択しクローズする( nameof( アイキャッチ.半回転黒フェード ) );
                        this.現在のフェーズ = フェーズ.フェードアウト;
                    }
                    else if( App.入力管理.上移動キーが入力された() )
                    {
                        this._パネルリスト.前のパネルを選択する();
                    }
                    else if( App.入力管理.下移動キーが入力された() )
                    {
                        this._パネルリスト.次のパネルを選択する();
                    }
                    else if( App.入力管理.左移動キーが入力された() )
                    {
                        this._パネルリスト.現在選択中のパネル.左移動キーが入力された();
                    }
                    else if( App.入力管理.右移動キーが入力された() )
                    {
                        this._パネルリスト.現在選択中のパネル.右移動キーが入力された();
                    }
                    else if( App.入力管理.確定キーが入力された() )
                    {
                        #region " 選択中のパネルの型と名前に応じて処理分岐。"
                        //----------------
                        switch( this._パネルリスト.現在選択中のパネル )
                        {
                            case パネル_フォルダ folder:
                                this._パネルリスト.子のパネルを選択する();
                                this._パネルリスト.フェードインを開始する();
                                break;

                            case パネル panel when( panel.パネル名 == this._パネル_入力割り当て.パネル名 ):
                                this.現在のフェーズ = フェーズ.入力割り当て;
                                break;

                            case パネル panel when( panel.パネル名 == this._パネル_曲読み込みフォルダ.パネル名 ):
                                this.現在のフェーズ = フェーズ.曲読み込みフォルダ割り当て;
                                break;

                            case パネル panel when( panel.パネル名 == this._パネル_設定完了.パネル名 ):
                                this._パネルリスト.フェードアウトを開始する();
                                App.ステージ管理.アイキャッチを選択しクローズする( nameof( アイキャッチ.シャッター ) );
                                this.現在のフェーズ = フェーズ.フェードアウト;
                                break;

                            case パネル panel when( panel.パネル名 == this._パネル_自動演奏_設定完了_戻る.パネル名 ):
                                this._パネルリスト.親のパネルを選択する();
                                this._パネルリスト.フェードインを開始する();
                                break;

                            case パネル panel when( panel.パネル名 == this._パネル_自動演奏_すべてONOFF.パネル名 ):
                                {
                                    bool 設定値 = !( this._パネル_自動演奏_ONOFFトグルリスト[ 0 ].ONである );  // 最初の項目値の反対にそろえる

                                    foreach( var typePanel in this._パネル_自動演奏_ONOFFトグルリスト )
                                        typePanel.ONである = 設定値;
                                }
                                break;

                            default:
                                this._パネルリスト.現在選択中のパネル.確定キーが入力された();    // パネルに処理させる
                                break;
                        }
                        //----------------
                        #endregion
                    }

                    break;
            }
        }


        private bool _初めての進行描画 = true;

        private 舞台画像 _舞台画像 = null;

        private パネルリスト _パネルリスト = null;

        private パネル_フォルダ _ルートパネルフォルダ = null;


        // 以下、コード内で参照が必要なパネルのホルダ。

        private パネル _パネル_入力割り当て = null;
        private パネル _パネル_曲読み込みフォルダ = null;
        private パネル _パネル_自動演奏_すべてONOFF = null;
        private List<パネル_ONOFFトグル> _パネル_自動演奏_ONOFFトグルリスト = null;
        private パネル_システムボタン _パネル_自動演奏_設定完了_戻る = null;
        private パネル_システムボタン _パネル_設定完了 = null;
    }
}
