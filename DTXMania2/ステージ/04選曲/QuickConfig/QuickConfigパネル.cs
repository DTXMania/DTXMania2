using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Data.Sqlite;
using SharpDX;
using SharpDX.Direct2D1;
using FDK;
using DTXMania2.曲;
using System.Linq;

namespace DTXMania2.選曲.QuickConfig
{
    class QuickConfigパネル : IDisposable
    {

        // プロパティ


        public enum フェーズ
        {
            表示,
            完了_戻る,
            完了_オプション設定,
        }

        public フェーズ 現在のフェーズ { get; protected set; } = フェーズ.表示;



        // 生成と終了


        /// <summary>
        ///     コンストラクタ。
        /// </summary>
        /// <param name="song">現在選択中の曲。曲以外が選択されているなら null 。</param>
        /// <param name="userId">現在ログイン中のユーザ名。</param>
        public QuickConfigパネル( Song? song, string userId )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._パネル = new 画像( @"$(Images)\SelectStage\QuickConfigPanel.png" );

            // 設定項目リストを構築する。
            this._設定項目リスト = new SelectableList<ラベル>();

            #region "「この曲の評価」"
            //----------------
            var score = song?.譜面リスト?.FirstOrDefault( ( s ) => s != null );
            if( null != score )
            {
                this._設定項目リスト.Add(
                    new リスト(
                        名前: "この曲の評価",
                        選択肢初期値リスト: new[] { "評価なし", "★☆☆☆☆", "★★☆☆☆", "★★★☆☆", "★★★★☆", "★★★★★" },
                        初期選択肢番号: score.譜面の属性?.Rating ?? 0,
                        値が変更された: ( list ) => {

                            int 新Rating = list.現在の選択肢番号;

                            #region " 属性DBの該当レコードを、更新または新規作成する。"
                            //----------------
                            using var db = new ScorePropertiesDB();

                            for( int i = 0; i < song!.譜面リスト!.Length; i++ )
                            {
                                var score = song.譜面リスト[ i ]!;

                                if( null != score )
                                {
                                    using var cmd = new SqliteCommand( "SELECT * FROM ScoreProperties WHERE ScorePath = @ScorePath AND UserId = @UserId", db.Connection );
                                    cmd.Parameters.AddRange( new[] {
                                        new SqliteParameter( "@ScorePath", score.譜面.ScorePath ),
                                        new SqliteParameter( "@UserId", Global.App.ログオン中のユーザ.ID ),
                                    } );
                                    var result = cmd.ExecuteReader();
                                    if( result.Read() )
                                    {
                                        // (A) 属性DBにレコードがあった → Ratingを更新して書き戻し
                                        var prop = new ScorePropertiesDBRecord( result );
                                        prop.Rating = 新Rating;
                                        prop.InsertTo( db );
                                    }
                                    else
                                    {
                                        // (B) 属性DBにレコードがなかった → レコードを新規生成
                                        var rc = new ScorePropertiesDBRecord() {
                                            ScorePath = score.譜面.ScorePath,
                                            UserId = userId,
                                            Rating = 新Rating,
                                        };
                                        rc.InsertTo( db );
                                    }
                                }
                            }
                            //----------------
                            #endregion

                            #region " DBから読み込み済みの属性も、更新または新規作成する。"
                            //----------------
                            for( int i = 0; i < song!.譜面リスト!.Length; i++ )
                            {
                                var score = song.譜面リスト[ i ]!;

                                if( null != score )
                                {
                                    if( score.譜面の属性 is null )
                                    {
                                        // (A) 新規作成
                                        score.譜面の属性 = new ScorePropertiesDBRecord() {
                                            ScorePath = score.譜面.ScorePath,
                                            UserId = userId,
                                            Rating = list.現在の選択肢番号,
                                        };
                                    }
                                    else
                                    {
                                        // (B) 更新
                                        score.譜面の属性.Rating = list.現在の選択肢番号;
                                    }
                                }
                            }
                            //----------------
                            #endregion

                        } )
                    );
            }
            //----------------
            #endregion
            #region "「オプション設定へ」"
            //----------------
            this._設定項目リスト.Add( new ラベル( "オプション設定へ" ) );
            //----------------
            #endregion
            #region "「戻る」"
            //----------------
            this._設定項目リスト.Add( new ラベル( "戻る" ) );
            //----------------
            #endregion

            this._設定項目リスト.SelectItem( 0 );
        }

        public void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            foreach( var item in this._設定項目リスト )
                item.Dispose();

