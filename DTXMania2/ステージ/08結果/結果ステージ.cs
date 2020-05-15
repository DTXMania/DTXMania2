using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using Microsoft.Data.Sqlite;
using FDK;
using SSTFormat.v004;

namespace DTXMania2.結果
{
    class 結果ステージ : IStage
    {

        // プロパティ


        public enum フェーズ
        {
            表示,
            アニメ完了,
            フェードアウト,
            完了,
        }

        public フェーズ 現在のフェーズ { get; protected set; } = フェーズ.完了;



        // 生成と終了


        public 結果ステージ( 成績 result )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._結果 = result;

            Global.App.システムサウンド.再生する( システムサウンド種別.ステージクリア );

            #region " 成績をDBへ反映する。"
            //----------------
            if( !Global.App.ログオン中のユーザ.AutoPlayがすべてONである )
            {
                this._最高成績である = this._成績を反映する();
            }
            else
            {
                // 全AUTOなら反映しない。
                this._最高成績である = false;
            }
            //----------------
            #endregion

            this._背景 = new 舞台画像();
            this._既定のノード画像 = new 画像( @"$(Images)\DefaultPreviewImage.png" );
            this._現行化前のノード画像 = new 画像( @"$(Images)\PreviewImageWaitForActivation.png" );
            this._曲名パネル = new 画像( @"$(Images)\ResultStage\ScoreTitlePanel.png" );
            this._曲名画像 = new 文字列画像D2D() {
                フォント名 = "HGMaruGothicMPRO",
                フォントサイズpt = 40f,
                フォントの太さ = FontWeight.Regular,
                フォントスタイル = FontStyle.Normal,
                描画効果 = 文字列画像D2D.効果.縁取り,
                縁のサイズdpx = 6f,
                前景色 = Color4.Black,
                背景色 = Color4.White,
            };
            this._サブタイトル画像 = new 文字列画像D2D() {
                フォント名 = "HGMaruGothicMPRO",
                フォントサイズpt = 25f,
                フォントの太さ = FontWeight.Regular,
                フォントスタイル = FontStyle.Normal,
                描画効果 = 文字列画像D2D.効果.縁取り,
                縁のサイズdpx = 5f,
                前景色 = Color4.Black,
                背景色 = Color4.White,
            };
            this._演奏パラメータ結果 = new 演奏パラメータ結果();
            this._ランク = new ランク();
            this._難易度 = new 難易度();
            this._曲別SKILL = new 曲別SKILL();
            this._達成率 = ( this._最高成績である ) ? (達成率Base) new 達成率更新() : new 達成率();

            this._システム情報 = new システム情報();
            this._黒マスクブラシ = new SolidColorBrush( Global.既定のD2D1DeviceContext, new Color4( Color3.Black, 0.75f ) );
            this._プレビュー枠ブラシ = new SolidColorBrush( Global.既定のD2D1DeviceContext, new Color4( 0xFF209292 ) );


            var 選択曲 = Global.App.演奏スコア;
            this._曲名画像.表示文字列 = 選択曲.曲名;
            this._サブタイトル画像.表示文字列 = 選択曲.アーティスト名;

            this._背景.ぼかしと縮小を適用する( 0.0 );    // 即時適用
            this.現在のフェーズ = フェーズ.表示;
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            Global.App.システムサウンド.停止する( システムサウンド種別.ステージクリア );

            Global.App.WAV管理.すべての発声を停止する();
            Global.App.WAV管理.Dispose();

            this._プレビュー枠ブラシ.Dispose();
            this._黒マスクブラシ.Dispose();
            this._システム情報.Dispose();
            this._達成率.Dispose();
            this._曲別SKILL.Dispose();
            this._難易度.Dispose();
            this._ランク.Dispose();
            this._演奏パラメータ結果.Dispose();
            this._サブタイトル画像.Dispose();
            this._曲名画像.Dispose();
            this._曲名パネル.Dispose();
            this._現行化前のノード画像.Dispose();
            this._既定のノード画像.Dispose();
            this._背景.Dispose();
        }



        // 進行と描画


