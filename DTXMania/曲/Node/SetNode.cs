using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FDK;

namespace DTXMania
{
    class SetNode : Node
    {

        // 曲ノード[]


        /// <summary>
        ///		このset.defブロックに登録される、最大５つの曲ノード。
        /// </summary>
        public MusicNode[] MusicNodes = new MusicNode[ 5 ];



        // 難易度


        public override double 難易度
            => this.MusicNodes[ App進行描画.曲ツリー.フォーカス難易度 ].難易度;

        public override string 難易度ラベル
            => this.MusicNodes[ App進行描画.曲ツリー.フォーカス難易度 ].難易度ラベル;

        public int ユーザ希望難易度に最も近い難易度レベルを返す( int ユーザ希望難易度 )
        {
            if( null != this.MusicNodes[ ユーザ希望難易度 ] )
                return ユーザ希望難易度;    // 難易度ぴったりの曲があった


            // 現在のアンカレベルから、難易度上向きに検索開始。

            int 最も近いレベル = ユーザ希望難易度;

            for( int i = 0; i < 5; i++ )
            {
                if( null != this.MusicNodes[ 最も近いレベル ] )
                    break;  // 曲があった。

                // 曲がなかったので次の難易度レベルへGo。（5以上になったら0に戻る。）
                最も近いレベル = ( 最も近いレベル + 1 ) % 5;
            }


            // 見つかった曲がアンカより下のレベルだった場合……
            // アンカから下向きに検索すれば、もっとアンカに近い曲があるんじゃね？

            if( 最も近いレベル < ユーザ希望難易度 )
            {
                // 現在のアンカレベルから、難易度下向きに検索開始。

                最も近いレベル = ユーザ希望難易度;

                for( int i = 0; i < 5; i++ )
                {
                    if( null != this.MusicNodes[ 最も近いレベル ] )
                        break;  // 曲があった。

                    // 曲がなかったので次の難易度レベルへGo。（0未満になったら4に戻る。）
                    最も近いレベル = ( ( 最も近いレベル - 1 ) + 5 ) % 5;
                }
            }

            return 最も近いレベル;
        }



        // プレビュー画像とプレビュー音声


        /// <summary>
        ///		ノードを表す画像の SetNode 用オーバーライド。
        /// </summary>
        /// <remarks>
        ///		このプロパティで返す値には、現在フォーカス中の<see cref="SetNode.MusicNodes"/>のノード画像が優先的に使用される。
        ///		<see cref="SetNode.MusicNodes"/>のノード画像が無効（または null）なら、このプロパティで返す値には、set.def と同じ場所にあるthumb画像（または null）が使用される。
        /// </remarks>
        public override テクスチャ ノード画像
        {
            get
            {
                // (1) 現在のフォーカス難易度と同じ MusicNode のノード画像が有効なら、それを返す。
                var 現在の難易度のMusicNode = this.MusicNodes[ App進行描画.曲ツリー.フォーカス難易度 ];

                if( null != 現在の難易度のMusicNode?.ノード画像 )
                    return 現在の難易度のMusicNode.ノード画像;

                // (2) SetNode 自身の持つノード画像が有効ならそれを返す。
                if( null != this._SetNode自身のノード画像 )
                    return this._SetNode自身のノード画像;


                // (3) 現在のフォーカス難易度に一番近い MusicNode のノード画像が有効ならそれを返す。
                return this.MusicNodes[ this.ユーザ希望難易度に最も近い難易度レベルを返す( App進行描画.曲ツリー.ユーザ希望難易度 ) ].ノード画像;
            }
        }

        public override VariablePath プレビュー音声ファイルの絶対パス
            => this.MusicNodes[ App進行描画.曲ツリー.フォーカス難易度 ].プレビュー音声ファイルの絶対パス;

        private テクスチャ _SetNode自身のノード画像 = null;



        // 生成と終了


        /// <summary>
        ///     指定された <see cref="SetDef.Block"/> をもとに、初期化する。
        /// </summary>
        public SetNode( SetDef.Block block, VariablePath 基点フォルダパス, Node 親ノード = null )
        {
            this.タイトル = block.Title;
            this.親ノード = 親ノード;

            for( int i = 0; i < this.MusicNodes.Length; i++ )
            {
                this.MusicNodes[ i ] = null;

                if( block.File[ i ].Nullでも空でもない() )
                {
                    var 曲ファイルパス = new VariablePath( Path.Combine( 基点フォルダパス.変数なしパス, block.File[ i ] ) );

                    if( File.Exists( 曲ファイルパス.変数なしパス ) )
                    {
                        try
                        {
                            this.MusicNodes[ i ] = new MusicNode( Path.Combine( 基点フォルダパス.変数なしパス, block.File[ i ] ), this );
                            this.MusicNodes[ i ].難易度ラベル = block.Label[ i ];
                            this.MusicNodes[ i ].難易度 = 0.00f;   // 仮置き
                            this.子ノードリスト.Add( this.MusicNodes[ i ] );
                        }
                        catch
                        {
                            Log.ERROR( "SetNode 内での MusicNode の生成に失敗しました。" );
                        }
                    }
                    else
                    {
                        Log.ERROR( $"set.def 内に指定されたファイルが存在しません。無視します。[{曲ファイルパス.変数付きパス}] " );
                    }
                }
            }

            #region " set.def自身のノード画像は廃止 "
            //----------------
            // 基点フォルダパス（set.def ファイルと同じ場所）に画像ファイルがあるなら、それをノード画像として採用する。
            //var サムネイル画像ファイルパス =
            //    ( from ファイル名 in Directory.GetFiles( 基点フォルダパス.変数なしパス )
            //      where _対応するサムネイル画像名.Any( thumbファイル名 => ( Path.GetFileName( ファイル名 ).ToLower() == thumbファイル名 ) )
            //      select ファイル名 ).FirstOrDefault();
            //this._SetNode自身のノード画像 = ( null != サムネイル画像ファイルパス ) ? new テクスチャ( サムネイル画像ファイルパス ) : null;  // ないなら null
            //----------------
            #endregion
        }

        public override void Dispose()
        {
            // MusicNodes[] は 子ノードリスト に登録してあるので、以下で一緒に解放される。
            base.Dispose();
        }



        // private


        private readonly string[] _対応するサムネイル画像名 = { "thumb.png", "thumb.bmp", "thumb.jpg", "thumb.jpeg" };
    }
}
