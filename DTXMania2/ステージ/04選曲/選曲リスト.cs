using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Animation;
using FDK;
using DTXMania2.曲;

namespace DTXMania2.選曲
{
    /// <summary>
    ///		フォーカスリストの表示、スクロール、フォーカスノードの選択など。
    /// </summary>
    /// <remarks>
    ///     フォーカスノードを中心として、フォーカスリストから10ノードを表示する。
    ///		画面に表示されるのは8行だが、スクロールを勘案して上下に１行ずつ追加し、計10行として扱う。
    /// </remarks>
    class 選曲リスト : IDisposable
    {

        // 生成と終了


        public 選曲リスト()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._カーソル位置 = 4;

            this._曲リスト全体のY軸移動オフセット = 0;
            this._スクロール用カウンタ = new 定間隔進行();

            this._選択ノードの表示オフセットdpx = null;
            this._選択ノードの表示オフセットのストーリーボード = null;
            this._選択ノードのオフセットアニメをリセットする();

            this._既定のノード画像 = new 画像D2D( @"$(Images)\DefaultPreviewImage.png" );
            this._現行化前のノード画像 = new 画像D2D( @"$(Images)\PreviewImageWaitForActivation.png" );
            this._成績アイコン = new 画像D2D( @"$(Images)\SelectStage\RecordIcon.png" );
            this._成績アイコンの矩形リスト = new 矩形リスト( @"$(Images)\SelectStage\RecordIcon.yaml" );
            this._評価アイコン = new 画像D2D( @"$(Images)\SelectStage\RatingIcon.png" );
            this._評価アイコンの矩形リスト = new 矩形リスト( @"$(Images)\SelectStage\RatingIcon.yaml" );
            this._達成率ゲージアイコン = new 画像D2D( @"$(Images)\AchivementIcon.png" );
            this._達成率数字画像 = new フォント画像D2D( @"$(Images)\ParameterFont_LargeBoldItalic.png", @"$(Images)\ParameterFont_LargeBoldItalic.yaml", 文字幅補正dpx: -2f, 不透明度: 0.5f );
            this._プレビュー音声 = new プレビュー音声();

            this.フォーカスリストを優先して現行化する();
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._選択ノードの表示オフセットのストーリーボード?.Dispose();
            this._選択ノードの表示オフセットdpx?.Dispose();

            this._プレビュー音声.Dispose();
            this._達成率数字画像.Dispose();
            this._達成率ゲージアイコン.Dispose();
            this._評価アイコン.Dispose();
            this._成績アイコン.Dispose();
            this._現行化前のノード画像.Dispose();
            this._既定のノード画像.Dispose();
        }



        // 進行と描画


