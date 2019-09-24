using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Animation;
using SharpDX.Direct2D1;
using FDK;
using SSTFormat.v4;

namespace DTXMania.選曲
{
    class 選曲ステージ : ステージ
    {
        public enum フェーズ
        {
            フェードイン,
            表示,
            フェードアウト,
            確定_選曲,
            確定_設定,
            キャンセル,
        }

        public フェーズ 現在のフェーズ { get; protected set; } = フェーズ.確定_選曲;



        // 生成と終了


        public 選曲ステージ()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
            }
        }

        public override void OnDispose()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                base.OnDispose();
            }
        }



        // 活性化と非活性化


        public override void On活性化()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this._舞台画像 = new 舞台画像( @"$(System)images\舞台_暗.jpg" );
                this._システム情報 = new システム情報();
                this._曲リスト = new 曲リスト();
                this._難易度と成績 = new 難易度と成績();
                this._曲ステータスパネル = new 曲ステータスパネル();
                this._ステージタイマー = new テクスチャ( @"$(System)images\選曲\ステージタイマー.png" );
                this._青い線 = new 青い線();
                this._選択曲枠ランナー = new 選択曲枠ランナー();
                this._BPMパネル = new BPMパネル();
                this._曲別スキルと達成率 = new 曲別スキルと達成率();
                this._表示方法選択パネル = new 表示方法選択パネル();
                this._SongNotFound = new 文字列画像() {
                    表示文字列 =
                        "Song not found...\n" +
                        "Hit BDx2 (in default SPACEx2) to select song folders."
                };
                this._UpdatingSoglistパネル = new UpdatingSoglistパネル();

                // 外部接続。
                this._難易度と成績.青い線を取得する = () => this._青い線;


                var dc = DXResources.Instance.既定のD2D1DeviceContext;

                this._白 = new SolidColorBrush( dc, Color4.White );
                this._黒 = new SolidColorBrush( dc, Color4.Black );
                this._黒透過 = new SolidColorBrush( dc, new Color4( Color3.Black, 0.5f ) );
                this._灰透過 = new SolidColorBrush( dc, new Color4( 0x80535353 ) );
                this._スクロールバー背景色 = new SolidColorBrush( dc, new Color4( 0.2f, 0.2f, 0.2f, 1.0f ) );
                this._スクロールバー枠色 = new SolidColorBrush( dc, Color4.Black );
                this._スクロールバーつまみ色 = new SolidColorBrush( dc, Color4.White );
                this._上に伸びる導線の長さdpx = null;
                this._左に伸びる導線の長さdpx = null;
                this._プレビュー枠の長さdpx = null;
                this._導線のストーリーボード = null;

                App進行描画.システムサウンド.再生する( システムサウンド種別.選曲ステージ_開始音 );

                this._フォーカスノードを初期化する();

                App進行描画.アイキャッチ管理.現在のアイキャッチ.オープンする();
                this._導線アニメをリセットする();

                this.現在のフェーズ = フェーズ.フェードイン;

                base.On活性化();
            }
        }

        public override void On非活性化()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this._スクロールバーつまみ色?.Dispose();
                this._スクロールバー枠色?.Dispose();
                this._スクロールバー背景色?.Dispose();
                this._灰透過?.Dispose();
                this._黒透過?.Dispose();
                this._黒?.Dispose();
                this._白?.Dispose();
                this._導線のストーリーボード?.Dispose();
                this._プレビュー枠の長さdpx?.Dispose();
                this._左に伸びる導線の長さdpx?.Dispose();
                this._上に伸びる導線の長さdpx?.Dispose();

                this._UpdatingSoglistパネル?.Dispose();
                this._SongNotFound?.Dispose();
                this._表示方法選択パネル?.Dispose();
                this._曲別スキルと達成率?.Dispose();
                this._BPMパネル?.Dispose();
                this._選択曲枠ランナー?.Dispose();
                this._青い線?.Dispose();
                this._ステージタイマー?.Dispose();
                this._曲ステータスパネル?.Dispose();
                this._難易度と成績?.Dispose();
                this._曲リスト?.Dispose();
                this._システム情報?.Dispose();
                this._舞台画像?.Dispose();

                base.On非活性化();
            }
        }



        // 進行と描画


        public override void 進行する()
        {
            this._システム情報.FPSをカウントしプロパティを更新する();

            App進行描画.入力管理.すべての入力デバイスをポーリングする();

            switch( this.現在のフェーズ )
            {
                case フェーズ.表示:
                    if( App進行描画.入力管理.確定キーが入力された() &&
                        1 < App進行描画.曲ツリー.フォーカスリスト.Count )   // 曲が1つ以上ある
                    {
                        #region " 確定 "
                        //----------------
                        if( App進行描画.曲ツリー.フォーカスノード is BoxNode boxNode )
                        {
                            App進行描画.システムサウンド.再生する( システムサウンド種別.決定音 );
                            this._曲リスト.BOXに入る();
                        }
                        else if( App進行描画.曲ツリー.フォーカスノード is BackNode backNode )
                        {
                            App進行描画.システムサウンド.再生する( システムサウンド種別.決定音 );
                            this._曲リスト.BOXから出る();
                        }
                        else if( null != App進行描画.曲ツリー.フォーカスノード )
                        {
                            if( App進行描画.曲ツリー.フォーカスノード is RandomSelectNode randomNode )
                            {
                                randomNode.選択曲を変更する();
                            }

                            // 選曲する

                            App進行描画.曲ツリー.フォーカスノード?.プレビュー音声を停止する();
                            App進行描画.システムサウンド.再生する( システムサウンド種別.選曲ステージ_曲決定音 );

                            App進行描画.アイキャッチ管理.アイキャッチを選択しクローズする( nameof( GO ) );
                            this.現在のフェーズ = フェーズ.フェードアウト;
                        }
                        //----------------
                        #endregion
                    }
                    else if( App進行描画.入力管理.キャンセルキーが入力された() )
                    {
                        #region " キャンセル "
                        //----------------
                        App進行描画.曲ツリー.フォーカスノード?.プレビュー音声を停止する();
                        App進行描画.システムサウンド.再生する( システムサウンド種別.取消音 );

                        if( App進行描画.曲ツリー.フォーカスノード != null &&
                            App進行描画.曲ツリー.フォーカスノード.親ノード != App進行描画.曲ツリー.ルートノード )
                        {
                            this._曲リスト.BOXから出る();
                        }
                        else
                        {
                            this.現在のフェーズ = フェーズ.キャンセル;
                        }
                        //----------------
                        #endregion
                    }
                    else if( App進行描画.入力管理.上移動キーが入力された() )
                    {
                        #region " 上移動 "
                        //----------------
                        if( null != App進行描画.曲ツリー.フォーカスノード )
                        {
                            App進行描画.システムサウンド.再生する( システムサウンド種別.カーソル移動音 );

                            //App.曲ツリー.前のノードをフォーカスする();	--> 曲リストへ委譲
                            this._曲リスト.前のノードを選択する();
                            this._導線アニメをリセットする();
                        }
                        //----------------
                        #endregion
                    }
                    else if( App進行描画.入力管理.下移動キーが入力された() )
                    {
                        #region " 下移動 "
                        //----------------
                        if( null != App進行描画.曲ツリー.フォーカスノード )
                        {
                            App進行描画.システムサウンド.再生する( システムサウンド種別.カーソル移動音 );

                            //App.曲ツリー.次のノードをフォーカスする();	--> 曲リストへ委譲
                            this._曲リスト.次のノードを選択する();
                            this._導線アニメをリセットする();
                        }
                        //----------------
                        #endregion
                    }
                    else if( App進行描画.入力管理.左移動キーが入力された() )
                    {
                        #region " 左移動 "
                        //----------------
                        App進行描画.システムサウンド.再生する( システムサウンド種別.変更音 );
                        this._表示方法選択パネル.前のパネルを選択する();
                        //----------------
                        #endregion
                    }
                    else if( App進行描画.入力管理.右移動キーが入力された() )
                    {
                        #region " 右移動 "
                        //----------------
                        App進行描画.システムサウンド.再生する( システムサウンド種別.変更音 );
                        this._表示方法選択パネル.次のパネルを選択する();
                        //----------------
                        #endregion
                    }
                    else if( App進行描画.入力管理.シーケンスが入力された( new[] { レーン種別.HiHat, レーン種別.HiHat }, App進行描画.ユーザ管理.ログオン中のユーザ.ドラムチッププロパティ管理 ) )
                    {
                        #region " HH×2 → 難易度変更 "
                        //----------------
                        App進行描画.曲ツリー.フォーカスノード?.プレビュー音声を停止する();
                        App進行描画.システムサウンド.再生する( システムサウンド種別.変更音 );

                        this._曲リスト.難易度アンカをひとつ増やす();

                        App進行描画.曲ツリー.フォーカスノード?.プレビュー音声を再生する();
                        //----------------
                        #endregion
                    }
                    else if( App進行描画.入力管理.シーケンスが入力された( new[] { レーン種別.Bass, レーン種別.Bass }, App進行描画.ユーザ管理.ログオン中のユーザ.ドラムチッププロパティ管理 ) )
                    {
                        #region " BD×2 → オプション設定 "
                        //----------------
                        App進行描画.曲ツリー.フォーカスノード?.プレビュー音声を停止する();
                        App進行描画.システムサウンド.再生する( システムサウンド種別.決定音 );

                        this.現在のフェーズ = フェーズ.確定_設定;
                        //----------------
                        #endregion
                    }
                    break;

                case フェーズ.フェードアウト:
                    if( App進行描画.アイキャッチ管理.現在のアイキャッチ.現在のフェーズ == アイキャッチ.フェーズ.クローズ完了 )
                        this.現在のフェーズ = フェーズ.確定_選曲;
                    break;
            }
        }

        public override void 描画する()
        {
            this._システム情報.VPSをカウントする();

            var dc = DXResources.Instance.既定のD2D1DeviceContext;
            dc.Transform = DXResources.Instance.拡大行列DPXtoPX;

            if( 1 < App進行描画.曲ツリー.フォーカスリスト.Count )
            {
                // (A) 曲がある場合　→　通常画面

                this._舞台画像.進行描画する( dc );
                this._曲リスト.進行描画する( dc );
                this._その他パネルを描画する( dc );
                this._表示方法選択パネル.進行描画する( dc );
                this._難易度と成績.描画する( dc, App進行描画.曲ツリー.フォーカス難易度 );
                this._曲ステータスパネル.描画する( dc );
                this._プレビュー画像を描画する( dc, App進行描画.曲ツリー.フォーカスノード );
                this._BPMパネル.描画する( dc );
                this._曲別スキルと達成率.進行描画する( dc );
                this._選択曲を囲む枠を描画する( dc );
                this._選択曲枠ランナー.進行描画する( dc );
                this._導線を描画する( dc );
                this._ステージタイマー.描画する( 1689f, 37f );
                this._スクロールバーを描画する( dc );
                this._UpdatingSoglistパネル.進行描画する( 40f, 740f );
            }
            else
            {
                // (B) 曲が１つもない場合 → Song Not Found 画面

                this._フォーカスノードを初期化する();     // ルートリストが設定された？

                this._舞台画像.進行描画する( dc );
                this._表示方法選択パネル.進行描画する( dc );
                this._ステージタイマー.描画する( 1689f, 37f );
                this._SongNotFound.描画する( dc, 1150f, 400f );
            }

            switch( this.現在のフェーズ )
            {
                case フェーズ.フェードイン:
                    App進行描画.アイキャッチ管理.現在のアイキャッチ.進行描画する( dc );
                    if( App進行描画.アイキャッチ管理.現在のアイキャッチ.現在のフェーズ == アイキャッチ.フェーズ.オープン完了 )
                        this.現在のフェーズ = フェーズ.表示;
                    this._システム情報.描画する( dc );
                    break;

                case フェーズ.表示:
                    this._システム情報.描画する( dc );
                    break;

                case フェーズ.フェードアウト:
                    App進行描画.アイキャッチ管理.現在のアイキャッチ.進行描画する( dc );
                    this._システム情報.描画する( dc );
                    break;
            }
        }



        // private


        private 舞台画像 _舞台画像 = null;

        private システム情報 _システム情報 = null;

        private 曲リスト _曲リスト = null;

        private 難易度と成績 _難易度と成績 = null;

        private 曲ステータスパネル _曲ステータスパネル = null;

        private 青い線 _青い線 = null;

        private 選択曲枠ランナー _選択曲枠ランナー = null;

        private BPMパネル _BPMパネル = null;

        private 曲別スキルと達成率 _曲別スキルと達成率 = null;

        private 表示方法選択パネル _表示方法選択パネル = null;

        private 文字列画像 _SongNotFound = null;

        private テクスチャ _ステージタイマー = null;

        private UpdatingSoglistパネル _UpdatingSoglistパネル = null;

        private SolidColorBrush _白 = null;

        private SolidColorBrush _黒 = null;

        private SolidColorBrush _黒透過 = null;

        private SolidColorBrush _灰透過 = null;

        private SolidColorBrush _スクロールバー背景色 = null;

        private SolidColorBrush _スクロールバー枠色 = null;

        private SolidColorBrush _スクロールバーつまみ色 = null;

        private readonly Vector3 _プレビュー画像表示位置dpx = new Vector3( 471f, 61f, 0f );

        private readonly Vector3 _プレビュー画像表示サイズdpx = new Vector3( 444f, 444f, 0f );


        private void _その他パネルを描画する( DeviceContext dc )
        {
            DXResources.Instance.D2DBatchDraw( dc, () => {

                dc.PrimitiveBlend = PrimitiveBlend.SourceOver;

                using( var ソートタブ上色 = new SolidColorBrush( dc, new Color4( 0xFF121212 ) ) )
                using( var ソートタブ下色 = new SolidColorBrush( dc, new Color4( 0xFF1f1f1f ) ) )
                {
                    // 曲リストソートタブ
                    dc.FillRectangle( new RectangleF( 927f, 50f, 993f, 138f ), ソートタブ上色 );
                    dc.FillRectangle( new RectangleF( 927f, 142f, 993f, 46f ), ソートタブ下色 );
                }

                // インフォメーションバー
                dc.FillRectangle( new RectangleF( 0f, 0f, 1920f, 50f ), this._黒 );
                dc.DrawLine( new Vector2( 0f, 50f ), new Vector2( 1920f, 50f ), this._白, strokeWidth: 1f );

                // ボトムバー
                dc.FillRectangle( new RectangleF( 0f, 1080f - 43f, 1920f, 1080f ), this._黒 );

                // プレビュー領域
                dc.FillRectangle( new RectangleF( 0f, 52f, 927f, 476f ), this._黒透過 );
                dc.DrawRectangle( new RectangleF( 0f, 52f, 927f, 476f ), this._灰透過, strokeWidth: 1f );
                dc.DrawLine( new Vector2( 1f, 442f ), new Vector2( 925f, 442f ), this._灰透過, strokeWidth: 1f );

            } );
        }

        private void _プレビュー画像を描画する( DeviceContext dc, Node ノード )
        {
            var 画像 = ノード.ノード画像 ?? Node.既定のノード画像;

            // テクスチャは画面中央が (0,0,0) で、Xは右がプラス方向, Yは上がプラス方向, Zは奥がプラス方向+。

            var 変換行列 =
                Matrix.Scaling(
                    this._プレビュー画像表示サイズdpx.X / 画像.サイズ.Width,
                    this._プレビュー画像表示サイズdpx.Y / 画像.サイズ.Height,
                    0f ) *
                Matrix.Translation(
                    DXResources.Instance.画面左上dpx.X + this._プレビュー画像表示位置dpx.X + this._プレビュー画像表示サイズdpx.X / 2f,
                    DXResources.Instance.画面左上dpx.Y - this._プレビュー画像表示位置dpx.Y - this._プレビュー画像表示サイズdpx.Y / 2f,
                    0f );

            画像.描画する( 変換行列 );
        }

        private void _選択曲を囲む枠を描画する( DeviceContext dc )
        {
            var 矩形 = new RectangleF( 1015f, 485f, 905f, 113f );

            this._青い線.描画する( dc, new Vector2( 矩形.Left - this._青枠のマージンdpx, 矩形.Top ), 幅dpx: 矩形.Width + this._青枠のマージンdpx * 2f );
            this._青い線.描画する( dc, new Vector2( 矩形.Left - this._青枠のマージンdpx, 矩形.Bottom ), 幅dpx: 矩形.Width + this._青枠のマージンdpx * 2f );
            this._青い線.描画する( dc, new Vector2( 矩形.Left, 矩形.Top - this._青枠のマージンdpx ), 高さdpx: 矩形.Height + this._青枠のマージンdpx * 2f );
        }

        private void _スクロールバーを描画する( DeviceContext dc )
        {
            var 全矩形 = new RectangleF( 1901f, 231f, 9f, 732f );  // 枠線含まず

            DXResources.Instance.D2DBatchDraw( dc, () => {

                dc.DrawRectangle( 全矩形, this._スクロールバー枠色, 4f );
                dc.FillRectangle( 全矩形, this._スクロールバー背景色 );

                int 曲数 = App進行描画.曲ツリー.フォーカスリスト.Count;

                if( 1 < 曲数 )
                {
                    float 曲の高さ = 全矩形.Height / 曲数;

                    var つまみ矩形 = new RectangleF(
                        全矩形.Left,
                        全矩形.Top + 曲の高さ * App進行描画.曲ツリー.フォーカスノードのインデックス,
                        全矩形.Width,
                        Math.Max( 2f, 曲の高さ ) );      // つまみは最小 2dpx 厚

                    dc.FillRectangle( つまみ矩形, this._スクロールバーつまみ色 );
                }

            } );
        }


        private Variable _上に伸びる導線の長さdpx = null;

        private Variable _左に伸びる導線の長さdpx = null;

        private Variable _プレビュー枠の長さdpx = null;

        private Storyboard _導線のストーリーボード = null;

        private readonly float _青枠のマージンdpx = 8f;


        private void _フォーカスノードを初期化する()
        {
            var tree = App進行描画.曲ツリー;

            if( null == tree.フォーカスノード )
            {
                // (A) 未選択なら、ルートノードの先頭ノードをフォーカスする。
                if( 0 < tree.ルートノード.子ノードリスト.Count )
                {
                    tree.フォーカスする( tree.ルートノード.子ノードリスト[ 0 ] );
                }
                else
                {
                    // ルートノードに子がないないなら null のまま。
                }
            }
            else
            {
                // (B) なんらかのノードを選択中なら、それを継続して使用する（フォーカスノードをリセットしない）。
                tree.フォーカスノード?.プレビュー音声を再生する();
            }
        }

        private void _導線アニメをリセットする()
        {
            var animation = DXResources.Instance.Animation;

            this._選択曲枠ランナー.リセットする();

            this._上に伸びる導線の長さdpx?.Dispose();
            this._上に伸びる導線の長さdpx = new Variable( animation.Manager, initialValue: 0.0 );

            this._左に伸びる導線の長さdpx?.Dispose();
            this._左に伸びる導線の長さdpx = new Variable( animation.Manager, initialValue: 0.0 );

            this._プレビュー枠の長さdpx?.Dispose();
            this._プレビュー枠の長さdpx = new Variable( animation.Manager, initialValue: 0.0 );

            this._導線のストーリーボード?.Abandon();
            this._導線のストーリーボード?.Dispose();
            this._導線のストーリーボード = new Storyboard( animation.Manager );

            double 期間 = 0.3;
            using( var 上に伸びる = animation.TrasitionLibrary.Constant( 期間 ) )
            using( var 左に伸びる = animation.TrasitionLibrary.Constant( 期間 ) )
            using( var 枠が広がる = animation.TrasitionLibrary.Constant( 期間 ) )
            {
                this._導線のストーリーボード.AddTransition( this._上に伸びる導線の長さdpx, 上に伸びる );
                this._導線のストーリーボード.AddTransition( this._左に伸びる導線の長さdpx, 左に伸びる );
                this._導線のストーリーボード.AddTransition( this._プレビュー枠の長さdpx, 枠が広がる );
            }

            期間 = 0.07;
            using( var 上に伸びる = animation.TrasitionLibrary.Linear( 期間, finalValue: 209.0 ) )
            using( var 左に伸びる = animation.TrasitionLibrary.Constant( 期間 ) )
            using( var 枠が広がる = animation.TrasitionLibrary.Constant( 期間 ) )
            {
                this._導線のストーリーボード.AddTransition( this._上に伸びる導線の長さdpx, 上に伸びる );
                this._導線のストーリーボード.AddTransition( this._左に伸びる導線の長さdpx, 左に伸びる );
                this._導線のストーリーボード.AddTransition( this._プレビュー枠の長さdpx, 枠が広がる );
            }

            期間 = 0.06;
            using( var 上に伸びる = animation.TrasitionLibrary.Constant( 期間 ) )
            using( var 左に伸びる = animation.TrasitionLibrary.Linear( 期間, finalValue: 129.0 ) )
            using( var 枠が広がる = animation.TrasitionLibrary.Constant( 期間 ) )
            {
                this._導線のストーリーボード.AddTransition( this._上に伸びる導線の長さdpx, 上に伸びる );
                this._導線のストーリーボード.AddTransition( this._左に伸びる導線の長さdpx, 左に伸びる );
                this._導線のストーリーボード.AddTransition( this._プレビュー枠の長さdpx, 枠が広がる );
            }

            期間 = 0.07;
            using( var 維持 = animation.TrasitionLibrary.Constant( 期間 ) )
            using( var 上に伸びる = animation.TrasitionLibrary.Constant( 期間 ) )
            using( var 左に伸びる = animation.TrasitionLibrary.Constant( 期間 ) )
            using( var 枠が広がる = animation.TrasitionLibrary.Linear( 期間, finalValue: 444.0 + this._青枠のマージンdpx * 2f ) )
            {
                this._導線のストーリーボード.AddTransition( this._上に伸びる導線の長さdpx, 上に伸びる );
                this._導線のストーリーボード.AddTransition( this._左に伸びる導線の長さdpx, 左に伸びる );
                this._導線のストーリーボード.AddTransition( this._プレビュー枠の長さdpx, 枠が広がる );
            }

            this._導線のストーリーボード.Schedule( animation.Timer.Time );
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
            this._青い線.描画する( dc, new Vector2( 右 + this._青枠のマージンdpx - z, 上 ), 幅dpx: z ); // 上辺
            this._青い線.描画する( dc, new Vector2( 右 + this._青枠のマージンdpx - z, 下 ), 幅dpx: z ); // 下辺
            this._青い線.描画する( dc, new Vector2( 左, 下 + this._青枠のマージンdpx - z ), 高さdpx: z ); // 左辺
            this._青い線.描画する( dc, new Vector2( 右, 下 + this._青枠のマージンdpx - z ), 高さdpx: z ); // 右辺
        }
    }
}
