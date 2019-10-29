using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace DTXMania2.起動
{
    class 起動ステージ : IStage
    {

        // プロパティ


        public enum フェーズ
        {
            開始,
            グローバルリソース生成中,
            システムサウンド構築中,
            曲ツリー構築中,
            開始音終了待ち,
            完了,
        }
        
        public フェーズ 現在のフェーズ { get; protected set; } = フェーズ.完了;



        // 生成と終了


        public 起動ステージ()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            if( !Global.Options.ビュアーモードである )
            {
                // この２つを先行読み込み。残りはシステムサウンド構築フェーズにて。 
                Global.App.システムサウンド.読み込む( システムサウンド種別.起動ステージ_開始音 );
                Global.App.システムサウンド.読み込む( システムサウンド種別.起動ステージ_ループBGM );

                // さっそく再生。
                Global.App.システムサウンド.再生する( システムサウンド種別.起動ステージ_開始音 );
                Global.App.システムサウンド.再生する( システムサウンド種別.起動ステージ_ループBGM, ループ再生する: true );
            }
            else
            {
                // ビュアーモードならシステムサウンドなし。
            }

            this._コンソールフォント = new フォント画像D2D( @"$(Images)\ConsoleFont20x32.png", @"$(Images)\ConsoleFont20x32.yaml", 文字幅補正dpx: -6f );

            var copyrights = (AssemblyCopyrightAttribute[]) Assembly.GetExecutingAssembly().GetCustomAttributes( typeof( AssemblyCopyrightAttribute ), false );
            this._コンソール表示内容 = new List<string>() {
                    $"{Application.ProductName} Release {int.Parse( Application.ProductVersion.Split( '.' ).ElementAt( 0 ) ):000} - Beats with your heart.",
                    $"{copyrights[ 0 ].Copyright}",
                    $"",
                };

            // 最初のフェーズへ。

            this._コンソール表示内容.Add( "Boot..." );
            this.現在のフェーズ = フェーズ.開始;
        }

        public void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            Global.App.システムサウンド.停止する( システムサウンド種別.起動ステージ_開始音 );
            Global.App.システムサウンド.停止する( システムサウンド種別.起動ステージ_ループBGM );

            this._コンソールフォント.Dispose();
        }



        // 進行と描画


        public void 進行する()
        {
            switch( this.現在のフェーズ )
            {
                case フェーズ.開始:
                    #region " 一度描画処理を通してから（画面を表示させてから）次のフェーズへ。"
                    //----------------
                    // 次のフェーズへ。
                    this._コンソール表示内容[ this._コンソール表示内容.Count - 1 ] += " done.";
                    this._コンソール表示内容.Add( "Creating global resources..." );
                    this.現在のフェーズ = フェーズ.グローバルリソース生成中;
                    //----------------
                    #endregion
                    break;

                case フェーズ.グローバルリソース生成中:
                    #region " *** "
                    //----------------
                    Global.App.グローバルリソースを作成する();

                    // 次のフェーズへ。
                    this._コンソール表示内容[ this._コンソール表示内容.Count - 1 ] += " done.";
                    this._コンソール表示内容.Add( "Creating system sounds..." );
                    this.現在のフェーズ = フェーズ.システムサウンド構築中;
                    //----------------
                    #endregion
                    break;

                case フェーズ.システムサウンド構築中:
                    #region " *** "
                    //----------------
                    Global.App.システムサウンド.すべて生成する();
                    Log.Info( "システムサウンドの読み込みが完了しました。" );

                    Global.App.ドラムサウンド.すべて生成する();
                    Log.Info( "システムサウンドの読み込みが完了しました。" );

                    // 次のフェーズへ。
                    this._コンソール表示内容[ this._コンソール表示内容.Count - 1 ] += " done.";
                    this._コンソール表示内容.Add( "Enumeration songs..." );
                    this.現在のフェーズ = フェーズ.曲ツリー構築中;
                    //----------------
                    #endregion
                    break;

                case フェーズ.曲ツリー構築中:
                    #region " *** "
                    //----------------
                    if( Global.Options.ビュアーモードである )
                    {
                        // (A) ビュアーモードでは曲ツリーを使わないので、構築しない。
                        this._コンソール表示内容[ this._コンソール表示内容.Count - 1 ] += " skipped.";
                    }
                    else
                    {
                        // (B) 通常モードでは曲ツリーの構築を開始する。
                        var tree = new 曲.標準の曲ツリー();

                        tree.構築する( Global.App.システム設定.曲検索フォルダ, Global.App.全譜面リスト );   // 標準の曲ツリーは全譜面リストも一緒に構築する。

                        Global.App.曲ツリーリスト.Add( tree );
                        Global.App.曲ツリーリスト.SelectFirst();

                        this._コンソール表示内容[ this._コンソール表示内容.Count - 1 ] += " done.";
                    }

                    // 次のフェーズへ。
                    this.現在のフェーズ = フェーズ.開始音終了待ち;
                    //----------------
                    #endregion
                    break;

                case フェーズ.開始音終了待ち:
                    #region " 開始音がまだ鳴っていれば、終了するまで待つ。"
                    //----------------
                    if( !Global.App.システムサウンド.再生中( システムサウンド種別.起動ステージ_開始音 ) )
                    {
                        // 再生が終わったので次のフェーズへ。
                        this.現在のフェーズ = フェーズ.完了;
                    }
                    //----------------
                    #endregion
                    break;

                case フェーズ.完了:
                    break;
            }
        }

        public void 描画する()
        {
            var dc = Global.既定のD2D1DeviceContext;
            dc.Transform = Global.拡大行列DPXtoPX;

            // 文字列表示
            for( int i = 0; i < this._コンソール表示内容.Count; i++ )
                this._コンソールフォント.描画する( dc, 0f, i * 32f, this._コンソール表示内容[ i ] );
        }



        // ローカル

        private readonly フォント画像D2D _コンソールフォント;

        private readonly List<string> _コンソール表示内容 = new List<string>();
    }
}