        public void 進行描画する( DeviceContext dc )
        {
            var フォーカスノード = Global.App.曲ツリーリスト.SelectedItem!.フォーカスノード;

            #region " 曲リストの縦方向スクロール進行残があれば進行する。"
            //----------------
            this._スクロール用カウンタ.経過時間の分だけ進行する( 1, () => {

                int オフセットの加減算速度 = 1;

                #region " カーソルが中央から遠いほど速くなるよう、オフセットの加減算速度（絶対値）を計算する。"
                //------------------
                int 距離 = Math.Abs( 4 - this._カーソル位置 );

                if( 2 > 距離 )
                    オフセットの加減算速度 = 1;
                else if( 4 > 距離 )
                    オフセットの加減算速度 = 2;
                else if( 6 > 距離 )
                    オフセットの加減算速度 = 3;
                else
                    オフセットの加減算速度 = 4;
                //------------------
                #endregion

                // オフセット と カーソル位置 を更新する。
                if( ( 4 > this._カーソル位置 ) ||
                  ( ( 4 == this._カーソル位置 ) && ( 0 > this._曲リスト全体のY軸移動オフセット ) ) )
                {
                    #region " (A) パネルは、上から下へ、移動する。"
                    //-----------------
                    this._曲リスト全体のY軸移動オフセット += オフセットの加減算速度;

                    // １行分移動した
                    if( 100 <= this._曲リスト全体のY軸移動オフセット )
                    {
                        this._曲リスト全体のY軸移動オフセット -= 100;  // 0 付近に戻る
                        this._カーソル位置++;
                    }
                    //-----------------
                    #endregion
                }
                else if( ( 4 < this._カーソル位置 ) ||
                       ( ( 4 == this._カーソル位置 ) && ( 0 < this._曲リスト全体のY軸移動オフセット ) ) )
                {
                    #region " (B) パネルは、下から上へ、移動する。"
                    //-----------------
                    this._曲リスト全体のY軸移動オフセット -= オフセットの加減算速度;

                    // １行分移動した
                    if( -100 >= this._曲リスト全体のY軸移動オフセット )
                    {
                        this._曲リスト全体のY軸移動オフセット += 100;  // 0 付近に戻る
                        this._カーソル位置--;
                    }
                    //-----------------
                    #endregion
                }

            } );
            //----------------
            #endregion

            #region " フォーカスノードまたはフォーカス譜面が変更されていればプレビュー音声を再生する。"
            //----------------
            if( this._現在のフォーカスノード != フォーカスノード )
            {
                // (A) 別のノードがフォーカスされた

                this._現在のフォーカスノード = フォーカスノード;
                this._プレビュー音声.停止する();

                if( フォーカスノード is SongNode snode && null != snode.曲.フォーカス譜面 )
                {
                    // (A-a) 新しくフォーカスされたのは SongNode である

                    if( !string.IsNullOrEmpty( snode.曲.フォーカス譜面.譜面.PreSound ) )
                    {
                        // (A-a-a) SondNode のフォーカス譜面に PreSound 指定がある

                        var 音声ファイルの絶対パス = Path.Combine(
                            Path.GetDirectoryName( snode.曲.フォーカス譜面.譜面.ScorePath ) ?? @"\",
                            snode.曲.フォーカス譜面.譜面.PreSound );

                        this._プレビュー音声.再生を予約する( 音声ファイルの絶対パス );
                    }
                    else
                    {
                        // (A-a-b) SongNode のフォーカス譜面に PreSound 指定がない
                    }
                }
                else
                {
                    // (A-b) 新しくフォーカスされたのは SongNode ではない
                }
            }
            else if( フォーカスノード is SongNode snode && null != snode.曲.フォーカス譜面 )
            {
                // (B) フォーカスノードは変更されておらず、同一の SongNode のままである

                if( this._現在のフォーカス譜面 != snode.曲.フォーカス譜面 )
                {
                    // (B-a) 同じ SongNode の別の譜面がフォーカスされた

                    this._現在のフォーカス譜面 = snode.曲.フォーカス譜面;

                    if( !string.IsNullOrEmpty( snode.曲.フォーカス譜面.譜面.PreSound ) )
                    {
                        // (B-a-a) SondNode のフォーカス譜面に PreSound 指定がある

                        var 音声ファイルの絶対パス = Path.Combine(
                            Path.GetDirectoryName( snode.曲.フォーカス譜面.譜面.ScorePath ) ?? @"\",
                            snode.曲.フォーカス譜面.譜面.PreSound );

                        this._プレビュー音声.再生を予約する( 音声ファイルの絶対パス );
                    }
                    else
                    {
                        // (B-a-b) SongNode のフォーカス譜面に PreSound 指定がない
                    }
                }
                else
                {
                    // (B-b) 同じ SongNode の同じ譜面をフォーカスしたまま変わっていない
                }
            }
            else
            {
                // (C) フォーカスノードは変更されておらず、それは SongNode でもない
            }
            //----------------
            #endregion

            #region " ノードを10行描画する。"
            //----------------
            {
                var node = Global.App.曲ツリーリスト.SelectedItem!.フォーカスノード;

                if( node is null )
                    return;

                // 表示する最上行のノードまで戻る。
                for( int i = 0; i < this._カーソル位置; i++ )
                    node = node.前のノード;

                // 10行描画する。
                for( int i = 0; i < 10; i++ )
                {
                    this._リストを1行描画する( dc, i, node );
                    node = node.次のノード;
                }
            }
            //----------------
            #endregion
        }

