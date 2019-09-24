using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using FDK;

using Record = DTXMania.Record08;

namespace DTXMania.結果
{
    class 結果ステージ : ステージ
    {
        public enum フェーズ
        {
            表示,
            フェードアウト,
            完了,
        }

        public フェーズ 現在のフェーズ { get; protected set; } = フェーズ.完了;



        // 外部依存アクション


        internal Func<成績> 結果を取得する = null;

        internal Action BGMを停止する = null;



        // 生成と終了


        public 結果ステージ()
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
                this._背景 = new 舞台画像();
                this._曲名パネル = new 画像( @"$(System)images\結果\曲名パネル.png" );
                this._曲名画像 = new 文字列画像() {
                    フォント名 = "HGMaruGothicMPRO",
                    フォントサイズpt = 40f,
                    フォント幅 = FontWeight.Regular,
                    フォントスタイル = FontStyle.Normal,
                    描画効果 = 文字列画像.効果.縁取り,
                    縁のサイズdpx = 6f,
                    前景色 = Color4.Black,
                    背景色 = Color4.White,
                };
                this._サブタイトル画像 = new 文字列画像() {
                    フォント名 = "HGMaruGothicMPRO",
                    フォントサイズpt = 25f,
                    フォント幅 = FontWeight.Regular,
                    フォントスタイル = FontStyle.Normal,
                    描画効果 = 文字列画像.効果.縁取り,
                    縁のサイズdpx = 5f,
                    前景色 = Color4.Black,
                    背景色 = Color4.White,
                };
                this._演奏パラメータ結果 = new 演奏パラメータ結果();
                this._ランク = new ランク();
                this._難易度 = new 難易度();
                this._曲別SKILL = new 曲別SKILL();
                this._達成率 = new 達成率();
                this._システム情報 = new システム情報();


                var 選択曲 = App進行描画.曲ツリー.フォーカス曲ノード;

                this._結果 = this.結果を取得する();

                App進行描画.システムサウンド.再生する( システムサウンド種別.ステージクリア );


                // 成績の反映。ただし、全AUTOなら行わない。

                if( !( App進行描画.ユーザ管理.ログオン中のユーザ.AutoPlayがすべてONである ) )
                {
                    this._成績を反映する();
                }

                this._曲名画像.表示文字列 = 選択曲.タイトル;
                this._サブタイトル画像.表示文字列 = 選択曲.サブタイトル;

                var dc = DXResources.Instance.既定のD2D1DeviceContext;

                this._黒マスクブラシ = new SolidColorBrush( dc, new Color4( Color3.Black, 0.75f ) );
                this._プレビュー枠ブラシ = new SolidColorBrush( dc, new Color4( 0xFF209292 ) );

                this._背景.ぼかしと縮小を適用する( 0.0 );    // 即時適用

                this.現在のフェーズ = フェーズ.表示;