        public void 進行する()
        {
            this._システム情報.FPSをカウントしプロパティを更新する();


            // 入力

            Global.App.ドラム入力.すべての入力デバイスをポーリングする();

            switch( this.現在のフェーズ )
            {
                case フェーズ.表示:

                    if( Global.App.ドラム入力.確定キーが入力された() ||
                        Global.App.ドラム入力.キャンセルキーが入力された() )
                    {
                        #region " 確定キー　→　アニメ完了へ "
                        //----------------
                        this._曲別SKILL.アニメを完了する();
                        this._達成率.アニメを完了する();
                        this._難易度.アニメを完了する();

                        this.現在のフェーズ = フェーズ.アニメ完了;
                        //----------------
                        #endregion
                    }
                    else
                    {
                        if( Global.App.ログオン中のユーザ.ドラムの音を発声する )
                        {
                            #region " その他　→　空うちサウンドを再生 "
                            //----------------
                            this._空うちサウンドを再生する();
                            //----------------
                            #endregion
                        }

                        #region " すべてのアニメ化完了 → アニメ完了へ "
                        //----------------
                        if( this._曲別SKILL.アニメ完了 && this._難易度.アニメ完了 && this._達成率.アニメ完了 )
                        {
                            this.現在のフェーズ = フェーズ.アニメ完了;
                        }
                        //----------------
                        #endregion
                    }
                    break;

                case フェーズ.アニメ完了:

                    if( Global.App.ドラム入力.確定キーが入力された() ||
                        Global.App.ドラム入力.キャンセルキーが入力された() )
                    {
                        #region " 確定キー　→　フェーズアウトへ "
                        //----------------
                        Global.App.システムサウンド.再生する( システムサウンド種別.取消音 );    // 確定だけど取消音
                        Global.App.アイキャッチ管理.アイキャッチを選択しクローズする( nameof( シャッター ) );
                        this.現在のフェーズ = フェーズ.フェードアウト;
                        //----------------
                        #endregion
                    }
                    else if( Global.App.ログオン中のユーザ.ドラムの音を発声する )
                    {
                        #region " その他　→　空うちサウンドを再生 "
                        //----------------
                        this._空うちサウンドを再生する();
                        //----------------
                        #endregion
                    }
                    break;
            }
        }

        public void 描画する()
        {
            var dc = Global.既定のD2D1DeviceContext;
            dc.Transform = Global.拡大行列DPXtoPX;

            this._システム情報.VPSをカウントする();

            this._背景.進行描画する( dc );
            D2DBatch.Draw( dc, () => {
                dc.FillRectangle( new RectangleF( 0f, 36f, Global.設計画面サイズ.Width, Global.設計画面サイズ.Height - 72f ), this._黒マスクブラシ );
            } );
            this._プレビュー画像を描画する( dc );
            this._曲名パネル.描画する( 660f, 796f );
            this._曲名を描画する( dc );
            this._サブタイトルを描画する( dc );
            this._演奏パラメータ結果.描画する( dc, 1317f, 716f, this._結果 );
            this._ランク.進行描画する( 200f, 300f, this._結果.ランク );
            this._難易度.進行描画する( dc, 1341f, 208f, Global.App.演奏スコア.難易度 );
            this._曲別SKILL.進行描画する( dc, 1329f, 327f, this._結果.スキル );
            this._達成率.進行描画する( dc, 1233f, 428f, this._結果.Achievement );
            this._システム情報.描画する( dc );

            switch( this.現在のフェーズ )
            {
                case フェーズ.フェードアウト:
                    Global.App.アイキャッチ管理.現在のアイキャッチ.進行描画する( dc );
                    if( Global.App.アイキャッチ管理.現在のアイキャッチ.現在のフェーズ == アイキャッチ.フェーズ.クローズ完了 )
                        this.現在のフェーズ = フェーズ.完了;
                    break;
            }
        }


        private void _プレビュー画像を描画する( DeviceContext dc )
        {
            // 枠
            D2DBatch.Draw( dc, () => {
                const float 枠の太さdpx = 5f;
                dc.FillRectangle(
                    new RectangleF(
                        this._プレビュー画像表示位置dpx.X - 枠の太さdpx,
                        this._プレビュー画像表示位置dpx.Y - 枠の太さdpx,
                        this._プレビュー画像表示サイズdpx.X + 枠の太さdpx * 2f,
                        this._プレビュー画像表示サイズdpx.Y + 枠の太さdpx * 2f ),
                    this._プレビュー枠ブラシ );
            } );

            // プレビュー画像
            var preimage = Global.App.演奏譜面.最高記録を現行化済み ? ( Global.App.演奏譜面.プレビュー画像 ?? this._既定のノード画像 ) : this._現行化前のノード画像;
            
            var 変換行列 =
                Matrix.Scaling(
                    this._プレビュー画像表示サイズdpx.X / preimage.サイズ.Width,
                    this._プレビュー画像表示サイズdpx.Y / preimage.サイズ.Height,
                    0f ) *
                Matrix.Translation( // テクスチャは画面中央が (0,0,0) で、Xは右がプラス方向, Yは上がプラス方向, Zは奥がプラス方向+。
                    Global.画面左上dpx.X + this._プレビュー画像表示位置dpx.X + this._プレビュー画像表示サイズdpx.X / 2f,
                    Global.画面左上dpx.Y - this._プレビュー画像表示位置dpx.Y - this._プレビュー画像表示サイズdpx.Y / 2f,
                    0f );
            
            preimage.描画する( 変換行列 );
        }