            this._パネル.Dispose();
        }



        // 進行と描画


        public void 進行描画する( float 左位置, float 上位置 )
        {
            var dc = Global.既定のD2D1DeviceContext;

            var 入力 = Global.App.ドラム入力;  // 呼び出し元でポーリング済み

            switch( this.現在のフェーズ )
            {
                case フェーズ.表示:
                {
                    #region " 入力処理。"
                    //----------------
                    if( 入力.キャンセルキーが入力された() )
                    {
                        #region " 完了_戻る フェーズへ。"
                        //----------------
                        Global.App.システムサウンド.再生する( システムサウンド種別.取消音 );
                        this.現在のフェーズ = フェーズ.完了_戻る;
                        //----------------
                        #endregion
                    }
                    else if( 入力.上移動キーが入力された() )
                    {
                        #region " １つ前の項目へカーソルを移動する。"
                        //----------------
                        Global.App.システムサウンド.再生する( システムサウンド種別.カーソル移動音 );
                        this._設定項目リスト.SelectPrev( Loop: true );
                        //----------------
                        #endregion
                    }
                    else if( 入力.下移動キーが入力された() )
                    {
                        #region " １つ後の項目へカーソルを移動する。"
                        //----------------
                        Global.App.システムサウンド.再生する( システムサウンド種別.カーソル移動音 );
                        this._設定項目リスト.SelectNext( Loop: true );
                        //----------------
                        #endregion
                    }
                    else if( 入力.左移動キーが入力された() )
                    {
                        #region " 項目の種類に応じて処理分岐。"
                        //----------------
                        var item = this._設定項目リスト.SelectedItem;

                        switch( item )
                        {
                            case リスト list:
                                Global.App.システムサウンド.再生する( システムサウンド種別.変更音 );
                                list.前を選択する( Loop: false );
                                break;

                            case ラベル label:
                                break;
                        }
                        //----------------
                        #endregion
                    }
                    else if( 入力.右移動キーが入力された() )
                    {
                        #region " 項目の種類に応じて処理分岐。"
                        //----------------
                        var item = this._設定項目リスト.SelectedItem;

                        switch( item )
                        {
                            case リスト list:
                                Global.App.システムサウンド.再生する( システムサウンド種別.変更音 );
                                list.次を選択する( Loop: false );
                                break;

                            case ラベル label:
                                break;
                        }
                        //----------------
                        #endregion
                    }
                    else if( 入力.確定キーが入力された() )
                    {
                        #region " 項目の種類や名前に応じて処理分岐。"
                        //----------------
                        var item = this._設定項目リスト.SelectedItem;

                        switch( item )
                        {
                            case リスト list:
                            {
                                Global.App.システムサウンド.再生する( システムサウンド種別.変更音 );
                                list.次を選択する( Loop: true );
                                break;
                            }
                            case ラベル label:
                            {
                                switch( label.名前 )
                                {
                                    case "オプション設定へ":
                                    {
                                        #region " 完了_オプション設定フェーズへ。"
                                        //----------------
                                        Global.App.システムサウンド.再生する( システムサウンド種別.決定音 );
                                        this.現在のフェーズ = フェーズ.完了_オプション設定;
                                        break;
                                        //----------------
                                        #endregion
                                    }
                                    case "戻る":
                                    {
                                        #region " 完了_戻る フェーズへ。"
                                        //----------------
                                        Global.App.システムサウンド.再生する( システムサウンド種別.取消音 );
                                        this.現在のフェーズ = フェーズ.完了_戻る;
                                        break;
                                        //----------------
                                        #endregion
                                    }
                                }
                                break;
                            }
                        }
                        //----------------
                        #endregion
                    }
                    //----------------
                    #endregion

                    #region " QuickConfig パネルを描画する。"
                    //----------------
                    this._パネル.進行描画する( 左位置, 上位置 );

                    for( int i = 0; i < this._設定項目リスト.Count; i++ )
                    {
                        const float 左右マージン = 24.0f;
                        const float 見出し行間 = 100.0f;
                        const float 項目行間 = 60.0f;
                        const float 項目ラベル上マージン = 6.0f;

                        // 選択カーソルを描画する。
                        if( this._設定項目リスト.SelectedIndex == i )
                        {
                            D2DBatch.Draw( dc, () => {
                                dc.Transform = Global.拡大行列DPXtoPX;
                                using var brush = new SolidColorBrush( dc, new SharpDX.Color( 0.6f, 0.6f, 1f, 0.4f ) );
                                dc.FillRectangle(
                                    new SharpDX.RectangleF(
                                        左位置 + 左右マージン,
                                        上位置 + 見出し行間 + 項目行間 * i,
                                        this._パネル.サイズ.Width - 左右マージン * 2,
                                        項目行間 ),
                                    brush );
                            } );
                        }

                        // 選択肢項目を描画する。
                        this._設定項目リスト[ i ].進行描画する( dc, 左位置 + 左右マージン * 2, 上位置 + 見出し行間 + i * 項目行間 + 項目ラベル上マージン );
                    }
                    //----------------
                    #endregion

                    break;
                }
                case フェーズ.完了_戻る:
                case フェーズ.完了_オプション設定:
                {
                    #region " 遷移終了。呼び出し元による継続処理を待つ。"
                    //----------------
                    //----------------
                    #endregion

                    break;
                }
            }
        }



        // ローカル


        private readonly 画像 _パネル;

        private readonly SelectableList<ラベル> _設定項目リスト;
    }
}