                base.On活性化();
            }
        }

        public override void On非活性化()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                App進行描画.システムサウンド.停止する( システムサウンド種別.ステージクリア );

                this._結果 = null;

                this._黒マスクブラシ?.Dispose();
                this._プレビュー枠ブラシ?.Dispose();

                this.BGMを停止する();

                App進行描画.WAV管理?.Dispose();

                this._背景?.Dispose();
                this._曲名パネル?.Dispose();
                this._曲名画像?.Dispose();
                this._サブタイトル画像?.Dispose();
                this._演奏パラメータ結果?.Dispose();
                this._ランク?.Dispose();
                this._難易度?.Dispose();
                this._曲別SKILL?.Dispose();
                this._達成率?.Dispose();
                this._システム情報?.Dispose();

                base.On非活性化();
            }
        }

        private void _成績を反映する()
        {
            var 選択曲 = App進行描画.曲ツリー.フォーカス曲ノード;


            // 成績を、RecordDB へ追加または反映する。

            var ユーザID = App進行描画.ユーザ管理.ログオン中のユーザ.ユーザID;
            using( var recorddb = new RecordDB() )
            {
                var record = recorddb.Records.Where( ( r ) => ( r.UserId == ユーザID && r.SongHashId == 選択曲.曲ファイルのハッシュ ) ).SingleOrDefault();

                if( null == record )
                {
                    // (A) レコードが存在しない → 新規追加する。

                    recorddb.Records.InsertOnSubmit( new Record() {
                        UserId = ユーザID,
                        SongHashId = 選択曲.曲ファイルのハッシュ,
                        Score = this._結果.Score,
                        CountMap = "",  // TODO: CountMap を成績DBに保存する。
                        Achievement = this._結果.Achievement,
                    } );
                }
                else
                {
                    // (B) レコードがすでに存在する → 記録を更新していれば、更新する。

                    if( record.Achievement < this._結果.Achievement )
                    {
                        record.Score = this._結果.Score;
                        record.Achievement = this._結果.Achievement;
                        // TODO: CountMap を成績DBに保存する。
                        //record.CountMap = ...
                    }
                }

                recorddb.DataContext.SubmitChanges();
            }


            // 成績を、曲ツリーへ反映する。

            選択曲.達成率 = this._結果.Achievement;
        }



        // 進行と描画


        public override void 進行する()
        {
            this._システム情報.FPSをカウントしプロパティを更新する();


            // 入力

            App進行描画.入力管理.すべての入力デバイスをポーリングする();

            switch( this.現在のフェーズ )
            {
                case フェーズ.表示:

                    if( App進行描画.入力管理.確定キーが入力された() ||
                        App進行描画.入力管理.キャンセルキーが入力された() )
                    {
                        #region " 確定キー　→　フェーズアウトへ "
                        //----------------
                        App進行描画.システムサウンド.再生する( システムサウンド種別.取消音 );    // 確定だけど取消音
                        App進行描画.アイキャッチ管理.アイキャッチを選択しクローズする( nameof( シャッター ) );

                        this.現在のフェーズ = フェーズ.フェードアウト;
                        //----------------
                        #endregion
                    }
                    else if( App進行描画.ユーザ管理.ログオン中のユーザ.ドラムの音を発声する )
                    {
                        #region " その他　→　空うちサウンドを再生 "
                        //----------------
                        // すべての押下入力について……
                        foreach( var 入力 in App進行描画.入力管理.ポーリング結果.Where( ( e ) => e.InputEvent.押された ) )
                        {
                            var 押下入力に対応するすべてのドラムチッププロパティのリスト
                                = App進行描画.ユーザ管理.ログオン中のユーザ.ドラムチッププロパティ管理.チップtoプロパティ.Where( ( kvp ) => ( kvp.Value.ドラム入力種別 == 入力.Type ) );

                            foreach( var kvp in 押下入力に対応するすべてのドラムチッププロパティのリスト )
                            {
                                var ドラムチッププロパティ = kvp.Value;

                                if( 0 < App進行描画.演奏スコア.空打ちチップマップ.Count )
                                {
                                    #region " (A) 空うちチップマップが存在する場合（DTX他の場合）"
                                    //----------------
                                    int zz = App進行描画.演奏スコア.空打ちチップマップ[ ドラムチッププロパティ.レーン種別 ];  // WAVのzz番号。登録されていなければ 0

                                    if( 0 != zz )
                                    {
                                        // (A-a) 空打ちチップの指定があるなら、それを発声する。
                                        App進行描画.WAV管理.発声する( zz, ドラムチッププロパティ.チップ種別, ドラムチッププロパティ.発声前消音, ドラムチッププロパティ.消音グループ種別, BGM以外も再生する: true );
                                    }
                                    else
                                    {
                                        // (A-b) 空打ちチップの指定がないなら、入力に対応する一番最後のチップを検索し、それを発声する。

                                        var chip = this.一番最後のチップを返す( 入力.Type );

                                        if( null != chip )
                                        {
                                            this.チップの発声を行う( chip, true );
                                            break;  // 複数のチップが該当する場合でも、最初のチップの発声のみ行う。
                                        }
                                    }
                                    //----------------
                                    #endregion
                                }
                                else
                                {
                                    #region " (B) 空うちチップマップ未使用の場合（SSTFの場合）"
                                    //----------------
                                    App進行描画.ドラムサウンド.発声する( ドラムチッププロパティ.チップ種別, 0, ドラムチッププロパティ.発声前消音, ドラムチッププロパティ.消音グループ種別 );
                                    //----------------
                                    #endregion
                                }
                            }
                        }
                        //----------------
                        #endregion
                    }
                    break;
            }
        }

        public override void 描画する()
        {
            this._システム情報.VPSをカウントする();

            var dc = DXResources.Instance.既定のD2D1DeviceContext;
            dc.Transform = DXResources.Instance.拡大行列DPXtoPX;

            this._背景.進行描画する( dc );
            DXResources.Instance.D2DBatchDraw( dc, () => {
                dc.FillRectangle( new RectangleF( 0f, 36f, DXResources.Instance.設計画面サイズ.Width, DXResources.Instance.設計画面サイズ.Height - 72f ), this._黒マスクブラシ );
            } );
            this._プレビュー画像を描画する( dc );
            this._曲名パネル.描画する( dc, 660f, 796f );
            this._曲名を描画する( dc );
            this._サブタイトルを描画する( dc );
            this._演奏パラメータ結果.描画する( dc, 1317f, 716f, this._結果 );
            this._ランク.進行描画する( 200f, 300f, this._結果.ランク );
            this._難易度.進行描画する( dc, 1341f, 208f, App進行描画.曲ツリー.フォーカスノード.難易度 );
            this._曲別SKILL.進行描画する( dc, 1329f, 327f, this._結果.スキル );
            this._達成率.進行描画する( dc, 1233f, 428f, this._結果.Achievement );
            this._システム情報.描画する( dc );

            switch( this.現在のフェーズ )
            {
                case フェーズ.フェードアウト:
                    App進行描画.アイキャッチ管理.現在のアイキャッチ.進行描画する( dc );
                    if( App進行描画.アイキャッチ管理.現在のアイキャッチ.現在のフェーズ == アイキャッチ.フェーズ.クローズ完了 )
                        this.現在のフェーズ = フェーズ.完了;
                    break;
            }
        }


        private void _プレビュー画像を描画する( DeviceContext dc )
        {
            var 選択曲 = App進行描画.曲ツリー.フォーカス曲ノード;
            var preimage = 選択曲.ノード画像 ?? Node.既定のノード画像;

            // 枠

            DXResources.Instance.D2DBatchDraw( dc, () => {
                const float 枠の太さdpx = 5f;
                dc.FillRectangle(
                    new RectangleF(
                        this._プレビュー画像表示位置dpx.X - 枠の太さdpx,
                        this._プレビュー画像表示位置dpx.Y - 枠の太さdpx,
                        this._プレビュー画像表示サイズdpx.X + 枠の太さdpx * 2f,
                        this._プレビュー画像表示サイズdpx.Y + 枠の太さdpx * 2f ),
                    this._プレビュー枠ブラシ );
            } );

            // テクスチャは画面中央が (0,0,0) で、Xは右がプラス方向, Yは上がプラス方向, Zは奥がプラス方向+。

            var 変換行列 =
                Matrix.Scaling(
                    this._プレビュー画像表示サイズdpx.X / preimage.サイズ.Width,
                    this._プレビュー画像表示サイズdpx.Y / preimage.サイズ.Height,
                    0f ) *
                Matrix.Translation(
                    DXResources.Instance.画面左上dpx.X + this._プレビュー画像表示位置dpx.X + this._プレビュー画像表示サイズdpx.X / 2f,
                    DXResources.Instance.画面左上dpx.Y - this._プレビュー画像表示位置dpx.Y - this._プレビュー画像表示サイズdpx.Y / 2f,
                    0f );

            preimage.描画する( 変換行列 );
        }

        private void _曲名を描画する( DeviceContext dc )
        {
            var 表示位置dpx = new Vector2( 690f, 820f );

            // 拡大率を計算して描画する。
            float 最大幅dpx = 545f;

            this._曲名画像.描画する(
                dc,
                表示位置dpx.X,
                表示位置dpx.Y,
                X方向拡大率: ( this._曲名画像.画像サイズdpx.Width <= 最大幅dpx ) ? 1f : 最大幅dpx / this._曲名画像.画像サイズdpx.Width );
        }

        private void _サブタイトルを描画する( DeviceContext dc )
        {
            var 表示位置dpx = new Vector2( 690f, 820f + 60f );

            // 拡大率を計算して描画する。
            float 最大幅dpx = 545f;

            this._サブタイトル画像.描画する(
                dc,
                表示位置dpx.X,
                表示位置dpx.Y,
                X方向拡大率: ( this._サブタイトル画像.画像サイズdpx.Width <= 最大幅dpx ) ? 1f : 最大幅dpx / this._サブタイトル画像.画像サイズdpx.Width );
        }



        // private


        private システム情報 _システム情報 = null;

        private 成績 _結果 = null;

        private 舞台画像 _背景 = null;

        private 画像 _曲名パネル = null;

        private 文字列画像 _曲名画像 = null;

        private 文字列画像 _サブタイトル画像 = null;

        private 演奏パラメータ結果 _演奏パラメータ結果 = null;

        private ランク _ランク = null;

        private 難易度 _難易度 = null;

        private 曲別SKILL _曲別SKILL = null;

        private 達成率 _達成率 = null;

        private SolidColorBrush _黒マスクブラシ = null;

        private SolidColorBrush _プレビュー枠ブラシ = null;

        private readonly Vector3 _プレビュー画像表示位置dpx = new Vector3( 668f, 194f, 0f );

        private readonly Vector3 _プレビュー画像表示サイズdpx = new Vector3( 574f, 574f, 0f );
    }
}
