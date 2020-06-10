using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Animation;
using FDK;
using SSTFormat.v004;
using DTXMania2.曲;

namespace DTXMania2.選曲
{
    class 選曲ステージ : IStage
    {

        // プロパティ


        public enum フェーズ
        {
            フェードイン,
            表示,
            QuickConfig,
            フェードアウト,
            確定_選曲,
            確定_設定,
            キャンセル,
        }

        public フェーズ 現在のフェーズ { get; protected set; } = フェーズ.確定_選曲;



        // 生成と終了


        public 選曲ステージ()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._舞台画像 = new 舞台画像( @"$(Images)\Background_Dark.jpg" );
            this._システム情報 = new システム情報();
            this._UpdatingSoglistパネル = new UpdatingSoglistパネル();
            this._表示方法選択パネル = new 表示方法選択パネル();
            this._青い線 = new 青い線();
            this._選択曲枠ランナー = new 選択曲枠ランナー();
            this._選曲リスト = new 選曲リスト();
            this._難易度と成績 = new 難易度と成績();
            this._難易度と成績.青い線を取得する = () => this._青い線;    // 外部依存アクションの接続
            this._曲ステータスパネル = new 曲ステータスパネル();
            this._BPMパネル = new BPMパネル();
            this._曲別スキルと達成率 = new 曲別スキルと達成率();
            this._ステージタイマー = new 画像D2D( @"$(Images)\SelectStage\StageTimer.png" );
            this._既定のノード画像 = new 画像D2D( @"$(Images)\DefaultPreviewImage.png" );
            this._現行化前のノード画像 = new 画像D2D( @"$(Images)\PreviewImageWaitForActivation.png" );
            this._SongNotFound = new 文字列画像D2D() {
                表示文字列 =
                    "Song not found...\n" +
                    "Hit BDx2 (in default SPACEx2) to select song folders.",
            };
            this._QuickConfig画面 = null!; // 使用時に生成
            this._フェートアウト後のフェーズ = フェーズ.確定_選曲;

            Global.App.システムサウンド.再生する( システムサウンド種別.選曲ステージ_開始音 );
            Global.App.アイキャッチ管理.現在のアイキャッチ.オープンする();
            this._導線アニメをリセットする();

            // 最初のフェーズへ。
            this.現在のフェーズ = フェーズ.フェードイン;
            this._フェーズ完了 = false;
        }

