using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SSTFEditor
{
    class 選択モード : IDisposable
    {
        public 選択モード( メインフォーム form )
        {
            this.Form = form;
        }

        public void Dispose()
        {
            this.選択領域用のブラシ?.Dispose();
            this.選択領域用のブラシ = null;

            this.選択領域用のペン?.Dispose();
            this.選択領域用のペン = null;

            this.Form = null;
        }

        public void 個別選択を解除する( 描画用チップ chip )
        {
            var cell = new UndoRedo.セル<描画用チップ>(
                所有者ID: null,
                Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                    変更対象.選択が確定している = true;
                    this.Form.未保存である = true;
                },
                Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                    変更対象.選択が確定している = false;
                    this.Form.未保存である = true;
                },
                変更対象: chip,
                変更前の値: null,
                変更後の値: null );

            this.Form.UndoRedo管理.セルを追加する( cell );
            cell.Redoを実行する();
            this.Form.UndoRedo用GUIのEnabledを設定する();
        }

        public void 全チップを選択する()
        {
            try
            {
                this.Form.UndoRedo管理.トランザクション記録を開始する();

                foreach( 描画用チップ chip in this.Form.譜面.SSTFormatScore.チップリスト )
                {
                    // 選択されていないすべてのチップを選択する。
                    if( chip.選択が確定していない )
                    {
                        var 変更前のチップ = new 描画用チップ( chip );

                        var 変更後のチップ = new 描画用チップ( chip ) {
                            ドラッグ操作により選択中である = false,
                            選択が確定している = true,
                        };

                        var cell = new UndoRedo.セル<描画用チップ>(
                            所有者ID: null,
                            Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                                変更対象.CopyFrom( 変更前 );
                                this.Form.未保存である = true;
                            },
                            Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                                変更対象.CopyFrom( 変更後 );
                                this.Form.未保存である = true;
                            },
                            変更対象: chip,
                            変更前の値: 変更前のチップ,
                            変更後の値: 変更後のチップ );

                        this.Form.UndoRedo管理.セルを追加する( cell );

                        cell.Redoを実行する();
                    }
                }
            }
            finally
            {
                this.Form.UndoRedo管理.トランザクション記録を終了する();

                this.Form.UndoRedo用GUIのEnabledを設定する();
                this.Form.選択チップの有無に応じて編集用GUIのEnabledを設定する();
                this.Form.譜面をリフレッシュする();
            }
        }

        public void 全チップの選択を解除する()
        {
            try
            {
                this.Form.UndoRedo管理.トランザクション記録を開始する();

                foreach( 描画用チップ chip in this.Form.譜面.SSTFormatScore.チップリスト )
                {
                    if( ( 0 != chip.枠外レーン数 ) || ( 0 > chip.譜面内絶対位置grid ) )
                    {
                        #region " 譜面範囲外に出たチップがあれば削除する。"
                        //-----------------
                        var chip変更前 = this.移動開始時のチップ状態[ chip ];

                        var cell = new UndoRedo.セル<描画用チップ>(
                            所有者ID: null,
                            Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                                変更対象.CopyFrom( chip変更前 );
                                this.Form.譜面.SSTFormatScore.チップリスト.Add( 変更対象 );
                                this.Form.譜面.SSTFormatScore.チップリスト.Sort();
                                this.Form.未保存である = true;
                            },
                            Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                                this.Form.譜面.SSTFormatScore.チップリスト.Remove( 変更対象 );
                                this.Form.未保存である = true;
                            },
                            変更対象: chip,
                            変更前の値: chip変更前,
                            変更後の値: null );

                        this.Form.UndoRedo管理.セルを追加する( cell );
                        cell.Redoを実行する();
                        //-----------------
                        #endregion
                    }
                    else if( chip.ドラッグ操作により選択中である || chip.選択が確定している )
                    {
                        #region " チップの選択を解除する。"
                        //-----------------
                        var chip変更前 = new 描画用チップ( chip );

                        var cell = new UndoRedo.セル<描画用チップ>(
                            所有者ID: null,
                            Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                                変更対象.CopyFrom( 変更前 );
                            },
                            Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => {
                                変更対象.選択が確定している = false;
                                変更対象.ドラッグ操作により選択中である = false;
                                変更対象.移動済みである = false;
                            },
                            変更対象: chip,
                            変更前の値: chip変更前,
                            変更後の値: null );

                        this.Form.UndoRedo管理.セルを追加する( cell );
                        cell.Redoを実行する();
                        //-----------------
                        #endregion
                    }
                }
            }
            finally
            {
                this.Form.UndoRedo管理.トランザクション記録を終了する();

                this.Form.UndoRedo用GUIのEnabledを設定する();
                this.Form.未保存である = true;
            }
        }

        public void 検索する()
        {
            using var dialog = new 検索条件入力ダイアログ();

            if( dialog.ShowDialog( this.Form ) != DialogResult.OK )
                return; // キャンセルされたら何もしない。

            int 開始小節番号 = ( dialog.小節範囲指定CheckBoxがチェックされている ) ? dialog.小節範囲開始番号 : 0;
            int 終了小節番号 = ( dialog.小節範囲指定CheckBoxがチェックされている ) ? dialog.小節範囲終了番号 : this.Form.譜面.SSTFormatScore.最大小節番号を返す();

            if( 0 > 開始小節番号 )
                開始小節番号 = 0; // 省略時は 0 とみなす。

            if( 0 > 終了小節番号 )
                終了小節番号 = this.Form.譜面.SSTFormatScore.最大小節番号を返す();        // 省略時は 最大小節番号とする。

            int 選択チップ数 = 0;
            try
            {
                this.Form.UndoRedo管理.トランザクション記録を開始する();

                foreach( 描画用チップ chip in this.Form.譜面.SSTFormatScore.チップリスト )
                {
                    var e編集レーン = this.Form.譜面.dicチップ編集レーン対応表[ chip.チップ種別 ];

                    // 編集レーンを持たないチップは無視する。
                    if( e編集レーン == 編集レーン種別.Unknown )
                        continue;

                    if( ( chip.小節番号 >= 開始小節番号 ) && ( chip.小節番号 <= 終了小節番号 ) )
                    {
                        if( dialog.選択されている( e編集レーン ) || dialog.選択されている( chip.チップ種別 ) )
                        {
                            // チップを選択する。
                            var chip変更前 = new 描画用チップ( chip );
                            var cell = new UndoRedo.セル<描画用チップ>(
                                所有者ID: null,
                                Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => { 変更対象.選択が確定している = 変更前.選択が確定している; },
                                Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => { 変更対象.選択が確定している = true; },
                                変更対象: chip,
                                変更前の値: chip変更前,
                                変更後の値: null );

                            this.Form.UndoRedo管理.セルを追加する( cell );
                            cell.Redoを実行する();

                            選択チップ数++;
                        }
                    }
                }
            }
            finally
            {
                this.Form.UndoRedo管理.トランザクション記録を終了する();

                this.Form.UndoRedo用GUIのEnabledを設定する();
                this.Form.譜面をリフレッシュする();
            }

            #region " チップ数に応じて結果を表示する。"
            //-----------------
            if( 0 < 選択チップ数 )
            {
                this.Form.選択チップの有無に応じて編集用GUIのEnabledを設定する();

                MessageBox.Show(
                    選択チップ数 + Properties.Resources.MSG_個のチップが選択されました,
                    Properties.Resources.MSG_検索結果ダイアログのタイトル,
                    MessageBoxButtons.OK, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1 );
            }
            else
            {
                MessageBox.Show(
                    Properties.Resources.MSG_該当するチップはありませんでした,
                    Properties.Resources.MSG_検索結果ダイアログのタイトル,
                    MessageBoxButtons.OK, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1 );
            }
            //-----------------
            #endregion
        }


        // イベント

        public void MouseClick( MouseEventArgs e )
        {
            // 右クリック → コンテクストメニュー表示
            if( e.Button == MouseButtons.Right )
                this.Form.選択モードのコンテクストメニューを表示する( e.X, e.Y );
        }

        public void MouseDown( MouseEventArgs e )
        {
            // 左クリック
            if( e.Button == MouseButtons.Left )
            {
                var chip = this.Form.譜面.譜面パネル内座標pxに存在するチップがあれば返す( e.X, e.Y );

                // (A) チップがないか、未選択のチップがあった場合。
                if( ( chip == null ) || ( false == chip.選択が確定している ) )
                    this.範囲選択の開始処理( e );

                // (B) 選択状態のチップがあり、かつ、クリック時に CTRL が押されている場合。
                else if( ( Control.ModifierKeys & Keys.Control ) == Keys.Control )
                    this.個別選択を解除する( chip );

                // (C) 選択状態のチップがあり、かつ、クリック時に CTRL が押されていない場合。
                else
                    this.移動の開始処理( e );
            }
        }

        public void MouseMove( MouseEventArgs e )
        {
            // (A) 左ボタンが押されながら移動している場合 → 継続処理
            if( e.Button == MouseButtons.Left )
            {
                if( this.範囲選択のためにドラッグ中である )
                    this.範囲選択の継続処理( e );

                else if( this.移動のためにドラッグ中である )
                    this.移動の継続処理( e );
            }

            // (B) 左ボタンが押されずに移動した場合 → 終了処理
            else
            {
                if( this.範囲選択のためにドラッグ中である )
                    this.範囲選択の終了処理( e );

                else if( this.移動のためにドラッグ中である )
                    this.移動の終了処理( e );
            }

            // 譜面を再描画する。
            this.Form.譜面をリフレッシュする();
        }

        public void Paint( PaintEventArgs e )
        {
            if( this.範囲選択のためにドラッグ中である )
                this.現在の選択範囲を描画する( e.Graphics );
        }


        // 全般

        protected メインフォーム Form = null;

        protected SolidBrush 選択領域用のブラシ = new SolidBrush( Color.FromArgb( 80, 55, 55, 255 ) );

        protected Pen 選択領域用のペン = new Pen( Color.LightBlue );

        protected void 現在の選択範囲を描画する( Graphics g )
        {
            var 現在の選択領域px = new Rectangle() {

                X = Math.Min(
                    this.現在の範囲選択用ドラッグ開始位置Xpx,
                    this.現在の範囲選択用ドラッグ終了位置Xpx ),

                Y = this.Form.譜面.譜面内絶対位置gridにおける対象領域内のY座標pxを返す(
                    Math.Max(   // grid は上に行くほど大きくなる
                        this.現在の範囲選択用ドラッグ開始位置Ypxの譜面内絶対位置grid,
                        this.現在の範囲選択用ドラッグ終了位置Ypxの譜面内絶対位置grid ),
                    this.Form.譜面パネルサイズ ),

                Width = Math.Abs( this.現在の範囲選択用ドラッグ開始位置Xpx - this.現在の範囲選択用ドラッグ終了位置Xpx ),

                Height = Math.Abs( this.現在の範囲選択用ドラッグ開始位置Ypxの譜面内絶対位置grid - this.現在の範囲選択用ドラッグ終了位置Ypxの譜面内絶対位置grid ) / this.Form.GRID_PER_PIXEL,
            };

            // クリッピング

            現在の選択領域px.Intersect( this.Form.譜面パネル領域 );

            // 描画

            if( ( 0 < 現在の選択領域px.Width ) && ( 0 < 現在の選択領域px.Height ) )
            {
                g.FillRectangle( this.選択領域用のブラシ, 現在の選択領域px );
                g.DrawRectangle( Pens.LightBlue, 現在の選択領域px );
            }
        }

        protected void 譜面パネルの上下端にきたならスクロールする( MouseEventArgs e )
        {
            const int 上端スクロール発動幅px = 70;
            const int 下端スクロール発動幅px = 50;

            if( e.Y <= 上端スクロール発動幅px )
            {
                // (A) マウスが上端にいるので、上から下へスクロールする。

                double 発動量 = Math.Min( Math.Max( 上端スクロール発動幅px - e.Y, 0 ) / (double) 上端スクロール発動幅px, 1.0 ); // 0～1  (Liner)
                double 速度係数 = ( 1.0 - Math.Cos( ( Math.PI / 2.0 ) * 発動量 ) ) * 2.0 + 1.0;    // 発動量:0→1 のとき、速度係数:3→1 (Cos)
                int スクロール量grid = (int) ( -速度係数 * 180.0 );  // 速度係数:1→3 のとき、スクロール量grid:-180→-540;

                this.Form.譜面を縦スクロールする( スクロール量grid );

                if( this.移動のためにドラッグ中である )
                    this.現在の移動用ドラッグ開始位置px.Y -= スクロール量grid / this.Form.GRID_PER_PIXEL;

                //if( this.範囲選択のためにドラッグ中である )
                //    this.現在の範囲選択用ドラッグ開始位置Ypxの譜面内絶対位置grid += スクロール量grid; // grid は上方向がプラス
                // → 譜面スクロール後のマウスイベントですぐに更新されるので、ここで位置を増減してしまうと２倍動いてしまう。なのでコメントアウト。
            }
            else if( e.Y >= ( this.Form.譜面パネルサイズ.Height - 下端スクロール発動幅px ) &&
                ( this.Form.譜面.譜面表示下辺の譜面内絶対位置grid > 0 ) )   // まだスクロールができる場合のみ
            {
                // (B) マウスが下端にいるので、下から上へスクロールする。

                double 発動量 = Math.Min( Math.Max( e.Y - ( this.Form.譜面パネルサイズ.Height - 下端スクロール発動幅px ), 0 ) / (double) 下端スクロール発動幅px, 1.0 );  // 0～1 (Liner)
                double 速度係数 = ( 1.0 - Math.Cos( ( Math.PI / 2.0 ) * 発動量 ) ) * 2.0 + 1.0;    // 発動量:0→1 のとき、速度係数:3→1 (Cos)
                int スクロール量grid = (int) ( 速度係数 * 180.0 );    // 速度係数:1→3 のとき、スクロール量grid:180→540;

                this.Form.譜面を縦スクロールする( スクロール量grid );

                if( this.移動のためにドラッグ中である )
                    this.現在の移動用ドラッグ開始位置px.Y -= スクロール量grid / this.Form.GRID_PER_PIXEL;

                //if( this.範囲選択のためにドラッグ中である )
                //    this.現在の範囲選択用ドラッグ開始位置Ypxの譜面内絶対位置grid += スクロール量grid;   // grid は上がプラス
                // → 譜面がスクロールしても開始位置は絶対grid指定なので、変更する必要がない。よって、コメントアウト。
            }
        }


        // 移動関連

        protected bool 移動のためにドラッグ中である = false;

        protected Point 現在の移動用ドラッグ開始位置px = new Point( 0, 0 );

        protected Point 現在の移動用ドラッグ終了位置px = new Point( 0, 0 );

        protected Dictionary<描画用チップ, 描画用チップ> 移動開始時のチップ状態 = new Dictionary<描画用チップ, 描画用チップ>();

        protected struct レーングリッド座標
        {
            public int 編集レーン番号;         // X座標に相当。
            public int 譜面内絶対位置grid;     // Y座標に相当。
        };
        protected レーングリッド座標 前回のマウス位置LaneGrid = new レーングリッド座標();

        protected void 移動の開始処理( MouseEventArgs e )
        {
            this.移動のためにドラッグ中である = true;

            // ドラッグ範囲の初期化。
            this.現在の移動用ドラッグ開始位置px.X = this.現在の移動用ドラッグ終了位置px.X = e.X;
            this.現在の移動用ドラッグ開始位置px.Y = this.現在の移動用ドラッグ終了位置px.Y = e.Y;

            // マウス位置（lane×grid）の初期化。
            this.前回のマウス位置LaneGrid = new レーングリッド座標() {
                編集レーン番号 = this.Form.譜面.dicレーン番号[ this.Form.譜面.譜面パネル内X座標pxにある編集レーンを返す( e.X ) ],
                譜面内絶対位置grid = this.Form.譜面.譜面パネル内Y座標pxにおける譜面内絶対位置gridをガイド幅単位で返す( e.Y ),
            };

            // 移動対象チップ（現在選択が確定しているチップ）の「移動開始時のチップの全状態」を、ローカルの Dictionary に控えておく。
            // これは 移動終了処理() で使用する。
            this.移動開始時のチップ状態.Clear();
            foreach( 描画用チップ chip in this.Form.譜面.SSTFormatScore.チップリスト )
            {
                if( chip.選択が確定している )
                    this.移動開始時のチップ状態.Add( chip, new 描画用チップ( chip ) );
            }
        }

        protected void 移動の継続処理( MouseEventArgs e )
        {
            // ドラッグ終了位置を現在のマウスの位置に更新。
            this.現在の移動用ドラッグ終了位置px.X = e.X;
            this.現在の移動用ドラッグ終了位置px.Y = e.Y;

            // スクロールチェック。
            this.譜面パネルの上下端にきたならスクロールする( e );

            // チップの移動。
            #region " 現在確定中のチップを移動する。"
            //-----------------

            // 現在の位置を算出。
            var 現在のドラッグ終了位置LaneGrid = new レーングリッド座標() {
                編集レーン番号 = this.Form.譜面.dicレーン番号[ this.Form.譜面.譜面パネル内X座標pxにある編集レーンを返す( this.現在の移動用ドラッグ終了位置px.X ) ],
                譜面内絶対位置grid = this.Form.譜面.譜面パネル内Y座標pxにおける譜面内絶対位置gridをガイド幅単位で返す( this.現在の移動用ドラッグ終了位置px.Y ),
            };

            // 前回位置からの移動量を算出。
            var 移動量LaneGrid = new レーングリッド座標() {
                編集レーン番号 = 現在のドラッグ終了位置LaneGrid.編集レーン番号 - this.前回のマウス位置LaneGrid.編集レーン番号,
                譜面内絶対位置grid = 現在のドラッグ終了位置LaneGrid.譜面内絶対位置grid - this.前回のマウス位置LaneGrid.譜面内絶対位置grid,
            };

            // 前回位置から移動していれば、選択されているすべてのチップを移動させる。
            if( ( 0 != 移動量LaneGrid.編集レーン番号 ) || ( 0 != 移動量LaneGrid.譜面内絶対位置grid ) )
            {
                #region " 全チップの移動済フラグをリセットする。"
                //-----------------
                foreach( 描画用チップ chip in this.Form.譜面.SSTFormatScore.チップリスト )
                    chip.移動済みである = false;
                //-----------------
                #endregion

                foreach( 描画用チップ chip in this.Form.譜面.SSTFormatScore.チップリスト )
                {
                    if( chip.選択が確定している && ( false == chip.移動済みである ) )
                    {
                        if( 0 != 移動量LaneGrid.編集レーン番号 )
                        {
                            #region " チップを横に移動する。"
                            //-----------------
                            int レーン数 = Enum.GetValues( typeof( 編集レーン種別 ) ).Length;
                            int チップの現在のレーン番号 = this.Form.譜面.dicレーン番号[ this.Form.譜面.dicチップ編集レーン対応表[ chip.チップ種別 ] ];
                            int チップの移動後のレーン番号;

                            #region " チップの移動後のレーン番号 を算出。"
                            //-----------------
                            if( 0 > chip.枠外レーン数 )
                            {
                                チップの移動後のレーン番号 = チップの現在のレーン番号 + 移動量LaneGrid.編集レーン番号;
                            }
                            else if( 0 < chip.枠外レーン数 )
                            {
                                チップの移動後のレーン番号 = ( ( レーン数 - 1 ) + chip.枠外レーン数 ) + 移動量LaneGrid.編集レーン番号;
                            }
                            else
                            {
                                チップの移動後のレーン番号 = チップの現在のレーン番号;
                            }
                            //-----------------
                            #endregion

                            #region " チップの移動後のレーン番号 から、チップのチップ種別 or 枠外レーン数 を修正する。"
                            //-----------------
                            if( 0 > チップの移動後のレーン番号 )             // 左にはみ出している
                            {
                                chip.枠外レーン数 = チップの移動後のレーン番号;
                            }
                            else if( レーン数 <= チップの移動後のレーン番号 )    // 右にはみ出している
                            {
                                chip.枠外レーン数 = チップの移動後のレーン番号 - ( レーン数 - 1 );
                            }
                            else
                            {
                                var eチップの移動後の編集レーン = this.Form.譜面.dicレーン番号逆引き[ チップの移動後のレーン番号 ];

                                foreach( var kvp in this.Form.譜面.dicチップ編集レーン対応表 )
                                {
                                    if( kvp.Value == eチップの移動後の編集レーン )   // 対応表で最初に見つけた kvp のチップ種別を採択する。
                                    {
                                        chip.チップ種別 = kvp.Key;
                                        break;
                                    }
                                }
                                chip.枠外レーン数 = 0;
                            }
                            //-----------------
                            #endregion

                            chip.移動済みである = true;
                            this.Form.未保存である = true;
                            //-----------------
                            #endregion
                        }

                        if( 移動量LaneGrid.譜面内絶対位置grid != 0 )
                        {
                            #region " チップを縦に移動する。"
                            //-----------------
                            int 移動後の譜面内絶対位置grid = chip.譜面内絶対位置grid + 移動量LaneGrid.譜面内絶対位置grid;

                            if( 0 > 移動後の譜面内絶対位置grid )
                            {
                                // 画面外なので何もしない。
                            }
                            else if( 移動後の譜面内絶対位置grid >= this.Form.譜面.全小節の高さgrid )
                            {
                                chip.譜面内絶対位置grid = 移動後の譜面内絶対位置grid;     // そこに小節はないが、一応描画する。
                            }
                            else
                            {
                                chip.譜面内絶対位置grid = 移動後の譜面内絶対位置grid;
                            }

                            chip.移動済みである = true;
                            this.Form.未保存である = true;
                            //-----------------
                            #endregion
                        }
                    }
                }

                // 前回位置を現在の位置に更新。
                this.前回のマウス位置LaneGrid = new レーングリッド座標() {
                    編集レーン番号 = 現在のドラッグ終了位置LaneGrid.編集レーン番号,
                    譜面内絶対位置grid = 現在のドラッグ終了位置LaneGrid.譜面内絶対位置grid,
                };
            }
            //-----------------
            #endregion
        }

        protected void 移動の終了処理( MouseEventArgs e )
        {
            try
            {
                this.Form.UndoRedo管理.トランザクション記録を開始する();

                foreach( 描画用チップ chip in this.Form.譜面.SSTFormatScore.チップリスト )
                {
                    // 選択が確定かつ初期位置から移動しているチップのみ対象とする。

                    if( chip.選択が確定している &&
                        ( chip.譜面内絶対位置grid != this.移動開始時のチップ状態[ chip ].譜面内絶対位置grid || chip.チップ種別 != this.移動開始時のチップ状態[ chip ].チップ種別 ) )
                    {
                        // ここではまだ、譜面範囲外に出ている（＝枠外レーン数がゼロでない）チップの削除は行わない。（その後再び移動される可能性があるため。）
                        // これらの削除処理は「t全チップの選択を解除する()」で行う。
                        var chip変更前 = this.移動開始時のチップ状態[ chip ];
                        var 小節情報 = this.Form.譜面.譜面内絶対位置gridに位置する小節の情報を返す( chip.譜面内絶対位置grid );
                        var chip変更後 = new 描画用チップ( chip ) {
                            小節番号 = 小節情報.小節番号,
                            小節解像度 = (int) ( this.Form.GRID_PER_PART * this.Form.譜面.SSTFormatScore.小節長倍率を取得する( 小節情報.小節番号 ) ),
                            小節内位置 = chip.譜面内絶対位置grid - 小節情報.小節の先頭位置grid,
                        };
                        var cell = new UndoRedo.セル<描画用チップ>(
                            所有者ID: null,
                            Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => { 変更対象.CopyFrom( 変更前 ); },
                            Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => { 変更対象.CopyFrom( 変更後 ); },
                            変更対象: chip,
                            変更前の値: chip変更前,
                            変更後の値: chip変更後 );

                        this.Form.UndoRedo管理.セルを追加する( cell );
                        cell.Redoを実行する();
                    }
                }
            }
            finally
            {
                this.Form.UndoRedo管理.トランザクション記録を終了する();

                this.Form.UndoRedo用GUIのEnabledを設定する();
                this.移動のためにドラッグ中である = false;
            }
        }


        // 範囲選択関連

        protected bool 範囲選択のためにドラッグ中である = false;

        protected int 現在の範囲選択用ドラッグ開始位置Xpx = 0;
        protected int 現在の範囲選択用ドラッグ開始位置Ypxの譜面内絶対位置grid = 0;

        protected int 現在の範囲選択用ドラッグ終了位置Xpx = 0;
        protected int 現在の範囲選択用ドラッグ終了位置Ypxの譜面内絶対位置grid = 0;

        protected void 範囲選択の開始処理( MouseEventArgs e )
        {
            this.範囲選択のためにドラッグ中である = true;

            // ドラッグ範囲の初期化。
            this.現在の範囲選択用ドラッグ開始位置Xpx =
                this.現在の範囲選択用ドラッグ終了位置Xpx =
                e.X;
            this.現在の範囲選択用ドラッグ開始位置Ypxの譜面内絶対位置grid =
                this.現在の範囲選択用ドラッグ終了位置Ypxの譜面内絶対位置grid =
                this.Form.譜面.譜面パネル内Y座標pxにおける譜面内絶対位置gridを返す( e.Y );

            // CTRL が押されていない場合、いったん全チップの選択を解除する。
            if( ( Control.ModifierKeys & Keys.Control ) != Keys.Control )
                this.全チップの選択を解除する();

            // 全チップについて、選択・選択解除の取捨選択。
            this.現在のドラッグ範囲中のチップをすべて選択状態にしそれ以外は選択を解除する();
        }

        protected void 範囲選択の継続処理( MouseEventArgs e )
        {
            // クリッピング。
            int Xpt = Math.Clamp( e.X, min: 0, max: this.Form.譜面パネルサイズ.Width );
            int Ypt = Math.Clamp( e.Y, min: 0, max: this.Form.譜面パネルサイズ.Height );

            // ドラッグ終了位置を現在のマウスの位置に更新。
            this.現在の範囲選択用ドラッグ終了位置Xpx = Xpt;
            this.現在の範囲選択用ドラッグ終了位置Ypxの譜面内絶対位置grid = this.Form.譜面.譜面パネル内Y座標pxにおける譜面内絶対位置gridを返す( Ypt );

            // スクロールチェック。
            this.譜面パネルの上下端にきたならスクロールする( e );

            // チップの選択 or 選択解除。
            this.現在のドラッグ範囲中のチップをすべて選択状態にしそれ以外は選択を解除する();
        }

        protected void 範囲選択の終了処理( MouseEventArgs e )
        {
            this.範囲選択のためにドラッグ中である = false;
            try
            {
                this.Form.UndoRedo管理.トランザクション記録を開始する();

                // ドラック選択範囲に入っているがまだ選択が確定していないチップを選択確定させる。
                foreach( 描画用チップ chip in this.Form.譜面.SSTFormatScore.チップリスト )
                {
                    // ドラッグ選択範囲内にあって既に選択が確定されているものについては何もしない。
                    if( chip.ドラッグ操作により選択中である && ( false == chip.選択が確定している ) )
                    {
                        var chip変更前 = new 描画用チップ( chip );
                        var chip変更後 = new 描画用チップ( chip ) {
                            ドラッグ操作により選択中である = false,
                            選択が確定している = true,
                        };
                        var cell = new UndoRedo.セル<描画用チップ>(
                            所有者ID: null,
                            Undoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => { 変更対象.CopyFrom( 変更前 ); },
                            Redoアクション: ( 変更対象, 変更前, 変更後, 任意1, 任意2 ) => { 変更対象.CopyFrom( 変更後 ); },
                            変更対象: chip,
                            変更前の値: chip変更前,
                            変更後の値: chip変更後 );

                        this.Form.UndoRedo管理.セルを追加する( cell );
                        cell.Redoを実行する();
                    }
                }
            }
            finally
            {
                this.Form.UndoRedo管理.トランザクション記録を終了する();

                this.Form.UndoRedo用GUIのEnabledを設定する();
                this.Form.選択チップの有無に応じて編集用GUIのEnabledを設定する();
            }
        }

        protected void 現在のドラッグ範囲中のチップをすべて選択状態にしそれ以外は選択を解除する()
        {
            // 現在のドラッグ範囲を (px, grid) 座標で算出する。
            // ※ px は上→下、grid は下→上に向かって増加するので注意！

            var 現在のドラッグ範囲pxGrid = new Rectangle() {

                X = Math.Min(
                    this.現在の範囲選択用ドラッグ開始位置Xpx,
                    this.現在の範囲選択用ドラッグ終了位置Xpx ),

                Y = Math.Min(
                    this.現在の範囲選択用ドラッグ開始位置Ypxの譜面内絶対位置grid,
                    this.現在の範囲選択用ドラッグ終了位置Ypxの譜面内絶対位置grid ),

                Width = Math.Abs(
                    this.現在の範囲選択用ドラッグ開始位置Xpx -
                    this.現在の範囲選択用ドラッグ終了位置Xpx ),

                Height = Math.Abs(
                    this.現在の範囲選択用ドラッグ開始位置Ypxの譜面内絶対位置grid -
                    this.現在の範囲選択用ドラッグ終了位置Ypxの譜面内絶対位置grid ),
            };

            // 現在のドラッグ範囲を (lane, grid) 座標に変換する。

            int ドラッグ範囲左のレーン番号 = this.Form.譜面.dicレーン番号[ this.Form.譜面.譜面パネル内X座標pxにある編集レーンを返す( 現在のドラッグ範囲pxGrid.Left ) ];
            int ドラッグ範囲右のレーン番号 = this.Form.譜面.dicレーン番号[ this.Form.譜面.譜面パネル内X座標pxにある編集レーンを返す( 現在のドラッグ範囲pxGrid.Right ) ];

            var 現在のドラッグ範囲LaneGrid = new Rectangle() {
                X = ドラッグ範囲左のレーン番号,
                Y = 現在のドラッグ範囲pxGrid.Y,
                Width = Math.Abs( ドラッグ範囲右のレーン番号 - ドラッグ範囲左のレーン番号 ) + 1,  // +1 を忘れずに。
                Height = 現在のドラッグ範囲pxGrid.Height + 1,    // +1 を忘れずに。
            };

            // すべてのチップについて、現在のドラッグ範囲内に存在しているチップは「ドラッグ操作により選択中」フラグを立てる。
            foreach( 描画用チップ chip in this.Form.譜面.SSTFormatScore.チップリスト )
            {
                int チップのレーン番号 = this.Form.譜面.dicレーン番号[ this.Form.譜面.dicチップ編集レーン対応表[ chip.チップ種別 ] ];
                int チップの厚さgrid = this.Form.譜面.チップサイズpx.Height * this.Form.GRID_PER_PIXEL;

                var チップの領域LaneGrid = new Rectangle() {
                    X = チップのレーン番号,
                    Y = chip.譜面内絶対位置grid,
                    Width = 1,  // +1 を忘れずに。
                    Height = チップの厚さgrid,
                };

                chip.ドラッグ操作により選択中である = チップの領域LaneGrid.IntersectsWith( 現在のドラッグ範囲LaneGrid );
            }
        }
    }
}
