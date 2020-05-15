using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using SharpDX;
using FDK;

namespace DTXMania2.曲
{
    partial class SetDef
    {

        // プロパティ


        public static readonly string[] デフォルトのラベル = new string[] { "BASIC", "ADVANCED", "EXTREME", "MASTER", "ULTIMATE" };

        public List<Block> Blocks = new List<Block>();



        // 生成と終了


        public SetDef()
        {
        }

        public SetDef( VariablePath SetDefファイルパス )
            : this()
        {
            this.読み込む( SetDefファイルパス );
        }

        /// <summary>
        ///     set.def ファイルを読み込む。
        ///     内容は上書きされる。
        /// </summary>
        public void 読み込む( VariablePath SetDefファイルパス )
        {
            using var sr = new StreamReader( SetDefファイルパス.変数なしパス, Encoding.GetEncoding( 932/*Shift-JIS*/ ) );

            var block = new Block();
            var blockが有効 = false;
            string? 行;

            while( null != ( 行 = sr.ReadLine() ) )
            {
                try
                {
                    string パラメータ = "";

                    #region " TITLE コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"TITLE", out パラメータ ) )
                    {
                        if( blockが有効 )
                        {
                            // 次のブロックに入ったので、現在のブロックを保存して新しいブロックを用意する。
                            _FILEの指定があるのにLxLABELが省略されているときはデフォルトの名前をセットする( block );
                            _LxLABELの指定があるのにFILEが省略されているときはなかったものとする( block );
                            this.Blocks.Add( block ); // リストに追加して
                            block = new Block();      // 新規作成。
                        }
                        block.Title = パラメータ;
                        blockが有効 = true;
                        continue;
                    }
                    //---------------------
                    #endregion

                    #region " FONTCOLOR コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"FONTCOLOR", out パラメータ ) )
                    {
                        if( !( パラメータ.StartsWith( "#" ) ) )
                            パラメータ = "#" + パラメータ;
                        var sysColor = System.Drawing.ColorTranslator.FromHtml( $"{パラメータ}" );
                        block.FontColor = new Color( sysColor.R, sysColor.G, sysColor.B, sysColor.A );
                        blockが有効 = true;
                        continue;
                    }
                    //---------------------
                    #endregion

                    #region " L1FILE コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"L1FILE", out パラメータ ) )
                    {
                        block.File[ 0 ] = パラメータ;
                        blockが有効 = true;
                        continue;
                    }
                    //---------------------
                    #endregion
                    
                    #region " L2FILE コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"L2FILE", out パラメータ ) )
                    {
                        block.File[ 1 ] = パラメータ;
                        blockが有効 = true;
                        continue;
                    }
                    //---------------------
                    #endregion
                    
                    #region " L3FILE コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"L3FILE", out パラメータ ) )
                    {
                        block.File[ 2 ] = パラメータ;
                        blockが有効 = true;
                        continue;
                    }
                    //---------------------
                    #endregion
                    
                    #region " L4FILE コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"L4FILE", out パラメータ ) )
                    {
                        block.File[ 3 ] = パラメータ;
                        blockが有効 = true;
                        continue;
                    }
                    //---------------------
                    #endregion
                    
                    #region " L5FILE コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"L5FILE", out パラメータ ) )
                    {
                        block.File[ 4 ] = パラメータ;
                        blockが有効 = true;
                        continue;
                    }
                    //---------------------
                    #endregion
                    
                    #region " L1LABEL コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"L1LABEL", out パラメータ ) )
                    {
                        block.Label[ 0 ] = パラメータ;
                        blockが有効 = true;
                        continue;
                    }
                    //---------------------
                    #endregion
                    
                    #region " L2LABEL コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"L2LABEL", out パラメータ ) )
                    {
                        block.Label[ 1 ] = パラメータ;
                        blockが有効 = true;
                        continue;
                    }
                    //---------------------
                    #endregion
                    
                    #region " L3LABEL コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"L3LABEL", out パラメータ ) )
                    {
                        block.Label[ 2 ] = パラメータ;
                        blockが有効 = true;
                        continue;
                    }
                    //---------------------
                    #endregion
                    
                    #region " L4LABEL コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"L4LABEL", out パラメータ ) )
                    {
                        block.Label[ 3 ] = パラメータ;
                        blockが有効 = true;
                        continue;
                    }
                    //---------------------
                    #endregion
                    
                    #region " L5LABEL コマンド "
                    //---------------------
                    if( Utilities.コマンドのパラメータ文字列部分を返す( 行, @"L5LABEL", out パラメータ ) )
                    {
                        block.Label[ 4 ] = パラメータ;
                        blockが有効 = true;
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

            if( blockが有効 )
            {
                _FILEの指定があるのにLxLABELが省略されているときはデフォルトの名前をセットする( block );
                _LxLABELの指定があるのにFILEが省略されているときはなかったものとする( block );

                this.Blocks.Add( block ); // リストに追加。
            }
        }
        
        /// <summary>
        ///     set.def ファイルに保存する。
        /// </summary>
        public void 保存する( VariablePath SetDefファイルパス )
        {
            using var sw = new StreamWriter( SetDefファイルパス.変数なしパス );

            foreach( var block in this.Blocks )
            {
                sw.WriteLine( $"#TITLE: {block.Title}" );

                sw.WriteLine( $"#FONTCOLOR: #{block.FontColor.R:X2}{block.FontColor.G:X2}{block.FontColor.B:X2}" );

                for( int i = 0; i < block.File.Length; i++ )
                {
                    sw.WriteLine( "" );
                    sw.WriteLine( $"L{i + 1}LABEL: {( string.IsNullOrEmpty( block.Label[ i ] ) ? デフォルトのラベル[ i ] : block.Label[ i ] )}" );
                    sw.WriteLine( $"L{i + 1}FILE: {block.File[ i ]}" );
                }
            }
        }



        // ローカル


        private static void _FILEの指定があるのにLxLABELが省略されているときはデフォルトの名前をセットする( Block block )
        {
            for( int i = 0; i < 5; i++ )
            {
                if( !string.IsNullOrEmpty( block.File[ i ] ) &&
                    string.IsNullOrEmpty( block.Label[ i ] ) )
                {
                    block.Label[ i ] = デフォルトのラベル[ i ];
                }
            }
        }

        private static void _LxLABELの指定があるのにFILEが省略されているときはなかったものとする( Block block )
        {
            for( int i = 0; i < 5; i++ )
            {
                if( string.IsNullOrEmpty( block.File[ i ] ) &&
                    !string.IsNullOrEmpty( block.Label[ i ] ) )
                {
                    block.Label[ i ] = null;
                }
            }
        }
    }
}