        public void プレビュー音声を停止する() => this._プレビュー音声.停止する();



        // リスト操作


        public void 前のノードを選択する()
        {
            this._カーソル位置--;     // 下限なし

            Global.App.曲ツリーリスト.SelectedItem!.前のノードをフォーカスする();
            this._選択ノードのオフセットアニメをリセットする();
            this.フォーカスノードを優先して現行化する();
        }

        public void 次のノードを選択する()
        {
            this._カーソル位置++;     // 上限なし

            Global.App.曲ツリーリスト.SelectedItem!.次のノードをフォーカスする();
            this._選択ノードのオフセットアニメをリセットする();
            this.フォーカスノードを優先して現行化する();
        }

        public void BOXに入る()
        {
            var boxNode = Global.App.曲ツリーリスト.SelectedItem!.フォーカスノード as BoxNode;
            if( boxNode is null )
                return;

            this._カーソル位置 = 4;
            this._曲リスト全体のY軸移動オフセット = 0;

            Global.App.曲ツリーリスト.SelectedItem!.フォーカスする( boxNode.子ノードリスト[ 0 ] );

            this.フォーカスリストを優先して現行化する();
        }

        public void BOXから出る()
        {
            var node = Global.App.曲ツリーリスト.SelectedItem!.フォーカスノード;
            if( node is null || node.親ノード is null )
                return;

            this._カーソル位置 = 4;
            this._曲リスト全体のY軸移動オフセット = 0;

            Global.App.曲ツリーリスト.SelectedItem!.フォーカスする( node.親ノード );

            this.フォーカスリストを優先して現行化する();
        }

        public async void フォーカスリストを優先して現行化する()
        {
            if( Global.App.曲ツリーリスト.SelectedItem!.フォーカスリスト.Any( ( node ) => !node.現行化済み ) )
            {
                // 現行化スタックは FIFO なので、このスタックに Push するだけで他より優先して現行化されるようになる。
                // このスタックには、すでに Push 済みのノードを重ねて Push しても構わない。（現行化済みのノードは単に無視されるため。）
                await Global.App.現行化.追加するAsync( Global.App.曲ツリーリスト.SelectedItem!.フォーカスリスト );

                // さらに、SongNode 以外（BOX名や「戻る」など）を優先する。
                var nodes = new List<Node>();
                foreach( var node in Global.App.曲ツリーリスト.SelectedItem!.フォーカスリスト )
                {
                    if( !( node is SongNode ) )
                        nodes.Add( node );
                }
                await Global.App.現行化.追加するAsync( nodes );
            }
        }

        public void フォーカスノードを優先して現行化する()
        {
            this.指定したノードを優先して現行化する( Global.App.曲ツリーリスト.SelectedItem!.フォーカスノード );
        }

        public async void 指定したノードを優先して現行化する( Node? node )
        {
            if( null != node && node is SongNode && !node.現行化済み )
            {
                await Global.App.現行化.追加するAsync( new Node[] { node } );
            }
        }


        // ローカル


        /// <summary>
        ///     カーソルの現在位置。
        /// </summary>
        /// <remarks>
        ///		静止時は 4 。曲リストがスクロールしているときは、4より大きい整数（下から上にスクロール中）か、
        ///		または 4 より小さい整数（上から下にスクロール中）になる。
        /// </remarks>
        private int _カーソル位置 = 4;

        private readonly 画像D2D _既定のノード画像;

        private readonly 画像D2D _現行化前のノード画像;

        private readonly 画像D2D _成績アイコン;

        private readonly 矩形リスト _成績アイコンの矩形リスト;

        private readonly 画像D2D _評価アイコン;

        private readonly 矩形リスト _評価アイコンの矩形リスト;

        private readonly 画像D2D _達成率ゲージアイコン;

