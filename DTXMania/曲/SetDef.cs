using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using SharpDX;
using FDK;

namespace DTXMania
{
    class SetDef
    {
        /// <summary>
        ///		最大５曲をレベル別に保有できるブロック。
        ///		set.def では任意個のブロックを宣言できる。（#TITLE行が登場するたび新しいブロックとみなされる）
        /// </summary>
        public class Block
        {
            /// <summary>
            ///		スコアのタイトル（#TITLE）を保持する。
            /// </summary>
            public string Title { get; set; }

            /// <summary>
            ///		スコアファイル名（#LxFILE）を保持する。
            ///		配列は [0～4] で、存在しないレベルは null となる。
            /// </summary>
            public string[] File { get; set; }

            /// <summary>
            ///		スコアのフォント色（#FONTCOLOR）を保持する。
            /// </summary>
            public Color FontColor { get; set; }

            /// <summary>
            ///		スコアのジャンル名を保持する。（現在は使われていない。）
            /// </summary>
            public string Genre { get; set; }

            /// <summary>
            ///		スコアのラベル（#LxLABEL）を保持する。
            ///		配列は[0～4] で、存在しないレベルは null となる。
            /// </summary>
            public string[] Label { get; set; }


            public Block()
            {
                this.Title = "(no title)";
                this.File = new string[ 5 ];
                this.FontColor = Color.White;
                this.Genre = "";
                this.Label = new string[ 5 ];
            }
        }

        public List<Block> Blocks = new List<Block>();


        public SetDef()
        {
        }

        public static SetDef 復元する( VariablePath Set定義ファイルパス )
        {
            var setDef = new SetDef();

            using( var sr = new StreamReader( Set定義ファイルパス.変数なしパス, Encoding.GetEncoding( 932/*Shift-JIS*/ ) ) )
            {
                string 行;
                var block = new Block();
                var blockが有効 = false;

                while( ( 行 = sr.ReadLine() ) != null )
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
                                setDef.Blocks.Add( block ); // リストに追加して
                                block = new Block();        // 新規作成。
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
                    setDef.Blocks.Add( block ); // リストに追加。
                }
            }

            return setDef;
        }


        private static void _FILEの指定があるのにLxLABELが省略されているときはデフォルトの名前をセットする( Block block )
        {
            var デフォルトのラベル = new string[] { "BASIC", "ADVANCED", "EXTREME", "MASTER", "ULTIMATE" };

            for( int i = 0; i < 5; i++ )
            {
                if( block.File[ i ].Nullでも空でもない() &&
                    block.Label[ i ].Nullまたは空である() )
                {
                    block.Label[ i ] = デフォルトのラベル[ i ];
                }
            }
        }

        private static void _LxLABELの指定があるのにFILEが省略されているときはなかったものとする( Block block )
        {
            for( int i = 0; i < 5; i++ )
            {
                if( block.File[ i ].Nullまたは空である() &&
                    block.Label[ i ].Nullでも空でもない() )
                {
                    block.Label[ i ] = null;
                }
            }
        }
    }
}