        private void _曲名を描画する( DeviceContext dc )
        {
            var 表示位置dpx = new Vector2( 690f, 820f );

            // 拡大率を計算して描画する。
            float 最大幅dpx = 545f;
            float X方向拡大率 = ( this._曲名画像.画像サイズdpx.Width <= 最大幅dpx ) ? 1f : 最大幅dpx / this._曲名画像.画像サイズdpx.Width;
            this._曲名画像.描画する( dc, 表示位置dpx.X, 表示位置dpx.Y, X方向拡大率: X方向拡大率 );
        }

        private void _サブタイトルを描画する( DeviceContext dc )
        {
            var 表示位置dpx = new Vector2( 690f, 820f + 60f );

            // 拡大率を計算して描画する。
            float 最大幅dpx = 545f;
            float X方向拡大率 = ( this._サブタイトル画像.画像サイズdpx.Width <= 最大幅dpx ) ? 1f : 最大幅dpx / this._サブタイトル画像.画像サイズdpx.Width;
            this._サブタイトル画像.描画する( dc, 表示位置dpx.X, 表示位置dpx.Y, X方向拡大率: X方向拡大率 );
        }



        // ローカル


        private readonly bool _最高成績である;

        private readonly 舞台画像 _背景;

        private readonly 画像 _既定のノード画像;

        private readonly 画像 _現行化前のノード画像;

        private readonly 画像 _曲名パネル;

        private readonly 文字列画像D2D _曲名画像;

        private readonly 文字列画像D2D _サブタイトル画像;

        private readonly 演奏パラメータ結果 _演奏パラメータ結果;

        private readonly ランク _ランク;

        private readonly 難易度 _難易度;

        private readonly 曲別SKILL _曲別SKILL;

        private readonly 達成率Base _達成率;

        private readonly システム情報 _システム情報;

        private readonly SolidColorBrush _黒マスクブラシ;

        private readonly SolidColorBrush _プレビュー枠ブラシ;

        private 成績 _結果 = null!;

        private readonly Vector3 _プレビュー画像表示位置dpx = new Vector3( 668f, 194f, 0f );

        private readonly Vector3 _プレビュー画像表示サイズdpx = new Vector3( 574f, 574f, 0f );

        /// <summary>
        ///     成績を、RecordDB と演奏譜面へ追加または反映する。
        /// </summary>
        /// <return>最高達成率を更新していればtrueを返す。</return>
        private bool _成績を反映する()
        {
            bool 最高成績である = false;

            using var recorddb = new RecordDB();
            using var query = new SqliteCommand( "SELECT * FROM Records WHERE ScorePath = @ScorePath AND UserId = @UserId", recorddb.Connection );
            query.Parameters.AddRange( new[] {
                new SqliteParameter( "@ScorePath", Global.App.演奏譜面.譜面.ScorePath ),
                new SqliteParameter( "@UserId", Global.App.ログオン中のユーザ.ID ),
            } );
            var result = query.ExecuteReader();
            if( result.Read() )
            {
                #region " (A) レコードがすでに存在する → 記録を更新していれば、更新する。"
                //----------------
                var record = new RecordDBRecord( result );

                if( record.Achievement < this._結果.Achievement )
                {
                    record.Score = this._結果.Score;
                    record.Achievement = this._結果.Achievement;
                    record.CountMap = this._結果.CountMap;

                    最高成績である = true;
                    Global.App.演奏譜面.最高記録 = record;
                    Global.App.演奏譜面.最高記録を現行化済み = true;
                }
                else
                {
                    最高成績である = ( Global.App.演奏譜面.最高記録 is null ); // 初回なら最高記録。
                    Global.App.演奏譜面.最高記録 ??= record;    // 初回なら代入。
                    Global.App.演奏譜面.最高記録を現行化済み = true;
                }
                //----------------
                #endregion
            }
            else
            {
                #region " (B) レコードが存在しない → 新規追加する。"
                //----------------
                var record = new RecordDBRecord() {
                    ScorePath = Global.App.演奏譜面.譜面.ScorePath,
                    UserId = Global.App.ログオン中のユーザ.ID!,
                    Score = this._結果.Score,
                    Achievement = this._結果.Achievement,
                    CountMap = this._結果.CountMap,
                };
                record.InsertTo( recorddb );

                最高成績である = true;
                Global.App.演奏譜面.最高記録 ??= record;
                Global.App.演奏譜面.最高記録を現行化済み = true;
                //----------------
                #endregion
            }

            return 最高成績である;
        }

