using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DTXMania2
{
    enum システムサウンド種別
    {
        [Alias( "CursorMove" )] カーソル移動音,
        [Alias( "Decide" )] 決定音,
        [Alias( "Cancel" )] 取消音,
        [Alias( "Change" )] 変更音,

        [Alias( "StageFailed" )] ステージ失敗,
        [Alias( "StageClear" )] ステージクリア,
        [Alias( "FullCombo" )] フルコンボ,
        [Alias( "Audience" )] 歓声,

        [Alias( "BootStage_Start" )] 起動ステージ_開始音,
        [Alias( "BootStage_LoopBGM" )] 起動ステージ_ループBGM,

        [Alias( "TitleStage_Start" )] タイトルステージ_開始音,
        [Alias( "TitleStage_LoopBGM" )] タイトルステージ_ループBGM,
        [Alias( "TitleStage_Decide" )] タイトルステージ_確定音,

        [Alias( "AuthStage_Start" )] 認証ステージ_開始音,
        [Alias( "AuthStage_LoopBGM" )] 認証ステージ_ループBGM,
        [Alias( "AuthStage_Decide" )] 認証ステージ_ログイン音,

        [Alias( "SelectStage_Start" )] 選曲ステージ_開始音,
        //[Alias( "SelectStage_LoopBGM" )] 選曲ステージ_ループBGM,    --> 未対応
        [Alias( "SelectStage_Decide" )] 選曲ステージ_曲決定音,

        [Alias( "OptionStage_Start" )] オプション設定ステージ_開始音,

        [Alias( "LoadingStage_Start" )] 曲読み込みステージ_開始音,
        [Alias( "LoadingStage_LoopBGM" )] 曲読み込みステージ_ループBGM,

        [Alias( "ExitStage_Start" )] 終了ステージ_開始音,
    }
}