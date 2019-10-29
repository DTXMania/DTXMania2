using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using SharpDX;
        
namespace DTXMania2.曲
{
    /// <summary>
    ///		BOX定義ファイルのマッピングクラス。
    /// </summary>
    public class BoxDef
    {

        // プロパティ


        /// <summary>
        ///     BOX名。
        /// </summary>
        public string TITLE { get; set; } = "(unknown)";

        /// <summary>
        ///     ボックスに関係するアーティスト名等。
        ///     未指定なら null 。
        /// </summary>
        public string? ARTIST { get; set; } = null;

        /// <summary>
        ///     BOXへのコメント。
        ///     未指定なら null 。
        /// </summary>
        public string? COMMENT { get; set; } = null;

        /// <summary>
        ///     BOXのプレビュー画像。
        ///     未指定なら null 。
        /// </summary>
        public string? PREIMAGE { get; set; } = null;

        /// <summary>
        ///     BOXのプレビュー音声。
        ///     未指定なら null 。
        /// </summary>
        public string? PREVIEW { get; set; } = null;

        /// <summary>
        ///     BOXのフォント色。
        ///     未指定なら null 。
        /// </summary>
        public Color? FONTCOLOR { get; set; } = null;

        /// <summary>
        ///     BOX内の曲に適用される Perfect 範囲[秒]。
        /// </summary>
        public double? PERFECTRANGE { get; set; } = null;

        /// <summary>
        ///     BOX内の曲に適用される Great 範囲[秒]。
        /// </summary>
        public double? GREATRANGE { get; set; } = null;

        /// <summary>
        ///     BOX内の曲に適用される Good 範囲[秒]。
        /// </summary>
        public double? GOODRANGE { get; set; } = null;

        /// <summary>
        ///     BOX内の曲に適用される Ok 範囲[秒]。
        /// </summary>
        public double? OKRANGE { get; set; } = null;

        /// <summary>
        ///     スキンへのパス。
        ///     box.def のあるフォルダからの相対パス。
        /// </summary>
        public string SKINPATH { get; set; } = "";

        /// <summary>
        ///     スキンへのパス（DTXMania Release 100 移行用）。
        ///     box.def のあるフォルダからの相対パス。
        /// </summary>
        public string SKINPATH100 { get; set; } = "";



        // 生成と終了


        public BoxDef()
        {
        }

        public BoxDef( VariablePath boxDefファイルパス )
            : this()
        {
            this.読み込む( boxDefファイルパス );
        }

