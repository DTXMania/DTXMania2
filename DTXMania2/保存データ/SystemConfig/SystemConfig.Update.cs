﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using YamlDotNet.RepresentationModel;
using FDK;

namespace DTXMania2
{
    partial class SystemConfig
    {
        public static void 最新版にバージョンアップする()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            var path = new VariablePath( @"$(AppData)\Configuration.yaml" );
            int version = 0;

            if( !File.Exists( path.変数なしパス ) )
                return;

            #region " YAML階層のルートノード 'Version' を検索し、バージョン値を取得する。"
            //----------------
            {
                var yamlText = File.ReadAllText( path.変数なしパス );
                var yamlStream = new YamlStream();
                yamlStream.Load( new StringReader( yamlText ) );
                var rootMapping = (YamlMappingNode)yamlStream.Documents[ 0 ].RootNode;
                var versionNode = new YamlScalarNode( "Version" );
                if( rootMapping.Children.ContainsKey( versionNode ) )
                {
                    var versionValue = rootMapping.Children[ versionNode ] as YamlScalarNode;
                    version = int.Parse( versionValue?.Value ?? "1" );  // 取得
                }
            }
            //----------------
            #endregion

            while( version < SystemConfig.VERSION )
            {
                switch( version )
                {
                    case 1:
                    {
                        break;  // 存在しない
                    }
                    case 2:
                    {
                        #region " 2 → 3 "
                        //----------------
                        var v2 = old.SystemConfig.v002_システム設定.読み込む( path );
                        var v3 = new old.SystemConfig.v003_システム設定( v2 );
                        v3.保存する( path );
                        version = v3.Version;
                        break;
                        //----------------
                        #endregion
                    }
                    case 3:
                    {
                        #region " 3 → 4 "
                        //----------------
                        var v3 = old.SystemConfig.v003_システム設定.読み込む( path );
                        var v4 = new old.SystemConfig.v004_SystemConfig( v3 );
                        v4.保存する();
                        version = v4.Version;
                        break;
                        //----------------
                        #endregion
                    }
                    case 4:
                    {
                        #region " 4 → 5 "
                        //----------------
                        var v4 = old.SystemConfig.v004_SystemConfig.読み込む( path );
                        var v5 = new old.SystemConfig.v005_SystemConfig( v4 );
                        v5.保存する();
                        version = v5.Version;
                        break;
                        //----------------
                        #endregion
                    }
                    case 5:
                    {
                        #region " 5 → 6 "
                        //----------------
                        var v5 = old.SystemConfig.v005_SystemConfig.読み込む( path );
                        var v6 = new old.SystemConfig.v006_SystemConfig( v5 );
                        v6.保存する();
                        version = v6.Version;
                        break;
                        //----------------
                        #endregion
                    }
                    case 6:
                    {
                        #region " 6 → 最新版 "
                        //----------------
                        var v6 = old.SystemConfig.v006_SystemConfig.読み込む( path );
                        var v7 = new SystemConfig( v6 );
                        v7.保存する();
                        version = v7.Version;
                        break;
                        //----------------
                        #endregion
                    }
                }
            }
        }
    }
}