        private readonly フォント画像D2D _達成率数字画像;

        private readonly 定間隔進行 _スクロール用カウンタ;

        private readonly プレビュー音声 _プレビュー音声;

        private Node? _現在のフォーカスノード = null;

        private Score? _現在のフォーカス譜面 = null;

        /// <summary>
        ///		-100～100。曲リスト全体の表示位置を、負数は 上 へ、正数は 下 へずらす 。（正負と上下の対応に注意。）
        /// </summary>
        private int _曲リスト全体のY軸移動オフセット;

        /// <summary>
        ///		選択中の曲ノードエリアを左にずらす度合い。
        ///		-50f ～ 0f [dpx] 。
        /// </summary>
        private Variable? _選択ノードの表示オフセットdpx;

        private Storyboard? _選択ノードの表示オフセットのストーリーボード;

        private const float _ノードの高さdpx = ( 913f / 8f );

        /// <summary>
        ///		曲リスト（10行分）の合計表示領域の左上隅の座標。
        /// </summary>
        /// <remarks>
        ///		基準というのは、曲リストがスクロールしていないときの位置、という意味。
        /// </remarks>
        private readonly Vector3 _曲リストの基準左上隅座標dpx = new Vector3( 1065f, 145f - _ノードの高さdpx, 0f );

        private readonly Vector3 _サムネイル表示サイズdpx = new Vector3( 100f, 100f, 0f );


