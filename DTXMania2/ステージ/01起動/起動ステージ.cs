using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using FDK;
using SharpDX.Direct2D1;

namespace DTXMania2.起動
{
    class 起動ステージ : IStage
    {

        // プロパティ


        public enum フェーズ
        {
            開始,
            グローバルリソース生成中,
            システムサウンド構築開始,
            システムサウンド構築完了待ち,
            曲ツリー構築開始,
            曲ツリー構築完了待ち,
            曲ツリーDB反映開始,
            曲ツリーDB反映完了待ち,
            曲ツリー文字列画像生成開始,
            曲ツリー文字列画像生成完了待ち,
            開始音終了待ち開始,
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
                {
                    #region " 一度描画処理を通してから（画面を表示させてから）次のフェーズへ。"
                    //----------------
                    this._コンソール表示内容[ ^1 ] += " done.";

                    // 次のフェーズへ。
                    this._コンソール表示内容.Add( "Creating global resources..." );
                    this.現在のフェーズ = フェーズ.グローバルリソース生成中;
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.グローバルリソース生成中:
                {
                    #region " グローバルリソースを生成して次のフェーズへ。"
                    //----------------

                    //Global.App.グローバルリソースを作成する();  --> 今は何もしない。全部作成済み。

                    this._コンソール表示内容[ ^1 ] += " done.";

                    // 次のフェーズへ。
                    this.現在のフェーズ = フェーズ.システムサウンド構築開始;
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.システムサウンド構築開始:
                {
                    #region " システムサウンドの構築を開始して次のフェーズへ。"
                    //----------------
                    // 次のフェーズへ。
                    this._コンソール表示内容.Add( "Creating system sounds..." );
                    this.現在のフェーズ = フェーズ.システムサウンド構築完了待ち;
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.システムサウンド構築完了待ち:
                {
                    #region " システムサウンドを構築して次のフェーズへ。"
                    //----------------
                    Global.App.システムサウンド.すべて生成する();
                    Log.Info( "システムサウンドの読み込みが完了しました。" );

                    Global.App.ドラムサウンド.すべて生成する();
                    Log.Info( "ドラムサウンドの読み込みが完了しました。" );

                    this._コンソール表示内容[ ^1 ] += " done.";

                    // 次のフェーズへ。
                    this.現在のフェーズ = ( Global.Options.ビュアーモードである ) ? フェーズ.開始音終了待ち開始 : フェーズ.曲ツリー構築開始;
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.曲ツリー構築開始:
                {
                    #region " 曲ツリーの構築を開始して次のフェーズへ。"
                    //----------------
                    this._コンソール表示内容.Add( "Enumeration songs..." );

                    var allTree = new 曲.曲ツリー_全曲();
                    var ratingTree = new 曲.曲ツリー_評価順();
                    Global.App.曲ツリーリスト.Add( allTree );
                    Global.App.曲ツリーリスト.Add( ratingTree );
                    Global.App.曲ツリーリスト.SelectFirst();

                    // 全曲ツリーの構築を開始する。（現行化はまだ）
                    this._曲ツリー構築タスク = allTree.構築するAsync( Global.App.システム設定.曲検索フォルダ );
                    // 評価順ツリーの構築は、ユーザのログオン時に行う。

                    // 次のフェーズへ。
                    this.現在のフェーズ = フェーズ.曲ツリー構築完了待ち;
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.曲ツリー構築完了待ち:
                {
                    #region " 曲ツリーの構築の完了を待って、次のフェーズへ。"
                    //----------------
                    if( !this._曲ツリー構築タスク.IsCompleted )
                    {
                        // 進捗カウンタを表示。
                        var allTree = (曲.曲ツリー_全曲) Global.App.曲ツリーリスト[ 0 ];
                        this._コンソール表示内容[ ^1 ] = $"Enumeration songs... {allTree.進捗カウンタ}";
                    }
                    else
                    {
                        this._コンソール表示内容[ ^1 ] += ", done.";

                        // 次のフェーズへ。
                        this.現在のフェーズ = フェーズ.曲ツリーDB反映開始;
                    }
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.曲ツリーDB反映開始:
                {
                    #region " 曲ツリーへのDB反映を開始して次のフェーズへ。"
                    //----------------
                    this._コンソール表示内容.Add( "Update songs..." );

                    var allTree = (曲.曲ツリー_全曲) Global.App.曲ツリーリスト[ 0 ];
                    this._曲ツリーDB反映タスク = allTree.ノードにDBを反映するAsync();

                    // 次のフェーズへ。
                    this.現在のフェーズ = フェーズ.曲ツリーDB反映完了待ち;
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.曲ツリーDB反映完了待ち:
                {
                    #region " 曲ツリーのDB反映の完了を待って、次のフェーズへ。"
                    //----------------
                    if( !this._曲ツリーDB反映タスク.IsCompleted )
                    {
                        // 進捗カウンタを表示。
                        var allTree = (曲.曲ツリー_全曲) Global.App.曲ツリーリスト[ 0 ];
                        this._コンソール表示内容[ ^1 ] = $"Update songs... {allTree.進捗カウンタ}";
                    }
                    else
                    {
                        this._コンソール表示内容[ ^1 ] += ", done.";

                        // 次のフェーズへ。
                        this.現在のフェーズ = フェーズ.曲ツリー文字列画像生成開始;
                    }
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.曲ツリー文字列画像生成開始:
                {
                    #region " 曲ツリーの文字列画像の生成を開始して次のフェーズへ。"
                    //----------------
                    this._コンソール表示内容.Add( "Building label images..." );

                    var allTree = (曲.曲ツリー_全曲) Global.App.曲ツリーリスト[ 0 ];
                    this._曲ツリー文字列画像生成タスク = allTree.文字列画像を生成するAsync();

                    // 次のフェーズへ。
                    this.現在のフェーズ = フェーズ.曲ツリー文字列画像生成完了待ち;
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.曲ツリー文字列画像生成完了待ち:
                {
                    #region " 曲ツリーの文字列画像生成の完了を待って、次のフェーズへ。"
                    //----------------
                    if( !this._曲ツリー文字列画像生成タスク.IsCompleted )
                    {
                        // 進捗カウンタを表示。
                        var allTree = (曲.曲ツリー_全曲) Global.App.曲ツリーリスト[ 0 ];
                        this._コンソール表示内容[ ^1 ] = $"Building label images... {allTree.進捗カウンタ}";
                    }
                    else
                    {
                        this._コンソール表示内容[ ^1 ] += ", done.";

                        // 次のフェーズへ。
                        this.現在のフェーズ = フェーズ.開始音終了待ち開始;
                    }
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.開始音終了待ち開始:
                {
                    #region " 開始音の終了待ちを開始して次のフェーズへ。"
                    //----------------
                    this._コンソール表示内容.Add( "All setup done." );

                    // 次のフェーズへ。
                    this.現在のフェーズ = フェーズ.開始音終了待ち;
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.開始音終了待ち:
                {
                    #region " 開始音が鳴り止むのを待って、次のフェーズへ。"
                    //----------------
                    // 開始音がまだ鳴っていれば、終了するまで待つ。
                    if( !Global.App.システムサウンド.再生中( システムサウンド種別.起動ステージ_開始音 ) )
                    {
                        // 再生が終わったので次のフェーズへ。
                        this.現在のフェーズ = フェーズ.完了;
                    }
                    break;
                    //----------------
                    #endregion
                }
                case フェーズ.完了:
                {
                    #region " 遷移終了。Appによるステージ遷移を待つ。"
                    //----------------
                    break;
                    //----------------
                    #endregion
                }
            }
        }

        public void 描画する()
        {
            Global.App.画面をクリアする();

            var dc = Global.GraphicResources.既定のD2D1DeviceContext;
            dc.Transform = SharpDX.Matrix3x2.Identity;

            dc.BeginDraw();

            #region " 文字列表示 "
            //----------------
            for( int i = 0; i < this._コンソール表示内容.Count; i++ )
                this._コンソールフォント.描画する( dc, 0f, i * 32f, this._コンソール表示内容[ i ] );
            //----------------
            #endregion

            dc.EndDraw();
        }



        // ローカル


        private readonly フォント画像D2D _コンソールフォント;

        private readonly List<string> _コンソール表示内容 = new List<string>();

        private Task _曲ツリー構築タスク = null!;

        private Task _曲ツリーDB反映タスク = null!;

        private Task _曲ツリー文字列画像生成タスク = null!;
    }
}