        public void 読み込む( VariablePath boxDefファイルパス )
        {
            using var sr = new StreamReader( boxDefファイルパス.変数なしパス, Encoding.GetEncoding( 932/*Shift-JIS*/ ) );

            string? 行;

            while( null != ( 行 = sr.ReadLine() ) )
            {
                try
                {
                    string パラメータ;

                    #region " TITLE コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"TITLE", out パラメータ ) )
                    {
                        this.TITLE = パラメータ;
                        continue;
                    }
                    //---------------------
                    #endregion

                    #region " COMMENT コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"COMMENT", out パラメータ ) )
                    {
                        this.COMMENT = パラメータ;
                        continue;
                    }
                    //---------------------
                    #endregion

                    #region " ARTIST コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"ARTIST", out パラメータ ) )
                    {
                        this.ARTIST = パラメータ;
                        continue;
                    }
                    //---------------------
                    #endregion

                    #region " PREIMAGE コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"PREIMAGE", out パラメータ ) )
                    {
                        this.PREIMAGE = パラメータ;
                        continue;
                    }
                    //---------------------
                    #endregion

                    #region " PREVIEW コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"PREVIEW", out パラメータ ) )
                    {
                        this.PREVIEW = パラメータ;
                        continue;
                    }
                    //---------------------
                    #endregion

                    #region " PERFECTRANGE コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"PERFECTRANGE", out パラメータ ) )
                    {
                        if( int.TryParse( パラメータ, out int value ) )
                            this.PERFECTRANGE = value / 0.001;
                        else
                            Log.ERROR( $"PERFECTRANGE の指定に誤りがあります。[{パラメータ}]" );
                        continue;
                    }
                    //---------------------
                    #endregion

                    #region " GREATRANGE コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"GREATRANGE", out パラメータ ) )
                    {
                        if( int.TryParse( パラメータ, out int value ) )
                            this.GREATRANGE = value / 0.001;
                        else
                            Log.ERROR( $"GREATRANGE の指定に誤りがあります。[{パラメータ}]" );
                        continue;
                    }
                    //---------------------
                    #endregion

                    #region " GOODRANGE コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"GOODRANGE", out パラメータ ) )
                    {
                        if( int.TryParse( パラメータ, out int value ) )
                            this.GOODRANGE = value / 0.001;
                        else
                            Log.ERROR( $"GOODRANGE の指定に誤りがあります。[{パラメータ}]" );
                        continue;
                    }
                    //---------------------
                    #endregion

                    #region " OKRANGE コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"OKRANGE", out パラメータ ) )
                    {
                        if( int.TryParse( パラメータ, out int value ) )
                            this.OKRANGE = value / 0.001;
                        else
                            Log.ERROR( $"OKRANGE の指定に誤りがあります。[{パラメータ}]" );
                        continue;
                    }
                    //---------------------
                    #endregion

                    #region " POORRANGE コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"POORRANGE", out パラメータ ) )
                    {
                        if( int.TryParse( パラメータ, out int value ) )
                            this.OKRANGE = value / 0.001;
                        else
                            Log.ERROR( $"POORRANGE の指定に誤りがあります。[{パラメータ}]" );
                        continue;
                    }
                    //---------------------
                    #endregion

                    #region " SKINPATH コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"SKINPATH", out パラメータ ) )
                    {
                        this.SKINPATH = パラメータ;
                        continue;
                    }
                    //---------------------
                    #endregion

                    #region " SKINPATH100 コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"SKINPATH100", out パラメータ ) )
                    {
                        this.SKINPATH100 = パラメータ;
                        continue;
                    }
                    //---------------------
                    #endregion
                }
                catch
                {
                    // 例外は無視。
                }
            }
        }

        public void 保存する( VariablePath boxDefファイルパス )
        {
            using var sw = new StreamWriter( boxDefファイルパス.変数なしパス );

            sw.WriteLine( $"#TITLE: {this.TITLE}" );
            
            if( !string.IsNullOrEmpty( this.ARTIST ) ) 
                sw.WriteLine( $"#ARTIST: {this.ARTIST}" );
            
            if( !string.IsNullOrEmpty( this.COMMENT ) )
                sw.WriteLine( $"#COMMENT: {this.COMMENT}" );

            if( !string.IsNullOrEmpty( this.PREIMAGE ) )
                sw.WriteLine( $"#PREIMAGE: {this.PREIMAGE}" );

            if( !string.IsNullOrEmpty( this.PREVIEW ) )
                sw.WriteLine( $"#PREVIEW: {this.PREVIEW}" );

            if( null != this.FONTCOLOR )
                sw.WriteLine( $"#FONTCOLOR: #{this.FONTCOLOR.Value.R:X2}{this.FONTCOLOR.Value.G:X2}{this.FONTCOLOR.Value.B:X2}" );

            if( null != this.PERFECTRANGE )
                sw.WriteLine( $"#PERFECTRANGE: {(int) ( this.PERFECTRANGE * 1000 )}" );

            if( null != this.GREATRANGE )
                sw.WriteLine( $"#GREATRANGE: {(int) ( this.GREATRANGE * 1000 )}" );

            if( null != this.GOODRANGE )
                sw.WriteLine( $"#GOODRANGE: {(int) ( this.GOODRANGE * 1000 )}" );

            if( null != this.OKRANGE )
                sw.WriteLine( $"#OKRANGE: {(int) ( this.OKRANGE * 1000 )}" );

            if( !string.IsNullOrEmpty( this.SKINPATH ) )
                sw.WriteLine( $"#SKINPATH: {this.SKINPATH}" );

            if( !string.IsNullOrEmpty( this.SKINPATH100 ) )
                sw.WriteLine( $"#SKINPATH100: {this.SKINPATH100}" );
        }
    }
}