        public void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            //this._QuickConfig画面.Dispose(); 使用時に破棄。
            this._SongNotFound.Dispose();
            this._現行化前のノード画像.Dispose();
            this._既定のノード画像.Dispose();
            this._ステージタイマー.Dispose();
            this._曲別スキルと達成率.Dispose();
            this._BPMパネル.Dispose();
            this._曲ステータスパネル.Dispose();
            this._難易度と成績.Dispose();
            this._選曲リスト.Dispose();
            this._選択曲枠ランナー.Dispose();
            this._青い線.Dispose();
            this._表示方法選択パネル.Dispose();
            this._UpdatingSoglistパネル.Dispose();
            this._システム情報.Dispose();
            this._舞台画像.Dispose();
        }



        // 進行と描画


        public void 進行描画する()
        {
            var 入力 = Global.App.ドラム入力;
            var 曲ツリー = Global.App.曲ツリーリスト.SelectedItem!;
            var フォーカスリスト = 曲ツリー.フォーカスリスト;
            var フォーカスノード = 曲ツリー.フォーカスノード!;

            var dc = Global.既定のD2D1DeviceContext;
            dc.Transform = Global.拡大行列DPXtoPX;

            入力.すべての入力デバイスをポーリングする();

            this._システム情報.VPSをカウントする();
            this._システム情報.FPSをカウントしプロパティを更新する();

            switch( this.現在のフェーズ )
            {
                case フェーズ.フェードイン:
                {
                    #region " 背景画面＆フェードインを描画する。"
                    //----------------
                    dc.BeginDraw();

                    this._背景画面を描画する( dc );
                    if( Global.App.アイキャッチ管理.現在のアイキャッチ.進行描画する( dc ) == アイキャッチ.フェーズ.オープン完了 )
                    {
                        // フェードイン描画が完了したら次のフェーズへ。
                        this.現在のフェーズ = フェーズ.表示;

                        if( !this._フェーズ完了 )             // まだ再開されてなければ、
                            Global.App.現行化.再開する();     // 曲ツリーの現行化タスクが一時停止していれば、再開する。
                    }
                    this._システム情報.描画する( dc );

                    dc.EndDraw();
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.表示:
                {
                    #region " 入力処理。"
                    //----------------
                    if( 入力.確定キーが入力された() && 1 < フォーカスリスト.Count )   // ノードが2つ以上ある（1つはランダムセレクト）
                    {
                        #region " 確定 "
                        //----------------
                        if( フォーカスノード is BoxNode )
                        {
                            #region " BOX の場合 → BOX に入る。"
                            //----------------
                            Global.App.システムサウンド.再生する( システムサウンド種別.決定音 );
                            this._選曲リスト.BOXに入る();
                            //----------------
                            #endregion
                        }
                        else if( フォーカスノード is BackNode )
                        {
                            #region " Back の場合 → BOX から出る。"
                            //----------------
                            Global.App.システムサウンド.再生する( システムサウンド種別.決定音 );
                            this._選曲リスト.BOXから出る();
                            //----------------
                            #endregion
                        }
                        else if( フォーカスノード is RandomSelectNode randomNode )
                        {
                            #region " RANDOM SELECT の場合 → ランダムに選曲してフェードアウトへ。"
                            //----------------
                            // ランダムに選曲する。
                            var sn = randomNode.譜面をランダムに選んで返す();
                            if( sn.HasValue )
                            {
                                // 選択曲の現行化がまだであれば完了を待つ。
                                if( !sn.Value.曲.現行化済み )
                                {
                                    this._選曲リスト.指定したノードを優先して現行化する( sn.Value.曲 );
                                    while( !sn.Value.曲.現行化済み )
                                        Thread.Sleep( 100 );
                                }

                                // 曲ツリーの現行化タスクが動いていれば、一時停止する。
                                Global.App.現行化.一時停止する();

                                // 選曲する。
                                Global.App.演奏譜面 = sn.Value.譜面;

                                Global.App.システムサウンド.再生する( システムサウンド種別.選曲ステージ_曲決定音 );
                                Global.App.アイキャッチ管理.アイキャッチを選択しクローズする( nameof( GO ) );

                                // 次のフェーズへ。
                                this._フェートアウト後のフェーズ = フェーズ.確定_選曲;
                                this.現在のフェーズ = フェーズ.フェードアウト;
                            }
                            //----------------
                            #endregion
                        }
                        else if( フォーカスノード is SongNode snode && null != snode.曲.フォーカス譜面 )
                        {
                            #region " 曲の場合 → 選曲してフェードアウトへ。"
                            //----------------
                            // 選択曲の現行化がまだであれば完了を待つ。
                            if( !snode.現行化済み )
                            {
                                this._選曲リスト.フォーカスノードを優先して現行化する();
                                while( !snode.現行化済み )
                                    Thread.Sleep( 100 );
                            }

                            // 曲ツリーの現行化タスクが動いていれば、一時停止する。
                            Global.App.現行化.一時停止する();

                            // 選曲する。
                            Global.App.演奏譜面 = snode.曲.フォーカス譜面;

                            this._選曲リスト.プレビュー音声を停止する();
                            Global.App.システムサウンド.再生する( システムサウンド種別.選曲ステージ_曲決定音 );
                            Global.App.アイキャッチ管理.アイキャッチを選択しクローズする( nameof( GO ) );

                            // 次のフェーズへ。
                            this._フェートアウト後のフェーズ = フェーズ.確定_選曲;
                            this.現在のフェーズ = フェーズ.フェードアウト;
                            //----------------
                            #endregion
                        }
                        //----------------
                        #endregion
                    }
                    else if( 入力.キャンセルキーが入力された() )
                    {
                        #region " キャンセル "
                        //----------------
                        this._選曲リスト.プレビュー音声を停止する();
                        Global.App.システムサウンド.再生する( システムサウンド種別.取消音 );

                        if( null != フォーカスノード.親ノード!.親ノード )
                        {
                            #region " BOX 内にいる場合 → BOX から出る。"
                            //----------------
                            this._選曲リスト.BOXから出る();
                            //----------------
                            #endregion
                        }
                        else
                        {
                            #region " 曲階層のルートにいる場合 → キャンセルフェーズへ。"
                            //----------------
                            this.現在のフェーズ = フェーズ.キャンセル;
                            //----------------
                            #endregion
                        }
                        //----------------
                        #endregion
                    }
                    else if( 入力.上移動キーが入力された() )
                    {
                        #region " 上移動 "
                        //----------------
                        Global.App.システムサウンド.再生する( システムサウンド種別.カーソル移動音 );
                        this._選曲リスト.前のノードを選択する();
                        this._導線アニメをリセットする();
                        //----------------
                        #endregion
                    }
                    else if( 入力.下移動キーが入力された() )
                    {
                        #region " 下移動 "
                        //----------------
                        Global.App.システムサウンド.再生する( システムサウンド種別.カーソル移動音 );
                        this._選曲リスト.次のノードを選択する();
                        this._導線アニメをリセットする();
                        //----------------
                        #endregion
                    }
                    else if( 入力.左移動キーが入力された() )
                    {
                        #region " 左移動 "
                        //----------------
                        Global.App.システムサウンド.再生する( システムサウンド種別.変更音 );
                        this._表示方法選択パネル.前のパネルを選択する();
                        //----------------
                        #endregion
                    }
                    else if( 入力.右移動キーが入力された() )
                    {
                        #region " 右移動 "
                        //----------------
                        Global.App.システムサウンド.再生する( システムサウンド種別.変更音 );
                        this._表示方法選択パネル.次のパネルを選択する();
                        //----------------
                        #endregion
                    }
                    else if( 入力.シーケンスが入力された( new[] { レーン種別.HiHat, レーン種別.HiHat }, Global.App.ログオン中のユーザ.ドラムチッププロパティリスト ) )
                    {
                        #region " HH×2 → 難易度変更 "
                        //----------------
                        Global.App.システムサウンド.再生する( システムサウンド種別.変更音 );
                        曲ツリー.ユーザ希望難易度をひとつ増やす();
                        //----------------
                        #endregion
                    }
                    else if( 入力.シーケンスが入力された( new[] { レーン種別.Bass, レーン種別.Bass }, Global.App.ログオン中のユーザ.ドラムチッププロパティリスト ) )
                    {
                        #region " BD×2 → QuickConfig画面を生成し、QuickConfig フェーズへ "
                        //----------------
                        Global.App.システムサウンド.再生する( システムサウンド種別.決定音 );

                        this._選曲リスト.フォーカスノードを優先して現行化する();

                        this._QuickConfig画面 = new QuickConfig.QuickConfigパネル(
                            song: ( フォーカスノード is SongNode snode ) ? snode.曲 : null,
                            userId: Global.App.ログオン中のユーザ.ID! );

                        this.現在のフェーズ = フェーズ.QuickConfig;
                        //----------------
                        #endregion
                    }
                    //----------------
                    #endregion

                    #region " 背景画面を描画する。"
                    //----------------
                    dc.BeginDraw();

                    this._背景画面を描画する( dc );
                    this._システム情報.描画する( dc );

                    dc.EndDraw();
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.QuickConfig:
                {
                    #region " 背景画面＆QuickConfigを描画する。"
                    //----------------
                    dc.BeginDraw();

                    this._背景画面を描画する( dc );
                    this._システム情報.描画する( dc );
                    this._QuickConfig画面.進行描画する( dc, 568f, 68f );

                    dc.EndDraw();

                    if( this._QuickConfig画面.現在のフェーズ == QuickConfig.QuickConfigパネル.フェーズ.完了_戻る )
                    {
                        // 戻る → 表示フェーズへ
                        this._QuickConfig画面.Dispose();
                        this.現在のフェーズ = フェーズ.表示;
                    }
                    else if( this._QuickConfig画面.現在のフェーズ == QuickConfig.QuickConfigパネル.フェーズ.完了_オプション設定 )
                    {
                        // オプション設定 → 確定_設定フェーズへ。
                        this._選曲リスト.プレビュー音声を停止する();
                        this._QuickConfig画面.Dispose();
                        this.現在のフェーズ = フェーズ.確定_設定;
                    }
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.フェードアウト:
                {
                    #region " 背景画面＆フェードアウトを描画する。"
                    //----------------
                    dc.BeginDraw();

                    this._背景画面を描画する( dc );
                    if( Global.App.アイキャッチ管理.現在のアイキャッチ.進行描画する( dc ) == アイキャッチ.フェーズ.クローズ完了 )
                    {
                        // フェードアウト描画が完了したら次のフェーズへ。
                        this.現在のフェーズ = this._フェートアウト後のフェーズ; // フェードアウト開始時に設定済み
                    }
                    this._システム情報.描画する( dc );

                    dc.EndDraw();
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.確定_選曲:
                case フェーズ.確定_設定:
                case フェーズ.キャンセル:
                {
                    #region " 遷移終了。Appによるステージ遷移を待つ。"
                    //----------------
                    //----------------
                    #endregion

                    break;
                }
            }
        }



        // ローカル


        private readonly 舞台画像 _舞台画像;

        private readonly システム情報 _システム情報;

        private readonly UpdatingSoglistパネル _UpdatingSoglistパネル;

        private readonly 表示方法選択パネル _表示方法選択パネル;

        private readonly 青い線 _青い線;

        private readonly 選択曲枠ランナー _選択曲枠ランナー;

        private readonly 選曲リスト _選曲リスト;

        private readonly 文字列画像D2D _SongNotFound;

        private readonly 難易度と成績 _難易度と成績;

        private readonly 曲ステータスパネル _曲ステータスパネル;

        private readonly BPMパネル _BPMパネル;

        private readonly 曲別スキルと達成率 _曲別スキルと達成率;

        private readonly 画像D2D _ステージタイマー;

        private QuickConfig.QuickConfigパネル _QuickConfig画面;

        private フェーズ _フェートアウト後のフェーズ;

        private bool _フェーズ完了;


        private void _その他パネルを描画する( DeviceContext dc )
        {
            var preBlend = dc.PrimitiveBlend;

            dc.PrimitiveBlend = PrimitiveBlend.SourceOver;

            using( var ソートタブ上色 = new SolidColorBrush( dc, new Color4( 0xFF121212 ) ) )
            using( var ソートタブ下色 = new SolidColorBrush( dc, new Color4( 0xFF1f1f1f ) ) )
            {
                // 曲リストソートタブ
                dc.FillRectangle( new RectangleF( 927f, 50f, 993f, 138f ), ソートタブ上色 );
                dc.FillRectangle( new RectangleF( 927f, 142f, 993f, 46f ), ソートタブ下色 );
            }

            using( var 黒 = new SolidColorBrush( dc, Color4.Black ) )
            using( var 白 = new SolidColorBrush( dc, Color4.White ) )
            using( var 黒透過 = new SolidColorBrush( dc, new Color4( Color3.Black, 0.5f ) ) )
            using( var 灰透過 = new SolidColorBrush( dc, new Color4( 0x80535353 ) ) )
            {
                // インフォメーションバー
                dc.FillRectangle( new RectangleF( 0f, 0f, 1920f, 50f ), 黒 );
                dc.DrawLine( new Vector2( 0f, 50f ), new Vector2( 1920f, 50f ), 白, strokeWidth: 1f );

                // ボトムバー
                dc.FillRectangle( new RectangleF( 0f, 1080f - 43f, 1920f, 1080f ), 黒 );

                // プレビュー領域
                dc.FillRectangle( new RectangleF( 0f, 52f, 927f, 476f ), 黒透過 );
                dc.DrawRectangle( new RectangleF( 0f, 52f, 927f, 476f ), 灰透過, strokeWidth: 1f );
                dc.DrawLine( new Vector2( 1f, 442f ), new Vector2( 925f, 442f ), 灰透過, strokeWidth: 1f );
            }

            dc.PrimitiveBlend = preBlend;
        }

        private void _背景画面を描画する( DeviceContext dc )
        {
            var 曲ツリー = Global.App.曲ツリーリスト.SelectedItem!;

            if( 1 >= 曲ツリー.フォーカスリスト.Count )  // どのリストにも、最低減ランダムセレクトノードがある。
            {
                // (A) ノードがない場合 → SongNotFound 画面

                this._舞台画像.進行描画する( dc );
                this._表示方法選択パネル.進行描画する( dc );
                this._ステージタイマー.描画する( dc, 1689f, 37f );
                this._SongNotFound.描画する( dc, 1150f, 400f );
            }
            else
            {
                // (B) ノードがある場合 → 通常の画面

                this._舞台画像.進行描画する( dc );
                this._選曲リスト.進行描画する( dc );
                this._その他パネルを描画する( dc );
                this._表示方法選択パネル.進行描画する( dc );
                this._難易度と成績.進行描画する( dc, 曲ツリー.フォーカス難易度レベル, 曲ツリー.フォーカスノード! );
                this._曲ステータスパネル.進行描画する( dc, 曲ツリー.フォーカスノード! );
                this._プレビュー画像を描画する( dc, 曲ツリー.フォーカスノード! );
                this._BPMパネル.進行描画する( dc, 曲ツリー.フォーカスノード! );
                this._曲別スキルと達成率.進行描画する( dc, 曲ツリー.フォーカスノード! );
                this._選択曲を囲む枠を描画する( dc );
                this._選択曲枠ランナー.進行描画する( dc );
                this._導線を描画する( dc );
                this._ステージタイマー.描画する( dc, 1689f, 37f );
                this._スクロールバーを描画する( dc, 曲ツリー.フォーカスリスト );
                this._UpdatingSoglistパネル.進行描画する( dc, 40f, 740f );
            }
        }
        
        private void _選択曲を囲む枠を描画する( DeviceContext dc )
        {
            var 矩形 = new RectangleF( 1015f, 485f, 905f, 113f );

            this._青い線.描画する( dc, new Vector2( 矩形.Left - _青枠のマージンdpx, 矩形.Top ), 幅dpx: 矩形.Width + _青枠のマージンdpx * 2f );
            this._青い線.描画する( dc, new Vector2( 矩形.Left - _青枠のマージンdpx, 矩形.Bottom ), 幅dpx: 矩形.Width + _青枠のマージンdpx * 2f );
            this._青い線.描画する( dc, new Vector2( 矩形.Left, 矩形.Top - _青枠のマージンdpx ), 高さdpx: 矩形.Height + _青枠のマージンdpx * 2f );
        }

        private void _スクロールバーを描画する( DeviceContext dc, SelectableList<Node> フォーカスリスト )
        {
            int 曲数 = フォーカスリスト.Count;
            if( 2 > 曲数 )
                return; // 1曲なら表示しない。

            var 全矩形 = new RectangleF( 1901f, 231f, 9f, 732f );  // 枠線含まず

            using var スクロールバー背景色 = new SolidColorBrush( dc, new Color4( 0.2f, 0.2f, 0.2f, 1.0f ) );
            using var スクロールバー枠色 = new SolidColorBrush( dc, Color4.Black );
            using var スクロールバーつまみ色 = new SolidColorBrush( dc, Color4.White );

            dc.DrawRectangle( 全矩形, スクロールバー枠色, 4f );
            dc.FillRectangle( 全矩形, スクロールバー背景色 );

            float 曲の高さ = 全矩形.Height / 曲数;

            var つまみ矩形 = new RectangleF(
                全矩形.Left,
                全矩形.Top + 曲の高さ * フォーカスリスト.SelectedIndex,
                全矩形.Width,
                Math.Max( 2f, 曲の高さ ) );      // つまみは最小 2dpx 厚

            dc.FillRectangle( つまみ矩形, スクロールバーつまみ色 );
        }


        // プレビュー画像

        private readonly 画像D2D _既定のノード画像;

        private readonly 画像D2D _現行化前のノード画像;

        private readonly Vector3 _プレビュー画像表示位置dpx = new Vector3( 471f, 61f, 0f );

        private readonly Vector3 _プレビュー画像表示サイズdpx = new Vector3( 444f, 444f, 0f );

        private void _プレビュー画像を描画する( DeviceContext dc, Node フォーカスノード )
        {
            画像D2D image =
                ( !フォーカスノード.現行化済み ) ? this._現行化前のノード画像 :
                ( フォーカスノード.ノード画像 is null ) ? this._既定のノード画像 : フォーカスノード.ノード画像;

            var 変換行列2D =
                Matrix3x2.Scaling(
                    this._プレビュー画像表示サイズdpx.X / image.サイズ.Width,
                    this._プレビュー画像表示サイズdpx.Y / image.サイズ.Height ) *
                Matrix3x2.Translation(
                    this._プレビュー画像表示位置dpx.X,
                    this._プレビュー画像表示位置dpx.Y );

            image.描画する( dc, 変換行列2D );
        }


        // 導線

        private Variable _上に伸びる導線の長さdpx = null!;

        private Variable _左に伸びる導線の長さdpx = null!;

        private Variable _プレビュー枠の長さdpx = null!;

        private Storyboard _導線のストーリーボード = null!;

        private const float _青枠のマージンdpx = 8f;

        private void _導線アニメをリセットする()
        {
            this._選択曲枠ランナー.リセットする();

            this._上に伸びる導線の長さdpx?.Dispose();
            this._上に伸びる導線の長さdpx = new Variable( Global.Animation.Manager, initialValue: 0.0 );

            this._左に伸びる導線の長さdpx?.Dispose();
            this._左に伸びる導線の長さdpx = new Variable( Global.Animation.Manager, initialValue: 0.0 );

            this._プレビュー枠の長さdpx?.Dispose();
            this._プレビュー枠の長さdpx = new Variable( Global.Animation.Manager, initialValue: 0.0 );

            this._導線のストーリーボード?.Dispose();
            this._導線のストーリーボード = new Storyboard( Global.Animation.Manager );

            double 期間 = 0.3;
            using( var 上に伸びる = Global.Animation.TrasitionLibrary.Constant( 期間 ) )
            using( var 左に伸びる = Global.Animation.TrasitionLibrary.Constant( 期間 ) )
            using( var 枠が広がる = Global.Animation.TrasitionLibrary.Constant( 期間 ) )
            {
                this._導線のストーリーボード.AddTransition( this._上に伸びる導線の長さdpx, 上に伸びる );
                this._導線のストーリーボード.AddTransition( this._左に伸びる導線の長さdpx, 左に伸びる );
                this._導線のストーリーボード.AddTransition( this._プレビュー枠の長さdpx, 枠が広がる );
            }

            期間 = 0.07;
            using( var 上に伸びる = Global.Animation.TrasitionLibrary.Linear( 期間, finalValue: 209.0 ) )
            using( var 左に伸びる = Global.Animation.TrasitionLibrary.Constant( 期間 ) )
            using( var 枠が広がる = Global.Animation.TrasitionLibrary.Constant( 期間 ) )
            {
                this._導線のストーリーボード.AddTransition( this._上に伸びる導線の長さdpx, 上に伸びる );
                this._導線のストーリーボード.AddTransition( this._左に伸びる導線の長さdpx, 左に伸びる );
                this._導線のストーリーボード.AddTransition( this._プレビュー枠の長さdpx, 枠が広がる );
            }

            期間 = 0.06;
            using( var 上に伸びる = Global.Animation.TrasitionLibrary.Constant( 期間 ) )
            using( var 左に伸びる = Global.Animation.TrasitionLibrary.Linear( 期間, finalValue: 129.0 ) )
            using( var 枠が広がる = Global.Animation.TrasitionLibrary.Constant( 期間 ) )
            {
                this._導線のストーリーボード.AddTransition( this._上に伸びる導線の長さdpx, 上に伸びる );
                this._導線のストーリーボード.AddTransition( this._左に伸びる導線の長さdpx, 左に伸びる );
                this._導線のストーリーボード.AddTransition( this._プレビュー枠の長さdpx, 枠が広がる );
            }

            期間 = 0.07;
            using( var 維持 = Global.Animation.TrasitionLibrary.Constant( 期間 ) )
            using( var 上に伸びる = Global.Animation.TrasitionLibrary.Constant( 期間 ) )
            using( var 左に伸びる = Global.Animation.TrasitionLibrary.Constant( 期間 ) )
            using( var 枠が広がる = Global.Animation.TrasitionLibrary.Linear( 期間, finalValue: 444.0 + _青枠のマージンdpx * 2f ) )
            {
                this._導線のストーリーボード.AddTransition( this._上に伸びる導線の長さdpx, 上に伸びる );
                this._導線のストーリーボード.AddTransition( this._左に伸びる導線の長さdpx, 左に伸びる );
                this._導線のストーリーボード.AddTransition( this._プレビュー枠の長さdpx, 枠が広がる );
            }

            this._導線のストーリーボード.Schedule( Global.Animation.Timer.Time );
        }

        private void _導線を描画する( DeviceContext dc )
        {
            var h = (float) this._上に伸びる導線の長さdpx.Value;
            this._青い線.描画する( dc, new Vector2( 1044f, 485f - h ), 高さdpx: h );

            var w = (float) this._左に伸びる導線の長さdpx.Value;
            this._青い線.描画する( dc, new Vector2( 1046f - w, 278f ), 幅dpx: w );

            var z = (float) this._プレビュー枠の長さdpx.Value;   // マージン×2 込み
            var 上 = this._プレビュー画像表示位置dpx.Y;
            var 下 = this._プレビュー画像表示位置dpx.Y + this._プレビュー画像表示サイズdpx.Y;
            var 左 = this._プレビュー画像表示位置dpx.X;
            var 右 = this._プレビュー画像表示位置dpx.X + this._プレビュー画像表示サイズdpx.X;
            this._青い線.描画する( dc, new Vector2( 右 + _青枠のマージンdpx - z, 上 ), 幅dpx: z ); // 上辺
            this._青い線.描画する( dc, new Vector2( 右 + _青枠のマージンdpx - z, 下 ), 幅dpx: z ); // 下辺
            this._青い線.描画する( dc, new Vector2( 左, 下 + _青枠のマージンdpx - z ), 高さdpx: z ); // 左辺
            this._青い線.描画する( dc, new Vector2( 右, 下 + _青枠のマージンdpx - z ), 高さdpx: z ); // 右辺
        }
    }
}