        /// <param name="行番号">
        ///		一番上:0 ～ 9:一番下。
        ///		「静止時の」可視範囲は 1～8。4 がフォーカスノード。
        ///	</param>
        private void _リストを1行描画する( DeviceContext dc, int 行番号, Node node )
        {
            bool 選択ノードである = ( 4 == 行番号 );

            float 実数行番号 = 行番号 + this._曲リスト全体のY軸移動オフセット / 100.0f;

            var ノード左上dpx = new Vector2(
                this._曲リストの基準左上隅座標dpx.X + ( 選択ノードである ? (float) ( this._選択ノードの表示オフセットdpx?.Value ?? 0f ) : 0f ),
                this._曲リストの基準左上隅座標dpx.Y + ( 実数行番号 * _ノードの高さdpx ) );

            var preBlend = dc.PrimitiveBlend;

            #region " 背景 "
            //----------------
            dc.PrimitiveBlend = PrimitiveBlend.SourceOver;

            if( node is BoxNode )
            {
                #region " BOXノードの背景 "
                //----------------
                using var brush = new SolidColorBrush( dc, new Color4( 0xffa3647c ) );
                using var pathGeometry = new PathGeometry( Global.D2D1Factory1 );
                using( var sink = pathGeometry.Open() )
                {
                    sink.SetFillMode( FillMode.Winding );
                    sink.BeginFigure( new Vector2( ノード左上dpx.X, ノード左上dpx.Y + 8f ), FigureBegin.Filled );     // 点1
                    var points = new SharpDX.Mathematics.Interop.RawVector2[] {
                            new Vector2( ノード左上dpx.X + 150f, ノード左上dpx.Y + 8f ),	                              // → 点2
                            new Vector2( ノード左上dpx.X + 170f, ノード左上dpx.Y + 18f ),                                 // → 点3
                            new Vector2( Global.設計画面サイズ.Width, ノード左上dpx.Y + 18f ),	                          // → 点4
                            new Vector2( Global.設計画面サイズ.Width, ノード左上dpx.Y + _ノードの高さdpx ),               // → 点5
                            new Vector2( ノード左上dpx.X, ノード左上dpx.Y + _ノードの高さdpx ),	                          // → 点6
                            new Vector2( ノード左上dpx.X, ノード左上dpx.Y + 8f ),	                                      // → 点1
                        };
                    sink.AddLines( points );
                    sink.EndFigure( FigureEnd.Closed );
                    sink.Close();
                }
                dc.FillGeometry( pathGeometry, brush );
                //----------------
                #endregion
            }
            else if( node is BackNode || node is RandomSelectNode )
            {
                #region " BACK, RandomSelectノードの背景 "
                //----------------
                using var brush = new SolidColorBrush( dc, Color4.Black );
                using var pathGeometry = new PathGeometry( Global.D2D1Factory1 );
                using( var sink = pathGeometry.Open() )
                {
                    sink.SetFillMode( FillMode.Winding );
                    sink.BeginFigure( new Vector2( ノード左上dpx.X, ノード左上dpx.Y + 8f ), FigureBegin.Filled ); // 点1
                    var points = new SharpDX.Mathematics.Interop.RawVector2[] {
                            new Vector2( ノード左上dpx.X + 150f, ノード左上dpx.Y + 8f ),	                          // → 点2
							new Vector2( ノード左上dpx.X + 170f, ノード左上dpx.Y + 18f ),	                          // → 点3
							new Vector2( Global.設計画面サイズ.Width, ノード左上dpx.Y + 18f ),                        // → 点4
							new Vector2( Global.設計画面サイズ.Width, ノード左上dpx.Y + _ノードの高さdpx ),           // → 点5
							new Vector2( ノード左上dpx.X, ノード左上dpx.Y + _ノードの高さdpx ),	                      // → 点6
							new Vector2( ノード左上dpx.X, ノード左上dpx.Y + 8f ),	                                  // → 点1
						};
                    sink.AddLines( points );
                    sink.EndFigure( FigureEnd.Closed );
                    sink.Close();
                }
                dc.FillGeometry( pathGeometry, brush );
                //----------------
                #endregion
            }
            else
            {
                #region " 既定の背景 "
                //----------------
                using var brush = new SolidColorBrush( dc, new Color4( 0f, 0f, 0f, 0.25f ) );   // 半透明の黒
                dc.FillRectangle( new RectangleF( ノード左上dpx.X, ノード左上dpx.Y, Global.設計画面サイズ.Width - ノード左上dpx.X, _ノードの高さdpx ), brush );
                //----------------
                #endregion
            }

            dc.PrimitiveBlend = preBlend;
            //----------------
            #endregion

            #region " サムネイル画像 "
            //----------------
            {
                // ノード画像を縮小して表示する。
                var ノード画像 = node.現行化済み ? ( node.ノード画像 ?? this._既定のノード画像 ) : this._現行化前のノード画像;

                var ノード内サムネイルオフセットdpx = new Vector3( 58f, 4f, 0f );
                var サムネイル表示左上dpx = new Vector2(
                    ノード左上dpx.X + ノード内サムネイルオフセットdpx.X,
                    ノード左上dpx.Y + ノード内サムネイルオフセットdpx.Y );

                if( node is BoxNode )
                {
                    #region " BOXノードのサムネイル画像 → 普通のノードよりも少し小さく表示する（涙 "
                    //----------------
                    var 変換行列2D =
                        Matrix3x2.Scaling(
                            this._サムネイル表示サイズdpx.X / ノード画像.サイズ.Width,
                            this._サムネイル表示サイズdpx.Y / ノード画像.サイズ.Height ) *
                        Matrix3x2.Scaling( 0.9f ) *         // ちょっと小さく
                        Matrix3x2.Translation( サムネイル表示左上dpx ) *
                        Matrix3x2.Translation( 0f, +12f );   // ちょっと下へ

                    ノード画像.描画する( dc, 変換行列2D );
                    //----------------
                    #endregion
                }
                else if( node is BackNode || node is RandomSelectNode )
                {
                    // BACK, RandomSelectノードはサムネイル画像なし
                }
                else
                {
                    #region " 既定のサムネイル画像 "
                    //----------------
                    var 変換行列2D =
                        Matrix3x2.Scaling(
                            this._サムネイル表示サイズdpx.X / ノード画像.サイズ.Width,
                            this._サムネイル表示サイズdpx.Y / ノード画像.サイズ.Height ) *
                        Matrix3x2.Translation( サムネイル表示左上dpx );

                    ノード画像.描画する( dc, 変換行列2D );
                    //----------------
                    #endregion
                }
            }
            //----------------
            #endregion

            #region " 成績・評価 "
            //----------------
            if( node is SongNode snode )
            {
                var score = snode.曲.フォーカス譜面;
                if( null != score )
                {
                    if( score.最高記録を現行化済み && ( null != score.最高記録 ) )
                    {
                        var 最高ランク = score.最高ランク!;
                        var 達成率 = score.最高記録.Achievement;

                        #region " 成績アイコン "
                        //----------------
                        this._成績アイコン.描画する(
                            dc,
                            ノード左上dpx.X + 6f,
                            ノード左上dpx.Y + 57f,
                            転送元矩形: this._成績アイコンの矩形リスト[ 最高ランク.ToString()! ] );
                        //----------------
                        #endregion

                        #region " 達成率ゲージ "
                        //----------------
                        this._達成率ゲージアイコン.描画する(
                            dc,
                            ノード左上dpx.X + 160f,
                            ノード左上dpx.Y - 27f,
                            X方向拡大率: 0.4f,
                            Y方向拡大率: 0.4f );

                        this._達成率数字画像.描画する(
                            dc,
                            ノード左上dpx.X + 204f,
                            ノード左上dpx.Y + 4,
                            score.最高記録.Achievement.ToString( "0.00" ).PadLeft( 6 ) + '%',
                            拡大率: new Size2F( 0.3f, 0.3f ) );

                        using var ゲージ色 = new SolidColorBrush( dc, new Color( 184, 156, 231, 255 ) );
                        using var ゲージ枠色 = new SolidColorBrush( dc, Color.White );
                        using var ゲージ背景色 = new SolidColorBrush( dc, new Color( 0.25f, 0.25f, 0.25f, 1f ) );
                        using var ゲージ枠ジオメトリ = new PathGeometry( Global.D2D1Factory1 );
                        using var ゲージジオメトリ = new PathGeometry( Global.D2D1Factory1 );

                        var ゲージサイズdpx = new Size2F( 448f, 17f );
                        var ゲージ位置 = new Vector2( ノード左上dpx.X + 310f, ノード左上dpx.Y + 10f );

                        using( var sink = ゲージジオメトリ.Open() )
                        {
                            var 割合0to1 = (float) ( 達成率 / 100.0 );
                            var p = new Vector2[] {
                                new Vector2( ゲージ位置.X, ゲージ位置.Y ),                                                                    // 左上
                                new Vector2( ゲージ位置.X + ゲージサイズdpx.Width * 割合0to1, ゲージ位置.Y ),                                 // 右上
                                new Vector2( ゲージ位置.X + ゲージサイズdpx.Width * 割合0to1 - 3f, ゲージ位置.Y + ゲージサイズdpx.Height ),   // 右下
                                new Vector2( ゲージ位置.X - 3f, ゲージ位置.Y + ゲージサイズdpx.Height ),                                      // 左下
                            };
                            sink.SetFillMode( FillMode.Winding );
                            sink.BeginFigure( p[ 0 ], FigureBegin.Filled );
                            sink.AddLine( p[ 1 ] );
                            sink.AddLine( p[ 2 ] );
                            sink.AddLine( p[ 3 ] );
                            sink.EndFigure( FigureEnd.Closed );
                            sink.Close();
                        }
                        using( var sink = ゲージ枠ジオメトリ.Open() )
                        {
                            var p = new Vector2[] {
                                new Vector2( ゲージ位置.X, ゲージ位置.Y ),                                                         // 左上
                                new Vector2( ゲージ位置.X + ゲージサイズdpx.Width, ゲージ位置.Y ),                                 // 右上
                                new Vector2( ゲージ位置.X + ゲージサイズdpx.Width - 3f, ゲージ位置.Y + ゲージサイズdpx.Height ),   // 右下
                                new Vector2( ゲージ位置.X - 3f, ゲージ位置.Y + ゲージサイズdpx.Height ),                           // 左下
                            };
                            sink.SetFillMode( FillMode.Winding );
                            sink.BeginFigure( p[ 0 ], FigureBegin.Filled );
                            sink.AddLine( p[ 1 ] );
                            sink.AddLine( p[ 2 ] );
                            sink.AddLine( p[ 3 ] );
                            sink.EndFigure( FigureEnd.Closed );
                            sink.Close();
                        }

                        dc.FillGeometry( ゲージ枠ジオメトリ, ゲージ背景色 );
                        dc.FillGeometry( ゲージジオメトリ, ゲージ色 );
                        dc.DrawGeometry( ゲージジオメトリ, ゲージ枠色, 1f );
                        dc.DrawGeometry( ゲージ枠ジオメトリ, ゲージ枠色, 2f );
                        //----------------
                        #endregion
                    }

                    #region " 評価アイコン "
                    //----------------
                    if( score.譜面の属性を現行化済み )
                    {
                        var 評価 = score.譜面の属性?.Rating ?? 0;    // 0～4; nullは0扱い

                        if( 0 < 評価 )
                        {
                            this._評価アイコン.描画する(
                                dc,
                                ノード左上dpx.X + 6f,
                                ノード左上dpx.Y + 0f,
                                転送元矩形: this._評価アイコンの矩形リスト[ 評価.ToString() ] );
                        }
                    }
                    //----------------
                    #endregion
                }
            }
            //----------------
            #endregion

            #region " タイトル文字列 "
            //----------------
            //if( node.現行化済み )  --> タイトル文字列とサブタイトル文字列は現行前から生成されている。
            {
                var image = node.タイトル文字列画像;

                // 最大幅を考慮して拡大率を決定する。
                float 最大幅dpx = Global.設計画面サイズ.Width - ノード左上dpx.X - 170f;

                if( null != image )
                {
                    image.描画する( 
                        dc,
                        ノード左上dpx.X + 170f,
                        ノード左上dpx.Y + 20f,
                        X方向拡大率: ( image.画像サイズdpx.Width <= 最大幅dpx ) ? 1f : 最大幅dpx / image.画像サイズdpx.Width );
                }
            }
            //----------------
            #endregion

            #region " サブタイトル文字列 "
            //----------------
            if( 選択ノードである )//&& node.現行化済み ) --> タイトル文字列とサブタイトル文字列は現行前から生成されている。
            {
                var image = node.サブタイトル文字列画像;

                // 最大幅を考慮して拡大率を決定する。
                float 最大幅dpx = Global.設計画面サイズ.Width - ノード左上dpx.X - 170f;

                if( null != image )
                {
                    image.描画する(
                        dc,
                        ノード左上dpx.X + 190f,
                        ノード左上dpx.Y + 70f,
                        X方向拡大率: ( image.画像サイズdpx.Width <= 最大幅dpx ) ? 1f : 最大幅dpx / image.画像サイズdpx.Width );
                }
            }
            //----------------
            #endregion
        }

        private void _選択ノードのオフセットアニメをリセットする()
        {
            this._選択ノードの表示オフセットdpx?.Dispose();
            this._選択ノードの表示オフセットdpx = new Variable( Global.Animation.Manager, initialValue: 0.0 );

            this._選択ノードの表示オフセットのストーリーボード?.Dispose();
            this._選択ノードの表示オフセットのストーリーボード = new Storyboard( Global.Animation.Manager );

            using( var 維持 = Global.Animation.TrasitionLibrary.Constant( 0.15 ) )
            using( var 左へ移動 = Global.Animation.TrasitionLibrary.Linear( 0.07, finalValue: -50f ) )
            {
                this._選択ノードの表示オフセットのストーリーボード.AddTransition( this._選択ノードの表示オフセットdpx, 維持 );
                this._選択ノードの表示オフセットのストーリーボード.AddTransition( this._選択ノードの表示オフセットdpx, 左へ移動 );
            }

            this._選択ノードの表示オフセットのストーリーボード.Schedule( Global.Animation.Timer.Time );
        }
    }
}
