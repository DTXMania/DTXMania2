using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SharpDX;

namespace DTXMania2.曲
{
    /// <summary>
    ///     1～5個の個別の難易度を持つ曲をまとめるクラス。
    /// </summary>
    partial class Song : IDisposable
    {

        // 外部依存アクション(static)


        /// <summary>
        ///     現在選択されている難易度レベル（0～4）を取得して返す。
        ///     外部から設定すること。
        /// </summary>
        public static Func<int> 現在の難易度レベル { get; set; } = () => throw new NotImplementedException();



        // プロパティ


        public Score フォーカス譜面 => this.ユーザ希望難易度に最も近い難易度レベルの譜面を返す( Song.現在の難易度レベル() );

        public Score?[] 譜面リスト { get; } = new Score?[ 5 ] { null, null, null, null, null };

        public static Color4[] 難易度色リスト { get; } = new Color4[ 5 ] {
             new Color4( 0xfffe9551 ),  // BASIC 相当
             new Color4( 0xff00aaeb ),  // ADVANCED 相当
             new Color4( 0xff7d5cfe ),  // EXTREME 相当
             new Color4( 0xfffe55c6 ),  // MASTER 相当
             new Color4( 0xff2b28ff ),  // ULTIMATE 相当
        };



        // 生成と終了


        /// <summary>
        ///     単一譜面から生成する。
        /// </summary>
        public Song( VariablePath 譜面ファイルの絶対パス )
        {
            this.譜面リスト[ 2 ] = new Score() {
                難易度ラベル = "FREE",
                譜面 = new ScoreDBRecord() {
                    //Title = "(new song!)",
                    Title = Path.GetFileName( 譜面ファイルの絶対パス.変数なしパス ),
                    ScorePath = 譜面ファイルの絶対パス.変数なしパス,
                },
                譜面の属性 = null,
                最高記録 = null,
            };
        }

        /// <summary>
        ///     複数譜面（set.def）から生成する。
        /// </summary>
        public Song( SetDef.Block block, VariablePath setDefのあるフォルダの絶対パス )
        {
            for( int i = 0; i < 5; i++ )
            {
                if( string.IsNullOrEmpty( block.File[ i ] ) )
                    continue;

                var socre_path = Path.Combine( setDefのあるフォルダの絶対パス.変数なしパス, block.File[ i ]! );

                this.譜面リスト[ i ] = new Score() {
                    難易度ラベル = block.Label[ i ] ?? SetDef.デフォルトのラベル[ i ], // LABEL は省略可
                    譜面 = new ScoreDBRecord() {
                        //Title = "(new song!)",
                        Title = Path.GetFileName( socre_path ),
                        ScorePath = socre_path,
                    },
                    譜面の属性 = null,
                    最高記録 = null,
                };
            }
        }

        public virtual void Dispose()
        {
            foreach( var score in this.譜面リスト )
                score?.Dispose();
        }



        // その他


        public int ユーザ希望難易度に最も近い難易度レベルを返す( int ユーザ希望難易度レベル0to4 )
        {
            if( null != this.譜面リスト[ ユーザ希望難易度レベル0to4 ] )
                return ユーザ希望難易度レベル0to4;    // 難易度ぴったりの曲があった


            // 現在のアンカレベルから、難易度上向きに検索開始。

            int 最も近いレベル = ユーザ希望難易度レベル0to4;

            for( int i = 0; i < 5; i++ )
            {
                if( null != this.譜面リスト[ 最も近いレベル ] )
                    break;  // 曲があった。

                // 曲がなかったので次の難易度レベルへGo。（5以上になったら0に戻る。）
                最も近いレベル = ( 最も近いレベル + 1 ) % 5;
            }

            if( 最も近いレベル == ユーザ希望難易度レベル0to4 )
            {
                // 5回回って見つからなかったということはすべて null だということ。
                throw new Exception( "譜面リストがすべて null です。" );
            }

            // 見つかった曲がアンカより下のレベルだった場合……
            // アンカから下向きに検索すれば、もっとアンカに近い曲（あるいは同じ曲）にヒットするはず。

            if( 最も近いレベル < ユーザ希望難易度レベル0to4 )
            {
                // 現在のアンカレベルから、難易度下向きに検索開始。

                最も近いレベル = ユーザ希望難易度レベル0to4;

                for( int i = 0; i < 5; i++ )
                {
                    if( null != this.譜面リスト[ 最も近いレベル ] )
                        break;  // 曲があった。

                    // 曲がなかったので次の難易度レベルへGo。（0未満になったら4に戻る。）
                    最も近いレベル = ( ( 最も近いレベル - 1 ) + 5 ) % 5;
                }
            }

            return 最も近いレベル;
        }

        public Score ユーザ希望難易度に最も近い難易度レベルの譜面を返す( int ユーザ希望難易度レベル0to4 )
        {
            int level0to4 = this.ユーザ希望難易度に最も近い難易度レベルを返す( ユーザ希望難易度レベル0to4 );
            return this.譜面リスト[ level0to4 ]!;
        }
    }
}
