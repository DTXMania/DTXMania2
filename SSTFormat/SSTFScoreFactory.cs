using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SSTFormat
{
    public static class SSTFScoreFactory
    {
        /// <summary>
        ///     バージョンとその生成関数、ひとつ前のバージョンからのアップデート関数のマッピング。
        /// </summary>
        private static List<(SSTFVersion version, Func<string, bool, ISSTFScore> create, Func<ISSTFScore, ISSTFScore> updateFrom)> _WorkMap
            = new List<(SSTFVersion version, Func<string, bool, ISSTFScore> create, Func<ISSTFScore, ISSTFScore> updateFrom)> {

                ( new SSTFVersion( 4, 0 ),
                    ( path, headerOnly ) => v004.スコア.SSTFファイルから生成する( path, headerOnly ),
                    ( score ) => new v004.スコア( (v003.スコア) score ) ),

                ( new SSTFVersion( 3, 4 ),
                    ( path, headerOnly ) => v003.スコア.ファイルから生成する( path, headerOnly ),
                    ( score ) => new v003.スコア( (v002.スコア) score ) ),

                ( new SSTFVersion( 2, 0 ),
                    ( path, headerOnly ) => new v002.スコア( path, headerOnly ), 
                    ( score ) => new v002.スコア( (v001_2.スコア) score ) ),

                ( new SSTFVersion( 1, 2 ),
                    ( path, headerOnly ) => new v001_2.スコア( path, headerOnly ),
                    ( score ) => new v001_2.スコア( (v001.スコア) score ) ),

                ( new SSTFVersion( 1, 0 ), 
                    ( path, headerOnly ) => new v001.スコア( path, headerOnly ),
                    null ),
            };

        /// <summary>
        ///     指定された譜面ファイルを読み込み、最新バージョンまでアップデートする。
        /// </summary>
        public static ISSTFScore CreateFromFile( string scorePath, bool headerOnly )
        {
            // ファイルからSSTFバージョンを取得する。
            var sstfVersion = SSTFVersion.CreateVersionFromFile( scorePath );

            // SSTFバージョンに対応するワークマップのインデックスを取得する。
            int mapIndex = _WorkMap.FindIndex( ( m ) => sstfVersion >= m.version );
            if( -1 == mapIndex )
                throw new Exception( "未対応のSSTFバージョンです。" );

            // マッピングを使ってスコアを生成する。
            var score = _WorkMap[ mapIndex ].create( scorePath, headerOnly );

            // マッピングを使ってスコアを最新バージョンまで1つずつバージョンアップする。
            while( 0 < mapIndex )
                score = _WorkMap[ --mapIndex ].updateFrom( score );

            return score;
        }
    }
}