        private チップ? _一番最後のチップを返す( ドラム入力種別 drumType )
        {
            var チップtoプロパティ = Global.App.ログオン中のユーザ.ドラムチッププロパティリスト.チップtoプロパティ;

            // チップリストの後方から先頭に向かって検索。
            for( int i = Global.App.演奏スコア.チップリスト.Count - 1; i >= 0; i-- )
            {
                var chip = Global.App.演奏スコア.チップリスト[ i ];

                if( チップtoプロパティ[ chip.チップ種別 ].ドラム入力種別 == drumType )
                    return chip;    // 見つけた
            }

            return null;    // 見つからなかった
        }

        private void _チップの発声を行う( チップ chip, bool ドラムサウンドを再生する )
        {
            if( chip.チップ種別 == チップ種別.背景動画 )
            {
                if( Global.App.ログオン中のユーザ.演奏中に動画を表示する )
                {
                    #region " (A) AVI動画を再生する。"
                    //----------------
                    int AVI番号 = chip.チップサブID;

                    if( Global.App.AVI管理.動画リスト.TryGetValue( AVI番号, out Video? video ) )
                    {
                        Global.App.サウンドタイマ.一時停止する();       // 止めても止めなくてもカクつくだろうが、止めておけば譜面は再開時にワープしない。
                        video.再生を開始する();
                        Global.App.サウンドタイマ.再開する();
                    }
                    //----------------
                    #endregion
                }
                return;
            }

            if( 0 == chip.チップサブID && ドラムサウンドを再生する ) // サブIDが 0 なら SSTF である
            {
                #region " (B) SSTF準拠のドラムサウンドを再生する。"
                //----------------
                var ドラムチッププロパティ = Global.App.ログオン中のユーザ.ドラムチッププロパティリスト.チップtoプロパティ[ chip.チップ種別 ];

                // ドラムサウンドを持つチップなら発声する。（持つかどうかはこのメソッド↓内で判定される。）
                Global.App.ドラムサウンド.再生する( chip.チップ種別, 0, ドラムチッププロパティ.発声前消音, ドラムチッププロパティ.消音グループ種別, ( chip.音量 / (float) チップ.最大音量 ) );
                //----------------
                #endregion
            }
            else
            {
                #region " (C) WAVサウンドを再生する。"
                //----------------
                int WAV番号 = chip.チップサブID;
                var prop = Global.App.ログオン中のユーザ.ドラムチッププロパティリスト.チップtoプロパティ[ chip.チップ種別 ];

                // WAVを持つチップなら発声する。（持つかどうかはこのメソッド↓内で判定される。）
                Global.App.WAV管理.発声する( chip.チップサブID, chip.チップ種別, prop.発声前消音, prop.消音グループ種別, BGM以外も再生する: ドラムサウンドを再生する, 音量: ( chip.音量 / (float) チップ.最大音量 ) );
                //----------------
                #endregion
            }
        }

        private void _空うちサウンドを再生する()
        {
            // すべての押下入力について……
            foreach( var 入力 in Global.App.ドラム入力.ポーリング結果.Where( ( e ) => e.InputEvent.押された ) )
            {
                var 押下入力に対応するすべてのドラムチッププロパティのリスト
                    = Global.App.ログオン中のユーザ.ドラムチッププロパティリスト.チップtoプロパティ.Where( ( kvp ) => ( kvp.Value.ドラム入力種別 == 入力.Type ) );

                foreach( var kvp in 押下入力に対応するすべてのドラムチッププロパティのリスト )
                {
                    var ドラムチッププロパティ = kvp.Value;

                    if( 0 < Global.App.演奏スコア.空打ちチップマップ.Count )
                    {
                        #region " (A) 空うちチップマップが存在する場合（DTX他の場合）"
                        //----------------
                        int zz = Global.App.演奏スコア.空打ちチップマップ[ ドラムチッププロパティ.レーン種別 ];  // WAVのzz番号。登録されていなければ 0

                        if( 0 != zz )
                        {
                            // (A-a) 空打ちチップの指定があるなら、それを発声する。
                            Global.App.WAV管理.発声する( zz, ドラムチッププロパティ.チップ種別, ドラムチッププロパティ.発声前消音, ドラムチッププロパティ.消音グループ種別, BGM以外も再生する: true );
                        }
                        else
                        {
                            // (A-b) 空打ちチップの指定がないなら、入力に対応する一番最後のチップを検索し、それを発声する。
                            var chip = this._一番最後のチップを返す( 入力.Type );
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
                        #region " (B) 空うちチップマップ未使用の場合（SSTFの場合）"
                        //----------------
                        Global.App.ドラムサウンド.再生する( ドラムチッププロパティ.チップ種別, 0, ドラムチッププロパティ.発声前消音, ドラムチッププロパティ.消音グループ種別 );
                        //----------------
                        #endregion
                    }
                }
            }
        }
    }
}
