using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using FDK;

namespace DTXMania2
{
    /// <summary>
    ///     グローバルリソース。すべて static。
    /// </summary>
    static class Global
    {

        // グローバルプロパティ


        /// <summary>
        ///     スワップチェーンなどのグラフィックリソース。
        /// </summary>
        public static GraphicResources GraphicResources { get; } = new GraphicResources();

        /// <summary>
        ///     メインフォームインスタンスへの参照。
        /// </summary>
        public static App App { get; set; } = null!;

        /// <summary>
        ///     メインフォームインスタンスのウィンドウハンドル。
        /// </summary>
        /// <remarks>
        ///     <see cref="DTXMania2.App.Handle"/> メンバと同じ値であるが、GUIスレッド 以 外 のスレッドから参照する場合は、
        ///     <see cref="DTXMania2.App.Handle"/> ではなくこのメンバを参照すること。
        ///     （<see cref="DTXMania2.App"/> のメンバはすべて、必ずGUIスレッドから参照されなれければならないため。）
        /// </remarks>
        public static IntPtr Handle { get; set; } = IntPtr.Zero;

        /// <summary>
        ///     アプリ起動時のコマンドラインオプション。
        /// </summary>
        /// <remarks>
        ///     <see cref="Program.Main(string[])"/> の引数から生成される。
        ///     YAML化することで、ビュアーモードで起動中の DTXMania2 のパイプラインサーバに送信する事が可能。
        /// </remarks>
        public static CommandLineOptions Options { get; set; } = null!;

        /// <summary>
        ///     タスク間メッセージングに使用するメッセージキュー。
        /// </summary>
        public static TaskMessageQueue TaskMessageQueue { get; private set; } = new TaskMessageQueue();

        /// <summary>
        ///     Windows Animation API 関連。
        /// </summary>
        public static Animation Animation { get; private set; } = null!;

        /// <summary>
        ///     Effekseer 関連。
        /// </summary>
        public static Effekseer Effekseer { get; private set; } = null!;



        // 生成と終了


        /// <summary>
        ///     各種グローバルリソースを生成する。
        /// </summary>
        public static void 生成する( App app, IntPtr hWindow, SharpDX.Size2F 設計画面サイズ, SharpDX.Size2F 物理画面サイズ )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            Global.App = app;
            Global.Handle = hWindow;

            Global.GraphicResources.スワップチェーンに依存しないグラフィックリソースの作成 += _スワップチェーンに依存しないグラフィックリソースの作成;
            Global.GraphicResources.スワップチェーンに依存しないグラフィックリソースの解放 += _スワップチェーンに依存しないグラフィックリソースの解放;
            Global.GraphicResources.スワップチェーンに依存するグラフィックリソースの作成 += _スワップチェーンに依存するグラフィックリソースの作成;
            Global.GraphicResources.スワップチェーンに依存するグラフィックリソースの解放 += _スワップチェーンに依存するグラフィックリソースの解放;
            Global.GraphicResources.生成する( Global.Handle, 設計画面サイズ, 物理画面サイズ );

            Global._Dispose済み = false;
        }

        /// <summary>
        ///     各種グローバルリソースを開放する。
        /// </summary>
        public static void 解放する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            #region " Dispose 済みなら何もしない。"
            //----------------
            if( Global._Dispose済み )
                return;

            Global._Dispose済み = true;
            //----------------
            #endregion

            Global.GraphicResources.Dispose();

            Global.Handle = IntPtr.Zero;
        }

        private static void _スワップチェーンに依存しないグラフィックリソースの作成( object? sender, EventArgs e )
        {
            Global.Animation = new Animation();
        }

        private static void _スワップチェーンに依存しないグラフィックリソースの解放( object? sender, EventArgs e )
        {
            Global.Animation.Dispose();
        }

        private static void _スワップチェーンに依存するグラフィックリソースの作成( object? sender, EventArgs e )
        {
            Global.Effekseer = new Effekseer(
                Global.GraphicResources.D3D11Device1,
                Global.GraphicResources.既定のD3D11DeviceContext,
                Global.GraphicResources.設計画面サイズ.Width,
                Global.GraphicResources.設計画面サイズ.Height );
        }

        private static void _スワップチェーンに依存するグラフィックリソースの解放( object? sender, EventArgs e )
        {
            Global.Effekseer.Dispose();
        }


        // その他


        /// <summary>
        ///		指定されたコマンド名が対象文字列内で使用されている場合に、パラメータ部分の文字列を返す。
        /// </summary>
        /// <remarks>
        ///		.dtx や box.def 等で使用されている "#＜コマンド名＞[:]＜パラメータ＞[;コメント]" 形式の文字列（対象文字列）について、
        ///		指定されたコマンドを使用する行であるかどうかを判別し、使用する行であるなら、そのパラメータ部分の文字列を引数に格納し、true を返す。
        ///		対象文字列のコマンド名が指定したコマンド名と異なる場合には、パラメータ文字列に null を格納して false を返す。
        ///		コマンド名は正しくてもパラメータが存在しない場合には、空文字列("") を格納して true を返す。
        /// </remarks>
        /// <param name="対象文字列">調べる対象の文字列。（例: "#TITLE: 曲名 ;コメント"）</param>
        /// <param name="コマンド名">調べるコマンドの名前（例:"TITLE"）。#は不要、大文字小文字は区別されない。</param>
        /// <returns>パラメータ文字列の取得に成功したら true、異なるコマンドだったなら false。</returns>
        public static bool コマンドのパラメータ文字列部分を返す( string 対象文字列, string コマンド名, out string パラメータ文字列 )
        {
            // コメント部分を除去し、両端をトリムする。なお、全角空白はトリムしない。
            対象文字列 = 対象文字列.Split( ';' )[ 0 ].Trim( ' ', '\t' );

            string 正規表現パターン = $@"^\s*#\s*{コマンド名}(:|\s)+(.*)\s*$";  // \s は空白文字。
            var m = Regex.Match( 対象文字列, 正規表現パターン, RegexOptions.IgnoreCase );

            if( m.Success && ( 3 <= m.Groups.Count ) )
            {
                パラメータ文字列 = m.Groups[ 2 ].Value;
                return true;
            }
            else
            {
                パラメータ文字列 = "";
                return false;
            }
        }



        // ローカル


        private static bool _Dispose済み = true;
    }
}
